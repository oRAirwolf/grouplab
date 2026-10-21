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
}
