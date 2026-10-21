using System.Globalization;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 462 section 2: the figures three research articles quote, worked out by GroupLab's own code and held here so
/// an article cannot drift from what it says it measured. Every simulation is seeded (2026) and reruns to the same numbers; the load is the
/// point blank article's (140 gr, 2710 ft/s, G7 0.326, sight 1.75 in, zero 100 yd, standard air).
/// </summary>
public class ArticleFiguresTests(ITestOutputHelper output)
{
    private static readonly BallisticInput Load = new(0.326, DragModel.G7, 2710, 140, SightHeightInches: 1.75, ZeroRangeYards: 100);

    private const double MoaRadians = Math.PI / 180 / 60;

    private void Say(FormattableString what) => output.WriteLine(what.ToString(CultureInfo.InvariantCulture));

    private static double Normal(Random random) => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());

    private static double FiveShotSpread(Random random)
    {
        var x = new double[5];
        var y = new double[5];
        for (int i = 0; i < 5; i++)
        {
            x[i] = Normal(random);
            y[i] = Normal(random);
        }

        double most = 0;
        for (int i = 0; i < 5; i++)
        {
            for (int j = i + 1; j < 5; j++)
            {
                most = Math.Max(most, Math.Sqrt(((x[i] - x[j]) * (x[i] - x[j])) + ((y[i] - y[j]) * (y[i] - y[j]))));
            }
        }

        return most;
    }

    /// <summary>
    /// "Does your rifle group as well as Top Gun says it should?": how far one, two, three or five five-shot groups wander from the average
    /// they estimate (10th to 90th percentile), the interval GroupLab gives a sigma from n shots on Top Gun's basis, how often a rifle exactly
    /// on the trend is called clearly wider, and how often one 25 percent wider is; and the trend for the article's example rifles.
    /// </summary>
    [Fact]
    public void TopGunsArticleFiguresHold()
    {
        double mean = RangeStatisticsTable.Default.Lookup(RangeStatistic.ExtremeSpread, TopGun.GroupShots)!.Value.Mean;
        Assert.InRange(mean, 3.06, 3.07);

        var random = new Random(2026);
        foreach (var (groups, p10, p90) in new[] { (1, 0.67, 1.36), (2, 0.76, 1.25), (3, 0.80, 1.20), (5, 0.85, 1.16) })
        {
            var averages = Enumerable.Range(0, 20000).Select(_ => Enumerable.Range(0, groups).Average(_ => FiveShotSpread(random)) / mean).Order().ToList();
            Say($"{groups} groups: 10th {averages[2000]:0.00}, 90th {averages[18000]:0.00}");
            Assert.InRange(averages[2000], p10 - 0.015, p10 + 0.015);
            Assert.InRange(averages[18000], p90 - 0.015, p90 + 0.015);
        }

        foreach (var (shots, lower, upper, caught) in new[] { (5, 0.68, 1.92, 0.25), (10, 0.76, 1.48, 0.35), (20, 0.82, 1.29, 0.57), (30, 0.85, 1.22, 0.73), (50, 0.88, 1.16, 0.90) })
        {
            int falseWider = 0, wider = 0;
            const int Trials = 4000;
            var one = default(Estimate);
            for (int t = 0; t < Trials; t++)
            {
                var offsets = Enumerable.Range(0, shots).Select(_ => new PointD(Normal(random), Normal(random))).ToList();
                var sigma = GroupStatistics.Rayleigh(offsets).Sigma;
                one = new Estimate(sigma.Value, sigma.Lower, sigma.Upper);
                falseWider += sigma.Lower * mean > mean ? 1 : 0;
                wider += sigma.Lower * 1.25 * mean > mean ? 1 : 0;
            }

            // The interval is a fixed multiple of the estimate for a given number of shots, so any one trial gives it.
            Say($"{shots} shots: {one.Lower / one.Value:0.00} to {one.Upper / one.Value:0.00} times, at the trend called wider {100.0 * falseWider / Trials:0.0} %, 25 % wider caught {100.0 * wider / Trials:0.0} %");
            Assert.InRange(one.Lower / one.Value, lower - 0.006, lower + 0.006);
            Assert.InRange(one.Upper / one.Value, upper - 0.006, upper + 0.006);
            Assert.InRange((double)falseWider / Trials, 0.02, 0.04);
            Assert.InRange((double)wider / Trials, caught - 0.03, caught + 0.03);
        }

        Assert.InRange(TopGun.GroupMoa(140, 2700, 12), 0.935, 0.95);
        Assert.InRange(TopGun.GroupMoa(175, 2600, 11), 1.18, 1.195);
        Assert.InRange(TopGun.GroupMoa(77, 2750, 8), 0.805, 0.82);
    }

    /// <summary>
    /// "What a canted scope costs at distance": the elevation dialled, the bullet's fall from the bore line, the sideways miss and the height lost
    /// at 1, 2 and 5 degrees of cant (the fall times the sine and times one less the cosine), and how far a scope's elevation reaches with and
    /// without a 20 MOA rail (<see cref="ScopeMount.Farthest"/>).
    /// </summary>
    [Fact]
    public void TheCantArticlesFiguresHold()
    {
        var flight = BallisticSolver.Solve(Load, 1000, 100);
        Assert.InRange(flight.ZeroAngleMoa, 3.95, 4.05);
        foreach (var (yards, dial, across1, across2, across5) in new[] { (300, 4.1, 0.4, 0.8, 2.1), (500, 10.0, 1.2, 2.5, 6.2), (800, 21.0, 3.6, 7.3, 18.1), (1000, 30.2, 6.2, 12.5, 31.1) })
        {
            var row = flight.Points.Single(p => p.RangeYards == yards);
            double fall = (Math.Tan(flight.ZeroAngleMoa * MoaRadians) * yards * 36) - Load.SightHeightInches - row.DropInches;
            double Across(double degrees) => fall * Math.Sin(degrees * Math.PI / 180);
            Say($"{yards} yd: dial {row.DropMoa:0.0} MOA, fall {fall:0.0} in, across {Across(1):0.0} {Across(2):0.0} {Across(5):0.0} in, 5 degrees lose {fall * (1 - Math.Cos(5 * Math.PI / 180)):0.00} in");
            Assert.InRange(row.DropMoa, dial - 0.06, dial + 0.06);
            Assert.InRange(Across(1), across1 - 0.06, across1 + 0.06);
            Assert.InRange(Across(2), across2 - 0.06, across2 + 0.06);
            Assert.InRange(Across(5), across5 - 0.06, across5 + 0.06);
        }

        var far = BallisticSolver.Solve(Load, ScopeMount.TravelLooked, 10);
        foreach (var (travel, rail, reach) in new[] { (50.0, 0.0, 790.0), (60, 0, 910), (50, 20, 1190), (60, 20, 1270) })
        {
            Assert.Equal(reach, ScopeMount.Farthest(far, (travel / 2) + rail - flight.ZeroAngleMoa));
        }
    }

    /// <summary>
    /// "When a click is not what it says": GroupLab's hit probability for a centred shot at a 10 in circle with a 0.63 MOA mean radius, and the
    /// same shots moved by a tracking error of 1, 2 and 4 percent of the elevation dialled at 600, 800 and 1000 yd.
    /// </summary>
    [Fact]
    public void TheTrackingArticlesFiguresHold()
    {
        var flight = BallisticSolver.Solve(Load, 1000, 100);
        double sigmaMoa = 0.63 / Math.Sqrt(Math.PI / 2);
        var random = new Random(2026);
        foreach (var (yards, centred, at1, at2, at4) in new[] { (600, 0.715, 0.703, 0.665, 0.533), (800, 0.507, 0.476, 0.397, 0.189), (1000, 0.364, 0.314, 0.204, 0.035) })
        {
            var row = flight.Points.Single(p => p.RangeYards == yards);
            double inchesPerMoa = Math.Tan(MoaRadians) * yards * 36, sigma = sigmaMoa * inchesPerMoa;
            var setup = new HitSetup(Load, yards, HitTarget.Circle(10), new HitPrecision(HitPrecision.MradFromMoa(sigmaMoa), null, null), new Dictionary<HitSource, HitUncertainty>());
            Assert.InRange(HitProbability.Work(setup).FirstRound.Value, centred - 0.01, centred + 0.01);

            double Chance(double percent)
            {
                double off = percent / 100 * row.DropMoa * inchesPerMoa;
                int hits = 0;
                const int Trials = 200000;
                for (int i = 0; i < Trials; i++)
                {
                    double x = sigma * Normal(random), y = (sigma * Normal(random)) + off;
                    hits += (x * x) + (y * y) <= 25 ? 1 : 0;
                }

                return (double)hits / Trials;
            }

            var (c0, c1, c2, c4) = (Chance(0), Chance(1), Chance(2), Chance(4));
            Say($"{yards} yd: dial {row.DropMoa:0.0} MOA, 1 % is {0.01 * row.DropMoa * inchesPerMoa:0.0} in; {c0:0.000} {c1:0.000} {c2:0.000} {c4:0.000}");
            Assert.InRange(c0, centred - 0.01, centred + 0.01);
            Assert.InRange(c1, at1 - 0.01, at1 + 0.01);
            Assert.InRange(c2, at2 - 0.01, at2 + 0.01);
            Assert.InRange(c4, at4 - 0.01, at4 + 0.01);
        }
    }

    /// <summary>
    /// Entry 464 section 3, "How much of your vertical is your velocity?": the vertical SD a velocity SD of 5, 10 and 15 ft/s adds at each
    /// distance (<see cref="Projection.DropPerFps"/>, the sight left where the mean velocity zeroes it), the share of the vertical it is for two
    /// rifles whose own sigma is 0.25 and 0.5 MOA, the same at 90 F and 6000 ft, and how wide the share and the SD measured from 10 and 20
    /// shots are at GroupLab's 90 percent (<see cref="VelocityVertical.Analyze"/>'s F and chi-squared intervals).
    /// </summary>
    [Fact]
    public void TheVelocityArticlesFiguresHold()
    {
        var hot = Load with { TemperatureF = 90, AltitudeFt = 6000 };
        foreach (var (yards, perFps, sd5, sd10, sd15, share25, share50, own25, total25) in new[]
        {
            (100, 0.0018, 0.01, 0.02, 0.03, 0.005, 0.001, 0.26, 0.26),
            (300, 0.0180, 0.09, 0.18, 0.27, 0.050, 0.013, 0.79, 0.81),
            (500, 0.0551, 0.28, 0.55, 0.83, 0.151, 0.042, 1.31, 1.42),
            (800, 0.1658, 0.83, 1.66, 2.49, 0.385, 0.136, 2.09, 2.67),
            (1000, 0.2926, 1.46, 2.93, 4.39, 0.555, 0.238, 2.62, 3.93),
        })
        {
            double k = Math.Abs(Projection.DropPerFps(Load, yards)), inchesPerMoa = Math.Tan(MoaRadians) * yards * 36;
            double Share(double sigmaMoa) => Math.Pow(10 * k, 2) / (Math.Pow(10 * k, 2) + Math.Pow(sigmaMoa * inchesPerMoa, 2));
            Say($"{yards} yd: {k:0.0000} in per ft/s, SD 5 10 15 add {5 * k:0.00} {10 * k:0.00} {15 * k:0.00} in; share {Share(0.25):0.0000} {Share(0.5):0.0000}; vertical {Math.Sqrt(Math.Pow(10 * k, 2) + Math.Pow(0.25 * inchesPerMoa, 2)):0.00} in");
            Assert.InRange(k, perFps - 0.00006, perFps + 0.00006);
            Assert.InRange(5 * k, sd5 - 0.006, sd5 + 0.006);
            Assert.InRange(10 * k, sd10 - 0.006, sd10 + 0.006);
            Assert.InRange(15 * k, sd15 - 0.006, sd15 + 0.006);
            Assert.InRange(Share(0.25), share25 - 0.002, share25 + 0.002);
            Assert.InRange(Share(0.5), share50 - 0.002, share50 + 0.002);
            Assert.InRange(0.25 * inchesPerMoa, own25 - 0.006, own25 + 0.006);
            Assert.InRange(Math.Sqrt(Math.Pow(10 * k, 2) + Math.Pow(0.25 * inchesPerMoa, 2)), total25 - 0.006, total25 + 0.006);
        }

        // At 1000 yd the bullet is in the air about a second and a half and has lost close to half its speed.
        var far = BallisticSolver.Solve(Load, 1000, 1000).Points[^1];
        Say($"1000 yd: {far.TimeOfFlight:0.000} s, {far.VelocityFps:0} ft/s");
        Assert.InRange(far.TimeOfFlight, 1.45, 1.55);
        Assert.InRange(far.VelocityFps, 1450, 1530);

        // A five-shot group's height, top to bottom, is on average about 2.3 times its vertical SD (the expected range of five normals, 2.326).
        var heights = new Random(2026);
        double height = Enumerable.Range(0, 200000).Average(_ =>
        {
            var five = Enumerable.Range(0, 5).Select(_ => Normal(heights)).ToList();
            return five.Max() - five.Min();
        });
        Say($"five shots: height {height:0.000} times the SD");
        Assert.InRange(height, 2.31, 2.34);

        Assert.InRange(10 * Math.Abs(Projection.DropPerFps(hot, 1000)), 2.525, 2.54);
        Assert.InRange(10 * Math.Abs(Projection.DropPerFps(hot, 800)), 1.485, 1.495);

        // A rifle's sigma of 0.25 and 0.5 MOA is an average five-shot group of about 0.77 and 1.5 MOA.
        double fiveShot = RangeStatisticsTable.Default.Lookup(RangeStatistic.ExtremeSpread, 5)!.Value.Mean;
        Assert.InRange(0.25 * fiveShot, 0.76, 0.77);
        Assert.InRange(0.5 * fiveShot, 1.52, 1.54);

        // The share's interval from n readings and n shots is the share divided and multiplied by F's 95th percentile; the SD's from n readings.
        foreach (var (n, factor, lowAt30, highAt30, sdLow, sdHigh) in new[] { (10, 3.18, 0.094, 0.954, 7.3, 16.5), (20, 2.17, 0.138, 0.650, 7.9, 13.7) })
        {
            double f = Distributions.FQuantile(0.95, n - 1, n - 1);
            var (low, high) = GroupLab.Core.Records.Chronograph.SdInterval(n, 10, GroupLab.Core.Marking.VelocityBlocks.Confidence)!.Value;
            Say($"{n} shots: share times or over {f:0.000}, 30 % is {0.3 / f:0.000} to {0.3 * f:0.000}; SD 10 is {low:0.0} to {high:0.0}");
            Assert.InRange(f, factor - 0.006, factor + 0.006);
            Assert.InRange(0.3 / f, lowAt30 - 0.0015, lowAt30 + 0.0015);
            Assert.InRange(0.3 * f, highAt30 - 0.0015, highAt30 + 0.0015);
            Assert.InRange(low, sdLow - 0.06, sdLow + 0.06);
            Assert.InRange(high, sdHigh - 0.06, sdHigh + 0.06);
        }
    }

    /// <summary>
    /// Entry 464 section 3, "Where a first-round miss comes from": GroupLab's hit probability (seeded, <see cref="HitProbability.DefaultSeed"/>)
    /// on a 10 in circle for a rifle of 0.63 MOA mean radius measured at 100 yd and a velocity SD of 10 ft/s, at 300, 600 and 1000 yd under each
    /// of the three confidence presets; the first and second round, and the two sources that cost the most first-round hits.
    /// </summary>
    [Fact]
    public void TheMissArticlesFiguresHold()
    {
        double sigmaMrad = HitPrecision.MradFromMoa(0.63 / Math.Sqrt(Math.PI / 2));

        // The rifle averages about 1.5 MOA for five shots; the presets' wind errors are 1.5, 3 and 4 mph, and the guessed distance 5 percent.
        Assert.InRange(0.63 / Math.Sqrt(Math.PI / 2) * RangeStatisticsTable.Default.Lookup(RangeStatistic.ExtremeSpread, 5)!.Value.Mean, 1.5, 1.56);
        Assert.Equal([1.5, 3, 4], HitPresets.All.Select(p => p.Errors(600)[HitSource.Wind].Sd));
        Assert.Equal(30, HitPresets.All[2].Errors(600)[HitSource.Range].Sd, 9);
        HitAnswer Work(int yards, IReadOnlyDictionary<HitSource, HitUncertainty> preset)
        {
            var errors = new Dictionary<HitSource, HitUncertainty>(preset) { [HitSource.Velocity] = new(10) };
            return HitProbability.Work(new HitSetup(Load, yards, HitTarget.Circle(10), new HitPrecision(sigmaMrad, null, 100), errors));
        }

        foreach (var (yards, alone) in new[] { (300, 0.994), (600, 0.706), (1000, 0.333) })
        {
            var answer = Work(yards, new Dictionary<HitSource, HitUncertainty>());
            Say($"{yards} yd, the rifle and its velocity alone: {answer.FirstRound.Value:0.000}");
            Assert.InRange(answer.FirstRound.Value, alone - 0.006, alone + 0.006);
        }

        var expected = new (int Preset, int Yards, double First, double Second, HitSource Most, double MostCost, HitSource Next, double NextCost)[]
        {
            (0, 300, 0.989, 0.914, HitSource.Dispersion, 0.011, HitSource.Wind, 0.005),
            (0, 600, 0.566, 0.445, HitSource.Dispersion, 0.301, HitSource.Wind, 0.136),
            (0, 1000, 0.162, 0.180, HitSource.Wind, 0.162, HitSource.Dispersion, 0.112),
            (1, 300, 0.964, 0.914, HitSource.Dispersion, 0.035, HitSource.Wind, 0.030),
            (1, 600, 0.379, 0.445, HitSource.Wind, 0.317, HitSource.Dispersion, 0.172),
            (1, 1000, 0.079, 0.180, HitSource.Wind, 0.204, HitSource.Dispersion, 0.030),
            (2, 300, 0.906, 0.914, HitSource.Dispersion, 0.075, HitSource.Wind, 0.071),
            (2, 600, 0.166, 0.445, HitSource.Wind, 0.219, HitSource.Range, 0.136),
            (2, 1000, 0.015, 0.180, HitSource.Wind, 0.063, HitSource.Range, 0.035),
        };
        foreach (var row in expected)
        {
            var answer = Work(row.Yards, HitPresets.All[row.Preset].Errors(row.Yards));
            var cost = answer.Costs.ToDictionary(c => c.Source, c => c.Cost);
            Say($"{HitPresets.All[row.Preset].Name}, {row.Yards} yd: first {answer.FirstRound.Value:0.000}, second {answer.SecondRound.Value:0.000}; {string.Join(", ", answer.Costs.Take(3).Select(c => $"{c.Source} {c.Cost:0.000}"))}; velocity {cost[HitSource.Velocity]:0.000}");
            Assert.Null(answer.Refusal);
            Assert.InRange(answer.FirstRound.Value, row.First - 0.006, row.First + 0.006);
            Assert.InRange(answer.SecondRound.Value, row.Second - 0.006, row.Second + 0.006);
            Assert.Equal(row.Most, answer.Costs[0].Source);
            Assert.Equal(row.Next, answer.Costs[1].Source);
            Assert.InRange(answer.Costs[0].Cost, row.MostCost - 0.006, row.MostCost + 0.006);
            Assert.InRange(answer.Costs[1].Cost, row.NextCost - 0.006, row.NextCost + 0.006);

            // The velocity's spread never costs as much as one hit in a hundred, and nothing in the air or the drag costs more than about one.
            Assert.True(cost[HitSource.Velocity] < 0.01);
            Assert.True(answer.Costs.Where(c => c.Source is HitSource.Drag or HitSource.Temperature or HitSource.Pressure or HitSource.Humidity or HitSource.Inclination).All(c => c.Cost < 0.012));
        }
    }
}
