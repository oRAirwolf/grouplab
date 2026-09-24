using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 140 section 4: Alan's own evening, recreated. A twenty five shot sheet is analysed with a calibre, a rounds
/// fired and a distance; then a fifteen shot sheet is opened on top of it and analysed.
/// <para>
/// <b>What happened before this.</b> The second sheet inherited the first one's calibre, and with it a flag on every single one of the fifteen
/// holes: sixteen review items on a sheet with nothing wrong with it.
/// </para>
/// <para>
/// <b>And one part of it was not a carry-over at all.</b> "You fired 25 and 15 are marked" was the sheet's own arithmetic, twenty five bulls
/// on a GL-CF25-LTR with nothing typed anywhere, worded as though Alan had said it. He read it as the last sheet's count following him across,
/// which was a reasonable reading of a sentence that claimed he had said something he had not. The shortfall is real and stays; the claim is
/// gone (see <see cref="SilentShortfallTests"/>).
/// </para>
/// <para>
/// <see cref="NewTargetResetsTests"/> holds the same rule field by field, by reading the state's own properties. This one holds it end to end,
/// through two real analyses, because the fault was never in one field: it was in what a person sees after opening their second target.
/// </para>
/// </summary>
public class AlansSecondSheetTests
{
    [Fact]
    public void NothingOfTheTwentyFiveShotSheetReachesTheFifteenShotOne()
    {
        // The first sheet: twenty five shots, a calibre named, the rounds fired and the distance entered, as Alan had it.
        var first = GeneratedSheet.Detect(25, holeRadiusInches: 0.055);
        var session = GeneratedSheet.Marked(first.Holes, first.Definition, first.Truth, path: @"C:\first.jpg");
        session.SetCalibre(Calibre.Of(0.224));
        session.SetExpectedShots(25);
        session.SetShotDistance(3600);
        var before = session.State;
        Assert.Equal(25, before.Shots.Count(s => s.IsShot));
        var firstMarks = before.Shots.Select(s => s.Image).ToList();

        // The second sheet: fifteen 6.5 mm shots, opened and analysed straight afterwards.
        var second = GeneratedSheet.Detect(15);
        session = GeneratedSheet.Marked(second.Holes, second.Definition, second.Truth, session, @"C:\second.jpg");
        var now = session.State;

        Assert.Equal(15, now.Shots.Count(s => s.IsShot));
        Assert.Null(now.ExpectedShots);
        Assert.Null(now.Calibre);
        Assert.Null(now.ShotDistanceInches);
        Assert.Null(now.Rule);
        Assert.Empty(now.Dismissed ?? []);
        Assert.DoesNotContain(now.Shots.Select(s => s.Image), p => firstMarks.Contains(p));

        // The queue Alan met: "You fired 25 and 15 are marked", then fifteen "Possibly two holes". Neither survives.
        var items = ReviewQueue.For(now);
        Assert.DoesNotContain(items, i => i.Kind == ReviewKind.Oversized);
        Assert.DoesNotContain(items, i => i.Sentence.Contains("You fired", StringComparison.Ordinal));

        // The shortfall itself is right and stays: this GL-CF25-LTR sheet has twenty five bulls and fifteen of them were shot at. What was
        // wrong was the claim that he had said so, which is what sent him looking for the last sheet's count. Entry 140 section 4.
        var count = Assert.Single(items, i => i.Kind == ReviewKind.Count);
        Assert.StartsWith("This sheet takes 25 shots and 15 are marked. Nobody has said how many rounds were fired:", count.Sentence, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same two sheets with the setup carried over on purpose, entry 140 section 1.3: the equipment arrives because the person asked for
    /// it, and the counts and marks still do not.
    /// </summary>
    [Fact]
    public void TheSameSetupButtonCarriesTheEquipmentAndNothingElse()
    {
        var first = GeneratedSheet.Detect(25, holeRadiusInches: 0.055);
        var session = GeneratedSheet.Marked(first.Holes, first.Definition, first.Truth, path: @"C:\first.jpg");
        session.SetCalibre(Calibre.Of(0.224));
        session.SetExpectedShots(25);
        session.SetShotDistance(3600);

        var second = GeneratedSheet.Detect(15);
        session = GeneratedSheet.Marked(second.Holes, second.Definition, second.Truth, session, @"C:\second.jpg");
        session.SetCalibre(Calibre.Of(0.264));
        session.SetShotDistance(3600);

        Assert.Equal(0.264, session.State.Calibre!.DiameterInches, 6);
        Assert.Equal(3600, session.State.ShotDistanceInches);
        Assert.Null(session.State.ExpectedShots);
        Assert.Equal(15, session.State.Shots.Count(s => s.IsShot));
    }
}
