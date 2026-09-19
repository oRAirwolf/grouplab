using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 107 section 2: what decides whether a sheet prints at actual size, as plain functions over a scene and a
/// described printer, so it runs on every platform's CI with no printer: the physical-offset shift, the paper refusal and the ink-in-margin
/// refusal, which covers inked items and never a marker's quiet zone.
/// </summary>
public class PrintFitTests
{
    private static readonly Rgb Black = new(0, 0, 0);

    /// <summary>A 600 dpi printer on Letter with the margins given in device pixels, left, top, right and bottom.</summary>
    private static PrinterPage Letter600(int left = 0, int top = 0, int right = 0, int bottom = 0) =>
        new("Test Printer", 600, 600, 5100, 6600, left, top, 5100 - left - right, 6600 - top - bottom);

    private static Scene LetterPage(params SceneItem[] items) => new(4318, 5588, 0, items);

    [Fact]
    public void GdiDrawsEachScenePointWhereItBelongsOnThePaper()
    {
        // GDI's origin is the printable area's corner, 100 and 120 pixels in from the paper's; the mapping moves it back to the paper's.
        var mapping = PrintFit.Mapping(Letter600(left: 100, top: 120));
        Assert.Equal((-100.0, -120.0), mapping.ToDevice(0, 0));

        // One inch, 508 half-dmm, is 600 device pixels from the paper's edge, which is 500 and 480 from the printable area's.
        Assert.Equal((500.0, 480.0), mapping.ToDevice(508, 508));
        Assert.Equal((508, 508, 600, 600), (mapping.WindowExtX, mapping.WindowExtY, mapping.ViewportExtX, mapping.ViewportExtY));

        // Unequal resolutions scale each axis by its own.
        var uneven = PrintFit.Mapping(new PrinterPage("Test Printer", 300, 600, 2550, 6600, 50, 100, 2450, 6400));
        Assert.Equal((250.0, 500.0), uneven.ToDevice(508, 508));
    }

    [Fact]
    public void ThePaperIsReadInSceneUnitsWithItsPrintableArea()
    {
        var printer = Letter600(left: 100, top: 100, right: 100, bottom: 200);
        Assert.Equal(4318, printer.PaperWidthUnits, 6);
        Assert.Equal(5588, printer.PaperHeightUnits, 6);
        Assert.Equal(100 * 508 / 600.0, printer.PrintableLeft, 6);
        Assert.Equal(4318 - (100 * 508 / 600.0), printer.PrintableRight, 6);
        Assert.Equal(5588 - (200 * 508 / 600.0), printer.PrintableBottom, 6);
    }

    [Fact]
    public void ALetterSheetOnAFourPaperIsRefusedWithBothNamed()
    {
        var a4 = new PrinterPage("Test Printer", 600, 600, 4960, 7016, 0, 0, 4960, 7016);
        string? refusal = PrintFit.PaperRefusal(LetterPage(), a4);
        Assert.NotNull(refusal);
        Assert.Contains("Test Printer is set to A4 (210 x 297 mm) paper", refusal, StringComparison.Ordinal);
        Assert.Contains("this sheet is Letter (215.9 x 279.4 mm)", refusal, StringComparison.Ordinal);
        Assert.Contains("never scales", refusal, StringComparison.Ordinal);
        Assert.Contains("nothing was printed", refusal, StringComparison.Ordinal);

        Assert.Null(PrintFit.PaperRefusal(LetterPage(), Letter600()));
        var landscape = new PrinterPage("Test Printer", 600, 600, 6600, 5100, 0, 0, 6600, 5100);
        Assert.Contains("Letter landscape", PrintFit.PaperRefusal(LetterPage(), landscape), StringComparison.Ordinal);
    }

