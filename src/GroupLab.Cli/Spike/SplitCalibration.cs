using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab holes split-calibration [--local &lt;manifest&gt;]</c>, NOTES-FROM-PLANNING.md entries 78 and 80: stage S8's split decision measured per
/// blob, so its two failures are calibrated together. A blob that holds one hole and is split is a hole counted twice, the failure that put
/// the friend's sigma at 0.607 in against 0.390 in. A blob that holds two holes and is not split is two holes counted as one oversized mark.
/// Moving the elongation threshold trades one for the other; the size a hole of the named calibre measures can veto a split of a blob too
/// small to be two holes, and never makes one.
/// <list type="bullet">
/// <item><b>The fit has two parts, and they are reported apart</b> (entry 80 section 2). Synthetic holes give coverage: the six Phase 0 letter
/// scans at 300 DPI, punched once with single holes and once with overlapping pairs. Real holes have the veto: the local manifest's sheets with
/// their hand-verified holes. A setting that misclassifies any real hole is not a candidate, whatever the synthetic tallies say, and the
/// report lists every setting that survives rather than one optimum.</item>
/// <item><b>The synthetic holes stand in for .308 on a scan.</b> The survey they are drawn from mixed .264, .308 and .338, and
/// render-and-difference reads them larger for their calibre than it reads real holes. Their lengths are scaled so the median whole single
/// hole reads .308 times <see cref="AutomaticMarking.ScanHoleToCalibre"/>, the factor found by interpolating two trial scales, and the named
/// size is that same figure.</item>
/// <item><b>Real sheets</b> are named .308 times the ratio for their kind, scan or photograph, as the automatic path names it.</item>
/// <item><b>A blob</b> is the detections sharing one hull. It holds the true holes within its own radius of the hull's centre, and never
/// less than 0.15 in. True holes no blob holds are missed; detections in a blob that holds none are spurious.</item>
/// </list>
/// </summary>
public static class SplitCalibration
{
    private const double DmmPerInch = 254, Loose = 0.15;

    public static IReadOnlyList<double> Elongations { get; } = [1.3, 1.45, 1.6, 1.8];

    public static IReadOnlyList<double?> MinimumHoles { get; } = [null, 1.2, 1.4, 1.6, 1.8];

    private sealed record Registered(string File, TargetDefinition Definition, GrayImage Value, IPageMapping Mapping, double Dpi, GrayImage Render);

    private sealed record Case(string Name, bool Real, string Corpus, GrayImage Value, TargetDefinition Definition, IPageMapping Mapping, double Dpi, GrayImage Render, IReadOnlyList<PointD> TruthDmm, double Size);

    private sealed record Tally(int OneSplit, int OneWhole, int TwoSplit, int TwoWhole, int MoreThanTwo, int Spurious, int Missed, int Detections, int Vetoed)
    {
        public static Tally operator +(Tally a, Tally b) => new(a.OneSplit + b.OneSplit, a.OneWhole + b.OneWhole, a.TwoSplit + b.TwoSplit, a.TwoWhole + b.TwoWhole,
            a.MoreThanTwo + b.MoreThanTwo, a.Spurious + b.Spurious, a.Missed + b.Missed, a.Detections + b.Detections, a.Vetoed + b.Vetoed);

        public static Tally Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

        public int Errors => OneSplit + TwoWhole;
    }

