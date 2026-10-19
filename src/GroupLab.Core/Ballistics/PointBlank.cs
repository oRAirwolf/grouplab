using System.Globalization;

namespace GroupLab.Core.Ballistics;

/// <summary>
/// What a point blank range is worked out on, NOTES-FROM-PLANNING.md entry 455: a circle (its diameter in <see cref="WidthInches"/>) or a
/// rectangle, centered on the aim, in inches.
/// </summary>
public sealed record PointBlankZone(HitShape Shape, double WidthInches, double HeightInches)
{
    public static PointBlankZone Circle(double diameterInches) => new(HitShape.Circle, diameterInches, diameterInches);

    public static PointBlankZone Rectangle(double widthInches, double heightInches) => new(HitShape.Rectangle, widthInches, heightInches);

    /// <summary>Half the zone's height: how far the path may rise above the aim, or fall below it, and stay inside.</summary>
    public double HalfHeight => HeightInches / 2;
}

/// <summary>Which list a target is in.</summary>
public enum PointBlankKind
{
    Game,
    Estimate,
    Practical,
}

/// <summary>
/// A target to choose from, entry 455's table: its zone, where the size comes from in plain words (a range where sources disagree, the
/// smaller figure used), whether it is GroupLab's estimate with no published size behind it, and the energy floor the rule of thumb gives.
/// </summary>
public sealed record PointBlankTarget(string Name, PointBlankKind Kind, PointBlankZone Zone, string Source, double? EnergyFloorFtLb = null)
{
    public bool IsEstimate => Kind == PointBlankKind.Estimate;
}

/// <summary>
/// The targets entry 455 lists, with the sizes the planning session's research found. The heart and lung zone uses the smaller published
/// figure where sources disagree. The estimates are GroupLab's own, conservative, and say so; the energy floors are rules of thumb, editable,
/// and blank where none is usual.
/// </summary>
public static class PointBlankTargets
{
    /// <summary>The usual rules of thumb for the least energy at the animal: 1000 ft lb for deer-sized game and 1500 for elk-sized.</summary>
    public const double DeerSized = 1000, ElkSized = 1500;

    private const double Cm = 1 / 2.54;

