using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 17 section 5: is what the surface fit leaves on a mounted frame structured or random? The
/// same question Phase 0 measurement 6 asked of the printer's displacement field (<c>grouplab spike field</c>), asked of
/// each gated photograph's post-fit corner residual in <c>surface.json</c>.
/// <para>
/// Each usable corner's signed page residual, the fitted mapping's page point for its image point less its declared page
/// point, is averaged over its marker, and the frame's mean is removed. The statistic is a spatial autocorrelation: the
/// mean dot product of the residual vectors of neighbouring markers, those within 1.5 times the median nearest-marker
/// distance, over the mean squared residual. It is near 0 for independent noise and near 1 for a field that varies slowly
/// across the sheet. Its significance is a permutation test, the markers' residuals shuffled among their positions 2000
/// times. The same statistic over marker pairs farther apart than half the largest separation is reported beside it.
/// Synthetic frames calibrate it: white corner noise alone, and a twist no developable surface takes, each fitted as the
/// real frames are.
/// </para>
/// </summary>
public static class SurfaceCorrelation
{
    private const int Width = 3000, Height = 4000, Seeds = 10, Permutations = 2000;
    private static readonly double ExifFocal = 4000 * 23 / 36.0;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    internal sealed record Field(int Markers, double NeighbourDistanceDmm, int NeighbourPairs, double Neighbour, double NeighbourP, int FarPairs, double Far, double RmsResidualDmm);

    public static int Run(string scans, string frozenDirectory, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var raw = new List<object>();
        output.WriteLine("Every gated photograph, from the fit in surface.json. Neighbour correlation of the per-marker post-fit residual, its permutation p over 2000 shuffles, and the far-pair correlation. Structured at p below 0.001.");
        output.WriteLine();
        output.WriteLine("| Gate | Photograph | Markers | RMS marker residual (dmm) | Neighbour distance (dmm) | Neighbour pairs | Neighbour correlation | p | Far correlation | Reading |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        using var document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(scans, "measurements", "surface.json")));
        foreach (var row in document.RootElement.GetProperty("rows").EnumerateArray())
        {
            string gate = row.GetProperty("gate").GetString()!;
            if (gate is not ("mounted" or "flat") || !row.TryGetProperty("corners", out var cornerRows))
            {
                continue;
            }

            string file = row.GetProperty("file").GetString()!;
            var definition = Phase0Spike.Definition(frozenDirectory, SampleSet.All.Single(s => s.File == file).Definition);
            var mapping = new SurfaceMapping(ModelFrom(row.GetProperty("mapping").GetProperty("parameters")), 0, 0, definition.Page.Width, definition.Page.Height);
            var corners = cornerRows.EnumerateArray().Select(c => (
                Image: new PointD(c.GetProperty("imageXPx").GetDouble(), c.GetProperty("imageYPx").GetDouble()),
                Page: new PointD(c.GetProperty("pageX").GetDouble(), c.GetProperty("pageY").GetDouble()),
                Usable: c.GetProperty("error").TryGetDouble(out double e) && e <= SurfaceFit.MisreadThreshold)).ToList();
            var (centres, residuals) = MarkerResiduals(corners.Select(c => c.Image).ToList(), corners.Select(c => c.Page).ToList(), corners.Select(c => c.Usable).ToList(), mapping);
            var field = Correlate(centres, residuals, 17);
            output.WriteLine(string.Create(Inv,
                $"| {gate} | `{file}` | {field.Markers} | {field.RmsResidualDmm:0.00} | {field.NeighbourDistanceDmm:0} | {field.NeighbourPairs} | {field.Neighbour:+0.00;-0.00} | {Show(field.NeighbourP)} | {field.Far:+0.00;-0.00} | {Reading(field)} |"));
            raw.Add(new
            {
                file,
                gate,
                field,
                markers = centres.Select((c, i) => new { centreX = RawMeasurements.R(c.X), centreY = RawMeasurements.R(c.Y), dx = RawMeasurements.R(residuals[i].X), dy = RawMeasurements.R(residuals[i].Y) }).ToArray(),
            });
        }

        var definitionForSynthetic = Phase0Spike.Definition(frozenDirectory, SampleSet.CentreFire);
        double w = definitionForSynthetic.Page.Width, h = definitionForSynthetic.Page.Height;
        var bow = SurfaceLens.Bow(0.40, w, h);
        (string Name, double TwistDmm, double Noise)[] cases =
        [
            ("white corner noise 0.52 px", 0, 0.52),
            ("white corner noise 1.0 px", 0, 1.0),
            ("white corner noise 2.0 px", 0, 2.0),
            ("0.25 in twist, 0.52 px", 0.25 * 254, 0.52),
            ("0.50 in twist, 0.52 px", 0.50 * 254, 0.52),
        ];
        var specs = cases.SelectMany((c, ci) => Enumerable.Range(1, Seeds).Select(seed => (Case: c, Index: ci, Seed: seed))).ToList();
        var fields = new Field[specs.Count];
        Parallel.For(0, specs.Count, i =>
        {
            var (c, index, seed) = specs[i];
            var matches = SyntheticSurface.Corners(definitionForSynthetic, bow, c.TwistDmm, c.Noise, new Random((seed * 7919) + (index * 104729)), Width, Height);
            var image = matches.SelectMany(m => m.ImageCorners).ToList();
            var page = matches.SelectMany(m => m.PageCorners).ToList();
            var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
            var frame = new SurfaceFrame("synthetic", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, ExifFocal, w / 2, h / 2), 0, 0, w, h);
            var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];
            var (centres, residuals) = MarkerResiduals(image, page, [.. fit.PageErrors.Select(err => err <= SurfaceFit.MisreadThreshold)], fit.Mapping);
            fields[i] = Correlate(centres, residuals, seed);
        });

