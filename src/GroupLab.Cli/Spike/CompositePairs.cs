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
/// <c>grouplab holes composite-pairs --local &lt;manifest&gt;</c>, NOTES-FROM-PLANNING.md entry 81 section 2: merged pairs made from real holes. No
/// real merged pair has been shot, so real hole images are lifted from a real 600 DPI scan and pasted in twos onto the clean Phase 0 scans of
/// the same sheet, at a range of centre separations from heavily overlapped to joined only by the closing. Synthetic geometry with real
/// texture: a bridge, not a substitute.
/// <list type="bullet">
/// <item><b>The source</b> is the manifest's first scan, Alan's. Its holes are its hand-verified truths that the detector finds whole, with
/// under 2 percent printed ink within 0.2 in. Each is lifted as the pixels within 0.2 in of its centre that are darker than the patch's paper
/// by 20 grey levels and that the expected artwork calls paper, shifted to the target's paper level, and pasted where darker than the target.
/// A scanned hole's core shows the lid, which reads as paper, so the pasted rim closes over the target's own paper.</item>
/// <item><b>The targets</b> are the three clean GL-CF25-LTR scans at 600 DPI. Each hole or pair is centred 0.70 in below a scoring bull,
/// inside the bull's cell and clear of its ring, the markers, the labels and the codes; a pair lies across the page. Even bulls carry a pair
/// and odd bulls a single hole, so the sheet has singles for the no-calibre oversize rule.</item>
/// <item><b>Scored</b> with the shipped settings, without a calibre and with .308: a pair is one mark, two marks, or missed, and one mark is
/// flagged oversized or not; a single hole is one mark or split, and flagged or not. Every blob's elongation and solidity are listed for
/// entry 81 section 3.</item>
/// </list>
/// </summary>
public static class CompositePairs
{
    private const double DmmPerInch = 254;

    public static IReadOnlyList<double> SeparationsInches { get; } = [0.10, 0.15, 0.20, 0.25, 0.30, 0.35, 0.40, 0.45];

    private sealed record Patch(int[] Dx, int[] Dy, byte[] Value, byte[] Grey, double Dpi);

    public static int Run(string scans, string frozen, string localManifest, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var inv = CultureInfo.InvariantCulture;
        void Say(string line)
        {
            output.WriteLine(line);
            output.Flush();
        }

        var source = JsonNode.Parse(File.ReadAllText(localManifest))!["images"]!.AsArray().First(i => ((string)i!["name"]!).Contains("scan", StringComparison.Ordinal))!;
        var patches = Lift((string)source["file"]!, GltdJsonReader.ReadFile((string)source["target"]!).Definition!,
            [.. source["truth"]!.AsArray().Select(p => new PointD((double)p![0]! * DmmPerInch, (double)p[1]! * DmmPerInch))], out string liftNote);
        Say($"source {(string)source["name"]!}: {liftNote}");
        if (patches.Count < 2)
        {
            return 1;
        }

        var definition = GltdJsonReader.ReadFile(Path.Combine(frozen, SampleSet.CentreFire)).Definition!;
        var scoring = definition.Bulls.Select((b, k) => (b, k)).Where(x => x.b.Scoring).ToList();
        var calibre = InkProximity.RealCalibreInches * AutomaticMarking.HoleToCalibre;
        Say("blob,target,separation,calibre,kind,marks,flagged,elongation,solidity,diameter,calibreHoles");
        var rows = new List<(double D, bool Calibre, bool Pair, int Marks, bool Flagged)>();
        foreach (var sample in SampleSet.All.Where(s => s.Definition == SampleSet.CentreFire && s.Kind == SampleSet.SampleKind.Scan && s.Dpi == 600 && s.Gated && !s.File.Contains("96.2", StringComparison.Ordinal)))
        {
            string file = Path.Combine(scans, sample.File);
            var (grey, metadata) = ImageLoader.Load(file);
            var (value, _) = ImageLoader.LoadMaxChannel(file);
            var clean = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), new TraceRecorder());
            if (clean.Scale is not { } sheet || clean.Difference is not { } difference)
            {
                Say($"{sample.File}: {clean.Failure}");
                continue;
            }

