using System.Text.Json.Nodes;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 22 section 2 and entry 27 section 1: intake refuses an opted-out or unprovenanced submission and one
/// whose files do not match their upload hashes, holds back what triage cannot use unless a person accepts it, and publishes scrubbed
/// files with both hashes and the consent record beside them, never over an existing directory.
/// </summary>
public class IntakeTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-intake-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string Submission(Action<JsonObject>? edit = null, bool optOutFile = false)
    {
        string directory = Path.Combine(root, "incoming", "sub-0001");
        Directory.CreateDirectory(directory);
        byte[] sheet = PhoneImages.Jpeg(), card = [.. PhoneImages.Jpeg(), 0x00];
        File.WriteAllBytes(Path.Combine(directory, "001.jpg"), sheet);
        File.WriteAllBytes(Path.Combine(directory, "002.jpg"), card);
        var meta = new JsonObject
        {
            ["submissionId"] = "sub-0001",
            ["consentVersion"] = "2026-09-01",
            ["submittedAt"] = "2026-09-14T18:02:11Z",
            ["answers"] = new JsonObject { ["backing"] = "", ["distance"] = "100 yd" },
            ["files"] = new JsonArray(
                new JsonObject { ["name"] = "001.jpg", ["sha256"] = Intake.Sha256(sheet) },
                new JsonObject { ["name"] = "002.jpg", ["sha256"] = Intake.Sha256(card) }),
        };
        edit?.Invoke(meta);
        File.WriteAllText(Path.Combine(directory, "meta.json"), meta.ToJsonString());
        if (optOutFile)
        {
            File.WriteAllText(Path.Combine(directory, PublicationCheck.DoNotPublish), "");
        }

        return directory;
    }

    private static TriageVerdict Triage(string name, byte[] bytes) => name == "001.jpg"
        ? new TriageVerdict(true, ["36 GroupLab markers decoded"])
        : new TriageVerdict(false, ["no GroupLab markers decoded, so neither scale nor registration is available"]);

    [Fact]
    public void AGoodSubmissionIsPublishedScrubbedWithBothHashesAndWhatTriageCouldNotUseIsHeld()
    {
        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(Submission(), publicRoot, Triage);

        Assert.Null(result.Refused);
        string published = Path.Combine(publicRoot, "sub-0001");
        Assert.True(File.Exists(Path.Combine(published, "001.jpg")));
        Assert.False(File.Exists(Path.Combine(published, "002.jpg")));
        byte[] bytes = File.ReadAllBytes(Path.Combine(published, "001.jpg"));
        Assert.Empty(PublicationCheck.LocationProblems(bytes));

        var provenance = JsonNode.Parse(File.ReadAllText(Path.Combine(published, PublicationCheck.ProvenanceFile)))!;
        Assert.Equal("2026-09-01", (string?)provenance["consentVersion"]);
        Assert.Equal("100 yd", (string?)provenance["answers"]!["distance"]);
        var files = provenance["files"]!.AsArray();
        Assert.Equal(Intake.Sha256(PhoneImages.Jpeg()), (string?)files[0]!["receivedSha256"]);
        Assert.Equal(Intake.Sha256(bytes), (string?)files[0]!["publishedSha256"]);
        Assert.NotEqual((string?)files[0]!["receivedSha256"], (string?)files[0]!["publishedSha256"]);
        Assert.Null((string?)files[1]!["publishedSha256"]);
        Assert.Contains("no GroupLab markers", (string?)files[1]!["held"], StringComparison.Ordinal);

        Assert.Contains("already exists", Intake.Run(Submission(), publicRoot, Triage).Refused, StringComparison.Ordinal);
    }

    [Fact]
    public void APersonCanAcceptWhatTriageHeld()
    {
        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(Submission(), publicRoot, Triage, accepted: ["002.jpg"]);
        Assert.Null(result.Refused);
        Assert.True(File.Exists(Path.Combine(publicRoot, "sub-0001", "002.jpg")));
    }

    [Fact]
    public void OptedOutUnprovenancedOrAlteredSubmissionsAreRefusedAndNothingIsWritten()
    {
        string publicRoot = Path.Combine(root, "donated");
        (string Why, Func<string> Make)[] cases =
        [
            ("opted out", () => Submission(optOutFile: true)),
            ("opted out", () => Submission(m => m["doNotPublish"] = true)),
            ("provenance", () => Submission(m => m.Remove("consentVersion"))),
            ("does not match", () => Submission(m => m["files"]!.AsArray()[0]!["sha256"] = new string('0', 64))),
            ("has no hash", () => Submission(m => m["files"]!.AsArray().RemoveAt(1))),
            ("not a safe directory name", () => Submission(m => m["submissionId"] = "../escape")),
        ];

        foreach (var (why, make) in cases)
        {
            var result = Intake.Run(make(), publicRoot, Triage);
            Assert.Contains(why, result.Refused, StringComparison.Ordinal);
            Assert.False(Directory.Exists(publicRoot) && Directory.EnumerateFileSystemEntries(publicRoot).Any(), $"{why}: something was written");
            Directory.Delete(Path.Combine(root, "incoming"), recursive: true);
        }
    }
}
