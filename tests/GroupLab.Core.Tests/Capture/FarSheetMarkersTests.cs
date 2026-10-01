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
/// its size gates refused every one unread. A second pass sized for a sheet across a quarter of the picture reads them. At 3 ft the
/// markers are found as squares and none decodes, and each square is read again cut out and enlarged.
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
    public void ASheetThreeFeetAwayIsReadFromItsMarkersEnlarged()
    {
        // At 3 ft a marker is 10.3 px and its module 1.3 px: found as a square, never decoded at the picture's size.
        var (image, definition) = Far(3);
        var backend = new OpenCvSharpBackend();
        double side = 40 / 254.0 * Scoreboard.FarPixelsPerInch(3);
        var whole = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, side));
        Assert.Empty(whole.Markers);
        Assert.True(whole.Undecoded.Count >= 30, $"{whole.Undecoded.Count} squares found");

        var read = SheetMeasurer.EnlargedCandidates(image, whole.Undecoded, 2 * side, backend, new MeasureOptions());
        Assert.True(read.Count >= 10, $"{read.Count} markers read enlarged");
        var printed = definition.Fiducials!.Markers!.Select(m => m.Id).ToHashSet();
        Assert.All(read, m => Assert.Contains(m.Id, printed));
        // A square wider than the largest side asked for is never cut out, so a bull's box is not enlarged.
        Assert.Empty(SheetMeasurer.EnlargedCandidates(image, whole.Undecoded, 1, backend, new MeasureOptions()));

        var metadata = new ImageMetadata("JPEG", image.Width, image.Height, null, null, null, null, null, null, null);
        var measured = SheetMeasurer.Measure(image, metadata, definition, new MeasureOptions(), backend);
        Assert.NotNull(measured.Registration);
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
