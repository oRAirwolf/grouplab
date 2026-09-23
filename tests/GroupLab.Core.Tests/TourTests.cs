using System.Text.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 146: a tour of the application, one page per screen, at <c>/tour/</c>.
/// <para>
/// Alan: "I think there should be a separate page for screenshots of each section of the application that explains what is happening instead
/// of just a few screenshots on the main page." A handful of pictures on the home page shows that GroupLab exists. It does not show what
/// using it is like, and that is the question somebody has before they download an unknown program.
/// </para>
/// <para>
/// <b>Section 4.1 and 4.2 are the whole reason this file exists.</b> The tour and the weekly screenshot job of entry 144 section 4 are two
/// lists of the same screens, kept in two languages that cannot see each other. Two lists drift. The site build already refuses to build when
/// they disagree, and this says the same thing from the other side, so a screen added to the render walk fails here with the name of the page
/// somebody still has to write, rather than quietly never appearing on the site.
/// </para>
/// </summary>
public class TourTests
{
    private const string Size = "-1400x900.png";

    private static JsonElement Tour()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("website", "tour.json")));
        return doc.RootElement.Clone();
    }

    /// <summary>The screens the render walk actually produced, by the name the tour uses.</summary>
    private static HashSet<string> Rendered() =>
    [
        .. Directory.EnumerateFiles(Repo.PathTo("docs", "figures", "screens", "current"), "*-light" + Size)
            .Select(f => Path.GetFileName(f)!)
            .Select(f => f[..^("-light" + Size).Length]),
    ];

    private static List<string> Order() =>
        [.. Tour().GetProperty("order").EnumerateArray().Select(e => e.GetString()!)];

    /// <summary>Entry 146 section 4.2, both directions. Either one alone would let the tour go quietly stale.</summary>
    [Fact]
    public void TheTourAndTheScreenshotsAreTheSameListOfScreens()
    {
        var listed = Order().ToHashSet(StringComparer.Ordinal);
        var rendered = Rendered();

        var noPicture = listed.Except(rendered).Order(StringComparer.Ordinal).ToList();
        Assert.True(noPicture.Count == 0,
            "the tour has a page for these and the render walk produces no picture of them, so the page would show a broken image: "
            + string.Join(", ", noPicture));

        var noPage = rendered.Except(listed).Order(StringComparer.Ordinal).ToList();
        Assert.True(noPage.Count == 0,
            "these screens are rendered and have no tour page, so the tour is missing a screen and nothing on the site would say so. "
            + "Add them to website/tour.json: " + string.Join(", ", noPage));
    }

    /// <summary>Both themes at the size the site shows, because a tour page offers the reader whichever their browser asks for.</summary>
    [Fact]
    public void EveryTourScreenHasBothThemes()
    {
        string folder = Repo.PathTo("docs", "figures", "screens", "current");
        var missing = new List<string>();

        foreach (string key in Order())
        {
            foreach (string theme in new[] { "dark", "light" })
            {
                string name = $"{key}-{theme}{Size}";
                if (!File.Exists(Path.Combine(folder, name)))
                {
                    missing.Add(name);
                }
            }
        }

        Assert.True(missing.Count == 0, "the tour needs both themes of every screen and these are not rendered: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Entry 146 sections 2 and 3: every page carries the same things, and enough of them to be worth opening. A page with one part named and
    /// no steps is a screenshot with a caption, which is what the home page already was.
    /// </summary>
    [Fact]
    public void EveryTourPageSaysWhatTheScreenIsForAndWhatToDoOnIt()
    {
        var screens = Tour().GetProperty("screens");
        var wrong = new List<string>();

        foreach (string key in Order())
        {
            if (!screens.TryGetProperty(key, out var screen))
            {
                wrong.Add($"{key}: is in the order and has no entry");
                continue;
            }

            foreach (string field in new[] { "name", "blurb", "purpose", "fits" })
            {
                if (!screen.TryGetProperty(field, out var value) || string.IsNullOrWhiteSpace(value.GetString()))
                {
                    wrong.Add($"{key}: no {field}");
                }
            }

            int parts = screen.TryGetProperty("parts", out var p) ? p.GetArrayLength() : 0;
            if (parts < 3)
            {
                wrong.Add($"{key}: names {parts} parts, and a reader has to be able to match the list to the picture");
            }

            int steps = screen.TryGetProperty("steps", out var st) ? st.GetArrayLength() : 0;
            if (steps is < 2 or > 5)
            {
                wrong.Add($"{key}: has {steps} steps, and section 2.4 asks for two to five");
            }
        }

        Assert.True(wrong.Count == 0, "website/tour.json:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// Entry 146 section 3: written for somebody who has never opened GroupLab, and never for somebody who has read the code. The same rule
    /// as the release notes, for the same reason, and the same failure if nobody checks: the words drift back towards the code.
    /// </summary>
    [Fact]
    public void NoTourPageNamesAClassOrAFile()
    {
        var wrong = new List<string>();

        foreach (var screen in Tour().GetProperty("screens").EnumerateObject())
        {
            foreach (string text in Prose(screen.Value))
            {
                // "fits" carries the links between pages, so its own markup is not prose.
                string prose = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");

                var file = System.Text.RegularExpressions.Regex.Match(prose, "\\b[\\w-]+\\.(?:md|py|cs|json|ya?ml|html|css|js)\\b");
                if (file.Success)
                {
                    wrong.Add($"{screen.Name}: names a file, {file.Value}");
                }

                var cls = System.Text.RegularExpressions.Regex.Match(prose, "\\b(?!GroupLab\\b)[A-Z][a-z0-9]+(?:[A-Z][A-Za-z0-9]*)+\\b");
                if (cls.Success)
                {
                    wrong.Add($"{screen.Name}: names something in code style, {cls.Value}");
                }
            }
        }

        Assert.True(wrong.Count == 0, "the tour is read by people who have never seen this repository:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>Every piece of writing on one screen's page, flattened.</summary>
    private static IEnumerable<string> Prose(JsonElement screen)
    {
        foreach (string field in new[] { "name", "blurb", "purpose", "fits" })
        {
            if (screen.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
            {
                yield return value.GetString()!;
            }
        }

        if (screen.TryGetProperty("parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                foreach (var piece in part.EnumerateArray())
                {
                    yield return piece.GetString()!;
                }
            }
        }

        if (screen.TryGetProperty("steps", out var steps))
        {
            foreach (var step in steps.EnumerateArray())
            {
                yield return step.GetString()!;
            }
        }
    }

    /// <summary>The section has to be reachable, or it is a folder of pages nobody finds.</summary>
    [Fact]
    public void TheTourIsInTheNavigationAndOnTheHomePage()
    {
        string builder = File.ReadAllText(Repo.PathTo("website", "build.py"));
        Assert.Contains("(\"Tour\", \"/tour/\")", builder, StringComparison.Ordinal);

        string built = Repo.PathTo("website", "_site", "index.html");
        if (!File.Exists(built))
        {
            return;
        }

        string home = File.ReadAllText(built);
        Assert.Contains("href=\"/tour/\"", home, StringComparison.Ordinal);

        // Entry 146 section 5: once the tour exists the home page keeps one or two pictures at most and links to the tour rather than trying
        // to be it. Four screenshots and a link is not a link instead of a gallery.
        var shots = System.Text.RegularExpressions.Regex.Matches(home, "/assets/screens/([a-z-]+)-(?:dark|light)-1400x900")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(shots.Count <= 2, "the home page shows " + shots.Count + " screens and section 5 allows two: " + string.Join(", ", shots.Order(StringComparer.Ordinal)));
    }
}
