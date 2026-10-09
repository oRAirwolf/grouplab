using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLab.Cli;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Evaluation;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 394: the learning loop's scoring, its check against the baseline, its held-out split and its waiting rule,
/// none of which needs a picture, and the driver's copy of the waiting number held to the code's.
/// </summary>
public class LearningLoopTests
{
    private static JsonObject Package(params (double X, double Y, string Change, bool NotAShot)[] marks) => new()
    {
        ["detected"] = new JsonObject { ["marks"] = new JsonArray(new JsonObject(), new JsonObject(), new JsonObject()) },
        ["corrected"] = new JsonObject
        {
            ["marks"] = new JsonArray([.. marks.Select(m => (JsonNode)new JsonObject { ["x"] = m.X, ["y"] = m.Y, ["change"] = m.Change, ["notAShot"] = m.NotAShot })]),
            ["removed"] = new JsonArray(new JsonObject { ["id"] = 9, ["x"] = 1.0, ["y"] = 1.0 }),
        },
    };

    private static RealRow Row(string submission, int found, int falseMarks, int holes = 25, double centre = 0.01) =>
        new(submission, "2026-10", true, "test", "sheet", "photo", 0.264, null, holes, found + falseMarks, found, holes - found, falseMarks, centre, centre * 2, 0.003, true, 0, 0, 0, 0, 0);

    [Fact]
    public void ThePersonsShotsAreTheirFinalMarksLessWhatTheyCalledNotAShot()
    {
        var person = RealScoreboard.Read(Package((10, 20, "kept", false), (30, 40, "moved", false), (50, 60, "added", false), (70, 80, "excluded", true)))!;

        Assert.Equal([new PointD(10, 20), new PointD(30, 40), new PointD(50, 60)], person.Shots);
        Assert.Equal((3, 1, 1, 1, 1, 1), (person.Detected, person.Kept, person.Moved, person.Added, person.Removed, person.NotAShot));
        Assert.Null(RealScoreboard.Read(new JsonObject()));
        Assert.Null(RealScoreboard.Read(null));
    }

    [Fact]
    public void ALineIsWorseOnlyPastTheScoreboardsMargins()
    {
        var baseline = new[] { Row("2026-10-01_aaaaaaaa", 25, 0), Row("2026-10-02_bbbbbbbb", 25, 0), Row("2026-10-03_cccccccc", 20, 1) };
        var tonight = new[] { Row("2026-10-01_aaaaaaaa", 24, 1), Row("2026-10-02_bbbbbbbb", 23, 0), Row("2026-10-03_cccccccc", 20, 3), Row("2026-10-04_dddddddd", 1, 9) };

        var drops = RealScoreboard.Drops(baseline, tonight, new ScoreboardMargin());

        // One hole lost and one false mark gained are within the margin; two lost, two gained are not; a row new tonight has no baseline.
        Assert.Equal(2, drops.Count);
        Assert.Contains(drops, d => d.Contains("2026-10-02_bbbbbbbb", StringComparison.Ordinal) && d.Contains("found 23 of 25, the baseline 25", StringComparison.Ordinal));
        Assert.Contains(drops, d => d.Contains("2026-10-03_cccccccc", StringComparison.Ordinal) && d.Contains("3 false marks, the baseline 1", StringComparison.Ordinal));
    }

    [Fact]
    public void TheBaselineMovesOnlyForTheBetterAndTakesNewRowsAsTheyAre()
    {
        var baseline = new[] { Row("a", 20, 2), Row("b", 25, 0) };
        var tonight = new[] { Row("a", 22, 1), Row("b", 21, 0), Row("c", 18, 0) };

        var next = RealScoreboard.NextBaseline(baseline, tonight).ToDictionary(r => r.Submission);

        Assert.Equal(22, next["a"].Found);
        Assert.Equal(25, next["b"].Found);
        Assert.Equal(18, next["c"].Found);
    }

    [Fact]
    public void TheHeldOutShareIsFixedByNameAndAboutThreeInTen()
    {
        var names = Enumerable.Range(0, 2000).Select(i => $"2026-10-{1 + (i % 28):00}_{i * 2654435761L % 4294967296:x8}").ToList();
        double share = names.Count(RealScoreboard.HeldOut) / (double)names.Count;

        Assert.InRange(share, 0.26, 0.34);
        Assert.All(names, n => Assert.Equal(RealScoreboard.HeldOut(n), RealScoreboard.HeldOut(n)));
    }

    [Fact]
    public void TheSummaryGivesTotalsAndNoSubmission()
    {
        var rows = new[] { Row("2026-10-01_aaaaaaaa", 25, 0), Row("2026-10-02_bbbbbbbb", 20, 1) };

        string summary = RealScoreboard.Summary(rows, [], "v0.2.0-nightly.183", "2026-10-10", "every condition within its margin.");

        Assert.Contains("Found 45 of 50 holes", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("aaaaaaaa", summary, StringComparison.Ordinal);
        Assert.True(summary.Split('\n').Length < 10, "the summary is a few lines");
    }

    [Fact]
    public void TuningWaitsUntilThereAreEnoughCorrectedSubmissions()
    {
        string root = Path.Combine(GroupLab.Tests.Support.TestTempRoot.Folder, "learn-tune-" + Guid.NewGuid().ToString("N"));
        for (int i = 0; i < 3; i++)
        {
            string folder = Path.Combine(root, $"2026-10-0{i + 1}_0000000{i}");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "meta.json"), new JsonObject { ["app"] = Package((1, 1, "kept", false)) }.ToJsonString());
        }

        var output = new StringWriter();
        int code = LearnVerb.Run(["tune", root], output, TextWriter.Null);

        Assert.Equal(0, code);
        var result = JsonNode.Parse(output.ToString())!;
        Assert.True((bool)result["waiting"]!);
        Assert.Equal(3, (int)result["corrected"]!);
        Assert.Equal(RealScoreboard.CorrectedForTuning, (int)result["needed"]!);
    }

    [Fact]
    public void TheDriversWaitingNumberIsTheCodes()
    {
        string script = File.ReadAllText(Repo.PathTo("scripts", "learning", "learn.py"));
        var match = Regex.Match(script, @"^CORRECTED_FOR_TUNING = (\d+)\r?$", RegexOptions.Multiline);

        Assert.True(match.Success, "learn.py names CORRECTED_FOR_TUNING");
        Assert.Equal(RealScoreboard.CorrectedForTuning, int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
    }
}
