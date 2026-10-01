using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 322 section 1: a sheet 2 ft from the phone, 98 pixels an inch in the 8 megapixel working picture, gives
/// markers of 15.5 px that read at their own size, but the first pass sized the detector for a sheet across half the picture, 23.4 px, and
/// its size gates refused every one unread. A second pass sized for a sheet across a quarter of the picture reads them.
/// </summary>
public class FarSheetMarkersTests
{
    private static (GrayImage Image, GroupLab.Core.Gltd.Model.TargetDefinition Definition) Far(double feet)
    {
        var definition = BuiltIns.Load(Scoreboard.SyntheticSheetFile);
        var (flat, _, _) = Scoreboard.SyntheticPicture(definition, Scoreboard.DefaultSeeds[0]);
        return (Scoreboard.Far(feet).Degrade(flat, new Random(1), null)!, definition);
    }

    [Fact]
    public void TheFirstGuessAloneRefusesASheetTwoFeetAway()
    {
        var (image, _) = Far(2);
        var backend = new OpenCvSharpBackend();
        double guess = 0.5 * Math.Max(image.Width, image.Height) / 2794 * 40;
        Assert.Empty(backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers);
        Assert.True(LiveSheet.PhotographMarkers(image, backend).Count >= 30, "the second pass reads most of the 38 markers");
    }

    [Fact]
    public void ASheetTwoFeetAwayRegisters()
    {
        var (image, definition) = Far(2);
        var metadata = new ImageMetadata("JPEG", image.Width, image.Height, null, null, null, null, null, null, null);
        var measured = SheetMeasurer.Measure(image, metadata, definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.NotNull(measured.Registration);
        Assert.True(measured.Fiducials!.Matches.Count >= 30, $"{measured.Fiducials.Matches.Count} of 38 markers matched");
    }

    [Fact]
    public void ASheetHalfTheFrameIsReadByTheFirstPassAsBefore()
    {
        // The second pass runs only when the first decodes none, so a sheet at the usual distance is read exactly as it was.
        var (image, _) = Far(1.25);
        var backend = new OpenCvSharpBackend();
        double guess = 0.5 * Math.Max(image.Width, image.Height) / 2794 * 40;
        var first = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, guess)).Markers;
        Assert.NotEmpty(first);
        Assert.Equal(first.Count, LiveSheet.PhotographMarkers(image, backend).Count);
    }
}
