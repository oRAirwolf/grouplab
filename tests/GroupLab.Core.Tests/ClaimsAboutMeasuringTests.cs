using GroupLab.Core.Marking;
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
    /// The second is false the other way round: a uniformly mis-scaled print is read correctly, because the markers shrank with the sheet, and
    /// since entry 171 a scan corrects its sizes to real inches.
    /// </summary>
    private static readonly (string Phrase, string Why)[] Banned =
    [
        ("only measure a sheet it printed",
            "GroupLab measures any target once the scale is set by hand. docs/WHAT-CAN-BE-MEASURED.md."),
        // Entry 161 corrected entry 152: nothing applied the print scale, so a sheet printed small made every figure read LARGE, and these
        // sentences got the direction wrong.
        ("measures three percent small",
            "a sheet printed at 97 percent and photographed makes every group read about 3 percent large, not small, because the ruler shrank."),
        ("measures 3 percent small",
            "a sheet printed at 97 percent and photographed makes every group read about 3 percent large, not small, because the ruler shrank."),
        ("still measures correctly",
            "a shrunk sheet is read correctly bull by bull; a scan corrects its sizes to real inches and a photograph cannot. Entries 161 and 171."),
        // Entry 171 answered question 49: a scan now corrects every distance for the print scale, so what entry 161 truthfully said about
        // the code is no longer true of it.
        ("It does not correct the figures",
            "on a scan every distance is multiplied by the measured print scale, entry 171. docs/WHAT-CAN-BE-MEASURED.md."),
        ("no figure is corrected for it",
            "on a scan every distance is multiplied by the measured print scale, entry 171. docs/WHAT-CAN-BE-MEASURED.md."),
        ("never applies the print scale",
            "on a scan every distance is multiplied by the measured print scale, entry 171. docs/WHAT-CAN-BE-MEASURED.md."),
        ("nothing multiplies them by the print scale",
            "on a scan every distance is multiplied by the measured print scale, entry 171. docs/WHAT-CAN-BE-MEASURED.md."),

        // Entry 159 section 5.3: every false claim the audit found, so the exact wording cannot come back.
        ("twenty-two built-in",
            "there are twenty built-in sheets; the two tiled layouts are the same sheets on six pages. scripts/counts.py counts them."),
        ("offered for Windows only",
            "Windows, Linux and macOS builds are all published. docs/PLATFORM-SUPPORT.md."),
        ("only platform offered as a download",
            "Windows, Linux and macOS builds are all published. docs/PLATFORM-SUPPORT.md."),
        ("the macOS build is not offered for download",
            "both macOS builds are on the download page. docs/PLATFORM-SUPPORT.md."),
        ("nobody has ever run one",
            "the Apple silicon build has been run on one Mac, entry 166. docs/PLATFORM-SUPPORT.md."),
        ("labeled untested",
            "the Apple silicon build has been run on one Mac and says so, entry 166."),
        ("no mounted photograph set",
            "the mounted photograph gate was measured on 59 photographs of 2026-09-20, entry 130 section 6b."),
        ("can read them as one group",
            "pooling several sheets of one load is not built; what a pooled group's center means is question 34."),
        ("caliber is the single most useful",
            "naming the caliber can make the reading worse on a photograph, entry 161; it is not the single most useful thing."),
        ("calibre is the single most useful",
            "naming the caliber can make the reading worse on a photograph, entry 161; it is not the single most useful thing."),
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

                    // The claims register quotes every published sentence, the corrected ones included, which is what a register is for.
                    || rel is "docs/claims-backing.json" or "docs/CLAIMS.md"

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

    /// <summary>
    /// Entry 159 sections 4 and 5.1: the README's download table and the download page each say how far each Mac build has been run, and
    /// they had come apart, the README still calling the Apple silicon build untested after entry 166. Neither can be generated from the other
    /// without moving the table, so the two are held to the platform statement's facts here.
    /// </summary>
    [Fact]
    public void TheReadmeAndTheDownloadPageSayTheSameAboutEachMacBuild()
    {
        string readme = File.ReadAllText(Repo.PathTo("README.md"));
        string builder = File.ReadAllText(Repo.PathTo("website", "build.py"));
        string arm = readme.Split('\n').Single(l => l.StartsWith("| **[macOS, Apple silicon]", StringComparison.Ordinal));
        string intel = readme.Split('\n').Single(l => l.StartsWith("| **[macOS, Intel]", StringComparison.Ordinal));
        bool statementSaysRun = File.ReadAllText(Repo.PathTo("docs", "PLATFORM-SUPPORT.md")).Contains("the Apple silicon build has been run on one Mac", StringComparison.Ordinal);

        Assert.Equal(statementSaysRun, arm.Contains("Run on one real Mac", StringComparison.Ordinal));
        Assert.Equal(statementSaysRun, builder.Contains("<strong>Run on one real Mac.</strong>", StringComparison.Ordinal));
        Assert.Contains("Untested on a real Mac", intel, StringComparison.Ordinal);
        Assert.Contains("\"macOS, Intel\", \"grouplab-macos-x64.tar.gz\", \"For a Mac with an Intel processor. Self-contained, built on macOS, and tested by the suite on every change.\", [\"<strong>Untested on a real Mac.</strong>", builder, StringComparison.Ordinal);
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
    /// NOTES-FROM-PLANNING.md entry 171 section 1.3: why to print at actual size is one sentence, and the print screen, the statement of record
    /// and the tour all say it from <see cref="DetectionAdvice.WhyActualSize"/>. The print screen uses the constant itself; the two pages
    /// are text, so they are held to it here. The photograph line on the results panel is said word for word from the entry.
    /// </summary>
    [Fact]
    public void WhyToPrintAtActualSizeIsSaidFromOneSentence()
    {
        Assert.Contains("DetectionAdvice.WhyActualSize", File.ReadAllText(Path.Combine(Repo.Root, "src", "GroupLab.App", "PrintWindow.cs")), StringComparison.Ordinal);
        Assert.Contains(DetectionAdvice.WhyActualSize, File.ReadAllText(Path.Combine(Repo.PathTo("docs"), "WHAT-CAN-BE-MEASURED.md")), StringComparison.Ordinal);

        using var tour = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo.PathTo("website"), "tour.json")));
        Assert.Contains(DetectionAdvice.WhyActualSize, tour.RootElement.GetProperty("screens").GetProperty("print").GetProperty("purpose").GetString(), StringComparison.Ordinal);

        Assert.Equal("Measured in the sheet's own inches; if the sheet was not printed at actual size, the figures are off by the same percentage.", DetectionAdvice.SheetInches);
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
        Assert.Contains("A sheet printed at the wrong size is read correctly, and a scan measures it in real inches", text, StringComparison.Ordinal);
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
