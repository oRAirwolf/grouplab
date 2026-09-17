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
/// <item><b>A single hole with a stroke of hand ink joined to it:</b> elongated, and cut in two without a calibre. With the size of one hole
/// named it is one hole, because its area holds fewer than two.</item>
/// <item><b>Two overlapping holes:</b> split with the size named as without it. The size only stops a split, it never prevents a real one.</item>
/// <item><b>One large round hole:</b> about three holes' area and nothing to split along. It stays one mark, flagged oversized, rather than
/// being cut to agree with the size.</item>
/// <item><b>A plain single hole:</b> untouched either way.</item>
/// </list>
/// The named size is the synthesis's own single-hole diameter, 0.235 in here, as a real sheet's is the calibre times what its holes measure.
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
        var without = Blobs(definition, truth, RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, backend, pageRender: render));
        var with = Blobs(definition, truth, RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, dpi, backend, new RenderDifferenceOptions(CalibreInches: SingleHoleInches), render));

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
                    break;
            }
        }
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
