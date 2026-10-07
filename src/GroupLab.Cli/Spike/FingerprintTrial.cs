using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab fingerprint-trial</c>, NOTES-FROM-PLANNING.md entry 332: a trial of recognising a store-bought target, and its scale, from a
/// fingerprint made from one 600 dpi scan of the blank. Nothing here is reached from any screen and nothing it writes is committed: the
/// scans are another maker's printing (request 58), so the blanks, the pictures made from them and the fingerprints stay on this computer.
/// Three steps, run in order: <c>build</c> makes a fingerprint of each blank with each feature method, <c>make</c> writes the test pictures
/// (phone views, flatbed scans, synthetic holes, halos and pasters, blank walls) with the true transform of each, and <c>match</c> runs
/// recognition and registration over them and over the real negatives, and prints sizes, rates, errors, times and memory. A fourth,
/// <c>shipped</c>, runs the application's own recognizer and fingerprints over the same pictures, for question 87.
/// </summary>
public static class FingerprintTrial
{
    public const string Usage =
        "grouplab fingerprint-trial build <blanks folder> <out> | make <blanks folder> <out> [--seed N] [--phone N] [--scan N] [--walls N] " +
        "[--sheet <png>] [--real <folder>]... | match <out> --method orb|akaze [--side N] [--ratio R]";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The resolution the reference features are found at; their positions are kept in inches.</summary>
    private const double ReferenceDpi = 100;

    /// <summary>The most features one fingerprint keeps, spread over the target in half inch cells.</summary>
    private const int FingerprintFeatures = 1500;

    /// <summary>A phone picture: 4000 by 3000 with a focal length of about a 26 mm equivalent lens.</summary>
    private const int PhoneWidth = 4000, PhoneHeight = 3000;

    private const double PhoneFocal = 2900;

