using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A1: the largest image the engine works on. The phone works at 8 megapixels at most; the desktop
/// at full size except for images far larger than any Letter or A4 sheet.
/// </summary>
public class WorkingSizeTests
{
    [Fact]
    public void AnImageIsBroughtToTheWorkingSizeAndNoFurther()
    {
        Assert.Equal(1, WorkingSize.Scale(4000, 3000, WorkingSize.LargeImageMegapixels));
        Assert.Equal(1, WorkingSize.Scale(4000, 3000, null));
        double scale = WorkingSize.Scale(4958, 6458, WorkingSize.PhoneMegapixels);
        Assert.InRange(4958 * scale * 6458 * scale, 7.99e6, 8.0e6);
        Assert.InRange(WorkingSize.Scale(25200, 14400, WorkingSize.LargeImageMegapixels), 0.40, 0.41);
    }

    /// <summary>The published sample read at the phone's size: no more than 8 megapixels, its resolution scaled with it, both channels alike.</summary>
    [Fact]
    public void TheSampleIsReadAtThePhonesSize()
    {
        string path = Path.Combine(Repo.PathTo("samples"), "gl-cf25-ltr-d-25-shots-600-dpi.png");
        var (grey, metadata) = ImageLoader.Load(path, WorkingSize.PhoneMegapixels);
        var (value, _) = ImageLoader.LoadMaxChannel(path, WorkingSize.PhoneMegapixels);
        Assert.True((double)grey.Width * grey.Height <= 8e6, $"{grey.Width} by {grey.Height} is over 8 megapixels");
        Assert.True((double)grey.Width * grey.Height > 7.9e6, $"{grey.Width} by {grey.Height} was brought down further than it needed");
        Assert.Equal((grey.Width, grey.Height), (value.Width, value.Height));
        Assert.Equal(grey.Width, metadata.Width);
        Assert.InRange(metadata.DpiX!.Value, 600 * grey.Width / 4958.0 - 0.5, 600 * grey.Width / 4958.0 + 0.5);

        var (full, fullMetadata) = ImageLoader.Load(path);
        Assert.Equal((4958, 6458), (full.Width, full.Height));
        Assert.InRange(fullMetadata.DpiX!.Value, 599.5, 600.5);
    }
}