    [Fact]
    public void AnInkedItemInTheMarginIsRefusedWithTheEdgeTheMarginAndWhatReachesIt()
    {
        // 100 pixels at 600 dpi is 4.2 mm unprinted; a marker module 2 mm from the left edge reaches into it.
        var printer = Letter600(left: 100);
        string? refusal = PrintFit.MarginRefusal(LetterPage(new RectFill(SceneLayer.Markers, Black, 40, 1000, 100, 100)), printer);
        Assert.NotNull(refusal);
        Assert.Contains("the left edge, where the printer leaves 4.2 mm unprinted and the sheet has markers 2.0 mm from it", refusal, StringComparison.Ordinal);
        Assert.Contains("nothing was printed", refusal, StringComparison.Ordinal);

        // Partly in counts as much as wholly in: a disc whose edge crosses into the bottom margin, and text on the right.
        var bottom = Letter600(bottom: 200);
        Assert.Contains("bull artwork", PrintFit.MarginRefusal(LetterPage(new DiscBand(SceneLayer.Bulls, Black, 2000, 5588 - 300, 400, 0)), bottom), StringComparison.Ordinal);
        var right = Letter600(right: 100);
        Assert.Contains("the right edge", PrintFit.MarginRefusal(LetterPage(new TextRun(SceneLayer.Name, Black, 4300, 500, 70, "GL-TEST", TextAnchor.Right)), right), StringComparison.Ordinal);
        Assert.Contains("codes", PrintFit.MarginRefusal(LetterPage(new RectFill(SceneLayer.Codes, Black, 2000, 20, 40, 40)), Letter600(top: 100)), StringComparison.Ordinal);
        Assert.Contains("rules", PrintFit.MarginRefusal(LetterPage(new RectFill(SceneLayer.DataBlockFrame, Black, 2000, 5500, 400, 4)), Letter600(bottom: 300)), StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 107 section 2's decision: the quiet zone is bare paper by design, and the unprintable margin leaves bare paper, so a marker whose
    /// quiet zone falls in the margin while its ink does not prints exactly as it would have, and is not refused.
    /// </summary>
    [Fact]
    public void AMarkerWhoseQuietZoneAloneFallsInTheMarginIsNotRefused()
    {
        var printer = Letter600(left: 100);
        double margin = printer.PrintableLeft;

        // The marker's ink starts 0.5 mm inside the printable area; its quiet zone, 2 mm wide, lies mostly in the margin, and is no item.
        var marker = new RectFill(SceneLayer.Markers, Black, (long)Math.Ceiling(margin) + 10, 1000, 200, 200);
        Assert.Null(PrintFit.MarginRefusal(LetterPage(marker), printer));

        // An item exactly on the printable edge is inside.
        Assert.Null(PrintFit.MarginRefusal(LetterPage(new RectFill(SceneLayer.Markers, Black, (long)Math.Round(margin), 1000, 200, 200)), printer));
    }

    [Fact]
    public void ARefusalOnATiledSetNamesTheSheet()
    {
        var pages = new[] { LetterPage(), LetterPage(new RectFill(SceneLayer.Markers, Black, 10, 1000, 100, 100)) };
        Assert.Contains("on sheet 2 of 2", PrintFit.Refusal(pages, Letter600(left: 100)), StringComparison.Ordinal);
        Assert.Null(PrintFit.Refusal(pages, Letter600()));
    }

    /// <summary>
    /// Every built-in sheet, every tile, prints on a printer with no margin on its own paper, so no refusal comes from the sheets themselves.
    /// </summary>
    [Fact]
    public void EveryBuiltInSheetPrintsWhereThePaperIsAllPrintable()
    {
        foreach (string file in BuiltIns.Files)
        {
            var result = TargetRenderer.Render(BuiltIns.Load(file));
            var page = result.Pages[0];
            int dpi = 600, width = (int)Math.Round(page.Width * dpi / PrintFit.UnitsPerInch), height = (int)Math.Round(page.Height * dpi / PrintFit.UnitsPerInch);
            Assert.True(PrintFit.Refusal(result.Pages, new PrinterPage("Test Printer", dpi, dpi, width, height, 0, 0, width, height)) is null, file);
        }
    }

    [Fact]
    public void TheConfirmationSaysSentToTheQueueAndNothingMore()
    {
        Assert.Equal("GL-CF25-LTR, 1 page at actual size, was sent to the print queue of Brother MFC-J430W Printer.", PrintFit.Confirmation("Brother MFC-J430W Printer", "GL-CF25-LTR", 1));
        Assert.Equal("GL-LR300-T, 6 pages at actual size, was sent to the print queue of Test Printer.", PrintFit.Confirmation("Test Printer", "GL-LR300-T", 6));
    }
}
