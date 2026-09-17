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
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab holes ink-proximity [--local &lt;manifest&gt;] [-v]</c>, NOTES-FROM-PLANNING.md entry 77 section 4: whether oversized marks, split holes and
/// spurious detections are one defect. Every detection render-and-difference makes on the corpus is placed by its centre's distance to the
/// nearest printed edge, and the three rates are read against that distance. If all three rise together as the distance goes to zero, they
/// are one defect in the difference stage.
/// <list type="bullet">
/// <item><b>Ink inside the footprint</b>, NOTES-FROM-PLANNING.md entry 86 section 3: how much of the expected artwork lies inside each
/// detection's own hull, which separates a hole centred on a ring from one beside it where a distance cannot.</item>
/// <item><b>Printed edges</b> are of two kinds, measured apart. <b>Artwork</b> is what the expected image draws: rings, dots, markers, codes
/// and rules, whose edges are the ink-to-paper transitions of <see cref="SceneRasterizer"/>'s render at 1 px per dmm. <b>Text</b> is what
/// the expected image does not draw, bull numbers, the identifier and the print note, measured as the distance to each run's glyph box
/// from <see cref="HelveticaMetrics"/>, cap height 0.72 and descender 0.21 of the size, zero inside it.</item>
/// <item><b>Truth.</b> The Phase 0 sample set has no holes, so each of its detections is spurious. The 300 DPI letter scans punched as
/// <see cref="CorpusCounts"/> punches them have known holes on real old print. A local manifest may list real sheets with their holes by hand,
/// <c>"truth": [[x, y], ...]</c> in page inches, and <c>"printNote": true</c> where the sheet carries the actual-size sentence.</item>
/// <item><b>Outcomes, per detection.</b> <b>Spurious:</b> no true hole within 0.15 in. <b>Split:</b> its nearest true hole within 0.15 in is
/// also the nearest of another detection. <b>Oversized:</b> its diameter over what the image's own clear detections give, 1.25 times or
/// more. For a punched hole that is the measured diameter over the hole's drawn outer diameter, normalised by the median of that ratio among
/// detections 0.2 in or more from any printed edge; for a real hole it is the diameter over the median of those clear detections. The
/// detector's own oversize flag is reported beside it. A split half has no size of its own, because it reports its whole blob's diameter, so
/// it is not given one; one row of the record is one detection, and for a split half its diameter is the blob's. So is the marking screen's size check, which is what entry 73 section 2 saw: the
/// apparent extent of the dark region under the detection, <see cref="HoleSize"/>'s measurement, over the largest a single hole can read,
/// a .308 hole plus <see cref="HoleSize.AllowanceInches"/> on the real sheets as the screen has it, and on the punched scans the extent over
/// the hole's drawn diameter, normalised by clear holes and flagged at 1.25 as the hull diameter is, because the size check does not read a
/// drawn hole at its drawn diameter.</item>
/// </list>
/// </summary>
public static class InkProximity
{
    private const double DmmPerInch = 254, Loose = 0.15, Clear = 0.2, OversizeRatio = 1.25;

    /// <summary>The distance bins, inches, from the centre to the nearest printed edge.</summary>
    public static IReadOnlyList<double> BinEdges { get; } = [0, 0.02, 0.05, 0.1, 0.2, double.PositiveInfinity];

    public sealed record Detection(string Image, string Corpus, double XIn, double YIn, double DiameterIn, double ArtworkIn, double TextIn, bool Spurious, bool Split, bool Oversized,
        bool OversizeFlag, bool PossibleMerge, double? SizeRatio, double? ApparentIn = null, bool? SizeCheckOversized = null, double InkInside = 0);

    /// <summary>The real sheets' calibre, .308, for the size check.</summary>
    public const double RealCalibreInches = 0.308;

    private sealed record Item(string Name, string Corpus, string File, TargetDefinition Definition, int Tile, bool PrintNote, IReadOnlyList<PointD>? TruthDmm, int? PunchOffset);

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, NewLine = "\n", PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int Run(string scans, string frozen, string phase1Scans, string? localManifest, bool verbose, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var definitions = new Dictionary<string, TargetDefinition>();
        TargetDefinition Load(string path) => definitions.TryGetValue(path, out var d) ? d : definitions[path] = GltdJsonReader.ReadFile(path).Definition!;

        var committed = SampleSet.All.Select(s => new Item(s.File, s.Kind == SampleSet.SampleKind.Scan ? "clean scans" : "clean photographs", Path.Combine(scans, s.File),
                Load(Path.Combine(frozen, s.Definition)), s.Tile, false, [], null))
            .Concat(SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Definition != SampleSet.Tile && s.Dpi == 300)
                .SelectMany(s => CorpusCounts.PunchOffsetsDmm.Select(o => new Item($"{s.File}, punched from {o} dmm", "punched scans", Path.Combine(scans, s.File),
                    Load(Path.Combine(frozen, s.Definition)), s.Tile, false, null, o))))
            .ToList();
        var committedRows = Measure(committed, verbose, output);
        RawMeasurements.Write(phase1Scans, "ink-proximity", committedRows);

