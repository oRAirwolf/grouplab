using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>
/// A group's calibre, NOTES-FROM-PLANNING.md entry 24 section 5: optional, entered once for the group and not per shot. It does three things:
/// extreme spread edge to edge as well as centre to centre, a tap snap radius sized to the hole, and a flag on a marked hole too large for the
/// calibre. Nothing is gated on it: without it there is no edge-to-edge figure, the default snap radius and no size check, never a refusal.
/// <para>
/// <b>It is a bullet diameter and nothing else, by Alan's decision, entry 107 section 1.</b> It is typed in inches, or in millimetres marked
/// mm, and no calibre or cartridge name is accepted. This reverses entry 105 section 7 and entry 106 section 3, which read names through a
/// table: every defect those entries fixed came from reading a name as a diameter, 36 of Alan's 44 names read wrong, nine meant more than one
/// diameter and "300 Blackout" read as 0.300, and every name the table did not know still fell back to a guess. A diameter has one meaning.
/// The names were removed on purpose; do not restore them as if they had been lost.
/// </para>
/// <para>
/// <see cref="Name"/> is how the calibre is shown, the diameter in both units, and a marking saved under a name before entry 107 loads with
/// its diameter shown in its place (<see cref="MarkingFile"/>).
/// </para>
/// </summary>
public sealed partial record Calibre(string Name, double DiameterInches)
{
    /// <summary>
    /// The pick list: every distinct bullet diameter in Alan's rifle and pistol lists of entry 105 section 7, in inches, from .172 to .510.
    /// .223, which came from the old ".22 LR" entry, is not among them and can still be typed.
    /// </summary>
    public static IReadOnlyList<double> Diameters { get; } =
    [
        0.172, 0.204, 0.2215, 0.224, 0.243, 0.257, 0.264, 0.277, 0.284, 0.308, 0.309, 0.310, 0.3105, 0.312, 0.321, 0.323, 0.338, 0.355, 0.356,
        0.357, 0.358, 0.366, 0.375, 0.400, 0.410, 0.411, 0.416, 0.423, 0.430, 0.451, 0.452, 0.454, 0.458, 0.474, 0.500, 0.505, 0.510,
    ];

    /// <summary>The pick list as calibres, each shown in both units, smallest first.</summary>
    public static IReadOnlyList<Calibre> Common { get; } = [.. Diameters.Order().Select(Of)];

    /// <summary>The one sentence a refusal gives, entry 107 section 1: what to type, not what was wrong with what was typed.</summary>
    public const string Refusal = "Enter the bullet diameter in inches, such as 0.308, or in millimetres with mm, such as 7.82 mm.";

    /// <summary>A diameter in both units, as the pick list, the load panel and the calibre box all show it: ".308 in (7.82 mm)".</summary>
    public static string Shown(double inches) => string.Create(CultureInfo.InvariantCulture, $"{inches.ToString(".000#", CultureInfo.InvariantCulture)} in ({inches * 25.4:0.00} mm)");

    /// <summary>The calibre of a diameter, named as it is shown.</summary>
    public static Calibre Of(double inches) => new(Shown(inches), inches);

    /// <summary>
    /// Reads what a person typed, entry 107 section 1. Inches: a decimal below one with or without its leading zero, or any number marked
    /// "in" or with an inch mark, and the pick list's own form, ".308 in (7.82 mm)". Millimetres only when marked mm, "7.82 mm" or "7.82mm".
    /// <b>A bare number of one or more is refused, not guessed:</b> read as millimetres "7.62" is 0.300 in, which no 7.62 bullet is, so the
    /// unit is what makes it a diameter rather than a name that happens to be a number. Every diameter must lie from 0.1 to 1 in, however it was
    /// entered. Returns null for empty text, and null with <paramref name="problem"/> for anything else it cannot read.
    /// </summary>
    public static Calibre? Parse(string? text, out string? problem)
    {
        problem = null;
        string typed = text?.Trim() ?? "";
        if (typed.Length == 0)
        {
            return null;
        }

        double inches;
        if (Inches().Match(typed) is { Success: true } i)
        {
            inches = Number(i.Groups["number"].Value);
        }
        else if (Millimetres().Match(typed) is { Success: true } m)
        {
            inches = Number(m.Groups["number"].Value) / 25.4;
        }
        else
        {
            problem = Refusal;
            return null;
        }

        if (!(inches >= 0.1 && inches <= 1))
        {
            problem = "A bullet diameter lies between 0.1 and 1 in. " + Refusal;
            return null;
        }

        return Of(inches);
    }

    private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>Inches: a decimal below one with or without its leading zero, any number marked in inches, or the pick list's own form.</summary>
    [GeneratedRegex("""^(?:(?<number>0?\.\d+)\s*(?:in|")?(?:\s*\(\s*\d*\.?\d+\s*mm\s*\))?|(?<number>\d*\.?\d+)\s*(?:in|"))$""", RegexOptions.IgnoreCase)]
    private static partial Regex Inches();

    /// <summary>Millimetres, only when marked mm.</summary>
    [GeneratedRegex("""^(?<number>\d*\.?\d+)\s*mm$""", RegexOptions.IgnoreCase)]
    private static partial Regex Millimetres();
}
