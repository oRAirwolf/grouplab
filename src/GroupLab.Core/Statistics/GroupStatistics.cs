using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>A value with its two-sided confidence interval.</summary>
public readonly record struct Estimate(double Value, double Lower, double Upper);

/// <summary>
/// Rayleigh sigma and the circular measures that are fixed multiples of it, docs/STATISTICS.md sections 3.2 to 3.4: the
/// sum of squared radii it came from, its degrees of freedom and bias-correction factor, and every interval, which is the
/// sigma interval rescaled because the multiples carry no sampling error.
/// </summary>
public sealed record RayleighEstimate(double SumOfSquaredRadii, double DegreesOfFreedom, double CorrectionFactor, double Level, Estimate Sigma)
{
    /// <summary>Mean radius as a multiple of sigma, sqrt(pi / 2).</summary>
    public const double MeanRadiusFactor = 1.2533141373155002512;

    /// <summary>Median radius as a multiple of sigma, sqrt(2 ln 2), which is also CEP(0.50).</summary>
    public const double MedianRadiusFactor = 1.1774100225154746910;

    /// <summary>Radial standard deviation as a multiple of sigma, sqrt((4 - pi) / 2).</summary>
    public const double RadialSdFactor = 0.65513637756203355309;

    public Estimate MeanRadius => Scale(MeanRadiusFactor);

    public Estimate MedianRadius => Scale(MedianRadiusFactor);

    public Estimate RadialSd => Scale(RadialSdFactor);

    /// <summary>The circular CEP at probability <paramref name="q"/>, sigma sqrt(-2 ln(1 - q)), section 3.4.</summary>
    public Estimate Cep(double q) => Scale(Math.Sqrt(-2 * SpecialFunctions.Log1P(-q)));

    private Estimate Scale(double k) => new(Sigma.Value * k, Sigma.Lower * k, Sigma.Upper * k);
}

/// <summary>The shape of a 2 by 2 covariance or ellipse matrix: its eigenvalues, the major axis's angle, and the descriptive ratios shotGroups reports.</summary>
public sealed record EllipseShape(double Major, double Minor, double AngleDegrees)
{
    /// <summary>The ratio of the semi-axes, sqrt(major / minor).</summary>
    public double AspectRatio => Math.Sqrt(Major / Minor);

    /// <summary>1 - minor semi-axis / major semi-axis.</summary>
    public double Flattening => 1 - Math.Sqrt(Minor / Major);

    public double Trace => Major + Minor;

    public double Determinant => Major * Minor;
}

/// <summary>
/// The single-group statistics of docs/STATISTICS.md: dispersion (section 3), CEP and hit probability (section 4), per-axis
/// spread and the centre with their intervals (section 6), and the error ellipse (section 7). Every function takes shots as
/// linear offsets at the target plane in one unit, section 13, and returns the same unit.
/// </summary>
public static class GroupStatistics
{
    /// <summary>The mean of the shots.</summary>
    public static PointD Centre(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        double sx = 0, sy = 0;
        foreach (var s in shots)
        {
            sx += s.X;
            sy += s.Y;
        }

        return new PointD(sx / shots.Count, sy / shots.Count);
    }

    /// <summary>The sample covariance, n - 1 in the denominator, as (xx, xy, yy).</summary>
    public static (double Xx, double Xy, double Yy) Covariance(IReadOnlyList<PointD> shots)
    {
        var c = Centre(shots);
        double xx = 0, xy = 0, yy = 0;
        foreach (var s in shots)
        {
            double dx = s.X - c.X, dy = s.Y - c.Y;
            xx += dx * dx;
            xy += dx * dy;
            yy += dy * dy;
        }

        int d = shots.Count - 1;
        return (xx / d, xy / d, yy / d);
    }

    /// <summary>The distance of every shot from <paramref name="centre"/>, in shot order.</summary>
    public static double[] Radii(IReadOnlyList<PointD> shots, PointD centre) =>
        [.. shots.Select(s => Math.Sqrt(((s.X - centre.X) * (s.X - centre.X)) + ((s.Y - centre.Y) * (s.Y - centre.Y))))];