        var rows = new List<Detection>(committedRows);
        if (localManifest is not null)
        {
            var manifest = JsonNode.Parse(File.ReadAllText(localManifest))!;
            var local = new List<Item>();
            foreach (var i in manifest["images"]!.AsArray())
            {
                if (i!["truth"] is not JsonArray truth)
                {
                    continue;
                }

                string target = (string?)i["target"] ?? Path.Combine("targets", (string)i["definition"]!);
                local.Add(new Item((string)i["name"]!, (string?)i["corpus"] ?? "real sheets", (string)i["file"]!, Load(target), 0, (bool?)i["printNote"] ?? false,
                    [.. truth.Select(p => new PointD((double)p![0]! * DmmPerInch, (double)p[1]! * DmmPerInch))], null));
            }

            var localRows = Measure(local, verbose, output);
            string record = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(localManifest))!, Path.GetFileNameWithoutExtension(localManifest) + ".ink-proximity.json");
            File.WriteAllText(record, JsonSerializer.Serialize(localRows, Options) + "\n");
            rows.AddRange(localRows);
        }

        foreach (var corpus in rows.Select(r => r.Corpus).Distinct())
        {
            Table(output, corpus, [.. rows.Where(r => r.Corpus == corpus)]);
        }

        Table(output, "everything with truth, clean sheets included", rows);
        Table(output, "real sheets and punched scans, nearest edge text", [.. rows.Where(r => r.Corpus != "clean scans" && r.Corpus != "clean photographs" && r.TextIn <= r.ArtworkIn)]);
        Table(output, "real sheets and punched scans, nearest edge artwork", [.. rows.Where(r => r.Corpus != "clean scans" && r.Corpus != "clean photographs" && r.ArtworkIn < r.TextIn)]);
        return 0;
    }

    private static List<Detection> Measure(IReadOnlyList<Item> items, bool verbose, TextWriter output)
    {
        var results = new List<Detection>[items.Count];
        var notes = new string?[items.Count];
        Parallel.For(0, items.Count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, k =>
        {
            results[k] = MeasureOne(items[k], out string? note);
            notes[k] = note;
        });

        for (int k = 0; k < items.Count; k++)
        {
            output.WriteLine(notes[k] is { } failed
                ? $"{items[k].Name}: {failed}"
                : string.Create(CultureInfo.InvariantCulture, $"{items[k].Name}: {results[k].Count} detections, {results[k].Count(d => d.Spurious)} spurious, {results[k].Count(d => d.Split)} split, {results[k].Count(d => d.Oversized)} oversized"));
            if (verbose)
            {
                foreach (var d in results[k])
                {
                    output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"    ({d.XIn:0.000}, {d.YIn:0.000}) in, {d.DiameterIn:0.000} in across, artwork {d.ArtworkIn:0.000} in, text {d.TextIn:0.000} in{(d.Spurious ? ", spurious" : "")}{(d.Split ? ", split" : "")}{(d.Oversized ? ", oversized" : "")}{(d.PossibleMerge ? ", split by the detector" : "")}{(d.OversizeFlag ? ", flagged oversized" : "")}{(d.ApparentIn is { } ap ? $", size check {ap:0.000} in" : "")}{(d.SizeCheckOversized == true ? " OVER" : "")}"));
                }
            }
        }

        output.WriteLine();
        return [.. results.SelectMany(r => r)];
    }

    private static List<Detection> MeasureOne(Item item, out string? failure)
    {
        AutomaticResult result;
        IReadOnlyList<PointD> truth;
        IReadOnlyList<double>? truthDiameters = null;
        GrayImage value;
        if (item.PunchOffset is { } offset)
        {
            var run = CorpusCounts.RunPunched(item.File, item.Definition, offset, out failure);
            if (run is null || failure is not null)
            {
                return [];
            }

            result = run.Result;
            value = run.Value;
            truth = [.. run.Holes.Select(h => new PointD(h.X, h.Y))];
            truthDiameters = [.. run.Holes.Select(h => 2 * ((h.RimRadius * (1 + h.LobeAmplitudes.Average())) + (h.RimWidth / 2) + h.ZoneLength) / DmmPerInch)];
        }
        else
        {
            var (grey, metadata) = ImageLoader.Load(item.File);
            (value, _) = ImageLoader.LoadMaxChannel(item.File);
            result = AutomaticMarking.Run(grey, value, metadata, item.Definition, new OpenCvSharpBackend(), new TraceRecorder());
            failure = result.Failure;
            if (failure is not null)
            {
                return [];
            }

            truth = item.TruthDmm ?? [];
        }

        if (result.Scale is not { } sheet || result.Difference is not { } difference)
        {
            failure = "no hole detection ran";
            return [];
        }

        var (artwork, text) = DistanceMaps(item.Definition, item.Tile, item.PrintNote);
        var pages = difference.Holes.Select(h => sheet.Mapping.ToPage(new PointD(h.X, h.Y))).ToList();
        int[] nearest = [.. pages.Select(p =>
        {
            int best = -1;
            double bestDistance = Loose * DmmPerInch;
            for (int t = 0; t < truth.Count; t++)
            {
                double d = Math.Sqrt(Math.Pow(p.X - truth[t].X, 2) + Math.Pow(p.Y - truth[t].Y, 2));
                if (d <= bestDistance)
                {
                    (best, bestDistance) = (t, d);
                }
            }

            return best;
        })];

        double Lookup(Mat map, PointD p)
        {
            int x = Math.Clamp((int)Math.Round(p.X), 0, map.Width - 1), y = Math.Clamp((int)Math.Round(p.Y), 0, map.Height - 1);
            return map.At<float>(y, x) / DmmPerInch;
        }

        var edges = pages.Select(p => (Artwork: Lookup(artwork, p), Text: Lookup(text, p))).ToList();
        artwork.Dispose();
        text.Dispose();

        // Size relative to what this image's own clear, matched, unsplit detections give.
        bool[] split = [.. nearest.Select((t, i) => t >= 0 && nearest.Where((u, j) => j != i && u == t).Any())];
        double[] raw = [.. difference.Holes.Select((h, i) => nearest[i] >= 0 && truthDiameters is not null ? h.DiameterInches / truthDiameters[nearest[i]] : h.DiameterInches)];
        var clear = Enumerable.Range(0, raw.Length).Where(i => nearest[i] >= 0 && !split[i] && Math.Min(edges[i].Artwork, edges[i].Text) >= Clear).Select(i => raw[i]).Order().ToList();
        double? reference = clear.Count >= 3 ? clear[clear.Count / 2] : null;

        // The marking screen's size check: the apparent extent of the dark region under each matched detection.
        double?[] apparent = [.. difference.Holes.Select((h, i) =>
        {
            if (nearest[i] < 0)
            {
                return (double?)null;
            }

            double expected = truthDiameters is not null ? truthDiameters[nearest[i]] : RealCalibreInches;
            double ppi = HoleSize.PixelsPerInch(sheet, new PointD(h.X, h.Y));
            return HoleSize.ApparentExtentPixels(value, new PointD(h.X, h.Y), HoleSize.CheckRadiusInDiameters * expected * ppi) / ppi;
        })];

        // A punched hole's drawn diameter is not what the size check reads on a clean hole, so there the extent is taken over the drawn
        // diameter and normalised by clear holes, as the hull diameter is; a real sheet has the screen's own rule.
        double? apparentReference = null;
        if (truthDiameters is not null)
        {
            var clearApparent = Enumerable.Range(0, raw.Length)
                .Where(i => apparent[i] is not null && !split[i] && Math.Min(edges[i].Artwork, edges[i].Text) >= Clear)
                .Select(i => apparent[i]!.Value / truthDiameters[nearest[i]]).Order().ToList();
            apparentReference = clearApparent.Count >= 3 ? clearApparent[clearApparent.Count / 2] : null;
        }

        bool? SizeCheck(int i) => apparent[i] is not { } read ? null
            : truthDiameters is null ? read > RealCalibreInches + HoleSize.AllowanceInches
            : apparentReference is { } a ? read / truthDiameters[nearest[i]] / a >= OversizeRatio
            : null;

        failure = null;
        return [.. difference.Holes.Select((h, i) =>
        {
            // A split half reports its whole blob's diameter, so it has no size of its own to compare (NOTES-FROM-PLANNING.md entry 80 section 4).
            double? ratio = nearest[i] >= 0 && !h.PossibleMerge && reference is { } r ? raw[i] / r : null;
            return new Detection(item.Name, item.Corpus, RawMeasurements.R(pages[i].X / DmmPerInch), RawMeasurements.R(pages[i].Y / DmmPerInch), RawMeasurements.R(h.DiameterInches),
                RawMeasurements.R(edges[i].Artwork), RawMeasurements.R(edges[i].Text), nearest[i] < 0, split[i], ratio >= OversizeRatio, h.Oversized, h.PossibleMerge,
                ratio is { } v ? RawMeasurements.R(v) : null, apparent[i] is { } a ? RawMeasurements.R(a) : null, SizeCheck(i), RawMeasurements.R(h.InkFraction));
        })];
    }

    /// <summary>Distance in dmm from every page point to the nearest artwork edge and to the nearest text glyph box, at 1 px per dmm.</summary>
    private static (Mat Artwork, Mat Text) DistanceMaps(TargetDefinition definition, int tile, bool printNote)
    {
        var scenes = SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true, PrintNote: printNote ? SceneBuilder.ActualSizeNote : null));
        var scene = scenes.Pages.FirstOrDefault(p => p.TileIndex == tile) ?? scenes.Pages[0];
        var render = SceneRasterizer.Rasterize(scene, DmmPerInch);
        int width = render.Width, height = render.Height;

        using var ink = new Mat(height, width, MatType.CV_8UC1);
        ink.SetArray([.. render.Pixels.Select(v => v < 128 ? (byte)255 : (byte)0)]);

        // An edge pixel is ink with paper beside it: the ink less its erosion.
        using var eroded = new Mat();
        Cv2.Erode(ink, eroded, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));
        using var edge = new Mat();
        Cv2.Subtract(ink, eroded, edge);
        using var notEdge = new Mat();
        Cv2.BitwiseNot(edge, notEdge);
        var artwork = new Mat();
        Cv2.DistanceTransform(notEdge, artwork, DistanceTypes.L2, DistanceTransformMasks.Precise);

        using var notText = new Mat(height, width, MatType.CV_8UC1, Scalar.All(255));
        foreach (var run in scene.Items.OfType<TextRun>())
        {
            double size = run.FontSize / 2.0, length = HelveticaMetrics.TextWidth(run.Text, run.FontSize) / 2.0, x = run.X / 2.0, baseline = run.Baseline / 2.0;
            double left = run.Anchor switch { TextAnchor.Centre => x - (length / 2), TextAnchor.Right => x - length, _ => x };
            Cv2.Rectangle(notText, new Rect((int)Math.Floor(left), (int)Math.Floor(baseline - (0.72 * size)), (int)Math.Ceiling(length), (int)Math.Ceiling(0.93 * size)), Scalar.All(0), -1);
        }

        var text = new Mat();
        Cv2.DistanceTransform(notText, text, DistanceTypes.L2, DistanceTransformMasks.Precise);
        return (artwork, text);
    }

    private static void Table(TextWriter output, string title, IReadOnlyList<Detection> rows)
    {
        var inv = CultureInfo.InvariantCulture;
        output.WriteLine($"{title}: {rows.Count} detections, by the centre's distance to the nearest printed edge");
        output.WriteLine("  distance (in)     detections  spurious  split  oversized  flagged oversized  size check oversized");
        for (int b = 0; b + 1 < BinEdges.Count; b++)
        {
            var bin = rows.Where(r => Math.Min(r.ArtworkIn, r.TextIn) >= BinEdges[b] && Math.Min(r.ArtworkIn, r.TextIn) < BinEdges[b + 1]).ToList();
            var sized = bin.Where(r => r.SizeRatio is not null).ToList();
            string Rate(int count, int of) => of == 0 ? "-" : string.Create(inv, $"{100.0 * count / of:0}%");
            string range = double.IsPositiveInfinity(BinEdges[b + 1]) ? string.Create(inv, $"{BinEdges[b]:0.00} and over") : string.Create(inv, $"{BinEdges[b]:0.00} to {BinEdges[b + 1]:0.00}");
            output.WriteLine(string.Create(inv,
                $"  {range,-16}  {bin.Count,10}  {Rate(bin.Count(r => r.Spurious), bin.Count),8}  {Rate(bin.Count(r => r.Split), bin.Count),5}  {Rate(sized.Count(r => r.Oversized), sized.Count),9}  {Rate(bin.Count(r => r.OversizeFlag), bin.Count),17}  {Rate(bin.Count(r => r.SizeCheckOversized == true), bin.Count(r => r.SizeCheckOversized is not null)),20}"));
        }

        output.WriteLine();
    }
}
