using System.Globalization;
using System.Reflection;
using System.Text;

namespace GroupLab.Core.Marking;

/// <summary>One cartridge of the lookup: its name, the other names it goes by, its bullet diameter, and how commonly it is shot.</summary>
/// <param name="Name">The name it is listed under.</param>
/// <param name="Aliases">Every other name it goes by, which a search finds it by too.</param>
/// <param name="DiameterInches">The bullet's diameter in inches.</param>
/// <param name="DiameterMillimetres">The same in millimetres, as the table gives it.</param>
/// <param name="Kind">"rifle", "handgun", or both where it is on both lists.</param>
/// <param name="Tier">1 everyday, 2 common, 3 niche but current, 4 rare or obsolete.</param>
/// <param name="Precision">Whether precision shooters (benchrest, F-Class, PRS, silhouette) commonly shoot it.</param>
/// <param name="Source">Where the row came from: "wikipedia", or "added" by planning.</param>
/// <param name="Note">What the table says about the row, such as a diameter that is approximate.</param>
public sealed record CartridgeRow(
    string Name,
    IReadOnlyList<string> Aliases,
    double DiameterInches,
    double DiameterMillimetres,
    string Kind,
    int Tier,
    bool Precision,
    string Source,
    string Note)
{
    /// <summary>The name and every alias.</summary>
    public IEnumerable<string> Names => Aliases.Prepend(Name);
}

/// <summary>
/// Every cartridge, NOTES-FROM-PLANNING.md entry 314 section 2: Alan asked for a lookup that autocompletes as one types, for people who know
/// the cartridge's name and not its bullet's diameter. <c>Data/cartridges.csv</c> is planning's draft of 661 rows from Wikipedia's rifle and
/// handgun lists and 12 precision cartridges Wikipedia leaves out, one row a cartridge: 56 rows that named a cartridge already listed (the
/// two lists share many, and some list one cartridge under two names) were merged into it, every name kept as an alias.
/// <para>
/// Alan chose, 2026-09-30: every row in the lookup, and only tiers 1 and 2 and every precision row in the list shown before anything is
/// typed, so the rare ones appear only when asked for. The confirmed families of <see cref="CartridgeTable"/> stay what a typed name is
/// read as; this table answers the search, and a name chosen from it carries its own diameter. A name typed in full and not chosen is not
/// read from here, so the cartridges entry 163 holds back (.45 ACP, 7.62x39 and others) still ask for a diameter when typed.
/// </para>
/// </summary>
public static class CartridgeLookup
{
    private static readonly Lazy<IReadOnlyList<CartridgeRow>> Loaded = new(Load);

    /// <summary>Every row of the table, in its order.</summary>
    public static IReadOnlyList<CartridgeRow> All => Loaded.Value;

    /// <summary>
    /// The rows a caliber box offers: every one whose bullet is from 0.1 to 1 in, the range a hole can be measured in. Three cannon rounds
    /// and the 2.34 mm rimfire lie outside it and are not offered.
    /// </summary>
    public static IReadOnlyList<CartridgeRow> Offered { get; } = [.. All.Where(r => r.DiameterInches >= 0.1 && r.DiameterInches <= 1)];

    /// <summary>What the box shows before anything is typed: tiers 1 and 2 and every precision row, the most shot first.</summary>
    public static IReadOnlyList<CartridgeRow> Common { get; } =
        [.. Offered.Where(r => r.Tier <= 2 || r.Precision).OrderBy(r => r.Tier).ThenByDescending(r => r.Precision).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)];

    private static List<CartridgeRow> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GroupLab.Core.Data.cartridges.csv")
            ?? throw new InvalidOperationException("the cartridge lookup is not embedded in GroupLab.Core");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var rows = new List<CartridgeRow>();
        string[]? header = null;
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var cells = Cells(line);
            if (header is null)
            {
                header = [.. cells];
                continue;
            }

            string Cell(string name) => cells[Array.IndexOf(header, name)];
            rows.Add(new CartridgeRow(
                Cell("name"),
                [.. Cell("aliases").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
                double.Parse(Cell("diameter_in"), NumberStyles.Float, CultureInfo.InvariantCulture),
                double.Parse(Cell("diameter_mm"), NumberStyles.Float, CultureInfo.InvariantCulture),
                Cell("type").Replace(';', ' '),
                int.Parse(Cell("tier"), CultureInfo.InvariantCulture),
                Cell("precision") == "yes",
                Cell("source"),
                Cell("note")));
        }

        return rows;
    }

    /// <summary>The cells of one line of the table, where a cell in double quotes may hold commas.</summary>
    internal static List<string> Cells(string line)
    {
        var cells = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == ',')
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else
            {
                cell.Append(c);
            }
        }

        cells.Add(cell.ToString());
        return cells;
    }

    /// <summary>A name with case, punctuation and spacing set aside: "6.5 Creedmoor", "6.5creedmoor" and "65 CREEDMOOR" are one key.</summary>
    public static string Key(string text) => new([.. text.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit)]);

    /// <summary>The words of a name with points taken out, so "6.5mm Creedmoor" is "65mm" and "creedmoor", and ".30-06" is "30" and "06".</summary>
    private static string[] Words(string text) =>
        new string([.. text.ToLowerInvariant().Replace(".", "", StringComparison.Ordinal).Select(c => char.IsAsciiLetterOrDigit(c) ? c : ' ')])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Whether a name answers what was typed: its key starts with the typed key, or every typed word starts one of its words.</summary>
    private static bool Answers(string name, string typedKey, string[] typedWords)
    {
        if (Key(name).StartsWith(typedKey, StringComparison.Ordinal))
        {
            return true;
        }

        var words = Words(name);
        return typedWords.All(t => words.Any(w => w.StartsWith(t, StringComparison.Ordinal)));
    }

    /// <summary>The row a name or alias means exactly, case, punctuation and spacing aside, or null.</summary>
    public static CartridgeRow? Exact(string? text)
    {
        string key = Key(text ?? "");
        return key.Length == 0 ? null : Offered.Where(r => r.Names.Any(n => Key(n) == key)).OrderBy(r => r.Tier).FirstOrDefault();
    }

    /// <summary>
    /// The rows that answer what was typed, forgiving punctuation and spacing ("65 creed", "6.5cm", "308", "9mm"), entry 314 section 2.2:
    /// a name or alias typed exactly first, then by tier, then the precision rows, then those whose name starts with what was typed. With
    /// nothing typed, <see cref="Common"/>.
    /// </summary>
    public static IReadOnlyList<CartridgeRow> Search(string? text)
    {
        string key = Key(text ?? "");
        if (key.Length == 0)
        {
            return Common;
        }

        var typedWords = Words(text!);
        return
        [
            .. Offered.Where(r => r.Names.Any(n => Answers(n, key, typedWords)))
                .OrderByDescending(r => r.Names.Any(n => Key(n) == key))
                .ThenBy(r => r.Tier)
                .ThenByDescending(r => r.Precision)
                .ThenByDescending(r => r.Names.Any(n => Key(n).StartsWith(key, StringComparison.Ordinal)))
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase),
        ];
    }
}
