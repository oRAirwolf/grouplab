using GroupLab.Core.Detection;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 140 section 3: the "Possibly two holes" check, and what it does when its assumption is wrong.
/// <para>
/// <b>What Alan saw.</b> He opened a fifteen shot 6.5 mm sheet straight after a smaller calibre, the calibre followed him across, and every
/// one of the fifteen holes was flagged at 2.0 to 2.36 holes' area: sixteen review items on a sheet with nothing wrong with it. A queue that
/// cries wolf teaches people to ignore it, which costs far more than any of those flags were worth.
/// </para>
/// <para>
/// The sheets here are generated, so the flags are the detector's own: fifteen holes of one size on the first fifteen bulls of GL-CF25-LTR,
/// and in the last test a genuine pair merged into one mark. The wrong calibre is taken as seven tenths of the size this sheet's own marks
/// read at, which puts every hole at about two holes' area, the figures on Alan's screenshot.
/// </para>
/// </summary>
public class CryingWolfTests
{
    private const int Shots = 15;

    private static List<ReviewItem> Oversized(MarkingState state) =>
        [.. ReviewQueue.For(state).Where(i => i.Kind == ReviewKind.Oversized)];

    /// <summary>
    /// Entry 140 sections 3.1 and 3.3: fifteen single holes of one calibre, with no calibre stated, raise nothing. The size they are judged
    /// against is the sheet's own marks, which is what they are, so none of them is twice its neighbours.
    /// </summary>
    [Fact]
    public void FifteenSingleHolesWithNoCalibreStatedRaiseNoDoubles()
    {
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots);

