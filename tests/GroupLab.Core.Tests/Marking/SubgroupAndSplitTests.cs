using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 94 sections 2 and 4: which bulls hold which load, kept in the session and never in the sheet, and taking an
/// oversized mark as the two shots it is without reaching for the mouse.
/// </summary>
public class SubgroupAndSplitTests
{
    /// <summary>
    /// Entry 94 section 2 and entry 89 section 4: one sheet carrying two loads is compared load against load. The comparison half was
    /// already built and had nothing to feed it; this is the mapping that feeds it, and it changes nothing about the sheet.
    /// </summary>
    [Fact]
    public void TwoLoadsOnOneSheetAreReportedSeparatelyAndCompared()
    {
        var session = Sheet(bulls: 6, perBull: 1);
        for (int bull = 0; bull < 6; bull++)
        {
            session.SetSubgroup(bull, bull < 3 ? "41.5 gr" : "42.0 gr");
        }

        var report = GroupAnalysis.Subgroups(session.State)!;

        Assert.Equal(["41.5 gr", "42.0 gr"], report.Subgroups.Select(g => g.Name));
        Assert.All(report.Subgroups, g => Assert.Equal(3, g.Shots));
        Assert.Equal([0, 1, 2], report.Subgroups[0].Bulls);
        Assert.NotNull(report.DispersionPValue);
        Assert.NotNull(report.CentrePValue);
        Assert.InRange(report.DispersionPValue!.Value, 0, 1);
        Assert.InRange(report.CentrePValue!.Value, 0, 1);

        // The whole-sheet figures are untouched: a subgroup is a way of reading one sheet, not a different sheet.
        Assert.Equal(6, GroupAnalysis.Analyse(session.State).AllShots!.Shots);
    }

    /// <summary>A bull in no subgroup is in no subgroup, and with nothing mapped there is no subgroup report at all.</summary>
    [Fact]
    public void BullsLeftOutOfEverySubgroupAreLeftOut()
    {
        var session = Sheet(bulls: 4, perBull: 1);
        Assert.Null(GroupAnalysis.Subgroups(session.State));

        session.SetSubgroup(0, "A");
        session.SetSubgroup(1, "A");
        var report = GroupAnalysis.Subgroups(session.State)!;
        Assert.Equal(2, Assert.Single(report.Subgroups).Shots);
        Assert.Equal("needs shots in at least two subgroups", report.ComparisonUnavailable);

        session.SetSubgroup(0, null);
        Assert.Single(GroupAnalysis.Subgroups(session.State)!.Subgroups[0].Bulls);
        session.ClearSubgroups();
        Assert.Null(GroupAnalysis.Subgroups(session.State));
    }

    /// <summary>The mapping is session data, so a marking saved and reopened still knows which bulls held which load.</summary>
    [Fact]
    public void TheMappingSurvivesTheMarkingFile()
    {
        var session = Sheet(bulls: 4, perBull: 1);
        session.SetSubgroup(0, "41.5 gr");
        session.SetSubgroup(3, "42.0 gr");

        var (read, notes) = MarkingFile.Read(MarkingFile.Write(session.State));

        Assert.Empty(notes);
        Assert.Equal("41.5 gr", read.Subgroups!.For(0));
        Assert.Equal("42.0 gr", read.Subgroups.For(3));
        Assert.Null(read.Subgroups.For(1));
    }

    /// <summary>
    /// Entry 94 section 4: the merged pair is the item the flag exists to raise, and until now the only way to take it as two shots was a tap
    /// on the image, so the two-minute loop broke on exactly that item. The choice is on the item, it makes the second shot, and the flag goes
    /// from both because it was a statement about one mark that is now two.
    /// </summary>
    [Fact]
    public void AnOversizedMarkIsTakenAsTwoShotsFromTheQueue()
    {
        var session = Sheet(bulls: 2, perBull: 1);
        int shot = session.State.Shots[0].Id;
        session.Load(session.State with
        {
            Shots = session.State.Shots.Replace(
                session.State.Shots[0],
                session.State.Shots[0] with { Oversize = new DetectedOversize(1.9, false, new PointD(96, 100), new PointD(124, 100)) }),
        });

        var item = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Oversized);
        var split = Assert.Single(item.Choices, c => c.Action == ReviewAction.SplitIntoTwo);
        Assert.Equal("Two shots", split.Label);

        ReviewQueue.Apply(session, item, split);

