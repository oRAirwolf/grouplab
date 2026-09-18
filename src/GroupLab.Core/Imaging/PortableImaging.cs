namespace GroupLab.Core.Imaging;

/// <summary>
/// The floating-point steps of registration done in managed code, so that they give the same answer on every platform,
/// NOTES-FROM-PLANNING.md entry 101 and the Phase 0 gate record of entries 32, 48 and 49.
/// <para>
/// <b>Why these four.</b> The gate record reproduced on Linux and not on macOS, and the rerun from Windows' corners (PHASE1-RESULTS.md,
/// "Entry 49 section 2") placed every difference in three native steps: the synthetic scan's perspective warp, which made a different image
/// before anything was detected; sub-pixel corner refinement, which moved corners on byte-identical images; and the homography's final
/// refinement, which moved the last printed digit on identical corners and inliers. Turning OpenCV's optimised paths off changed nothing on
/// macOS and broke a line on Linux, so the differences are in the native arithmetic itself, not in a switchable path. With those three
/// managed, the only lines still differing were the contour refinement's, which fits its lines in single precision natively, so it is the
/// fourth step here. Native code is kept for
/// what it does in integers, finding and decoding marker candidates and choosing the RANSAC inliers, which the record shows is already
/// identical everywhere.
/// </para>
/// <para>
/// <b>What makes these portable.</b> Scalar arithmetic in a fixed order: addition, subtraction, multiplication, division and square root,
/// which IEEE 754 rounds identically on x64 and arm64. No call into a platform's maths library, whose last bit may differ: the one
/// exponential the corner refinement needs is a series, and no vectorised sum, whose order depends on the vector width.
/// </para>
/// </summary>
public static class PortableImaging
{
    /// <summary>
    /// Resamples bilinearly, as OpenCV's <c>warpPerspective</c> with linear interpolation and a constant border of paper: each destination
    /// pixel is read from the source through the inverse of <paramref name="transform"/>, which takes source pixels to destination pixels.
    /// </summary>
    public static GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(transform);
        var inverse = transform.Inverse();
        double m00 = inverse[0, 0], m01 = inverse[0, 1], m02 = inverse[0, 2];
        double m10 = inverse[1, 0], m11 = inverse[1, 1], m12 = inverse[1, 2];
        double m20 = inverse[2, 0], m21 = inverse[2, 1], m22 = inverse[2, 2];
        var pixels = new byte[width * height];
        int sw = image.Width, sh = image.Height;
        var src = image.Pixels;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double w = (m20 * x) + (m21 * y) + m22;
                double sx = ((m00 * x) + (m01 * y) + m02) / w;
                double sy = ((m10 * x) + (m11 * y) + m12) / w;
                int x0 = (int)Math.Floor(sx), y0 = (int)Math.Floor(sy);
                double fx = sx - x0, fy = sy - y0;
                double v00 = At(x0, y0), v01 = At(x0 + 1, y0), v10 = At(x0, y0 + 1), v11 = At(x0 + 1, y0 + 1);
                double top = v00 + ((v01 - v00) * fx);
                double bottom = v10 + ((v11 - v10) * fx);
                double v = top + ((bottom - top) * fy);
                pixels[(y * width) + x] = (byte)Math.Clamp((int)Math.Floor(v + 0.5), 0, 255);
            }
        }

        return new GrayImage(width, height, pixels);

        double At(int px, int py) => px < 0 || py < 0 || px >= sw || py >= sh ? 255 : src[(py * sw) + px];
    }

    /// <summary>
    /// OpenCV's <c>cornerSubPix</c>, ported to give the same answer on every platform: each corner moves to where the image gradient in a
    /// window around it is most nearly orthogonal to the offset from it, iterated until it moves less than <paramref name="epsilon"/> pixels or
    /// <paramref name="maxIterations"/> are spent, and put back where it started if it wanders more than the window. The arithmetic follows
    /// OpenCV's: single-precision window and weights, double-precision sums, and bilinear sampling with the border replicated.
    /// </summary>
    public static PointD[] CornerSubPix(GrayImage image, IReadOnlyList<PointD> corners, int window, int maxIterations, double epsilon)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(corners);
        int winW = (window * 2) + 1, winH = winW;
        int iterations = Math.Min(Math.Max(maxIterations, 1), 100);
        double eps = Math.Max(epsilon, 0) * Math.Max(epsilon, 0);
        var mask = new float[winW * winH];
        for (int i = 0; i < winH; i++)
        {
            float y = (float)(i - window) / window;
            float vy = (float)Exp(-(double)(y * y));
            for (int j = 0; j < winW; j++)
            {
                float x = (float)(j - window) / window;
                mask[(i * winW) + j] = (float)(vy * Exp(-(double)(x * x)));
            }
        }

        var patch = new float[(winW + 2) * (winH + 2)];
        var result = new PointD[corners.Count];
        for (int p = 0; p < corners.Count; p++)
        {
            float tx = (float)corners[p].X, ty = (float)corners[p].Y;
            float cx = tx, cy = ty;
            int iter = 0;
            double err;
            do
            {
                RectSubPix(image, winW + 2, winH + 2, cx, cy, patch);
                double a = 0, b = 0, c = 0, bb1 = 0, bb2 = 0;
                int k = 0;
                for (int i = 0; i < winH; i++)
                {
                    double py = i - window;
                    int row = ((i + 1) * (winW + 2)) + 1;
                    for (int j = 0; j < winW; j++, k++)
                    {
                        double m = mask[k];
                        double tgx = patch[row + j + 1] - patch[row + j - 1];
                        double tgy = patch[row + j + winW + 2] - patch[row + j - winW - 2];
                        double gxx = tgx * tgx * m, gxy = tgx * tgy * m, gyy = tgy * tgy * m;
                        double px = j - window;
                        a += gxx;
                        b += gxy;
                        c += gyy;
                        bb1 += (gxx * px) + (gxy * py);
                        bb2 += (gxy * px) + (gyy * py);
                    }
                }

                double det = (a * c) - (b * b);
                // C's DBL_EPSILON, as OpenCV compares against; .NET's double.Epsilon is the smallest subnormal and would never stop it.
                const double dblEpsilon = 2.220446049250313e-16;
                if (Math.Abs(det) <= dblEpsilon * dblEpsilon)
                {
                    break;
                }

                double scale = 1.0 / det;
                float nx = (float)(cx + (c * scale * bb1) - (b * scale * bb2));
                float ny = (float)(cy - (b * scale * bb1) + (a * scale * bb2));
                err = ((double)(nx - cx) * (nx - cx)) + ((double)(ny - cy) * (ny - cy));
                cx = nx;
                cy = ny;
                if (cx < 0 || cx >= image.Width || cy < 0 || cy >= image.Height)
                {
                    break;
                }
            }
            while (++iter < iterations && err > eps);

            if (Math.Abs(cx - tx) > window || Math.Abs(cy - ty) > window)
            {
                (cx, cy) = (tx, ty);
            }

            result[p] = new PointD(cx, cy);
        }

        return result;
    }

    /// <summary>
    /// The homography over the given correspondences that minimises the squared distance, in destination units, between each destination point
    /// and its mapped source point, as OpenCV's <c>findHomography</c> refines its RANSAC answer: a normalised linear estimate, then
    /// Levenberg-Marquardt over the eight free entries. Only the correspondences marked <paramref name="use"/> take part.
    /// </summary>
    public static Homography? RefineHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, IReadOnlyList<bool> use)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(use);
        var src = new List<PointD>();
        var dst = new List<PointD>();
        for (int i = 0; i < source.Count; i++)
        {
            if (use[i])
            {
                src.Add(source[i]);
                dst.Add(destination[i]);
            }
        }

        if (src.Count < 4 || Linear(src, dst) is not { } start)
        {
            return null;
        }

        var h = new double[8];
        for (int i = 0; i < 8; i++)
        {
            h[i] = start[i / 3, i % 3] / start[2, 2];
        }

        double lambda = 1e-3;
        double cost = Cost(h, src, dst);
        bool converged = false;
        for (int iteration = 0; iteration < 100 && !converged; iteration++)
        {
            var jtj = new double[8, 8];
            var jtr = new double[8];
            for (int i = 0; i < src.Count; i++)
            {
                double x = src[i].X, y = src[i].Y;
                double w = (h[6] * x) + (h[7] * y) + 1;
                double u = ((h[0] * x) + (h[1] * y) + h[2]) / w;
                double v = ((h[3] * x) + (h[4] * y) + h[5]) / w;
                double ru = u - dst[i].X, rv = v - dst[i].Y;
                double[] ju = [x / w, y / w, 1 / w, 0, 0, 0, -u * x / w, -u * y / w];
                double[] jv = [0, 0, 0, x / w, y / w, 1 / w, -v * x / w, -v * y / w];
                for (int a = 0; a < 8; a++)
                {
                    jtr[a] += (ju[a] * ru) + (jv[a] * rv);
                    for (int b = 0; b < 8; b++)
                    {
                        jtj[a, b] += (ju[a] * ju[b]) + (jv[a] * jv[b]);
                    }
                }
            }

            bool improved = false;
            while (lambda < 1e12)
            {
                var damped = (double[,])jtj.Clone();
                for (int a = 0; a < 8; a++)
                {
                    damped[a, a] += lambda * jtj[a, a];
                }

                var negative = new double[8];
                for (int a = 0; a < 8; a++)
                {
                    negative[a] = -jtr[a];
                }

                if (GroupLab.Core.Registration.LinearSolve.Solve(damped, negative) is not { } step)
                {
                    lambda *= 10;
                    continue;
                }

                var trial = new double[8];
                for (int a = 0; a < 8; a++)
                {
                    trial[a] = h[a] + step[a];
                }

                double trialCost = Cost(trial, src, dst);
                if (trialCost < cost)
                {
                    double change = cost - trialCost;
                    h = trial;
                    cost = trialCost;
                    lambda = Math.Max(lambda / 10, 1e-12);
                    improved = true;
                    converged = change <= 1e-15 * (1 + cost);

                    break;
                }

                lambda *= 10;
            }

            if (!improved)
            {
                break;
            }
        }

        return new Homography([h[0], h[1], h[2], h[3], h[4], h[5], h[6], h[7], 1]);
    }

    private static double Cost(double[] h, List<PointD> src, List<PointD> dst)
    {
        double sum = 0;
        for (int i = 0; i < src.Count; i++)
        {
            double x = src[i].X, y = src[i].Y;
            double w = (h[6] * x) + (h[7] * y) + 1;
            double ru = (((h[0] * x) + (h[1] * y) + h[2]) / w) - dst[i].X;
            double rv = (((h[3] * x) + (h[4] * y) + h[5]) / w) - dst[i].Y;
            sum += (ru * ru) + (rv * rv);
        }

        return sum;
    }

    /// <summary>The linear estimate with Hartley's normalisation, in a fixed order and with no call outside plain arithmetic and square root.</summary>
    private static Homography? Linear(List<PointD> src, List<PointD> dst)
    {
        var (ts, s) = Normalise(src);
        var (td, d) = Normalise(dst);
        var normal = new double[8, 8];
        var rhs = new double[8];
        for (int i = 0; i < s.Length; i++)
        {
            double x = s[i].X, y = s[i].Y, u = d[i].X, v = d[i].Y;
            double[] r0 = [x, y, 1, 0, 0, 0, -u * x, -u * y];
            double[] r1 = [0, 0, 0, x, y, 1, -v * x, -v * y];
            for (int a = 0; a < 8; a++)
            {
                rhs[a] += (r0[a] * u) + (r1[a] * v);
                for (int b = 0; b < 8; b++)
                {
                    normal[a, b] += (r0[a] * r0[b]) + (r1[a] * r1[b]);
                }
            }
        }

        if (GroupLab.Core.Registration.LinearSolve.Solve(normal, rhs) is not { } h)
        {
            return null;
        }

        var hn = new Homography([h[0], h[1], h[2], h[3], h[4], h[5], h[6], h[7], 1]);
        return Homography.Compose(Homography.Compose(ts, hn), td.Inverse());
    }

    private static (Homography Transform, PointD[] Points) Normalise(List<PointD> points)
    {
        double mx = 0, my = 0;
        foreach (var p in points)
        {
            mx += p.X;
            my += p.Y;
        }

        mx /= points.Count;
        my /= points.Count;
        double mean = 0;
        foreach (var p in points)
        {
            mean += Math.Sqrt(((p.X - mx) * (p.X - mx)) + ((p.Y - my) * (p.Y - my)));
        }

        mean /= points.Count;
        double k = mean > 0 ? Math.Sqrt(2) / mean : 1;
        var transform = new Homography([k, 0, -k * mx, 0, k, -k * my, 0, 0, 1]);
        var moved = new PointD[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            moved[i] = transform.Apply(points[i]);
        }

        return (transform, moved);
    }

    /// <summary>OpenCV's <c>getRectSubPix</c> for an 8-bit image into single precision: bilinear, with the border replicated.</summary>
    private static void RectSubPix(GrayImage image, int w, int h, float centreX, float centreY, float[] patch)
    {
        float left = centreX - ((w - 1) * 0.5f), topY = centreY - ((h - 1) * 0.5f);
        int ix = (int)Math.Floor(left), iy = (int)Math.Floor(topY);
        float fa = left - ix, fb = topY - iy;
        float a11 = (1 - fa) * (1 - fb), a12 = fa * (1 - fb), a21 = (1 - fa) * fb, a22 = fa * fb;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                patch[(i * w) + j] = (a11 * Px(ix + j, iy + i)) + (a12 * Px(ix + j + 1, iy + i)) + (a21 * Px(ix + j, iy + i + 1)) + (a22 * Px(ix + j + 1, iy + i + 1));
            }
        }

        float Px(int x, int y) => image[Math.Clamp(x, 0, image.Width - 1), Math.Clamp(y, 0, image.Height - 1)];
    }

    /// <summary>
    /// OpenCV ArUco's contour refinement (<c>_refineCandidateLines</c>), in managed code: the marker's contour is split at its four corners
    /// into sides, a straight line is fitted to each side by least squares, and each corner moves to where its two sides' lines cross.
    /// The corners must be points on the contour, as the unrefined corners are. OpenCV fits and crosses in single precision through its
    /// matrix solver, whose arithmetic differs by platform; here both are done in double precision, in a fixed order.
    /// <para>
    /// Checked against the native step on sheets 1 to 3 of GL-CF25-LTR at 600 and 300 DPI: with OpenCV's single-precision normal equations
    /// and elimination put back in, this reproduces it to within one single-precision step on all 136 corners of each image, so the contour
    /// and the grouping are the same. Without them it differs from the native answer by up to 0.09 px on sides 5000 px from the origin,
    /// which is how far the single-precision solve is from the least-squares line.
    /// </para>
    /// </summary>
    public static PointD[] RefineOnContour(IReadOnlyList<(int X, int Y)> contour, IReadOnlyList<PointD> corners)
    {
        ArgumentNullException.ThrowIfNull(contour);
        ArgumentNullException.ThrowIfNull(corners);
        if (corners.Count != 4)
        {
            throw new ArgumentException("A marker has four corners.", nameof(corners));
        }

        // Groups 0 to 3 are the points from each corner to the next; group 4 holds any points before the first corner, and joins the
        // last corner's group. OpenCV initialises only the first corner's index to -1 and the rest to 0, which its direction test reads.
        var groups = new List<(int X, int Y)>[5];
        for (int g = 0; g < 5; g++)
        {
            groups[g] = [];
        }

        int[] cornerIndex = [-1, 0, 0, 0];
        bool[] found = new bool[4];
        int group = 4;
        for (int i = 0; i < contour.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (corners[j].X == contour[i].X && corners[j].Y == contour[i].Y)
                {
                    cornerIndex[j] = i;
                    found[j] = true;
                    group = j;
                }
            }

            groups[group].Add(contour[i]);
        }

        if (found.Contains(false))
        {
            throw new ArgumentException("Every corner must be a point on the contour.", nameof(corners));
        }

        groups[group].AddRange(groups[4]);

        int inc = 1;
        inc = cornerIndex[0] > cornerIndex[1] && cornerIndex[3] > cornerIndex[0] ? -1 : inc;
        inc = cornerIndex[2] > cornerIndex[3] && cornerIndex[1] > cornerIndex[2] ? -1 : inc;

        var lines = new (double A, double B, double C)[4];
        for (int i = 0; i < 4; i++)
        {
            lines[i] = FitLine(groups[i]);
        }

        var refined = new PointD[4];
        for (int i = 0; i < 4; i++)
        {
            refined[i] = Cross(lines[i], lines[inc < 0 ? (i + 1) % 4 : (i + 3) % 4]);
        }

        return refined;
    }

    /// <summary>
    /// <c>_interpolate2Dline</c>: y = m x + c over the points when they spread wider than tall, x = m y + c otherwise, by the normal
    /// equations, as a line a x + b y + c = 0.
    /// </summary>
    private static (double A, double B, double C) FitLine(List<(int X, int Y)> points)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("A side needs two points to fit a line.", nameof(points));
        }

        int minX = points[0].X, maxX = minX, minY = points[0].Y, maxY = minY;
        foreach (var (x, y) in points)
        {
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }

        bool wide = maxX - minX > maxY - minY;
        double suu = 0, su = 0, suv = 0, sv = 0;
        foreach (var (x, y) in points)
        {
            double u = wide ? x : y, v = wide ? y : x;
            suu += u * u;
            su += u;
            suv += u * v;
            sv += v;
        }

        double n = points.Count, det = (suu * n) - (su * su);
        double m = ((suv * n) - (su * sv)) / det, c = ((suu * sv) - (su * suv)) / det;
        return wide ? (m, -1, c) : (-1, m, c);
    }

    /// <summary><c>_getCrossPoint</c>: where two lines a x + b y + c = 0 meet.</summary>
    private static PointD Cross((double A, double B, double C) first, (double A, double B, double C) second)
    {
        double det = (first.A * second.B) - (first.B * second.A);
        return new PointD(((-first.C * second.B) + (first.B * second.C)) / det, ((-first.A * second.C) + (first.C * second.A)) / det);
    }

    /// <summary>
    /// e to the <paramref name="x"/> for x from -1 to 0, by its series, which converges to double precision in 25 terms there. Written out so
    /// that it is the same on every platform, where <see cref="Math.Exp"/> calls each platform's own maths library.
    /// </summary>
    internal static double Exp(double x)
    {
        double sum = 1, term = 1;
        for (int n = 1; n < 30; n++)
        {
            term = term * x / n;
            sum += term;
        }

        return sum;
    }
}
