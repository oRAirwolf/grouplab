using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Cli.Imaging;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 376 section A2, through the session the desktop and the phone both use: a synthetic C bull sheet, detected
/// as a photo would be, then edited the way Alan edited his on the tablet.
/// </summary>
public class WhichBullsTests
{
    /// <summary>
    /// A rendered GL-CF25-LTR-C with one hole for each bull named, every hole moved by <paramref name="shift"/> (page dmm, right and down),
    /// detected and loaded into a session. The bulls' indexes and page positions come back with it.
    /// </summary>
    private static (MarkingSession Session, List<(int Index, double X, double Y)> Scoring) Sheet(IEnumerable<int> fired, double shiftX, double shiftY)
    {
        var definition = BuiltIns.Load("GL-CF25-LTR-C.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 300);
        var random = new Random(376);
        var scoring = definition.Bulls.Select((b, i) => (Index: i, b.X, b.Y, b.Scoring)).Where(b => b.Scoring).Select(b => (b.Index, X: (double)b.X, Y: (double)b.Y)).ToList();
        var holes = fired.Select(k => SyntheticSheet.SampleHole(random, scoring[k].X + shiftX + random.Next(-40, 41), scoring[k].Y + shiftY + random.Next(-40, 41),
            onInk: false, HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / 300.0;
        var image = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(image, image, new ImageMetadata("PNG", image.Width, image.Height, 300, 300, null, null, null, null, null), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);
        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        return (session, scoring);
    }

    private static List<int> Bulls(MarkingSession session) => [.. session.State.Shots.Where(s => s.IsShot).Select(s => s.Bull ?? -1).Order()];

    /// <summary>
    /// Alan's tablet sheet, entry 376 section A1: bulls 1 to 15, an inch low and half an inch right, bull 10's hole missed by detection and
    /// added by hand. Nightly 171 gave the hand-added sheet to bulls 1, 3 and 6 to 20; it must stay bulls 1 to 15, without asking.
    /// </summary>
    [Fact]
    public void AHoleAddedByHandKeepsTheSheetOnBulls1To15()
    {
        var (session, scoring) = Sheet(Enumerable.Range(0, 15).Where(k => k != 9), 0.46 * 254, 1.0 * 254);
        Assert.Equal(scoring.Take(15).Where((_, k) => k != 9).Select(b => b.Index), Bulls(session));

        var sheet = (SheetReference)session.State.Scale!;
        session.AddShot(sheet.Mapping.ToImage(new PointD(scoring[9].X + (0.9 * 254), scoring[9].Y + (1.05 * 254))));

        Assert.Equal(scoring.Take(15).Select(b => b.Index), Bulls(session));
        Assert.Null(session.WhichBulls());
    }

    /// <summary>
    /// Fifteen shots at bulls 11 to 25, a third of an inch low and right, fit bulls 1 to 15 two rows low just as well: the session asks, its guess is where
    /// they landed, and the answer is kept as the rule "Bulls you fired at" makes.
    /// </summary>
    [Fact]
    public void ASheetThatFitsTwoWaysAsksAndTheAnswerIsKept()
    {
        var (session, scoring) = Sheet(Enumerable.Range(10, 15), 0.35 * 254, 0.35 * 254);
        var question = session.WhichBulls();

        Assert.NotNull(question);
        Assert.True(question.Choices[0].Guess);
        // Detection on the synthetic sheet can miss a hole, so the bulls are checked by where they lie rather than one by one.
        Assert.EndsWith("to 25", question.Choices[0].Bulls, StringComparison.Ordinal);
        var firstRows = scoring.Take(15).Select(b => b.Index).ToHashSet();
        var other = Assert.Single(question.Choices, c => c.Indexes.All(firstRows.Contains));

        session.AnswerWhichBulls(other);
        Assert.Null(session.WhichBulls());
        Assert.All(Bulls(session), b => Assert.Contains(b, other.Indexes));
    }

    /// <summary>
    /// Entry 376 section A1, on the photo itself where it is on this machine (it is kept out of the repository, in
    /// C:\Dev\grouplab-local\tablet-2026-10-05): detection finds bulls 1 to 9 and 11 to 15, and bull 10's hole, torn at the paper's right
    /// edge, added by hand where Alan put it on the tablet, keeps the sheet on bulls 1 to 15.
    /// </summary>
    [Fact]
    public void TheTabletPhotoIsBulls1To15WithTheHandAddedHole()
    {
        const string photo = @"C:\Dev\grouplab-local\tablet-2026-10-05\test-photo-5x5-C-bull-bulls-1-to-15.jpg";
        if (!File.Exists(photo))
        {
            Assert.True(true, "skipped: the tablet photo is not on this machine");
            return;
        }

        var result = GroupLab.Cli.AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(photo), null, out string? failure, [Repo.PathTo("targets")], null);
        Assert.Null(failure);
        var session = new MarkingSession(result!.Marking!);
        var scoring = session.State.Bulls.Where(b => b.Scoring).ToList();
        Assert.Equal(scoring.Take(15).Where((_, k) => k != 9).Select(b => b.Index), Bulls(session));

        var sheet = (SheetReference)session.State.Scale!;
        var ten = scoring[9].Declared!.Value;
        session.AddShot(sheet.Mapping.ToImage(new PointD(ten.X + (1.1 * 254), ten.Y + (1.1 * 254))));
        Assert.Equal(scoring.Take(15).Select(b => b.Index), Bulls(session));
        Assert.Null(session.WhichBulls());
    }

    [Theory]
    [InlineData(new[] { "4" }, "bull 4")]
    [InlineData(new[] { "1", "2", "3" }, "bulls 1 to 3")]
    [InlineData(new[] { "1", "2", "4", "5", "6", "9" }, "bulls 1, 2, 4 to 6 and 9")]
    public void BullsAreSaidAsRuns(string[] labels, string words) => Assert.Equal(words, AimedBulls.Words(labels));
}
