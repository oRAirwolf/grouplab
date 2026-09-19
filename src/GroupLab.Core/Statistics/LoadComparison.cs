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
    IReadOnlyList<ResolveRow> Resolve);

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
                compared.Count == 2 ? "Group centre, Hotelling's T squared" : "Group centres, MANOVA",
                manova.PValue,
                manova.PValue < 0.05
                    ? $"The centres differ beyond chance, {P(manova.PValue)}: the loads put their groups in different places."
                    : $"No evidence the centres differ, {P(manova.PValue)}. That is not evidence that they are in the same place.",
                $"It detects a shift between centres of about {length(shift)} or more 80 percent of the time, at this spread and these shot counts."));
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
            explanation.Add("The sigma intervals overlap, so the data do not separate them. Ordering them by their point estimates would be reading noise.");
        }
        else if (overlap)
        {
            explanation.Add("Each load's own sigma interval overlaps the other's, which a real but modest difference often does; the ratio's interval is the comparison, and it excludes 1.");
        }

        if (compared.Count == 2)
        {
            var only = paired[0];
            explanation.Add($"{compared[0].Name}'s sigma is {only.Ratio.Value.ToString("0.00", CultureInfo.InvariantCulture)} times {compared[1].Name}'s, 95 percent interval {only.Ratio.Lower.ToString("0.00", CultureInfo.InvariantCulture)} to {only.Ratio.Upper.ToString("0.00", CultureInfo.InvariantCulture)}.");
        }
        else
        {
            explanation.Add("Every pair's sigma ratio is below with its interval, and its p-value adjusted by Holm's method for the number of pairs compared, which is said because six loads make fifteen pairs and one of them will look different by chance.");
        }

        explanation.Add($"To resolve a difference of 10 percent, with 80 percent power at the 5 percent level, takes {SampleSize.ShotsPerLoad(1.10)} shots per load. These groups have {string.Join(", ", compared.Select(g => g.Shots))}.");
        var resolve = new[] { 1.10, 1.25, 1.50 }.Select(k => new ResolveRow(k, SampleSize.ShotsPerLoad(k))).ToList();
        return new LoadComparisonReport(compared, tests, paired, overlap, headline, explanation, resolve);
    }
}
