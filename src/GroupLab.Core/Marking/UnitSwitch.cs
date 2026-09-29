using System.Globalization;
using System.Text.RegularExpressions;
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
public static partial class UnitSwitch
{
    /// <summary>The one-time hint, entry 273: word for word.</summary>
    public const string Hint = "Tap a number to switch units";

    /// <summary>
    /// The hint's longer form, shown once on a result. Entry 280 section 1 (Alan): a tap switches that one number, not every number of its
    /// kind, and the choice is remembered for that figure.
    /// </summary>
    public const string HintMore = "Tap a number to switch this number between MOA and mil, or inches and centimeters; GroupLab remembers it for that figure. Tap a label to have it explained. Press and hold a number for every unit.";

    /// <summary>The unit symbols a value of <paramref name="kind"/> can be shown in, in the order a tap steps through the first two.</summary>
    public static IReadOnlyList<string> Symbols(UnitKind kind) => kind switch
    {
        UnitKind.Angle => ["MOA", "mil", "SMOA", "NATO mil"],
        UnitKind.Length => ["in", "cm", "mm"],
        _ => ["yd", "m"],
    };

    /// <summary>What one tap on a number in <paramref name="from"/> shows it in: MOA and mil, inches and centimeters, yards and meters.</summary>
    public static string Next(UnitKind kind, string from) => kind switch
    {
        UnitKind.Angle => from == "MOA" ? "mil" : "MOA",
        UnitKind.Length => from == "in" ? "cm" : "in",
        _ => from == "yd" ? "m" : "yd",
    };

    /// <summary>The kind a unit symbol belongs to.</summary>
    public static UnitKind? KindOfSymbol(string symbol) => symbol switch
    {
        "MOA" or "SMOA" or "mil" or "NATO mil" => UnitKind.Angle,
        "in" or "cm" or "mm" => UnitKind.Length,
        "yd" or "m" => UnitKind.Distance,
        _ => null,
    };

    /// <summary>The unit the first number of <paramref name="kind"/> in <paramref name="text"/> is shown in, or null.</summary>
    public static string? SymbolIn(string? text, UnitKind kind) =>
        text is null ? null : Numbers().Matches(text).Select(m => m.Groups["u"].Value).FirstOrDefault(u => KindOfSymbol(u) == kind);

    /// <summary>
    /// Entry 280 section 1: the text with every number of <paramref name="kind"/> shown in <paramref name="to"/> instead, a range such as
    /// "0.110 to 0.166 in" as a whole, and every other number left as it was. The number is converted from what the screen shows, so the
    /// result is as exact as the digits shown, which is closer than the digits it is shown to.
    /// </summary>
    public static string Convert(string text, UnitKind kind, string to)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Numbers().Replace(text, m =>
        {
            string from = m.Groups["u"].Value;
            if (KindOfSymbol(from) != kind || from == to)
            {
                return m.Value;
            }

            string One(string number) => Format(Factor(kind, from, to) * double.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture), to);
            string space = m.Groups["s"].Value;
            return m.Groups["a"].Success
                ? One(m.Groups["a"].Value) + m.Groups["to"].Value + One(m.Groups["n"].Value) + space + to
                : One(m.Groups["n"].Value) + space + to;
        });
    }

    private static double Factor(UnitKind kind, string from, string to) => kind switch
    {
        UnitKind.Angle => Angular.Constant(Angle(to)) / Angular.Constant(Angle(from)),
        UnitKind.Length => UnitSettings.FromInches(1, Linear(to)) / UnitSettings.FromInches(1, Linear(from)),
        _ => to == "m" ? 0.9144 : 1 / 0.9144,
    };

    private static AngularUnit Angle(string symbol) => symbol switch { "mil" => AngularUnit.Mrad, "SMOA" => AngularUnit.Smoa, "NATO mil" => AngularUnit.Mil, _ => AngularUnit.Moa };

    private static LinearUnit Linear(string symbol) => symbol switch { "cm" => LinearUnit.Centimetre, "mm" => LinearUnit.Millimetre, _ => LinearUnit.Inch };

    /// <summary>The digits a unit is shown to: angles to hundredths, inches to thousandths, centimeters to hundredths, the rest whole.</summary>
    private static string Format(double value, string unit) => value.ToString(unit switch
    {
        "in" => "0.000",
        "cm" or "MOA" or "SMOA" or "mil" or "NATO mil" => "0.00",
        "mm" => "0.0",
        _ => "0",
    }, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(?:(?<a>-?\d+(?:\.\d+)?)(?<to> to |–))?(?<n>-?\d+(?:\.\d+)?)(?<s>\s?)(?<u>NATO mil|SMOA|MOA|mil|in|cm|mm|yd|m)(?![A-Za-z])")]
    private static partial Regex Numbers();

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
