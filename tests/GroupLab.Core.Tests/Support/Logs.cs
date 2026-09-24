namespace GroupLab.Core.Tests.Support;

/// <summary>
/// The planning log and the questions log, live file and archive together.
/// <para>
/// NOTES-FROM-PLANNING.md entry 160 section 1 split three files that weighed 2.1 MB between them, because reading them whole is most of a
/// day's token allowance spent before a line of work happens. Nothing was deleted: the older material sits in <c>docs/notes/archive/</c>,
/// whole and unedited.
/// </para>
/// <para>
/// <b>A test that reads only the live file is a test that quietly stops checking anything.</b> That is not a hypothetical here. The entry
/// headings in the log drifted from <c>##</c> to <c>#</c> at entry 119, and the two tests that read them look for <c>##</c>, so they had
/// been silently skipping the thirty four newest entries for days with nothing going red. Entry 160 normalised the headings; this class is
/// so that the split does not produce the same failure a second time by a different route.
/// </para>
/// </summary>
public static class Logs
{
    private static string Archive => Repo.PathTo("docs", "notes", "archive");

    /// <summary>Every line of the planning log, the live file first and then each archive file.</summary>
    public static string[] Notes() => Live(Repo.PathTo("docs", "NOTES-FROM-PLANNING.md"), "notes-*.md");

    /// <summary>Every line of the questions log, open questions first and then the answered archive.</summary>
    public static string[] Questions() => Live(Repo.PathTo("docs", "QUESTIONS-FOR-PLANNING.md"), "questions-*.md");

    private static string[] Live(string live, string pattern)
    {
        var lines = new List<string>(File.ReadAllLines(live));
        if (Directory.Exists(Archive))
        {
            foreach (string file in Directory.EnumerateFiles(Archive, pattern).Order(StringComparer.Ordinal))
            {
                lines.AddRange(File.ReadAllLines(file));
            }
        }

        return [.. lines];
    }
}
