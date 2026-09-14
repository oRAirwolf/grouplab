using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>The axis-aligned box around a group: its edges, width, height, figure of merit (the mean of the two) and diagonal.</summary>
public sealed record BoundingBox(double Left, double Bottom, double Right, double Top)
{
    public double Width => Right - Left;

    public double Height => Top - Bottom;

    public double FigureOfMerit => (Width + Height) / 2;

    public double Diagonal => Math.Sqrt((Width * Width) + (Height * Height));
}

/// <summary>
/// A rotated box: its four corners, the lengths of its first and second sides, and the direction of its longer side in degrees in
/// [0, 180), as an axis has no sign. That is shotGroups' <c>getMinBBox</c> convention: width is always the first side, and the
/// angle follows whichever side is longer.
/// </summary>
public sealed record RotatedBox(IReadOnlyList<PointD> Corners, double Width, double Height, double AngleDegrees)
{
    public double FigureOfMerit => (Width + Height) / 2;

    public double Diagonal => Math.Sqrt((Width * Width) + (Height * Height));
}

/// <summary>An ellipse as a centre and a 2 by 2 shape matrix E, the points x with (x - c)' E^-1 (x - c) &lt;= 1.</summary>
public sealed record ShapeEllipse(PointD Centre, double Exx, double Exy, double Eyy)
{
    public EllipseShape Shape => GroupStatistics.Shape(Exx, Exy, Eyy);

    public double SemiMajor => Math.Sqrt(Shape.Major);

    public double SemiMinor => Math.Sqrt(Shape.Minor);

    public double Area => Math.PI * SemiMajor * SemiMinor;
}

/// <summary>
/// The geometric constructions of docs/STATISTICS.md sections 5 and 6: extreme spread, the bounding boxes, the minimum
/// enclosing circle and the minimum-volume enclosing ellipse. Section 15.3 compares the exact ones at 1e-9 absolute, and the
/// ellipse, which is iterative, at 1e-4 relative with shotGroups' own stopping tolerance.
/// </summary>
public static class GroupGeometry
{
    /// <summary>The largest distance between two shots, extreme spread, with the zero-based indices of the pair.</summary>
    public static (double Distance, int First, int Second) MaximumPairDistance(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        double best = -1;
        int a = 0, b = 0;
        for (int i = 0; i < shots.Count; i++)
        {
            for (int j = i + 1; j < shots.Count; j++)
            {
                double dx = shots[i].X - shots[j].X, dy = shots[i].Y - shots[j].Y, d = (dx * dx) + (dy * dy);
                if (d > best)
                {
                    (best, a, b) = (d, i, j);
                }
            }
        }

        return (Math.Sqrt(best), a, b);
    }

    public static BoundingBox Box(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        return new BoundingBox(shots.Min(s => s.X), shots.Min(s => s.Y), shots.Max(s => s.X), shots.Max(s => s.Y));
    }

