using System.IO.Compression;
using System.Text;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 358 section 2: the thermal print mode. One bit at the printer's own dot pitch, every edge on a whole dot, black
/// only, never fitted to the page, and what would be cut off said before printing.
/// </summary>
public class ThermalRasterTests
{
    private static Scene Letter(BullColour colour = BullColour.Black) =>
        SceneBuilder.Build(GltdJsonReader.ReadFile(Repo.PathTo("targets", "GL-CF25-LTR.gltd.json")).Definition!, new RenderOptions(BullColour: colour)).Pages[0];

    private static Scene Label() =>
        SceneBuilder.Build(ParametricSheet.Design(new SheetSpec("Label", "4x6", 2, 2, 1.5, 254, 0, false)).Definition!).Pages[0];

    /// <summary>Every run of black dots along each row and down each column, within the items of one layer.</summary>
    private static List<int> Runs(DotImage image)
    {
        var runs = new List<int>();
        for (int y = 0; y < image.Height; y++)
        {
            int run = 0;
            for (int x = 0; x <= image.Width; x++)
            {
                if (x < image.Width && image[x, y])
                {
                    run++;
                }
                else if (run > 0)
                {
                    runs.Add(run);
                    run = 0;
                }
            }
        }

        for (int x = 0; x < image.Width; x++)
        {
            int run = 0;
            for (int y = 0; y <= image.Height; y++)
            {
                if (y < image.Height && image[x, y])
                {
                    run++;
                }
                else if (run > 0)
                {
                    runs.Add(run);
                    run = 0;
                }
            }
        }

        return runs;
    }

    [Theory]
    [InlineData(203.2, SceneLayer.Codes, 3)]
    [InlineData(300.0, SceneLayer.Codes, 5)]
    [InlineData(304.8, SceneLayer.Codes, 5)]
    [InlineData(203.2, SceneLayer.Markers, 4)]
    [InlineData(300.0, SceneLayer.Markers, 6)]
    public void EveryModuleIsTheSameWholeNumberOfDots(double dpi, SceneLayer layer, int dots)
    {
        var page = Letter();
        var only = page with { Items = [.. page.Items.Where(i => i.Layer == layer)] };
        var print = ThermalRaster.Render(only, new PrintHead(dpi, 2600));
        var runs = Runs(print.Image);

        Assert.NotEmpty(runs);
        Assert.All(runs, r => Assert.Equal(0, r % dots));
        Assert.Contains(runs, r => r == dots);
        if (layer == SceneLayer.Codes)
        {
            // The finder pattern's outer ring: seven modules across, at exactly seven times the module.
            Assert.Contains(runs, r => r == 7 * dots);
        }
    }

    [Theory]
    [InlineData(203.2)]
    [InlineData(300.0)]
    public void EverySymbolIsCentredWithinHalfADotOfTrue(double dpi)
    {
        var page = Letter();
        double s = dpi / 508.0;
        foreach (var rect in page.Items.OfType<RectFill>().Where(r => r.Module is not null))
        {
            var m = rect.Module!;
            long k = ThermalRaster.ModuleDots(m.Size, s);
            var (x0, y0, _, _) = ThermalRaster.Dots(rect, s);
            double trueCentreX = (rect.X - (m.Column * m.Size) + (m.Size * m.Modules / 2.0)) * s;
            double printedCentreX = x0 - (m.Column * k) + (m.Modules * k / 2.0);
            double trueCentreY = (rect.Y - (m.Row * m.Size) + (m.Size * m.Modules / 2.0)) * s;
            double printedCentreY = y0 - (m.Row * k) + (m.Modules * k / 2.0);
            Assert.InRange(Math.Abs(printedCentreX - trueCentreX), 0, 0.5 + 1e-9);
            Assert.InRange(Math.Abs(printedCentreY - trueCentreY), 0, 0.5 + 1e-9);
        }
    }