    public static IReadOnlyList<PointBlankTarget> All { get; } =
    [
        new("Prairie dog", PointBlankKind.Game, PointBlankZone.Circle(2), "2 in, Wikipedia's point-blank range article"),
        new("Coyote", PointBlankKind.Game, PointBlankZone.Circle(4), "4 in, Ron Spomer"),
        new("Pronghorn", PointBlankKind.Game, PointBlankZone.Circle(8.5), "8.5 in, Chuck Hawks (8.5 to 9 in)", DeerSized),
        new("Whitetail deer", PointBlankKind.Game, PointBlankZone.Circle(8), "8 in, American Hunter (6 to 12 in published)", DeerSized),
        new("Mule deer", PointBlankKind.Game, PointBlankZone.Circle(8), "8 in, Ron Spomer (10 in at most)", DeerSized),
        new("Black bear", PointBlankKind.Game, PointBlankZone.Circle(8), "8 in, HuntWise (8 to 10 in)", DeerSized),
        new("Bighorn sheep", PointBlankKind.Game, PointBlankZone.Circle(12), "12 in, Chuck Hawks (12 to 13 in)", DeerSized),
        new("Mountain goat", PointBlankKind.Game, PointBlankZone.Circle(13), "13 in, Chuck Hawks (13 to 14.5 in)", DeerSized),
        new("Caribou", PointBlankKind.Game, PointBlankZone.Circle(14.5), "14.5 in, Chuck Hawks (14.5 to 15.5 in)", ElkSized),
        new("Elk", PointBlankKind.Game, PointBlankZone.Circle(14.5), "14.5 in, Chuck Hawks (14.5 to 15.5 in; American Hunter 15 in)", ElkSized),
        new("Moose", PointBlankKind.Game, PointBlankZone.Circle(18), "18 in, Chuck Hawks (18 to 21.5 in; American Hunter 24 in)", ElkSized),

        // Entry 455: no published size was found for these. Each is GroupLab's conservative figure from the animal's size against the sourced
        // ones above, and the row says how it was chosen; it is never presented as sourced.
        new("Wild hog", PointBlankKind.Estimate, PointBlankZone.Circle(6),
            "Estimate, no published size found: 6 in, under a whitetail's 8 in, because a hog's heart and lungs sit low and forward", DeerSized),
        new("European wild boar", PointBlankKind.Estimate, PointBlankZone.Circle(7),
            "Estimate, no published size found: 7 in, a little under a whitetail's 8 in for a heavier body built the same way", DeerSized),
        new("Impala", PointBlankKind.Estimate, PointBlankZone.Circle(7),
            "Estimate, no published size found: 7 in, under a pronghorn's 8.5 in for an animal of about its size", DeerSized),
        new("Red stag", PointBlankKind.Estimate, PointBlankZone.Circle(10),
            "Estimate, no published size found: 10 in, between a mule deer's 8 in and an elk's 14.5 in", ElkSized),
        new("Kudu", PointBlankKind.Estimate, PointBlankZone.Circle(12),
            "Estimate, no published size found: 12 in, under an elk's 14.5 in for a lighter animal of about its height", ElkSized),

        new("IPSC target, A zone", PointBlankKind.Practical, PointBlankZone.Rectangle(15 * Cm, 32.5 * Cm), "15 by 32.5 cm, IPSC rules January 2024, appendix B2"),
        new("USPSA target, head A zone", PointBlankKind.Practical, PointBlankZone.Rectangle(10 * Cm, 5 * Cm), "10 by 5 cm, USPSA rules, appendix B1"),
        new("IDPA target, body -0", PointBlankKind.Practical, PointBlankZone.Circle(8), "8 in circle, IDPA"),
        new("Steel silhouette, full size", PointBlankKind.Practical, PointBlankZone.Rectangle(18, 30), "18 by 30 in, a vendor's shape, not a rule"),
        new("Steel silhouette, two thirds", PointBlankKind.Practical, PointBlankZone.Rectangle(12, 20), "12 by 20 in, a vendor's shape, not a rule"),
    ];

    /// <summary>The line shown wherever a hunting target is chosen: a reminder, never a legal verdict.</summary>
    public const string CheckTheLaw = "Check your local law on the least energy, caliber and distance for this animal; GroupLab does not know it.";
}

/// <summary>How the shooter's zero is chosen in the point blank view.</summary>
public enum PointBlankMode
{
    /// <summary>The longest stretch of distances held dead on at the chosen certainty.</summary>
    Longest,

    /// <summary>The group centered on the zone at the distance the shot is most likely to be.</summary>
    MostLikely,

    /// <summary>The zero with the best worst-case chance across a window of distances.</summary>
    Window,
}

/// <summary>What ends the stretch of distances held dead on, whichever comes first.</summary>
public enum PointBlankEnd
{
    Certainty,
    Energy,
    Expansion,
    SolverRange,
}

/// <summary>The classic point blank range, for reference: the path kept within half the zone's height above and below the aim.</summary>
/// <param name="OffsetAt100Inches">How high the group sits at 100 yd above the aim with this zero.</param>
public sealed record ClassicPointBlank(double StartYards, double NearZeroYards, double PeakInches, double PeakYards, double FarZeroYards, double EndYards,
    double OffsetAt100Inches, double LaunchDeltaMoa);

/// <summary>A stretch of distances where at least the chosen share of shots lands in the zone, and the zero that gives it.</summary>
/// <param name="LaunchDeltaMoa">The zero as a change of the bore's angle from the rifle's current zero, in MOA, positive up.</param>
public sealed record PointBlankBand(double StartYards, double EndYards, double LaunchDeltaMoa, bool Exists)
{
    public double Length => Exists ? EndYards - StartYards : 0;
}

/// <summary>The window case: the zero with the best worst-case chance across the window, and where that worst point is.</summary>
public sealed record PointBlankWindow(double FromYards, double ToYards, double LaunchDeltaMoa, double WorstChance, double WorstYards, bool Reaches);

