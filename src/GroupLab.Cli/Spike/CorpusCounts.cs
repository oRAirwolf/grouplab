using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab corpus counts [--local &lt;manifest&gt;] [--write]</c>, NOTES-FROM-PLANNING.md entry 77 section 3 item 2: every corpus image through the
/// automatic path, with what render-and-difference found on each, compared with the counts recorded before. Entry 76's caption change took a
/// real hole from 15 detections to 13 and was only caught because that comparison happened to be run. This makes it a standing check: the record
/// carries the fingerprint of every definition's printed artwork, a test fails when the renderer's artwork no longer matches it, and the only way
/// to make that test pass is to run this command, which prints the counts before and after.
/// <list type="bullet">
/// <item><b>The committed corpus</b> is the Phase 0 sample set, sheets printed before any change since, recorded in
/// <c>scans/phase1/measurements/detection-counts.json</c>. It has no holes, so every detection on it is spurious. Its scans of the
/// letter sheets at 300 DPI are also run punched: <see cref="PunchPitchDmm"/> apart over the whole page, printed matter included, synthetic
/// holes are punched through each scan's own registration, so a change that widens an exclusion zone or adds ink moves a count on sheets
/// already printed. Each scan is punched three times with the grid shifted by a third of its pitch, which puts a row of holes every 50 dmm
/// across the three without crowding any one of them, so no printed line can fall between the rows. Unpunched, the committed corpus
/// could not see entry 76's caption change; punched once on a single grid it still could not, because no row crossed the caption.</item>
/// <item><b>A local corpus</b> is a manifest of images that cannot be committed, such as real shot sheets without a consent record:
/// <c>{"images": [{"name": ..., "file": ..., "target": ...}]}</c>, with <c>target</c> optional. Its record is written beside the manifest,
/// never into the repository, and names each image by its <c>name</c> rather than its path.</item>
/// </list>
/// Without <c>--write</c> the command exits 1 when anything differs from the record, so a change is reviewed before it is recorded.
/// </summary>
public static class CorpusCounts
{
    public const string CommittedRecord = "scans/phase1/measurements/detection-counts.json";

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, NewLine = "\n", PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public sealed record Row(string Name, string Definition, int Holes, int Rejected, int InsideZones, int SplitHalves, int Oversized, string? Failure);

    /// <summary>The spacing of the punched holes, page dmm, about 0.6 in, wide enough that neighbours never merge.</summary>
    public const int PunchPitchDmm = 150;

    /// <summary>Where the punched grids start, page dmm, across and down: a third of the pitch apart.</summary>
    public static IReadOnlyList<int> PunchOffsetsDmm { get; } = [25, 75, 125];

    private sealed record Item(string Name, string File, string? Target, int? PunchOffset = null);

    private sealed record Record(string Measurement, string Command, IReadOnlyDictionary<string, string> Artwork, IReadOnlyList<Row> Rows);

