using GroupLab.Core.Marking;

namespace GroupLab.Core.Ballistics;

/// <summary>The air a trajectory is flown through, as the person states it; pressure left out is taken from the altitude.</summary>
public sealed record AirInput(double TemperatureF = 59, double? PressureInHg = null, double AltitudeFt = 0, double HumidityPct = 50);

/// <summary>One axis of a zero correction carried to another distance: the offset and its half-width there, or why there is nothing to carry.</summary>
public sealed record CarriedAxis(double OffsetInches, double HalfWidthInches, bool Distinguishable, string Dial, Clicks? Clicks);

/// <summary>A zero correction carried from the distance shot to another, with the transfer factor of each axis.</summary>
public sealed record CarriedZero(double FromYards, double ToYards, CarriedAxis Windage, CarriedAxis Elevation, double WindageTransfer, double ElevationTransfer);

/// <summary>
/// The solver's first two uses on screen, NOTES-FROM-PLANNING.md entry 112 section 4: what a rifle and load record must carry before it can be
/// used, the zero correction carried to a second distance with its uncertainty, and a dope table.
/// </summary>
public static class SolverUse
{
    /// <summary>The crosswind a dope table's wind column is for.</summary>
    public const double DopeWindMph = 10;

    /// <summary>
    /// What the records lack for the solver, in words, empty when they have everything. Twist, bullet length and diameter are optional: without
    /// them there is no spin drift, which the table says.
    /// </summary>
    public static IReadOnlyList<string> Missing(Rifle? rifle, Load? load)
    {
        var missing = new List<string>();
        if (rifle is null)
        {
            missing.Add("a rifle");
        }
        else
        {
            if (rifle.SightHeightInches is not > 0)
            {
                missing.Add("the rifle's sight height");
            }

            if (rifle.ZeroDistanceYards is not > 0)
            {
                missing.Add("the rifle's zero distance");
            }
        }

        if (load is null)
        {
            missing.Add("a load");
        }
        else
        {
            if (load.MuzzleVelocityFps is not > 0)
            {
                missing.Add("the load's muzzle velocity");
            }

            if (load.BallisticCoefficient is not > 0)
            {
                missing.Add("the load's BC");
            }

            if (load.DragModel is null)
            {
                missing.Add("the BC's drag model, G1 or G7");
            }

            if (load.BcReference is null)
            {
                missing.Add("the BC's reference atmosphere");
            }

            if (load.BulletWeightGrains is not > 0)
            {
                missing.Add("the bullet's weight");
            }
        }

        return missing;
    }

    /// <summary>The solver's input from the records and the air, or null when <see cref="Missing"/> names anything.</summary>
    public static BallisticInput? Input(Rifle? rifle, Load? load, AirInput air)
    {
        ArgumentNullException.ThrowIfNull(air);
        if (Missing(rifle, load).Count > 0)
        {
            return null;
        }

        return new BallisticInput(
            load!.BallisticCoefficient!.Value,
            load.DragModel!.Value,
            load.MuzzleVelocityFps!.Value,
            load.BulletWeightGrains!.Value,
            rifle!.SightHeightInches!.Value,
            rifle.ZeroDistanceYards!.Value,
            air.TemperatureF,
            air.PressureInHg,
            air.AltitudeFt,
            air.HumidityPct,
            Reference: load.BcReference!.Value,
            TwistInches: rifle.TwistInches,
            TwistDirection: rifle.TwistDirection ?? 1,
            BulletDiameterInches: load.BulletDiameterInches,
            BulletLengthInches: load.BulletLengthInches);
    }

    /// <summary>The height of the bullet above the line of sight at one range, from a solve with a row there.</summary>
    private static double PathAt(BallisticInput input, double yards) => BallisticSolver.Solve(input, yards, yards).Points[^1].DropInches;

