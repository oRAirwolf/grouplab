using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab outline-trial</c>, NOTES-FROM-PLANNING.md entry 362 section 2: each photograph read upright, as the store-bought target's
/// screen reads it, the GroupLab sheet's outline and the store-bought target's tried on it, and a small picture written with the corners each
/// found, to be checked by eye. Nothing else is written.
/// </summary>
public static class OutlineTrial
{
    public const string Usage = "grouplab outline-trial <photo>... -o <folder>";

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        int o = args.ToList().IndexOf("-o");
        if (o < 1 || o + 1 >= args.Count)
        {
            error.WriteLine(Usage);
            return 2;
        }

        string folder = args[o + 1];
        Directory.CreateDirectory(folder);
        foreach (string path in args.Take(o))
        {
            var (grey, metadata) = ImageLoader.Load(path, null);
            using var stored = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
            using var colour = UprightMat.Apply(stored, metadata.Orientation);
            var upright = Upright.Apply(grey, metadata.Orientation);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var sheet = SheetOutline.Find(upright, out string? sheetWhy);
            var outline = StoreTargetOutline.Find(colour, scan: metadata is { IsCamera: false, DpiX: >= 150 });
            watch.Stop();
            string name = Path.GetFileNameWithoutExtension(path);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{name}: orientation {metadata.Orientation?.ToString(CultureInfo.InvariantCulture) ?? "none"}, {colour.Width}x{colour.Height}; sheet outline: {(sheet is null ? "refused, " + sheetWhy : "found")}; store outline: {outline.Said} ({watch.ElapsedMilliseconds} ms)"));
            foreach (string line in outline.Tried)
            {
                output.WriteLine("    " + line);
            }

            {
                output.WriteLine("    corners " + string.Join(" ", outline.Corners.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.X:0},{p.Y:0}"))));
            }

            double k = 1000.0 / Math.Max(colour.Width, colour.Height);
            using var small = new Mat();
            Cv2.Resize(colour, small, new Size(0, 0), k, k, InterpolationFlags.Area);
            if (sheet is not null)
            {
                Draw(small, sheet.Corners, k, Scalar.Red);
            }

            Draw(small, outline.Corners, k, outline.Found ? new Scalar(0, 200, 0) : new Scalar(0, 200, 255));
            Cv2.ImWrite(Path.Combine(folder, name + ".jpg"), small);
            StoreTargetOutline.Picture(colour, Path.Combine(folder, name + "-edges.png"));
            foreach (var (i, p) in outline.Corners.Select((p, i) => (i, p)))
            {
                int r = 60;
                var rect = new Rect((int)Math.Clamp(p.X - r, 0, colour.Width - (2 * r)), (int)Math.Clamp(p.Y - r, 0, colour.Height - (2 * r)), 2 * r, 2 * r);
                using var crop = new Mat(colour, rect).Clone();
                Cv2.Circle(crop, new Point(p.X - rect.X, p.Y - rect.Y), 3, new Scalar(0, 0, 255), -1);
                using var big = new Mat();
                Cv2.Resize(crop, big, new Size(0, 0), 2, 2, InterpolationFlags.Nearest);
                Cv2.ImWrite(Path.Combine(folder, string.Create(CultureInfo.InvariantCulture, $"{name}-corner{i}.jpg")), big);
            }
        }

        return 0;
    }

    private static void Draw(Mat image, IReadOnlyList<PointD> corners, double k, Scalar colour)
    {
        if (corners.Count != 4)
        {
            return;
        }

        var points = corners.Select(p => new Point(p.X * k, p.Y * k)).ToArray();
        Cv2.Polylines(image, [points], true, colour, 2);
        foreach (var p in points)
        {
            Cv2.Circle(image, p, 5, colour, 2);
        }
    }
}
