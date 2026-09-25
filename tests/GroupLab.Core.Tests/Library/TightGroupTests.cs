using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 196 and 197: a tight group, from renders with synthetic .308 holes, counted exactly. On a sheet with one
/// scoring bull, a five-shot group goes to that bull with nothing to review; a shot far out is still that bull's. A touching pair, a
/// touching pair across a printed ring edge, and three shots through one ragged hole, on a one bull sheet and on the 25 bull sheet.
/// <para>
/// The one bull sheet is the MOA zeroing sheet, because it is the library's only one and a sheet has to encode into its own printed codes
/// to be drawn at all: the 5x5 sheet with one bull left does not. Nothing here is about zeroing grids as such (entry 197 section 2.3);
/// the grid lines are one more printed line for a hole to cross.
/// </para>
/// </summary>
public class TightGroupTests
{
    private const double Dpi = 300;

    public static TheoryData<bool> Sheets() => new() { true, false };

    /// <summary>The 5x5 Letter sheet, or the library's one bull sheet.</summary>
    private static TargetDefinition Sheet(bool oneBull) => BuiltIns.Load(oneBull ? "GL-ZERO-MOA-100Y.gltd.json" : "GL-CF25-LTR.gltd.json");

    /// <summary>The middle scoring bull, and its outer radius in dmm.</summary>
    private static (Bull Aim, double Radius) Aim(TargetDefinition definition)
    {
        var page = definition.Page;
        var aim = definition.Bulls.Where(b => b.Scoring).MinBy(b => Math.Pow(b.X - (page.Width / 2.0), 2) + Math.Pow(b.Y - (page.Height / 2.0), 2))!;
        return (aim, definition.RingSets.Single(r => r.Key == aim.RingSet).Discs.Max(d => d.Diameter) / 2.0);
    }

