using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 327: a store-bought target on the scanner glass is often a crop of it, running off one or more edges, and
    /// nothing may assume the sheet's outline or its corners are in the picture. A crop of each synthetic kind, cut through its top left aim's
    /// printing on the left and top edges, is read like the whole target: every hole well inside the crop that the whole picture's reading
    /// found is found again, and the cut edges, a straight line through black print, add no mark of their own.
    /// </summary>
    [Theory]
    [InlineData("black bulls")]
    [InlineData("diamonds")]
    [InlineData("grid")]
    public void ACropThatRunsOffTwoEdgesIsReadLikeTheWholeTarget(string kind)
    {
        double dpi = Scoreboard.AnyTargetDpi;
        int left = (int)(1.2 * dpi), top = (int)(1.5 * dpi);
        var (picture, holes) = Scoreboard.AnyTargetPicture(kind, Scoreboard.AnyTargetSeeds[0]);
        var crop = Crop(picture, left, top);
        var whole = AnyTargetHoleFinder.Find(picture, dpi, Backend, Scoreboard.AnyTargetCalibre).Holes.Select(h => h.Image).ToList();
        var cropped = AnyTargetHoleFinder.Find(crop, dpi, Backend, Scoreboard.AnyTargetCalibre).Holes.Select(h => new PointD(h.Image.X + left, h.Image.Y + top)).ToList();

        // Holes at least half an inch inside the crop, where the cut is not part of what is around them.
        var inside = holes.Where(h => h.X - left > 0.5 * dpi && h.Y - top > 0.5 * dpi).ToList();
        Assert.NotEmpty(inside);
        double within = Scoreboard.FoundWithinInches * dpi;
        var (foundWhole, _, _) = Scoreboard.Match(inside, whole, within);
        var (foundCrop, _, _) = Scoreboard.Match(inside, cropped, within);
        Assert.True(foundCrop >= foundWhole, $"{kind}: {foundCrop} of {inside.Count} found in the crop, {foundWhole} in the whole picture");

        // A mark near a cut edge that the whole picture did not have is the cut read as a hole.
        var atTheCut = cropped.Where(p => (p.X - left < 0.3 * dpi || p.Y - top < 0.3 * dpi) && !whole.Any(w => Distance(w, p) < within)).ToList();
        Assert.True(atTheCut.Count == 0, $"{kind}: {atTheCut.Count} marks at the cut edges, first at {atTheCut.FirstOrDefault()}");
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 325: on a store-bought target, a black diamond printed in a colored aim disc is solid, dark and of a
    /// hole's size, and was proposed as one, on paper and, on a target that is mostly black, as a dark center in a bright ring. Its straight
    /// sides tell it apart; a round dark spot of the same size and darkness beside it is still proposed.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APrintedDiamondInAColoredAimDiscIsNotProposed(bool mostlyBlack)
    {
        const int dpi = 200;
        const int half = 63;
        using var mat = new Mat(1400, 1100, MatType.CV_8UC1, new Scalar(mostlyBlack ? 20 : 240));
        (int X, int Y)[] diamonds = [(300, 300), (800, 1100)];
        foreach (var (x, y) in diamonds)
        {
            // The disc is red, bright in the brightest channel; the diamond in it is black, 0.63 in across its points, about the size of the
            // one on the store-bought target that showed it.
            Cv2.Circle(mat, new Point(x, y), 120, new Scalar(235), -1, LineTypes.AntiAlias);
            Cv2.FillConvexPoly(mat, new Point[] { new(x, y - half), new(x + half, y), new(x, y + half), new(x - half, y) }, new Scalar(16), LineTypes.AntiAlias);
        }

        Cv2.Circle(mat, new Point(800, 300), 120, new Scalar(235), -1, LineTypes.AntiAlias);
        Cv2.Circle(mat, new Point(800, 300), 30, new Scalar(16), -1, LineTypes.AntiAlias);
        var finding = AnyTargetHoleFinder.Find(OpenCvSharpBackend.Copy(mat), dpi, Backend);
        Assert.DoesNotContain(finding.Holes, h => diamonds.Any(d => Distance(h.Image, new PointD(d.X, d.Y)) < 0.1 * dpi));
        Assert.Contains(finding.Rejected, r => r.Reason.Contains("straight sides", StringComparison.Ordinal));
        Assert.Contains(finding.Holes, h => Distance(h.Image, new PointD(800, 300)) < 0.1 * dpi);
    }

    private static GrayImage Crop(GrayImage image, int left, int top)
    {
        int w = image.Width - left, h = image.Height - top;
        var pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            Array.Copy(image.Pixels, ((y + top) * image.Width) + left, pixels, y * w, w);
        }

        return new GrayImage(w, h, pixels);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    [Fact]
    public void AResolutionThatIsNotPositiveIsRefused()
    {
        var blank = new GrayImage(10, 10, new byte[100]);
        Assert.Throws<ArgumentOutOfRangeException>(() => AnyTargetHoleFinder.Find(blank, 0, Backend));
    }
}
