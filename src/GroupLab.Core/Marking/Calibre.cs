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
    /// <para>
    /// <b>0.222 is the rimfire 22, NOTES-FROM-PLANNING.md entry 153 section 4.</b> It was missing entirely: 0.2215 is 5.45x39 and 0.224 is
    /// the centrefire 22 of 5.56x45 and 22 ARC, so the most commonly shot cartridge in the world had nothing in this list to pick, and a
    /// rimfire shooter's nearest choice was 0.9 percent too wide. The two figures sit next to each other here on purpose: they are different
    /// cartridges that happen to be a thousandth apart, not a duplicate.
    /// </para>
    /// </summary>
    public static IReadOnlyList<double> Diameters { get; } =
    [
        0.172, 0.204, 0.2215, 0.222, 0.224, 0.243, 0.257, 0.264, 0.277, 0.284, 0.308, 0.309, 0.310, 0.3105, 0.312, 0.321, 0.323, 0.338, 0.355, 0.356,
        0.357, 0.358, 0.366, 0.375, 0.400, 0.410, 0.411, 0.416, 0.423, 0.430, 0.451, 0.452, 0.454, 0.458, 0.474, 0.500, 0.505, 0.510,
    ];

    /// <summary>The pick list as calibres, each shown in both units, smallest first.</summary>
    public static IReadOnlyList<Calibre> Common { get; } = [.. Diameters.Order().Select(Of)];

    /// <summary>
    /// Calibre designations in inches that are not themselves bullet diameters, refused rather than read, NOTES-FROM-PLANNING.md entry 108
    /// section 2: ".38" would read as 0.380 in when a .38 bullet is .357 or .358, and ".270" as 0.270 against .277. A value typed with more
    /// digits is the same value, so ".300" and ".280" are refused with ".30" and ".28". This is a list of numbers refused, never matched to
    /// anything: nothing is read from it and it cannot produce a diameter, so it is not the name table returning. .40, .41 and .50 are
    /// deliberately absent, because each is also a real diameter.
    /// </summary>
    public static IReadOnlyList<decimal> InchDesignations { get; } = [0.17m, 0.20m, 0.22m, 0.25m, 0.27m, 0.28m, 0.30m, 0.303m, 0.32m, 0.35m, 0.38m, 0.44m, 0.45m];

    /// <summary>
    /// Calibre designations in millimetres that are not bullet diameters, refused for the same reason: "7.62 mm" would read as 0.300 in, a
    /// diameter no 7.62 bullet has. 9.3 and 12.7 are deliberately absent, because they are real diameters, .366 and .500.
    /// </summary>
    public static IReadOnlyList<decimal> MillimetreDesignations { get; } = [5.45m, 5.56m, 6m, 6.5m, 6.8m, 7m, 7.5m, 7.62m, 7.65m, 8m, 9m, 10m];

    /// <summary>The refusal of a designation, entry 108 section 2: it names the problem and never guesses which bullet was meant.</summary>
    public static string DesignationRefusal(string typed) => typed + " is a calibre's name, not the bullet's diameter. Enter the bullet's diameter, such as 7.82 mm or 0.308.";

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
    /// unit is what makes it a diameter rather than a name that happens to be a number. <b>A calibre designation is refused in either unit</b>,
    /// entry 108: ".38", ".270", "9mm" and "7.62 mm" are names written as numbers (<see cref="InchDesignations"/>, <see cref="MillimetreDesignations"/>). Every diameter must lie from 0.1 to 1 in, however it was
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
            string number = i.Groups["number"].Value;
            if (InchDesignations.Contains(decimal.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture)))
            {
                problem = DesignationRefusal(number + " in");
                return null;
            }

            inches = Number(number);
        }
        else if (Millimetres().Match(typed) is { Success: true } m)
        {
            string number = m.Groups["number"].Value;
            if (MillimetreDesignations.Contains(decimal.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture)))
            {
                problem = DesignationRefusal(number + " mm");
                return null;
            }

            inches = Number(number) / 25.4;
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
