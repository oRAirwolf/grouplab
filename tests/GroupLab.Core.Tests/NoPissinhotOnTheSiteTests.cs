using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// Alan's instruction of 2026-09-23: no link to the old site appears anywhere on grouplab.org.
/// <para>
/// <b>Why a test and not a careful edit.</b> The link lived in one constant and reached four places, one of them the footer, which put it
/// on every page of the site. Removing it by hand is easy and keeping it removed is not: the next person who wants a "send us a target"
/// button has an obvious address to reach for, and nothing between them and publishing it.
/// </para>
/// <para>
/// This checks the built site rather than the source, because the built site is what people read. It also checks the source that produces
/// it, so the failure names the line to change rather than a generated file nobody edits.
/// </para>
/// <para>
/// <b>What it does not cover.</b> The other site itself and the redirect on it are not this repository's business and are not touched.
/// <c>website/server/install.py</c> names it once, in a sentence promising the installer never goes near it, and a promise not to touch
/// something is not a link to it.
/// </para>
/// </summary>
public class NoPissinhotOnTheSiteTests
{
    private const string TheOldSite = "pissinhot";

    /// <summary>The one file allowed to name it: an installer saying it never goes near it.</summary>
    private static readonly string[] Allowed = ["install.py"];

    [Fact]
    public void NothingInTheBuiltSiteNamesTheOldSite()
    {
        string built = Repo.PathTo("website", "_site");
        if (!Directory.Exists(built))
        {
            Assert.True(true, "skipped: website/_site has not been built on this machine, so there was nothing to check");
            return;
        }

        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(built, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is not (".html" or ".css" or ".js" or ".json" or ".txt" or ".xml" or ".svg"))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains(TheOldSite, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(Path.GetRelativePath(built, file));
            }
        }

        Assert.True(found.Count == 0,
            $"the built site names {TheOldSite} on these pages, and it must not appear anywhere on grouplab.org:\n  "
            + string.Join("\n  ", found.Take(20)));
    }

    /// <summary>
    /// And the source, so a failure says where to fix it. A generated page is a symptom; the template is the cause.
    /// </summary>
    [Fact]
    public void NothingThatBuildsTheSiteNamesTheOldSite()
    {
        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("website"), "*.*", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (file.Contains($"{Path.DirectorySeparatorChar}_site{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Allowed.Contains(name, StringComparer.OrdinalIgnoreCase)
                || Path.GetExtension(file) is not (".py" or ".html" or ".md" or ".css" or ".js"))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains(TheOldSite, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(Path.GetRelativePath(Repo.PathTo("website"), file));
            }
        }

        Assert.True(found.Count == 0,
            $"these files under website/ name {TheOldSite}, so the built site would carry it:\n  " + string.Join("\n  ", found.Take(20)));
    }
}
