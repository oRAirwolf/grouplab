using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 78 section 4 and 79 section 1: the size a hole of the named calibre is detected at, fed into segmentation.
/// On a synthetic GL-CF25-LTR at 150 DPI with a hole of each of four kinds by bull:
/// <list type="bullet">
/// <item><b>A single hole with a stroke of hand ink joined to it:</b> elongated, but its area holds fewer than two holes of the calibre, so
/// with the calibre it is one hole. Without one, this sheet's round marks fall into two sizes, plain and large, so no size fits it
/// (entry 82 section 3) and shape alone cuts it in two.</item>
/// <item><b>Two overlapping holes:</b> split with the size named as without it. The size only stops a split, it never prevents a real one.</item>
/// <item><b>One large round hole:</b> about three holes' area and nothing to split along. It stays one mark, flagged oversized, rather than
/// being cut to agree with the size.</item>
/// <item><b>A plain single hole:</b> untouched either way.</item>
/// </list>
/// The named size is the synthesis's own single-hole diameter, 0.235 in here, as a real sheet's is the calibre times what its holes measure.
/// <para>
/// This tests the veto's mechanism, so it pins the split elongation at 1.45, where these drawn shapes split, rather than at the shipped 1.80,
/// where a stroked hole and a close pair no longer ask for a split at all (NOTES-FROM-PLANNING.md entry 81 section 1).
/// </para>
/// </summary>
public class CalibreSplitTests
{
    private const double SingleHoleInches = 0.235;

    private enum Kind
    {
        Plain,
        Stroked,
        Pair,
        Large,
    }

