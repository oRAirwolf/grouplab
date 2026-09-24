using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 157 section 4, the capture pieces built on the desktop before the mobile work starts: the off-axis angle and
/// the focal length from a homography, the paper's outline, the quality score, the refusal that names the angle, and the record kept with a
/// photograph. The synthetic cases photograph a rendered sheet through a known camera, so the truth is exact; the real ones hold what the
/// 2026-09-20 range photographs measured, where they are on this machine.
/// </summary>
public class CaptureTests
{
    private const double Width = 8.5, Height = 11;

    /// <summary>A camera of <paramref name="focal"/> pixels looking at a Letter page tilted and turned, page inches to image pixels.</summary>
    private static Homography Camera(double tiltDegrees, double turnDegrees, double focal, int width, int height, double distance, double rollDegrees = 3)
    {
        double t = tiltDegrees * Math.PI / 180, u = turnDegrees * Math.PI / 180, r = rollDegrees * Math.PI / 180;
        // R = Rz(roll) Ry(turn) Rx(tilt), applied to the page's x and y axes.
        double[] X(double x, double y, double z)
        {
            double y1 = (y * Math.Cos(t)) - (z * Math.Sin(t)), z1 = (y * Math.Sin(t)) + (z * Math.Cos(t));
            double x2 = (x * Math.Cos(u)) + (z1 * Math.Sin(u)), z2 = (-x * Math.Sin(u)) + (z1 * Math.Cos(u));
            return [(x2 * Math.Cos(r)) - (y1 * Math.Sin(r)), (x2 * Math.Sin(r)) + (y1 * Math.Cos(r)), z2];
        }

        var r1 = X(1, 0, 0);
        var r2 = X(0, 1, 0);
        var fromCenter = new Homography([1, 0, -Width / 2, 0, 1, -Height / 2, 0, 0, 1]);
        var pose = new Homography([r1[0], r2[0], 0, r1[1], r2[1], 0, r1[2], r2[2], distance]);
        var k = new Homography([focal, 0, (width - 1) / 2.0, 0, focal, (height - 1) / 2.0, 0, 0, 1]);
        return Homography.Compose(Homography.Compose(fromCenter, pose), k);
    }

