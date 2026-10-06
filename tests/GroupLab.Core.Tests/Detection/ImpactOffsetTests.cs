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
    /// <summary>
    /// Entry 374 section 1: the C bull sheet of 4 October, one shot of .300 Norma Magnum at each of bulls 1 to 15, all about an inch low and
    /// right; the 14 holes found in the phone's photo, inches on the page, bull 10's torn off at the edge. Read a row higher they fit as
    /// well, as fired at bulls 6 to 20; the shooting order decides, as Alan's own drawing of hole to bull does.
    /// </summary>
    [Fact]
    public void ASheetShotOneRowOffIsReadInTheOrderItWasShot()
    {
        (double X, double Y)[] holes = [(1.495, 2.674), (4.556, 2.543), (3.066, 3.107), (6.147, 3.178), (7.514, 2.971), (2.082, 4.668), (3.335, 4.696),
            (4.772, 4.666), (6.238, 4.990), (2.008, 6.189), (3.327, 5.764), (4.567, 6.124), (6.035, 6.281), (8.016, 6.057)];
        var bulls = Enumerable.Range(0, 25).Select(i => new Offset(254 * (1.260 + (1.496 * (i % 5))), 254 * (2.126 + (1.496 * (i / 5))))).ToList();
        var found = ImpactOffsets.WholeSheet([.. holes.Select(h => new Offset(254 * h.X, 254 * h.Y))], bulls, [.. Enumerable.Range(0, 25)]);
        Assert.NotNull(found);
        Assert.InRange(found.Shift.X / 254, 0.3, 0.7);
        Assert.InRange(found.Shift.Y / 254, 0.8, 1.1);
    }

    /// <summary>The C bull sheet's 25 bulls, page dmm: 1.496 in apart, the first at 1.260, 2.126 in.</summary>
    private static List<Offset> CBullBulls() =>
        [.. Enumerable.Range(0, 25).Select(i => new Offset(254 * (1.260 + (1.496 * (i % 5))), 254 * (2.126 + (1.496 * (i / 5)))))];

    /// <summary>The 14 holes detection found on Alan's tablet photo of 5 October, page inches, bulls 1 to 9 and 11 to 15 in that order.</summary>
    private static readonly (double X, double Y)[] TabletHoles = [(1.494, 2.727), (3.071, 3.111), (4.551, 2.551), (6.137, 3.183), (7.476, 2.992),
        (2.077, 4.666), (3.329, 4.696), (4.776, 4.659), (6.230, 5.001), (2.005, 6.193), (3.320, 5.768), (4.569, 6.131), (6.021, 6.296), (7.995, 6.057)];

    /// <summary>
    /// Entry 376 section A1: the same photo with bull 10's hole, torn at the paper's right edge, added by hand wherever a thumb might put it.
    /// The rule this replaced let one hole in ten fall outside the shooting order, so with fifteen holes the reading a row higher (bulls 6 to
    /// 20) passed as well, the two tied, and the sheet fell back to nearest bull: bulls 1, 3 and 6 to 20, "Shot 20" first on the Shots page.
    /// Every place has to come out as bulls 1 to 15, without asking.
    /// </summary>
    [Theory]
    [InlineData(7.70, 4.40)]
    [InlineData(7.95, 4.60)]
    [InlineData(8.20, 4.70)]
    [InlineData(8.40, 4.90)]
    [InlineData(8.40, 4.30)]
    public void TheTabletPhotoWithTheHandAddedHoleIsBulls1To15(double x, double y)
    {
        var holes = TabletHoles.Take(9).Append((X: x, Y: y)).Concat(TabletHoles.Skip(9)).Select(h => new Offset(254 * h.X, 254 * h.Y)).ToList();
        var read = ImpactOffsets.ReadWholeSheet(holes, CBullBulls(), [.. Enumerable.Range(0, 25)]);

        Assert.NotNull(read);
        Assert.False(read.Ask);
        Assert.Equal(Enumerable.Range(0, 15), read.Taken.Bulls);
        Assert.InRange(read.Taken.Shift.X / 254, 0.3, 0.7);
        Assert.InRange(read.Taken.Shift.Y / 254, 0.8, 1.15);
        Assert.Contains(read.Choices, c => c.Bulls.SequenceEqual(Enumerable.Range(5, 15)));
    }

    /// <summary>The 14 holes as detection found them read as bulls 1 to 9 and 11 to 15, bull 10 left empty.</summary>
    [Fact]
    public void TheTabletPhotoAsDetectedLeavesBull10Empty()
    {
        var read = ImpactOffsets.ReadWholeSheet([.. TabletHoles.Select(h => new Offset(254 * h.X, 254 * h.Y))], CBullBulls(), [.. Enumerable.Range(0, 25)]);

        Assert.NotNull(read);
        Assert.False(read.Ask);
        Assert.Equal(Enumerable.Range(0, 15).Where(b => b != 9), read.Taken.Bulls);
    }

    /// <summary>
    /// Fifteen shots at bulls 11 to 25, landing where they were aimed. Read in shooting order they would be bulls 1 to 15 with the rifle two
    /// rows low, which is further off than the bulls are apart; the holes fit several sheets equally, so the person is asked, and the guess
    /// offered first is where they landed.
    /// </summary>
    [Fact]
    public void AShotSheetThatFitsSeveralWaysAsks()
    {
        var grid = Grid(5, 5);
        var read = ImpactOffsets.ReadWholeSheet(Shots(Enumerable.Range(10, 15), grid, new Offset(100, 100)), grid, [.. Enumerable.Range(0, 25)]);

        Assert.NotNull(read);
        Assert.True(read.Ask);
        Assert.Equal(Enumerable.Range(10, 15), read.Taken.Bulls);
        Assert.Contains(read.Choices, c => c.Bulls.SequenceEqual(Enumerable.Range(0, 15)));
    }

    /// <summary>Ten shots at bulls 1 to 10 where they were aimed: read as aimed without asking, and nothing moved.</summary>
    [Fact]
    public void ASheetShotFromBull1WhereAimedIsLeftAlone()
    {
        var grid = Grid(5, 5);
        var holes = Shots(Enumerable.Range(0, 10), grid, Offset.Zero);
        var read = ImpactOffsets.ReadWholeSheet(holes, grid, [.. Enumerable.Range(0, 25)]);

        Assert.NotNull(read);
        Assert.False(read.Ask);
        Assert.Equal(Enumerable.Range(0, 10), read.Taken.Bulls);
        Assert.Null(ImpactOffsets.WholeSheet(holes, grid, [.. Enumerable.Range(0, 25)]));
    }

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
