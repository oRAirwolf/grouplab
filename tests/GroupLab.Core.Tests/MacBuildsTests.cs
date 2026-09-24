using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 147: macOS test builds published alongside Windows and Linux, marked honestly.
/// <para>
/// <b>The thing that goes wrong here is not a build failure.</b> It is a Mac build sitting on the download page with nothing saying nobody
/// has ever run it, or an Apple silicon owner taking the Intel one because the page never said which was which. Both look fine in a diff and
/// both waste somebody's evening. So section 5 asks for these, and they are checks on the words rather than on the code.
/// </para>
/// </summary>
public partial class MacBuildsTests
{
    private static string Builder() => File.ReadAllText(Repo.PathTo("website", "build.py"));

    /// <summary>The one source of the statement, entry 147 section 3.2, without the file's own notes about where it is used.</summary>
    private static string Source()
    {
        string text = File.ReadAllText(Repo.PathTo("docs", "PLATFORM-SUPPORT.md")).ReplaceLineEndings("\n");
        int at = text.IndexOf("\n---\n", StringComparison.Ordinal);
        Assert.True(at > 0, "docs/PLATFORM-SUPPORT.md has no rule separating its notes from the statement");
        return text[(at + 5)..].Trim();
    }

    private static string Built()
    {
        string path = Repo.PathTo("website", "_site", "download", "index.html");
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    private static string Workflow(string name) => File.ReadAllText(Repo.PathTo(".github", "workflows", name));

    /// <summary>
    /// Section 5.1: a macOS asset is never published without the untested wording on the download page. The two travel together or neither
    /// goes, because a build nobody has run is only honest while it says so.
    /// </summary>
    [Fact]
    public void AMacBuildIsNeverOfferedWithoutSayingNobodyHasRunIt()
    {
        string page = Builder();
        var assets = MacAsset().Matches(page).Select(m => m.Value).Distinct().ToList();

        if (assets.Count == 0)
        {
            // Nothing to be dishonest about. This is a real pass: withdrawing the builds is a legitimate answer to the rule.
            return;
        }

        // Entry 166 section 4: the Apple silicon build has been run by one tester on one Mac, and the Intel build by nobody. Each card says
        // which, and the Intel one still says untested.
        Assert.Contains("Untested on a real Mac", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Run on one real Mac", page, StringComparison.OrdinalIgnoreCase);

        // Entry 147's own instruction: a test fails if the section's macOS wording, or the sentence saying nobody has run it on a Mac,
        // disappears while a macOS asset is still published. The wording is Alan's and is not to be reworded, so it is checked literally
        // against its one source.
        string source = Source();
        foreach (string sentence in TheStatement)
        {
            Assert.Contains(sentence, source, StringComparison.Ordinal);
        }

        string built = Built();
        if (built.Length > 0)
        {
            foreach (string asset in assets)
            {
                Assert.Contains(asset, built, StringComparison.Ordinal);
            }

            // Once on each card: what has been run on a Mac and what has not. A single note somewhere down the page is not the same as the
            // words beside the button.
            int said = Regex.Matches(built, "Untested on a real Mac|Run on one real Mac", RegexOptions.IgnoreCase).Count;
            Assert.True(said >= assets.Count,
                $"the page offers {assets.Count} Mac builds and says how far each was tested {said} times. It belongs beside each button.");
        }
    }

    /// <summary>
    /// Section 5.2: each build's architecture is named, so an Apple silicon owner does not take the Intel one. Taking the wrong one gives an
    /// application that will not open and no useful message about why, which is the worst kind of first impression.
    /// </summary>
    [Fact]
    public void ThePageSaysWhichMacEachBuildIsFor()
    {
        string built = Built();
        if (built.Length == 0)
        {
            Assert.True(true, "skipped: website/_site has not been built here, so the download page was not read");
            return;
        }

        if (!built.Contains("grouplab-macos-", StringComparison.Ordinal))
        {
            return;
        }

        Assert.Contains("Apple silicon", built, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Intel", built, StringComparison.OrdinalIgnoreCase);

        // And how to find out, because "Apple silicon" means nothing to somebody who has never had to care.
        Assert.Contains("About This Mac", built, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Section 2: the command, exactly, on the download page and in the README. It is the one thing a reader has to type, and a wrong path or
    /// a missing flag turns into "it does not work" with nothing to go on.
    /// </summary>
    [Fact]
    public void TheGatekeeperCommandIsOnThePageAndInTheReadme()
    {
        const string command = "xattr -dr com.apple.quarantine /Applications/GroupLab.app";

        Assert.Contains(command, Builder(), StringComparison.Ordinal);
        Assert.Contains(command, File.ReadAllText(Repo.PathTo("README.md")), StringComparison.Ordinal);

        string built = Built();
        if (built.Length > 0)
        {
            Assert.Contains(command, built, StringComparison.Ordinal);

            // Saying what it does, not just what to type. A command somebody runs without knowing what it does is a habit worth not teaching.
            Assert.Contains("quarantine flag", built, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Section 5.3: a macOS build that fails to package must not take the nightly down with it, and the run must say which one is missing
    /// rather than publishing a shorter list quietly.
    /// </summary>
    [Fact]
    public void AFailedMacPackageDoesNotStopTheNightly()
    {
        string package = Workflow("package.yml");

        // The Mac targets are optional and the Linux one is not. Those two facts together are the whole rule.
        Assert.Contains("optional: true", package, StringComparison.Ordinal);
        Assert.Contains("optional: false", package, StringComparison.Ordinal);
        Assert.Contains("continue-on-error: ${{ matrix.target.optional }}", package, StringComparison.Ordinal);
        Assert.Contains("fail-fast: false", package, StringComparison.Ordinal);

        string nightly = Workflow("nightly.yml");
        foreach (string supported in new[] { "grouplab-setup-win-x64.exe", "grouplab-win-x64.zip", "grouplab-linux-x64.tar.gz" })
        {
            Assert.Contains(supported, nightly, StringComparison.Ordinal);
        }

        // And it says so rather than going quiet.
        Assert.Contains("did not package", nightly, StringComparison.Ordinal);
    }

    /// <summary>
    /// Section 3.1: the build list is one list, so adding a target really is a line. If somebody has to edit three places to add arm64, the
    /// promise on the download page is not true.
    /// </summary>
    [Fact]
    public void TheBuildListIsOneList()
    {
        string package = Workflow("package.yml");
        int at = package.IndexOf("matrix:", StringComparison.Ordinal);
        Assert.True(at > 0, "package.yml has no matrix, so the targets are not in one place");

        string targets = package[at..];
        foreach (string rid in new[] { "linux-x64", "osx-arm64", "osx-x64" })
        {
            Assert.Contains("rid: " + rid, targets, StringComparison.Ordinal);
        }

        // One publish step and one pack step, whatever the target. Three of each would mean three places to edit.
        //
        // The carriage return in those patterns is not decoration: a checkout on Windows has CRLF line endings, so a
        // pattern anchored with $ alone matches nothing there while passing everywhere else. This test went red on the
        // Windows runner and nowhere else, for exactly that reason.
        Assert.Single(Regex.Matches(package, "^[ ]+- name: Publish\\r?$", RegexOptions.Multiline));
        Assert.Single(Regex.Matches(package, "^[ ]+- name: Pack\\r?$", RegexOptions.Multiline));
    }

    /// <summary>
    /// The page promises other Linux targets on request, and says what to tell us. A promise with no way to act on it is worse than no
    /// promise, because somebody takes the trouble to ask and gets nowhere.
    /// </summary>
    [Fact]
    public void ThePromiseOfOtherLinuxTargetsSaysHowToAsk()
    {
        Assert.Contains("Other targets can be added to the nightly builds on request", Source(), StringComparison.Ordinal);
        Assert.Contains("naming the distribution and the architecture", Source(), StringComparison.Ordinal);
        Assert.Contains("support@grouplab.org", Source(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 147 section 3, the sentences that carry it. The whole statement is word for word and not to be reworded; these are the lines
    /// whose loss would change what the page claims, which is what the entry asks to be held.
    /// </summary>
    private static readonly string[] TheStatement =
    [
        "Windows is the supported platform.",
        "the Apple silicon build has been run on one Mac.",
        "The Intel build has never been run on a Mac.",
        "The updater does not install them, and the developer still does not own a Mac.",
        "These builds are an experiment rather than a release.",
        "does not own a Mac, does not intend to buy one, and is not going to pay a yearly fee for a platform they do not own",
        "donate a Mac for testing and cover the developer fees",
        "The hardware to test it exists; the machine to build it does not.",
    ];

    /// <summary>
    /// Entry 147 section 3.2: the same statement on the download page, in the README and on every release carrying a macOS asset, all three
    /// generated from one source.
    /// <para>
    /// <b>Why one source and not three careful copies.</b> A statement this specific, in somebody's settled words, is exactly the thing that
    /// gets reworded in one place and not the others. Then there are two versions of what the project promises about macOS and no way to tell
    /// which is the real one.
    /// </para>
    /// </summary>
    [Fact]
    public void TheStatementComesFromOneSourceAndReachesAllThreePlaces()
    {
        string source = Source();

        // Nothing restates it. The page renders the file, and the README is written from it by a script.
        Assert.Contains("PLATFORM-SUPPORT.md", Builder(), StringComparison.Ordinal);
        Assert.Contains("platform_support()", Builder(), StringComparison.Ordinal);

        string readme = File.ReadAllText(Repo.PathTo("README.md")).ReplaceLineEndings("\n");
        Assert.Contains("<!-- platform-support:", readme, StringComparison.Ordinal);
        Assert.Contains("<!-- end platform-support -->", readme, StringComparison.Ordinal);

        // Entry 168 section 5: a release that carries a Mac build carries one generated line and a link to the download page, not
        // the whole statement, because a published release is frozen and a whole copy on it goes stale the day the statement changes.
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml")).ReplaceLineEndings("\n");
        Assert.Contains("scripts/platform-support.py --release", nightly, StringComparison.Ordinal);
        Assert.Contains("release/grouplab-macos-*.tar.gz", nightly, StringComparison.Ordinal);
        Assert.DoesNotContain("echo \"---\"\n              echo\n              python3 scripts/platform-support.py --release", nightly, StringComparison.Ordinal);
        string script = File.ReadAllText(Repo.PathTo("scripts", "platform-support.py"));
        Assert.Contains("https://grouplab.org/download/", script, StringComparison.Ordinal);
        Assert.DoesNotContain("## What is supported, and what is not\\n\\n", script, StringComparison.Ordinal);

        foreach (string sentence in TheStatement)
        {
            Assert.Contains(sentence, source, StringComparison.Ordinal);
            Assert.Contains(sentence, readme, StringComparison.Ordinal);
        }

        string built = Built();
        if (built.Length > 0 && built.Contains("grouplab-macos-", StringComparison.Ordinal))
        {
            foreach (string sentence in TheStatement)
            {
                Assert.Contains(sentence, built, StringComparison.Ordinal);
            }
        }
    }

    [GeneratedRegex(@"grouplab-macos-[a-z0-9]+\.tar\.gz")]
    private static partial Regex MacAsset();
}
