using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 136: the release notes page is built from <c>docs/RELEASE-NOTES.md</c>, so that file is the source of truth
/// and the page cannot disagree with it.
/// <para>
/// <b>Section 2.4 asks for the one check that matters</b>: a published build newer than anything in the file means the page would go out
/// already behind, and nobody would notice, because a page that is missing its newest entry looks exactly like a page nobody has updated.
/// The tags in the repository are the evidence, never the network, so this runs the same everywhere and offline.
/// </para>
/// </summary>
public class ReleaseNotesTests
{
    private static string Notes() => File.ReadAllText(Repo.PathTo("docs/RELEASE-NOTES.md"));

    /// <summary>Every version heading in the file, newest first as the file is written.</summary>
    private static List<string> Versions() =>
        [.. Regex.Matches(Notes(), @"^## (.+)$", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Trim())];

    /// <summary>Every v* tag in the repository that names a build, as a version without its leading v.</summary>
    private static List<string> PublishedVersions()
    {
        var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", "tag --list v*")
        {
            WorkingDirectory = Repo.Root,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        })!;
        string tags = git.StandardOutput.ReadToEnd();
        git.WaitForExit(30000);
        return
        [
            .. tags.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.StartsWith('v') && !t.EndsWith("-draft", StringComparison.Ordinal))
                .Select(t => t[1..]),
        ];
    }

    [Fact]
    public void EveryPublishedBuildIsInTheFile()
    {
        var written = Versions().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = PublishedVersions().Where(v => !written.Contains(v)).Order(StringComparer.Ordinal).ToList();

        Assert.True(missing.Count == 0,
            $"docs/RELEASE-NOTES.md has fallen behind: {string.Join(", ", missing)} {(missing.Count == 1 ? "is a build that was published and is" : "are builds that were published and are")} "
            + "not in it. Add them from their Release-note trailers before the site is published.");
    }

    /// <summary>
    /// And nothing is in the file that was never published. A version somebody typed by hand, or a tag that was deleted, would send a reader
    /// to a downloads page that is not there.
    /// </summary>
    [Fact]
    public void NothingIsInTheFileThatWasNeverPublished()
    {
        var published = PublishedVersions().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invented = Versions().Where(v => !published.Contains(v)).ToList();

        Assert.True(invented.Count == 0, $"docs/RELEASE-NOTES.md lists {string.Join(", ", invented)}, which no tag in this repository names");
    }

    /// <summary>The newest build is first, because the page opens its first block and that has to be the one people came for.</summary>
    [Fact]
    public void TheNewestBuildIsFirst()
    {
        var versions = Versions();
        Assert.NotEmpty(versions);

        // Nightly numbers sort by their run, and the file is written newest first.
        var nightlies = versions.Where(v => v.Contains("-nightly.", StringComparison.Ordinal))
            .Select(v => int.Parse(v.Split("-nightly.")[1], System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.Equal(nightlies.OrderByDescending(n => n), nightlies);
    }

    /// <summary>
    /// The anchor the update bar links to is the anchor the page actually carries. The two are worked out in different languages in files
    /// that cannot see each other, so this holds them to the same answer on every real version.
    /// </summary>
    [Fact]
    public void TheApplicationsAnchorsMatchThePages()
    {
        string built = Repo.PathTo("website/_site/releases/index.html");
        if (!File.Exists(built))
        {
            // The site is built by its own job. Where it has not been built here, the anchors still have to be well formed, which is the
            // half of this that does not need the page.
            Assert.All(Versions(), v => Assert.False(string.IsNullOrWhiteSpace(ReleaseNotesPage.Anchor(v)), $"{v} makes no anchor"));
            return;
        }

        string page = File.ReadAllText(built, System.Text.Encoding.UTF8);

        foreach (string version in Versions())
        {
            string anchor = ReleaseNotesPage.Anchor(version);
            Assert.False(string.IsNullOrWhiteSpace(anchor), $"{version} makes no anchor");
            Assert.Contains($"id=\"{anchor}\"", page, StringComparison.Ordinal);
        }
    }

    /// <summary>A version with nothing to say still gets a page, never a dead link.</summary>
    [Fact]
    public void AVersionNobodyHasWrittenAboutStillOpensThePage()
    {
        Assert.Equal(ReleaseNotesPage.Address, ReleaseNotesPage.For(null));
        Assert.Equal(ReleaseNotesPage.Address, ReleaseNotesPage.For("   "));
        Assert.Equal(ReleaseNotesPage.Address + "#9-9-9", ReleaseNotesPage.For("9.9.9"));
    }
}
