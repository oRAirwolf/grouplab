using System.Globalization;
using System.Text;
using System.Text.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// The statistics engine against shotGroups 0.8.4, docs/STATISTICS.md section 15 and test/fixtures/shotgroups/README.md.
/// <para>
/// Every key of every fixture is accounted for in one of three ways, and a key that is none of them fails the test, so a
/// section the engine silently skips cannot pass: it is <b>compared</b>, at section 15.3's tolerance for its class; it is
/// <b>excluded</b>, with the reason, because the specification deliberately does not implement it (section 4's eight CEP
/// estimators, the robust estimates no section specifies, the Monte Carlo normality test the README lists as stochastic);
/// or it is <b>pending</b>, a part of the specification not yet built, which the report names so it cannot be mistaken for
/// a pass.
/// </para>
/// </summary>
public class ShotGroupsFixtureTests(ITestOutputHelper output)
{
    /// <summary>Shot units per distance unit, from the README's table and STATISTICS.md section 15.2: yards and inches, metres and centimetres, metres and millimetres.</summary>
    private static readonly Dictionary<string, double> UnitFactor = new()
    {
        ["DF300BLK"] = 36,
        ["DFscar17"] = 36,
        ["DFcciHV"] = 36,
        ["DF300BLKhl"] = 36,
        ["DFcm"] = 100,
        ["DFinch"] = 36,
        ["DFsavage"] = 1000,
        ["DFlandy04"] = 36,
        ["DFlandy01"] = 1000,
    };

    public static TheoryData<string> Datasets => new(UnitFactor.Keys);

    [Theory]
    [MemberData(nameof(Datasets))]
    public void EveryKeyIsComparedExcludedOrPendingAndEveryComparisonHolds(string dataset)
    {
        var report = ShotGroupsComparison.Run(dataset, UnitFactor[dataset]);
        output.WriteLine(report.Summary());
        foreach (var failure in report.Failures)
        {
            output.WriteLine("FAIL " + failure);
        }

        Assert.True(report.Unaccounted.Count == 0, "keys neither compared, excluded nor pending: " + string.Join(", ", report.Unaccounted.Take(20)));
        Assert.True(report.Failures.Count == 0, "comparisons outside tolerance:\n" + string.Join("\n", report.Failures.Take(40)));
    }
}

/// <summary>A tolerance class of docs/STATISTICS.md section 15.3.</summary>
internal sealed record ToleranceClass(string Name, double Relative, double Absolute);

/// <summary>One dataset's comparison: per class, how many keys were compared and the worst error; the failures; what was excluded and why; what is pending; and what was not accounted for.</summary>
internal sealed class ComparisonReport(string dataset)
{
    public string Dataset { get; } = dataset;

    public Dictionary<string, (int Compared, double WorstRelative, string WorstKey)> Classes { get; } = [];

    public List<string> Failures { get; } = [];

    public Dictionary<string, int> Excluded { get; } = [];

    public Dictionary<string, int> Pending { get; } = [];

    public Dictionary<string, int> Disputed { get; } = [];

    public List<string> Unaccounted { get; } = [];

    public string Summary()
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"{Dataset}: {Classes.Values.Sum(c => c.Compared)} compared, {Failures.Count} outside tolerance, {Excluded.Values.Sum()} excluded, {Pending.Values.Sum()} pending, {Disputed.Values.Sum()} disputed, {Unaccounted.Count} unaccounted");
        foreach (var (name, c) in Classes.OrderBy(c => c.Key))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"  compared  {name,-28} {c.Compared,6}  worst relative {c.WorstRelative:0.0e+00}  {c.WorstKey}");
        }

        foreach (var (reason, count) in Excluded.OrderBy(e => e.Key))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"  excluded  {count,6}  {reason}");
        }

        foreach (var (reason, count) in Disputed.OrderBy(e => e.Key))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"  disputed  {count,6}  {reason}");
        }

        foreach (var (reason, count) in Pending.OrderBy(e => e.Key))
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"  pending   {count,6}  {reason}");
        }

        return text.ToString();
    }
}

/// <summary>Loads a fixture, recomputes each scope from its shots, and compares key by key.</summary>
internal static class ShotGroupsComparison
{
    private static readonly ToleranceClass ClosedForm = new("closed form, 1e-12 relative", 1e-12, 1e-15);
    private static readonly ToleranceClass AngularClass = new("angular, 1e-12 relative", 1e-12, 1e-15);
    private static readonly ToleranceClass HoytClass = new("CorrNormal, 1e-8 relative", 1e-8, 1e-15);
    private static readonly ToleranceClass GeometryClass = new("geometry, 1e-9 absolute", 0, 1e-9);
    private static readonly ToleranceClass EllipseClass = new("minimum ellipse, 1e-4 relative", 1e-4, 1e-12);
    private static readonly ToleranceClass RangeClass = new("range lookup, 2e-3 relative", 2e-3, 1e-12);

    /// <summary>
    /// The intercept MANOVA's Wilks' lambda sits within 3e-7 of 1 on DFcciHV, and R forms its F from 1 - lambda, which costs it the
    /// digits this engine keeps by forming F from q directly; 1e-8 relative is the class for the MANOVA row.
    /// </summary>
    private static readonly ToleranceClass ManovaClass = new("MANOVA intercept row, 1e-8 relative", 1e-8, 1e-15);

    /// <summary>
    /// Rank test statistics built from normal-quantile scores: the Fligner-Killeen statistic subtracts n mean^2 from a sum of
    /// squared group sums of up to 530 scores, which costs about three of the digits each quantile carries, so 1e-10 relative.
    /// </summary>
    private static readonly ToleranceClass RankTestClass = new("rank test statistics, 1e-10 relative", 1e-10, 1e-15);

    /// <summary>docs/STATISTICS.md section 15.4 item 12: shotGroups' CorrNormal CEP against its own root finder, which misses by up to 2.3e-5 relative.</summary>
    private static readonly ToleranceClass CorrNormalCepClass = new("CorrNormal CEP against shotGroups' root finder, 1e-4 relative", 1e-4, 1e-15);

    /// <summary>docs/STATISTICS.md section 15.4 item 11: shotGroups' SMOA inverse, with its constant encoded, at the angular tolerance.</summary>
    private static readonly ToleranceClass SmoaInverseClass = new("SMOA inverse with shotGroups' constant, 1e-12 relative", 1e-12, 1e-15);

    private const double SmoaInverseExcess = 6.21288e-10;

    private static readonly string[] LinearUnits = ["unit", "MOA", "SMOA", "mrad"];