    /// <summary>The angle between the page's normal and the camera's axis for that pose, worked out directly.</summary>
    private static double TrueTilt(double tiltDegrees, double turnDegrees)
    {
        double t = tiltDegrees * Math.PI / 180, u = turnDegrees * Math.PI / 180;
        return Math.Acos(Math.Cos(t) * Math.Cos(u)) * 180 / Math.PI;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 0)]
    [InlineData(30, 20)]
    [InlineData(45, 10)]
    [InlineData(60, 0)]
    public void TheAngleIsTheSheetsTiltToTheCamera(double tilt, double turn)
    {
        const int w = 3000, h = 4000;
        const double f = 3000;
        var camera = Camera(tilt, turn, f, w, h, 16);
        var angle = CameraGeometry.Measure(camera, w, h, f, FocalSource.Camera);
        Assert.Equal(TrueTilt(tilt, turn), angle.Degrees, 6);

        // The same sheet as four corners on a unit square gives the same angle, and its shape.
        var unit = Homography.Compose(new Homography([Width, 0, 0, 0, Height, 0, 0, 0, 1]), camera);
        var fromCorners = CameraGeometry.Measure(unit, w, h, f, FocalSource.Camera);
        Assert.Equal(angle.Degrees, fromCorners.Degrees, 6);
        Assert.Equal(Width / Height, fromCorners.Aspect, 6);
        Assert.Equal(("Letter", 8.5, 11.0), CameraGeometry.NearestPaper(fromCorners.Aspect));
    }

    /// <summary>
    /// The focal length solved from the sheet: from the markers' true lengths whatever the pose, and from a unit square only when the camera
    /// is turned about both axes, since a tilt about one side's axis puts a vanishing point at infinity.
    /// </summary>
    [Fact]
    public void TheFocalLengthIsSolvedWhereTheSheetSaysIt()
    {
        const int w = 3000, h = 4000;
        const double f = 2658;
        double diagonal = Math.Sqrt((w * w) + (h * h));
        var both = Camera(30, 20, f, w, h, 16, 0);
        Assert.Equal(f, CameraGeometry.SolveFocal(both, (w - 1) / 2.0, (h - 1) / 2.0, diagonal, trueLengths: true)!.Value, 0);
        var unit = Homography.Compose(new Homography([Width, 0, 0, 0, Height, 0, 0, 0, 1]), both);
        Assert.Equal(f, CameraGeometry.SolveFocal(unit, (w - 1) / 2.0, (h - 1) / 2.0, diagonal)!.Value, 0);

        var oneAxis = Homography.Compose(new Homography([Width, 0, 0, 0, Height, 0, 0, 0, 1]), Camera(30, 0, f, w, h, 16, 0));
        Assert.Null(CameraGeometry.SolveFocal(oneAxis, (w - 1) / 2.0, (h - 1) / 2.0, diagonal));

        // Without the file's focal length a nearly square photograph takes the assumed one, and a tilted one solves it.
        Assert.Equal(FocalSource.Assumed, CameraGeometry.Measure(Camera(10, 5, f, w, h, 16), w, h).Focal);
        var tilted = CameraGeometry.Measure(Camera(35, 20, f, w, h, 16), w, h, trueLengths: true);
        Assert.Equal(FocalSource.Solved, tilted.Focal);
        Assert.Equal(TrueTilt(35, 20), tilted.Degrees, 1);
    }

    /// <summary>A rendered sheet on a dark board, photographed at <paramref name="tilt"/> degrees by a 1500 by 2000 camera.</summary>
    private static (GrayImage Photograph, Homography Camera, ImageMetadata Metadata) Photograph(double tilt, double turn = 0, byte board = 70)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 100, margin = 14;
        var page = SceneRasterizer.Rasterize(SceneBuilder.Build(definition, new RenderOptions()).Pages[0], dpi);
        int cw = (int)Math.Round((Width + (2 * margin)) * dpi), ch = (int)Math.Round((Height + (2 * margin)) * dpi);
        var canvas = new byte[cw * ch];
        Array.Fill(canvas, board);
        int o = (int)Math.Round(margin * dpi);
        // Paper at 235, as a well exposed photograph shows it, not the render's 255, which the exposure part would read as blown out.
        for (int y = 0; y < page.Height; y++)
        {
            for (int x = 0; x < page.Width; x++)
            {
                canvas[((y + o) * cw) + o + x] = (byte)(page.Pixels[(y * page.Width) + x] * 235 / 255);
            }
        }

        const int w = 1500, h = 2000;
        double f = 26 * Math.Sqrt((w * w) + (h * h)) / CameraGeometry.FullFrameDiagonalMm;
        double distance = 14.5;
        var camera = Camera(tilt, turn, f, w, h, distance);
        var toImage = Homography.Compose(new Homography([1 / dpi, 0, (0.5 / dpi) - margin, 0, 1 / dpi, (0.5 / dpi) - margin, 0, 0, 1]), camera);
        var inverse = toImage.Inverse();
        var pixels = new byte[w * h];
        var source = new GrayImage(cw, ch, canvas);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var s = inverse.Apply(new PointD(x, y));
                int x0 = (int)Math.Floor(s.X), y0 = (int)Math.Floor(s.Y);
                if (x0 < 0 || y0 < 0 || x0 >= cw - 1 || y0 >= ch - 1)
                {
                    pixels[(y * w) + x] = board;
                    continue;
                }

                double fx = s.X - x0, fy = s.Y - y0;
                double top = source[x0, y0] + ((source[x0 + 1, y0] - source[x0, y0]) * fx);
                double bottom = source[x0, y0 + 1] + ((source[x0 + 1, y0 + 1] - source[x0, y0 + 1]) * fx);
                pixels[(y * w) + x] = (byte)Math.Clamp((int)Math.Round(top + ((bottom - top) * fy)), 0, 255);
            }
        }

        return (new GrayImage(w, h, pixels), camera, new ImageMetadata("synthetic", w, h, null, null, null, null, null, 4.3, 26));
    }

    /// <summary>Section 4 item 1: the paper's corners on a dark board, to a fraction of a pixel, and the angle they give.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(25, 10)]
    [InlineData(45, 0)]
    public void ThePapersCornersAreFoundOnADarkBoard(double tilt, double turn)
    {
        var (photograph, camera, metadata) = Photograph(tilt, turn);
        var quad = SheetOutline.Find(photograph, out string? why);
        Assert.True(quad is not null, why);
        PointD[] truth = [camera.Apply(new(0, 0)), camera.Apply(new(Width, 0)), camera.Apply(new(Width, Height)), camera.Apply(new(0, Height))];
        foreach (var (found, expected) in quad.Corners.Zip(truth))
        {
            Assert.InRange(Math.Sqrt(Math.Pow(found.X - expected.X, 2) + Math.Pow(found.Y - expected.Y, 2)), 0, 1.0);
        }

        Assert.Equal(TrueTilt(tilt, turn), CameraGeometry.Measure(quad.FromUnitSquare(), photograph.Width, photograph.Height, metadata).Degrees, 0);
    }

    /// <summary>Section 4 item 1's refusals, each saying why.</summary>
    [Fact]
    public void WhereThereIsNoSheetToFindItSaysWhy()
    {
        var blank = new GrayImage(800, 600, Enumerable.Repeat((byte)128, 800 * 600).ToArray());
        Assert.Null(SheetOutline.Find(blank, out string? why));
        Assert.Equal("no sheet stands out from what is behind it", why);

        // The same sheet with a camera too close to hold it: its corners are outside the frame.
        var (photograph, _, _) = Photograph(0);
        var crop = new byte[900 * 1200];
        for (int y = 0; y < 1200; y++)
        {
            Array.Copy(photograph.Pixels, ((y + 400) * photograph.Width) + 300, crop, y * 900, 900);
        }

        Assert.Null(SheetOutline.Find(new GrayImage(900, 1200, crop), out why));
        Assert.Equal("the sheet runs out of the frame, so it has no four corners to find", why);
    }

    /// <summary>Section 5: the score's parts respond to what they measure, and the weakest sets the score.</summary>
    [Fact]
    public void TheQualityScoreIsItsWeakestPart()
    {
        var (sharp, camera, _) = Photograph(0);
        // This small camera gives about 100 pixels an inch, which is what holds the sharp, well exposed photograph back.
        var good = CaptureQualities.Measure(sharp, camera, Width, Height, 0, 38, 38);
        Assert.True(good.Weakest == "resolution", good.Describe());
        Assert.Equal(1, good.FocusPart);
        Assert.Equal(1, good.ExposurePart);
        Assert.Equal((int)Math.Round(100 * good.ResolutionPart, MidpointRounding.AwayFromZero), good.Score);
        Assert.InRange(good.BlurInches!.Value, 0, CaptureQualities.SharpBlurInches);

        // Blurred by a 7 pixel box: the focus part falls and names itself.
        var blurred = Box(sharp, 3);
        var soft = CaptureQualities.Measure(blurred, camera, Width, Height, 0, 38, 38);
        Assert.True(soft.BlurInches > good.BlurInches * 2, $"{soft.BlurInches} against {good.BlurInches}");
        Assert.True(soft.FocusPart < good.FocusPart);

        // Overexposed: the paper blown out at 255.
        var bright = new GrayImage(sharp.Width, sharp.Height, [.. sharp.Pixels.Select(p => (byte)Math.Min(255, p + 40))]);
        var blown = CaptureQualities.Measure(bright, camera, Width, Height, 0, 38, 38);
        Assert.Equal(0, blown.ExposurePart);
        Assert.Equal("poor", blown.Words);
        Assert.Equal("exposure", blown.Weakest);

        // Off square and with markers missing.
        Assert.Equal("angle", CaptureQualities.Measure(sharp, camera, Width, Height, 35, 38, 38).Weakest);
        Assert.Equal("markings read", CaptureQualities.Measure(sharp, camera, Width, Height, 0, 20, 38).Weakest);
        Assert.Equal(("good", "usable", "poor"), (CaptureQualities.Words(70), CaptureQualities.Words(40), CaptureQualities.Words(39)));
    }

    private static GrayImage Box(GrayImage image, int r)
    {
        int w = image.Width, h = image.Height;
        var result = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sum = 0, n = 0;
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int xx = Math.Clamp(x + dx, 0, w - 1), yy = Math.Clamp(y + dy, 0, h - 1);
                        sum += image[xx, yy];
                        n++;
                    }
                }

                result[(y * w) + x] = (byte)(sum / n);
            }
        }

        return new GrayImage(w, h, result);
    }

    /// <summary>Section 3 item 4: a refusal a person can act on names the angle and the limit.</summary>
    [Fact]
    public void TheRefusalNamesTheAngleAndTheLimit()
    {
        Assert.Null(OffAxisLimit.Refusal(OffAxisLimit.Degrees));
        Assert.Equal("This photograph was taken 52 degrees off square to the sheet, and GroupLab corrects up to 40 degrees. Hold the camera more squarely over the sheet and take it again.",
            OffAxisLimit.Refusal(52.3));
    }

    /// <summary>Sections 3 and 5 in the pipeline: a photograph beyond the limit is refused before anything is measured, one inside it carries its record.</summary>
    [Fact]
    public void TheAutomaticPathRefusesAPhotographTooFarOffSquare()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (steep, _, metadata) = Photograph(50);
        var refused = AutomaticMarking.Run(steep, steep, metadata, definition, new OpenCvSharpBackend());
        Assert.StartsWith("This photograph was taken 50 degrees off square", refused.Failure, StringComparison.Ordinal);
        Assert.Equal(50, refused.Capture!.OffAxisDegrees, 0);

        var (square, _, _) = Photograph(15);
        var kept = AutomaticMarking.Run(square, square, metadata, definition, new OpenCvSharpBackend());
        Assert.Null(kept.Failure);
        Assert.Equal(15, kept.Capture!.OffAxisDegrees, 0);
        Assert.Contains("photographed 15 degrees off square", kept.Summary, StringComparison.Ordinal);
        Assert.Equal("the camera's own focal length", kept.Capture.FocalSource);
    }

    /// <summary>Section 3 item 5: the record goes into the marking file and comes back, and nothing in it is a place or a time.</summary>
    [Fact]
    public void TheRecordIsKeptWithTheMarking()
    {
        var quality = new CaptureQuality(74, "good", 0.0036, 1, 0, 231, 1, 12.1, 0.93, 212, 1, 26, 34, 0.74);
        var record = new CaptureRecord("main camera", 4.3, 26, 12.1, "the camera's own focal length", "homography with radial distortion", -0.01, 0.002, quality);
        var state = MarkingState.Empty with { ImagePath = "photo.jpg", Capture = record };
        string json = MarkingFile.Write(state);
        Assert.DoesNotContain("gps", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("latitude", json, StringComparison.OrdinalIgnoreCase);
        var back = MarkingFile.Read(json).State.Capture!;
        Assert.Equal(record with { Quality = back.Quality }, back);
        Assert.Equal(quality, back.Quality);
    }

    public static TheoryData<string, double> RangeAngles => new()
    {
        { "20260920_153325.jpg", 3.1 },
        { "20260920_165627.jpg", 12.1 },
        { "20260920_153336.jpg", 32.4 },
    };

    /// <summary>What the 2026-09-20 range photographs measured, held so a change that moves them is seen: the markers' angle for three of them.</summary>
    [Theory]
    [MemberData(nameof(RangeAngles))]
    public void TheRangePhotographsKeepTheirAngles(string file, double degrees)
    {
        string path = Path.Combine(@"C:\Dev\grouplab-range-2026-09-20\photos", file);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {file} is not on this machine");
            return;
        }

        var (grey, metadata) = ImageLoader.Load(path);
        var result = AutomaticMarking.Run(grey, grey, metadata, BuiltIns.Load("GL-CF25-LTR-D.gltd.json"), new OpenCvSharpBackend());
        Assert.Equal(degrees, result.Capture!.OffAxisDegrees, 0.15);
    }

    /// <summary>The one owner photograph with a whole sheet in frame, on a darker mat: its outline, where it is on this machine.</summary>
    [Fact]
    public void ARealSheetOnAMatIsOutlined()
    {
        string path = TestData.Path("20260329_183028.jpg", @"C:\Dev\grouplab-testdata\owner");
        if (!File.Exists(path))
        {
            Assert.True(true, "skipped: the owner photograph is not on this machine");
            return;
        }

        var (grey, _) = ImageLoader.Load(path);
        var quad = SheetOutline.Find(grey, out string? why);
        Assert.True(quad is not null, why);
        PointD[] recorded = [new(293, 167), new(3762, 34), new(3755, 2769), new(226, 2792)];
        foreach (var (found, expected) in quad.Corners.Zip(recorded))
        {
            Assert.InRange(Math.Sqrt(Math.Pow(found.X - expected.X, 2) + Math.Pow(found.Y - expected.Y, 2)), 0, 3);
        }
    }
}