    /// <summary>
    /// Rayleigh sigma, docs/STATISTICS.md section 3.2. With the centre estimated from the data the sum of squared radii is on
    /// 2(n - 1) degrees of freedom and the correction is 1 / c4(2n - 1); with a known point of aim it is on 2n and 1 / c4(2n + 1).
    /// The two cases differ in three places at once, which is why they share this one function and never a copy.
    /// </summary>
    /// <param name="knownCentre">The true point of aim, when one is supplied; otherwise the group centre is estimated.</param>
    public static RayleighEstimate Rayleigh(IReadOnlyList<PointD> shots, double level = 0.95, PointD? knownCentre = null)
    {
        ArgumentNullException.ThrowIfNull(shots);
        var centre = knownCentre ?? Centre(shots);
        double sum = 0;
        foreach (var s in shots)
        {
            sum += ((s.X - centre.X) * (s.X - centre.X)) + ((s.Y - centre.Y) * (s.Y - centre.Y));
        }

        int n = shots.Count;
        return knownCentre is null ? FromSumOfSquares(sum, 2.0 * (n - 1), (2.0 * n) - 1, level) : FromSumOfSquares(sum, 2.0 * n, (2.0 * n) + 1, level);
    }

    /// <summary>
    /// Rayleigh sigma from a sum of squared radii, its chi-square degrees of freedom, and the argument of c4, section 3.3: the
    /// correction multiplies the interval's endpoints as well as the estimate, as shotGroups does and GroupLab matches
    /// (docs/PHASE1-BRIEF.md section 5), so the interval stays centred on the corrected estimate and its coverage for sigma
    /// itself is not exactly nominal. Pooled groups, section 11, come through here with df = 2(N - k) and c4(2N - 2k + 1).
    /// </summary>
    public static RayleighEstimate FromSumOfSquares(double sumOfSquares, double degreesOfFreedom, double c4Argument, double level = 0.95)
    {
        double correction = 1 / SpecialFunctions.C4(c4Argument), alpha = 1 - level;
        double sigma = correction * Math.Sqrt(sumOfSquares / degreesOfFreedom);
        double lower = correction * Math.Sqrt(sumOfSquares / Distributions.ChiSquareQuantile(1 - (alpha / 2), degreesOfFreedom));
        double upper = correction * Math.Sqrt(sumOfSquares / Distributions.ChiSquareQuantile(alpha / 2, degreesOfFreedom));
        return new RayleighEstimate(sumOfSquares, degreesOfFreedom, correction, level, new Estimate(sigma, lower, upper));
    }

    /// <summary>
    /// The per-axis standard deviations with their intervals, docs/STATISTICS.md section 6: univariate, so chi-square on
    /// n - 1 degrees of freedom, not the 2(n - 1) of sigma.
    /// </summary>
    public static (Estimate X, Estimate Y) AxisSd(IReadOnlyList<PointD> shots, double level = 0.95)
    {
        var (xx, _, yy) = Covariance(shots);
        double df = shots.Count - 1, alpha = 1 - level;
        double hi = Distributions.ChiSquareQuantile(1 - (alpha / 2), df), lo = Distributions.ChiSquareQuantile(alpha / 2, df);
        Estimate Axis(double variance) => new(Math.Sqrt(variance), Math.Sqrt(variance * df / hi), Math.Sqrt(variance * df / lo));
        return (Axis(xx), Axis(yy));
    }

    /// <summary>The centre with its per-axis t intervals on n - 1 degrees of freedom, docs/STATISTICS.md section 6.</summary>
    public static (Estimate X, Estimate Y) CentreIntervals(IReadOnlyList<PointD> shots, double level = 0.95)
    {
        var c = Centre(shots);
        var (xx, _, yy) = Covariance(shots);
        int n = shots.Count;
        double t = Distributions.StudentTQuantile(1 - ((1 - level) / 2), n - 1);
        Estimate Axis(double mean, double variance)
        {
            double half = t * Math.Sqrt(variance / n);
            return new Estimate(mean, mean - half, mean + half);
        }

        return (Axis(c.X, xx), Axis(c.Y, yy));
    }

