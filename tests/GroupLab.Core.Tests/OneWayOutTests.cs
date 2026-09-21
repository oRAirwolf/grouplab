using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 122 section 2: every way GroupLab reaches out of its own process goes through one implementation, and this
/// fails if anything else starts a browser, a viewer or a file manager of its own.
/// <para>
/// It exists because of a real harm. The settings page's link to the repository started the default browser itself, and entry 117's control
/// walk clicks every control it finds, so a test run opened browser tabs on Alan's machine. That is the same fault as entry 114's print to
/// the OneNote driver, and the same answer: the tests replace the one way out with a recorder.
/// </para>
/// </summary>
public partial class OneWayOutTests
{
    /// <summary>The one file that may start something outside the process.</summary>
    private const string TheOneWayOut = "OutsideWorld.cs";

    [Fact]
    public void NothingButTheOneWayOutStartsAnythingOutsideTheProcess()
    {
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.GetFileName(file) == TheOneWayOut)
            {
                continue;
            }

            string text = File.ReadAllText(file);
            foreach (Match m in Shelling().Matches(text))
            {
                found.Add($"{Path.GetFileName(file)}: {m.Value.ReplaceLineEndings(" ")}");
            }
        }

        Assert.True(found.Count == 0,
            $"these start something outside GroupLab's own process without going through {TheOneWayOut}, so a test or the benchmark can "
            + $"reach into the person's own applications: {string.Join("; ", found)}");
    }

    /// <summary>
    /// The one way out is an interface with a real implementation and a recorder, so a test can put the recorder in its place. Without the
    /// interface the guard above would only move the problem.
    /// </summary>
    [Fact]
    public void TheOneWayOutCanBeReplacedByARecorder()
    {
        string text = File.ReadAllText(Repo.PathTo("src", "GroupLab.Core", "Updates", TheOneWayOut));
        Assert.Contains("interface IOutsideWorld", text, StringComparison.Ordinal);
        Assert.Contains("class TheOutsideWorld", text, StringComparison.Ordinal);
        Assert.Contains("class RecordedOutsideWorld", text, StringComparison.Ordinal);
        Assert.Contains("static IOutsideWorld Current", text, StringComparison.Ordinal);
    }

    /// <summary>Starting a process with the shell, or with a launcher, is what reaches a person's own applications.</summary>
    [GeneratedRegex(@"UseShellExecute\s*=\s*true|Launcher\.LaunchUriAsync|Launcher\.LaunchFileAsync|Launcher\.LaunchDirectoryInfoAsync")]
    private static partial Regex Shelling();
}
