using System.Globalization;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// What <see cref="StoreTargetOutline.Find"/> made of a photograph: the four corners, clockwise from the top left, found or the best guess; which
/// method gave them; a sentence for the screen; and a line for each method tried, for Show work.
/// </summary>
public sealed record StoreTargetOutlineResult(bool Found, PointD[] Corners, string Method, string Said, IReadOnlyList<string> Tried);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 362 section 2: the four corners of a store-bought target in a photograph or a scan. <see cref="SheetOutline"/>
/// was built for GroupLab sheets, the largest light region by brightness alone, and on Alan's five photographs of 2026-10-03 the largest
/// light region was the counter, which runs out of the frame, so every one was refused. A store-bought target differs: white paper on a white
/// or cream counter differs more in colour than in brightness, a shadow changes brightness far more than colour, and large dark or coloured
/// printing fills most of it.
/// <para>
/// <b>How.</b> At about 1000 pixels across, three kinds of rough outline are made. Straight edges of colour (lightness and both colour axes,
/// the colour axes weighted up), as Canny and a Hough transform find them and as a second Hough finds long faint ones, each pixel voting for
/// the lines across its own gradient; a printed line, with the same paper beyond both sides, is left out. Every four of those lines making a
/// convex quadrilateral is one outline. The region whose colour, brightness aside, differs from the picture's border is another, outlined
/// by the GroupLab sheet finder, or where only the printing stands out the smallest rectangle round it. The GroupLab sheet finder's own
/// light region is the third. Each rough outline is then snapped a side at a time to the outermost nearby line on which the colour across
/// it changes the same way all along (or a thin line of one colour runs, the shadow of a lifted edge), and every outline is scored the same
/// way: how much of its weakest side lies on such an edge, and how far its edges run on past its corners, as a counter's edge or a shadow does
/// and a paper's edge does not. The largest outline that passes wins, since the paper's edge lies outside any printed border, unless a
/// smaller one that passes lies inside it with a strip of counter between them, a side gone past the target onto the counter's own edge. Each side is
/// then refined at full size to the strongest colour change across it, averaged along the side, twice.
/// </para>
/// <para>
/// <b>A scan.</b> A target laid in a flatbed's corner has the scan's own edges for two or three of its sides; those count as found where the
/// target reaches them, and not where the scanner's blank lid lies along them.
/// </para>
/// <para>
/// <b>When it says no</b>, it says why in a sentence for the screen, and the handles start at its best guess: the colour region's outline
/// where there is one, the best supported other outline where not, a fixed inset where nothing was found, the whole scan for a scan.
/// </para>
/// </summary>
public static class StoreTargetOutline
{
    private const int Working = 1000;

    /// <summary>A side counts as on an edge where the gradient across it is at least this share of the picture's edge threshold.</summary>
    private const double Support = 0.5;

    /// <summary>The share of each side that must lie on an edge for the four to be called found.</summary>
    public const double SideShare = 0.7;

    /// <summary>The same for a colour region, whose four straight sides are already shown.</summary>
    public const double RegionShare = 0.4;

    /// <summary>The most an outline's edges may run on past its corners, as a share of the points looked at there.</summary>
    public const double MostRunOn = 0.3;

    public const string NoEdges = "no straight edges stand out from what the target lies on; a plain surface of a different colour helps";

    public const string SameColour = "the paper's edge could not be told from what it lies on all the way round; a darker or plainer surface helps";

    public const string Broken = "one side of the target is hidden or broken, by a shadow, glare or something lying over it";

    private readonly record struct Line(PointD Point, PointD Direction, double Length);

    private sealed record Candidate(PointD[] Corners, double Weakest, double RunOn, double Area, string Method);

    /// <param name="colour">The picture, upright.</param>
    /// <param name="scan">A flatbed scan, not a photograph: a target laid against the scanner's corner has the scan's own edges for two of its
    /// sides, so a side along the picture's border counts as found.</param>
    public static StoreTargetOutlineResult Find(Mat colour, bool scan = false)
    {
        ArgumentNullException.ThrowIfNull(colour);
        var tried = new List<string>();
        double k = Math.Max(1, Math.Max(colour.Width, colour.Height) / (double)Working);
        using var small = new Mat();
        Cv2.Resize(colour, small, new Size((int)Math.Round(colour.Width / k), (int)Math.Round(colour.Height / k)), 0, 0, InterpolationFlags.Area);
        using var gradients = new Gradients(small) { Scan = scan };
        int w = small.Width, h = small.Height;
        double floor = gradients.Threshold;

        var lines = Lines(gradients, w, h);
        var candidates = new List<Candidate>();
        // The edge quadrilaterals, the largest few of the well supported ones snapped like the regions, since a faint side found as a line may
        // be a printed border's.
        if (scan)
        {
            lines.AddRange([new Line(new PointD(0, 0), new PointD(1, 0), w), new Line(new PointD(0, h - 1), new PointD(1, 0), w),
                new Line(new PointD(0, 0), new PointD(0, 1), h), new Line(new PointD(w - 1, 0), new PointD(0, 1), h)]);
        }

        // The colour region first: what it learns of the counter's colour is used in scoring every outline after it.
        var regionCandidates = new List<Candidate>();
        if (ColourRegion(gradients, w, h, out string colourWhy) is { } region)
        {
            regionCandidates.Add(Score(region, gradients, w, h, colourWhy.Length == 0 ? "colour region, unsnapped" : "printed extent, unsnapped"));
            var c = Score(Snap(region, gradients, w, h), gradients, w, h, colourWhy.Length == 0 ? "colour region" : "printed extent");
            regionCandidates.Add(c);
            tried.Add(string.Create(CultureInfo.InvariantCulture, $"{c.Method}: found, weakest side {c.Weakest:0.00} on an edge, edges run on past the corners {c.RunOn:0.00}"));
        }
        else
        {
            tried.Add("colour region: " + colourWhy);
        }

        var quads = Quads(lines, gradients, w, h);
        candidates.AddRange(quads);
        candidates.AddRange(quads.Where(c => c.Weakest >= 0.5).OrderByDescending(c => c.Area).Take(4)
            .Select(c => Score(Snap(c.Corners, gradients, w, h), gradients, w, h, "edges, snapped")));
        tried.Add(string.Create(CultureInfo.InvariantCulture, $"edges: {lines.Count} straight lines, {candidates.Count} four-sided shapes scored, edge threshold {floor:0.0}"));

        candidates.AddRange(regionCandidates);

        // The GroupLab sheet's own finder, scored the same way, so a sheet it finds well is not lost.
        using (var grey = new Mat())
        {
            Cv2.CvtColor(small, grey, ColorConversionCodes.BGR2GRAY);
            var image = OpenCvSharpBackend.Copy(grey);
            if (SheetOutline.Find(image, out string? why) is { } sheet)
            {
                var c = Score(Snap([.. sheet.Corners], gradients, w, h), gradients, w, h, "light region");
                candidates.Add(c);
                tried.Add(string.Create(CultureInfo.InvariantCulture, $"light region: found, weakest side {c.Weakest:0.00} on an edge"));
            }
            else
            {
                tried.Add("light region: " + why);
            }
        }

        // A colour region has already passed the GroupLab sheet finder's test of four straight sides, and its colour is evidence the edges
        // alone are not, so its sides need be on an edge for less of their length.
        var good = candidates.Where(c => c.Weakest >= (c.Method == "colour region" ? RegionShare : SideShare) && c.RunOn <= MostRunOn).ToList();
        if (good.Count > 0)
        {
            // The largest that passes, unless a smaller one that passes lies inside it with only counter between them: then a side went
            // past the target onto something of the counter's, and the smaller is the target.
            var ordered = good.OrderByDescending(c => c.Area).ToList();
            var best = ordered[0];
            if (!scan)
            {
                double margin = 0.015 * Math.Min(w, h);
                foreach (var smaller in ordered.Skip(1).Where(c => c.Area >= 0.4 * best.Area && c.Corners.All(p => InsideQuad(p, best.Corners) || Near(p, best.Corners, margin))))
                {
                    bool between = CounterBetween(best.Corners, smaller.Corners, gradients, out double share);
                    if (between)
                    {
                        tried.Add(string.Create(CultureInfo.InvariantCulture, $"{best.Method} lost: {100 * share:0}% of what lies between it and the {smaller.Method} inside it is counter"));
                        best = smaller;
                    }
                }
            }
            // Twice: a side that starts several working pixels off moves most of the way the first time and settles the second.
            var full = Refine(colour, best.Corners.Select(p => Up(p, k)).ToArray(), (int)Math.Ceiling(5 * k) + 2, k);
            full = Refine(colour, full, (int)Math.Ceiling(2 * k) + 2, k);
            foreach (var c in candidates.Where(c => c != best).OrderByDescending(c => c.Area).Take(3))
            {
                tried.Add(Lost(c, best));
            }

            tried.Add(string.Create(CultureInfo.InvariantCulture, $"chose {best.Method}: weakest side {best.Weakest:0.00} on an edge, {100 * best.Area / (w * h):0}% of the picture"));
            return new StoreTargetOutlineResult(true, full, best.Method, "found from the target's edges", tried);
        }

        // Not found: the best partial guess, so the handles start near the target rather than at a fixed inset.
        // The region's own outline before any snapping is the best start where it exists, since it follows what the target looks like; then
        // the best supported of the rest.
        var partial = candidates.Where(c => c.Method.EndsWith("unsnapped", StringComparison.Ordinal) && Squarish(c.Corners)).MaxBy(c => c.Area)
            ?? candidates.Where(c => c.Weakest >= 0.3 && Squarish(c.Corners)).OrderByDescending(c => c.Weakest - c.RunOn + (c.Area / (w * h))).FirstOrDefault();
        string said = candidates.Count == 0 ? NoEdges : partial is not null && partial.Weakest < 0.5 ? Broken : SameColour;
        tried.Add("not found: " + said);
        var corners = partial is null || scan
            ? scan ? [new(0, 0), new(colour.Width - 1, 0), new(colour.Width - 1, colour.Height - 1), new(0, colour.Height - 1)] : Inset(colour.Width, colour.Height)
            : partial.Corners.Select(p => Up(p, k)).ToArray();
        return new StoreTargetOutlineResult(false, corners, partial?.Method ?? "", said, tried);
    }

