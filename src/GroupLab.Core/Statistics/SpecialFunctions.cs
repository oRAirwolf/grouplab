namespace GroupLab.Core.Statistics;

/// <summary>
/// The special functions the statistics engine is built on: log-gamma, the regularised incomplete gamma and beta functions,
/// and the <c>c4</c> bias-correction factor of docs/STATISTICS.md section 3.2.
/// <para>
/// Precision is the requirement, not speed. Section 15.3 compares closed-form quantities with shotGroups at 1e-12 relative,
/// and every chi-square, t and F quantile behind them is computed here. The two places double precision is usually lost are
/// both handled the way R handles them: the prefactor <c>x^a e^-x / Gamma(a)</c> is formed through <see cref="Bd0"/> and the
/// Stirling remainder rather than as a difference of large logarithms, and an upper tail is computed as an upper tail, never as
/// one minus a lower tail.
/// </para>
/// </summary>
public static class SpecialFunctions
{
    private const double HalfLogTwoPi = 0.91893853320467274178;

    /// <summary>
    /// ln Gamma(x) for x &gt; 0: the Stirling series from 10 upward, and below 10 the recurrence Gamma(x) = Gamma(x + m) /
    /// (x (x + 1) ... (x + m - 1)), so the series is only ever evaluated where eight terms reach machine precision.
    /// </summary>
    public static double LogGamma(double x)
    {
        if (!(x > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, "log-gamma is defined here for x > 0 only.");
        }

        if (x >= 10)
        {
            return ((x - 0.5) * Math.Log(x)) - x + HalfLogTwoPi + StirlingSeries(x);
        }

        double product = 1;
        while (x < 10)
        {
            product *= x;
            x += 1;
        }

        return ((x - 0.5) * Math.Log(x)) - x + HalfLogTwoPi + StirlingSeries(x) - Math.Log(product);
    }

    /// <summary>
    /// The Stirling remainder, ln Gamma(x) - ((x - 1/2) ln x - x + ln sqrt(2 pi)), for x &gt;= 10: the asymptotic series in the
    /// Bernoulli numbers, whose next term is below 1e-17 there.
    /// </summary>
    private static double StirlingSeries(double x)
    {
        double z = 1 / (x * x);
        return (1.0 / 12 - z * (1.0 / 360 - z * (1.0 / 1260 - z * (1.0 / 1680 - z * (1.0 / 1188 - z * (691.0 / 360360 - z * (1.0 / 156 - z * 3617.0 / 122400))))))) / x;
    }

    /// <summary>The Stirling remainder for any x &gt; 0, from the series at 10 upward and from log-gamma below.</summary>
    private static double StirlingError(double x) =>
        x >= 10 ? StirlingSeries(x) : LogGamma(x) - (((x - 0.5) * Math.Log(x)) - x + HalfLogTwoPi);

    /// <summary>
    /// <c>x ln(x / m) + m - x</c>, the deviance term of R's <c>bd0</c>, computed by its series when x and m are close so that
    /// the cancellation of the three terms costs no digits.
    /// </summary>
    internal static double Bd0(double x, double m)
    {
        if (Math.Abs(x - m) < 0.1 * (x + m))
        {
            double v = (x - m) / (x + m), s = (x - m) * v, ej = 2 * x * v, v2 = v * v;
            for (int j = 1; j < 1000; j++)
            {
                ej *= v2;
                double next = s + (ej / ((2 * j) + 1));
                if (next == s)
                {
                    return next;
                }

                s = next;
            }

            return s;
        }

        return (x * Math.Log(x / m)) + m - x;
    }

    /// <summary>ln(x^a e^-x / Gamma(a)), the prefactor of both incomplete gamma expansions, without the cancellation of forming it directly.</summary>
    private static double LogGammaPrefactor(double a, double x) =>
        -Bd0(a, x) + (0.5 * Math.Log(a)) - HalfLogTwoPi - StirlingError(a);

    /// <summary>The regularised lower incomplete gamma function P(a, x).</summary>
    public static double GammaP(double a, double x) => IncompleteGamma(a, x).Lower;

    /// <summary>The regularised upper incomplete gamma function Q(a, x) = 1 - P(a, x), computed as an upper tail.</summary>
    public static double GammaQ(double a, double x) => IncompleteGamma(a, x).Upper;

