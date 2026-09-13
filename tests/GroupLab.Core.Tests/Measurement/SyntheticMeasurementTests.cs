using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// PHASE0-SPIKE-BRIEF.md section 5: validate a bull locator on the synthetic raster, where conformance test 43 already
/// reaches 0.0002 in, before pointing it at paper, so the estimator is known not to be the limit. Both locators, on the
/// real PDF rasterised by PDFium and distorted by <see cref="Perturbation.Phase0"/>, must recover every bull inside test 43's
/// gate, and the tile must be inferred from the markers alone.
/// </summary>
[Collection(PdfiumCollection.Name)]
public class SyntheticMeasurementTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("GL-CF25-LTR.gltd.json", 0, 300)]
    [InlineData("GL-CF25-LTR.gltd.json", 0, 600)]
    [InlineData("GL-LR300-T.gltd.json", 2, 300)]
    public void BothLocatorsRecoverEveryBullOnAPerturbedRender(string file, int page, int dpi)
    {
        var definition = BuiltIns.Load(file);
        var rendered = TargetRenderer.Render(definition);
        var backend = new OpenCvSharpBackend();
        var clean = Raster.WholePage(rendered.Pdf!, page, dpi);
        var (transform, width, height) = Perturbation.Phase0.For(clean.Width, clean.Height, dpi);
        var scan = backend.WarpPerspective(clean, transform, width, height);
        var metadata = ImageMetadata.ForScan(width, height, dpi);
        var options = new MeasureOptions();
        var trace = new TraceRecorder();

        var fiducials = SheetMeasurer.DetectFiducials(scan, metadata, definition, options, backend, trace);
        var fit = SheetMeasurer.Register(scan, metadata, fiducials, options, backend, trace);

        Assert.Equal(rendered.Pages[page].TileIndex, fiducials.TileIndex);
        Assert.NotNull(fit);
        foreach (var locator in Enum.GetValues<BullLocatorKind>())
        {
            var bulls = SheetMeasurer.LocateBulls(scan, definition, fit.Mapping, options with { Locator = locator }, trace);
            var worst = bulls.MaxBy(b => b.Error)!;
            output.WriteLine($"{file} page {page} at {dpi} DPI, {locator}: mean {bulls.Average(b => b.Error) / 254:0.00000} in, worst {worst.Error / 254:0.00000} in at {worst.Name}"
                + (bulls.All(b => b.InkSpread is null) ? "" : $", ink spread {bulls.Average(b => b.InkSpread!.Value) / 10:+0.0000;-0.0000} mm"));
            Assert.All(bulls, b => Assert.Null(b.Failure));
            Assert.True(worst.Error < SyntheticScanCheck.Gate, $"{locator}: bull {worst.Name} is {worst.Error / 254:0.00000} in out.");
        }
    }
}
