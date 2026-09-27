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

    /// <summary>The zeroing sheets' own date, since entry 226 redrew them.</summary>
    internal const string ZeroCreated = "2026-09-27";

    /// <summary>Where every zeroing grid sits and how far its drawn field reaches, dmm (entry 226 section 1, question 59).</summary>
    internal const int ZeroCentreY = 1412;

    internal const int ZeroField = 914;

    /// <summary>
    /// The zeroing sheets as redrawn by NOTES-FROM-PLANNING.md entries 226 and 227 section 1: grid style 2 (<see cref="GridStyle2"/>),
    /// a field of 914 dmm each side of the aiming point, which is exactly plus or minus 1.0 mil at 100 yd, fine squares of 0.25 mil or
    /// 0.5 MOA, the whole unit heaviest, every heavier line labeled, and the scale statement with a 4 in or 10 cm ruler above the grid.
    /// Two codes at the top rather than four in the corners leave the page's height to the grid. The page and name still come from
    /// <c>layouts.json</c>; the grid no longer does, and the sheets printed from the old one are frozen in <c>targets/frozen/zero-grid-1</c>.
    /// </summary>
    private static BuiltInTarget Zero(JsonElement z)
    {
        string name = z.GetProperty("name").GetString()!;
        var (size, width, height) = Pages[z.GetProperty("page").GetString()!];
        int dataBlockHeight = z.GetProperty("data_block").GetInt32();
        string unitName = z.GetProperty("unit").GetString()!;
        var unit = unitName.Contains("MOA", StringComparison.Ordinal) ? GridUnit.Moa : GridUnit.Mil;
        var distanceUnit = unitName.EndsWith("yd", StringComparison.Ordinal) ? DistanceUnit.Yards : DistanceUnit.Metres;
        int cx = width / 2;

        // The lattice: a whole number of fine squares whose half reaches at least the field, so every line is rounded from a stored
        // half (section 3.13) and the field cuts it. Mil: 0.25 mil squares over 1.25 mil, whose half is a whole number of dmm at 100 yd
        // (1143.0), so no line is more than half a dmm from its angle; over 1 mil, 914.4 rounds to 914 and the first line misses by 0.6.
        // MOA: 0.5 MOA squares over 3.5 MOA.
        var (halfUnits, divisions, majorEvery, wholeEvery) = unit == GridUnit.Mil ? (1.25, 5, 2, 4) : (3.5, 7, 2, 2);
        double unitDmm = (distanceUnit == DistanceUnit.Yards ? 9144.0 : 10000.0) * (unit == GridUnit.Mil ? 0.001 : Math.Tan(Math.PI / 180.0 / 60.0)) * 100;
        int half = (int)Math.Round(unitDmm * halfUnits);
        int field = Math.Min(half, ZeroField);
        var grid = new MeasurementGrid("zero", cx, ZeroCentreY, half, divisions, majorEvery, unit, 100, distanceUnit,
            "black", "black", "black", GridStyle2.FineStroke, GridStyle2.MajorStroke, GridStyle2.WholeStroke, majorEvery, "black",
            GridStyle2.Style, field, field, wholeEvery);

        string unitLabel = unit == GridUnit.Moa ? "MOA" : "mil";
        double reach = field / unitDmm;
        string description = string.Create(CultureInfo.InvariantCulture,
            $"One aiming ring on a grid of {1.0 / wholeEvery:0.##} {unitLabel} squares reaching {reach:0.0#} {unitLabel} each side of the aim " +
            $"at 100 {(distanceUnit == DistanceUnit.Yards ? "yards" : "meters")}, heavier every {(double)majorEvery / wholeEvery:0.##} {unitLabel} and " +
            $"heaviest every {unitLabel}, every heavier line labeled, with its scale printed on the sheet and a ruler to check the print, " +
            $"and a six-field load block.")
            // NOTES-FROM-PLANNING.md entry 197 section 3: what a zeroing grid is for, and what it is not.
            + " For sighting in by eye at the bench: it prints at exact scale, so the correction is read straight off the grid after each shot. For a zero worked out from a group, and group figures, shoot a 5x5 sheet.";

        var definition = new TargetDefinition(
            1, 0, null, Names[name], description, "GroupLab built-in library", "CC0-1.0", ZeroCreated, "dmm",
            new Page(size, width, height, Orientation.Portrait),
            Inks,
            [new RingSet("aim", [new Disc(200, "black"), new Disc(150, "paper")])],
            [new Bull(cx, ZeroCentreY, "aim", null, true, null)],
            null,
            new Fiducials("field-ring-1", FiducialFamily.AprilTag36h11, 40, 10, "fid", null),
            Codes(width, height, dataBlockHeight, 2),
            Print,
            new DataBlock(Corners1.SafeMargin, height - Corners1.SafeMargin - dataBlockHeight, width - (2 * Corners1.SafeMargin),
                dataBlockHeight, DataBlockLayout.Fields3x2, FieldSet.Standard6, 210, "black", "text", 2, null),
            null,
            null,
            [grid],
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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 243 section 1.1, answering question 61: the E bull, offered beside the current one and not in place of it.
    /// A black disc the size of the sheet's own bull, a white center of 0.36 in and a 0.10 in dot: design E of the aim point test of
    /// 2026-09-26 drawn as discs, the one design both shooters could center on through every high power scope at 10x. It becomes the default
    /// only once Alan has shot it and says so, so these sheets carry identifiers of their own and nothing already printed changes.
    /// </summary>
    public static IReadOnlyList<BuiltInTarget> Additions(string layoutsJsonPath)
    {
        var built = Build(layoutsJsonPath);
        var added = new List<BuiltInTarget>();
        foreach (var (stem, name) in EBullSheets)
        {
            var source = built.Single(t => t.FileName == stem + ".gltd.json").Definition;
            int outer = source.RingSets.Single().Discs.Max(d => d.Diameter);
            var definition = source with
            {
                Id = null,
                Name = name,
                Description = source.Description + " Each bull is a black disc with a 0.36 in white center and a small dot, the E bull of the aim point test.",
                Created = EBullCreated,
                RingSets = [new RingSet("e", EDiscs(outer))],
                Bulls = [.. source.Bulls.Select(b => b with { RingSet = "e" })],
            };
            added.Add(Finish(name, stem + "-E", definition));
        }

        // Entry 243 section 4: the C bull as tested, a black diamond standing on a point with a white diamond center and a dot, on the same
        // sheets and grid. Its points come within 31 dmm of the cell boundary, and the layout's own marker drop test decides what fits.
        foreach (var (stem, name) in CBullSheets)
        {
            var source = built.Single(t => t.FileName == stem + ".gltd.json").Definition;
            var definition = source with
            {
                Id = null,
                Name = name,
                Description = source.Description + " Each bull is a black diamond standing on a point, 1.25 in point to point, with a white diamond center and a small dot, the C bull of the aim point test.",
                Created = CBullCreated,
                RingSets = [new RingSet("c", CDiscs(CDiagonal))],
                Bulls = [.. source.Bulls.Select(b => b with { RingSet = "c" })],
            };
            added.Add(Finish(name, stem + "-C", definition));
        }

        return added;
    }

    /// <summary>The sheets drawn with the C bull, and their names.</summary>
    internal static readonly (string Stem, string Name)[] CBullSheets =
    [
        ("GL-CF25-LTR", "GroupLab 5x5 Load Development, C Bull, Letter"),
        ("GL-CF25-LTR-D", "GroupLab 5x5 Load Development with Load Block, C Bull, Letter"),
        ("GL-CF25-A4", "GroupLab 5x5 Load Development, C Bull, A4"),
    ];

    internal const string CBullCreated = "2026-09-27";

    /// <summary>The aim point card's C: 1.25 in point to point, 318 dmm.</summary>
    internal const int CDiagonal = 318;

    /// <summary>
    /// The C bull's stack at any size, entry 243 section 4, in the aim point card's proportions: a black diamond <paramref name="diagonal"/>
    /// point to point, a white diamond center 0.36/1.25 of it (the card's 3.47 to 1), and a round black dot 0.10/0.36 of the center, as E's.
    /// Both diamonds stand on a point. At the card's size that is 318, 92 and 25 dmm.
    /// </summary>
    public static List<Disc> CDiscs(int diagonal)
    {
        int centre = (int)Math.Round(diagonal * 0.36 / 1.25, MidpointRounding.AwayFromZero);
        int dot = (int)Math.Round(diagonal * 0.10 / 1.25, MidpointRounding.AwayFromZero);
        return [new Disc(diagonal, "black", DiscShape.Square, 45), new Disc(centre, "paper", DiscShape.Square, 45), new Disc(dot, "black")];
    }

    /// <summary>The sheets drawn with the E bull, and their names.</summary>
    internal static readonly (string Stem, string Name)[] EBullSheets =
    [
        ("GL-CF25-LTR", "GroupLab 5x5 Load Development, E Bull, Letter"),
        ("GL-CF25-LTR-D", "GroupLab 5x5 Load Development with Load Block, E Bull, Letter"),
        ("GL-CF25-A4", "GroupLab 5x5 Load Development, E Bull, A4"),
    ];

    internal const string EBullCreated = "2026-09-27";

    /// <summary>The E bull's stack: the disc, a 0.36 in white center (91 dmm) and a 0.10 in dot (25 dmm).</summary>
    public static List<Disc> EDiscs(int outer) => [new Disc(outer, "black"), new Disc(91, "paper"), new Disc(25, "black")];

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
