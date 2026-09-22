using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Tests.Support;

/// <summary>
/// A generated GL-CF25-LTR sheet with one hole on each of the first so many bulls, detected and put through a marking session, for the tests
/// NOTES-FROM-PLANNING.md entry 140 sections 3 and 4 ask for.
/// <para>
/// The holes are the detector's to judge: nothing here tells it what it should find. The registration is the truth mapping the sheet was drawn
/// through rather than a detected one, which is what lets a sheet be analysed in a couple of seconds instead of a couple of minutes.
/// </para>
/// </summary>
public static class GeneratedSheet
{
    public const double Dpi = 150;

    /// <summary>The rim radius of one hole, inches. It reads at about 0.29 in across: a 6.5 mm hole as docs/SCAN-MEASUREMENTS.md measured them.</summary>
    public const double RimRadiusInches = 0.075;

    /// <summary>One hole on each of the first <paramref name="shots"/> scoring bulls, plus a second into the third bull where asked.</summary>
    /// <param name="calibreInches">The calibre the person named, or null where they have named none.</param>
    /// <param name="holeRadiusInches">The rim radius every hole is drawn at, so a sheet of another calibre can be generated.</param>
    public static (RenderDifferenceResult Holes, TargetDefinition Definition, IPageMapping Truth) Detect(
        int shots, double? calibreInches = null, bool withARealDouble = false, int seed = 1403, double holeRadiusInches = RimRadiusInches)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var scoring = Scoring(definition).Take(shots).ToList();
        var holes = scoring.Select(b => Hole(b.Bull.X + 90, b.Bull.Y + 90, b.Index, holeRadiusInches)).ToList();
        if (withARealDouble)
        {
            // Two shots into the third bull, far enough over each other that their shape does not ask to be cut: one mark holding two holes.
            holes.Add(Hole(scoring[2].Bull.X + 90 + (0.10 * 254), scoring[2].Bull.Y + 90, 103, holeRadiusInches));
        }

        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], new Random(seed));
        var options = new RenderDifferenceOptions(CalibreInches: calibreInches);
        return (RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, Dpi, new OpenCvSharpBackend(), options, render), definition, truth);
    }

    /// <summary>The definition's scoring bulls with the index each has in the definition.</summary>
    public static IEnumerable<(Bull Bull, int Index)> Scoring(TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Bulls.Select((b, i) => (Bull: b, Index: i)).Where(b => b.Bull.Scoring);
    }

    private static SyntheticHole Hole(double x, double y, int phase, double radiusInches) =>
        new(x, y, radiusInches * 254, 0.035 * 254, 34, 192, 0.014 * 254, [0.3, 0.2, 0.1, 0.1], [0, 1, 2, phase]);

    /// <summary>
    /// The detector's marks loaded into <paramref name="session"/>, each on the bull it lies nearest, so the review queue sees them as it would
    /// after a real analysis. Passing an open session is how a second sheet is opened on top of a first.
    /// </summary>
    public static MarkingSession Marked(RenderDifferenceResult holes, TargetDefinition definition, IPageMapping truth, MarkingSession? session = null, string path = "sheet.png", DetectionRecord? detection = null)
    {
        ArgumentNullException.ThrowIfNull(holes);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(truth);
        var bulls = definition.Bulls
            .Select((b, i) => new BullAim(i, (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), truth.ToImage(new PointD(b.X, b.Y)), b.Scoring))
            .ToList();
        var marks = holes.Holes.GroupBy(h => (h.HullX, h.HullY)).Select(g => g.First()).ToList();
        var detections = new List<DetectedShot>();
        var assigned = new List<AssignedShot>();
        for (int k = 0; k < marks.Count; k++)
        {
            var mark = marks[k];
            var page = truth.ToPage(new PointD(mark.HullX, mark.HullY));
            int bull = bulls.Where(b => b.Scoring).MinBy(b => Away(definition.Bulls[b.Index], page))!.Index;
            double away = Math.Sqrt(Away(definition.Bulls[bull], page));
            var shot = new AssignedShot(k, bull, away, bull, away, 3000, false);
            assigned.Add(shot);
            detections.Add(new DetectedShot(new PointD(mark.X, mark.Y), shot, mark.DiameterInches,
                mark.Oversized ? new DetectedOversize(mark.SizeHoles ?? 0, mark.OversizeTentative, mark.SplitA, mark.SplitB) : null));
        }

        session ??= new MarkingSession();
        session.Open(path);
        session.LoadDetections(new LengthReference(new PointD(0, 0), new PointD(Dpi, 0), 1), bulls, detections,
            new ShotAssignmentResult(AssignmentMethod.OneToOne, "test", assigned), [], "test", detection);
        return session;
    }

    private static double Away(Bull bull, PointD page) => Math.Pow(bull.X - page.X, 2) + Math.Pow(bull.Y - page.Y, 2);
}
