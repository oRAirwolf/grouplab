using GroupLab.Core.Ballistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// The solver's own behaviour, NOTES-FROM-PLANNING.md entry 110 sections 2b to 2e: shooting angle measured along the line of sight, the zero
/// found where it is applied, rows at their exact ranges, Miller's pressure term, the BC's reference atmosphere, and the terms not modelled.
/// The angle tolerances were written into these tests before they were first run.
/// </summary>
public class BallisticSolverTests
{
    /// <summary>The case entry 110 section 2a checked: a 175 grain .308 at 2700 fps, G7 0.243, zeroed at 100 yd.</summary>
    private static readonly BallisticInput Case = new(0.243, DragModel.G7, 2700, 175, PressureInHg: 29.92, HumidityPct: 50, CrosswindMph: 10);

    private static IReadOnlyList<TrajectoryPoint> Flat(double max = 1000, double step = 100) => BallisticSolver.Solve(Case, max, step).Points;

    [Fact]
    public void AngleZeroIsTheFlatTableExactly()
    {
        var flat = Flat();
        var level = BallisticSolver.Solve(Case with { AngleDegrees = 0 }, 1000, 100).Points;
        Assert.Equal(flat, level);
    }

    /// <summary>
    /// Entry 110 section 2b: the JavaScript read −297.6 MOA at 100 yd for a 5 degree angle. Measured along the line of sight, a small angle
    /// changes the drop at a short range by almost nothing.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    [InlineData(10)]
    [InlineData(-10)]
    public void ASmallAngleReadsAsTheLineOfSightNotAsItsRise(double degrees)
    {
        var inclined = BallisticSolver.Solve(Case with { AngleDegrees = degrees }, 1000, 100).Points;
        var flat = Flat();
        Assert.True(Math.Abs(inclined[1].DropMoa - flat[1].DropMoa) < 0.05, $"{degrees} degrees at 100 yd: {inclined[1].DropMoa:0.000} MOA against {flat[1].DropMoa:0.000} flat");
        for (int i = 1; i < inclined.Count; i++)
        {
            // Up or down, a shot at an angle strikes higher than a level one at the same range, never lower.
            Assert.True(inclined[i].DropMoa <= flat[i].DropMoa + 1e-9, $"{degrees} degrees at {inclined[i].RangeYards} yd: {inclined[i].DropMoa:0.000} MOA against {flat[i].DropMoa:0.000} flat");
        }
    }

    /// <summary>
    /// The rifleman's rule: at a slant range R and angle θ, the path about the line of sight is close to the level path at the horizontal
    /// distance R cos θ. Tolerance, stated before the test was run: within 5 percent of that level drop plus 0.25 in, to 600 yd, at 5 and 10
    /// degrees. The rule neglects the component of gravity along the path and the zero's own angle, both small at these angles and ranges.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(-10)]
    public void TheRiflemansRuleHoldsForASmallAngle(double degrees)
    {
        double cos = Math.Cos(degrees * Math.PI / 180);
        var inclined = BallisticSolver.Solve(Case with { AngleDegrees = degrees }, 600, 100).Points;
        foreach (var row in inclined.Where(p => p.RangeYards >= 200))
        {
            // The level path at exactly R cos θ, from a row placed there.
            var level = BallisticSolver.Solve(Case, row.RangeYards * cos, row.RangeYards * cos).Points[^1];
            Assert.Equal(row.RangeYards * cos, level.RangeYards, 9);
            double allowed = (0.05 * Math.Abs(level.DropInches)) + 0.25;
            Assert.True(Math.Abs(row.DropInches - level.DropInches) <= allowed,
                $"{degrees} degrees at {row.RangeYards} yd: {row.DropInches:0.00} in against {level.DropInches:0.00} in level at {level.RangeYards:0.0} yd, allowed {allowed:0.00}");
        }
    }

