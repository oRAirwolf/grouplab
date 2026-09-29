using System.Diagnostics;
using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab capture-tune</c>, NOTES-FROM-PLANNING.md entry 291 sections 3.2, 3.3 and 3.5: the numbers the capture screen's guidance is
/// tuned by, from real pictures and from rendered ones. Each picture is taken as the phone's working copy and made as the phone would have
/// seen it from further away (<c>--distance</c>: the picture shrunk inside a frame of its own size, the rest the paper's surround) or less
/// steadily (<c>--blur</c>: a Gaussian or a sideways shake, in the picture's pixels). For each, the live frame is that picture at the
/// analysis stream's size, 1440 pixels across as the Fold 7 gives it, and the table says what the frame reads, what the guidance says, and
/// what the picture itself reads: its markers, its codes, and its holes. <c>--synthetic</c> does the same for a rendered sheet photographed
/// through a known camera at several distances. Nothing is written but the table; the pictures stay where they are.
/// </summary>
public static class CaptureTuneVerb
{
    public const string Usage = "grouplab capture-tune (--distance | --blur) <image>... | --synthetic";

    /// <summary>The analysis stream's longer side on the Fold 7, from its log: analysis=1440x1080.</summary>
    public const int LiveSide = 1440;

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        string mode = args.FirstOrDefault(a => a is "--distance" or "--blur" or "--synthetic") ?? "";
        var images = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
        if (mode.Length == 0 || (mode != "--synthetic" && images.Count == 0))
        {
            error.WriteLine(Usage);
            return 2;
        }

        var library = SheetIdentification.Candidates([AnalyzeVerb.DefaultLibrary]);
        var backend = new OpenCvSharpBackend();
        output.WriteLine("picture | step | printed room | module px | marker px | live markers | live codes | predicted | picture markers | picture codes, ms | holes | live blur px | old says | new says");
        if (mode == "--synthetic")
        {
            Synthetic(library, backend, output);
            return 0;
        }

        foreach (string path in images)
        {
            var (grey, metadata) = ImageLoader.Load(path);
            var definition = SheetIdentification.Identify(grey, library, backend, new TraceRecorder()).Definition;
            if (definition is null)
            {
                output.WriteLine($"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)} | not identified");
                continue;
            }

            string name = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileNameWithoutExtension(path);
            if (mode == "--distance")
            {
                foreach (double f in new[] { 1.0, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4 })
                {
                    Row(output, name, string.Create(CultureInfo.InvariantCulture, $"x{f:0.00}"), Shrunk(grey, f), metadata, definition, library, backend);
                }
            }
            else
            {
                foreach (var (label, blurred) in new (string, Func<GrayImage>)[]
                {
                    ("none", () => grey),
                    ("gauss 1", () => Gauss(grey, 1)), ("gauss 1.5", () => Gauss(grey, 1.5)), ("gauss 2", () => Gauss(grey, 2)),
                    ("shake 4", () => Shake(grey, 4)), ("shake 6", () => Shake(grey, 6)), ("shake 8", () => Shake(grey, 8)),
                })
                {
                    Row(output, name, label, blurred(), metadata, definition, library, backend);
                }
            }
        }

