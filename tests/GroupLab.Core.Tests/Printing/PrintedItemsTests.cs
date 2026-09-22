using GroupLab.Cli.Printing;
using GroupLab.Core.Imaging;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using GroupLab.Core.Rendering.Pdf;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// The printers these tests may use, NOTES-FROM-PLANNING.md entry 114 section 1. <b>Nothing here enumerates the machine's printers.</b> The
/// list is the two Microsoft drivers that write a file and open nothing, and whatever <c>GROUPLAB_PRINT_DRIVERS</c> names, semicolon
/// separated, for a real driver to be exercised deliberately. Every job is redirected to a file, so no paper is used; a driver that opens an
/// application when it prints, as the OneNote one does, must never be named here.
/// </summary>
public static class SilentPrinters
{
    public const string Variable = "GROUPLAB_PRINT_DRIVERS";

    public static IReadOnlyList<string> Installed()
    {
        if (!OperatingSystem.IsWindows())
        {
            return [];
        }

        var named = (Environment.GetEnvironmentVariable(Variable) ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return [.. new[] { PrintToPdfFactAttribute.Printer, "Microsoft XPS Document Writer" }.Concat(named).Distinct(StringComparer.OrdinalIgnoreCase).Where(WindowsPrinter.Installed)];
    }
}

/// <summary>
/// Runs where a printer that writes a file silently is installed. The driver comparison needs two, since one driver cannot tell a fault that
/// only some drivers have; xunit v2 decides a skip at discovery, so the check is made here.
/// </summary>
public sealed class PrintDriversFactAttribute : FactAttribute
{
    public PrintDriversFactAttribute(int needed = 1)
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Printing from inside GroupLab is Windows only, and this is not Windows.";
        }
        else if (SilentPrinters.Installed().Count < needed)
        {
            Skip = $"This machine has {SilentPrinters.Installed().Count} of the printers that write a file silently, and the comparison needs {needed}. Name another with {SilentPrinters.Variable}; docs/PHASE1-RESULTS.md entry 114 gives the measurement made against the Brother driver by hand.";
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 114 section 1, the comparison that would have caught the fault before it cost a range trip. The in-app print
/// path drew its rectangles with <c>FillRect</c>, a pattern blit; the Brother MFC-J430W driver dropped every one, so its sheets carried no
/// markers, no codes and no load block rules and could not be measured, while Microsoft Print to PDF honoured them and the printed-size test
/// passed. Two checks stand in its place:
/// <list type="number">
/// <item><b>Every built-in sheet, item by item.</b> Printed through the in-app path to "Microsoft Print to PDF" and rasterised, against the
/// same sheet from Save PDF rasterised the same way: every code, marker, ring, rule and text item must carry its ink in the same place.</item>
/// <item><b>Every driver that writes a file silently.</b> One sheet printed to a file with its rectangles and again without them: a driver
/// that keeps them writes a larger job, and a driver that drops them writes the same bytes either way, which is exactly what the Brother did.
/// The printers are the fixed list of <see cref="SilentPrinters"/>, never the machine's own, so nothing reaches paper or opens an
/// application.</item>
/// </list>
/// Neither test needs paper: every job goes to a file.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class PrintedItemsTests(ITestOutputHelper output)
{
    private const int Dpi = 200;

    /// <summary>How much of the reference's ink an item must keep to count as printed. A dropped item keeps none.</summary>
    private const double InkKept = 0.5;

    [PrintDriversFact]
    public void EveryItemOfEveryBuiltInSheetReachesThePrinterWhereSavePdfPutsIt()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var failures = new List<string>();
        int sheets = 0, refused = 0, items = 0;
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("targets"), "*.gltd.json").Order(StringComparer.Ordinal))
        {
            var definition = BuiltIns.Load(Path.GetFileName(file));
            var pages = TargetRenderer.Render(definition).Pages;
            string printed = Path.Combine(Path.GetTempPath(), $"grouplab-items-{Guid.NewGuid():N}.pdf");
            try
            {
                var outcome = WindowsPrinter.PrintTo(PrintToPdfFactAttribute.Printer, pages, definition.Name, printed);
                if (outcome.Kind == PrintOutcomeKind.Refused)
                {
                    // A sheet the driver's paper or margins cannot take is refused before anything is drawn, which is PrintFit's own test.
                    refused++;
                    continue;
                }

                Assert.True(outcome.Kind == PrintOutcomeKind.Sent, outcome.Message);
                byte[] fromPrinter = Spooled(printed, "%%EOF"u8.ToArray());
                byte[] fromSavePdf = PdfWriter.Write(pages);
                sheets++;
                for (int page = 0; page < pages.Count; page++)
                {
                    var driver = Raster.WholePage(fromPrinter, page, Dpi);
                    var reference = Raster.WholePage(fromSavePdf, page, Dpi);
                    Assert.True(driver.Width == reference.Width && driver.Height == reference.Height,
                        $"{definition.Name} page {page + 1}: the printed page is {driver.Width} by {driver.Height} pixels and Save PDF's is {reference.Width} by {reference.Height}");
                    foreach (var (kind, missing, counted) in Compare(pages[page], driver, reference))
                    {
                        items += counted;
                        if (missing > 0)
                        {
                            failures.Add($"{definition.Name} page {page + 1}: {missing} of {counted} {kind} are missing from the printed sheet");
                        }
                    }
                }
            }
            finally
            {
                Delete(printed);
            }
        }

