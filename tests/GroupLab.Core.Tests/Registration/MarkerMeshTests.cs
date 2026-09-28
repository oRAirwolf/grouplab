using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260, "paper that is not flat", and entry 261's study, which found a sheet bowed by 15 pixels across its
/// width at 300 dpi (0.05 in) read every marker and still failed to register. It is now registered through every marker corner.
/// </summary>
public class MarkerMeshTests
{
    private const double Dpi = 300;

    /// <summary>GL-CF25-LTR photographed flat, then bowed: every column moved down by the curl at that column.</summary>
    private static (GrayImage Image, List<PointD> Holes) Curled(double amplitude, bool withHoles)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(260);
        var holes = withHoles
            ? definition.Bulls.Where(b => b.Scoring).Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871)).ToList()
            : [];
        var flat = withHoles ? SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random) : render;
        double Shift(double x) => amplitude * Math.Sin(2 * Math.PI * x / flat.Width);
        var pixels = new byte[flat.Pixels.Length];
        for (int x = 0; x < flat.Width; x++)
        {
            double shift = Shift(x);
            for (int y = 0; y < flat.Height; y++)
            {
                double from = y - shift;
                int y0 = (int)Math.Floor(from);
                double f = from - y0;
                int a = Math.Clamp(y0, 0, flat.Height - 1), b = Math.Clamp(y0 + 1, 0, flat.Height - 1);
                double v = (flat.Pixels[(a * flat.Width) + x] * (1 - f)) + (flat.Pixels[(b * flat.Width) + x] * f);
                pixels[(y * flat.Width) + x] = (byte)Math.Round(withHoles ? v : v * 225 / 255);
            }
        }

        var at = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).Select(p => new PointD(p.X, p.Y + Shift(p.X))).ToList();
        return (new GrayImage(flat.Width, flat.Height, pixels), at);
    }

    private static ImageMetadata Photo(GrayImage image) => new("JPEG", image.Width, image.Height, null, null, "Test", "Curl", 1, 6.25, 24);

    [Fact]
    public void ACurledSheetIsRegisteredThroughItsMarkers()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, _) = Curled(15, withHoles: false);
        var measured = SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.Null(measured.Failure);
        Assert.IsType<MarkerMesh>(measured.Registration!.Mapping);
        Assert.True(measured.Registration.RmsResidual / 254 < 0.01, $"the mesh's error between markers is {measured.Registration.RmsResidual / 254:0.0000} in");
    }

    /// <summary>A flat sheet keeps its homography: the mesh is only for a sheet the homography cannot hold.</summary>
    [Fact]
    public void AFlatSheetKeepsItsHomography()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, _) = Curled(0, withHoles: false);
        var measured = SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend());
        Assert.Null(measured.Failure);
        Assert.IsNotType<MarkerMesh>(measured.Registration!.Mapping);
    }

    /// <summary>A session measured through the mesh reopens with the same mapping, with no image, as every other registration does.</summary>
    [Fact]
    public void ASavedSessionKeepsItsMesh()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, _) = Curled(15, withHoles: false);
        var mesh = Assert.IsType<MarkerMesh>(SheetMeasurer.Measure(image, Photo(image), definition, new MeasureOptions(), new OpenCvSharpBackend()).Registration!.Mapping);
        var session = new MarkingSession();
        session.Open("sheet.png", 1);
        session.SetScale(new SheetReference(mesh, "38 of 38 markers found"));
        session.AddShot(new PointD(1200, 900));
        var (state, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        var read = Assert.IsType<MarkerMesh>(Assert.IsType<SheetReference>(state.Scale).Mapping);
        Assert.Equal(mesh.LeaveOneOutRms, read.LeaveOneOutRms);
        foreach (var at in new[] { new PointD(300, 400), new PointD(1250, 1650), new PointD(2300, 3000) })
        {
            var a = mesh.ToPage(at);
            var b = read.ToPage(at);
            Assert.True(Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(a.Y - b.Y) < 1e-6, $"{at}: {a} against {b}");
        }
    }

    /// <summary>The whole pipeline on the curled sheet: its holes are found where they are.</summary>
    [Fact]
    public void TheHolesOnACurledSheetAreFound()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var (image, holes) = Curled(15, withHoles: true);
        var result = AutomaticMarking.Run(image, image, Photo(image), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        var errors = holes.Select(h => result.Detections.Select(d => Math.Sqrt(((d.Image.X - h.X) * (d.Image.X - h.X)) + ((d.Image.Y - h.Y) * (d.Image.Y - h.Y)))).DefaultIfEmpty(double.MaxValue).Min() / Dpi)
            .Where(e => e <= 0.1).Order().ToList();
        Assert.True(errors.Count >= 23, $"{errors.Count} of {holes.Count} holes found");
        Assert.True(errors[errors.Count / 2] < 0.01, $"median error {errors[errors.Count / 2]:0.0000} in");
    }
}
