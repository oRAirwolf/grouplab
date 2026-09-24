using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 112 section 4: the solver on screen. A record set missing anything the solver needs says what; the zero
/// correction carries to a second distance with its uncertainty, and an offset that cannot be told from zero stays one; a dope table carries
/// the wind column and the sentence about aerodynamic jump.
/// </summary>
public class SolverUseTests
{
    private static readonly Rifle Rifle = new("Tikka T3x", 0.25, AngularUnit.Moa) { SightHeightInches = 1.75, ZeroDistanceYards = 100 };

    private static readonly Load Load = new("H4350 41.5", null)
    {
        MuzzleVelocityFps = 2710, BallisticCoefficient = 0.326, DragModel = DragModel.G7, BcReference = ReferenceAtmosphere.Icao, BulletWeightGrains = 140,
    };

    [Fact]
    public void WhatTheRecordsLackIsNamed()
    {
        Assert.Equal(["a rifle", "a load"], SolverUse.Missing(null, null));
        Assert.Equal(["the rifle's sight height", "the rifle's zero distance", "the load's muzzle velocity", "the load's BC", "the BC's drag model, G1 or G7", "the BC's reference atmosphere", "the bullet's weight"],
            SolverUse.Missing(new Rifle("Bare", 0.25, AngularUnit.Moa), new Load("Bare", null)));
        Assert.Empty(SolverUse.Missing(Rifle, Load));
        Assert.Null(SolverUse.Input(Rifle, Load with { BallisticCoefficient = null }, new AirInput()));
        var input = SolverUse.Input(Rifle, Load, new AirInput(TemperatureF: 40, AltitudeFt: 5000))!;
        Assert.Equal((0.326, DragModel.G7, 2710.0, 140.0, 1.75, 100.0, 40.0, 5000.0), (input.BallisticCoefficient, input.Model, input.MuzzleVelocityFps, input.BulletWeightGrains, input.SightHeightInches, input.ZeroRangeYards, input.TemperatureF, input.AltitudeFt));
    }

    /// <summary>
    /// The vertical transfer is the linearisation of the solver's own path: an offset from a small change of the bore's angle, made directly
    /// by moving the zero range, lands at the second distance as the transfer says, within a percent. It is close to the ratio of the
    /// distances and not equal to it, which is the curvature the solver is there for.
    /// </summary>
    [Fact]
    public void TheVerticalTransferIsTheSolversOwnLinearisation()
    {
        var input = SolverUse.Input(Rifle, Load, new AirInput())!;
        Assert.Equal(1, SolverUse.VerticalTransfer(input, 100, 100), 9);
        foreach (double to in new[] { 50.0, 300, 600 })
        {
            double transfer = SolverUse.VerticalTransfer(input, 100, to);
            double Path(double zero, double yards) => BallisticSolver.Solve(input with { ZeroRangeYards = zero }, yards, yards).Points[^1].DropInches;
            double atFrom = Path(103, 100) - Path(100, 100), atTo = Path(103, to) - Path(100, to);
            Assert.InRange(atTo / atFrom, transfer * 0.99, transfer * 1.01);
            Assert.InRange(transfer, to / 100 * 0.9, to / 100 * 1.1);
        }
    }

    [Fact]
    public void ACorrectionCarriesWithItsUncertaintyAndARefusalStaysARefusal()
    {
        var input = SolverUse.Input(Rifle, Load, new AirInput())!;
        var zero = new ZeroCorrection(25, 0.2, 48, true,
            new ZeroAxis(0.5, 0.1, true, null, "left", "right"),
            new ZeroAxis(-0.05, 0.1, false, 40, "down", "high"),
            0.3, 0.08);
        var carried = SolverUse.Carry(input, zero, 100, 300, Rifle);
        Assert.Equal(3, carried.WindageTransfer, 12);
        Assert.Equal(1.5, carried.Windage.OffsetInches, 9);
        Assert.Equal(0.3, carried.Windage.HalfWidthInches, 9);
        Assert.True(carried.Windage.Distinguishable);
        Assert.Equal(Clicks.For(1.5, 300 * 36, Rifle, "left"), carried.Windage.Clicks);

        Assert.False(carried.Elevation.Distinguishable);
        Assert.Null(carried.Elevation.Clicks);
        Assert.Equal(-0.05 * carried.ElevationTransfer, carried.Elevation.OffsetInches, 12);
        Assert.Equal(0.1 * carried.ElevationTransfer, carried.Elevation.HalfWidthInches, 12);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 170 section 1.2: a group shot at 25 yd with a rifle zeroed at 100 is not meant to be on the aim. A group
    /// sitting exactly where the 100 yd path puts it at 25 yd needs no elevation at 100, where carrying the whole offset would dial the
    /// rifle off its zero; and an error on top of that path is carried by the solver's transfer, with windage in proportion to range.
    /// </summary>
    [Fact]
    public void ACorrectionForTheRiflesOwnZeroAllowsForWhereTheBulletShouldBe()
    {
        var input = SolverUse.Input(Rifle, Load, new AirInput())!;
        double path = BallisticSolver.Solve(input, 25, 25).Points[^1].DropInches;
        Assert.True(path < -0.3, $"a 100 yd zero is {path:0.00} in above the aim at 25 yd, where it should be below");

        ZeroCorrection At(double windage, double lowInches) => new(10, 0.05, 18, true,
            new ZeroAxis(windage, 0.03, Math.Abs(windage) > 0.03, null, windage >= 0 ? "left" : "right", windage >= 0 ? "right" : "left"),
            new ZeroAxis(lowInches, 0.03, true, null, "up", "low"), 0.05, 0.05);

        var (onPath, expectedLow) = SolverUse.ToZeroDistance(input, At(0, -path), 25, Rifle);
        Assert.Equal(-path, expectedLow, 9);
        Assert.False(onPath.Elevation.Distinguishable);
        Assert.Equal(0, onPath.Elevation.OffsetInches, 9);

        var (off, _) = SolverUse.ToZeroDistance(input, At(0.25, -path + 0.2), 25, Rifle);
        Assert.Equal(4, off.WindageTransfer, 9);
        Assert.Equal(1.0, off.Windage.OffsetInches, 9);
        Assert.Equal(0.2 * SolverUse.VerticalTransfer(input, 25, 100), off.Elevation.OffsetInches, 9);
        Assert.Equal("up", off.Elevation.Dial);
        Assert.NotNull(off.Elevation.Clicks);
    }

    [Fact]
    public void ADopeTableCarriesItsWindAndSaysWhatIsNotModelled()
    {
        var input = SolverUse.Input(Rifle, Load, new AirInput())!;
        var table = SolverUse.Dope(input, 600, 100);
        Assert.Equal([0.0, 100, 200, 300, 400, 500, 600], table.Points.Select(p => p.RangeYards));
        Assert.Equal(0, table.Points[1].DropInches, 2);
        Assert.True(table.Points[^1].DropInches < -50);
        Assert.True(Math.Abs(table.Points[^1].WindInches) > Math.Abs(table.Points[3].WindInches));
        Assert.Contains("Aerodynamic jump is not modelled.", table.NotModelled);
    }
}
