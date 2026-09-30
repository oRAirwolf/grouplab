using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>What the caliber box offers, NOTES-FROM-PLANNING.md entry 314 section 1, one setting on the desktop, Android and iOS alike.</summary>
public enum CaliberList
{
    /// <summary>Bullet diameters, each with its usual names: what a shooter who knows the caliber looks for. Alan's choice.</summary>
    Calibers,

    /// <summary>Cartridge names, each carrying its diameter: for somebody who knows the cartridge and not the bullet.</summary>
    Cartridges,

    /// <summary>One list, calibers first, then cartridges; the default for a new install.</summary>
    Both,
}

/// <summary>One line of the Calibers list: a bullet diameter, its usual names, and the cartridges that share it.</summary>
/// <param name="Inches">The bullet's diameter.</param>
/// <param name="Label">".264 (6.5 mm)": the diameter and its usual names, which the box keeps after it is chosen.</param>
/// <param name="Line">The label and the cartridges at that diameter, as the list shows it.</param>
/// <param name="Keys">Everything a search finds it by: its diameter, its usual names, and the names of its cartridges.</param>
public sealed record CaliberLine(double Inches, string Label, string Line, IReadOnlyList<string> Keys);

/// <summary>
/// The caliber box's list and what it keeps, NOTES-FROM-PLANNING.md entries 312 section 5 and 314.
/// <para>
/// Entry 312 section 5, from Alan's iPad: a choice filled the box with its whole line, "6.5 Creedmoor, 6.5x55 Swedish, .260 Remington and
/// others: 0.264 in (6.71 mm)", and changing it meant deleting all of that. The box keeps a short name, "6.5 Creedmoor, 0.264 in" or
/// ".264 (6.5 mm)", with the whole line under it, and the short name reads back as the same diameter.
/// </para>
/// <para>
/// Entry 314: a setting chooses what the box offers, <see cref="CaliberList"/>. The Calibers list is the bullet diameters of the cartridge
/// table's tiers 1 and 2, each with its usual names and the cartridges <see cref="CartridgeTable"/> groups under it; the Cartridges list is
/// <see cref="CartridgeLookup"/>. Any other diameter is still typed in inches or millimetres.
/// </para>
/// </summary>
public static partial class CaliberChoices
{
    /// <summary>
    /// The usual names of each diameter, entry 314 section 1's own list (".224 (5.56 mm)" to ".452 (.45)"), and the names shooters use for
    /// the other diameters of tiers 1 and 2. A diameter not here is shown bare, as ".311" and ".338" are in the entry.
    /// </summary>
    private static readonly Dictionary<double, string> UsualNames = new()
    {
        [0.172] = ".17",
        [0.204] = ".20",
        [0.223] = ".22 rimfire",
        [0.224] = "5.56 mm",
        [0.243] = "6 mm",
        [0.251] = ".25 ACP",
        [0.257] = ".25",
        [0.264] = "6.5 mm",
        [0.277] = "6.8 mm, .270",
        [0.284] = "7 mm",
        [0.308] = "7.62 mm, .30",
        [0.309] = ".32 ACP",
        [0.312] = ".32",
        [0.355] = "9 mm",
        [0.356] = ".38 Super",
        [0.363] = "9x18",
        [0.400] = "10 mm, .40",
        [0.410] = ".41",
        [0.429] = ".44",
        [0.452] = ".45",
        [0.458] = ".45-70",
        [0.500] = ".50",
    };

    private static readonly Lazy<IReadOnlyList<CaliberLine>> CaliberLines = new(MakeCalibers);

    /// <summary>The Calibers list: every bullet diameter of the table's tiers 1 and 2, smallest first.</summary>
    public static IReadOnlyList<CaliberLine> Calibers => CaliberLines.Value;