/// <summary>Where a band ends once the energy floor and the bullet's least expansion velocity are taken into account, each named.</summary>
public sealed record PointBlankEnding(double EndYards, PointBlankEnd Why, double? EnergyEndYards, double? ExpansionEndYards);

/// <summary>
/// Maximum point blank range, NOTES-FROM-PLANNING.md entry 455. The trajectory is flown once from the rifle's current zero; a change of zero
/// is a change of the bore's angle, which moves the path by that angle times the range, so every zero the view tries is the same flight
/// tilted. The chance a shot lands in the zone at a distance comes from the shooter's mean radius (a per-axis sigma, MR = sigma times the
/// square root of pi over 2, growing with range) and the load's velocity SD carried through the solver as a vertical spread, aimed at the
/// zone's center: a circle is the normal integrated over it, a rectangle the product of two intervals. Wind is left out, as point blank
/// range always leaves it out, and is shown beside it.
/// </summary>
public sealed class PointBlankModel
{
    /// <summary>Inches per MOA at one yard.</summary>
    public const double InchesPerMoaPerYard = 0.0104719755;

    /// <summary>The farthest distance looked at, and the step between the distances looked at.</summary>
    public const int MaxYards = 1000;

    /// <summary>The most certainty the view takes: 100 % cannot be promised for any shot.</summary>
    public const double MostCertainty = 0.999;

    public const string NoPromise = "No shot can be promised to land: the most GroupLab can work to is 99.9 %.";

    private readonly double[] path, velocity, energy, wind, spread;
    private readonly double sigmaMoa;

    /// <param name="input">The solver's input with the rifle's current zero, the air and the shot angle.</param>
    /// <param name="meanRadiusMoa">The shooter's mean radius, as an angle, from the position the shot will be taken from.</param>
    /// <param name="velocitySdFps">The load's muzzle velocity SD, or 0 where it is not known.</param>
    /// <param name="crosswindMph">The full-value crosswind whose drift is shown beside the band.</param>
    public PointBlankModel(BallisticInput input, double meanRadiusMoa, double velocitySdFps = 0, double crosswindMph = 10)
    {
        ArgumentNullException.ThrowIfNull(input);
        Input = input;
        MeanRadiusMoa = meanRadiusMoa;
        sigmaMoa = meanRadiusMoa / Math.Sqrt(Math.PI / 2);
        var flown = BallisticSolver.Solve(input with { CrosswindMph = crosswindMph }, MaxYards, 1);
        LaunchAngleMoa = flown.ZeroAngleMoa;
        int n = MaxYards + 1;
        path = new double[n];
        velocity = new double[n];
        energy = new double[n];
        wind = new double[n];
        spread = new double[n];
        foreach (var p in flown.Points)
        {
            int i = (int)Math.Round(p.RangeYards);
            if (i >= 0 && i < n)
            {
                (path[i], velocity[i], energy[i], wind[i]) = (p.DropInches, p.VelocityFps, p.EnergyFtLb, p.WindInches);
            }
        }

        // At the muzzle the bullet is the sight height below the line of sight, which a solver's first row may not carry.
        path[0] = -input.SightHeightInches;
        velocity[0] = input.MuzzleVelocityFps;
        energy[0] = flown.Points.FirstOrDefault()?.EnergyFtLb ?? 0;

        // The velocity SD as a vertical spread: the path flown a standard deviation fast and slow with the bore held where it is.
        if (velocitySdFps > 0)
        {
            var fast = BallisticSolver.Solve(input with { MuzzleVelocityFps = input.MuzzleVelocityFps + velocitySdFps, LaunchAngleMoa = LaunchAngleMoa }, MaxYards, 1);
            var slow = BallisticSolver.Solve(input with { MuzzleVelocityFps = input.MuzzleVelocityFps - velocitySdFps, LaunchAngleMoa = LaunchAngleMoa }, MaxYards, 1);
            for (int i = 0; i < Math.Min(fast.Points.Count, slow.Points.Count); i++)
            {
                int at = (int)Math.Round(fast.Points[i].RangeYards);
                if (at >= 0 && at < n)
                {
                    spread[at] = Math.Abs(fast.Points[i].DropInches - slow.Points[i].DropInches) / 2;
                }
            }
        }

        Crosswind = crosswindMph;
    }