    private static readonly (RangeStatistic Stat, string Name)[] RangeColumns =
        [(RangeStatistic.ExtremeSpread, "ES"), (RangeStatistic.FigureOfMerit, "FoM"), (RangeStatistic.Diagonal, "D"), (RangeStatistic.RayleighSigma, "RS")];

    public static ComparisonReport Run(string dataset, double unitFactor)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(Repo.PathTo("test", "fixtures", "shotgroups", $"shotGroups_{dataset}.json")));
        var root = document.RootElement;
        var values = new Dictionary<string, double?>();
        foreach (var property in root.GetProperty("values").EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind == JsonValueKind.Number ? property.Value.GetDouble() : null;
        }

        var labels = root.GetProperty("seriesLabels") is { ValueKind: JsonValueKind.Array } array
            ? array.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [root.GetProperty("seriesLabels").GetString()!];
        string stochastic = root.GetProperty("stochastic").GetString()!;

        int n = root.GetProperty("nShots").GetInt32();
        // Each shot twice: as getXYmat gives it, and relative to its point of aim, as shotGroups' data frame gives it to groupLocation,
        // groupSpread, groupShape and compareGroups (test/fixtures/shotgroups/README.md, NOTES-FROM-PLANNING.md entry 23 section 1).
        var shots = new List<(PointD Point, double Distance, int Series)>();
        var aimed = new List<(PointD Point, double Distance, int Series)>();
        for (int i = 1; i <= n; i++)
        {
            string at = i.ToString(CultureInfo.InvariantCulture);
            double distance = values["shots.distance." + at]!.Value;
            int series = (int)values["shots.seriesIndex." + at]!.Value;
            shots.Add((new PointD(values["shots.x." + at]!.Value, values["shots.y." + at]!.Value), distance, series));
            aimed.Add((new PointD(values["shots.xPOA." + at]!.Value, values["shots.yPOA." + at]!.Value), distance, series));
        }

        var expected = new Dictionary<string, (double Value, ToleranceClass Class)>();
        if (labels.Count > 1)
        {
            CompareGroups(expected, labels, aimed, unitFactor);
        }

        if (labels.Count > 2)
        {
            FlignerProbe(expected, labels, aimed);
        }

        if (labels.Count > 1)
        {
            // shotGroups' multi-group range path, the nGroups argument of range2sigma, range2CEP and getRangeStatEff, at the most
            // common group size; R sorts its table of sizes by name and keeps that order among ties, so the first such name wins.
            var sizes = shots.GroupBy(s => s.Series).Select(g => g.Count()).ToList();
            int nPer = sizes.GroupBy(size => size).OrderByDescending(g => g.Count()).ThenBy(g => g.Key.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal).First().Key;
            int k = labels.Count;
            expected["multiGroup.nGroups"] = (k, ClosedForm);
            expected["multiGroup.nPerGroup"] = (nPer, ClosedForm);
            expected["multiGroup.groupsEqual"] = (sizes.Distinct().Count() == 1 ? 1 : 0, ClosedForm);
            if (nPer is >= 2 and <= 100 && k <= 10)
            {
                expected["multiGroup.getRangeStatEff.n"] = (nPer, ClosedForm);
                expected["multiGroup.getRangeStatEff.nGroups"] = (k, ClosedForm);
                expected["multiGroup.getRangeStatEff.nTotal"] = (nPer * k, ClosedForm);
                foreach (var (stat, name) in RangeColumns)
                {
                    expected[$"multiGroup.getRangeStatEff.{name}_efficiency"] = (RangeStatistics.Efficiency(stat, nPer, k), RangeClass);
                }

                var sigma = RangeStatistics.Sigma(RangeStatistic.ExtremeSpread, 1, nPer, k);
                var cep = RangeStatistics.Cep(RangeStatistic.ExtremeSpread, 1, nPer, k);
                expected["multiGroup.range2sigma.sigma.unit"] = (sigma.Value, RangeClass);
                expected["multiGroup.range2sigma.sigmaCI.unit.sigma ("] = (sigma.Lower, RangeClass);
                expected["multiGroup.range2sigma.sigmaCI.unit.sigma )"] = (sigma.Upper, RangeClass);
                expected["multiGroup.range2CEP.CEP.unit"] = (cep.Value, RangeClass);
                expected["multiGroup.range2CEP.CEPCI.unit.CEP ("] = (cep.Lower, RangeClass);
                expected["multiGroup.range2CEP.CEPCI.unit.CEP )"] = (cep.Upper, RangeClass);
            }
            else
            {
                expected["multiGroup.beyondTable"] = (1, ClosedForm);
            }
        }

        var scopes = new List<(string Prefix, List<(PointD Point, double Distance, int Series)> Shots)> { ("", shots) };
        if (labels.Count > 1)
        {
            scopes.AddRange(labels.Select((label, k) => ($"series.{label}.", shots.Where(s => s.Series == k + 1).ToList())));
        }

        foreach (var (prefix, scoped) in scopes)
        {
            var distances = scoped.Select(s => s.Distance).Distinct().ToList();
            Compute(expected, prefix, [.. scoped.Select(s => s.Point)], distances.Count == 1 ? distances[0] : null, distances.Count, unitFactor);
        }

        // The functions that take the frame are recomputed from the aimed shots, scope by scope, and replace the matrix's figures.
        string[] framePrefixes = ["groupLocation.", "groupSpread.", "groupShape."];
        var aimedScopes = new List<List<(PointD Point, double Distance, int Series)>> { aimed };
        if (labels.Count > 1)
        {
            aimedScopes.AddRange(labels.Select((_, k) => aimed.Where(s => s.Series == k + 1).ToList()));
        }

        for (int s = 0; s < scopes.Count; s++)
        {
            var framed = new Dictionary<string, (double Value, ToleranceClass Class)>();
            var distances = aimedScopes[s].Select(x => x.Distance).Distinct().ToList();
            Compute(framed, scopes[s].Prefix, [.. aimedScopes[s].Select(x => x.Point)], distances.Count == 1 ? distances[0] : null, distances.Count, unitFactor);
            foreach (var (key, value) in framed.Where(f => framePrefixes.Any(p => f.Key.StartsWith(scopes[s].Prefix + p, StringComparison.Ordinal))))
            {
                expected[key] = value;
            }
        }

        // The order a box's corners are listed in is a convention, not a measurement: each scope's corners are matched to
        // shotGroups' in whichever of the eight orders around the box fits best, and then compared at the geometry tolerance.
        foreach (var (prefix, _) in scopes)
        {
            string Corner(int i, string axis) => $"{prefix}getMinBBox.pts.{i}.{axis}";
            if (!values.ContainsKey(Corner(1, "x")) || !expected.ContainsKey(Corner(1, "x")))
            {
                continue;
            }

            var theirs = Enumerable.Range(1, 4).Select(i => (X: values[Corner(i, "x")] ?? double.NaN, Y: values[Corner(i, "y")] ?? double.NaN)).ToArray();
            var mine = Enumerable.Range(1, 4).Select(i => (X: expected[Corner(i, "x")].Value, Y: expected[Corner(i, "y")].Value)).ToArray();
            var best = Enumerable.Range(0, 8)
                .Select(p => Enumerable.Range(0, 4).Select(i => mine[p < 4 ? (i + p) % 4 : ((p - i) % 4 + 4) % 4]).ToArray())
                .MinBy(order => order.Zip(theirs).Sum(z => Math.Pow(z.First.X - z.Second.X, 2) + Math.Pow(z.First.Y - z.Second.Y, 2)))!;
            for (int i = 0; i < 4; i++)
            {
                expected[Corner(i + 1, "x")] = (best[i].X, GeometryClass);
                expected[Corner(i + 1, "y")] = (best[i].Y, GeometryClass);
            }
        }

        // Extreme spread's pair is a pair: shotGroups lists its two shots in whichever order R's matrix search met them.
        foreach (var (prefix, _) in scopes)
        {
            string first = prefix + "getMaxPairDist.idx1", second = prefix + "getMaxPairDist.idx2";
            if (values.TryGetValue(first, out var a) && values.TryGetValue(second, out var b) && a is not null && b is not null && expected.TryGetValue(first, out var mineA) && expected.TryGetValue(second, out var mineB))
            {
                // Several pairs can share the largest distance, and R may meet a different one first: any of them is the pair.
                var scoped = scopes.First(sc => sc.Prefix == prefix).Shots;
                var p1 = scoped[(int)a.Value - 1].Point;
                var p2 = scoped[(int)b.Value - 1].Point;
                double theirs = Math.Sqrt(((p1.X - p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) * (p1.Y - p2.Y)));
                if (theirs == expected[prefix + "getMaxPairDist.d"].Value)
                {
                    (expected[first], expected[second]) = ((a.Value, mineA.Class), (b.Value, mineB.Class));
                }
            }
        }

        // Question 11, answered A, A and A in NOTES-FROM-PLANNING.md entry 23 section 1 and recorded in docs/STATISTICS.md section 15.4
        // items 11 and 12. shotGroups' CorrNormal CEP is compared at 1e-4 relative, because its root finder misses its own distribution;
        // the distribution is gated through the hit probabilities at 1e-8, and this engine's CEP is required to be a root of it below.
        // shotGroups' fromMOA in SMOA is its getMOA inverse times 1 + 6.21288e-10, and is compared with that constant encoded.
        foreach (string key in expected.Keys.ToList())
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(key, @"^(?:.*getCEP\.accuracy_FALSE\.CEP\.CEP[0-9.]+\.unit\.CorrNormal|.*groupSpread\.CEP\.CEP0\.5\.(?:unit|MOA|SMOA|mrad)\.CorrNormal|compareGroups\.CEP\.(?:unit|MOA|SMOA|mrad)\..+)$"))
            {
                expected[key] = (expected[key].Value, CorrNormalCepClass);
            }
            else if (key.EndsWith("fromMOA.SMOA", StringComparison.Ordinal))
            {
                expected[key] = (expected[key].Value * (1 + SmoaInverseExcess), SmoaInverseClass);
            }
        }

        var report = new ComparisonReport(dataset);

        // This engine's CorrNormal CEP must be a root of its own distribution to 1e-12, in every scope and in both coordinate forms.
        const string RootClass = "CorrNormal CEP is a root of its distribution, 1e-12 absolute";
        int roots = 0;
        double worstRoot = 0;
        foreach (var scoped in scopes.Select(sc => sc.Shots).Concat(aimedScopes))
        {
            var points = scoped.Select(x => x.Point).ToList();
            if (points.Count < 3)
            {
                continue;
            }

            var (xx, xy, yy) = GroupStatistics.Covariance(points);
            var shape = GroupStatistics.Shape(xx, xy, yy);
            foreach (double q in new[] { 0.5, 0.9, 0.95 })
            {
                double miss = Math.Abs(GroupStatistics.HoytCdf(GroupStatistics.CepCorrNormal(xx, xy, yy, q), shape.Major, shape.Minor) - q);
                roots++;
                worstRoot = Math.Max(worstRoot, miss);
                if (!(miss <= 1e-12))
                {
                    report.Failures.Add(string.Create(CultureInfo.InvariantCulture, $"CorrNormal CEP at {q} misses its own distribution by {miss:0.0e+00}"));
                }
            }
        }

        report.Classes[RootClass] = (roots, worstRoot, "");
        foreach (var (key, fixtureValue) in values)
        {
            string local = StripScope(key, labels);
            string? excluded = key == stochastic || local == stochastic ? "stochastic, listed by the fixture: groupShape.multNorm.p.value" : Excluded(local);
            if (excluded is not null)
            {
                report.Excluded[excluded] = report.Excluded.GetValueOrDefault(excluded) + 1;
                continue;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(key, @"^compareGroups\.(?:Fligner[XY]|Kruskal)\.p\.value$") && fixtureValue is { } monteCarlo
                && Math.Abs((monteCarlo * 9999) - Math.Round(monteCarlo * 9999)) < 1e-6)
            {
                const string MonteCarloReason = "Monte Carlo p-value from coin's 9999 resamples, shown by p x 9999 being an integer: another generator cannot reproduce it";
                report.Excluded[MonteCarloReason] = report.Excluded.GetValueOrDefault(MonteCarloReason) + 1;
                continue;
            }

            string? pending = Pending(local);
            if (pending is not null)
            {
                report.Pending[pending] = report.Pending.GetValueOrDefault(pending) + 1;
                continue;
            }

            if (!expected.TryGetValue(key, out var mine))
            {
                report.Unaccounted.Add(key);
                continue;
            }

            if (fixtureValue is null)
            {
                if (!double.IsNaN(mine.Value))
                {
                    report.Failures.Add($"{key}: shotGroups gives NA, GroupLab {mine.Value:R}");
                }

                continue;
            }

            double reference = fixtureValue.Value, difference = Math.Abs(mine.Value - reference);
            double relative = difference / Math.Max(Math.Abs(reference), 1e-300);
            var c = mine.Class;
            var entry = report.Classes.TryGetValue(c.Name, out var found) ? found : (Compared: 0, WorstRelative: 0.0, WorstKey: "");
            report.Classes[c.Name] = (entry.Compared + 1, relative > entry.WorstRelative || double.IsNaN(relative) ? relative : entry.WorstRelative, relative > entry.WorstRelative || double.IsNaN(relative) ? key : entry.WorstKey);
            if (!(difference <= Math.Max(c.Relative * Math.Abs(reference), c.Absolute)))
            {
                report.Failures.Add(string.Create(CultureInfo.InvariantCulture, $"{key}: shotGroups {reference:R}, GroupLab {mine.Value:R}, relative {relative:0.0e+00} ({c.Name})"));
            }
        }

        // Section 15.5 point 3: angular output GroupLab produced where shotGroups produced none is a wrong number, not a bonus.
        foreach (var key in expected.Keys.Where(k => (k.Contains("getMOA.", StringComparison.Ordinal) || k.Contains("fromMOA.", StringComparison.Ordinal)) && !values.ContainsKey(k)))
        {
            report.Failures.Add($"{key}: GroupLab produced an angular value shotGroups suppressed");
        }

        return report;
    }

    /// <summary>
    /// The fixture's <c>flignerProbe</c> block, NOTES-FROM-PLANNING.md entry 28 section 3, computed as <c>sg_dump.R</c> computes it:
    /// the vectors <c>compareGroups</c> hands the Fligner-Killeen test, each series' median, count and mean centred score, the score
    /// variance, the count of absolute deviations R's <c>table()</c> sees as tied at 15 significant digits, the statistic rebuilt from
    /// them, and whether the frame's rows are in series order (section 15.4 item 14).
    /// </summary>
    private static void FlignerProbe(Dictionary<string, (double, ToleranceClass)> into, List<string> labels, List<(PointD Point, double Distance, int Series)> shots)
    {
        int n = shots.Count;
        var series = shots.Select(s => s.Series).ToList();
        into["flignerProbe.rowsSortedBySeries"] = (series.SequenceEqual(series.Order()) ? 1 : 0, ClosedForm);

        // The coordinates as compareGroups pastes them, section 15.4 item 14: split by series and bound back in the dataset's own factor
        // level order, then set beside the rows in their original order. That order is the labels' leading number, 1, 2 and on to 53 in
        // DFlandy01, where the fixture lists the labels as strings, "10_..." before "1_...". Where the rows are already in that order this
        // is every shot beside its own label.
        static (long Number, string Label) LevelOrder(string label) =>
            (long.TryParse(new string([.. label.TakeWhile(char.IsAsciiDigit)]), NumberStyles.None, CultureInfo.InvariantCulture, out long number) ? number : long.MaxValue, label);
        var pasted = shots.Select((s, i) => (Shot: s, Row: i)).OrderBy(p => LevelOrder(labels[p.Shot.Series - 1])).ThenBy(p => p.Row).Select(p => p.Shot).ToList();
        foreach (var (axis, key) in new[] { ('x', "FlignerX"), ('y', "FlignerY") })
        {
            var v = pasted.Select(s => axis == 'x' ? s.Point.X : s.Point.Y).ToList();
            for (int i = 0; i < n; i++)
            {
                into[string.Create(CultureInfo.InvariantCulture, $"flignerProbe.{key}.input.{i + 1}")] = (v[i], GeometryClass);
            }

            var levels = series.Distinct().Order().ToList();
            var medians = levels.ToDictionary(l => l, l =>
            {
                var sorted = Enumerable.Range(0, n).Where(i => series[i] == l).Select(i => v[i]).Order().ToArray();
                return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2;
            });
            var centred = Enumerable.Range(0, n).Select(i => v[i] - medians[series[i]]).ToArray();
            var scores = GroupComparison.MidRanks([.. centred.Select(Math.Abs)]).Select(r => Distributions.NormalQuantile((1 + (r / (n + 1))) / 2)).ToArray();
            double mean = scores.Average();
            var scoresCentred = scores.Select(a => a - mean).ToArray();
            double variance = scoresCentred.Sum(a => a * a) / (n - 1), statistic = 0;
            foreach (int level in levels)
            {
                var members = Enumerable.Range(0, n).Where(i => series[i] == level).ToList();
                double groupMean = members.Average(i => scoresCentred[i]);
                statistic += members.Count * groupMean * groupMean;
                string label = labels[level - 1];
                into[$"flignerProbe.{key}.groupMedian.{label}"] = (medians[level], ClosedForm);
                into[$"flignerProbe.{key}.scoreMean.{label}"] = (groupMean, RankTestClass);
                into[$"flignerProbe.{key}.n.{label}"] = (members.Count, ClosedForm);
            }

            into[$"flignerProbe.{key}.scoreVar"] = (variance, RankTestClass);
            into[$"flignerProbe.{key}.tiedValues"] = (centred.Select(d => Math.Abs(d).ToString("G15", CultureInfo.InvariantCulture)).GroupBy(t => t).Count(g => g.Count() > 1), ClosedForm);
            into[$"flignerProbe.{key}.recomputed"] = (statistic / variance, RankTestClass);
        }
    }

    /// <summary>
    /// shotGroups' <c>compareGroups</c>, docs/STATISTICS.md section 8: every group's descriptive figures, keyed by the group's
    /// label, the two-group Ansari-Bradley tests on each axis and Wilcoxon test on distance to centre, or with more groups the
    /// Fligner-Killeen tests and the Kruskal-Wallis test, and the intercept row of the MANOVA the fixture records.
    /// </summary>
    private static void CompareGroups(Dictionary<string, (double, ToleranceClass)> into, List<string> labels, List<(PointD Point, double Distance, int Series)> shots, double unitFactor)
    {
        const string Open = " (", Close = " )";
        var distanceToCentre = new double[shots.Count];
        for (int k = 0; k < labels.Count; k++)
        {
            string label = labels[k];
            var indices = Enumerable.Range(0, shots.Count).Where(i => shots[i].Series == k + 1).ToList();
            var group = indices.Select(i => shots[i].Point).ToList();
            var distances = indices.Select(i => shots[i].Distance).Distinct().ToList();
            var units = new List<(string Unit, Func<double, double> Convert)> { ("unit", x => x) };
            if (distances.Count == 1)
            {
                double d = distances[0];
                units.Add(("MOA", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Moa)));
                units.Add(("SMOA", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Smoa)));
                units.Add(("mrad", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Mrad)));
            }

            var centre = GroupStatistics.Centre(group);
            var (xx, xy, yy) = GroupStatistics.Covariance(group);
            var ray = GroupStatistics.Rayleigh(group);
            var (sdX, sdY) = GroupStatistics.AxisSd(group);
            var radii = GroupStatistics.Radii(group, centre);
            for (int j = 0; j < indices.Count; j++)
            {
                distanceToCentre[indices[j]] = radii[j];
            }

            // compareGroups' bounding-box figures are the minimum-area box's, not the axis-aligned box's.
            var box = GroupGeometry.MinimumAreaBox(group);
            double cep = GroupStatistics.CepCorrNormal(xx, xy, yy, 0.5), r = xy / Math.Sqrt(xx * yy);
            into[$"compareGroups.ctr.x.{label}"] = (centre.X, ClosedForm);
            into[$"compareGroups.ctr.y.{label}"] = (centre.Y, ClosedForm);
            into[$"compareGroups.corXY.{label}.x.x"] = (1, ClosedForm);
            into[$"compareGroups.corXY.{label}.x.y"] = (r, ClosedForm);
            into[$"compareGroups.corXY.{label}.y.x"] = (r, ClosedForm);
            into[$"compareGroups.corXY.{label}.y.y"] = (1, ClosedForm);
            foreach (var (u, f) in units)
            {
                into[$"compareGroups.distPOA.{u}.{label}"] = (f(Math.Sqrt((centre.X * centre.X) + (centre.Y * centre.Y))), ClosedForm);
                into[$"compareGroups.sdXY.{label}.{u}.x"] = (f(sdX.Value), ClosedForm);
                into[$"compareGroups.sdXY.{label}.{u}.y"] = (f(sdY.Value), ClosedForm);
                into[$"compareGroups.sdXYci.{label}.{u}.sdX{Open}"] = (f(sdX.Lower), ClosedForm);
                into[$"compareGroups.sdXYci.{label}.{u}.sdX{Close}"] = (f(sdX.Upper), ClosedForm);
                into[$"compareGroups.sdXYci.{label}.{u}.sdY{Open}"] = (f(sdY.Lower), ClosedForm);
                into[$"compareGroups.sdXYci.{label}.{u}.sdY{Close}"] = (f(sdY.Upper), ClosedForm);
                into[$"compareGroups.meanDistToCtr.{u}.{label}"] = (f(radii.Average()), ClosedForm);
                into[$"compareGroups.maxPairDist.{u}.{label}"] = (f(GroupGeometry.MaximumPairDistance(group).Distance), GeometryClass);
                into[$"compareGroups.bbFoM.{u}.{label}"] = (f(box.FigureOfMerit), GeometryClass);
                into[$"compareGroups.bbDiag.{u}.{label}"] = (f(box.Diagonal), GeometryClass);
                into[$"compareGroups.minCircleRad.{u}.{label}"] = (f(GroupGeometry.MinimumEnclosingCircle(group).Radius), GeometryClass);
                into[$"compareGroups.sigma.{u}.{label}"] = (f(ray.Sigma.Value), ClosedForm);
                into[$"compareGroups.MR.{u}.{label}"] = (f(ray.MeanRadius.Value), ClosedForm);
                into[$"compareGroups.sigmaMRci.{label}.{u}.sigma{Open}"] = (f(ray.Sigma.Lower), ClosedForm);
                into[$"compareGroups.sigmaMRci.{label}.{u}.sigma{Close}"] = (f(ray.Sigma.Upper), ClosedForm);
                into[$"compareGroups.sigmaMRci.{label}.{u}.MR{Open}"] = (f(ray.MeanRadius.Lower), ClosedForm);
                into[$"compareGroups.sigmaMRci.{label}.{u}.MR{Close}"] = (f(ray.MeanRadius.Upper), ClosedForm);
                into[$"compareGroups.CEP.{u}.{label}"] = (f(cep), HoytClass);
            }
        }

        var points = shots.Select(s => s.Point).ToList();
        var groups = shots.Select(s => s.Series).ToList();
        if (labels.Count == 2)
        {
            var first = groups.Select(g => g == 1).ToList();
            var ansariX = GroupComparison.AnsariBradley([.. points.Select(p => p.X)], first);
            var ansariY = GroupComparison.AnsariBradley([.. points.Select(p => p.Y)], first);
            var wilcoxon = GroupComparison.WilcoxonRankSum(distanceToCentre, first);
            into["compareGroups.AnsariX.statistic"] = (ansariX.Statistic, ClosedForm);
            into["compareGroups.AnsariX.p.value"] = (ansariX.PValue, ClosedForm);
            into["compareGroups.AnsariY.statistic"] = (ansariY.Statistic, ClosedForm);
            into["compareGroups.AnsariY.p.value"] = (ansariY.PValue, ClosedForm);
            into["compareGroups.Wilcoxon.statistic"] = (wilcoxon.Statistic, ClosedForm);
            into["compareGroups.Wilcoxon.p.value"] = (wilcoxon.PValue, ClosedForm);
        }
        else
        {
            var flignerX = GroupComparison.FlignerKilleen([.. points.Select(p => p.X)], groups);
            var flignerY = GroupComparison.FlignerKilleen([.. points.Select(p => p.Y)], groups);
            var kruskal = GroupComparison.KruskalWallis(distanceToCentre, groups);
            into["compareGroups.FlignerX.statistic"] = (flignerX.Statistic, RankTestClass);
            into["compareGroups.FlignerY.statistic"] = (flignerY.Statistic, RankTestClass);
            into["compareGroups.Kruskal.statistic"] = (kruskal.Statistic, ClosedForm);
        }

        var manova = GroupComparison.ManovaIntercept(points, groups);
        into["compareGroups.MANOVA.Wilks"] = (manova.Wilks, ManovaClass);
        into["compareGroups.MANOVA.approxF"] = (manova.F, ManovaClass);
        into["compareGroups.MANOVA.p.value"] = (manova.PValue, ManovaClass);
    }

    private static string StripScope(string key, List<string> labels)
    {
        if (!key.StartsWith("series.", StringComparison.Ordinal))
        {
            return key;
        }

        foreach (var label in labels.OrderByDescending(l => l.Length))
        {
            string prefix = $"series.{label}.";
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                return key[prefix.Length..];
            }
        }

        return key;
    }

    /// <summary>Why a key is not compared, when the specification deliberately does not implement what it measures.</summary>
    private static string? Excluded(string key)
    {
        string[] notImplemented = ["GrubbsPearson", "GrubbsLiu", "Krempasky", "Ignani", "RMSE", "Ethridge", "RAND", "Valstar"];
        if ((key.StartsWith("getCEP.", StringComparison.Ordinal) || key.StartsWith("getHitProb.", StringComparison.Ordinal)) && notImplemented.Any(e => key.EndsWith("." + e, StringComparison.Ordinal)))
        {
            return "STATISTICS.md section 4: CEP estimators deliberately not implemented in version one";
        }

        if (!key.StartsWith("flignerProbe.", StringComparison.Ordinal) && (key.Contains("Rob", StringComparison.Ordinal) || key.Contains("rob", StringComparison.Ordinal)))
        {
            return "robust (MCD) estimates: no section of STATISTICS.md specifies them";
        }

        if (key.StartsWith("groupShape.Shapiro", StringComparison.Ordinal) || key.StartsWith("groupShape.multNorm", StringComparison.Ordinal))
        {
            return "normality tests: section 14 point 5 notes shotGroups runs them; section 7 replaces them with the circularity and stringing tests";
        }

        if (key.StartsWith("getCEP.accuracy_TRUE", StringComparison.Ordinal))
        {
            return "CEP about the point of aim: section 4 names the offset case but specifies no estimator for it";
        }

        if (key.StartsWith("getRiceParam", StringComparison.Ordinal))
        {
            return "Rice parameter estimates: section 4 names the Rice distribution for offset hit probability only";
        }

        if (key.StartsWith("shots.", StringComparison.Ordinal) || key is "meta.coinInstalled" or "meta.mvoutlierInstalled" or "meta.nSeries" or "meta.nGroups" or "meta.nDistances" or "meta.distance")
        {
            return "fixture inputs and provenance";
        }

        return null;
    }

    /// <summary>What the specification requires and the engine does not yet compute.</summary>
    private static string? Pending(string key)
    {
        return null;
    }

    /// <summary>Every compared key one scope produces, in shotGroups' scheme.</summary>
    private static void Compute(Dictionary<string, (double, ToleranceClass)> into, string scope, List<PointD> shots, double? distance, int distanceCount, double unitFactor)
    {
        void Put(string key, double value, ToleranceClass c) => into[scope + key] = (value, c);
        IEnumerable<(string Unit, Func<double, double> Convert)> Units()
        {
            yield return ("unit", x => x);
            if (distance is { } d)
            {
                yield return ("MOA", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Moa));
                yield return ("SMOA", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Smoa));
                yield return ("mrad", x => Angular.ToAngle(x, d, unitFactor, AngularUnit.Mrad));
            }
        }

        const string Open = " (", Close = " )";
        int n = shots.Count;
        Put("meta.nShots", n, ClosedForm);
        var centre = GroupStatistics.Centre(shots);
        var (xx, xy, yy) = GroupStatistics.Covariance(shots);
        var ray = GroupStatistics.Rayleigh(shots);
        var (sdX, sdY) = GroupStatistics.AxisSd(shots);
        var radii = GroupStatistics.Radii(shots, centre);
        var sortedRadii = radii.Order().ToArray();
        double median = n % 2 == 1 ? sortedRadii[n / 2] : (sortedRadii[(n / 2) - 1] + sortedRadii[n / 2]) / 2;
        var pair = GroupGeometry.MaximumPairDistance(shots);
        var box = GroupGeometry.Box(shots);
        var minBox = GroupGeometry.MinimumAreaBox(shots);
        var circle = GroupGeometry.MinimumEnclosingCircle(shots);
        var ellipse = GroupGeometry.MinimumVolumeEllipse(shots);
        var conf = GroupStatistics.ConfidenceEllipse(shots, 0.5);
        double cepCorr50 = GroupStatistics.CepCorrNormal(xx, xy, yy, 0.5);

        // groupSpread
        foreach (var (u, f) in Units())
        {
            Put($"groupSpread.sdXY.{u}.x", f(sdX.Value), ClosedForm);
            Put($"groupSpread.sdXY.{u}.y", f(sdY.Value), ClosedForm);
            Put($"groupSpread.sdXci.{u}.sdX{Open}", f(sdX.Lower), ClosedForm);
            Put($"groupSpread.sdXci.{u}.sdX{Close}", f(sdX.Upper), ClosedForm);
            Put($"groupSpread.sdYci.{u}.sdY{Open}", f(sdY.Lower), ClosedForm);
            Put($"groupSpread.sdYci.{u}.sdY{Close}", f(sdY.Upper), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.mean", f(radii.Average()), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.median", f(median), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.max", f(radii.Max()), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.sigma", f(ray.Sigma.Value), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.RSD", f(ray.RadialSd.Value), ClosedForm);
            Put($"groupSpread.distToCtr.{u}.MR", f(ray.MeanRadius.Value), ClosedForm);
            Put($"groupSpread.sigmaCI.{u}.sigma{Open}", f(ray.Sigma.Lower), ClosedForm);
            Put($"groupSpread.sigmaCI.{u}.sigma{Close}", f(ray.Sigma.Upper), ClosedForm);
            Put($"groupSpread.RSDci.{u}.RSD{Open}", f(ray.RadialSd.Lower), ClosedForm);
            Put($"groupSpread.RSDci.{u}.RSD{Close}", f(ray.RadialSd.Upper), ClosedForm);
            Put($"groupSpread.MRci.{u}.MR{Open}", f(ray.MeanRadius.Lower), ClosedForm);
            Put($"groupSpread.MRci.{u}.MR{Close}", f(ray.MeanRadius.Upper), ClosedForm);
            Put($"groupSpread.maxPairDist.{u}", f(pair.Distance), GeometryClass);
            Put($"groupSpread.groupRect.{u}.width", f(box.Width), GeometryClass);
            Put($"groupSpread.groupRect.{u}.height", f(box.Height), GeometryClass);
            Put($"groupSpread.groupRect.{u}.FoM", f(box.FigureOfMerit), GeometryClass);
            Put($"groupSpread.groupRect.{u}.diag", f(box.Diagonal), GeometryClass);
            Put($"groupSpread.groupRectMin.{u}.width", f(minBox.Width), GeometryClass);
            Put($"groupSpread.groupRectMin.{u}.height", f(minBox.Height), GeometryClass);
            Put($"groupSpread.groupRectMin.{u}.FoM", f(minBox.FigureOfMerit), GeometryClass);
            Put($"groupSpread.groupRectMin.{u}.diag", f(minBox.Diagonal), GeometryClass);
            Put($"groupSpread.minCircleRad.{u}", f(circle.Radius), GeometryClass);
            Put($"groupSpread.minEll.{u}.semi_major", f(ellipse.SemiMajor), EllipseClass);
            Put($"groupSpread.minEll.{u}.semi_minor", f(ellipse.SemiMinor), EllipseClass);
            Put($"groupSpread.confEll.{u}.semi-major", f(conf.SemiMajor), ClosedForm);
            Put($"groupSpread.confEll.{u}.semi-minor", f(conf.SemiMinor), ClosedForm);
            Put($"groupSpread.CEP.CEP0.5.{u}.CorrNormal", f(cepCorr50), HoytClass);
        }

        Put("groupSpread.covXY.x.x", xx, ClosedForm);
        Put("groupSpread.covXY.x.y", xy, ClosedForm);
        Put("groupSpread.covXY.y.x", xy, ClosedForm);
        Put("groupSpread.covXY.y.y", yy, ClosedForm);
        void PutShape(string stem, EllipseShape shape, ToleranceClass c)
        {
            Put($"{stem}.angle", shape.AngleDegrees, c);
            Put($"{stem}.aspectRatio", shape.AspectRatio, c);
            Put($"{stem}.flattening", shape.Flattening, c);
            Put($"{stem}.trace", shape.Trace, c);
            Put($"{stem}.det", shape.Determinant, c);
        }

        PutShape("groupSpread.confEllShape", conf.Shape, ClosedForm);

        // groupLocation
        var (ciX, ciY) = GroupStatistics.CentreIntervals(shots);
        Put("groupLocation.ctr.x", centre.X, ClosedForm);
        Put("groupLocation.ctr.y", centre.Y, ClosedForm);
        foreach (var (u, f) in Units())
        {
            Put($"groupLocation.distPOA.{u}", f(Math.Sqrt((centre.X * centre.X) + (centre.Y * centre.Y))), ClosedForm);
        }

        Put($"groupLocation.ctrXci.t.x{Open}", ciX.Lower, ClosedForm);
        Put($"groupLocation.ctrXci.t.x{Close}", ciX.Upper, ClosedForm);
        Put($"groupLocation.ctrYci.t.y{Open}", ciY.Lower, ClosedForm);
        Put($"groupLocation.ctrYci.t.y{Close}", ciY.Upper, ClosedForm);
        var hotelling = GroupStatistics.HotellingOneSample(shots, new PointD(0, 0));
        Put("groupLocation.Hotelling.statistic", hotelling.F, ClosedForm);
        Put("groupLocation.Hotelling.p.value", hotelling.PValue, ClosedForm);

        // groupShape
        double r = xy / Math.Sqrt(xx * yy);
        Put("groupShape.corXY.x.x", 1, ClosedForm);
        Put("groupShape.corXY.x.y", r, ClosedForm);
        Put("groupShape.corXY.y.x", r, ClosedForm);
        Put("groupShape.corXY.y.y", 1, ClosedForm);

        // getCEP, accuracy FALSE
        var errorShape = GroupStatistics.Shape(xx, xy, yy);
        foreach (double q in (double[])[0.5, 0.9, 0.95])
        {
            string level = q.ToString(CultureInfo.InvariantCulture);
            Put($"getCEP.accuracy_FALSE.CEP.CEP{level}.unit.CorrNormal", GroupStatistics.CepCorrNormal(xx, xy, yy, q), HoytClass);
            Put($"getCEP.accuracy_FALSE.CEP.CEP{level}.unit.GrubbsPatnaik", GroupStatistics.CepGrubbsPatnaik(xx, xy, yy, q), ClosedForm);
            Put($"getCEP.accuracy_FALSE.CEP.CEP{level}.unit.Rayleigh", ray.Cep(q).Value, ClosedForm);
        }

        Put("getCEP.accuracy_FALSE.ellShape.aspectRatio", errorShape.AspectRatio, ClosedForm);
        Put("getCEP.accuracy_FALSE.ellShape.flattening", errorShape.Flattening, ClosedForm);
        Put("getCEP.accuracy_FALSE.ctr.x", centre.X, ClosedForm);
        Put("getCEP.accuracy_FALSE.ctr.y", centre.Y, ClosedForm);

        // getRayParam
        Put("getRayParam.sigma.sigma", ray.Sigma.Value, ClosedForm);
        Put("getRayParam.sigma.sigCIlo", ray.Sigma.Lower, ClosedForm);
        Put("getRayParam.sigma.sigCIup", ray.Sigma.Upper, ClosedForm);
        Put("getRayParam.RSD.RSD", ray.RadialSd.Value, ClosedForm);
        Put("getRayParam.RSD.RSDciLo", ray.RadialSd.Lower, ClosedForm);
        Put("getRayParam.RSD.RSDciUp", ray.RadialSd.Upper, ClosedForm);
        Put("getRayParam.MR.MR", ray.MeanRadius.Value, ClosedForm);
        Put("getRayParam.MR.MRciLo", ray.MeanRadius.Lower, ClosedForm);
        Put("getRayParam.MR.MRciUp", ray.MeanRadius.Upper, ClosedForm);
        Put("getRayParam.MEDR.MEDR", ray.MedianRadius.Value, ClosedForm);
        Put("getRayParam.MEDR.MEDRciLo", ray.MedianRadius.Lower, ClosedForm);
        Put("getRayParam.MEDR.MEDRciUP", ray.MedianRadius.Upper, ClosedForm);

        var hoyt = GroupStatistics.Hoyt(xx, xy, yy);
        Put("getHoytParam.q", hoyt.Q, ClosedForm);
        Put("getHoytParam.omega", hoyt.Omega, ClosedForm);

        foreach (double radius in (double[])[0.5, 1, 2])
        {
            string at = radius.ToString(CultureInfo.InvariantCulture);
            Put($"getHitProb.r.R{at}.CorrNormal", GroupStatistics.HitProbabilityCorrNormal(xx, xy, yy, radius), HoytClass);
            Put($"getHitProb.r.R{at}.GrubbsPatnaik", GroupStatistics.HitProbabilityGrubbsPatnaik(xx, xy, yy, radius), ClosedForm);
            Put($"getHitProb.r.R{at}.Rayleigh", GroupStatistics.HitProbabilityRayleigh(ray.Sigma.Value, radius), ClosedForm);
        }

        // geometry
        Put("getBoundingBox.pts.xleft", box.Left, GeometryClass);
        Put("getBoundingBox.pts.ybottom", box.Bottom, GeometryClass);
        Put("getBoundingBox.pts.xright", box.Right, GeometryClass);
        Put("getBoundingBox.pts.ytop", box.Top, GeometryClass);
        Put("getBoundingBox.width", box.Width, GeometryClass);
        Put("getBoundingBox.height", box.Height, GeometryClass);
        Put("getBoundingBox.FoM", box.FigureOfMerit, GeometryClass);
        Put("getBoundingBox.diag", box.Diagonal, GeometryClass);
        for (int i = 0; i < 4; i++)
        {
            Put($"getMinBBox.pts.{i + 1}.x", minBox.Corners[i].X, GeometryClass);
            Put($"getMinBBox.pts.{i + 1}.y", minBox.Corners[i].Y, GeometryClass);
        }

        Put("getMinBBox.width", minBox.Width, GeometryClass);
        Put("getMinBBox.height", minBox.Height, GeometryClass);
        Put("getMinBBox.FoM", minBox.FigureOfMerit, GeometryClass);
        Put("getMinBBox.diag", minBox.Diagonal, GeometryClass);
        Put("getMinBBox.angle", minBox.AngleDegrees, GeometryClass);
        Put("getMinCircle.ctr.1", circle.Centre.X, GeometryClass);
        Put("getMinCircle.ctr.2", circle.Centre.Y, GeometryClass);
        Put("getMinCircle.rad", circle.Radius, GeometryClass);
        Put("getMinEllipse.ctr.1", ellipse.Centre.X, EllipseClass);
        Put("getMinEllipse.ctr.2", ellipse.Centre.Y, EllipseClass);
        PutShape("getMinEllipse.shape", ellipse.Shape, EllipseClass);
        Put("getMinEllipse.size.1", ellipse.SemiMajor, EllipseClass);
        Put("getMinEllipse.size.2", ellipse.SemiMinor, EllipseClass);
        Put("getMinEllipse.area", ellipse.Area, EllipseClass);
        Put("getMaxPairDist.d", pair.Distance, GeometryClass);
        Put("getMaxPairDist.idx1", pair.First + 1, GeometryClass);
        Put("getMaxPairDist.idx2", pair.Second + 1, GeometryClass);

        Put("getConfEll.ctr.x", centre.X, ClosedForm);
        Put("getConfEll.ctr.y", centre.Y, ClosedForm);
        Put("getConfEll.cov.x.x", xx, ClosedForm);
        Put("getConfEll.cov.x.y", xy, ClosedForm);
        Put("getConfEll.cov.y.x", xy, ClosedForm);
        Put("getConfEll.cov.y.y", yy, ClosedForm);
        Put("getConfEll.size.unit.semi-major", conf.SemiMajor, ClosedForm);
        Put("getConfEll.size.unit.semi-minor", conf.SemiMinor, ClosedForm);
        PutShape("getConfEll.shape", conf.Shape, ClosedForm);
        Put("getConfEll.magFac", conf.MagnificationFactor, ClosedForm);
        for (int i = 0; i < n; i++)
        {
            Put($"getDistToCtr.r.{i + 1}", radii[i], ClosedForm);
        }

        // range statistics, STATISTICS.md section 5, through GroupLab's own table
        foreach (var (stat, name, value) in (ReadOnlySpan<(RangeStatistic, string, double)>)[(RangeStatistic.ExtremeSpread, "ES", pair.Distance), (RangeStatistic.FigureOfMerit, "FoM", box.FigureOfMerit), (RangeStatistic.Diagonal, "D", box.Diagonal)])
        {
            var interval = RangeStatistics.Interval(stat, value, n);
            foreach (var (u, f) in Units())
            {
                Put($"getRangeStat.range_stat.{u}.{name}", f(value), GeometryClass);
                Put($"getRangeStat.CI.{name}.{u}.{name}{Open}", f(interval.Lower), RangeClass);
                Put($"getRangeStat.CI.{name}.{u}.{name}{Close}", f(interval.Upper), RangeClass);
                if (double.IsNaN(interval.Lower) && name == "FoM")
                {
                    // Past the table shotGroups names the figure of merit's interval FOM, with its value NA.
                    Put($"getRangeStat.CI.FOM.{u}.FoM{Open}", double.NaN, RangeClass);
                    Put($"getRangeStat.CI.FOM.{u}.FoM{Close}", double.NaN, RangeClass);
                }
            }
        }

        var fromSpread = RangeStatistics.Sigma(RangeStatistic.ExtremeSpread, pair.Distance, n);
        if (double.IsNaN(fromSpread.Value))
        {
            // Past the table shotGroups raises an error, which the fixture records; GroupLab has no value either.
            Put("range2sigma._error", 1, ClosedForm);
            Put("range2CEP._error", 1, ClosedForm);
            Put("getRangeStatEff._error", 1, ClosedForm);
        }
        else
        {
            var cepFromSpread = RangeStatistics.Cep(RangeStatistic.ExtremeSpread, pair.Distance, n);
            Put("range2sigma.sigma.unit", fromSpread.Value, RangeClass);
            Put($"range2sigma.sigmaCI.unit.sigma{Open}", fromSpread.Lower, RangeClass);
            Put($"range2sigma.sigmaCI.unit.sigma{Close}", fromSpread.Upper, RangeClass);
            Put("range2CEP.CEP.unit", cepFromSpread.Value, RangeClass);
            Put($"range2CEP.CEPCI.unit.CEP{Open}", cepFromSpread.Lower, RangeClass);
            Put($"range2CEP.CEPCI.unit.CEP{Close}", cepFromSpread.Upper, RangeClass);
            Put("getRangeStatEff.n", n, ClosedForm);
            Put("getRangeStatEff.nGroups", 1, ClosedForm);
            Put("getRangeStatEff.nTotal", n, ClosedForm);
            foreach (var (stat, name) in RangeColumns)
            {
                Put($"getRangeStatEff.{name}_efficiency", RangeStatistics.Efficiency(stat, n), RangeClass);
            }
        }

        // angular conversion of one shot unit and one angular unit
        if (distance is { } dst)
        {
            foreach (var (name, unit) in (ReadOnlySpan<(string, AngularUnit)>)[("deg", AngularUnit.Degree), ("rad", AngularUnit.Radian), ("MOA", AngularUnit.Moa), ("SMOA", AngularUnit.Smoa), ("mrad", AngularUnit.Mrad), ("mil", AngularUnit.Mil)])
            {
                Put($"getMOA.{name}", Angular.ToAngle(1, dst, unitFactor, unit), AngularClass);
                Put($"fromMOA.{name}", Angular.FromAngle(1, dst, unitFactor, unit), AngularClass);
            }
        }

        Put("angular.nDistances", distanceCount, ClosedForm);
        _ = LinearUnits;
    }
}