    private static IReadOnlyList<CaliberLine> MakeCalibers()
    {
        var lines = new List<CaliberLine>();
        foreach (var at in CartridgeLookup.Offered.Where(r => r.Tier <= 2).GroupBy(r => r.DiameterInches).OrderBy(g => g.Key))
        {
            double inches = at.Key;
            string number = inches.ToString(".000#", CultureInfo.InvariantCulture);
            string label = UsualNames.TryGetValue(inches, out string? usual) ? $"{number} ({usual})" : number;

            // Entry 314 section 4: the grouping CartridgeTable already makes under one diameter, where it has that diameter; otherwise the
            // table's own cartridges at it, the most shot first.
            var family = CartridgeTable.Families.FirstOrDefault(f => Math.Abs(f.Diameter - inches) < 1e-9);
            var names = family is not null
                ? family.Cartridges.Select(c => c.Name).ToList()
                : [.. at.OrderBy(r => r.Tier).ThenByDescending(r => r.Precision).Select(r => r.Name)];
            string listed = names.Count <= 3 ? string.Join(", ", names) : string.Join(", ", names.Take(3)) + " and others";
            var keys = new List<string> { number, number.TrimStart('.') };
            if (usual is not null)
            {
                keys.AddRange(usual.Split(", "));
            }

            if (family is not null)
            {
                keys.AddRange(family.Shorthand.Append(family.Name).Concat(family.Cartridges.SelectMany(c => c.Shorthand.Prepend(c.Name))));
            }

            keys.AddRange(at.SelectMany(r => r.Names));
            lines.Add(new CaliberLine(inches, label, $"{label}: {listed}", [.. keys.Distinct(StringComparer.OrdinalIgnoreCase)]));
        }

        return lines;
    }

    /// <summary>A cartridge as its list shows it: "6.5 Creedmoor, 0.264 in (6.71 mm)".</summary>
    public static string Line(CartridgeRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return string.Create(CultureInfo.InvariantCulture, $"{ShortName(row.Name, row.DiameterInches)} ({row.DiameterInches * 25.4:0.00} mm)");
    }

    /// <summary>
    /// What the box offers for what was typed, entry 314 section 1: in Calibers, the diameters whose number, usual names or cartridges answer
    /// it, with the families the first could be mistaken for beneath it (entry 163); in Cartridges, the lookup's answer; in Both, the calibers
    /// first, then the cartridges. With nothing typed, every caliber, and the cartridges shown before typing (tiers 1 and 2 and every
    /// precision row).
    /// </summary>
    public static IReadOnlyList<string> Suggest(string? text, CaliberList list)
    {
        var lines = new List<string>();
        if (list != CaliberList.Cartridges)
        {
            lines.AddRange(SuggestCalibers(text));
        }

        if (list != CaliberList.Calibers)
        {
            lines.AddRange(CartridgeLookup.Search(text).Take(string.IsNullOrWhiteSpace(text) ? int.MaxValue : 60).Select(Line));
        }

        return lines;
    }

    private static IEnumerable<string> SuggestCalibers(string? text)
    {
        string key = CartridgeLookup.Key(text ?? "");
        if (key.Length == 0)
        {
            return Calibers.Select(c => c.Line);
        }

        var found = Calibers
            .Where(c => c.Keys.Any(k => CartridgeLookup.Key(k).StartsWith(key, StringComparison.Ordinal)))
            .OrderByDescending(c => c.Keys.Any(k => CartridgeLookup.Key(k) == key))
            .ToList();
        if (found.Count == 0)
        {
            return [];
        }

        var lines = new List<string> { found[0].Line };
        if (CartridgeTable.Families.FirstOrDefault(f => Math.Abs(f.Diameter - found[0].Inches) < 1e-9) is { } family)
        {
            lines.AddRange(family.Traps.Select(CartridgeTable.Trap).OfType<string>());
        }

        lines.AddRange(found.Skip(1).Select(c => c.Line));
        return lines;
    }

    /// <summary>The short name for a line of the list; a line that is not a choice, such as a warning, or typed text, is kept as it is.</summary>
    public static string Short(string? line)
    {
        string text = line?.Trim() ?? "";
        if (Calibers.FirstOrDefault(c => c.Line == text || c.Label == text) is { } caliber)
        {
            return caliber.Label;
        }

        if (LineForm().Match(text) is { Success: true } cartridge && Named(cartridge.Groups["name"].Value, cartridge.Groups["inches"].Value) is not null)
        {
            return cartridge.Groups["short"].Value;
        }

        return CartridgeTable.FromSuggestion(text) is { } family ? ShortName(family.Cartridges[0].Name, family.Diameter) : text;
    }

