using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 5: the analysis page's right-hand panel rebuilt as clear blocks, each with a heading, a table of
/// figures and one headline figure.
/// <para>
/// <b>The test worth having is <see cref="EveryMeasuredFigureCanExplainItself"/>.</b> Entry 131 section 4.1 asks for a <b>?</b> beside every
/// figure, and a promise like that decays the first time somebody adds a row in a hurry. Held as a test, a figure with nothing to say about
/// itself cannot reach the page at all.
/// </para>
/// </summary>
public class AnalysisPanelTests
{
    /// <summary>A twenty shot group an inch across at a hundred yards, low and left of the aim, with a rifle whose scope is in mils.</summary>
    private static MarkingState Group(int shots = 20, Rifle? rifle = null)
    {
        var session = new MarkingSession(MarkingState.Empty with
        {
            // 100 pixels to the inch.
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            PointOfAim = new PointD(500, 500),
            ShotDistanceInches = 3600,
            Rifle = rifle,
        });

        var random = new Random(12);
        for (int k = 0; k < shots; k++)
        {
            session.AddShot(new PointD(530 + (random.NextDouble() * 40), 540 + (random.NextDouble() * 40)));
        }

        return session.State;
    }

    [Fact]
    public void ThePanelIsTheBlocksSection5Asks()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);

        Assert.Equal(["group", "zero", "shots", "equipment"], panel.Blocks.Select(b => b.Key));
        Assert.All(panel.Blocks, b => Assert.False(string.IsNullOrWhiteSpace(b.Heading), $"{b.Key} has no heading"));
    }

    /// <summary>One headline a block, no more: two figures in orange is two headlines, which is the busy panel section 5 is replacing.</summary>
    [Fact]
    public void EachBlockHasAtMostOneHeadline()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);

        Assert.All(panel.Blocks, b => Assert.True(b.Figures.Count(f => f.Headline) <= 1, $"{b.Key} has more than one headline"));
        Assert.True(panel.Blocks.Single(b => b.Key == "group").Figures.Single(f => f.Headline).Key == "meanRadius",
            "section 5.1 makes the mean radius the group block's headline");
    }

    /// <summary>
    /// Entry 131 section 4.1: a <b>?</b> beside each figure. Every row that carries a measured figure carries its explanation and its More
    /// link, and a figure added later with neither cannot reach the page without this test saying so.
    /// </summary>
    [Fact]
    public void EveryMeasuredFigureCanExplainItself()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);

        var measured = panel.AllFigures.Where(f => FigureExplanations.All.ContainsKey(f.Key)).ToList();
        Assert.NotEmpty(measured);
        Assert.All(measured, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.Explanation), $"{f.Key} is shown with no explanation behind its ?");
            Assert.False(string.IsNullOrWhiteSpace(f.More), $"{f.Key} is shown with nowhere for More to go");
        });
    }

    /// <summary>Until grouplab.org is published, More goes to the copy of the glossary that already reads on GitHub, entry 131 section 4.2.</summary>
    [Fact]
    public void MoreGoesToTheGithubCopyUntilTheSiteIsPublished()
    {
        var before = AnalysisPanel.Build(Group(), UnitSettings.Imperial);
        var after = AnalysisPanel.Build(Group(), UnitSettings.Imperial, sitePublished: true);

        Assert.All(before.AllFigures.Where(f => f.More is not null), f => Assert.StartsWith("https://github.com/", f.More!, StringComparison.Ordinal));
        Assert.All(after.AllFigures.Where(f => f.More is not null), f => Assert.StartsWith("https://grouplab.org/", f.More!, StringComparison.Ordinal));
    }

    /// <summary>
    /// A figure too small a group to quote keeps its row with the reason in it. The panel must not change shape as shots are added, and a
    /// person is owed the reason rather than a gap where a number was.
    /// </summary>
    [Fact]
    public void AFigureThatCannotBeQuotedKeepsItsRowAndGivesTheReason()
    {
        var panel = AnalysisPanel.Build(Group(shots: 3), UnitSettings.Imperial);
        var group = panel.Blocks.Single(b => b.Key == "group");

        var meanRadius = group.Figures.Single(f => f.Key == "meanRadius");
        Assert.True(meanRadius.Withheld);
        Assert.False(string.IsNullOrWhiteSpace(meanRadius.Value));
        Assert.Contains("5", meanRadius.Value, StringComparison.Ordinal);
        Assert.Equal(["shots", "meanRadius", "sigma", "extremeSpread"], group.Figures.Select(f => f.Key));
    }

    /// <summary>The metric reading changes the numbers and the unit beside them, and nothing else, entry 131 section 3.2.</summary>
    [Fact]
    public void ReadingItInMetricChangesTheNumbersAndNotTheShape()
    {
        var state = Group();
        var imperial = AnalysisPanel.Build(state, UnitSettings.Imperial);
        var metric = AnalysisPanel.Build(state, UnitSettings.Metric);

        Assert.Equal(imperial.AllFigures.Select(f => f.Key), metric.AllFigures.Select(f => f.Key));

        var a = imperial.Blocks.Single(b => b.Key == "group").Figures.Single(f => f.Key == "meanRadius");
        var b = metric.Blocks.Single(b => b.Key == "group").Figures.Single(f => f.Key == "meanRadius");
        Assert.Equal("in", a.Unit);
        Assert.Equal("cm", b.Unit);
        Assert.NotEqual(a.Value, b.Value);
    }

    /// <summary>
    /// Entry 131 section 3.1: where the rifle records its scope's unit, that unit leads and the other three stay visible beneath. A shooter
    /// reading a correction in a unit their turret does not use is being asked to convert it themselves at the bench.
    /// </summary>
    [Fact]
    public void TheScopesOwnUnitLeadsTheZeroBlock()
    {
        var mil = AnalysisPanel.Build(Group(rifle: new Rifle("Tikka", 0.1, AngularUnit.Mrad)), UnitSettings.Imperial);
        var zero = mil.Blocks.Single(b => b.Key == "zero");
        var elevation = zero.Figures.First(f => f.Label == "Elevation");

        Assert.Contains("mil", elevation.Value, StringComparison.Ordinal);
        Assert.Contains("click", elevation.Interval!, StringComparison.Ordinal);

        var others = zero.Figures.Single(f => f.Label == "Elevation, other units");
        Assert.Contains("MOA", others.Value, StringComparison.Ordinal);
        Assert.Contains("in", others.Value, StringComparison.Ordinal);
        Assert.Contains("cm", others.Value, StringComparison.Ordinal);
    }

    /// <summary>With no rifle recorded, MOA leads and no click count is invented, because a click is a property of a scope nobody named.</summary>
    [Fact]
    public void WithNoRifleTheCorrectionIsInMoaAndNoClicksAreGuessed()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);
        var elevation = panel.Blocks.Single(b => b.Key == "zero").Figures.First(f => f.Label == "Elevation");

        Assert.Contains("MOA", elevation.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("click", elevation.Interval ?? "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A marking with no distance has no zero block at all, rather than a heading over four rows of "needs the shot distance".</summary>
    [Fact]
    public void WithNoDistanceThereIsNoZeroBlock()
    {
        var state = Group() with { ShotDistanceInches = null };

        Assert.DoesNotContain(AnalysisPanel.Build(state, UnitSettings.Imperial).Blocks, b => b.Key == "zero");
    }

    /// <summary>The shots block lists every shot by the name the rest of the application gives it, entry 131 section 5.3.</summary>
    [Fact]
    public void TheShotsBlockNamesEveryShot()
    {
        var state = Group(shots: 6);
        var panel = AnalysisPanel.Build(state, UnitSettings.Imperial);
        var shots = panel.Blocks.Single(b => b.Key == "shots");

        Assert.Equal(6, shots.Figures.Count);
        Assert.Equal(ShotLabels.For(state).Select(l => l.Text ?? "shot"), shots.Figures.Select(f => f.Label));
    }

    /// <summary>The marks a shot carries are on its row, so a person can see a flyer or a left-out shot without opening it.</summary>
    [Fact]
    public void AShotsMarksAreOnItsRow()
    {
        var session = new MarkingSession(Group(shots: 6));
        int id = session.State.Shots[0].Id;
        session.SetFlyer(id, true);
        session.SetExclusion(id, ExclusionReason.PulledShot);

        // The list is in the sheet's own order, not the order the shots were added, so the row is found by the shot's id.
        var row = AnalysisPanel.Build(session.State, UnitSettings.Imperial).Blocks.Single(b => b.Key == "shots").Figures.Single(f => f.Key == "shot" + id.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Contains("flyer", row.Value, StringComparison.Ordinal);
        Assert.Contains("pulled shot", row.Value, StringComparison.Ordinal);
    }

    /// <summary>Equipment that was not recorded says so rather than showing an empty row, entry 131 section 5.4.</summary>
    [Fact]
    public void EquipmentNobodyRecordedSaysSo()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);
        var equipment = panel.Blocks.Single(b => b.Key == "equipment");

        Assert.All(equipment.Figures, f => Assert.Equal("not recorded", f.Value));
        Assert.All(equipment.Figures, f => Assert.True(f.Withheld));

        var withRifle = AnalysisPanel.Build(Group(rifle: new Rifle("Tikka", 0.25, AngularUnit.Moa)), UnitSettings.Imperial);
        var rows = withRifle.Blocks.Single(b => b.Key == "equipment").Figures;
        Assert.Equal("Tikka", rows.Single(f => f.Key == "rifle").Value);
        Assert.Equal("0.25 MOA a click", rows.Single(f => f.Key == "click").Value);
    }

    /// <summary>An exclusion is said in the block it changes, because docs/STATISTICS.md section 10 forbids an exclusion that is not visible.</summary>
    [Fact]
    public void AnExclusionIsSaidInTheGroupBlock()
    {
        var session = new MarkingSession(Group());
        session.SetExclusion(session.State.Shots[0].Id, ExclusionReason.BadRound);

        string? note = AnalysisPanel.Build(session.State, UnitSettings.Imperial).Blocks.Single(b => b.Key == "group").Note;

        Assert.Contains("1 shot is left out", note!, StringComparison.Ordinal);
    }

    /// <summary>No row is ever blank: every one says a number or says why there is not one.</summary>
    [Fact]
    public void NoRowIsBlankExceptAShotWithNothingToSay()
    {
        var panel = AnalysisPanel.Build(Group(), UnitSettings.Imperial);

        Assert.All(panel.AllFigures.Where(f => !f.Key.StartsWith("shot", StringComparison.Ordinal)),
            f => Assert.False(string.IsNullOrWhiteSpace(f.Value), $"{f.Key} shows nothing at all"));
        Assert.All(panel.AllFigures, f => Assert.False(string.IsNullOrWhiteSpace(f.Label), $"{f.Key} has no label"));
    }
}
