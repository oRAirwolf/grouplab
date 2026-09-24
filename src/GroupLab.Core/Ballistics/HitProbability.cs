using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Ballistics;

/// <summary>The shapes a hit probability is worked out on.</summary>
public enum HitShape
{
    Circle,
    Rectangle,
    Ipsc,
}

/// <summary>
/// A target centered on the aim, in inches: across positive to the right, up positive. A circle's size is its diameter in
/// <see cref="WidthInches"/>. The IPSC shape is GroupLab's own outline drawn from the width and height given, not an official one: a body
/// four fifths of the height with its corners cut by a sixth of the width, a head a third of the width on top, and the aim at the body's
/// center.
/// </summary>
public sealed record HitTarget(HitShape Shape, double WidthInches, double HeightInches)
{
    /// <summary>The share of an IPSC outline's height that is its body; the rest is the head.</summary>
    public const double IpscBodyShare = 0.8;

    public static HitTarget Circle(double diameterInches) => new(HitShape.Circle, diameterInches, diameterInches);

    /// <summary>True where a shot at (<paramref name="across"/>, <paramref name="up"/>) from the aim is on the target.</summary>
    public bool Contains(double across, double up)
    {
        double x = Math.Abs(across);
        switch (Shape)
        {
            case HitShape.Circle:
                double r = WidthInches / 2;
                return (across * across) + (up * up) <= r * r;
            case HitShape.Rectangle:
                return x <= WidthInches / 2 && Math.Abs(up) <= HeightInches / 2;
            default:
                double half = WidthInches / 2, body = HeightInches * IpscBodyShare / 2, cut = WidthInches / 6;
                if (up > body)
                {
                    return up <= body + (HeightInches * (1 - IpscBodyShare)) && x <= WidthInches / 6;
                }

                double y = Math.Abs(up);
                if (x > half || y > body)
                {
                    return false;
                }

                return x <= half - cut || y <= body - cut || (x - (half - cut)) + (y - (body - cut)) <= cut;
        }
    }
}

/// <summary>
/// The error sources a hit probability carries. <see cref="Dispersion"/> and <see cref="Velocity"/> are drawn for every shot; every other
/// source is drawn once for a string, because the shooter reads the wind, the range and the air once and fires on that reading.
/// </summary>
public enum HitSource
{
    Dispersion,
    Velocity,
    Wind,
    Range,
    Zero,
    Drag,
    Temperature,
    Pressure,
    Humidity,
    Inclination,
    Azimuth,
    Latitude,
}

/// <summary>
/// One error source: the standard deviation of the truth about what the shooter believes, and the bias, the true value's mean less the
/// believed one, which a standard deviation alone cannot say (a chronograph reading 20 ft/s fast is a bias). Units are the source's own:
/// ft/s, mph of full-value crosswind, yards, mrad per axis for the zero, percent of the drag, degrees Fahrenheit, inches of mercury, percent
/// humidity and degrees.
/// </summary>
public sealed record HitUncertainty(double Sd, double Bias = 0)
{
    public static HitUncertainty None { get; } = new(0);

    public bool Active => Sd > 0 || Bias != 0;
}

/// <summary>
/// The rifle's precision: the per-axis standard deviation of shots about their own center, as an angle in mrad. For circular dispersion that
/// is the Rayleigh parameter sigma; a radial figure such as the mean radius is about 1.25 times it and must never be entered here.
/// <see cref="DegreesOfFreedom"/> is what the estimate rests on, 2(n - 1) for one group of n, and null when the figure was typed, so its own
/// uncertainty is not known. <see cref="FromYards"/> is the distance it was measured at, where the velocity's share of the vertical is taken
/// out before the velocity is added back per shot.
/// </summary>
public sealed record HitPrecision(double SigmaMrad, double? DegreesOfFreedom, double? FromYards)
{
    /// <summary>A per-axis sigma in inches at a distance, as an angle in mrad.</summary>
    public static double MradFromInches(double sigmaInches, double yards) => sigmaInches / (yards * 36) * 1000;

    /// <summary>An angle in MOA as mrad, by the solver's own factor.</summary>
    public static double MradFromMoa(double moa) => moa * BallisticSolver.MilPerMoa;
}

