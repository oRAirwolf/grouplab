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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entries 49 section 4 and 50 section 1: the README said for days that only Windows builds, after CI had built
    /// and tested all three platforms. The platforms it names as building must be the platforms the build and test workflow runs.
    /// </summary>
    [Fact]
    public void TheStatedPlatformsAreThePlatformsCiBuildsAndTests()
    {
        var matrix = CiMatrix().Match(File.ReadAllText(Repo.PathTo(".github", "workflows", "ci.yml")));
        Assert.True(matrix.Success, ".github/workflows/ci.yml has no `os: [...]` matrix to compare the README against.");
        var names = new Dictionary<string, string> { ["windows"] = "Windows", ["ubuntu"] = "Linux", ["macos"] = "macOS" };
        var built = matrix.Groups["os"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(runner => names.TryGetValue(runner.Split('-')[0], out string? name) ? name : runner).Order(StringComparer.Ordinal).ToList();
        var stated = Lines.Select((line, i) => (Line: i + 1, Match: Platforms().Match(line))).Where(x => x.Match.Success).ToList();
        Assert.True(stated.Count > 0, "README.md no longer names the platforms that build between <!--platforms--> and <!--/platforms--> markers, so this test cannot check it. Put the markers back around them.");
        foreach (var (line, match) in stated)
        {
            var claimed = match.Groups["list"].Value.Replace(" and ", ", ", StringComparison.Ordinal).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal).ToList();
            Assert.True(claimed.SequenceEqual(built), $"README.md line {line} names {string.Join(", ", claimed)} as building; ci.yml builds and tests on {string.Join(", ", built)}. Change the platforms between the markers to match the workflow.");
        }
    }

    [Fact]
    public void NoEmDashAppears()
    {
        var lines = Lines.Select((line, i) => (Line: i + 1, Text: line)).Where(x => x.Text.Contains('—', StringComparison.Ordinal)).Select(x => x.Line).ToList();
        Assert.True(lines.Count == 0, $"README.md has an em dash on line(s) {string.Join(", ", lines)}. CONTRIBUTING.md rules them out; use a comma, a colon or two sentences.");
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 87 section 2: the Planned section's states are hand-kept, and a hand-kept status list goes stale. So it is
    /// tied to DESIGN.md section 21: every phase there is a row here with exactly one of the four states and a gate beside it, every phase row
    /// has a feature list, and every feature carries a state of its own. A phase in one document and not the other fails this test.
    /// <para>
    /// It also keeps three things out of the section, following entry 60: no test counts, no percentages of progress and no dates, because all
    /// three go stale within days. A gate's own threshold, such as 99 percent of holes, is the gate and is spelled in words.
    /// </para>
    /// </summary>
    [Fact]
    public void EveryPhaseOfTheBuildPlanCarriesOneStateAndItsFeaturesDoToo()
    {
        string[] states = ["Not started", "In progress", "Built, not proven", "Done"];
        var planned = Section("## Planned", "## What GroupLab is not");
        var design = File.ReadAllLines(Repo.PathTo("DESIGN.md"));

        var inDesign = design.Select(l => DesignPhase().Match(l)).Where(m => m.Success)
            .ToDictionary(m => m.Groups["id"].Value, m => m.Groups["name"].Value.Trim(), StringComparer.OrdinalIgnoreCase);
        Assert.NotEmpty(inDesign);

        var rows = planned.Select(l => PhaseRow().Match(l)).Where(m => m.Success).ToList();
        var inReadme = rows.ToDictionary(m => m.Groups["id"].Value, m => m.Groups["name"].Value.Trim(), StringComparer.OrdinalIgnoreCase);
        Assert.Equal(inDesign.Keys.Order(), inReadme.Keys.Order());
        foreach (var (id, name) in inDesign)
        {
            Assert.Equal(name, inReadme[id], ignoreCase: true);
        }

        foreach (var row in rows)
        {
            Assert.Contains(row.Groups["state"].Value, states);
            Assert.True(row.Groups["gate"].Value.Trim().Length > 20, $"phase {row.Groups["id"].Value} has no gate beside its state");
        }

        // Every phase has a feature list under "What each phase holds", and every feature carries a state.
        var headings = planned.Select(l => FeatureHeading().Match(l)).Where(m => m.Success).Select(m => m.Groups["id"].Value).ToList();
        Assert.Equal(inDesign.Keys.Order(), headings.Order());
        var features = planned.Select(l => Feature().Match(l)).Where(m => m.Success).ToList();
        Assert.True(features.Count >= inDesign.Count, $"only {features.Count} features carry a state");
        foreach (var feature in features)
        {
            Assert.Contains(feature.Groups["state"].Value, states);
        }

        foreach (string line in planned)
        {
            Assert.DoesNotContain("%", line, StringComparison.Ordinal);
            Assert.False(Stale().IsMatch(line), $"the Planned section carries a date or a count that will go stale: {line}");
        }
    }

    /// <summary>The lines of one README section, from its heading to the next one named.</summary>
    private static string[] Section(string heading, string next)
    {
        int from = Array.FindIndex(Lines, l => l.StartsWith(heading, StringComparison.Ordinal));
        int to = Array.FindIndex(Lines, l => l.StartsWith(next, StringComparison.Ordinal));
        Assert.True(from >= 0 && to > from, $"README has no {heading} section before {next}");
        return [.. Lines[from..to]];
    }

    [GeneratedRegex(@"^\*\*Phase (?<id>[0-9]+a?): (?<name>[^.*]+)\.\*\*")]
    private static partial Regex DesignPhase();

    [GeneratedRegex(@"^\| \*\*(?<id>[0-9]+a?)\. (?<name>[^*|]+?)\*\* \| \*\*(?<state>[^*|]+)\*\* \|(?<gate>[^|]+)\|")]
    private static partial Regex PhaseRow();

    [GeneratedRegex(@"^\*\*Phase (?<id>[0-9]+a?)\. [^*]+\.\*\*$")]
    private static partial Regex FeatureHeading();

    [GeneratedRegex(@"^- \*\*(?<state>[^*.]+)\.\*\* ")]
    private static partial Regex Feature();

    [GeneratedRegex(@"\b(19|20)\d\d\b|\b\d+ (tests?|passing|complete)\b")]
    private static partial Regex Stale();

    [GeneratedRegex(@"(?<bang>!)?\[[^\]]*\]\((?<target>[^)\s]+)\)")]
    private static partial Regex Link();

    [GeneratedRegex(@"<!--count:sheets-->(?<n>\d+)<!--/count-->")]
    private static partial Regex SheetCount();

    [GeneratedRegex(@"<!--framework-->\.NET (?<v>\d+)<!--/framework-->")]
    private static partial Regex Framework();

    [GeneratedRegex(@"<TargetFramework>net(?<v>\d+)\.0</TargetFramework>")]
    private static partial Regex TargetFramework();

    [GeneratedRegex(@"<!--platforms-->(?<list>[^<]+)<!--/platforms-->")]
    private static partial Regex Platforms();

    [GeneratedRegex(@"os:\s*\[(?<os>[^\]]+)\]")]
    private static partial Regex CiMatrix();
}
