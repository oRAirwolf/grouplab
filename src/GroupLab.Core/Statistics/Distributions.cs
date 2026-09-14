namespace GroupLab.Core.Statistics;

/// <summary>
/// The normal, chi-square, Student t and F distributions: CDFs in both tails and quantiles, on <see cref="SpecialFunctions"/>.
/// A quantile is the root of its CDF found by Newton's method inside a bracket that bisection keeps honest, started from a
/// classical approximation and run until the step is below machine precision, and it solves against whichever tail keeps the
/// target probability away from 1, which is where a lower-tail equation loses its digits.
/// </summary>
public static class Distributions
{
    /// <summary>The standard normal CDF, through the incomplete gamma function: Phi(z) = Q(1/2, z^2 / 2) / 2 for z &lt;= 0.</summary>
    public static double NormalCdf(double z) =>
        z <= 0 ? 0.5 * SpecialFunctions.GammaQ(0.5, z * z / 2) : 1 - (0.5 * SpecialFunctions.GammaQ(0.5, z * z / 2));

    /// <summary>The standard normal upper tail, 1 - Phi(z), computed as a tail.</summary>
    public static double NormalUpper(double z) => NormalCdf(-z);

    /// <summary>The standard normal density.</summary>
    public static double NormalDensity(double z) => Math.Exp(-z * z / 2) / Math.Sqrt(2 * Math.PI);

    /// <summary>
    /// The standard normal quantile: Abramowitz and Stegun 26.2.23 as the start, good to 4.5e-4, then Halley's method on the
    /// tail the probability sits in, which triples the correct digits each step.
    /// </summary>
    public static double NormalQuantile(double p)
    {
        if (!(p > 0 && p < 1))
        {
            return p == 0 ? double.NegativeInfinity : p == 1 ? double.PositiveInfinity : double.NaN;
        }

        double tail = Math.Min(p, 1 - p), t = Math.Sqrt(-2 * Math.Log(tail));
        double z = t - ((2.515517 + (0.802853 * t) + (0.010328 * t * t)) / (1 + (1.432788 * t) + (0.189269 * t * t) + (0.001308 * t * t * t)));
        z = p < 0.5 ? -z : z;
        for (int i = 0; i < 6; i++)
        {
            double error = p < 0.5 ? NormalCdf(z) - p : (1 - p) - NormalUpper(z);
            double density = NormalDensity(z);
            if (density == 0)
            {
                break;
            }

            double step = error / density;
            double next = z - (step / (1 + (z * step / 2)));
            if (Math.Abs(next - z) <= 1e-16 * Math.Max(1, Math.Abs(z)))
            {
                return next;
            }

            z = next;
        }

        return z;
    }

    /// <summary>The chi-square CDF on <paramref name="df"/> degrees of freedom, both tails.</summary>
    public static (double Lower, double Upper) ChiSquare(double x, double df) => SpecialFunctions.IncompleteGamma(df / 2, x / 2);

    /// <summary>The chi-square density.</summary>
    public static double ChiSquareDensity(double x, double df) =>
        x <= 0 ? 0 : Math.Exp(((df / 2 - 1) * Math.Log(x / 2)) - (x / 2) - SpecialFunctions.LogGamma(df / 2)) / 2;

    /// <summary>The chi-square quantile, started from the Wilson-Hilferty cube-root approximation.</summary>
    public static double ChiSquareQuantile(double p, double df)
    {
        if (!(p > 0 && p < 1))
        {
            return p == 0 ? 0 : p == 1 ? double.PositiveInfinity : double.NaN;
        }

        double h = 2 / (9 * df), start = df * Math.Pow(1 - h + (NormalQuantile(p) * Math.Sqrt(h)), 3);
        start = start > 0 ? start : df * 1e-3;
        return Solve(p, x => ChiSquare(x, df), x => ChiSquareDensity(x, df), start, 0, double.PositiveInfinity);
    }

