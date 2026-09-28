using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entry 273 section 5: the paper's own edge checks the print scale on every photograph.</summary>
public class PaperEdgeCheckTests
{
    private const double Dpi = 150;
    private static readonly DateOnly Today = new(2026, 9, 28);

    /// <summary>
    /// GL-CF25-LTR printed at <paramref name="printed"/> in the middle of a sheet of Letter paper, the paper on a dark board with a margin
    /// all round, as a photograph with no stated resolution would show it straight on.
    /// </summary>
    private static GrayImage OnPaper(double printed)
    {
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(BuiltIns.Load("GL-CF25-LTR.gltd.json")).Pages[0], Dpi * printed);
        int paperW = (int)Math.Round(8.5 * Dpi), paperH = (int)Math.Round(11 * Dpi), margin = 120;
        int w = paperW + (2 * margin), h = paperH + (2 * margin);
        var pixels = new byte[w * h];
        Array.Fill(pixels, (byte)70);
        for (int y = margin; y < margin + paperH; y++)
        {
            Array.Fill(pixels, (byte)255, (y * w) + margin, paperW);
        }

        int ox = margin + ((paperW - render.Width) / 2), oy = margin + ((paperH - render.Height) / 2);
        for (int y = 0; y < render.Height; y++)
        {
            Array.Copy(render.Pixels, y * render.Width, pixels, ((oy + y) * w) + ox, render.Width);
        }

        return new GrayImage(w, h, pixels);
    }

    private static ImageMetadata Photo(GrayImage image) => new("JPEG", image.Width, image.Height, null, null, "Test", "Phone", 1, 6.25, 24);

    [Fact]
    public void ASheetPrintedWithFitToPageIsSaidToBe()
    {
        var image = OnPaper(0.962);
        var result = AutomaticMarking.Run(image, image, Photo(image), BuiltIns.Load("GL-CF25-LTR.gltd.json"), new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        var paper = result.Paper;
        Assert.NotNull(paper);
        Assert.Equal("Letter", paper.Paper);
        Assert.InRange(paper.Across, 0.955, 0.969);
        Assert.InRange(paper.Down, 0.955, 0.969);
        Assert.Contains("looks like Fit to page", PaperEdgeCheck.Advice(paper, null), StringComparison.Ordinal);

        var agrees = new PrinterProfile("My printer", 0.962, 0.962, PrinterMethod.Card, Today, PrinterProfile.CardUncertainty);
        Assert.Null(PaperEdgeCheck.Advice(paper, agrees));
        Assert.StartsWith("agrees", PaperEdgeCheck.Agreement(paper, agrees), StringComparison.Ordinal);
        var other = agrees with { Name = "Office", Across = 1.0, Down = 1.0 };
        Assert.Contains("disagrees with Office's 100.0%", PaperEdgeCheck.Advice(paper, other), StringComparison.Ordinal);
    }

    [Fact]
    public void ASheetPrintedAtItsTrueSizeSaysNothing()
    {
        var image = OnPaper(1.0);
        var result = AutomaticMarking.Run(image, image, Photo(image), BuiltIns.Load("GL-CF25-LTR.gltd.json"), new OpenCvSharpBackend());
        Assert.NotNull(result.Paper);
        Assert.InRange(result.Paper.Scale, 0.993, 1.007);
        Assert.Null(PaperEdgeCheck.Advice(result.Paper, null));
    }
}
