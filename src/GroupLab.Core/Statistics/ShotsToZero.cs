namespace GroupLab.Core.Statistics;

/// <summary>What one goal needs: the smallest group, in shots, that reaches each chance, or null where 1,000 shots do not.</summary>
public sealed record ShotsFor(int? Ninety, int? NinetyFive, int? NinetyNine);

/// <summary>
/// The answer: shots needed for the chance of landing on the closest click, and within one click of it, on both axes; the same for each axis
/// alone; the curve of both-axes chance against shots; and what the simulation was, so the numbers can be repeated or questioned.
/// </summary>
public sealed record ShotsToZeroAnswer(
    ShotsFor ClosestClick,
    ShotsFor WithinOneClick,
    ShotsFor ClosestClickOneAxis,
    ShotsFor WithinOneClickOneAxis,
    IReadOnlyList<(int Shots, double ClosestClick, double WithinOneClick)> Curve,
    bool ExactSigma,
    int Trials,
    int Seed,
    int ShotsCanMove);

/// <summary>
/// "Shots Needed to Zero", NOTES-FROM-PLANNING.md entry 252 sections 3 and 4, suggested by Jylee. The procedure is the one a shooter follows:
/// the true zero lies anywhere within a click on each axis, n shots are fired, their centre is taken, and the scope is moved by that centre
/// rounded to the nearest click. <b>Closest click</b> is landing, on both axes, on the click nearest the true zero; <b>within 1 click</b> is
/// landing on it or on one either side.
/// <para>
/// <b>How it is worked out, and why it is cheap</b> (section 4). With sigma known, one axis's chance is an integral with a closed form: the
/// true zero u is uniform on (-1/2, 1/2) clicks and the centre of n shots is normal about it with t = sigma / sqrt(n), so the chance that the
/// centre rounds to the right click is t [G((h + 1/2)/t) - G((h - 1/2)/t) - G((1/2 - h)/t) + G((-h - 1/2)/t)], where G(x) = x Phi(x) + phi(x)
/// is the integral of the normal distribution function, h = 1/2 for the closest click and 3/2 for within one. The axes are independent given
/// sigma, so both is the square. That is the exact-sigma answer, with no simulation at all.
/// </para>
/// <para>
/// Where sigma is itself uncertain, estimated from the group's shots with its degrees of freedom, it is drawn per trial from that uncertainty,
/// sigma^2 = s^2 df / chi-square(df), once for both axes; each trial's chance is then the closed form above, so the only thing the simulation
/// samples is sigma. The same draws serve every n (common random numbers), so the curve is smooth and never falls, and the thresholds are
/// found by bisection over n from 1 to 1,000 rather than by trying every n.
/// </para>
/// </summary>
public static class ShotsToZero
{
    public const int MostShots = 1000;

    /// <summary>
    /// Sigma is one number, drawn stratified: trial i takes the quantile (i + r) / N of sigma's distribution, r uniform from the seed. Each
    /// trial's chance falls as sigma rises, so the error of the mean is bounded by the chance's range over N; entry 252 section 4.
    /// </summary>
    public const int DefaultTrials = 4_000;

    /// <summary>The goals' chances, in the order the answer gives them.</summary>
    public static readonly double[] Levels = [0.90, 0.95, 0.99];

    /// <summary>The shot counts the curve is drawn at.</summary>
    public static readonly int[] CurveShots = [1, 2, 3, 5, 7, 10, 15, 20, 30, 50, 70, 100, 150, 200, 300, 500, 700, 1000];

    /// <summary>
    /// A group's sigma, one axis, in clicks: the angle it subtends at the distance, over the click's own angle. Shared by the desktop and
    /// the phone (entry 258), so the two cannot differ.
    /// </summary>
    public static double SigmaClicks(double sigmaInches, double distanceInches, AngularUnit clickUnit, double clickValue) =>
        Angular.Constant(clickUnit) / 2 * Math.Atan(sigmaInches / distanceInches) / clickValue;

    /// <summary>One axis's chance, sigma known, for a group of <paramref name="shots"/>: h 0.5 for the closest click, 1.5 for within one.</summary>
    public static double Axis(double sigmaClicks, int shots, double h)
    {
        double t = sigmaClicks / Math.Sqrt(shots);
        if (t <= 1e-12)
        {
            return 1;
        }

        static double G(double x) => (x * Phi(x)) + (Math.Exp(-x * x / 2) * InverseRootTwoPi);
        double value = t * (G((h + 0.5) / t) - G((h - 0.5) / t) - G((0.5 - h) / t) + G((-h - 0.5) / t));
        return Math.Clamp(value, 0, 1);
    }

