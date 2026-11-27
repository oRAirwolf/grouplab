namespace GroupLab.Core.Detection;

/// <summary>
/// Error report 60, the .338 sets of 2026-11-26: a large hole over a white backer, read from the torn paper round it.
/// <para>
/// A flatbed scan of a sheet shot over a white paper backer shows a hole as the backer's white, as light as the sheet's own paper, with
/// torn fibers scattered round it. On bare paper nothing else is left of the hole; on print a small white middle is left, with dark torn
/// petals of print round it. At 150 dpi the fibers break into specks of 0.03 to 0.07 in, every one too small to be a hole, and no outline
/// closes round them, so entry 417's rim test, which asks for an arc round half the hole, never sees one. The .224 sets shot the same day
/// over cardboard read 8 or 9 holes a page, and the .338 sets over the white backer 0 to 3 of about 10.
/// </para>
/// <para>
/// So on a scan with a caliber named, the specks are gathered: the pieces of the thresholded residual, before it is opened, that are no
/// wider than a hole, that are gray rather than colored (a red or blue pen line is dark in luminance and bright in its own color, and
/// torn paper is neither), and that do not lie on printed words, a marker, a code or the data block. A disc of the caliber's hole that
/// they fill, all the way round and not along a line, is a hole, placed at their centre and given the caliber's size, because the specks
/// say where the hole is and not how large it was.
/// </para>
/// </summary>
public static partial class RenderDifferenceHoleDetector
{
    /// <summary>
    /// The share of a disc of the caliber's hole the specks must fill. On the poster's six .338 pages the holes it should find filled 0.052
    /// to 0.36 (the faintest, on bare paper, 0.058 and 0.062; one more, at 0.033, is still missed); the printing that is not words, a pen
    /// line's broken end and the printed tick under the title filled 0.03 to 0.048.
    /// </summary>
    internal const double FibresFillAtLeast = 0.05;

    /// <summary>
    /// How many of eight sectors round the centre the specks must reach. Every hole found reached 7 or 8; a broken line reaches 2 to 4.
    /// </summary>
    internal const int FibresSectorsAtLeast = 7;

    /// <summary>
    /// How much longer than wide the specks inside the disc may lie, from their second moments. The holes found measured 1.0 to 1.75; the
    /// printed title, where its words are not subtracted, 3.5 to 4.7, and pen lines and print edges 7 to 37.
    /// </summary>
    internal const double FibresElongationAtMost = 2.0;

    /// <summary>
    /// How far, in grey levels, a speck's brightest channel may stand above its luminance and still be torn paper. Torn fibers over a white
    /// backer averaged 0 to 8.5 on the poster's pages, including those on the rainbow-banded print; the red pen lines and writing 55 to 77.
    /// </summary>
    internal const double SpeckColourAtMost = 24;

    /// <summary>
    /// How far from solid print a speck must lie, inches. The slivers a registration error leaves along a
    /// printed ring lie within it, and on the 600 dpi scans of the local bench, read as though shot with a .338, they made a round cluster
    /// at the center of every bull until this was added.
    /// </summary>
    internal const double FibresClearOfPrintInches = 0.03;

    /// <summary>
    /// How many holes across from a hole already found a cluster must lie. A hole throws torn fibers round it: on error report 60's first scan,
    /// read with its .22, a scatter of them 0.39 in from a found hole, at a bull's edge, was the one cluster taken for a hole that the poster's
    /// count says is not one. On the .338 sets every hole read from its fibers lay further than this from any other.
    /// </summary>
    internal const double FibresClearOfHoles = 2;

    /// <summary>
    /// The printed ink over a cluster's disc, 0 to 1, from which on a scan printed as dots it must hold a white middle, and the share of the
    /// disc that middle must fill. On the poster's .338 pages every hole over ink of 0.26 or more held one filling 0.018 to 0.10; on a
    /// laser screen drawn at 300 dpi, the specks round a bull's solid center dot made a cluster over ink of 0.77 holding none.
    /// </summary>
    internal const double FibresOnPrintInk = 0.3, FibresMiddleAtLeast = 0.01;

    /// <summary>The resolution the specks are gathered at, dpi: a 300 or 600 dpi scan is pooled down to it, so the thresholds hold and memory does not grow.</summary>
    internal const double FibresWorkingDpi = 150;

    /// <summary>A cluster of specks a hole over a white backer may be: its centre, image pixels, and what it is judged on.</summary>
    internal sealed record SpeckCluster(double X, double Y, double Fill, int Sectors, double Elongation);

    /// <summary>Whether a cluster is a hole by <see cref="FibresFillAtLeast"/>, <see cref="FibresSectorsAtLeast"/> and <see cref="FibresElongationAtMost"/>.</summary>
    internal static bool IsFibresHole(SpeckCluster c) =>
        c.Fill >= FibresFillAtLeast && c.Sectors >= FibresSectorsAtLeast && c.Elongation <= FibresElongationAtMost;

