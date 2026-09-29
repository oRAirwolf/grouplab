using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entry 278 section 5f: each shot's offset from the aim point, with the clicks that would bring it onto the aim.</summary>
public class ShotOffsetsTests
{
    [Fact]
    public void EachShotHasItsOffsetRightAndUpAndItsClicks()
    {
        // One shot 1 in right and 2 in low at 100 yd (screen axes are right and down). At a quarter MOA a click: 1 in is atan(1 / 3600), 0.9549
        // MOA, 3.82 clicks, so 4 left; 2 in is 1.9099 MOA, 7.64 clicks, so 8 up.
        var state = ShotCsv.Marking([new PointD(1, 2), new PointD(-0.5, -0.5)], 3600) with { Rifle = new Rifle("Test rifle", 0.25, AngularUnit.Moa) };
        var rows = ShotOffsets.Table(state);
        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].AcrossInches, 9);
        Assert.Equal(-2, rows[0].UpInches, 9);
        Assert.Equal("4 clicks left", rows[0].AcrossClicks!.Describe());
        Assert.Equal("8 clicks up", rows[0].UpClicks!.Describe());
        Assert.Equal("2 clicks down", rows[1].UpClicks!.Describe());
    }

    [Fact]
    public void WithoutAScopeThereAreNoClicksAndALeftOutShotIsMarkedNotDropped()
    {
        var session = new MarkingSession();
        session.Load(ShotCsv.Marking([new PointD(1, 2), new PointD(-0.5, -0.5)], null));
        session.SetExclusion(session.State.Shots[0].Id, ExclusionReason.PulledShot);
        var rows = ShotOffsets.Table(session.State);
        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].LeftOut);
        Assert.False(rows[1].LeftOut);
        Assert.All(rows, r => Assert.Null(r.AcrossClicks));
    }

    [Fact]
    public void NoAimMeansNoTable()
    {
        Assert.Empty(ShotOffsets.Table(ShotCsv.Marking([new PointD(1, 2), new PointD(0, 0)], 3600, fromGroupCentre: true)));
    }
}
