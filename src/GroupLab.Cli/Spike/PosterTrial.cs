using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab poster-trial &lt;out folder&gt;</c>, NOTES-FROM-PLANNING.md entry 344 section 1: what each scale source achieves on poster-sized
/// targets photographed the way a person would. Two synthetic posters drawn by GroupLab (12 by 18 and 23 by 35 inches: rings, numbers,
/// words and a silhouette, no maker's artwork) are hung on a wall beside the GroupLab sample sheet and photographed by a 4000 by 3000 camera
/// with a 26 mm equivalent lens, from far enough to hold both, tilted up to 30 degrees, with blur, noise and JPEG. Each photograph is
/// straightened by (a) the printed size typed, (b) the GroupLab sheet and (c) two taps three pixels astray and a typed distance, and the
/// target's true size is compared with what each made of it, beside the uncertainty each claimed. Then a fingerprint made from the first
/// photograph of each poster is tried on the others. Nothing it writes is committed.
/// </summary>
public static class PosterTrial
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const int Width = 4000, Height = 3000;

    private const double Focal = 2900, PosterDpi = 150;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Count < 1)
        {
            error.WriteLine("grouplab poster-trial <out folder> [--photos N] [--seed N]");
            return 2;
        }

        string folder = args[0];
        int photos = Option(args, "--photos", 10), seed = Option(args, "--seed", 344);
        Directory.CreateDirectory(folder);
        string sampleFile = Path.Combine("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
        using var sample = Cv2.ImRead(sampleFile, ImreadModes.Color);
        var (sampleGrey, _) = ImageLoader.Load(sampleFile);
        var library = SheetIdentification.Candidates([AnalyzeVerb.DefaultLibrary]);
        var definition = SheetIdentification.Identify(sampleGrey, library, new OpenCvSharpBackend(), new TraceRecorder()).Definition
            ?? throw new InvalidDataException("the sample sheet's codes did not name it");
        // The sample is a 600 dpi scan of the printed sheet, so it is placed on the wall at 600 pixels an inch, its true size.
        double sheetW = sample.Width / 600.0, sheetH = sample.Height / 600.0;

        // The sample sheet was printed a little small, as most are; its 600 dpi scan measures by how much, as a printer check would.
        var (sampleValue, sampleMeta) = ImageLoader.LoadMaxChannel(sampleFile);
        double printScale = AutomaticMarking.Run(sampleGrey, sampleValue, sampleMeta, definition, new OpenCvSharpBackend()).Scale?.PrintScale ?? 1;
        output.WriteLine(string.Create(Inv, $"The sample sheet printed at {100 * printScale:0.00} percent of its size; (b) is corrected by that, as a printer check would, and (b0) is not."));

        output.WriteLine("poster    photo  tilt  px/in | (a) printed size: error / claimed | (b) GroupLab sheet: error / claimed | (c) two taps: error / claimed");
        var rows = new List<(string Poster, string Source, double Error, double Claimed, bool Read)>();
        var rng = new Random(seed);
        foreach (var (pw, ph) in new[] { (12.0, 18.0), (23.0, 35.0) })
        {
            using var poster = Draw(pw, ph, rng);
            string tag = string.Create(Inv, $"{pw:0}x{ph:0}");
            TargetFingerprint? made = null;
            var tried = new List<string>();
            for (int k = 0; k < photos; k++)
            {
                var (photo, toImage, tilt, ppi) = Photograph(poster, pw, ph, sample, sheetW, sheetH, rng);
                string file = Path.Combine(folder, $"{tag}-{k:00}.jpg");
                Cv2.ImWrite(file, photo, new ImageEncodingParam(ImwriteFlags.JpegQuality, 88));
                photo.Dispose();
                var (grey, metadata) = ImageLoader.Load(file);
                var (value, _) = ImageLoader.LoadMaxChannel(file);
                PointD[] truth = [toImage.Apply(new(0, 0)), toImage.Apply(new(pw, 0)), toImage.Apply(new(pw, ph)), toImage.Apply(new(0, ph))];

                // The corners as GroupLab finds them, else as a person would tap them, about two pixels astray.
                var quad = SheetOutline.Find(grey, out _);
                bool found = quad is not null && quad.Corners.Zip(truth).All(p => Distance(p.First, p.Second) < 15);
                IReadOnlyList<PointD> corners = found ? quad!.Corners : [.. truth.Select(p => new PointD(p.X + Gauss(rng, 2), p.Y + Gauss(rng, 2)))];
                double cornerPx = found ? quad!.EdgeRmsPixels : 2;

                var a = TargetStraightening.FromPrintedSize(corners, pw, ph, cornerPx);
                var sheet = TargetReferenceMaker.ReadGroupLabSheet(grey, value, metadata, library, printScale, definition);
                var b = sheet is { } s ? TargetStraightening.FromGroupLabSheet(s.Plane, corners, s.ResidualInches, s.SheetInches, printerMeasured: true) : null;
                var raw = TargetReferenceMaker.ReadGroupLabSheet(grey, value, metadata, library, 1, definition);
                var b0 = raw is { } r ? TargetStraightening.FromGroupLabSheet(r.Plane, corners, r.ResidualInches, r.SheetInches, printerMeasured: false) : null;
                var t1 = toImage.Apply(new(pw * 0.1, ph * 0.1));
                var t2 = toImage.Apply(new(pw * 0.9, ph * 0.9));
                double tapped = Math.Sqrt(Math.Pow(pw * 0.8, 2) + Math.Pow(ph * 0.8, 2));
                var c = TargetStraightening.FromTwoPoints(corners, new(t1.X + Gauss(rng, TargetStraightening.TapPixels), t1.Y + Gauss(rng, TargetStraightening.TapPixels)),
                    new(t2.X + Gauss(rng, TargetStraightening.TapPixels), t2.Y + Gauss(rng, TargetStraightening.TapPixels)), tapped, grey.Width, grey.Height, metadata);

                // The error: the true corners taken through each plane, every side and diagonal against its true length.
                double Err(StraightenedTarget st)
                {
                    var q = truth.Select(st.Plane.ToInches).ToArray();
                    PointD[] p = [new(0, 0), new(pw, 0), new(pw, ph), new(0, ph)];
                    return new[] { (0, 1), (1, 2), (2, 3), (3, 0), (0, 2), (1, 3) }.Max(e => Math.Abs((Distance(q[e.Item1], q[e.Item2]) / Distance(p[e.Item1], p[e.Item2])) - 1));
                }
                rows.Add((tag, "a", Err(a), a.Uncertainty, found));
                if (b is not null && b0 is not null)
                {
                    rows.Add((tag, "b", Err(b), b.Uncertainty, true));
                    rows.Add((tag, "b0", Err(b0), b0.Uncertainty, true));
                }

                rows.Add((tag, "c", Err(c), c.Uncertainty, found));
                output.WriteLine(string.Create(Inv,
                    $"{tag,-8} {k,5} {tilt,5:0} {ppi,6:0} | {100 * Err(a),6:0.00}% / {100 * a.Uncertainty,5:0.00}%{(found ? "" : " (corners tapped)")} | {(b is null ? "   not read" : $"{100 * Err(b),6:0.00}% / {100 * b.Uncertainty,5:0.00}%")} | {100 * Err(c),6:0.00}% / {100 * c.Uncertainty,5:0.00}%"));

                // A fingerprint from the first photograph by (a), tried on the rest.
                if (k == 0)
                {
                    using var colour = Cv2.ImRead(file, ImreadModes.Color);
                    var (straight, dpi) = TargetReferenceMaker.Straighten(colour, a);
                    using (straight)
                    {
                        made = StoreFingerprintBuilder.Make("poster-" + tag, straight, dpi);
                        output.WriteLine(TargetReferenceMaker.Describe(TargetReferenceMaker.CheckFamily(straight, dpi, StoreTargetLibrary.Shipped)));
                    }
                }
                else if (made is not null)
                {
                    tried.Add(file);
                }
            }

            if (made is not null)
            {
                var reference = new TargetReference(new StoreTarget("poster-" + tag, "GroupLab", "trial poster {size}", tag, "", null), made, ScaleSource.PrintedSize, 0, "");
                StoreTargetLibrary.Install([reference]);
                var backend = new OpenCvFingerprintBackend();
                int named = 0;
                var scaleErrors = new List<double>();
                foreach (string file in tried)
                {
                    var seen = StoreTargetRecognizer.Recognize(file, backend)!;
                    if (seen.Named?.Target.Id != reference.Target.Id)
                    {
                        continue;
                    }

                    named++;
                    var scale = seen.Named.Scale();
                    var p0 = scale.ToTarget(seen.Named.ToImage.Apply(new(0, 0)));
                    var p1 = scale.ToTarget(seen.Named.ToImage.Apply(new(made.Layout.Width, 0)));
                    scaleErrors.Add(Math.Abs(Distance(p0, p1) / made.Layout.Width - 1));
                }

                StoreTargetLibrary.Uninstall();
                output.WriteLine(string.Create(Inv, $"{tag}: the fingerprint from photograph 0 named the poster in {named} of {tried.Count} other photographs ({made.Points.Count} features), its scale agreeing with the fingerprint's to a median {(scaleErrors.Count == 0 ? double.NaN : 100 * Pct([.. scaleErrors.Order()], 50)):0.00}%."));
            }
        }

        output.WriteLine();
        output.WriteLine("poster    source  photos  corners found  error median / 95th / largest  claimed median  within claim");
        foreach (var g in rows.GroupBy(r => (r.Poster, r.Source)).OrderBy(g => g.Key.Poster, StringComparer.Ordinal).ThenBy(g => g.Key.Source, StringComparer.Ordinal))
        {
            var e = g.Select(r => r.Error).Order().ToList();
            var u = g.Select(r => r.Claimed).Order().ToList();
            output.WriteLine(string.Create(Inv,
                $"{g.Key.Poster,-8}  {g.Key.Source,-6}  {e.Count,6}  {g.Count(r => r.Read),13}  {100 * Pct(e, 50),6:0.00}% / {100 * Pct(e, 95),5:0.00}% / {100 * e[^1],5:0.00}%  {100 * Pct(u, 50),12:0.00}%  {g.Count(r => r.Error <= r.Claimed),6} of {g.Count()}"));
        }

        return 0;
    }

    private static int Option(IReadOnlyList<string> args, string name, int fallback)
    {
        for (int i = 0; i + 1 < args.Count; i++)
        {
            if (args[i] == name)
            {
                return int.Parse(args[i + 1], Inv);
            }
        }

        return fallback;
    }

    private static double Pct(List<double> sorted, double p) => sorted[Math.Clamp((int)Math.Round(p / 100 * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    private static double Gauss(Random rng, double sigma)
    {
        double u1 = 1 - rng.NextDouble(), u2 = rng.NextDouble();
        return sigma * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    /// <summary>A poster GroupLab draws: a coloured ground, a border, bulls of rings with their numbers, words, a silhouette and a scoring grid.</summary>
    private static Mat Draw(double w, double h, Random rng)
    {
        int pw = (int)(w * PosterDpi), ph = (int)(h * PosterDpi);
        var grounds = new[] { new Scalar(235, 240, 245), new Scalar(200, 230, 245), new Scalar(190, 235, 200) };
        var poster = new Mat(ph, pw, MatType.CV_8UC3, grounds[rng.Next(grounds.Length)]);
        int px(double inches) => (int)Math.Round(inches * PosterDpi);
        Cv2.Rectangle(poster, new Rect(px(0.3), px(0.3), pw - px(0.6), ph - px(0.6)), Scalar.Black, px(0.08));

        // A silhouette: a head and shoulders, filled black, with a red centre.
        var body = new Point[] { new(px(w * 0.3), px(h * 0.95)), new(px(w * 0.32), px(h * 0.62)), new(px(w * 0.42), px(h * 0.55)), new(px(w * 0.58), px(h * 0.55)),
            new(px(w * 0.68), px(h * 0.62)), new(px(w * 0.7), px(h * 0.95)) };
        Cv2.FillPoly(poster, [body], new Scalar(40, 40, 40));
        Cv2.Circle(poster, new Point(px(w * 0.5), px(h * 0.45)), px(w * 0.11), new Scalar(40, 40, 40), -1, LineTypes.AntiAlias);

        // Bulls: rings with their numbers, one large and several small.
        int n = h > 20 ? 6 : 3;
        for (int i = 0; i < n; i++)
        {
            double r = i == 0 ? Math.Min(w, h) * 0.18 : Math.Min(w, h) * 0.07;
            double cx = i == 0 ? w * 0.5 : (0.15 + (0.7 * (i - 1) / Math.Max(1, n - 2))) * w;
            double cy = i == 0 ? h * 0.25 : h * 0.8;
            for (int ring = 5; ring >= 1; ring--)
            {
                Cv2.Circle(poster, new Point(px(cx), px(cy)), px(r * ring / 5), ring % 2 == 0 ? Scalar.White : Scalar.Black, -1, LineTypes.AntiAlias);
                Cv2.PutText(poster, (10 - ring).ToString(Inv), new Point(px(cx + (r * (ring - 0.6) / 5)), px(cy) + px(0.05)), HersheyFonts.HersheySimplex,
                    r * 0.6, ring % 2 == 0 ? Scalar.Black : Scalar.White, Math.Max(1, px(r * 0.01)), LineTypes.AntiAlias);
            }

            Cv2.Circle(poster, new Point(px(cx), px(cy)), px(r / 8), new Scalar(30, 30, 220), -1, LineTypes.AntiAlias);
        }

        // Words along the top and bottom, and a grid in one corner.
        string[] words = ["TACTICAL", "SIGHT-IN", "ZERO", "RANGE", "PRECISION", "100 YD", "SCORE", "PRACTICE"];
        for (int line = 0; line < 3; line++)
        {
            string text = string.Join(" ", Enumerable.Range(0, 3).Select(_ => words[rng.Next(words.Length)]));
            Cv2.PutText(poster, text, new Point(px(0.8), px(1.2 + (line * 0.7))), HersheyFonts.HersheyDuplex, w / 14, Scalar.Black, Math.Max(1, px(0.03)), LineTypes.AntiAlias);
        }

        for (double g = 0; g <= 3; g += 0.5)
        {
            Cv2.Line(poster, new Point(px(w - 3.8 + g), px(h - 4.2)), new Point(px(w - 3.8 + g), px(h - 1.2)), new Scalar(60, 60, 60), Math.Max(1, px(0.015)));
            Cv2.Line(poster, new Point(px(w - 3.8), px(h - 4.2 + g)), new Point(px(w - 0.8), px(h - 4.2 + g)), new Scalar(60, 60, 60), Math.Max(1, px(0.015)));
        }

        return poster;
    }

    /// <summary>
    /// A photograph of the poster on a wall, the GroupLab sample sheet to its right on the same wall, from far enough to hold both with a
    /// margin, tilted up to 30 degrees: the picture, the homography from poster inches to its pixels, the tilt and the poster's pixels an inch.
    /// </summary>
    private static (Mat Photo, Homography ToImage, double Tilt, double Ppi) Photograph(Mat poster, double pw, double ph, Mat sample, double sw, double sh, Random rng)
    {
        // The scene in wall inches: the poster at the origin, the sheet 2 in to its right, its middle level with the poster's.
        double sx = pw + 2, sy = (ph / 2) - (sh / 2);
        double sceneW = sx + sw, sceneH = ph;
        double cx = sceneW / 2, cy = sceneH / 2;
        double tilt = rng.NextDouble() * 30 * Math.PI / 180, axis = rng.NextDouble() * 2 * Math.PI, roll = (rng.NextDouble() - 0.5) * 6 * Math.PI / 180;
        double fill = 0.72 + (rng.NextDouble() * 0.12);
        double distance = Focal * Math.Max(sceneW / (Width * fill), sceneH / (Height * fill)) * (1 + (0.4 * Math.Sin(tilt)));
        double[] rot = Multiply(Rodrigues(Math.Cos(axis) * tilt, Math.Sin(axis) * tilt), RotZ(roll));
        double[] t = [-(rot[0] * cx) - (rot[1] * cy), -(rot[3] * cx) - (rot[4] * cy), distance - (rot[6] * cx) - (rot[7] * cy)];
        double[] k = [Focal, 0, Width / 2.0, 0, Focal, Height / 2.0, 0, 0, 1];
        double[] wallToImage = Multiply(k, [rot[0], rot[1], t[0], rot[3], rot[4], t[1], rot[6], rot[7], t[2]]);

        var photo = new Mat(Height, Width, MatType.CV_8UC3, new Scalar(70 + rng.Next(40), 80 + rng.Next(40), 90 + rng.Next(40)));
        using (var noise = new Mat(photo.Size(), MatType.CV_8UC3))
        {
            Cv2.Randn(noise, Scalar.All(0), Scalar.All(6));
            Cv2.Add(photo, noise, photo);
        }

        Place(photo, poster, PosterDpi, wallToImage);
        const double sampleDpi = 600;
        Place(photo, sample, sampleDpi, Multiply(wallToImage, [1, 0, sx, 0, 1, sy, 0, 0, 1]));
        Cv2.GaussianBlur(photo, photo, new Size(0, 0), 0.6 + (rng.NextDouble() * 0.6));
        using (var noise = new Mat(photo.Size(), MatType.CV_16SC3))
        {
            Cv2.Randn(noise, Scalar.All(0), Scalar.All(3));
            using var wide = new Mat();
            photo.ConvertTo(wide, MatType.CV_16SC3);
            Cv2.Add(wide, noise, wide);
            wide.ConvertTo(photo, MatType.CV_8UC3);
        }

        var toImage = new Homography(wallToImage);
        var (xx, xy, yx, yy) = toImage.Jacobian(new PointD(pw / 2, ph / 2));
        return (photo, toImage, tilt * 180 / Math.PI, Math.Sqrt(Math.Abs((xx * yy) - (xy * yx))));
    }

    /// <summary>A flat picture at <paramref name="dpi"/> warped onto the photograph, its own inches taken by <paramref name="inchesToImage"/>.</summary>
    private static void Place(Mat photo, Mat picture, double dpi, double[] inchesToImage)
    {
        double[] h = Multiply(inchesToImage, [1 / dpi, 0, 0, 0, 1 / dpi, 0, 0, 0, 1]);
        double scale = Math.Sqrt(Math.Abs((h[0] * h[4]) - (h[1] * h[3])));
        using var small = new Mat();
        double f = Math.Min(1, 2 * scale);
        Cv2.Resize(picture, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        double[] hs = Multiply(h, [1 / f, 0, 0, 0, 1 / f, 0, 0, 0, 1]);
        using var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, hs);
        Cv2.WarpPerspective(small, photo, hm, photo.Size(), InterpolationFlags.Linear, BorderTypes.Transparent);
    }

    private static double[] Rodrigues(double rx, double ry)
    {
        double theta = Math.Sqrt((rx * rx) + (ry * ry));
        if (theta < 1e-12)
        {
            return [1, 0, 0, 0, 1, 0, 0, 0, 1];
        }

        double kx = rx / theta, ky = ry / theta, c = Math.Cos(theta), s = Math.Sin(theta), v = 1 - c;
        return [c + (kx * kx * v), kx * ky * v, ky * s, kx * ky * v, c + (ky * ky * v), -kx * s, -ky * s, kx * s, c];
    }

    private static double[] RotZ(double a) => [Math.Cos(a), -Math.Sin(a), 0, Math.Sin(a), Math.Cos(a), 0, 0, 0, 1];

    private static double[] Multiply(double[] a, double[] b)
    {
        var r = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                r[(i * 3) + j] = (a[i * 3] * b[j]) + (a[(i * 3) + 1] * b[3 + j]) + (a[(i * 3) + 2] * b[6 + j]);
            }
        }

        return r;
    }
}
