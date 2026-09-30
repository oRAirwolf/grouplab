using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>
/// What the caliber box keeps after a line of its list is chosen, and what it says beneath, NOTES-FROM-PLANNING.md entry 312 section 5. On
/// Alan's iPad a choice filled the box with its whole line, "6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and others: 0.264 in (6.71 mm)",
/// and changing it meant deleting all of that. The box now keeps a short name, "6.5 Creedmoor, 0.264 in", with the whole line under it, and
/// the short name reads back as the same diameter.
/// </summary>
public static partial class CaliberChoices
{
    /// <summary>The short name for a line of the list; a line that is not a choice, such as a warning, or typed text, is kept as it is.</summary>
    public static string Short(string? line)
    {
        string text = line?.Trim() ?? "";
        return CartridgeTable.FromSuggestion(text) is { } family ? ShortName(family.Cartridges[0].Name, family.Diameter) : text;
    }

    /// <summary>"6.5 Creedmoor, 0.264 in": a name and its bullet diameter, as the box keeps it.</summary>
    public static string ShortName(string name, double inches) =>
        string.Create(CultureInfo.InvariantCulture, $"{name}, {inches.ToString("0.000#", CultureInfo.InvariantCulture)} in");

    /// <summary>
    /// The whole line to show under the box for what it holds: the cartridges at that diameter, or null where the box holds nothing chosen
    /// or already shows the whole line.
    /// </summary>
    public static string? Explain(string? text)
    {
        string typed = text?.Trim() ?? "";
        if (ShortForm().Match(typed) is not { Success: true } m)
        {
            return null;
        }

        double inches = double.Parse(m.Groups["inches"].Value, CultureInfo.InvariantCulture);
        return CartridgeTable.Families.FirstOrDefault(f => Math.Abs(f.Diameter - inches) < 1e-9) is { } family ? CartridgeTable.Describe(family) : null;
    }

    /// <summary>The diameter a short name carries, "6.5 Creedmoor, 0.264 in", or null for anything else.</summary>
    public static double? Diameter(string? text) =>
        ShortForm().Match(text?.Trim() ?? "") is { Success: true } m ? double.Parse(m.Groups["inches"].Value, CultureInfo.InvariantCulture) : null;

    [GeneratedRegex(@"^(?<name>.+?), (?<inches>0?\.\d{3,4}) in$")]
    private static partial Regex ShortForm();
}