        output.WriteLine($"{sheets} sheets compared item by item, {items} items, {refused} refused by the driver's paper or margins");
        Assert.True(sheets >= 10, $"only {sheets} sheets were compared");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>Each kind of item on a page, how many carry no ink where the reference has it, and how many were counted.</summary>
    private static IEnumerable<(string Kind, int Missing, int Counted)> Compare(Scene page, GrayImage driver, GrayImage reference)
    {
        var missing = new Dictionary<string, int>(StringComparer.Ordinal);
        var counted = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in page.Items)
        {
            var box = Box(item);
            if (box is not { } b)
            {
                continue;
            }

            double ink = Ink(reference, b);
            if (ink <= 0)
            {
                // The paper's own knockouts lay no ink, so there is nothing to compare.
                continue;
            }

            string kind = Kind(item);
            counted[kind] = counted.GetValueOrDefault(kind) + 1;
            if (Ink(driver, b) < InkKept * ink)
            {
                missing[kind] = missing.GetValueOrDefault(kind) + 1;
            }
        }

        return counted.Select(c => (c.Key, missing.GetValueOrDefault(c.Key), c.Value));
    }

    private static string Kind(SceneItem item) => item switch
    {
        RectFill => item.Layer switch
        {
            SceneLayer.Markers => "markers",
            SceneLayer.Codes => "code modules",
            SceneLayer.DataBlockFrame or SceneLayer.DataBlockContent => "load block rules",
            _ => "rectangles",
        },
        DiscBand => "ring bands",
        TextRun => "text items",
        _ => "other items",
    };

    /// <summary>An item's box in half-dmm on the page, or null for an item with no ink of its own.</summary>
    private static (long Left, long Top, long Right, long Bottom)? Box(SceneItem item) => item switch
    {
        RectFill r => (r.X, r.Y, r.X + r.Width, r.Y + r.Height),
        DiscBand d => (d.CentreX - d.OuterRadius, d.CentreY - d.OuterRadius, d.CentreX + d.OuterRadius, d.CentreY + d.OuterRadius),
        TextRun t => (t.X - HelveticaMetrics.TextWidth(t.Text, t.FontSize), t.Baseline - t.FontSize, t.X + HelveticaMetrics.TextWidth(t.Text, t.FontSize), t.Baseline + (t.FontSize / 4)),
        _ => null,
    };

    /// <summary>How much ink a box holds, as the sum of how dark its pixels are, so a thin rule counts as well as a filled square.</summary>
    private static double Ink(GrayImage image, (long Left, long Top, long Right, long Bottom) box)
    {
        const double perUnit = Dpi / 508.0;
        int left = (int)Math.Floor(box.Left * perUnit), top = (int)Math.Floor(box.Top * perUnit);
        int right = (int)Math.Ceiling(box.Right * perUnit), bottom = (int)Math.Ceiling(box.Bottom * perUnit);
        double ink = 0;
        for (int y = Math.Max(0, top); y <= Math.Min(image.Height - 1, bottom); y++)
        {
            for (int x = Math.Max(0, left); x <= Math.Min(image.Width - 1, right); x++)
            {
                ink += Math.Max(0, 255 - image.Pixels[(y * image.Width) + x]);
            }
        }

        return ink;
    }

    /// <summary>
    /// Entry 114 section 1: a driver that drops what GroupLab draws writes the same job whether the marks are there or not. The sheet is
    /// printed to a file with its rectangles and again without them, through each of <see cref="SilentPrinters"/>, and a driver that keeps
    /// them writes more.
    /// </summary>
    [PrintDriversFact(2)]
    public void EveryInstalledDriverKeepsTheSheetsRectangles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var full = TargetRenderer.Render(definition).Pages;
        var without = full.Select(p => p with { Items = [.. p.Items.Where(i => i is not RectFill)] }).ToList();
        Assert.True(full[0].Items.OfType<RectFill>().Count() > 1000, "the sheet carries no rectangles to compare");

        var failures = new List<string>();
        int compared = 0;
        foreach (string printer in SilentPrinters.Installed())
        {
            long With = Job(printer, full, "with its rectangles"), Without = Job(printer, without, "without them");
            if (With < 0 || Without < 0)
            {
                output.WriteLine($"{printer}: it would not take the sheet, so it is not compared");
                continue;
            }

            compared++;
            output.WriteLine($"{printer}: {With} bytes with the rectangles, {Without} without");
            if (With <= Without)
            {
                failures.Add($"{printer} wrote {With} bytes for the sheet and {Without} for the same sheet with every rectangle taken out: its driver is dropping them, so its sheets carry no markers and cannot be measured");
            }
        }

        Assert.True(compared >= 2, $"only {compared} of the printers that write a file silently took the sheet, and a fault only some drivers have needs two");
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The bytes a printer writes for these pages, or -1 where it would not take them.</summary>
    private static long Job(string printer, IReadOnlyList<Scene> pages, string what)
    {
        string file = Path.Combine(Path.GetTempPath(), $"grouplab-driver-{Guid.NewGuid():N}.out");
        try
        {
            var outcome = WindowsPrinter.PrintTo(printer, pages, $"GroupLab, {what}", file);
            return outcome.Kind == PrintOutcomeKind.Sent ? Spooled(file, null).Length : -1;
        }
        finally
        {
            Delete(file);
        }
    }

    /// <summary>The spooler writes the file after EndDoc returns, so this waits for it to be whole: to its ending, or to stop growing.</summary>
    private static byte[] Spooled(string path, byte[]? ending)
    {
        var until = DateTime.UtcNow.AddSeconds(120);
        long last = -1;
        while (DateTime.UtcNow < until)
        {
            try
            {
                if (File.Exists(path))
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                    if (stream.Length > 0)
                    {
                        var bytes = new byte[stream.Length];
                        stream.ReadExactly(bytes);
                        if (ending is null ? bytes.Length == last : bytes.AsSpan().LastIndexOf(ending) >= 0)
                        {
                            return bytes;
                        }

                        last = bytes.Length;
                    }
                }
            }
            catch (IOException)
            {
                // Still being written.
            }

            Thread.Sleep(500);
        }

        throw new TimeoutException($"{path} was not written within two minutes.");
    }

    /// <summary>The spooler may still hold the file; it is a temporary one either way, so a cleanup that cannot delete gives up quietly.</summary>
    private static void Delete(string path) => GroupLab.Tests.Support.Temp.DeleteFile(path);
}
