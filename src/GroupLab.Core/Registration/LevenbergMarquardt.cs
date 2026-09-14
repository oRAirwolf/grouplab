namespace GroupLab.Core.Registration;

/// <summary>
/// Levenberg-Marquardt for the small nonlinear least-squares fits of PHASE1-BRIEF.md M1, with a forward-difference
/// Jacobian, the same damping as <see cref="LensFit"/>, and a damping floor so a parameter the residual does not yet depend
/// on, a ruling angle on a flat sheet, is left where it is rather than stalling the step.
/// </summary>
public static class LevenbergMarquardt
{
    public sealed record Result(double[] Parameters, double Cost, int Iterations);

    /// <param name="residuals">Fills the residual vector for a parameter vector and returns the sum of squares.</param>
    /// <param name="start">Starting parameters; not modified.</param>
    /// <param name="residualCount">Length of the residual vector.</param>
    /// <param name="steps">Forward-difference step for each parameter, in that parameter's units.</param>
    public static Result Minimise(Func<double[], double[], double> residuals, double[] start, int residualCount, IReadOnlyList<double> steps, int maximumIterations = 300)
    {
        ArgumentNullException.ThrowIfNull(residuals);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(steps);
        int k = start.Length, n = residualCount;
        var p = (double[])start.Clone();
        var r = new double[n];
        var trial = new double[n];
        var jacobian = new double[n, k];
        double cost = residuals(p, r);
        double lambda = 1e-3;
        int iteration = 0;
        for (; iteration < maximumIterations; iteration++)
        {
            for (int j = 0; j < k; j++)
            {
                var shifted = (double[])p.Clone();
                shifted[j] += steps[j];
                residuals(shifted, trial);
                for (int i = 0; i < n; i++)
                {
                    jacobian[i, j] = (trial[i] - r[i]) / steps[j];
                }
            }

            var normal = new double[k, k];
            var gradient = new double[k];
            for (int i = 0; i < n; i++)
            {
                for (int a = 0; a < k; a++)
                {
                    double ja = jacobian[i, a];
                    if (ja == 0)
                    {
                        continue;
                    }

                    gradient[a] -= ja * r[i];
                    for (int b = a; b < k; b++)
                    {
                        normal[a, b] += ja * jacobian[i, b];
                    }
                }
            }

            double largest = 0;
            for (int a = 0; a < k; a++)
            {
                largest = Math.Max(largest, normal[a, a]);
                for (int b = 0; b < a; b++)
                {
                    normal[a, b] = normal[b, a];
                }
            }

            bool improved = false;
            double gain = 0;
            while (lambda < 1e14)
            {
                var damped = (double[,])normal.Clone();
                for (int a = 0; a < k; a++)
                {
                    damped[a, a] += lambda * Math.Max(normal[a, a], 1e-9 * largest);
                }

                if (LinearSolve.Solve(damped, gradient) is not { } delta || delta.Any(double.IsNaN))
                {
                    lambda *= 10;
                    continue;
                }

                var candidate = p.Zip(delta, (x, d) => x + d).ToArray();
                double candidateCost = residuals(candidate, trial);
                if (candidateCost < cost)
                {
                    gain = (cost - candidateCost) / Math.Max(cost, 1e-300);
                    p = candidate;
                    cost = residuals(p, r);
                    lambda = Math.Max(lambda / 10, 1e-12);
                    improved = true;
                    break;
                }

                lambda *= 10;
            }

            if (!improved || gain < 1e-13)
            {
                break;
            }
        }

        return new Result(p, cost, iteration);
    }
}
