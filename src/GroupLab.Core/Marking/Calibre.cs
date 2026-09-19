using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Marking;

/// <summary>
/// What a typed calibre read as, NOTES-FROM-PLANNING.md entry 105 section 7: one calibre, or the candidates a name that means several
/// diameters could be, or the reason it could not be read. Never more than one of the three.
/// </summary>
public sealed record CalibreReading(Calibre? Calibre, IReadOnlyList<Calibre> Candidates, string? Problem);

/// <summary>
/// A group's calibre, NOTES-FROM-PLANNING.md entry 24 section 5: optional, entered once for the group and not per shot, as free text
/// with a pick list, because somebody will want a wildcat. It does three things: extreme spread edge to edge as well as centre to
/// centre, a tap snap radius sized to the hole, and a flag on a marked hole too large for the calibre. Nothing is gated on it:
/// without it there is no edge-to-edge figure, the default snap radius and no size check, never a refusal.
/// <para>
/// <b>Names are read as the bullets they fire, entry 105 section 7.</b> A calibre's name is usually not its diameter: "38" fires a .357 or
/// .358 bullet, "6.5mm" a .264 and "7.62mm" a .310 or a .308. Read by the leading number, 36 of the 44 rows of the table below came out
/// wrong, "38" by 0.023 in, which moves edge-to-edge extreme spread and the oversize flag by that much. So a name is looked up in the table
/// first, and a name that means several diameters is never resolved to one: the person is shown the candidates and chooses.
/// </para>
/// </summary>
public sealed partial record Calibre(string Name, double DiameterInches)
{
    /// <summary>
    /// The bullet diameters of entry 105 section 7, from Alan's rifle and pistol lists: 44 rows, 42 distinct pairs, each a name and the
    /// diameter it fires. Nine names appear with more than one diameter and are ambiguous by name alone.
    /// </summary>
    public static IReadOnlyList<Calibre> Table { get; } =
    [
        new("17 Cal.", 0.172),
        new("20 Cal.", 0.204),
        new("5.45 Cal.", 0.2215),
        new("22 Cal.", 0.224),
        new("6mm", 0.243),
        new("25 Cal.", 0.257),
        new("6.5mm", 0.264),
        new("270 Cal.", 0.277),
        new("7mm", 0.284),
        new("30 Cal.", 0.308),
        new("30 Cal.", 0.309),
        new("7.62mm", 0.310),
        new("303 Cal.", 0.3105),
        new("303 Cal.", 0.312),
        new("32 Cal.", 0.312),
        new("32 Cal.", 0.321),
        new("8mm", 0.323),
        new("338 Cal.", 0.338),
        new("35 Cal.", 0.355),
        new("35 Cal.", 0.357),
        new("35 Cal.", 0.358),
        new("9mm", 0.355),
        new("9mm", 0.356),
        new("38 Cal.", 0.357),
        new("38 Cal.", 0.358),
        new("9.3mm", 0.366),
        new("375 Cal.", 0.375),
        new("10mm", 0.400),
        new("400 Cal.", 0.410),
        new("41 Cal.", 0.410),
        new("405 Cal.", 0.411),
        new("416 Cal.", 0.416),
        new("423 Cal.", 0.423),
        new("44 Cal.", 0.430),
        new("45 Cal.", 0.451),
        new("45 Cal.", 0.452),
        new("45 Cal.", 0.454),
        new("45 Cal.", 0.458),
        new("470 Cal.", 0.474),
        new("50 Cal.", 0.500),
        new("505 Cal.", 0.505),
        new("50 Cal.", 0.510),
    ];

