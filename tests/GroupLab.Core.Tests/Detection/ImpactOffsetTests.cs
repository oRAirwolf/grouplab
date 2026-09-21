using GroupLab.Core.Detection;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 3, from entry 120 section 2. Every sheet here is generated arithmetic: no scan, no photograph.
/// <para>
/// <b>The defect these are about is the worst this project has found.</b> On scan 5 every shot was measured against a bull it was not aimed
/// at, because each hole had landed nearer a neighbouring bull than its own. Nothing looked wrong. The group came out tight, it came out
/// centred, and the zero correction said there was nothing to dial. A shooter would have believed all three, and all three were false.
/// </para>
/// <para>
/// A group that is quietly wrong is worse than one that is obviously wrong and worse than no group at all, because it is the only kind that
/// changes what somebody does. The fix is to stop assuming shots land where they were aimed: find the translation that explains them, assign
/// in that frame, and report the translation as the point of impact, which is what the shooter came to measure in the first place.
/// </para>
/// </summary>
public class ImpactOffsetTests
{
    /// <summary>A grid of bulls, page dmm, at a pitch of 60 mm, as a multi-bull sheet has.</summary>
    private static List<Offset> Grid(int columns, int rows, double pitch = 600)
    {
        var bulls = new List<Offset>();
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                bulls.Add(new Offset(300 + (column * pitch), 300 + (row * pitch)));
            }
        }

        return bulls;
    }

    /// <summary>Shots around the bulls named, displaced by one translation, with a little spread so they are not exact.</summary>
    private static List<Offset> Shots(IEnumerable<int> bulls, IReadOnlyList<Offset> grid, Offset shift, int seed = 7)
    {
        var random = new Random(seed);
        var holes = new List<Offset>();
        foreach (int bull in bulls)
        {
            holes.Add(new Offset(
                grid[bull].X + shift.X + ((random.NextDouble() - 0.5) * 30),
                grid[bull].Y + shift.Y + ((random.NextDouble() - 0.5) * 30)));
        }

        return holes;
    }

    /// <summary>
    /// Scan 5, recreated: a whole sheet landing one bull up and to the left. Nearest-bull gives every shot to the wrong bull and produces a
    /// tight, centred, entirely false group. The offset has to come out at one pitch in each direction.
    /// </summary>
    [Fact]
    public void AWholeSheetShiftedOneBullUpAndLeftIsFound()
    {
        var grid = Grid(5, 5);
        var aimed = Enumerable.Range(0, 25).ToList();
        var shift = new Offset(-600, -600);
        var holes = Shots(aimed, grid, shift);

        var found = ImpactOffsets.Solve("the sheet", holes, grid, aimed);

        Assert.True(found.Certain, found.Why);
        Assert.True(Math.Abs(found.Shift.X - shift.X) < 20, $"across: wanted {shift.X}, found {found.Shift.X:0}");
        Assert.True(Math.Abs(found.Shift.Y - shift.Y) < 20, $"down: wanted {shift.Y}, found {found.Shift.Y:0}");
        Assert.True(found.Moved);

        // And it says where the group went, in inches, which is what gets dialled.
        Assert.Contains("in right", found.Shift.Describe(), StringComparison.Ordinal);
        Assert.Contains("in up", found.Shift.Describe(), StringComparison.Ordinal);
    }

    /// <summary>Shots that landed where they were aimed give no offset, so this cannot be passing by always finding a shift.</summary>
    [Fact]
    public void ASheetShotWhereItWasAimedGivesNoOffset()
    {
        var grid = Grid(5, 5);
        var aimed = Enumerable.Range(0, 25).ToList();
        var holes = Shots(aimed, grid, Offset.Zero);

        var found = ImpactOffsets.Solve("the sheet", holes, grid, aimed);

        Assert.True(found.Certain, found.Why);
        Assert.True(found.Shift.Length < 20, $"found a shift of {found.Shift.Length:0} dmm where there was none");
        Assert.False(found.Moved);
        Assert.Contains("landing where they were aimed", found.Why, StringComparison.Ordinal);
    }

    /// <summary>
    /// Scan 4, recreated: a sight change after the second row, so the rows above and below it landed in different places. Solved as two
    /// subgroups, each with its own offset, which is what a row break is for.
    /// </summary>
    [Fact]
    public void TwoOffsetsSplitAtARowAreFoundSeparately()
    {
        var grid = Grid(5, 4);
        var before = Enumerable.Range(0, 10).ToList();
        var after = Enumerable.Range(10, 10).ToList();

        var firstShift = new Offset(300, 200);
        var secondShift = new Offset(-250, 150);

        var first = ImpactOffsets.Solve("rows 1 and 2", Shots(before, grid, firstShift), grid, before);
        var second = ImpactOffsets.Solve("rows 3 and 4", Shots(after, grid, secondShift, seed: 11), grid, after);

        Assert.True(first.Certain, first.Why);
        Assert.True(second.Certain, second.Why);
        Assert.True(Math.Abs(first.Shift.X - firstShift.X) < 20);
        Assert.True(Math.Abs(second.Shift.X - secondShift.X) < 20);

        // The windage changed between them, which is the thing the shooter did and wants to see.
        Assert.True(Math.Abs(first.Shift.X - second.Shift.X) > 400);
    }

    /// <summary>
    /// Scan 6, recreated: two loads a row apart, each with its own point of impact, plus one shot far from everything. The flyer must not
    /// drag either offset, because an offset pulled by one wild shot moves every other shot's measurement with it.
    /// </summary>
    [Fact]
    public void AFarFlyerDoesNotDragTheOffset()
    {
        var grid = Grid(5, 2);
        var aimed = Enumerable.Range(0, 10).ToList();
        var shift = new Offset(150, -200);

        var clean = ImpactOffsets.Solve("the load", Shots(aimed, grid, shift), grid, aimed);

        var withFlyer = Shots(aimed, grid, shift);
        withFlyer.Add(new Offset(2400, 1800));
        var pulled = ImpactOffsets.Solve("the load", withFlyer, grid, aimed);

        // One shot in eleven may move the answer a little; it must not move it far.
        double moved = pulled.Shift.Minus(clean.Shift).Length;
        Assert.True(moved < 80, $"one flyer moved the point of impact by {moved:0} dmm, which would move every other shot's measurement with it");
    }

    /// <summary>
    /// Uncertainty is reported rather than hidden. With two shots and a symmetric grid there is more than one translation that explains them
    /// equally well, and saying so is the whole of entry 130 section 3.3.
    /// </summary>
    [Fact]
    public void AnAmbiguousSheetIsCalledUncertain()
    {
        var grid = Grid(3, 1);

        // Two holes exactly between bulls: the sheet could have been shot half a pitch left or half a pitch right.
        var holes = new List<Offset> { new(600, 300), new(1200, 300) };

        var found = ImpactOffsets.Solve("the sheet", holes, grid, [0, 1, 2]);

        Assert.False(found.Certain);
        Assert.Contains("more than one place", found.Why, StringComparison.Ordinal);
        Assert.Contains("Check the assignment", found.Why, StringComparison.Ordinal);
    }

    /// <summary>The answer does not depend on where the search began, which is what trying every hole-to-bull pair as a start buys.</summary>
    [Fact]
    public void TheAnswerDoesNotDependOnWhereTheSearchBegan()
    {
        var grid = Grid(4, 4);
        var aimed = Enumerable.Range(0, 16).ToList();

        // Two and a half pitches away: far enough that a search starting at zero would settle somewhere else entirely.
        var shift = new Offset(1500, -1200);
        var found = ImpactOffsets.Solve("the sheet", Shots(aimed, grid, shift), grid, aimed);

        Assert.True(Math.Abs(found.Shift.X - shift.X) < 20, $"across: wanted {shift.X}, found {found.Shift.X:0}");
        Assert.True(Math.Abs(found.Shift.Y - shift.Y) < 20, $"down: wanted {shift.Y}, found {found.Shift.Y:0}");
    }

    /// <summary>Nothing to place is not an error, and does not claim a shift.</summary>
    [Fact]
    public void NoShotsGivesNoOffsetAndNoComplaint()
    {
        var grid = Grid(3, 3);
        var found = ImpactOffsets.Solve("the sheet", [], grid, [0, 1, 2]);

        Assert.True(found.Certain);
        Assert.Equal(Offset.Zero, found.Shift);
        Assert.Contains("No shots", found.Why, StringComparison.Ordinal);
    }
}