    /// <summary>For the trial: the working picture with its edge ridges in grey and its lines in colour, written to <paramref name="path"/>.</summary>
    public static void Picture(Mat colour, string path)
    {
        ArgumentNullException.ThrowIfNull(colour);
        double k = Math.Max(1, Math.Max(colour.Width, colour.Height) / (double)Working);
        using var small = new Mat();
        Cv2.Resize(colour, small, new Size((int)Math.Round(colour.Width / k), (int)Math.Round(colour.Height / k)), 0, 0, InterpolationFlags.Area);
        using var gradients = new Gradients(small);
        using var picture = new Mat();
        using var scaled = new Mat();
        var rejected = new List<LineSegmentPoint>();
        var lines = Lines(gradients, small.Width, small.Height, scaled, rejected);
        Cv2.CvtColor(scaled, picture, ColorConversionCodes.GRAY2BGR);
        picture.ConvertTo(picture, -1, 0.5);
        foreach (var s in rejected)
        {
            Cv2.Line(picture, s.P1, s.P2, new Scalar(255, 128, 0), 2);
        }

        foreach (var line in lines)
        {
            var a = new Point(line.Point.X - (2000 * line.Direction.X), line.Point.Y - (2000 * line.Direction.Y));
            var b = new Point(line.Point.X + (2000 * line.Direction.X), line.Point.Y + (2000 * line.Direction.Y));
            Cv2.Line(picture, a, b, new Scalar(0, 0, 255), 1);
        }

        Cv2.ImWrite(path, picture);
    }

    /// <summary>The fixed rectangle offered when nothing better was found.</summary>
    public static PointD[] Inset(int width, int height)
    {
        double dx = width * 0.12, dy = height * 0.12;
        return [new(dx, dy), new(width - dx, dy), new(width - dx, height - dy), new(dx, height - dy)];
    }

    private static string Lost(Candidate c, Candidate best) => c.Weakest < (c.Method == "colour region" ? RegionShare : SideShare)
        ? string.Create(CultureInfo.InvariantCulture, $"{c.Method} lost: its weakest side is only {c.Weakest:0.00} on an edge")
        : c.RunOn > MostRunOn
            ? string.Create(CultureInfo.InvariantCulture, $"{c.Method} lost: its edges run on past the corners ({c.RunOn:0.00}), as a counter's edge or a printed line does")
            : string.Create(CultureInfo.InvariantCulture, $"{c.Method} lost: smaller than the one chosen ({100 * c.Area / best.Area:0}% of it), so a printed border inside the paper");

    private static PointD Up(PointD p, double k) => new(((p.X + 0.5) * k) - 0.5, ((p.Y + 0.5) * k) - 0.5);

    /// <summary>The colour picture's gradients, in lightness and the two colour axes, and the Lab picture they came from.</summary>
    private sealed class Gradients : IDisposable
    {
        private readonly float[][] gx = new float[3][];
        private readonly float[][] gy = new float[3][];
        private readonly byte[] lab;

