using GroupLab.Core.Imaging;

namespace GroupLab.Core.Detection;

/// <summary>A hole's centre fitted to its edge, with what the fit saw.</summary>
/// <param name="Centre">The fitted centre, in image pixels.</param>
/// <param name="RadiusPixels">The fitted radius.</param>
/// <param name="Rays">How many rays found an edge and were kept.</param>
/// <param name="ResidualPixels">The root mean square distance of the kept edge points from the circle.</param>
/// <param name="Shadow">A unit vector toward the side whose edge is softest, which on a scan is the side the lamp's shadow falls on; zero where no side is.</param>
public sealed record HoleEdge(PointD Centre, double RadiusPixels, int Rays, double ResidualPixels, PointD Shadow);

/// <summary>
/// A hole's centre from its edge rather than from its dark area, NOTES-FROM-PLANNING.md entry 170 section 4.3.
/// <para>
/// <b>Why the edge.</b> A flatbed scanner lights from one side, so a hole in card stock carries a shadow on the same edge every time, and
/// question 38 showed that shadow is what drives a photographed hole's measured size. A centre taken from the dark area is pulled toward the
/// shadow by however much shadow there is. The torn edge is where the paper stops, which the shadow does not move, only softens: so the edge
/// is found along rays from the centre, each ray is weighted by how sharp its crossing is, and a circle is fitted to the edges, so the soft
/// shadowed side counts for less than the sharp lit side.
/// </para>
/// <para>
/// This measures; it does not yet replace the detector's centre. Entry 170 asks for the comparison first, so what it finds decides that.
/// </para>
/// </summary>
public static class HoleEdgeFit
{
    /// <summary>The number of rays cast from the centre.</summary>
    public const int RayCount = 72;

    /// <summary>The fewest rays that must find an edge for a circle to be fitted.</summary>
    public const int FewestRays = 24;

    /// <summary>
    /// The hole's centre fitted to its edge, starting from <paramref name="guess"/> with the hole's rough radius, or null where too few rays
    /// found an edge: a hole on printed ink, at the sheet's edge or merged with another.
    /// </summary>
    /// <param name="residual">True where <paramref name="value"/> is the detector's residual, bright where the hole is and zero on paper
    /// and printed ink alike, which is the better image to fit on because ink cancels out of it.</param>
    public static HoleEdge? Fit(GrayImage value, PointD guess, double radiusPixels, bool residual = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // The rays are cast from the guess, so a guess off the true centre meets the edge at a slant on one side. The fit is run again from
        // its own centre until it moves less than a quarter of a pixel, which it does in two or three passes on every hole measured.
        HoleEdge? fit = null;
        var from = guess;
        for (int pass = 0; pass < 5; pass++)
        {
            if (Once(value, from, radiusPixels, residual) is not { } next)
            {
                return fit;
            }

            bool settled = Math.Sqrt(Math.Pow(next.Centre.X - from.X, 2) + Math.Pow(next.Centre.Y - from.Y, 2)) < 0.25;
            fit = next;
            from = next.Centre;
            if (settled)
            {
                break;
            }
        }

        return fit;
    }

