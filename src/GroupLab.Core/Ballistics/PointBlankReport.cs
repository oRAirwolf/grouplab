using System.Globalization;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Ballistics;

/// <summary>What the point blank view is asked, NOTES-FROM-PLANNING.md entry 455: the zone and its name, the certainty as a share, how the
/// zero is chosen and what that needs, what ends the distances, and the crosswind shown beside them.</summary>
public sealed record PointBlankRequest(
    PointBlankZone Zone,
    string ZoneName,
    double Certainty,
    PointBlankMode Mode = PointBlankMode.Longest,
    double LikelyYards = 200,
    double WindowFromYards = 150,
    double WindowToYards = 300,
    double? EnergyFloorFtLb = null,
    double? ExpansionFps = null);

/// <summary>A hold past the band, for the most likely distance: the distance and how far high to hold, inches.</summary>
public sealed record PointBlankHold(double Yards, double HoldInches, string Says);

/// <summary>What the point blank view shows, its figures and its sentences, the same on the computer, the phone and the card.</summary>
public sealed record PointBlankResult(
    PointBlankBand Band,
    PointBlankEnding Ending,
    ClassicPointBlank Classic,
    PointBlankBand Current,
    PointBlankBand CurrentClassic,
    PointBlankWindow? Window,
    string Headline,
    string Certainty,
    string HowToSet,
    string Ends,
    string? Expansion,
    string CurrentZero,
    string ClassicLine,
    string Wind,
    string? WindWarning,
    string? WindowSays,
    IReadOnlyList<PointBlankHold> Holds,
    double? FarZeroYards,
    string KeepSays);

/// <summary>
/// The point blank view's figures and words, NOTES-FROM-PLANNING.md entry 455, worked out once in Core so the computer, the phone and the
/// printed card cannot say different things. Distances are in the person's units, angles in the rifle's scope unit, clicks in its clicks.
/// </summary>
public static class PointBlankReport
{
    /// <summary>The line every point blank answer carries: what it does not count.</summary>
    public const string NotCovered = "Not counted: the wind (shown separately), a moving animal, and a misjudged distance.";

    /// <summary>The sentence under the mean radius: where it should come from.</summary>
    public const string FromThePosition =
        "Use a group shot from the position you will shoot from: a bench group is much tighter than one from sticks or a bipod.";

    /// <summary>What a load with no least expansion velocity says in its place.</summary>
    public const string ExpansionNotEntered = "The bullet's least expansion velocity is not entered; the bullet's maker publishes one.";

    /// <summary>A share in words: "9 in 10", "19 in 20", "99 in 100", or a percent where no small fraction says it.</summary>
    public static string Share(double share)
    {
        foreach (int of in new[] { 10, 20, 100, 1000 })
        {
            double k = share * of;
            if (Math.Abs(k - Math.Round(k)) < 1e-9 && (of < 1000 || Math.Round(k) == 999))
            {
                return string.Create(CultureInfo.InvariantCulture, $"{Math.Round(k):0} in {of}");
            }
        }

        return Percent(share);
    }

    public static string Percent(double share) => string.Create(CultureInfo.InvariantCulture, $"{share * 100:0.#} %");

    /// <summary>The zone in words: "an 8 in circle", "a 15 by 32.5 cm rectangle".</summary>
    public static string Zone(PointBlankZone zone, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(zone);
        ArgumentNullException.ThrowIfNull(units);
        string size = zone.Shape == HitShape.Circle ? Size(units, zone.WidthInches) + " circle" : $"{SizeNumber(units, zone.WidthInches)} by {Size(units, zone.HeightInches)} rectangle";
        return (size.StartsWith('8') || size.StartsWith("11", StringComparison.Ordinal) || size.StartsWith("18", StringComparison.Ordinal) ? "an " : "a ") + size;
    }

    /// <summary>Works out what the view shows for <paramref name="request"/>, with the rifle's clicks and zero distance and the person's units.</summary>
    public static PointBlankResult Work(PointBlankModel model, PointBlankRequest request, Rifle rifle, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rifle);
        ArgumentNullException.ThrowIfNull(units);
        string Distance(double yards) => Whole(yards, units);
        string share = Share(request.Certainty);
        var zone = request.Zone;