        public Gradients(Mat bgr)
        {
            Width = bgr.Width;
            Height = bgr.Height;
            int n = Width * Height;
            using var blurred = new Mat();
            Cv2.GaussianBlur(bgr, blurred, new Size(9, 9), 2.0);
            using var labMat = new Mat();
            Cv2.CvtColor(blurred, labMat, ColorConversionCodes.BGR2Lab);
            labMat.GetArray(out Vec3b[] labPixels);
            lab = new byte[3 * n];
            for (int i = 0; i < n; i++)
            {
                lab[3 * i] = labPixels[i].Item0;
                lab[(3 * i) + 1] = labPixels[i].Item1;
                lab[(3 * i) + 2] = labPixels[i].Item2;
            }

            var channels = Cv2.Split(labMat);
            for (int c = 0; c < 3; c++)
            {
                using var f = new Mat();
                // White on cream differs mostly in the colour axes, which are narrower than lightness: weight them up.
                channels[c].ConvertTo(f, MatType.CV_32F, Weight(c));
                using var x = new Mat();
                using var y = new Mat();
                Cv2.Sobel(f, x, MatType.CV_32F, 1, 0, 3, 0.125);
                Cv2.Sobel(f, y, MatType.CV_32F, 0, 1, 3, 0.125);
                x.GetArray(out gx[c]);
                y.GetArray(out gy[c]);
                channels[c].Dispose();
            }

            // The strongest channel at each pixel gives the edge's direction, as Canny on a colour picture does.
            var magnitude = new float[n];
            Dx = new short[n];
            Dy = new short[n];
            for (int i = 0; i < n; i++)
            {
                int best = 0;
                float strongest = -1;
                double sum = 0;
                for (int c = 0; c < 3; c++)
                {
                    float m = (gx[c][i] * gx[c][i]) + (gy[c][i] * gy[c][i]);
                    sum += m;
                    if (m > strongest)
                    {
                        (strongest, best) = (m, c);
                    }
                }

                magnitude[i] = (float)Math.Sqrt(sum);
                Dx[i] = (short)Math.Clamp(Math.Round(gx[best][i] * Scale), short.MinValue, short.MaxValue);
                Dy[i] = (short)Math.Clamp(Math.Round(gy[best][i] * Scale), short.MinValue, short.MaxValue);
            }

            Magnitude = magnitude;
            var sorted = (float[])magnitude.Clone();
            Array.Sort(sorted);
            // The edge threshold from the picture's own texture: well above its typical gradient, never below a fixed floor, and never so high
            // that only the boldest printing passes.
            Threshold = Math.Min(Math.Max(2.0, 4 * sorted[n / 2]), 0.25 * sorted[n - 1]);
        }

        /// <summary>The 16-bit gradients are the float ones times this.</summary>
        public const double Scale = 16;

        public static double Weight(int channel) => channel == 0 ? 1.0 : 2.5;

        public int Width { get; }

        public int Height { get; }

        /// <summary>A flatbed scan, whose border may be two of the target's sides.</summary>
        public bool Scan { get; init; }

        /// <summary>
        /// Each pixel's colour distance from what lies round the picture's border, and the distance past which it is not the counter, once
        /// the colour region has been looked for; null before, or where nothing differed.
        /// </summary>
        public byte[]? Distance { get; set; }

        public byte DistanceThreshold { get; set; }

        public float[] Magnitude { get; }

        public short[] Dx { get; }

        public short[] Dy { get; }

        public double Threshold { get; }

        /// <summary>The weighted Lab colour at the nearest pixel, or null outside the picture.</summary>
        public double[]? Colour(PointD p)
        {
            int x = (int)Math.Round(p.X), y = (int)Math.Round(p.Y);
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return null;
            }

            int i = 3 * ((y * Width) + x);
            return [lab[i], Weight(1) * lab[i + 1], Weight(2) * lab[i + 2]];
        }

        /// <summary>The strongest gradient across <paramref name="normal"/> within a pixel and a half of the point.</summary>
        public double Across(PointD p, PointD normal)
        {
            double best = 0;
            for (int o = -3; o <= 3; o++)
            {
                int x = (int)Math.Round(p.X + (0.5 * o * normal.X)), y = (int)Math.Round(p.Y + (0.5 * o * normal.Y));
                if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1)
                {
                    continue;
                }

                int i = (y * Width) + x;
                double sum = 0;
                for (int c = 0; c < 3; c++)
                {
                    double g = (gx[c][i] * normal.X) + (gy[c][i] * normal.Y);
                    sum += g * g;
                }

                best = Math.Max(best, sum);
            }

            return Math.Sqrt(best);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Straight runs of edge, merged into lines, longest first. A printed line has an edge on each side with paper beyond both, and the paper's
    /// own edge has the paper on one side and something else on the other; so a line whose two sides, a few pixels out, are the same colour is
    /// printing, and is left out.
    /// </summary>
    private static List<Line> Lines(Gradients gradients, int w, int h) => Lines(gradients, w, h, null, null);

    private static List<Line> Lines(Gradients gradients, int w, int h, Mat? edgesOut, List<LineSegmentPoint>? rejected)
    {
        using var dx = new Mat(h, w, MatType.CV_16SC1);
        using var dy = new Mat(h, w, MatType.CV_16SC1);
        dx.SetArray(gradients.Dx);
        dy.SetArray(gradients.Dy);
        using var thin = new Mat();
        double t = gradients.Threshold * Gradients.Scale;
        Cv2.Canny(dx, dy, thin, 0.35 * t, 0.7 * t, true);
        if (edgesOut is not null)
        {
            thin.CopyTo(edgesOut);
        }

        int shortest = (int)(0.1 * Math.Min(w, h));
        var segments = Cv2.HoughLinesP(thin, 1, Math.PI / 360, Math.Max(20, shortest / 3), shortest, 0.03 * Math.Min(w, h));
        var clusters = new List<(double Theta, double Rho, List<LineSegmentPoint> Members, double Length)>();
        foreach (var s in segments.Concat(Faint(gradients, w, h)).OrderByDescending(s => s.Length()))
        {
            if (!Step(gradients, s))
            {
                rejected?.Add(s);
                continue;
            }

            double theta = Math.Atan2(s.P2.X - s.P1.X, -(s.P2.Y - s.P1.Y));
            double rho = (s.P1.X * Math.Cos(theta)) + (s.P1.Y * Math.Sin(theta));
            int at = clusters.FindIndex(c => Near(c.Theta, c.Rho, theta, rho));
            if (at < 0)
            {
                clusters.Add((theta, rho, [s], s.Length()));
            }
            else
            {
                var c = clusters[at];
                c.Members.Add(s);
                clusters[at] = (c.Theta, c.Rho, c.Members, c.Length + s.Length());
            }
        }

        var lines = new List<Line>();
        foreach (var c in clusters.OrderByDescending(c => c.Length).Take(48))
        {
            var points = c.Members.SelectMany(s => Enumerable.Range(0, 11).Select(i => new PointD(s.P1.X + ((s.P2.X - s.P1.X) * i / 10.0), s.P1.Y + ((s.P2.Y - s.P1.Y) * i / 10.0)))).ToList();
            var (p, d) = Fit(points);
            lines.Add(new Line(p, d, c.Length));
        }

        return lines;
    }

