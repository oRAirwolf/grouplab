using GroupLab.Core.Statistics;

namespace GroupLab.Core.Ballistics;

/// <summary>A group's dispersion carried to another distance: sigma across and up and down there, in inches, at one sigma from the distance shot.</summary>
public sealed record ProjectedSigma(double AcrossInches, double UpDownInches, bool LowerEndAllVelocity);

/// <summary>
/// A group carried to another distance, NOTES-FROM-PLANNING.md entry 113 section 3: its sigma there at the point estimate and at both ends of
/// the interval measured at the distance shot, the velocity and wind terms that went into it, and whether it is only angular scaling.
/// </summary>
public sealed record DistanceProjection(
    double FromYards,
    double ToYards,
    ProjectedSigma Point,
    ProjectedSigma Lower,
    ProjectedSigma Upper,
    double VelocityAtFromInches,
    double VelocityAtToInches,
    double WindAtToInches,
    bool AngularOnly);

/// <summary>The probability of a hit, at the lower end of the sigma interval, the point estimate and the upper end: a range, never one number.</summary>
public sealed record HitRange(double AtLowerSigma, double AtPoint, double AtUpperSigma);

/// <summary>
/// Hit probability at a distance other than the one shot, and distance normalisation, propagated through the solver rather than by scaling a
/// group linearly (DESIGN.md section 3, specified by entry 113 section 3). At distance d the shots are bivariate normal about the aim plus the
/// zero offset carried to d, with
/// <code>
/// sigma_x(d)^2 = (sigma_x,ang d)^2 + (d drift / d wind  sigma_wind)^2
/// sigma_y(d)^2 = (sigma_y,ang d)^2 + (d drop / d V  sigma_V)^2
/// </code>
/// the partial derivatives by finite difference from the solver at d. The angular sigma measured at d0 already holds the velocity's share
/// there, so with sigma_V given that share is taken out in quadrature first, and a subtraction that goes negative is refused: the sigma_V
/// entered is too large for the group measured. With neither sigma_V nor sigma_wind the result is angular scaling exactly, and it says so.
/// </summary>
public static class Projection
{
    /// <summary>The drift per mph of full-value crosswind at a range, in inches. Drift is linear in the wind under the lag rule, so one mph is exact.</summary>
    public static double DriftPerMph(BallisticInput input, double yards)
    {
        ArgumentNullException.ThrowIfNull(input);
        double At(double mph) => BallisticSolver.Solve(input with { CrosswindMph = mph }, yards, yards).Points[^1].WindInches;
        return (At(1) - At(-1)) / 2;
    }

    /// <summary>
    /// The change of the bullet's height at a range per ft/s of muzzle velocity, in inches, with the sight left where the load's own velocity
    /// zeroes it: a central difference of 5 ft/s with the bore's angle held.
    /// </summary>
    public static double DropPerFps(BallisticInput input, double yards)
    {
        ArgumentNullException.ThrowIfNull(input);
        const double h = 5;
        double launch = BallisticSolver.Solve(input, input.ZeroRangeYards, input.ZeroRangeYards).ZeroAngleMoa;
        double At(double velocity) => BallisticSolver.Solve(input with { MuzzleVelocityFps = velocity, LaunchAngleMoa = launch }, yards, yards).Points[^1].DropInches;
        return (At(input.MuzzleVelocityFps + h) - At(input.MuzzleVelocityFps - h)) / (2 * h);
    }

    /// <summary>
    /// The group's per-axis sigma at <paramref name="toYards"/>, from its sigma at <paramref name="fromYards"/> with its interval, or null with the
    /// reason when the velocity's share at the distance shot exceeds the sigma measured there.
    /// </summary>
    public static (DistanceProjection? Projection, string? Refusal) Project(BallisticInput input, Estimate sigmaInches, double fromYards, double toYards, double? sigmaVelocityFps, double? sigmaWindMph)
    {
        ArgumentNullException.ThrowIfNull(input);
        double from = fromYards * 36, to = toYards * 36;
        double sv = sigmaVelocityFps ?? 0, sw = sigmaWindMph ?? 0;
        double velocityFrom = sv > 0 ? Math.Abs(DropPerFps(input, fromYards)) * sv : 0;
        double velocityTo = sv > 0 ? Math.Abs(DropPerFps(input, toYards)) * sv : 0;
        double windTo = sw > 0 ? Math.Abs(DriftPerMph(input, toYards)) * sw : 0;
        if (sigmaInches.Value * sigmaInches.Value <= velocityFrom * velocityFrom)
        {
            return (null, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"The muzzle velocity's spread alone would make the group {velocityFrom:0.000} in up and down at the distance shot, as much as or more than the {sigmaInches.Value:0.000} in measured, so the velocity SD entered is too large for this group. Check it before carrying the group anywhere."));
        }

