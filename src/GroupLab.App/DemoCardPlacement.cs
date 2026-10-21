using Avalonia;

namespace GroupLab.App;

/// <summary>Which side of the outlined part the tour's card is on, or the bottom of the window where no side has room.</summary>
internal enum DemoCardSide
{
    Right,
    Left,
    Below,
    Above,
    Bottom,
}

/// <summary>
/// Where the tour's card goes: its top left corner, the side it is on, and where its pointer sits, the centre of the small diamond on the
/// card's edge that points at the part; no pointer where the part is not beyond that edge.
/// </summary>
internal readonly record struct DemoCardPlace(Point At, DemoCardSide Side, Point? Pointer);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 item 6A, chosen by Alan from the mockups for entry 462 section 3 (the tour's card covered the panel
/// text it was describing): the card is placed beside the outlined part, on whichever side has room, never over the part, with a small
/// pointer toward it. A part less tall than the card, a button or a line, takes the card below or above it; a taller one, a panel or the
/// plot, to its right or left, so the card lies along the part rather than hanging past it. Of those two sides, the one where the whole
/// card fits with the most room is taken, else the better of the other two; along that side the card is centred on the part as far as
/// the window allows. If no side has room, the card falls back to the bottom of the window, at the left, the middle or the right,
/// whichever covers least of the part. Pure, so the rule is held by tests rather than by looking; the phone's file links this one.
/// </summary>
internal static class DemoCardPlacement
{
    /// <summary>Half the diagonal of the pointer's diamond, how far it stands out from the card's edge.</summary>
    public const double PointerReach = 10;

    /// <summary>The space between the part's outline and the card, room for the pointer and a little air.</summary>
    public const double Gap = 14;

    /// <summary>
    /// Places a card of size <paramref name="card"/> in a window of size <paramref name="room"/>, keeping <paramref name="margin"/> from
    /// its edges, beside <paramref name="part"/> (the outline drawn round the control the step describes), or with no part, in the middle
    /// at the bottom.
    /// </summary>
    public static DemoCardPlace Place(Size room, Rect? part, Size card, double margin)
    {
        // Only the part of the outline inside the window counts: a control still scrolled out of view is placed against as none.
        var shown = part?.Intersect(new Rect(room));
        if (shown is not { Width: > 0, Height: > 0 } p)
        {
            return new(new Point(Clamp((room.Width - card.Width) / 2, margin, room.Width - margin - card.Width), Bottom(room, card, margin)), DemoCardSide.Bottom, null);
        }

        // How much room each side leaves once the card and the gap are in it; a side the card does not fit has none.
        var sides = new (DemoCardSide Side, double Spare)[]
        {
            (DemoCardSide.Right, room.Width - margin - (p.Right + Gap) - card.Width),
            (DemoCardSide.Left, p.X - Gap - margin - card.Width),
            (DemoCardSide.Below, room.Height - margin - (p.Bottom + Gap) - card.Height),
            (DemoCardSide.Above, p.Y - Gap - margin - card.Height),
        };
        bool acrossFits = card.Height <= room.Height - 2 * margin, downFits = card.Width <= room.Width - 2 * margin;
        var fits = sides.Where(s => s.Spare >= 0 && (s.Side is DemoCardSide.Right or DemoCardSide.Left ? acrossFits : downFits)).ToList();
        if (fits.Count > 0)
        {
            bool Along(DemoCardSide side) => (side is DemoCardSide.Below or DemoCardSide.Above) == (p.Height < card.Height);
            var side = fits.OrderByDescending(s => Along(s.Side)).ThenByDescending(s => s.Spare).First().Side;
            double x = side switch
            {
                DemoCardSide.Right => p.Right + Gap,
                DemoCardSide.Left => p.X - Gap - card.Width,
                _ => Clamp(p.Center.X - card.Width / 2, margin, room.Width - margin - card.Width),
            };
            double y = side switch
            {
                DemoCardSide.Below => p.Bottom + Gap,
                DemoCardSide.Above => p.Y - Gap - card.Height,
                _ => Clamp(p.Center.Y - card.Height / 2, margin, room.Height - margin - card.Height),
            };
            var at = new Point(x, y);
            return new(at, side, PointerFor(new Rect(at, card), p, side));
        }

        // No side has room: the bottom of the window, wherever along it covers least of the part.
        double top = Bottom(room, card, margin);
        var along = new[] { margin, (room.Width - card.Width) / 2, room.Width - margin - card.Width }
            .Select(x => Clamp(x, margin, room.Width - margin - card.Width))
            .OrderBy(x => Covered(new Rect(x, top, card.Width, card.Height), p))
            .ThenBy(x => Math.Abs(x + card.Width / 2 - room.Width / 2))
            .First();
        var placed = new Rect(along, top, card.Width, card.Height);
        return new(placed.Position, DemoCardSide.Bottom, PointerFor(placed, p, DemoCardSide.Bottom));
    }

    /// <summary>
    /// The centre of the pointer on a card already placed: on the edge facing the part, as near the part's middle as the card's rounded
    /// corners allow; none where the part does not lie beyond that edge. The phone, whose card is always above or below, uses this alone.
    /// </summary>
    public static Point? PointerFor(Rect card, Rect part, DemoCardSide side)
    {
        double inset = PointerReach + 8;
        double AlongX() => Clamp(part.Center.X, card.X + inset, card.Right - inset);
        double AlongY() => Clamp(part.Center.Y, card.Y + inset, card.Bottom - inset);
        return side switch
        {
            DemoCardSide.Right when part.Right <= card.X => new Point(card.X, AlongY()),
            DemoCardSide.Left when part.X >= card.Right => new Point(card.Right, AlongY()),
            DemoCardSide.Below when part.Bottom <= card.Y => new Point(AlongX(), card.Y),
            DemoCardSide.Above when part.Y >= card.Bottom => new Point(AlongX(), card.Bottom),
            // At the bottom the card points up at a part whose middle is above it, and at none whose middle it would hide.
            DemoCardSide.Bottom when part.Center.Y < card.Y => new Point(AlongX(), card.Y),
            _ => null,
        };
    }

    private static double Bottom(Size room, Size card, double margin) => Math.Max(margin, room.Height - margin - card.Height);

    private static double Covered(Rect card, Rect part) => card.Intersect(part) is { Width: > 0, Height: > 0 } both ? both.Width * both.Height : 0;

    private static double Clamp(double value, double low, double high) => high < low ? low : Math.Clamp(value, low, high);
}
