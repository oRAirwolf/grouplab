using System.Globalization;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Rendering;

/// <summary>
/// One built-in sheet as a shooter would choose it, NOTES-FROM-PLANNING.md entry 25 section 2 point 1: what it is for, how many bulls,
/// the sheet size and the distance it was designed around, rather than its identifier.
/// </summary>
public sealed record LibrarySheet(string File, string Family, string? DesignedFor, TargetDefinition Definition)
{
    /// <summary>The number of sheets a print makes: one, or every tile of an assembly.</summary>
    public int Sheets => Definition.Tiling is { } tiling ? tiling.Cols * tiling.Rows : 1;

    /// <summary>The paper, named where it has a name, with its size in millimetres and inches.</summary>
    public string Paper
    {
        get
        {
            var page = Definition.Page;
            string size = string.Create(CultureInfo.InvariantCulture, $"{page.Width / 10.0:0.#} by {page.Height / 10.0:0.#} mm, {page.Width / 254.0:0.##} by {page.Height / 254.0:0.##} in");
            // Entry 113 section 7: a roll by its width in words, never the page size's own name.
            string name = page.Size switch { PageSize.Roll24 => "24 in roll", PageSize.Roll36 => "36 in roll", PageSize.Roll42 => "42 in roll", _ => page.Size.ToString() };
            return page.Size == PageSize.Custom ? size : $"{name}, {size}";
        }
    }

    /// <summary>The sentence the print screen lists the sheet by.</summary>
    public string Summary =>
        $"{Definition.Description}{(DesignedFor is null ? "" : $" Designed for {DesignedFor}.")} {Paper}{(Sheets > 1 ? $", {Sheets.ToString(CultureInfo.InvariantCulture)} sheets that assemble into one target" : "")}.";
}

/// <summary>
/// The built-in library as the print screen lists it, from docs/TARGET-LIBRARY.md: section 1's six families and section 2's intended
/// distance for each layout, keyed by file name, over the definitions themselves.
/// </summary>
public static class TargetLibrary
{
    /// <summary>File-name prefixes, most specific first, with the family and the distance each layout is designed around.</summary>
    private static readonly (string Prefix, string Family, string DesignedFor)[] Catalogue =
    [
        ("GL-CF25-100M-", "Centrefire load development", "100 m"),
        ("GL-CF25-", "Centrefire load development", "100 yd"),
        ("GL-CF30-", "Centrefire load development", "100 yd"),
        ("GL-RF25-A4", "Rimfire", "50 m"),
        ("GL-RF25-", "Rimfire", "50 yd"),
        ("GL-RF36-", "Rimfire", "50 yd"),
        ("GL-LR25-", "Large format", "200 yd"),
        ("GL-LR30-", "Large format", "200 yd"),
        ("GL-LR300-T", "Long range, tiled", "300 yd"),
        ("GL-LR300-R", "Long range, roll media", "300 yd"),
        ("GL-ZERO-MOA-100Y", "Zeroing", "100 yd"),
        ("GL-ZERO-MOA-100M", "Zeroing", "100 m"),
        ("GL-ZERO-MIL-100Y", "Zeroing", "100 yd"),
        ("GL-ZERO-MIL-100M", "Zeroing", "100 m"),
    ];

    /// <summary>The family shown for a file the catalogue does not list, which a test keeps from happening to a built-in.</summary>
    public const string OtherFamily = "Other";

    /// <summary>Every readable definition in a directory, in the catalogue's order.</summary>
    public static IReadOnlyList<LibrarySheet> Load(string directory)
    {
        var sheets = new List<(int Order, LibrarySheet Sheet)>();
        foreach (string path in Directory.EnumerateFiles(directory, "*.gltd.json").Order(StringComparer.Ordinal))
        {
            if (GltdJsonReader.ReadFile(path).Definition is not { } definition)
            {
                continue;
            }

            string file = Path.GetFileName(path);
            int order = Array.FindIndex(Catalogue, c => file.StartsWith(c.Prefix, StringComparison.Ordinal));
            var sheet = order < 0
                ? new LibrarySheet(file, OtherFamily, null, definition)
                : new LibrarySheet(file, Catalogue[order].Family, Catalogue[order].DesignedFor, definition);
            sheets.Add((order < 0 ? int.MaxValue : order, sheet));
        }

        return [.. sheets.OrderBy(s => s.Order).ThenBy(s => s.Sheet.File, StringComparer.Ordinal).Select(s => s.Sheet)];
    }
}
