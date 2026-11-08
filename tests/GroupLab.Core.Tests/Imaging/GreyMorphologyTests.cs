using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 484 section 2: the managed open and close of a grey image give OpenCV's own answer to the byte, so finding
/// holes on a target GroupLab did not print is faster and finds exactly what it found before. Each case opens and closes the same image
/// both ways, with the element OpenCV makes, and compares every pixel: noise, smooth shading, dark and light marks, values at 0 and 255,
/// elements wider and taller than the picture, and marks on all four edges, where the border rule shows.
/// </summary>
public class GreyMorphologyTests
{
    public static TheoryData<int, int, int, int> Cases => new()
    {
        // width, height, radius, seed
        { 97, 61, 1, 1 }, { 97, 61, 2, 2 }, { 120, 90, 3, 3 }, { 200, 150, 7, 4 }, { 333, 211, 12, 5 }, { 400, 300, 33, 6 },
        { 64, 400, 33, 7 }, { 160, 90, 30, 8 }, { 512, 512, 60, 9 }, { 129, 257, 17, 10 }, { 40, 30, 50, 11 }, { 850, 1100, 60, 12 },
        { 425, 550, 30, 13 }, { 1, 50, 4, 14 }, { 50, 1, 4, 15 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void AnOpenAndACloseAreOpenCvsToTheByte(int width, int height, int radius, int seed)
    {
        var pixels = Picture(width, height, radius, seed);
        using var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size((2 * radius) + 1, (2 * radius) + 1));
        element.GetArray(out byte[] points);
        var halves = BinaryMorphology.HalfWidths(points, element.Cols, element.Rows);
        Assert.NotNull(halves);

        foreach (var (type, managed) in new[] { (MorphTypes.Close, GreyMorphology.Close(pixels, width, height, halves)), (MorphTypes.Open, GreyMorphology.Open(pixels, width, height, halves)) })
        {
            using var source = Mat.FromPixelData(height, width, MatType.CV_8UC1, pixels);
            using var expected = new Mat();
            Cv2.MorphologyEx(source, expected, type, element);
            expected.GetArray(out byte[] native);
            int differ = native.Zip(managed).Count(p => p.First != p.Second);
            Assert.True(differ == 0, $"{type}: {differ} of {pixels.Length} pixels differ at radius {radius}");
        }
    }

    /// <summary>A grey picture: shading across it, noise, dark and light discs of several sizes, some over the edges, and pure 0 and 255.</summary>
    private static byte[] Picture(int width, int height, int radius, int seed)
    {
        var random = new Random(seed);
        var pixels = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double shade = 120 + (60 * Math.Sin(x / 37.0)) + (40 * Math.Cos(y / 23.0));
                pixels[(y * width) + x] = (byte)Math.Clamp(shade + random.Next(-30, 31), 0, 255);
            }
        }

        int marks = 4 + ((width * height) / Math.Max(1, 40 * radius * radius));
        for (int m = 0; m < Math.Min(marks, 200); m++)
        {
            int cx = random.Next(-radius, width + radius), cy = random.Next(-radius, height + radius), r = random.Next(1, (2 * radius) + 2);
            byte level = (byte)(m % 3 == 0 ? 0 : m % 3 == 1 ? 255 : random.Next(256));
            for (int y = Math.Max(0, cy - r); y < Math.Min(height, cy + r + 1); y++)
            {
                for (int x = Math.Max(0, cx - r); x < Math.Min(width, cx + r + 1); x++)
                {
                    if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                    {
                        pixels[(y * width) + x] = level;
                    }
                }
            }
        }

        return pixels;
    }
}
