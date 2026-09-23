using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 144, which replaced entry 128 section 6: the site publishes itself when its content changes, and what is
/// visible is controlled by the content rather than by the pipeline.
/// <para>
/// <b>What this still pins, and why it is not weaker than the rule it replaced.</b> The old test said the only trigger was a person. The
/// new one says the only triggers are a person and a change to something the site is built from, which is the same guarantee turned the
/// right way round: an ordinary code commit must not republish the website, and nothing but this workflow may write the site release.
/// </para>
/// <para>
/// The nightly's "keep the newest thirty" step deletes releases by pattern, so a pattern that ever matched <c>site</c> would delete the
/// website without anyone noticing until the server pulled nothing. That half of the test is untouched.
/// </para>
/// </summary>
public partial class WebsiteWorkflowTests
{
    private static string Website => File.ReadAllText(Repo.PathTo(".github", "workflows", "website.yml"));

    private static IEnumerable<string> OtherWorkflows() =>
        Directory.EnumerateFiles(Repo.PathTo(".github", "workflows"), "*.yml")
            .Where(f => Path.GetFileName(f) != "website.yml");

    /// <summary>
    /// The only two triggers the website workflow may have: a person, and a change to the content it publishes. Entry 144 section 1.1.
    /// </summary>
    [Fact]
    public void TheWebsiteIsPublishedByAPersonOrByItsOwnContentChanging()
    {
        var triggers = Triggers(Website);

        Assert.Equal(["push", "workflow_dispatch"], triggers.Order(StringComparer.Ordinal));

        // A push publishes only from main, and only when something the site is built from has changed. Without the paths filter every
        // commit would republish the website, which is the thing entry 144 was careful to keep.
        Assert.Contains("branches: [main]", Website, StringComparison.Ordinal);
        Assert.Contains("paths:", Website, StringComparison.Ordinal);
        foreach (string path in new[] { "'website/**'", "'docs/RELEASE-NOTES.md'", "'targets/**'" })
        {
            Assert.Contains(path, Website, StringComparison.Ordinal);
        }

        // And it still asks a person why, when a person is the one asking.
        Assert.Contains("reason:", Website, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 144 section 1.2: the site's gate is its own. The whole point of publishing on a push is that it does not wait half an hour for
    /// a Windows test run, so a dependency on the other workflow would quietly undo the change.
    /// </summary>
    [Fact]
    public void TheWebsiteDoesNotWaitOnTheOtherWorkflow()
    {
        Assert.DoesNotContain("workflow_run", Website, StringComparison.Ordinal);
        Assert.DoesNotContain("needs:", Website, StringComparison.Ordinal);

        // It runs the checks that can tell whether a page is wrong, rather than none at all.
        foreach (string test in new[] { "ResearchArticleTests", "ReleaseNotesTests", "NoPissinhotOnTheSiteTests" })
        {
            Assert.Contains(test, Website, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Entry 144 section 1.3: a burst of commits publishes once. Queueing one publish per commit would mean the server pulling several
    /// parcels in a row, each already out of date when it arrived.
    /// </summary>
    [Fact]
    public void ABurstOfCommitsPublishesOnce()
    {
        Assert.Contains("group: website", Website, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", Website, StringComparison.Ordinal);
    }

    /// <summary>Nothing else may start it, which is the other half of the same rule.</summary>
    [Fact]
    public void NoOtherWorkflowStartsTheWebsiteWorkflow()
    {
        var found = new List<string>();
        foreach (string file in OtherWorkflows())
        {
            // Naming it is not starting it: ci.yml's comment says what publishes the site, which is the opposite of publishing it. What
            // counts is calling it, dispatching it, or triggering on it.
            foreach (Match m in StartsTheWebsite().Matches(File.ReadAllText(file)))
            {
                found.Add($"{Path.GetFileName(file)}: {m.Value.ReplaceLineEndings(" ")}");
            }
        }

        Assert.True(found.Count == 0,
            "only a person may publish the website, so no other workflow may start it: " + string.Join(", ", found));
    }

    /// <summary>
    /// Nothing else may write to the <c>site</c> release either. Entry 128 section 3.6 asks specifically about the nightly's cleanup step,
    /// and the answer is in the shape of the pattern it matches: a version tag, which <c>site</c> is not.
    /// </summary>
    [Fact]
    public void NothingElseTouchesTheSiteRelease()
    {
        var found = new List<string>();
        foreach (string file in OtherWorkflows())
        {
            foreach (Match m in TouchesSite().Matches(File.ReadAllText(file)))
            {
                found.Add($"{Path.GetFileName(file)}: {m.Value}");
            }
        }

        Assert.True(found.Count == 0, "the site release belongs to website.yml alone: " + string.Join("; ", found));
    }

    /// <summary>
    /// The nightly deletes old releases, and this is the one that proves it can never delete the website. Its pattern is anchored and
    /// requires a version and a nightly number, so the tag "site" cannot match it whatever else changes around it.
    /// </summary>
    [Fact]
    public void TheNightlyCleanupCannotMatchTheSiteTag()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml"));
        var shape = ShapePattern().Match(nightly);
        Assert.True(shape.Success, "nightly.yml no longer has a tag pattern for the releases it deletes, so this cannot check it");

        var pattern = new Regex(shape.Groups["pattern"].Value.Replace(@"\d", "[0-9]", StringComparison.Ordinal));

        Assert.False(pattern.IsMatch("site"), "the nightly's cleanup would delete the website's release");
        Assert.False(pattern.IsMatch("nightly"), "the nightly's cleanup would delete the rolling nightly release itself");
        Assert.False(pattern.IsMatch("v0.1.0"), "the nightly's cleanup would delete a numbered release");

        // And it still matches what it is for, so this is not passing by having stopped matching anything.
        Assert.Matches(pattern, "v0.2.0-nightly.16");
    }

    /// <summary>The workflow signs what it publishes, with the key that already exists rather than a new one.</summary>
    [Fact]
    public void TheSiteIsSignedWithTheKeyTheApplicationAlreadyTrusts()
    {
        Assert.Contains("GROUPLAB_UPDATE_SIGNING_KEY", Website, StringComparison.Ordinal);
        Assert.Contains("grouplab-site.tar.gz.sig", Website, StringComparison.Ordinal);

        // The signature is checked against the public half the application carries, before anything is published: a key that cannot verify
        // its own signature fails here rather than on the server, where the only symptom is a site that stops updating.
        Assert.Contains("UpdateKeys.cs", Website, StringComparison.Ordinal);
        Assert.Contains("-verify public.pem", Website, StringComparison.Ordinal);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 123 section 1: a nightly for a commit that main has moved past publishes nothing and says so, rather
    /// than failing.
    /// <para>
    /// The reason is not tidiness. <c>GITHUB_TOKEN</c> cannot hold the <c>workflows</c> permission, so GitHub refuses to let it create a ref
    /// on a commit whose <c>.github/workflows</c> differ from the default branch. Creating the release tag is creating a ref, so a stale
    /// nightly over a push that touched a workflow file is refused with a 403 that reads like a misconfiguration and is not one.
    /// </para>
    /// <para>
    /// Without this, the symptom is a red nightly on a perfectly good commit, and the temptation is to widen a permission to fix it. That is
    /// what makes it worth a test: the wrong response to this failure is a worse repository.
    /// </para>
    /// </summary>
    [Fact]
    public void ANightlyThatMainHasMovedPastPublishesNothing()
    {
        string nightly = File.ReadAllText(Repo.PathTo(".github", "workflows", "nightly.yml"));

        // It asks what main's head is, and compares it with the commit that was tested.
        Assert.Contains("commits/main", nightly, StringComparison.Ordinal);
        Assert.Contains("fresh=no", nightly, StringComparison.Ordinal);
        Assert.Contains("fresh=yes", nightly, StringComparison.Ordinal);

        // Both jobs that would publish are guarded by it, so a stale run does not even package.
        int guards = Guard().Matches(nightly).Count;
        Assert.True(guards >= 2, $"only {guards} jobs are guarded by the freshness check, and both package and publish must be");

        // And it says so rather than failing: a notice and a summary, no non-zero exit on that path.
        Assert.Contains("::notice::", nightly, StringComparison.Ordinal);
        Assert.Contains("publishes nothing", nightly, StringComparison.Ordinal);
    }

    /// <summary>The triggers a workflow declares, read from its <c>on:</c> block.</summary>
    private static List<string> Triggers(string yaml)
    {
        var found = new List<string>();
        bool inOn = false;
        foreach (string line in yaml.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("on:", StringComparison.Ordinal))
            {
                inOn = true;
                continue;
            }

            if (inOn)
            {
                if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                {
                    break;
                }

                var m = Trigger().Match(line);
                if (m.Success)
                {
                    found.Add(m.Groups["name"].Value);
                }
            }
        }

        return found;
    }

    [GeneratedRegex(@"^  (?<name>[a-z_]+):")]
    private static partial Regex Trigger();

    /// <summary>Calling it, dispatching it, or firing on it: the three ways one workflow can set another going.</summary>
    [GeneratedRegex(@"uses:\s*\.?/?\.github/workflows/website\.yml|gh workflow run\s+website(?:\.yml)?|workflow_run:[\s\S]{0,200}?workflows:\s*\[?[^\]\r\n]*website")]
    private static partial Regex StartsTheWebsite();

    [GeneratedRegex(@"gh release (?:create|edit|upload|delete)\s+site\b|releases/download/site\b")]
    private static partial Regex TouchesSite();

    [GeneratedRegex(@"shape = re\.compile\(r'(?<pattern>[^']+)'\)")]
    private static partial Regex ShapePattern();

    [GeneratedRegex(@"if:\s*needs\.name-it\.outputs\.fresh == 'yes'")]
    private static partial Regex Guard();
}