    /// <summary>
    /// The cartridge names the pick list carried before entry 105, kept so nobody loses a name they already use. Each resolves to its own
    /// diameter by its full name and by the short names in its key list; where a short name is also a table name, both count, which is how
    /// "7.62" comes to mean .308 and .310.
    /// </summary>
    private static readonly (Calibre Calibre, string[] Keys)[] Cartridges =
    [
        (new(".17 HMR", 0.172), ["17hmr"]),
        (new(".22 LR", 0.223), ["22lr"]),
        (new(".223 Rem, 5.56 NATO", 0.224), ["223", "223rem", "5.56", "5.56nato", "5.56mm"]),
        (new("6 mm", 0.243), ["6mm"]),
        (new("6.5 mm, .264", 0.264), ["6.5mm", "264"]),
        (new(".270 Win", 0.277), ["270", "270win"]),
        (new("7 mm, .284", 0.284), ["7mm", "284"]),
        (new(".308, 7.62 mm", 0.308), ["308", "308win", "7.62mm"]),
        (new(".338", 0.338), ["338"]),
        (new("9 mm", 0.355), ["9mm"]),
        (new(".375", 0.375), ["375"]),
        (new(".45", 0.452), ["45"]),
    ];

    /// <summary>The pick list: every table pair and every cartridge name, each shown with its diameter, which is what tells two 35s apart.</summary>
    public static IReadOnlyList<Calibre> Common { get; } =
        [.. Table.Select(c => new Calibre(Shown(c), c.DiameterInches)), .. Cartridges.Select(c => new Calibre(Shown(c.Calibre), c.Calibre.DiameterInches))];

    /// <summary>A name with its diameter, "35 Cal. .357", as the pick list shows it and as a chosen candidate is named.</summary>
    public static string Shown(Calibre calibre)
    {
        ArgumentNullException.ThrowIfNull(calibre);
        string diameter = calibre.DiameterInches.ToString(".000#", CultureInfo.InvariantCulture);
        return calibre.Name.Contains(diameter, StringComparison.Ordinal) ? calibre.Name : $"{calibre.Name} {diameter}";
    }

    private static readonly Lazy<Dictionary<string, List<Calibre>>> ByKey = new(() =>
    {
        var keys = new Dictionary<string, List<Calibre>>(StringComparer.Ordinal);
        void Add(string key, Calibre calibre)
        {
            if (!keys.TryGetValue(key, out var list))
            {
                keys[key] = list = [];
            }

            if (list.All(c => c.DiameterInches != calibre.DiameterInches))
            {
                list.Add(calibre);
            }
        }

        foreach (var row in Table)
        {
            string key = Key(row.Name);
            Add(key, row);
            if (key.EndsWith("mm", StringComparison.Ordinal))
            {
                Add(key[..^2], row);
            }
        }

        foreach (var (calibre, aliases) in Cartridges)
        {
            Add(Key(calibre.Name), calibre);
            foreach (string alias in aliases)
            {
                Add(alias, calibre);
                if (alias.EndsWith("mm", StringComparison.Ordinal))
                {
                    Add(alias[..^2], calibre);
                }
            }
        }

        return keys;
    });

    /// <summary>
    /// A name as it is matched: lower case, no leading point, no "cal" or "cal." on the end, no spaces, so "6.5 mm", "6.5mm" and "6.5MM"
    /// are one name and so are "270", ".270" and "270 Cal.".
    /// </summary>
    private static string Key(string name)
    {
        string key = name.Trim().ToLowerInvariant().Replace(" ", "", StringComparison.Ordinal);
        key = CalSuffix().Replace(key, "");
        return key.StartsWith('.') ? key[1..] : key;
    }