    private static HoleEdge? Once(GrayImage value, PointD guess, double radiusPixels, bool residual)
    {
        if (radiusPixels < 2)
        {
            return null;
        }

        // The two levels: the hole's own darkness inside, and the paper's brightness in a ring well outside the torn edge.
        var inside = Samples(value, guess, 0, 0.45 * radiusPixels, residual);
        var outside = Samples(value, guess, 1.7 * radiusPixels, 2.3 * radiusPixels, residual);
        if (inside.Count == 0 || outside.Count == 0)
        {
            return null;
        }

        double hole = Percentile(inside, 0.2), paper = Percentile(outside, 0.8);
        if (paper - hole < 30)
        {
            return null;
        }

        // The paper's edge along a ray: the point just past the last sample clearly darker than the paper, once the paper has held for a
        // fifth of the radius. Whatever is inside the hole, the lid seen through it, unevenly lit, or the shadow of the torn edge, is not
        // paper, so this finds where the paper starts on the lit side and on the shadowed side alike. A ray that meets printed ink at the
        // edge runs on through it and lands wide; the robust refit below drops it.
        double notPaper = paper - (0.35 * (paper - hole));
        var points = new List<(PointD Point, double Weight, PointD Direction, double Width)>();
        for (int k = 0; k < RayCount; k++)
        {
            double angle = 2 * Math.PI * k / RayCount;
            var direction = new PointD(Math.Cos(angle), Math.Sin(angle));
            const double step = 0.25;
            double? lastDark = null;
            double run = 0, previous = double.NaN, darkValue = double.NaN;
            double edge = double.NaN, softness = 0;
            for (double r = 0.3 * radiusPixels; r <= 1.8 * radiusPixels; r += step)
            {
                double here = Sample(value, guess, direction, r, residual);
                if (double.IsNaN(here))
                {
                    lastDark = null;
                    break;
                }

                if (here < notPaper)
                {
                    lastDark = r;
                    darkValue = here;
                    run = 0;
                }
                else if (lastDark is not null)
                {
                    run += step;
                    if (run >= 0.2 * radiusPixels)
                    {
                        break;
                    }
                }

                previous = here;
            }

            if (lastDark is not { } dark)
            {
                continue;
            }

            // Between the last dark sample and the next, where the profile crosses the threshold.
            double next = Sample(value, guess, direction, dark + step, residual);
            edge = double.IsNaN(next) || next <= darkValue ? dark : dark + (step * (notPaper - darkValue) / (next - darkValue));
            softness = Math.Max(0.5, paper - darkValue);
            _ = previous;
            points.Add((new PointD(guess.X + (edge * direction.X), guess.Y + (edge * direction.Y)), 1, direction, softness));
        }

        if (points.Count < FewestRays)
        {
            return null;
        }

        // A circle fitted by weighted least squares, then fitted again without the rays more than three robust deviations off it, which is
        // where printed ink or a neighbouring hole gave a ray a false edge.
        var circle = Circle(points.Select(p => (p.Point, p.Weight)).ToList());
        if (circle is null)
        {
            return null;
        }

        var residuals = points.Select(p => Math.Abs(Distance(p.Point, circle.Value.Centre) - circle.Value.Radius)).ToList();
        double mad = Percentile(residuals, 0.5) * 1.4826;
        var kept = points.Where((_, i) => residuals[i] <= Math.Max(3 * mad, 0.5)).ToList();
        if (kept.Count < FewestRays || Circle(kept.Select(p => (p.Point, p.Weight)).ToList()) is not { } final)
        {
            return null;
        }

        double rms = Math.Sqrt(kept.Average(p => Math.Pow(Distance(p.Point, final.Centre) - final.Radius, 2)));
        double meanWidth = kept.Average(p => p.Width);
        double sx = kept.Sum(p => (p.Width - meanWidth) * p.Direction.X), sy = kept.Sum(p => (p.Width - meanWidth) * p.Direction.Y);
        double norm = Math.Sqrt((sx * sx) + (sy * sy));
        var shadow = norm > 1e-9 ? new PointD(sx / norm, sy / norm) : new PointD(0, 0);
        return new HoleEdge(final.Centre, final.Radius, kept.Count, rms, shadow);
    }

    /// <summary>A weighted algebraic circle fit, x^2 + y^2 + Dx + Ey + F = 0, solved in coordinates about the points' mean.</summary>
    private static (PointD Centre, double Radius)? Circle(IReadOnlyList<(PointD Point, double Weight)> points)
    {
        double w = points.Sum(p => p.Weight);
        double mx = points.Sum(p => p.Weight * p.Point.X) / w, my = points.Sum(p => p.Weight * p.Point.Y) / w;
        double suu = 0, suv = 0, svv = 0, suuu = 0, svvv = 0, suvv = 0, svuu = 0;
        foreach (var (point, weight) in points)
        {
            double u = point.X - mx, v = point.Y - my;
            suu += weight * u * u;
            suv += weight * u * v;
            svv += weight * v * v;
            suuu += weight * u * u * u;
            svvv += weight * v * v * v;
            suvv += weight * u * v * v;
            svuu += weight * v * u * u;
        }

        double det = (suu * svv) - (suv * suv);
        if (Math.Abs(det) < 1e-12)
        {
            return null;
        }

        double a = (suuu + suvv) / 2, b = (svvv + svuu) / 2;
        double uc = ((a * svv) - (b * suv)) / det, vc = ((b * suu) - (a * suv)) / det;
        double radius = Math.Sqrt((uc * uc) + (vc * vc) + ((suu + svv) / w));
        return (new PointD(uc + mx, vc + my), radius);
    }

    private static List<double> Samples(GrayImage image, PointD centre, double from, double to, bool residual)
    {
        var values = new List<double>();
        int reach = (int)Math.Ceiling(to);
        for (int y = (int)centre.Y - reach; y <= (int)centre.Y + reach; y++)
        {
            for (int x = (int)centre.X - reach; x <= (int)centre.X + reach; x++)
            {
                double d = Math.Sqrt(Math.Pow(x - centre.X, 2) + Math.Pow(y - centre.Y, 2));
                if (d >= from && d <= to && x >= 0 && y >= 0 && x < image.Width && y < image.Height)
                {
                    values.Add(residual ? 255 - image[x, y] : image[x, y]);
                }
            }
        }

        return values;
    }

    private static double Sample(GrayImage image, PointD centre, PointD direction, double r, bool residual)
    {
        double x = centre.X + (r * direction.X), y = centre.Y + (r * direction.Y);
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 + 1 >= image.Width || y0 + 1 >= image.Height)
        {
            return double.NaN;
        }

        double fx = x - x0, fy = y - y0;
        double v = (image[x0, y0] * (1 - fx) * (1 - fy)) + (image[x0 + 1, y0] * fx * (1 - fy)) + (image[x0, y0 + 1] * (1 - fx) * fy) + (image[x0 + 1, y0 + 1] * fx * fy);
        return residual ? 255 - v : v;
    }

    private static double Percentile(List<double> values, double q)
    {
        values.Sort();
        return values[(int)Math.Clamp(Math.Round(q * (values.Count - 1)), 0, values.Count - 1)];
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