    /// <summary>
    /// How a vertical offset at <paramref name="fromYards"/> that comes from the sight's angle appears at <paramref name="toYards"/>: the ratio of
    /// the two heights' changes when the bore's angle changes, by a central finite difference of the zero range. For a flat line it would be
    /// the ratio of the distances; the solver's curved path makes it slightly different, which is why moving a zero needs the solver.
    /// </summary>
    public static double VerticalTransfer(BallisticInput input, double fromYards, double toYards)
    {
        ArgumentNullException.ThrowIfNull(input);
        double h = Math.Max(0.25, input.ZeroRangeYards / 400);
        var up = input with { ZeroRangeYards = input.ZeroRangeYards + h };
        var down = input with { ZeroRangeYards = input.ZeroRangeYards - h };
        return (PathAt(up, toYards) - PathAt(down, toYards)) / (PathAt(up, fromYards) - PathAt(down, fromYards));
    }

    /// <summary>
    /// A zero correction carried from the distance shot to another. Windage from the sight's angle grows exactly with range, since drag acts
    /// along the velocity and leaves its sideways share unchanged, so its transfer is the ratio of the distances; elevation's is
    /// <see cref="VerticalTransfer"/>. Each offset and its half-width are carried by the same factor, so the uncertainty comes through, and an
    /// axis that could not be told from zero at the distance shot stays one that cannot be told from zero: nothing is carried for it.
    /// </summary>
    public static CarriedZero Carry(BallisticInput input, ZeroCorrection zero, double fromYards, double toYards, Rifle? rifle)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(zero);
        double windage = toYards / fromYards, elevation = VerticalTransfer(input, fromYards, toYards);
        CarriedAxis Axis(ZeroAxis axis, double transfer)
        {
            double offset = axis.OffsetInches * transfer, half = Math.Abs(axis.HalfWidthInches * transfer);
            var clicks = axis.Distinguishable && rifle is { ClickValue: > 0 } ? Clicks.For(offset, toYards * 36, rifle, axis.Dial) : null;
            return new CarriedAxis(offset, half, axis.Distinguishable, axis.Dial, clicks);
        }

        return new CarriedZero(fromYards, toYards, Axis(zero.Windage, windage), Axis(zero.Elevation, elevation), windage, elevation);
    }

    /// <summary>
    /// The correction for the rifle's own zero distance, from a group shot at another, NOTES-FROM-PLANNING.md entry 170 section 1.2.
    /// <para>
    /// <b>Not the same as <see cref="Carry"/>.</b> A rifle zeroed at 100 yards and shot at 25 is not meant to hit the aim at 25: its bullet is
    /// still climbing there, below the line of sight by the trajectory's own height. So only the part of the offset that is not that height is
    /// the sight's error, and it is that part that is carried to the zero distance, elevation along the solver's path and windage in
    /// proportion to range. <paramref name="zeroed"/> is the solver's input with its zero range at the rifle's zero distance.
    /// </para>
    /// </summary>
    public static (CarriedZero Carried, double ExpectedLowInches) ToZeroDistance(BallisticInput zeroed, ZeroCorrection zero, double shotYards, Rifle? rifle)
    {
        ArgumentNullException.ThrowIfNull(zero);
        ArgumentNullException.ThrowIfNull(zeroed);

        // The zero's elevation is positive low, and the solver's path is positive up: a correctly zeroed rifle hits this far low of the aim here.
        double expectedLow = -PathAt(zeroed, shotYards);
        double error = zero.Elevation.OffsetInches - expectedLow;
        var elevation = zero.Elevation with
        {
            OffsetInches = error,
            Distinguishable = Math.Abs(error) > zero.Elevation.HalfWidthInches,
            Dial = error >= 0 ? "up" : "down",
            Sits = error >= 0 ? "low" : "high",
            Clicks = null,
        };
        return (Carry(zeroed, zero with { Elevation = elevation }, shotYards, zeroed.ZeroRangeYards, rifle), expectedLow);
    }

    /// <summary>A dope table: every row to <paramref name="maxYards"/>, with a full-value crosswind of <see cref="DopeWindMph"/> for its wind column.</summary>
    public static Trajectory Dope(BallisticInput input, double maxYards, double stepYards)
    {
        ArgumentNullException.ThrowIfNull(input);
        return BallisticSolver.Solve(input with { CrosswindMph = DopeWindMph }, maxYards, stepYards);
    }
}