    public BallisticInput Input { get; }

    public double MeanRadiusMoa { get; }

    public double Crosswind { get; }

    /// <summary>The bore's angle above the line of sight at the rifle's current zero, in MOA.</summary>
    public double LaunchAngleMoa { get; }

    /// <summary>The height of the path above the line of sight at a distance, with the zero moved by <paramref name="deltaMoa"/>.</summary>
    public double Height(double yards, double deltaMoa = 0) => Interpolate(path, yards) + (deltaMoa * InchesPerMoaPerYard * yards);

    public double VelocityAt(double yards) => Interpolate(velocity, yards);

    public double EnergyAt(double yards) => Interpolate(energy, yards);

    /// <summary>The crosswind's drift at a distance, inches, positive to the right.</summary>
    public double WindAt(double yards) => Interpolate(wind, yards);

    /// <summary>The zero change that puts the group's center on the aim at a distance.</summary>
    public double DeltaForZeroAt(double yards) => -Interpolate(path, yards) / (InchesPerMoaPerYard * yards);

    /// <summary>The chance a shot at this distance lands in the zone, with the zero moved by <paramref name="deltaMoa"/>.</summary>
    public double Chance(PointBlankZone zone, double yards, double deltaMoa)
    {
        ArgumentNullException.ThrowIfNull(zone);
        double mu = Height(yards, deltaMoa);
        double across = Math.Max(1e-9, sigmaMoa * InchesPerMoaPerYard * yards);
        double s = Interpolate(spread, yards);
        double up = Math.Max(1e-9, Math.Sqrt((across * across) + (s * s)));
        if (zone.Shape == HitShape.Rectangle)
        {
            return (Phi((zone.HalfHeight - mu) / up) - Phi((-zone.HalfHeight - mu) / up)) * ((2 * Phi(zone.WidthInches / 2 / across)) - 1);
        }

        // The circle: the vertical normal integrated over the part of the circle's height it has any weight in (eight sigmas either side of
        // the group's center), each slice's horizontal chance inside its chord, by 48-point Gauss-Legendre. Integrating over only that part
        // keeps a spread of a hundredth of an inch near the muzzle from falling between the points.
        double r = zone.WidthInches / 2;
        double a = Math.Max(-r, mu - (8 * up)), b = Math.Min(r, mu + (8 * up));
        if (a >= b)
        {
            return 0;
        }

        double half = (b - a) / 2, middle = (a + b) / 2, sum = 0;
        for (int k = 0; k < Gauss.Length; k++)
        {
            double y = middle + (half * Gauss[k].X), chord = Math.Sqrt(Math.Max(0, (r * r) - (y * y)));
            double z = (y - mu) / up;
            sum += Gauss[k].W * Math.Exp(-0.5 * z * z) / (up * Math.Sqrt(2 * Math.PI)) * ((2 * Phi(chord / across)) - 1);
        }

        return Math.Clamp(sum * half, 0, 1);
    }

    /// <summary>
    /// The first stretch of distances, from the muzzle outward, where every distance has at least <paramref name="certainty"/> of its shots
    /// in the zone with this zero. A zone smaller than the sight height starts some way out, where the path has climbed into it.
    /// </summary>
    public PointBlankBand Band(PointBlankZone zone, double certainty, double deltaMoa)
    {
        int start = -1;
        for (int y = 0; y <= MaxYards; y++)
        {
            bool inside = y == 0 ? Math.Abs(Height(0, deltaMoa)) <= zone.HalfHeight : Chance(zone, y, deltaMoa) >= certainty;
            if (inside && start < 0)
            {
                start = y;
            }
            else if (!inside && start >= 0)
            {
                return new PointBlankBand(start, y - 1, deltaMoa, true);
            }
        }

        return start < 0 ? new PointBlankBand(0, 0, deltaMoa, false) : new PointBlankBand(start, MaxYards, deltaMoa, true);
    }

