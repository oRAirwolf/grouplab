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
    /// Entry 140 sections 3.2 and 3.3, Alan's own case: the same sheet with a calibre too small for it. Every hole reads as about two, and the
    /// queue says so once, about the calibre, rather than fifteen times about the holes.
    /// </summary>
    [Fact]
    public void AWrongSmallerCalibreRaisesOneQuestionAboutTheCalibreNotFifteenItems()
    {
        double sheet = GeneratedSheet.Detect(Shots).Holes.HoleSize!.FlagInches!.Value;
        var (holes, definition, truth) = GeneratedSheet.Detect(Shots, calibreInches: 0.7 * sheet);

        Assert.Equal(Shots, holes.Holes.Count(h => h.Oversized));
        Assert.All(holes.Holes, h => Assert.InRange(h.SizeHoles!.Value, 1.5, 2.6));

        var item = Assert.Single(Oversized(GeneratedSheet.Marked(holes, definition, truth).State));
        Assert.Equal("oversized:all", item.Key);
        Assert.Contains("15 of the 15 marks", item.Sentence, StringComparison.Ordinal);
        Assert.Contains("the calibre is wrong", item.Sentence, StringComparison.Ordinal);
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
