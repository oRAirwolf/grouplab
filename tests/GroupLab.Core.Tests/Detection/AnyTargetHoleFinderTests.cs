using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 318 section 2: the finder for a target GroupLab did not print, on each synthetic kind of target clean, held to
/// stated bounds, and on the one commercial scan whose holes a person checked. The scoreboard holds every kind under every condition to its
/// baseline (<c>AnyTargetScoreboardTests</c>); these say what each kind is expected to do at the least, in words.
/// </summary>
public class AnyTargetHoleFinderTests
{
    private static readonly OpenCvSharpBackend Backend = new();

    /// <summary>
    /// Clean, on both seeds: at least half the 16 holes a sheet found and no more than three false marks; the fluorescent target at least
    /// 12 and none, because a hit's dark centre in its bright ring is the plainest hole there is.
    /// </summary>
    [Theory]
    [InlineData("black bulls", 8, 3)]
    [InlineData("fluorescent", 12, 0)]
    [InlineData("diamonds", 8, 3)]
    [InlineData("grid", 8, 3)]
    public void EachKindOfTargetIsReadWithinItsBounds(string kind, int leastFound, int mostFalse)
    {
        foreach (int seed in Scoreboard.AnyTargetSeeds)
        {
            var (picture, holes) = Scoreboard.AnyTargetPicture(kind, seed);
            var finding = AnyTargetHoleFinder.Find(picture, Scoreboard.AnyTargetDpi, Backend, Scoreboard.AnyTargetCalibre);
            var (found, falseMarks, _) = Scoreboard.Match(holes, [.. finding.Holes.Select(h => h.Image)], Scoreboard.FoundWithinInches * Scoreboard.AnyTargetDpi);
            Assert.True(found >= leastFound, $"{kind}, seed {seed}: {found} of {holes.Count} found, fewer than {leastFound}");
            Assert.True(falseMarks <= mostFalse, $"{kind}, seed {seed}: {falseMarks} false marks, more than {mostFalse}");
        }
    }

    /// <summary>
    /// Printing that looks most like a hole is refused, not proposed: the fluorescent target's bright aim dots, which have no dark centre, and the
    /// diamond target's white diamonds in their black ones, which have straight sides.
    /// </summary>
    [Theory]
    [InlineData("fluorescent")]
    [InlineData("diamonds")]
    public void ThePrintedAimMarkIsNotProposed(string kind)
    {
        (double X, double Y)[] aims = [(1.75, 2.25), (4.75, 2.25), (1.75, 6.25), (4.75, 6.25)];
        var (picture, _) = Scoreboard.AnyTargetPicture(kind, Scoreboard.AnyTargetSeeds[0]);
        var finding = AnyTargetHoleFinder.Find(picture, Scoreboard.AnyTargetDpi, Backend, Scoreboard.AnyTargetCalibre);
        double dpi = Scoreboard.AnyTargetDpi;
        Assert.DoesNotContain(finding.Holes, h => aims.Any(a => Math.Sqrt(Math.Pow((h.Image.X / dpi) - a.X, 2) + Math.Pow((h.Image.Y / dpi) - a.Y, 2)) < 0.1));
    }

    /// <summary>
    /// docs/SCAN-MEASUREMENTS.md section 8: on <c>300_nm_hand_load.jpg</c>, a commercial target scanned at 600 dpi, a person checked 27 holes,
    /// and the survey's detector found 25 of them with no false marks. The finder, which reads the dark holes at 300 dpi with the paper taken
    /// locally as well as once, finds the same 25 and proposes nothing else.
    /// </summary>
    [Fact]
    public void OnTheCommercialScanWithCheckedHolesItFindsWhatTheSurveyFoundAndNothingElse()
    {
        var baseline = JsonNode.Parse(File.ReadAllText(Repo.PathTo("scans", "phase1", "measurements", "holes-baseline.json")))!;
        var row = baseline["rows"]!.AsArray().Single(r => (string)r!["file"]! == "300_nm_hand_load.jpg")!;
        double dpi = (double)row["dpi"]!;
        List<PointD> checkedHoles = [.. row["holes"]!.AsArray().Select(h => new PointD((double)h!["xIn"]! * dpi, (double)h["yIn"]! * dpi)), new(8.02 * dpi, 4.42 * dpi), new(7.98 * dpi, 5.88 * dpi)];
        Assert.Equal(27, checkedHoles.Count);

        var (image, _) = ImageLoader.LoadMaxChannel(Repo.PathTo("scans", "300_nm_hand_load.jpg"));
        var finding = AnyTargetHoleFinder.Find(image, dpi, Backend, 0.308);
        var (found, falseMarks, _) = Scoreboard.Match(checkedHoles, [.. finding.Holes.Select(h => h.Image)], 0.1 * dpi);
        Assert.True(found >= 25, $"{found} of 27 found");
        Assert.Equal(0, falseMarks);
    }

    [Fact]
    public void AResolutionThatIsNotPositiveIsRefused()
    {
        var blank = new GrayImage(10, 10, new byte[100]);
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTargetHoleFinder.Find(blank, 0, Backend));
    }
}
