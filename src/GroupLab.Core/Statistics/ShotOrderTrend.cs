namespace GroupLab.Core.Statistics;

/// <summary>
/// Whether a group opened up, or tightened, as the shots were fired, NOTES-FROM-PLANNING.md entry 141 section 5.2.3.
/// </summary>
/// <param name="Shots">How many shots the order is known for.</param>
/// <param name="Correlation">Spearman's rank correlation between the order fired and the distance from the group's centre.</param>
/// <param name="PValue">The share of shuffles whose correlation was at least as large in size as this one, two sided.</param>
/// <param name="Resamples">How many shuffles, so the figure can be reproduced.</param>
/// <param name="Seed">The seed those shuffles came from, for the same reason.</param>
public sealed record OrderTrend(int Shots, double Correlation, double PValue, int Resamples, ulong Seed);

/// <summary>
/// Did the group open up as it was shot? A barrel warming, a shooter tiring and a rest settling are all things people believe they can see
/// in a group, and a group of ten shots will always look like it did one of them.
/// <para>
/// <b>Rank correlation, and a permutation test, for two reasons.</b> Ranks, because the question is whether later shots sit further out
/// rather than whether they sit further out in proportion to anything, and because one wild shot should not decide the answer. A permutation
/// test, because under the hypothesis that order does not matter every ordering of the same shots is equally likely, so the null distribution
/// can be built exactly from the shots themselves rather than assumed. That needs no distributional assumption at all, which matters at the
/// sample sizes people actually shoot.
/// </para>
/// <para>
/// <b>It is deliberately two sided.</b> A shooter looking for a barrel warming will find it one-sided, and a shooter looking for settling in
/// will find the opposite, and the data cannot be asked both questions at once without paying for it.
/// </para>
/// </summary>
public static class ShotOrderTrend
{
    /// <summary>The fewest shots worth testing. Below this every ordering is a large share of the possible ones and no p-value can be small.</summary>
    public const int FewestShots = 5;

    /// <summary>
    /// The trend in distance from the centre against the order fired, or null where there are too few shots.
    /// </summary>
    /// <param name="radii">Each shot's distance from the group's centre, in the order it was fired.</param>
    public static OrderTrend? Of(IReadOnlyList<double> radii, int resamples = 9999, ulong seed = 20260922)
    {
        ArgumentNullException.ThrowIfNull(radii);
        int n = radii.Count;
        if (n < FewestShots)
        {
            return null;
        }

        var ranks = Ranks(radii);
        var order = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
        double observed = Math.Abs(Pearson(order, ranks));

        var random = new StatisticsRandom(seed);
        var shuffled = new double[n];
        int atLeast = 0;
        for (int r = 0; r < resamples; r++)
        {
            ranks.CopyTo(shuffled, 0);
            for (int i = n - 1; i > 0; i--)
            {
                int j = (int)(random.NextDouble() * (i + 1));
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            if (Math.Abs(Pearson(order, shuffled)) >= observed)
            {
                atLeast++;
            }
        }

        return new OrderTrend(n, Pearson(order, ranks), (1.0 + atLeast) / (resamples + 1.0), resamples, seed);
    }

    /// <summary>Ranks, ties sharing their average, which is what makes this Spearman's rather than something close to it.</summary>
    private static double[] Ranks(IReadOnlyList<double> values)
    {
        var order = Enumerable.Range(0, values.Count).OrderBy(i => values[i]).ToArray();
        var ranks = new double[values.Count];
        for (int i = 0; i < order.Length;)
        {
            int j = i;
            while (j + 1 < order.Length && values[order[j + 1]] == values[order[i]])
            {
                j++;
            }

            double shared = ((i + j) / 2.0) + 1;
            for (int k = i; k <= j; k++)
            {
                ranks[order[k]] = shared;
            }

            i = j + 1;
        }

        return ranks;
    }

    private static double Pearson(IReadOnlyList<double> a, IReadOnlyList<double> b)
    {
        int n = a.Count;
        double meanA = a.Average(), meanB = b.Average();
        double top = 0, leftSum = 0, rightSum = 0;
        for (int i = 0; i < n; i++)
        {
            double da = a[i] - meanA, db = b[i] - meanB;
            top += da * db;
            leftSum += da * da;
            rightSum += db * db;
        }

        return leftSum <= 0 || rightSum <= 0 ? 0 : top / Math.Sqrt(leftSum * rightSum);
    }
}
