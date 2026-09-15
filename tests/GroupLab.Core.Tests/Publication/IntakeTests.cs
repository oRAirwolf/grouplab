using System.Text.Json.Nodes;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 22 section 2, entry 27 section 1 and entry 28 section 1: intake reads the upload page's real
/// <c>meta.json</c>, refuses an opted-out, unconsented, unprovenanced or altered submission and an unknown schema, holds back what
/// triage cannot use unless a person accepts it, and publishes scrubbed files with the consent text and both hashes beside them,
/// never over an existing directory. Empty answers, the normal case, are never a reason to refuse.
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

    /// <summary>A submission shaped exactly as entry 28 section 1 records the first real one, with every answer an empty string.</summary>
    private string Submission(Action<JsonObject>? edit = null)
    {
        string directory = Path.Combine(root, "incoming", "1a8f39ad");
        Directory.CreateDirectory(directory);
        byte[] sheet = PhoneImages.Jpeg(), card = [.. PhoneImages.Jpeg(), 0x00];
        File.WriteAllBytes(Path.Combine(directory, "001_20180623_104930.jpg"), sheet);
        File.WriteAllBytes(Path.Combine(directory, "002_card.jpg"), card);
        JsonObject FileEntry(int index, string stored, string original, byte[] bytes) => new()
        {
            ["index"] = index,
            ["stored_name"] = stored,
            ["original_name"] = original,
            ["bytes"] = bytes.Length,
            ["sniffed_type"] = "image/jpeg",
            ["sha256"] = Intake.Sha256(bytes),
        };
        var meta = new JsonObject
        {
            ["schema_version"] = 1,
            ["submission_id"] = "1a8f39ad",
            ["submitted_utc"] = "2026-09-14T20:41:55Z",
            ["exclude_from_public_dataset"] = false,
            ["consent"] = new JsonObject
            {
                ["agreed"] = true,
                ["version"] = "consent_v1",
                ["agreed_at_utc"] = "2026-09-14T20:41:55Z",
                ["text"] = "I took these photos, or I have permission to share them.",
            },
            ["answers"] = new JsonObject { ["target_backing"] = "", ["attachment_method"] = "", ["shot_distance"] = "", ["caliber"] = "", ["notes"] = "", ["credit_name"] = "" },
            ["user_agent"] = "Mozilla/5.0",
            ["files"] = new JsonArray(FileEntry(1, "001_20180623_104930.jpg", "20180623_104930.jpg", sheet), FileEntry(2, "002_card.jpg", "card.jpg", card)),
        };
        edit?.Invoke(meta);
        File.WriteAllText(Path.Combine(directory, "meta.json"), meta.ToJsonString());
        return directory;
    }

    private static TriageVerdict Triage(string name, byte[] bytes) => name.StartsWith("001", StringComparison.Ordinal)
        ? new TriageVerdict(true, ["36 GroupLab markers decoded"])
        : new TriageVerdict(false, ["no GroupLab markers decoded, so neither scale nor registration is available"]);

    [Fact]
    public void AGoodSubmissionIsPublishedScrubbedWithTheConsentTextAndBothHashesAndWhatTriageCouldNotUseIsHeld()
    {
        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(Submission(), publicRoot, Triage);

        Assert.Null(result.Refused);
        string published = Path.Combine(publicRoot, "2026-09-14_1a8f39ad");
        Assert.True(File.Exists(Path.Combine(published, "001_20180623_104930.jpg")));
        Assert.False(File.Exists(Path.Combine(published, "002_card.jpg")));
        byte[] bytes = File.ReadAllBytes(Path.Combine(published, "001_20180623_104930.jpg"));
        Assert.Empty(PublicationCheck.LocationProblems(bytes));

        var provenance = JsonNode.Parse(File.ReadAllText(Path.Combine(published, PublicationCheck.ProvenanceFile)))!;
        Assert.Equal("consent_v1", (string?)provenance["consent"]!["version"]);
        Assert.Equal("I took these photos, or I have permission to share them.", (string?)provenance["consent"]!["text"]);
        Assert.False((bool)provenance["excludeFromPublicDataset"]!);
        Assert.Equal("", (string?)provenance["answers"]!["caliber"]);
        var files = provenance["files"]!.AsArray();
        Assert.Equal("20180623_104930.jpg", (string?)files[0]!["originalName"]);
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
        var result = Intake.Run(Submission(), publicRoot, Triage, accepted: ["002_card.jpg"]);
        Assert.Null(result.Refused);
        Assert.True(File.Exists(Path.Combine(publicRoot, "2026-09-14_1a8f39ad", "002_card.jpg")));
    }

    [Fact]
    public void OptedOutUnconsentedUnprovenancedAlteredOrUnknownSubmissionsAreRefusedAndNothingIsWritten()
    {
        string publicRoot = Path.Combine(root, "donated");
        (string Why, Func<string> Make)[] cases =
        [
            ("opted out", () => Submission(m => m["exclude_from_public_dataset"] = true)),
            ("unknown and not false", () => Submission(m => m.Remove("exclude_from_public_dataset"))),
            ("schema_version", () => Submission(m => m["schema_version"] = 2)),
            ("agreed", () => Submission(m => m["consent"]!["agreed"] = false)),
            ("its text", () => Submission(m => m["consent"]!.AsObject().Remove("text"))),
            ("submission_id", () => Submission(m => m.Remove("submission_id"))),
            ("does not match", () => Submission(m => m["files"]!.AsArray()[0]!["sha256"] = new string('0', 64))),
            ("bytes, where meta.json recorded", () => Submission(m => m["files"]!.AsArray()[0]!["bytes"] = 12)),
            ("not in meta.json", () => Submission(m => m["files"]!.AsArray().RemoveAt(1))),
            ("not a safe file name", () => Submission(m => m["files"]!.AsArray()[0]!["stored_name"] = "../escape.jpg")),
            ("not a safe directory name", () => Submission(m => m["submission_id"] = "../escape")),
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
