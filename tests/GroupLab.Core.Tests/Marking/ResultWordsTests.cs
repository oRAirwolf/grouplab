using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 280 section 2: the words of row 10's screens, shared by the phone and the desktop (Shots A, Zero from this
/// group, the aim points, the zero offset handed to Ballistics), and Share A's box lines and mean radius circle.
/// </summary>
public class ResultWordsTests
{
    private static readonly PointD[] Group = [new(0.3, 0.1), new(0.1, -0.2), new(0.5, 0.2), new(0.2, 0.3), new(0.4, -0.1), new(0.25, 0.05)];

    [Fact]
    public void ShotsASummaryAndRowsSayTheCountedShotsAndTheOffsets()
    {
        var session = new MarkingSession();
        session.Load(ShotCsv.Marking([.. Group, new PointD(-1, 0)], 3600) with { Rifle = new Rifle("Test rifle", 0.25, AngularUnit.Moa) });
        session.SetExclusion(session.State.Shots[^1].Id, ExclusionReason.ByShooter);
        string summary = ResultWords.ShotsSummary(GroupAnalysis.Analyse(session.State), UnitSettings.Imperial)!;
        Assert.StartsWith("6 shots counted, mean radius ", summary, StringComparison.Ordinal);
        Assert.EndsWith("; 1 left out by the shooter, still on the record.", summary, StringComparison.Ordinal);
        var row = ShotOffsets.Table(session.State)[0];
        Assert.Equal("0.300 in right, 0.100 in down", ResultWords.Offset(row, UnitSettings.Imperial));
        Assert.Equal($"{row.AcrossClicks!.Describe()}, {row.UpClicks!.Describe()}", ResultWords.Clicks(row));
        Assert.Contains("0.25 MOA a click", ResultWords.ShotsIntro(session.State), StringComparison.Ordinal);
    }

    [Fact]
    public void ZeroFromThisGroupSaysEachAxisTheScopeHowWellAndTheVerdict()
    {
        var state = ShotCsv.Marking([.. Group.Select(p => new PointD(p.X + 1, p.Y + 2))], 3600) with { Rifle = new Rifle("Test rifle", 0.25, AngularUnit.Moa) };
        var words = ResultWords.ZeroFrom(state, UnitSettings.Imperial);
        Assert.Null(words.Refusal);
        Assert.Equal(2, words.Axes.Count);
        Assert.StartsWith("Up and down: the group sits ", words.Axes[0], StringComparison.Ordinal);
        Assert.StartsWith("Across: the group sits ", words.Axes[1], StringComparison.Ordinal);
        Assert.Equal("Clicks at 0.25 MOA a click, the click value of Test rifle.", words.Scope);
        Assert.StartsWith("How well the center is known: ", words.HowWell, StringComparison.Ordinal);
        Assert.Equal("Worth dialing.", words.Verdict);
        Assert.NotNull(ResultWords.ZeroFrom(ShotCsv.Marking([new PointD(0, 0)], 3600, fromGroupCentre: true), UnitSettings.Imperial).Refusal);
    }

    [Fact]
    public void TheZeroOffsetIsTheAngleTheCentreSitsFromTheAim()
    {
        // The group sits 2 in low and 1 in right at 100 yd, so dial up about 1.91 MOA and left about 0.95.
        var state = ShotCsv.Marking([.. Group.Select(p => new PointD(p.X - 0.2917 + 1, p.Y - 0.0583 + 2))], 3600);
        var offset = ResultWords.ZeroOffsetFor(state)!;
        var centre = GroupAnalysis.Analyse(state).Counted!.CentreFromAim!.Value;
        Assert.Equal(Angular.Constant(AngularUnit.Moa) / 2 * Math.Atan(Math.Abs(centre.Y) / 3600), offset.UpMoa, 9);
        Assert.Equal(Angular.Constant(AngularUnit.Moa) / 2 * Math.Atan(Math.Abs(centre.X) / 3600), offset.LeftMoa, 9);
        Assert.Equal(Math.Abs(centre.Y), offset.UpInchesAt(3600), 9);
        Assert.StartsWith("With the zero offset of the group you carried in, ", offset.Words, StringComparison.Ordinal);
        Assert.Null(ResultWords.ZeroOffsetFor(ShotCsv.Marking(Group, null)));
    }