            var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition, new RenderOptions(AllowInvalid: true)).Pages[0], difference.Dpi);
            double paper = Percentile(value.Pixels, 0.9);
            foreach (double d in SeparationsInches)
            {
                var v = (byte[])value.Pixels.Clone();
                var sites = new List<(PointD Centre, bool Pair)>();
                for (int n = 0; n < scoring.Count; n++)
                {
                    var bull = scoring[n].b;
                    var centre = new PointD(bull.X, bull.Y + (0.70 * DmmPerInch));
                    bool pair = n % 2 == 0;
                    if (pair)
                    {
                        var (a, b) = (patches[n % patches.Count], patches[(n + 1) % patches.Count]);
                        Paste(v, value.Width, value.Height, a, sheet.Mapping.ToImage(new PointD(centre.X - (d / 2 * DmmPerInch), centre.Y)), difference.Dpi, paper);
                        Paste(v, value.Width, value.Height, b, sheet.Mapping.ToImage(new PointD(centre.X + (d / 2 * DmmPerInch), centre.Y)), difference.Dpi, paper);
                    }
                    else
                    {
                        Paste(v, value.Width, value.Height, patches[n % patches.Count], sheet.Mapping.ToImage(centre), difference.Dpi, paper);
                    }

                    sites.Add((centre, pair));
                }

                var composite = new GrayImage(value.Width, value.Height, v);
                foreach (bool named in new[] { false, true })
                {
                    var options = new RenderDifferenceOptions(CalibreInches: named ? calibre : null);
                    var result = RenderDifferenceHoleDetector.Detect(composite, definition, 0, sheet.Mapping, difference.Dpi, new OpenCvSharpBackend(), options, render);
                    foreach (var (centre, pair) in sites)
                    {
                        var near = result.Holes.Where(h =>
                        {
                            var p = sheet.Mapping.ToPage(new PointD(h.X, h.Y));
                            return Math.Sqrt(Math.Pow(p.X - centre.X, 2) + Math.Pow(p.Y - centre.Y, 2)) <= 0.45 * DmmPerInch;
                        }).ToList();
                        bool flagged = near.Count == 1 && near[0].Oversized;
                        rows.Add((d, named, pair, near.Count, flagged));
                        foreach (var blob in near.GroupBy(h => (h.HullX, h.HullY)))
                        {
                            var h = blob.First();
                            Say(string.Create(inv, $"blob,{sample.File},{d:0.00},{named},{(pair ? "pair" : "single")},{near.Count},{h.Oversized},{h.Elongation:0.000},{h.Solidity:0.000},{h.DiameterInches:0.000},{h.CalibreHoles:0.00}"));
                        }
                    }
                }

                Say(string.Create(inv, $"  {sample.File}: separation {d:0.00} in done"));
            }
        }

        Say("");
        foreach (bool named in new[] { false, true })
        {
            Say(named ? string.Create(inv, $"with .308, a hole taken as {calibre:0.000} in:") : "without a calibre:");
            Say("  separation (in)  pairs  one mark  flagged  two marks  other  |  singles  split  flagged");
            foreach (double d in SeparationsInches)
            {
                var pairs = rows.Where(r => r.D == d && r.Calibre == named && r.Pair).ToList();
                var singles = rows.Where(r => r.D == d && r.Calibre == named && !r.Pair).ToList();
                Say(string.Create(inv,
                    $"  {d,15:0.00}  {pairs.Count,5}  {pairs.Count(r => r.Marks == 1),8}  {pairs.Count(r => r.Marks == 1 && r.Flagged),7}  {pairs.Count(r => r.Marks == 2),9}  {pairs.Count(r => r.Marks is 0 or > 2),5}  |  {singles.Count,7}  {singles.Count(r => r.Marks >= 2),5}  {singles.Count(r => r.Marks == 1 && r.Flagged),7}"));
            }

            Say("");
        }

        return 0;
    }

    private static double Percentile(byte[] pixels, double q)
    {
        var histogram = new long[256];
        foreach (byte p in pixels)
        {
            histogram[p]++;
        }

        long target = (long)(q * pixels.Length), seen = 0;
        for (int i = 0; i < 256; i++)
        {
            seen += histogram[i];
            if (seen >= target)
            {
                return i;
            }
        }

        return 255;
    }

    private static List<Patch> Lift(string file, TargetDefinition definition, IReadOnlyList<PointD> truth, out string note)
    {
        var (grey, metadata) = ImageLoader.Load(file);
        var (value, _) = ImageLoader.LoadMaxChannel(file);
        var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), new TraceRecorder());
        var patches = new List<Patch>();
        if (result.Scale is not { } sheet || result.Difference is not { } difference || difference.Expected is not { } expected)
        {
            note = result.Failure ?? "did not register";
            return patches;
        }

        double dpi = difference.Dpi;
        int reach = (int)Math.Ceiling(0.2 * dpi);
        int considered = 0;
        foreach (var hole in difference.Holes.Where(h => !h.PossibleMerge))
        {
            var page = sheet.Mapping.ToPage(new PointD(hole.X, hole.Y));
            if (!truth.Any(t => Math.Sqrt(Math.Pow(t.X - page.X, 2) + Math.Pow(t.Y - page.Y, 2)) <= 0.15 * DmmPerInch))
            {
                continue;
            }

            considered++;
            int cx = (int)Math.Round(hole.X), cy = (int)Math.Round(hole.Y), inside = 0, ink = 0;
            var paperValues = new List<byte>();
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if ((dx * dx) + (dy * dy) > reach * reach || x < 0 || y < 0 || x >= value.Width || y >= value.Height)
                    {
                        continue;
                    }

                    inside++;
                    if (expected.Pixels[(y * value.Width) + x] < 128)
                    {
                        ink++;
                    }
                    else
                    {
                        paperValues.Add(value.Pixels[(y * value.Width) + x]);
                    }
                }
            }

            if (ink > 0.02 * inside || paperValues.Count == 0)
            {
                continue;
            }

            paperValues.Sort();
            double paper = paperValues[(int)(0.9 * (paperValues.Count - 1))];
            var dxs = new List<int>();
            var dys = new List<int>();
            var vs = new List<byte>();
            var gs = new List<byte>();
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if ((dx * dx) + (dy * dy) > reach * reach || x < 0 || y < 0 || x >= value.Width || y >= value.Height)
                    {
                        continue;
                    }

                    int i = (y * value.Width) + x;
                    if (expected.Pixels[i] >= 128 && value.Pixels[i] < paper - 20)
                    {
                        dxs.Add(dx);
                        dys.Add(dy);
                        vs.Add((byte)Math.Clamp(value.Pixels[i] - paper + 255, 0, 255));
                        gs.Add((byte)Math.Clamp(grey.Pixels[i] - paper + 255, 0, 255));
                    }
                }
            }

            patches.Add(new Patch([.. dxs], [.. dys], [.. vs], [.. gs], dpi));
        }

        note = $"{patches.Count} of {considered} real whole holes lifted clear of printed ink";
        return patches;
    }

    /// <summary>A lifted hole pasted at an image point, its pixels stored relative to paper at 255 and moved to the target's paper, darker winning.</summary>
    private static void Paste(byte[] target, int width, int height, Patch patch, PointD at, double dpi, double paper)
    {
        double scale = dpi / patch.Dpi;
        for (int k = 0; k < patch.Dx.Length; k++)
        {
            int x = (int)Math.Round(at.X + (patch.Dx[k] * scale)), y = (int)Math.Round(at.Y + (patch.Dy[k] * scale));
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                continue;
            }

            byte shade = (byte)Math.Clamp(patch.Value[k] - 255 + paper, 0, 255);
            int i = (y * width) + x;
            target[i] = Math.Min(target[i], shade);
        }
    }
}
