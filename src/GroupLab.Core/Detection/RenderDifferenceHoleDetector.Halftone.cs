using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>
/// Error report 60: what the detector made of a scan's print. <see cref="Texture"/> is how far, on average, a pixel of solid print stands from
/// the mean of its neighbours, as a share of the paper's level, the median over blocks of solid print, or zero where the sheet has too few
/// such blocks to say; <see cref="DotPattern"/> whether that is a printed dot pattern, a grey or color made of dots, in which case the print
/// shows a hole only where it reaches near the paper's white; <see cref="NotPaperWhite"/> how many marks were refused for never reaching it.
/// </summary>
public sealed record HalftoneRead(double Texture, bool DotPattern, int NotPaperWhite);

public static partial class RenderDifferenceHoleDetector
{
    /// <summary>
    /// The texture from which a scan's solid print is a dot pattern. Error report 60's scan, a blue bull printed grey on a black and white
    /// laser printer and scanned in color at 150 dpi, reads 0.082; black print reads 0.002 to 0.003 on the same scanner and 0.019 to 0.040 on
    /// the 300 and 600 dpi scans of the local bench (2026-11-24).
    /// </summary>
    internal const double HalftoneTextureAtLeast = 0.06;

    /// <summary>The side of the blocks the texture is measured in, inches: small enough that the rings of the CF25 sheet hold whole ones.</summary>
    internal const double TextureBlockInches = 0.04;

    /// <summary>How far a pixel's neighbours reach for the texture, inches: one pixel at 150 dpi, four at 600.</summary>
    internal const double TextureReachInches = 0.006;

    /// <summary>
    /// How far the picture is smoothed to judge where a dot pattern reaches the paper's white, inches: three passes of a box this far each way.
    /// It runs the dots and the bands a scan makes of them into the grey that was printed, and leaves a hole's white middle white. On error
    /// report 60's scan anything from 0.004 to 0.016 in finds the same holes.
    /// </summary>
    internal const double HalftoneSmoothInches = 0.008;

    /// <summary>
    /// How near the paper's white printed paper must read, smoothed, to show a hole there, as a share of the paper's level. On error report
    /// 60's scan the holes' white middles read 0.98 to 0.99: from 0.75 to 0.875 the same eight holes are found and nothing else, and at 0.70
    /// a band of the pattern is taken for a hole too.
    /// </summary>
    internal const double HoleReachesPaper = 0.80;

    /// <summary>
    /// How far from a hole's near-white middle its torn grey edge is still taken as the hole's, inches. On error report 60's scan the white
    /// middles of the .22 holes measure 0.11 to 0.15 in, under the smallest hole looked for without a caliber, and the holes about 0.2 in;
    /// from 0.045 to 0.1 in the same eight holes are found and nothing else.
    /// </summary>
    internal const double TornEdgeInches = 0.06;

    /// <summary>What a mark refused by <see cref="HoleReachesPaper"/> is refused with.</summary>
    internal const string NotPaperWhite = "a band of the printed dot pattern, never as light as the paper a hole shows on a scan";

    /// <summary>
    /// The texture of <paramref name="image"/>'s solid print: over each block of <paramref name="block"/> pixels that the render calls solid
    /// ink, with every neighbour within <paramref name="reach"/> also solid ink so a printed edge is never counted, the mean distance of a
    /// pixel from the mean of its neighbours, as a share of <paramref name="paperLevel"/>; the median block's, or zero under eight blocks.
    /// </summary>
    internal static double Texture(GrayImage image, GrayImage expected, bool[] inside, int block, int reach, double paperLevel)
    {
        int width = image.Width, height = image.Height, side = block + (2 * reach);
        var textures = new List<double>();
        var sums = new long[(side + 1) * (side + 1)];
        for (int top = reach; top + block + reach <= height; top += block)
        {
            for (int left = reach; left + block + reach <= width; left += block)
            {
                bool ink = true;
                for (int y = top - reach; y < top + block + reach && ink; y++)
                {
                    for (int x = left - reach; x < left + block + reach; x++)
                    {
                        int k = (y * width) + x;
                        if (!inside[k] || expected.Pixels[k] > 20)
                        {
                            ink = false;
                            break;
                        }
                    }
                }

                if (!ink)
                {
                    continue;
                }

                // A summed table of the block and its margin, so each pixel's neighbours are added in four lookups.
                for (int v = 0; v < side; v++)
                {
                    long row = 0;
                    for (int u = 0; u < side; u++)
                    {
                        row += image.Pixels[((top - reach + v) * width) + left - reach + u];
                        sums[((v + 1) * (side + 1)) + u + 1] = sums[(v * (side + 1)) + u + 1] + row;
                    }
                }

                double sum = 0, n = (2 * reach + 1) * (2 * reach + 1);
                for (int v = reach; v < reach + block; v++)
                {
                    for (int u = reach; u < reach + block; u++)
                    {
                        long s = sums[((v + reach + 1) * (side + 1)) + u + reach + 1] - sums[((v - reach) * (side + 1)) + u + reach + 1]
                            - sums[((v + reach + 1) * (side + 1)) + u - reach] + sums[((v - reach) * (side + 1)) + u - reach];
                        sum += Math.Abs(image.Pixels[((top - reach + v) * width) + left - reach + u] - (s / n));
                    }
                }

                textures.Add(sum / (block * block) / Math.Max(1, paperLevel));
            }
        }

        if (textures.Count < 8)
        {
            return 0;
        }

        textures.Sort();
        return textures[textures.Count / 2];
    }

