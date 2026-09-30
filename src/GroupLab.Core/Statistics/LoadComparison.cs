using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Statistics;

/// <summary>One group as the comparison shows it: its name, its shots as composite offsets, and its figures with their intervals.</summary>
public sealed record ComparedGroup(string Name, IReadOnlyList<PointD> Offsets, RayleighEstimate Rayleigh, Estimate MeanRadius, double ExtremeSpread, PointD Centre)
{
    public int Shots => Offsets.Count;
}

/// <summary>A dispersion ratio between two of the groups, with its interval, its p-value and, among more than two, Holm's adjustment of it.</summary>
public sealed record PairedDispersion(int First, int Second, Estimate Ratio, double PValue, double HolmPValue);

/// <summary>One test run, its p-value, what it found, and what it could have detected.</summary>
public sealed record ComparisonTest(string Name, double PValue, string Verdict, string Power);

/// <summary>A row of the planning table: the shots per load that resolve a difference of a given size with 80 percent power at 5 percent.</summary>
public sealed record ResolveRow(double Ratio, int ShotsPerLoad);

/// <summary>
/// A comparison of loads, NOTES-FROM-PLANNING.md entry 113 section 2 and docs/STATISTICS.md sections 8 and 9: two or more groups, each with
/// its figures and intervals; the dispersion and centre tests with their verdicts; what each test could have detected; and the headline,
/// which never ranks the loads by their point estimates. When the sigma intervals overlap the headline says the data do not separate them.
/// </summary>
public sealed record LoadComparisonReport(
    IReadOnlyList<ComparedGroup> Groups,
    IReadOnlyList<ComparisonTest> Tests,
    IReadOnlyList<PairedDispersion> Pairs,
    bool IntervalsOverlap,
    string Headline,
    IReadOnlyList<string> Explanation,
    IReadOnlyList<ResolveRow> Resolve)
{
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 312 section 3: what the explanation says, in plain words a shooter reads first, such as "Their spreads
    /// are within about a third of each other either way". The statistician's sentences behind them are <see cref="Details"/>.
    /// </summary>
    public IReadOnlyList<string> Plain { get; init; } = [];

    /// <summary>The exact figures behind <see cref="Plain"/>: the ratio of the sigmas with its 95 percent interval, and the power sentence.</summary>
    public IReadOnlyList<string> Details { get; init; } = [];
}

public static class LoadComparison
{
    /// <summary>The noncentrality at which a chi-square test on 2 degrees of freedom has 80 percent power at 5 percent, the large-sample Hotelling T\u00b2.</summary>
    public const double CentreNoncentrality = 9.635;

    /// <summary>The fewest shots a group needs to take part: a sigma needs two, and a test on so few says nothing, which its power statement shows.</summary>
    public const int MinimumShots = 3;

    private static string Percent(double ratio) => (100 * (ratio - 1)).ToString("0", CultureInfo.InvariantCulture);

    private static string P(double p) => p < 0.001 ? "p < 0.001" : "p = " + p.ToString("0.000", CultureInfo.InvariantCulture);

    /// <summary>
    /// Compares the groups. Each group's offsets are its shots about their own bulls, in one unit at one distance: groups shot at different
    /// distances are the caller's to put on a common footing first, and to say so.
    /// </summary>
    public static LoadComparisonReport Compare(IReadOnlyList<(string Name, IReadOnlyList<PointD> Offsets)> groups, Func<double, string> length)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(length);
        if (groups.Count < 2)
        {
            throw new ArgumentException("A comparison needs two groups or more.", nameof(groups));
        }

        if (groups.FirstOrDefault(g => g.Offsets.Count < MinimumShots) is { Name: { } few })
        {
            throw new ArgumentException($"{few} has fewer than {MinimumShots} shots, too few to compare.", nameof(groups));
        }

        var compared = groups.Select(g =>
        {
            var rayleigh = GroupStatistics.Rayleigh(g.Offsets);
            double spread = GroupGeometry.MaximumPairDistance([.. g.Offsets]) is var (d, _, _) ? d : 0;
            return new ComparedGroup(g.Name, g.Offsets, rayleigh, rayleigh.MeanRadius, spread, GroupStatistics.Centre(g.Offsets));
        }).ToList();

        // Pairwise dispersion ratios, with Holm's adjustment when there are more than two groups (STATISTICS.md section 8.4).
        var pairs = new List<(int A, int B, Estimate Ratio, double P)>();
        for (int a = 0; a < compared.Count; a++)
        {
            for (int b = a + 1; b < compared.Count; b++)
            {
                var (ratio, _, p) = GroupComparison.DispersionRatio(compared[a].Rayleigh, compared[b].Rayleigh);
                pairs.Add((a, b, ratio, p));
            }
        }

        double[] holm = GroupComparison.Holm([.. pairs.Select(p => p.P)]);
        var paired = pairs.Select((p, i) => new PairedDispersion(p.A, p.B, p.Ratio, p.P, holm[i])).ToList();

