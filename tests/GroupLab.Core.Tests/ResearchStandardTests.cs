using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 153: the standard every research article is held to.
/// <para>
/// Three of these are about the articles and one is about a number. They are together because they are one entry, and because the number is
/// the reason the entry exists at all: an article published a rimfire diameter that was 0.9 percent wrong, and nothing anywhere would have
/// caught it.
/// </para>
/// </summary>
public class ResearchStandardTests
{
    /// <summary>
    /// Entry 153 section 1.4: the developer's name appears nowhere under <c>website/research/</c>. Alan: "For all of the research documents,
    /// I would prefer if my name is not mentioned. Just say the author or developer."
    /// <para>
    /// <b>Scoped to that directory on purpose.</b> His name legitimately belongs in the licence, in the commit history and in
    /// <c>samples/PROVENANCE.md</c>, where it sits under a consent record. A test that banned it everywhere would be wrong, would fail on
    /// work nobody should change, and would eventually be switched off, taking this with it.
    /// </para>
    /// </summary>
    [Fact]
    public void TheDevelopersNameIsNotInAnyResearchFile()
    {
        string root = Repo.PathTo("website", "research");
        Assert.True(Directory.Exists(root), "website/research is where the articles live, and it is not here.");

        var found = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is not (".md" or ".py" or ".csv" or ".json" or ".txt"))
            {
                continue;
            }

            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (IOException)
            {
                continue;
            }

            if (text.Contains("Alan", StringComparison.Ordinal))
            {
                found.Add(Path.GetRelativePath(Repo.Root, file).Replace(Path.DirectorySeparatorChar, '/'));
            }
        }

        Assert.True(found.Count == 0,
            "the developer is named in these research files, and entry 153 section 1 asks that he is not. Say \"the developer\": "
            + string.Join(", ", found));
    }

    /// <summary>
    /// Entry 153 section 1.2: one byline, used everywhere, rather than a variant invented per page.
    /// </summary>
    [Fact]
    public void EveryArticlePageCarriesTheOneByline()
    {
        string build = File.ReadAllText(Path.Combine(Repo.PathTo("website"), "build.py"));
        Assert.Contains("GroupLab project. Researched and written with Claude. Testing and data collection by the developer.",
            build, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 153 section 4.5, and this is the one that had to be a test rather than a correction.
    /// <para>
    /// <b>A rimfire 22 is nominally 0.222 in.</b> 0.224 belongs to the centrefire 22 cartridges, 5.56x45 and 22 ARC, and 0.2215 is 5.45x39.
    /// The pick list held the other two and not this one, so the most commonly shot cartridge in the world had nothing in it to choose, and
    /// the nearest thing was 0.9 percent too wide. Being close enough to look right is exactly why it survived this long.
    /// </para>
    /// </summary>
    [Fact]
    public void ThePickListKnowsARimfireFromACentrefire()
    {
        Assert.Contains(0.222, Calibre.Diameters);
        Assert.Contains(0.224, Calibre.Diameters);
        Assert.Contains(0.2215, Calibre.Diameters);

        // Three different cartridges that happen to sit within a thousandth of each other. A "tidy up" that merged them would be a
        // measurable error in every edge-to-edge figure and every oversize flag on a rimfire sheet.
        Assert.Equal(3, Calibre.Diameters.Count(d => d is > 0.21 and < 0.23));
    }

    /// <summary>
    /// And nothing published treats a rimfire 22 as 0.224. The two research articles that did were recomputed rather than edited, because
    /// the ratios in them are measured hole divided by bullet diameter and the divisor moved.
    /// </summary>
    [Fact]
    public void NoArticleCallsARimfireTwoTwoFour()
    {
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("website", "research"), "*.md", SearchOption.AllDirectories))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                bool rimfire = lines[i].Contains("22 LR", StringComparison.OrdinalIgnoreCase)
                    || lines[i].Contains("rimfire", StringComparison.OrdinalIgnoreCase);
                Assert.False(rimfire && lines[i].Contains("0.224", StringComparison.Ordinal),
                    $"{Path.GetFileName(file)} line {i + 1} calls a rimfire 22 a 0.224: it is nominally 0.222, and 0.224 is the "
                    + "centrefire 22 of 5.56x45 and 22 ARC. Entry 153 section 4.");
            }
        }
    }
}
