using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>
/// Sample-size planning, docs/STATISTICS.md section 9: how well n shots know sigma, and how many shots per load a comparison
/// needs. Section 9 calls the second the single most valuable thing in the application.
/// </summary>
public static class SampleSize
{
    /// <summary>The approximate coefficient of variation of sigma from n shots, 1 / (2 sqrt(n - 1)), section 9.1.</summary>
    public static double SigmaCoefficientOfVariation(int n) => 1 / (2 * Math.Sqrt(n - 1.0));

    /// <summary>
    /// The confidence interval for the true sigma as multiples of the estimate, section 9.1's table: sqrt(df / chi^2(1 - alpha / 2))
    /// to sqrt(df / chi^2(alpha / 2)) on df = 2(n - 1). The c4 correction multiplies estimate and endpoints alike, so it cancels.
    /// </summary>
    public static (double Lower, double Upper) SigmaIntervalMultiples(int n, double level = 0.95)
    {
        double df = 2.0 * (n - 1), alpha = 1 - level;
        return (Math.Sqrt(df / Distributions.ChiSquareQuantile(1 - (alpha / 2), df)), Math.Sqrt(df / Distributions.ChiSquareQuantile(alpha / 2, df)));
    }

    /// <summary>
    /// The power of section 8.1's two-sided F test with <paramref name="n"/> shots per load when the true dispersion ratio is
    /// <paramref name="ratio"/>: the observed F is ratio^2 times an F on (2(n - 1), 2(n - 1)), so power is the probability that
    /// it lands beyond either critical value.
    /// </summary>
    public static double Power(double ratio, int n, double alpha = 0.05)
    {
        double df = 2.0 * (n - 1), k2 = ratio * ratio;
        double high = Distributions.FQuantile(1 - (alpha / 2), df, df), low = Distributions.FQuantile(alpha / 2, df, df);
        return Distributions.F(high / k2, df, df).Upper + Distributions.F(low / k2, df, df).Lower;
    }

    /// <summary>Section 9.2's approximation, n = 1 + (z_{alpha/2} + z_beta)^2 / (2 (ln k)^2), rounded up.</summary>
    public static int ShotsPerLoadApproximation(double ratio, double alpha = 0.05, double power = 0.8)
    {
        double z = Distributions.NormalQuantile(1 - (alpha / 2)) + Distributions.NormalQuantile(power), log = Math.Log(ratio);
        return (int)Math.Ceiling(1 + (z * z / (2 * log * log)));
    }

    /// <summary>
    /// The shots per load the exact F test needs to detect <paramref name="ratio"/> with <paramref name="power"/>, section 9.2:
    /// the smallest n whose power reaches it, searched upward from a little below the approximation, which section 9.2 found
    /// within one shot of the exact answer everywhere it was checked.
    /// </summary>
    public static int ShotsPerLoad(double ratio, double alpha = 0.05, double power = 0.8)
    {
        int n = Math.Max(2, ShotsPerLoadApproximation(ratio, alpha, power) - 5);
        while (n > 2 && Power(ratio, n, alpha) >= power)
        {
            n--;
        }

        while (Power(ratio, n, alpha) < power)
        {
            n++;
        }

        return n;
    }

    /// <summary>
    /// The smallest dispersion ratio <paramref name="n"/> shots per load detect with <paramref name="power"/>, section 9.2's
    /// "these 20 shots per load cannot resolve a difference smaller than about 45 percent", by bisection on the ratio.
    /// </summary>
    public static double MinimumDetectableRatio(int n, double alpha = 0.05, double power = 0.8)
    {
        double low = 1, high = 2;
        while (Power(high, n, alpha) < power)
        {
            high *= 2;
        }

        for (int i = 0; i < 200 && high - low > 1e-12 * high; i++)
        {
            double mid = (low + high) / 2;
            if (Power(mid, n, alpha) < power)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        return high;
    }
}

/// <summary>
/// What a group of n shots is expected to do at its worst, docs/STATISTICS.md section 10, stated before a user may exclude a
/// shot as a flyer.
/// </summary>
public static class Flyers
{
    /// <summary>
    /// The expected largest radius of n shots from a circular normal, in sigmas: E[R_max] = integral from 0 to infinity of
    /// 1 - F(r)^n dr with F(r) = 1 - exp(-r^2 / 2).
    /// <para>
    /// Section 10 also gives it as sqrt(pi / 2) times a sum over k of C(n, k) (-1)^(k + 1) / sqrt(k). That sum is a numerical trap:
    /// at 25 shots its terms reach about 5.2 million with alternating signs, and double precision summation loses most of the
    /// digits. The integral has no cancellation, so it is what is computed, by Simpson's rule on a grid fine enough that the
    /// smooth integrand gives machine precision, out to where F(r)^n is within 1e-17 of 1.
    /// </para>
    /// </summary>
    public static double ExpectedWorstInSigmas(int n)
    {
        double end = Math.Sqrt(2 * Math.Log(n * 1e17));
        const int intervals = 20000;
        double h = end / intervals, sum = 0;
        for (int i = 0; i <= intervals; i++)
        {
            double r = i * h;
            double tail = Math.Exp(-r * r / 2);
            double value = tail >= 1 ? 1 : -SpecialFunctions.ExpM1(n * SpecialFunctions.Log1P(-tail));
            sum += value * (i == 0 || i == intervals ? 1 : i % 2 == 1 ? 4 : 2);
        }

        return sum * h / 3;
    }

