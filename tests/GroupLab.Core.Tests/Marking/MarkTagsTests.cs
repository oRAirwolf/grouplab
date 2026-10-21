using System.Globalization;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 465 section 1A: on the marking picture no tag lies on another tag, a bull's number or a hole, each bull's
/// number sits outside its ring at the top left, and the tags of several holes on one bull stand in a column with leader lines. The
/// crowded sheet is the one the marking screen's picture is taken from (a hole on every scoring bull of GL-CF25-LTR, placed as that
/// picture's own test places them), where bulls 14 and 17 had their numbers stacked; the hand-marked target is the "Any target" picture's,
/// five holes close together on each of four bulls, where 1a, 1b and 1c sat on each other.
/// </summary>
public class MarkTagsTests
{
    /// <summary>A mono tag's size on screen at this many characters, about what both screens draw.</summary>
    private static (double Width, double Height) Tag(string text) => ((7.2 * text.Length) + 7, 17);

    /// <summary>A bull number's size, plain text.</summary>
    private static (double Width, double Height) Number(string text) => ((7 * text.Length) + 2, 15);

    /// <summary>The marking picture's sheet at <paramref name="ringOnScreen"/> pixels of outer ring radius, and its holes, bull 14 holding two.</summary>
    private static (List<TagBull> Bulls, List<TagHole> Holes, double Ring) Crowded(double ringOnScreen)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var rings = MarkTags.OuterRingInches(definition);
        double ppi = ringOnScreen / rings[0];
        PointD Screen(double xDmm, double yDmm) => new(xDmm / 254 * ppi, yDmm / 254 * ppi);

