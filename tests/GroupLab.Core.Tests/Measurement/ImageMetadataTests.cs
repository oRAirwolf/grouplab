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
        Assert.Equal(2.2, m.FNumber!.Value, 3);
        Assert.True(m.IsCamera);
        Assert.Null(m.DpiX);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 27 section 2: digital zoom is read and is part of the lens grouping key, and a missing tag is unknown
    /// rather than 1, so frames that agree on every other field but differ in zoom, or in whether they state it, never share a fit.
    /// </summary>
    [Fact]
    public void DigitalZoomIsReadAndSeparatesOtherwiseIdenticalFrames()
    {
        var zoomed = ImageMetadataReader.Read(ExifJpeg(zoom: (164, 100)));
        var stated = ImageMetadataReader.Read(ExifJpeg(zoom: (1, 1)));
        var absent = ImageMetadataReader.Read(ExifJpeg(zoom: null));

        Assert.Equal(1.64, zoomed.DigitalZoomRatio!.Value, 12);
        Assert.Equal(1.0, stated.DigitalZoomRatio!.Value, 12);
        Assert.Null(absent.DigitalZoomRatio);
        Assert.Equal(2.2, absent.FocalLengthMm!.Value, 12);
        Assert.Equal(13, absent.FocalLength35mm);
        Assert.Contains("digital zoom 1.64", zoomed.LensGroupKey, StringComparison.Ordinal);
        Assert.Contains("digital zoom unknown", absent.LensGroupKey, StringComparison.Ordinal);
        Assert.Equal(3, new[] { zoomed, stated, absent }.Select(m => m.LensGroupKey).Distinct().Count());

        // EXIF defines a stated 0 as digital zoom not used, which is the geometry of a stated 1.
        var zero = ImageMetadataReader.Read(ExifJpeg(zoom: (0, 1)));
        Assert.Equal(0, zero.DigitalZoomRatio!.Value);
        Assert.Equal(stated.LensGroupKey, zero.LensGroupKey);
    }

    /// <summary>A minimal little-endian JPEG whose EXIF block carries a 2.2 mm focal length, a 13 mm equivalent and, optionally, a digital zoom ratio.</summary>
    private static byte[] ExifJpeg((uint Numerator, uint Denominator)? zoom)
    {
        var tiff = new List<byte>();
        void U16(int v) => tiff.AddRange(BitConverter.GetBytes((ushort)v));
        void U32(uint v) => tiff.AddRange(BitConverter.GetBytes(v));
        void Entry(int tag, int type, uint count, uint value)
        {
            U16(tag);
            U16(type);
            U32(count);
            U32(value);
        }

        int exifEntries = zoom is null ? 2 : 3;
        const uint ExifIfd = 26;
        uint values = ExifIfd + 2 + (uint)(12 * exifEntries) + 4;
        tiff.AddRange("II"u8.ToArray());
        U16(42);
        U32(8);
        U16(1);
        Entry(0x8769, 4, 1, ExifIfd);
        U32(0);
        U16(exifEntries);
        Entry(0x920A, 5, 1, values);
        Entry(0xA405, 3, 1, 13);
        if (zoom is not null)
        {
            Entry(0xA404, 5, 1, values + 8);
        }

        U32(0);
        U32(22);
        U32(10);
        if (zoom is { } z)
        {
            U32(z.Numerator);
            U32(z.Denominator);
        }

        byte[] body = [.. "Exif\0\0"u8.ToArray(), .. tiff];
        int length = body.Length + 2;
        return [0xFF, 0xD8, 0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. body, 0xFF, 0xD9];
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