    public static int Run(string scans, string frozen, string targets, string? localManifest, bool write, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var artwork = ArtworkFingerprint.All(targets);
        var committed = SampleSet.All.Select(s => new Item(s.File, Path.Combine(scans, s.File), Path.Combine(frozen, s.Definition)))
            .Concat(SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Definition != SampleSet.Tile && s.Dpi == 300)
                .SelectMany(s => PunchOffsetsDmm.Select(o => new Item($"{s.File}, punched from {o} dmm", Path.Combine(scans, s.File), Path.Combine(frozen, s.Definition), o))))
            .ToList();
        bool differs = Compare("committed corpus", CommittedRecord, committed, artwork, write, output);
        if (localManifest is not null)
        {
            var manifest = JsonNode.Parse(File.ReadAllText(localManifest))!;
            var local = manifest["images"]!.AsArray().Select(i => new Item((string)i!["name"]!, (string)i["file"]!, (string?)i["target"])).ToList();
            string record = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(localManifest))!, Path.GetFileNameWithoutExtension(localManifest) + ".counts.json");
            differs |= Compare("local corpus", record, local, artwork, write, output);
        }

        if (differs && !write)
        {
            output.WriteLine("Something differs from the record. Review it, and record it with --write once the change is understood.");
            return 1;
        }

        return 0;
    }

    /// <summary>The artwork fingerprints a record was made against, for the test that makes the check standing.</summary>
    public static IReadOnlyDictionary<string, string> RecordedArtwork(string record)
    {
        var node = JsonNode.Parse(File.ReadAllText(record))!;
        return node["artwork"]!.AsObject().ToDictionary(p => p.Key, p => (string)p.Value!, StringComparer.Ordinal);
    }

    private static bool Compare(string title, string recordPath, IReadOnlyList<Item> items, IReadOnlyDictionary<string, string> artwork, bool write, TextWriter output)
    {
        var inv = CultureInfo.InvariantCulture;
        var rows = new Row[items.Count];
        Parallel.For(0, items.Count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, k => rows[k] = Count(items[k]));

        Dictionary<string, Row> before = [];
        Dictionary<string, string> beforeArtwork = [];
        if (File.Exists(recordPath))
        {
            var old = JsonSerializer.Deserialize<Record>(File.ReadAllText(recordPath), Options)!;
            before = old.Rows.ToDictionary(r => r.Name);
            beforeArtwork = old.Artwork.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        }

        output.WriteLine($"{title}: {items.Count} images");
        output.WriteLine("image                                                 holes  rejected  in zones  split  oversized");
        bool differs = false;
        foreach (var row in rows)
        {
            string Cell(Func<Row, int> f, int width) => before.TryGetValue(row.Name, out var b) && f(b) != f(row) ? $"{f(b)}->{f(row)}".PadLeft(width) : f(row).ToString(inv).PadLeft(width);
            bool changed = !before.TryGetValue(row.Name, out var was) || was != row;
            differs |= changed;
            output.WriteLine(row.Failure is not null
                ? $"{row.Name,-52}  failed: {row.Failure}{(was is null ? "   NEW" : changed ? "   CHANGED" : "")}"
                : $"{row.Name,-52}  {Cell(r => r.Holes, 5)}  {Cell(r => r.Rejected, 8)}  {Cell(r => r.InsideZones, 8)}  {Cell(r => r.SplitHalves, 5)}  {Cell(r => r.Oversized, 9)}{(was is null ? "   NEW" : changed ? "   CHANGED" : "")}{(was is not null && was.Definition != row.Definition ? $" (definition was {was.Definition})" : "")}");
        }

        int unchanged = rows.Count(r => before.TryGetValue(r.Name, out var b) && b == r);
        output.WriteLine($"{unchanged} of {rows.Length} images unchanged from the record");
        var moved = artwork.Where(p => !beforeArtwork.TryGetValue(p.Key, out string? h) || h != p.Value).Select(p => p.Key)
            .Concat(beforeArtwork.Keys.Where(k => !artwork.ContainsKey(k))).Order(StringComparer.Ordinal).ToList();
        if (moved.Count > 0)
        {
            differs = true;
            output.WriteLine($"printed artwork differs from the record for {moved.Count} definition{(moved.Count == 1 ? "" : "s")}: {string.Join(", ", moved)}");
        }

        if (write)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(recordPath))!);
            var record = new Record("detection-counts", "grouplab corpus counts --write", artwork, rows);
            File.WriteAllText(recordPath, JsonSerializer.Serialize(record, Options) + "\n");
            output.WriteLine($"recorded in {(Path.IsPathRooted(recordPath) ? "the local record beside the manifest" : recordPath)}");
        }

        output.WriteLine();
        return differs;
    }

    private static Row Count(Item item)
    {
        string definition = item.Target is null ? "named by its codes" : Path.GetFileName(item.Target);
        if (item.PunchOffset is { } offset)
        {
            return Punched(item, definition, offset);
        }

        var result = GroupLab.Cli.AnalyzeVerb.Analyze(item.File, item.Target, out string? failure);
        var holes = result?.Trace.LastOrDefault(r => r.Stage == "S5-S8.holes");
        if (holes is null)
        {
            return new Row(item.Name, definition, 0, 0, 0, 0, 0, failure ?? result?.Failure ?? "no hole detection ran");
        }

        return FromStage(item.Name, definition, holes);
    }

    private static Row FromStage(string name, string definition, StageRecord holes)
    {
        int Metric(string metric) => (int)(holes.Metrics.FirstOrDefault(m => m.Name == metric)?.Value ?? 0);
        return new Row(name, definition, Metric("holes"), Metric("rejected"), Metric("inside exclusion zones"), Metric("split halves"), Metric("oversized"), null);
    }

    /// <summary>The scan registered as it is, punched through that registration on a grid over the whole page, and run again.</summary>
    private static Row Punched(Item item, string definitionName, int offset)
    {
        var definition = GltdJsonReader.ReadFile(item.Target!).Definition!;
        var run = RunPunched(item.File, definition, offset, out string? failure);
        var stage = run?.Trace.Records.LastOrDefault(r => r.Stage == "S5-S8.holes");
        return stage is null
            ? new Row(item.Name, definitionName, 0, 0, 0, 0, 0, failure ?? "no hole detection ran")
            : FromStage(item.Name, definitionName, stage) with { Definition = $"{definitionName}, {run!.Holes.Count} holes punched" };
    }

    /// <summary>A punched run: what the automatic path found, the holes punched, page dmm, and its trace.</summary>
    public sealed record PunchedRun(AutomaticResult Result, IReadOnlyList<SyntheticHole> Holes, TraceRecorder Trace, GrayImage Value);

    /// <summary>
    /// <paramref name="file"/> registered as it is, punched with synthetic holes <see cref="PunchPitchDmm"/> apart from <paramref name="offset"/>
    /// dmm across and down, and run through the automatic path again. Null with the reason when the clean image does not register.
    /// </summary>
    public static PunchedRun? RunPunched(string file, TargetDefinition definition, int offset, out string? failure)
    {
        ArgumentNullException.ThrowIfNull(definition);
        failure = null;
        var (grey, metadata) = ImageLoader.Load(file);
        var (value, _) = ImageLoader.LoadMaxChannel(file);
        var backend = new OpenCvSharpBackend();
        var clean = AutomaticMarking.Run(grey, value, metadata, definition, backend, new TraceRecorder());
        if (clean.Scale is not { } sheet || clean.Difference is not { } difference)
        {
            failure = clean.Failure ?? "the clean scan did not register";
            return null;
        }

        var random = new Random(77 + offset);
        var holes = new List<SyntheticHole>();
        for (int y = offset; y < definition.Page.Height; y += PunchPitchDmm)
        {
            for (int x = offset; x < definition.Page.Width; x += PunchPitchDmm)
            {
                holes.Add(SyntheticSheet.SampleHole(random, x, y, onInk: false, HoleBacking.ScannerLid));
            }
        }

        var punchedGrey = SyntheticSheet.Punch(grey, sheet.Mapping, difference.Dpi, holes);
        var punchedValue = SyntheticSheet.Punch(value, sheet.Mapping, difference.Dpi, holes);
        var trace = new TraceRecorder();
        var result = AutomaticMarking.Run(punchedGrey, punchedValue, metadata, definition, backend, trace);
        failure = result.Failure;
        return new PunchedRun(result, holes, trace, punchedValue);
    }
}
