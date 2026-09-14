namespace GroupLab.Core.Statistics;

/// <summary>
/// What an interval the application prints actually covers, so that no interval is labelled with a coverage it does not have
/// (NOTES-FROM-PLANNING.md entry 23 section 2 and entry 24 section 1).
/// </summary>
public static class IntervalCoverage
{
    /// <summary>
    /// The exact coverage of the true sigma, and so of the true mean radius, by section 3.3's interval from n shots about their own
    /// centre under the circular normal model. The c4 correction multiplies both endpoints (<see cref="GroupStatistics.FromSumOfSquares"/>),
    /// so with k = 1 / c4(2n − 1), the interval covers sigma exactly when q_lo / k² ≤ χ²(2(n − 1)) ≤ q_hi / k², and that probability
    /// is the coverage. It is below the nominal level, by most at the smallest n.
    /// </summary>
    public static double RayleighSigma(int n, double level = 0.95)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 2);
        double df = 2.0 * (n - 1), alpha = 1 - level;
        double k = 1 / SpecialFunctions.C4((2.0 * n) - 1);
        double lo = Distributions.ChiSquareQuantile(alpha / 2, df) / (k * k), hi = Distributions.ChiSquareQuantile(1 - (alpha / 2), df) / (k * k);
        return Distributions.ChiSquare(hi, df).Lower - Distributions.ChiSquare(lo, df).Lower;
    }
}
