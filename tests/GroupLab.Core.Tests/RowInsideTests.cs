using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 447 section 4.3: a mark's row inside its hull is found by halving from one pixel known to be inside, and
/// gives the same two ends as walking every pixel from each end of the box, on every row of thousands of hulls: small and large, many
/// cornered, with flat edges, corners on whole pixels and corners between them.
/// </summary>
public class RowInsideTests
{
    private static List<PointD> Hull(IEnumerable<PointD> points)
    {
        // Andrew's monotone chain: the convex hull, anticlockwise, without repeated points.
        var p = points.Distinct().OrderBy(q => q.X).ThenBy(q => q.Y).ToList();
        if (p.Count < 3)
        {
            return p;
        }

        double Cross(PointD o, PointD a, PointD b) => ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<PointD>();
        foreach (var pass in new[] { p, Enumerable.Reverse(p).ToList() })
        {
            int start = hull.Count;
            foreach (var q in pass)
            {
                while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], q) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(q);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    [Fact]
    public void HalvingFindsTheEndsWalkingFound()
    {
        var random = new Random(447);
        int rows = 0;
        for (int n = 0; n < 1500; n++)
        {
            int size = random.Next(2, n % 10 == 0 ? 500 : 60), corners = random.Next(3, 60);
            bool whole = n % 3 == 0;
            var points = Enumerable.Range(0, corners).Select(_ =>
            {
                double angle = random.NextDouble() * 2 * Math.PI, r = size * (0.5 + (random.NextDouble() / 2));
                double x = 1000 + (r * Math.Cos(angle) * (n % 4 == 0 ? 3 : 1)), y = 1000 + (r * Math.Sin(angle));
                return whole ? new PointD(Math.Round(x), Math.Round(y)) : new PointD(x, y);
            }).ToList();
            if (n % 5 == 0)
            {
                // A flat top and bottom, as a mark cut by a printed line has.
                points.Add(new PointD(1000 - size, 1000 - size));
                points.Add(new PointD(1000 + size, 1000 - size));
            }

            var hull = Hull(points);
            if (hull.Count < 3)
            {
                continue;
            }

            int left = (int)Math.Floor(hull.Min(p => p.X)), top = (int)Math.Floor(hull.Min(p => p.Y));
            int right = (int)Math.Ceiling(hull.Max(p => p.X)), bottom = (int)Math.Ceiling(hull.Max(p => p.Y));
            var blob = new ImageBlob(0, left, top, right - left + 1, bottom - top + 1, hull);
            for (int y = blob.Top; y < blob.Top + blob.Height; y++)
            {
                Assert.Equal(RenderDifferenceHoleDetector.RowInsideWalked(blob, y), RenderDifferenceHoleDetector.RowInside(blob, y));
                rows++;
            }
        }

        Assert.True(rows > 40_000, $"only {rows} rows");
    }
}