    /// <summary>
    /// The clusters of specks that fill a disc of <paramref name="radius"/> image pixels by <see cref="FibresFillAtLeast"/> or more, strongest
    /// first, none within a hole of another. <paramref name="over"/> is the thresholded residual before it is opened; <paramref name="colour"/>,
    /// where there is one, each pixel's brightest channel less its luminance; <paramref name="printedAt"/> says whether an image point lies on
    /// printing the residual does not subtract.
    /// </summary>
    internal static List<SpeckCluster> SpeckClusters(byte[] over, byte[]? colour, int width, int height, double dpi, double radius, Func<double, double, bool> printedAt)
    {
        // Pooled to the working resolution: a cell holds a speck where a quarter of its pixels do, as a pixel of a 150 dpi scan darkens
        // where a quarter of it is fiber, and its colour is its pixels' mean. A cell that took any one pixel made a speck of every grain of
        // paper on a 600 dpi scan.
        int f = Math.Max(1, (int)Math.Floor(dpi / FibresWorkingDpi));
        int w = (width + f - 1) / f, h = (height + f - 1) / f;
        var count = new int[w * h];
        var on = new byte[w * h];
        var tint = colour is null ? null : new double[w * h];
        var cells = new int[w * h];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width) + x, j = ((y / f) * w) + (x / f);
                count[j] += over[i] != 0 ? 1 : 0;
                if (tint is not null)
                {
                    tint[j] += colour![i];
                    cells[j]++;
                }
            }
        }

        int least = Math.Max(1, (f * f) / 4);
        for (int j = 0; j < on.Length; j++)
        {
            on[j] = count[j] >= least ? (byte)1 : (byte)0;
        }

        if (tint is not null)
        {
            for (int j = 0; j < tint.Length; j++)
            {
                tint[j] /= Math.Max(1, cells[j]);
            }
        }

        double r = radius / f;
        var keep = Specks(on, tint, w, h, (int)Math.Ceiling(2 * r), (x, y) => printedAt((x * f) + ((f - 1) / 2.0), (y * f) + ((f - 1) / 2.0)));

        // The share of a disc's area the specks fill, everywhere, from a summed table over a square of the disc's area.
        var sum = new int[(w + 1) * (h + 1)];
        for (int y = 0; y < h; y++)
        {
            int row = 0;
            for (int x = 0; x < w; x++)
            {
                row += keep[(y * w) + x];
                sum[((y + 1) * (w + 1)) + x + 1] = sum[(y * (w + 1)) + x + 1] + row;
            }
        }

        int half = Math.Max(1, (int)Math.Round(0.886 * r));
        double square = ((2 * half) + 1) * ((2 * half) + 1);
        var seeds = new List<(double Fill, int X, int Y)>();
        for (int y = half; y < h - half - 1; y += 2)
        {
            for (int x = half; x < w - half - 1; x += 2)
            {
                int a = ((y - half) * (w + 1)) + x - half, b = ((y - half) * (w + 1)) + x + half + 1;
                int c = ((y + half + 1) * (w + 1)) + x - half, d = ((y + half + 1) * (w + 1)) + x + half + 1;
                double fill = (sum[d] - sum[b] - sum[c] + sum[a]) / square;
                if (fill >= FibresFillAtLeast)
                {
                    seeds.Add((fill, x, y));
                }
            }
        }

        var found = new List<SpeckCluster>();
        foreach (var (_, sx, sy) in seeds.OrderByDescending(s => s.Fill))
        {
            if (found.Any(c => Math.Pow(c.X - sx, 2) + Math.Pow(c.Y - sy, 2) < 4 * r * r))
            {
                continue;
            }

            // The centre moves to the specks' own centroid within the disc, a few times, so the disc settles over the hole.
            double cx = sx, cy = sy;
            for (int pass = 0; pass < 4; pass++)
            {
                var (mx, my, n) = Centroid(keep, w, h, cx, cy, r);
                if (n == 0)
                {
                    break;
                }

                (cx, cy) = (mx, my);
            }

            var cluster = Judge(keep, w, h, cx, cy, r);
            if (cluster.Fill >= FibresFillAtLeast && !found.Any(c => Math.Pow(c.X - cx, 2) + Math.Pow(c.Y - cy, 2) < 4 * r * r))
            {
                found.Add(cluster);
            }
        }

        return [.. found.Select(c => c with { X = (c.X * f) + ((f - 1) / 2.0), Y = (c.Y * f) + ((f - 1) / 2.0) })];
    }

    /// <summary>
    /// The pixels of every piece of <paramref name="on"/>, eight-connected, narrower and shorter than <paramref name="most"/>, gray, and not on
    /// printing, as 1; everything else 0.
    /// </summary>
    private static byte[] Specks(byte[] on, double[]? tint, int w, int h, int most, Func<double, double, bool> printedAt)
    {
        var seen = new bool[w * h];
        var keep = new byte[w * h];
        var stack = new Stack<int>();
        var members = new List<int>();
        for (int start = 0; start < on.Length; start++)
        {
            if (on[start] == 0 || seen[start])
            {
                continue;
            }

            members.Clear();
            int left = int.MaxValue, right = -1, top = int.MaxValue, bottom = -1;
            seen[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                members.Add(i);
                int x = i % w, y = i / w;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h && on[(ny * w) + nx] != 0 && !seen[(ny * w) + nx])
                        {
                            seen[(ny * w) + nx] = true;
                            stack.Push((ny * w) + nx);
                        }
                    }
                }
            }

            if (right - left >= most || bottom - top >= most
                || (tint is not null && members.Average(i => tint[i]) > SpeckColourAtMost)
                || printedAt(members.Average(i => i % w), members.Average(i => i / w)))
            {
                continue;
            }

            foreach (int i in members)
            {
                keep[i] = 1;
            }
        }

        return keep;
    }

    /// <summary>The share of a disc of <paramref name="mask"/> that is set.</summary>
    private static double ShareOver(byte[] mask, int width, int height, double cx, double cy, double r)
    {
        int set = 0, n = 0;
        for (int y = Math.Max(0, (int)(cy - r)); y <= Math.Min(height - 1, (int)(cy + r)); y++)
        {
            for (int x = Math.Max(0, (int)(cx - r)); x <= Math.Min(width - 1, (int)(cx + r)); x++)
            {
                if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                {
                    set += mask[(y * width) + x] != 0 ? 1 : 0;
                    n++;
                }
            }
        }

        return n == 0 ? 0 : (double)set / n;
    }

    /// <summary>The mean printed ink over a disc of the expected artwork, 0 for paper and 1 for solid print.</summary>
    private static double InkOver(Imaging.GrayImage expected, int width, int height, double cx, double cy, double r)
    {
        double total = 0;
        int n = 0;
        for (int y = Math.Max(0, (int)(cy - r)); y <= Math.Min(height - 1, (int)(cy + r)); y++)
        {
            for (int x = Math.Max(0, (int)(cx - r)); x <= Math.Min(width - 1, (int)(cx + r)); x++)
            {
                if (((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                {
                    total += 1 - (expected.Pixels[(y * width) + x] / 255.0);
                    n++;
                }
            }
        }

        return n == 0 ? 0 : total / n;
    }

    private static (double X, double Y, int N) Centroid(byte[] keep, int w, int h, double cx, double cy, double r)
    {
        double sx = 0, sy = 0;
        int n = 0;
        for (int y = Math.Max(0, (int)(cy - r)); y <= Math.Min(h - 1, (int)(cy + r)); y++)
        {
            for (int x = Math.Max(0, (int)(cx - r)); x <= Math.Min(w - 1, (int)(cx + r)); x++)
            {
                if (keep[(y * w) + x] != 0 && ((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= r * r)
                {
                    sx += x;
                    sy += y;
                    n++;
                }
            }
        }

        return n == 0 ? (cx, cy, 0) : (sx / n, sy / n, n);
    }

    /// <summary>The disc's fill, the sectors of eight its specks reach (each holding a fortieth of them, and at least two cells), and their elongation.</summary>
    private static SpeckCluster Judge(byte[] keep, int w, int h, double cx, double cy, double r)
    {
        var sectors = new int[8];
        double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0;
        int n = 0, area = 0;
        for (int y = Math.Max(0, (int)(cy - r)); y <= Math.Min(h - 1, (int)(cy + r)); y++)
        {
            for (int x = Math.Max(0, (int)(cx - r)); x <= Math.Min(w - 1, (int)(cx + r)); x++)
            {
                double dx = x - cx, dy = y - cy;
                if ((dx * dx) + (dy * dy) > r * r)
                {
                    continue;
                }

                area++;
                if (keep[(y * w) + x] == 0)
                {
                    continue;
                }

                n++;
                sx += dx;
                sy += dy;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
                sectors[(int)Math.Floor((Math.Atan2(dy, dx) + Math.PI) / (Math.PI / 4)) & 7]++;
            }
        }

        if (n == 0)
        {
            return new SpeckCluster(cx, cy, 0, 0, double.PositiveInfinity);
        }

        double mx = sx / n, my = sy / n;
        double vxx = (sxx / n) - (mx * mx), vyy = (syy / n) - (my * my), vxy = (sxy / n) - (mx * my);
        double tr = vxx + vyy, det = (vxx * vyy) - (vxy * vxy), spread = Math.Sqrt(Math.Max(0, (tr * tr / 4) - det));
        double l1 = (tr / 2) + spread, l2 = Math.Max(1e-9, (tr / 2) - spread);
        return new SpeckCluster(cx, cy, (double)n / area, sectors.Count(c => c >= Math.Max(2, n / 40)), Math.Sqrt(l1 / l2));
    }
}
