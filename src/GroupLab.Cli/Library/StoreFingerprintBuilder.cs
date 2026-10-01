using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Library;

/// <summary>
/// <c>grouplab store-fingerprints build &lt;blanks folder&gt; &lt;out folder&gt;</c>, NOTES-FROM-PLANNING.md entry 340: the fingerprint of each
/// store-bought target from its 600 dpi scan of the blank, made exactly as the trial of entry 332 made them
/// (docs/notes/fingerprint-trial.md). Only the fingerprints are written; the scans stay where they are, on the computer they were made on,
/// and nothing of them is committed but what this writes (samples/PROVENANCE.md). The output goes into
/// src/GroupLab.Core/StoreTargets/Fingerprints, which the application ships as resources.
/// </summary>
public static class StoreFingerprintBuilder
{
    public const string Usage = "grouplab store-fingerprints build <blanks folder> <out folder>";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>The scans are 600 dpi.</summary>
    private const double ScanDpi = 600;

    /// <summary>The resolution the features are found at; their positions are kept in inches.</summary>
    private const double ReferenceDpi = 100;

    /// <summary>The most features one fingerprint keeps at the finer resolution, spread over the target in half inch cells.</summary>
    private const int FingerprintFeatures = 1500;

    /// <summary>The layout is read from the target made smooth at eight points an inch, so a fine line on a cell's edge cannot swing it.</summary>
    private const double LayoutDpi = 8, LayoutSigma = 1.2;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string[] a = [.. args];
        if (a is ["recognize", .. var pictures] && pictures.Length > 0)
        {
            return Recognize(pictures.SelectMany<string, string>(p => Directory.Exists(p)
                ? Directory.EnumerateFiles(p, "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal)
                : [p]), output);
        }

        if (a is not ["build", var blanks, var outFolder])
        {
            error.WriteLine(Usage);
            return 2;
        }

        Directory.CreateDirectory(outFolder);
        foreach (var target in StoreTargetLibrary.All)
        {
            string file = Path.Combine(blanks, target.Id, "blank.png");
            if (!File.Exists(file))
            {
                error.WriteLine($"{target.Id}: no blank.png in {Path.Combine(blanks, target.Id)}");
                return 1;
            }

            using var blank = Cv2.ImRead(file, ImreadModes.Color);
            var fp = Make(target.Id, blank);
            byte[] bytes = fp.ToBytes();
            File.WriteAllBytes(Path.Combine(outFolder, target.Id + ".glfp"), bytes);
            output.WriteLine(string.Create(Inv,
                $"{target.Id}: {fp.Points.Count} features, {fp.Bulls.Count} bulls at {string.Join(" ", fp.Bulls.Select(b => $"({b.X:0.00}, {b.Y:0.00})"))}, layout {fp.Layout.Cols} x {fp.Layout.Rows} from ({fp.Layout.X0:0.00}, {fp.Layout.Y0:0.00}), {bytes.Length / 1024.0:0.0} KB"));
        }

        return 0;
    }

    /// <summary>Each picture through recognition with the shipped fingerprints: every product's numbers and what was decided.</summary>
    private static int Recognize(IEnumerable<string> pictures, TextWriter output)
    {
        var backend = new OpenCvFingerprintBackend();
        foreach (string picture in pictures)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (StoreTargetRecognizer.Recognize(picture, backend) is not { } seen)
            {
                output.WriteLine($"{Path.GetFileName(picture)}: not an image");
                continue;
            }

            output.WriteLine(string.Create(Inv, $"{Path.GetFileName(picture)}: {seen.Describe()} ({watch.ElapsedMilliseconds} ms)"));
            foreach (var c in seen.Candidates)
            {
                string where = "";
                if (c.ToImage is { } h && c.Target.Fingerprint.Bulls.Count > 0)
                {
                    var bull = c.Target.Fingerprint.Bulls[0];
                    var (xx, xy, yx, yy) = h.Jacobian(bull);
                    var at = h.Apply(bull);
                    where = string.Create(Inv, $", first bull at ({at.X:0}, {at.Y:0}) px, {Math.Sqrt(Math.Abs((xx * yy) - (xy * yx))):0.0} px an inch there");
                }

                output.WriteLine(string.Create(Inv, $"   {c.Target.Id}: {c.Inliers} features, layout {c.Layout:0.000}, trimmed {c.Trimmed:0.000}{where}"));
            }
        }

