using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>NOTES-FROM-PLANNING.md entry 113 section 3's tests for hit probability at distance and distance normalisation.</summary>
public class ProjectionTests
{
    private static readonly BallisticInput Input = SolverUse.Input(
        new Rifle("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 },
        new Load("H4350 41.5", null) { MuzzleVelocityFps = 2710, BallisticCoefficient = 0.326, DragModel = DragModel.G7, BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140 },
        new AirInput())!;

    private static readonly Estimate Sigma = new(0.10, 0.083, 0.125);

    /// <summary>With neither sigma_V nor sigma_wind, the group at d is the group at d0 scaled by d / d0, at the estimate and at both ends.</summary>
    [Fact]
    public void WithNoVelocityOrWindSpreadItIsAngularScalingExactly()
    {
        foreach (double? zero in new double?[] { null, 0 })
        {
            var (projection, refusal) = Projection.Project(Input, Sigma, 100, 600, zero, zero);
            Assert.Null(refusal);
            Assert.True(projection!.AngularOnly);
            foreach (var (at, sigma) in new[] { (projection.Point, Sigma.Value), (projection.Lower, Sigma.Lower), (projection.Upper, Sigma.Upper) })
            {
                Assert.Equal(sigma * 6, at.AcrossInches, 12);
                Assert.Equal(sigma * 6, at.UpDownInches, 12);
            }
        }
    }

    /// <summary>With a velocity spread, vertical sigma grows faster than linearly with distance, and at the distance shot it is the sigma measured.</summary>
    [Fact]
    public void AVelocitySpreadMakesVerticalGrowFasterThanDistance()
    {
        double previous = 0;
        foreach (double to in new[] { 100.0, 300, 500, 700, 900 })
        {
            var (projection, _) = Projection.Project(Input, Sigma, 100, to, 12, null);
            double perYard = projection!.Point.UpDownInches / to;
            Assert.True(perYard > previous, $"{to} yd: {perYard} per yd after {previous}");
            previous = perYard;
            Assert.Equal(Sigma.Value * to / 100, projection.Point.AcrossInches, 12);
            Assert.False(projection.AngularOnly);
        }

        Assert.Equal(Sigma.Value, Projection.Project(Input, Sigma, 100, 100, 12, null).Projection!.Point.UpDownInches, 9);
        Assert.True(Projection.DropPerFps(Input, 600) > 0);
        Assert.True(Math.Abs(Projection.DriftPerMph(Input, 600)) > Math.Abs(Projection.DriftPerMph(Input, 300)));
    }

    /// <summary>A centred circle with equal axes is the Rayleigh closed form, and the integration off centre agrees with it at the centre.</summary>
    [Fact]
    public void ACentredCircleWithEqualAxesMatchesTheRayleighClosedForm()
    {
        foreach (double r in new[] { 0.05, 0.1, 0.25, 0.5 })
        {
            double closed = 1 - Math.Exp(-r * r / (2 * 0.1 * 0.1));
            Assert.Equal(closed, Projection.HitCircle(0, 0, 0.1, 0.1, r), 12);
            Assert.Equal(closed, Projection.HitCircleIntegrated(0, 0, 0.1, 0.1, r), 9);
        }

        // Off centre, the chance falls, and a rectangle is the product of its two axes.
        Assert.True(Projection.HitCircle(0.1, 0, 0.1, 0.1, 0.2) < Projection.HitCircle(0, 0, 0.1, 0.1, 0.2));
        Assert.Equal(Math.Pow(Distributions.NormalCdf(1) - Distributions.NormalCdf(-1), 2), Projection.HitRectangle(0, 0, 0.1, 0.1, 0.2, 0.2), 12);
    }

    /// <summary>A velocity spread whose share at the distance shot is at least the sigma measured is refused, and says why.</summary>
    [Fact]
    public void AVelocitySpreadTooLargeForTheGroupIsRefused()
    {
        double perFps = Projection.DropPerFps(Input, 300);
        var (projection, refusal) = Projection.Project(Input, new Estimate(0.2, 0.15, 0.3), 300, 600, 0.2 / perFps * 1.01, null);
        Assert.Null(projection);
        Assert.Contains("too large for this group", refusal, StringComparison.Ordinal);

        var (allowed, _) = Projection.Project(Input, new Estimate(0.2, 0.15, 0.3), 300, 600, 0.17 / perFps, null);
        Assert.True(allowed!.Lower.LowerEndAllVelocity);
        Assert.False(allowed.Point.LowerEndAllVelocity);
    }
}
