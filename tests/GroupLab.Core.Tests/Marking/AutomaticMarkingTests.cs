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
        Assert.Equal((render.Width, render.Height), (result.ExpectedArtwork!.Width, result.ExpectedArtwork.Height));
        Assert.Equal(definition.Bulls.Count, result.Detections.Count);
        var truthImage = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        foreach (var (image, assigned) in result.Detections)
        {
            int nearest = Enumerable.Range(0, truthImage.Count).MinBy(i => Math.Pow(truthImage[i].X - image.X, 2) + Math.Pow(truthImage[i].Y - image.Y, 2));
            Assert.Equal(nearest, assigned.Bull);
        }

        // Entry 70 section 5: every bull carries its declared page position for the matching, beside its located image position.
        Assert.All(result.Bulls, b => Assert.Equal(new PointD(definition.Bulls[b.Index].X, definition.Bulls[b.Index].Y), b.Declared));
        Assert.NotNull(result.Assignment);
        Assert.NotNull(result.Rejected);

        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected, result.Summary);

        // Entry 70 section 4: what the matching decided reaches the marking, under the ids the detections were given.
        var review = session.State.Assignment!;
        Assert.Equal(AssignmentMethod.OneToOne, review.Method);
        Assert.Equal(result.Assignment!.Reason, review.Reason);
        Assert.Equal(result.Detections.Count, review.Shots.Count);
        foreach (var shot in session.State.Shots)
        {
            var detail = review.For(shot.Id)!;
            Assert.Equal(shot.Bull, detail.Bull);
            Assert.True(detail.DistanceInches is >= 0 and < 0.5, $"shot {shot.Id} is {detail.DistanceInches} in from its bull");
        }

        Assert.Equal(result.Rejected!.Count, review.Rejected.Count);
        var report = GroupAnalysis.Analyse(session.State);
        // The sighter bulls' shots are reported and left out of the group (NOTES-FROM-PLANNING.md entry 33 section 1), so the group's
        // placement counts cover the scoring shots only.
        int scoring = definition.Bulls.Count(b => b.Scoring);
        Assert.Equal(scoring, report.Automatic);
        Assert.Equal(scoring, report.AllShots!.Shots);
        Assert.Equal(definition.Bulls.Count - scoring, report.SighterShots);
    }
}