    /// <summary>The Student t CDF on <paramref name="df"/> degrees of freedom, both tails, through the incomplete beta function.</summary>
    public static (double Lower, double Upper) StudentT(double t, double df)
    {
        double tail = 0.5 * SpecialFunctions.IncompleteBeta(df / (df + (t * t)), df / 2, 0.5).Lower;
        return t <= 0 ? (tail, 1 - tail) : (1 - tail, tail);
    }

    /// <summary>The Student t density.</summary>
    public static double StudentTDensity(double t, double df) =>
        Math.Exp(-((df + 1) / 2 * SpecialFunctions.Log1P(t * t / df)) - (0.5 * Math.Log(df)) - SpecialFunctions.LogBeta(0.5, df / 2));

    /// <summary>The Student t quantile, started from the normal quantile.</summary>
    public static double StudentTQuantile(double p, double df)
    {
        if (!(p > 0 && p < 1))
        {
            return p == 0 ? double.NegativeInfinity : p == 1 ? double.PositiveInfinity : double.NaN;
        }

        if (p == 0.5)
        {
            return 0;
        }

        return Solve(p, t => StudentT(t, df), t => StudentTDensity(t, df), NormalQuantile(p), double.NegativeInfinity, double.PositiveInfinity);
    }

    /// <summary>The F CDF on (<paramref name="df1"/>, <paramref name="df2"/>) degrees of freedom, both tails.</summary>
    public static (double Lower, double Upper) F(double x, double df1, double df2) =>
        x <= 0 ? (0, 1) : SpecialFunctions.IncompleteBeta(df1 * x / ((df1 * x) + df2), df1 / 2, df2 / 2);

    /// <summary>The F density.</summary>
    public static double FDensity(double x, double df1, double df2) =>
        x <= 0 ? 0 : Math.Exp((df1 / 2 * Math.Log(df1 / df2)) + ((df1 / 2 - 1) * Math.Log(x)) - ((df1 + df2) / 2 * SpecialFunctions.Log1P(df1 * x / df2)) - SpecialFunctions.LogBeta(df1 / 2, df2 / 2));

    /// <summary>The F quantile, started from the ratio of chi-square quantiles' means.</summary>
    public static double FQuantile(double p, double df1, double df2)
    {
        if (!(p > 0 && p < 1))
        {
            return p == 0 ? 0 : p == 1 ? double.PositiveInfinity : double.NaN;
        }

        double start = ChiSquareQuantile(p, df1) / df1;
        return Solve(p, x => F(x, df1, df2), x => FDensity(x, df1, df2), start > 0 ? start : 1, 0, double.PositiveInfinity);
    }

    /// <summary>
    /// The root of CDF(x) = p inside (low, high): Newton steps against the lower tail below the median and the upper tail above
    /// it, falling back to bisection, or to doubling when the bracket is still open, whenever a step would leave the bracket.
    /// </summary>
    private static double Solve(double p, Func<double, (double Lower, double Upper)> cdf, Func<double, double> density, double x, double low, double high)
    {
        bool upper = p > 0.5;
        double target = upper ? 1 - p : p;
        for (int i = 0; i < 400; i++)
        {
            var (lo, up) = cdf(x);
            double value = upper ? up : lo;

            // Below the root the lower tail is short of the target and the upper tail is over it.
            bool below = upper ? value > target : value < target;
            if (below)
            {
                low = x;
            }
            else
            {
                high = x;
            }

            double error = value - target;
            if (error == 0)
            {
                return x;
            }

            double slope = density(x);
            double next = slope > 0 ? (upper ? x + (error / slope) : x - (error / slope)) : double.NaN;
            if (!(next > low && next < high))
            {
                next = double.IsFinite(low) && double.IsFinite(high) ? (low + high) / 2
                    : double.IsFinite(low) ? x + Math.Max(1, Math.Abs(x))
                    : x - Math.Max(1, Math.Abs(x));
            }

            if (Math.Abs(next - x) <= 4e-16 * Math.Max(Math.Abs(x), 1e-300))
            {
                return next;
            }

            x = next;
        }

        return x;
    }
}
