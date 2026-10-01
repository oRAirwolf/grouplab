using GroupLab.Cli;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Evaluation;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 308 section 4: a store-bought target's blank and shot scans go on the scoreboard as an "any target" case, read
/// by the detector that needs no printed artwork. Here a made-up orange target stands in for Alan's, which stay on his computer. Entry 318
/// section 2: the class's synthetic targets, drawn in code, are held to docs/scoreboard/any-target-baseline.json in every build.
/// </summary>
public class AnyTargetScoreboardTests
{
    private static ScoreboardBaseline Baseline() =>
        Scoreboard.FromJson(File.ReadAllText(Repo.PathTo("docs", "scoreboard", "any-target-baseline.json")));

    [Fact]
    public void TheSyntheticTargetsGroupLabDidNotPrintHoldTheirBaseline()
    {
        var baseline = Baseline();
        var rows = Scoreboard.Rows(Scoreboard.RunAnyTarget(new OpenCvSharpBackend(), baseline.Seeds));
        var drops = Scoreboard.Drops(baseline.Rows, rows, baseline.Margin);
        Assert.True(drops.Count == 0,
            "The any-target scoreboard fell beyond its margin against docs/scoreboard/any-target-baseline.json:\n" + string.Join("\n", drops) + "\n\nThis run:\n" + Scoreboard.Table(rows));
    }

    /// <summary>A kind or a condition added without a baseline line would never be held to anything; each line is its own class, "any target".</summary>
    [Fact]
    public void EveryKindUnderEveryConditionHasABaselineLine()
    {
        var names = Baseline().Rows.Select(r => r.Condition).ToHashSet();
        foreach (string kind in Scoreboard.AnyTargetKinds)
        {
            Assert.All(Scoreboard.AnyTargetConditions, c => Assert.Contains(Scoreboard.AnyTargetLine(kind, c), names));
        }

        Assert.All(names, n => Assert.StartsWith("any target, ", n, StringComparison.Ordinal));
        Assert.All(Scoreboard.AnyTargetConditions, c => Assert.Contains(Scoreboard.Conditions, s => s.Name == c));
    }

    [Fact]
    public void AStoreBoughtTargetIsScoredOnItsShotScanAndItsBlankOne()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-anytarget-{Guid.NewGuid():N}");
        string dir = Directory.CreateDirectory(Path.Combine(folder, "orange-bull")).FullName;
        try
        {
            // A 5 by 5 inch piece of a target at 200 dpi: white paper, an orange bull with rings, then five holes that show the dark backer.
            const int dpi = 200;
            using (var blank = new Mat(1000, 1000, MatType.CV_8UC3, new Scalar(245, 245, 245)))
            {
                Cv2.Circle(blank, new Point(500, 500), 300, new Scalar(40, 120, 245), -1, LineTypes.AntiAlias);
                Cv2.Circle(blank, new Point(500, 500), 200, new Scalar(245, 245, 245), 6, LineTypes.AntiAlias);
                Cv2.Circle(blank, new Point(500, 500), 100, new Scalar(245, 245, 245), 6, LineTypes.AntiAlias);
                Cv2.ImWrite(Path.Combine(dir, "blank.png"), blank);
                foreach (var (x, y) in new[] { (430, 470), (560, 520), (500, 380), (380, 600), (640, 420) })
                {
                    Cv2.Circle(blank, new Point(x, y), 28, new Scalar(35, 35, 35), -1, LineTypes.AntiAlias);
                }

                Cv2.ImWrite(Path.Combine(dir, "shot.png"), blank);
            }

            File.WriteAllText(Path.Combine(dir, "truth.json"), $"{{\"target\":\"any\",\"picture\":\"shot.png\",\"blank\":\"blank.png\",\"count\":5,\"dpi\":{dpi}}}");
            var rows = Scoreboard.Rows(ScoreboardVerb.RunCorpus(folder, TextWriter.Null));
            var shot = rows.Single(r => r.Condition == "any target, shot");
            var empty = rows.Single(r => r.Condition == "any target, blank");
            Assert.Equal(5, shot.Found);
            Assert.Equal(0, shot.FalseMarks);
            Assert.Equal(0, empty.FalseMarks);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entries 325 and 327: Alan's store-bought targets arrive as a folder with blank.png and, once shot, shot.png, at
    /// 600 dpi and with no truth file, and each scan is a crop: the sheet runs off the glass at the top and the left, and the scanner's white lid
    /// shows below the sheet's own bottom edge. The scoreboard reads such a folder as an "any target" case, with nothing found on the blank and
    /// the shot scan's holes proposed, its count unknown until a truth file names it.
    /// </summary>
    [Fact]
    public void AStoreBoughtTargetsFolderWithOnlyItsScansIsReadAsACropOfTheTarget()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-anytarget-{Guid.NewGuid():N}");
        string dir = Directory.CreateDirectory(Path.Combine(folder, "black-bull-crop")).FullName;
        try
        {
            // A 4 by 5 inch scan at 600 dpi: the sheet's paper down to 4 in, its black bull and rings centred off the picture's top left corner,
            // and below the sheet's edge the lid, brighter than the paper, with the edge's faint shadow.
            using (var scan = new Mat(3000, 2400, MatType.CV_8UC3, new Scalar(238, 238, 238)))
            {
                Cv2.Rectangle(scan, new Rect(0, 2400, 2400, 600), new Scalar(254, 254, 254), -1);
                Cv2.Line(scan, new Point(0, 2400), new Point(2400, 2400), new Scalar(200, 200, 200), 3);
                Cv2.Circle(scan, new Point(300, 200), 1300, new Scalar(30, 30, 30), 25, LineTypes.AntiAlias);
                Cv2.Circle(scan, new Point(300, 200), 1000, new Scalar(20, 20, 20), -1, LineTypes.AntiAlias);
                Cv2.Circle(scan, new Point(300, 200), 700, new Scalar(238, 238, 238), 18, LineTypes.AntiAlias);
                Cv2.Circle(scan, new Point(300, 200), 400, new Scalar(238, 238, 238), 18, LineTypes.AntiAlias);
                Cv2.ImWrite(Path.Combine(dir, "blank.png"), scan);

                // Three holes on the paper show the dark backer; two in the black show the lid through them.
                foreach (var (x, y) in new[] { (1800, 900), (1500, 1900), (600, 2000) })
                {
                    Cv2.Circle(scan, new Point(x, y), 90, new Scalar(35, 35, 35), -1, LineTypes.AntiAlias);
                }

                foreach (var (x, y) in new[] { (900, 600), (500, 900) })
                {
                    Cv2.Circle(scan, new Point(x, y), 60, new Scalar(250, 250, 250), -1, LineTypes.AntiAlias);
                }

                Cv2.ImWrite(Path.Combine(dir, "shot.png"), scan);
            }

            var pictures = ScoreboardVerb.RunCorpus(folder, TextWriter.Null);
            var empty = Assert.Single(pictures, p => p.Condition == "any target, blank");
            var shot = Assert.Single(pictures, p => p.Condition == "any target, shot");
            Assert.Equal("black-bull-crop/blank.png", empty.Picture);
            Assert.Equal(0, empty.Marks);
            Assert.Equal("count-unknown", shot.Truth);
            Assert.Equal(5, shot.Marks);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