        // The size came from this sheet's own round marks and not from an assumed calibre, which is what section 3.1 asks for.
        Assert.Equal(HoleSizeSource.Sheet, holes.HoleSize!.Source);
        Assert.Equal(Shots, holes.Holes.Count);
        Assert.DoesNotContain(holes.Holes, h => h.Oversized);
        Assert.Empty(Oversized(GeneratedSheet.Marked(holes, definition, truth).State));
    }

    /// <summary>
    /// Alan's own case, and what entry 141 section 4 did to it: the same sheet with a calibre too small for it now flags **nothing**, because
    /// fifteen round marks outrank a stated calibre and the sheet's own quarter-point is what a hole on this sheet actually measures.
    /// <para>
    /// This is a better answer than entry 140 section 3.2's one question, and it is the one question 38 earned: a hole is not the bullet, and
    /// how much smaller it is depends on the paper, the backing and, on a photograph, the light. The flood guard is still there for a sheet
    /// with too few marks to speak for itself, which is the test below.
    /// </para>
    /// </summary>
    [Fact]
    public void AWrongSmallerCalibreNoLongerFloodsBecauseTheSheetOutranksIt()
    {
        double sheet = GeneratedSheet.Detect(Shots).Holes.HoleSize!.FlagInches!.Value;
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots, calibreInches: 0.7 * sheet);

        Assert.Equal(HoleSizeSource.Sheet, holes.HoleSize!.Source);
        Assert.Contains("so the sheet's own marks are used", holes.HoleSize.Description, StringComparison.Ordinal);
        Assert.DoesNotContain(holes.Holes, h => h.Oversized);
        Assert.Empty(Oversized(GeneratedSheet.Marked(holes, definition, truth).State));
    }

    /// <summary>
    /// Entry 140 section 3.2's flood guard was written for this sheet: too few marks to speak for themselves and a calibre too small, so every
    /// mark was flagged and the guard turned five items into one question about the calibre.
    /// <para>
    /// <b>NOTES-FROM-PLANNING.md entry 161 removed the flood at its source</b>, which is better than a guard on it. Five marks are now enough
    /// for the sheet's own reference, named calibre or not, and below five a calibre flags nothing, because a flag from the calibre alone is
    /// the hole-to-bullet constant again and entry 161 measured that constant wrong by nearly half a hole's area on a real scan. So the same
    /// wrong calibre on the same small sheet now raises nothing at all, which is the right answer for five single holes.
    /// </para>
    /// </summary>
    [Fact]
    public void ASmallSheetWithAWrongCalibreRaisesNothing()
    {
        double sheet = GeneratedSheet.Detect(Shots).Holes.HoleSize!.FlagInches!.Value;
        foreach (int few in new[] { 5, 4 })
        {
            var (holes, definition, truth) = GeneratedSheet.Detect(few, calibreInches: 0.7 * sheet);

            Assert.Equal(few >= 5 ? HoleSizeSource.SheetTentative : HoleSizeSource.Calibre, holes.HoleSize!.Source);
            Assert.DoesNotContain(holes.Holes, h => h.Oversized);
            Assert.Empty(Oversized(GeneratedSheet.Marked(holes, definition, truth).State));
        }
    }

    /// <summary>
    /// Entry 140 section 3.3: quietening the flood must not quieten the thing the flag is for. One mark holding two holes among fourteen
    /// singles is still raised, and only it.
    /// </summary>
    [Fact]
    public void ARealDoubleAmongSinglesIsStillFound()
    {
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots, withARealDouble: true);
        var state = GeneratedSheet.Marked(holes, definition, truth).State;
        int paired = GeneratedSheet.Scoring(definition).ElementAt(2).Index;

        // The two holes overlap too far for their shape to ask to be cut, so they arrive as one mark rather than two.
        Assert.Equal(Shots, holes.Holes.Count);

        var item = Assert.Single(Oversized(state));
        Assert.NotEqual("oversized:all", item.Key);
        var shot = state.Find(item.ShotId!.Value)!;
        Assert.Equal(paired, shot.Bull);

        // Two holes this far over each other hold well under two holes' worth of paper, which is why the threshold is 1.35 and not 2.
        Assert.True(shot.Oversize!.Holes >= new RenderDifferenceOptions().OversizeHoles, $"the pair read as {shot.Oversize.Holes:0.00} holes");
        Assert.All(state.Shots.Where(s => s.Id != shot.Id), s => Assert.Null(s.Oversize));
    }

    /// <summary>
    /// Entry 141 section 4.2: the sheet's own reference has to survive the very doubles it is judging. A third of this sheet's marks hold two
    /// holes each, and the quarter-point is unmoved by them, because a merged pair measures larger than anything else and sits at the top of
    /// the order. A mean of every mark would be pulled up by each one, and on a sheet like this it would not be a hole size at all.
    /// </summary>
    [Fact]
    public void DoublesAmongTheMarksDoNotMoveTheSheetsOwnReference()
    {
        var plain = GeneratedSheet.Detect(Shots);
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots, doubledBulls: 3);

        Assert.Equal(HoleSizeSource.Sheet, holes.HoleSize!.Source);

        // The reference moves by less than a thousandth of an inch, with a fifth of the marks now pairs.
        Assert.Equal(plain.Holes.HoleSize!.FlagInches!.Value, holes.HoleSize.FlagInches!.Value, 3);

        // And the doubles are what gets flagged, not the singles.
        var state = GeneratedSheet.Marked(holes, definition, truth).State;
        Assert.InRange(state.Shots.Count(s => s.Oversize is not null), 1, 5);
        Assert.All(Oversized(state), i => Assert.NotEqual("oversized:all", i.Key));
    }

    /// <summary>
    /// The boundary, and it was a conflict between two entries rather than a fault: at a third doubles the marks fall into two clear sizes,
    /// and entry 82 section 3 refused to read a size from a sheet like that and asked for the calibre instead. Entry 141 section 4.2 asks
    /// for the sheet's own reference to survive exactly this, and question 40 carried the conflict.
    /// <para>
    /// <b>Entry 149 section 2 settled it and amends entry 82 section 3.</b> The sheet is still recognised as carrying two sizes and the
    /// calibre is still asked for, because which of the two sizes a single shot makes is the thing not known. But a size is read now, the
    /// quarter-point of the smaller group, so the doubles are flagged. Refusing flagged nothing, and a sheet carrying five doubles is
    /// exactly the sheet where flagging nothing is worst.
    /// </para>
    /// </summary>
    [Fact]
    public void AThirdBeingDoublesStillAsksForTheCalibreAndNowFlagsTheDoubles()
    {
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots, doubledBulls: 5);

        Assert.Equal(HoleSizeSource.TwoSizes, holes.HoleSize!.Source);
        Assert.Contains("name the calibre", holes.HoleSize.Description, StringComparison.Ordinal);

        // A size is read, and it is the smaller group's, so it sits below the doubles rather than between the two groups.
        Assert.NotNull(holes.HoleSize.FlagInches);

        // And it is the doubles that are flagged, not every mark on the sheet.
        var state = GeneratedSheet.Marked(holes, definition, truth).State;
        int flagged = state.Shots.Count(s => s.Oversize is not null);
        Assert.InRange(flagged, 1, 8);
        Assert.All(Oversized(state), i => Assert.NotEqual("oversized:all", i.Key));
    }

    /// <summary>
    /// Entry 140 section 3.2's own boundary: a sheet where a couple of marks are flagged is not a sheet with the wrong calibre, and those marks
    /// are still raised one by one. Three of fifteen is a fifth of the sheet, well under the share at which the calibre becomes the likelier
    /// explanation, and a person who fired three doubles deserves to be told which three.
    /// </summary>
    [Fact]
    public void AFewFlaggedMarksAreStillRaisedOneByOne()
    {
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots);
        var marked = GeneratedSheet.Marked(holes, definition, truth).State;
        var three = marked.Shots.Where(s => s.IsShot).Take(3).Select(s => s.Id).ToHashSet();
        var state = marked with { Shots = [.. marked.Shots.Select(s => three.Contains(s.Id) ? s with { Oversize = new DetectedOversize(1.9, false) } : s)] };

        var items = Oversized(state);
        Assert.Equal(3, items.Count);
        Assert.All(items, i => Assert.NotEqual("oversized:all", i.Key));
    }
}
