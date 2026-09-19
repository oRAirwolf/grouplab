using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using System.Collections.Immutable;
using GroupLab.Cli.Imaging;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 113 section 4: the doubles sheet breaks one shot a bull on purpose, two shots into each of bulls 1 to 10 and
/// none into 11 to 25, on a synthetic scan of GL-CF25-LTR. Read without a rule, one-to-one matching pushes each second shot onto an empty
/// bull and the review queue raises every one of them. Read with the rule that says so, bulls 1 to 10 hold two each and nothing is raised,
/// whether the rule names the bulls taking two or reads the sheet by nearest bull; and the rule survives the marking file.
/// </summary>
public class DoublesSheetTests
{
    private static MarkingSession Doubles()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 300);
        var random = new Random(1134);
        var scoring = definition.Bulls.Select((b, i) => (Bull: b, Index: i)).Where(b => b.Bull.Scoring).ToList();
        var holes = scoring.Take(10)
            .SelectMany(b => new[] { (X: b.Bull.X - 70.0, Y: b.Bull.Y - 20.0), (X: b.Bull.X + 60.0, Y: b.Bull.Y + 40.0) })
            .Select(p => SyntheticSheet.SampleHole(random, p.X, p.Y, onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / 300.0;
        var image = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(image, image, new ImageMetadata("PNG", image.Width, image.Height, 300, 300, null, null, null, null, null), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        Assert.Equal(20, session.State.Shots.Count(sh => sh.IsShot));
        return session;
    }

    private static IReadOnlyList<int> FirstTen(MarkingState state) => [.. state.Bulls.Where(b => b.Scoring).Take(10).Select(b => b.Index)];

    [Fact]
    public void WithoutARuleTheQueueRaisesEverySecondShotPushedOntoAnEmptyBull()
    {
        var session = Doubles();
        var state = session.State;
        var first = FirstTen(state).ToHashSet();
        Assert.Equal(10, state.Shots.Count(sh => sh.IsShot && sh.Bull is { } b && !first.Contains(b)));

        // The matching can chain, moving a first shot on to make room for a second, so at least ten shots are off their nearest bull, and
        // the queue raises every one of them, naming the bull that already holds a shot.
        var contested = ReviewQueue.For(state, false).Where(i => i.Kind == ReviewKind.Contested && !i.Resolved).ToList();
        var pushed = state.Shots.Where(sh => sh.IsShot && state.Assignment!.For(sh.Id) is { } d && d.Bull != d.NearestBull).Select(sh => sh.Id).ToList();
        Assert.True(pushed.Count >= 10, $"{pushed.Count} pushed");
        Assert.All(pushed, id => Assert.Contains(contested, i => i.ShotId == id && i.Sentence.Contains("already holds shot", StringComparison.Ordinal)));
    }

    [Fact]
    public void TwoShotsOnTheNamedBullsReadAsTheyWereFired()
    {
        var session = Doubles();
        var first = FirstTen(session.State);
        session.SetAssignmentRule(new AssignmentRule(false, first.ToImmutableDictionary(b => b, _ => 2)));
        var state = session.State;
        Assert.All(first, b => Assert.Equal(2, state.Shots.Count(sh => sh.IsShot && sh.Bull == b)));
        Assert.DoesNotContain(ReviewQueue.For(state, false), i => !i.Resolved && i.Kind is ReviewKind.Contested or ReviewKind.Doubled);

        var reopened = MarkingFile.Read(MarkingFile.Write(state)).State;
        Assert.Equal(state.Rule!.PerBull, reopened.Rule!.PerBull);
        Assert.False(reopened.Rule.NearestOnly);
    }

    [Fact]
    public void ReadByNearestBullTheDoublesAreNotRaised()
    {
        var session = Doubles();
        session.SetAssignmentRule(AssignmentRule.Nearest);
        var state = session.State;
        Assert.All(FirstTen(state), b => Assert.Equal(2, state.Shots.Count(sh => sh.IsShot && sh.Bull == b)));
        Assert.DoesNotContain(ReviewQueue.For(state, false), i => !i.Resolved && i.Kind is ReviewKind.Contested or ReviewKind.Doubled);
        Assert.True(MarkingFile.Read(MarkingFile.Write(state)).State.Rule!.NearestOnly);

        // Taking the rule away matches one a bull again, and the queue raises the pushed shots once more.
        session.SetAssignmentRule(null);
        Assert.True(ReviewQueue.For(session.State, false).Count(i => i.Kind == ReviewKind.Contested && !i.Resolved && i.Sentence.Contains("already holds shot", StringComparison.Ordinal)) >= 10);
    }
}
