using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 152: two published claims about what GroupLab can measure were wrong, and one of them was contradicted by a
/// research article on the same website.
/// <para>
/// <b>Why that is the worst kind of error.</b> A reader who read both the tour and the article learned that the site cannot be trusted,
/// rather than which sentence was right. A single wrong sentence is a mistake; two published sentences that disagree is a statement about
/// how carefully the whole site was written.
/// </para>
/// <para>
/// <c>docs/WHAT-CAN-BE-MEASURED.md</c> is the one source, and these hold everything else to it: the banned phrasings, which is the
/// mechanism entry 145 used for "nothing in this build changes", and a check that the tour and the articles point at the source rather than
/// each writing their own version of it.
/// </para>
/// </summary>
public class ClaimsAboutMeasuringTests
{
    /// <summary>
    /// The two claims themselves, and the words they were made of. The first is false: any target can be measured once the scale is set.
    /// The second is false the other way round: a uniformly mis-scaled print is corrected, because the markers shrank with the sheet.
    /// </summary>
    private static readonly (string Phrase, string Why)[] Banned =
    [
        ("only measure a sheet it printed",
            "GroupLab measures any target once the scale is set by hand. docs/WHAT-CAN-BE-MEASURED.md."),
        // Entry 161 corrected entry 152: GroupLab measures in the sheet's own inches and never applies the print scale, so a sheet printed
        // small makes every figure read LARGE. The sentences banned here are the ones that said otherwise, including the ones entry 152
        // wrote, and the one that got the direction wrong.
        ("measures three percent small",
            "a sheet printed at 97 percent makes every group read about 3 percent large, not small, because the ruler shrank."),
        ("measures 3 percent small",
            "a sheet printed at 97 percent makes every group read about 3 percent large, not small, because the ruler shrank."),
        ("still measures correctly",
            "a shrunk sheet is read correctly bull by bull, and every distance on it is in the sheet's own inches. Entry 161."),
        ("corrects every figure",
            "the print scale is measured on a scan and reported, and no figure is corrected for it. Entry 161."),
        ("The measurements are corrected for it",
            "the print scale is measured on a scan and reported, and no figure is corrected for it. Entry 161."),
    ];

    /// <summary>
    /// Where a claim like this can be published from. The inbox and the two logs are excluded because they quote the wrong sentences on
    /// purpose: a log that cannot record what was wrong is not a log.
    /// </summary>
    private static IEnumerable<string> Published()
    {
        string[] folders = ["website", "src", "docs"];
        foreach (string folder in folders)
        {
            string root = Repo.PathTo(folder);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(Repo.Root, file).Replace(Path.DirectorySeparatorChar, '/');
                if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal)
                    || rel.StartsWith("website/_site/", StringComparison.Ordinal)
                    || rel.StartsWith("docs/notes/", StringComparison.Ordinal)
                    || rel is "docs/NOTES-FROM-PLANNING.md" or "docs/PHASE1-RESULTS.md" or "docs/QUESTIONS-FOR-PLANNING.md"

                    // What each build said when it was published. Nightly 93 carries entry 152's false note that a shrunk sheet measures
                    // correctly; a published release is not edited to hide a mistake, and the correction is entry 161's note in the next.
                    || rel == "docs/RELEASE-NOTES.md")
                {
                    continue;
                }

                if (Path.GetExtension(file) is ".md" or ".json" or ".cs" or ".py" or ".axaml" or ".html")
                {
                    yield return file;
                }
            }
        }

        yield return Path.Combine(Repo.Root, "README.md");
    }

    [Fact]
    public void NeitherClaimIsPublishedAnywhere()
    {
        var found = new List<string>();
        foreach (string file in Published())
        {
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (IOException)
            {
                continue;
            }

            string rel = Path.GetRelativePath(Repo.Root, file).Replace(Path.DirectorySeparatorChar, '/');
            foreach (var (phrase, why) in Banned)
            {
                if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add($"{rel}: \"{phrase}\" is not true, and {why}");
                }
            }
        }

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>
    /// Entry 152 section 5.2, the cheap version and the right one: one place says what GroupLab can measure, and everything else points at
    /// it. Two carefully written copies of a statement this specific drift, and then there are two versions of what the project claims with
    /// no way to tell which is the real one. That is exactly how these two claims came to disagree with a research article.
    /// </summary>
    [Fact]
    public void ThereIsOneSourceAndTheTourAndTheArticlePointAtIt()
    {
        string source = Path.Combine(Repo.PathTo("docs"), "WHAT-CAN-BE-MEASURED.md");
        Assert.True(File.Exists(source), "docs/WHAT-CAN-BE-MEASURED.md is the one source for what GroupLab can measure, and it is not here.");

        string text = File.ReadAllText(source);
        Assert.Contains("A sheet printed at the wrong size is read correctly, and measured in its own inches", text, StringComparison.Ordinal);
        Assert.Contains("Any target can be measured once the scale is set", text, StringComparison.Ordinal);

        string build = File.ReadAllText(Path.Combine(Repo.PathTo("website"), "build.py"));
        Assert.Contains("WHAT-CAN-BE-MEASURED.md", build, StringComparison.Ordinal);
        Assert.Contains("/what-can-be-measured/", build, StringComparison.Ordinal);

        string article = Path.Combine(Repo.PathTo("website"), "research", "printer-true-size.md");
        if (File.Exists(article))
        {
            Assert.Contains("/what-can-be-measured/", File.ReadAllText(article), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Entry 152 section 4: every tour page says what that screen does with a target GroupLab did not print. On several of them the answer
    /// is "no difference", and that line is worth as much as the others, because the question a reader actually has is whether the whole
    /// application is useless to them without a printed sheet.
    /// </summary>
    [Fact]
    public void EveryTourScreenSaysWhatItDoesWithoutAGroupLabSheet()
    {
        string path = Path.Combine(Repo.PathTo("website"), "tour.json");
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var screens = doc.RootElement.GetProperty("screens");

        var missing = new List<string>();
        foreach (var screen in screens.EnumerateObject())
        {
            if (!screen.Value.TryGetProperty("withoutASheet", out var line) || string.IsNullOrWhiteSpace(line.GetString()))
            {
                missing.Add(screen.Name);
            }
        }

        Assert.True(missing.Count == 0,
            "these tour screens do not say what they do with a target GroupLab did not print: " + string.Join(", ", missing));
    }
}
