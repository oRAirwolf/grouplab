using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 39 section 1 and 40 section 1, the marking screen's two findings from its second human session:
/// hand-placed shots on a sheet of bulls are assigned to them, a group whose shots are not is not quoted, and a tap on printed artwork
/// is not snapped onto it. Entry 73 section 2: an oversized mark on the artwork is not declared to be ink, because a real hole on a
/// printed ring reads the same, so every explanation is named.
/// </summary>
public class ArtworkAndAssignmentTests
{
    private static readonly LengthReference Scale = new(new PointD(0, 0), new PointD(100, 0), 1);

    private static BullAim[] Bulls() => [new BullAim(0, "1", new PointD(200, 200)), new BullAim(1, "2", new PointD(500, 200)), new BullAim(2, "S1", new PointD(800, 200), Scoring: false)];

    [Fact]
    public void HandPlacedShotsTakeTheirNearestBullIncludingThoseMarkedBeforeDetectionAndFollowAMoveUnlessReassigned()
    {
        var session = new MarkingSession();
        session.SetScale(Scale);
        int early = session.AddShot(new PointD(510, 190));
        Assert.Null(session.State.Find(early)!.Bull);

        session.LoadDetections(Scale, Bulls(), [], "test");
        Assert.Equal(1, session.State.Find(early)!.Bull);

        int near0 = session.AddShot(new PointD(215, 190));
        Assert.Equal(0, session.State.Find(near0)!.Bull);

        session.MoveShot(near0, new PointD(490, 210));
        Assert.Equal(1, session.State.Find(near0)!.Bull);

        session.AssignBull(near0, 0);
        session.MoveShot(near0, new PointD(495, 205));
        Assert.Equal(0, session.State.Find(near0)!.Bull);

        session.AssignBull(early, null);
        session.MoveShot(early, new PointD(505, 195));
        Assert.Null(session.State.Find(early)!.Bull);
    }

    [Fact]
    public void OnASheetOfSeveralBullsUnassignedShotsWithholdTheFiguresAndSayWhy()
    {
        var session = new MarkingSession();
        session.LoadDetections(Scale, Bulls(), [], "test");
        PointD[] offsets = [new(5, 0), new(-3, 4), new(0, -6), new(2, 2), new(-4, -3), new(6, 5)];
        var ids = offsets.Select((o, k) => session.AddShot(new PointD((k % 2 == 0 ? 200 : 500) + o.X, 200 + o.Y))).ToList();

        var assigned = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.NotNull(assigned.MeanRadius);
        Assert.NotNull(assigned.CentreFromAim);

        session.AssignBull(ids[0], null);
        var one = GroupAnalysis.Analyse(session.State);
        Assert.Null(one.AllShots!.MeanRadius);
        Assert.Null(one.AllShots.CentreFromAim);
        Assert.Equal("1 of 6 shots is not assigned to a bull, so it would be measured from the point of aim and the rest from their bulls, and no figure means anything. Assign every shot to its bull.", one.AllShots.DispersionWithheld);

        foreach (int id in ids)
        {
            session.AssignBull(id, null);
        }

        session.SetExclusion(ids[1], ExclusionReason.CalledFlyer);
        var none = GroupAnalysis.Analyse(session.State);
        Assert.StartsWith("6 shots, none assigned to a bull, so figures from them would measure the spread of your marks", none.AllShots!.DispersionWithheld, StringComparison.Ordinal);
        Assert.Equal(5, none.WithoutExclusions!.Shots);
        Assert.Null(none.WithoutExclusions.MeanRadius);

        // One scoring bull: nothing to confuse, so the single aim is the right origin and the figures stand.
        var single = new MarkingSession();
        single.LoadDetections(Scale, [new BullAim(0, "1", new PointD(200, 200)), new BullAim(1, "S1", new PointD(800, 200), Scoring: false)], [], "test");
        single.SetPointOfAim(new PointD(200, 200));
        foreach (var o in offsets)
        {
            single.AssignBull(single.AddShot(new PointD(200 + o.X, 200 + o.Y)), null);
        }

        Assert.NotNull(GroupAnalysis.Analyse(single.State).AllShots!.MeanRadius);
    }

    /// <summary>Light paper, a printed ring of ink about the point (100, 100), and a hole in bare paper at (170, 100), with the ring as the known artwork.</summary>
    private static (GrayImage Value, GrayImage Artwork) Sheet()
    {
        const int size = 240;
        var value = Enumerable.Repeat((byte)235, size * size).ToArray();
        var artwork = Enumerable.Repeat((byte)255, size * size).ToArray();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double ring = Math.Sqrt(((x - 100.0) * (x - 100.0)) + ((y - 100.0) * (y - 100.0)));
                if (ring is >= 22 and <= 28)
                {
                    value[(y * size) + x] = 30;
                    artwork[(y * size) + x] = 0;
                }

                if (((x - 170.0) * (x - 170.0)) + ((y - 100.0) * (y - 100.0)) <= 36)
                {
                    value[(y * size) + x] = 40;
                }
            }
        }

        return (new GrayImage(size, size, value), new GrayImage(size, size, artwork));
    }

    [Fact]
    public void ATapOnPrintedArtworkIsPlacedWhereItWasTappedAndATapNearAHoleStillSnaps()
    {
        var (value, artwork) = Sheet();

        var onRing = Snapping.ToHole(value, new PointD(125, 100), 15, artwork);
        Assert.Equal(new PointD(125, 100), onRing.At);
        Assert.Contains("printed target", onRing.NotSnapped, StringComparison.Ordinal);

        var nearHole = Snapping.ToHole(value, new PointD(165, 104), 15, artwork);
        Assert.Null(nearHole.NotSnapped);
        Assert.Equal(170, nearHole.At.X, 1);
        Assert.Equal(100, nearHole.At.Y, 1);

        // Without the artwork, the old snap is unchanged: it cannot tell the ring from a hole.
        Assert.Equal(Snapping.ToDarkCentroid(value, new PointD(125, 100), 15), Snapping.ToHole(value, new PointD(125, 100), 15, null).At);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 87 section 1, keeping the evidence for why the screen's own size check was removed: the dark region under a
    /// mark takes in the printed line it touches, so a point on a ring reads far wider than a hole of the same size beside one. The extent is
    /// still measured, as a measurement; nothing judges a mark by it any more.
    /// </summary>
    [Fact]
    public void TheDarkRegionUnderAMarkOnAPrintedLineReadsFarWiderThanAHoleBesideOne()
    {
        var (value, _) = Sheet();

        double onRing = HoleSize.ApparentExtentPixels(value, new PointD(125, 100), 70)!.Value;
        double beside = HoleSize.ApparentExtentPixels(value, new PointD(170, 100), 70)!.Value;

        Assert.True(onRing > 3 * beside, $"on the ring {onRing:0.0} px, beside it {beside:0.0} px");
    }
}
