using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>A tag's box on screen: its top left corner and its size, in the screen's own units.</summary>
public readonly record struct TagBox(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;

    public double Bottom => Y + Height;

    public PointD Centre => new(X + (Width / 2), Y + (Height / 2));

    /// <summary>Whether two boxes share any area, each grown by <paramref name="pad"/>.</summary>
    public bool Overlaps(TagBox other, double pad = 0) =>
        X - pad < other.Right && other.X - pad < Right && Y - pad < other.Bottom && other.Y - pad < Bottom;

    /// <summary>The point of the box nearest <paramref name="p"/>.</summary>
    public PointD Nearest(PointD p) => new(Math.Clamp(p.X, X, Right), Math.Clamp(p.Y, Y, Bottom));

    /// <summary>Whether the box reaches inside a circle.</summary>
    public bool Overlaps(PointD centre, double radius)
    {
        var n = Nearest(centre);
        return Math.Pow(n.X - centre.X, 2) + Math.Pow(n.Y - centre.Y, 2) < radius * radius;
    }
}

/// <summary>
/// A bull as the tags see it, on screen: its centre, the radius of its outer printed ring (0 where it is not known), and the size of its
/// number's text.
/// </summary>
public sealed record TagBull(int Index, PointD At, double Ring, double Width, double Height);

/// <summary>
/// A hole as the tags see it, on screen: its centre, the radius a tag keeps clear of (the hole's circle, or the selection's ring and ticks
/// round it), the bull it is on, and the size of its tag, with no tag where <paramref name="Width"/> is 0. A hole with no tag is still kept clear of.
/// </summary>
public sealed record TagHole(int ShotId, PointD At, double Radius, int? Bull, double Width, double Height);

/// <summary>Where a bull's number goes.</summary>
public sealed record PlacedBullTag(int Index, TagBox Box);

/// <summary>Where a hole's tag goes, and the short leader line from the hole's edge to the tag where the tag stands away from its hole.</summary>
public sealed record PlacedHoleTag(int ShotId, TagBox Box, PointD? LeaderFrom, PointD? LeaderTo);

/// <summary>Every bull number and hole tag on the picture.</summary>
public sealed record TagLayout(IReadOnlyList<PlacedBullTag> Bulls, IReadOnlyList<PlacedHoleTag> Holes);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 section 1A, Alan's pick from the drawings for entry 462 section 3: on the marking picture each bull's
/// number sits outside its outer ring, top left, and each hole's tag sits just outside the ring, placed so no tag lies on another tag, a
/// bull's number or a hole. Where a bull holds more than one hole, as five shots into one bull of a target GroupLab did not print, the tags
/// stand in a column beside the ring (1a, 1b, 1c, top to bottom as they are lettered) with a short leader line from each hole, never
/// stacked on each other. Before this a hole's number was drawn up and right of the hole and a bull's number down and right of its
/// centre, so around bulls 14 and 17 of a full sheet the two sat on each other, and on a hand-marked target 1a, 1b and 1c did.
/// <para>
/// Everything here is in screen units, so the computer and the phone place the tags the same way at any zoom; each measures its own text
/// and draws the tags in its own ink. When the ring is large on screen, zoomed in, the tags come in to just outside the holes instead,
/// so they stay near what they name.
/// </para>
/// </summary>
public static class MarkTags
{
    /// <summary>The space between a ring, or a hole, and a tag beside it.</summary>
    public const double Gap = 3;

    /// <summary>The space kept between two tags.</summary>
    public const double Spacing = 2;

    /// <summary>
    /// How far outside the holes, at most, the tags go round a bull: a ring larger than this beyond its holes, zoomed in, has its tags
    /// brought in to this distance from the holes.
    /// </summary>
    public const double MostRoom = 40;

    /// <summary>The ring assumed round a bull whose printed rings are not known and that holds no hole, as a bull placed by hand.</summary>
    public const double LeastRing = 16;

    /// <summary>
    /// The radius of each bull's outer ring in inches, by the bull's index in the definition, for the tags to sit outside. Empty without a
    /// definition, as on a target GroupLab did not print.
    /// </summary>
    public static IReadOnlyDictionary<int, double> OuterRingInches(TargetDefinition? definition)
    {
        var rings = new Dictionary<int, double>();
        if (definition is null)
        {
            return rings;
        }

        var outer = definition.RingSets.ToDictionary(r => r.Key, r => r.Discs.Count > 0 ? r.Discs.Max(d => d.Diameter) / 2.0 / 254.0 : 0);
        for (int i = 0; i < definition.Bulls.Count; i++)
        {
            if (outer.TryGetValue(definition.Bulls[i].RingSet, out double r) && r > 0)
            {
                rings[i] = r;
            }
        }

        return rings;
    }