        bool overlap = compared.SelectMany((g, i) => compared.Skip(i + 1).Select(h => (g, h))).All(x => x.g.Rayleigh.Sigma.Lower <= x.h.Rayleigh.Sigma.Upper && x.h.Rayleigh.Sigma.Lower <= x.g.Rayleigh.Sigma.Upper);

        int fewest = compared.Min(g => g.Shots);
        double detectable = SampleSize.MinimumDetectableRatio(fewest);
        string dispersionPower = $"With {fewest} shots{(compared.Any(g => g.Shots != fewest) ? " in the smallest group" : " a load")}, this test detects a difference in dispersion of about {Percent(detectable)} percent or more 80 percent of the time, at the 5 percent level; smaller differences it will usually miss.";

        var tests = new List<ComparisonTest>();
        var offsets = compared.SelectMany(g => g.Offsets).ToList();
        var labels = compared.SelectMany((g, i) => g.Offsets.Select(_ => i)).ToList();
        if (compared.Count == 2)
        {
            var only = paired[0];
            tests.Add(new ComparisonTest(
                "Sigma ratio, F test on the Rayleigh sigmas",
                only.PValue,
                only.PValue < 0.05
                    ? $"The dispersions differ beyond chance, {P(only.PValue)}."
                    : $"No evidence of a difference in dispersion, {P(only.PValue)}. That is not evidence that they are the same.",
                dispersionPower));
        }
        else
        {
            var radii = compared.SelectMany(g => g.Offsets.Select(o => Math.Sqrt(Math.Pow(o.X - g.Centre.X, 2) + Math.Pow(o.Y - g.Centre.Y, 2)))).ToList();
            double fk = GroupComparison.FlignerKilleen(radii, labels).PValue;
            tests.Add(new ComparisonTest(
                "Dispersion, Fligner-Killeen across all the groups",
                fk,
                fk < 0.05
                    ? $"At least one dispersion differs beyond chance, {P(fk)}."
                    : $"No evidence that any dispersion differs, {P(fk)}. That is not evidence that they are the same.",
                dispersionPower));
        }

        if (offsets.Count > compared.Count + 2)
        {
            var manova = GroupComparison.ManovaGroups(offsets, labels);
            double pooledSigma = Math.Sqrt(compared.Sum(g => g.Rayleigh.SumOfSquaredRadii) / compared.Sum(g => g.Rayleigh.DegreesOfFreedom));
            int second = compared.Select(g => g.Shots).Order().Skip(1).First();
            double shift = Math.Sqrt(CentreNoncentrality * ((1.0 / fewest) + (1.0 / second))) * pooledSigma;
            tests.Add(new ComparisonTest(
                compared.Count == 2 ? "Group center, Hotelling's T squared" : "Group centers, MANOVA",
                manova.PValue,
                manova.PValue < 0.05
                    ? $"The centers differ beyond chance, {P(manova.PValue)}: the loads put their groups in different places."
                    : $"No evidence the centers differ, {P(manova.PValue)}. That is not evidence that they are in the same place.",
                $"It detects a shift between centers of about {length(shift)} or more 80 percent of the time, at this spread and these shot counts."));
        }

        // The headline. Never a ranking by point estimate: when the intervals overlap the loads are not separated, whatever the estimates say.
        var explanation = new List<string>();
        bool dispersionDiffers = tests[0].PValue < 0.05;
        bool centreDiffers = tests.Count > 1 && tests[1].PValue < 0.05;
        string these = compared.Count == 2 ? "These two loads" : "These loads";
        string headline = (dispersionDiffers, centreDiffers) switch
        {
            (false, false) => $"{these} are not distinguishable on this evidence.",
            (true, false) => $"{these} differ in dispersion beyond chance.",
            (false, true) => $"{these} group alike as far as these shots can tell, and put their groups in different places.",
            _ => $"{these} differ in dispersion and in where they group.",
        };
        if (overlap && !dispersionDiffers)
        {
            // Entry 295 section 1.5: in plain words, with the statistician's name for the ranges kept in brackets, for its explanation.
            explanation.Add("Each load's spread could really be anywhere in a range, and those ranges overlap (the sigma intervals), so these shots do not separate them. Ranking them by the measured numbers alone would be reading noise.");
        }
        else if (overlap)
        {
            explanation.Add("Each load's range overlaps the other's (the sigma intervals), which a real but modest difference often does; the range of the ratio between them is the comparison, and it leaves out 1, so the spreads do differ.");
        }

        int said = explanation.Count;
        var plain = new List<string>(explanation);
        if (compared.Count == 2)
        {
            var only = paired[0];
            plain.Add(Apart(compared[0].Name, compared[1].Name, only.Ratio.Lower, only.Ratio.Upper));
            explanation.Add($"{compared[0].Name}'s sigma is {only.Ratio.Value.ToString("0.00", CultureInfo.InvariantCulture)} times {compared[1].Name}'s, 95 percent interval {only.Ratio.Lower.ToString("0.00", CultureInfo.InvariantCulture)} to {only.Ratio.Upper.ToString("0.00", CultureInfo.InvariantCulture)}.");
        }
        else
        {
            explanation.Add("Every pair's sigma ratio is below with its interval, and its p-value adjusted by Holm's method for the number of pairs compared, which is said because six loads make fifteen pairs and one of them will look different by chance.");
        }