    /// <summary>The phone's working copy, <c>WorkingSize.PhoneMegapixels</c>.</summary>
    private const double WorkingMegapixels = 8;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        try
        {
            string[] a = [.. args];
            return a switch
            {
                ["build", var blanks, var outFolder] => Build(blanks, outFolder, output),
                ["make", var blanks, var outFolder, .. var rest] => Make(blanks, outFolder, rest, output),
                ["match", var outFolder, .. var rest] => Match(outFolder, rest, output),
                ["shipped", var outFolder, .. var rest] => Shipped(outFolder, rest, output),
                _ => Fail(error),
            };
        }
        catch (Exception e) when (e is IOException or OpenCVException or JsonException or FormatException)
        {
            error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Fail(TextWriter error)
    {
        error.WriteLine(Usage);
        return 2;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // The fingerprint

    private sealed record Fingerprint(string Product, string Method, double Dpi, Point2d[] Points, Mat Descriptors, Point2d[] Bulls, float[] Signature, Layout Layout);

    /// <summary>
    /// The colour layout: the mean colour of each half inch cell of the printing, in Lab, two points an inch. It is what tells apart two
    /// products whose features agree (a black disc on chartreuse looks the same at 6 and 8 inches once scaled); no usable picture can be
    /// made from it.
    /// </summary>
    private sealed record Layout(double X0, double Y0, int Cols, int Rows, byte[] Lab);

    private const double LayoutCell = 0.5;

    private static IEnumerable<string> Products(string blanks) =>
        Directory.EnumerateDirectories(blanks).Where(d => File.Exists(Path.Combine(d, "blank.png"))).Order(StringComparer.Ordinal);

    private static Feature2D Detector(string method, int features) => method switch
    {
        "orb" => ORB.Create(features, 1.2f, 8, 31, 0, 2, ORBScoreType.Harris, 31, 12),
        "akaze" => AKAZE.Create(AKAZEDescriptorType.MLDB, 0, 3, 0.0008f),
        _ => throw new FormatException($"unknown method {method}"),
    };

    private static int Build(string blanks, string outFolder, TextWriter output)
    {
        foreach (string method in new[] { "orb", "akaze" })
        {
            string folder = Path.Combine(outFolder, "fingerprints", method);
            Directory.CreateDirectory(folder);
            foreach (string dir in Products(blanks))
            {
                string product = Path.GetFileName(dir);
                var watch = Stopwatch.StartNew();
                using var blank = Cv2.ImRead(Path.Combine(dir, "blank.png"), ImreadModes.Color);
                var fp = MakeFingerprint(product, method, blank);
                byte[] raw = Serialise(fp);
                byte[] packed = Gzip(raw);
                File.WriteAllBytes(Path.Combine(folder, product + ".glfp"), raw);
                output.WriteLine(string.Create(Inv,
                    $"{method} {product}: {fp.Points.Length} features, {fp.Bulls.Length} bulls, {raw.Length / 1024.0:0.0} KB raw, {packed.Length / 1024.0:0.0} KB gzip, built in {watch.ElapsedMilliseconds} ms"));
                if (method == "orb")
                {
                    output.WriteLine("   bulls (in): " + string.Join("  ", fp.Bulls.Select(b => string.Create(Inv, $"({b.X:0.000}, {b.Y:0.000})"))));
                    Preview(blank, fp, Path.Combine(outFolder, "preview-" + product + ".png"));
                }

                fp.Descriptors.Dispose();
            }
        }

        return 0;
    }

    private static Fingerprint MakeFingerprint(string product, string method, Mat blank)
    {
        // Two resolutions, so a target seen from three feet, at a third of the detail, still finds features made at its own scale.
        var points = new List<Point2d>();
        var parts = new List<Mat>();
        float[] signature = [];
        using var detector = Detector(method, 20000);
        foreach (var (dpi, most) in new[] { (ReferenceDpi, FingerprintFeatures), (ReferenceDpi / 2, FingerprintFeatures / 2) })
        {
            using var small = new Mat();
            Cv2.Resize(blank, small, new Size(0, 0), dpi / 600, dpi / 600, InterpolationFlags.Area);
            if (signature.Length == 0)
            {
                signature = Signature(small);
            }

            using var gray = new Mat();
            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);

            // Specks of dust and toner on the scan are not on the next sheet: a slight blur, and a floor under the feature strength.
            Cv2.GaussianBlur(gray, gray, new Size(0, 0), 0.8);
            var all = detector.Detect(gray);
            var kept = Spread(all, dpi / 2, most);
            var part = new Mat();
            detector.Compute(gray, ref kept, part);
            parts.Add(part);
            points.AddRange(kept.Select(k => new Point2d(k.Pt.X / dpi, k.Pt.Y / dpi)));
        }

        var descriptors = new Mat();
        Cv2.VConcat(parts.ToArray(), descriptors);
        foreach (var part in parts)
        {
            part.Dispose();
        }

        return new Fingerprint(product, method, ReferenceDpi, [.. points], descriptors, Bulls(blank), signature, MakeLayout(blank));
    }

    /// <summary>The layout is read from the target made smooth at eight points an inch, so a fine line falling on a cell's edge cannot swing it.</summary>
    private const double LayoutDpi = 8, LayoutSigma = 1.2;

    private static Layout MakeLayout(Mat blank)
    {
        var box = ContentBox(blank);
        int cols = Math.Max(1, (int)Math.Round(box.Width / LayoutCell)), rows = Math.Max(1, (int)Math.Round(box.Height / LayoutCell));
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), LayoutDpi / 600, LayoutDpi / 600, InterpolationFlags.Area);
        using var lab = new Mat();
        Cv2.CvtColor(small, lab, ColorConversionCodes.BGR2Lab);
        Cv2.GaussianBlur(lab, lab, new Size(0, 0), LayoutSigma);
        var values = new byte[cols * rows * 3];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var v = Sample(lab, (box.X + ((col + 0.5) * LayoutCell)) * LayoutDpi, (box.Y + ((row + 0.5) * LayoutCell)) * LayoutDpi);
                int k = ((row * cols) + col) * 3;
                values[k] = (byte)Math.Clamp(v.Item0, 0, 255);
                values[k + 1] = (byte)Math.Clamp(v.Item1, 0, 255);
                values[k + 2] = (byte)Math.Clamp(v.Item2, 0, 255);
            }
        }

        return new Layout(box.X, box.Y, cols, rows, values);
    }

    private static Vec3d Sample(Mat lab, double x, double y)
    {
        int x0 = Math.Clamp((int)Math.Floor(x - 0.5), 0, lab.Width - 1), y0 = Math.Clamp((int)Math.Floor(y - 0.5), 0, lab.Height - 1);
        int x1 = Math.Min(x0 + 1, lab.Width - 1), y1 = Math.Min(y0 + 1, lab.Height - 1);
        double fx = Math.Clamp(x - 0.5 - x0, 0, 1), fy = Math.Clamp(y - 0.5 - y0, 0, 1);
        var a = lab.At<Vec3b>(y0, x0);
        var b = lab.At<Vec3b>(y0, x1);
        var c = lab.At<Vec3b>(y1, x0);
        var d = lab.At<Vec3b>(y1, x1);
        double Mix(byte p, byte q, byte r, byte t) => (p * (1 - fx) * (1 - fy)) + (q * fx * (1 - fy)) + (r * (1 - fx) * fy) + (t * fx * fy);
        return new Vec3d(Mix(a.Item0, b.Item0, c.Item0, d.Item0), Mix(a.Item1, b.Item1, c.Item1, d.Item1), Mix(a.Item2, b.Item2, c.Item2, d.Item2));
    }

    /// <summary>
    /// How well the picture, read through the fitted homography, has the fingerprint's colour layout: the correlation of each Lab channel
    /// over the cells in view, weighted by how much that channel varies on the target. One for a perfect match, near zero for no relation.
    /// The picture is first brought to about eight points an inch of the target and smoothed the same way as the fingerprint was.
    /// </summary>
    private static (double Full, double Trimmed) LayoutScore(Layout layout, double[] h, Mat lab, int w, int ht)
    {
        double cx = layout.X0 + (layout.Cols * LayoutCell / 2), cy = layout.Y0 + (layout.Rows * LayoutCell / 2);
        var c0 = Apply(h, new Point2d(cx, cy));
        var c1 = Apply(h, new Point2d(cx + 1, cy));
        var c2 = Apply(h, new Point2d(cx, cy + 1));
        double scale = Math.Sqrt(Math.Abs(((c1.X - c0.X) * (c2.Y - c0.Y)) - ((c1.Y - c0.Y) * (c2.X - c0.X))));
        double f = Math.Min(1, LayoutDpi / Math.Max(1e-6, scale));
        using var small = new Mat();
        Cv2.Resize(lab, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        Cv2.GaussianBlur(small, small, new Size(0, 0), LayoutSigma * Math.Min(1, scale * f / LayoutDpi));
        var r = new List<double>[3];
        var q = new List<double>[3];
        for (int c = 0; c < 3; c++)
        {
            r[c] = [];
            q[c] = [];
        }

        for (int row = 0; row < layout.Rows; row++)
        {
            for (int col = 0; col < layout.Cols; col++)
            {
                var p = Apply(h, new Point2d(layout.X0 + ((col + 0.5) * LayoutCell), layout.Y0 + ((row + 0.5) * LayoutCell)));
                if (p.X < 2 || p.Y < 2 || p.X > w - 2 || p.Y > ht - 2)
                {
                    continue;
                }

                var v = Sample(small, p.X * f, p.Y * f);
                int k = ((row * layout.Cols) + col) * 3;
                r[0].Add(layout.Lab[k]);
                r[1].Add(layout.Lab[k + 1]);
                r[2].Add(layout.Lab[k + 2]);
                q[0].Add(v.Item0);
                q[1].Add(v.Item1);
                q[2].Add(v.Item2);
            }
        }

        if (r[0].Count < 12)
        {
            return (0, 0);
        }

        // Shot holes, halos and pasters change a few cells a lot; on a target that is nearly all black (the sight-in) those few cells
        // would carry the whole correlation. So the fifth of the cells that disagree most are set aside and the correlation taken again.
        var keep = Enumerable.Range(0, r[0].Count).ToList();
        double full = Correlation(r, q, keep, out var residual);
        int drop = keep.Count / 5;
        keep = [.. keep.OrderBy(i => residual[i]).Take(keep.Count - drop)];
        return (full, Correlation(r, q, keep, out _));
    }

    private static double Correlation(List<double>[] r, List<double>[] q, List<int> keep, out double[] residual)
    {
        residual = new double[r[0].Count];
        double sum = 0, weights = 0;
        for (int c = 0; c < 3; c++)
        {
            double mr = keep.Average(i => r[c][i]), mq = keep.Average(i => q[c][i]);
            double cov = 0, vr = 0, vq = 0;
            foreach (int i in keep)
            {
                cov += (r[c][i] - mr) * (q[c][i] - mq);
                vr += (r[c][i] - mr) * (r[c][i] - mr);
                vq += (q[c][i] - mq) * (q[c][i] - mq);
            }

            double weight = Math.Sqrt(vr / keep.Count);
            sum += vq <= 1e-9 || vr <= 1e-9 ? 0 : weight * cov / Math.Sqrt(vr * vq);
            weights += weight;
            double sr = Math.Sqrt(vr / keep.Count), sq = Math.Sqrt(vq / keep.Count);
            for (int i = 0; i < residual.Length; i++)
            {
                if (sr > 1e-9 && sq > 1e-9)
                {
                    residual[i] += weight * Math.Abs(((q[c][i] - mq) / sq) - ((r[c][i] - mr) / sr));
                }
            }
        }

        return weights <= 0 ? 0 : sum / weights;
    }

    /// <summary>The strongest features, at most a share of them in any one cell, so the text alone cannot carry a match.</summary>
    private static KeyPoint[] Spread(KeyPoint[] all, double cell, int most)
    {
        var strongest = all.Select(k => k.Response).OrderDescending().Take(500).ToList();
        float floor = strongest.Count == 0 ? 0 : 0.1f * strongest[strongest.Count / 2];
        var byCell = all.Where(k => k.Response >= floor).GroupBy(k => ((int)(k.Pt.X / cell), (int)(k.Pt.Y / cell)))
            .Select(g => g.OrderByDescending(k => k.Response).ToList()).ToList();
        int cap = 1;
        while (cap < 400 && byCell.Sum(c => Math.Min(c.Count, cap + 1)) <= most)
        {
            cap++;
        }

        return [.. byCell.SelectMany(c => c.Take(cap)).OrderByDescending(k => k.Response).Take(most)];
    }

    /// <summary>
    /// The aim points: red marks at least a tenth of an inch across, compact rather than long (the crosshair's arms are long), ringed by
    /// black ink and at least an inch inside the printing, which leaves out the repair pasters in the corners. Found at 150 dpi; in inches.
    /// </summary>
    private static Point2d[] Bulls(Mat blank)
    {
        const double dpi = 150;
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), dpi / 600, dpi / 600, InterpolationFlags.Area);
        var ch = Cv2.Split(small);
        using var b = ch[0];
        using var g = ch[1];
        using var r = ch[2];
        using var rg = new Mat();
        using var rb = new Mat();
        Cv2.Subtract(r, g, rg);
        Cv2.Subtract(r, b, rb);
        using var m1 = new Mat();
        using var m2 = new Mat();
        using var m3 = new Mat();
        Cv2.Threshold(r, m1, 130, 255, ThresholdTypes.Binary);
        Cv2.Threshold(rg, m2, 70, 255, ThresholdTypes.Binary);
        Cv2.Threshold(rb, m3, 70, 255, ThresholdTypes.Binary);
        using var red = new Mat();
        Cv2.BitwiseAnd(m1, m2, red);
        Cv2.BitwiseAnd(red, m3, red);

        // Thin red lines (the crosshair's hairlines) would join the centre to the arms; an opening of about 0.07 in takes them away.
        using (var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(11, 11)))
        {
            Cv2.MorphologyEx(red, red, MorphTypes.Open, kernel);
        }

        var box = ContentBox(blank);
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        byte[] gp = Bytes(gray);
        int w = gray.Width, h = gray.Height;
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();
        int n = Cv2.ConnectedComponentsWithStats(red, labels, stats, centroids);
        var bulls = new List<Point2d>();
        for (int i = 1; i < n; i++)
        {
            int bw = stats.At<int>(i, 2), bh = stats.At<int>(i, 3), area = stats.At<int>(i, 4);
            if (area < 0.1 * 0.1 * dpi * dpi || Math.Max(bw, bh) > 1.6 * Math.Min(bw, bh) || area < 0.35 * bw * bh)
            {
                continue;
            }

            double cx = centroids.At<double>(i, 0), cy = centroids.At<double>(i, 1);

            // Repair pasters sit in the sheet's corners and edges; an aim point is at least an inch inside the printing.
            double ix = cx / dpi, iy = cy / dpi;
            if (ix < box.X + 1 || iy < box.Y + 1 || ix > box.X + box.Width - 1 || iy > box.Y + box.Height - 1)
            {
                continue;
            }

            bool ringed = new[] { 1.5, 2.2 }.Any(f =>
            {
                double radius = f * Math.Max(bw, bh) / 2;
                int dark = 0, seen = 0;
                for (int k = 0; k < 90; k++)
                {
                    int x = (int)Math.Round(cx + (radius * Math.Cos(k * Math.PI / 45)));
                    int y = (int)Math.Round(cy + (radius * Math.Sin(k * Math.PI / 45)));
                    if (x < 0 || y < 0 || x >= w || y >= h)
                    {
                        continue;
                    }

                    seen++;
                    dark += gp[(y * w) + x] < 70 ? 1 : 0;
                }

                return seen > 60 && dark >= 0.55 * seen;
            });
            if (ringed)
            {
                bulls.Add(new Point2d(cx / dpi, cy / dpi));
            }
        }

        return [.. bulls.OrderBy(p => p.Y).ThenBy(p => p.X)];
    }

    /// <summary>
    /// The cheap global signature for shortlisting: the share of the target's non-white pixels that are dark, grey, or in each of twelve
    /// hue bins. Fourteen numbers; no picture can be made from it.
    /// </summary>
    private static float[] Signature(Mat bgr)
    {
        using var small = new Mat();
        double f = Math.Min(1, 400.0 / Math.Max(bgr.Width, bgr.Height));
        Cv2.Resize(bgr, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        using var hsv = new Mat();
        Cv2.CvtColor(small, hsv, ColorConversionCodes.BGR2HSV);
        byte[] px = Bytes(hsv);
        var bins = new float[14];
        int total = 0;
        for (int i = 0; i + 2 < px.Length; i += 3)
        {
            int hue = px[i], sat = px[i + 1], val = px[i + 2];
            if (val > 190 && sat < 45)
            {
                continue;
            }

            total++;
            int bin = val < 60 ? 0 : sat < 50 ? 1 : 2 + Math.Min(11, hue / 15);
            bins[bin]++;
        }

        for (int k = 0; k < bins.Length; k++)
        {
            bins[k] /= Math.Max(1, total);
        }

        return bins;
    }

    private static double Intersection(float[] a, float[] b) => a.Zip(b, Math.Min).Sum();

    private static byte[] Serialise(Fingerprint fp)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        w.Write("GLFP1");
        w.Write(fp.Product);
        w.Write(fp.Method);
        w.Write(fp.Dpi);
        w.Write(fp.Bulls.Length);
        foreach (var b in fp.Bulls)
        {
            w.Write((float)b.X);
            w.Write((float)b.Y);
        }

        foreach (float s in fp.Signature)
        {
            w.Write(s);
        }

        w.Write(fp.Layout.X0);
        w.Write(fp.Layout.Y0);
        w.Write(fp.Layout.Cols);
        w.Write(fp.Layout.Rows);
        w.Write(fp.Layout.Lab);
        w.Write(fp.Points.Length);
        w.Write(fp.Descriptors.Cols);
        byte[] desc = Bytes(fp.Descriptors);
        for (int i = 0; i < fp.Points.Length; i++)
        {
            w.Write((float)fp.Points[i].X);
            w.Write((float)fp.Points[i].Y);
            w.Write(desc, i * fp.Descriptors.Cols, fp.Descriptors.Cols);
        }

        w.Flush();
        return ms.ToArray();
    }

    private static Fingerprint Load(string file)
    {
        using var r = new BinaryReader(File.OpenRead(file));
        if (r.ReadString() != "GLFP1")
        {
            throw new FormatException($"{file} is not a fingerprint");
        }

        string product = r.ReadString(), method = r.ReadString();
        double dpi = r.ReadDouble();
        var bulls = new Point2d[r.ReadInt32()];
        for (int i = 0; i < bulls.Length; i++)
        {
            bulls[i] = new Point2d(r.ReadSingle(), r.ReadSingle());
        }

        var sig = new float[14];
        for (int i = 0; i < sig.Length; i++)
        {
            sig[i] = r.ReadSingle();
        }

        double lx = r.ReadDouble(), ly = r.ReadDouble();
        int lc = r.ReadInt32(), lr = r.ReadInt32();
        var layout = new Layout(lx, ly, lc, lr, r.ReadBytes(lc * lr * 3));
        int n = r.ReadInt32(), cols = r.ReadInt32();
        var points = new Point2d[n];
        var desc = new byte[n * cols];
        for (int i = 0; i < n; i++)
        {
            points[i] = new Point2d(r.ReadSingle(), r.ReadSingle());
            r.Read(desc, i * cols, cols);
        }

        var mat = new Mat(n, cols, MatType.CV_8UC1);
        Marshal.Copy(desc, 0, mat.Data, desc.Length);
        return new Fingerprint(product, method, dpi, points, mat, bulls, sig, layout);
    }

    private static byte[] Gzip(byte[] raw)
    {
        using var ms = new MemoryStream();
        using (var z = new GZipStream(ms, CompressionLevel.SmallestSize))
        {
            z.Write(raw);
        }

        return ms.ToArray();
    }

    private static byte[] Bytes(Mat mat)
    {
        using var copy = mat.IsContinuous() ? null : mat.Clone();
        var m = copy ?? mat;
        var bytes = new byte[m.Total() * m.ElemSize()];
        Marshal.Copy(m.Data, bytes, 0, bytes.Length);
        return bytes;
    }

    private static void Preview(Mat blank, Fingerprint fp, string file)
    {
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), 0.1, 0.1, InterpolationFlags.Area);
        foreach (var p in fp.Points)
        {
            Cv2.Circle(small, new Point(p.X * 60, p.Y * 60), 1, Scalar.Blue, -1);
        }

        foreach (var b in fp.Bulls)
        {
            Cv2.Circle(small, new Point(b.X * 60, b.Y * 60), 8, Scalar.Cyan, 2);
        }

        Cv2.ImWrite(file, small);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // The test pictures

    private sealed class Rng(int seed)
    {
        private readonly Random random = new(seed);

        public double U(double a, double b) => a + (random.NextDouble() * (b - a));

        public int I(int a, int b) => random.Next(a, b);

        public bool P(double p) => random.NextDouble() < p;

        public double N()
        {
            double u1 = 1 - random.NextDouble(), u2 = random.NextDouble();
            return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
        }
    }

    private static int Make(string blanks, string outFolder, IReadOnlyList<string> rest, TextWriter output)
    {
        int seed = IntOption(rest, "--seed", 332), phones = IntOption(rest, "--phone", 30), scans = IntOption(rest, "--scan", 10);
        int walls = IntOption(rest, "--walls", 30);
        string? sheet = Option(rest, "--sheet");
        var real = new List<string>();
        for (int i = 0; i + 1 < rest.Count; i++)
        {
            if (rest[i] == "--real")
            {
                real.Add(rest[i + 1]);
            }
        }

        var rng = new Rng(seed);
        string pictures = Path.Combine(outFolder, "pictures");
        Directory.CreateDirectory(pictures);
        var manifest = new JsonArray();
        int made = 0;
        var watch = Stopwatch.StartNew();

        var sources = Products(blanks).Select(d => (Product: Path.GetFileName(d), File: Path.Combine(d, "blank.png"))).ToList();
        if (sheet is not null)
        {
            sources.Add((Product: "", File: sheet));
        }

        foreach (var (product, file) in sources)
        {
            using var blank = Cv2.ImRead(file, ImreadModes.Color);
            bool shootNC = product.Contains("shoot-n-c", StringComparison.Ordinal);
            var bulls = product.Length > 0 ? Bulls(blank) : [new Point2d(blank.Width / 1200.0, blank.Height / 1200.0)];
            var box = ContentBox(blank);
            int nPhone = product.Length > 0 ? phones : Math.Max(4, phones / 3);
            int nScan = product.Length > 0 ? scans : 2;
            for (int k = 0; k < nPhone + nScan; k++)
            {
                bool isScan = k >= nPhone;
                bool holes = product.Length > 0 && (isScan ? rng.P(0.5) : rng.P(0.75));
                using var marked = blank.Clone();
                string note = holes ? Shoot(marked, bulls, shootNC, rng) : "no holes";
                var (picture, h, conditions) = isScan ? Scan(marked, box, rng) : Phone(marked, box, rng);
                using (picture)
                {
                    string id = $"{(product.Length > 0 ? product : "grouplab-sheet")}-{(isScan ? "scan" : "phone")}-{k:00}";
                    string name = id + ".jpg";
                    int quality = isScan ? 92 : rng.I(60, 93);
                    Cv2.ImWrite(Path.Combine(pictures, name), picture, new ImageEncodingParam(ImwriteFlags.JpegQuality, quality));
                    manifest.Add(new JsonObject
                    {
                        ["id"] = id,
                        ["file"] = Path.Combine("pictures", name),
                        ["kind"] = product.Length > 0 ? (isScan ? "scan" : "phone") : "grouplab-sheet",
                        ["product"] = product.Length > 0 ? product : null,
                        ["h"] = new JsonArray([.. h.Select(v => (JsonNode)JsonValue.Create(v))]),
                        ["conditions"] = string.Create(Inv, $"{conditions}; {note}; jpeg {quality}"),
                    });
                    made++;
                }
            }
        }

        for (int k = 0; k < walls; k++)
        {
            using var wall = Wall(PhoneWidth, PhoneHeight, rng, out string kind);
            Finish(wall, rng, out string finish);
            string id = $"wall-{k:00}";
            Cv2.ImWrite(Path.Combine(pictures, id + ".jpg"), wall, new ImageEncodingParam(ImwriteFlags.JpegQuality, rng.I(60, 93)));
            manifest.Add(new JsonObject { ["id"] = id, ["file"] = Path.Combine("pictures", id + ".jpg"), ["kind"] = "wall", ["product"] = null, ["conditions"] = kind + "; " + finish });
            made++;
        }

        foreach (string folder in real)
        {
            foreach (string f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal))
            {
                manifest.Add(new JsonObject { ["id"] = "real-" + manifest.Count, ["file"] = f, ["kind"] = "real", ["product"] = null, ["conditions"] = "real photograph or scan" });
                made++;
            }
        }

        File.WriteAllText(Path.Combine(outFolder, "manifest.json"), manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        output.WriteLine($"{made} pictures listed, {watch.Elapsed.TotalSeconds:0} s");
        return 0;
    }

    private static int IntOption(IReadOnlyList<string> args, string name, int fallback) =>
        Option(args, name) is { } v ? int.Parse(v, Inv) : fallback;

    private static string? Option(IReadOnlyList<string> args, string name)
    {
        for (int i = 0; i + 1 < args.Count; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>The printed area: the bounding box of everything not near white, in inches.</summary>
    private static Rect2d ContentBox(Mat blank)
    {
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), 0.1, 0.1, InterpolationFlags.Area);
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        using var ink = new Mat();
        Cv2.Threshold(gray, ink, 200, 255, ThresholdTypes.BinaryInv);
        var r = Cv2.BoundingRect(ink);
        return new Rect2d(r.X / 60.0, r.Y / 60.0, r.Width / 60.0, r.Height / 60.0);
    }

    /// <summary>
    /// Synthetic shot holes in groups around one to three bulls: a dark, ragged hole about .22 to .35 inch across, on a Shoot-N-C target a
    /// chartreuse halo where it struck black ink, and a repair paster over some of them.
    /// </summary>
    private static string Shoot(Mat img, Point2d[] bulls, bool shootNC, Rng rng)
    {
        const double dpi = 600;
        var paper = shootNC ? PaperColour(img) : new Scalar(245, 245, 245);
        int groups = Math.Min(bulls.Length, rng.I(1, 4)), holes = 0, pasters = 0;

        // The halo shows only where the black ink was, so the ink is found once, before any hole is made.
        using var gray = new Mat();
        Cv2.CvtColor(img, gray, ColorConversionCodes.BGR2GRAY);
        using var dark = new Mat();
        Cv2.Threshold(gray, dark, 80, 255, ThresholdTypes.BinaryInv);
        for (int gi = 0; gi < groups; gi++)
        {
            var c = bulls[rng.I(0, bulls.Length)];
            double spread = rng.U(0.2, 0.8);
            int n = rng.I(3, 11);
            for (int s = 0; s < n; s++)
            {
                double x = (c.X + (spread * rng.N())) * dpi, y = (c.Y + (spread * rng.N())) * dpi;
                if (x < 0 || y < 0 || x >= img.Width || y >= img.Height)
                {
                    continue;
                }

                double radius = rng.U(0.11, 0.175) * dpi;
                var centre = new Point2d(x, y);
                if (shootNC)
                {
                    var halo = Ragged(centre, radius * rng.U(2.0, 3.6), 0.25, rng);
                    using var mask = new Mat(img.Size(), MatType.CV_8UC1, Scalar.All(0));
                    Cv2.FillPoly(mask, [halo], Scalar.All(255));
                    Cv2.BitwiseAnd(mask, dark, mask);
                    img.SetTo(paper, mask);
                }

                Cv2.FillPoly(img, [Ragged(centre, radius, 0.18, rng)], Scalar.All(rng.U(20, 60)));
                holes++;
                if (rng.P(0.2))
                {
                    Cv2.Circle(img, new Point(x, y), (int)(rng.U(0.35, 0.5) * dpi), shootNC && rng.P(0.5) ? paper : Scalar.All(15), -1, LineTypes.AntiAlias);
                    pasters++;
                }
            }
        }

        return $"{holes} holes{(shootNC ? " with halos" : "")}, {pasters} pasters";
    }

    private static Scalar PaperColour(Mat img)
    {
        using var small = new Mat();
        Cv2.Resize(img, small, new Size(0, 0), 0.05, 0.05, InterpolationFlags.Area);
        byte[] px = Bytes(small);
        double b = 0, g = 0, r = 0;
        int n = 0;
        for (int i = 0; i + 2 < px.Length; i += 3)
        {
            // Chartreuse: green and red high, blue low.
            if (px[i + 1] > 180 && px[i + 2] > 150 && px[i] < 140)
            {
                b += px[i];
                g += px[i + 1];
                r += px[i + 2];
                n++;
            }
        }

        return n == 0 ? new Scalar(90, 240, 220) : new Scalar(b / n, g / n, r / n);
    }

    private static Point[] Ragged(Point2d centre, double radius, double jitter, Rng rng)
    {
        int n = 24;
        return [.. Enumerable.Range(0, n).Select(k =>
        {
            double a = k * 2 * Math.PI / n, rr = radius * (1 + (jitter * rng.U(-1, 1)));
            return new Point(centre.X + (rr * Math.Cos(a)), centre.Y + (rr * Math.Sin(a)));
        })];
    }

    /// <summary>
    /// A phone picture of the sheet on a wall or board: 1 to 3 ft away, tilted up to 37 degrees in any direction, turned any way, and in
    /// some pictures cut to half the target. Returns the picture and the homography from target inches to its pixels.
    /// </summary>
    private static (Mat Picture, double[] H, string Conditions) Phone(Mat marked, Rect2d box, Rng rng)
    {
        double d = rng.U(12, 36), tilt = rng.U(0, 37) * Math.PI / 180, axis = rng.U(0, 2 * Math.PI);
        double turn = rng.P(0.5) ? rng.U(-20, 20) : rng.U(-180, 180);
        var cx = box.X + (box.Width / 2);
        var cy = box.Y + (box.Height / 2);
        double[] rot = Multiply3(Rodrigues(Math.Cos(axis) * tilt, Math.Sin(axis) * tilt), RotZ(turn * Math.PI / 180));

        // Target inches (x, y) map to camera (R (x - cx, y - cy, 0) + (0, 0, d)), projected with the focal length about the frame's centre.
        double[] t = [-(rot[0] * cx) - (rot[1] * cy), -(rot[3] * cx) - (rot[4] * cy), d - (rot[6] * cx) - (rot[7] * cy)];
        double[] k = [PhoneFocal, 0, PhoneWidth / 2.0, 0, PhoneFocal, PhoneHeight / 2.0, 0, 0, 1];
        double[] m = [rot[0], rot[1], t[0], rot[3], rot[4], t[1], rot[6], rot[7], t[2]];
        double[] h = Multiply3(k, m);

        double pxPerInch = PhoneFocal / d;
        double srcDpi = Math.Min(600, Math.Ceiling(2 * pxPerInch));
        using var src = new Mat();
        Cv2.Resize(marked, src, new Size(0, 0), srcDpi / 600, srcDpi / 600, InterpolationFlags.Area);
        double[] hSrc = Multiply3(h, [1 / srcDpi, 0, 0, 0, 1 / srcDpi, 0, 0, 0, 1]);

        var picture = Wall(PhoneWidth, PhoneHeight, rng, out string wall);
        using (var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, hSrc))
        {
            Cv2.WarpPerspective(src, picture, hm, picture.Size(), InterpolationFlags.Linear, BorderTypes.Transparent);
        }

        string cut = "whole";
        if (rng.P(0.3))
        {
            // Half the target: the frame cut through the target's centre, keeping one side.
            var c = Apply(h, new Point2d(cx, cy));
            int side = rng.I(0, 4);
            var roi = side switch
            {
                0 => new Rect(0, 0, Math.Clamp((int)c.X, 400, PhoneWidth), PhoneHeight),
                1 => new Rect(Math.Clamp((int)c.X, 0, PhoneWidth - 400), 0, PhoneWidth - Math.Clamp((int)c.X, 0, PhoneWidth - 400), PhoneHeight),
                2 => new Rect(0, 0, PhoneWidth, Math.Clamp((int)c.Y, 400, PhoneHeight)),
                _ => new Rect(0, Math.Clamp((int)c.Y, 0, PhoneHeight - 400), PhoneWidth, PhoneHeight - Math.Clamp((int)c.Y, 0, PhoneHeight - 400)),
            };
            var cropped = new Mat(picture, roi).Clone();
            picture.Dispose();
            picture = cropped;
            h = Multiply3([1, 0, -roi.X, 0, 1, -roi.Y, 0, 0, 1], h);
            cut = "half";
        }

        Finish(picture, rng, out string finish);
        return (picture, h, string.Create(Inv, $"phone {d:0} in, tilt {tilt * 180 / Math.PI:0} deg, turned {turn:0} deg, {cut}, {wall}, {finish}"));
    }

    /// <summary>A flatbed scan: 150, 300 or 600 dpi, the sheet laid anywhere on a letter-size glass at any angle.</summary>
    private static (Mat Picture, double[] H, string Conditions) Scan(Mat marked, Rect2d box, Rng rng)
    {
        double dpi = new[] { 150.0, 300.0, 600.0 }[rng.I(0, 3)];
        double angle = rng.P(0.5) ? (rng.I(0, 4) * 90) + rng.U(-3, 3) : rng.U(0, 360);
        double a = angle * Math.PI / 180, cx = box.X + (box.Width / 2), cy = box.Y + (box.Height / 2);
        double gx = 4.25 + rng.U(-1.5, 1.5), gy = 5.85 + rng.U(-1.5, 1.5);
        double c = Math.Cos(a), s = Math.Sin(a);

        // Target inches to glass pixels: turn about the target's centre, place it on the glass, scale to the scan's resolution.
        double[] h = [dpi * c, -dpi * s, dpi * (gx - (c * cx) + (s * cy)), dpi * s, dpi * c, dpi * (gy - (s * cx) - (c * cy)), 0, 0, 1];
        using var src = new Mat();
        Cv2.Resize(marked, src, new Size(0, 0), dpi / 600, dpi / 600, InterpolationFlags.Area);
        double[] hSrc = Multiply3(h, [1 / dpi, 0, 0, 0, 1 / dpi, 0, 0, 0, 1]);
        var picture = new Mat(new Size((int)(8.5 * dpi), (int)(11.7 * dpi)), MatType.CV_8UC3, Scalar.All(rng.U(235, 252)));
        using (var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, hSrc))
        {
            Cv2.WarpPerspective(src, picture, hm, picture.Size(), InterpolationFlags.Linear, BorderTypes.Transparent);
        }

        double sigma = rng.U(0.3, 0.9);
        Cv2.GaussianBlur(picture, picture, new Size(0, 0), sigma);
        return (picture, h, string.Create(Inv, $"scan {dpi:0} dpi, turned {angle:0} deg, blur {sigma:0.0}"));
    }

    /// <summary>A plain textured surface: painted plaster, plywood, cardboard or brick, with slow and fine variation.</summary>
    private static Mat Wall(int w, int h, Rng rng, out string kind)
    {
        kind = new[] { "plaster", "plywood", "cardboard", "brick" }[rng.I(0, 4)];
        var baseColour = kind switch
        {
            "plaster" => new Scalar(rng.U(150, 230), rng.U(150, 230), rng.U(150, 230)),
            "plywood" => new Scalar(rng.U(90, 140), rng.U(140, 180), rng.U(180, 220)),
            "cardboard" => new Scalar(rng.U(80, 110), rng.U(120, 150), rng.U(160, 190)),
            _ => new Scalar(rng.U(60, 90), rng.U(70, 100), rng.U(140, 180)),
        };
        var wall = new Mat(new Size(w, h), MatType.CV_8UC3, baseColour);
        using var slow = new Mat(new Size(Math.Max(2, w / 200), Math.Max(2, h / 200)), MatType.CV_32FC1);
        Cv2.Randn(slow, Scalar.All(0), Scalar.All(rng.U(6, 18)));
        using var slowBig = new Mat();
        Cv2.Resize(slow, slowBig, new Size(w, h), 0, 0, InterpolationFlags.Cubic);
        using var fine = new Mat(new Size(w, h), MatType.CV_32FC1);
        Cv2.Randn(fine, Scalar.All(0), Scalar.All(rng.U(2, 6)));
        using var grain = new Mat();
        Cv2.Add(slowBig, fine, grain);
        if (kind == "plywood")
        {
            using var stripes = new Mat(new Size(1, h), MatType.CV_32FC1);
            double period = rng.U(15, 60), phase = rng.U(0, 6);
            for (int y = 0; y < h; y++)
            {
                stripes.Set(y, 0, (float)(10 * Math.Sin((y / period) + phase + (3 * Math.Sin(y / (period * 7))))));
            }

            using var stripesBig = new Mat();
            Cv2.Repeat(stripes, 1, w, stripesBig);
            Cv2.Add(grain, stripesBig, grain);
        }

        using var grain3 = new Mat();
        Cv2.Merge([grain, grain, grain], grain3);
        using var wallF = new Mat();
        wall.ConvertTo(wallF, MatType.CV_32FC3);
        Cv2.Add(wallF, grain3, wallF);
        wallF.ConvertTo(wall, MatType.CV_8UC3);
        if (kind == "brick")
        {
            double bh = rng.U(60, 140), bw = bh * 2.7;
            for (int row = 0; row * bh < h; row++)
            {
                Cv2.Line(wall, new Point(0, row * bh), new Point(w, row * bh), Scalar.All(170), (int)(bh / 8));
                for (double x = (row % 2) * bw / 2; x < w; x += bw)
                {
                    Cv2.Line(wall, new Point(x, row * bh), new Point(x, (row + 1) * bh), Scalar.All(170), (int)(bh / 8));
                }
            }
        }

        return wall;
    }

    /// <summary>What a phone does to a picture: uneven light and a colour cast, blur, sensor noise.</summary>
    private static void Finish(Mat picture, Rng rng, out string finish)
    {
        int w = picture.Width, h = picture.Height;
        double low = rng.U(0.45, 1.0), angle = rng.U(0, 2 * Math.PI);
        using var light = new Mat(new Size(w, h), MatType.CV_32FC1);
        float[] row = new float[w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                double u = ((((x / (double)w) - 0.5) * Math.Cos(angle)) + (((y / (double)h) - 0.5) * Math.Sin(angle))) + 0.5;
                double rx = (x / (double)w) - 0.5, ry = (y / (double)h) - 0.5;
                double vignette = 1 - (0.35 * ((rx * rx) + (ry * ry)));
                row[x] = (float)((low + ((1 - low) * Math.Clamp(u, 0, 1))) * vignette);
            }

            Marshal.Copy(row, 0, light.Ptr(y), w);
        }

        double cb = rng.U(0.85, 1.1), cg = rng.U(0.9, 1.05), cr = rng.U(0.9, 1.15);
        using var f = new Mat();
        picture.ConvertTo(f, MatType.CV_32FC3);
        var ch = Cv2.Split(f);
        Cv2.Multiply(ch[0], light, ch[0], cb);
        Cv2.Multiply(ch[1], light, ch[1], cg);
        Cv2.Multiply(ch[2], light, ch[2], cr);
        Cv2.Merge(ch, f);
        foreach (var c in ch)
        {
            c.Dispose();
        }

        double sigma = rng.U(0, 2.5), noise = rng.U(1.5, 5);
        if (sigma > 0.3)
        {
            Cv2.GaussianBlur(f, f, new Size(0, 0), sigma);
        }

        using var n = new Mat(f.Size(), MatType.CV_32FC3);
        Cv2.Randn(n, Scalar.All(0), Scalar.All(noise));
        Cv2.Add(f, n, f);
        f.ConvertTo(picture, MatType.CV_8UC3);
        finish = string.Create(Inv, $"light {low:0.00} to 1, blur {sigma:0.0} px, noise {noise:0.0}");
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

    private static double[] Multiply3(double[] a, double[] b)
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

    private static Point2d Apply(double[] h, Point2d p)
    {
        double w = (h[6] * p.X) + (h[7] * p.Y) + h[8];
        return new Point2d(((h[0] * p.X) + (h[1] * p.Y) + h[2]) / w, ((h[3] * p.X) + (h[4] * p.Y) + h[5]) / w);
    }

    private static double[] Invert(double[] m)
    {
        double a = m[0], b = m[1], c = m[2], d = m[3], e = m[4], f = m[5], g = m[6], h = m[7], i = m[8];
        double det = (a * ((e * i) - (f * h))) - (b * ((d * i) - (f * g))) + (c * ((d * h) - (e * g)));
        return [((e * i) - (f * h)) / det, ((c * h) - (b * i)) / det, ((b * f) - (c * e)) / det,
            ((f * g) - (d * i)) / det, ((a * i) - (c * g)) / det, ((c * d) - (a * f)) / det,
            ((d * h) - (e * g)) / det, ((b * g) - (a * h)) / det, ((a * e) - (b * d)) / det];
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Recognition and registration

    private sealed record Candidate(string Product, int Ratio, int Inliers, double Spread, double[]? H, double Layout = 0, int Visible = 0, double Trimmed = 0);

    private sealed record Result(string Id, string Kind, string? Truth, List<Candidate> Candidates, int Rank);

    /// <summary>
    /// The decision: of the products with at least <paramref name="inliers"/> verified features, the one whose colour layout agrees best
    /// with the picture, claimed only when that agreement is at least <paramref name="layout"/> and beats the next by <paramref name="margin"/>.
    /// </summary>
    private static Candidate? Decide(List<Candidate> candidates, int inliers, double layout, double margin)
    {
        var able = candidates.Where(c => c.Inliers >= inliers && c.H is not null).OrderByDescending(c => c.Layout).ToList();
        if (able.Count == 0 || able[0].Layout < layout)
        {
            return null;
        }

        return able.Count > 1 && able[0].Layout - able[1].Layout < margin ? null : able[0];
    }

    private static int Match(string outFolder, IReadOnlyList<string> rest, TextWriter output)
    {
        string method = Option(rest, "--method") ?? "orb";
        int side = IntOption(rest, "--side", 2000);
        double ratio = double.Parse(Option(rest, "--ratio") ?? "0.8", Inv);
        long startSet = Environment.WorkingSet;
        var fps = Directory.EnumerateFiles(Path.Combine(outFolder, "fingerprints", method), "*.glfp").Order(StringComparer.Ordinal).Select(Load).ToList();
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(outFolder, "manifest.json")))!.AsArray();
        using var detector = Detector(method, 6000);
        using var matcher = new BFMatcher(NormTypes.Hamming);
        int guidedDistance = method == "orb" ? 64 : 110;
        var csv = new List<string> { "id,kind,product,candidate,inliers,spread,layout,visible,ratio,trimmed" };
        var decode = new List<double>();
        var detect = new List<double>();
        var match = new List<double>();
        var results = new List<Result>();
        var truths = new Dictionary<string, (double[] H, double S, int W, int Ht)>();

        foreach (var node in manifest)
        {
            string id = (string)node!["id"]!, kind = (string)node["kind"]!, file = (string)node["file"]!;
            string? truth = (string?)node["product"];
            string path = Path.IsPathRooted(file) ? file : Path.Combine(outFolder, file);
            var watch = Stopwatch.StartNew();

            // Pixels only, decoded without the picture's orientation tag or any other metadata; the phone's 8 megapixel working copy.
            using var full = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
            double toWorking = Math.Min(1, Math.Sqrt(WorkingMegapixels * 1e6 / ((double)full.Width * full.Height)));
            using var working = new Mat();
            Cv2.Resize(full, working, new Size(0, 0), toWorking, toWorking, InterpolationFlags.Area);
            double toSide = Math.Min(1, side / (double)Math.Max(working.Width, working.Height));
            using var small = new Mat();
            Cv2.Resize(working, small, new Size(0, 0), toSide, toSide, InterpolationFlags.Area);
            using var gray = new Mat();
            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
            double decodeMs = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            var keys = detector.Detect(gray);
            if (keys.Length > 6000)
            {
                keys = [.. keys.OrderByDescending(k => k.Response).Take(6000)];
            }

            using var desc = new Mat();
            detector.Compute(gray, ref keys, desc);
            float[] signature = Signature(small);
            using var lab = new Mat();
            Cv2.CvtColor(small, lab, ColorConversionCodes.BGR2Lab);
            double detectMs = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            var candidates = fps.Select(fp => Try(fp, keys, desc, matcher, ratio, guidedDistance, small.Width, small.Height, lab)).ToList();
            double matchMs = watch.Elapsed.TotalMilliseconds;
            decode.Add(decodeMs);
            detect.Add(detectMs);
            match.Add(matchMs);

            var shortlist = fps.OrderByDescending(fp => Intersection(fp.Signature, signature)).Select(fp => fp.Product).ToList();
            int rank = truth is null ? 0 : shortlist.IndexOf(truth) + 1;
            results.Add(new Result(id, kind, truth, candidates, rank));
            if (node["h"] is JsonArray hArray && truth is not null)
            {
                truths[id] = ([.. hArray.Select(v => (double)v!)], toWorking * toSide, small.Width, small.Height);
            }

            foreach (var c in candidates)
            {
                csv.Add(string.Join(',', id, kind, truth ?? "", c.Product, c.Inliers, F(c.Spread), F(c.Layout), c.Visible, c.Ratio, F(c.Trimmed)));
            }
        }

        File.WriteAllLines(Path.Combine(outFolder, $"candidates-{method}-{side}.csv"), csv);
        long peak = Process.GetCurrentProcess().PeakWorkingSet64;

        output.WriteLine($"method {method}, detection side {side} px, ratio {ratio}, {fps.Count} fingerprints, {results.Count} pictures");
        foreach (var fp in fps)
        {
            output.WriteLine(string.Create(Inv, $"  {fp.Product}: {fp.Points.Length} features x ({fp.Descriptors.Cols} + 8) bytes, {fp.Bulls.Length} bulls, layout {fp.Layout.Cols} x {fp.Layout.Rows}"));
        }

        var pos = results.Where(r => r.Truth is not null).ToList();
        var neg = results.Where(r => r.Truth is null).ToList();
        // "Not in the library": each positive picture again with its own product's fingerprint taken away, as if somebody photographed a
        // product GroupLab has no fingerprint for. Any claim then is a wrong product, with the wrong scale.
        output.WriteLine("inliers  layout  margin  identified(phone)  identified(scan)  wrong product  claims on negatives (sheet/wall/real)  claims with own product absent");
        foreach (int inl in new[] { 15, 25, 40 })
        {
            foreach (double lay in new[] { 0.0, 0.5, 0.7, 0.75, 0.8, 0.85, 0.9 })
            {
                foreach (double margin in new[] { 0.0, 0.1 })
                {
                    int phoneOk = pos.Count(r => r.Kind == "phone" && Decide(r.Candidates, inl, lay, margin)?.Product == r.Truth);
                    int scanOk = pos.Count(r => r.Kind == "scan" && Decide(r.Candidates, inl, lay, margin)?.Product == r.Truth);
                    int wrong = pos.Count(r => Decide(r.Candidates, inl, lay, margin) is { } c && c.Product != r.Truth);
                    int Neg(string k) => neg.Count(r => r.Kind == k && Decide(r.Candidates, inl, lay, margin) is not null);
                    int absent = pos.Count(r => Decide([.. r.Candidates.Where(c => c.Product != r.Truth)], inl, lay, margin) is not null);
                    output.WriteLine(string.Create(Inv,
                        $"  {inl,4}   {lay,4:0.00}   {margin,4:0.0}    {phoneOk,3}/{pos.Count(r => r.Kind == "phone")}   {scanOk,3}/{pos.Count(r => r.Kind == "scan")}   {wrong,3}/{pos.Count}   {Neg("grouplab-sheet")}/{neg.Count(r => r.Kind == "grouplab-sheet")} {Neg("wall")}/{neg.Count(r => r.Kind == "wall")} {Neg("real")}/{neg.Count(r => r.Kind == "real")}   {absent}/{pos.Count}"));
                }
            }
        }

        // The chosen rule, written once here and used for the errors below.
        int chosenInliers = IntOption(rest, "--inliers", 25);
        double chosenLayout = double.Parse(Option(rest, "--layout") ?? "0.85", Inv), chosenMargin = double.Parse(Option(rest, "--margin") ?? "0.1", Inv);
        output.WriteLine(string.Create(Inv, $"chosen rule: at least {chosenInliers} verified features, layout agreement at least {chosenLayout:0.00}, ahead of the next by {chosenMargin:0.00}"));
        double negLayout = neg.SelectMany(r => r.Candidates).Where(c => c.Inliers >= chosenInliers).Select(c => c.Layout).DefaultIfEmpty(0).Max();
        double wrongLayout = pos.SelectMany(r => r.Candidates.Where(c => c.Product != r.Truth)).Where(c => c.Inliers >= chosenInliers).Select(c => c.Layout).DefaultIfEmpty(0).Max();
        output.WriteLine(string.Create(Inv, $"highest layout agreement with {chosenInliers}+ features: on a negative picture {negLayout:0.000}, on a wrong product {wrongLayout:0.000}"));
        foreach (var r in pos.Where(r => Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin) is { } c && c.Product != r.Truth))
        {
            var c = Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin)!;
            output.WriteLine(string.Create(Inv, $"  wrong: {r.Id} claimed {c.Product} ({c.Inliers} features, layout {c.Layout:0.000})"));
        }

        foreach (var r in neg.Where(r => Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin) is not null))
        {
            var c = Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin)!;
            output.WriteLine(string.Create(Inv, $"  false: {r.Id} ({r.Kind}) claimed {c.Product} ({c.Inliers} features, layout {c.Layout:0.000})"));
        }

        foreach (var r in pos.Where(r => Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin) is null))
        {
            var t = r.Candidates.First(c => c.Product == r.Truth);
            output.WriteLine(string.Create(Inv, $"  missed: {r.Id}: true product {t.Inliers} features, layout {t.Layout:0.000}"));
        }

        var ranks = pos.Select(r => r.Rank).ToList();
        output.WriteLine($"shortlist by colour signature: true product first in {ranks.Count(r => r == 1)}/{ranks.Count}, in the first two {ranks.Count(r => r <= 2)}, first three {ranks.Count(r => r <= 3)}");

        // Registration, for the pictures identified correctly under the chosen rule: each bull in view, from where it truly is in the
        // picture back through the fitted homography to the target, against where the fingerprint has it; and two inches about it.
        var errors = new Dictionary<string, (List<double> Bull, List<double> Scale)>();
        var worst = new List<(double Err, string Id, double Scale)>();
        foreach (var r in pos)
        {
            if (Decide(r.Candidates, chosenInliers, chosenLayout, chosenMargin) is not { H: { } hFit } c || c.Product != r.Truth || !truths.TryGetValue(r.Id, out var t))
            {
                continue;
            }

            double[] hTrue = Multiply3([t.S, 0, 0, 0, t.S, 0, 0, 0, 1], t.H);
            double[] back = Invert(hFit);
            var fp = fps.First(f => f.Product == r.Truth);
            if (!errors.TryGetValue(r.Kind, out var e))
            {
                errors[r.Kind] = e = ([], []);
            }

            double worstHere = 0, worstScale = 0;
            foreach (var b in fp.Bulls)
            {
                var p = Apply(hTrue, b);
                if (p.X < 5 || p.Y < 5 || p.X > t.W - 5 || p.Y > t.Ht - 5)
                {
                    continue;
                }

                var q = Apply(back, p);
                double err = Math.Sqrt(Math.Pow(q.X - b.X, 2) + Math.Pow(q.Y - b.Y, 2));
                e.Bull.Add(err);
                worstHere = Math.Max(worstHere, err);
                foreach (var (dx, dy) in new[] { (1.0, 0.0), (0.0, 1.0) })
                {
                    var a1 = Apply(back, Apply(hTrue, new Point2d(b.X - dx, b.Y - dy)));
                    var a2 = Apply(back, Apply(hTrue, new Point2d(b.X + dx, b.Y + dy)));
                    double pct = Math.Abs(((Math.Sqrt(Math.Pow(a2.X - a1.X, 2) + Math.Pow(a2.Y - a1.Y, 2)) / 2) - 1) * 100);
                    e.Scale.Add(pct);
                    worstScale = Math.Max(worstScale, pct);
                }
            }

            worst.Add((worstHere, r.Id, worstScale));
        }

        foreach (var (kind, (bull, scale)) in errors.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            var be = Sorted(bull);
            var se = Sorted(scale);
            output.WriteLine(string.Create(Inv,
                $"{kind}: bull error (in) median {Pct(be, 50):0.000}, 95th {Pct(be, 95):0.000}, max {be.LastOrDefault():0.000} over {be.Count} bulls; scale error (%) median {Pct(se, 50):0.00}, 95th {Pct(se, 95):0.00}, max {se.LastOrDefault():0.00}"));
        }

        foreach (var (err, id, scale) in worst.OrderByDescending(x => x.Err).Take(6))
        {
            output.WriteLine(string.Create(Inv, $"  largest: {id}: bull {err:0.000} in, scale {scale:0.00} %"));
        }

        output.WriteLine(string.Create(Inv,
            $"time per picture (ms, median / 95th): decode and working copy {Pct(Sorted(decode), 50):0} / {Pct(Sorted(decode), 95):0}, features and layout {Pct(Sorted(detect), 50):0} / {Pct(Sorted(detect), 95):0}, matching against {fps.Count} {Pct(Sorted(match), 50):0} / {Pct(Sorted(match), 95):0}"));
        output.WriteLine($"memory: working set at start {startSet / 1048576} MB, peak {peak / 1048576} MB");
        foreach (var fp in fps)
        {
            fp.Descriptors.Dispose();
        }

        return 0;
    }

    /// <summary>
    /// Question 87 (Alan, 2026-10-07: re-run the trial): <c>shipped &lt;out&gt; [--known &lt;picture&gt; &lt;product id&gt;]...</c>. Every
    /// picture in the manifest, and each real photograph named with its product, through <see cref="StoreTargetRecognizer"/> and the shipped
    /// library, as the application runs it. It prints how many features the wrong products' fits ever reached, and what a second line would
    /// do: a fit carried by at least F features claimed at a colour layout agreement of L or more, below the 0.85 the trial set, when no
    /// other product is within the margin. "Absent" is each product picture again with its own product's candidate taken out.
    /// </summary>
    private static int Shipped(string outFolder, IReadOnlyList<string> rest, TextWriter output)
    {
        var pictures = JsonNode.Parse(File.ReadAllText(Path.Combine(outFolder, "manifest.json")))!.AsArray()
            .Select(n => (Id: (string)n!["id"]!, Kind: (string)n["kind"]!, File: (string)n["file"]!, Truth: (string?)n["product"])).ToList();
        for (int i = 0; i + 2 < rest.Count; i++)
        {
            if (rest[i] == "--known")
            {
                // "none" names a real photograph of a product the library does not have.
                pictures.Add(rest[i + 2] == "none" ? ($"real-{pictures.Count}", "real", rest[i + 1], null) : ($"range-{pictures.Count}", "range", rest[i + 1], rest[i + 2]));
            }
        }

        var backend = new GroupLab.Cli.Imaging.OpenCvFingerprintBackend();
        var seen = new List<(string Id, string Kind, string? Truth, IReadOnlyList<StoreTargetCandidate> Candidates, string? Named)>();
        var csv = new List<string> { "id,kind,truth,candidate,inliers,layout,named" };
        foreach (var (id, kind, file, truth) in pictures)
        {
            string path = Path.IsPathRooted(file) ? file : Path.Combine(outFolder, file);
            if (StoreTargetRecognizer.Recognize(path, backend) is not { } r)
            {
                continue;
            }

            seen.Add((id, kind, truth, r.Candidates, r.Named?.Target.Id));
            csv.AddRange(r.Candidates.Select(c => string.Join(',', id, kind, truth ?? "", c.Target.Id, c.Inliers, F(c.Layout), r.Named?.Target.Id ?? "")));
        }

        File.WriteAllLines(Path.Combine(outFolder, "candidates-shipped.csv"), csv);
        output.WriteLine($"{StoreTargetLibrary.All.Count} shipped fingerprints, {seen.Count} pictures");
        bool Wrong(StoreTargetCandidate c, string? truth) => c.Target.Id != truth && c.ToImage is not null;
        foreach (var group in seen.GroupBy(s => s.Truth is null ? "picture of none" : "wrong product on a product picture"))
        {
            var top = group.SelectMany(s => s.Candidates.Where(c => Wrong(c, s.Truth)).Select(c => (s.Id, c))).OrderByDescending(x => x.c.Inliers).Take(5);
            output.WriteLine($"most features on a {group.Key}: " + string.Join("; ", top.Select(x => string.Create(Inv, $"{x.Id} {x.c.Target.Id} {x.c.Inliers} at {x.c.Layout:0.000}"))));
        }

        string? Claim(IReadOnlyList<StoreTargetCandidate> all, int least, double line)
        {
            if (StoreTargetRecognizer.Decide(all).Named is { } named)
            {
                return named.Target.Id;
            }

            var able = all.Where(c => c.Inliers >= StoreTargetRecognizer.LeastInliers && c.ToImage is not null).OrderByDescending(c => c.Layout).ToList();
            return able.Count > 0 && able[0].Inliers >= least && able[0].Layout >= line
                && able.Skip(1).All(c => able[0].Layout - c.Layout >= StoreTargetRecognizer.Margin) ? able[0].Target.Id : null;
        }

        output.WriteLine("features  layout | named right (phone / scan / range) | wrong product | claims on pictures of none | claims with own product absent");
        foreach (int least in new[] { int.MaxValue, 150, 200, 250, 300, 400 })
        {
            foreach (double line in least == int.MaxValue ? new[] { 0.85 } : new[] { 0.5, 0.55, 0.6, 0.7, 0.75 })
            {
                int Right(string k) => seen.Count(s => s.Kind == k && s.Truth is not null && Claim(s.Candidates, least, line) == s.Truth);
                int wrong = seen.Count(s => s.Truth is not null && Claim(s.Candidates, least, line) is { } c && c != s.Truth);
                int none = seen.Count(s => s.Truth is null && Claim(s.Candidates, least, line) is not null);
                int absent = seen.Count(s => s.Truth is not null && Claim([.. s.Candidates.Where(c => c.Target.Id != s.Truth)], least, line) is not null);
                output.WriteLine(string.Create(Inv,
                    $"{(least == int.MaxValue ? "as now" : least.ToString(Inv)),8}  {line,6:0.00} | {Right("phone"),3} / {Right("scan"),2} / {Right("range")} of {seen.Count(s => s.Kind == "phone")} / {seen.Count(s => s.Kind == "scan")} / {seen.Count(s => s.Kind == "range")} | {wrong,3} | {none,3} of {seen.Count(s => s.Truth is null)} | {absent,3}"));
            }
        }

        foreach (var s in seen.Where(s => s.Kind == "range"))
        {
            var own = s.Candidates.First(c => c.Target.Id == s.Truth);
            var next = s.Candidates.Where(c => c.Target.Id != s.Truth && c.Inliers >= StoreTargetRecognizer.LeastInliers && c.ToImage is not null).OrderByDescending(c => c.Layout).FirstOrDefault();
            output.WriteLine(string.Create(Inv, $"  {s.Id} {s.Truth}: {own.Inliers} features at {own.Layout:0.000}, named now {s.Named ?? "none"}; next product {(next is null ? "none able" : $"{next.Target.Id} {next.Inliers} at {next.Layout:0.000}")}"));
        }

        return 0;
    }

    private static void Add(Dictionary<string, List<double>> d, string key, double v)
    {
        if (!d.TryGetValue(key, out var list))
        {
            d[key] = list = [];
        }

        list.Add(v);
    }

    private static List<double> Sorted(List<double> v) => [.. v.Order()];

    private static double Pct(List<double> sorted, double p) =>
        sorted.Count == 0 ? double.NaN : sorted[Math.Clamp((int)Math.Round(p / 100 * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static string F(double v) => double.IsNaN(v) ? "" : v.ToString("0.####", Inv);

    /// <summary>
    /// One fingerprint against one picture: ratio-tested matches, a RANSAC homography from target inches to picture pixels, then every
    /// fingerprint feature looked for near where that homography puts it, and the homography fitted again to what was found. The score is
    /// the count of features that agree with the final fit, and the area they cover on the target.
    /// </summary>
    private static Candidate Try(Fingerprint fp, KeyPoint[] keys, Mat desc, BFMatcher matcher, double ratio, int guidedDistance, int w, int h, Mat lab)
    {
        var none = new Candidate(fp.Product, 0, 0, 0, null);
        if (keys.Length < 8 || desc.Rows < 8)
        {
            return none;
        }

        // Two ways to start. The ratio test keeps only matches with no close rival, which is right for words and numbers but throws away
        // every feature of a pattern the target repeats (the sight-in's four corner diamonds, a bull's rings). So a second start keeps the
        // three nearest fingerprint features of each picture feature when they are close, and leaves the rest to a robust fit.
        var knn = matcher.KnnMatch(desc, fp.Descriptors, 3);
        var good = knn.Where(m => m.Length >= 2 && m[0].Distance < ratio * m[1].Distance).Select(m => m[0]).ToList();
        int close = guidedDistance * 3 / 4;
        var many = knn.SelectMany(m => m).Where(m => m.Distance <= close).ToList();
        double threshold = 0.004 * Math.Max(w, h);
        byte[] qd = Bytes(desc), fd = Bytes(fp.Descriptors);
        int cols = desc.Cols;
        double cell = Math.Max(4, threshold * 2);
        var grid = new Dictionary<(int, int), List<int>>();
        int rows = Math.Min(keys.Length, desc.Rows);
        for (int i = 0; i < rows; i++)
        {
            var keyCell = ((int)(keys[i].Pt.X / cell), (int)(keys[i].Pt.Y / cell));
            if (!grid.TryGetValue(keyCell, out var list))
            {
                grid[keyCell] = list = [];
            }

            list.Add(i);
        }

        var best = none with { Ratio = good.Count };
        foreach (var (seed, method) in new[] { (good, HomographyMethods.Ransac), (many, HomographyMethods.USAC_MAGSAC) })
        {
            if (seed.Count < 8)
            {
                continue;
            }

            var src = seed.Select(m => fp.Points[m.TrainIdx]).ToArray();
            var dst = seed.Select(m => new Point2d(keys[m.QueryIdx].Pt.X, keys[m.QueryIdx].Pt.Y)).ToArray();
            double[]? hm = Homography(src, dst, threshold, method, out _, out _);
            if (hm is null || !Plausible(hm, fp, w, h))
            {
                continue;
            }

            var found = Guided(fp, keys, qd, fd, cols, grid, cell, hm, threshold, guidedDistance, w, h, lab);
            if (found is not null && found.Inliers > best.Inliers)
            {
                best = found with { Ratio = good.Count };
            }
        }

        return best;
    }

    /// <summary>
    /// Each fingerprint feature looked for near where a first fit puts it, the homography fitted again to what was found, and the result
    /// scored: the features that agree with it, the area they cover on the target, and the colour layout read through it.
    /// </summary>
    private static Candidate? Guided(Fingerprint fp, KeyPoint[] keys, byte[] qd, byte[] fd, int cols, Dictionary<(int, int), List<int>> grid,
        double cell, double[] hm, double threshold, int guidedDistance, int w, int h, Mat lab)
    {
        int visible = 0;
        var gs = new List<Point2d>();
        var gd = new List<Point2d>();
        for (int j = 0; j < fp.Points.Length; j++)
        {
            var p = Apply(hm, fp.Points[j]);
            if (p.X < 0 || p.Y < 0 || p.X >= w || p.Y >= h)
            {
                continue;
            }

            visible++;
            int bestIdx = -1, bestDist = guidedDistance;
            int gx = (int)(p.X / cell), gy = (int)(p.Y / cell);
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    if (!grid.TryGetValue((gx + ox, gy + oy), out var list))
                    {
                        continue;
                    }

                    foreach (int i in list)
                    {
                        double dx = keys[i].Pt.X - p.X, dy = keys[i].Pt.Y - p.Y;
                        if ((dx * dx) + (dy * dy) > threshold * threshold * 4)
                        {
                            continue;
                        }

                        int dist = Hamming(qd, i * cols, fd, j * cols, cols);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = i;
                        }
                    }
                }
            }

            if (bestIdx >= 0)
            {
                gs.Add(fp.Points[j]);
                gd.Add(new Point2d(keys[bestIdx].Pt.X, keys[bestIdx].Pt.Y));
            }
        }

        if (gs.Count < 8)
        {
            return null;
        }

        double[]? refined = Homography([.. gs], [.. gd], threshold * 0.6, HomographyMethods.Ransac, out int inliers, out var inlierPoints);
        if (refined is null || !Plausible(refined, fp, w, h))
        {
            return null;
        }

        double spread = 0;
        if (inlierPoints.Count >= 3)
        {
            var hull = Cv2.ConvexHull(inlierPoints.Select(p => new Point2f((float)p.X, (float)p.Y)));
            spread = Cv2.ContourArea(hull);
        }

        var (full, trimmed) = LayoutScore(fp.Layout, refined, lab, w, h);
        return new Candidate(fp.Product, 0, inliers, spread, refined, full, visible, trimmed);
    }

    private static double[]? Homography(Point2d[] src, Point2d[] dst, double threshold, HomographyMethods method, out int inliers, out List<Point2d> inlierSrc)
    {
        inliers = 0;
        inlierSrc = [];
        using var mask = new Mat();
        using var hm = Cv2.FindHomography(src, dst, method, threshold, mask, 10000, 0.999);
        if (hm.Empty())
        {
            return null;
        }

        byte[] m = Bytes(mask);
        var s = new List<Point2d>();
        var d = new List<Point2d>();
        for (int i = 0; i < m.Length; i++)
        {
            if (m[i] != 0)
            {
                s.Add(src[i]);
                d.Add(dst[i]);
            }
        }

        inliers = s.Count;
        inlierSrc = s;
        if (s.Count < 8)
        {
            return null;
        }

        // A least squares fit to the inliers alone.
        using var ls = Cv2.FindHomography(s, d, HomographyMethods.None, 0, null);
        var fit = ls.Empty() ? hm : ls;
        var r = new double[9];
        for (int i = 0; i < 9; i++)
        {
            r[i] = fit.At<double>(i / 3, i % 3);
        }

        return r;
    }

    /// <summary>A homography a camera or scanner could have made: orientation kept, a sensible scale, not stretched more than a 60 degree tilt.</summary>
    private static bool Plausible(double[] h, Fingerprint fp, int w, int ht)
    {
        double cx = fp.Points.Average(p => p.X), cy = fp.Points.Average(p => p.Y);
        var c = Apply(h, new Point2d(cx, cy));
        var ex = Apply(h, new Point2d(cx + 0.5, cy));
        var ey = Apply(h, new Point2d(cx, cy + 0.5));
        double ax = (ex.X - c.X) * 2, ay = (ex.Y - c.Y) * 2, bx = (ey.X - c.X) * 2, by = (ey.Y - c.Y) * 2;
        double det = (ax * by) - (ay * bx);
        if (det <= 0)
        {
            return false;
        }

        double scale = Math.Sqrt(det);
        double la = Math.Sqrt((ax * ax) + (ay * ay)), lb = Math.Sqrt((bx * bx) + (by * by));
        double stretch = Math.Max(la, lb) / Math.Min(la, lb);
        double perspective = Math.Abs(h[6] / h[8]) + Math.Abs(h[7] / h[8]);
        return scale > 3 && scale < Math.Max(w, ht) && stretch < 2 && perspective < 0.5 && !double.IsNaN(c.X);
    }

    private static int Hamming(byte[] a, int ai, byte[] b, int bi, int n)
    {
        int d = 0;
        for (int k = 0; k < n; k++)
        {
            d += System.Numerics.BitOperations.PopCount((uint)(a[ai + k] ^ b[bi + k]));
        }

        return d;
    }
}