        // As Entry109Tests.Sheet places them: a hole on every scoring bull, up to 45 dmm off its centre.
        var random = new Random(109);
        var at = new List<(int Bull, PointD At)>();
        for (int i = 0; i < definition.Bulls.Count; i++)
        {
            var b = definition.Bulls[i];
            if (b.Scoring)
            {
                var hole = SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871);
                at.Add((i, Screen(hole.X, hole.Y)));
            }
        }

        var bulls = definition.Bulls.Select((b, i) =>
        {
            string label = b.Label ?? (i + 1).ToString(CultureInfo.InvariantCulture);
            var (w, h) = Number(label);
            return new TagBull(i, Screen(b.X, b.Y), rings[i] * ppi, w, h);
        }).ToList();

        // Bull 14 took two shots, close together, as the review in that picture asks about.
        int fourteen = bulls.FindIndex(b => definition.Bulls[b.Index].Label == "14");
        var extra = at.First(a => a.Bull == fourteen).At;
        at.Add((fourteen, new PointD(extra.X + (0.2 * ppi), extra.Y + (0.1 * ppi))));
        double radius = Math.Max(3, 0.154 * ppi);
        var labels = new Dictionary<int, int>();
        var holes = at.Select((a, id) =>
        {
            string text = definition.Bulls[a.Bull].Label ?? "?";
            if (a.Bull == fourteen)
            {
                text += labels.TryGetValue(a.Bull, out int k) ? "b" : "a";
                labels[a.Bull] = 1;
            }

            var (w, h) = Tag(text);
            return new TagHole(id, a.At, radius, a.Bull, w, h);
        }).ToList();
        return (bulls, holes, ringOnScreen);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(26)]
    [InlineData(40)]
    [InlineData(90)]
    public void OnTheCrowdedSheetNoTagLiesOnAnotherTagABullNumberOrAHole(double ring)
    {
        var (bulls, holes, _) = Crowded(ring);
        var layout = MarkTags.Place(bulls, holes);
        Assert.Equal(bulls.Count, layout.Bulls.Count);
        Assert.Equal(holes.Count, layout.Holes.Count);
        var overlaps = MarkTags.Overlaps(layout, holes);
        Assert.True(overlaps.Count == 0, string.Join("; ", overlaps));
    }

    [Fact]
    public void AtTheScreenshotsZoomEachTagIsOutsideItsBullsRingAndEachNumberTopLeft()
    {
        var (bulls, holes, ring) = Crowded(26);
        var layout = MarkTags.Place(bulls, holes);
        foreach (var tag in layout.Holes)
        {
            var hole = holes.Single(h => h.ShotId == tag.ShotId);
            var bull = bulls.Single(b => b.Index == hole.Bull);
            var near = tag.Box.Nearest(bull.At);
            Assert.True(Math.Sqrt(Math.Pow(near.X - bull.At.X, 2) + Math.Pow(near.Y - bull.At.Y, 2)) >= ring, $"hole {tag.ShotId}'s tag is inside bull {bull.Index}'s ring");
        }

        // Every bull's number at the top left, where nothing on that sheet is in the way.
        foreach (var number in layout.Bulls)
        {
            var bull = bulls.Single(b => b.Index == number.Index);
            Assert.True(number.Box.Right < bull.At.X && number.Box.Bottom < bull.At.Y, $"bull {bull.Index}'s number is not at its top left");
        }
    }

    [Fact]
    public void TwoHolesOnOneBullHaveTheirTagsInAColumnWithLeaders()
    {
        var (bulls, holes, _) = Crowded(26);
        var layout = MarkTags.Place(bulls, holes);
        var fourteen = holes.Where(h => h.Width == Tag("14a").Width).Select(h => layout.Holes.Single(t => t.ShotId == h.ShotId)).ToList();
        Assert.Equal(2, fourteen.Count);
        Assert.All(fourteen, t => Assert.NotNull(t.LeaderFrom));
        Assert.True(Math.Abs(fourteen[0].Box.X - fourteen[1].Box.X) < 1e-9 || Math.Abs(fourteen[0].Box.Right - fourteen[1].Box.Right) < 1e-9, "the two tags are not in one column");
    }

    /// <summary>The "Any target" picture's plain sample: four bulls at 150 dpi, five holes close together on each, placed by hand.</summary>
    private static (List<TagBull> Bulls, List<TagHole> Holes) AnyTarget(double zoom)
    {
        PointD[] centres = [new(425, 560), new(850, 560), new(425, 1110), new(850, 1110)];
        (int X, int Y)[] at =
        [
            (440, 548), (409, 575), (431, 590), (452, 566), (417, 541),
            (866, 552), (839, 571), (858, 584), (871, 565), (844, 543),
            (433, 1098), (409, 1121), (447, 1127), (425, 1093), (452, 1112),
            (861, 1101), (835, 1119), (872, 1124), (848, 1094), (866, 1115),
        ];
        var bulls = centres.Select((c, i) =>
        {
            var (w, h) = Number((i + 1).ToString(CultureInfo.InvariantCulture));
            return new TagBull(i, new PointD(c.X * zoom, c.Y * zoom), 0, w, h);
        }).ToList();
        double radius = Math.Max(3, 0.154 * 150 * zoom);
        var holes = at.Select((p, id) =>
        {
            var (w, h) = Tag((id / 5) + 1 + "a");
            return new TagHole(id, new PointD(p.X * zoom, p.Y * zoom), radius, id / 5, w, h);
        }).ToList();
        return (bulls, holes);
    }

    [Theory]
    [InlineData(0.35)]
    [InlineData(0.6)]
    [InlineData(1.5)]
    public void OnAnyTargetCloseHolesFanOutInAColumnOnLeadersAndNeverStack(double zoom)
    {
        var (bulls, holes) = AnyTarget(zoom);
        var layout = MarkTags.Place(bulls, holes);
        var overlaps = MarkTags.Overlaps(layout, holes);
        Assert.True(overlaps.Count == 0, string.Join("; ", overlaps));
        foreach (var bull in bulls)
        {
            var mine = holes.Where(h => h.Bull == bull.Index).OrderBy(h => h.At.Y).ThenBy(h => h.At.X).Select(h => layout.Holes.Single(t => t.ShotId == h.ShotId)).ToList();
            Assert.All(mine, t => Assert.NotNull(t.LeaderFrom));

            // In a column, a, b, c down the page in the order they are lettered.
            for (int i = 1; i < mine.Count; i++)
            {
                Assert.True(mine[i].Box.Y >= mine[i - 1].Box.Bottom, $"bull {bull.Index}'s tags are not one under another");
            }
        }
    }

    [Fact]
    public void TheSelectedHolesWiderRingIsKeptClearToo()
    {
        var (bulls, holes) = AnyTarget(0.35);
        holes[0] = holes[0] with { Radius = holes[0].Radius + 6 + 8 };
        var overlaps = MarkTags.Overlaps(MarkTags.Place(bulls, holes), holes);
        Assert.True(overlaps.Count == 0, string.Join("; ", overlaps));
    }

    [Fact]
    public void AnUnassignedHoleAndABullWithNoHolesStillGetTags()
    {
        var bulls = new List<TagBull> { new(0, new PointD(100, 100), 0, 9, 15) };
        var holes = new List<TagHole> { new(1, new PointD(300, 300), 4, null, 80, 17) };
        var layout = MarkTags.Place(bulls, holes);
        Assert.Single(layout.Bulls);
        Assert.Single(layout.Holes);
        Assert.Empty(MarkTags.Overlaps(layout, holes));
        Assert.True(layout.Bulls[0].Box.Right < 100 && layout.Bulls[0].Box.Bottom < 100);
    }

    [Fact]
    public void WithoutADefinitionThereAreNoRings()
    {
        Assert.Empty(MarkTags.OuterRingInches(null));
        var rings = MarkTags.OuterRingInches(BuiltIns.Load("GL-CF25-LTR.gltd.json"));
        Assert.InRange(rings[0], 0.2, 1.5);
    }
}