    /// <summary>"6.5 Creedmoor, 0.264 in": a name and its bullet diameter, as the box keeps it.</summary>
    public static string ShortName(string name, double inches) =>
        string.Create(CultureInfo.InvariantCulture, $"{name}, {inches.ToString("0.000#", CultureInfo.InvariantCulture)} in");

    /// <summary>
    /// The whole line to show under the box for a short name it holds: the caliber's cartridges, or the cartridge's other names, kind and
    /// diameter in both units; null where the box holds typed text or a whole line already.
    /// </summary>
    public static string? Explain(string? text)
    {
        string typed = text?.Trim() ?? "";
        if (Calibers.FirstOrDefault(c => c.Label == typed) is { } caliber)
        {
            return caliber.Line;
        }

        if (ShortForm().Match(typed) is not { Success: true } m)
        {
            return null;
        }

        if (Named(m.Groups["name"].Value, m.Groups["inches"].Value) is { } row)
        {
            var others = row.Aliases.Where(a => !string.Equals(a, m.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)).Take(3).ToList();
            string also = others.Count == 0 ? "" : "; also called " + string.Join(", ", others);
            return string.Create(CultureInfo.InvariantCulture, $"{row.Name}: {row.DiameterInches:0.000#} in ({row.DiameterInches * 25.4:0.00} mm), {Kind(row.Kind)}{also}");
        }

        double inches = double.Parse(m.Groups["inches"].Value, CultureInfo.InvariantCulture);
        return CartridgeTable.Families.FirstOrDefault(f => Math.Abs(f.Diameter - inches) < 1e-9) is { } family ? CartridgeTable.Describe(family) : null;
    }

    private static string Kind(string kind) => kind switch
    {
        "rifle" => "a rifle cartridge",
        "handgun" => "a handgun cartridge",
        _ => "a rifle and handgun cartridge",
    };

    /// <summary>
    /// The diameter a choice carries: a caliber's label or line, or a cartridge's short name or line whose name is in the lookup or the
    /// confirmed table at that diameter. Null for anything else, which is read as typed.
    /// </summary>
    public static double? Diameter(string? text)
    {
        string typed = text?.Trim() ?? "";
        if (Calibers.FirstOrDefault(c => c.Line == typed || c.Label == typed) is { } caliber)
        {
            return caliber.Inches;
        }

        var m = LineForm().Match(typed);
        if (!m.Success)
        {
            m = ShortForm().Match(typed);
        }

        if (!m.Success)
        {
            return null;
        }

        double inches = double.Parse(m.Groups["inches"].Value, CultureInfo.InvariantCulture);
        bool known = Named(m.Groups["name"].Value, m.Groups["inches"].Value) is not null
            || (CartridgeTable.Named(m.Groups["name"].Value) is { } family && Math.Abs(family.Diameter - inches) < 1e-9);
        return known ? inches : null;
    }

    /// <summary>The lookup's row for a name at the diameter written beside it, or null where the two do not agree.</summary>
    private static CartridgeRow? Named(string name, string inches)
    {
        double at = double.Parse(inches, CultureInfo.InvariantCulture);
        string key = CartridgeLookup.Key(name);
        return CartridgeLookup.Offered.FirstOrDefault(r => Math.Abs(r.DiameterInches - at) < 1e-9 && r.Names.Any(n => CartridgeLookup.Key(n) == key));
    }

    [GeneratedRegex(@"^(?<name>.+?), (?<inches>0?\.\d{3,4}) in$")]
    private static partial Regex ShortForm();

    [GeneratedRegex(@"^(?<short>(?<name>.+?), (?<inches>0?\.\d{3,4}) in) \(\d+\.\d{2} mm\)$")]
    private static partial Regex LineForm();
}