    /// <summary>Uphill and downhill at the same angle agree closely at short range. Tolerance, stated before the test was run: 0.05 MOA to 300 yd.</summary>
    [Fact]
    public void UphillAndDownhillAgreeAtShortRange()
    {
        var up = BallisticSolver.Solve(Case with { AngleDegrees = 10 }, 300, 50).Points;
        var down = BallisticSolver.Solve(Case with { AngleDegrees = -10 }, 300, 50).Points;
        for (int i = 1; i < up.Count; i++)
        {
            Assert.True(Math.Abs(up[i].DropMoa - down[i].DropMoa) <= 0.05, $"{up[i].RangeYards} yd: up {up[i].DropMoa:0.000}, down {down[i].DropMoa:0.000}");
        }
    }

    /// <summary>
    /// Entry 110 section 2e: the zero is found by RK4 on the trajectory it is applied to and each row is interpolated to its range, so the drop
    /// at the zero range is zero, where the JavaScript read 0.01 in, and every row is at its range exactly.
    /// </summary>
    [Fact]
    public void TheZeroReadsZeroAndEveryRowIsAtItsRange()
    {
        var flat = Flat(1000, 25);
        Assert.Equal(-1.5, flat[0].DropInches, 12);
        var zero = flat.Single(p => p.RangeYards == 100);
        Assert.True(Math.Abs(zero.DropInches) < 0.001, $"drop at the zero range {zero.DropInches:0.00000} in");
        Assert.Equal(Enumerable.Range(0, 41).Select(i => i * 25.0), flat.Select(p => p.RangeYards));

        var far = BallisticSolver.Solve(Case with { ZeroRangeYards = 300 }, 300, 300).Points[^1];
        Assert.True(Math.Abs(far.DropInches) < 0.001, $"drop at a 300 yd zero {far.DropInches:0.00000} in");
    }

    /// <summary>
    /// Entry 110 section 2e: the BC's reference atmosphere. Army Standard Metro, 59 °F, 29.5275 inHg and 78 percent humidity, has a density of
    /// 0.0751265 lb/ft³ by its definition; by the solver's own formula it comes within 0.1 percent of that, and a coefficient stated against it
    /// flies with about 1.8 percent more drag in the same air.
    /// </summary>
    [Fact]
    public void TheReferenceAtmosphereChangesTheDensityRatioByTheirDensities()
    {
        Assert.Equal(1.0, Atmosphere.DensityRatio(59, Atmosphere.StandardPressureInHg, 0), 12);
        double metro = Atmosphere.DensityRatio(59, Atmosphere.MetroPressureInHg, Atmosphere.MetroHumidityPct) * Atmosphere.StandardDensity;
        Assert.True(Math.Abs(metro / 0.0751265 - 1) < 0.001, $"Army Standard Metro density {metro:0.0000000} lb/ft³");

        foreach (var (t, p, h) in new[] { (59.0, 29.92, 50.0), (20.0, 30.4, 60.0), (90.0, 26.0, 20.0) })
        {
            double ratio = Atmosphere.DensityRatio(t, p, h, ReferenceAtmosphere.ArmyStandardMetro) / Atmosphere.DensityRatio(t, p, h, ReferenceAtmosphere.Icao);
            Assert.Equal(Atmosphere.StandardDensity / metro, ratio, 12);
            Assert.InRange(ratio, 1.017, 1.019);
        }

        // More drag, so more drop at 1000 yd.
        var icao = Flat()[^1];
        var asm = BallisticSolver.Solve(Case with { Reference = ReferenceAtmosphere.ArmyStandardMetro }, 1000, 100).Points[^1];
        Assert.True(asm.DropMoa > icao.DropMoa && asm.TimeOfFlight > icao.TimeOfFlight);
    }