    /// <summary>
    /// The classic stretch for a given zero: the distances, from the muzzle outward, where the path stays within half the zone's height of
    /// the aim, with no allowance for spread. The rifle's own zero is read this way beside the certainty band.
    /// </summary>
    public PointBlankBand ClassicBand(PointBlankZone zone, double deltaMoa)
    {
        ArgumentNullException.ThrowIfNull(zone);
        int start = -1;
        for (int y = 0; y <= MaxYards; y++)
        {
            bool inside = Math.Abs(Height(y, deltaMoa)) <= zone.HalfHeight;
            if (inside && start < 0)
            {
                start = y;
            }
            else if (!inside && start >= 0)
            {
                return new PointBlankBand(start, y - 1, deltaMoa, true);
            }
        }

        return start < 0 ? new PointBlankBand(0, 0, deltaMoa, false) : new PointBlankBand(start, MaxYards, deltaMoa, true);
    }

    /// <summary>The zero that reaches farthest held dead on, at the chosen certainty: the first mode.</summary>
    public PointBlankBand Longest(PointBlankZone zone, double certainty)
    {
        // Searched over the height at 100 yd, from the zone's bottom to well above its top, coarse first and then around the best.
        PointBlankBand best = Band(zone, certainty, 0);
        double Delta(double at100) => (at100 - Height(100)) / (InchesPerMoaPerYard * 100);
        double step = Math.Max(0.02, zone.HeightInches / 100);
        for (double h = -zone.HalfHeight; h <= zone.HeightInches * 1.5; h += step)
        {
            var band = Band(zone, certainty, Delta(h));
            if (band.Exists && (!best.Exists || band.EndYards > best.EndYards || (band.EndYards == best.EndYards && band.StartYards < best.StartYards)))
            {
                best = band;
            }
        }

        double around = best.LaunchDeltaMoa;
        double fine = step / 20 / (InchesPerMoaPerYard * 100);
        for (int i = -20; i <= 20; i++)
        {
            var band = Band(zone, certainty, around + (i * fine));
            if (band.Exists && (band.EndYards > best.EndYards || (band.EndYards == best.EndYards && band.StartYards < best.StartYards)))
            {
                best = band;
            }
        }

        return best;
    }

    /// <summary>The second mode: the group centered on the zone at the most likely distance, and the band that zero gives.</summary>
    public PointBlankBand MostLikely(PointBlankZone zone, double certainty, double yards) => Band(zone, certainty, DeltaForZeroAt(yards));

    /// <summary>
    /// The third mode: the zero whose least chance across the window is the highest, found over the zeros that center the group somewhere
    /// from the window's near end to beyond its far end, then refined. Whether that least chance reaches the certainty is said, not hidden.
    /// </summary>
    public PointBlankWindow Window(PointBlankZone zone, double certainty, double fromYards, double toYards)
    {
        (double Worst, double At) WorstOf(double delta)
        {
            double worst = 1, at = fromYards;
            for (double y = fromYards; y <= toYards + 1e-9; y += Math.Max(1, (toYards - fromYards) / 60))
            {
                double c = Chance(zone, y, delta);
                if (c < worst)
                {
                    (worst, at) = (c, y);
                }
            }

            return (worst, at);
        }

        double lo = DeltaForZeroAt(Math.Max(1, fromYards / 2)), hi = DeltaForZeroAt(Math.Min(MaxYards, toYards * 1.5));
        if (lo > hi)
        {
            (lo, hi) = (hi, lo);
        }

        double bestDelta = lo, bestWorst = -1, bestAt = fromYards;
        for (int pass = 0; pass < 3; pass++)
        {
            double step = (hi - lo) / 40;
            for (int i = 0; i <= 40; i++)
            {
                double d = lo + (i * step);
                var (worst, at) = WorstOf(d);
                if (worst > bestWorst)
                {
                    (bestDelta, bestWorst, bestAt) = (d, worst, at);
                }
            }

            (lo, hi) = (bestDelta - (2 * step), bestDelta + (2 * step));
        }

        return new PointBlankWindow(fromYards, toYards, bestDelta, bestWorst, bestAt, bestWorst >= certainty);
    }