        PointBlankWindow? window = null;
        PointBlankBand band;
        string? windowSays = null;
        switch (request.Mode)
        {
            case PointBlankMode.MostLikely:
                band = model.MostLikely(zone, request.Certainty, request.LikelyYards);
                windowSays = $"Zeroed for {Distance(request.LikelyYards)}: the group is centered on the zone there, and {Percent(model.Chance(zone, request.LikelyYards, band.LaunchDeltaMoa))} of shots land in it.";
                break;
            case PointBlankMode.Window:
                window = model.Window(zone, request.Certainty, request.WindowFromYards, request.WindowToYards);
                band = model.Band(zone, request.Certainty, window.LaunchDeltaMoa);
                string across = Span(request.WindowFromYards, request.WindowToYards, units);
                windowSays = window.Reaches
                    ? $"Across {across}, at least {Percent(window.WorstChance)} of shots land in the zone with this zero; the fewest at {Distance(window.WorstYards)}."
                    : $"No single zero keeps {share} across {across}. The best one keeps {Percent(window.WorstChance)} at its worst point, {Distance(window.WorstYards)}. "
                      + "To do better: narrow the window, choose a larger zone, or hold over at the far end.";
                break;
            default:
                band = model.Longest(zone, request.Certainty);
                break;
        }

        var ending = model.Ending(band, request.EnergyFloorFtLb, request.ExpansionFps);
        var classic = model.Classic(zone);
        var current = model.Band(zone, request.Certainty, 0);
        var currentClassic = model.ClassicBand(zone, 0);

        string headline = band.Exists
            ? "Dead on " + Span(band.StartYards, ending.EndYards, units)
            : $"No distance keeps {share} in the zone";
        string certainty = $"{share} shots in {Zone(zone, units)}, from your mean radius of {Angle(model.MeanRadiusMoa, units.Aiming(rifle))}";

        double zeroYards = rifle.ZeroDistanceYards ?? model.Input.ZeroRangeYards;
        string howToSet = HowToSet(model, band.LaunchDeltaMoa, rifle, units, zeroYards);

        string ends = ending.Why switch
        {
            PointBlankEnd.Energy => $"It ends at {Distance(ending.EndYards)}, where the bullet falls below the energy floor of {Energy(request.EnergyFloorFtLb!.Value, units)} (a rule of thumb), before the {share} band would.",
            PointBlankEnd.Expansion => $"It ends at {Distance(ending.EndYards)}, where the bullet slows below {units.Speed(request.ExpansionFps!.Value)}, the least it opens at, before the {share} band would.",
            PointBlankEnd.SolverRange => $"It runs on past {Distance(PointBlankModel.MaxYards)}, as far as GroupLab looks.",
            _ => band.Exists ? $"It ends at {Distance(ending.EndYards)}, past which fewer than {share} shots would land in the zone." : $"Even at the best zero fewer than {share} shots land in the zone: choose a larger zone or a lower certainty.",
        };
        string? expansion = request.ExpansionFps is > 0 ? null : ExpansionNotEntered;

        string currentZero = current.Exists
            ? $"Your rifle's {Distance(zeroYards)} zero: dead on {Span(current.StartYards, current.EndYards, units)} at {share}"
              + (currentClassic.Exists ? $", or {Span(currentClassic.StartYards, currentClassic.EndYards, units)} by the classic rule." : ".")
            : $"Your rifle's {Distance(zeroYards)} zero keeps fewer than {share} shots in the zone at every distance.";

        string classicLine = $"The classic figure, for reference: zero {Size(units, Math.Abs(classic.OffsetAt100Inches))} {(classic.OffsetAt100Inches >= 0 ? "high" : "low")} at {Distance(100)}, "
            + $"the path within {Size(units, zone.HalfHeight)} of the aim from {Span(classic.StartYards, classic.EndYards, units)} "
            + $"(near zero {Distance(classic.NearZeroYards)}, peak {Size(units, classic.PeakInches)} at {Distance(classic.PeakYards)}, far zero {Distance(classic.FarZeroYards)}). "
            + "It keeps the path in the zone, not the shots: at its top and its far end about half of them land outside.";

        double drift = Math.Abs(model.WindAt(ending.EndYards));
        string wind = $"A {WindSpeed(model.Crosswind, units)} full-value crosswind moves the bullet {Size(units, drift)} at {Distance(ending.EndYards)}.";
        string? windWarning = drift > zone.WidthInches / 2 ? "That is more than half the zone's width: the wind alone can take the shot out of it." : null;

        var holds = new List<PointBlankHold>();
        if (request.Mode == PointBlankMode.MostLikely && band.Exists)
        {
            var aim = units.Aiming(rifle);
            for (double y = Math.Ceiling((ending.EndYards + 1) / 25) * 25; y <= ending.EndYards + 100 && y <= PointBlankModel.MaxYards; y += 25)
            {
                double low = -model.Height(y, band.LaunchDeltaMoa);
                holds.Add(new PointBlankHold(y, low, $"{Distance(y)}: hold {Size(units, Math.Abs(low))} {(low >= 0 ? "high" : "low")}, {Angle(MoaOf(Math.Abs(low), y), aim)}"));
            }
        }

