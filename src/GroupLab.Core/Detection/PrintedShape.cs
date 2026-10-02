using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>
/// Whether a mark the any-target finder proposed is printing rather than a hole, NOTES-FROM-PLANNING.md entry 331 section 1, from the five
/// store-bought blanks of request 58: on two of them, eleven marks on clean sheets were printed ring numbers and letters (white 7 and 8 on
/// black, black 6 and the logo's R on white) and solid black diamonds in a red band. A hole is a compact patch: the widest circle that fits
/// inside it is most of its own size, even torn. A printed number or letter is strokes, so the widest circle inside it is a stroke's width.
/// A printed diamond or square is solid with straight sides and sharp corners, filling the smallest rectangle around it almost wholly,
/// where a round hole fills about 0.79 of it and two touching holes less.
/// <para>
/// Entry 352 item 2: six marks were left on the Eze-Scorer, its printed 6s and 7s and two letters of its logo. Bold printing is strokes of
/// one width all along, which a torn hole is not, and a 6 closes round a small counter, which a hole's rim never does. That refuses the
/// numbers; the logo's two letters have no counter and stay, as no measure here tells them from a dark crescent of rim on a real scan.
/// </para>
/// </summary>
public static class PrintedShape
{
    /// <summary>Strokes: the widest circle inside, over the radius of a disc of the same area, below this.</summary>
    public const double StrokeRatio = 0.45;

    /// <summary>
    /// Entry 352 item 2, strokes of one width: stroke-like below these (the widest circle inside, with and without the counters filled), and
    /// the ridge's median distance at least this share of its widest, for a light mark and a dark one. A printed glyph measured 0.63 to
    /// 0.92; the synthetic holes of the scoreboards at most 0.60, the real holes of the fifteen commercial scans at most 0.84 dark.
    /// </summary>
    public const double EvenStrokeRatio = 0.5, EvenFilledRatio = 0.78, LightEven = 0.62, DarkEven = 0.75;

    /// <summary>A small closed counter, as a share of the filled piece: a printed 6 measured 0.05 to 0.07.</summary>
    public static readonly (double Least, double Most) SmallCounter = (0.04, 0.15);

    /// <summary>And still stroke-like with its enclosed counters filled, as an 8 or a 6 is, where a hole's dark rim filled is a disc.</summary>
    public const double FilledRatio = 0.85;

    /// <summary>Straight sided: filling the smallest rectangle around it at least this much, and its own hull almost wholly.</summary>
    public const double Rectangular = 0.93, Solid = 0.95;

    /// <summary>The reason a mark is printing, or null where it may be a hole. <paramref name="dark"/> is whether the mark is darker than around it.</summary>
    public static string? Why(GrayImage value, double x, double y, double diameterPixels, bool dark)
    {
        ArgumentNullException.ThrowIfNull(value);
        double r = Math.Max(3, diameterPixels / 2);
        var near = Piece(value, x, y, r, 1.3, dark);
        if (near is { } p)
        {
            // Only for a light mark in print: a torn hole drawn dark on paper is often a dark rim round a grey or white centre, and its rim
            // alone is a thin ring that reads as strokes, so a dark mark is never judged by its strokes alone (the any-target scoreboard's grid
            // and diamond pictures, where four real holes were refused before this).
            if (!dark && p.Ratio < StrokeRatio && p.FilledRatio < FilledRatio)
            {
                return $"thin strokes, as a printed number or letter is: the widest circle inside is {p.Ratio:0.00} of a hole's of the same area";
            }

            var (hullArea, rectangleArea) = HullAndRectangle(p.Filled, p.W, p.H);
            if (hullArea > 0 && rectangleArea > 0 && p.FilledArea / hullArea > Solid && hullArea / rectangleArea > Rectangular)
            {
                return $"a solid shape with straight sides, as a printed diamond or square is: it fills {hullArea / rectangleArea:0.00} of the rectangle around it";
            }
        }

        // Entry 352 item 2: a printed number or letter larger than the mark the finder saw in it runs off the crop above, so it is looked at
        // again in a wider one, where it is whole; a piece that runs off that too is part of something larger, which the finder judges.
        var whole = near ?? Piece(value, x, y, r, Wider, dark);
        if (whole is not { } q)
        {
            return null;
        }

        // A printed number or letter is strokes of one width all along; a torn hole is a wide core with spikes that narrow to nothing. A
        // light mark in print is judged by that alone. A dark one also needs a small closed counter, as a printed 6, 8, 9 or 0 has: a hole
        // in a scan is often a dark crescent of rim, as even as a stroke, but it never closes round a small light counter (fifteen
        // commercial scans, where 44 real holes were refused before this; a rim closed round the hole's light centre encloses most of itself).
        double counter = (q.FilledArea - q.Area) / (double)q.FilledArea;
        if (q.Ratio < EvenStrokeRatio && q.FilledRatio < EvenFilledRatio && counter < SmallCounter.Most
            && (dark ? counter >= SmallCounter.Least && Even(q.Piece, q.W, q.H) >= DarkEven : Even(q.Piece, q.W, q.H) >= LightEven))
        {
            return dark
                ? "strokes of one width round a small counter, as a printed 6, 8, 9 or 0 has"
                : "strokes of one width all along, as a printed number or letter has";
        }

        return null;
    }

