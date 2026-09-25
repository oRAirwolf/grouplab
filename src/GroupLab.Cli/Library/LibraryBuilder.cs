using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Cli.Library;

/// <summary>One built-in definition of TARGET-LIBRARY.md sections 4 and 5, with the file it is committed as in <c>targets/</c>.</summary>
public sealed record BuiltInTarget(string Name, string FileName, TargetDefinition Definition);

/// <summary>
/// Builds the twenty built-in sheets and the two extra tile presets from <c>tools/layout/layouts.json</c>,
/// exactly as <c>tools/gltd/check.py</c> assembles its definitions, adding what GLTD-J carries and the body
/// does not: names, descriptions, ink keys and roles, and print settings, following the section 4 example.
/// </summary>
public static class LibraryBuilder
{
    public const string Created = "2026-09-13";

    internal static readonly Dictionary<int, int[]> Stacks = new()
    {
        [254] = [254, 238, 127, 115, 25],
        [222] = [222, 208, 111, 101, 25],
        [152] = [152, 142, 76, 68, 20],
        [127] = [127, 117, 64, 56, 18],
        [381] = [381, 365, 191, 179, 38],
        [356] = [356, 342, 178, 166, 36],
        [320] = [320, 312, 160, 154, 32],
        [635] = [635, 613, 318, 302, 64],
    };