        output.WriteLine();
        output.WriteLine("Synthetic calibration: M1.7's truth camera at a 0.40 in bow, every marker, fitted as a cylinder one frame at a time, 10 seeds per case.");
        output.WriteLine();
        output.WriteLine("| Case | RMS marker residual (dmm), median | Neighbour correlation, median | Seeds structured at p below 0.001 | Far correlation, median |");
        output.WriteLine("|---|---|---|---|---|");
        for (int ci = 0; ci < cases.Length; ci++)
        {
            var group = specs.Select((s, i) => (s, i)).Where(x => x.s.Index == ci).Select(x => fields[x.i]).ToList();
            output.WriteLine(string.Create(Inv,
                $"| {cases[ci].Name} | {Median(group.Select(f => f.RmsResidualDmm)):0.00} | {Median(group.Select(f => f.Neighbour)):+0.00;-0.00} | {group.Count(f => f.NeighbourP < 0.001 && f.Neighbour > 0)} of {group.Count} | {Median(group.Select(f => f.Far)):+0.00;-0.00} |"));
            raw.Add(new { synthetic = cases[ci].Name, fields = group });
        }

        RawMeasurements.Write(scans, "surface-correlation", raw);
        return 0;
    }

    /// <summary>Each marker's centre and its mean signed page residual over its usable corners, dmm, the frame's mean removed; corners come four to a marker.</summary>
    internal static (List<PointD> Centres, List<PointD> Residuals) MarkerResiduals(IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, IReadOnlyList<bool> usable, IPageMapping mapping)
    {
        var centres = new List<PointD>();
        var residuals = new List<PointD>();
        for (int m = 0; m + 3 < image.Count; m += 4)
        {
            double sx = 0, sy = 0;
            int n = 0;
            for (int k = m; k < m + 4; k++)
            {
                if (!usable[k])
                {
                    continue;
                }

                var mapped = mapping.ToPage(image[k]);
                if (!double.IsFinite(mapped.X) || !double.IsFinite(mapped.Y))
                {
                    continue;
                }

                sx += mapped.X - page[k].X;
                sy += mapped.Y - page[k].Y;
                n++;
            }

            if (n >= 2)
            {
                centres.Add(new PointD((page[m].X + page[m + 1].X + page[m + 2].X + page[m + 3].X) / 4, (page[m].Y + page[m + 1].Y + page[m + 2].Y + page[m + 3].Y) / 4));
                residuals.Add(new PointD(sx / n, sy / n));
            }
        }

        double mx = residuals.Count == 0 ? 0 : residuals.Average(r => r.X), my = residuals.Count == 0 ? 0 : residuals.Average(r => r.Y);
        return (centres, [.. residuals.Select(r => new PointD(r.X - mx, r.Y - my))]);
    }

    internal static Field Correlate(IReadOnlyList<PointD> centres, IReadOnlyList<PointD> residuals, int seed)
    {
        int n = centres.Count;
        if (n < 4)
        {
            return new Field(n, double.NaN, 0, double.NaN, double.NaN, 0, double.NaN, double.NaN);
        }

        double Distance(int a, int b) => Math.Sqrt(Math.Pow(centres[a].X - centres[b].X, 2) + Math.Pow(centres[a].Y - centres[b].Y, 2));
        var nearest = Enumerable.Range(0, n).Select(a => Enumerable.Range(0, n).Where(b => b != a).Min(b => Distance(a, b))).Order().ToList();
        double neighbourLimit = 1.5 * nearest[n / 2];
        double largest = Enumerable.Range(0, n).SelectMany(a => Enumerable.Range(a + 1, n - a - 1).Select(b => Distance(a, b))).Max();
        var near = new List<(int A, int B)>();
        var far = new List<(int A, int B)>();
        for (int a = 0; a < n; a++)
        {
            for (int b = a + 1; b < n; b++)
            {
                double d = Distance(a, b);
                if (d <= neighbourLimit)
                {
                    near.Add((a, b));
                }
                else if (d > largest / 2)
                {
                    far.Add((a, b));
                }
            }
        }

        double power = residuals.Average(r => (r.X * r.X) + (r.Y * r.Y));
        double Of(List<(int A, int B)> pairs, int[] order) => pairs.Count == 0 || power == 0 ? double.NaN
            : pairs.Average(p => (residuals[order[p.A]].X * residuals[order[p.B]].X) + (residuals[order[p.A]].Y * residuals[order[p.B]].Y)) / power;

        var identity = Enumerable.Range(0, n).ToArray();
        double observed = Of(near, identity);
        var random = new Random(seed);
        int atLeast = 0;
        var order = (int[])identity.Clone();
        for (int t = 0; t < Permutations; t++)
        {
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            if (Of(near, order) >= observed)
            {
                atLeast++;
            }
        }

        return new Field(n, neighbourLimit, near.Count, observed, (atLeast + 1.0) / (Permutations + 1), far.Count, Of(far, identity), Math.Sqrt(power));
    }

    private static SurfaceModel ModelFrom(JsonElement p)
    {
        double D(string name) => p.GetProperty(name).GetDouble();
        var family = p.TryGetProperty("family", out var f) && f.ValueKind == JsonValueKind.String && string.Equals(f.GetString(), "general", StringComparison.OrdinalIgnoreCase)
            ? SurfaceFamily.General : SurfaceFamily.Cylinder;
        IReadOnlyList<double>? turn = p.TryGetProperty("turn", out var t) && t.ValueKind == JsonValueKind.Array ? [.. t.EnumerateArray().Select(v => v.GetDouble())] : null;
        var projection = p.TryGetProperty("projection", out var pr) && pr.ValueKind == JsonValueKind.String && string.Equals(pr.GetString(), "orthographic", StringComparison.OrdinalIgnoreCase)
            ? SurfaceProjection.Orthographic : SurfaceProjection.Perspective;
        return new SurfaceModel(projection, D("rulingAngle"), [.. p.GetProperty("bend").EnumerateArray().Select(v => v.GetDouble())],
            D("rotationX"), D("rotationY"), D("rotationZ"), D("translationX"), D("translationY"), D("translationZ"), D("focal"), D("k1"), D("k2"),
            D("centreX"), D("centreY"), D("scale"), D("pageCentreX"), D("pageCentreY"), family, turn);
    }

    private static string Reading(Field f) => double.IsNaN(f.NeighbourP) ? "too few markers" : f.NeighbourP < 0.001 && f.Neighbour > 0 ? "structured" : "not distinguishable from random";

    private static string Show(double p) => p < 0.001 ? "< 0.001" : p.ToString("0.000", Inv);

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[sorted.Count / 2];
    }
}
