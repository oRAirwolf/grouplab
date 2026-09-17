using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab holes split-calibration [--local &lt;manifest&gt;]</c>, NOTES-FROM-PLANNING.md entry 78 sections 3 and 4: stage S8's split decision
/// measured per blob, so its two failures are calibrated together. A blob that holds one hole and is split is a hole counted twice, the
/// failure that put the friend's sigma at 0.607 in against 0.390 in. A blob that holds two holes and is not split is two holes counted as
/// one oversized mark. Moving the elongation threshold trades one for the other; the calibre, where one is named, can veto a split of a
/// blob too small to be two holes, and never makes one.
/// <list type="bullet">
/// <item><b>Corpus.</b> The Phase 0 letter scans at 300 DPI punched once with single holes and once with overlapping pairs, from 75 dmm,
/// and the local manifest's real sheets with their hand-verified holes, at .308 times what a hole of it measures on a scan or in a
/// photograph (<see cref="AutomaticMarking.ScanHoleToCalibre"/>, NOTES-FROM-PLANNING.md entry 79 section 1).</item>
/// <item><b>A blob</b> is the detections sharing one hull. It holds the true holes within its own radius of the hull's centre, and never
/// less than 0.15 in. True holes no blob holds are missed; detections in a blob that holds none are spurious.</item>
/// <item><b>The synthetic calibre.</b> The punched holes are not one calibre, so theirs is the median diameter of the whole, single-hole
/// blobs at the shipped settings, which is what a single hole of that synthesis measures.</item>
/// </list>
/// </summary>
public static class SplitCalibration
{
    private const double DmmPerInch = 254, Loose = 0.15;

    public static IReadOnlyList<double> Elongations { get; } = [1.3, 1.45, 1.6, 1.8, 2.0];

    public static IReadOnlyList<double?> MinimumHoles { get; } = [null, 1.2, 1.4, 1.6, 1.8, 2.0];

    private sealed record Case(string Name, string Corpus, GrayImage Value, TargetDefinition Definition, IPageMapping Mapping, double Dpi, IReadOnlyList<PointD> TruthDmm, double? Calibre);

    private sealed record Tally(int OneSplit, int OneWhole, int TwoSplit, int TwoWhole, int MoreThanTwo, int Spurious, int Missed, int Detections, int Vetoed)
    {
        public static Tally operator +(Tally a, Tally b) => new(a.OneSplit + b.OneSplit, a.OneWhole + b.OneWhole, a.TwoSplit + b.TwoSplit, a.TwoWhole + b.TwoWhole,
            a.MoreThanTwo + b.MoreThanTwo, a.Spurious + b.Spurious, a.Missed + b.Missed, a.Detections + b.Detections, a.Vetoed + b.Vetoed);

        public static Tally Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    public static int Run(string scans, string frozen, string? localManifest, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var definition = GltdJsonReader.ReadFile(Path.Combine(frozen, SampleSet.CentreFire)).Definition!;
        var loadBlock = GltdJsonReader.ReadFile(Path.Combine(frozen, SampleSet.LoadBlock)).Definition!;
        var synthetic = new List<Case>();
        foreach (var sample in SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Definition != SampleSet.Tile && s.Dpi == 300))
        {
            var d = sample.Definition == SampleSet.LoadBlock ? loadBlock : definition;
            foreach (bool pairs in new[] { false, true })
            {
                var run = CorpusCounts.RunPunched(Path.Combine(scans, sample.File), d, 75, out string? failure, pairs);
                if (run?.Result.Scale is not { } sheet || run.Result.Difference is not { } difference)
                {
                    output.WriteLine($"{sample.File}: {failure}");
                    continue;
                }

                synthetic.Add(new Case($"{sample.File}, {(pairs ? "pairs" : "singles")}", pairs ? "punched pairs" : "punched singles", run.Value, d, sheet.Mapping, difference.Dpi,
                    [.. run.Holes.Select(h => new PointD(h.X, h.Y))], null));
            }
        }

        var real = new List<Case>();
        if (localManifest is not null)
        {
            foreach (var i in JsonNode.Parse(File.ReadAllText(localManifest))!["images"]!.AsArray())
            {
                if (i!["truth"] is not JsonArray truth)
                {
                    continue;
                }

                var d = GltdJsonReader.ReadFile((string)i["target"]!).Definition!;
                var (grey, metadata) = ImageLoader.Load((string)i["file"]!);
                var (value, _) = ImageLoader.LoadMaxChannel((string)i["file"]!);
                var clean = AutomaticMarking.Run(grey, value, metadata, d, new OpenCvSharpBackend(), new TraceRecorder());
                if (clean.Scale is not { } sheet || clean.Difference is not { } difference)
                {
                    output.WriteLine($"{(string)i["name"]!}: {clean.Failure}");
                    continue;
                }

                real.Add(new Case((string)i["name"]!, "real sheets", value, d, sheet.Mapping, difference.Dpi,
                    [.. truth.Select(p => new PointD((double)p![0]! * DmmPerInch, (double)p[1]! * DmmPerInch))],
                    InkProximity.RealCalibreInches * (metadata.IsCamera ? AutomaticMarking.PhotographHoleToCalibre : AutomaticMarking.ScanHoleToCalibre)));
            }
        }

