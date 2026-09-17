using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 77 section 5: the sheet's name is printed, name first and identifier second, only where the analyser never
/// looks, so no exclusion box is needed and nothing a sheet with the name or without it reads is different.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class PrintedNameTests
{
    public static TheoryData<string> Sheets => [.. BuiltIns.Files];

    [Fact]
    public void TheNameLeadsTheIdentifierAlongTheTopOfTheLetterSheet()
    {
        var page = SceneBuilder.Build(BuiltIns.Load("GL-CF25-LTR.gltd.json")).Pages[0];
        var name = Assert.Single(page.Items.OfType<TextRun>(), t => t.Layer == SceneLayer.Name);
        Assert.Equal("GroupLab 5x5 Load Development, Letter · GL-20J3-Y141-0BN3-EYME", name.Text);
        Assert.Equal(SceneBuilder.NameFontSize, name.FontSize);
        Assert.True(name.Baseline / 2.0 < 200, "the name is along the top edge");

        // The identifier and the print note stay where every printed sheet has them.
        var identifier = Assert.Single(page.Items.OfType<TextRun>(), t => t.Layer == SceneLayer.Identifier);
        Assert.Equal("GL-20J3-Y141-0BN3-EYME", identifier.Text);
        Assert.Equal((2 * 2794) - 160, identifier.Baseline);
    }

    /// <summary>On every built-in sheet the name is clear of every bull's cell and of everything else printed, or it is not printed.</summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void TheNameIsWhereTheAnalyserNeverLooksOrNowhere(string file)
    {
        var definition = BuiltIns.Load(file);
        var cells = BullCells.Of(definition);
        foreach (var page in SceneBuilder.Build(definition).Pages)
        {
            var names = page.Items.OfType<TextRun>().Where(t => t.Layer == SceneLayer.Name).ToList();
            if (definition.Tiling is not null)
            {
                Assert.Empty(names);
                continue;
            }

            var name = Assert.Single(names);
            var box = Box(name);
            Assert.All(cells, c => Assert.True(Gap(box, (c.X - c.HalfWidth, c.Y - c.HalfHeight, c.X + c.HalfWidth, c.Y + c.HalfHeight)) >= SceneBuilder.NameCellClearance,
                $"{file}: the name is within {SceneBuilder.NameCellClearance} dmm of the cell at ({c.X}, {c.Y})"));
            foreach (var item in page.Items.Where(i => i != name))
            {
                var other = item switch
                {
                    DiscBand d => ((d.CentreX - d.OuterRadius) / 2.0, (d.CentreY - d.OuterRadius) / 2.0, (d.CentreX + d.OuterRadius) / 2.0, (d.CentreY + d.OuterRadius) / 2.0),
                    RectFill r => (r.X / 2.0, r.Y / 2.0, (r.X + r.Width) / 2.0, (r.Y + r.Height) / 2.0),
                    TextRun t => Box(t),
                    _ => throw new InvalidOperationException(),
                };
                Assert.True(Gap(box, other) >= SceneBuilder.NameItemClearance, $"{file}: the name is within {SceneBuilder.NameItemClearance} dmm of a {item.Layer} item");
            }
        }
    }

    /// <summary>
    /// The sheet printed with its name and printed without one, both rasterised by PDFium so the text is on the paper, and both punched with
    /// the same holes, including one just inside each top-row cell under the name, clear of the codes' own zones: render-and-difference finds
    /// every hole, at the same places on both, and swallows nothing more.
    /// </summary>
    [Fact]
    public void ASheetReadsTheSameWithTheNameAsWithout()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const int dpi = 150;
        double s = 254.0 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var top = definition.Bulls.Where(b => b.Scoring).Min(b => b.Y);
        var holes = definition.Bulls.Select((b, k) => new SyntheticHole(b.X + 40, b.Y + 30, 0.10 * 254, 0.065 * 254, 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, k]))
            .Concat(definition.Bulls.Where(b => b.Y == top && b.X > 500 && b.X < 1700).Select(b => new SyntheticHole(b.X - 60, top - 160, 0.10 * 254, 0.065 * 254, 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3])))
            .ToList();
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);

        RenderDifferenceResult Read(GroupLab.Core.Gltd.Model.TargetDefinition printed)
        {
            var pdf = TargetRenderer.Render(printed, new RenderOptions(AllowInvalid: true)).Pdf!;
            var paper = SyntheticSheet.Punch(Raster.WholePage(pdf, 0, dpi), truth, dpi, holes);
            return RenderDifferenceHoleDetector.Detect(paper, definition, 0, truth, dpi, new OpenCvSharpBackend(), pageRender: render);
        }

        var named = Read(definition);
        var unnamed = Read(definition with { Name = " " });
        Assert.Contains(TargetRenderer.Render(definition).Pages[0].Items, i => i.Layer == SceneLayer.Name);
        Assert.DoesNotContain(TargetRenderer.Render(definition with { Name = " " }, new RenderOptions(AllowInvalid: true)).Pages[0].Items, i => i.Layer == SceneLayer.Name);

        Assert.Equal(unnamed.Holes.Select(h => (Math.Round(h.X, 3), Math.Round(h.Y, 3))), named.Holes.Select(h => (Math.Round(h.X, 3), Math.Round(h.Y, 3))));
        Assert.Equal(unnamed.InsideZones.Count, named.InsideZones.Count);
        var missed = holes.Where(h =>
        {
            var at = truth.ToImage(new PointD(h.X, h.Y));
            return !named.Holes.Any(d => Math.Sqrt(Math.Pow(d.X - at.X, 2) + Math.Pow(d.Y - at.Y, 2)) <= 0.15 * dpi);
        }).Select(h => $"({h.X}, {h.Y})").ToList();
        Assert.True(missed.Count == 0, $"missed {string.Join(", ", missed)}");
    }

    private static (double Left, double Top, double Right, double Bottom) Box(TextRun run)
    {
        double width = HelveticaMetrics.TextWidth(run.Text, run.FontSize) / 2.0, x = run.X / 2.0, baseline = run.Baseline / 2.0, size = run.FontSize / 2.0;
        double left = run.Anchor switch { TextAnchor.Centre => x - (width / 2), TextAnchor.Right => x - width, _ => x };
        return (left, baseline - (0.72 * size), left + width, baseline + (0.21 * size));
    }

    private static double Gap((double Left, double Top, double Right, double Bottom) a, (double Left, double Top, double Right, double Bottom) b)
    {
        double dx = Math.Max(0, Math.Max(b.Left - a.Right, a.Left - b.Right));
        double dy = Math.Max(0, Math.Max(b.Top - a.Bottom, a.Top - b.Bottom));
        return Math.Max(dx, dy);
    }
}
