using GroupLab.Core.Imaging;

namespace GroupLab.Core.Capture;

/// <summary>
/// A sheet's four corners in a photograph, in image pixels, clockwise from the top left as the image shows it, with how straight its edges
/// were: the root mean square distance of the edge points from the four fitted lines, in pixels, and the share of the frame the sheet fills.
/// </summary>
public sealed record SheetQuad(PointD TopLeft, PointD TopRight, PointD BottomRight, PointD BottomLeft, double EdgeRmsPixels, double FrameShare)
{
    public IReadOnlyList<PointD> Corners => [TopLeft, TopRight, BottomRight, BottomLeft];

    /// <summary>The homography from a unit square, (0, 0) at the top left, to the corners: the sheet in the image with no size assumed.</summary>
    public Homography FromUnitSquare() =>
        Registration.HomographyEstimate.Fit([new(0, 0), new(1, 0), new(1, 1), new(0, 1)], Corners)
            ?? throw new InvalidOperationException("the four corners do not make a quadrilateral");
}

/// <summary>
/// Finds the paper in a photograph of a sheet with no usable markers, NOTES-FROM-PLANNING.md entry 157 section 4 item 1: the edges the
/// corrected perspective and the off-axis angle are worked out from when there is nothing printed to register on.
/// <para>
/// <b>How.</b> The image is reduced to about 800 pixels across and split into light and dark by Otsu's threshold. The largest light region
/// is taken as the paper, after a closing that bridges the holes and printing in it. Its convex hull gives four corners, the four hull points
/// enclosing the most area. Each side is then fitted as a straight line through the region's boundary between its corners, and refined at
/// full resolution to the strongest edge along the side's normal, to a fraction of a pixel. The corners are where the lines meet.
/// </para>
/// <para>
/// <b>When it says no.</b> When no light region fills a tenth of the frame; when the region runs out of the frame, because a sheet partly out
/// of the picture has no fourth corner to find; and when the boundary is not four straight sides, a curled, folded or partly hidden sheet.
/// Each refusal says which.
/// </para>
/// </summary>
public static class SheetOutline
{
    private const int Working = 800;

    /// <summary>The share of the boundary that must lie on the four sides.</summary>
    public const double StraightShare = 0.85;

    public static SheetQuad? Find(GrayImage image, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(image);
        int k = Math.Max(1, (int)Math.Ceiling(Math.Max(image.Width, image.Height) / (double)Working));
        var small = Reduce(image, k);
        int w = small.Width, h = small.Height;

        // White paper on a light board is the usual case at a range, and one threshold puts the board and the paper on the same side of it.
        // So when the largest light region is not a sheet, it is split again by its own threshold, which separates the whiter paper from the
        // board, and that is tried in turn: three levels at most.
        var within = new bool[w * h];
        Array.Fill(within, true);
        reason = "no sheet stands out from what is behind it";
        for (int level = 0; level < 3; level++)
        {
            var inside = small.Pixels.Where((_, i) => within[i]).ToArray();
            if (inside.Length < 0.1 * w * h || inside.Max() - inside.Min() < 16)
            {
                break;
            }

            byte threshold = Otsu(inside);
            var light = new bool[w * h];
            for (int i = 0; i < light.Length; i++)
            {
                light[i] = within[i] && small.Pixels[i] > threshold;
            }

            // A closing: holes, printing and grid lines inside the paper join it, and the background's specks do not.
            int radius = Math.Max(2, Math.Min(w, h) / 100);
            light = Erode(Dilate(light, w, h, radius), w, h, radius);
            var (region, area) = Largest(light, w, h);
            if (area < 0.1 * w * h)
            {
                break;
            }

            if (Quad(image, region, w, h, k, out string? why) is { } found)
            {
                reason = null;
                return found;
            }

            reason = why;
            within = region;
        }

        return null;
    }

    /// <summary>The sheet a region is, or null with the reason it is not one.</summary>
    private static SheetQuad? Quad(GrayImage image, bool[] region, int w, int h, int k, out string? reason)
    {
        var boundary = new List<(int X, int Y)>();
        int onFrame = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!region[(y * w) + x])
                {
                    continue;
                }