    /// <summary>
    /// Long faint edges that Canny breaks into pieces too short to count: every pixel votes for the lines across its own gradient, weighted by
    /// how strong it is, so an edge that is faint but straight for a long way adds up where texture, pointing every way, does not. Each peak is
    /// then cut to the stretch of it that lies on an edge.
    /// </summary>
    private static List<LineSegmentPoint> Faint(Gradients gradients, int w, int h)
    {
        const int angles = 360;
        int diagonal = (int)Math.Ceiling(Math.Sqrt((w * w) + (h * h)));
        int rhos = (2 * diagonal) + 1;
        var votes = new float[angles * rhos];
        var cos = new double[angles];
        var sin = new double[angles];
        for (int a = 0; a < angles; a++)
        {
            cos[a] = Math.Cos(a * Math.PI / angles);
            sin[a] = Math.Sin(a * Math.PI / angles);
        }

        double floor = 0.3 * gradients.Threshold * Gradients.Scale;
        for (int y = 2; y < h - 2; y++)
        {
            for (int x = 2; x < w - 2; x++)
            {
                int i = (y * w) + x;
                double gx = gradients.Dx[i], gy = gradients.Dy[i];
                double m = Math.Sqrt((gx * gx) + (gy * gy));
                if (m < floor)
                {
                    continue;
                }

                double phi = Math.Atan2(gy, gx);
                if (phi < 0)
                {
                    phi += Math.PI;
                }

                int centre = (int)Math.Round(phi * angles / Math.PI);
                for (int d = -4; d <= 4; d++)
                {
                    int a = ((centre + d) % angles + angles) % angles;
                    // A line's normal at angle a: rho = x cos + y sin, and wrapping past 180 degrees flips rho's sign.
                    int r = (int)Math.Round((x * cos[a]) + (y * sin[a])) + diagonal;
                    votes[(a * rhos) + r] += (float)(m * (1 - (Math.Abs(d) / 5.0)));
                }
            }
        }

        double least = 0.12 * Math.Min(w, h) * 0.5 * gradients.Threshold * Gradients.Scale;
        var peaks = new List<(int A, int R, float V)>();
        for (int a = 0; a < angles; a++)
        {
            for (int r = 1; r < rhos - 1; r++)
            {
                float v = votes[(a * rhos) + r];
                if (v < least)
                {
                    continue;
                }

                bool top = true;
                for (int da = -3; da <= 3 && top; da++)
                {
                    int aa = a + da;
                    int rr0 = r;
                    if (aa < 0 || aa >= angles)
                    {
                        aa = (aa + angles) % angles;
                        rr0 = (2 * diagonal) - r;
                    }

                    for (int dr = -6; dr <= 6 && top; dr++)
                    {
                        int rr = rr0 + dr;
                        if ((da != 0 || dr != 0) && rr >= 0 && rr < rhos && votes[(aa * rhos) + rr] > v)
                        {
                            top = false;
                        }
                    }
                }

                if (top)
                {
                    peaks.Add((a, r, v));
                }
            }
        }

        var segments = new List<LineSegmentPoint>();
        foreach (var (a, r, _) in peaks.OrderByDescending(p => p.V).Take(80))
        {
            double rho = r - diagonal;
            var normal = new PointD(cos[a], sin[a]);
            var along = new PointD(-sin[a], cos[a]);
            var origin = new PointD(rho * cos[a], rho * sin[a]);
            // Walk the line across the picture and keep the longest stretch that changes colour the same way, bridging gaps of up to a
            // hundredth of the picture and holding the stretch to being on the edge for most of its length, so a ring's tangent is not a line.
            var ts = Enumerable.Range(-diagonal, (2 * diagonal) + 1)
                .Where(t => Inside(new PointD(origin.X + (t * along.X), origin.Y + (t * along.Y)), w, h, 4))
                .ToList();
            if (ts.Count < 10)
            {
                continue;
            }

            var (_, onLine) = Coherent(gradients, [.. ts.Select(t => new PointD(origin.X + (t * along.X), origin.Y + (t * along.Y)))], normal);
            int gap = Math.Max(4, (int)(0.012 * Math.Min(w, h))), bestStart = 0, bestEnd = -1, start = int.MinValue, lastOn = int.MinValue, onInRun = 0;
            for (int j = 0; j < ts.Count; j++)
            {
                if (!onLine[j])
                {
                    continue;
                }

                int t = ts[j];
                if (lastOn == int.MinValue || t - lastOn > gap)
                {
                    (start, onInRun) = (t, 0);
                }

                lastOn = t;
                onInRun++;
                if (t - start > bestEnd - bestStart && onInRun >= 0.75 * (t - start + 1))
                {
                    (bestStart, bestEnd) = (start, t);
                }
            }

            if (bestEnd - bestStart >= 0.12 * Math.Min(w, h))
            {
                segments.Add(new LineSegmentPoint(
                    new Point(origin.X + (bestStart * along.X), origin.Y + (bestStart * along.Y)),
                    new Point(origin.X + (bestEnd * along.X), origin.Y + (bestEnd * along.Y))));
            }
        }

        return segments;
    }

