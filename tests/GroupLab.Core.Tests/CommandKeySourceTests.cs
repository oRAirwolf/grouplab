using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 166 section 2: Command Z did nothing on a Mac because a shortcut read the Control key directly, and on a Mac
/// the Command key arrives as Meta. Every shortcut and every label naming one now goes through the application's CommandKey, and this reads
/// the application's source so a new direct read, or a label hard coding "Ctrl", fails here rather than on the next Mac tester.
/// </summary>
public class CommandKeySourceTests
{
    private static IEnumerable<(string File, int Line, string Text)> AppLines()
    {
        string root = Repo.PathTo("src", "GroupLab.App");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                     .Where(f => Path.GetFileName(f) != "CommandKey.cs"))
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                yield return (Path.GetFileName(file), i + 1, lines[i]);
            }
        }
    }

    [Fact]
    public void NoShortcutReadsTheControlKeyDirectly()
    {
        var reads = AppLines().Where(l => l.Text.Contains("KeyModifiers.Control", StringComparison.Ordinal)).Select(l => $"{l.File}:{l.Line}").ToList();
        Assert.True(reads.Count == 0, "read the command key through CommandKey, not KeyModifiers.Control: " + string.Join(", ", reads));
    }

    [Fact]
    public void NoLabelNamesTheControlKeyByHand()
    {
        // A string literal saying Ctrl, outside a comment. Comments may still say Ctrl+Z when they mean the shortcut.
        var literal = new Regex("\"[^\"]*\\bCtrl\\b[^\"]*\"");
        var hard = AppLines().Where(l => !l.Text.TrimStart().StartsWith("//", StringComparison.Ordinal) && literal.IsMatch(l.Text)).Select(l => $"{l.File}:{l.Line}").ToList();
        Assert.True(hard.Count == 0, "name a shortcut with CommandKey.Label, so a Mac shows Command: " + string.Join(", ", hard));
    }
}
