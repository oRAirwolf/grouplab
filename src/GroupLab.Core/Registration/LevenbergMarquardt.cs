namespace GroupLab.Core.Registration;

/// <summary>
/// Levenberg-Marquardt for the small nonlinear least-squares fits of PHASE1-BRIEF.md M1, with a forward-difference
/// Jacobian, the same damping as <see cref="LensFit"/>, and a damping floor so a parameter the residual does not yet depend
/// on, a ruling angle on a flat sheet, is left where it is rather than stalling the step.
/// <para>
/// A model can be undefined beyond a boundary, where a residual function returns a penalty: a point behind the camera, or
/// a general developable surface whose rulings cross inside the page (<see cref="FoldedSheet"/>). A fit that ends on that
/// boundary puts the forward-difference step across it, and the penalty's column, of order 1e12, then set the damping
/// floor of every other parameter, so a joint fit with one such frame took no step at all (PHASE1-RESULTS.md M1.10). So a
/// forward step whose cost is not finite or jumps by more than <see cref="Undefined"/> takes the backward difference, a
/// parameter undefined both ways is left out of that iteration, and a parameter whose step would carry it across the
/// boundary is held for that step. An ordinary step never jumps by that much, so a fit that never meets the boundary is
/// unchanged. It does not free a fit pressed against the boundary along a combination of parameters, which is what the
/// general surface's joint fit on the Phase 0 frames does; M1.10 reports that.
/// </para>
/// </summary>
public static class LevenbergMarquardt
{
    /// <summary>The cost jump, beyond a thousand times the current cost, that marks a difference step as undefined.</summary>
    public const double Undefined = 1e9;

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
            var wall = new bool[k];
            for (int j = 0; j < k; j++)
            {
                var shifted = (double[])p.Clone();
                double step = steps[j];
                shifted[j] += step;
                if (!Defined(residuals(shifted, trial), cost))
                {
                    wall[j] = true;
                    step = -steps[j];
                    shifted[j] = p[j] + step;
                    if (!Defined(residuals(shifted, trial), cost))
                    {
                        for (int i = 0; i < n; i++)
                        {
                            jacobian[i, j] = 0;
                        }

                        continue;
                    }
                }

                for (int i = 0; i < n; i++)
                {
                    jacobian[i, j] = (trial[i] - r[i]) / step;
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
                // A parameter on an undefined boundary whose step would carry it across is held for this step, and the
                // system is solved again without it, so the rest of the fit can still move.
                var held = new bool[k];
                double[]? delta;
                while (true)
                {
                    var damped = (double[,])normal.Clone();
                    var rhs = (double[])gradient.Clone();
                    for (int a = 0; a < k; a++)
                    {
                        damped[a, a] += lambda * Math.Max(normal[a, a], 1e-9 * largest);
                        if (held[a])
                        {
                            for (int b = 0; b < k; b++)
                            {
                                damped[a, b] = damped[b, a] = 0;
                            }

                            damped[a, a] = 1;
                            rhs[a] = 0;
                        }
                    }

                    delta = LinearSolve.Solve(damped, rhs);
                    if (delta is null || delta.Any(double.IsNaN))
                    {
                        break;
                    }

                    bool more = false;
                    for (int a = 0; a < k; a++)
                    {
                        if (wall[a] && !held[a] && delta[a] > 0)
                        {
                            held[a] = more = true;
                        }
                    }

                    if (!more)
                    {
                        break;
                    }
                }

                if (delta is null || delta.Any(double.IsNaN))
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

    private static bool Defined(double shiftedCost, double cost) => shiftedCost < Undefined + (1e3 * cost);
}
