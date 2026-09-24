using System.Security.Cryptography;
using System.Text.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Analysis;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 172: the ground truth for the 2026-09-20 ST-4 target, the first real material for entry 158's program A.
/// The file is what the entry states and nothing more: twenty groups, seventeen of five and three of ten, 115 shots, and a primer mix that
/// adds to the same 115 but is recorded as unlocated. Where the photographs are on the machine, each is the file that was counted.
/// </summary>
public class St4GroundTruthTests
{
    private static JsonElement Truth()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("tests", "GroupLab.Core.Tests", "Analysis", "st4-2026-09-20.json")));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void TheGroundTruthIsWhatTheEntrySays()
    {
        var truth = Truth();
        var groups = truth.GetProperty("groups").EnumerateArray().ToList();
        Assert.Equal(20, groups.Count);
        Assert.Equal(115, groups.Sum(g => g.GetProperty("shots").GetInt32()));
        Assert.Equal(17, groups.Count(g => g.GetProperty("shots").GetInt32() == 5));
        Assert.Equal(3, groups.Count(g => g.GetProperty("shots").GetInt32() == 10));
        Assert.All(groups, g =>
        {
            Assert.InRange(g.GetProperty("xInches").GetDouble(), -7.5, 7.5);
            Assert.InRange(g.GetProperty("yInches").GetDouble(), -7.5, 7.5);
        });

        var primers = truth.GetProperty("primers");
        Assert.Equal(115, primers.GetProperty("Remington 7 1/2 BR").GetInt32() + primers.GetProperty("CCI BR-4").GetInt32());

        // Every group's close-up is one of the listed frames, so no group points at a photograph nobody counted.
        var frames = truth.GetProperty("frames").EnumerateArray().Select(f => f.GetProperty("file").GetString()).ToHashSet();
        Assert.All(groups, g => Assert.Contains(g.GetProperty("closeUp").GetString(), frames));
    }

    [Fact]
    public void WhereThePhotographsAreHereTheyAreTheOnesCounted()
    {
        var truth = Truth();
        string folder = truth.GetProperty("folder").GetString()!;
        if (!Directory.Exists(folder))
        {
            Assert.True(true, $"skipped: {folder} is not on this machine");
            return;
        }

        foreach (var frame in truth.GetProperty("frames").EnumerateArray().Append(truth.GetProperty("evidence")))
        {
            string path = Path.Combine(folder, frame.GetProperty("file").GetString()!);
            Assert.True(File.Exists(path), $"{path} is missing");
            Assert.Equal(frame.GetProperty("sha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))));
        }
    }
}
