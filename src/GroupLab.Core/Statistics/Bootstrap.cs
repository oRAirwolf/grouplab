using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>
/// A bootstrap interval as docs/STATISTICS.md section 6 requires it to be reported: the estimate, the interval, which method made
/// it, the resample count and seed that reproduce it exactly, and whether there were enough shots for it to be trusted.
/// </summary>
public sealed record BootstrapInterval(double Estimate, double Lower, double Upper, string Method, int Resamples, ulong Seed, bool Reliable);

/// <summary>
/// The bootstrap of docs/STATISTICS.md section 6, for the statistics with no closed form: the minimum enclosing circle, the
/// bounding box's dimensions and the elliptical CEP. BCa by default, which corrects for the bias and skew these statistics have;
/// the percentile interval when the acceleration cannot be computed; 9999 resamples, because 1000 leave Monte Carlo error visible
/// in the reported digits; shots resampled, never residuals; and fewer than 10 shots flagged as unreliable rather than reported
/// silently.
/// <para>
/// Its coverage is below nominal at the counts people fire: 79.5, 89.1 and 92.7 percent at 10, 25 and 50 shots for a nominal 95
/// (docs/PHASE1-RESULTS.md M3.1). So a closed form is preferred wherever one exists, and a bootstrap interval is never labelled with a
/// bare nominal level: it carries its measured coverage or is called approximate and optimistic at small n (NOTES-FROM-PLANNING.md
/// entry 23 section 2). No screen shows one yet.
/// </para>
/// </summary>
public static class Bootstrap
{
    public const int DefaultResamples = 9999;

    /// <summary>Section 6: below this many shots the bootstrap interval is reported as unreliable.</summary>
    public const int MinimumReliableShots = 10;

    public const ulong DefaultSeed = 20260914;

    public static BootstrapInterval Interval(IReadOnlyList<PointD> shots, Func<IReadOnlyList<PointD>, double> statistic, double level = 0.95, int resamples = DefaultResamples, ulong seed = DefaultSeed)
    {
        ArgumentNullException.ThrowIfNull(shots);
        ArgumentNullException.ThrowIfNull(statistic);
        int n = shots.Count;
        double estimate = statistic(shots);
        var random = new StatisticsRandom(seed);
        var resample = new PointD[n];
        var replicates = new double[resamples];
        for (int b = 0; b < resamples; b++)
        {
            for (int i = 0; i < n; i++)
            {
                resample[i] = shots[random.NextInt(n)];
            }

            replicates[b] = statistic(resample);
        }

        Array.Sort(replicates);
        double alpha = 1 - level;
        bool reliable = n >= MinimumReliableShots;

        // Bias correction: the share of replicates below the estimate, ties counted half.
        int below = 0, equal = 0;
        foreach (double r in replicates)
        {
            below += r < estimate ? 1 : 0;
            equal += r == estimate ? 1 : 0;
        }

        double share = (below + (0.5 * equal)) / resamples;

        // Acceleration from the jackknife: a = sum d^3 / (6 (sum d^2)^1.5), d the mean leave-one-out value less each one.
        var leaveOut = new double[n];
        var without = new PointD[n - 1];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0, k = 0; j < n; j++)
            {
                if (j != i)
                {
                    without[k++] = shots[j];
                }
            }

            leaveOut[i] = statistic(without);
        }

        double mean = leaveOut.Average(), squares = 0, cubes = 0;
        foreach (double v in leaveOut)
        {
            double d = mean - v;
            squares += d * d;
            cubes += d * d * d;
        }

        double acceleration = cubes / (6 * Math.Pow(squares, 1.5));
        if (!(share > 0 && share < 1) || !double.IsFinite(acceleration))
        {
            return new BootstrapInterval(estimate, Quantile(replicates, alpha / 2), Quantile(replicates, 1 - (alpha / 2)), "percentile, because the BCa correction could not be computed", resamples, seed, reliable);
        }

        double z0 = Distributions.NormalQuantile(share);
        double Adjusted(double p)
        {
            double z = Distributions.NormalQuantile(p);
            return Distributions.NormalCdf(z0 + ((z0 + z) / (1 - (acceleration * (z0 + z)))));
        }

        return new BootstrapInterval(estimate, Quantile(replicates, Adjusted(alpha / 2)), Quantile(replicates, Adjusted(1 - (alpha / 2))), "BCa", resamples, seed, reliable);
    }

    /// <summary>R's type 7 quantile of sorted values.</summary>
    private static double Quantile(double[] sorted, double p)
    {
        double position = (sorted.Length - 1) * Math.Clamp(p, 0, 1);
        int low = (int)Math.Floor(position);
        int high = Math.Min(low + 1, sorted.Length - 1);
        return sorted[low] + ((position - low) * (sorted[high] - sorted[low]));
    }
}