    /// <summary>The expected worst shot as a multiple of the mean radius.</summary>
    public static double ExpectedWorstInMeanRadii(int n) => ExpectedWorstInSigmas(n) / RayleighEstimate.MeanRadiusFactor;

    /// <summary>
    /// The probability that the worst of n shots lies beyond <paramref name="multiple"/> mean radii, 1 - F(multiple sqrt(pi / 2))^n,
    /// section 10's table: at 25 shots, beyond twice the mean radius two times in three.
    /// </summary>
    public static double ProbabilityWorstBeyond(int n, double multiple)
    {
        double r = multiple * RayleighEstimate.MeanRadiusFactor;
        return -SpecialFunctions.ExpM1(n * SpecialFunctions.Log1P(-Math.Exp(-r * r / 2)));
    }
}

/// <summary>One pairwise dispersion comparison between two pooled targets, with its Holm-adjusted p-value.</summary>
public sealed record PairwiseDispersion(int First, int Second, double Ratio, double PValue, double HolmPValue);

/// <summary>
/// Pooled and virtual groups, docs/STATISTICS.md section 11. Two questions with different answers, and the engine never picks one
/// silently: how the rifle and load disperse, pooled after re-centring each target, and where they put shots all in, pooled
/// without re-centring. Before pooling, the targets are compared with each other, so a reason not to pool is said before it is
/// hidden in the pooled figure.
/// </summary>
public static class Pooling
{
    /// <summary>
    /// Question A: every target re-centred on its own centre, rSqSum pooled, on 2(N - k) degrees of freedom with the correction
    /// 1 / c4(2N - 2k + 1). Each re-centring costs two degrees of freedom: eight 25-shot targets give 384, not 398.
    /// </summary>
    public static RayleighEstimate Recentred(IReadOnlyList<IReadOnlyList<PointD>> targets, double level = 0.95)
    {
        ArgumentNullException.ThrowIfNull(targets);
        double sum = 0;
        int shots = 0;
        foreach (var target in targets)
        {
            var c = GroupStatistics.Centre(target);
            sum += target.Sum(s => ((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y)));
            shots += target.Count;
        }

        int k = targets.Count;
        return GroupStatistics.FromSumOfSquares(sum, 2.0 * (shots - k), (2.0 * shots) - (2.0 * k) + 1, level);
    }

    /// <summary>
    /// Question B: every shot's raw offset pooled without re-centring, so zero drift between sessions stays in, about the pooled
    /// centre or, when it is supplied, about the point of aim.
    /// </summary>
    public static RayleighEstimate AllIn(IReadOnlyList<IReadOnlyList<PointD>> targets, double level = 0.95, PointD? pointOfAim = null)
    {
        ArgumentNullException.ThrowIfNull(targets);
        return GroupStatistics.Rayleigh([.. targets.SelectMany(t => t)], level, pointOfAim);
    }

    /// <summary>
    /// Section 11's guard: section 8.1's F test between every pair of the targets to be pooled, with Holm's adjustment of section 8.4
    /// across the family, so a significant difference in dispersion is reported before the targets are pooled.
    /// </summary>
    public static IReadOnlyList<PairwiseDispersion> CompareBeforePooling(IReadOnlyList<IReadOnlyList<PointD>> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var estimates = targets.Select(t => GroupStatistics.Rayleigh(t)).ToList();
        var pairs = new List<(int A, int B, double Ratio, double P)>();
        for (int a = 0; a < targets.Count; a++)
        {
            for (int b = a + 1; b < targets.Count; b++)
            {
                var (ratio, _, p) = GroupComparison.DispersionRatio(estimates[a], estimates[b]);
                pairs.Add((a, b, ratio.Value, p));
            }
        }

        var holm = GroupComparison.Holm([.. pairs.Select(p => p.P)]);
        return [.. pairs.Select((p, i) => new PairwiseDispersion(p.A, p.B, p.Ratio, p.P, holm[i]))];
    }
}
