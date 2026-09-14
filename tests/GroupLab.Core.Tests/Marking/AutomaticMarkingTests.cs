using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// The automatic path as the marking screen uses it, NOTES-FROM-PLANNING.md entry 21 section 3: on a rendered GL-CF25-LTR with a
/// hole beside every bull, registration from the printed markers, render-and-difference and one-to-one assignment pre-fill one
/// automatic shot per bull, each assigned to the bull it was punched beside, and the session loads them as ordinary marks.
/// </summary>
public class AutomaticMarkingTests
{
    [Fact]
    public void ARenderedSheetIsPrefilledWithOneAssignedShotPerBull()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));

        bool OnInk(double x, double y)
        {
            var p = truth.ToImage(new PointD(x, y));
            return render.Pixels[((int)p.Y * render.Width) + (int)p.X] < 128;
        }

        var holes = definition.Bulls.Select(b =>
        {
            double x = b.X + 30, y = b.Y - 20;
            bool ink = OnInk(x, y);
            return new SyntheticHole(x, y, 0.10 * 254, (ink ? 0.08 : 0.065) * 254, ink ? 48 : 34, 192, 0.006 * 254, [0.15, 0.10, 0.05, 0.05], [0, 1, 2, 3]);
        }).ToList();
        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], new Random(5));

        var result = AutomaticMarking.Run(observed, observed, ImageMetadata.ForScan(render.Width, render.Height, dpi), definition, new OpenCvSharpBackend());

        Assert.Null(result.Failure);
        Assert.NotNull(result.Scale);
        Assert.Empty(result.MissingMarkers);
        Assert.Equal(definition.Bulls.Count, result.Detections.Count);
        var truthImage = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        foreach (var (image, bull) in result.Detections)
        {
            int nearest = Enumerable.Range(0, truthImage.Count).MinBy(i => Math.Pow(truthImage[i].X - image.X, 2) + Math.Pow(truthImage[i].Y - image.Y, 2));
            Assert.Equal(nearest, bull);
        }

        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Summary);
        var report = GroupAnalysis.Analyse(session.State);
        Assert.Equal(definition.Bulls.Count, report.Automatic);
        Assert.Equal(definition.Bulls.Count, report.AllShots!.Shots);
    }
}
