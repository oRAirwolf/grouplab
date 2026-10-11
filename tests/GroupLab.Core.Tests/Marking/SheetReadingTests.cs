using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 2: the Groups section as "How did you shoot this sheet?" and "Loads on this sheet", and the line
/// reading back what GroupLab will make of it. The rules underneath are the ones the old controls made, so a saved session reads the same.
/// </summary>
public class SheetReadingTests
{
    /// <summary>A five by five sheet with a shot on each of the first <paramref name="shots"/> bulls.</summary>
    private static MarkingSession Sheet(int shots)
    {
        var bulls = new List<BullAim>();
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 5; c++)
            {
                int index = (r * 5) + c;
                bulls.Add(new BullAim(index, (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    new PointD(100 + (c * 100), 100 + (r * 100)), Scoring: true, Declared: new PointD(400 + (c * 500), 300 + (r * 500))));
            }
        }

        var session = new MarkingSession();
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), bulls,
            [.. bulls.Take(shots).Select(b => (new PointD(b.Image.X + 3, b.Image.Y + 2), (int?)b.Index))], "test");
        return session;
    }

    [Fact]
    public void EachAnswerIsTheRuleTheOldControlsMade()
    {
        var state = Sheet(25).State;
        Assert.Null(SheetReading.Rule(ShotPattern.OnePerBull, state.Bulls, null, null));
        Assert.Same(AssignmentRule.Nearest, SheetReading.Rule(ShotPattern.Nearest, state.Bulls, null, null));
        var two = SheetReading.Rule(ShotPattern.SomeTookMore, state.Bulls, null, new HashSet<int> { 0, 1 })!;
        Assert.Equal(2, two.For(0));
        Assert.Equal(1, two.For(5));
        Assert.Equal(ShotPattern.SomeTookMore, SheetReading.PatternOf(two));
        Assert.Equal([0, 1], SheetReading.Doubled(two).Order());
    }

    [Fact]
    public void TheBullsFiredAtAreLeftToGroupLabUntilABullIsTapped()
    {
        var session = Sheet(10);
        Assert.Equal(15, SheetReading.Unused(session.State).Count);
        Assert.Equal("Bulls left blank are found by themselves: 15 bulls unused. Wrong? Tap them.", SheetReading.UnusedLine(session.State));

        // Tapping bull 11 says it was fired at too: the ten GroupLab found and that one, and the offset solver has the person's own set.
        session.SetAssignmentRule(SheetReading.TapFiredAt(session.State, 10));
        var fired = SheetReading.FiredAt(session.State.Rule, session.State.Bulls)!;
        Assert.Equal(11, fired.Count);
        Assert.Equal(11, session.State.Rule!.Aimed!.Count);
        Assert.Equal("You said which bulls you fired at: 14 bulls unused. Tap a bull to change it.", SheetReading.UnusedLine(session.State));
        Assert.Equal(ShotPattern.OnePerBull, SheetReading.PatternOf(session.State.Rule));
    }

    [Fact]
    public void TheReadBackSaysTheBullsTheShotsAndTheLoads()
    {
        var session = Sheet(25);
        Assert.Equal("GroupLab will read it as: 25 bulls fired at, one shot each, no load named.", SheetReading.ReadBack(session.State));

        var rows = SheetReading.Rows(session.State.Bulls);
        string[] loads = ["Load A", "Load B", "Load C", "Load D", "Load E"];
        for (int i = 0; i < rows.Count; i++)
        {
            SheetReading.SetLoad(session, rows[i], loads[i]);
        }

        Assert.Equal(LoadLayout.OneARow, SheetReading.LayoutOf(session.State));
        Assert.Equal("GroupLab will read it as: 25 bulls fired at, one shot each, rows 1 to 5 as Load A to Load E.", SheetReading.ReadBack(session.State));

        session.SetAssignmentRule(AssignmentRule.Nearest);
        Assert.StartsWith("GroupLab will read it as: every hole to its nearest bull", SheetReading.ReadBack(session.State), StringComparison.Ordinal);
    }

    [Fact]
    public void ColumnsAreTheBullsAtTheSamePlaceInEveryRow()
    {
        var state = Sheet(0).State;
        var columns = SheetReading.Columns(state.Bulls);
        Assert.Equal(5, columns.Count);
        Assert.Equal([0, 5, 10, 15, 20], columns[0]);
    }

    [Fact]
    public void AnOldOnlyTheBullsIAimedAtRuleReadsAsBullsFiredAt()
    {
        var state = Sheet(10).State;
        var old = AimedBulls.Parse("rows 1-2", state.Bulls)!;
        Assert.Equal(10, SheetReading.FiredAt(old, state.Bulls)!.Count);
        Assert.Equal(ShotPattern.OnePerBull, SheetReading.PatternOf(old));
    }
}
