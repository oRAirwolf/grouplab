using Avalonia;

namespace GroupLab.Mobile;

/// <summary>
/// Error report 53's reply (entry 510 section 1): on a picture already enlarged, every touch is kept from the gesture recognizers, so the page
/// does not scroll under a drag (entry 374 section 4); but that kept the second finger of a pinch from them too, and a picture once let go of
/// could be enlarged no further and never made smaller again. The picture follows the fingers itself instead: two down make a pinch, the
/// change in their spread is the zoom, and nothing pans or taps until every finger is up.
/// </summary>
internal sealed class TwoFingers
{
    private readonly Dictionary<int, Point> down = [];
    private double spread;

    /// <summary>True from a second finger touching until the last one lifts.</summary>
    internal bool Pinching { get; private set; }

    /// <summary>Whether this finger is being followed.</summary>
    internal bool Has(int finger) => down.ContainsKey(finger);

    internal void Press(int finger, Point at)
    {
        down[finger] = at;
        if (down.Count >= 2)
        {
            spread = Spread();
            Pinching = true;
        }
    }

    /// <summary>The factor to zoom by since the last move, while two fingers are down; null otherwise.</summary>
    internal double? Move(int finger, Point at)
    {
        if (!down.ContainsKey(finger))
        {
            return null;
        }

        down[finger] = at;
        if (down.Count < 2)
        {
            return null;
        }

        double now = Spread();
        if (spread < 1 || now < 1)
        {
            spread = now;
            return null;
        }

        double factor = now / spread;
        spread = now;
        return factor;
    }

    internal void Release(int finger)
    {
        down.Remove(finger);
        if (down.Count >= 2)
        {
            spread = Spread();
        }

        if (down.Count == 0)
        {
            Pinching = false;
        }
    }

    private double Spread()
    {
        var two = down.Values.Take(2).ToArray();
        return Point.Distance(two[0], two[1]);
    }
}