    /// <summary>
    /// Reads what a person typed, in this order.
    /// <list type="number">
    /// <item><b>A diameter wins.</b> A decimal below one, with or without its leading zero, or a number marked in inches, is the diameter as
    /// typed: ".357" is 0.357 whatever calibre it belongs to.</item>
    /// <item><b>A pick-list entry</b>, name and diameter together, is that entry.</item>
    /// <item><b>A name</b> from the table or the cartridge names, matched as <see cref="Key"/> describes, then its leading word on its own
    /// ("6.5 Creedmoor" is the 6.5mm row). One diameter resolves; several are returned as candidates and none is chosen.</item>
    /// <item><b>Otherwise the leading number</b> is read as a diameter, as it always was: in millimetres when it says mm or lies from 1 to 14,
    /// in hundredths of an inch from 14 to 100 and thousandths from 100 to 1000. That is where a wildcat lands, and the result says what it
    /// read, so the diameter can be typed exactly when the name misleads.</item>
    /// </list>
    /// </summary>
    public static CalibreReading Read(string? text)
    {
        string name = text?.Trim() ?? "";
        if (name.Length == 0)
        {
            return new CalibreReading(null, [], null);
        }

        if (Diameter().Match(name) is { Success: true } typed && double.TryParse(typed.Groups["number"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double inchesTyped))
        {
            return inchesTyped is >= 0.1 and <= 1
                ? new CalibreReading(new Calibre(name, inchesTyped), [], null)
                : new CalibreReading(null, [], "That does not read as a bullet diameter. Enter it in inches, such as 0.308, or millimetres, such as 7.62 mm.");
        }

        if (Common.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) is { } listed)
        {
            return new CalibreReading(listed, [], null);
        }

        foreach (string key in new[] { Key(name), Key(LeadingWord().Match(name).Value) }.Where(k => k.Length > 0).Distinct())
        {
            if (ByKey.Value.TryGetValue(key, out var found))
            {
                return found.Count == 1
                    ? new CalibreReading(new Calibre(name, found[0].DiameterInches), [], null)
                    : new CalibreReading(null, [.. found.OrderBy(c => c.DiameterInches).Select(c => new Calibre(Shown(c), c.DiameterInches))], null);
            }
        }

        var match = LeadingNumber().Match(name);
        if (!match.Success || !double.TryParse(match.Groups["number"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
        {
            return new CalibreReading(null, [], "Enter a calibre from the list, or the bullet diameter, such as 0.308 or 7.62 mm.");
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
        return inches is >= 0.1 and <= 1
            ? new CalibreReading(new Calibre(name, inches), [], null)
            : new CalibreReading(null, [], "That does not read as a bullet diameter. Enter it in inches, such as 0.308, or millimetres, such as 7.62 mm.");
    }

    /// <summary>
    /// <see cref="Read"/> for a caller that takes one calibre or a reason: a name that means several diameters is a problem that lists
    /// them, never a silent choice.
    /// </summary>
    public static Calibre? Parse(string? text, out string? problem)
    {
        var reading = Read(text);
        problem = reading.Problem ?? (reading.Candidates.Count > 0 ? Ambiguity(text?.Trim() ?? "", reading.Candidates) : null);
        return reading.Calibre;
    }

    /// <summary>The sentence for a name that means several diameters, with each candidate as it can be chosen.</summary>
    public static string Ambiguity(string name, IReadOnlyList<Calibre> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return $"\"{name}\" is fired as more than one bullet diameter: {string.Join(", ", candidates.Select(c => c.Name))}. Choose one, or type the diameter.";
    }

    [GeneratedRegex("""^\s*(?<number>\d*\.?\d+)\s*(?<unit>mm|in|")?""", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingNumber();

    /// <summary>A typed diameter: a decimal below one with or without its leading zero, or any number marked in inches.</summary>
    [GeneratedRegex("""^\s*(?:(?<number>0?\.\d+)\s*(?:in|")?|(?<number>\d*\.?\d+)\s*(?:in|"))\s*$""", RegexOptions.IgnoreCase)]
    private static partial Regex Diameter();

    [GeneratedRegex(@"cal\.?$")]
    private static partial Regex CalSuffix();

    /// <summary>The leading number of a name, with "mm" when it has it: "6.5" of "6.5 Creedmoor", "30" of "30-06", "9mm" of "9mm Luger".</summary>
    [GeneratedRegex(@"^\s*\.?\d*\.?\d+(?:\s*mm)?", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingWord();
}
