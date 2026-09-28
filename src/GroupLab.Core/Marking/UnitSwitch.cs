using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>What kind of value a number on screen is, which decides what tapping it switches.</summary>
public enum UnitKind
{
    /// <summary>An angle: MOA and mil.</summary>
    Angle,

    /// <summary>A size on the paper: inches and centimeters.</summary>
    Length,

    /// <summary>A distance to the target: yards and meters.</summary>
    Distance,
}

/// <summary>
/// Tap a number to switch units, NOTES-FROM-PLANNING.md entries 272 and 273, as the concept's working demo does it: tapping an angle switches
/// MOA and mil, a size on the paper inches and centimeters, a distance yards and meters, every number of that kind at once, everywhere, and
/// remembered, because it is the same setting Settings shows. Pressing and holding (or right-clicking) lists every unit the value can take,
/// so SMOA and millimeters are never out of reach. The label beside a number still explains it; only the number switches.
/// </summary>
public static class UnitSwitch
{
    /// <summary>The one-time hint, entry 273: word for word.</summary>
    public const string Hint = "Tap a number to switch units";

    /// <summary>The hint's longer form, shown once on a result, from the concept.</summary>
    public const string HintMore = "Angles switch between MOA and mil, sizes between inches and centimeters, everywhere at once, and GroupLab remembers. Tap a label to have it explained. Press and hold a number for every unit.";

    /// <summary>The settings after one tap on a value of <paramref name="kind"/>.</summary>
    public static UnitSettings Switch(UnitSettings units, UnitKind kind)
    {
        ArgumentNullException.ThrowIfNull(units);
        return kind switch
        {
            UnitKind.Angle => units with { Angular = units.Angular == AngularUnit.Mrad ? AngularUnit.Moa : AngularUnit.Mrad },
            UnitKind.Length => units with { Linear = units.Linear == LinearUnit.Inch ? LinearUnit.Centimetre : LinearUnit.Inch },
            _ => units with { Distance = units.Distance == DistanceUnit.Yard ? DistanceUnit.Metre : DistanceUnit.Yard },
        };
    }

    /// <summary>Every unit a value of <paramref name="kind"/> can take, for press and hold: its symbol, and the settings with it chosen.</summary>
    public static IReadOnlyList<(string Symbol, UnitSettings Units, bool Current)> Choices(UnitSettings units, UnitKind kind)
    {
        ArgumentNullException.ThrowIfNull(units);
        return kind switch
        {
            UnitKind.Angle => [.. UnitSettings.AngularChoices.Select(a => (UnitSettings.Symbol(a), units with { Angular = a }, a == units.Angular))],
            UnitKind.Length => [.. new[] { LinearUnit.Inch, LinearUnit.Centimetre, LinearUnit.Millimetre }.Select(l => (UnitSettings.Symbol(l), units with { Linear = l }, l == units.Linear))],
            _ => [.. new[] { DistanceUnit.Yard, DistanceUnit.Metre }.Select(d => (UnitSettings.Symbol(d), units with { Distance = d }, d == units.Distance))],
        };
    }

    /// <summary>The note that says what changed, entry 273: "Angles now in mil everywhere".</summary>
    public static string Said(UnitSettings after, UnitKind kind)
    {
        ArgumentNullException.ThrowIfNull(after);
        return kind switch
        {
            UnitKind.Angle => $"Angles now in {UnitSettings.Symbol(after.Angular)} everywhere",
            UnitKind.Length => $"Sizes now in {after.Linear switch { LinearUnit.Centimetre => "centimeters", LinearUnit.Millimetre => "millimeters", _ => "inches" }} everywhere",
            _ => $"Distances now in {(after.Distance == DistanceUnit.Metre ? "meters" : "yards")} everywhere",
        };
    }
}
