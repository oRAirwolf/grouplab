using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.3.4 and question 37: saying which bulls were aimed at.
/// <para>
/// <b>This is the one fact the shooter has and the sheet does not.</b> Twenty holes on a twenty five bull sheet does not say whether five
/// bulls were missed, five were never fired at, or every shot landed a bull away from its aim point. Entry 120's 6 ARC sheet is the third
/// case: every one of the twenty shots is nearer a bull it was not aimed at, so nearest bull assigns all twenty wrongly and the group that
/// comes out is tight, confident and about nothing.
/// </para>
/// <para>
/// The rows here come from where the bulls sit on the page rather than from their order in the definition, because a person reads rows off
/// the paper and a definition may list its bulls in any order it likes.
/// </para>
/// </summary>
public class AimedBullsTests
{
    /// <summary>A five by five sheet, listed deliberately out of order, so the row finder cannot pass by accident.</summary>
    private static IReadOnlyList<BullAim> Sheet(int rows = 5, int columns = 5)
    {
        var bulls = new List<BullAim>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int index = (r * columns) + c;
                bulls.Add(new BullAim(index, (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    new PointD(0, 0), Scoring: true, Declared: new PointD(400 + (c * 500), 300 + (r * 500))));
            }
        }

        // Listed back to front, which is what a definition is free to do.
        bulls.Reverse();
        return bulls;
    }

    [Fact]
    public void TheRowsComeFromThePageAndNotFromTheListOrder()
    {
        var rows = AimedBulls.Rows(Sheet());

        Assert.Equal(5, rows.Count);
        Assert.All(rows, r => Assert.Equal(5, r.Count));
        Assert.Equal([0, 1, 2, 3, 4], rows[0]);
        Assert.Equal([20, 21, 22, 23, 24], rows[4]);
    }

    /// <summary>A bull nobody aimed at expects no shots, which is what closes it to the matching.</summary>
    [Fact]
    public void ABullNobodyAimedAtExpectsNothing()
    {
        var bulls = Sheet();
        var rule = AimedBulls.For(bulls, [0, 1, 2], each: 1);

        Assert.Equal(1, rule.For(0));
        Assert.Equal(0, rule.For(7));
        Assert.False(rule.NearestOnly);
        Assert.Equal([0, 1, 2], AimedBulls.Of(rule, bulls));
    }

    /// <summary>Entry 120's own patterns, which are the ones a person actually shoots.</summary>
    [Fact]
    public void ThePresetsAreThePatternsPeopleShoot()
    {
        var bulls = Sheet();

        // Scan 1 and the 6.5 Creedmoor sheet: rows 1 to 3, one shot a bull.
        var rows123 = AimedBulls.RowsOf(bulls, [1, 2, 3]);
        Assert.Equal(Enumerable.Range(0, 15), AimedBulls.Of(rows123, bulls));

        // Scan 5, the 6 ARC sheet: bulls 2 to 5 of every row.
        var columns = AimedBulls.ColumnsOfEveryRow(bulls, [2, 3, 4, 5]);
        Assert.Equal(20, AimedBulls.Of(columns, bulls).Count);
        Assert.DoesNotContain(0, AimedBulls.Of(columns, bulls));
        Assert.Contains(1, AimedBulls.Of(columns, bulls));
        Assert.DoesNotContain(5, AimedBulls.Of(columns, bulls));

        // Scan 3: every bull.
        Assert.Equal(25, AimedBulls.Of(AimedBulls.EveryBull(bulls), bulls).Count);

        // Scan 4: every bull, but the shooter fired 23 rather than 25, which the count item is for rather than this.
        Assert.Equal(25, AimedBulls.EveryBull(bulls).PerBull.Values.Sum());
    }

    /// <summary>Two shots a bull is the same rule with a bigger number, not a different one.</summary>
    [Fact]
    public void MoreThanOneShotABullIsTheSameRule()
    {
        var bulls = Sheet();
        var rule = AimedBulls.RowsOf(bulls, [1], each: 2);

        Assert.Equal(2, rule.For(0));
        Assert.Equal(0, rule.For(10));
        Assert.Equal(10, rule.PerBull.Values.Sum());
    }

    /// <summary>
    /// A person has to be able to read back what they told GroupLab, because this is the input that decides what every figure afterwards is
    /// about. A sentence nobody can check is an input nobody can correct.
    /// </summary>
    [Fact]
    public void ItSaysBackWhatItWasTold()
    {
        var bulls = Sheet();

        Assert.Equal("25 shots at 25 bulls, one shot each.", AimedBulls.Says(AimedBulls.EveryBull(bulls), bulls));
        Assert.Equal("15 shots at 15 bulls, one shot each, and 10 bulls nobody aimed at.", AimedBulls.Says(AimedBulls.RowsOf(bulls, [1, 2, 3]), bulls));
        Assert.Equal("10 shots at 5 bulls, 2 shots each, and 20 bulls nobody aimed at.", AimedBulls.Says(AimedBulls.RowsOf(bulls, [1], each: 2), bulls));
        Assert.Equal("Every bull is expected to hold one shot.", AimedBulls.Says(null, bulls));
        Assert.Contains("nearest", AimedBulls.Says(AssignmentRule.Nearest, bulls), StringComparison.Ordinal);
    }

    /// <summary>
    /// The mechanism, on the pattern of the sheet that made this necessary: the rule leaves room at every bull that was aimed at and none at
    /// any other, which is what closes the wrong bulls to the matching.
    /// <para>
    /// <b>This tests the rule and not the matching.</b> Putting the 6 ARC sheet's shots through the matching end to end needs a registered
    /// sheet rather than a hand built one, and that proof is the one entry 141 section 5.3.4 asks for against entry 120's ground truth. It is
    /// not done yet, and this test does not pretend to be it.
    /// </para>
    /// </summary>
    [Fact]
    public void TheRuleLeavesRoomAtEveryBullAimedAtAndNoneAtAnyOther()
    {
        var bulls = Sheet();
        var aimed = AimedBulls.ColumnsOfEveryRow(bulls, [2, 3, 4, 5]);
        var wanted = AimedBulls.Of(aimed, bulls).ToHashSet();

        Assert.Equal(20, wanted.Count);
        foreach (var bull in bulls)
        {
            Assert.Equal(wanted.Contains(bull.Index) ? 1 : 0, aimed.For(bull.Index));
        }

        // Column 1 of every row is the bull a shot high and left would land nearest, and it is closed.
        foreach (var row in AimedBulls.Rows(bulls))
        {
            Assert.Equal(0, aimed.For(row[0]));
        }
    }
}
