using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 7 item 4, the scoreboard's first proposed fix. On photographs taken 9 and 15 degrees off square,
/// every miss and false mark was one mark 2.1 to 2.3 holes across: a hole and the printed rings beside it read as one, placed up to 0.18 in
/// off the hole, cut into two shots, or refused as not compact. A mark twice a hole across or more is now opened with a disc a hole can fill
/// and a line cannot, and the shot is placed on the one hole-sized part left, shown for review. These are synthetic sheets only: a hole with
/// two arms of ink leaving it, as a ring's residue leaves a hole it passes through, beside the plain holes, a merged pair and one large round
/// hole, which must all read as they did before.
/// </summary>
public class JoinedHoleTests
{
    private const double Dpi = 150;

    private enum Kind
    {
        Plain,
        Joined,
        Pair,
        Large,
    }

    private static (TargetDefinition Definition, GrayImage Render, IPageMapping Truth, GrayImage Observed, Kind[] Kinds, List<PointD> Centres) Sheet()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], Dpi);
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var holes = new List<SyntheticHole>();
        var strokes = new List<InkStroke>();
        var kinds = new Kind[definition.Bulls.Count];
        var centres = new List<PointD>();
        for (int k = 0; k < definition.Bulls.Count; k++)
        {
            var bull = definition.Bulls[k];
            double x = bull.X + 90, y = bull.Y + 90;
            // Most bulls hold one plain hole, so the sheet has its own size of a single hole to judge the others against.
            kinds[k] = (k % 6) switch { 1 => Kind.Joined, 3 when k < 12 => Kind.Pair, 5 when k < 12 => Kind.Large, _ => Kind.Plain };
            centres.Add(new PointD(x, y));
            holes.Add(Hole(x, y, kinds[k] == Kind.Large ? 0.13 : 0.06, k));
            if (kinds[k] == Kind.Joined)
            {
                // Two arms 0.6 in long and 0.08 in wide leaving the hole at 60 degrees either side of the right, as the two sides of a
                // printed ring leave a hole it passes through when the registration is a little off.
                foreach (double angle in new[] { -Math.PI / 3, Math.PI / 3 })
                {
                    strokes.Add(new InkStroke(x, y, x + (0.6 * 254 * Math.Cos(angle)), y + (0.6 * 254 * Math.Sin(angle)), 0.08 * 254, 60));
                }
            }
            else if (kinds[k] == Kind.Pair)
            {
                holes.Add(Hole(x + (0.15 * 254), y, 0.06, k + 100));
            }
        }

        var observed = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, strokes, new Random(2917));
        return (definition, render, truth, observed, kinds, centres);
    }

    [Fact]
    public void AHoleJoinedToArmsOfInkIsPlacedOnTheHoleAndEverythingElseReadsAsBefore()
    {
        var (definition, render, truth, observed, kinds, centres) = Sheet();
        var backend = new OpenCvSharpBackend();
        var result = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, Dpi, backend, pageRender: render);
        var before = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, Dpi, backend,
            new RenderDifferenceOptions(JoinedDiameters: double.PositiveInfinity), render);
        double single = result.HoleSize!.FlagInches!.Value;
        Assert.Equal(HoleSizeSource.Sheet, result.HoleSize.Source);

        for (int k = 0; k < kinds.Length; k++)
        {
            var near = Near(result, truth, centres[k]);
            var nearBefore = Near(before, truth, centres[k]);
            switch (kinds[k])
            {
                case Kind.Joined:
                    // Without the rule the mark is wrong: refused, cut in two, or placed off the hole. With it, one shot sits on the hole.
                    Assert.False(nearBefore.Count == 1 && Off(nearBefore[0], truth, centres[k]) < 0.03,
                        $"bull {k}: the sheet no longer tests the rule, the mark was read on the hole without it");
                    var joined = Assert.Single(near);
                    Assert.True(Off(joined, truth, centres[k]) < 0.03, $"bull {k}: placed {Off(joined, truth, centres[k]):0.000} in off the hole");
                    Assert.True(joined.JoinedHoles >= 2, $"bull {k}: the whole mark holds {joined.JoinedHoles:0.00} holes");
                    // Entry 318 section 1: the whole mark's size across travels with the shot, to be said to the person.
                    Assert.InRange(joined.JoinedAcrossHoles!.Value, 2, 3);
                    Assert.Equal(joined.JoinedAcrossHoles.Value * single, joined.JoinedAcrossInches!.Value, 6);
                    Assert.False(joined.Oversized);
                    Assert.False(joined.PossibleMerge);
                    Assert.InRange(joined.DiameterInches, 0.7 * single, 1.4 * single);
                    break;
                case Kind.Pair:
                    // Holes merged with each other are no wider than two holes and survive the opening whole: judged as they always were.
                    Assert.Equal(nearBefore.Select(h => (h.X, h.Y, h.Oversized, h.PossibleMerge)), near.Select(h => (h.X, h.Y, h.Oversized, h.PossibleMerge)));
                    Assert.All(near, h => Assert.Null(h.JoinedHoles));
                    Assert.True(near.Count == 2 || near.Single().Oversized, $"bull {k}: a merged pair read as one unflagged hole");
                    break;
                case Kind.Large:
                    var large = Assert.Single(near);
                    Assert.True(large.Oversized, $"bull {k}: a large round hole is not flagged");
                    Assert.Null(large.JoinedHoles);
                    break;
                default:
                    var plain = Assert.Single(near);
                    Assert.Null(plain.JoinedHoles);
                    Assert.True(Off(plain, truth, centres[k]) < 0.03);
                    break;
            }
        }
    }

    [Fact]
    public void ASheetWithNoSizeToJudgeByLeavesAJoinedMarkAsItWas()
    {
        // With no caliber and too few marks to measure a hole from, there is no hole size to open with, so nothing is placed by the rule.
        var (definition, render, truth, observed, _, _) = Sheet();
        var few = RenderDifferenceHoleDetector.Detect(observed, definition, 0, truth, Dpi, new OpenCvSharpBackend(), new RenderDifferenceOptions(MarksForTentativeSize: 100, MarksForSheetSize: 100), render);
        Assert.Null(few.HoleSize!.FlagInches);
        Assert.All(few.Holes, h => Assert.Null(h.JoinedHoles));
    }

    [Fact]
    public void AJoinedShotIsReviewedOnItsOwnAndKeepsItsFlagThroughASave()
    {
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);
        BullAim[] bulls = [new(0, "1", new PointD(100, 100)), new(1, "2", new PointD(250, 100)), new(2, "3", new PointD(400, 100))];
        var assigned = new[] { new AssignedShot(0, 0, 5, 0, 5, 3000, false), new AssignedShot(1, 1, 5, 1, 5, 3000, false), new AssignedShot(2, 2, 5, 2, 5, 3000, false) };
        var session = new MarkingSession();
        session.Open("sheet.png");
        // Three of three shots flagged would ask one question about the caliber, if the joined ones counted: they do not.
        session.LoadDetections(scale, bulls,
        [
            new DetectedShot(new PointD(100, 100), assigned[0], 0.24, new DetectedOversize(2.6, false, Joined: true)),
            new DetectedShot(new PointD(250, 100), assigned[1], 0.24, new DetectedOversize(2.4, false, Joined: true)),
            new DetectedShot(new PointD(400, 100), assigned[2], 0.24, new DetectedOversize(1.9, false)),
        ], new ShotAssignmentResult(AssignmentMethod.OneToOne, "test", assigned), [], "test");

        var items = ReviewQueue.For(session.State);
        Assert.Equal([ReviewKind.Oversized, ReviewKind.Joined, ReviewKind.Joined], items.Select(i => i.Kind));
        var joined = items[1];
        Assert.Contains("placed on the part the size of one hole", joined.Sentence, StringComparison.Ordinal);
        Assert.Contains("about 2.6 holes' area", joined.Sentence, StringComparison.Ordinal);
        Assert.Equal([ReviewAction.Keep, ReviewAction.NotAShot], joined.Choices.Select(c => c.Action));

        ReviewQueue.Apply(session, joined, joined.Choices[0]);
        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.True(read.Shots[0].Oversize!.Joined);
        Assert.False(read.Shots[2].Oversize!.Joined);
        Assert.True(ReviewQueue.For(read).Single(i => i.Key == joined.Key).Resolved);

        // Moving the shot is the other way to settle it: the flag described the point the detector chose.
        session.MoveShot(session.State.Shots[1].Id, new PointD(255, 100));
        Assert.Single(ReviewQueue.For(session.State), i => i.Kind == ReviewKind.Joined);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 318 section 1: "a mark much bigger than your bullet goes to Alan to check". A shot placed inside a mark 2.2
    /// times the bullet across is on the result as a flag, said in bullets across, through a save, and stays until the person says it is on
    /// the hole or moves it; a mark flagged as possibly two holes goes when the person says it is one shot.
    /// </summary>
    [Fact]
    public void AMarkMuchBiggerThanTheBulletStaysFlaggedOnTheResultUntilSettled()
    {
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);
        BullAim[] bulls = [new(0, "1", new PointD(100, 100)), new(1, "2", new PointD(250, 100)), new(2, "3", new PointD(400, 100))];
        var assigned = new[] { new AssignedShot(0, 0, 5, 0, 5, 3000, false), new AssignedShot(1, 1, 5, 1, 5, 3000, false), new AssignedShot(2, 2, 5, 2, 5, 3000, false) };
        MarkingSession Loaded()
        {
            var session = new MarkingSession();
            session.Open("sheet.png");
            session.SetCalibre(Calibre.Of(0.243));
            session.LoadDetections(scale, bulls,
            [
                new DetectedShot(new PointD(100, 100), assigned[0], 0.22, new DetectedOversize(2.6, false, Joined: true, AcrossInches: 0.535, AcrossHoles: 2.3)),
                new DetectedShot(new PointD(250, 100), assigned[1], 0.24, null),
                new DetectedShot(new PointD(400, 100), assigned[2], 0.40, new DetectedOversize(1.9, false)),
            ], new ShotAssignmentResult(AssignmentMethod.OneToOne, "test", assigned), [], "test");
            return session;
        }

        var session = Loaded();
        int joined = session.State.Shots[0].Id, plain = session.State.Shots[1].Id, pair = session.State.Shots[2].Id;
        var flags = ReviewQueue.SizeFlags(session.State);
        Assert.Equal([joined, pair], flags.Select(f => f.ShotId));
        var mark = flags[0];
        Assert.True(mark.Joined);
        Assert.Contains("2.2 times your bullet across", mark.Sentence, StringComparison.Ordinal);
        Assert.Contains("check that the hole is where GroupLab put it", mark.Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(flags, f => f.ShotId == plain);

        // Without a caliber the same mark is said in single holes across.
        session.SetCalibre(null);
        Assert.Contains("2.3 times a single hole across", ReviewQueue.SizeFlags(session.State)[0].Sentence, StringComparison.Ordinal);
        session.SetCalibre(Calibre.Of(0.243));

        // It survives a save, and the review queue says the same sentence the result does.
        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.Equal(flags, ReviewQueue.SizeFlags(read));
        var item = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Joined);
        Assert.Equal(mark.Sentence, item.Sentence);

        // Settled by saying it is on the hole, and by saying the other mark is one shot.
        ReviewQueue.Apply(session, item, item.Choices.Single(c => c.Label == "It is on the hole"));
        Assert.Equal([pair], ReviewQueue.SizeFlags(session.State).Select(f => f.ShotId));
        var two = ReviewQueue.For(session.State).Single(i => i.Kind == ReviewKind.Oversized);
        ReviewQueue.Apply(session, two, two.Choices.Single(c => c.Action == ReviewAction.Keep));
        Assert.Empty(ReviewQueue.SizeFlags(session.State));

        // Or by moving it, or by saying it is not a shot.
        var moved = Loaded();
        moved.MoveShot(joined, new PointD(104, 100));
        Assert.DoesNotContain(ReviewQueue.SizeFlags(moved.State), f => f.ShotId == joined);
        var refused = Loaded();
        refused.SetNotAShot(joined, true);
        Assert.DoesNotContain(ReviewQueue.SizeFlags(refused.State), f => f.ShotId == joined);
    }

    private static List<RenderDifferenceHole> Near(RenderDifferenceResult result, IPageMapping truth, PointD centre) =>
        [.. result.Holes.Where(h => Off(h, truth, centre) < 0.45)];

    private static double Off(RenderDifferenceHole hole, IPageMapping truth, PointD centre)
    {
        var page = truth.ToPage(new PointD(hole.X, hole.Y));
        return Math.Sqrt(Math.Pow(page.X - centre.X, 2) + Math.Pow(page.Y - centre.Y, 2)) / 254;
    }

    private static SyntheticHole Hole(double x, double y, double rimRadiusInches, int phase) =>
        new(x, y, rimRadiusInches * 254, 0.035 * 254, 34, 192, 0.014 * 254, [0.3, 0.2, 0.1, 0.1], [0, 1, 2, phase]);
}