    /// <summary>The picture smoothed by three passes of a box out to <paramref name="radius"/> each way, each pass along the rows and then the columns.</summary>
    internal static GrayImage Smoothed(GrayImage image, int radius)
    {
        int width = image.Width, height = image.Height;
        var current = image.Pixels;
        var across = new byte[current.Length];
        for (int pass = 0; pass < 3; pass++)
        {
            var source = current;
            var down = new byte[current.Length];
            Parallel.For(0, height, y =>
            {
                int row = y * width, sum = 0, count = 0;
                for (int x = 0; x < Math.Min(width, radius); x++)
                {
                    sum += source[row + x];
                    count++;
                }

                for (int x = 0; x < width; x++)
                {
                    if (x + radius < width)
                    {
                        sum += source[row + x + radius];
                        count++;
                    }

                    if (x - radius - 1 >= 0)
                    {
                        sum -= source[row + x - radius - 1];
                        count--;
                    }

                    across[row + x] = (byte)((sum + (count / 2)) / count);
                }
            });
            Parallel.For(0, width, x =>
            {
                int sum = 0, count = 0;
                for (int y = 0; y < Math.Min(height, radius); y++)
                {
                    sum += across[(y * width) + x];
                    count++;
                }

                for (int y = 0; y < height; y++)
                {
                    if (y + radius < height)
                    {
                        sum += across[((y + radius) * width) + x];
                        count++;
                    }

                    if (y - radius - 1 >= 0)
                    {
                        sum -= across[((y - radius - 1) * width) + x];
                        count--;
                    }

                    down[(y * width) + x] = (byte)((sum + (count / 2)) / count);
                }
            });
            current = down;
        }

        return new GrayImage(width, height, current);
    }

    /// <summary>Every pixel within <paramref name="radius"/> each way of a set pixel of <paramref name="mask"/>, along the rows and then the columns.</summary>
    internal static byte[] Grown(byte[] mask, int width, int height, int radius)
    {
        var across = new byte[mask.Length];
        Parallel.For(0, height, y =>
        {
            int row = y * width, last = int.MinValue / 2;
            for (int x = 0; x < width; x++)
            {
                last = mask[row + x] != 0 ? x : last;
                across[row + x] = x - last <= radius ? (byte)1 : (byte)0;
            }

            last = int.MaxValue / 2;
            for (int x = width - 1; x >= 0; x--)
            {
                last = mask[row + x] != 0 ? x : last;
                across[row + x] |= last - x <= radius ? (byte)1 : (byte)0;
            }
        });
        var grown = new byte[mask.Length];
        Parallel.For(0, width, x =>
        {
            int last = int.MinValue / 2;
            for (int y = 0; y < height; y++)
            {
                last = across[(y * width) + x] != 0 ? y : last;
                grown[(y * width) + x] = y - last <= radius ? (byte)1 : (byte)0;
            }

            last = int.MaxValue / 2;
            for (int y = height - 1; y >= 0; y--)
            {
                last = across[(y * width) + x] != 0 ? y : last;
                grown[(y * width) + x] |= last - y <= radius ? (byte)1 : (byte)0;
            }
        });
        return grown;
    }

    /// <summary>The 95th percentile of the pixels inside the sheet that the render calls paper, every second pixel of every second row.</summary>
    private static double SheetPaper(GrayImage image, GrayImage expected, bool[] inside)
    {
        var histogram = new int[256];
        int count = 0;
        for (int y = 0; y < image.Height; y += 2)
        {
            for (int x = 0; x < image.Width; x += 2)
            {
                int k = (y * image.Width) + x;
                if (inside[k] && expected.Pixels[k] >= 250)
                {
                    histogram[image.Pixels[k]]++;
                    count++;
                }
            }
        }

        return count == 0 ? 255 : Quantile(histogram, count, 0.95);
    }

    /// <summary>The level the brightest tenth of the pixels inside a mark's outline reach, as a share of the local paper.</summary>
    private static double BrightestTenth(GrayImage image, double[] paper, int block, ImageBlob blob)
    {
        int width = image.Width;
        var ratios = new List<double>();
        for (int y = blob.Top; y < blob.Top + blob.Height; y++)
        {
            var (from, to) = RowInside(blob, y);
            for (int x = from; x <= to; x++)
            {
                ratios.Add(image.Pixels[(y * width) + x] / Math.Max(1, PaperAt(paper, block, width, image.Height, x, y)));
            }
        }

        if (ratios.Count == 0)
        {
            return 1;
        }

        ratios.Sort();
        return ratios[(int)(0.9 * (ratios.Count - 1))];
    }
}
