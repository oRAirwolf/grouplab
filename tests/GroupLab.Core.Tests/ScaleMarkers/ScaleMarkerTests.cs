using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.ScaleMarkers;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.ScaleMarkers;

/// <summary>NOTES-FROM-PLANNING.md entry 365: the scale markers' identifiers, pages and fit, on exact points where a photo is not needed.</summary>
public class ScaleMarkerTests
{
    /// <summary>A camera looking at the surface from about 600 mm, tilted 20 degrees: surface millimetres to pixels.</summary>
    private static readonly Homography Camera = Tilted(20, 600);

    private static Homography Tilted(double degrees, double distance)
    {
        double t = degrees * Math.PI / 180, f = 2900;
        // Rotation about x, then the pinhole: x' = f X / Z, with the surface's origin straight ahead.
        double[] r = [1, 0, 0, 0, Math.Cos(t), -Math.Sin(t), 0, Math.Sin(t), Math.Cos(t)];
        return new Homography([f * r[0], f * r[1], 2000 * distance, f * r[3], f * r[4], 1500 * distance, r[6], r[7], distance]);
    }

    private static DetectedMarker Seen(MarkerTag tag, double angle, PointD at) =>
        new(tag.Id, [.. tag.Corners.Select(c => Camera.Apply(new PointD(at.X + (Math.Cos(angle) * c.X) - (Math.Sin(angle) * c.Y), at.Y + (Math.Sin(angle) * c.X) + (Math.Cos(angle) * c.Y))))]);

    [Fact]
    public void TheMarkersKeepTheTopOfTheFamilyAndNoSheetReachesIt()
    {
        Assert.Equal(586, ScaleMarkerLayout.Last);
        Assert.Equal(470, ScaleMarkerLayout.First);
        foreach (int id in Enumerable.Range(ScaleLabels.First, ScaleLabels.Last - ScaleLabels.First + 1).Concat(Enumerable.Range(ScaleMarkerLayout.BracketFirst, 32)))
        {
            Assert.Equal(id, ScaleMarkerLayout.Tag(id)!.Id);
        }

        Assert.Null(ScaleMarkerLayout.Tag(ScaleMarkerLayout.First - 1));
        foreach (var file in Directory.GetFiles(Repo.PathTo("targets"), "*.gltd.json"))
        {
            var ids = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!["fiducials"]?["markers"]?.AsArray().Select(m => (int)m!["id"]!) ?? [];
            Assert.All(ids, id => Assert.True(id < ScaleMarkerLayout.First, $"{Path.GetFileName(file)} uses {id}"));
        }
    }

