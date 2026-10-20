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

    /// <summary>
    /// Entry 433: the close now works only the columns that can change: an erosion from a row's first set pixel to its last, a dilation
    /// within a half width of another row's set pixels. Marks well inside a wide picture, as a residual has them, with nothing on the top or bottom edge,
    /// rows set from side to side, and marks closer together than the element, so each of those stretches is where the answer is decided.
    /// </summary>
    public static TheoryData<int, int, int, int> Marks => new()
    {
        // width, height, radius, seed
        { 600, 200, 33, 21 }, { 1200, 400, 30, 22 }, { 900, 300, 12, 23 }, { 300, 300, 50, 24 }, { 1500, 160, 3, 25 }, { 700, 500, 66, 26 },
    };

    [Theory]
    [MemberData(nameof(Marks))]
    public void ACloseOfMarksInsideAWidePictureIsOpenCvsToTheByte(int width, int height, int radius, int seed)
    {
        var pixels = MarksInside(width, height, radius, seed);
        using var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size((2 * radius) + 1, (2 * radius) + 1));
        element.GetArray(out byte[] points);
        var halves = BinaryMorphology.HalfWidths(points, element.Cols, element.Rows)!;

        using var source = Mat.FromPixelData(height, width, MatType.CV_8UC1, pixels);
        using var expected = new Mat();
        Cv2.MorphologyEx(source, expected, MorphTypes.Close, element);
        expected.GetArray(out byte[] native);

        var managed = BinaryMorphology.Close(pixels, width, height, halves);

        int differ = native.Zip(managed).Count(p => p.First != p.Second);
        Assert.True(differ == 0, $"{differ} of {pixels.Length} pixels differ at radius {radius}");
        Assert.True(managed.Count(b => b == 1) > pixels.Count(b => b == 1), "the close filled nothing, so it tested nothing");
    }

    /// <summary>
    /// Phase 9, 2026-10-20: hole finding now thresholds its residual and then opens it, rather than opening the grey residual and then
    /// thresholding. Each case is a grey picture with discs on and over the edges, a residual's speckle and rows and columns set from side to
    /// side, opened by OpenCV and thresholded, against thresholded and opened by the managed open; and the managed open against OpenCV's own
    /// open of the same 0 and 1 image. Every pixel is compared.
    /// </summary>
    public static TheoryData<int, int, int, int, int> Opens => new()
    {
        // width, height, radius, seed, threshold
        { 97, 61, 1, 31, 40 }, { 120, 90, 2, 32, 90 }, { 200, 150, 5, 33, 128 }, { 333, 211, 12, 34, 64 }, { 400, 300, 30, 35, 200 },
        { 64, 400, 17, 36, 1 }, { 512, 512, 50, 37, 255 }, { 900, 300, 9, 38, 77 },
    };

    [Theory]
    [MemberData(nameof(Opens))]
    public void ThresholdingThenOpeningIsOpeningThenThresholdingToTheByte(int width, int height, int radius, int seed, int threshold)
    {
        var grey = Grey(width, height, radius, seed);
        using var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size((2 * radius) + 1, (2 * radius) + 1));
        element.GetArray(out byte[] points);
        var halves = BinaryMorphology.HalfWidths(points, element.Cols, element.Rows)!;

        using var source = Mat.FromPixelData(height, width, MatType.CV_8UC1, grey);
        using var opened = new Mat();
        Cv2.MorphologyEx(source, opened, MorphTypes.Open, element);
        opened.GetArray(out byte[] greyOpened);
        var before = greyOpened.Select(v => v >= threshold ? (byte)1 : (byte)0).ToArray();

        var over = grey.Select(v => v >= threshold ? (byte)1 : (byte)0).ToArray();
        var after = BinaryMorphology.Open(over, width, height, halves);
        int differ = before.Zip(after).Count(p => p.First != p.Second);
        Assert.True(differ == 0, $"{differ} of {grey.Length} pixels differ at radius {radius}, threshold {threshold}");

        using var binary = Mat.FromPixelData(height, width, MatType.CV_8UC1, over);
        using var nativeOpen = new Mat();
        Cv2.MorphologyEx(binary, nativeOpen, MorphTypes.Open, element);
        nativeOpen.GetArray(out byte[] native);
        Assert.True(native.SequenceEqual(after), "the managed open is not OpenCV's");
        Assert.Contains(after, v => v == 1);
        Assert.True(after.Count(v => v == 1) < over.Count(v => v == 1), "the open removed nothing, so it tested nothing");
    }

    private static byte[] Grey(int width, int height, int radius, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)random.Next(random.Next(4) == 0 ? 256 : 60);
        }

        for (int k = 0; k < 6; k++)
        {
            // Discs from a little under the element to several times it, some centred on or past an edge.
            int cx = random.Next(-radius, width + radius), cy = random.Next(-radius, height + radius), r = Math.Max(1, (radius * (1 + random.Next(6))) / 2);
            byte level = (byte)(80 + random.Next(176));
            for (int y = Math.Max(0, cy - r); y <= Math.Min(height - 1, cy + r); y++)
            {
                for (int x = Math.Max(0, cx - r); x <= Math.Min(width - 1, cx + r); x++)
                {
                    if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                    {
                        pixels[(y * width) + x] = (byte)Math.Max(pixels[(y * width) + x], level - random.Next(20));
                    }
                }
            }
        }

        // And one disc wider than the element, at full strength, wholly inside, so something survives every open.
        int big = Math.Min((2 * radius) + 1, (Math.Min(width, height) / 2) - 1), bx = width / 2, by = height / 2;
        for (int y = by - big; y <= by + big; y++)
        {
            for (int x = bx - big; x <= bx + big; x++)
            {
                if (((x - bx) * (x - bx)) + ((y - by) * (y - by)) <= big * big)
                {
                    pixels[(y * width) + x] = 255;
                }
            }
        }

        int row = random.Next(height), column = random.Next(width);
        for (int x = 0; x < width; x++)
        {
            pixels[(row * width) + x] = 255;
        }

        for (int y = 0; y < height; y++)
        {
            pixels[(y * width) + column] = 255;
        }

        return pixels;
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

    /// <summary>Round marks and pairs of marks closer than the element, a few rows set from side to side, nothing on the edges; seeded.</summary>
    private static byte[] MarksInside(int width, int height, int radius, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height];
        void Disc(int cx, int cy, int r)
        {
            for (int y = Math.Max(1, cy - r); y <= Math.Min(height - 2, cy + r); y++)
            {
                for (int x = Math.Max(1, cx - r); x <= Math.Min(width - 2, cx + r); x++)
                {
                    if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                    {
                        pixels[(y * width) + x] = 1;
                    }
                }
            }
        }

        for (int k = 0; k < 12; k++)
        {
            int cx = random.Next(width / 5, 4 * width / 5), cy = random.Next(height / 5, 4 * height / 5), r = 1 + random.Next(Math.Max(2, radius / 2));
            Disc(cx, cy, r);
            // A second mark just beyond this one, inside the element's reach, which the close joins.
            Disc(cx + r + 1 + random.Next(Math.Max(1, radius)), cy + random.Next(-r, r + 1), 1 + random.Next(Math.Max(2, radius / 3)));
        }

        int band = random.Next(height / 3, 2 * height / 3);
        for (int y = band; y < Math.Min(height - 1, band + 2); y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[(y * width) + x] = 1;
            }
        }

        return pixels;
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
