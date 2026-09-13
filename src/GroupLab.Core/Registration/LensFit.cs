using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// Fits <see cref="RadialHomographyMapping"/> to marker corners by Levenberg-Marquardt, minimising the page-space
/// residual. DESIGN.md section 11: thirty-odd known points overdetermine the model, so the distortion is fitted from the
/// image itself and no camera metadata is used. The distortion centre is the image centre; a single view of a plane
/// cannot separate it from the homography's translation.
/// </summary>
public static class LensFit
{
    private const int MaximumIterations = 200;

    public static RadialHomographyMapping Fit(IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, Homography imageToPage, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(imageToPage);
        if (image.Count != page.Count || image.Count < 6)
        {
            throw new InvalidOperationException($"A lens fit needs at least 6 matched points, got {image.Count}.");
        }

        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0, s = Math.Max(width, height) / 2.0;
        var h = Homography.Compose(new Homography([s, 0, cx, 0, s, cy, 0, 0, 1]), imageToPage);
        double h22 = h[2, 2];
        double[] p = [h[0, 0] / h22, h[0, 1] / h22, h[0, 2] / h22, h[1, 0] / h22, h[1, 1] / h22, h[1, 2] / h22, h[2, 0] / h22, h[2, 1] / h22, 0, 0];
        var u = image.Select(q => new PointD((q.X - cx) / s, (q.Y - cy) / s)).ToArray();

        int n = 2 * u.Length, k = p.Length;
        var residual = new double[n];
        var trial = new double[n];
        var jacobian = new double[n, k];
        double cost = Residuals(p, u, page, residual);
        double lambda = 1e-3;
        for (int iteration = 0; iteration < MaximumIterations; iteration++)
        {
            for (int j = 0; j < k; j++)
            {
                double step = 1e-7 * Math.Max(Math.Abs(p[j]), 1e-3);
                var shifted = (double[])p.Clone();
                shifted[j] += step;
                Residuals(shifted, u, page, trial);
                for (int i = 0; i < n; i++)
                {
                    jacobian[i, j] = (trial[i] - residual[i]) / step;
                }
            }

            var normal = new double[k, k];
            var gradient = new double[k];
            for (int i = 0; i < n; i++)
            {
                for (int a = 0; a < k; a++)
                {
                    gradient[a] -= jacobian[i, a] * residual[i];
                    for (int b = a; b < k; b++)
                    {
                        normal[a, b] += jacobian[i, a] * jacobian[i, b];
                    }
                }
            }

            for (int a = 0; a < k; a++)
            {
                for (int b = 0; b < a; b++)
                {
                    normal[a, b] = normal[b, a];
                }
            }

            bool improved = false;
            double gain = 0;
            while (lambda < 1e12)
            {
                var damped = (double[,])normal.Clone();
                for (int a = 0; a < k; a++)
                {
                    damped[a, a] += lambda * Math.Max(normal[a, a], 1e-12);
                }

                if (LinearSolve.Solve(damped, gradient) is not { } delta)
                {
                    lambda *= 10;
                    continue;
                }

                var candidate = p.Zip(delta, (x, d) => x + d).ToArray();
                double candidateCost = Residuals(candidate, u, page, trial);
                if (candidateCost < cost)
                {
                    gain = (cost - candidateCost) / cost;
                    p = candidate;
                    cost = Residuals(p, u, page, residual);
                    lambda = Math.Max(lambda / 10, 1e-12);
                    improved = true;
                    break;
                }

                lambda *= 10;
            }

            if (!improved || gain < 1e-12)
            {
                break;
            }
        }

        return new RadialHomographyMapping(cx, cy, s, p[8], p[9], new Homography([p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], 1]));
    }

    private static double Residuals(double[] p, PointD[] u, IReadOnlyList<PointD> page, double[] r)
    {
        double cost = 0;
        for (int i = 0; i < u.Length; i++)
        {
            double r2 = (u[i].X * u[i].X) + (u[i].Y * u[i].Y);
            double f = 1 + (p[8] * r2) + (p[9] * r2 * r2);
            double x = u[i].X * f, y = u[i].Y * f;
            double w = (p[6] * x) + (p[7] * y) + 1;
            r[2 * i] = (((p[0] * x) + (p[1] * y) + p[2]) / w) - page[i].X;
            r[(2 * i) + 1] = (((p[3] * x) + (p[4] * y) + p[5]) / w) - page[i].Y;
            cost += (r[2 * i] * r[2 * i]) + (r[(2 * i) + 1] * r[(2 * i) + 1]);
        }

        return cost;
    }
}

/// <summary>Dense linear solves for the small normal equations of the fits in this namespace and in measurement.</summary>
public static class LinearSolve
{
    /// <summary>Solves <c>a x = b</c> by Gaussian elimination with partial pivoting, or returns null when <paramref name="a"/> is singular.</summary>
    public static double[]? Solve(double[,] a, double[] b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        int n = b.Length;
        var m = (double[,])a.Clone();
        var x = (double[])b.Clone();
        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
            {
                if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
                {
                    pivot = row;
                }
            }

            if (Math.Abs(m[pivot, col]) < 1e-300)
            {
                return null;
            }

            if (pivot != col)
            {
                for (int j = 0; j < n; j++)
                {
                    (m[col, j], m[pivot, j]) = (m[pivot, j], m[col, j]);
                }

                (x[col], x[pivot]) = (x[pivot], x[col]);
            }

            for (int row = col + 1; row < n; row++)
            {
                double factor = m[row, col] / m[col, col];
                for (int j = col; j < n; j++)
                {
                    m[row, j] -= factor * m[col, j];
                }

                x[row] -= factor * x[col];
            }
        }

        for (int row = n - 1; row >= 0; row--)
        {
            double sum = x[row];
            for (int j = row + 1; j < n; j++)
            {
                sum -= m[row, j] * x[j];
            }

            x[row] = sum / m[row, row];
        }

        return x;
    }
}
