using GroupLab.Cli;
using GroupLab.Core.Evaluation;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Evaluation;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 308 section 4: a store-bought target's blank and shot scans go on the scoreboard as an "any target" case, read
/// by the detector that needs no printed artwork. Here a made-up orange target stands in for Alan's, which stay on his computer.
/// </summary>
public class AnyTargetScoreboardTests
{
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
