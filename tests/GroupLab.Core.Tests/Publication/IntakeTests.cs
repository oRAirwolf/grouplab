using System.Text.Json.Nodes;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 22 section 2, entry 27 section 1, entry 28 section 1 and entry 37 sections 1 and 2: intake reads the
/// upload page's real <c>meta.json</c>, refuses an opted-out (by either signal), unconsented, unprovenanced or altered submission and an
/// unknown schema, holds any file whose bytes are in a withheld submission, holds back what triage cannot use unless a person accepts
/// it, and publishes scrubbed files with the consent text and both hashes beside them, never over an existing directory. Empty answers,
/// the normal case, are never a reason to refuse.
/// </summary>
public class IntakeTests : IDisposable
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> NoOptOuts = new Dictionary<string, IReadOnlyList<string>>();

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

    /// <summary>A submission beside it with the opt-out file, as the page wrote <c>2026-09-15_eac0bae6</c>.</summary>
    private string WithSentinel(Action<JsonObject>? edit = null)
    {
        string directory = Submission(edit);
        File.WriteAllText(Path.Combine(directory, Intake.DoNotPublish), "The contributor asked that these photos are not published.\nTesting on a private machine only. Do not add to the public data set.\n");
        return directory;
    }

    private static TriageVerdict Triage(string name, byte[] bytes) => name.StartsWith("001", StringComparison.Ordinal)
        ? new TriageVerdict(true, ["36 GroupLab markers decoded"])
        : new TriageVerdict(false, ["no GroupLab markers decoded, so neither scale nor registration is available"]);

    [Fact]
    public void AGoodSubmissionIsPublishedScrubbedWithTheConsentTextAndBothHashesAndWhatTriageCouldNotUseIsHeld()
    {
        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(Submission(), publicRoot, NoOptOuts, Triage);

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

        Assert.Contains("already exists", Intake.Run(Submission(), publicRoot, NoOptOuts, Triage).Refused, StringComparison.Ordinal);
    }

    [Fact]
    public void APersonCanAcceptWhatTriageHeld()
    {
        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(Submission(), publicRoot, NoOptOuts, Triage, accepted: ["002_card.jpg"]);
        Assert.Null(result.Refused);
        Assert.True(File.Exists(Path.Combine(publicRoot, "2026-09-14_1a8f39ad", "002_card.jpg")));
    }

    [Fact]
    public void OptedOutUnconsentedUnprovenancedAlteredOrUnknownSubmissionsAreRefusedAndNothingIsWritten()
    {
        string publicRoot = Path.Combine(root, "donated");
        (string Why, Func<string> Make)[] cases =
        [
            ("exclude_from_public_dataset is true, with no DO-NOT-PUBLISH file", () => Submission(m => m["exclude_from_public_dataset"] = true)),
            ("DO-NOT-PUBLISH file is present although exclude_from_public_dataset is false. The two signals disagree", () => WithSentinel()),
            ("exclude_from_public_dataset is true and a DO-NOT-PUBLISH file is present", () => WithSentinel(m => m["exclude_from_public_dataset"] = true)),
            ("DO-NOT-PUBLISH file is present, and exclude_from_public_dataset is missing", () => WithSentinel(m => m.Remove("exclude_from_public_dataset"))),
            ("opted out: a DO-NOT-PUBLISH file is present", () => WithSentinel(m => m["schema_version"] = 2)),
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
            var result = Intake.Run(make(), publicRoot, NoOptOuts, Triage);
            Assert.Contains(why, result.Refused, StringComparison.Ordinal);
            Assert.False(Directory.Exists(publicRoot) && Directory.EnumerateFileSystemEntries(publicRoot).Any(), $"{why}: something was written");
            Directory.Delete(Path.Combine(root, "incoming"), recursive: true);
        }
    }

    /// <summary>
    /// Entry 37 section 2, as it arrived: the same photograph in a publishable submission and in one withheld by the opt-out file. The
    /// publishable copy is held, even when a person accepts it by name, and its provenance names the withheld submission. A directory
    /// whose consent cannot be read at all withholds its files too.
    /// </summary>
    [Fact]
    public void BytesInAWithheldSubmissionAreHeldFromEverySubmissionAndTheConflictIsRecorded()
    {
        string incoming = Path.Combine(root, "incoming");
        string optedOut = Path.Combine(incoming, "2026-09-15_eac0bae6");
        Directory.CreateDirectory(optedOut);
        File.WriteAllBytes(Path.Combine(optedOut, "003_IMG_1580.jpg"), PhoneImages.Jpeg());
        File.WriteAllText(Path.Combine(optedOut, Intake.DoNotPublish), "Do not add to the public data set.");
        File.WriteAllText(Path.Combine(optedOut, "meta.json"), """{ "schema_version": 1, "submission_id": "eac0bae6", "exclude_from_public_dataset": false }""");
        string unreadable = Path.Combine(incoming, "2026-09-15_unreadable");
        Directory.CreateDirectory(unreadable);
        File.WriteAllBytes(Path.Combine(unreadable, "001_card.jpg"), [.. PhoneImages.Jpeg(), 0x00]);

        string submission = Submission();
        var withheld = Intake.WithheldHashes(incoming);

        Assert.Equal(["eac0bae6"], withheld[Intake.Sha256(PhoneImages.Jpeg())]);
        Assert.Equal(["2026-09-15_unreadable"], withheld[Intake.Sha256([.. PhoneImages.Jpeg(), 0x00])]);
        Assert.DoesNotContain(withheld.Values, ids => ids.Contains("1a8f39ad"));

        string publicRoot = Path.Combine(root, "donated");
        var result = Intake.Run(submission, publicRoot, withheld, Triage, accepted: ["001_20180623_104930.jpg", "002_card.jpg"]);

        Assert.Null(result.Refused);
        string published = Path.Combine(publicRoot, "2026-09-14_1a8f39ad");
        Assert.Equal([PublicationCheck.ProvenanceFile], Directory.EnumerateFiles(published).Select(Path.GetFileName));
        var files = JsonNode.Parse(File.ReadAllText(Path.Combine(published, PublicationCheck.ProvenanceFile)))!["files"]!.AsArray();
        Assert.Contains("consent conflict", (string?)files[0]!["held"], StringComparison.Ordinal);
        Assert.Equal(["eac0bae6"], files[0]!["optedOutIn"]!.AsArray().Select(n => (string?)n));
        Assert.Equal(["2026-09-15_unreadable"], files[1]!["optedOutIn"]!.AsArray().Select(n => (string?)n));
    }
}
