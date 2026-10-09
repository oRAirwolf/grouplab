using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 392 section 2: the managed close of a 0 and 1 image gives OpenCV's own answer to the byte, so hole finding is
/// faster and finds exactly what it found before. Each case closes the same image both ways, with the element OpenCV makes, and compares
/// every pixel: specks, blobs, lines, and set pixels on all four edges, where the border rule shows.
/// </summary>
public class BinaryMorphologyTests
{
    public static TheoryData<int, int, int, int> Cases => new()
    {
        // width, height, radius, seed
        { 97, 61, 1, 1 }, { 97, 61, 2, 2 }, { 120, 90, 3, 3 }, { 200, 150, 7, 4 }, { 333, 211, 12, 5 },
        { 400, 300, 33, 6 }, { 64, 400, 33, 7 }, { 160, 90, 30, 8 }, { 512, 512, 50, 9 }, { 129, 257, 17, 10 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void ACloseIsOpenCvsToTheByte(int width, int height, int radius, int seed)
    {
        var pixels = Picture(width, height, radius, seed);
        using var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size((2 * radius) + 1, (2 * radius) + 1));
        element.GetArray(out byte[] points);
        var halves = BinaryMorphology.HalfWidths(points, element.Cols, element.Rows);
        Assert.NotNull(halves);

        using var source = Mat.FromPixelData(height, width, MatType.CV_8UC1, pixels);
        using var expected = new Mat();
        Cv2.MorphologyEx(source, expected, MorphTypes.Close, element);
        expected.GetArray(out byte[] native);

        var managed = BinaryMorphology.Close(pixels, width, height, halves);

        int differ = native.Zip(managed).Count(p => p.First != p.Second);
        Assert.True(differ == 0, $"{differ} of {pixels.Length} pixels differ at radius {radius}");
        Assert.Contains(managed, b => b == 1);
        Assert.Contains(managed, b => b == 0);
    }

    [Fact]
    public void OnlyAnImageOfZerosAndOnesIsTakenAsBinary()
    {
        Assert.True(BinaryMorphology.IsBinary(new byte[100]));
        var pixels = new byte[100];
        pixels[99] = 1;
        Assert.True(BinaryMorphology.IsBinary(pixels));
        pixels[42] = 2;
        Assert.False(BinaryMorphology.IsBinary(pixels));
        pixels[42] = 0;
        pixels[3] = 255;
        Assert.False(BinaryMorphology.IsBinary(pixels));
    }

    [Fact]
    public void AnElementThatIsNotCentredRunsIsRefused()
    {
        // A cross is centred runs; a ring has a gap in its middle rows; a run off centre is not symmetric.
        Assert.NotNull(BinaryMorphology.HalfWidths([0, 1, 0, 1, 1, 1, 0, 1, 0], 3, 3));
        Assert.Null(BinaryMorphology.HalfWidths([1, 1, 1, 1, 0, 1, 1, 1, 1], 3, 3));
        Assert.Null(BinaryMorphology.HalfWidths([0, 1, 1, 0, 1, 0, 0, 1, 0], 3, 3));
        Assert.Null(BinaryMorphology.HalfWidths([1, 1, 1, 1], 2, 2));
    }

    /// <summary>Specks, round blobs, a few lines and set pixels on every edge, seeded.</summary>
    private static byte[] Picture(int width, int height, int radius, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height];
        // Sparse enough, for the radius, that the close leaves gaps as well as filling them.
        for (int k = 0; k < (width * height / (8 * radius * radius)) + 3; k++)
        {
            pixels[random.Next(pixels.Length)] = 1;
        }

        for (int k = 0; k < 3; k++)
        {
            int cx = random.Next(width), cy = random.Next(height), r = 1 + random.Next(Math.Max(2, Math.Min(width, height) / 10));
            for (int y = Math.Max(0, cy - r); y <= Math.Min(height - 1, cy + r); y++)
            {
                for (int x = Math.Max(0, cx - r); x <= Math.Min(width - 1, cx + r); x++)
                {
                    if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                    {
                        pixels[(y * width) + x] = 1;
                    }
                }
            }
        }

        int row = random.Next(height), column = random.Next(width);
        for (int x = 0; x < width; x++)
        {
            pixels[(row * width) + x] = 1;
        }

        for (int y = 0; y < height / 3; y += 2)
        {
            pixels[(y * width) + column] = 1;
        }

        pixels[0] = pixels[width - 1] = pixels[(height - 1) * width] = pixels[^1] = 1;
        return pixels;
    }
}
