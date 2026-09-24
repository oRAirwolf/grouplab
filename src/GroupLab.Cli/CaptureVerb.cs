using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab capture-check</c>, NOTES-FROM-PLANNING.md entry 157 section 4: what the capture pieces measure on real photographs, and a
/// synthetic sweep of the off-axis angle. For each photograph, the paper's outline and the angle it gives; on a GroupLab sheet, the angle
/// its markers give, how far a perspective corrected from the paper's corners alone lands from the markers' across the page, and the
/// quality score. The sweep tilts a rendered sheet through a known camera and reports, at each angle, the outline's corner error, both
/// angles against the truth, and whether the markers still register and how far out the worst bull is. Nothing is written but the table.
/// </summary>
public static class CaptureVerb
{
    private const double DmmPerInch = 254;

    public const string Usage = "grouplab capture-check <image>... [--library <directory>] [--sweep]";

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var images = new List<string>();
        var libraries = new List<string>();
        bool sweep = false;
        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--library" when i + 1 < args.Count:
                    libraries.Add(args[++i]);
                    break;
                case "--sweep":
                    sweep = true;
                    break;
                default:
                    images.Add(args[i]);
                    break;
            }
        }

        if (sweep)
        {
            Sweep(libraries.Count > 0 ? libraries : [AnalyzeVerb.DefaultLibrary], output);
            return 0;
        }

        if (images.Count == 0)
        {
            error.WriteLine("capture-check: name at least one image, or --sweep");
            error.WriteLine(Usage);
            return 2;
        }

        var inv = CultureInfo.InvariantCulture;
        output.WriteLine("image | outline | outline angle | marker angle | focal | corners against markers, in | quality");
        var backend = new OpenCvSharpBackend();
        var candidates = SheetIdentification.Candidates(libraries.Count > 0 ? libraries : [AnalyzeVerb.DefaultLibrary]);
        foreach (string path in images)
        {
            string name = Path.GetFileName(path);
            GrayImage grey;
            ImageMetadata metadata;
            try
            {
                (grey, metadata) = ImageLoader.Load(path);
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or OpenCvSharp.OpenCVException)
            {
                output.WriteLine($"{name} | could not be read: {ex.Message}");
                continue;
            }

            var quad = SheetOutline.Find(grey, out string? why);
            var outlineAngle = quad is null ? null : CameraGeometry.Measure(quad.FromUnitSquare(), grey.Width, grey.Height, metadata);
            string outline = quad is null ? "none: " + why : string.Create(inv, $"found, edges {quad.EdgeRmsPixels:0.0} px rms, {100 * quad.FrameShare:0} percent of the frame, corners {string.Join(" ", quad.Corners.Select(c => $"{c.X:0},{c.Y:0}"))}");
            string outlineText = outlineAngle is null ? "" : string.Create(inv, $"{outlineAngle.Degrees:0.0} deg");

            var identity = SheetIdentification.Identify(grey, candidates, backend, new TraceRecorder());
            if (identity.Definition is not { } definition)
            {
                output.WriteLine($"{name} | {outline} | {outlineText} | not a GroupLab sheet it could read | {Focal(outlineAngle)} | | {(outlineAngle is null ? "" : "no size to score against")}");
                continue;
            }

            var measurement = SheetMeasurer.Measure(grey, metadata, definition, new MeasureOptions(), backend);
            if (measurement.Registration is not { } registration)
            {
                output.WriteLine($"{name} | {outline} | {outlineText} | did not register: {measurement.Failure} | {Focal(outlineAngle)} | |");
                continue;
            }

            double widthInches = definition.Page.Width / DmmPerInch, heightInches = definition.Page.Height / DmmPerInch;
            var pageToImage = CaptureRecord.PageToImage(registration.Mapping, definition.Page.Width, definition.Page.Height);
            var markerAngle = CameraGeometry.Measure(pageToImage, grey.Width, grey.Height, metadata, trueLengths: true);
            var markerBare = CameraGeometry.Measure(pageToImage, grey.Width, grey.Height, null, trueLengths: true);
            string corners = "";
            if (quad is not null)
            {
                // A perspective from the paper's four corners and its known size, against the markers' across a grid of the page.
                var fromCorners = HomographyEstimate.Fit([new(0, 0), new(widthInches, 0), new(widthInches, heightInches), new(0, heightInches)], quad.Corners);
                if (fromCorners is not null)
                {
                    var errors = Grid(widthInches, heightInches).Select(p => pageToImage.Apply(p)).Select(image =>
                    {
                        var a = fromCorners.Inverse().Apply(image);
                        var b = registration.Mapping.ToPage(image);
                        return Math.Sqrt(Math.Pow(a.X - (b.X / DmmPerInch), 2) + Math.Pow(a.Y - (b.Y / DmmPerInch), 2));
                    }).Order().ToList();
                    corners = string.Create(inv, $"median {errors[errors.Count / 2]:0.000}, worst {errors[^1]:0.000}");
                }
            }

            int expected = measurement.Fiducials?.Expected ?? 0, read = measurement.Fiducials?.Matches.Count ?? 0;
            var quality = CaptureQualities.Measure(grey, pageToImage, widthInches, heightInches, markerAngle.Degrees, read, expected);
            output.WriteLine(string.Create(inv,
                $"{name} | {outline} | {outlineText} | {markerAngle.Degrees:0.0} deg, {read} of {expected} markers | {Focal(markerAngle)}; without the file's, {markerBare.Degrees:0.0} deg by {Focal(markerBare)} | {corners} | {quality.Describe()}"));
        }

        return 0;
    }

    private static string Focal(OffAxis? angle) => angle is null ? "" : string.Create(CultureInfo.InvariantCulture, $"{CameraGeometry.Describe(angle.Focal)}, {angle.FocalPixels:0} px");

    private static IEnumerable<PointD> Grid(double width, double height)
    {
        for (int i = 1; i < 10; i++)
        {
            for (int j = 1; j < 10; j++)
            {
                yield return new PointD(width * i / 10, height * j / 10);
            }
        }
    }

    /// <summary>
    /// The sweep: GL-CF25-LTR rendered at 150 dpi on a dark board, photographed by a 4000 by 3000 camera of 26 mm equivalent from 11 in, tilted
    /// about the sheet's horizontal axis from 0 to 75 degrees, with a small roll so no edge lies along the pixel grid.
    /// </summary>
    private static void Sweep(IReadOnlyList<string> libraries, TextWriter output)
    {
        var inv = CultureInfo.InvariantCulture;
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Path.Combine(libraries[0], "GL-CF25-LTR.gltd.json")).Definition
            ?? throw new InvalidOperationException("GL-CF25-LTR.gltd.json is not in " + libraries[0]);
        const double dpi = 150, margin = 12, board = 70;
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition, new RenderOptions()).Pages[0], dpi);
        double widthInches = definition.Page.Width / DmmPerInch, heightInches = definition.Page.Height / DmmPerInch;
        int cw = (int)Math.Round((widthInches + (2 * margin)) * dpi), ch = (int)Math.Round((heightInches + (2 * margin)) * dpi);
        var canvas = new byte[cw * ch];
        Array.Fill(canvas, (byte)board);
        int ox = (int)Math.Round(margin * dpi), oy = (int)Math.Round(margin * dpi);
        for (int y = 0; y < page.Height; y++)
        {
            Array.Copy(page.Pixels, y * page.Width, canvas, ((y + oy) * cw) + ox, page.Width);
        }

        var board2 = new GrayImage(cw, ch, canvas);
        const int width = 3000, height = 4000;
        double f = 26 * Math.Sqrt((width * width) + (height * height)) / CameraGeometry.FullFrameDiagonalMm, cx = (width - 1) / 2.0, cy = (height - 1) / 2.0;
        var metadata = new ImageMetadata("synthetic", width, height, null, null, null, null, null, 4.3, 26);
        var backend = new OpenCvSharpBackend();
        output.WriteLine("tilt | distance, in | outline corners, px | outline angle | solved focal | angle without metadata | marker angle | markers | worst bull, in | least pixels an inch");
        for (int tilt = 0; tilt <= 75; tilt += 5)
        {
            double t = tilt * Math.PI / 180, roll = 3 * Math.PI / 180;
            double cr = Math.Cos(roll), sr = Math.Sin(roll);

            // Page inches, (0, 0) at the top left, to image pixels: K R [u v 0]^T + t, with u and v from the page's center.
            double[] r1 = [cr, sr, 0], r2 = [-sr * Math.Cos(t), cr * Math.Cos(t), Math.Sin(t)];
            // The camera stands back until the tilted sheet fits inside the middle nine tenths of the frame, as a person would step back.
            var fromCenter = new Homography([1, 0, -widthInches / 2, 0, 1, -heightInches / 2, 0, 0, 1]);
            Homography Camera(double d) => Homography.Compose(Homography.Compose(fromCenter, new Homography([r1[0], r2[0], 0, r1[1], r2[1], 0, r1[2], r2[2], d])), new Homography([f, 0, cx, 0, f, cy, 0, 0, 1]));
            double distance = heightInches * f / (0.75 * height);
            while (new PointD[] { new(0, 0), new(widthInches, 0), new(widthInches, heightInches), new(0, heightInches) }
                .Select(p => Camera(distance).Apply(p)).Any(p => p.X < 0.05 * width || p.X > 0.95 * width || p.Y < 0.05 * height || p.Y > 0.95 * height))
            {
                distance *= 1.05;
            }

            var camera = Camera(distance);
            var canvasToPage = new Homography([1 / dpi, 0, (0.5 / dpi) - margin, 0, 1 / dpi, (0.5 / dpi) - margin, 0, 0, 1]);
            var photograph = Warp(board2, Homography.Compose(canvasToPage, camera), width, height, (byte)board);

            PointD[] truth = [camera.Apply(new(0, 0)), camera.Apply(new(widthInches, 0)), camera.Apply(new(widthInches, heightInches)), camera.Apply(new(0, heightInches))];
            var quad = SheetOutline.Find(photograph, out string? why);
            string cornerText = quad is null ? "none: " + why : string.Create(inv, $"{quad.Corners.Zip(truth).Max(p => Math.Sqrt(Math.Pow(p.First.X - p.Second.X, 2) + Math.Pow(p.First.Y - p.Second.Y, 2))):0.00}");
            string outlineAngle = "", solved = "", bare = "";
            if (quad is not null)
            {
                var h = quad.FromUnitSquare();
                outlineAngle = string.Create(inv, $"{CameraGeometry.Measure(h, width, height, metadata).Degrees:0.00}");
                solved = CameraGeometry.SolveFocal(h, cx, cy, Math.Sqrt((width * width) + (height * height))) is { } s ? string.Create(inv, $"{s / f:0.000} of true") : "not solvable";
                var without = CameraGeometry.Measure(h, width, height);
                bare = string.Create(inv, $"{without.Degrees:0.00}, {without.Focal}");
            }

            var measurement = SheetMeasurer.Measure(photograph, metadata, definition, new MeasureOptions(), backend);
            string markerAngle = "", markers = "", bull = "";
            if (measurement.Registration is { } registration)
            {
                markerAngle = string.Create(inv, $"{CameraGeometry.Measure(CaptureRecord.PageToImage(registration.Mapping, definition.Page.Width, definition.Page.Height), width, height, metadata).Degrees:0.00}");
                markers = string.Create(inv, $"{measurement.Fiducials?.Matches.Count ?? 0} of {measurement.Fiducials?.Expected ?? 0}");
                bull = double.IsNaN(measurement.WorstError) ? "none located" : string.Create(inv, $"{measurement.WorstError / DmmPerInch:0.0000}, {measurement.Bulls.Count(b => b.Recovered is not null)} of {measurement.Bulls.Count} bulls");
            }
            else
            {
                markers = "did not register: " + measurement.Failure;
            }

            string least = string.Create(inv, $"{CaptureQualities.LeastPixelsPerInch(camera, widthInches, heightInches):0}");
            output.WriteLine(string.Create(inv, $"{tilt} | {distance:0.0} | {cornerText} | {outlineAngle} | {solved} | {bare} | {markerAngle} | {markers} | {bull} | {least}"));
        }
    }

    /// <summary>A bilinear warp whose outside is the board, not paper.</summary>
    private static GrayImage Warp(GrayImage source, Homography sourceToDestination, int width, int height, byte outside)
    {
        var inverse = sourceToDestination.Inverse();
        var pixels = new byte[width * height];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                var s = inverse.Apply(new PointD(x, y));
                int x0 = (int)Math.Floor(s.X), y0 = (int)Math.Floor(s.Y);
                if (x0 < 0 || y0 < 0 || x0 >= source.Width - 1 || y0 >= source.Height - 1)
                {
                    pixels[(y * width) + x] = outside;
                    continue;
                }

                double fx = s.X - x0, fy = s.Y - y0;
                double top = source[x0, y0] + ((source[x0 + 1, y0] - source[x0, y0]) * fx);
                double bottom = source[x0, y0 + 1] + ((source[x0 + 1, y0 + 1] - source[x0, y0 + 1]) * fx);
                pixels[(y * width) + x] = (byte)Math.Clamp((int)Math.Round(top + ((bottom - top) * fy)), 0, 255);
            }
        });
        return new GrayImage(width, height, pixels);
    }
}
