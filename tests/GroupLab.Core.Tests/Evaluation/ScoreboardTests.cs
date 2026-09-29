using GroupLab.Cli;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Evaluation;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 7, the detection scoreboard of docs/DETECTION-LEARNING-STUDY.md section 9: every build re-reads
/// the synthetic degradations and fails, naming the condition and the numbers, when a line falls beyond the margin against the committed
/// baseline. Real photographs are scored the same way, locally, and never here.
/// </summary>
public class ScoreboardTests
{
    private static ScoreboardBaseline Baseline() =>
        Scoreboard.FromJson(File.ReadAllText(Repo.PathTo("docs", "scoreboard", "synthetic-baseline.json")));

    [Fact]
    public void TheSyntheticScoreboardHoldsItsBaseline()
    {
        var baseline = Baseline();
        var definition = BuiltIns.Load(Scoreboard.SyntheticSheetFile);
        var rows = Scoreboard.Rows(Scoreboard.RunSynthetic(definition, new OpenCvSharpBackend(), baseline.Seeds, ScoreboardVerb.Jpeg));
        var drops = Scoreboard.Drops(baseline.Rows, rows, baseline.Margin);
        Assert.True(drops.Count == 0,
            "The detection scoreboard fell beyond its margin against docs/scoreboard/synthetic-baseline.json:\n" + string.Join("\n", drops) + "\n\nThis run:\n" + Scoreboard.Table(rows));
    }

    /// <summary>A condition added to the list without a baseline line would never be held to anything.</summary>
    [Fact]
    public void EveryConditionHasABaselineLine()
    {
        var names = Baseline().Rows.Select(r => r.Condition).ToHashSet();
        Assert.All(Scoreboard.Conditions, c => Assert.Contains(c.Name, names));
    }

    [Fact]
    public void EachMarkCountsForOneHoleAtMost()
    {
        PointD[] holes = [new(0, 0), new(10, 0)];
        PointD[] marks = [new(1, 0), new(50, 50)];
        var (found, falseMarks, errors) = Scoreboard.Match(holes, marks, 30);
        Assert.Equal(1, found);
        Assert.Equal(1, falseMarks);
        Assert.Equal(1, Assert.Single(errors), 6);
    }

    [Fact]
    public void ADropBeyondTheMarginIsNamedAndAnExpectedFailureIsNot()
    {
        static ScoreboardRow Row(string name, int found, int falseMarks = 0, double centre = 0.006) =>
            new(name, "holes", 2, 2, 50, found + falseMarks, found, falseMarks, centre, 0.07, 0.0001, 0.0003, 1000, [found / 2, found - (found / 2)]);
        var margin = new ScoreboardMargin();
        ScoreboardRow[] baseline = [Row("clean", 49), Row("curl", 0) with { ExpectedToFail = true }, Row("glare", 45)];

        Assert.Empty(Scoreboard.Drops(baseline, [Row("clean", 48), Row("glare", 45)], margin));
        var drops = Scoreboard.Drops(baseline, [Row("clean", 47, falseMarks: 2, centre: 0.02)], margin);
        Assert.Contains(drops, d => d.StartsWith("clean: 47 of 50 holes found, the baseline 49", StringComparison.Ordinal));
        Assert.Contains(drops, d => d.StartsWith("clean: 2 false marks, the baseline 0", StringComparison.Ordinal));
        Assert.Contains(drops, d => d.StartsWith("clean: median center error 0.0200 in", StringComparison.Ordinal));
        Assert.Contains(drops, d => d.StartsWith("glare: not run", StringComparison.Ordinal));
        Assert.DoesNotContain(drops, d => d.StartsWith("curl", StringComparison.Ordinal));

        // A fix to the line expected to fail shows as an improvement.
        Assert.Contains(Scoreboard.Improvements(baseline, [Row("curl", 48)], margin), i => i.Contains("a line expected to fail", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBaselineReadsBackAsItWasWritten()
    {
        var baseline = Baseline();
        var again = Scoreboard.FromJson(Scoreboard.ToJson(baseline));
        Assert.Equal(baseline.Rows.Count, again.Rows.Count);
        Assert.Equal(baseline.Rows.Select(r => (r.Condition, r.Found, r.FalseMarks, r.ExpectedToFail)), again.Rows.Select(r => (r.Condition, r.Found, r.FalseMarks, r.ExpectedToFail)));
        Assert.Equal(baseline.Margin, again.Margin);
    }
}
