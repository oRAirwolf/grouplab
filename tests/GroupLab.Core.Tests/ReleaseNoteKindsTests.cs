using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 149 section 1, answering question 47: there are five kind words, they land under two headings, and the
/// documentation and the generator name the same five.
/// <para>
/// <b>What drifted, and why a test rather than care.</b> Entry 145 named two words because two headings are what a reader sees.
/// <c>CLAUDE.md</c> named some of them. <c>scripts/release-notes.py</c> accepted five. Three descriptions of one rule, none of them wrong
/// on its own, and no way for a person writing a commit trailer to tell which one to believe. Nobody notices a documentation file falling
/// behind a regular expression, so the pair is held together here instead.
/// </para>
/// <para>
/// The test reads both files rather than carrying its own copy of the list. A third copy would drift the same way.
/// </para>
/// </summary>
public class ReleaseNoteKindsTests
{
    private static readonly string[] Expected = ["new", "fixed", "changed", "user", "internal"];

    /// <summary>The words the generator will accept on a <c>Release-note-kind:</c> line.</summary>
    private static HashSet<string> InTheGenerator()
    {
        string text = File.ReadAllText(Path.Combine(Repo.PathTo("scripts"), "release-notes.py"));
        var line = Regex.Match(text, @"^KIND\s*=\s*re\.compile\(r""\^Release-note-kind:.*?\(\?P<kind>(?<words>[a-z|]+)\)",
            RegexOptions.Multiline);
        Assert.True(line.Success, "scripts/release-notes.py no longer has a KIND pattern this test can read. "
            + "If the generator changed shape, this test has to change with it rather than be deleted.");
        return [.. line.Groups["words"].Value.Split('|')];
    }

    /// <summary>The words CLAUDE.md names, taken from the two bullets that say which heading each one lands under.</summary>
    private static HashSet<string> InTheRules()
    {
        string text = File.ReadAllText(Path.Combine(Repo.Root, "CLAUDE.md"));
        var words = new HashSet<string>();
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("- `") || !trimmed.Contains(" it under **"))
            {
                continue;
            }

            string before = trimmed[..trimmed.IndexOf(" it under **", StringComparison.Ordinal)];
            foreach (Match m in Regex.Matches(before, "`([a-z]+)`"))
            {
                words.Add(m.Groups[1].Value);
            }
        }

        return words;
    }

    [Fact]
    public void TheRulesAndTheGeneratorNameTheSameFiveWords()
    {
        var generator = InTheGenerator();
        var rules = InTheRules();

        Assert.Equal(Expected.Order(), generator.Order());
        Assert.Equal(Expected.Order(), rules.Order());
    }

    /// <summary>
    /// Four of the five land under "What you will notice" and one under "Under the hood". A word that lands nowhere would be accepted on a
    /// trailer and then silently drop the line out of the notes, which is the failure this whole area exists to prevent.
    /// </summary>
    [Fact]
    public void EveryWordSaysWhichHeadingItLandsUnder()
    {
        string text = File.ReadAllText(Path.Combine(Repo.Root, "CLAUDE.md"));
        foreach (string word in Expected)
        {
            string heading = word == "internal" ? "Under the hood" : "What you will notice";
            bool said = text.Split('\n').Any(l =>
                l.Contains($"`{word}`", StringComparison.Ordinal)
                && l.Contains(" it under **", StringComparison.Ordinal)
                && l.Contains(heading, StringComparison.Ordinal));

            Assert.True(said, $"CLAUDE.md does not say that a Release-note-kind of {word} lands under {heading}. "
                + "Every one of the five words has to say where it goes, or the writer has to read the generator to find out.");
        }
    }

    /// <summary>
    /// The generator sorts the four noticed words into one heading. This is the half that would fail quietly: adding a sixth word to the
    /// pattern without adding it to KINDS or SAME accepts the trailer and then loses the line.
    /// </summary>
    [Fact]
    public void EveryAcceptedWordIsSortedSomewhere()
    {
        string text = File.ReadAllText(Path.Combine(Repo.PathTo("scripts"), "release-notes.py"));
        var kinds = Regex.Match(text, @"^KINDS\s*=\s*\[(?<list>[^\]]*)\]", RegexOptions.Multiline);
        var same = Regex.Match(text, @"^SAME\s*=\s*\{(?<list>[^}]*)\}", RegexOptions.Multiline);
        Assert.True(kinds.Success && same.Success, "scripts/release-notes.py no longer has KINDS and SAME this test can read.");

        var sorted = new HashSet<string>(Regex.Matches(kinds.Groups["list"].Value, "\"([a-z]+)\"").Select(m => m.Groups[1].Value));
        foreach (Match m in Regex.Matches(same.Groups["list"].Value, "\"([a-z]+)\"\\s*:"))
        {
            sorted.Add(m.Groups[1].Value);
        }

        foreach (string word in InTheGenerator())
        {
            Assert.True(sorted.Contains(word),
                $"release-notes.py accepts a Release-note-kind of {word} but sorts it under no heading, so a commit using it "
                + "would be accepted and then vanish from the notes.");
        }
    }
}
