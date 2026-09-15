using GroupLab.Core.Imaging;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 48 section 3: a published photograph keeps what describes the camera and the exposure, the lens model now
/// among them, and still loses where, when, who and anything typed.
/// </summary>
public class CameraFieldsTests
{
    [Fact]
    public void ScrubbingKeepsTheLensModelIsoAndExposureAndStillDropsTheDateAndTheLocation()
    {
        byte[] original = PhoneImages.Jpeg();
        var scrubbed = ImageScrubber.Scrub(original);
        var before = ImageMetadataReader.Read(original);
        var after = ImageMetadataReader.Read(scrubbed.Bytes);

        Assert.Equal(("Galaxy S9+ Rear Camera", 400), (before.LensModel, before.IsoSpeed));
        Assert.Equal((before.LensModel, before.IsoSpeed, before.ExposureTimeSeconds), (after.LensModel, after.IsoSpeed, after.ExposureTimeSeconds));
        Assert.Equal(1.0 / 120, after.ExposureTimeSeconds!.Value, 12);
        Assert.Contains("LensModel", scrubbed.Kept);
        Assert.Contains("LensModel", ImageScrubber.KeptFieldNames);

        Assert.Contains("GPS", scrubbed.Removed);
        Assert.Empty(PublicationCheck.LocationProblems(scrubbed.Bytes));
        Assert.True(original.AsSpan().IndexOf("2026:09:14"u8) >= 0, "the test image should carry a capture date to drop");
        Assert.True(scrubbed.Bytes.AsSpan().IndexOf("2026:09:14"u8) < 0, "the capture date survived scrubbing");
    }
}
