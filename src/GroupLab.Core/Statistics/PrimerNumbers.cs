using System.Globalization;
using System.Text.Json;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 438 section 2.5: every number the statistics primer on grouplab.org shows, worked out by these same
/// statistics on a fixed seed. <c>grouplab primer</c> writes it to <c>docs/figures/primer/primer-data.json</c>, the site draws the primer's
/// figures from that file and does no statistics of its own, so the page can never teach a figure GroupLab would compute differently.
/// Nothing in the application reads it.
/// </summary>
public static class PrimerNumbers
{
    /// <summary>The rifle every example shoots: its typical miss, sigma, in MOA. Its five-shot groups measure about one MOA.</summary>
    public const double Sigma = 0.4;

    public const ulong Seed = 20261017;

    private static readonly int[] Sizes = [3, 5, 10, 20, 30];

    /// <summary>The file's text, the same on every run.</summary>
    public static string Json() => JsonSerializer.Serialize(Data(), new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";

    public static object Data()
    {
        var random = new StatisticsRandom(Seed);
        IReadOnlyList<PointD> Group(int n) => [.. Enumerable.Range(0, n).Select(_ => new PointD(Sigma * random.NextNormal(), Sigma * random.NextNormal()))];
        double Es(IReadOnlyList<PointD> g) => GroupGeometry.MaximumPairDistance(g).Distance;
        double Mr(IReadOnlyList<PointD> g) => GroupStatistics.Radii(g, GroupStatistics.Centre(g)).Average();
        static double R(double v) => Math.Round(v, 3);

        // Groups the page steps through when the reader presses "Shoot five more", each with the figures GroupLab gives it.
        var groups = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (int n in new[] { 5, 10, 20 })
        {
            groups[n.ToString(CultureInfo.InvariantCulture)] = Enumerable.Range(0, 40).Select(_ =>
            {
                var g = Group(n);
                var sigma = GroupStatistics.Rayleigh(g);
                return new
                {
                    shots = g.Select(p => new[] { R(p.X), R(p.Y) }).ToArray(),
                    es = R(Es(g)),
                    mr = R(Mr(g)),
                    sigma = R(sigma.Sigma.Value),
                    sigmaLow = R(sigma.Sigma.Lower),
                    sigmaHigh = R(sigma.Sigma.Upper),
                };
            }).ToArray();
        }

        // How far a group's size wanders from one group to the next, 4000 groups at each size: the middle 90 percent.
        var spread = Sizes.Select(n =>
        {
            var es = new List<double>();
            var mr = new List<double>();
            for (int i = 0; i < 4000; i++)
            {
                var g = Group(n);
                es.Add(Es(g));
                mr.Add(Mr(g));
            }

            es.Sort();
            mr.Sort();
            var (lower, upper) = SampleSize.SigmaIntervalMultiples(n);
            return new
            {
                n,
                esLow = R(es[200]), esMid = R(es[2000]), esHigh = R(es[3800]),
                mrLow = R(mr[200]), mrMid = R(mr[2000]), mrHigh = R(mr[3800]),
                sigmaLowMultiple = R(lower), sigmaHighMultiple = R(upper),
                worstInMeanRadii = R(Flyers.ExpectedWorstInMeanRadii(n)),
            };
        }).ToArray();

        // Two loads that are really the same, shot against each other again and again.
        var same = new[] { 5, 10 }.Select(n =>
        {
            int clearly = 0, significant = 0;
            var examples = new List<object>();
            for (int i = 0; i < 2000; i++)
            {
                var a = Group(n);
                var b = Group(n);
                double ea = Es(a), eb = Es(b);
                bool looks = Math.Min(ea, eb) <= 0.8 * Math.Max(ea, eb);
                double p = GroupComparison.DispersionRatio(GroupStatistics.Rayleigh(a), GroupStatistics.Rayleigh(b)).PValue;
                clearly += looks ? 1 : 0;
                significant += p < 0.05 ? 1 : 0;
                if (examples.Count < 30)
                {
                    examples.Add(new { a = a.Select(s => new[] { R(s.X), R(s.Y) }).ToArray(), b = b.Select(s => new[] { R(s.X), R(s.Y) }).ToArray(), esA = R(ea), esB = R(eb), p = R(p) });
                }
            }

            return new { n, trials = 2000, looksClearlyBetter = R(clearly / 2000.0), testSaysDifferent = R(significant / 2000.0), examples };
        }).ToArray();

        // The chance of a hit on a circle, from precision alone: a perfect zero and no wind or range error.
        double[] sigmas = [0.25, 0.5, 1.0];
        int[] yards = [100, 300, 500, 800];
        double[] circles = [2, 4, 8, 12];
        var hits = sigmas.SelectMany(s => yards.SelectMany(d => circles.Select(c => new
        {
            sigmaMoa = s,
            yards = d,
            circleInches = c,
            chance = R(GroupStatistics.HitProbabilityRayleigh(Angular.FromAngle(s, d, 36, AngularUnit.Moa), c / 2)),
        }))).ToArray();

        // "Twenty percent tighter" means the other load's sigma is 0.8 of this one's, a ratio of 1.25, as the article How many shots says.
        double[] tighter = [0.1, 0.2, 0.3, 0.5];
        var shotsPerLoad = tighter.Select(t => new { tighterPercent = (int)Math.Round(t * 100), ratio = R(1 / (1 - t)), shots = SampleSize.ShotsPerLoad(1 / (1 - t)) }).ToArray();

        return new
        {
            about = "Entry 438: every number the statistics primer shows, from GroupLab.Core on a fixed seed (grouplab primer). Angles in MOA.",
            seed = Seed,
            sigma = Sigma,
            meanRadiusFactor = R(RayleighEstimate.MeanRadiusFactor),
            cep50Factor = R(Math.Sqrt(2 * Math.Log(2))),
            groups,
            spread,
            sameLoads = same,
            hits,
            shotsPerLoad,
        };
    }
}