        ProjectedSigma At(double sigma)
        {
            double across = sigma / from;
            double residual = (sigma * sigma) - (velocityFrom * velocityFrom);
            double upDown = Math.Sqrt(Math.Max(0, residual)) / from;
            return new ProjectedSigma(
                Math.Sqrt(Math.Pow(across * to, 2) + (windTo * windTo)),
                Math.Sqrt(Math.Pow(upDown * to, 2) + (velocityTo * velocityTo)),
                residual <= 0);
        }

        return (new DistanceProjection(fromYards, toYards, At(sigmaInches.Value), At(sigmaInches.Lower), At(sigmaInches.Upper), velocityFrom, velocityTo, windTo, sv <= 0 && sw <= 0), null);
    }

    /// <summary>
    /// The chance a shot lands inside a circle of <paramref name="radius"/> about the aim, the shots normal about (<paramref name="muX"/>,
    /// <paramref name="muY"/>) with independent axes. Centred, it is the engine's correlated-normal estimator, which is the Rayleigh closed form
    /// when the axes are equal; off centre it integrates the normal over the circle, one axis in closed form and the other by Simpson's rule in
    /// an angle that removes the edge's square root.
    /// </summary>
    public static double HitCircle(double muX, double muY, double sigmaX, double sigmaY, double radius)
    {
        if (radius <= 0)
        {
            return 0;
        }

        if (muX == 0 && muY == 0)
        {
            return sigmaX == sigmaY
                ? GroupStatistics.HitProbabilityRayleigh(sigmaX, radius)
                : GroupStatistics.HitProbabilityCorrNormal(sigmaX * sigmaX, 0, sigmaY * sigmaY, radius);
        }

        return HitCircleIntegrated(muX, muY, sigmaX, sigmaY, radius);
    }

    /// <summary>The off-centre circle by integration, public so the tests can hold it to the closed forms where those apply.</summary>
    public static double HitCircleIntegrated(double muX, double muY, double sigmaX, double sigmaY, double radius)
    {
        const int n = 2000;
        double h = Math.PI / n, sum = 0;
        for (int i = 0; i <= n; i++)
        {
            double t = (-Math.PI / 2) + (i * h);
            double x = radius * Math.Sin(t), half = radius * Math.Cos(t);
            double f = Distributions.NormalDensity((x - muX) / sigmaX) / sigmaX * radius * Math.Cos(t)
                * (Distributions.NormalCdf((half - muY) / sigmaY) - Distributions.NormalCdf((-half - muY) / sigmaY));
            sum += f * (i == 0 || i == n ? 1 : i % 2 == 1 ? 4 : 2);
        }

        return Math.Clamp(sum * h / 3, 0, 1);
    }

    /// <summary>
    /// The chance a shot lands inside a <paramref name="width"/> by <paramref name="height"/> rectangle centred on the aim. The model's axes are
    /// the rectangle's, independent, so it is the product of the two normal probabilities, exactly.
    /// </summary>
    public static double HitRectangle(double muX, double muY, double sigmaX, double sigmaY, double width, double height)
    {
        double Axis(double mu, double sigma, double side) =>
            Distributions.NormalCdf(((side / 2) - mu) / sigma) - Distributions.NormalCdf(((-side / 2) - mu) / sigma);
        return Axis(muX, sigmaX, width) * Axis(muY, sigmaY, height);
    }

    /// <summary>The hit range for a target: at the sigma interval's lower end, the point estimate and the upper end.</summary>
    public static HitRange Hit(DistanceProjection projection, double muX, double muY, Func<double, double, double, double, double> target)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(target);
        double At(ProjectedSigma s) => target(muX, muY, s.AcrossInches, s.UpDownInches);
        return new HitRange(At(projection.Lower), At(projection.Point), At(projection.Upper));
    }
}
