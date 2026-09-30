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
}