    /// <summary>
    /// The target as the region whose colour, leaving brightness aside, differs from what lies round the edge of the picture: target paper
    /// is bluer than a cream or wooden counter, and a shadow changes brightness far more than colour. The region is outlined by the GroupLab
    /// sheet's own finder, on a black and white picture of it. Null with the reason where it gives no outline.
    /// </summary>
    private static PointD[]? ColourRegion(Gradients gradients, int w, int h, out string why)
    {
        int band = Math.Max(3, (int)(0.03 * Math.Min(w, h)));
        var a = new List<double>();
        var b = new List<double>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (x < band || y < band || x >= w - band || y >= h - band)
                {
                    var c = gradients.Colour(new PointD(x, y))!;
                    a.Add(c[1]);
                    b.Add(c[2]);
                }
            }
        }

        a.Sort();
        b.Sort();
        double a0 = a[a.Count / 2], b0 = b[b.Count / 2];
        var distance = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = gradients.Colour(new PointD(x, y))!;
                distance[(y * w) + x] = (byte)Math.Min(255, 4 * Math.Sqrt(((c[1] - a0) * (c[1] - a0)) + ((c[2] - b0) * (c[2] - b0))));
            }
        }

        // Ink differs from the counter far more than paper does, so Otsu's split of the plain distances separates the printing from everything
        // else. Clipped at the distance three quarters of the picture is under, the paper and its printing are one class and the split falls
        // between the counter and the paper.
        var sorted = (byte[])distance.Clone();
        Array.Sort(sorted);
        byte cap = Math.Max((byte)1, sorted[(int)(0.75 * (sorted.Length - 1))]);
        byte threshold = SheetOutline.Otsu([.. distance.Select(d => Math.Min(d, cap))]);
        if (distance.Count(d => d > threshold) < 0.1 * w * h)
        {
            why = "nothing differs in colour from what lies round the picture's edge";
            return null;
        }

        (gradients.Distance, gradients.DistanceThreshold) = (distance, threshold);

        var mask = new byte[w * h];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = distance[i] > threshold ? (byte)255 : (byte)0;
        }

        // A closing the size of a grid's cells, not a fixed hundredth of the frame, so the printing joins the paper between its lines and a large
        // dark bull does not leave a hole that reaches the edge.
        using (var m = new Mat(h, w, MatType.CV_8UC1))
        {
            m.SetArray(mask);
            // The largest piece alone, before the closing, so the closing cannot join the target to something beside it.
            using var labels = new Mat();
            using var stats = new Mat();
            using var centroids = new Mat();
            int count = Cv2.ConnectedComponentsWithStats(m, labels, stats, centroids, PixelConnectivity.Connectivity8);
            int largest = 0, area = 0;
            for (int label = 1; label < count; label++)
            {
                int size = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
                if (size > area)
                {
                    (largest, area) = (label, size);
                }
            }

            using (var only = new Mat())
            {
                Cv2.Compare(labels, Scalar.All(largest), only, CmpTypes.EQ);
                only.CopyTo(m);
            }

            int r = Math.Max(3, (int)(0.03 * Math.Min(w, h)));
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size((2 * r) + 1, (2 * r) + 1));
            Cv2.MorphologyEx(m, m, MorphTypes.Close, kernel);
            m.GetArray(out mask);
        }

        var quad = SheetOutline.Find(new GrayImage(w, h, mask), out string? reason);
        why = reason ?? "";
        if (quad is not null)
        {
            return [.. quad.Corners];
        }

        if (reason != SheetOutline.NotFourSides)
        {
            return null;
        }

        // Not four clean sides, as where only the printing stands out and the paper does not: the smallest rectangle round it, the artwork's
        // extent, for the snap to carry out to the paper's edge.
        using var region = new Mat(h, w, MatType.CV_8UC1);
        region.SetArray(mask);
        using var nonZero = new Mat();
        Cv2.FindNonZero(region, nonZero);
        if (nonZero.Rows < 0.1 * w * h)
        {
            return null;
        }

        nonZero.GetArray(out Point[] inside);
        var box = Cv2.MinAreaRect(inside).Points();
        why = "the artwork's extent, its paper not clear of what it lies on";
        return Order([.. box.Select(p => new PointD(p.X, p.Y))]);
    }

    /// <summary>Whether a segment is a step from one colour to another, rather than a printed stroke with the same colour on both sides.</summary>
    private static bool Step(Gradients gradients, LineSegmentPoint s)
    {
        double length = s.Length();
        var along = new PointD((s.P2.X - s.P1.X) / length, (s.P2.Y - s.P1.Y) / length);
        var normal = new PointD(-along.Y, along.X);
        int steps = 0, counted = 0;
        for (int i = 1; i < 12; i++)
        {
            var at = new PointD(s.P1.X + ((s.P2.X - s.P1.X) * i / 12.0), s.P1.Y + ((s.P2.Y - s.P1.Y) * i / 12.0));
            var nearA = gradients.Colour(new PointD(at.X - (2 * normal.X), at.Y - (2 * normal.Y)));
            var nearB = gradients.Colour(new PointD(at.X + (2 * normal.X), at.Y + (2 * normal.Y)));
            var farA = gradients.Colour(new PointD(at.X - (9 * normal.X), at.Y - (9 * normal.Y)));
            var farB = gradients.Colour(new PointD(at.X + (9 * normal.X), at.Y + (9 * normal.Y)));
            if (nearA is null || nearB is null || farA is null || farB is null)
            {
                continue;
            }

            counted++;
            double near = Apart(nearA, nearB), far = Apart(farA, farB);
            steps += far >= 0.5 * near && far >= 3 ? 1 : 0;
        }

        return counted >= 4 && steps >= 0.5 * counted;
    }

    /// <summary>The smallest clear colour change across an edge, in weighted Lab units.</summary>
    private const double Clear = 2.5;

    /// <summary>
    /// Which of the points given lie on the line's edge, the test of an edge: the colour across the line changes the same way all along it, or
    /// a thin line of one colour runs along it (the shadow of a paper edge lifted a little), by at least a third of the average (a shadow
    /// shrinks the change without turning it round). Texture changes colour every which way and averages out. The averages are returned, so
    /// the same edge can be followed past a corner.
    /// </summary>
    private static (double[][] Means, bool[] On) Coherent(Gradients gradients, IReadOnlyList<PointD> points, PointD normal, double[][]? means = null)
    {
        // Two measures at each point: the step, the colour on one side less the other, and the ridge, the colour on the line less the mean of
        // the two sides.
        var measures = new double[2][][];
        measures[0] = new double[points.Count][];
        measures[1] = new double[points.Count][];
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var plus = gradients.Colour(new PointD(p.X + (3 * normal.X), p.Y + (3 * normal.Y)));
            var minus = gradients.Colour(new PointD(p.X - (3 * normal.X), p.Y - (3 * normal.Y)));
            var on = gradients.Colour(p);
            if (plus is null || minus is null || on is null)
            {
                continue;
            }

            measures[0][i] = [plus[0] - minus[0], plus[1] - minus[1], plus[2] - minus[2]];
            measures[1][i] = [on[0] - ((plus[0] + minus[0]) / 2), on[1] - ((plus[1] + minus[1]) / 2), on[2] - ((plus[2] + minus[2]) / 2)];
        }

        means ??= [Mean(measures[0]), Mean(measures[1])];
        var result = new bool[points.Count];
        for (int m = 0; m < 2; m++)
        {
            var mean = means[m];
            double size = Math.Sqrt(mean.Sum(v => v * v));
            if (size < Clear)
            {
                continue;
            }

            double least = Math.Max(Clear, 0.35 * size);
            for (int i = 0; i < points.Count; i++)
            {
                result[i] |= measures[m][i] is { } v && ((v[0] * mean[0]) + (v[1] * mean[1]) + (v[2] * mean[2])) / size >= least;
            }
        }

        return (means, result);
    }

    private static double[] Mean(double[]?[] values)
    {
        var mean = new double[3];
        int n = 0;
        foreach (var v in values)
        {
            if (v is null)
            {
                continue;
            }

            n++;
            for (int c = 0; c < 3; c++)
            {
                mean[c] += v[c];
            }
        }

        for (int c = 0; c < 3; c++)
        {
            mean[c] /= Math.Max(1, n);
        }

        return mean;
    }

    /// <summary>
    /// Whether the ring between an outline and a smaller one inside it is the counter, by the colour region's own test: a side that went past
    /// the target onto a counter's own edge or a shadow encloses a strip of counter, where the white margin between a paper's edge and its
    /// printed border is paper. False where that test has not been made, or where the ring is thinner than a twentieth of the outline.
    /// </summary>
    private static bool CounterBetween(PointD[] outer, PointD[] inner, Gradients gradients, out double share)
    {
        share = 0;
        if (gradients.Distance is not { } distance)
        {
            return false;
        }

        double x0 = outer.Min(p => p.X), x1 = outer.Max(p => p.X), y0 = outer.Min(p => p.Y), y1 = outer.Max(p => p.Y);
        int counter = 0, counted = 0;
        for (int i = 0; i < 60; i++)
        {
            for (int j = 0; j < 60; j++)
            {
                var p = new PointD(x0 + ((x1 - x0) * (i + 0.5) / 60), y0 + ((y1 - y0) * (j + 0.5) / 60));
                if (!InsideQuad(p, outer) || InsideQuad(p, inner))
                {
                    continue;
                }

                int x = (int)Math.Round(p.X), y = (int)Math.Round(p.Y);
                if (x < 0 || y < 0 || x >= gradients.Width || y >= gradients.Height)
                {
                    continue;
                }

                counted++;
                counter += distance[(y * gradients.Width) + x] <= gradients.DistanceThreshold ? 1 : 0;
            }
        }

        share = counted == 0 ? 0 : counter / (double)counted;
        // A ring a twentieth of the outline or more: a paper's thin white margin, as pale as the counter, is not a strip of counter.
        return counted >= 180 && counter >= 0.8 * counted;
    }

    /// <summary>Whether a point lies within <paramref name="margin"/> of a quadrilateral's outline.</summary>
    private static bool Near(PointD p, PointD[] q, double margin)
    {
        for (int i = 0; i < 4; i++)
        {
            PointD a = q[i], b = q[(i + 1) % 4];
            double dx = b.X - a.X, dy = b.Y - a.Y, t = Math.Clamp((((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / ((dx * dx) + (dy * dy)), 0, 1);
            if (Math.Sqrt(Math.Pow(p.X - (a.X + (t * dx)), 2) + Math.Pow(p.Y - (a.Y + (t * dy)), 2)) <= margin)
            {
                return true;
            }
        }

        return false;
    }

    private static bool InsideQuad(PointD p, PointD[] q)
    {
        int sign = 0;
        for (int i = 0; i < 4; i++)
        {
            PointD a = q[i], b = q[(i + 1) % 4];
            int s = Math.Sign(((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X)));
            if (s != 0 && sign != 0 && s != sign)
            {
                return false;
            }

            sign = s != 0 ? s : sign;
        }

        return true;
    }

    /// <summary>
    /// Whether the strip just inside a side along a scan's border is the scanner's blank lid: nearly white and without colour for most of its
    /// length, where a target's paper, printing or sticker tint would not be.
    /// </summary>
    private static bool Blank(PointD a, PointD b, PointD[] corners, Gradients gradients)
    {
        double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
        int blank = 0, counted = 0;
        for (int i = 0; i < 40; i++)
        {
            double t = 0.05 + (0.9 * i / 39.0);
            var at = new PointD(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
            double dx = cx - at.X, dy = cy - at.Y, d = Math.Sqrt((dx * dx) + (dy * dy));
            double inward = 0.04 * Math.Min(gradients.Width, gradients.Height);
            if (gradients.Colour(new PointD(at.X + (inward * dx / d), at.Y + (inward * dy / d))) is { } c)
            {
                counted++;
                double chroma = Math.Sqrt(Math.Pow((c[1] / Gradients.Weight(1)) - 128, 2) + Math.Pow((c[2] / Gradients.Weight(2)) - 128, 2));
                blank += c[0] > 225 && chroma < 4 ? 1 : 0;
            }
        }

        return counted > 0 && blank > 0.7 * counted;
    }

    /// <summary>
    /// Whether a side runs along the picture's border: both ends within two and a half percent of the same edge, where a scanner leaves the
    /// dark line of its own rim.
    /// </summary>
    private static bool OnBorder(PointD a, PointD b, int w, int h)
    {
        double dx = Math.Max(4, 0.025 * w), dy = Math.Max(4, 0.025 * h);
        return (a.X <= dx && b.X <= dx) || (a.Y <= dy && b.Y <= dy) || (a.X >= w - 1 - dx && b.X >= w - 1 - dx) || (a.Y >= h - 1 - dy && b.Y >= h - 1 - dy);
    }

    private static bool Inside(PointD p, int w, int h, int margin) => p.X >= margin && p.Y >= margin && p.X <= w - 1 - margin && p.Y <= h - 1 - margin;

    private static double Apart(double[] a, double[] b) => Math.Sqrt(Enumerable.Range(0, 3).Sum(c => (a[c] - b[c]) * (a[c] - b[c])));

    private static bool Near(double theta, double rho, double t, double r)
    {
        double dt = Math.Abs(theta - t);
        if (dt > Math.PI / 2)
        {
            dt = Math.PI - dt;
            r = -r;
        }

        return dt < 2.0 * Math.PI / 180 && Math.Abs(rho - r) < 5;
    }

    /// <summary>Every four lines making a convex quadrilateral inside the picture, scored.</summary>
    private static List<Candidate> Quads(List<Line> lines, Gradients gradients, int w, int h)
    {
        var pairs = new List<(int A, int B, double Angle)>();
        for (int i = 0; i < lines.Count; i++)
        {
            for (int j = i + 1; j < lines.Count; j++)
            {
                if (Angle(lines[i], lines[j]) < 25 && LineDistance(lines[j].Point, lines[i]) > 0.2 * Math.Min(w, h))
                {
                    pairs.Add((i, j, Math.Atan2(lines[i].Direction.Y, lines[i].Direction.X)));
                }
            }
        }

        var found = new List<Candidate>();
        double least = 0.08 * w * h;
        for (int p = 0; p < pairs.Count; p++)
        {
            for (int q = p + 1; q < pairs.Count; q++)
            {
                var (a, b, _) = pairs[p];
                var (c, d, _) = pairs[q];
                if (a == c || a == d || b == c || b == d || Angle(lines[a], lines[c]) < 55)
                {
                    continue;
                }

                var corners = new[] { Intersect(lines[a], lines[c]), Intersect(lines[c], lines[b]), Intersect(lines[b], lines[d]), Intersect(lines[d], lines[a]) };
                if (corners.Any(x => !double.IsFinite(x.X) || x.X < -2 || x.Y < -2 || x.X > w + 1 || x.Y > h + 1))
                {
                    continue;
                }

                var ordered = Order(corners);
                if (!Convex(ordered) || Area(ordered) < least)
                {
                    continue;
                }

                var quick = Score(ordered, gradients, w, h, "edges", 12);
                if (quick.Weakest < 0.4)
                {
                    continue;
                }

                found.Add(Score(ordered, gradients, w, h, "edges"));
            }
        }

        return found;
    }

    /// <summary>
    /// A rough outline brought onto the target's edge, a side at a time: every line near the side, a few percent of the picture either way and
    /// a few degrees round, is tried, and the side moves to the outermost of those that lie on an edge for nearly as much of their length as
    /// the best. Outermost, because the paper's edge lies outside any printed border.
    /// </summary>
    private static PointD[] Snap(PointD[] corners, Gradients gradients, int w, int h)
    {
        // Again from where it landed, up to three times, so a side that settled on a grid's outer line can carry on out to the paper's edge.
        for (int round = 0; round < 3; round++)
        {
            var next = SnapOnce(corners, gradients, w, h);
            bool still = next.Zip(corners).All(p => Math.Abs(p.First.X - p.Second.X) + Math.Abs(p.First.Y - p.Second.Y) < 1);
            corners = next;
            if (still)
            {
                break;
            }
        }

        return corners;
    }

    private static PointD[] SnapOnce(PointD[] corners, Gradients gradients, int w, int h)
    {
        double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
        int reach = Math.Max(6, (int)(0.08 * Math.Min(w, h)));
        var lines = new (PointD Point, PointD Direction)[4];
        for (int side = 0; side < 4; side++)
        {
            PointD a = corners[side], b = corners[(side + 1) % 4];
            double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            var along = new PointD((b.X - a.X) / length, (b.Y - a.Y) / length);
            if (gradients.Scan && OnBorder(a, b, w, h))
            {
                lines[side] = (a, along);
                continue;
            }

            var outward = new PointD(-along.Y, along.X);
            var middle = new PointD((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            if (((middle.X - cx) * outward.X) + ((middle.Y - cy) * outward.Y) < 0)
            {
                outward = new PointD(-outward.X, -outward.Y);
            }

            var tried = new List<(double Support, int Offset, PointD Point, PointD Direction)>();
            for (int degrees = -6; degrees <= 6; degrees++)
            {
                double turn = degrees * 0.5 * Math.PI / 180;
                var direction = new PointD((along.X * Math.Cos(turn)) - (along.Y * Math.Sin(turn)), (along.X * Math.Sin(turn)) + (along.Y * Math.Cos(turn)));
                var normal = new PointD(-direction.Y, direction.X);
                for (int offset = -reach; offset <= reach; offset++)
                {
                    var centre = new PointD(middle.X + (offset * outward.X), middle.Y + (offset * outward.Y));
                    var points = Enumerable.Range(0, 32)
                        .Select(i => -0.45 + (0.9 * i / 31.0))
                        .Select(t => new PointD(centre.X + (t * length * direction.X), centre.Y + (t * length * direction.Y)))
                        .ToList();
                    var (means, on) = Coherent(gradients, points, normal);
                    double support = on.Count(x => x) / (double)on.Length;

                    // A counter's edge, a shadow or a table's grain runs on past where the paper's corners would be; the paper's edge stops.
                    var past = new[] { -1.0, 1.0 }
                        .SelectMany(end => Enumerable.Range(1, 6).Select(i => end * (0.5 + (0.06 * i / 6.0))))
                        .Select(t => new PointD(centre.X + (t * length * direction.X), centre.Y + (t * length * direction.Y)))
                        .Where(p => Inside(p, w, h, 4))
                        .ToList();
                    if (support >= 0.3 && past.Count >= 4 && Coherent(gradients, past, normal, means).On.Count(x => x) > 0.6 * past.Count)
                    {
                        continue;
                    }

                    tried.Add((support, offset, centre, direction));
                }
            }

            double best = tried.Count == 0 ? 0 : tried.Max(t => t.Support);
            if (best < 0.3)
            {
                lines[side] = (middle, along);
                continue;
            }

            // The outermost line still sees the edge from a few pixels out, so among those near it the side takes the best supported, the
            // middle one where several tie: the edge's centre, not its outer reach.
            var eligible = tried.Where(t => t.Support >= Math.Max(0.5, 0.8 * best)).DefaultIfEmpty(tried.MaxBy(t => t.Support)).ToList();
            int outermost = eligible.Max(t => t.Offset);
            var near = eligible.Where(t => t.Offset >= outermost - 5).ToList();
            double top = near.Max(t => t.Support);
            var tied = near.Where(t => t.Support >= top - 1e-9).OrderBy(t => t.Offset).ToList();
            var chosen = tied[tied.Count / 2];
            lines[side] = (chosen.Point, chosen.Direction);
        }

        var snapped = Enumerable.Range(0, 4).Select(i => Intersect(lines[(i + 3) % 4], lines[i])).ToArray();
        return snapped.All(p => double.IsFinite(p.X) && double.IsFinite(p.Y)) && Convex(Order(snapped)) ? Order(snapped) : corners;
    }

    /// <summary>How much of each side lies on an edge, and how far the edges run on past the corners.</summary>
    private static Candidate Score(PointD[] corners, Gradients gradients, int w, int h, string method, int samples = 48)
    {
        // A target laid in a scanner's corner has two of the scan's edges for sides, or three where it fills the scan's width; four are the
        // scan itself.
        if (gradients.Scan && Enumerable.Range(0, 4).Count(i => OnBorder(corners[i], corners[(i + 1) % 4], w, h)) > 3)
        {
            return new Candidate(corners, 0, 1, Area(corners), method);
        }

        double weakest = 1, runOn = 0;
        for (int side = 0; side < 4; side++)
        {
            PointD a = corners[side], b = corners[(side + 1) % 4];
            double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            if (length < 1)
            {
                return new Candidate(corners, 0, 1, 0, method);
            }

            if (gradients.Scan && OnBorder(a, b, w, h))
            {
                // The target reaches this edge of the scan only where something other than the scanner's blank lid lies along it.
                if (Blank(a, b, corners, gradients))
                {
                    weakest = 0;
                }

                continue;
            }

            var along = new PointD((b.X - a.X) / length, (b.Y - a.Y) / length);
            var normal = new PointD(-along.Y, along.X);
            var points = Enumerable.Range(0, samples).Select(i => 0.04 + (0.92 * i / (samples - 1))).Select(t => new PointD(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t))).ToList();
            var (means, on) = Coherent(gradients, points, normal);
            weakest = Math.Min(weakest, on.Count(x => x) / (double)samples);


            // Past each end, a short way: a paper's edge stops at its corner.
            foreach (var (from, sign) in new[] { (a, -1.0), (b, 1.0) })
            {
                double reach = 0.08 * length;
                var past = Enumerable.Range(1, 8)
                    .Select(i => new PointD(from.X + (sign * along.X * reach * (0.25 + (0.75 * i / 8))), from.Y + (sign * along.Y * reach * (0.25 + (0.75 * i / 8)))))
                    .Where(p => p.X >= 4 && p.Y >= 4 && p.X <= w - 5 && p.Y <= h - 5)
                    .ToList();
                if (past.Count >= 4)
                {
                    var (_, still) = Coherent(gradients, past, normal, means);
                    runOn = Math.Max(runOn, still.Count(x => x) / (double)past.Count);
                }
            }
        }

        return new Candidate(corners, weakest, runOn, Area(corners), method);
    }

    /// <summary>Each side at full size: the strongest colour change across it, to a fraction of a pixel, and a line through those points.</summary>
    private static PointD[] Refine(Mat colour, PointD[] corners, int reach, double spacing)
    {
        using var lab = new Mat();
        Cv2.CvtColor(colour, lab, ColorConversionCodes.BGR2Lab);
        var lines = new (PointD Point, PointD Direction)[4];
        for (int side = 0; side < 4; side++)
        {
            PointD a = corners[side], b = corners[(side + 1) % 4];
            double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            var direction = new PointD((b.X - a.X) / length, (b.Y - a.Y) / length);
            var normal = new PointD(-direction.Y, direction.X);
            var points = new List<PointD>();
            const int samples = 80;
            for (int s = 0; s < samples; s++)
            {
                double t = 0.06 + (0.88 * s / (samples - 1));
                var at = new PointD(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
                // Each point of the profile averaged over five places along the side, a working pixel apart, so a phone's noise at full size
                // does not outweigh a faint edge.
                var profile = new double[(2 * reach) + 3][];
                bool inside = true;
                for (int o = -reach - 1; o <= reach + 1 && inside; o++)
                {
                    var sum = new double[3];
                    for (int along = -2; along <= 2 && inside; along++)
                    {
                        var v = Sample(lab, at.X + (o * normal.X) + (along * spacing * direction.X), at.Y + (o * normal.Y) + (along * spacing * direction.Y));
                        inside = v is not null;
                        for (int c = 0; c < 3 && inside; c++)
                        {
                            sum[c] += v![c] / 5;
                        }
                    }

                    profile[o + reach + 1] = sum;
                }

                if (!inside)
                {
                    continue;
                }

                var g = new double[profile.Length];
                for (int i = 1; i < profile.Length - 1; i++)
                {
                    double sum = 0;
                    for (int c = 0; c < 3; c++)
                    {
                        double d = (profile[i + 1][c] - profile[i - 1][c]) * (c == 0 ? 1 : 2.5);
                        sum += d * d;
                    }

                    g[i] = Math.Sqrt(sum);
                }

                int best = 1;
                for (int i = 2; i < g.Length - 1; i++)
                {
                    if (g[i] > g[best])
                    {
                        best = i;
                    }
                }

                if (best <= 1 || best >= g.Length - 2 || g[best] < 6)
                {
                    continue;
                }

                double denominator = g[best - 1] - (2 * g[best]) + g[best + 1];
                double shift = Math.Abs(denominator) < 1e-9 ? 0 : Math.Clamp(0.5 * (g[best - 1] - g[best + 1]) / denominator, -0.5, 0.5);
                double offset = best - reach - 1 + shift;
                points.Add(new PointD(at.X + (offset * normal.X), at.Y + (offset * normal.Y)));
            }

            if (points.Count < 12)
            {
                lines[side] = (a, direction);
                continue;
            }

            // Trim the points furthest from a first fit (a hanging hole, a curl at a corner), then fit again.
            var first = Fit(points);
            var kept = points.OrderBy(p => LineDistance(p, first)).Take(Math.Max(12, (int)(0.75 * points.Count))).ToList();
            lines[side] = Fit(kept);
        }

        return Enumerable.Range(0, 4).Select(i => Intersect(lines[(i + 3) % 4], lines[i])).ToArray();
    }

    private static double[]? Sample(Mat lab, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 >= lab.Width - 1 || y0 >= lab.Height - 1)
        {
            return null;
        }

        double fx = x - x0, fy = y - y0;
        var p00 = lab.At<Vec3b>(y0, x0);
        var p10 = lab.At<Vec3b>(y0, x0 + 1);
        var p01 = lab.At<Vec3b>(y0 + 1, x0);
        var p11 = lab.At<Vec3b>(y0 + 1, x0 + 1);
        var result = new double[3];
        for (int c = 0; c < 3; c++)
        {
            double top = p00[c] + ((p10[c] - p00[c]) * fx);
            double bottom = p01[c] + ((p11[c] - p01[c]) * fx);
            result[c] = top + ((bottom - top) * fy);
        }

        return result;
    }

    private static double Angle(Line a, Line b)
    {
        double dot = Math.Abs((a.Direction.X * b.Direction.X) + (a.Direction.Y * b.Direction.Y));
        return Math.Acos(Math.Min(1, dot)) * 180 / Math.PI;
    }

    private static (PointD Point, PointD Direction) Fit(IReadOnlyList<PointD> points)
    {
        double mx = points.Average(p => p.X), my = points.Average(p => p.Y), sxx = 0, sxy = 0, syy = 0;
        foreach (var p in points)
        {
            sxx += (p.X - mx) * (p.X - mx);
            sxy += (p.X - mx) * (p.Y - my);
            syy += (p.Y - my) * (p.Y - my);
        }

        double angle = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
        return (new PointD(mx, my), new PointD(Math.Cos(angle), Math.Sin(angle)));
    }

    private static double LineDistance(PointD p, Line line) => LineDistance(p, (line.Point, line.Direction));

    private static double LineDistance(PointD p, (PointD Point, PointD Direction) line) =>
        Math.Abs(((p.X - line.Point.X) * line.Direction.Y) - ((p.Y - line.Point.Y) * line.Direction.X));

    private static PointD Intersect(Line a, Line b) => Intersect((a.Point, a.Direction), (b.Point, b.Direction));

    private static PointD Intersect((PointD Point, PointD Direction) a, (PointD Point, PointD Direction) b)
    {
        double det = (a.Direction.X * b.Direction.Y) - (a.Direction.Y * b.Direction.X);
        if (Math.Abs(det) < 1e-9)
        {
            return new PointD(double.NaN, double.NaN);
        }

        double t = (((b.Point.X - a.Point.X) * b.Direction.Y) - ((b.Point.Y - a.Point.Y) * b.Direction.X)) / det;
        return new PointD(a.Point.X + (t * a.Direction.X), a.Point.Y + (t * a.Direction.Y));
    }

    /// <summary>Clockwise in the image from the corner nearest the top left.</summary>
    private static PointD[] Order(PointD[] corners)
    {
        double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
        var around = corners.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
        int first = around.IndexOf(around.MinBy(p => p.X + p.Y));
        return [.. Enumerable.Range(0, 4).Select(i => around[(first + i) % 4])];
    }

    /// <summary>Every corner between 60 and 120 degrees: what a flat sheet photographed from in front of it looks like.</summary>
    private static bool Squarish(PointD[] q)
    {
        for (int i = 0; i < 4; i++)
        {
            PointD a = q[(i + 3) % 4], b = q[i], c = q[(i + 1) % 4];
            double ux = a.X - b.X, uy = a.Y - b.Y, vx = c.X - b.X, vy = c.Y - b.Y;
            double angle = Math.Acos(Math.Clamp(((ux * vx) + (uy * vy)) / (Math.Sqrt((ux * ux) + (uy * uy)) * Math.Sqrt((vx * vx) + (vy * vy))), -1, 1)) * 180 / Math.PI;
            if (angle is < 60 or > 120)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Convex(PointD[] q)
    {
        int sign = 0;
        for (int i = 0; i < 4; i++)
        {
            PointD a = q[i], b = q[(i + 1) % 4], c = q[(i + 2) % 4];
            double cross = ((b.X - a.X) * (c.Y - b.Y)) - ((b.Y - a.Y) * (c.X - b.X));
            int s = Math.Sign(cross);
            if (s == 0 || (sign != 0 && s != sign))
            {
                return false;
            }

            sign = s;
        }

        return true;
    }

    private static double Area(PointD[] q)
    {
        double sum = 0;
        for (int i = 0; i < q.Length; i++)
        {
            sum += (q[i].X * q[(i + 1) % q.Length].Y) - (q[(i + 1) % q.Length].X * q[i].Y);
        }

        return Math.Abs(sum) / 2;
    }
}