    [Fact]
    public void TheHoleSizeKeepsAStrokedHoleWholeAndNeitherPreventsAPairNorCutsALargeHole()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var holes = new List<SyntheticHole>();
        var strokes = new List<InkStroke>();
        var kinds = new Kind[definition.Bulls.Count];
        for (int k = 0; k < definition.Bulls.Count; k++)
        {
            var bull = definition.Bulls[k];
            double x = bull.X + 90, y = bull.Y + 90;
            kinds[k] = (Kind)(k % 4);
            holes.Add(Hole(x, y, kinds[k] == Kind.Large ? 0.13 : 0.06, k));
            if (kinds[k] == Kind.Stroked)
            {
                strokes.Add(new InkStroke(x + 20, y, x + 20 + (0.12 * 254), y, 0.05 * 254, 60));
            }
            else if (kinds[k] == Kind.Pair)
            {
                holes.Add(Hole(x + (0.15 * 254), y, 0.06, k + 100));
            }
        }

        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, strokes, new Random(78));
        var backend = new OpenCvSharpBackend();
        var mechanism = new RenderDifferenceOptions(SplitElongation: 1.45);
        var withoutResult = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, backend, mechanism, render);
        Assert.Equal(HoleSizeSource.TwoSizes, withoutResult.HoleSize!.Source);
        var without = Blobs(definition, truth, withoutResult);
        var with = Blobs(definition, truth, RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, backend, mechanism with { CalibreInches = SingleHoleInches }, render));

        for (int k = 0; k < kinds.Length; k++)
        {
            var (before, after) = (Assert.Single(without, b => b.Bull == k), Assert.Single(with, b => b.Bull == k));
            Assert.Null(before.Holes[0].CalibreHoles);
            switch (kinds[k])
            {
                case Kind.Plain:
                    Assert.Single(before.Holes);
                    Assert.Single(after.Holes);
                    Assert.False(after.Holes[0].Oversized);
                    break;
                case Kind.Stroked:
                    Assert.Equal(2, before.Holes.Count);
                    var kept = Assert.Single(after.Holes);
                    Assert.True(kept.SplitVetoed);
                    Assert.False(kept.PossibleMerge);
                    break;
                case Kind.Pair:
                    Assert.Equal(2, before.Holes.Count);
                    Assert.Equal(2, after.Holes.Count);
                    Assert.All(after.Holes, h => Assert.True(h.PossibleMerge));
                    break;
                case Kind.Large:
                    var large = Assert.Single(after.Holes);
                    Assert.Single(before.Holes);
                    Assert.True(large.Oversized, $"bull {k}: a hole of {large.CalibreHoles:0.00} single holes is not flagged");
                    Assert.False(large.SplitVetoed);

                    // NOTES-FROM-PLANNING.md entry 94 section 1: the flag counts the mark's own area, not the convex hull thrown around it,
                    // and a flagged mark carries the two centres a split would give so that taking it as two shots needs no mouse.
                    Assert.True(large.AreaInches <= large.HullAreaInches, "a mark cannot hold more ink than its own hull");
                    Assert.Equal(large.AreaInches / (Math.PI * Math.Pow(SingleHoleInches / 2, 2)), large.SizeHoles!.Value, 6);
                    Assert.NotNull(large.SplitA);
                    Assert.NotNull(large.SplitB);
                    break;
            }
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 78 section 2 and entry 81 section 3: a diagonal sliver, as a photograph's residue along a printed edge
    /// leaves, is far longer than any hole and holds less than one hole's area. Its bounding box is square, so the aspect filter passes it,
    /// and it used to become two holes. It is refused as residue, with or without a calibre, and the real holes beside it stand.
    /// </summary>
    [Fact]
    public void ASliverTooSmallForTwoHolesAndLongerThanAnyIsRefused()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var holes = definition.Bulls.Take(14).Select((b, k) => Hole(b.X + 90, b.Y + 90, 0.06, k)).ToList();
        var strokes = definition.Bulls.Skip(16).Take(4).Select(b => new InkStroke(b.X - 160, b.Y + 50, b.X - 50, b.Y + 160, 0.04 * 254, 60)).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, strokes, new Random(811));
        foreach (double? calibre in new double?[] { null, SingleHoleInches })
        {
            var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), new RenderDifferenceOptions(CalibreInches: calibre), render);
            Assert.Equal(holes.Count, result.Holes.Count);
            Assert.True(strokes.Count == result.Rejected.Count(r => r.Reason.StartsWith("residue", StringComparison.Ordinal)), string.Join(" | ", result.Rejected.Select(r => $"{truth.ToPage(new PointD(r.X, r.Y))} {r.Reason}")));
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 81 section 2: at the shipped settings a pair overlapped too far for its shape to ask for a split stays one
    /// mark, and that mark is flagged oversized, with a calibre and without one, while the single holes around it are not.
    /// </summary>
    [Fact]
    public void AMergedPairLeftWholeIsFlaggedAndTheSinglesAreNot()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var holes = new List<SyntheticHole>();
        var pairs = new List<int>();
        for (int k = 0; k < 20; k++)
        {
            var bull = definition.Bulls[k];
            holes.Add(Hole(bull.X + 90, bull.Y + 90, 0.06, k));
            if (k % 5 == 0)
            {
                holes.Add(Hole(bull.X + 90 + (0.09 * 254), bull.Y + 90, 0.06, k + 100));
                pairs.Add(k);
            }
        }

        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], new Random(812));
        foreach (double? calibre in new double?[] { null, SingleHoleInches })
        {
            var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), new RenderDifferenceOptions(CalibreInches: calibre), render);
            var blobs = Blobs(definition, truth, result);
            Assert.Equal(calibre is null ? HoleSizeSource.Sheet : HoleSizeSource.Calibre, result.HoleSize!.Source);
            for (int k = 0; k < 20; k++)
            {
                var mark = Assert.Single(Assert.Single(blobs, b => b.Bull == k).Holes);
                Assert.True(mark.Oversized == pairs.Contains(k), $"bull {k}, calibre {calibre}: {mark.DiameterInches:0.000} in, oversized {mark.Oversized}");
            }
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 82 sections 1 and 2: on a sheet with no holes at all, the case the residue is worst on, there is no size to
    /// learn, and the smallest hole any bullet makes still vetoes a sliver too small to be two of them.
    /// </summary>
    [Fact]
    public void OnASheetWithNoHolesTheSmallestPossibleHoleStillRefusesASliver()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var strokes = definition.Bulls.Take(4).Select(b => new InkStroke(b.X - 160, b.Y + 50, b.X - 50, b.Y + 160, 0.035 * 254, 60)).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, [], strokes, new Random(820));
        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), pageRender: render);
        Assert.Equal(HoleSizeSource.Bound, result.HoleSize!.Source);
        Assert.Empty(result.Holes);
        Assert.True(strokes.Count == result.Rejected.Count(r => r.Reason.StartsWith("residue", StringComparison.Ordinal)), string.Join(" | ", result.Rejected.Select(r => $"{r.DiameterInches:0.000} {r.Reason}")));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 82 sections 2, 3 and 7: where the size of a single hole comes from. A calibre is used as named. Without one
    /// the sheet's quarter-point round mark is trusted from twelve marks, tentative from five, and below that only the smallest possible hole
    /// is known, which flags nothing. The quarter-point never leaves what a bullet can make. Two clearly separate sizes give no size and a
    /// request for the calibre; a continuous spread, however wide, does not.
    /// </summary>
    [Fact]
    public void TheSizeOfASingleHoleIsGradedByWhatSupportsIt()
    {
        var options = new RenderDifferenceOptions();
        var calibre = RenderDifferenceHoleDetector.SizeReference([0.29, 0.30], options with { CalibreInches = 0.29 });
        Assert.Equal((HoleSizeSource.Calibre, 0.29, (double?)0.29), (calibre.Source, calibre.VetoInches, calibre.FlagInches));

        var few = RenderDifferenceHoleDetector.SizeReference([0.29, 0.30, 0.31], options);
        Assert.Equal((HoleSizeSource.Bound, options.SmallestHoleInches, (double?)null), (few.Source, few.VetoInches, few.FlagInches));

        double[] seven = [0.28, 0.29, 0.29, 0.30, 0.30, 0.31, 0.32];
        var tentative = RenderDifferenceHoleDetector.SizeReference(seven, options);
        Assert.Equal((HoleSizeSource.SheetTentative, options.SmallestHoleInches, (double?)0.29), (tentative.Source, tentative.VetoInches, tentative.FlagInches));

        double[] full = [.. Enumerable.Range(0, 16).Select(i => 0.28 + (0.002 * i))];
        var sheet = RenderDifferenceHoleDetector.SizeReference(full, options);
        Assert.Equal(HoleSizeSource.Sheet, sheet.Source);
        Assert.Equal(full[4], sheet.VetoInches, 9);

        double[] residue = [.. Enumerable.Range(0, 16).Select(i => 0.150 + (0.001 * i))];
        var clamped = RenderDifferenceHoleDetector.SizeReference(residue, options);
        Assert.Equal(options.SmallestHoleInches, clamped.VetoInches, 9);

        double[] twoCalibres = [.. Enumerable.Repeat(0.24, 8).Select((d, i) => d + (0.001 * i)), .. Enumerable.Repeat(0.32, 8).Select((d, i) => d + (0.001 * i))];
        var two = RenderDifferenceHoleDetector.SizeReference(twoCalibres, options);
        Assert.Equal(HoleSizeSource.TwoSizes, two.Source);
        Assert.Null(two.FlagInches);
        Assert.Contains("name the calibre", two.Description, StringComparison.Ordinal);

        double[] spread = [.. Enumerable.Range(0, 20).Select(i => 0.20 + (0.01 * i))];
        Assert.Equal(HoleSizeSource.Sheet, RenderDifferenceHoleDetector.SizeReference(spread, options).Source);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 83 section 2: sizes are read at each hole's own scale. Under a perspective that changes the scale by about a
    /// quarter across the page, identical holes read the same size, where one scale for the image read the near ones large and flagged them.
    /// </summary>
    [Fact]
    public void IdenticalHolesReadTheSameSizeAcrossAnObliqueView()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 2e-4, 0, 1]));
        var holes = definition.Bulls.Select((b, k) => Hole(b.X + 90, b.Y + 90, 0.06, k % 4)).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], new Random(83));
        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), pageRender: render);
        var sizes = result.Holes.Where(h => !h.PossibleMerge).Select(h => h.DiameterInches).Order().ToList();
        double left = RenderDifferenceHoleDetector.LocalPixelsPerInch(truth, new PointD(0, 0)), right = RenderDifferenceHoleDetector.LocalPixelsPerInch(truth, new PointD(render.Width, 0));
        Assert.True(Math.Max(left, right) / Math.Min(left, right) > 1.2, $"the view is not oblique enough: {left:0} and {right:0} px per inch");
        Assert.True(sizes[^1] / sizes[0] < 1.12, $"sizes {sizes[0]:0.000} to {sizes[^1]:0.000} in");
        Assert.DoesNotContain(result.Holes, h => h.Oversized);
    }

    /// <summary>With fewer than five whole marks and no calibre there is no size to veto with, and an elongated blob is split on shape alone.</summary>
    [Fact]
    public void WithoutASizeTheShapeAloneDecides()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var bull = definition.Bulls[0];
        double x = bull.X + 90, y = bull.Y + 90;
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, [Hole(x, y, 0.06, 1)],
            [new InkStroke(x + 20, y, x + 20 + (0.12 * 254), y, 0.05 * 254, 60)], new Random(81));
        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, new OpenCvSharpBackend(), new RenderDifferenceOptions(SplitElongation: 1.45), render);
        Assert.Equal(2, result.Holes.Count);
        Assert.All(result.Holes, h => Assert.True(h.PossibleMerge));
    }

    private static SyntheticHole Hole(double x, double y, double rimRadiusInches, int phase) =>
        new(x, y, rimRadiusInches * 254, 0.035 * 254, 34, 192, 0.014 * 254, [0.3, 0.2, 0.1, 0.1], [0, 1, 2, phase]);

    /// <summary>The detections of each blob, with the bull it lies nearest, page dmm.</summary>
    private static List<(int Bull, List<RenderDifferenceHole> Holes)> Blobs(TargetDefinition definition, IPageMapping truth, RenderDifferenceResult result) =>
        [.. result.Holes.GroupBy(h => (h.HullX, h.HullY)).Select(g =>
        {
            var page = truth.ToPage(new PointD(g.Key.HullX, g.Key.HullY));
            int bull = Enumerable.Range(0, definition.Bulls.Count).MinBy(i => Math.Pow(definition.Bulls[i].X - page.X, 2) + Math.Pow(definition.Bulls[i].Y - page.Y, 2));
            return (bull, g.ToList());
        })];
}