    /// <summary>Places every bull's number and every hole's tag.</summary>
    public static TagLayout Place(IReadOnlyList<TagBull> bulls, IReadOnlyList<TagHole> holes)
    {
        ArgumentNullException.ThrowIfNull(bulls);
        ArgumentNullException.ThrowIfNull(holes);
        var byBull = bulls.ToDictionary(b => b.Index);
        var placed = new List<TagBox>();
        int Cost(TagBox box) => placed.Count(p => p.Overlaps(box, Spacing)) + holes.Count(h => box.Overlaps(h.At, h.Radius + 1));

        // The ring the tags go round: the printed outer ring, never inside the bull's own holes, and brought in towards the holes when it is
        // far outside them on screen.
        var orbit = new Dictionary<int, double>();
        foreach (var bull in bulls)
        {
            double edge = holes.Where(h => h.Bull == bull.Index).Select(h => Distance(h.At, bull.At) + h.Radius).DefaultIfEmpty(0).Max();
            double ring = bull.Ring > 0 ? Math.Min(bull.Ring, edge + MostRoom) : LeastRing;
            orbit[bull.Index] = Math.Max(ring, edge) + Gap;
        }

        var bullTags = new List<PlacedBullTag>();
        foreach (var bull in bulls.Where(b => b.Width > 0))
        {
            // Top left first, as Alan chose; then the other corners, then further out, where a hole lies outside the ring there.
            var at = Best(Enumerable.Range(0, Steps).SelectMany(step => Corners.Select(way => Outside(bull.At, orbit[bull.Index] + (step * bull.Height), way, bull.Width, bull.Height))), Cost);
            placed.Add(at);
            bullTags.Add(new PlacedBullTag(bull.Index, at));
        }

        var holeTags = new List<PlacedHoleTag>();
        var tagged = holes.Where(h => h.Width > 0).ToList();
        var groups = tagged.Where(h => h.Bull is { } b && byBull.ContainsKey(b)).GroupBy(h => h.Bull!.Value).OrderBy(g => g.Key)
            .Select(g => (Centre: byBull[g.Key].At, Orbit: orbit[g.Key], Holes: g.ToList()))
            .Concat(tagged.Where(h => h.Bull is not { } b || !byBull.ContainsKey(b)).Select(h => (Centre: h.At, Orbit: h.Radius + Gap, Holes: new List<TagHole> { h })))
            .ToList();
        foreach (var (centre, around, members) in groups)
        {
            var result = members.Count == 1 ? Single(members[0], centre, around, Cost) : Fan(members, centre, around, Cost);
            foreach (var tag in result)
            {
                placed.Add(tag.Box);
                holeTags.Add(tag);
            }
        }

        return new TagLayout(bullTags, holeTags);
    }

    /// <summary>
    /// Every overlap in a layout, in words, for the tests: a tag on another tag, on a bull's number or on a hole. Empty when there is none.
    /// </summary>
    public static IReadOnlyList<string> Overlaps(TagLayout layout, IReadOnlyList<TagHole> holes)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(holes);
        var boxes = layout.Bulls.Select(b => ($"bull {b.Index}'s number", b.Box)).Concat(layout.Holes.Select(h => ($"hole {h.ShotId}'s tag", h.Box))).ToList();
        var found = new List<string>();
        for (int i = 0; i < boxes.Count; i++)
        {
            for (int j = i + 1; j < boxes.Count; j++)
            {
                if (boxes[i].Box.Overlaps(boxes[j].Box))
                {
                    found.Add($"{boxes[i].Item1} on {boxes[j].Item1}");
                }
            }

            found.AddRange(holes.Where(h => boxes[i].Box.Overlaps(h.At, h.Radius)).Select(h => $"{boxes[i].Item1} on hole {h.ShotId}"));
        }

