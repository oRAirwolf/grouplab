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
    [Fact]
    public void TheReadmesDownloadLinksAreTheAssetsTheWorkflowAttaches()
    {
        string readme = File.ReadAllText(Repo.PathTo("README.md"));
        string release = File.ReadAllText(Repo.PathTo(".github", "workflows", "release.yml"));
        string package = File.ReadAllText(Repo.PathTo("scripts", "package-windows.ps1"));

        var linked = LatestDownload().Matches(readme).Select(m => m.Groups["asset"].Value).ToList();
        Assert.Equal(["grouplab-setup-win-x64.exe", "grouplab-win-x64.zip", "grouplab-linux-x64.tar.gz"], linked);

        // Every linked asset is one the packaging script or the release workflow writes under that exact name.
        foreach (string asset in linked)
        {
            Assert.True(package.Contains(asset, StringComparison.Ordinal) || release.Contains(asset, StringComparison.Ordinal),
                $"the README links to {asset}, and neither the release workflow nor the packaging script writes a file of that name");
        }

        // The release must attach whatever it built, and a run by hand must make a draft rather than something people can see.
        Assert.Contains("files: release/*", release, StringComparison.Ordinal);
        Assert.Contains("draft: ${{ github.ref_type != 'tag' }}", release, StringComparison.Ordinal);
        Assert.Contains("workflow_dispatch:", release, StringComparison.Ordinal);

        // The releases page, for the older builds, and the honest line about what the build is.
        Assert.Contains("https://github.com/oRAirwolf/grouplab/releases)", readme, StringComparison.Ordinal);
        Assert.Contains("Windows protected your PC", readme, StringComparison.Ordinal);
        Assert.Contains("unsigned", readme, StringComparison.Ordinal);
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

    [GeneratedRegex(@"scans/[A-Za-z0-9._/-]+\.(?:png|jpg|jpeg)")]
    private static partial Regex CopiedImage();
}
