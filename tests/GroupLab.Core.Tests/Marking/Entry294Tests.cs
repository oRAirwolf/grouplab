using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 294 section 1, a mil shooter should never feel like an afterthought: a session in mil shows no MOA in any
/// aiming figure unless the person asked for it, a rifle's scope unit overrides Settings, and nothing about the angle is guessed from the
/// region.
/// </summary>
public class Entry294Tests
{
    private static readonly PointD[] Group = [new(0.3, 0.1), new(0.1, -0.2), new(0.5, 0.2), new(0.2, 0.3), new(0.4, -0.1), new(0.25, 0.05)];

    /// <summary>A group 2 in right and 3 in low of its aim at 100 yd, worth dialing on both axes.</summary>
    private static MarkingState Off(Rifle? rifle) =>
        ShotCsv.Marking([.. Group.Select(p => new PointD(p.X + 2, p.Y + 3))], 3600) with { Rifle = rifle };

    private static readonly UnitSettings MilSettings = UnitSettings.Imperial with { Angular = AngularUnit.Mrad };

    /// <summary>Every aiming sentence and figure the shared code writes for a marking, the zero, Zero from this group, the carried offset and the figures.</summary>
    private static List<string> AimingText(MarkingState state, UnitSettings units)
    {
        var said = new List<string>();
        var panel = AnalysisPanel.Build(state, units);
        said.AddRange(panel.Blocks.Where(b => b.Key is "zero" or "group").SelectMany(b => b.Figures).SelectMany(f => new[] { f.Value, f.Interval ?? "" }));
        var words = ResultWords.ZeroFrom(state, units);
        said.AddRange([.. words.Axes, words.Scope, words.HowWell, words.Verdict]);
        said.Add(ResultWords.ZeroOffsetFor(state, units)!.Words);
        var (tiles, sections) = ResultFigures.Build(state, units, units.Aiming(state.Rifle).Angular);
        said.AddRange(tiles.Select(t => t.Value));
        said.AddRange(sections.SelectMany(s => s.Figures).Select(f => f.Value + " " + f.Beneath));
        said.AddRange(ShareCard.Lines(state, "Test", "", units).Select(l => l.Text));
        return said;
    }

    [Fact]
    public void InMilModeNoAimingFigureSaysMoa()
    {
        var said = AimingText(Off(null), MilSettings);
        Assert.Contains(said, s => s.Contains(" mil", StringComparison.Ordinal));
        Assert.DoesNotContain(said, s => s.Contains("MOA", StringComparison.Ordinal));
    }

    [Fact]
    public void ARiflesScopeUnitOverridesSettings()
    {
        var mil = new Rifle("Comp rifle", 0.1, AngularUnit.Mrad);
        Assert.Equal(AngularUnit.Mrad, UnitSettings.Imperial.Aiming(mil).Angular);
        Assert.Equal(AngularUnit.Moa, MilSettings.Aiming(new Rifle("Hunting rifle", 0.25, AngularUnit.Moa)).Angular);
        Assert.Equal(AngularUnit.Moa, UnitSettings.Imperial.Aiming(null).Angular);

        // Settings says MOA, the rifle says mil: every aiming figure is in mil, with the rifle's clicks.
        var said = AimingText(Off(mil), UnitSettings.Imperial);
        Assert.DoesNotContain(said, s => s.Contains("MOA", StringComparison.Ordinal));
        Assert.Contains(said, s => s.Contains("clicks", StringComparison.Ordinal) && s.Contains(" mil", StringComparison.Ordinal));

        // And the other way round: an MOA hunting rifle in a mil setting reads in MOA.
        Assert.DoesNotContain(AimingText(Off(new Rifle("Hunting rifle", 0.25, AngularUnit.Moa)), MilSettings), s => s.Contains(" mil", StringComparison.Ordinal));
    }

    [Fact]
    public void TheZeroReadsInTheScopesUnitWithTheClicks()
    {
        var words = ResultWords.ZeroFrom(Off(new Rifle("Comp rifle", 0.1, AngularUnit.Mrad)), UnitSettings.Imperial);
        Assert.Matches(@"^Up and down: the group sits \d+\.\d\d mil low \(\d+\.\d{3} in\); dial \d+\.\d\d mil up, \d+ clicks up\.$", words.Axes[0]);
        Assert.Matches(@"^Across: the group sits \d+\.\d\d mil right \(\d+\.\d{3} in\); dial \d+\.\d\d mil left, \d+ clicks left\.$", words.Axes[1]);
        Assert.Equal(ResultWords.WhichRifle, ResultWords.ZeroFrom(Off(null), MilSettings, askWhichRifle: true).Scope);
    }

    [Fact]
    public void TheAngleIsNeverGuessedFromTheRegion()
    {
        Assert.Equal(UnitSettings.Unanswered, UnitSettings.ForRegion("US").Angular);
        Assert.Equal(UnitSettings.Unanswered, UnitSettings.ForRegion("DE").Angular);
        Assert.Equal(LinearUnit.Inch, UnitSettings.ForRegion("US").Linear);

        // The page's inches and centimeters toggle leaves the scope's unit alone.
        Assert.Equal(AngularUnit.Mrad, UnitSettings.ForAnalysis("imperial", MilSettings).Angular);
        Assert.Equal(AngularUnit.Moa, UnitSettings.ForAnalysis("metric", UnitSettings.Imperial).Angular);
    }

    [Fact]
    public void TheCommonClicksAreBothUnitsWithMilFirst()
    {
        Assert.Equal(["0.1 mil", "0.05 mil", "1/4 MOA", "1/8 MOA"], ScopeClicks.Common.Select(c => c.Words));
        Assert.Equal((0.1, AngularUnit.Mrad), ScopeClicks.Usual(AngularUnit.Mrad));
        Assert.Equal((0.25, AngularUnit.Moa), ScopeClicks.Usual(AngularUnit.Moa));
        Assert.Equal(1, ScopeClicks.IndexOf(0.05, AngularUnit.Mrad));
        Assert.Equal(-1, ScopeClicks.IndexOf(0.2, AngularUnit.Mrad));
        Assert.Equal(AngularUnit.Mrad, UnitSettings.AngularChoices[0]);
    }
}
