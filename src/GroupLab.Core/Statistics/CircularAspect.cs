namespace GroupLab.Core.Statistics;

/// <summary>
/// What the error ellipse's aspect ratio is from a group that is really circular, docs/STATISTICS.md section 7 as NOTES-FROM-PLANNING.md
/// entry 76 section 1 extended it: the aspect is still reported without a test, but beside the figure a circular group of the same size
/// would give, the pattern section 10 uses for the worst shot. Ten circular shots give a median aspect of about 1.5, so an aspect of 2.8
/// from ten shots is unusual but not rare, and a reader shown only the 2.8 cannot tell.
/// <para>
/// The distribution is exact. The aspect is sqrt(l1 / l2), l1 and l2 the eigenvalues of the centred sums of squares, which for n shots
/// from a circular normal are a 2 by 2 Wishart on m = n - 1 degrees of freedom with joint density proportional to
/// (l1 l2)^((m - 3) / 2) (l1 - l2) exp(-(l1 + l2) / 2). Integrating out the scale leaves, for u = 1 / aspect on (0, 1), a density
/// proportional to u^(m - 2) (1 - u^2) / (1 + u^2)^m, which is smooth for m of 2 or more. Entry 76's simulated quantiles are
/// reproduced to the printed digits (tests), and a seeded simulation agrees.
/// </para>
/// </summary>
public static class CircularAspect
{
    /// <summary>The fewest shots with a defined distribution: at two shots the ellipse is a line.</summary>
    public const int MinimumShots = 3;

    private const int Intervals = 20000;

    /// <summary>The probability that n circular shots give an aspect ratio above <paramref name="aspect"/>.</summary>
    public static double ProbabilityAbove(int n, double aspect)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, MinimumShots);
        return aspect <= 1 ? 1 : Integral(n, 1 / aspect) / Integral(n, 1);
    }

    /// <summary>The aspect ratio n circular shots exceed with probability <paramref name="p"/>; 0.5 gives the median.</summary>
    public static double Quantile(int n, double p)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, MinimumShots);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(p);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(p, 1);
        double total = Integral(n, 1), low = 0, high = 1;
        for (int i = 0; i < 60; i++)
        {
            double u = (low + high) / 2;
            (Integral(n, u) / total < p ? ref low : ref high) = u;
        }

        return 2 / (low + high);
    }

    /// <summary>The median aspect ratio of n circular shots.</summary>
    public static double Median(int n) => Quantile(n, 0.5);

    /// <summary>The integral of the unnormalised density of 1 / aspect from 0 to <paramref name="end"/>, by Simpson's rule.</summary>
    private static double Integral(int n, double end)
    {
        int m = n - 1;
        double h = end / Intervals, sum = 0;
        for (int i = 0; i <= Intervals; i++)
        {
            double u = i * h;
            double value = u >= 1 ? 0 : m == 2 ? (1 - (u * u)) / Math.Pow(1 + (u * u), 2)
                : u <= 0 ? 0 : Math.Exp(((m - 2) * Math.Log(u)) + SpecialFunctions.Log1P(-u * u) - (m * SpecialFunctions.Log1P(u * u)));
            sum += value * (i == 0 || i == Intervals ? 1 : i % 2 == 1 ? 4 : 2);
        }

        return sum * h / 3;
    }
}
