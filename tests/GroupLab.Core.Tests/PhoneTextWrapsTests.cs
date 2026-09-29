using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 282 section 1: on the Fold 7 the picture check's notes ran off the card's edge, "GroupLab could not read the
/// square codes that r…", and a section heading was cut to "Full CEP table and the fitted". A phone text that takes its words from a value
/// wraps to its box. Every <c>new TextBlock</c> in the phone's code either wraps, holds fixed words written into the code, or says on its
/// line why it is one line on purpose (a number, a score, a tab's name). The phone's views cannot be laid out in a test here, so this reads
/// the code, which is where the fault was.
/// </summary>
public partial class PhoneTextWrapsTests
{
    [Fact]
    public void EveryPhoneTextThatTakesItsWordsFromAValueWraps()
    {
        var unwrapped = new List<string>();
        foreach (string path in Directory.EnumerateFiles(Repo.PathTo("android", "GroupLab.Android"), "*.cs"))
        {
            string code = File.ReadAllText(path);
            foreach (Match m in NewTextBlock().Matches(code))
            {
                int open = code.IndexOf('{', m.Index + m.Length);
                if (open < 0 || code[(m.Index + m.Length)..open].Replace("\r", "", StringComparison.Ordinal).Split('\n')
                        .Any(l => l.Trim().Length > 0 && !l.TrimStart().StartsWith("//", StringComparison.Ordinal)))
                {
                    continue; // not an object initializer
                }

                int depth = 0, end = open;
                for (; end < code.Length; end++)
                {
                    depth += code[end] == '{' ? 1 : code[end] == '}' ? -1 : 0;
                    if (depth == 0)
                    {
                        break;
                    }
                }

                string initializer = code[open..(end + 1)];
                int lineEnd = code.IndexOf('\n', end);
                string said = code[m.Index..(lineEnd < 0 ? code.Length : lineEnd)];
                bool fixedWords = FixedText().IsMatch(initializer);
                if (!initializer.Contains("TextWrapping", StringComparison.Ordinal) && !fixedWords
                    && !said.Contains("one line on purpose", StringComparison.Ordinal))
                {
                    int line = code[..m.Index].Count(c => c == '\n') + 1;
                    unwrapped.Add($"{Path.GetFileName(path)}:{line}");
                }
            }
        }

        Assert.True(unwrapped.Count == 0, "texts that take their words from a value and do not wrap: " + string.Join(", ", unwrapped));
    }

    [GeneratedRegex(@"new TextBlock\b")]
    private static partial Regex NewTextBlock();

    /// <summary>Words written into the code, "Cancel" or "✓": they are known to fit, and are not what ran off the card.</summary>
    [GeneratedRegex(@"Text\s*=\s*""[^""{}]*""")]
    private static partial Regex FixedText();
}
