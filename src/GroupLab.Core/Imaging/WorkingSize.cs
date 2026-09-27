namespace GroupLab.Core.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A1: the largest image the engine works on. Measured on the Galaxy Z Fold 7 in entry 209, the
/// published 600 dpi sample brought to 8 megapixels, 300 dpi for a Letter sheet, took 3.3 s against 16.1 s, peaked at 373 MB against 715,
/// and found all 25 holes, each a mean 2.5 thousandths of an inch from where the full image put it. So a phone always works at 8
/// megapixels at most, decoding at that size rather than shrinking a full decode, which is what sets the peak.
/// <para>
/// The desktop works at full size, because it has the memory and a scan's full detail costs it only time, except for images far larger
/// than any Letter or A4 sheet: a 42 in roll sheet scanned at 600 dpi is over 360 megapixels. Those are brought to
/// <see cref="LargeImageMegapixels"/>, the size of the sample at 600 dpi and then some.
/// </para>
/// </summary>
public static class WorkingSize
{
    /// <summary>The most the phone application works on, in megapixels.</summary>
    public const double PhoneMegapixels = 8;

    /// <summary>The most the desktop works on; only very large images are brought down to it.</summary>
    public const double LargeImageMegapixels = 60;

    /// <summary>The factor an image of this size is scaled by to fit <paramref name="mostMegapixels"/>: 1 when it fits already or no limit is given.</summary>
    public static double Scale(int width, int height, double? mostMegapixels)
    {
        double pixels = (double)width * height;
        return mostMegapixels is { } most && most > 0 && pixels > most * 1e6 ? Math.Sqrt(most * 1e6 / pixels) : 1;
    }

    /// <summary>
    /// The power of two a picture is decoded at a fraction of, so that it never exists in memory at full size and never comes out smaller
    /// than <paramref name="mostMegapixels"/>: the largest 2, 4, 8 up to 64 whose reduced size still holds that many pixels, and 1 when the
    /// picture is no larger than twice it. NOTES-FROM-PLANNING.md entry 239: the phone decoded a picked 600 dpi scan at its full 32 megapixels
    /// and only then shrank it, which is what put it at about 555 MB; decoded at a half, the same scan is 8 megapixels from the start.
    /// </summary>
    public static int SampleFor(int width, int height, double mostMegapixels)
    {
        int sample = 1;
        while (sample < 64 && ((double)width / (sample * 2)) * ((double)height / (sample * 2)) >= mostMegapixels * 1e6)
        {
            sample *= 2;
        }

        return sample;
    }

    /// <summary>What the file says about itself, for the image as brought to working size: its size and its resolution scaled with it.</summary>
    public static ImageMetadata Scaled(ImageMetadata metadata, int width, int height, double scale)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return scale >= 1 ? metadata : metadata with { Width = width, Height = height, DpiX = metadata.DpiX * scale, DpiY = metadata.DpiY * scale };
    }
}