    public static int Run(string scans, string frozen, string? localManifest, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var inv = CultureInfo.InvariantCulture;
        void Say(string line)
        {
            output.WriteLine(line);
            output.Flush();
        }

        double target = InkProximity.RealCalibreInches * AutomaticMarking.ScanHoleToCalibre;
        var shipped = new RenderDifferenceOptions();

        // Register each scan once.
        var registered = new List<Registered>();
        foreach (var sample in SampleSet.All.Where(s => s.Kind == SampleSet.SampleKind.Scan && s.Definition != SampleSet.Tile && s.Dpi == 300))
        {
            var d = GltdJsonReader.ReadFile(Path.Combine(frozen, sample.Definition)).Definition!;
            if (Register(Path.Combine(scans, sample.File), d, out string? failure) is { } r)
            {
                registered.Add(r);
            }
            else
            {
                Say($"{sample.File}: {failure}");
            }
        }

        Say($"registered {registered.Count} scans");

        // The scale at which a synthetic single hole reads what a .308 hole reads on a scan.
        double Median(double scale)
        {
            var sizes = new List<double>();
            foreach (var r in registered)
            {
                var c = Punched(r, pairs: false, scale, target);
                sizes.AddRange(Blobs(c, Detect(c, shipped)).Where(b => b.Truths == 1 && !b.Split).Select(b => b.Diameter));
            }

            sizes.Sort();
            return sizes[sizes.Count / 2];
        }

        double atOne = Median(1), atLower = Median(0.8);
        double scale = 0.8 + ((target - atLower) * (1 - 0.8) / (atOne - atLower));
        double atScale = Median(scale);
        Say(string.Create(inv, $"synthetic single hole: {atOne:0.000} in at the survey's scale, {atOne / InkProximity.RealCalibreInches:0.000} of .308; {atLower:0.000} in at 0.80"));
        Say(string.Create(inv, $"scaled by {scale:0.000}: {atScale:0.000} in, {atScale / InkProximity.RealCalibreInches:0.000} of .308, against {AutomaticMarking.ScanHoleToCalibre:0.000} measured on real scans"));

        var cases = new List<Case>();
        foreach (var r in registered)
        {
            cases.Add(Punched(r, pairs: false, scale, target));
            cases.Add(Punched(r, pairs: true, scale, target));
        }

        if (localManifest is not null)
        {
            foreach (var i in JsonNode.Parse(File.ReadAllText(localManifest))!["images"]!.AsArray())
            {
                if (i!["truth"] is not JsonArray truth)
                {
                    continue;
                }

                var d = GltdJsonReader.ReadFile((string)i["target"]!).Definition!;
                string file = (string)i["file"]!;
                bool camera = ImageLoader.Load(file).Metadata.IsCamera;
                if (Register(file, d, out string? failure) is not { } r)
                {
                    Say($"{(string)i["name"]!}: {failure}");
                    continue;
                }

                cases.Add(new Case((string)i["name"]!, true, camera ? "real photographs" : "real scans", r.Value, d, r.Mapping, r.Dpi, r.Render,
                    [.. truth.Select(p => new PointD((double)p![0]! * DmmPerInch, (double)p[1]! * DmmPerInch))],
                    InkProximity.RealCalibreInches * (camera ? AutomaticMarking.PhotographHoleToCalibre : AutomaticMarking.ScanHoleToCalibre)));
            }
        }

        var configs = Elongations.SelectMany(e => MinimumHoles.Select(m => (Elongation: e, MinimumHoles: m))).ToList();
        var tallies = new Tally[cases.Count, configs.Count];
        int done = 0;
        Parallel.For(0, cases.Count, new ParallelOptions { MaxDegreeOfParallelism = 3 }, i =>
        {
            for (int j = 0; j < configs.Count; j++)
            {
                var (e, m) = configs[j];
                var options = shipped with { SplitElongation = e, CalibreInches = m is null ? null : cases[i].Size, SplitMinimumHoles = m ?? shipped.SplitMinimumHoles };
                tallies[i, j] = Score(cases[i], Detect(cases[i], options));
            }

            lock (output)
            {
                Say($"  {++done} of {cases.Count} images swept: {cases[i].Name}");
            }
        });

        Say("");
        Tally Sum(Func<Case, bool> which, int j) => Enumerable.Range(0, cases.Count).Where(i => which(cases[i])).Aggregate(Tally.Zero, (a, i) => a + tallies[i, j]);
        foreach (var corpus in cases.Select(c => c.Corpus).Distinct())
        {
            Say($"{corpus}: {cases.Count(c => c.Corpus == corpus)} images");
            Say("  elongation  size veto      one hole split  two holes whole  one whole  two split  >2  spurious  missed  detections  vetoed");
            for (int j = 0; j < configs.Count; j++)
            {
                var t = Sum(c => c.Corpus == corpus, j);
                var (e, m) = configs[j];
                Say(string.Create(inv,
                    $"  {e,10:0.00}  {(m is { } v ? $"under {v:0.0} holes" : "none"),-13}  {t.OneSplit,14}  {t.TwoWhole,15}  {t.OneWhole,9}  {t.TwoSplit,9}  {t.MoreThanTwo,2}  {t.Spurious,8}  {t.Missed,6}  {t.Detections,10}  {t.Vetoed,6}"));
            }

            Say("");
        }

        // The veto: real holes first.
        var shippedIndex = configs.FindIndex(c => c.Elongation == shipped.SplitElongation && c.MinimumHoles is null);
        var realAtShipped = Sum(c => c.Real, shippedIndex);
        Say("settings that misclassify no real hole, with no more real spurious detections or misses than the shipped rule without a size,");
        Say("ranked by synthetic errors; the synthetic tallies are coverage, not the choice:");
        var survivors = Enumerable.Range(0, configs.Count)
            .Select(j => (j, Real: Sum(c => c.Real, j), Synthetic: Sum(c => !c.Real, j)))
            .Where(x => x.Real.Errors == 0 && x.Real.Spurious <= realAtShipped.Spurious && x.Real.Missed <= realAtShipped.Missed)
            .OrderBy(x => x.Synthetic.Errors).ThenBy(x => x.Synthetic.Spurious)
            .ToList();
        foreach (var (j, real, synthetic) in survivors)
        {
            var (e, m) = configs[j];
            Say(string.Create(inv,
                $"  elongation {e:0.00}, size veto {(m is { } v ? $"under {v:0.0} holes" : "none")}: synthetic {synthetic.OneSplit} split and {synthetic.TwoWhole} merged of {synthetic.OneSplit + synthetic.OneWhole + synthetic.TwoSplit + synthetic.TwoWhole} blobs, {synthetic.Spurious} spurious; real {real.Spurious} spurious, {real.Missed} missed"));
        }

        if (survivors.Count == 0)
        {
            Say("  none");
        }

        return 0;
    }