        var shots = session.State.Shots.Where(s => s.IsShot).ToList();
        Assert.Equal(3, shots.Count);
        Assert.All(shots, s => Assert.Null(s.Oversize));
        Assert.Equal(new PointD(96, 100), session.State.Find(shot)!.Image);
        Assert.Contains(shots, s => s.Image == new PointD(124, 100) && s.Bull == session.State.Find(shot)!.Bull);
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Oversized);

        // The pair now shows as a bull holding two shots, which is the item that asks whether that is what happened.
        Assert.Contains(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Doubled);
    }

    /// <summary>A mark the detector could not halve offers no such choice, because there is nowhere to put the second shot.</summary>
    [Fact]
    public void AMarkWithNoHalvesOffersNoSplit()
    {
        var session = Sheet(bulls: 2, perBull: 1);
        session.Load(session.State with
        {
            Shots = session.State.Shots.Replace(session.State.Shots[0], session.State.Shots[0] with { Oversize = new DetectedOversize(1.9, false) }),
        });

        var item = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Oversized);
        Assert.DoesNotContain(item.Choices, c => c.Action == ReviewAction.SplitIntoTwo);
    }

    /// <summary>
    /// Entry 95 section 2, too few: the person fired six and five are marked. The marks nearest two holes' size are named largest first, and
    /// the first is taken as two shots from the item itself, which makes the count agree and the item go.
    /// </summary>
    [Fact]
    public void TooFewMarksNameTheOnesMostLikelyToBeTwoAndSplitTheFirst()
    {
        var session = Sized(new double[] { 0.95, 1.31, 1.02, 0.91, 1.12 });
        session.SetExpectedShots(6);

        var item = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
        // Entry 130 section 2b.2 put a sentence between the count and the ranking, saying where the missing shot is not. Here every bull has
        // something on it, which is the useful thing to know: the shot that is missing is inside a mark rather than on an empty bull.
        Assert.StartsWith("You fired 6 and 5 are marked. Every bull has a shot on it, so a mark may be two. Most likely to be two, closest to two holes' size first: shot 2 at 1.31 holes, shot 5 at 1.12 holes, shot 3 at 1.02 holes.", item.Sentence, StringComparison.Ordinal);
        Assert.Equal(3, ReviewQueue.CountCandidates);
        var two = item.Choices[0];
        Assert.Equal(ReviewAction.SplitIntoTwo, two.Action);

        ReviewQueue.Apply(session, item, two);

        Assert.Equal(6, session.State.Shots.Count(s => s.IsShot));
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }

    /// <summary>Entry 95 section 2, the reverse: more marks than rounds, which nobody was told of before. The smallest mark is named first.</summary>
    [Fact]
    public void TooManyMarksNameTheOnesLeastLikeAHole()
    {
        var session = Sized(new double[] { 0.95, 0.41, 1.02, 0.91, 1.12 });
        session.SetExpectedShots(4);

        var item = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
        Assert.Contains("You fired 4 and 5 are marked. Least like a hole, smallest first: shot 2 at 0.41 holes", item.Sentence, StringComparison.Ordinal);
        ReviewQueue.Apply(session, item, item.Choices[0]);
        Assert.True(session.State.Shots.Single(s => s.Size?.Holes == 0.41).NotAShot);
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }

    /// <summary>With no count stated, or one that agrees, there is no item; and the count survives the marking file.</summary>
    [Fact]
    public void AnAgreeingOrMissingCountRaisesNothingAndTheCountIsSaved()
    {
        var session = Sized(new double[] { 0.95, 1.02 });
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
        session.SetExpectedShots(2);
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);

        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.Equal(2, read.ExpectedShots);
        Assert.Equal(1.02, read.Shots[1].Size!.Holes);
    }

    /// <summary>A row of bulls, one detected shot on each, with the sizes given and two halves either side of each mark.</summary>
    private static MarkingSession Sized(double[] holes)
    {
        var session = Sheet(bulls: holes.Length, perBull: 1);
        session.Load(session.State with
        {
            Shots = [.. session.State.Shots.Select((s, k) => s with { Size = new MarkSize(holes[k], new PointD(s.Image.X - 5, s.Image.Y), new PointD(s.Image.X + 5, s.Image.Y)) })],
        });
        return session;
    }

    /// <summary>A sheet of bulls a fixed distance apart, each holding <paramref name="perBull"/> shots a little off centre.</summary>
    private static MarkingSession Sheet(int bulls, int perBull)
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        var aims = Enumerable.Range(0, bulls).Select(k => new BullAim(k, (k + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new PointD(100 + (200 * k), 100))).ToList();
        session.LoadDetections(
            session.State.Scale!,
            aims,
            [.. Enumerable.Range(0, bulls).SelectMany(k => Enumerable.Range(0, perBull).Select(j => (Image: new PointD(100 + (200 * k) + (3 * j) + (k % 2 == 0 ? 2 : -2), 100 + (2 * j) + (k % 3)), Bull: (int?)k)))],
            "a test sheet");
        return session;
    }
}