        return 0;
    }

    /// <summary>A fingerprint of one blank scanned at 600 dpi.</summary>
    public static TargetFingerprint Make(string product, Mat blank) => Make(product, blank, ScanDpi);

    /// <summary>
    /// A fingerprint of one blank at <paramref name="sourceDpi"/> pixels an inch: a scan, or a photograph straightened to the target (entry
    /// 344), where <paramref name="bulls"/> are the aim points the person confirmed, in inches, in place of the red ones found.
    /// </summary>
    public static TargetFingerprint Make(string product, Mat blank, double sourceDpi, IReadOnlyList<PointD>? bulls = null)
    {
        ArgumentNullException.ThrowIfNull(blank);

        // Two resolutions, so a target seen from three feet, at a third of the detail, still finds features made at its own scale.
        var points = new List<PointD>();
        var parts = new List<byte[]>();
        using var detector = OpenCvFingerprintBackend.Orb(20000);
        foreach (var (dpi, most) in new[] { (ReferenceDpi, FingerprintFeatures), (ReferenceDpi / 2, FingerprintFeatures / 2) })
        {
            using var small = new Mat();
            Cv2.Resize(blank, small, new Size(0, 0), dpi / sourceDpi, dpi / sourceDpi, InterpolationFlags.Area);
            using var gray = new Mat();
            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);

            // Specks of dust and toner on the scan are not on the next sheet: a slight blur, and a floor under the feature strength.
            Cv2.GaussianBlur(gray, gray, new Size(0, 0), 0.8);
            var kept = Spread(detector.Detect(gray), dpi / 2, most);
            using var part = new Mat();
            detector.Compute(gray, ref kept, part);
            parts.Add(OpenCvFingerprintBackend.Bytes(part));
            points.AddRange(kept.Select(k => new PointD(k.Pt.X / dpi, k.Pt.Y / dpi)));
        }

        byte[] descriptors = [.. parts.SelectMany(p => p)];
        return new TargetFingerprint(product, points, descriptors, 32, bulls ?? Bulls(blank, sourceDpi), MakeLayout(blank, sourceDpi));
    }

    private static ColourLayout MakeLayout(Mat blank, double sourceDpi)
    {
        var box = ContentBox(blank, sourceDpi);
        int cols = Math.Max(1, (int)Math.Round(box.Width / ColourLayout.Cell)), rows = Math.Max(1, (int)Math.Round(box.Height / ColourLayout.Cell));
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), LayoutDpi / sourceDpi, LayoutDpi / sourceDpi, InterpolationFlags.Area);
        using var lab = new Mat();
        Cv2.CvtColor(small, lab, ColorConversionCodes.BGR2Lab);
        Cv2.GaussianBlur(lab, lab, new Size(0, 0), LayoutSigma);
        var image = new LabImage(lab.Width, lab.Height, OpenCvFingerprintBackend.Bytes(lab));
        var values = new byte[cols * rows * 3];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var v = StoreTargetRecognizer.Sample(image, (box.X + ((col + 0.5) * ColourLayout.Cell)) * LayoutDpi, (box.Y + ((row + 0.5) * ColourLayout.Cell)) * LayoutDpi);
                int k = ((row * cols) + col) * 3;
                for (int c = 0; c < 3; c++)
                {
                    values[k + c] = (byte)Math.Clamp(v[c], 0, 255);
                }
            }
        }

        return new ColourLayout(box.X, box.Y, cols, rows, values);
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
    /// <summary>The aim points found on a blank at <paramref name="sourceDpi"/>, as <see cref="Make(string, Mat, double, IReadOnlyList{PointD})"/> proposes them.</summary>
    public static PointD[] Bulls(Mat blank, double sourceDpi)
    {
        const double dpi = 150;
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), dpi / sourceDpi, dpi / sourceDpi, InterpolationFlags.Area);
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

        var box = ContentBox(blank, sourceDpi);
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        byte[] gp = OpenCvFingerprintBackend.Bytes(gray);
        int w = gray.Width, h = gray.Height;
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();
        int n = Cv2.ConnectedComponentsWithStats(red, labels, stats, centroids);
        var bulls = new List<PointD>();
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
                bulls.Add(new PointD(cx / dpi, cy / dpi));
            }
        }

        return [.. bulls.OrderBy(p => p.Y).ThenBy(p => p.X)];
    }

    /// <summary>The printed area: the bounding box of everything not near white, in inches.</summary>
    private static Rect2d ContentBox(Mat blank, double sourceDpi)
    {
        using var small = new Mat();
        Cv2.Resize(blank, small, new Size(0, 0), 60 / sourceDpi, 60 / sourceDpi, InterpolationFlags.Area);
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        using var ink = new Mat();
        Cv2.Threshold(gray, ink, 200, 255, ThresholdTypes.BinaryInv);
        var r = Cv2.BoundingRect(ink);
        return new Rect2d(r.X / 60.0, r.Y / 60.0, r.Width / 60.0, r.Height / 60.0);
    }
}