    /// <summary>The sheet with holes at these offsets from the middle bull, in page dmm, detected with the caliber named; and the review.</summary>
    private static (AutomaticResult Result, IReadOnlyList<ReviewItem> Review) Shoot(TargetDefinition definition, (double X, double Y)[] offsets, int seed, int? roundsFired = null)
    {
        var scene = SceneBuilder.Build(definition);
        Assert.True(scene.Pages.Count > 0, string.Join("; ", scene.Diagnostics.Select(d => d.Message)));
        var render = SceneRasterizer.Rasterize(scene.Pages[0], Dpi);
        var (aim, _) = Aim(definition);
        var random = new Random(seed);
        bool OnInk(double x, double y) => render[(int)(x * Dpi / 254), (int)(y * Dpi / 254)] < 128;
        var holes = offsets.Select(o => SyntheticSheet.SampleHole(random, aim.X + o.X, aim.Y + o.Y, OnInk(aim.X + o.X, aim.Y + o.Y), HoleBacking.ScannerLid, 0.871)).ToList();
        double s = 254 / Dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, Dpi, truth, render.Width, render.Height, holes, [], random);
        var metadata = new ImageMetadata("PNG", image.Width, image.Height, Dpi, Dpi, null, null, null, null, null);
        var result = AutomaticMarking.Run(image, image, metadata, definition, new OpenCvSharpBackend(), calibre: Calibre.Of(0.308));
        Assert.True(result.Failure is null, result.Failure);
        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary);
        if (roundsFired is { } fired)
        {
            session.SetExpectedShots(fired);
        }

        return (result, [.. ReviewQueue.For(session.State).Where(r => !r.Resolved)]);
    }

    private static string Said(IReadOnlyList<ReviewItem> review) => string.Join(" | ", review.Select(r => $"{r.Kind}: {r.Sentence}"));

    private static string Where(AutomaticResult result) => string.Join(" ", result.Detections.Select(d => $"({d.Image.X:0},{d.Image.Y:0}) {d.DiameterInches:0.000}"));

    /// <summary>
    /// One alone, a touching pair, and a touching pair whose join sits on the bull's outer edge: about an inch across. Centres one .308
    /// hole apart, so the rims meet.
    /// </summary>
    private static (double X, double Y)[] FiveShotGroup(double radius) => [(-60, -150), (-150, 40), (-76, 40), (radius - 37, 120), (radius + 37, 120)];

    [Fact]
    public void AFiveShotGroupOnAOneBullSheetIsAllThatBullsWithNothingToReview()
    {
        var definition = Sheet(oneBull: true);
        var (result, review) = Shoot(definition, FiveShotGroup(Aim(definition).Radius), 196);
        Assert.True(result.Detections.Count == 5, $"{result.Detections.Count} of 5 found. {Said(review)}");
        Assert.All(result.Detections, d => Assert.Equal(0, d.Assignment.Bull));
        Assert.True(review.Count == 0, Said(review));
    }

    /// <summary>With the rounds entered as five, still nothing: the count agrees.</summary>
    [Fact]
    public void AFiveShotGroupWithItsRoundsEnteredHasNothingToReview()
    {
        var definition = Sheet(oneBull: true);
        var (result, review) = Shoot(definition, FiveShotGroup(Aim(definition).Radius), 196, roundsFired: 5);
        Assert.Equal(5, result.Detections.Count);
        Assert.True(review.Count == 0, Said(review));
    }

    /// <summary>Section 1.2's gate: on a one bull sheet the matching has none, so a first shot four inches out is still the bull's.</summary>
    [Fact]
    public void AShotFarFromTheOneBullIsStillThatBulls()
    {
        var (result, review) = Shoot(Sheet(oneBull: true), [(-1016, 0)], 1961);
        var shot = Assert.Single(result.Detections);
        Assert.Equal(0, shot.Assignment.Bull);
        Assert.True(review.Count == 0, Said(review));
    }

    /// <summary>
    /// A touching pair: rims meeting, centres 0.29 in apart. Measured over twenty seeds each, it is found as two shots about half the time,
    /// alike across the bull's printed edge (10 and 9 of 20), on paper (12 and 9) and inside the black (9 and 8), so a printed line is not
    /// what makes it hard. Left whole, on a sheet of fewer than five marks nothing flags it (entry 161, question 57). With the rounds
    /// entered, it is either two shots, with nothing to review on the one bull sheet, or one mark the count names first as most likely to be two.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void ATouchingPairIsTwoShotsOrNamedByTheCount(bool oneBull)
    {
        var definition = Sheet(oneBull);
        double radius = Aim(definition).Radius;
        for (int seed = 1; seed <= 6; seed++)
        {
            var (result, review) = Shoot(definition, [(radius - 37, 120), (radius + 37, 120)], seed, roundsFired: 2);
            if (result.Detections.Count == 2 && oneBull)
            {
                Assert.True(result.Detections[0].Assignment.Bull == result.Detections[1].Assignment.Bull, $"seed {seed}: the pair went to two bulls");
                Assert.True(review.Count == 0, $"seed {seed}: {Said(review)}");
            }
            else if (result.Detections.Count != 2)
            {
                // Two in one bull of the 25 is the multi-bull rule's business: one to a bull, and the second one Contested.
                Assert.True(result.Detections.Count == 1, $"seed {seed}: {result.Detections.Count} found at {Where(result)}");
                var count = Assert.Single(review, r => r.Kind == ReviewKind.Count);
                Assert.StartsWith("You fired 2 and 1 is marked.", count.Sentence, StringComparison.Ordinal);
                Assert.Contains("Most likely to be two", count.Sentence, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Section 2.2's ragged hole: three shots through one hole read as one mark of about two holes' area. With only a caliber to judge it,
    /// and so few marks, nothing flags it by entry 161's rule, since a wrong caliber makes every single hole read that large. Told how many
    /// rounds were fired, the review says the count is short and names this mark as the one most likely to hold more than one.
    /// </summary>
    [Theory]
    [MemberData(nameof(Sheets))]
    public void ThreeShotsThroughOneRaggedHoleAreNamedOnceTheRoundsAreKnown(bool oneBull)
    {
        var (result, review) = Shoot(Sheet(oneBull), [(100, 100), (140, 100), (120, 135)], 1962, roundsFired: 3);
        var mark = Assert.Single(result.Detections);
        Assert.NotNull(mark.Assignment.Bull);
        var count = Assert.Single(review);
        Assert.Equal(ReviewKind.Count, count.Kind);
        Assert.StartsWith("You fired 3 and 1 is marked.", count.Sentence, StringComparison.Ordinal);
        Assert.Contains("Most likely to be two", count.Sentence, StringComparison.Ordinal);
    }

    /// <summary>Section 2.3: the oversize sentence says three only from a named caliber, and without one says a caliber is what would tell.</summary>
    [Fact]
    public void ThreeIsSaidOnlyFromANamedCaliber()
    {
        Assert.Contains("It may be three or more", new DetectedOversize(2.9, false, CalibreHoles: 2.9).Describe("1"), StringComparison.Ordinal);
        Assert.DoesNotContain("three", new DetectedOversize(2.1, false, CalibreHoles: 2.1).Describe("1"), StringComparison.Ordinal);
        string without = new DetectedOversize(2.9, false).Describe("1");
        Assert.DoesNotContain("It may be three", without, StringComparison.Ordinal);
        Assert.Contains("Name the caliber and GroupLab can say whether it may be three.", without, StringComparison.Ordinal);
    }
}