    /// <summary>The wider crop a mark is looked at again in, in the mark's radii from its centre.</summary>
    private const double Wider = 2.5;

    /// <summary>The mark's own piece, its enclosed regions filled, and how stroke-like each is; null where it runs off the crop.</summary>
    private readonly record struct Shape(bool[] Piece, bool[] Filled, int W, int H, int Area, int FilledArea, double Ratio, double FilledRatio);

    private static Shape? Piece(GrayImage value, double x, double y, double r, double reach, bool dark)
    {
        int half = (int)Math.Ceiling(reach * r) + 2;
        int x0 = Math.Max(0, (int)x - half), y0 = Math.Max(0, (int)y - half);
        int x1 = Math.Min(value.Width - 1, (int)x + half), y1 = Math.Min(value.Height - 1, (int)y + half);
        int w = x1 - x0 + 1, h = y1 - y0 + 1;
        if (w < 5 || h < 5)
        {
            return null;
        }

        var crop = new byte[w * h];
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                crop[(j * w) + i] = value.Pixels[((y0 + j) * value.Width) + x0 + i];
            }
        }

        int threshold = Otsu(crop);
        var mask = new bool[crop.Length];
        for (int k = 0; k < crop.Length; k++)
        {
            mask[k] = dark ? crop[k] <= threshold : crop[k] > threshold;
        }

        // The mark's own piece: the connected piece with the most of itself within the mark's radius of its centre.
        var label = new int[crop.Length];
        int best = 0, bestInside = 0, next = 0;
        var stack = new Stack<int>();
        double cx = x - x0, cy = y - y0;
        for (int start = 0; start < crop.Length; start++)
        {
            if (!mask[start] || label[start] != 0)
            {
                continue;
            }

            next++;
            int inside = 0;
            label[start] = next;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int p = stack.Pop();
                int px = p % w, py = p / w;
                if (((px - cx) * (px - cx)) + ((py - cy) * (py - cy)) <= r * r)
                {
                    inside++;
                }

                foreach (int q in new[] { p - 1, p + 1, p - w, p + w })
                {
                    if (q >= 0 && q < crop.Length && Math.Abs((q % w) - px) <= 1 && mask[q] && label[q] == 0)
                    {
                        label[q] = next;
                        stack.Push(q);
                    }
                }
            }

            if (inside > bestInside)
            {
                (best, bestInside) = (next, inside);
            }
        }

        if (best == 0)
        {
            return null;
        }

        var piece = new bool[crop.Length];
        int area = 0;
        for (int k = 0; k < crop.Length; k++)
        {
            if (label[k] == best)
            {
                piece[k] = true;
                area++;
            }
        }

        // A piece that runs off the crop is part of something larger than the mark.
        for (int i = 0; i < w; i++)
        {
            if (piece[i] || piece[((h - 1) * w) + i])
            {
                return null;
            }
        }

        for (int j = 0; j < h; j++)
        {
            if (piece[j * w] || piece[(j * w) + w - 1])
            {
                return null;
            }
        }

        var filled = Filled(piece, w, h);
        int filledArea = filled.Count(f => f);
        return new Shape(piece, filled, w, h, area, filledArea, Inscribed(piece, w, h) / Math.Sqrt(area / Math.PI), Inscribed(filled, w, h) / Math.Sqrt(filledArea / Math.PI));
    }

    /// <summary>The radius of the widest circle inside the piece, in pixels, by a 3-4 chamfer distance.</summary>
    private static double Inscribed(bool[] piece, int w, int h) => Distances(piece, w, h).Max() / 3.0;

    /// <summary>Each pixel's distance to the nearest pixel outside the piece, in thirds of a pixel, by a 3-4 chamfer.</summary>
    private static int[] Distances(bool[] piece, int w, int h)
    {
        var d = new int[piece.Length];
        const int Big = int.MaxValue / 4;
        for (int k = 0; k < d.Length; k++)
        {
            d[k] = piece[k] ? Big : 0;
        }

        int At(int i, int j) => i < 0 || j < 0 || i >= w || j >= h ? 0 : d[(j * w) + i];
        for (int j = 0; j < h; j++)
        {
            for (int i = 0; i < w; i++)
            {
                int k = (j * w) + i;
                if (d[k] == 0)
                {
                    continue;
                }

                d[k] = Math.Min(d[k], Math.Min(Math.Min(At(i - 1, j) + 3, At(i, j - 1) + 3), Math.Min(At(i - 1, j - 1) + 4, At(i + 1, j - 1) + 4)));
            }
        }

        for (int j = h - 1; j >= 0; j--)
        {
            for (int i = w - 1; i >= 0; i--)
            {
                int k = (j * w) + i;
                if (d[k] == 0)
                {
                    continue;
                }

                d[k] = Math.Min(d[k], Math.Min(Math.Min(At(i + 1, j) + 3, At(i, j + 1) + 3), Math.Min(At(i + 1, j + 1) + 4, At(i - 1, j + 1) + 4)));
            }
        }

        return d;
    }

    /// <summary>
    /// How even the piece's strokes are: the median distance along its ridge (each pixel at least as far inside as its eight neighbours, and a
    /// pixel or more in) over the ridge's widest. A printed letter's strokes are one width all along; a torn hole is a wide core with spikes
    /// that narrow to nothing.
    /// </summary>
    internal static double Even(bool[] piece, int w, int h)
    {
        var d = Distances(piece, w, h);
        var ridge = new List<int>();
        for (int j = 1; j < h - 1; j++)
        {
            for (int i = 1; i < w - 1; i++)
            {
                int k = (j * w) + i;
                if (d[k] < 3)
                {
                    continue;
                }

                bool top = true;
                for (int dj = -1; dj <= 1 && top; dj++)
                {
                    for (int di = -1; di <= 1 && top; di++)
                    {
                        top = d[((j + dj) * w) + i + di] <= d[k];
                    }
                }

                if (top)
                {
                    ridge.Add(d[k]);
                }
            }
        }

        if (ridge.Count < 3)
        {
            return 0;
        }

        ridge.Sort();
        return ridge[ridge.Count / 2] / (double)ridge[^1];
    }

    /// <summary>The piece with every region it encloses filled: the background not reachable from the crop's border.</summary>
    private static bool[] Filled(bool[] piece, int w, int h)
    {
        var outside = new bool[piece.Length];
        var stack = new Stack<int>();
        for (int i = 0; i < w; i++)
        {
            foreach (int k in new[] { i, ((h - 1) * w) + i })
            {
                if (!piece[k] && !outside[k])
                {
                    outside[k] = true;
                    stack.Push(k);
                }
            }
        }

        for (int j = 0; j < h; j++)
        {
            foreach (int k in new[] { j * w, (j * w) + w - 1 })
            {
                if (!piece[k] && !outside[k])
                {
                    outside[k] = true;
                    stack.Push(k);
                }
            }
        }

        while (stack.Count > 0)
        {
            int p = stack.Pop();
            int px = p % w;
            foreach (int q in new[] { p - 1, p + 1, p - w, p + w })
            {
                if (q >= 0 && q < piece.Length && Math.Abs((q % w) - px) <= 1 && !piece[q] && !outside[q])
                {
                    outside[q] = true;
                    stack.Push(q);
                }
            }
        }

        return [.. outside.Select(o => !o)];
    }

    /// <summary>The convex hull's area and the smallest rectangle around it, from the pixel corners of the filled piece.</summary>
    private static (double Hull, double Rectangle) HullAndRectangle(bool[] filled, int w, int h)
    {
        var points = new List<(double X, double Y)>();
        for (int j = 0; j < h; j++)
        {
            int first = -1, last = -1;
            for (int i = 0; i < w; i++)
            {
                if (filled[(j * w) + i])
                {
                    first = first < 0 ? i : first;
                    last = i;
                }
            }

            if (first >= 0)
            {
                points.Add((first, j));
                points.Add((first, j + 1));
                points.Add((last + 1, j));
                points.Add((last + 1, j + 1));
            }
        }

        if (points.Count < 3)
        {
            return (0, 0);
        }

        // Andrew's monotone chain.
        points.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        static double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<(double X, double Y)>();
        foreach (var pass in new[] { points, Enumerable.Reverse(points).ToList() })
        {
            int start = hull.Count;
            foreach (var p in pass)
            {
                while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], p) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(p);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        double area = 0;
        for (int k = 0; k < hull.Count; k++)
        {
            var a = hull[k];
            var b = hull[(k + 1) % hull.Count];
            area += (a.X * b.Y) - (b.X * a.Y);
        }

        area = Math.Abs(area) / 2;

        // The smallest rectangle has a side along one of the hull's edges.
        double smallest = double.MaxValue;
        for (int k = 0; k < hull.Count; k++)
        {
            var a = hull[k];
            var b = hull[(k + 1) % hull.Count];
            double ex = b.X - a.X, ey = b.Y - a.Y, len = Math.Sqrt((ex * ex) + (ey * ey));
            if (len < 1e-9)
            {
                continue;
            }

            (ex, ey) = (ex / len, ey / len);
            double minU = double.MaxValue, maxU = double.MinValue, minV = double.MaxValue, maxV = double.MinValue;
            foreach (var p in hull)
            {
                double u = (p.X * ex) + (p.Y * ey), v = (-p.X * ey) + (p.Y * ex);
                (minU, maxU, minV, maxV) = (Math.Min(minU, u), Math.Max(maxU, u), Math.Min(minV, v), Math.Max(maxV, v));
            }

            smallest = Math.Min(smallest, (maxU - minU) * (maxV - minV));
        }

        return (area, smallest);
    }

    /// <summary>Otsu's threshold of a crop's levels.</summary>
    private static int Otsu(byte[] pixels)
    {
        var histogram = new int[256];
        foreach (byte p in pixels)
        {
            histogram[p]++;
        }

        double total = pixels.Length, sum = 0;
        for (int t = 0; t < 256; t++)
        {
            sum += t * histogram[t];
        }

        double sumB = 0, wB = 0, best = -1;
        int threshold = 127;
        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0)
            {
                continue;
            }

            double wF = total - wB;
            if (wF == 0)
            {
                break;
            }

            sumB += t * histogram[t];
            double mB = sumB / wB, mF = (sum - sumB) / wF;
            double between = wB * wF * (mB - mF) * (mB - mF);
            if (between > best)
            {
                (best, threshold) = (between, t);
            }
        }

        return threshold;
    }
}
