using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 279 section 3, Unholy's report: excluding a shot did not exclude it from the analysis. The figures shown were
/// those of every shot, and the ones without the shot left out were only a line beneath. Entry 278 section 5c: a shot left out stays on the
/// record and is shown as left out, and every figure, the saved session and what is sent leave it out. Each of these failed before the fix.
/// </summary>
public class LeftOutShotTests
{
    // Five shots in a tight group and a flyer 3 in away, left out as a called flyer.
    private static readonly PointD[] Group = [new(0.1, 0.0), new(-0.1, 0.1), new(0.0, -0.1), new(0.15, 0.1), new(-0.05, -0.05)];

    private static MarkingState WithFlyer()
    {
        var session = new MarkingSession();
        session.Load(ShotCsv.Marking([.. Group, new(3, 0)], 3600));
        session.SetExclusion(session.State.Shots[^1].Id, ExclusionReason.CalledFlyer);
        return session.State;
    }

    private static double FiveShotMeanRadius => GroupAnalysis.Analyse(ShotCsv.Marking(Group, 3600)).AllShots!.MeanRadius!.Value;

    [Fact]
    public void TheReportedFiguresLeaveTheShotOutAndEveryShotIsKeptBeside()
    {
        var report = GroupAnalysis.Analyse(WithFlyer());
        Assert.Equal(1, report.Excluded);
        Assert.Equal(5, report.Counted!.Shots);
        Assert.Equal(FiveShotMeanRadius, report.Counted.MeanRadius!.Value, 12);
        Assert.Equal(6, report.AllShots!.Shots);
        Assert.True(report.AllShots.MeanRadius!.Value > 2 * FiveShotMeanRadius);
    }

    [Fact]
    public void ThePhonesResultAndTheSharedFiguresLeaveItOut()
    {
        var units = UnitSettings.Imperial;
        var (tiles, _) = ResultFigures.Build(WithFlyer(), units);
        Assert.Contains(tiles, t => t.Value.StartsWith(units.Length(FiveShotMeanRadius), StringComparison.Ordinal));
        var panel = AnalysisPanel.Build(WithFlyer(), units);
        var meanRadius = panel.AllFigures.Single(f => f.Key == "meanRadius");
        Assert.Contains(units.Number(FiveShotMeanRadius), meanRadius.Value, StringComparison.Ordinal);
        Assert.Equal("5", panel.AllFigures.Single(f => f.Key == "shots").Value);
    }

    [Fact]
    public void TheSavedSessionLeavesItOutAndKeepsItOnTheRecord()
    {
        var state = WithFlyer();
        var record = SessionRecords.Build(state, null, UnitSettings.Imperial, false, null, null, null, null, DateTime.UtcNow, DateTime.Now);
        Assert.Equal(FiveShotMeanRadius, record.MeanRadiusInches!.Value, 12);
        Assert.Equal(5, record.ShotCount);
        var (reopened, _) = MarkingFile.Read(record.MarkingJson);
        Assert.Equal(ExclusionReason.CalledFlyer, reopened.Shots[^1].Exclusion);
        Assert.Equal(FiveShotMeanRadius, GroupAnalysis.Analyse(reopened).Counted!.MeanRadius!.Value, 12);
    }

    [Fact]
    public void TheCsvSaysWhichShotWasLeftOut()
    {
        var rows = ShotCsv.Write(WithFlyer()).Trim().Split('\n');
        Assert.EndsWith(",yes", rows[^1], StringComparison.Ordinal);
        Assert.All(rows[1..^1], r => Assert.EndsWith(",no", r, StringComparison.Ordinal));
    }
}
