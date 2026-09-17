using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 75: a shot is named by its bull everywhere, a doubled bull's shots are 7a and 7b lettered by position rather
/// than by detection order, a shot with no bull is the word unassigned, and the order is the sheet's, scoring bulls before sighters.
/// </summary>
public class ShotLabelsTests
{
    private static readonly BullAim[] Bulls = [new(0, "1", new PointD(100, 100)), new(1, "2", new PointD(300, 100)), new(2, "S1", new PointD(100, 300), Scoring: false), new(3, "3", new PointD(500, 100))];

    [Fact]
    public void EachShotIsNamedByItsBullInTheSheetsOrder()
    {
        var state = MarkingState.Empty with
        {
            Bulls = [.. Bulls],
            Shots =
            [
                new MarkedShot(1, new PointD(510, 100), ShotProvenance.Automatic, Bull: 3),
                new MarkedShot(2, new PointD(100, 310), ShotProvenance.Automatic, Bull: 2),
                new MarkedShot(3, new PointD(95, 100), ShotProvenance.Automatic, Bull: 0),
                new MarkedShot(4, new PointD(300, 105), ShotProvenance.Manual, Bull: 1),
            ],
        };

        var labels = ShotLabels.For(state);

        Assert.Equal([(3, "1"), (4, "2"), (1, "3"), (2, "S1")], labels.Select(l => (l.ShotId, l.Text!)));
        Assert.All(labels, l => Assert.False(l.Abnormal));
    }

    /// <summary>Two shots on bull 2 are 2a and 2b, top one first, whichever the detector emitted first; neither can be read as bull 26 or 27.</summary>
    [Fact]
    public void ADoubledBullIsLetteredByPositionAndTheAbnormalCasesAreMarked()
    {
        var state = MarkingState.Empty with
        {
            Bulls = [.. Bulls],
            Shots =
            [
                new MarkedShot(1, new PointD(310, 120), ShotProvenance.Automatic, Bull: 1),
                new MarkedShot(2, new PointD(700, 700), ShotProvenance.Automatic, Bull: null),
                new MarkedShot(3, new PointD(290, 90), ShotProvenance.Automatic, Bull: 1),
                new MarkedShot(4, new PointD(90, 100), ShotProvenance.Automatic, NotAShot: true, Bull: 0),
                new MarkedShot(5, new PointD(100, 100), ShotProvenance.Automatic, Bull: 0),
            ],
        };

        var labels = ShotLabels.For(state).ToDictionary(l => l.ShotId);

        Assert.Equal(("2a", "2b"), (labels[3].Text, labels[1].Text));
        Assert.True(labels[3].Abnormal && labels[1].Abnormal);
        Assert.Equal(("1", false), (labels[5].Text, labels[5].Abnormal));
        Assert.Equal((ShotLabels.Unassigned, true), (labels[2].Text, labels[2].Abnormal));
        Assert.Equal((ShotLabels.NotAShot, true), (labels[4].Text, labels[4].Abnormal));
        Assert.Equal([5, 3, 1, 2, 4], ShotLabels.For(state).Select(l => l.ShotId));
    }

    /// <summary>A plain group has no printed numbers to name its shots by: no text, ordered by position, and nothing marked abnormal.</summary>
    [Fact]
    public void AMarkingWithoutBullsNamesNoShotAndOrdersThemByPosition()
    {
        var state = MarkingState.Empty with
        {
            Shots = [new MarkedShot(1, new PointD(300, 300), ShotProvenance.Manual), new MarkedShot(2, new PointD(360, 280), ShotProvenance.Manual)],
        };

        var labels = ShotLabels.For(state);

        Assert.Equal([2, 1], labels.Select(l => l.ShotId));
        Assert.All(labels, l => Assert.Null(l.Text));
        Assert.All(labels, l => Assert.False(l.Abnormal));
    }

    [Fact]
    public void LettersGoPastZ()
    {
        var shots = Enumerable.Range(0, 28).Select(k => new MarkedShot(k + 1, new PointD(100, k), ShotProvenance.Automatic, Bull: 0)).ToList();
        var labels = ShotLabels.For(MarkingState.Empty with { Bulls = [.. Bulls], Shots = [.. shots] });
        Assert.Equal(["1a", "1z", "1aa", "1ab"], new[] { 0, 25, 26, 27 }.Select(k => labels[k].Text!));
    }
}
