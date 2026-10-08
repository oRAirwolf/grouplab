using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// docs/PROOF-CHECKLIST.md row 13, the rounds fired as a check on the count, against its proposed gate: a sheet with a known count and one hole
/// holding two shots is flagged. SubgroupAndSplitTests holds the queue's sentences on marks whose sizes are typed in; nothing held the whole
/// path, from the pixels of two bullets through one hole to the item that names that hole. Here GL-CF25-LTR is rendered at 300 dpi, one .308
/// hole on every scoring bull and two on one, the second two thirds of a bullet from the first so the paper shows one ragged hole, and the
/// real detector reads it with the calibre given. With the rounds fired entered, the count item is raised and names that hole, and taking it
/// as two shots settles the count; with the right count, or none, nothing is raised.
/// </summary>
public class RoundsFiredOnARenderedSheetTests(ITestOutputHelper output)
{
    private const double Dpi = 300;
    private const double DmmPerInch = 254;

    /// <summary>The bull holding two shots, counted among the scoring bulls.</summary>
    private const int Doubled = 7;

    /// <summary>How far apart the two shots through one hole went, in inches: two thirds of a .308 bullet, so the holes overlap by a third.</summary>
    private const double Apart = 0.2;

    private static (MarkingSession Session, PointD Pair, int Fired) Sheet(bool withPair)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = DmmPerInch / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var random = new Random(1313);
        var holes = new List<SyntheticHole>();
        var scoring = definition.Bulls.Where(b => b.Scoring).ToList();
        PointD pair = default;
        for (int k = 0; k < scoring.Count; k++)
        {
            double x = scoring[k].X + random.Next(-40, 41), y = scoring[k].Y + random.Next(-40, 41);
            // Entry 81: drawn at 0.871, where a synthetic single hole reads what a real .308 hole reads.
            holes.Add(SyntheticSheet.SampleHole(random, x, y, onInk: false, HoleBacking.ScannerLid, 0.871));
            if (withPair && k == Doubled)
            {
                double second = x + (Apart * DmmPerInch);
                holes.Add(SyntheticSheet.SampleHole(random, second, y, onInk: false, HoleBacking.ScannerLid, 0.871));
                pair = new PointD((((x + second) / 2) / s) - 0.5, (y / s) - 0.5);
            }
        }

        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-rounds-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "sheet.png");
            using (var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels))
            {
                OpenCvSharp.Cv2.ImWrite(path, mat);
            }

            var (grey, metadata) = ImageLoader.Load(path);
            var (value, _) = ImageLoader.LoadMaxChannel(path);
            var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
            Assert.Null(result.Failure);
            var session = new MarkingSession();
            session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary);
            session.SetCalibre(Calibre.Of(0.308));
            return (session, pair, holes.Count);
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    private static double Inches(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y))) / Dpi;

    /// <summary>
    /// The pair overlaps by a third of a bullet and reads about 1.4 holes' area, as the count item's own comment says a pair overlapping that
    /// much does, and as a torn single hole can: on this sheet one single hole with a wide torn rim reads 1.48 and is named before it. So the
    /// image cannot say which mark is two and the count does: with the rounds fired entered, the item is raised and names the ragged hole among
    /// the marks most likely to be two, and the hole's own item says it may be two shots through one hole. Once the second shot is placed on
    /// that bull and kept there, the count agrees, no other shot has moved, and nothing about the count or the bulls is left asking. Without
    /// the rounds fired nothing is raised, because there is nothing to check against.
    /// </summary>
    [Fact]
    public void TwoShotsThroughOneHoleAreFlaggedWhenTheRoundsFiredAreEntered()
    {
        var (session, pair, fired) = Sheet(withPair: true);
        var marks = session.State.Shots.Where(s => s.IsShot).ToList();
        Assert.Equal(fired - 1, marks.Count);
        var hole = marks.OrderBy(m => Inches(m.Image, pair)).First();
        Assert.True(Inches(hole.Image, pair) < 0.05, $"the ragged hole was read {Inches(hole.Image, pair):0.000} in from where the two shots went");
        Assert.InRange(hole.Size!.Holes, 1.2, 2.2);

        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);

        session.SetExpectedShots(fired);
        var item = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
        Assert.False(item.Resolved);
        output.WriteLine(item.Sentence);
        Assert.StartsWith($"You fired {fired} and {fired - 1} are marked. Every bull has a shot on it, so a mark may be two.", item.Sentence, StringComparison.Ordinal);
        string named = item.Sentence[item.Sentence.IndexOf("Most likely to be two", StringComparison.Ordinal)..];
        Assert.Contains(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"at {hole.Size.Holes:0.00} holes"), named, StringComparison.Ordinal);

        // The hole's own item says it may be two shots. The detector found no halves in a mark this round, so neither item offers a key press
        // for the second shot: the person puts the two shots on the two holes, both on that bull.
        var own = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Oversized && i.ShotId == hole.Id);
        Assert.False(own.Resolved);
        Assert.Contains("two shots through one hole", own.Sentence, StringComparison.Ordinal);
        Assert.Null(hole.Oversize!.SplitA);
        Assert.DoesNotContain(own.Choices, c => c.Action == ReviewAction.SplitIntoTwo);
        int bull = hole.Bull!.Value;
        var onBulls = session.State.Shots.Where(s => s.IsShot).ToDictionary(s => s.Id, s => s.Bull);
        session.MoveShot(hole.Id, new PointD(pair.X - (Apart * Dpi / 2), pair.Y));
        session.AssignBull(hole.Id, bull);
        int second = session.AddShot(new PointD(pair.X + (Apart * Dpi / 2), pair.Y), bull);

        var after = session.State.Shots.Where(s => s.IsShot).ToList();
        Assert.Equal(fired, after.Count);
        Assert.All(after.Where(s => s.Id != second), s => Assert.Equal(onBulls[s.Id], s.Bull));
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => !i.Resolved && i.Kind is ReviewKind.Count or ReviewKind.Contested or ReviewKind.Doubled);

        // The bull holding two is said, and counted as answered because the person put both shots there.
        var doubled = Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Doubled);
        Assert.True(doubled.Resolved);
        Assert.StartsWith("Bull 8 holds 2 shots", doubled.Sentence, StringComparison.Ordinal);
        var halves = after.Where(s => Inches(s.Image, pair) < Apart).ToList();
        Assert.Equal(2, halves.Count);
        Assert.All(halves, h => Assert.Equal(bull, h.Bull));
    }

    [Fact]
    public void ASheetWhoseCountAgreesRaisesNoCountItem()
    {
        var (session, _, fired) = Sheet(withPair: false);
        Assert.Equal(fired, session.State.Shots.Count(s => s.IsShot));
        session.SetExpectedShots(fired);
        Assert.DoesNotContain(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Count);
    }
}
