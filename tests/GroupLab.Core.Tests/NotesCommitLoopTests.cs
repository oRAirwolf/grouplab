using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 144 section 2.2: the nightly writes its own release notes and pushes them, and that push must publish the
/// site and start nothing else.
/// <para>
/// <b>The loop it guards against is not hypothetical.</b> A notes commit lands on main. "build and test" runs. The nightly triggers on that
/// workflow succeeding, builds, publishes, writes notes, and pushes them. That is a build every few minutes for ever, each one deleting the
/// oldest release to keep thirty, until somebody notices.
/// </para>
/// <para>
/// <b>And a run whose jobs were all skipped still completes as a success</b>, so skipping the jobs is not by itself enough: the nightly has
/// to check the marker too. Both halves are pinned here, and so is the other direction, which matters just as much: an ordinary commit must
/// still run the full suite. A guard that skipped everything would also be "no loop".
/// </para>
/// </summary>
public partial class NotesCommitLoopTests
{
    /// <summary>The marker the nightly puts in the subject of the commit it makes, and nothing else writes.</summary>
    private const string Marker = "[notes] ";

    /// <summary>
    /// The weekly screenshot job's marker, entry 144 section 4. An image-only commit needs no C# test run, and a build of the application
    /// from one would be a release of nothing.
    /// </summary>
    private const string ScreensMarker = "[screens] ";

    private static string Workflow(string name) => File.ReadAllText(Repo.PathTo(".github", "workflows", name));

    /// <summary>Every job in a workflow, with the condition it runs under, or null where it has none.</summary>
    private static IEnumerable<(string Job, string? If)> Jobs(string yaml)
    {
        string[] lines = yaml.ReplaceLineEndings("\n").Split('\n');
        int at = Array.FindIndex(lines, l => l.StartsWith("jobs:", StringComparison.Ordinal));
        Assert.True(at >= 0, "the workflow has no jobs");

        string? job = null;
        var condition = new List<string>();
        for (int i = at + 1; i < lines.Length; i++)
        {
            var name = JobName().Match(lines[i]);
            if (name.Success)
            {
                if (job is not null)
                {
                    yield return (job, condition.Count > 0 ? string.Join(" ", condition) : null);
                }

                job = name.Groups[1].Value;
                condition.Clear();
            }
            else if (job is not null && lines[i].TrimStart().StartsWith("if:", StringComparison.Ordinal))
            {
                condition.Add(lines[i].Trim()[3..].Trim());
                // A folded condition carries on over the following indented lines.
                for (int j = i + 1; j < lines.Length && lines[j].StartsWith("      ", StringComparison.Ordinal)
                    && !lines[j].TrimStart().StartsWith("- ", StringComparison.Ordinal)
                    && !JobName().IsMatch(lines[j]) && lines[j].Trim().Length > 0
                    && !Regex.IsMatch(lines[j].Trim(), @"^[a-zA-Z-]+:"); j++)
                {
                    condition.Add(lines[j].Trim());
                }
            }
        }

        if (job is not null)
        {
            yield return (job, condition.Count > 0 ? string.Join(" ", condition) : null);
        }
    }

    /// <summary>
    /// A notes commit starts no test run. Every job in the test workflow refuses it, because a job that ran would make the run succeed and
    /// the nightly would fire on that success.
    /// </summary>
    [Fact]
    public void ANotesCommitStartsNoTestRun()
    {
        var jobs = Jobs(Workflow("ci.yml")).ToList();
        Assert.NotEmpty(jobs);

        var unguarded = jobs.Where(j => j.If is null || !j.If.Contains(Marker, StringComparison.Ordinal)).Select(j => j.Job).ToList();

        Assert.True(unguarded.Count == 0,
            "these jobs would run on the nightly's own release-notes commit, and a run with any job in it succeeds, which starts another "
            + "nightly, which writes more notes: " + string.Join(", ", unguarded));

        var unscreened = jobs.Where(j => j.If is null || !j.If.Contains(ScreensMarker, StringComparison.Ordinal)).Select(j => j.Job).ToList();
        Assert.True(unscreened.Count == 0,
            "these jobs would run on the weekly screenshot commit, which changes only images: " + string.Join(", ", unscreened));
    }

    /// <summary>And the nightly refuses it too, because an all-skipped run still reports success.</summary>
    [Fact]
    public void TheNightlyRefusesItsOwnNotesCommit()
    {
        string nightly = Workflow("nightly.yml");

        Assert.Contains("head_commit.message", nightly, StringComparison.Ordinal);
        Assert.Contains(Marker, nightly, StringComparison.Ordinal);

        var first = Jobs(nightly).First();
        Assert.NotNull(first.If);
        Assert.Contains(Marker, first.If!, StringComparison.Ordinal);
        Assert.Contains("startsWith", first.If!, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other direction. An ordinary commit has no marker, so every guard passes it and the full suite runs. This is the half that would
    /// fail silently: a guard written the wrong way round turns the test suite off and nothing says so.
    /// </summary>
    [Fact]
    public void AnOrdinaryCommitStillRunsEverything()
    {
        foreach (var (job, condition) in Jobs(Workflow("ci.yml")))
        {
            Assert.NotNull(condition);

            // The guard is a negation of the marker test, so a subject without the marker satisfies it. A guard that asserted the marker
            // rather than denying it would skip every ordinary commit instead.
            Assert.StartsWith("\"!startsWith", condition!, StringComparison.Ordinal);

            // Every clause must be a denial. One positive clause anywhere would skip every ordinary commit instead of the marked ones,
            // which is the failure that turns the suite off without saying so.
            foreach (string clause in condition!.Trim('"').Split("&&", StringSplitOptions.TrimEntries))
            {
                Assert.StartsWith("!startsWith(", clause, StringComparison.Ordinal);
                Assert.Contains("head_commit.message", clause, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("||", condition!, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The notes commit does publish the site, which is the whole point of writing them automatically. It changes one file, and that file
    /// is in the site workflow's paths filter.
    /// </summary>
    [Fact]
    public void ANotesCommitPublishesTheSite()
    {
        string nightly = Workflow("nightly.yml");
        Assert.Contains("docs/RELEASE-NOTES.md", nightly, StringComparison.Ordinal);
        Assert.Contains("git push origin main", nightly, StringComparison.Ordinal);

        Assert.Contains("'docs/RELEASE-NOTES.md'", Workflow("website.yml"), StringComparison.Ordinal);
    }

    [GeneratedRegex(@"^  ([a-zA-Z0-9_-]+):\s*$")]
    private static partial Regex JobName();
}