        // The synthesis's own single-hole size, from the shipped settings.
        var shipped = new RenderDifferenceOptions();
        var singleDiameters = synthetic.SelectMany(c => Blobs(c, Detect(c, shipped)).Where(b => b.Truths == 1 && !b.Split).Select(b => b.Diameter)).Order().ToList();
        double syntheticCalibre = singleDiameters[singleDiameters.Count / 2];
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"synthetic calibre, the median whole single-hole blob: {syntheticCalibre:0.000} in over {singleDiameters.Count} blobs"));
        output.WriteLine();
        synthetic = [.. synthetic.Select(c => c with { Calibre = syntheticCalibre })];

        var cases = synthetic.Concat(real).ToList();
        var configs = Elongations.SelectMany(e => MinimumHoles.Select(m => (Elongation: e, MinimumHoles: m))).ToList();
        var tallies = new Tally[cases.Count, configs.Count];
        Parallel.For(0, cases.Count * configs.Count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, k =>
        {
            var c = cases[k / configs.Count];
            var (e, m) = configs[k % configs.Count];
            var options = shipped with { SplitElongation = e, CalibreInches = m is null ? null : c.Calibre, SplitMinimumHoles = m ?? shipped.SplitMinimumHoles };
            tallies[k / configs.Count, k % configs.Count] = Score(c, Detect(c, options));
        });

        foreach (var corpus in cases.Select(c => c.Corpus).Distinct())
        {
            output.WriteLine($"{corpus}: {cases.Count(c => c.Corpus == corpus)} images");
            output.WriteLine("  elongation  calibre veto   one hole split  two holes whole  one whole  two split  >2   spurious  missed  detections  vetoed");
            for (int j = 0; j < configs.Count; j++)
            {
                var t = Enumerable.Range(0, cases.Count).Where(i => cases[i].Corpus == corpus).Aggregate(Tally.Zero, (a, i) => a + tallies[i, j]);
                var (e, m) = configs[j];
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  {e,10:0.00}  {(m is { } v ? $"under {v:0.0} holes" : "none"),-13}  {t.OneSplit,14}  {t.TwoWhole,15}  {t.OneWhole,9}  {t.TwoSplit,9}  {t.MoreThanTwo,3}  {t.Spurious,8}  {t.Missed,6}  {t.Detections,10}  {t.Vetoed,6}"));
            }

            output.WriteLine();
        }

        foreach (var c in real)
        {
            var t = Score(c, Detect(c, shipped with { CalibreInches = c.Calibre }));
            var s = Score(c, Detect(c, shipped));
            output.WriteLine($"{c.Name}: one hole split {s.OneSplit} -> {t.OneSplit}, two holes whole {s.TwoWhole} -> {t.TwoWhole}, spurious {s.Spurious} -> {t.Spurious}, missed {s.Missed} -> {t.Missed}, at the shipped settings without and with .308");
        }

        return 0;
    }

    private static RenderDifferenceResult Detect(Case c, RenderDifferenceOptions options) =>
        RenderDifferenceHoleDetector.Detect(c.Value, c.Definition, 0, c.Mapping, c.Dpi, new OpenCvSharpBackend(), options);

    private sealed record Blob(double Diameter, bool Split, int Truths, int Detections, bool Vetoed);

    private static List<Blob> Blobs(Case c, RenderDifferenceResult result, HashSet<int>? held = null) =>
        [.. result.Holes.GroupBy(h => (Math.Round(h.HullX, 3), Math.Round(h.HullY, 3))).Select(g =>
        {
            var first = g.First();
            var centre = c.Mapping.ToPage(new PointD(first.HullX, first.HullY));
            double reach = Math.Max(Loose, first.DiameterInches / 2) * DmmPerInch;
            var inside = Enumerable.Range(0, c.TruthDmm.Count).Where(t => Math.Sqrt(Math.Pow(c.TruthDmm[t].X - centre.X, 2) + Math.Pow(c.TruthDmm[t].Y - centre.Y, 2)) <= reach).ToList();
            held?.UnionWith(inside);
            return new Blob(first.DiameterInches, g.Count() > 1, inside.Count, g.Count(), first.SplitVetoed);
        })];

    private static Tally Score(Case c, RenderDifferenceResult result)
    {
        var held = new HashSet<int>();
        var blobs = Blobs(c, result, held);
        return new Tally(
            blobs.Count(b => b.Truths == 1 && b.Split),
            blobs.Count(b => b.Truths == 1 && !b.Split),
            blobs.Count(b => b.Truths == 2 && b.Split),
            blobs.Count(b => b.Truths == 2 && !b.Split),
            blobs.Count(b => b.Truths > 2),
            blobs.Where(b => b.Truths == 0).Sum(b => b.Detections),
            c.TruthDmm.Count - held.Count,
            result.Holes.Count,
            blobs.Count(b => b.Vetoed));
    }
}
