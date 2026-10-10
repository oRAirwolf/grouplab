using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 399 section 2: a photograph that lost its camera tags was taken for a scan and read with no lens fit, which
/// is how the proof checklist's off-axis figures came to be measured. An image with no focal length, stating no scanning resolution, whose
/// codes fit a flat page worse than <see cref="SheetMeasurer.UntaggedPhotographRmsDmm"/>, is now read as a photograph. A scan, and an
/// untagged image whose codes do fit a flat page, are read exactly as before.
/// </summary>
public class UntaggedPhotographTests
{
    private const int Width = 3240;
    private const int Height = 4190;

    [Fact]
    public void AnUntaggedPictureWhoseCodesCurveIsReadThroughTheLens()
    {
        var (fit, trace) = Register(Codes(distortion: -0.04), dpi: null);

        Assert.Equal("homography with radial distortion", fit.Mapping.Model);
        Assert.Contains(trace.Records.SelectMany(r => r.Decisions), d => d.Because.Contains("no camera tags", StringComparison.Ordinal));
    }

    [Fact]
    public void SeventyTwoDpiIsAPhotographToolsResolutionNotAScanners()
    {
        Assert.Equal("homography with radial distortion", Register(Codes(distortion: -0.04), dpi: 72).Fit.Mapping.Model);
    }

    [Fact]
    public void AScanStatingItsResolutionIsReadAsAScanWhateverItsCodesDo()
    {
        var (fit, trace) = Register(Codes(distortion: -0.04), dpi: 300);

        Assert.Equal("homography", fit.Mapping.Model);
        Assert.DoesNotContain(trace.Records.SelectMany(r => r.Decisions), d => d.Because.Contains("no camera tags", StringComparison.Ordinal));
    }

    [Fact]
    public void AnUntaggedPictureWhoseCodesFitAFlatPageStaysAHomography()
    {
        Assert.Equal("homography", Register(Codes(distortion: 0), dpi: null).Fit.Mapping.Model);
    }

    private static (RegistrationFit Fit, TraceRecorder Trace) Register(IReadOnlyList<MarkerMatch> matches, double? dpi)
    {
        var metadata = new ImageMetadata("JPEG", Width, Height, dpi, dpi, null, null, null, null, null);
        var fiducials = new FiducialResult(0, 1, matches.Count, matches, 0, 0, 0, 1.5, 80, [], []);
        var trace = new TraceRecorder();
        var fit = SheetMeasurer.Register(new GrayImage(Width, Height, new byte[Width * Height]), metadata, fiducials, new MeasureOptions(), new OpenCvSharpBackend(), trace);
        Assert.NotNull(fit);
        return (fit, trace);
    }

    /// <summary>A 6 by 6 grid of codes on a letter page, 1.5 pixels to the dmm, bent by a radial term about the image centre.</summary>
    private static List<MarkerMatch> Codes(double distortion)
    {
        double half = Math.Sqrt((Width * Width) + (Height * Height)) / 2;
        PointD ToImage(PointD page)
        {
            double x = (page.X * 1.5) - (Width / 2.0), y = (page.Y * 1.5) - (Height / 2.0);
            double r2 = ((x * x) + (y * y)) / (half * half);
            double f = 1 + (distortion * r2);
            return new PointD((Width / 2.0) + (x * f), (Height / 2.0) + (y * f));
        }

        var matches = new List<MarkerMatch>();
        int id = 0;
        for (int row = 0; row < 6; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                double cx = 150 + (column * 370), cy = 200 + (row * 470);
                PointD[] page = [new(cx - 40, cy - 40), new(cx + 40, cy - 40), new(cx + 40, cy + 40), new(cx - 40, cy + 40)];
                matches.Add(new MarkerMatch(id++, [.. page.Select(ToImage)], page));
            }
        }

        return matches;
    }
}