    /// <summary>
    /// Both regularised incomplete gamma functions: the power series for x &lt; a + 1, where it converges fastest, and the
    /// continued fraction by the modified Lentz method otherwise; the other tail is the complement of the one computed.
    /// </summary>
    public static (double Lower, double Upper) IncompleteGamma(double a, double x)
    {
        if (!(a > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(a), a, "the shape must be positive.");
        }

        if (x <= 0)
        {
            return (0, 1);
        }

        if (double.IsPositiveInfinity(x))
        {
            return (1, 0);
        }

        double prefactor = Math.Exp(LogGammaPrefactor(a, x));
        if (x < a + 1)
        {
            double term = 1 / a, sum = term;
            for (int n = 1; n < 100000; n++)
            {
                term *= x / (a + n);
                sum += term;
                if (Math.Abs(term) < Math.Abs(sum) * 1e-17)
                {
                    break;
                }
            }

            double lower = prefactor * sum;
            return (lower, 1 - lower);
        }

        const double tiny = 1e-300;
        double bb = x + 1 - a, c = 1 / tiny, d = 1 / bb, h = d;
        for (int i = 1; i < 100000; i++)
        {
            double an = -i * (i - a);
            bb += 2;
            d = (an * d) + bb;
            if (Math.Abs(d) < tiny)
            {
                d = tiny;
            }

            c = bb + (an / c);
            if (Math.Abs(c) < tiny)
            {
                c = tiny;
            }

            d = 1 / d;
            double delta = d * c;
            h *= delta;
            if (Math.Abs(delta - 1) < 1e-17)
            {
                break;
            }
        }

        double upper = prefactor * h;
        return (1 - upper, upper);
    }

    /// <summary>
    /// ln(1 + x), accurate for small x: through ln(1 + x) = 2 atanh(x / (2 + x)) and its series when |x| &lt; 1/2, where forming
    /// 1 + x first would round away the digits of x.
    /// </summary>
    public static double Log1P(double x)
    {
        if (!(Math.Abs(x) < 0.5))
        {
            return Math.Log(1 + x);
        }

        double u = x / (2 + x), u2 = u * u, term = u, sum = u;
        for (int k = 3; k < 200; k += 2)
        {
            term *= u2;
            double next = sum + (term / k);
            if (next == sum)
            {
                break;
            }

            sum = next;
        }

        return 2 * sum;
    }

    /// <summary>exp(x) - 1, accurate for small x, through 2 tanh(x / 2) / (1 - tanh(x / 2)).</summary>
    public static double ExpM1(double x)
    {
        if (!(Math.Abs(x) < 1))
        {
            return Math.Exp(x) - 1;
        }

        double t = Math.Tanh(x / 2);
        return 2 * t / (1 - t);
    }

    /// <summary>ln B(a, b).</summary>
    public static double LogBeta(double a, double b) => LogGamma(a) + LogGamma(b) - LogGamma(a + b);

    /// <summary>
    /// Both tails of the regularised incomplete beta function, I_x(a, b) and 1 - I_x(a, b), by the continued fraction on
    /// whichever side of the mean converges, the other tail by symmetry I_x(a, b) = 1 - I_{1-x}(b, a).
    /// </summary>
    public static (double Lower, double Upper) IncompleteBeta(double x, double a, double b)
    {
        if (!(a > 0) || !(b > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(a), "both shapes must be positive.");
        }

        if (x <= 0)
        {
            return (0, 1);
        }

        if (x >= 1)
        {
            return (1, 0);
        }

        if (x > (a + 1) / (a + b + 2))
        {
            var (lower, upper) = IncompleteBeta(1 - x, b, a);
            return (upper, lower);
        }

        double front = Math.Exp((a * Math.Log(x)) + (b * Log1P(-x)) - LogBeta(a, b)) / a;
        double value = front * BetaContinuedFraction(x, a, b);
        return (value, 1 - value);
    }

    /// <summary>The continued fraction for the incomplete beta function, by the modified Lentz method.</summary>
    private static double BetaContinuedFraction(double x, double a, double b)
    {
        const double tiny = 1e-300;
        double qab = a + b, qap = a + 1, qam = a - 1, c = 1, d = 1 - (qab * x / qap);
        if (Math.Abs(d) < tiny)
        {
            d = tiny;
        }

        d = 1 / d;
        double h = d;
        for (int m = 1; m < 100000; m++)
        {
            int m2 = 2 * m;
            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1 + (aa * d);
            d = Math.Abs(d) < tiny ? tiny : d;
            c = 1 + (aa / c);
            c = Math.Abs(c) < tiny ? tiny : c;
            d = 1 / d;
            h *= d * c;
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1 + (aa * d);
            d = Math.Abs(d) < tiny ? tiny : d;
            c = 1 + (aa / c);
            c = Math.Abs(c) < tiny ? tiny : c;
            d = 1 / d;
            double delta = d * c;
            h *= delta;
            if (Math.Abs(delta - 1) < 1e-17)
            {
                break;
            }
        }

        return h;
    }

    /// <summary>
    /// The bias-correction factor for the square root of a variance estimate on k - 1 degrees of freedom, docs/STATISTICS.md
    /// section 3.2: c4(k) = sqrt(2 / (k - 1)) exp(ln Gamma(k / 2) - ln Gamma((k - 1) / 2)), through log-gamma so it never
    /// overflows, and clamped to 1 where rounding would put it above.
    /// </summary>
    public static double C4(double k)
    {
        if (!(k > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "c4 needs k > 1.");
        }

        double value = Math.Sqrt(2 / (k - 1)) * Math.Exp(LogGamma(k / 2) - LogGamma((k - 1) / 2));
        return double.IsFinite(value) ? Math.Min(1, value) : 1;
    }
}