    [Fact]
    public void AnAimPointSaysItsOwnFigures()
    {
        var aim = new BullAim(0, "A", new PointD(0, 0));
        Assert.Equal("Aim A: no shots yet.", ResultWords.AimPoint(aim, null, UnitSettings.Imperial));
        var figures = GroupAnalysis.Analyse(ShotCsv.Marking(Group, 3600)).Counted!;
        string said = ResultWords.AimPoint(aim, figures, UnitSettings.Imperial);
        Assert.StartsWith("Aim A: 6 shots, mean radius ", said, StringComparison.Ordinal);
        Assert.Contains(", extreme spread ", said, StringComparison.Ordinal);
        Assert.EndsWith(" from its aim.", said, StringComparison.Ordinal);
    }

    [Fact]
    public void ShareABoxStartsWithTheTitleShotsMeanRadiusAndSpread()
    {
        var state = ShotCsv.Marking(Group, 3600) with { Load = "Test load" };
        var lines = ShareCard.Lines(state, "Test sheet", "2026-09-29", UnitSettings.Imperial);
        Assert.Equal(["title", "shots", "mean-radius", "extreme-spread"], lines.Where(l => l.Shown).Select(l => l.Key));
        Assert.Equal("Test sheet, 2026-09-29", lines[0].Text);
        Assert.Contains(lines, l => l.Key == "equipment" && l.Text == "Test load" && !l.Shown);
        Assert.Contains(lines, l => l.Key == "distance" && l.Text == "At 100 yd");
        Assert.Contains("MOA)", lines.Single(l => l.Key == "mean-radius").Text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMeanRadiusCircleIsDrawnAboutTheGroupsCentreOnThePicture()
    {
        var state = ShotCsv.Marking(Group, 3600);
        var circle = Assert.Single(ShareCard.Circles(state));
        var shots = state.Shots.Where(s => s.IsShot).ToList();
        // One scale everywhere, so the centre on the picture is the mean of the holes, and the radius the mean radius at the scale.
        Assert.Equal(shots.Average(s => s.Image.X), circle.Centre.X, 6);
        Assert.Equal(shots.Average(s => s.Image.Y), circle.Centre.Y, 6);
        double pixelsPerInch = Math.Abs(shots[0].Image.X - shots[1].Image.X) / Math.Abs(Group[0].X - Group[1].X);
        Assert.Equal(GroupAnalysis.Analyse(state).Counted!.MeanRadius!.Value * pixelsPerInch, circle.Radius, 6);

        var area = ShareCard.GroupArea(state, 1e6, 1e6)!.Value;
        Assert.Equal(area.Width, area.Height);
        Assert.All(shots, s => Assert.InRange(s.Image.X, area.X, area.X + area.Width));
    }

    [Fact]
    public void OnSeveralAimPointsThereIsACircleAtEach()
    {
        var session = new MarkingSession();
        session.Load(MarkingState.Empty);
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(1000, 0), 1));
        int left = session.AddBull(new PointD(20000, 20000)), right = session.AddBull(new PointD(30000, 20000));
        foreach (var (x, y) in new[] { (200.0, 0.0), (-200.0, 0.0), (0.0, 200.0), (0.0, -200.0), (100.0, 100.0) })
        {
            session.AddShot(new PointD(20000 + x, 20000 + y), left);
            session.AddShot(new PointD(30000 + x, 20000 + y), right);
        }

        var circles = ShareCard.Circles(session.State);
        Assert.Equal(2, circles.Count);
        Assert.Equal(20020, circles[0].Centre.X, 6);
        Assert.Equal(30020, circles[1].Centre.X, 6);
    }
}
