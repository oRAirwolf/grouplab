namespace GroupLab.Core.Detection;

/// <summary>What a mark cut by the edge of the image is: how much of it is there, and how wide the whole hole would be.</summary>
/// <param name="AtTheEdge">Whether the mark runs into the image boundary at all.</param>
/// <param name="VisibleShare">Roughly how much of the disc is in the picture, from 0 to 1.</param>
/// <param name="DiameterPixels">The whole hole's diameter, estimated from what can be seen of it.</param>
/// <param name="Enough">Whether enough of the rim is there to call it a hole rather than a guess.</param>
public sealed record PartialHole(bool AtTheEdge, double VisibleShare, double DiameterPixels, bool Enough);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.5: a hole cut by the edge of the scan is still a hole.
/// <para>
/// Scan 6's sixth shot was at the crop edge and was not detected. The reason is arithmetic rather than judgement: every size gate in the
/// detector measures a blob's area and turns it into a diameter, and half a hole has half the area, so it reads as a hole of about seven
/// tenths the width. A .22 hole cut in half measures like a speck and is refused as too small.
/// </para>
/// <para>
/// The way out is to stop using area for a mark that touches the boundary. A circle cut by a straight edge still shows its full extent along
/// that edge: a hole running off the left of the image is cut vertically, so its height is still the whole diameter as long as more than half
/// of it is in the picture. So the diameter comes from the extent parallel to the edge, and the area is used only to say how much is missing.
/// </para>
/// <para>
/// <b>It is marked partial rather than treated as ordinary.</b> Its centre is estimated from less evidence than a whole hole's, and a person
/// deciding whether to keep it should know that. Entry 130 section 2b.5 asks for exactly that: detected, and marked.
/// </para>
/// </summary>
public static class PartialHoles
{
    /// <summary>
    /// How much of the disc has to be in the picture. Below half, the extent along the edge is no longer the full diameter and the centre
    /// is a guess; a third is generous enough to catch a real shot at the crop line and mean enough to refuse a smudge at the border.
    /// </summary>
    public const double LeastVisible = 0.35;

    /// <summary>How near the boundary a blob has to come to count as touching it, in pixels.</summary>
    public const int Touching = 2;

    /// <summary>
    /// What to make of a blob that may be cut by the edge.
    /// </summary>
    /// <param name="left">The blob's bounding box in image pixels.</param>
    /// <param name="top">The blob's bounding box in image pixels.</param>
    /// <param name="width">The blob's bounding box in image pixels.</param>
    /// <param name="height">The blob's bounding box in image pixels.</param>
    /// <param name="area">The blob's filled area in pixels.</param>
    /// <param name="imageWidth">The image's size, to know where its edges are.</param>
    /// <param name="imageHeight">The image's size, to know where its edges are.</param>
    public static PartialHole Read(double left, double top, double width, double height, double area, int imageWidth, int imageHeight)
    {
        bool atLeft = left <= Touching;
        bool atTop = top <= Touching;
        bool atRight = left + width >= imageWidth - Touching;
        bool atBottom = top + height >= imageHeight - Touching;
        bool atTheEdge = atLeft || atTop || atRight || atBottom;

        if (!atTheEdge)
        {
            // Not at an edge, so the ordinary reading stands: a filled disc's area gives its diameter.
            double whole = 2 * Math.Sqrt(Math.Max(area, 0) / Math.PI);
            return new PartialHole(false, 1, whole, true);
        }

        bool cutVertically = atLeft || atRight;
        bool cutHorizontally = atTop || atBottom;

        // A corner is refused outright, and a failing test is why. Both extents are truncated there, so the bounding box cannot say how
        // wide the hole was, and every estimate from it looks complete: a quarter disc in a corner has a box of half the diameter each way,
        // and its area fills that box's circle exactly. The share would read 1.0 and call a quarter of a hole whole. There is nothing
        // dishonest to be done with a corner, so it is left for the shooter to place by hand.
        if (cutVertically && cutHorizontally)
        {
            return new PartialHole(true, 0, 0, false);
        }

        // Cut by a side means the height is the full diameter; cut by the top or bottom means the width is.
        double diameter = cutVertically ? height : width;

        if (diameter <= 0)
        {
            return new PartialHole(true, 0, 0, false);
        }

        double wholeArea = Math.PI * Math.Pow(diameter / 2, 2);
        double share = wholeArea <= 0 ? 0 : Math.Clamp(area / wholeArea, 0, 1);

        return new PartialHole(true, share, diameter, share >= LeastVisible);
    }
}