    [Fact]
    public void ALineKeepsItsWidthWhereverItFalls()
    {
        double s = 203.2 / 508.0;
        var widths = Enumerable.Range(0, 40).Select(i => new RectFill(SceneLayer.MeasurementGrid, new(0, 0, 0), 1000 + i, 0, 8, 100))
            .Select(r => ThermalRaster.Dots(r, s)).Select(d => d.X1 - d.X0).Distinct().ToList();
        Assert.Equal([3L], widths);
    }

    [Fact]
    public void ALabelPrintsAtItsOwnSizeOnAWiderHeadAndNothingIsCutOff()
    {
        var page = Label();
        var print = ThermalRaster.Render(page, new PrintHead(203.2, 832));
        Assert.Equal(813, print.Image.Width);
        Assert.Equal(1219, print.Image.Height);
        Assert.Equal(813, print.PageDots);
        Assert.Null(print.CutOff);
        Assert.True(print.Image.BlackDots() > 0);
    }

    [Fact]
    public void ALetterPageOnAFourInchHeadSaysWhatWouldBeCutOffAndIsNeverShrunk()
    {
        var page = Letter();
        var head = new PrintHead(203.2, 832);
        var print = ThermalRaster.Render(page, head);
        Assert.Equal(832, print.Image.Width);
        Assert.Equal((int)Math.Round(page.Height * head.DotsPerUnit), print.Image.Height);
        Assert.NotNull(print.CutOff);
        Assert.Contains("215.9 mm wide", print.CutOff, StringComparison.Ordinal);
        Assert.Contains("would be cut off", print.CutOff, StringComparison.Ordinal);
        Assert.Contains("markers", print.CutOff, StringComparison.Ordinal);
        Assert.Contains("never shrinks", print.CutOff, StringComparison.Ordinal);
    }

    [Fact]
    public void ABullsColourPrintsBlackOnAThermalPrinter()
    {
        var head = new PrintHead(203.2, 2000);
        Assert.Equal(ThermalRaster.Render(Letter(), head).Image.Bits, ThermalRaster.Render(Letter(BullColour.Red), head).Image.Bits);
    }

    [Fact]
    public void ThePreviewIsTheOneBitImageAsItWillPrint()
    {
        var print = ThermalRaster.Render(Label(), new PrintHead(203.2, 832));
        var grey = print.Image.ToGray();
        Assert.All(grey.Pixels, p => Assert.True(p is 0 or 255));
        Assert.Equal(print.Image.BlackDots(), grey.Pixels.LongCount(p => p == 0));
    }

    [Fact]
    public void ThePdfHoldsTheDotsAtActualSizeAndAsksForNoScaling()
    {
        var page = Label();
        var head = new PrintHead(203.2, 832);
        var print = ThermalRaster.Render(page, head);
        byte[] pdf = ThermalPdf.Write([(page, print)], head);
        string text = Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF-1.7", text, StringComparison.Ordinal);
        Assert.Contains("/PrintScaling /None", text, StringComparison.Ordinal);
        Assert.Contains("/BitsPerComponent 1", text, StringComparison.Ordinal);
        Assert.Contains("/MediaBox [0 0 288 432]", text, StringComparison.Ordinal);

        int start = text.IndexOf("stream\n", text.IndexOf("/Subtype /Image", StringComparison.Ordinal), StringComparison.Ordinal) + 7;
        using var z = new ZLibStream(new MemoryStream(pdf, start, pdf.Length - start), CompressionMode.Decompress);
        var bits = new byte[print.Image.Bits.Length];
        z.ReadExactly(bits);
        Assert.Equal(print.Image.Bits, bits);
    }

    [Fact]
    public void TsplsInvertedRowsKeepThePaddingPaper()
    {
        var image = new DotImage(10, 1);
        image[0, 0] = true;
        Assert.Equal([0x80, 0x00], image.Bits);
        Assert.Equal([0x7F, 0xFF], image.Inverted());
    }
}