    /// <summary>
    /// The answer for a group whose per-axis sigma is <paramref name="sigmaClicks"/> in clicks, estimated with <paramref name="degreesOfFreedom"/>
    /// (null, or <paramref name="exact"/>, to take it as known). <paramref name="trials"/> sigma draws with <paramref name="seed"/>.
    /// </summary>
    public static ShotsToZeroAnswer Work(double sigmaClicks, int? degreesOfFreedom, bool exact, int seed, int trials = DefaultTrials, CancellationToken cancellation = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sigmaClicks);
        bool known = exact || degreesOfFreedom is not > 0;
        double[] sigmas;
        if (known)
        {
            sigmas = [sigmaClicks];
        }
        else
        {
            double offset = new Random(seed).NextDouble();
            sigmas = new double[trials];
            double df = degreesOfFreedom!.Value;
            for (int i = 0; i < trials; i++)
            {
                if (i % 512 == 0)
                {
                    cancellation.ThrowIfCancellationRequested();
                }

                // The upper tail of chi-square gives the small ones: a small chi-square is a large sigma.
                sigmas[i] = sigmaClicks * Math.Sqrt(df / Distributions.ChiSquareQuantile((i + offset) / trials, df));
            }
        }

        // Both axes share a trial's sigma, so both is the mean of the squares, never the square of the mean. Each count is worked out once.
        var cache = new Dictionary<(int, double), (double Both, double One)>();
        (double Both, double One) Chance(int shots, double h)
        {
            if (cache.TryGetValue((shots, h), out var known))
            {
                return known;
            }

            double both = 0, one = 0;
            foreach (double s in sigmas)
            {
                double p = Axis(s, shots, h);
                both += p * p;
                one += p;
            }

            return cache[(shots, h)] = (both / sigmas.Length, one / sigmas.Length);
        }

        int? Smallest(double level, double h, bool bothAxes)
        {
            double At(int n) => bothAxes ? Chance(n, h).Both : Chance(n, h).One;
            if (At(MostShots) < level)
            {
                return null;
            }

            int low = 1, high = MostShots;
            while (low < high)
            {
                cancellation.ThrowIfCancellationRequested();
                int middle = (low + high) / 2;
                if (At(middle) >= level)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }

        ShotsFor For(double h, bool bothAxes) =>
            new(Smallest(Levels[0], h, bothAxes), Smallest(Levels[1], h, bothAxes), Smallest(Levels[2], h, bothAxes));

        var curve = CurveShots.Select(n => (n, Chance(n, 0.5).Both, Chance(n, 1.5).Both)).ToList();

        // How far a reported count can move between runs: the counts where the chance sits two standard errors either side of the level.
        int move = 0;
        if (!known)
        {
            foreach (double level in Levels)
            {
                foreach (double h in (double[])[0.5, 1.5])
                {
                    if (Smallest(level, h, true) is not { } n)
                    {
                        continue;
                    }

                    // Stratified over a monotone chance, the mean is within the chance's whole range over N of the true one.
                    double spread = (Math.Pow(Axis(sigmas.Min(), n, h), 2) - Math.Pow(Axis(sigmas.Max(), n, h), 2)) / sigmas.Length;
                    int lower = Smallest(level - (2 * spread), h, true) ?? n;
                    int upper = Smallest(level + (2 * spread), h, true) ?? MostShots;
                    move = Math.Max(move, Math.Max(n - lower, upper - n));
                }
            }
        }

        return new ShotsToZeroAnswer(For(0.5, true), For(1.5, true), For(0.5, false), For(1.5, false), curve, known, known ? 0 : trials, seed, move);
    }

    private const double InverseRootTwoPi = 0.3989422804014327;

    /// <summary>
    /// The normal distribution function by the complementary error function of Numerical Recipes (Chebyshev fit, relative error under
    /// 1.2e-7 everywhere), a hundred times quicker than the incomplete gamma, and far finer than any chance shown.
    /// </summary>
    private static double Phi(double x)
    {
        double z = Math.Abs(x) / Math.Sqrt(2), t = 1 / (1 + (0.5 * z));
        double poly = 0.17087277;
        foreach (double c in (double[])[-0.82215223, 1.48851587, -1.13520398, 0.27886807, -0.18628806, 0.09678418, 0.37409196, 1.00002368, -1.26551223])
        {
            poly = c + (t * poly);
        }

        double erfc = t * Math.Exp((-z * z) + poly);
        return x >= 0 ? 1 - (erfc / 2) : erfc / 2;
    }
}