                bool frame = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                if (frame || !region[(y * w) + x - 1] || !region[(y * w) + x + 1] || !region[((y - 1) * w) + x] || !region[((y + 1) * w) + x])
                {
                    boundary.Add((x, y));
                    onFrame += frame ? 1 : 0;
                }
            }
        }

        if (onFrame > 0.02 * boundary.Count)
        {
            reason = "the sheet runs out of the frame, so it has no four corners to find";
            return null;
        }

        var hull = Hull(boundary);
        var quad = LargestQuadrilateral(hull);
        var lines = new (PointD Point, PointD Direction)[4];
        int straight = 0;
        double squares = 0;
        for (int side = 0; side < 4; side++)
        {
            PointD a = quad[side], b = quad[(side + 1) % 4];
            double length = Distance(a, b), tolerance = Math.Max(2.5, 0.015 * length);
            var along = boundary
                .Select(p => new PointD(p.X, p.Y))
                .Where(p => SegmentDistance(p, a, b, 0.1, 0.9) is { } d && d <= tolerance)
                .ToList();
            if (along.Count < 12)
            {
                reason = "the paper's edge is not four straight sides; it may be curled, folded or partly hidden";
                return null;
            }

            lines[side] = FitLine(along);
        }

        foreach (var (x, y) in boundary)
        {
            var p = new PointD(x, y);
            double nearest = lines.Min(l => LineDistance(p, l));
            if (nearest <= Math.Max(2.5, 0.006 * (w + h)))
            {
                straight++;
                squares += nearest * nearest;
            }
        }

        if (straight < StraightShare * boundary.Count)
        {
            reason = "the paper's edge is not four straight sides; it may be curled, folded or partly hidden";
            return null;
        }

        // Back to full resolution, each side refined to the strongest edge across it.
        var full = new (PointD Point, PointD Direction)[4];
        for (int side = 0; side < 4; side++)
        {
            var point = Up(lines[side].Point, k);
            full[side] = Refine(image, (point, lines[side].Direction), Up(Intersect(lines[(side + 3) % 4], lines[side]), k), Up(Intersect(lines[side], lines[(side + 1) % 4]), k), 3 * k);
        }

        var corners = Enumerable.Range(0, 4).Select(i => Intersect(full[(i + 3) % 4], full[i])).ToArray();
        reason = null;
        var ordered = Order(corners);
        return new SheetQuad(ordered[0], ordered[1], ordered[2], ordered[3], Math.Sqrt(squares / Math.Max(1, straight)) * k, Area(ordered) / ((double)image.Width * image.Height));
    }

    private static PointD Up(PointD p, int k) => new(((p.X + 0.5) * k) - 0.5, ((p.Y + 0.5) * k) - 0.5);

    /// <summary>A box average by <paramref name="k"/> in each direction.</summary>
    private static GrayImage Reduce(GrayImage image, int k)
    {
        if (k == 1)
        {
            return image;
        }

        int w = image.Width / k, h = image.Height / k;
        var pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sum = 0;
                for (int dy = 0; dy < k; dy++)
                {
                    int row = ((y * k) + dy) * image.Width;
                    for (int dx = 0; dx < k; dx++)
                    {
                        sum += image.Pixels[row + (x * k) + dx];
                    }
                }

                pixels[(y * w) + x] = (byte)(sum / (k * k));
            }
        }

        return new GrayImage(w, h, pixels);
    }

    /// <summary>Otsu's threshold: the level that best separates the histogram into two classes.</summary>
    public static byte Otsu(byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        var histogram = new long[256];
        foreach (byte p in pixels)
        {
            histogram[p]++;
        }

        double total = pixels.Length, sum = 0;
        for (int i = 0; i < 256; i++)
        {
            sum += i * histogram[i];
        }

        double below = 0, weight = 0, best = -1;
        byte level = 127;
        for (int t = 0; t < 256; t++)
        {
            weight += histogram[t];
            if (weight == 0 || weight == total)
            {
                continue;
            }

            below += t * histogram[t];
            double m0 = below / weight, m1 = (sum - below) / (total - weight);
            double between = weight * (total - weight) * (m0 - m1) * (m0 - m1);
            if (between > best)
            {
                best = between;
                level = (byte)t;
            }
        }

        return level;
    }

    private static bool[] Dilate(bool[] mask, int w, int h, int r) => Sweep(mask, w, h, r, true);

    private static bool[] Erode(bool[] mask, int w, int h, int r) => Sweep(mask, w, h, r, false);

    /// <summary>A square structuring element, done as a row pass and a column pass.</summary>
    private static bool[] Sweep(bool[] mask, int w, int h, int r, bool any)
    {
        var rows = new bool[mask.Length];
        for (int y = 0; y < h; y++)
        {
            int count = 0;
            for (int x = -r; x < w + r; x++)
            {
                if (x + r < w && x + r >= 0 && mask[(y * w) + x + r] == any)
                {
                    count++;
                }

                if (x - r - 1 >= 0 && x - r - 1 < w && mask[(y * w) + x - r - 1] == any)
                {
                    count--;
                }

                if (x >= 0 && x < w)
                {
                    rows[(y * w) + x] = any ? count > 0 : count == 0;
                }
            }
        }

        var result = new bool[mask.Length];
        for (int x = 0; x < w; x++)
        {
            int count = 0;
            for (int y = -r; y < h + r; y++)
            {
                if (y + r < h && y + r >= 0 && rows[((y + r) * w) + x] == any)
                {
                    count++;
                }

                if (y - r - 1 >= 0 && y - r - 1 < h && rows[((y - r - 1) * w) + x] == any)
                {
                    count--;
                }

                if (y >= 0 && y < h)
                {
                    result[(y * w) + x] = any ? count > 0 : count == 0;
                }
            }
        }

        return result;
    }

    /// <summary>The largest four-connected region of the mask, with its holes filled.</summary>
    private static (bool[] Region, int Area) Largest(bool[] mask, int w, int h)
    {
        var label = new int[mask.Length];
        int next = 0, bestLabel = 0, bestArea = 0;
        var stack = new Stack<int>();
        for (int start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || label[start] != 0)
            {
                continue;
            }

            next++;
            int area = 0;
            label[start] = next;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                area++;
                int x = i % w, y = i / w;
                foreach (int j in new[] { x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1 })
                {
                    if (j >= 0 && mask[j] && label[j] == 0)
                    {
                        label[j] = next;
                        stack.Push(j);
                    }
                }
            }

            if (area > bestArea)
            {
                bestArea = area;
                bestLabel = next;
            }
        }

        // Fill the region's holes: whatever is not the region and cannot reach the frame through non-region pixels is inside it.
        var outside = new bool[mask.Length];
        for (int i = 0; i < mask.Length; i++)
        {
            int x = i % w, y = i / w;
            if ((x == 0 || y == 0 || x == w - 1 || y == h - 1) && label[i] != bestLabel && !outside[i])
            {
                outside[i] = true;
                stack.Push(i);
            }
        }

        while (stack.Count > 0)
        {
            int i = stack.Pop();
            int x = i % w, y = i / w;
            foreach (int j in new[] { x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1 })
            {
                if (j >= 0 && label[j] != bestLabel && !outside[j])
                {
                    outside[j] = true;
                    stack.Push(j);
                }
            }
        }

        var region = new bool[mask.Length];
        int filled = 0;
        for (int i = 0; i < mask.Length; i++)
        {
            region[i] = !outside[i];
            filled += region[i] ? 1 : 0;
        }

        return bestArea == 0 ? (region, 0) : (region, filled);
    }

    /// <summary>Andrew's monotone chain, counterclockwise in a y-down image.</summary>
    private static List<PointD> Hull(List<(int X, int Y)> points)
    {
        var sorted = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).Select(p => new PointD(p.X, p.Y)).ToList();
        if (sorted.Count < 3)
        {
            return sorted;
        }

        static double Cross(PointD o, PointD a, PointD b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<PointD>();
        foreach (var p in sorted)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], p) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(p);
        }

        int lower = hull.Count + 1;
        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            while (hull.Count >= lower && Cross(hull[^2], hull[^1], sorted[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(sorted[i]);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    /// <summary>The four hull points enclosing the most area, by coordinate ascent from the four diagonal extremes.</summary>
    private static PointD[] LargestQuadrilateral(List<PointD> hull)
    {
        int n = hull.Count;
        int[] at =
        [
            Arg(hull, p => -p.X - p.Y),
            Arg(hull, p => p.X - p.Y),
            Arg(hull, p => p.X + p.Y),
            Arg(hull, p => -p.X + p.Y),
        ];
        double best = Area(at.Select(i => hull[i]).ToArray());
        for (int round = 0; round < 20; round++)
        {
            bool moved = false;
            for (int v = 0; v < 4; v++)
            {
                for (int candidate = 0; candidate < n; candidate++)
                {
                    var trial = (int[])at.Clone();
                    trial[v] = candidate;
                    double area = Area(trial.Select(i => hull[i]).ToArray());
                    if (area > best + 1e-9)
                    {
                        best = area;
                        at = trial;
                        moved = true;
                    }
                }
            }

            if (!moved)
            {
                break;
            }
        }

        return Order([.. at.Select(i => hull[i])]);
    }

    private static int Arg(List<PointD> points, Func<PointD, double> score)
    {
        int best = 0;
        for (int i = 1; i < points.Count; i++)
        {
            if (score(points[i]) > score(points[best]))
            {
                best = i;
            }
        }

        return best;
    }

    /// <summary>Clockwise in the image from the corner nearest the top left.</summary>
    private static PointD[] Order(PointD[] corners)
    {
        double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
        var around = corners.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToList();
        int first = around.IndexOf(around.MinBy(p => p.X + p.Y));
        return [.. Enumerable.Range(0, 4).Select(i => around[(first + i) % 4])];
    }

    private static double Area(PointD[] q)
    {
        double sum = 0;
        for (int i = 0; i < q.Length; i++)
        {
            var a = q[i];
            var b = q[(i + 1) % q.Length];
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(sum) / 2;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    /// <summary>A point's distance from a segment's line where it projects between the given fractions of it, or null outside them.</summary>
    private static double? SegmentDistance(PointD p, PointD a, PointD b, double from, double to)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, length2 = (dx * dx) + (dy * dy);
        double t = (((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / length2;
        return t < from || t > to ? null : Math.Abs(((p.X - a.X) * dy) - ((p.Y - a.Y) * dx)) / Math.Sqrt(length2);
    }

    private static double LineDistance(PointD p, (PointD Point, PointD Direction) line) =>
        Math.Abs(((p.X - line.Point.X) * line.Direction.Y) - ((p.Y - line.Point.Y) * line.Direction.X));

    /// <summary>A total least squares line: the centroid and the principal direction, of unit length.</summary>
    private static (PointD Point, PointD Direction) FitLine(IReadOnlyList<PointD> points)
    {
        double mx = points.Average(p => p.X), my = points.Average(p => p.Y), sxx = 0, sxy = 0, syy = 0;
        foreach (var p in points)
        {
            sxx += (p.X - mx) * (p.X - mx);
            sxy += (p.X - mx) * (p.Y - my);
            syy += (p.Y - my) * (p.Y - my);
        }

        double angle = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
        return (new PointD(mx, my), new PointD(Math.Cos(angle), Math.Sin(angle)));
    }

    private static PointD Intersect((PointD Point, PointD Direction) a, (PointD Point, PointD Direction) b)
    {
        double det = (a.Direction.X * b.Direction.Y) - (a.Direction.Y * b.Direction.X);
        double t = (((b.Point.X - a.Point.X) * b.Direction.Y) - ((b.Point.Y - a.Point.Y) * b.Direction.X)) / det;
        return new PointD(a.Point.X + (t * a.Direction.X), a.Point.Y + (t * a.Direction.Y));
    }

    /// <summary>
    /// One side at full resolution: along it, between its corners, the strongest change of brightness within <paramref name="reach"/> pixels
    /// either side of the coarse line, to a fraction of a pixel by a parabola through the peak, and a line fitted through those points.
    /// </summary>
    private static (PointD Point, PointD Direction) Refine(GrayImage image, (PointD Point, PointD Direction) line, PointD from, PointD to, int reach)
    {
        var normal = new PointD(-line.Direction.Y, line.Direction.X);
        var points = new List<PointD>();
        const int samples = 60;
        for (int s = 0; s < samples; s++)
        {
            double t = 0.1 + (0.8 * s / (samples - 1));
            var at = new PointD(from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t));
            double bestGradient = 0;
            int bestOffset = int.MinValue;
            var profile = new double[(2 * reach) + 3];
            for (int o = -reach - 1; o <= reach + 1; o++)
            {
                profile[o + reach + 1] = Sample(image, at.X + (o * normal.X), at.Y + (o * normal.Y));
            }

            for (int o = -reach; o <= reach; o++)
            {
                double g = Math.Abs(profile[o + reach + 2] - profile[o + reach]);
                if (g > bestGradient)
                {
                    bestGradient = g;
                    bestOffset = o;
                }
            }

            if (bestOffset == int.MinValue || bestGradient < 8 || Math.Abs(bestOffset) == reach)
            {
                continue;
            }

            double left = Math.Abs(profile[bestOffset + reach + 1] - profile[bestOffset + reach - 1]);
            double right = Math.Abs(profile[bestOffset + reach + 3] - profile[bestOffset + reach + 1]);
            double denominator = left - (2 * bestGradient) + right;
            double shift = Math.Abs(denominator) < 1e-9 ? 0 : 0.5 * (left - right) / denominator;
            double offset = bestOffset + Math.Clamp(shift, -0.5, 0.5);
            points.Add(new PointD(at.X + (offset * normal.X), at.Y + (offset * normal.Y)));
        }

        if (points.Count < 10)
        {
            return line;
        }

        // One pass of trimming: the points furthest from the first fit, a hole or a staple on the edge, are left out of the second.
        var first = FitLine(points);
        var kept = points.OrderBy(p => LineDistance(p, first)).Take(Math.Max(10, (int)(0.8 * points.Count))).ToList();
        return FitLine(kept);
    }

    private static double Sample(GrayImage image, double x, double y)
    {
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 >= image.Width - 1 || y0 >= image.Height - 1)
        {
            return 0;
        }

        double fx = x - x0, fy = y - y0;
        double top = image[x0, y0] + ((image[x0 + 1, y0] - image[x0, y0]) * fx);
        double bottom = image[x0, y0 + 1] + ((image[x0 + 1, y0 + 1] - image[x0, y0 + 1]) * fx);
        return top + ((bottom - top) * fy);
    }
}
