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
        string testBuild = File.ReadAllText(Repo.PathTo(".github", "workflows", "test-build.yml"));
        string package = File.ReadAllText(Repo.PathTo("scripts", "package-windows.ps1"));
        string packaging = File.ReadAllText(Repo.PathTo(".github", "workflows", "package.yml"));

        // Entry 119 section 2: two tables, the test build first and the numbered release after it, each linking all three stable names.
        Assert.Equal(Stable, TestBuildDownload().Matches(readme).Select(m => m.Groups["asset"].Value));
        Assert.Equal(Stable, LatestDownload().Matches(readme).Select(m => m.Groups["asset"].Value));
        Assert.True(readme.IndexOf("releases/download/test-build/", StringComparison.Ordinal) < readme.IndexOf("releases/latest/download/", StringComparison.Ordinal),
            "the test build table goes first: it is the one a tester should use, and the numbered release changes only when one is made");

        // Every linked asset is one the packaging script or a workflow writes under that exact name.
        foreach (string asset in Stable)
        {
            Assert.True(package.Contains(asset, StringComparison.Ordinal) || packaging.Contains(asset, StringComparison.Ordinal),
                $"the README links to {asset}, and neither the packaging workflow nor the packaging script writes a file of that name");
        }

        // The release must attach whatever it built, and a run by hand must make a draft rather than something people can see.
        Assert.Contains("files: release/*", release, StringComparison.Ordinal);
        Assert.Contains("draft: ${{ github.ref_type != 'tag' }}", release, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", release, StringComparison.Ordinal);

        // The releases page, for the older builds, and the honest line about what the build is.
        Assert.Contains("https://github.com/oRAirwolf/grouplab/releases)", readme, StringComparison.Ordinal);
        Assert.Contains("Windows protected your PC", readme, StringComparison.Ordinal);
        Assert.Contains("unsigned", readme, StringComparison.Ordinal);
        Assert.Contains("untested by hand", readme, StringComparison.Ordinal);
        Assert.Contains("untested by hand", testBuild, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 119 section 1: the rolling test build publishes only what passed, builds the commit that passed rather
    /// than the branch head, stays a pre-release so releases/latest keeps pointing at the newest numbered version, and shares its packaging
    /// steps with the release rather than carrying a copy of them.
    /// </summary>
    [Fact]
    public void TheTestBuildPublishesOnlyWhatPassedAndNeverBecomesTheLatestRelease()
    {
        string testBuild = File.ReadAllText(Repo.PathTo(".github", "workflows", "test-build.yml"));
        string release = File.ReadAllText(Repo.PathTo(".github", "workflows", "release.yml"));
        string ci = File.ReadAllText(Repo.PathTo(".github", "workflows", "ci.yml"));

        // It follows the workflow that tests, by that workflow's own name, and only where it succeeded.
        var name = WorkflowName().Match(ci);
        Assert.True(name.Success, "ci.yml has no name, and the test build follows it by name");
        Assert.Contains($"workflows: [\"{name.Groups["name"].Value}\"]", testBuild, StringComparison.Ordinal);
        Assert.Contains("github.event.workflow_run.conclusion == 'success'", testBuild, StringComparison.Ordinal);
        Assert.Contains("branches: [phase-1, main]", testBuild, StringComparison.Ordinal);

        // It builds the commit that was tested, never a branch head that may have moved.
        Assert.Contains("ref: ${{ github.event.workflow_run.head_sha }}", testBuild, StringComparison.Ordinal);
        Assert.DoesNotContain("ref: ${{ github.event.workflow_run.head_branch }}", testBuild, StringComparison.Ordinal);

        // A pre-release under one fixed tag, so the fixed address never changes and releases/latest is left alone.
        Assert.Contains("--prerelease", testBuild, StringComparison.Ordinal);
        Assert.Contains("gh release create test-build", testBuild, StringComparison.Ordinal);
        Assert.Contains("--target \"$SHA\"", testBuild, StringComparison.Ordinal);
        Assert.Contains("group: test-build", testBuild, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", testBuild, StringComparison.Ordinal);

        // Both call the same packaging workflow, which is what stops a release and a test build drifting apart.
        Assert.Contains("uses: ./.github/workflows/package.yml", testBuild, StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/package.yml", release, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 119 section 1 item 3: the steps that build a package live in one file. A second copy of the publish or the tarball would be the
    /// drift the reusable workflow exists to prevent, so nothing but package.yml may run the packaging script or pack the tarball.
    /// </summary>
    [Fact]
    public void OnlyOneWorkflowBuildsTheDownloads()
    {
        var builders = new List<string>();
        foreach (string path in Directory.GetFiles(Repo.PathTo(".github", "workflows"), "*.yml"))
        {
            string text = File.ReadAllText(path);
            // What builds a download: running the packaging script, or packing the tarball. Naming an asset in prose is not building one.
            if (text.Contains("package-windows.ps1", StringComparison.Ordinal) || text.Contains("tar -czf", StringComparison.Ordinal))
            {
                builders.Add(Path.GetFileName(path));
            }
        }

        // ci.yml builds a package on every push as a check that packaging still works; it publishes nothing. package.yml is the one the
        // downloads come from. A third file here means somebody has copied the steps again.
        Assert.Equal(["ci.yml", "package.yml"], builders.Order(StringComparer.Ordinal));
    }

    /// <summary>The package carries the licence, the notices, a read me and the samples, because a loose executable loses all of them.</summary>
    [Fact]
    public void ThePackageCarriesItsLicenceItsNoticesAndItsSamples()
    {
        string package = File.ReadAllText(Repo.PathTo("scripts", "package-windows.ps1"));
        foreach (string required in new[] { "LICENSE", "THIRD-PARTY-NOTICES.md", "README.txt", "samples/sample-25-shots.png", "OpenCvSharpExtern.dll", "hostfxr.dll" })
        {
            Assert.Contains(required, package, StringComparison.Ordinal);
        }

        // Nothing donated goes in it: the only image copied from the repository is Alan's own unshot sheet, and the shot one is generated.
        var copied = CopiedImage().Matches(package).Select(m => m.Value).ToList();
        Assert.Equal(["scans/phase0/gl-cf25-ltr-1-300-dpi.png"], copied);

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

    [GeneratedRegex(@"releases/download/test-build/(?<asset>[A-Za-z0-9._-]+)")]
    private static partial Regex TestBuildDownload();

    [GeneratedRegex(@"^name: (?<name>.+)$", RegexOptions.Multiline)]
    private static partial Regex WorkflowName();

    [GeneratedRegex(@"scans/[A-Za-z0-9._/-]+\.(?:png|jpg|jpeg)")]
    private static partial Regex CopiedImage();
}
