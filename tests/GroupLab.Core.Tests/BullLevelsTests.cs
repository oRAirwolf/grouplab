using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part D: the bull locators' grey levels found from each row's run of pixels inside the page circle give
/// the same histogram, pixel for pixel, as mapping and testing every pixel of the box, on homographies from a flat scan to a steep
/// photograph and with a lens's radial distortion well past what a phone gives.
/// </summary>
public class BullLevelsTests
{
    private static GrayImage Noise(Random random, int width, int height)
    {
        var pixels = new byte[width * height];
        random.NextBytes(pixels);
        return new GrayImage(width, height, pixels);
    }

    /// <summary>A sheet seen at <paramref name="pixelsPerDmm"/>, turned, shifted and tipped back by <paramref name="tilt"/>, as image to page.</summary>
    private static Homography ImageToPage(Random random, double pixelsPerDmm, double tilt)
    {
        double angle = random.NextDouble() * 2 * Math.PI, s = 1 / pixelsPerDmm;
        double c = Math.Cos(angle) * s, n = Math.Sin(angle) * s;
        double g = (random.NextDouble() - 0.5) * tilt, h = (random.NextDouble() - 0.5) * tilt;
        return new Homography([c, -n, random.Next(-200, 200), n, c, random.Next(-200, 200), g, h, 1]);
    }

    private static void Same(Random random, IPageMapping map, GrayImage image, int cases)
    {
        var histogram = new int[256];
        for (int k = 0; k < cases; k++)
        {
            var at = map.ToPage(new PointD(random.Next(250, image.Width - 250), random.Next(250, image.Height - 250)));
            double radius = 5 + (random.NextDouble() * 140);
            if (Window.Bounds(image, map, at, radius) is not { } box)
            {
                continue;
            }

            var (low, high) = Window.Levels(image, map, box, at, radius, histogram);
            var expected = Window.EveryPixel(image, map, box, at, radius);
            Assert.Equal(expected, histogram);
            Assert.True(low <= high, $"{low} above {high}");
        }
    }

    [Fact]
    public void ARowsRunGivesTheSameHistogramAsEveryPixelUnderAHomography()
    {
        var random = new Random(481);
        var image = Noise(random, 1400, 1400);
        foreach (double pixelsPerDmm in new[] { 0.6, 1.2, 2.4 })
        {
            foreach (double tilt in new[] { 0.0, 1e-4, 5e-4 })
            {
                Same(random, new HomographyMapping(ImageToPage(random, pixelsPerDmm, tilt)), image, 60);
            }
        }
    }

    [Fact]
    public void ARowsRunGivesTheSameHistogramAsEveryPixelWithALensDistortion()
    {
        var random = new Random(4812);
        var image = Noise(random, 1400, 1400);
        foreach (var (k1, k2) in new[] { (-0.05, 0.065), (-0.3, 0.3), (0.3, -0.2), (0.0, 0.0) })
        {
            var map = new RadialHomographyMapping(700, 700, 700, k1, k2, ImageToPage(random, 1.0 / 700, 0.3));
            Same(random, map, image, 80);
        }
    }
}
