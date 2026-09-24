using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 160 section 2: <c>docs/notes/STATE.md</c> is the file both sessions read first, and it is under 120 lines,
/// rewritten rather than appended to at the end of every run.
/// <para>
/// <b>The length is the whole point of it.</b> A state file that grows is a fourth log, and the reason this entry exists is that three logs
/// had grown to 2.1 MB between them. A limit nobody checks is a limit that lasts about a week, which is roughly how long it took the entry
/// headings in the planning log to drift to a form two tests could not read.
/// </para>
/// </summary>
public class StateFileTests
{
    private static string Path => System.IO.Path.Combine(Repo.PathTo("docs", "notes"), "STATE.md");

    [Fact]
    public void TheStateFileIsShortEnoughToBeReadFirst()
    {
        Assert.True(File.Exists(Path), "docs/notes/STATE.md is the file both sessions read first, and it is not here.");

        int lines = File.ReadAllLines(Path).Length;
        Assert.True(lines <= 120,
            $"docs/notes/STATE.md is {lines} lines and entry 160 section 2 says under 120. It is rewritten every run, not "
            + "appended to: anything that wants to be kept belongs in the logs, which is what they are for.");
    }

    /// <summary>
    /// And it answers the questions section 2 names. A state file missing one of these sends the reader back into the logs, which is the
    /// cost this file exists to avoid.
    /// </summary>
    [Fact]
    public void ItAnswersWhatItIsFor()
    {
        string text = File.ReadAllText(Path);
        foreach (string heading in new[] { "In flight", "The next three", "Blocked", "Open questions", "The inbox" })
        {
            Assert.True(text.Contains(heading, StringComparison.OrdinalIgnoreCase),
                $"docs/notes/STATE.md does not say anything about \"{heading}\". Entry 160 section 2 lists what it holds.");
        }
    }

    /// <summary>
    /// The archive exists and the live logs are small. Entry 160 section 1: nothing is deleted, so the check is that the split happened and
    /// has not silently been undone by a file growing back.
    /// </summary>
    [Fact]
    public void TheLiveLogsStayedSmallAndNothingWasDeleted()
    {
        string archive = System.IO.Path.Combine(Repo.PathTo("docs", "notes"), "archive");
        Assert.True(Directory.Exists(archive), "docs/notes/archive is where the older log material lives, and it is not here.");
        Assert.NotEmpty(Directory.EnumerateFiles(archive, "*.md"));

        foreach (string name in new[] { "NOTES-FROM-PLANNING.md", "PHASE1-RESULTS.md", "QUESTIONS-FOR-PLANNING.md" })
        {
            long bytes = new FileInfo(System.IO.Path.Combine(Repo.PathTo("docs"), name)).Length;
            Assert.True(bytes < 300_000,
                $"docs/{name} is back up to {bytes / 1024} KB. Run scripts/split-logs.py: reading these whole is most of a "
                + "day's token allowance spent before any work happens, which is what entry 160 was about.");
        }
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 171 section 5: the inbox line is the one fact in this file a machine can check, and it was the one that
    /// was wrong. It listed 154 to 159 and 161 when the directory held 154 to 159, 164 to 167, 169 and 170. The line starting
    /// <c>**Holds:**</c> is read as a list of entry numbers and compared with the <c>entry-NN.md</c> files in the directory.
    /// </summary>
    [Fact]
    public void ItsInboxListIsWhatTheInboxHolds()
    {
        string line = File.ReadAllLines(Path).SingleOrDefault(l => l.StartsWith("**Holds:**", StringComparison.Ordinal))
            ?? throw new Xunit.Sdk.XunitException("docs/notes/STATE.md has no \"**Holds:**\" line under The inbox, so what it says the inbox holds cannot be checked.");
        var listed = System.Text.RegularExpressions.Regex.Matches(line, @"\d+").Select(m => int.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).Order().ToList();

        var held = Directory.EnumerateFiles(Repo.PathTo("docs", "notes", "inbox"), "entry-*.md")
            .Select(f => System.Text.RegularExpressions.Regex.Match(System.IO.Path.GetFileName(f), @"^entry-(\d+)\.md$"))
            .Where(m => m.Success).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).Order().ToList();

        Assert.True(listed.SequenceEqual(held),
            $"docs/notes/STATE.md says the inbox holds {string.Join(", ", listed)}, and it holds {string.Join(", ", held)}. Rewrite the line.");
    }
}