    [Fact]
    public void FourBracketsOnARectangleGiveTheTargetsSizeAndCorners()
    {
        const double w = 300, h = 450;
        PointD[] corners = [new(0, 0), new(w, 0), new(w, h), new(0, h)];
        var seen = Enumerable.Range(1, 4).SelectMany(n => ScaleMarkerLayout.Bracket(n).Select(t => Seen(t, 0, corners[n - 1]))).ToList();
        var (finding, _) = ScaleMarkerReading.Read(seen, null, null, []);
        Assert.NotNull(finding);
        Assert.NotNull(finding.TargetCorners);
        var plane = finding.Plane;
        var c = finding.TargetCorners!.Select(plane.ToInches).ToArray();
        Assert.Equal(w / 25.4, Distance(c[0], c[1]), 4);
        Assert.Equal(h / 25.4, Distance(c[0], c[3]), 4);
        Assert.False(finding.ScaleOnly);
        Assert.Contains("4 corner brackets", finding.Says(0.002), StringComparison.Ordinal);
        Assert.Contains(ScaleMarkerWords.NoPrinterCheck, finding.Says(0.002), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BracketsLaidALittleAstrayStillGiveTheScale(int seed)
    {
        // Each piece a third of a millimetre and half a degree from the corner, as a hand lays it.
        var rng = new Random(seed);
        double Gauss(double sigma) => sigma * Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
        const double w = 300, h = 300;
        PointD[] corners = [new(0, 0), new(w, 0), new(w, h), new(0, h)];
        var seen = new List<DetectedMarker>();
        for (int n = 1; n <= 4; n++)
        {
            var at = new PointD(corners[n - 1].X + Gauss(0.3), corners[n - 1].Y + Gauss(0.3));
            double turn = Gauss(0.5 * Math.PI / 180);
            seen.AddRange(ScaleMarkerLayout.Bracket(n).Select(t => Seen(t, turn, at)));
        }

        var (finding, _) = ScaleMarkerReading.Read(seen, null, null, []);
        var plane = finding!.Plane;
        double across = Distance(plane.ToInches(Camera.Apply(corners[0])), plane.ToInches(Camera.Apply(corners[1]))) * 25.4;
        double down = Distance(plane.ToInches(Camera.Apply(corners[0])), plane.ToInches(Camera.Apply(corners[3]))) * 25.4;
        Assert.True(Math.Abs((across / w) - 1) < 0.001 && Math.Abs((down / h) - 1) < 0.001, $"{across:0.000} by {down:0.000} mm");
    }

    /// <summary>Entry 372: a 70 by 80 mm label's two rows each give the scale across the label; its width is in its identifiers.</summary>
    [Fact]
    public void AScaleLabelsRowsGiveTheScaleAcrossTheLabel()
    {
        var pairs = ScaleLabels.Pairs(70, 80, 3);
        Assert.Equal(2, pairs.Count);
        Assert.NotEqual(pairs[0].Left, pairs[1].Left);
        var seen = pairs.SelectMany((p, r) => new[] { p.Left, p.Right }.Select(id => Seen(ScaleMarkerLayout.Tag(id)!, 0.1, new PointD(100, 100 + (10.5 * r))))).ToList();
        var (finding, _) = ScaleMarkerReading.Read(seen, null, null, []);
        Assert.NotNull(finding);
        Assert.True(finding.ScaleOnly);
        Assert.All(finding.Lengths, l => Assert.Equal(60 / 25.4, l.Inches, 6));
        Assert.Contains("rows of scale labels", finding.Says(0.01), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => ScaleLabels.Pairs(33, 30, 1));
    }

    [Fact]
    public void ABarGivesTheScaleAlongItAndSaysItCannotGiveTheAngle()
    {
        var bar = ScaleMarkerLayout.Bar(false, 1);
        var (finding, _) = ScaleMarkerReading.Read([.. bar.Select(t => Seen(t, 0.3, new PointD(20, 300)))], null, null, []);
        Assert.NotNull(finding);
        Assert.True(finding.ScaleOnly);
        var (a, b, inches) = Assert.Single(finding.Lengths);
        Assert.Equal(10.0, inches, 9);
        Assert.Equal(10.0, Distance(finding.Plane.ToInches(a), finding.Plane.ToInches(b)), 4);
    }

    [Fact]
    public void AMeasuredBoardIsFoundAndOneThatHasMovedIsRefused()
    {
        var stickers = Enumerable.Range(1, 4).Select(i => ScaleMarkerLayout.Sticker('B', i)).ToArray();
        PointD[] at = [new(-60, -60), new(360, -60), new(360, 500), new(-60, 500)];
        var board = new ScaleBoard("Board 1", 'B', new DateOnly(2026, 10, 4), 0.001,
            stickers.Select((t, i) => (t.Id, Corners: t.Corners.Select(c => new PointD(c.X + at[i].X, c.Y + at[i].Y)).ToArray())).ToDictionary(x => x.Id, x => x.Corners));
        Assert.Equal(board.Stickers.Keys, ScaleBoard.FromJson(board.ToJson())!.Stickers.Keys);
        var seen = stickers.Select((t, i) => Seen(t, 0, at[i])).ToList();
        var (finding, said) = ScaleMarkerReading.Read(seen, null, null, [board]);
        Assert.NotNull(finding);
        Assert.Null(said);
        Assert.Equal("Board 1", finding.Board);
        Assert.Equal(420 / 25.4, Distance(finding.Plane.ToInches(seen[0].Corners[0]), finding.Plane.ToInches(seen[1].Corners[0])), 4);

        // One sticker 8 mm from where it was measured, as one peeled and stuck back: it is said, not used. Four stickers fix a homography
        // all but exactly, so a move shows only as the misfit of that sticker's own square, and an even swelling of the board not at all.
        var moved = stickers.Select((t, i) => Seen(t, 0, i == 2 ? new PointD(at[i].X + 8, at[i].Y) : at[i])).ToList();
        var (none, why) = ScaleMarkerReading.Read(moved, null, null, [board]);
        Assert.Null(none);
        Assert.Contains("no longer agree", why, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryPageDrawsItsOwnCodesAtTheirSizeInsideThePrintersMargins()
    {
        foreach (var (kind, paper, ids) in new[]
        {
            (MarkerKind.Bracket, MarkerPaper.Letter, Enumerable.Range(555, 8)), (MarkerKind.Bracket, MarkerPaper.A4, Enumerable.Range(555, 8)),
            (MarkerKind.InchBar, MarkerPaper.Letter, Enumerable.Range(563, 4)), (MarkerKind.MetricBar, MarkerPaper.A4, Enumerable.Range(567, 4)),
            (MarkerKind.Sticker, MarkerPaper.Letter, Enumerable.Range(571, 16)), (MarkerKind.Sticker, MarkerPaper.Label4x6, Enumerable.Range(571, 4)),
        })
        {
            var page = Assert.Single(ScaleMarkerPages.Pages(kind, paper));
            var modules = page.Items.OfType<RectFill>().Where(r => r.Module is not null).ToList();
            Assert.Equal(ids.Sum(id => ScaleMarkerLayout.Inked(id).Cast<bool>().Count(b => b)), modules.Sum(m => m.Width / m.Module!.Size));

            // A quarter inch from every edge, where any printer reaches; the cut lines along a bar may run off the page.
            long margin = (long)(6.35 * 20);
            Assert.All(page.Items.Where(i => i.Layer != SceneLayer.CutLines).OfType<RectFill>(), r =>
                Assert.True(r.X >= margin && r.Y >= margin && r.X + r.Width <= page.Width - margin && r.Y + r.Height <= page.Height - margin, $"{kind} on {paper}: {r}"));
        }

        // The inch bar's two codes: 10.000 in between centres, exactly.
        var bar = ScaleMarkerPages.Bars(MarkerPaper.Letter).Items.OfType<RectFill>().Where(r => r.Module is not null).ToList();
        double Left(int fromX, int toX) => bar.Where(r => r.X >= fromX && r.X < toX).Min(r => r.X - (r.Module!.Column * r.Module.Size));
        Assert.Equal(254 * 20, Left(4000, 6000) - Left(0, 1000), 0);
    }

    [Fact]
    public void AMarkerScaleIsSavedAndReadBackWithTheMarking()
    {
        var reference = MarkerReference.Upright(Camera.Inverse(), new PointD(2000, 1500), "Scale from 4 corner brackets in the photo: good to about 0.1 percent.");
        var back = Assert.IsType<MarkerReference>(MarkingFile.ReadScaleForTest(MarkingFile.WriteScaleForTest(reference)));
        Assert.Equal(reference.ToTarget(new PointD(100, 200)).X, back.ToTarget(new PointD(100, 200)).X, 9);
        Assert.Equal(reference.Summary, back.Summary);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
