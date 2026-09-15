using System.Text.Json.Nodes;
using GroupLab.Core.Publication;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 34 section 2: the owner's own photographs are published scrubbed, with a provenance record that says
/// who took them and on what terms, both hashes and the original name, and without an invented submission record. A held file is
/// recorded and not published, and nothing is written when anything is wrong.
/// </summary>
public sealed class OwnerPublicationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grouplab-owner-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    private string Source()
    {
        string source = Path.Combine(root, "mounted");
        Directory.CreateDirectory(source);
        File.WriteAllBytes(Path.Combine(source, "PXL_20250324_004445754.MP~2.jpg"), PhoneImages.Jpeg());
        File.WriteAllBytes(Path.Combine(source, "signal-2023.jpg"), [.. PhoneImages.Jpeg(), 0x00]);
        return source;
    }

    [Fact]
    public void TheOwnersPhotographsArePublishedScrubbedWithWhoTookThemAndBothHashes()
    {
        string source = Source(), target = Path.Combine(root, "owner");
        var result = Intake.PublishOwner(source, target, "A. Person", "Taken by the copyright holder and published by them under GPL-3.0.", new Dictionary<string, string> { ["signal-2023.jpg"] = "not known to be the owner's photograph" });

        Assert.Null(result.Refused);
        string published = Path.Combine(target, "PXL_20250324_004445754.MP-2.jpg");
        Assert.Empty(PublicationCheck.LocationProblems(File.ReadAllBytes(published)));
        Assert.False(File.Exists(Path.Combine(target, "signal-2023.jpg")));

        var provenance = JsonNode.Parse(File.ReadAllText(Path.Combine(target, PublicationCheck.ProvenanceFile)))!;
        Assert.Equal(Intake.OwnerProvenanceFormat, (string?)provenance["format"]);
        Assert.Equal("A. Person", (string?)provenance["takenBy"]);
        Assert.Null(provenance["submissionId"]);
        var files = provenance["files"]!.AsArray();
        Assert.Equal("PXL_20250324_004445754.MP~2.jpg", (string?)files[0]!["originalName"]);
        Assert.Equal(Intake.Sha256(PhoneImages.Jpeg()), (string?)files[0]!["receivedSha256"]);
        Assert.Equal(Intake.Sha256(File.ReadAllBytes(published)), (string?)files[0]!["publishedSha256"]);
        Assert.Contains("GPS", files[0]!["removed"]!.AsArray().Select(r => (string?)r));
        Assert.Null(files[1]!["publishedSha256"]);
        Assert.Equal("held: not known to be the owner's photograph", (string?)files[1]!["held"]);

        Assert.Contains("already exists", Intake.PublishOwner(source, target, "A. Person", "terms").Refused, StringComparison.Ordinal);
    }

    [Fact]
    public void NothingIsWrittenWithoutWhoTookThemOrForAHoldThatNamesNoFile()
    {
        string source = Source(), target = Path.Combine(root, "owner");
        Assert.NotNull(Intake.PublishOwner(source, target, "", "terms").Refused);
        Assert.NotNull(Intake.PublishOwner(source, target, "A. Person", " ").Refused);
        Assert.Contains("is to be held", Intake.PublishOwner(source, target, "A. Person", "terms", new Dictionary<string, string> { ["missing.jpg"] = "why" }).Refused, StringComparison.Ordinal);
        Assert.False(Directory.Exists(target));
    }
}