    /// <summary>
    /// The classic figure, for reference: the zero that puts the path's peak at the top of the zone, its near and far zeros, the start (not the
    /// muzzle where the sight sits more than half the zone below the aim) and the end, where the path leaves the bottom of the zone.
    /// </summary>
    public ClassicPointBlank Classic(PointBlankZone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        double h = zone.HalfHeight;
        double Peak(double delta)
        {
            double top = double.MinValue;
            for (int y = 1; y <= MaxYards; y++)
            {
                top = Math.Max(top, Height(y, delta));
            }

            return top;
        }

        double lo = -20, hi = 60;
        for (int i = 0; i < 60; i++)
        {
            double mid = (lo + hi) / 2;
            if (Peak(mid) > h)
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        double delta = lo;
        int peakAt = 1;
        for (int y = 1; y <= MaxYards; y++)
        {
            if (Height(y, delta) > Height(peakAt, delta))
            {
                peakAt = y;
            }
        }

        double start = 0;
        for (int y = 0; y <= peakAt; y++)
        {
            if (Height(y, delta) >= -h)
            {
                start = y == 0 ? 0 : Crossing(y - 1, y, -h, delta);
                break;
            }
        }

        double near = 0, far = MaxYards, end = MaxYards;
        for (int y = 1; y <= MaxYards; y++)
        {
            if (Height(y - 1, delta) < 0 && Height(y, delta) >= 0 && y <= peakAt)
            {
                near = Crossing(y - 1, y, 0, delta);
            }

            if (y > peakAt && Height(y - 1, delta) >= 0 && Height(y, delta) < 0)
            {
                far = Crossing(y - 1, y, 0, delta);
            }

            if (y > peakAt && Height(y - 1, delta) >= -h && Height(y, delta) < -h)
            {
                end = Crossing(y - 1, y, -h, delta);
                break;
            }
        }

        return new ClassicPointBlank(start, near, Height(peakAt, delta), peakAt, far, end, Height(100, delta), delta);
    }

    /// <summary>
    /// Where the band really ends: the certainty band, the energy floor and the bullet's least expansion velocity, whichever comes first, each
    /// named. A floor or a velocity left blank ends nothing.
    /// </summary>
    public PointBlankEnding Ending(PointBlankBand band, double? energyFloorFtLb, double? expansionFps)
    {
        ArgumentNullException.ThrowIfNull(band);
        double? energyEnd = null, expansionEnd = null;
        for (int y = 1; y <= MaxYards; y++)
        {
            if (energyEnd is null && energyFloorFtLb is > 0 && energy[y] < energyFloorFtLb)
            {
                energyEnd = y - 1;
            }

            if (expansionEnd is null && expansionFps is > 0 && velocity[y] < expansionFps)
            {
                expansionEnd = y - 1;
            }
        }

        var (end, why) = band.EndYards >= MaxYards ? (band.EndYards, PointBlankEnd.SolverRange) : (band.EndYards, PointBlankEnd.Certainty);
        if (energyEnd is { } e && e < end)
        {
            (end, why) = (e, PointBlankEnd.Energy);
        }

        if (expansionEnd is { } x && x < end)
        {
            (end, why) = (x, PointBlankEnd.Expansion);
        }

        return new PointBlankEnding(Math.Max(band.StartYards, end), why, energyEnd, expansionEnd);
    }

    /// <summary>The height at the rifle's zero distance that a zero change makes, inches, positive high.</summary>
    public double OffsetAtZeroDistance(double deltaMoa) => deltaMoa * InchesPerMoaPerYard * Input.ZeroRangeYards;

    /// <summary>The far distance where the path with this zero crosses the line of sight again, the zero distance a rifle record holds.</summary>
    public double? FarZero(double deltaMoa)
    {
        int peak = 1;
        for (int y = 1; y <= MaxYards; y++)
        {
            if (Height(y, deltaMoa) > Height(peak, deltaMoa))
            {
                peak = y;
            }
        }

        for (int y = peak + 1; y <= MaxYards; y++)
        {
            if (Height(y - 1, deltaMoa) >= 0 && Height(y, deltaMoa) < 0)
            {
                return Crossing(y - 1, y, 0, deltaMoa);
            }
        }

        return null;
    }

    /// <summary>A certainty as typed, as a share, or the reason it cannot be used.</summary>
    public static (double? Share, string? Refusal) Certainty(double percent) => percent switch
    {
        >= 100 => (null, NoPromise),
        > 99.9 => (null, NoPromise),
        <= 0 or double.NaN => (null, "Enter a certainty above 0 %."),
        _ => (percent / 100, null),
    };

    /// <summary>A mean radius in MOA from a value in MOA, mil or inches at 100 yd.</summary>
    public static double ToMoa(double value, PointBlankPrecisionUnit unit) => unit switch
    {
        PointBlankPrecisionUnit.Mil => value / BallisticSolver.MilPerMoa,
        PointBlankPrecisionUnit.InchesAt100 => value / (InchesPerMoaPerYard * 100),
        _ => value,
    };

    private double Crossing(int a, int b, double level, double delta)
    {
        double ha = Height(a, delta) - level, hb = Height(b, delta) - level;
        return Math.Abs(hb - ha) < 1e-12 ? b : a + ((b - a) * (-ha / (hb - ha)));
    }

    private static double Interpolate(double[] values, double yards)
    {
        if (yards <= 0)
        {
            return values[0];
        }

        if (yards >= values.Length - 1)
        {
            return values[^1];
        }

        int i = (int)yards;
        double f = yards - i;
        return values[i] + ((values[i + 1] - values[i]) * f);
    }

    /// <summary>The standard normal CDF by the complementary error function's Chebyshev fit (Numerical Recipes' erfcc), good to 1.2e-7, fast enough for the searches above.</summary>
    internal static double Phi(double z)
    {
        double x = Math.Abs(z) / Math.Sqrt(2), t = 1 / (1 + (0.5 * x));
        double poly = 0;
        foreach (double c in ErfcFit)
        {
            poly = (poly * t) + c;
        }

        double erfc = t * Math.Exp((-x * x) + poly);
        return z >= 0 ? 1 - (erfc / 2) : erfc / 2;
    }

    /// <summary>erfcc's coefficients, highest power first.</summary>
    private static readonly double[] ErfcFit = [0.17087277, -0.82215223, 1.48851587, -1.13520398, 0.27886807, -0.18628806, 0.09678418, 0.37409196, 1.00002368, -1.26551223];

    private static readonly (double X, double W)[] Gauss = GaussLegendre(48);

    /// <summary>Gauss-Legendre nodes and weights on [-1, 1] by Newton's method on the Legendre polynomial.</summary>
    private static (double X, double W)[] GaussLegendre(int n)
    {
        var nodes = new (double, double)[n];
        for (int i = 0; i < n; i++)
        {
            double x = Math.Cos(Math.PI * (i + 0.75) / (n + 0.5)), dp = 0;
            for (int iteration = 0; iteration < 100; iteration++)
            {
                double p0 = 1, p1 = x;
                for (int k = 2; k <= n; k++)
                {
                    (p0, p1) = (p1, (((2 * k) - 1) * x * p1 - ((k - 1) * p0)) / k);
                }

                dp = n * ((x * p1) - p0) / ((x * x) - 1);
                double dx = p1 / dp;
                x -= dx;
                if (Math.Abs(dx) < 1e-15)
                {
                    break;
                }
            }

            nodes[i] = (x, 2 / ((1 - (x * x)) * dp * dp));
        }

        return nodes;
    }

}

/// <summary>The units a mean radius is typed in: MOA, mil, or inches at 100 yd.</summary>
public enum PointBlankPrecisionUnit
{
    Moa,
    Mil,
    InchesAt100,
}