    /// <summary>The convex hull by Andrew's monotone chain, counter-clockwise, collinear points dropped.</summary>
    public static List<PointD> ConvexHull(IReadOnlyList<PointD> shots)
    {
        ArgumentNullException.ThrowIfNull(shots);
        var points = shots.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        if (points.Count < 3)
        {
            return points;
        }

        static double Cross(PointD o, PointD a, PointD b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<PointD>();
        foreach (var p in points)
        {
            while (hull.Count >= 2 && Cross(hull[^2], hull[^1], p) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(p);
        }

        int lower = hull.Count + 1;
        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count >= lower && Cross(hull[^2], hull[^1], points[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(points[i]);
        }

        hull.RemoveAt(hull.Count - 1);
        return hull;
    }

    /// <summary>
    /// The minimum-area enclosing rectangle: one of its sides lies along an edge of the convex hull, so every hull edge is
    /// tried and the smallest box kept. Width is the side along that edge.
    /// </summary>
    public static RotatedBox MinimumAreaBox(IReadOnlyList<PointD> shots)
    {
        var hull = ConvexHull(shots);
        RotatedBox? best = null;
        double bestArea = double.PositiveInfinity;
        for (int i = 0; i < hull.Count; i++)
        {
            var p = hull[i];
            var q = hull[(i + 1) % hull.Count];
            double length = Math.Sqrt(((q.X - p.X) * (q.X - p.X)) + ((q.Y - p.Y) * (q.Y - p.Y)));
            double ux = (q.X - p.X) / length, uy = (q.Y - p.Y) / length;
            double minU = double.PositiveInfinity, maxU = double.NegativeInfinity, minV = double.PositiveInfinity, maxV = double.NegativeInfinity;
            foreach (var h in hull)
            {
                double u = (h.X * ux) + (h.Y * uy), v = (-h.X * uy) + (h.Y * ux);
                minU = Math.Min(minU, u);
                maxU = Math.Max(maxU, u);
                minV = Math.Min(minV, v);
                maxV = Math.Max(maxV, v);
            }

            double area = (maxU - minU) * (maxV - minV);
            if (area < bestArea)
            {
                bestArea = area;
                PointD Corner(double u, double v) => new((u * ux) - (v * uy), (u * uy) + (v * ux));
                double angle = (maxU - minU >= maxV - minV ? Math.Atan2(uy, ux) : Math.Atan2(ux, -uy)) * 180 / Math.PI;
                best = new RotatedBox([Corner(minU, minV), Corner(maxU, minV), Corner(maxU, maxV), Corner(minU, maxV)], maxU - minU, maxV - minV, angle < 0 ? angle + 180 : angle >= 180 ? angle - 180 : angle);
            }
        }

        return best ?? new RotatedBox([.. hull], 0, 0, 0);
    }

    /// <summary>
    /// The minimum enclosing circle by Welzl's algorithm in its iterative form, over the hull in a fixed order: exact up to
    /// floating point, and deterministic, because the order of a randomised Welzl only changes its running time.
    /// </summary>
    public static (PointD Centre, double Radius) MinimumEnclosingCircle(IReadOnlyList<PointD> shots)
    {
        var points = ConvexHull(shots);
        if (points.Count == 0)
        {
            return (new PointD(double.NaN, double.NaN), double.NaN);
        }

        static (PointD C, double R) Two(PointD a, PointD b) =>
            (new PointD((a.X + b.X) / 2, (a.Y + b.Y) / 2), Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / 2);

        static (PointD C, double R) Three(PointD a, PointD b, PointD c)
        {
            double bx = b.X - a.X, by = b.Y - a.Y, cx = c.X - a.X, cy = c.Y - a.Y, d = 2 * ((bx * cy) - (by * cx));
            if (Math.Abs(d) < 1e-300)
            {
                var candidates = new[] { Two(a, b), Two(a, c), Two(b, c) };
                return candidates.MaxBy(t => t.R);
            }

            double ux = ((cy * ((bx * bx) + (by * by))) - (by * ((cx * cx) + (cy * cy)))) / d;
            double uy = ((bx * ((cx * cx) + (cy * cy))) - (cx * ((bx * bx) + (by * by)))) / d;
            return (new PointD(a.X + ux, a.Y + uy), Math.Sqrt((ux * ux) + (uy * uy)));
        }

        static bool Inside((PointD C, double R) circle, PointD p) =>
            Math.Sqrt(((p.X - circle.C.X) * (p.X - circle.C.X)) + ((p.Y - circle.C.Y) * (p.Y - circle.C.Y))) <= circle.R * (1 + 1e-12);

        var best = (C: points[0], R: 0.0);
        for (int i = 1; i < points.Count; i++)
        {
            if (Inside(best, points[i]))
            {
                continue;
            }

            best = (points[i], 0);
            for (int j = 0; j < i; j++)
            {
                if (Inside(best, points[j]))
                {
                    continue;
                }

                best = Two(points[i], points[j]);
                for (int k = 0; k < j; k++)
                {
                    if (!Inside(best, points[k]))
                    {
                        best = Three(points[i], points[j], points[k]);
                    }
                }
            }
        }

        return (best.C, best.R);
    }

    /// <summary>
    /// The minimum-volume enclosing ellipse by Khachiyan's algorithm, as shotGroups' <c>getMinEllipse</c> computes it, with its
    /// default tolerance 0.001 on the change in the weights and 1000 iterations. docs/STATISTICS.md section 15.3 says to match
    /// the tolerance or expect disagreement, so the stopping rule is the package's, not a tighter one.
    /// </summary>
    public static ShapeEllipse MinimumVolumeEllipse(IReadOnlyList<PointD> shots, double tolerance = 0.001, int maximumIterations = 1000)
    {
        var points = ConvexHull(shots);
        int n = points.Count;
        var u = Enumerable.Repeat(1.0 / n, n).ToArray();
        for (int iteration = 0; iteration < maximumIterations; iteration++)
        {
            // X = sum u_i q_i q_i' with q_i = (x, y, 1).
            double sxx = 0, sxy = 0, sx = 0, syy = 0, sy = 0, s1 = 0;
            for (int i = 0; i < n; i++)
            {
                double x = points[i].X, y = points[i].Y, w = u[i];
                sxx += w * x * x;
                sxy += w * x * y;
                sx += w * x;
                syy += w * y * y;
                sy += w * y;
                s1 += w;
            }

            var inverse = Invert3(sxx, sxy, sx, syy, sy, s1);
            int j = 0;
            double maximum = double.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                double x = points[i].X, y = points[i].Y;
                double m = (inverse[0] * x * x) + (2 * inverse[1] * x * y) + (2 * inverse[2] * x) + (inverse[4] * y * y) + (2 * inverse[5] * y) + inverse[8];
                if (m > maximum)
                {
                    (maximum, j) = (m, i);
                }
            }

            const int d = 2;
            double step = (maximum - d - 1) / ((d + 1) * (maximum - 1)), change = 0;
            for (int i = 0; i < n; i++)
            {
                double next = (1 - step) * u[i] + (i == j ? step : 0);
                change += (next - u[i]) * (next - u[i]);
                u[i] = next;
            }

            if (Math.Sqrt(change) <= tolerance)
            {
                break;
            }
        }

        double cx = 0, cy = 0, axx = 0, axy = 0, ayy = 0;
        for (int i = 0; i < n; i++)
        {
            cx += u[i] * points[i].X;
            cy += u[i] * points[i].Y;
            axx += u[i] * points[i].X * points[i].X;
            axy += u[i] * points[i].X * points[i].Y;
            ayy += u[i] * points[i].Y * points[i].Y;
        }

        // E^-1 = (1/d) (P' U P - c c')^-1, so E = d (P' U P - c c').
        axx -= cx * cx;
        axy -= cx * cy;
        ayy -= cy * cy;
        return new ShapeEllipse(new PointD(cx, cy), 2 * axx, 2 * axy, 2 * ayy);
    }

    /// <summary>The inverse of the symmetric 3 by 3 matrix [[a, b, c], [b, d, e], [c, e, f]], row-major.</summary>
    private static double[] Invert3(double a, double b, double c, double d, double e, double f)
    {
        double A = (d * f) - (e * e), B = -((b * f) - (c * e)), C = (b * e) - (c * d);
        double D = (a * f) - (c * c), E = -((a * e) - (b * c)), F = (a * d) - (b * b);
        double det = (a * A) + (b * B) + (c * C);
        return [A / det, B / det, C / det, B / det, D / det, E / det, C / det, E / det, F / det];
    }
}