    /// <summary>
    /// Hotelling's one-sample T^2 test of the centre against a point, docs/STATISTICS.md section 6: T^2 = n d' S^-1 d, reported as
    /// its exact F on (2, n - 2) degrees of freedom, F = (n - 2) / (2 (n - 1)) T^2, with the upper-tail p-value.
    /// </summary>
    public static (double F, double PValue) HotellingOneSample(IReadOnlyList<PointD> shots, PointD point)
    {
        var c = Centre(shots);
        var (xx, xy, yy) = Covariance(shots);
        int n = shots.Count;
        double dx = c.X - point.X, dy = c.Y - point.Y, det = (xx * yy) - (xy * xy);
        double t2 = n * ((yy * dx * dx) - (2 * xy * dx * dy) + (xx * dy * dy)) / det;
        double f = (n - 2.0) / (2.0 * (n - 1)) * t2;
        return (f, Distributions.F(f, 2, n - 2).Upper);
    }

    /// <summary>The eigen-decomposition of a symmetric 2 by 2 matrix, with the major axis's angle in degrees in [0, 180).</summary>
    public static EllipseShape Shape(double xx, double xy, double yy)
    {
        double mean = (xx + yy) / 2, root = Math.Sqrt((((xx - yy) / 2) * ((xx - yy) / 2)) + (xy * xy));
        double major = mean + root, minor = mean - root;
        double angle = 0.5 * Math.Atan2(2 * xy, xx - yy) * 180 / Math.PI;
        angle = angle < 0 ? angle + 180 : angle;
        return new EllipseShape(major, minor, angle);
    }

    /// <summary>
    /// The confidence ellipse of the shots at <paramref name="level"/>: the covariance's ellipse scaled by
    /// sqrt(2 F(level; 2, n - 1)), which is shotGroups' <c>magFac</c>. It describes where shots fall, not where the centre is.
    /// </summary>
    public static (double MagnificationFactor, double SemiMajor, double SemiMinor, EllipseShape Shape) ConfidenceEllipse(IReadOnlyList<PointD> shots, double level)
    {
        var (xx, xy, yy) = Covariance(shots);
        var shape = Shape(xx, xy, yy);
        double mag = Math.Sqrt(2 * Distributions.FQuantile(level, 2, shots.Count - 1));
        return (mag, mag * Math.Sqrt(shape.Major), mag * Math.Sqrt(shape.Minor), shape);
    }

    /// <summary>The Hoyt parameters of a covariance, docs/STATISTICS.md section 2: q, the ratio of the minor to the major standard deviation, and omega, the trace.</summary>
    public static (double Q, double Omega) Hoyt(double xx, double xy, double yy)
    {
        var shape = Shape(xx, xy, yy);
        return (Math.Sqrt(shape.Minor / shape.Major), shape.Trace);
    }

    /// <summary>
    /// The probability that a centred bivariate normal with principal variances <paramref name="major"/> and
    /// <paramref name="minor"/> falls within <paramref name="r"/>: the Hoyt CDF, section 4, as an integral over angle of the
    /// radial integral in closed form, (1 / (2 pi sqrt(major minor))) int (1 - exp(-a r^2 / 2)) / a dtheta with a =
    /// cos^2 / major + sin^2 / minor. The integrand is smooth and periodic, so the trapezoid rule converges geometrically;
    /// the rule is doubled until two estimates agree to machine precision.
    /// </summary>
    public static double HoytCdf(double r, double major, double minor)
    {
        if (r <= 0)
        {
            return 0;
        }

        double scale = 1 / (2 * Math.PI * Math.Sqrt(major * minor)), previous = double.NaN;
        for (int points = 64; points <= 1 << 20; points *= 2)
        {
            double sum = 0;
            for (int i = 0; i < points; i++)
            {
                double theta = 2 * Math.PI * i / points, c = Math.Cos(theta), s = Math.Sin(theta);
                double a = (c * c / major) + (s * s / minor);
                sum += -SpecialFunctions.ExpM1(-a * r * r / 2) / a;
            }

            double value = scale * sum * 2 * Math.PI / points;
            if (Math.Abs(value - previous) <= 1e-16 * Math.Abs(value))
            {
                return value;
            }

            previous = value;
        }

        return previous;
    }

