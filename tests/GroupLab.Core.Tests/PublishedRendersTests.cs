using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 126 section 3.2: everything in <c>docs/figures/screens/current/</c> is published on grouplab.org by the site
/// build, so it may only ever show material nobody owns.
/// <para>
/// The risk is specific and it is not hypothetical. This repository has stood beside range folders full of Alan's own photographs and a
/// donated submission, and the rule everywhere else has been that none of it is committed. A screenshot is the one place that rule could be
/// broken by accident rather than on purpose: a test opens an image, photographs the window, and the photograph goes to a website. Nobody
/// would notice by reading the diff, because a PNG diff shows nothing.
/// </para>
/// <para>
/// So this holds two things. Every published image is named in <c>SOURCES.md</c> with what it was made from, and that source is on a short
/// allowed list. And the tests that write into the folder are named here, so a new one cannot quietly start publishing something else.
/// </para>
/// </summary>
public partial class PublishedRendersTests
{
    /// <summary>This file, which names the folder and the act in its own patterns and so matches itself.</summary>
    private const string TheGuard = "PublishedRendersTests.cs";

    private static string Folder => Repo.PathTo("docs", "figures", "screens", "current");

    private static string Manifest => Path.Combine(Folder, "SOURCES.md");

    /// <summary>What a render may be made from. Adding to this list is a decision, which is why it is a list and not a pattern.</summary>
    private static readonly string[] Allowed =
    [
        "Entry109Tests synthetic sheet",
        "built-in library sheet",
        "no sheet at all",
        "scan 3",
    ];

    /// <summary>
    /// The only files that may write into the published folder. Entry 126 section 3.2: a new one is a new way to publish something, so it
    /// has to be added here deliberately, with whatever it renders added to <see cref="Allowed"/>.
    /// </summary>
    private static readonly string[] MayPublish =
    [
        "Entry109Tests.cs",
        "LibraryLayoutTests.cs",
        "SettingsLayoutTests.cs",
    ];

    [Fact]
    public void EveryPublishedImageSaysWhatItWasMadeFrom()
    {
        Assert.True(File.Exists(Manifest), $"{Manifest} is the record of what every published screenshot is. It must exist.");
        var said = Rows();

        var missing = Directory.EnumerateFiles(Folder, "*.png")
            .Select(Path.GetFileName)
            .Where(name => !said.ContainsKey(name!))
            .ToList();

        Assert.True(missing.Count == 0,
            "these are published on grouplab.org and SOURCES.md does not say what they were made from, so nobody can tell whether they show "
            + "somebody's target: " + string.Join(", ", missing));

        var gone = said.Keys.Where(name => !File.Exists(Path.Combine(Folder, name))).ToList();
        Assert.True(gone.Count == 0, "SOURCES.md names files that are not here: " + string.Join(", ", gone));
    }

    [Fact]
    public void NoPublishedImageCameFromAnythingButTheAllowedSources()
    {
        var wrong = Rows().Where(r => !Allowed.Contains(r.Value, StringComparer.Ordinal)).ToList();

        Assert.True(wrong.Count == 0,
            "a published screenshot may only show a sheet GroupLab generated, a built-in sheet, no sheet, or scan 3 under its consent "
            + "record: " + string.Join("; ", wrong.Select(r => $"{r.Key} says \"{r.Value}\"")));
    }

    /// <summary>
    /// Only the named tests may write into the folder. Without this the manifest would be a promise about the past: somebody could add a
    /// test that opens a photograph, render it, and the new file would simply be added to SOURCES.md with whatever words seemed right.
    /// </summary>
    [Fact]
    public void OnlyTheNamedTestsWriteIntoThePublishedFolder()
    {
        var writers = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("tests"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            // Naming the folder is not publishing into it: UserGuideTests reads the path to check the guide's links, and this file names it
            // to guard it. What matters is a file that both names the folder and saves a rendered frame.
            string text = File.ReadAllText(file);
            if (WritesToTheFolder().IsMatch(text) && SavesARender().IsMatch(text)
                && Path.GetFileName(file) != TheGuard
                && !MayPublish.Contains(Path.GetFileName(file), StringComparer.Ordinal))
            {
                writers.Add(Path.GetFileName(file));
            }
        }

        Assert.True(writers.Count == 0,
            "these write into the folder the website publishes, and they are not on the list that may: " + string.Join(", ", writers)
            + ". Add the file to MayPublish and its material to SOURCES.md, having checked that what it renders is nobody's target.");
    }

    /// <summary>
    /// Nothing that publishes may read an image from outside the repository. A range folder or a submission reached by absolute path is
    /// exactly how a photograph would arrive in a screenshot, and it is the one thing the manifest cannot catch by itself.
    /// </summary>
    [Fact]
    public void NothingThatPublishesReadsAnImageFromOutsideTheRepository()
    {
        var reaching = new List<string>();
        foreach (string name in MayPublish)
        {
            string[] found = Directory.GetFiles(Repo.PathTo("tests"), name, SearchOption.AllDirectories);
            foreach (string file in found.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in OutsideThisRepository().Matches(text))
                {
                    reaching.Add($"{name} names {m.Value}");
                }
            }
        }

        Assert.True(reaching.Count == 0,
            "a test that photographs the window must not read an image from outside this repository, because what it reads is published: "
            + string.Join("; ", reaching));
    }

    /// <summary>The manifest's table: the file name, and what it was made from.</summary>
    private static Dictionary<string, string> Rows()
    {
        var said = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Row().Matches(File.ReadAllText(Manifest)))
        {
            said[m.Groups["file"].Value] = m.Groups["source"].Value.Trim();
        }

        return said;
    }

    [GeneratedRegex(@"^\| `(?<file>[^`]+\.png)` \| (?<source>[^|]+) \|", RegexOptions.Multiline)]
    private static partial Regex Row();

    [GeneratedRegex(@"figures""\s*,\s*""screens""\s*,\s*""current""|figures/screens/current")]
    private static partial Regex WritesToTheFolder();

    /// <summary>Photographing the window and writing the picture out, which is the act this guards.</summary>
    [GeneratedRegex(@"CaptureRenderedFrame|\.Save\(")]
    private static partial Regex SavesARender();

    /// <summary>A path leaving this repository: a drive letter, a home directory, or a walk up past the root.</summary>
    [GeneratedRegex(@"""[A-Za-z]:[\\/][^""]*""|grouplab-range-[0-9-]+|2026-09-[0-9]{2}_[0-9]+")]
    private static partial Regex OutsideThisRepository();
}
