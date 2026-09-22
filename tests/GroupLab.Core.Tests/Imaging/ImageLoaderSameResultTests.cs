using GroupLab.Cli.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 6 item 2: an optimisation changes no result. Same holes, same assignments, same gate record.
/// <para>
/// <b>The optimisation this guards.</b> Opening an image in the application read the file three times and decoded it three times, once
/// grey and twice in colour. <see cref="ImageLoader.LoadForEditor"/> does it once and converts the colour to grey instead of decoding a
/// second time.
/// </para>
/// <para>
/// <b>Why that needs proving rather than assuming.</b> OpenCV's grayscale decode and its BGR to grey conversion are documented to use the
/// same coefficients, and a rounding difference of one level anywhere would still be a different image. Every threshold in the detector is
/// a comparison against a grey level, so a single level could move a hole in or out, and nothing downstream would say why.
/// </para>
/// </summary>
public class ImageLoaderSameResultTests
{
    /// <summary>Real images committed to the repository: the screen renders, which are full colour PNGs of the sizes people actually open.</summary>
    private static IReadOnlyList<string> Corpus() =>
        Directory.Exists(Repo.PathTo("docs", "figures", "screens", "current"))
            ? [.. Directory.EnumerateFiles(Repo.PathTo("docs", "figures", "screens", "current"), "*.png").Order(StringComparer.Ordinal).Take(8)]
            : [];

    public static TheoryData<string> Images()
    {
        var data = new TheoryData<string>();
        foreach (string file in Corpus())
        {
            data.Add(file);
        }

        return data;
    }

    /// <summary>
    /// A theory with no data passes without running, which looks exactly like a theory that ran and was satisfied. This is what stops the
    /// check above from quietly becoming nothing the day those renders move.
    /// </summary>
    [Fact]
    public void ThereIsSomethingToCompare() =>
        Assert.True(Corpus().Count >= 4, $"only {Corpus().Count} images to compare, so the comparison proves nothing");

    [Theory]
    [MemberData(nameof(Images))]
    public void TheOneDecodeGreyIsTheSameImageAsTheGreyDecode(string path)
    {
        var (separate, _) = ImageLoader.Load(path);
        var (together, max, colour, _) = ImageLoader.LoadForEditor(path);
        using (colour)
        {
            Assert.Equal(separate.Width, together.Width);
            Assert.Equal(separate.Height, together.Height);

            int differences = 0, worst = 0;
            for (int i = 0; i < separate.Pixels.Length; i++)
            {
                int gap = Math.Abs(separate.Pixels[i] - together.Pixels[i]);
                if (gap > 0)
                {
                    differences++;
                    worst = Math.Max(worst, gap);
                }
            }

            Assert.True(differences == 0,
                $"{Path.GetFileName(path)}: the converted grey differs from the decoded grey at {differences} of "
                + $"{separate.Pixels.Length} pixels, by up to {worst} levels. An optimisation that changes the image is not an optimisation.");

            // And the max channel, which the editor uses for the holes, is the same as the separate loader's.
            var (separateMax, _) = ImageLoader.LoadMaxChannel(path);
            Assert.Equal(separateMax.Pixels.Length, max.Pixels.Length);
            Assert.True(separateMax.Pixels.SequenceEqual(max.Pixels), $"{Path.GetFileName(path)}: the max channel differs");
        }
    }
}
