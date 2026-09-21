using GroupLab.Core.Detection;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.5: scan 6's sixth shot was at the crop edge and was not detected.
/// <para>
/// The reason was arithmetic rather than judgement. Every size gate turns a blob's area into a diameter, and half a hole has half the area,
/// so it reads as a hole about seven tenths the width. A .22 hole cut in half measures like a speck and is refused as too small. These hold
/// the way out: for a mark touching the boundary the diameter comes from its extent along the edge, and the area only says how much is
/// missing.
/// </para>
/// </summary>
public class PartialHoleTests
{
    /// <summary>A disc of the given diameter, cut by a vertical line, as a blob's bounding box and area.</summary>
    private static (double Left, double Top, double Width, double Height, double Area) CutFromTheLeft(double diameter, double share)
    {
        double r = diameter / 2;
        return (0, 100, r * 2 * share, diameter, Math.PI * r * r * share);
    }

    [Fact]
    public void AWholeHoleAwayFromTheEdgeIsReadTheOrdinaryWay()
    {
        double diameter = 60;
        double area = Math.PI * Math.Pow(diameter / 2, 2);

        var read = PartialHoles.Read(500, 500, diameter, diameter, area, 5100, 6600);

        Assert.False(read.AtTheEdge);
        Assert.Equal(1, read.VisibleShare);
        Assert.Equal(diameter, read.DiameterPixels, 1);
        Assert.True(read.Enough);
    }

    /// <summary>
    /// The case itself: a hole running off the left of the image, half there. Its area says 42 pixels across; its height says 60, which is
    /// the truth.
    /// </summary>
    [Fact]
    public void AHoleCutInHalfByTheEdgeKeepsItsRealDiameter()
    {
        double diameter = 60;
        var (left, top, width, height, area) = CutFromTheLeft(diameter, 0.5);

        var read = PartialHoles.Read(left, top, width, height, area, 5100, 6600);

        Assert.True(read.AtTheEdge);
        Assert.True(read.Enough);
        Assert.Equal(diameter, read.DiameterPixels, 1);

        // What the old reading would have said, and why it was refused: area alone makes it seven tenths the width.
        double fromAreaAlone = 2 * Math.Sqrt(area / Math.PI);
        Assert.True(fromAreaAlone < diameter * 0.75);
    }

    [Fact]
    public void HowMuchOfItIsThereIsReported()
    {
        var (left, top, width, height, area) = CutFromTheLeft(60, 0.5);
        var read = PartialHoles.Read(left, top, width, height, area, 5100, 6600);

        Assert.InRange(read.VisibleShare, 0.45, 0.55);
    }

    /// <summary>A sliver at the border is not a hole, and this is what stops the change letting every edge smudge through.</summary>
    [Fact]
    public void ASliverAtTheBorderIsNotEnough()
    {
        var (left, top, width, height, area) = CutFromTheLeft(60, 0.12);
        var read = PartialHoles.Read(left, top, width, height, area, 5100, 6600);

        Assert.True(read.AtTheEdge);
        Assert.False(read.Enough);
        Assert.True(read.VisibleShare < PartialHoles.LeastVisible);
    }

    /// <summary>Each of the four edges behaves the same way, because a scan can be cropped on any of them.</summary>
    [Fact]
    public void EveryEdgeIsTreatedTheSameWay()
    {
        double d = 60, r = 30;
        double half = Math.PI * r * r * 0.5;

        var atLeft = PartialHoles.Read(0, 300, r, d, half, 5100, 6600);
        var atRight = PartialHoles.Read(5100 - r, 300, r, d, half, 5100, 6600);
        var atTop = PartialHoles.Read(300, 0, d, r, half, 5100, 6600);
        var atBottom = PartialHoles.Read(300, 6600 - r, d, r, half, 5100, 6600);

        foreach (var read in new[] { atLeft, atRight, atTop, atBottom })
        {
            Assert.True(read.AtTheEdge);
            Assert.True(read.Enough);
            Assert.Equal(d, read.DiameterPixels, 1);
        }
    }

    /// <summary>
    /// A corner is refused outright, and writing this test is what showed why. Both extents are truncated there, so the bounding box cannot
    /// say how wide the hole was: a quarter disc in a corner has a box half the diameter each way, and its area fills that box's circle
    /// exactly, so every share computed from it reads as 1.0 and calls a quarter of a hole whole. There is nothing honest to be done with a
    /// corner, so it is left for the shooter to place by hand.
    /// </summary>
    [Fact]
    public void ACornerIsRefusedBecauseItCannotBeMeasured()
    {
        double r = 30;
        double quarter = Math.PI * r * r * 0.25;

        var read = PartialHoles.Read(0, 0, r, r, quarter, 5100, 6600);

        Assert.True(read.AtTheEdge);
        Assert.False(read.Enough);
        Assert.Equal(0, read.DiameterPixels);
    }

    /// <summary>Nothing at all at the edge is refused rather than crashing on a division.</summary>
    [Fact]
    public void AnEmptyBlobAtTheEdgeIsRefused()
    {
        var read = PartialHoles.Read(0, 0, 0, 0, 0, 5100, 6600);

        Assert.True(read.AtTheEdge);
        Assert.False(read.Enough);
        Assert.Equal(0, read.DiameterPixels);
    }
}
