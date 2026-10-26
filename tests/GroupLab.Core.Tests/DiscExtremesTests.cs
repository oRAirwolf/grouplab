using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests;

/// <summary>
/// Entry 474 section 1: the darkest and lightest within a disc, taken row by row, are the disc walked whole, pixel for pixel, at every
/// radius the hole finder uses and more, for boxes inside the image, against its edges and wider than it.
/// </summary>
public class DiscExtremesTests
{
    private static (byte[] Darkest, byte[] Lightest) Walked(GrayImage image, int left, int top, int bw, int bh, int radius)
    {
        var darkest = new byte[bw * bh];
        var lightest = new byte[bw * bh];
        for (int y = top; y < top + bh; y++)
        {
            for (int x = left; x < left + bw; x++)
            {
                byte lo = 255, hi = 0;
                for (int v = Math.Max(0, y - radius); v <= Math.Min(image.Height - 1, y + radius); v++)
                {
                    for (int u = Math.Max(0, x - radius); u <= Math.Min(image.Width - 1, x + radius); u++)
                    {
                        if (((u - x) * (u - x)) + ((v - y) * (v - y)) <= radius * radius)
                        {
                            lo = Math.Min(lo, image.Pixels[(v * image.Width) + u]);
                            hi = Math.Max(hi, image.Pixels[(v * image.Width) + u]);
                        }
                    }
                }

                darkest[((y - top) * bw) + (x - left)] = lo;
                lightest[((y - top) * bw) + (x - left)] = hi;
            }
        }

        return (darkest, lightest);
    }

    [Fact]
    public void RowByRowIsTheDiscWalkedWhole()
    {
        var random = new Random(474);
        for (int trial = 0; trial < 300; trial++)
        {
            int width = random.Next(1, 60), height = random.Next(1, 60);
            var pixels = new byte[width * height];
            random.NextBytes(pixels);
            var image = new GrayImage(width, height, pixels);
            int left = random.Next(0, width), top = random.Next(0, height);
            int bw = random.Next(1, width - left + 1), bh = random.Next(1, height - top + 1);
            int radius = random.Next(0, 16);
            var (darkest, lightest) = RenderDifferenceHoleDetector.DiscExtremes(image, left, top, bw, bh, radius);
            var (d, l) = Walked(image, left, top, bw, bh, radius);
            Assert.True(d.AsSpan().SequenceEqual(darkest), $"darkest, trial {trial}, radius {radius}");
            Assert.True(l.AsSpan().SequenceEqual(lightest), $"lightest, trial {trial}, radius {radius}");
        }
    }
}
