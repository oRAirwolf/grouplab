using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 185: the releases page listed nightly 94 twice, once as its own release and once as the rolling one, with
/// the same title shape and the same notes, and a test data release among the builds.
/// </summary>
public class ReleasesPageTests
{
    private static string Workflow(string name) => File.ReadAllText(Repo.PathTo(".github", "workflows", name)).ReplaceLineEndings("\n");

    /// <summary>
    /// Section 1: the rolling release is needed, because the updater reads its manifest from the fixed address, so it is kept and named for
    /// what it is, with one line pointing at the numbered release and none of the notes or the platform line repeated.
    /// </summary>
    [Fact]
    public void TheRollingReleaseCannotBeMistakenForASecondBuild()
    {
        Assert.Contains("releases/download/nightly/update-manifest.json", File.ReadAllText(Repo.PathTo("src", "GroupLab.Core", "Updates", "UpdateTrain.cs")), StringComparison.Ordinal);

        string nightly = Workflow("nightly.yml");
        int at = nightly.IndexOf("- name: Move the nightly release", StringComparison.Ordinal);
        string step = nightly[at..nightly.IndexOf("- name: ", at + 10, StringComparison.Ordinal)];
        Assert.Contains("--title \"Latest nightly (always the newest build, moves with every build)\"", step, StringComparison.Ordinal);
        Assert.Contains("--notes-file rolling.md", step, StringComparison.Ordinal);
        Assert.DoesNotContain("notes.md \\", step, StringComparison.Ordinal);
        Assert.Contains("releases/tag/v$VERSION", step, StringComparison.Ordinal);

        // The script that rewrites release bodies writes the same one line there, not the notes.
        string rewrite = File.ReadAllText(Repo.PathTo("scripts", "rewrite-release-notes.py"));
        Assert.Contains("Latest nightly (always the newest build, moves with every build)", rewrite, StringComparison.Ordinal);
        Assert.Contains("def rolling_body", rewrite, StringComparison.Ordinal);
    }

    /// <summary>
    /// Section 2: the test data release is a draft, off the page people read for builds. A draft's files are not at the public download
    /// address and only a token that can write contents sees it, so the one job with that token fetches the files through the API and the
    /// test jobs take them as an artifact; nothing puts the files into the repository's history.
    /// </summary>
    [Fact]
    public void TheTestDataReleaseIsADraftTheTestsCanStillRead()
    {
        string ci = Workflow("ci.yml");
        int job = ci.IndexOf("\n  test-data-release:", StringComparison.Ordinal);
        string release = ci[job..ci.IndexOf("\n  windows-package:", job, StringComparison.Ordinal)];
        Assert.Contains("contents: write", release, StringComparison.Ordinal);
        Assert.Contains("gh release create test-data --draft", release, StringComparison.Ordinal);
        Assert.Contains("GH_TOKEN: ${{ github.token }}", release, StringComparison.Ordinal);
        Assert.Contains("python3 scripts/test-data.py fetch", release, StringComparison.Ordinal);
        Assert.Contains("name: test-data", release, StringComparison.Ordinal);

        string test = ci[ci.IndexOf("\n  test:", StringComparison.Ordinal)..job];
        Assert.Contains("needs: test-data-release", test, StringComparison.Ordinal);
        Assert.Contains("actions/download-artifact", test, StringComparison.Ordinal);
        Assert.DoesNotContain("scripts/test-data.py fetch", test, StringComparison.Ordinal);

        string fetch = File.ReadAllText(Repo.PathTo("scripts", "test-data.py"));
        Assert.Contains("api.github.com/repos/", fetch, StringComparison.Ordinal);
        Assert.Contains("application/octet-stream", fetch, StringComparison.Ordinal);
    }
}