    /// <summary>Entry 110 section 2e: Miller's correction scales with pressure as well as temperature, and at 29.92 inHg it is the temperature term alone.</summary>
    [Fact]
    public void MillersStabilityScalesWithPressure()
    {
        double standard = Stability.MillerStability(10, 0.308, 1.24, 175, 2700, 59);
        Assert.Equal(standard, Stability.MillerStability(10, 0.308, 1.24, 175, 2700, 59, 29.92), 12);
        Assert.Equal(standard * 29.92 / 25.0, Stability.MillerStability(10, 0.308, 1.24, 175, 2700, 59, 25.0), 12);
        Assert.True(double.IsNaN(Stability.MillerStability(0, 0.308, 1.24, 175, 2700, 59)));
    }

    /// <summary>
    /// Spin drift and the Coriolis horizontal term appear only with their inputs: to the right for a right-hand twist and in the northern
    /// hemisphere, to the left otherwise. Aerodynamic jump and the Coriolis vertical term are never computed, and every trajectory says so.
    /// </summary>
    [Fact]
    public void SpinAndCoriolisAppearWithTheirInputsAndTheRestSaysItIsNotModelled()
    {
        var plain = BallisticSolver.Solve(Case, 1000, 500);
        Assert.Null(plain.Stability);
        Assert.All(plain.Points, p => Assert.Null(p.SpinDriftInches));
        Assert.All(plain.Points, p => Assert.Null(p.CoriolisInches));
        Assert.Contains("Aerodynamic jump is not modelled.", plain.NotModelled);
        Assert.Contains("The Coriolis vertical (Eötvös) term is not modelled.", plain.NotModelled);

        var spun = BallisticSolver.Solve(Case with { TwistInches = 10, BulletDiameterInches = 0.308, BulletLengthInches = 1.24, LatitudeDegrees = 45 }, 1000, 500);
        Assert.NotNull(spun.Stability);
        Assert.True(spun.Points[^1].SpinDriftInches > 0 && spun.Points[^1].CoriolisInches > 0);
        var left = BallisticSolver.Solve(Case with { TwistInches = 10, BulletDiameterInches = 0.308, BulletLengthInches = 1.24, TwistDirection = -1, LatitudeDegrees = -45 }, 1000, 500);
        Assert.True(left.Points[^1].SpinDriftInches < 0 && left.Points[^1].CoriolisInches < 0);
        Assert.Equal(spun.Points[^1].DropInches, plain.Points[^1].DropInches, 12);
    }

    /// <summary>
    /// Entry 110 section 2g: <c>grouplab trajectory</c> prints a table from stated inputs, says every input it assumed above the table and
    /// what is not modelled below it, and refuses a missing input with the usage.
    /// </summary>
    [Fact]
    public void TheTrajectoryCommandPrintsItsInputsTheTableAndWhatIsNotModelled()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.Equal(0, GroupLab.Cli.TrajectoryVerb.Run(["--bc", "0.243", "--model", "G7", "--mv", "2700", "--weight", "175", "--pressure", "29.92", "--wind", "10"], output, error));
        string text = output.ToString();
        Assert.Contains("G7 BC 0.243 (ICAO), 2700 fps, 175 gr, sight 1.50 in, zeroed at 100 yd", text, StringComparison.Ordinal);
        Assert.Contains("59 F, 29.92 inHg, 50% humidity, crosswind 10 mph from the left, angle 0 degrees", text, StringComparison.Ordinal);
        Assert.Contains("   1000 ", text, StringComparison.Ordinal);
        Assert.Contains("Aerodynamic jump is not modelled.", text, StringComparison.Ordinal);
        Assert.Contains("The Coriolis vertical (Eötvös) term is not modelled.", text, StringComparison.Ordinal);

        Assert.Equal(2, GroupLab.Cli.TrajectoryVerb.Run(["--bc", "0.243"], new StringWriter(), error));
        Assert.Contains("--bc, --model, --mv and --weight are required", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ACrosswindFromTheRightDriftsTheBulletLeft()
    {
        var right = BallisticSolver.Solve(Case with { CrosswindMph = -10 }, 1000, 500).Points[^1];
        var left = Flat(1000, 500)[^1];
        Assert.Equal(-left.WindInches, right.WindInches, 12);
        Assert.True(left.WindInches > 0);
    }
}
