using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260, "paper that is not flat": the page registered through every marker corner by a thin-plate spline, so a
/// curl, a wave or a fold is followed rather than refused. A GroupLab sheet carries thirty or more markers, each with four corners known on
/// the page, and a thin-plate spline is the smoothest surface through them: exact where a marker is, and bending no more than it must
/// between them. On 2026-09-28 a rendered sheet bowed by 15 pixels across its width at 300 dpi read every one of its 38 markers and still
/// failed to register, because a homography, even with radial distortion, keeps none of the corners of a bent sheet (entry 261's study).
/// <para>
/// Two splines are fitted, page to image and image to page, each in coordinates centred and scaled to about one, so the system is well
/// conditioned. A corner a misread marker put far from the rest is dropped after a first fit and the splines fitted again. The residual a
/// person is told is each marker's corners predicted by a mesh fitted without that marker, which is what a point between markers can expect.
/// </para>
/// </summary>
public sealed class MarkerMesh : IPageMapping
{
    private readonly Spline toImage;
    private readonly Spline toPage;

    private MarkerMesh(Spline toImage, Spline toPage, IReadOnlyList<bool> kept, double leaveOneOutRms, IReadOnlyList<PointD> imagePoints, IReadOnlyList<PointD> pagePoints)
    {
        this.toImage = toImage;
        this.toPage = toPage;
        Kept = kept;
        LeaveOneOutRms = leaveOneOutRms;
        ImagePoints = imagePoints;
        PagePoints = pagePoints;
    }

    public string Model => "a mesh through every marker";

    /// <summary>Whether each corner was kept, in the order given.</summary>
    public IReadOnlyList<bool> Kept { get; }

    /// <summary>Each marker's corners, in page dmm, as a mesh fitted without that marker placed them: the error between markers.</summary>
    public double LeaveOneOutRms { get; }

    /// <summary>The corners the mesh passes through, in image pixels, which is all a saved session needs to rebuild it.</summary>
    public IReadOnlyList<PointD> ImagePoints { get; }

    /// <summary>The same corners on the page, in dmm.</summary>
    public IReadOnlyList<PointD> PagePoints { get; }

    public PointD ToImage(PointD page) => toImage.At(page);

    public PointD ToPage(PointD image) => toPage.At(image);

    public (double XX, double XY, double YX, double YY) Jacobian(PointD image) => toPage.Jacobian(image);

    /// <summary>
    /// The mesh through the corners, image pixels and page dmm, four to a marker in order; null with fewer than <paramref name="least"/>
    /// markers' corners.
    /// </summary>
    public static MarkerMesh? Fit(IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, double dropBeyondDmm = 3, int least = 12)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        if (image.Count != page.Count || image.Count < 4 * least)
        {
            return null;
        }

        var all = Enumerable.Range(0, image.Count).ToList();
        // The first pass is smoothed, so a corner a misread marker put out of place stands off it; an interpolating spline would pass
        // through that corner too and leave nothing to drop.
        var first = Spline.Fit([.. all.Select(i => image[i])], [.. all.Select(i => page[i])], 1e-2);
        var kept = all.Select(i => Distance(first.At(image[i]), page[i]) <= dropBeyondDmm).ToList();
        var use = all.Where(i => kept[i]).ToList();
        if (use.Count < 4 * least)
        {
            return null;
        }

        var forward = Spline.Fit([.. use.Select(i => page[i])], [.. use.Select(i => image[i])], 1e-6);
        var back = Spline.Fit([.. use.Select(i => image[i])], [.. use.Select(i => page[i])], 1e-6);

        // Leave one marker out: its four corners predicted by the mesh through the rest.
        double sum = 0;
        int n = 0;
        foreach (var marker in use.GroupBy(i => i / 4))
        {
            var rest = use.Where(i => i / 4 != marker.Key).ToList();
            var without = Spline.Fit([.. rest.Select(i => image[i])], [.. rest.Select(i => page[i])], 1e-6);
            foreach (int i in marker)
            {
                double d = Distance(without.At(image[i]), page[i]);
                sum += d * d;
                n++;
            }
        }

