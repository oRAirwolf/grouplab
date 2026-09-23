using GroupLab.Cli.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 43: a cap at 400 megapixels on what GroupLab will decode.
/// <para>
/// <b>It is a limit against a hostile or broken file, not a judgement about scanning.</b> A few bytes of header can claim an image of any
/// size. Alan's largest real file is a 600 dpi letter scan at about 32 megapixels, so the cap is twelve times anything he shoots, and a file
/// that reaches it is either broken or built to exhaust memory.
/// </para>
/// <para>
/// Entry 137 section 4 promised this safety and the code did not have it, which is what question 43 was raised about. The gap is closed and
/// the number is here rather than only in prose.
/// </para>
/// </summary>
public class PixelCapTests
{
    [Fact]
    public void TheCapIsTwelveTimesTheLargestRealScan()
    {
        Assert.Equal(400L * 1000 * 1000, ImageLoader.MostPixels);

        // A 600 dpi letter scan, which is the largest thing anybody here actually makes.
        const long realScan = (long)(8.5 * 600) * (long)(11 * 600);
        Assert.True(realScan < ImageLoader.MostPixels / 10,
            $"a real 600 dpi letter scan is {realScan / 1_000_000} megapixels and the cap is {ImageLoader.MostPixels / 1_000_000}. "
            + "The cap has to stay far above anything somebody scans, or it refuses real work.");
    }

    /// <summary>
    /// An ordinary image is read, so the cap is not refusing everything. This is the half that would fail silently: a cap written the wrong
    /// way round refuses every file, and a test that only checked the refusal would pass.
    /// </summary>
    [Fact]
    public void AnOrdinaryImageIsStillRead()
    {
        string path = Path.Combine(Repo.PathTo("samples"), "gl-cf25-ltr-d-25-shots-600-dpi.png");
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not here, so nothing was decoded");
            return;
        }

        var (image, _) = ImageLoader.Load(GroupLab.Tests.Support.Temp.Readable(path));

        Assert.True(image.Width > 0 && image.Height > 0);
        Assert.True((long)image.Width * image.Height < ImageLoader.MostPixels);
    }
}