/// <summary>
/// What a hit probability is worked out from. <see cref="Solver"/> holds what the shooter believes, and the elevation and wind are dialed
/// from it; <see cref="Errors"/> says how far the truth may lie from each belief.
/// </summary>
public sealed record HitSetup(
    BallisticInput Solver,
    double DistanceYards,
    HitTarget Target,
    HitPrecision Precision,
    IReadOnlyDictionary<HitSource, HitUncertainty> Errors,
    int ShotsInString = 1,
    int Trials = HitProbability.DefaultTrials,
    ulong Seed = HitProbability.DefaultSeed);

/// <summary>
/// A probability with its interval. The interval holds both the sigma's own uncertainty, from the ends of its 95 percent interval, and the
/// simulation's, 1.96 standard errors of the trial count; <see cref="SigmaHalfWidth"/> and <see cref="MonteCarloHalfWidth"/> say which is the
/// larger.
/// </summary>
public sealed record HitChance(double Value, double Lower, double Upper, double SigmaHalfWidth, double MonteCarloHalfWidth)
{
    public bool SigmaDominates => SigmaHalfWidth >= MonteCarloHalfWidth;
}

/// <summary>
/// What one error source costs: the first-round probability it takes away, found by working the answer out again without it on the same
/// random numbers, and the spread it puts on the impacts up and down and across, in inches at the distance.
/// </summary>
public sealed record HitCost(HitSource Source, bool PerShot, double Cost, double UpDownInches, double AcrossInches);

/// <summary>One point of hit probability against distance, with the interval as a band.</summary>
public sealed record HitCurvePoint(double Yards, double Value, double Lower, double Upper);

/// <summary>
/// A hit probability's answer. <see cref="Refusal"/> is set, in a plain sentence, when there is nothing honest to show; then
/// <see cref="ShotsNeeded"/> says how many shots in one group would make it mean something, where the refusal is about the group's size.
/// </summary>
public sealed record HitAnswer(
    HitChance FirstRound,
    HitChance SecondRound,
    HitChance AtLeastOne,
    HitChance HitShare,
    int ShotsInString,
    IReadOnlyList<PointD> FirstScatter,
    IReadOnlyList<PointD> SecondScatter,
    IReadOnlyList<HitCost> Costs,
    double UpDownInches,
    double AcrossInches,
    int Trials,
    ulong Seed,
    IReadOnlyList<string> NotIncluded,
    string? Refusal = null,
    int? ShotsNeeded = null)
{
    /// <summary>The expected number of hits in the string, with its interval.</summary>
    public (double Value, double Lower, double Upper) ExpectedHits => (HitShare.Value * ShotsInString, HitShare.Lower * ShotsInString, HitShare.Upper * ShotsInString);
}

/// <summary>
/// Hit probability by simulation, NOTES-FROM-PLANNING.md entry 156, from the shooter's own measured dispersion.
/// <para>
/// <b>The model.</b> Each simulated shot lands at the sum of independent contributions. <b>Per shot</b>: the dispersion, bivariate normal
/// with the precision's sigma on each axis, and the muzzle velocity's spread carried through the drop at the distance. <b>Per string</b>,
/// drawn once and shared by every shot in it: the wind call's error carried through the drift, the range estimate's error carried through
/// the drop and the drift with the dial set for the believed range, the zero's error as a fixed angular offset, and the drag, the air, the
/// inclination and the Earth's rotation, each carried through the solver. The difference is the point: a shooter reads the wind once and
/// fires, so a per-shot draw of the wind call would make it look like dispersion, average out across a string and flatter the answer.
/// </para>
/// <para>
/// <b>How each source reaches the target.</b> The elevation and wind are dialed from the believed inputs, with the sight's zero angle held.
/// Each source's effect on the impact is the solver's trajectory with that input moved, less the believed one, at the distance, fitted by a
/// quadratic through two solves either side of the belief at the bias plus two standard deviations, so a draw is two multiplications. The
/// sources are added, which leaves out how they interact with each other; that is second order at the sizes the presets carry.
/// </para>
/// <para>
/// <b>The precision's own uncertainty.</b> Sigma itself is drawn for every string from its sampling distribution, sigma times
/// sqrt(df / chi-squared(df)), at stratified quantiles so the draw is exact in distribution. So a probability from nine shots is less
/// knowable than one from ninety, and the interval says so: it is widened by the answer worked out again at both ends of sigma's 95 percent
/// interval, on the same random numbers, and by 1.96 standard errors of the simulation.
/// </para>
/// <para>
/// <b>The velocity's share.</b> A group measured at a distance already holds the muzzle velocity's spread in its vertical, so that share is
/// taken out in quadrature before the velocity is drawn per shot, as <see cref="Projection"/> does; a velocity SD larger than the group
/// allows is refused.
/// </para>
/// <para>
/// <b>The second round.</b> After the first shot the shooter sees its impact and dials off the whole of the miss. That removes every
/// per-string error, but the correction also holds the first shot's own dispersion and velocity, because nobody can tell which part of a
/// miss was the wind and which the rifle. So the second shot lands at its own per-shot error less the first's, and a model that forgets the
/// second term overstates the second-round figure.
/// </para>
/// <para>
/// <b>Repeatable.</b> Every run draws from xoshiro256** seeded by <see cref="HitSetup.Seed"/>, in a fixed order whether or not a source is
/// in use, so a seed gives the same answer and removing a source changes nothing else. That is what makes the cost of each source a clean
/// difference.
/// </para>
/// </summary>
public static class HitProbability
{
    public const int DefaultTrials = 10000;

