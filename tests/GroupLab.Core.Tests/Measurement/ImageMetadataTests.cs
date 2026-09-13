using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>Stage S0 and S1 of DETECTION-PIPELINE.md read what the file states, and a camera's resolution is never taken as one.</summary>
public class ImageMetadataTests
{
    [Fact]
    public void AScanStatesItsResolution()
    {
        var m = ImageMetadataReader.Read(File.ReadAllBytes(Repo.PathTo("scans", "phase0", "gl-cf25-ltr-1-600-dpi.png")));

        Assert.Equal("PNG", m.Format);
        Assert.Equal((4958, 6458), (m.Width, m.Height));
        Assert.Equal(600, m.DpiX!.Value, 2);
        Assert.Equal(600, m.DpiY!.Value, 2);
        Assert.False(m.IsCamera);
    }

    [Fact]
    public void APhotographIsACameraImageWithNoResolution()
    {
        var m = ImageMetadataReader.Read(File.ReadAllBytes(Repo.PathTo("scans", "phase0", "20260913_130543.jpg")));

        Assert.Equal("JPEG", m.Format);
        Assert.Equal((4000, 3000), (m.Width, m.Height));
        Assert.Equal("samsung", m.CameraMake);
        Assert.Equal(6, m.Orientation);
        Assert.Equal(2.2, m.FocalLengthMm!.Value, 3);
        Assert.Equal(23, m.FocalLength35mm);
        Assert.True(m.IsCamera);
        Assert.Null(m.DpiX);
    }

    [Fact]
    public void TruncatedInputYieldsWhatWasReadBeforeTheFault()
    {
        byte[] head = File.ReadAllBytes(Repo.PathTo("scans", "phase0", "gl-cf25-ltr-1-600-dpi.png"))[..40];

        var m = ImageMetadataReader.Read(head);

        Assert.Equal("PNG", m.Format);
        Assert.Equal(4958, m.Width);
        Assert.Null(m.DpiX);
    }
}