        return 0;
    }

    private static void Row(TextWriter output, string name, string step, GrayImage picture, ImageMetadata metadata, TargetDefinition definition, IReadOnlyList<TargetDefinition> library, IImagingBackend backend)
    {
        var inv = CultureInfo.InvariantCulture;
        var live = Resize(picture, (double)LiveSide / Math.Max(picture.Width, picture.Height));
        double scale = (double)Math.Max(picture.Width, picture.Height) / LiveSide;
        var liveMeta = new ImageMetadata("YUV", live.Width, live.Height, null, null, "camera", "analysis", 1, null, null);
        var codes = LiveSheet.Find(live, library, backend).CodesRead;
        var verdict = CaptureGuidance.JudgeFrame(live, liveMeta, definition, backend, codes);
        var said = new GuidanceSteadier().Instant(verdict, scale).Say;

        var clock = Stopwatch.StartNew();
        var identity = SheetIdentification.Identify(picture, library, backend, new TraceRecorder());
        long identifyMs = clock.ElapsedMilliseconds;
        var pictureMeta = metadata with { Width = picture.Width, Height = picture.Height };
        var marked = identity.Definition is { } named ? AutomaticMarking.Run(picture, picture, pictureMeta, named, backend) : null;
        string markers = marked?.Measurement.Fiducials is { } f ? $"{f.Matches.Count} of {f.Expected}" : "none";
        string holes = marked is null ? "no sheet" : marked.Failure is { } why ? "failed" : marked.Detections.Count.ToString(inv);
        double? blurPx = verdict.FrameBlurPixels;
        output.WriteLine(string.Create(inv,
            $"{name} | {step} | {verdict.PrintedRoom:0.000} | {verdict.ModulePixels * scale:0.0} | {verdict.MarkerPixels * scale:0.0} | {verdict.MarkersRead} | {codes} | {verdict.MarkersPredicted(scale)} | {markers} | {identity.CodesRead}, {identifyMs} | {holes} | {blurPx:0.00} | {Old(verdict, scale)} | {said}"));
    }

    /// <summary>What the guidance of nightly 124 said to a frame, before entry 291: the paper's corners with 2 percent room, 150 pixels an inch, the focus part, and the markers read.</summary>
    private static Instruction Old(FrameVerdict raw, double scale)
    {
        if (raw.Quality is not { } q)
        {
            return raw.Say;
        }

        return raw.EdgeRoom is { } room && room < GuidanceSteadier.EnterEdgeRoom ? Instruction.MoveBack
            : q.LeastPixelsPerInch * scale < CaptureQualities.FinePixelsPerInch ? Instruction.MoveCloser
            : q.OffAxisDegrees > OffAxisLimit.Degrees ? Instruction.LessAngle
            : !raw.InFocus ? Instruction.HoldSteadier
            : !raw.ExposureWithin ? Instruction.MoreLight
            : !raw.MarkingsRead ? Instruction.HoldSteadier
            : Instruction.Ready;
    }

    /// <summary>The picture made <paramref name="factor"/> times as large inside a frame of its own size, as from further back: the rest of the frame the median of its edge.</summary>
    internal static GrayImage Shrunk(GrayImage picture, double factor)
    {
        if (factor >= 1)
        {
            return picture;
        }

        var small = Resize(picture, factor);
        var edge = new List<byte>();
        for (int x = 0; x < picture.Width; x += 7)
        {
            edge.Add(picture[x, 0]);
            edge.Add(picture[x, picture.Height - 1]);
        }

        edge.Sort();
        var pixels = new byte[picture.Width * picture.Height];
        Array.Fill(pixels, edge[edge.Count / 2]);
        int ox = (picture.Width - small.Width) / 2, oy = (picture.Height - small.Height) / 2;
        for (int y = 0; y < small.Height; y++)
        {
            Array.Copy(small.Pixels, y * small.Width, pixels, ((y + oy) * picture.Width) + ox, small.Width);
        }

        return new GrayImage(picture.Width, picture.Height, pixels);
    }

    internal static GrayImage Resize(GrayImage image, double factor)
    {
        using var source = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var resized = new Mat();
        Cv2.Resize(source, resized, new OpenCvSharp.Size(0, 0), factor, factor, factor < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
        return Grey(resized);
    }

    private static GrayImage Gauss(GrayImage image, double sigma)
    {
        using var source = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var blurred = new Mat();
        Cv2.GaussianBlur(source, blurred, new OpenCvSharp.Size(0, 0), sigma);
        return Grey(blurred);
    }

    /// <summary>A sideways shake of <paramref name="length"/> pixels: every pixel the average of a line that long.</summary>
    private static GrayImage Shake(GrayImage image, int length)
    {
        using var source = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var blurred = new Mat();
        using var kernel = new Mat(1, length, MatType.CV_32FC1, new Scalar(1.0 / length));
        Cv2.Filter2D(source, blurred, -1, kernel);
        return Grey(blurred);
    }

    private static GrayImage Grey(Mat mat)
    {
        var pixels = new byte[mat.Rows * mat.Cols];
        using var continuous = mat.IsContinuous() ? mat.Clone() : mat.Clone();
        System.Runtime.InteropServices.Marshal.Copy(continuous.Data, pixels, 0, pixels.Length);
        return new GrayImage(mat.Cols, mat.Rows, pixels);
    }

    /// <summary>
    /// A rendered sheet on a grey board, photographed square on through a 26 mm equivalent lens onto a 3266 by 2449 picture from further and
    /// further away, softened as a phone's picture is (0.8 pixel) with a little noise.
    /// </summary>
    private static void Synthetic(IReadOnlyList<TargetDefinition> library, IImagingBackend backend, TextWriter output)
    {
        var definition = GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(Path.Combine(AnalyzeVerb.DefaultLibrary, "GL-CF25-LTR-D.gltd.json")).Definition
            ?? throw new InvalidOperationException("GL-CF25-LTR-D.gltd.json is not in " + AnalyzeVerb.DefaultLibrary);
        const double dpi = 300, margin = 6, board = 90;
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition, new RenderOptions()).Pages[0], dpi);
        double widthInches = definition.Page.Width / 254.0, heightInches = definition.Page.Height / 254.0;
        int cw = (int)Math.Round((widthInches + (2 * margin)) * dpi), ch = (int)Math.Round((heightInches + (2 * margin)) * dpi);
        var canvas = new byte[cw * ch];
        Array.Fill(canvas, (byte)board);
        int ox = (int)Math.Round(margin * dpi), oy = (int)Math.Round(margin * dpi);
        for (int y = 0; y < page.Height; y++)
        {
            Array.Copy(page.Pixels, y * page.Width, canvas, ((y + oy) * cw) + ox, page.Width);
        }

        var surface = new GrayImage(cw, ch, canvas);
        const int width = 3266, height = 2449;
        var metadata = new ImageMetadata("synthetic", width, height, null, null, null, null, null, 4.3, 26);
        var random = new Random(291);
        foreach (double across in new[] { 1.0, 0.95, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4 })
        {
            // The sheet's 11 inches along the frame's longer side, taking this share of it; the sheet turned a little, as a hand holds it.
            double k = across * width / heightInches;
            double turn = 3 * Math.PI / 180, c = Math.Cos(turn) * k, s = Math.Sin(turn) * k;
            // Page inches to picture: the sheet's long side along x, centered.
            var pageToPicture = new Homography([s, c, 0, -c, s, 0, 0, 0, 1]);
            var centre = pageToPicture.Apply(new PointD(widthInches / 2, heightInches / 2));
            pageToPicture = Homography.Compose(pageToPicture, new Homography([1, 0, (width / 2.0) - centre.X, 0, 1, (height / 2.0) - centre.Y, 0, 0, 1]));
            var canvasToPage = new Homography([1 / dpi, 0, (0.5 / dpi) - margin, 0, 1 / dpi, (0.5 / dpi) - margin, 0, 0, 1]);
            var picture = backend.WarpPerspective(surface, Homography.Compose(canvasToPage, pageToPicture), width, height);
            picture = Gauss(picture, 0.8);
            for (int i = 0; i < picture.Pixels.Length; i++)
            {
                // Paper at about 215, as a phone exposes it, not the render's 255, which reads as blown out.
                picture.Pixels[i] = (byte)Math.Clamp((picture.Pixels[i] * 0.85) + random.Next(-4, 5), 0, 255);
            }

            Row(output, "rendered", string.Create(CultureInfo.InvariantCulture, $"sheet {100 * across:0}% across"), picture, metadata, definition, library, backend);
        }
    }
}
