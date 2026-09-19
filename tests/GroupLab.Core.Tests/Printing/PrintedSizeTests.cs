using GroupLab.Cli.Imaging;
using GroupLab.Cli.Printing;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Printing;

/// <summary>
/// Runs only where "Microsoft Print to PDF" is installed, which is Windows with the feature on; anywhere else it is skipped with that reason
/// named. xunit v2 decides a skip at discovery, so the check is made here rather than inside the test.
/// </summary>
public sealed class PrintToPdfFactAttribute : FactAttribute
{
    public const string Printer = "Microsoft Print to PDF";

    public PrintToPdfFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = $"Printing from inside GroupLab is Windows only, and this is not Windows, so \"{Printer}\" cannot be printed to.";
        }
        else if (!WindowsPrinter.Installed(Printer))
        {
            Skip = $"\"{Printer}\" is not installed on this machine, so the printed size cannot be checked here; docs/PHASE1-RESULTS.md entry 107 gives the check by hand.";
        }
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 107 section 2, the test that matters: a sheet printed through GroupLab's own Windows printing, the same GDI
/// drawing the Print button uses, to "Microsoft Print to PDF", then rasterised and registered. Every marker must sit within 0.1 mm of its
/// definition coordinates on the page, which holds only if the sheet reached the paper at actual size and in the right place.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class PrintedSizeTests(ITestOutputHelper output)
{
    private const int Dpi = 600;

    [PrintToPdfFact]
    public void APrintedSheetsMarkersLandWithinATenthOfAMillimetreOfTheDefinition()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var pages = TargetRenderer.Render(definition).Pages;
        string file = Path.Combine(Path.GetTempPath(), $"grouplab-printed-{Guid.NewGuid():N}.pdf");
        try
        {
            var outcome = WindowsPrinter.PrintTo(PrintToPdfFactAttribute.Printer, pages, "GL-CF25-LTR", file);
            Assert.True(outcome.Kind == PrintOutcomeKind.Sent, outcome.Message);

            byte[] pdf = WaitForSpooledFile(file);
            var image = Raster.WholePage(pdf, 0, Dpi);
            // The driver writes its page box to its own rounding, which comes to a pixel at most; the markers are what measure the size.
            Assert.True(Math.Abs(image.Width - 5100) <= 1 && Math.Abs(image.Height - 6600) <= 1, $"the printed page is {image.Width} x {image.Height} pixels at {Dpi} dpi, not Letter's 5100 x 6600");

            var markers = FiducialDerivation.Derive(definition).Markers!;
            var expected = MarkerIds.Assign(markers.Positions, definition.Tiling, 0, MarkerIds.DictionarySize(definition.Fiducials!.Family)).Markers;
            double side = definition.Fiducials.MarkerSize / 254.0 * Dpi;
            var found = new OpenCvSharpBackend().DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, side)).Markers;
            Assert.True(found.Count == expected.Count, $"{found.Count} markers found in the printed page, {expected.Count} defined");

            var errors = new List<string>();
            double worst = 0;
            foreach (var marker in expected)
            {
                var hit = Assert.Single(found, m => m.Id == marker.Id);

                // Pixel centres are whole numbers, so a pixel's page position is half a pixel in from its left and top edges.
                double x = (hit.Corners.Average(c => c.X) + 0.5) / Dpi * 254, y = (hit.Corners.Average(c => c.Y) + 0.5) / Dpi * 254;
                double error = Math.Sqrt(Math.Pow(x - marker.X, 2) + Math.Pow(y - marker.Y, 2));
                worst = Math.Max(worst, error);
                if (error > 1)
                {
                    errors.Add($"marker {marker.Id} at ({x:0.00}, {y:0.00}) dmm, defined at ({marker.X}, {marker.Y}): {error / 10:0.000} mm");
                }
            }

            Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
            output.WriteLine($"worst marker {worst / 10:0.0000} mm from its definition, over {expected.Count} markers");
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>The printer writes its file from the spooler after EndDoc returns, so the test waits for it to be complete.</summary>
    private static byte[] WaitForSpooledFile(string path)
    {
        var until = DateTime.UtcNow.AddSeconds(90);
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
                        if (bytes.AsSpan().LastIndexOf("%%EOF"u8) >= 0)
                        {
                            return bytes;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Still being written.
            }

            Thread.Sleep(250);
        }

        throw new TimeoutException($"{PrintToPdfFactAttribute.Printer} did not write its file within 90 seconds.");
    }
}