    /// <summary>The Hoyt density in r, the derivative of <see cref="HoytCdf"/>, by the same rule.</summary>
    private static double HoytDensity(double r, double major, double minor)
    {
        const int points = 4096;
        double sum = 0;
        for (int i = 0; i < points; i++)
        {
            double theta = 2 * Math.PI * i / points, c = Math.Cos(theta), s = Math.Sin(theta);
            sum += Math.Exp(-((c * c / major) + (s * s / minor)) * r * r / 2);
        }

        return r * sum / (points * Math.Sqrt(major * minor));
    }

    /// <summary>
    /// CEP under the correlated bivariate normal, shotGroups' <c>CorrNormal</c>, docs/STATISTICS.md section 4: the Hoyt
    /// quantile of the sample covariance, found by Newton's method on <see cref="HoytCdf"/> from the circular estimate.
    /// </summary>
    public static double CepCorrNormal(double xx, double xy, double yy, double q)
    {
        var shape = Shape(xx, xy, yy);
        double r = Math.Sqrt(shape.Trace / 2 * -2 * SpecialFunctions.Log1P(-q)), low = 0, high = double.PositiveInfinity;
        for (int i = 0; i < 200; i++)
        {
            double error = HoytCdf(r, shape.Major, shape.Minor) - q;
            if (error < 0)
            {
                low = r;
            }
            else
            {
                high = r;
            }

            double next = r - (error / HoytDensity(r, shape.Major, shape.Minor));
            if (!(next > low && next < high))
            {
                next = double.IsFinite(high) ? (low + high) / 2 : r * 2;
            }

            if (Math.Abs(next - r) <= 1e-15 * r)
            {
                return next;
            }

            r = next;
        }

        return r;
    }

    /// <summary>
    /// CEP by the Grubbs-Patnaik approximation, docs/STATISTICS.md section 4: the squared radius matched in mean m = trace S and
    /// variance v = 2 trace S^2 to a scaled central chi-square, (v / 2m) chi^2 on 2 m^2 / v degrees of freedom.
    /// </summary>
    public static double CepGrubbsPatnaik(double xx, double xy, double yy, double q)
    {
        double m = xx + yy, v = 2 * ((xx * xx) + (2 * xy * xy) + (yy * yy));
        return Math.Sqrt(v / (2 * m) * Distributions.ChiSquareQuantile(q, 2 * m * m / v));
    }

    /// <summary>The probability of a hit within <paramref name="r"/> under the Grubbs-Patnaik approximation.</summary>
    public static double HitProbabilityGrubbsPatnaik(double xx, double xy, double yy, double r)
    {
        double m = xx + yy, v = 2 * ((xx * xx) + (2 * xy * xy) + (yy * yy));
        return Distributions.ChiSquare(r * r * 2 * m / v, 2 * m * m / v).Lower;
    }

    /// <summary>The probability of a hit within <paramref name="r"/> under the correlated bivariate normal.</summary>
    public static double HitProbabilityCorrNormal(double xx, double xy, double yy, double r)
    {
        var shape = Shape(xx, xy, yy);
        return HoytCdf(r, shape.Major, shape.Minor);
    }

    /// <summary>The probability of a hit within <paramref name="r"/> under the circular model, 1 - exp(-r^2 / 2 sigma^2), section 4.</summary>
    public static double HitProbabilityRayleigh(double sigma, double r) => -SpecialFunctions.ExpM1(-r * r / (2 * sigma * sigma));
}