    private static Registered? Register(string file, TargetDefinition definition, out string? failure)
    {
        var (grey, metadata) = ImageLoader.Load(file);
        var (value, _) = ImageLoader.LoadMaxChannel(file);
        var clean = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), new TraceRecorder());
        failure = clean.Failure;
        if (clean.Scale is not { } sheet || clean.Difference is not { } difference)
        {
            failure ??= "did not register";
            return null;
        }

        var scene = SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true)).Pages[0];
        return new Registered(Path.GetFileName(file), definition, value, sheet.Mapping, difference.Dpi, SceneRasterizer.Rasterize(scene, difference.Dpi));
    }

    private static Case Punched(Registered r, bool pairs, double scale, double size)
    {
        var random = new Random(80 + (pairs ? 1 : 0));
        var holes = new List<SyntheticHole>();
        for (int y = 75; y < r.Definition.Page.Height; y += CorpusCounts.PunchPitchDmm)
        {
            for (int x = 75; x < r.Definition.Page.Width; x += CorpusCounts.PunchPitchDmm)
            {
                var hole = SyntheticSheet.SampleHole(random, x, y, onInk: false, HoleBacking.ScannerLid, scale);
                holes.Add(hole);
                if (pairs)
                {
                    double angle = random.NextDouble() * 2 * Math.PI, apart = (0.5 + (0.4 * random.NextDouble())) * 2 * hole.RimRadius;
                    holes.Add(SyntheticSheet.SampleHole(random, x + (apart * Math.Cos(angle)), y + (apart * Math.Sin(angle)), onInk: false, HoleBacking.ScannerLid, scale));
                }
            }
        }

        var value = SyntheticSheet.Punch(r.Value, r.Mapping, r.Dpi, holes);
        return new Case($"{r.File}, {(pairs ? "pairs" : "singles")}", false, pairs ? "synthetic pairs" : "synthetic singles", value, r.Definition, r.Mapping, r.Dpi, r.Render,
            [.. holes.Select(h => new PointD(h.X, h.Y))], size);
    }

    private static RenderDifferenceResult Detect(Case c, RenderDifferenceOptions options) =>
        RenderDifferenceHoleDetector.Detect(c.Value, c.Definition, 0, c.Mapping, c.Dpi, new OpenCvSharpBackend(), options, c.Render);

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
