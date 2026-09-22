using System.Text.RegularExpressions;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 sections 5.1.1 and 5.1.2: one type scale and one spacing scale for the whole application, defined once.
/// <para>
/// <b>Why a test that reads the source rather than the screen.</b> <see cref="Entry109Tests"/> already walks a rendered window and fails on
/// a text size that is not on the scale, which is the better test of the two where it reaches. It only reaches what a test happens to render:
/// a size set on a control that no headless test shows is invisible to it, and stays invisible until somebody sees the screen. This one reads
/// every file, so a number typed anywhere fails here on the commit that typed it.
/// </para>
/// </summary>
public class TypeScaleTests
{
    private static readonly string Source = Path.Combine(Root(), "src", "GroupLab.App");

    /// <summary>
    /// The repository, found from this file's own path rather than from the build output. The App tests are built into a scratch folder
    /// outside the repository when a test run has to avoid a locked file, and nothing walking up from the output folder finds anything there.
    /// </summary>
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string here = "")
    {
        for (var dir = new DirectoryInfo(Path.GetDirectoryName(here)!); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "GroupLab.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException($"No GroupLab.slnx above {here}.");
    }

    /// <summary>The one file allowed to hold numbers: the scale has to be written down somewhere.</summary>
    private const string TheStyleFile = "Theme";

    private static IEnumerable<(string File, int Line, string Text)> Lines()
    {
        foreach (string path in Directory.EnumerateFiles(Source, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(Source, path);
            if (relative.Split(Path.DirectorySeparatorChar)[0] == TheStyleFile || relative.Contains("obj") || relative.Contains("bin"))
            {
                continue;
            }

            var text = File.ReadAllLines(path);
            for (int i = 0; i < text.Length; i++)
            {
                yield return (relative, i + 1, text[i]);
            }
        }
    }

    /// <summary>
    /// A font size is never a number outside the style file. It is always a name from <see cref="Tokens"/>, so changing the scale changes
    /// the application rather than one control somebody remembered.
    /// </summary>
    [Fact]
    public void NoFontSizeIsALiteralOutsideTheStyleFile()
    {
        var typed = Lines()
            .Where(l => Regex.IsMatch(l.Text, @"FontSize\s*[=:]\s*-?\d"))
            .Select(l => $"{l.File} line {l.Line}: {l.Text.Trim()}")
            .ToList();

        Assert.True(typed.Count == 0,
            "a font size is a number here rather than a name from Tokens, so the type scale no longer describes the application:\n  "
            + string.Join("\n  ", typed));
    }

    /// <summary>The scale itself is five or six sizes, entry 141 section 5.1.1, because a scale of ten is not a scale.</summary>
    [Fact]
    public void TheScaleIsFiveOrSixSizes()
    {
        Assert.InRange(Tokens.TypeScale.Count, 5, 6);
        Assert.All(Tokens.TypeScale, size => Assert.InRange(size, 8, 48));
    }

    /// <summary>
    /// The spacing scale is one ladder, and every gap between controls is a step on it. Zero is allowed, because no gap is a decision rather
    /// than a size.
    /// </summary>
    [Fact]
    public void EverySpacingIsAStepOnTheScale()
    {
        var steps = new HashSet<double> { 0, Tokens.Space4, Tokens.Space8, Tokens.Space12, Tokens.Space16, Tokens.Space20, Tokens.Space24 };
        var off = new List<string>();
        foreach (var line in Lines())
        {
            foreach (Match match in Regex.Matches(line.Text, @"(?:Spacing|ItemSpacing|LineSpacing)\s*=\s*(-?[\d.]+)"))
            {
                if (double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out double value) && !steps.Contains(value))
                {
                    off.Add($"{line.File} line {line.Line}: {match.Value}");
                }
            }
        }

        Assert.True(off.Count == 0,
            "a gap here is not a step on the spacing scale, so two screens that look the same are built differently:\n  "
            + string.Join("\n  ", off));
    }
}