    public const ulong DefaultSeed = 20260924;

    /// <summary>A refusal's threshold: the sigma's interval alone moving the answer across more than half the scale says nothing.</summary>
    public const double WidestUseful = 0.5;

    /// <summary>The most shots a refusal will name; past it the sentence says more than this.</summary>
    public const int MostShotsNamed = 400;

    private const int StrataCount = 1000;

    private const int ScatterPoints = 1500;

    /// <summary>The sources drawn for every shot.</summary>
    public static bool PerShot(HitSource source) => source is HitSource.Dispersion or HitSource.Velocity;

    private static readonly HitSource[] StringSources =
        [HitSource.Wind, HitSource.Range, HitSource.Drag, HitSource.Temperature, HitSource.Pressure, HitSource.Humidity, HitSource.Inclination, HitSource.Azimuth, HitSource.Latitude];

    /// <summary>Works the answer out, or refuses with the reason.</summary>
    public static HitAnswer Work(HitSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        Check(setup);
        var model = Model.Build(setup, setup.DistanceYards, out string? refusal);
        var notIncluded = NotIncluded(setup);
        if (model is null)
        {
            return Empty(setup, notIncluded, refusal!);
        }

        var scatterFirst = new List<PointD>();
        var scatterSecond = new List<PointD>();
        var spread = new double[Enum.GetValues<HitSource>().Length * 4];
        var basis = Run(model, setup, Sigma.Drawn, null, setup.Trials, scatterFirst, scatterSecond, spread);
        var (lower, upper) = Ends(model, setup, setup.Trials);
        HitChance Chance(Func<Tally, (double Mean, double Se)> pick)
        {
            var (p, se) = pick(basis);
            double a = lower is null ? p : pick(lower.Value).Mean, b = upper is null ? p : pick(upper.Value).Mean;
            double half = 1.96 * se;
            return new HitChance(p, Math.Clamp(Math.Min(p, Math.Min(a, b)) - half, 0, 1), Math.Clamp(Math.Max(p, Math.Max(a, b)) + half, 0, 1), Math.Abs(a - b) / 2, half);
        }

        var first = Chance(t => t.First);
        var costs = new List<HitCost>();
        foreach (var source in Enum.GetValues<HitSource>())
        {
            if (!model.Uses(source))
            {
                continue;
            }

            var without = Run(model, setup, Sigma.Drawn, source, setup.Trials, null, null, null);
            int i = (int)source * 4;
            double Sd(int at) => Math.Sqrt(Math.Max(0, (spread[at + 1] / setup.Trials) - Math.Pow(spread[at] / setup.Trials, 2)));
            costs.Add(new HitCost(source, PerShot(source), without.First.Mean - basis.First.Mean, Sd(i), Sd(i + 2)));
        }

        costs.Sort((a, b) => b.Cost.CompareTo(a.Cost));
        var answer = new HitAnswer(
            first,
            Chance(t => t.Second),
            Chance(t => t.Any),
            Chance(t => t.Share),
            setup.ShotsInString,
            scatterFirst,
            scatterSecond,
            costs,
            basis.UpDownSd,
            basis.AcrossSd,
            setup.Trials,
            setup.Seed,
            notIncluded);
        if (setup.Precision.DegreesOfFreedom is { } df && lower is { } lo && upper is { } up && Math.Abs(lo.First.Mean - up.First.Mean) > WidestUseful)
        {
            int? needed = ShotsNeeded(model, setup, df);
            string shots = needed is { } n ? n.ToString(CultureInfo.InvariantCulture) + " shots" : "more than " + MostShotsNamed.ToString(CultureInfo.InvariantCulture) + " shots";
            return answer with
            {
                Refusal = string.Create(CultureInfo.InvariantCulture,
                    $"The group behind this precision is too small to say: the chance of a hit could lie anywhere from {100 * Math.Min(lo.First.Mean, up.First.Mean):0} to {100 * Math.Max(lo.First.Mean, up.First.Mean):0} percent across its sigma's interval alone. About {shots} in one group would narrow that to half the scale or less."),
                ShotsNeeded = needed,
            };
        }

        return answer;
    }

