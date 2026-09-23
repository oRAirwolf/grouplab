using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 145: every build says what changed, in plain words, and no build ever says nothing changed.
/// <para>
/// <b>Why it needed an entry.</b> Six published builds said "Nothing in this build changes what you see or do. It carries internal work
/// only." Alan read them and said that whatever is done, it should be stated plainly what changed. He is right, and the sentence was not even
/// true: something changed in every build or there would have been no build. One of those six carried the first measurement GroupLab has
/// against 59 real photographs of a target on a board. Another carried eight research articles. Calling those "internal work only" teaches a
/// reader that this page is filler, and trains them to stop reading it.
/// </para>
/// <para>
/// So these hold the page to the shape entry 145 section 2 describes, and to plain words. The sentence itself is named here, because the way
/// it comes back is not somebody typing it again: it is the generator falling back to it when nothing else is easy.
/// </para>
/// </summary>
public partial class PlainReleaseNotesTests
{
    private static string Notes() => File.ReadAllText(Repo.PathTo("docs/RELEASE-NOTES.md"));

    /// <summary>Everything between one version heading and the next, keyed by version.</summary>
    private static Dictionary<string, string> Entries()
    {
        string text = Notes().ReplaceLineEndings("\n");
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        var headings = Regex.Matches(text, "^## (.+)$", RegexOptions.Multiline).ToList();

        for (int i = 0; i < headings.Count; i++)
        {
            int from = headings[i].Index + headings[i].Length;
            int to = i + 1 < headings.Count ? headings[i + 1].Index : text.Length;
            found[headings[i].Groups[1].Value.Trim()] = text[from..to];
        }

        return found;
    }

    /// <summary>
    /// The lines of an entry that are prose about the build, which is what the plain-words rules apply to. The date and commit line names a
    /// commit because that is its job, and the downloads link is an address; neither is prose and neither is checked.
    /// </summary>
    private static IEnumerable<string> Body(string entry) =>
        entry.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("- ", StringComparison.Ordinal));

    /// <summary>The sentence entry 145 exists to remove, and the shape of the count that replaced the rest of a build.</summary>
    [Fact]
    public void NoBuildSaysNothingChanged()
    {
        string text = Notes();

        Assert.DoesNotContain("Nothing in this build changes", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal work only", text, StringComparison.OrdinalIgnoreCase);

        // "Plus 7 internal changes (tests, documentation, build)." The count was the other way a build said nothing: it named a number where
        // a reader wanted the seven things.
        var counted = Counted().Matches(text).Select(m => m.Value).ToList();
        Assert.True(counted.Count == 0,
            "these give a count where the changes themselves belong, which is what entry 145 removed: " + string.Join("; ", counted));
    }

    /// <summary>Every build has something under one of the headings. A build with no lines at all is the old fault wearing a new shape.</summary>
    [Fact]
    public void EveryBuildListsSomething()
    {
        var empty = Entries().Where(e => !Body(e.Value).Any()).Select(e => e.Key).ToList();

        Assert.True(empty.Count == 0,
            "these builds list nothing at all, and something changed in every build or there would have been no build: "
            + string.Join(", ", empty));
    }

    /// <summary>
    /// And every build has a heading over those lines. A loose list with nothing saying whether it is the part you meet or the part you do
    /// not is worse than either heading on its own.
    /// </summary>
    [Fact]
    public void EveryBuildSaysWhichKindOfChangeItIs()
    {
        string[] headings =
        [
            "**What you will notice**", "**Under the hood**",

            // The shape published before 2026-09-23, left as it was. Entry 145 section 5 is a rewording of the entries that said nothing, not
            // a rewrite of the history, and New, Fixed and Changed are all things a person would notice.
            "### New", "### Fixed", "### Changed",
        ];

        var loose = Entries()
            .Where(e => Body(e.Value).Any() && !headings.Any(h => e.Value.Contains(h, StringComparison.Ordinal)))
            .Select(e => e.Key)
            .ToList();

        Assert.True(loose.Count == 0, "these builds list changes under no heading: " + string.Join(", ", loose));
    }

    /// <summary>
    /// Entry 145 section 4: write for a shooter who has never read this repository. A file path, a commit hash or a class name in the body of
    /// an entry is a line written from the code's side, and the reader it was written for is not the reader this page has.
    /// </summary>
    [Fact]
    public void NoLineNamesSomethingOnlyThisRepositoryKnowsAbout()
    {
        (string What, Regex Pattern)[] shaped =
        [
            ("a file path", FilePath()),
            ("a commit hash", CommitHash()),
            ("a class or method name", ClassName()),
            ("a name in code style", CodeStyle()),
        ];

        var found = new List<string>();
        foreach (var (version, entry) in Entries())
        {
            foreach (string line in Body(entry))
            {
                // The reference in brackets at the end is the one place a note may name an entry, and it is not prose.
                string prose = Reference().Replace(line, "");
                foreach (var (what, pattern) in shaped)
                {
                    var m = pattern.Match(prose);
                    if (m.Success)
                    {
                        found.Add(version + ": " + what + ", " + m.Value + ", in: " + line);
                    }
                }
            }
        }

        Assert.True(found.Count == 0,
            "these lines name something only this repository knows about, and this page is read by people who have never seen it:\n  "
            + string.Join("\n  ", found));
    }

    /// <summary>The generator is the thing that would bring the old sentence back, so it is held to the same rule.</summary>
    [Fact]
    public void TheGeneratorCannotWriteTheOldSentenceEither()
    {
        string script = File.ReadAllText(Repo.PathTo("scripts/release-notes.py"));

        // It appears in the explanation at the top of the script, which is where it belongs: the reason for the rule. It must not appear in
        // anything the script could print, so the check is on the code below the explanation.
        int code = script.IndexOf("import json", StringComparison.Ordinal);
        Assert.True(code > 0, "scripts/release-notes.py no longer starts the way this test expects");

        string body = script[code..];
        Assert.DoesNotContain("Nothing in this build changes", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internal changes (tests", body, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("What you will notice", body, StringComparison.Ordinal);
        Assert.Contains("Under the hood", body, StringComparison.Ordinal);
    }

    [GeneratedRegex("(?i)\\b(?:plus|and)\\s+\\d+\\s+(?:more\\s+)?(?:internal|other)\\b[^.]*\\.")]
    private static partial Regex Counted();

    [GeneratedRegex("\\b[\\w.-]+/[\\w./-]+|\\b[\\w-]+\\.(?:md|py|cs|json|ya?ml|html|css|js|txt|pdf|png)\\b", RegexOptions.IgnoreCase)]
    private static partial Regex FilePath();

    [GeneratedRegex("\\b(?=[0-9a-f]{7,40}\\b)(?=[^\\s]*\\d)[0-9a-f]{7,40}\\b")]
    private static partial Regex CommitHash();

    [GeneratedRegex("\\b(?!GroupLab\\b)[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]*)+\\b")]
    private static partial Regex ClassName();

    [GeneratedRegex("`[^`]+`")]
    private static partial Regex CodeStyle();

    [GeneratedRegex("\\((?i:entry)[^)]*\\)\\s*$")]
    private static partial Regex Reference();
}