        return found;
    }

    /// <summary>Up and left first, then down and left, up and right, down and right, each a unit vector on screen, y down.</summary>
    private static readonly PointD[] Corners = [new(-Math.Sqrt(0.5), -Math.Sqrt(0.5)), new(-Math.Sqrt(0.5), Math.Sqrt(0.5)), new(Math.Sqrt(0.5), -Math.Sqrt(0.5)), new(Math.Sqrt(0.5), Math.Sqrt(0.5))];

    /// <summary>A box just outside a circle of radius <paramref name="r"/> round <paramref name="centre"/>, in the direction <paramref name="way"/>.</summary>
    private static TagBox Outside(PointD centre, double r, PointD way, double width, double height)
    {
        // The box's centre stands out by the circle's radius and by as much of the box as lies along the direction, so its nearest corner
        // or edge just meets the circle.
        double reach = r + (Math.Abs(way.X) * width / 2) + (Math.Abs(way.Y) * height / 2);
        return new TagBox(centre.X + (way.X * reach) - (width / 2), centre.Y + (way.Y * reach) - (height / 2), width, height);
    }

    /// <summary>One hole's tag, on the ring the way the hole lies from its bull, turned a little each way until it is clear.</summary>
    private static IEnumerable<PlacedHoleTag> Single(TagHole hole, PointD centre, double around, Func<TagBox, int> cost)
    {
        // A hole near the middle has no way of its own, so its tag goes up and right, where the hole's number always stood.
        double off = Distance(hole.At, centre);
        double angle = off > 0.25 * around ? Math.Atan2(hole.At.Y - centre.Y, hole.At.X - centre.X) : -Math.PI / 4;
        var candidates = Enumerable.Range(0, Steps).SelectMany(step => Enumerable.Range(0, 36).Select(k =>
        {
            double turn = (k % 2 == 1 ? 1 : -1) * ((k + 1) / 2) * Math.PI / 18;
            return Outside(centre, around + (step * hole.Height), new PointD(Math.Cos(angle + turn), Math.Sin(angle + turn)), hole.Width, hole.Height);
        }));
        yield return WithLeader(hole, Best(candidates, cost), far: 3 * hole.Height);
    }

    /// <summary>
    /// The tags of a bull holding more than one hole, in a column beside its ring, lettered top to bottom, with a leader line from each hole:
    /// to the right where it is clear, else to the left, else moved up or down a tag at a time, else further out.
    /// </summary>
    private static IEnumerable<PlacedHoleTag> Fan(List<TagHole> members, PointD centre, double around, Func<TagBox, int> cost)
    {
        var order = members.OrderBy(h => h.At.Y).ThenBy(h => h.At.X).ThenBy(h => h.ShotId).ToList();
        double width = order.Max(h => h.Width), height = order.Sum(h => h.Height) + (Spacing * 2 * (order.Count - 1));
        double row = order.Max(h => h.Height);
        List<TagBox> Column(double x, double top, bool right)
        {
            var boxes = new List<TagBox>();
            double y = top;
            foreach (var hole in order)
            {
                boxes.Add(new TagBox(right ? x : x + width - hole.Width, y, hole.Width, hole.Height));
                y += hole.Height + (Spacing * 2);
            }

            return boxes;
        }

        var columns = Enumerable.Range(0, Steps).SelectMany(step => Enumerable.Range(0, 9).SelectMany(shift =>
        {
            double r = around + (step * row);
            double top = centre.Y - (height / 2) + ((shift % 2 == 1 ? 1 : -1) * ((shift + 1) / 2) * row);
            return new[] { Column(centre.X + r, top, right: true), Column(centre.X - r - width, top, right: false) };
        }));
        var chosen = Best(columns, c => c.Sum(cost));
        for (int i = 0; i < order.Count; i++)
        {
            yield return WithLeader(order[i], chosen[i], far: 0);
        }
    }

    /// <summary>How many rings further out, a tag's height at a time, a tag is tried at before the least crowded place is taken.</summary>
    private const int Steps = 5;

    /// <summary>The first candidate that is clear, in the order of preference, or where none is, the one with fewest things under it.</summary>
    private static T Best<T>(IEnumerable<T> candidates, Func<T, int> cost)
    {
        T best = default!;
        int least = int.MaxValue;
        foreach (var candidate in candidates)
        {
            int c = cost(candidate);
            if (c == 0)
            {
                return candidate;
            }

            if (c < least)
            {
                (best, least) = (candidate, c);
            }
        }

        return best;
    }

    /// <summary>The tag with a leader from the hole's edge to the tag's nearest point, where the tag stands further than <paramref name="far"/> from the hole.</summary>
    private static PlacedHoleTag WithLeader(TagHole hole, TagBox box, double far)
    {
        var to = box.Nearest(hole.At);
        double d = Distance(to, hole.At);
        if (d - hole.Radius <= far || d <= 1e-9)
        {
            return new PlacedHoleTag(hole.ShotId, box, null, null);
        }

        var from = new PointD(hole.At.X + ((to.X - hole.At.X) / d * hole.Radius), hole.At.Y + ((to.Y - hole.At.Y) / d * hole.Radius));
        return new PlacedHoleTag(hole.ShotId, box, from, to);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