        if (compared.Count > 2)
        {
            plain.Add("Every pair of loads is compared under Details, and with this many pairs one of them can look different by chance alone.");
        }

        string have = compared.Count == 2 ? $"{compared[0].Shots} and {compared[1].Shots}" : string.Join(", ", compared.Select(g => g.Shots));
        plain.Add($"To tell a 10 percent difference in spread apart would take about {SampleSize.ShotsPerLoad(1.10)} shots each; these groups have {have}.");
        explanation.Add($"To resolve a difference of 10 percent, with 80 percent power at the 5 percent level, takes {SampleSize.ShotsPerLoad(1.10)} shots per load. These groups have {string.Join(", ", compared.Select(g => g.Shots))}.");
        var resolve = new[] { 1.10, 1.25, 1.50 }.Select(k => new ResolveRow(k, SampleSize.ShotsPerLoad(k))).ToList();
        var details = explanation.Skip(said).ToList();
        return new LoadComparisonReport(compared, tests, paired, overlap, headline, explanation, resolve) { Plain = plain, Details = details };
    }

    /// <summary>
    /// Entry 312 section 3: the ratio of two sigmas and its 95 percent interval, said as a shooter would say it. Where the interval holds 1
    /// the spreads may be the same, and the sentence gives how far apart they could still be either way, as a fraction where one is near
    /// ("about a third") and in percent otherwise; where it leaves 1 out, which load spreads more and by how much.
    /// </summary>
    internal static string Apart(string first, string second, double lower, double upper)
    {
        if (lower <= 1 && upper >= 1)
        {
            double most = Math.Max(upper, 1 / lower);
            return most >= 2
                ? $"Their spreads could be the same, or either could be up to {Times(most)} the other's; these shots cannot say which."
                : $"Their spreads are within about {Fraction(most - 1)} of each other either way; these shots cannot say which is smaller.";
        }

        return lower > 1
            ? $"{first} spreads more than {second}: by {Range(lower - 1, upper - 1)}."
            : $"{first} spreads less than {second}: by {Range(1 - upper, 1 - lower)}.";
    }

    private static string Range(double low, double high) =>
        $"somewhere between {Percent(1 + low)} and {Percent(1 + high)} percent";

    private static string Times(double ratio) => ratio.ToString("0.#", CultureInfo.InvariantCulture) + " times";

    /// <summary>A fraction in words where a common one is within a few points of it, and in percent otherwise.</summary>
    internal static string Fraction(double part)
    {
        (double Value, string Words)[] common = [(0.1, "a tenth"), (0.2, "a fifth"), (0.25, "a quarter"), (1.0 / 3, "a third"), (0.5, "half"), (2.0 / 3, "two thirds"), (0.75, "three quarters")];
        var near = common.MinBy(c => Math.Abs(c.Value - part));
        return Math.Abs(near.Value - part) <= 0.03 ? near.Words : Percent(1 + part) + " percent";
    }

    /// <summary>
    /// What the chart of one figure says in words beneath it, NOTES-FROM-PLANNING.md entry 295 section 1.4: never anything the verdict
    /// disagrees with. The ranges drawn for mean radius and every CEP are sigma's range scaled, so they overlap exactly when the sigma
    /// intervals do, and the sentence follows the same dispersion test the headline does. A figure drawn with no range, such as extreme
    /// spread, has nothing to compare and says so, pointing to mean radius, rather than reading "no overlap" into dots that have no range.
    /// </summary>
    /// <param name="report">The comparison the chart is drawn from.</param>
    /// <param name="figure">The figure's name as the chart's heading has it.</param>
    /// <param name="hasRange">Whether the chart draws each load's range for this figure.</param>
    public static string ChartSays(LoadComparisonReport report, string figure, bool hasRange)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(figure);
        if (!hasRange)
        {
            return $"{figure} has no range to compare here, so it cannot say whether these loads differ; mean radius has one, and is the figure to compare them on.";
        }

        bool differs = report.Tests.Count > 0 && report.Tests[0].PValue < 0.05;
        bool apart = report.Tests.Count > 1 && report.Tests[1].PValue < 0.05;
        return (report.IntervalsOverlap, differs) switch
        {
            // Where the centers differ the headline says so, and "not told apart" would contradict it unless it says by what.
            (true, false) when apart => "Every one of these overlaps every other, so these shots do not tell them apart by this figure; where they group is another matter, in the card below.",
            (true, false) => "Every one of these overlaps every other, so these shots do not tell them apart.",
            (true, true) => "These ranges overlap, but the test on the spreads themselves finds a difference beyond chance; the card below says how large.",
            (false, true) => "Some of these do not overlap, so there is a difference these shots can see.",
            (false, false) => "Some of these do not overlap, but the test on all the spreads together finds no difference beyond chance, so these shots do not settle it.",
        };
    }
}