        // Entry 456 section 1: a zero that needs more elevation than the scope has left above the present one cannot be dialed.
        if (ScopeMount.Figures(rifle, model.LaunchAngleMoa)?.LeftAboveZeroMoa is { } left && band.LaunchDeltaMoa > left)
        {
            howToSet += string.Create(CultureInfo.InvariantCulture,
                $" The scope has only {ScopeMount.FromMoa(left, rifle.ClickUnit):0.0} {(rifle.ClickUnit == AngularUnit.Mrad ? "mil" : "MOA")} left above your zero, so this zero cannot be dialed.");
        }

        double? far = model.FarZero(band.LaunchDeltaMoa);
        string keep = far is { } f
            ? $"Keeping it on {rifle.Name} sets its zero distance to {Distance(f)}, where this path crosses the line of sight again, and keeps the {Distance(zeroYards)} zero to put back."
            : $"This zero never crosses the line of sight again, so it cannot be kept on {rifle.Name} as a zero distance.";

        return new PointBlankResult(band, ending, classic, current, currentClassic, window, headline, certainty, howToSet, ends, expansion, currentZero,
            classicLine, wind, windWarning, windowSays, holds, far, keep);
    }

    /// <summary>
    /// "Zero 2.6 in high at 100 yd: 0.72 mil, 7 clicks up from your 100 yd zero", in the rifle's scope unit and clicks, at the distance the
    /// rifle is zeroed at now.
    /// </summary>
    public static string HowToSet(PointBlankModel model, double deltaMoa, Rifle rifle, UnitSettings units, double zeroYards)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(rifle);
        ArgumentNullException.ThrowIfNull(units);
        string at = Whole(zeroYards, units);
        double inches = deltaMoa * PointBlankModel.InchesPerMoaPerYard * zeroYards;
        if (Math.Abs(inches) < 0.05)
        {
            return $"Your {at} zero already gives this.";
        }

        string direction = inches > 0 ? "up" : "down";
        string clicks = rifle.ClickValue > 0 ? ", " + Clicks.For(inches, zeroYards * 36, rifle, direction).Describe() : "";
        if (ScopeTracking.Applied(rifle.SightScaleFactor) is { } ssf && clicks.Length > 0)
        {
            clicks += " (" + ssf + ")";
        }
        return $"Zero {Size(units, Math.Abs(inches))} {(inches > 0 ? "high" : "low")} at {at}: {Angle(Math.Abs(deltaMoa), units.Aiming(rifle))}{clicks} from your {at} zero.";
    }

    /// <summary>A length to a tenth of an inch or centimetre, or a whole millimetre: what a point blank figure is good to, no finer.</summary>
    public static string Size(UnitSettings units, double inches) => SizeNumber(units, inches) + " " + UnitSettings.Symbol(units.Linear);

    private static string SizeNumber(UnitSettings units, double inches) =>
        UnitSettings.FromInches(inches, units.Linear).ToString(units.Linear == LinearUnit.Millimetre ? "0" : "0.#", CultureInfo.InvariantCulture);

    /// <summary>A distance to the whole yard or meter, which is as well as anybody knows a distance to an animal.</summary>
    public static string Whole(double yards, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return UnitSettings.DistanceFromInches(yards * 36, units.Distance).ToString("0", CultureInfo.InvariantCulture) + " " + UnitSettings.Symbol(units.Distance);
    }

    /// <summary>"0 to 263 yd": two distances with their unit once.</summary>
    public static string Span(double fromYards, double toYards, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(units);
        string N(double yards) => UnitSettings.DistanceFromInches(yards * 36, units.Distance).ToString("0", CultureInfo.InvariantCulture);
        return $"{N(fromYards)} to {N(toYards)} {UnitSettings.Symbol(units.Distance)}";
    }

    private static double MoaOf(double inches, double yards) => inches / (PointBlankModel.InchesPerMoaPerYard * yards);

    /// <summary>An angle given in MOA, in the scope's unit: "0.72 mil", "2.5 MOA".</summary>
    private static string Angle(double moa, UnitSettings aim)
    {
        double value = aim.Angular == AngularUnit.Mrad ? moa * BallisticSolver.MilPerMoa : moa;
        return string.Create(CultureInfo.InvariantCulture, $"{value:0.00} {UnitSettings.Symbol(aim.Angular == AngularUnit.Mrad ? AngularUnit.Mrad : AngularUnit.Moa)}");
    }

    private static string Energy(double footPounds, UnitSettings units) => units.Distance == DistanceUnit.Metre
        ? (footPounds * 1.3558179483).ToString("0", CultureInfo.InvariantCulture) + " J"
        : footPounds.ToString("0", CultureInfo.InvariantCulture) + " ft lb";

    private static string WindSpeed(double mph, UnitSettings units) => units.Distance == DistanceUnit.Metre
        ? (mph * 1.609344).ToString("0.#", CultureInfo.InvariantCulture) + " km/h"
        : mph.ToString("0.#", CultureInfo.InvariantCulture) + " mph";
}
