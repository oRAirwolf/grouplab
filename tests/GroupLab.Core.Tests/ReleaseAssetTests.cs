using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 116 section 3: the README's Download links point at the newest release's assets by a stable name, so they
/// keep working without anyone editing the README after a release. Renaming an asset in the workflow, or in the packaging script, must fail
/// here rather than leave a dead link on the front page.
/// </summary>
public partial class ReleaseAssetTests
{
    /// <summary>The three stable names both workflows publish, and the README links to twice.</summary>
    private static readonly string[] Stable = ["grouplab-setup-win-x64.exe", "grouplab-win-x64.zip", "grouplab-linux-x64.tar.gz"];

    [Fact]
    public void TheReadmesDownloadLinksAreTheAssetsTheWorkflowAttaches()
    {
        string readme = File.ReadAllText(Repo.PathTo("README.md"));
        string release = File.ReadAllText(Repo.PathTo(".github", "workflows", "release.yml"));
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml"));
        string package = File.ReadAllText(Repo.PathTo("scripts", "package-windows.ps1"));
        string packaging = File.ReadAllText(Repo.PathTo(".github", "workflows", "package.yml"));

        // Entry 119 section 7 and entry 121 section 2.4: one table, the latest build, and nothing pointing at a numbered release until Alan
        // asks for one. v0.1.0 exists and is deliberately outside every train, so a link to it would offer older code than any nightly.
        Assert.Equal(Stable, NightlyDownload().Matches(readme).Select(m => m.Groups["asset"].Value));
        Assert.DoesNotContain("releases/latest", readme, StringComparison.Ordinal);
        Assert.DoesNotContain("v0.1.0", readme, StringComparison.Ordinal);
        Assert.Equal(1, DownloadTableRow().Matches(readme).Count / 3);

        // Entry 128 section 1.2 found this held for the README alone. The guides ship in the package and are published on the website, and
        // docs/TESTING-GUIDE.md had been sending testers to releases/latest, which returns nothing because no numbered release exists.
        foreach (string doc in new[] { "USER-GUIDE.md", "TESTING-GUIDE.md" })
        {
            string text = File.ReadAllText(Repo.PathTo("docs", doc));
            Assert.DoesNotContain("releases/latest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("releases/tag/v0.1.0", text, StringComparison.Ordinal);
        }

        // Every linked asset is one the packaging script or a workflow writes under that exact name.
        foreach (string asset in Stable)
        {
            Assert.True(package.Contains(asset, StringComparison.Ordinal) || packaging.Contains(asset, StringComparison.Ordinal) || nightly.Contains(asset, StringComparison.Ordinal),
                $"the README links to {asset}, and no workflow or script writes a file of that name");
        }

        // The release must attach whatever it built, and a run by hand must make a draft rather than something people can see.
        Assert.Contains("files: release/*", release, StringComparison.Ordinal);
        Assert.Contains("draft: ${{ github.ref_type != 'tag' }}", release, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", release, StringComparison.Ordinal);

        // The honest lines about what the build is.
        Assert.Contains("Windows protected your PC", readme, StringComparison.Ordinal);
        Assert.Contains("unsigned", readme, StringComparison.Ordinal);
        Assert.Contains("may be broken", readme, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 119 section 2: the nightly publishes only what passed, builds the commit that passed rather than the
    /// branch head, stays a pre-release so releases/latest is left for a deliberate release, keeps its packaging in the shared workflow, and
    /// refuses to publish anything it cannot sign.
    /// </summary>
    [Fact]
    public void TheNightlyPublishesOnlyWhatPassedAndOnlyWhatItCanSign()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml"));
        string release = File.ReadAllText(Repo.PathTo(".github", "workflows", "release.yml"));
        string ci = File.ReadAllText(Repo.PathTo(".github", "workflows", "ci.yml"));

        // It follows the workflow that tests, by that workflow's own name, and only where it succeeded.
        var name = WorkflowName().Match(ci);
        Assert.True(name.Success, "ci.yml has no name, and the nightly follows it by name");
        Assert.Contains($"workflows: [\"{name.Groups["name"].Value}\"]", nightly, StringComparison.Ordinal);
        Assert.Contains("github.event.workflow_run.conclusion == 'success'", nightly, StringComparison.Ordinal);
        Assert.Contains("branches: [phase-1, main]", nightly, StringComparison.Ordinal);

        // It builds the commit that was tested, never a branch head that may have moved.
        Assert.Contains("ref: ${{ github.event.workflow_run.head_sha }}", nightly, StringComparison.Ordinal);
        Assert.DoesNotContain("ref: ${{ github.event.workflow_run.head_branch }}", nightly, StringComparison.Ordinal);

        // Pre-releases under a fixed rolling tag and a versioned one, so the addresses never change and releases/latest is left alone.
        Assert.Contains("--prerelease", nightly, StringComparison.Ordinal);
        Assert.Contains("gh release create nightly", nightly, StringComparison.Ordinal);
        Assert.Contains("gh release create \"v$VERSION\"", nightly, StringComparison.Ordinal);
        Assert.Contains("group: nightly", nightly, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", nightly, StringComparison.Ordinal);

        // No key, no publishing, and it says so before it builds anything.
        Assert.Contains(GroupLab.Core.Updates.UpdateKeys.SecretName, nightly, StringComparison.Ordinal);
        Assert.Contains("update-manifest", nightly, StringComparison.Ordinal);
        Assert.Contains("update-check", nightly, StringComparison.Ordinal);

        // It keeps the newest thirty and touches nothing it did not make.
        Assert.Contains("nightlies[30:]", nightly, StringComparison.Ordinal);
        Assert.Contains(@"^v\d+\.\d+\.\d+-nightly\.\d+$", nightly, StringComparison.Ordinal);

        // Both call the same packaging workflow, which is what stops a release and a nightly drifting apart.
        Assert.Contains("uses: ./.github/workflows/package.yml", nightly, StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/package.yml", release, StringComparison.Ordinal);

        // Only the nightly may tag, and it tags only its own pre-releases (entry 119 section 8).
        Assert.DoesNotContain("git tag", nightly, StringComparison.Ordinal);
    }

    /// <summary>The package carries the licence, the notices, a read me and the samples, because a loose executable loses all of them.</summary>
    [Fact]
    public void ThePackageCarriesItsLicenceItsNoticesAndItsSamples()
    {
        string package = File.ReadAllText(Repo.PathTo("scripts", "package-windows.ps1"));
        foreach (string required in new[] { "LICENSE", "THIRD-PARTY-NOTICES.md", "README.txt", "samples/gl-cf25-ltr-d-25-shots-600-dpi.png", "samples/PROVENANCE.md", "OpenCvSharpExtern.dll", "hostfxr.dll" })
        {
            Assert.Contains(required, package, StringComparison.Ordinal);
        }

        // Nothing donated goes in it. Both images copied from the repository are Alan's own, and the shot one carries a consent record
        // beside it (entry 120 section 9). A third path into the repository's images here means something has gone in without one.
        var copied = CopiedImage().Matches(package).Select(m => m.Groups["image"].Value).ToList();
        Assert.Equal(["samples/gl-cf25-ltr-d-25-shots-600-dpi.png", "scans/phase0/gl-cf25-ltr-1-300-dpi.png"], copied.Order(StringComparer.Ordinal));

        // The consent record travels with the sample, in the package and in the repository, and names what was agreed.
        string provenance = File.ReadAllText(Repo.PathTo("samples", "PROVENANCE.md"));
        Assert.Contains("gl-cf25-ltr-d-25-shots-600-dpi.png", provenance, StringComparison.Ordinal);
        Assert.Contains("No consent record, no publication", provenance, StringComparison.Ordinal);
        Assert.Contains("93140a6a37777667", provenance, StringComparison.Ordinal);

        // The package must read the sample as the shooter says it is, or fail rather than reach a tester.
        Assert.Contains("\"shots\": 25", File.ReadAllText(Repo.PathTo("samples", "sample.json")), StringComparison.Ordinal);
        Assert.Contains("shots pooled about their own bulls", package, StringComparison.Ordinal);

        string readme = File.ReadAllText(Repo.PathTo("packaging", "windows", "README.txt.template"));
        foreach (string said in new[] { "SmartScreen", "antivirus", "%APPDATA%\\GroupLab", "Report a problem", "WHAT IS NOT DONE YET", "General Public License" })
        {
            Assert.Contains(said, readme, StringComparison.Ordinal);
        }

        Assert.Contains("{version}", readme, StringComparison.Ordinal);
        Assert.Contains("{commit}", readme, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"releases/latest/download/(?<asset>[A-Za-z0-9._-]+)")]
    private static partial Regex LatestDownload();

    [GeneratedRegex(@"releases/download/nightly/(?<asset>[A-Za-z0-9._-]+)")]
    private static partial Regex NightlyDownload();

    [GeneratedRegex(@"^\| \*\*\[[^\]]+\]\(https://github\.com/oRAirwolf/grouplab/releases/", RegexOptions.Multiline)]
    private static partial Regex DownloadTableRow();

    // [^\r\n] rather than . : a checkout on Windows has CRLF line endings, and a captured carriage return made this test look for a
    // workflow name with one in it, which failed on the Windows runner alone (NOTES-FROM-PLANNING.md entry 121 section 3).
    [GeneratedRegex(@"^name: (?<name>[^\r\n]+)", RegexOptions.Multiline)]
    private static partial Regex WorkflowName();

    [GeneratedRegex(@"Copy-Item '(?<image>[A-Za-z0-9._/-]+\.(?:png|jpg|jpeg))'")]
    private static partial Regex CopiedImage();
}