    private static readonly Dictionary<string, (PageSize Size, int Width, int Height)> Pages = new(StringComparer.Ordinal)
    {
        ["letter"] = (PageSize.Letter, 2159, 2794),
        ["a4"] = (PageSize.A4, 2100, 2970),
        ["a3"] = (PageSize.A3, 2970, 4200),
        ["tabloid"] = (PageSize.Tabloid, 2794, 4318),
        ["roll24"] = (PageSize.Roll24, 6096, 7112),
        ["roll36"] = (PageSize.Roll36, 9144, 6096),
        ["roll42"] = (PageSize.Roll42, 10668, 6096),
    };

    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["GL-CF25-LTR"] = "GroupLab 5x5 Load Development, Letter",
        ["GL-CF25-LTR-D"] = "GroupLab 5x5 Load Development with Load Block, Letter",
        ["GL-CF25-A4"] = "GroupLab 5x5 Load Development, A4",
        ["GL-CF25-100M-A4"] = "GroupLab 5x5 Load Development, 100 m, A4",
        ["GL-CF30-LTR"] = "GroupLab 5x6 Load Development, Letter",
        ["GL-RF25-LTR"] = "GroupLab 5x5 Rimfire, 50 yd, Letter",
        ["GL-RF25-A4"] = "GroupLab 5x5 Rimfire, 50 m, A4",
        ["GL-RF36-LTR"] = "GroupLab 6x6 Rimfire, Letter",
        ["GL-LR25-TAB"] = "GroupLab 5x5 Large Format, Tabloid",
        ["GL-LR25-A3"] = "GroupLab 5x5 Large Format, A3",
        ["GL-LR30-TAB"] = "GroupLab 5x6 Large Format, Tabloid",
        ["GL-LR300-T"] = "GroupLab 300 yd Tile, Letter, 2x2 Assembly",
        ["GL-LR300-TA4"] = "GroupLab 300 yd Tile, A4, 2x2 Assembly",
        ["GL-LR300-R24"] = "GroupLab 300 yd, 24 in Roll",
        ["GL-LR300-R36"] = "GroupLab 300 yd, 36 in Roll",
        ["GL-LR300-R42"] = "GroupLab 300 yd, 42 in Roll",
        ["GL-ZERO-MOA-100Y"] = "GroupLab Zeroing Grid, MOA at 100 yd",
        ["GL-ZERO-MIL-100Y"] = "GroupLab Zeroing Grid, mil at 100 yd",
        ["GL-ZERO-MOA-100M"] = "GroupLab Zeroing Grid, MOA at 100 m",
        ["GL-ZERO-MIL-100M"] = "GroupLab Zeroing Grid, mil at 100 m",
        ["GL-LR300-T (3x2)"] = "GroupLab 300 yd Tile, Letter, 3x2 Assembly",
        ["GL-LR300-TA4 (3x2)"] = "GroupLab 300 yd Tile, A4, 3x2 Assembly",
    };

    internal static readonly Ink[] Inks =
    [
        new("black", "#000000", InkRole.Artwork),
        new("paper", "#FFFFFF", InkRole.Paper),
        new("fid", "#000000", InkRole.Fiducial),
        new("code", "#000000", InkRole.Code),
        new("text", "#000000", InkRole.Text),
    ];

    internal static readonly PrintSettings Print = new(PrintScaling.None, 300, ColourMode.Mono, null, "GroupLab 0.1.0",
        "Print at 100 percent. Do not use fit to page.");

    /// <summary>In <c>check.py</c>'s order: the sixteen multi-bull sheets, the four zeroing sheets, then the 3 by 2 tile presets.</summary>
    public static IReadOnlyList<BuiltInTarget> Build(string layoutsJsonPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(layoutsJsonPath));
        var root = document.RootElement;
        var layouts = root.GetProperty("layouts").EnumerateArray().ToList();
        var targets = new List<BuiltInTarget>();
        targets.AddRange(layouts.Select(r => Sheet(r, preset: null)));
        targets.AddRange(root.GetProperty("zero").EnumerateArray().Select(Zero));
        targets.AddRange(layouts.Where(r => r.GetProperty("tile").ValueKind == JsonValueKind.Array).Select(r => Sheet(r, preset: (3, 2))));
        return targets;
    }

    private static BuiltInTarget Sheet(JsonElement r, (int Cols, int Rows)? preset)
    {
        string layoutName = r.GetProperty("name").GetString()!;
        string name = preset is null ? layoutName : $"{layoutName} ({preset.Value.Cols}x{preset.Value.Rows})";
        var (size, width, height) = Pages[r.GetProperty("page").GetString()!];
        int pitch = (int)Math.Round(r.GetProperty("pitch_in").GetDouble() * 254);
        int ring = (int)Math.Round(r.GetProperty("ring_in").GetDouble() * 254);
        var xs = Ints(r, "xs");
        var ys = Ints(r, "ys");
        var sighterXs = Ints(r, "sighter_x");
        var sighterYs = Ints(r, "sighter_y");
        int dataBlockHeight = r.GetProperty("data_block").GetInt32();
        int codeCount = r.GetProperty("qr_count").GetInt32();

        var bulls = new List<Bull>();
        foreach (int y in ys)
        {
            foreach (int x in xs)
            {
                bulls.Add(new Bull(x, y, "std", (bulls.Count + 1).ToString(CultureInfo.InvariantCulture), true, null));
            }
        }

        int sighter = 0;
        foreach (int y in sighterYs)
        {
            foreach (int x in sighterXs)
            {
                bulls.Add(new Bull(x, y, "std", "S" + (++sighter).ToString(CultureInfo.InvariantCulture), false, null));
            }
        }

        Tiling? tiling = null;
        var tile = r.GetProperty("tile");
        if (tile.ValueKind == JsonValueKind.Array)
        {
            var (tileCols, tileRows) = preset ?? (tile[0].GetInt32(), tile[1].GetInt32());
            tiling = new Tiling(tileCols, tileRows, width, height, 0);
        }

        DataBlock? dataBlock = dataBlockHeight > 0
            ? new DataBlock(Corners1.SafeMargin, height - Corners1.SafeMargin - dataBlockHeight, width - (2 * Corners1.SafeMargin),
                dataBlockHeight, DataBlockLayout.Fields3x3, FieldSet.Standard9, 280, "black", "text", 2, null)
            : null;

        // TARGET-SCHEMA.md sections 3.6 and 7: a sheet whose sighter gap departs from 1.2 times the pitch declares it.
        // layout.py reports the gap only where a sheet sets its own; cells.grid derives, so the library omits it.
        var gap = r.GetProperty("sighter_gap");
        Cells? cells = gap.ValueKind == JsonValueKind.Number ? new Cells(CellsMode.Grid, false, gap.GetInt32(), null, null, null, null) : null;

        var definition = new TargetDefinition(
            1, 0, null, Names[name], SheetDescription(xs.Count * ys.Count, pitch, sighterXs.Count * sighterYs.Count, dataBlockHeight > 0, tiling),
            "GroupLab built-in library", "CC0-1.0", Created, "dmm",
            new Page(size, width, height, Orientation.Portrait),
            Inks,
            [new RingSet("std", Discs(ring))],
            bulls,
            cells,
            new Fiducials(r.GetProperty("fid_scheme").GetString()!, FiducialFamily.AprilTag36h11, 40, 10, "fid", null),
            Codes(width, height, dataBlockHeight, codeCount),
            Print,
            dataBlock,
            null,
            tiling,
            null,
            []);

        return Finish(name, preset is null ? layoutName : $"{layoutName}.{preset.Value.Cols}x{preset.Value.Rows}", definition);
    }

    private static BuiltInTarget Zero(JsonElement z)
    {
        string name = z.GetProperty("name").GetString()!;
        var (size, width, height) = Pages[z.GetProperty("page").GetString()!];
        int cx = z.GetProperty("cx").GetInt32(), cy = z.GetProperty("cy").GetInt32();
        int divisions = z.GetProperty("divisions").GetInt32();
        int majorEvery = z.GetProperty("major_every").GetInt32();
        int dataBlockHeight = z.GetProperty("data_block").GetInt32();
        string unitName = z.GetProperty("unit").GetString()!;
        var unit = unitName.Contains("MOA", StringComparison.Ordinal) ? GridUnit.Moa : GridUnit.Mil;
        var distanceUnit = unitName.EndsWith("yd", StringComparison.Ordinal) ? DistanceUnit.Yards : DistanceUnit.Metres;
        double halfUnits = z.GetProperty("half_units").GetDouble();

        string unitLabel = unit == GridUnit.Moa ? "MOA" : "mil";
        string description = string.Create(CultureInfo.InvariantCulture,
            $"One aiming mark on a {halfUnits / divisions:0.##} {unitLabel} grid spanning plus or minus {halfUnits:0.0} {unitLabel} " +
            $"at 100 {(distanceUnit == DistanceUnit.Yards ? "yards" : "metres")}, with a six-field load block.")
            // NOTES-FROM-PLANNING.md entry 197 section 3: what a zeroing grid is for, and what it is not.
            + " For sighting in by eye at the bench: it prints at exact scale, so the correction is read straight off the grid after each shot. For a zero worked out from a group, and group figures, shoot a 5x5 sheet.";

        var definition = new TargetDefinition(
            1, 0, null, Names[name], description, "GroupLab built-in library", "CC0-1.0", Created, "dmm",
            new Page(size, width, height, Orientation.Portrait),
            Inks,
            [new RingSet("aim", [new Disc(127, "black"), new Disc(114, "paper"), new Disc(25, "black")])],
            [new Bull(cx, cy, "aim", null, true, null)],
            null,
            new Fiducials("field-ring-1", FiducialFamily.AprilTag36h11, 40, 10, "fid", null),
            Codes(width, height, dataBlockHeight, 4),
            Print,
            new DataBlock(Corners1.SafeMargin, height - Corners1.SafeMargin - dataBlockHeight, width - (2 * Corners1.SafeMargin),
                dataBlockHeight, DataBlockLayout.Fields3x2, FieldSet.Standard6, 210, "black", "text", 2, null),
            null,
            null,
            [new MeasurementGrid("zero", cx, cy, z.GetProperty("half").GetInt32(), divisions, majorEvery, unit, 100, distanceUnit,
                "black", "black", "black", 2, 3, 4, majorEvery, "text")],
            []);

        return Finish(name, name, definition);
    }

    internal static BuiltInTarget Finish(string name, string fileStem, TargetDefinition definition)
    {
        definition = FiducialDerivation.WithDerivedMarkers(definition);
        var encoded = GltdBinary.Encode(definition);
        if (encoded.Encoding is null)
        {
            throw new InvalidOperationException($"{name} does not encode: {string.Join("; ", encoded.Diagnostics)}");
        }

        return new BuiltInTarget(name, $"{fileStem}.gltd.json", definition with { Id = encoded.Encoding.DefinitionId });
    }

    internal static Codes Codes(int width, int height, int dataBlockHeight, int count) =>
        new(count, Projection.CodeVersion, EcLevel.H, 4, Projection.CodeQuietZone, CodePlacement.Corners1, true,
            Corners1.Positions(width, height, dataBlockHeight, count, 4));

    internal static List<Disc> Discs(int outer) =>
        Stacks.TryGetValue(outer, out var stack)
            ? [.. stack.Select((diameter, i) => new Disc(diameter, i % 2 == 0 ? "black" : "paper"))]
            : throw new InvalidOperationException($"No documented disc stack for a {outer} dmm ring.");

    private static string SheetDescription(int scoring, int pitch, int sighters, bool dataBlock, Tiling? tiling)
    {
        string pitchMm = (pitch / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
        if (tiling is not null)
        {
            return $"One tile of a {tiling.Cols} by {tiling.Rows} assembly: {scoring} scoring bulls per sheet on a {pitchMm} mm grid, " +
                $"{scoring * tiling.Cols * tiling.Rows} across the assembly.";
        }

        var parts = new List<string>();
        if (sighters > 0)
        {
            parts.Add($"a {sighters}-bull sighter row");
        }

        if (dataBlock)
        {
            parts.Add("a nine-field load block");
        }

        return $"{scoring} scoring bulls on a {pitchMm} mm grid" + (parts.Count > 0 ? " with " + string.Join(" and ", parts) : "") + ".";
    }

    private static List<int> Ints(JsonElement e, string name) => [.. e.GetProperty(name).EnumerateArray().Select(v => v.GetInt32())];
}