    /// <summary>Hit probability against distance, with the interval as a band, at fewer trials than the answer so it is quick.</summary>
    public static IReadOnlyList<HitCurvePoint> Curve(HitSetup setup, IReadOnlyList<double> distancesYards, int trials = 2000)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(distancesYards);
        Check(setup);
        var points = new List<HitCurvePoint>();
        foreach (double yards in distancesYards)
        {
            var at = setup with { DistanceYards = yards };
            var model = Model.Build(at, yards, out _);
            if (model is null)
            {
                continue;
            }

            var basis = Run(model, at, Sigma.Drawn, null, trials, null, null, null);
            var (lower, upper) = Ends(model, at, trials);
            double p = basis.First.Mean, half = 1.96 * basis.First.Se;
            double a = lower?.First.Mean ?? p, b = upper?.First.Mean ?? p;
            points.Add(new HitCurvePoint(yards, p, Math.Clamp(Math.Min(p, Math.Min(a, b)) - half, 0, 1), Math.Clamp(Math.Max(p, Math.Max(a, b)) + half, 0, 1)));
        }

        return points;
    }

    /// <summary>
    /// A probability as a person reads it: two significant figures at most, and never a digit finer than the simulation supports at its
    /// trial count, <paramref name="supportHalfWidth"/> being 1.96 standard errors. Near the ends it says "more than 99" or "under" rather than
    /// print a certainty the model cannot have.
    /// </summary>
    public static string Percent(double p, double supportHalfWidth)
    {
        double percent = p * 100;
        int decimals = Decimals(p, supportHalfWidth);
        string format = Format(decimals);
        if (percent > 99 && Math.Round(percent, decimals) >= 99.5)
        {
            return "more than 99";
        }

        double floor = Math.Pow(10, -decimals);
        if (Math.Round(percent, decimals) < floor)
        {
            return "under " + floor.ToString(format, CultureInfo.InvariantCulture);
        }

        return percent.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A chance and its interval as a person reads them: the value as <see cref="Percent"/> writes it, and the ends at the same decimals,
    /// rounded outward, so an interval never reads narrower than it is and never "100 to 100".
    /// </summary>
    public static (string Value, string Lower, string Upper) Percents(HitChance chance)
    {
        ArgumentNullException.ThrowIfNull(chance);
        int decimals = Decimals(chance.Value, chance.MonteCarloHalfWidth);
        double scale = Math.Pow(10, decimals);
        double lower = Math.Clamp(Math.Floor((chance.Lower * 100 * scale) + 1e-9) / scale, 0, 100);
        double upper = Math.Clamp(Math.Ceiling((chance.Upper * 100 * scale) - 1e-9) / scale, 0, 100);
        return (Percent(chance.Value, chance.MonteCarloHalfWidth), lower.ToString(Format(decimals), CultureInfo.InvariantCulture), upper.ToString(Format(decimals), CultureInfo.InvariantCulture));
    }

    /// <summary>Two significant figures at most, and no finer than the leading digit of the simulation's 1.96 standard errors.</summary>
    private static int Decimals(double p, double supportHalfWidth)
    {
        double percent = p * 100, support = Math.Max(supportHalfWidth * 100, 1e-6);
        int significant = percent >= 10 ? 0 : percent >= 1 ? 1 : 2;
        int supported = Math.Max(0, -(int)Math.Floor(Math.Log10(support)));
        return Math.Min(significant, supported);
    }

    private static string Format(int decimals) => decimals == 0 ? "0" : "0." + new string('0', decimals);

    private static void Check(HitSetup setup)
    {
        if (setup.Trials is < 100 or > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(setup), "The trials must be between 100 and a million.");
        }

        if (setup.ShotsInString is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(setup), "A string is between 1 and 50 shots.");
        }

        if (setup.DistanceYards <= 0 || setup.Target.WidthInches <= 0 || setup.Target.HeightInches <= 0 || setup.Precision.SigmaMrad < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(setup), "The distance, the target's size and the precision must be positive.");
        }
    }

    private static IReadOnlyList<string> NotIncluded(HitSetup setup)
    {
        var list = new List<string>();
        if (setup.Solver.LatitudeDegrees is null)
        {
            list.Add("The Earth's rotation is left out, because no latitude is given.");
        }

        if (setup.Precision.DegreesOfFreedom is null)
        {
            list.Add("The precision was typed, so its own uncertainty is not known and is left out of the interval.");
        }

        if (setup.Precision.FromYards is null && Error(setup, HitSource.Velocity).Sd > 0)
        {
            list.Add("The precision was typed, so it is taken to hold none of the velocity's spread, which is added on top.");
        }

        return list;
    }

    private static HitUncertainty Error(HitSetup setup, HitSource source) => setup.Errors.TryGetValue(source, out var u) ? u : HitUncertainty.None;

    private static HitAnswer Empty(HitSetup setup, IReadOnlyList<string> notIncluded, string refusal)
    {
        var none = new HitChance(0, 0, 0, 0, 0);
        return new HitAnswer(none, none, none, none, setup.ShotsInString, [], [], [], 0, 0, setup.Trials, setup.Seed, notIncluded, refusal);
    }

    /// <summary>The answer at both ends of sigma's interval, on the same random numbers, or nulls where the precision was typed.</summary>
    private static (Tally? Lower, Tally? Upper) Ends(Model model, HitSetup setup, int trials)
    {
        if (setup.Precision.DegreesOfFreedom is null)
        {
            return (null, null);
        }

        return (Run(model, setup, Sigma.Lower, null, trials, null, null, null), Run(model, setup, Sigma.Upper, null, trials, null, null, null));
    }

    /// <summary>The fewest shots in one group whose sigma interval would move the first-round answer across half the scale or less.</summary>
    private static int? ShotsNeeded(Model model, HitSetup setup, double df)
    {
        const int trials = 2000;
        double Width(int n)
        {
            var (lo, hi) = SigmaMultiples(2.0 * (n - 1));
            var low = Run(model with { Fixed = model.SigmaMrad * lo }, setup, Sigma.Fixed, null, trials, null, null, null);
            var high = Run(model with { Fixed = model.SigmaMrad * hi }, setup, Sigma.Fixed, null, trials, null, null, null);
            return Math.Abs(low.First.Mean - high.First.Mean);
        }

        int from = Math.Max(3, (int)Math.Ceiling((df / 2) + 1) + 1);
        if (Width(MostShotsNamed) > WidestUseful)
        {
            return null;
        }

        int a = from, b = MostShotsNamed;
        while (a < b)
        {
            int mid = (a + b) / 2;
            if (Width(mid) > WidestUseful)
            {
                a = mid + 1;
            }
            else
            {
                b = mid;
            }
        }

        return a;
    }

    /// <summary>Sigma's 95 percent interval as multiples of the estimate, on df degrees of freedom, docs/STATISTICS.md section 9.1.</summary>
    public static (double Lower, double Upper) SigmaMultiples(double df) =>
        (Math.Sqrt(df / Distributions.ChiSquareQuantile(0.975, df)), Math.Sqrt(df / Distributions.ChiSquareQuantile(0.025, df)));

    private enum Sigma
    {
        Drawn,
        Lower,
        Upper,
        Fixed,
    }

    private readonly record struct Tally((double Mean, double Se) First, (double Mean, double Se) Second, (double Mean, double Se) Any, (double Mean, double Se) Share, double UpDownSd, double AcrossSd);

    private static Tally Run(Model m, HitSetup setup, Sigma mode, HitSource? without, int trials, List<PointD>? scatterFirst, List<PointD>? scatterSecond, double[]? spread)
    {
        var rng = new Xoshiro(setup.Seed);
        int shots = Math.Max(2, setup.ShotsInString);
        double inches = setup.DistanceYards * 36 / 1000;
        double first = 0, second = 0, any = 0, share = 0, shareSq = 0, v = 0, vv = 0, h = 0, hh = 0;
        var stringSources = StringSources;
        for (int t = 0; t < trials; t++)
        {
            double sigma = mode switch
            {
                Sigma.Lower => m.SigmaMrad * m.LowerMultiple,
                Sigma.Upper => m.SigmaMrad * m.UpperMultiple,
                Sigma.Fixed => m.Fixed,
                _ => m.Strata is { } strata ? strata[t % strata.Length] : m.SigmaMrad,
            };
            double sigmaUp = Math.Sqrt(Math.Max(0, (sigma * sigma) - (m.VelocityShareMrad * m.VelocityShareMrad)));
            bool dispersion = without != HitSource.Dispersion, velocity = without != HitSource.Velocity;

            // The string's own errors, drawn in a fixed order whether or not each is in use.
            double sv = 0, sh = 0;
            foreach (var source in stringSources)
            {
                double z = rng.Normal();
                if (without != source && m.Responses[(int)source] is { } r)
                {
                    var (dv, dh) = r.At(r.Bias + (r.Sd * z));
                    sv += dv;
                    sh += dh;
                    Spread(spread, source, dv, dh);
                }
            }

            double zv = rng.Normal(), zh = rng.Normal();
            if (without != HitSource.Zero && m.ZeroMrad > 0)
            {
                sv += m.ZeroMrad * inches * zv;
                sh += m.ZeroMrad * inches * zh;
                Spread(spread, HitSource.Zero, m.ZeroMrad * inches * zv, m.ZeroMrad * inches * zh);
            }

            int hits = 0;
            double firstV = 0, firstH = 0;
            for (int j = 0; j < shots; j++)
            {
                double za = rng.Normal(), zb = rng.Normal(), zc = rng.Normal();
                double pv = dispersion ? sigmaUp * inches * zb : 0, ph = dispersion ? sigma * inches * za : 0;
                double qv = 0, qh = 0;
                if (velocity && m.Responses[(int)HitSource.Velocity] is { } vr)
                {
                    (qv, qh) = vr.At(vr.Bias + (vr.Sd * zc));
                }

                double iv = sv + pv + qv, ih = sh + ph + qh;
                if (j == 0)
                {
                    firstV = pv + qv;
                    firstH = ph + qh;
                    Spread(spread, HitSource.Dispersion, pv, ph);
                    Spread(spread, HitSource.Velocity, qv, qh);
                    bool hit = setup.Target.Contains(ih, iv);
                    first += hit ? 1 : 0;
                    v += iv;
                    vv += iv * iv;
                    h += ih;
                    hh += ih * ih;
                    if (scatterFirst is not null && scatterFirst.Count < ScatterPoints)
                    {
                        scatterFirst.Add(new PointD(ih, iv));
                    }
                }
                else if (j == 1)
                {
                    // Dialed off the first shot's whole miss: the string's errors cancel, the first shot's own do not.
                    double cv = pv + qv - firstV, ch = ph + qh - firstH;
                    second += setup.Target.Contains(ch, cv) ? 1 : 0;
                    if (scatterSecond is not null && scatterSecond.Count < ScatterPoints)
                    {
                        scatterSecond.Add(new PointD(ch, cv));
                    }
                }

                if (j < setup.ShotsInString && setup.Target.Contains(ih, iv))
                {
                    hits++;
                }
            }

            any += hits > 0 ? 1 : 0;
            double s = (double)hits / setup.ShotsInString;
            share += s;
            shareSq += s * s;
        }

        (double, double) Binary(double count)
        {
            double p = count / trials;
            return (p, Math.Sqrt(p * (1 - p) / trials));
        }

        double mean = share / trials;
        return new Tally(
            Binary(first),
            Binary(second),
            Binary(any),
            (mean, Math.Sqrt(Math.Max(0, (shareSq / trials) - (mean * mean)) / trials)),
            Math.Sqrt(Math.Max(0, (vv / trials) - Math.Pow(v / trials, 2))),
            Math.Sqrt(Math.Max(0, (hh / trials) - Math.Pow(h / trials, 2))));
    }

    private static void Spread(double[]? spread, HitSource source, double dv, double dh)
    {
        if (spread is null)
        {
            return;
        }

        int i = (int)source * 4;
        spread[i] += dv;
        spread[i + 1] += dv * dv;
        spread[i + 2] += dh;
        spread[i + 3] += dh * dh;
    }

    /// <summary>A source's effect on the impact against the draw, up and across in inches: a quadratic through the solver either side.</summary>
    private sealed record Response(double Sd, double Bias, double UpA, double UpB, double AcrossA, double AcrossB)
    {
        public (double Up, double Across) At(double delta) => ((UpA * delta) + (UpB * delta * delta), (AcrossA * delta) + (AcrossB * delta * delta));
    }

    /// <summary>Everything a run needs at one distance, found once from the solver.</summary>
    private sealed record Model(Response?[] Responses, double SigmaMrad, double[]? Strata, double LowerMultiple, double UpperMultiple, double VelocityShareMrad, double ZeroMrad)
    {
        public double Fixed { get; init; }

        public bool Uses(HitSource source) => source switch
        {
            HitSource.Dispersion => SigmaMrad > 0,
            HitSource.Zero => ZeroMrad > 0,
            _ => Responses[(int)source] is not null,
        };

        public static Model? Build(HitSetup setup, double yards, out string? refusal)
        {
            refusal = null;
            var believed = setup.Solver with
            {
                LaunchAngleMoa = setup.Solver.LaunchAngleMoa ?? BallisticSolver.Solve(setup.Solver, setup.Solver.ZeroRangeYards, setup.Solver.ZeroRangeYards).ZeroAngleMoa,
            };
            (double Up, double Across) Impact(BallisticInput input, double at)
            {
                var p = BallisticSolver.Solve(input, at, at).Points[^1];
                return (p.DropInches + (p.CoriolisVerticalInches ?? 0), p.WindInches + (p.CoriolisInches ?? 0));
            }

            var basis = Impact(believed, yards);
            (double Up, double Across) Delta(HitSource source, double d)
            {
                if (source == HitSource.Range)
                {
                    // The dial is set for the believed range and the target stands at another: the hold scales with range, the path does not.
                    var (up, across) = Impact(believed, yards + d);
                    double scale = (yards + d) / yards;
                    return (up - (basis.Up * scale), across - (basis.Across * scale));
                }

                var moved = Impact(Perturb(believed, source, d), yards);
                return (moved.Up - basis.Up, moved.Across - basis.Across);
            }

            var responses = new Response?[Enum.GetValues<HitSource>().Length];
            foreach (var source in StringSources.Append(HitSource.Velocity))
            {
                var u = Error(setup, source);
                if (!u.Active || (source is HitSource.Azimuth or HitSource.Latitude && believed.LatitudeDegrees is null))
                {
                    continue;
                }

                double h = Math.Abs(u.Bias) + (2 * u.Sd);
                h = source switch
                {
                    HitSource.Range => Math.Min(h, yards / 2),
                    HitSource.Drag => Math.Min(h, 50),
                    _ => h,
                };
                var plus = Delta(source, h);
                var minus = Delta(source, -h);
                responses[(int)source] = new Response(
                    u.Sd,
                    u.Bias,
                    (plus.Up - minus.Up) / (2 * h),
                    (plus.Up + minus.Up) / (2 * h * h),
                    (plus.Across - minus.Across) / (2 * h),
                    (plus.Across + minus.Across) / (2 * h * h));
            }

            double velocityShare = 0;
            double velocitySd = Error(setup, HitSource.Velocity).Sd;
            if (setup.Precision.FromYards is { } from && velocitySd > 0)
            {
                velocityShare = HitPrecision.MradFromInches(Math.Abs(Projection.DropPerFps(setup.Solver, from)) * velocitySd, from);
                if (velocityShare >= setup.Precision.SigmaMrad)
                {
                    refusal = string.Create(CultureInfo.InvariantCulture,
                        $"The muzzle velocity's spread alone would make the group {velocityShare:0.000} mrad up and down where it was shot, as much as or more than the {setup.Precision.SigmaMrad:0.000} mrad measured, so the velocity SD is too large for this group. Check it first.");
                    return null;
                }
            }

            double[]? strata = null;
            double lower = 1, upper = 1;
            if (setup.Precision.DegreesOfFreedom is { } df)
            {
                strata = new double[StrataCount];
                for (int i = 0; i < StrataCount; i++)
                {
                    strata[i] = setup.Precision.SigmaMrad * Math.Sqrt(df / Distributions.ChiSquareQuantile((i + 0.5) / StrataCount, df));
                }

                // Spread the strata through the trials, so a short run still sees the whole distribution.
                var order = new Xoshiro(setup.Seed ^ 0x9E3779B97F4A7C15UL);
                for (int i = StrataCount - 1; i > 0; i--)
                {
                    int j = (int)(order.Next() % (ulong)(i + 1));
                    (strata[i], strata[j]) = (strata[j], strata[i]);
                }

                (lower, upper) = SigmaMultiples(df);
            }

            return new Model(responses, setup.Precision.SigmaMrad, strata, lower, upper, velocityShare, Error(setup, HitSource.Zero).Sd);
        }

        private static BallisticInput Perturb(BallisticInput input, HitSource source, double d) => source switch
        {
            HitSource.Velocity => input with { MuzzleVelocityFps = input.MuzzleVelocityFps + d },
            HitSource.Wind => input with { CrosswindMph = input.CrosswindMph + d },
            HitSource.Drag => input with { BallisticCoefficient = input.BallisticCoefficient / (1 + (d / 100)) },
            HitSource.Temperature => input with { TemperatureF = input.TemperatureF + d },
            HitSource.Pressure => input with { PressureInHg = input.StationPressureInHg + d },
            HitSource.Humidity => input with { HumidityPct = Math.Clamp(input.HumidityPct + d, 0, 100) },
            HitSource.Inclination => input with { AngleDegrees = input.AngleDegrees + d },
            HitSource.Azimuth => input with { AzimuthDegrees = input.AzimuthDegrees + d },
            HitSource.Latitude => input with { LatitudeDegrees = input.LatitudeDegrees + d },
            _ => input,
        };
    }

    /// <summary>xoshiro256** seeded by SplitMix64, with normal deviates in pairs by Marsaglia's polar method.</summary>
    private struct Xoshiro
    {
        private ulong s0, s1, s2, s3;
        private double spare;
        private bool hasSpare;

        public Xoshiro(ulong seed)
        {
            ulong state = seed;
            s0 = SplitMix(ref state);
            s1 = SplitMix(ref state);
            s2 = SplitMix(ref state);
            s3 = SplitMix(ref state);
        }

        private static ulong SplitMix(ref ulong state)
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        public ulong Next()
        {
            ulong result = ulong.RotateLeft(s1 * 5, 7) * 9, t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = ulong.RotateLeft(s3, 45);
            return result;
        }

        public double Normal()
        {
            if (hasSpare)
            {
                hasSpare = false;
                return spare;
            }

            while (true)
            {
                double u = ((Next() >> 11) * (1.0 / (1UL << 53)) * 2) - 1, v = ((Next() >> 11) * (1.0 / (1UL << 53)) * 2) - 1, s = (u * u) + (v * v);
                if (s > 0 && s < 1)
                {
                    double f = Math.Sqrt(-2 * Math.Log(s) / s);
                    spare = v * f;
                    hasSpare = true;
                    return u * f;
                }
            }
        }
    }
}
