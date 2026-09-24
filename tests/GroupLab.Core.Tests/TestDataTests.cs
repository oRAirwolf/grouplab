using System.Text.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 171 section 6, closing request 8: a sample over about 10 MB is a download on the <c>test-data</c> release,
/// never a file in the repository, because a file committed to git is carried by every clone for ever. Scan 3, at 17.6 MB, was committed
/// before the rule and is the one exception.
/// </summary>
public class TestDataTests
{
    private const long About10MB = 10_000_000;

    private static readonly string[] CommittedBeforeTheRule = ["samples/gl-cf25-ltr-d-25-shots-600-dpi.png"];

    /// <summary>No file in the folders samples and fixtures go in is over about 10 MB, apart from scan 3.</summary>
    [Fact]
    public void NoLargeSampleIsCommitted()
    {
        var large = new List<string>();
        foreach (string folder in new[] { "samples", "tests", "targets", "docs", "website" })
        {
            string root = Repo.PathTo(folder);
            foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(Repo.Root, file).Replace('\\', '/');
                if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal) || rel.StartsWith("website/_site/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (new FileInfo(file).Length > About10MB && !CommittedBeforeTheRule.Contains(rel))
                {
                    large.Add($"{rel}: {new FileInfo(file).Length / 1_000_000.0:0.0} MB");
                }
            }
        }

        Assert.True(large.Count == 0,
            "these belong on the test-data release, listed in tests/test-data.json with their SHA-256, not in the repository: " + string.Join(", ", large));
    }

    /// <summary>
    /// Every file on the release has a hash, a size and a consent record that exists, so nothing is published there that
    /// <c>samples/PROVENANCE.md</c> does not cover.
    /// </summary>
    [Fact]
    public void EveryFileOnTheReleaseHasAHashAndAConsentRecord()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("tests", "test-data.json")));
        Assert.Equal("test-data", doc.RootElement.GetProperty("release").GetString());
        string provenance = File.ReadAllText(Repo.PathTo("samples", "PROVENANCE.md"));
        foreach (var file in doc.RootElement.GetProperty("files").EnumerateArray())
        {
            string name = file.GetProperty("name").GetString()!;
            Assert.Matches("^[0-9a-f]{64}$", file.GetProperty("sha256").GetString()!);
            Assert.True(file.GetProperty("bytes").GetInt64() > 0);
            Assert.StartsWith("samples/PROVENANCE.md", file.GetProperty("consent").GetString(), StringComparison.Ordinal);
            Assert.Contains(file.GetProperty("sha256").GetString()!, provenance, StringComparison.Ordinal);
            Assert.Contains(name, provenance, StringComparison.Ordinal);
        }
    }
}