        return new MarkerMesh(forward, back, kept, Math.Sqrt(sum / Math.Max(1, n)), [.. use.Select(i => image[i])], [.. use.Select(i => page[i])]);
    }

    /// <summary>
    /// The mesh a saved session kept, rebuilt from its corners: the same fit through the same points, so the same mapping, with nothing
    /// dropped again and the error between markers as it was measured. Null where the corners do not pair up.
    /// </summary>
    public static MarkerMesh? Rebuild(IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, double leaveOneOutRms)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        if (image.Count != page.Count || image.Count < 4)
        {
            return null;
        }

        var forward = Spline.Fit([.. page], [.. image], 1e-6);
        var back = Spline.Fit([.. image], [.. page], 1e-6);
        return new MarkerMesh(forward, back, [.. image.Select(_ => true)], leaveOneOutRms, [.. image], [.. page]);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>A thin-plate spline from one plane to another, in normalised coordinates.</summary>
    private sealed class Spline
    {
        private readonly PointD[] centres;
        private readonly double[] wx;
        private readonly double[] wy;
        private readonly double[] ax;
        private readonly double[] ay;
        private readonly double cx;
        private readonly double cy;
        private readonly double s;
        private readonly double ox;
        private readonly double oy;
        private readonly double os;

        private Spline(PointD[] centres, double[] wx, double[] wy, double[] ax, double[] ay, double cx, double cy, double s, double ox, double oy, double os)
        {
            (this.centres, this.wx, this.wy, this.ax, this.ay) = (centres, wx, wy, ax, ay);
            (this.cx, this.cy, this.s, this.ox, this.oy, this.os) = (cx, cy, s, ox, oy, os);
        }

        private static double U(double r2) => r2 <= 1e-18 ? 0 : r2 * Math.Log(r2) / 2;

        public static Spline Fit(PointD[] from, PointD[] to, double lambda)
        {
            int n = from.Length;
            double cx = from.Average(p => p.X), cy = from.Average(p => p.Y);
            double s = Math.Max(1e-9, from.Max(p => Math.Max(Math.Abs(p.X - cx), Math.Abs(p.Y - cy))));
            double ox = to.Average(p => p.X), oy = to.Average(p => p.Y);
            double os = Math.Max(1e-9, to.Max(p => Math.Max(Math.Abs(p.X - ox), Math.Abs(p.Y - oy))));
            var c = from.Select(p => new PointD((p.X - cx) / s, (p.Y - cy) / s)).ToArray();
            var m = new double[n + 3, n + 3];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    double dx = c[i].X - c[j].X, dy = c[i].Y - c[j].Y;
                    m[i, j] = U((dx * dx) + (dy * dy)) + (i == j ? lambda : 0);
                }

                m[i, n] = m[n, i] = 1;
                m[i, n + 1] = m[n + 1, i] = c[i].X;
                m[i, n + 2] = m[n + 2, i] = c[i].Y;
            }

            var bx = new double[n + 3];
            var by = new double[n + 3];
            for (int i = 0; i < n; i++)
            {
                bx[i] = (to[i].X - ox) / os;
                by[i] = (to[i].Y - oy) / os;
            }

            var (x, y) = Solve(m, bx, by);
            return new Spline(c, x[..n], y[..n], x[n..], y[n..], cx, cy, s, ox, oy, os);
        }

        /// <summary>
        /// The spline tabulated on a 64 by 64 grid over its centres and a margin, and read by bilinear interpolation inside it, because a
        /// warp calls it for every pixel and each exact value sums over every centre. A thin-plate spline through marker corners is smooth
        /// over the span of a grid cell, about a fifth of an inch on a Letter sheet, so the table is exact to well under a tenth of a pixel.
        /// </summary>
        private const int Cells = 64;

        private double[,]? tableX;
        private double[,]? tableY;

        private (double[,] X, double[,] Y) Table()
        {
            if (tableX is not null && tableY is not null)
            {
                return (tableX, tableY);
            }

            var x = new double[Cells + 1, Cells + 1];
            var y = new double[Cells + 1, Cells + 1];
            for (int i = 0; i <= Cells; i++)
            {
                for (int j = 0; j <= Cells; j++)
                {
                    var q = Exact(-Margin + (2 * Margin * i / Cells), -Margin + (2 * Margin * j / Cells));
                    x[i, j] = q.X;
                    y[i, j] = q.Y;
                }
            }

            (tableX, tableY) = (x, y);
            return (x, y);
        }

        /// <summary>The table's half width in normalised input: the centres span at most 1, and a margin beyond them is covered too.</summary>
        private const double Margin = 1.25;

        public PointD At(PointD p)
        {
            double u = (p.X - cx) / s, v = (p.Y - cy) / s;
            if (Math.Abs(u) >= Margin || Math.Abs(v) >= Margin)
            {
                return Exact(u, v);
            }

            var (tx, ty) = Table();
            double fu = (u + Margin) / (2 * Margin) * Cells, fv = (v + Margin) / (2 * Margin) * Cells;
            int i = Math.Min(Cells - 1, (int)fu), j = Math.Min(Cells - 1, (int)fv);
            double a = fu - i, b = fv - j;
            double X(double[,] t) => (t[i, j] * (1 - a) * (1 - b)) + (t[i + 1, j] * a * (1 - b)) + (t[i, j + 1] * (1 - a) * b) + (t[i + 1, j + 1] * a * b);
            return new PointD(X(tx), X(ty));
        }

        private PointD Exact(double u, double v)
        {
            double x = ax[0] + (ax[1] * u) + (ax[2] * v), y = ay[0] + (ay[1] * u) + (ay[2] * v);
            for (int i = 0; i < centres.Length; i++)
            {
                double dx = u - centres[i].X, dy = v - centres[i].Y, k = U((dx * dx) + (dy * dy));
                x += wx[i] * k;
                y += wy[i] * k;
            }

            return new PointD(ox + (os * x), oy + (os * y));
        }

        /// <summary>The derivatives of the output by the input, in their own units.</summary>
        public (double XX, double XY, double YX, double YY) Jacobian(PointD p)
        {
            double u = (p.X - cx) / s, v = (p.Y - cy) / s;
            double xu = ax[1], xv = ax[2], yu = ay[1], yv = ay[2];
            for (int i = 0; i < centres.Length; i++)
            {
                double dx = u - centres[i].X, dy = v - centres[i].Y, r2 = (dx * dx) + (dy * dy);
                if (r2 <= 1e-18)
                {
                    continue;
                }

                double g = Math.Log(r2) + 1;
                xu += wx[i] * dx * g;
                xv += wx[i] * dy * g;
                yu += wy[i] * dx * g;
                yv += wy[i] * dy * g;
            }

            double k = os / s;
            return (xu * k, xv * k, yu * k, yv * k);
        }

        /// <summary>Two right-hand sides of one system, by Gaussian elimination with partial pivoting.</summary>
        private static (double[] X, double[] Y) Solve(double[,] a, double[] b1, double[] b2)
        {
            int n = b1.Length;
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < n; r++)
                {
                    if (Math.Abs(a[r, col]) > Math.Abs(a[pivot, col]))
                    {
                        pivot = r;
                    }
                }

                if (Math.Abs(a[pivot, col]) < 1e-14)
                {
                    throw new InvalidOperationException("the markers do not span the page, so no mesh can be fitted through them");
                }

                if (pivot != col)
                {
                    for (int k = 0; k < n; k++)
                    {
                        (a[col, k], a[pivot, k]) = (a[pivot, k], a[col, k]);
                    }

                    (b1[col], b1[pivot]) = (b1[pivot], b1[col]);
                    (b2[col], b2[pivot]) = (b2[pivot], b2[col]);
                }

                for (int r = col + 1; r < n; r++)
                {
                    double f = a[r, col] / a[col, col];
                    if (f == 0)
                    {
                        continue;
                    }

                    for (int k = col; k < n; k++)
                    {
                        a[r, k] -= f * a[col, k];
                    }

                    b1[r] -= f * b1[col];
                    b2[r] -= f * b2[col];
                }
            }

            var x1 = new double[n];
            var x2 = new double[n];
            for (int r = n - 1; r >= 0; r--)
            {
                double s1 = b1[r], s2 = b2[r];
                for (int k = r + 1; k < n; k++)
                {
                    s1 -= a[r, k] * x1[k];
                    s2 -= a[r, k] * x2[k];
                }

                x1[r] = s1 / a[r, r];
                x2[r] = s2 / a[r, r];
            }

            return (x1, x2);
        }
    }
}
