using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// The facts in README.md that a test can check, NOTES-FROM-PLANNING.md entry 31 section 3: the repository's front page has gone stale
/// twice, and both times a person caught it. Prose is left to people. What is checked here is what a reader cannot tell is wrong: links and
/// images that resolve, the number of built-in sheets, the target framework, and the project's no-em-dash rule. Every failure names the
/// README line and says what to change.
/// </summary>
public partial class ReadmeTests
{
    private static readonly string[] Lines = File.ReadAllLines(Repo.PathTo("README.md"));

    [Fact]
    public void EveryRelativeLinkAndImageResolves()
    {
        var failures = new List<string>();
        for (int i = 0; i < Lines.Length; i++)
        {
            foreach (Match m in Link().Matches(Lines[i]))
            {
                string target = m.Groups["target"].Value;
                if (target.Contains("://", StringComparison.Ordinal) || target.StartsWith('#') || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    continue;
                }

                string path = target.Split('#')[0];
                string full = Repo.PathTo(path.Split('/', StringSplitOptions.RemoveEmptyEntries));
                if (!File.Exists(full) && !Directory.Exists(full))
                {
                    bool image = m.Groups["bang"].Success;
                    failures.Add($"README.md line {i + 1}: the {(image ? "image" : "link")} to {target} does not exist in the repository. Add the file, or correct the path.");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TheStatedNumberOfBuiltInSheetsMatchesTargets()
    {
        int actual = Directory.GetFiles(Repo.PathTo("targets"), "*.gltd.json").Length;
        var stated = Lines.Select((line, i) => (Line: i + 1, Match: SheetCount().Match(line))).Where(x => x.Match.Success).ToList();
        Assert.True(stated.Count > 0, "README.md no longer states the number of built-in sheets between <!--count:sheets--> and <!--/count--> markers, so this test cannot check it. Put the markers back around the number.");
        foreach (var (line, match) in stated)
        {
            int claimed = int.Parse(match.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(claimed == actual, $"README.md line {line} claims {claimed} built-in sheets; targets/ holds {actual}. Change the number between the count markers to {actual}.");
        }
    }

    [Fact]
    public void TheStatedFrameworkMatchesTheBuild()
    {
        var props = TargetFramework().Match(File.ReadAllText(Repo.PathTo("Directory.Build.props")));
        Assert.True(props.Success, "Directory.Build.props has no <TargetFramework>netN.0</TargetFramework> to compare the README against.");
        string version = props.Groups["v"].Value;
        var stated = Lines.Select((line, i) => (Line: i + 1, Match: Framework().Match(line))).Where(x => x.Match.Success).ToList();
        Assert.True(stated.Count > 0, "README.md no longer states the framework between <!--framework--> and <!--/framework--> markers, so this test cannot check it. Put the markers back around it.");
        foreach (var (line, match) in stated)
        {
            Assert.True(match.Groups["v"].Value == version, $"README.md line {line} says .NET {match.Groups["v"].Value}; Directory.Build.props builds net{version}.0. Change the README to .NET {version}.");
        }
    }

    [Fact]
    public void NoEmDashAppears()
    {
        var lines = Lines.Select((line, i) => (Line: i + 1, Text: line)).Where(x => x.Text.Contains('—', StringComparison.Ordinal)).Select(x => x.Line).ToList();
        Assert.True(lines.Count == 0, $"README.md has an em dash on line(s) {string.Join(", ", lines)}. CONTRIBUTING.md rules them out; use a comma, a colon or two sentences.");
    }

    [GeneratedRegex(@"(?<bang>!)?\[[^\]]*\]\((?<target>[^)\s]+)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"<!--count:sheets-->(?<n>\d+)<!--/count-->")]
    private static partial Regex SheetCount();

    [GeneratedRegex(@"<!--framework-->\.NET (?<v>\d+)<!--/framework-->")]
    private static partial Regex Framework();

    [GeneratedRegex(@"<TargetFramework>net(?<v>\d+)\.0</TargetFramework>")]
    private static partial Regex TargetFramework();
}
