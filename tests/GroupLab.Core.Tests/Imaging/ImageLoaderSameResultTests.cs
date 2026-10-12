using GroupLab.Cli.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 6 item 2: an optimisation changes no result. Same holes, same assignments, same gate record.
/// <para>
/// <b>The optimisation this guards.</b> Opening an image in the application read the file three times and decoded it three times, once
/// grey and twice in colour. <see cref="ImageLoader.LoadForEditor"/> does it once and converts the colour to grey instead of decoding a
/// second time. Entry 414 section 1.1 does the same for a PNG with libpng's own conversion, which is identical where OpenCV's is not.
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

    /// <summary>
    /// Entry 414 section 1.1: a PNG is decoded once, its grey made from the colour decode as libpng makes it. The grey and the brightest
    /// channel from <see cref="ImageLoader.LoadGreyAndMax"/> are the separate loaders' to the bit, on the renders, on the 600 dpi sample,
    /// and on a render given a gamma chunk, where the grey is decoded on its own as before because libpng then corrects it.
    /// </summary>
    [Fact]
    public void OneDecodeGivesTheSeparateLoadersImagesToTheBit()
    {
        string withGamma = Path.Combine(Path.GetTempPath(), $"grouplab-gamma-{Guid.NewGuid():N}.png");
        try
        {
            byte[] render = File.ReadAllBytes(Corpus()[0]);
            File.WriteAllBytes(withGamma, WithChunk(render, "gAMA", [0, 0, 0xB1, 0x8F]));
            Assert.True(ImageLoader.OneDecodeServes(render));
            Assert.False(ImageLoader.OneDecodeServes(File.ReadAllBytes(withGamma)));

            foreach (string path in Corpus().Take(3).Append(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png")).Append(withGamma))
            {
                var (grey, max, metadata) = ImageLoader.LoadGreyAndMax(path);
                var (separateGrey, separateMetadata) = ImageLoader.Load(path);
                var (separateMax, _) = ImageLoader.LoadMaxChannel(path);
                Assert.True(grey.Pixels.SequenceEqual(separateGrey.Pixels), $"{Path.GetFileName(path)}: the grey differs");
                Assert.True(max.Pixels.SequenceEqual(separateMax.Pixels), $"{Path.GetFileName(path)}: the brightest channel differs");
                Assert.Equal((separateGrey.Width, separateMetadata.Width, separateMetadata.DpiX), (grey.Width, metadata.Width, metadata.DpiX));

                // The editor's own load makes the same grey.
                var (editorGrey, _, colour, _) = ImageLoader.LoadForEditor(path);
                using (colour)
                {
                    Assert.True(editorGrey.Pixels.SequenceEqual(separateGrey.Pixels), $"{Path.GetFileName(path)}: the editor's grey differs");
                }
            }
        }
        finally
        {
            File.Delete(withGamma);
        }
    }

    /// <summary>A PNG with one more chunk straight after its header.</summary>
    private static byte[] WithChunk(byte[] png, string type, byte[] data)
    {
        byte[] typed = [.. System.Text.Encoding.ASCII.GetBytes(type), .. data];
        uint crc = Crc32(typed);
        byte[] chunk = [(byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length, .. typed,
            (byte)(crc >> 24), (byte)(crc >> 16), (byte)(crc >> 8), (byte)crc];
        return [.. png[..33], .. chunk, .. png[33..]];
    }

    private static uint Crc32(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }
}
