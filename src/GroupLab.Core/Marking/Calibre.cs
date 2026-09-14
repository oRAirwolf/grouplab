using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>
/// A group's calibre, NOTES-FROM-PLANNING.md entry 24 section 5: optional, entered once for the group and not per shot, as free text
/// with a short pick list, because somebody will want a wildcat. It does three things: extreme spread edge to edge as well as centre
/// to centre, a tap snap radius sized to the hole, and a flag on a marked hole too large for the calibre. Nothing is gated on it:
/// without it there is no edge-to-edge figure, the default snap radius and no size check, never a refusal.
/// </summary>
public sealed partial record Calibre(string Name, double DiameterInches)
{
    /// <summary>The pick list: common bullet diameters, in inches.</summary>
    public static IReadOnlyList<Calibre> Common { get; } =
    [
        new(".17 HMR", 0.172),
        new(".22 LR", 0.223),
        new(".223 Rem, 5.56 NATO", 0.224),
        new("6 mm", 0.243),
        new("6.5 mm, .264", 0.264),
        new(".270 Win", 0.277),
        new("7 mm, .284", 0.284),
        new(".308, 7.62 mm", 0.308),
        new(".338", 0.338),
        new("9 mm", 0.355),
        new(".375", 0.375),
        new(".45", 0.452),
    ];

    /// <summary>
    /// Reads what a person typed. A name from the pick list is that entry. Otherwise the leading number is read as a diameter: in
    /// millimetres when it says mm or lies from 1 to 14 ("6.5 Creedmoor", "7.62"), in inches when it says in or is below 1 (".308 Win"),
    /// in hundredths of an inch from 14 to 100 ("22", "30-06") and thousandths from 100 to 1000 ("308", "300 Win Mag"). A name is not
    /// always its diameter, .300 Win Mag fires a .308 bullet, so the result says what it read and the diameter can be typed exactly.
    /// Returns null for empty text, and null with <paramref name="problem"/> for text it cannot read.
    /// </summary>
    public static Calibre? Parse(string? text, out string? problem)
    {
        problem = null;
        string name = text?.Trim() ?? "";
        if (name.Length == 0)
        {
            return null;
        }

        if (Common.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) is { } common)
        {
            return common;
        }

        var match = LeadingNumber().Match(name);
        if (!match.Success || !double.TryParse(match.Groups["number"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        {
            problem = "Enter a calibre from the list, or the bullet diameter, such as 0.308 or 7.62 mm.";
            return null;
        }

        string unit = match.Groups["unit"].Value.ToLowerInvariant();
        double inches = unit switch
        {
            "mm" => number / 25.4,
            "in" or "\"" => number,
            _ when number < 1 => number,
            _ when number < 14 => number / 25.4,
            _ when number < 100 => number / 100,
            _ when number < 1000 => number / 1000,
            _ => double.NaN,
        };
        if (!(inches >= 0.1 && inches <= 1))
        {
            problem = "That does not read as a bullet diameter. Enter it in inches, such as 0.308, or millimetres, such as 7.62 mm.";
            return null;
        }

        return new Calibre(name, inches);
    }

    [GeneratedRegex("""^\s*(?<number>\d*\.?\d+)\s*(?<unit>mm|in|")?""", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingNumber();
}
