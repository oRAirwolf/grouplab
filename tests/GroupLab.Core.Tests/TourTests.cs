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
    /// <summary>Every rendered screen, less the second pictures a stop shows beside its own (entry 242: the optic stop's 4x set).</summary>
    private static HashSet<string> Rendered()
    {
        var more = Tour().GetProperty("screens").EnumerateObject()
            .SelectMany(s => s.Value.TryGetProperty("moreShots", out var shots) ? shots.EnumerateArray().Select(x => x[0].GetString()!) : [])
            .ToHashSet(StringComparer.Ordinal);
        return
        [
            .. Directory.EnumerateFiles(Repo.PathTo("docs", "figures", "screens", "current"), "*-light" + Size)
                .Select(f => Path.GetFileName(f)!)
                .Select(f => f[..^("-light" + Size).Length])
                .Where(k => !more.Contains(k)),
        ];
    }

    private static List<string> Order() =>
        [.. Tour().GetProperty("order").EnumerateArray().Select(e => e.GetString()!)];

    /// <summary>The stops the desktop has: every one but those only the phone has (entry 249), which have no desktop render to show.</summary>
    private static List<string> DesktopOrder()
    {
        var screens = Tour().GetProperty("screens");
        return [.. Order().Where(k => !(screens.GetProperty(k).TryGetProperty("platform", out var p) && p.GetString() == "mobile"))];
    }

    /// <summary>Entry 146 section 4.2, both directions. Either one alone would let the tour go quietly stale.</summary>
    [Fact]
    public void TheTourAndTheScreenshotsAreTheSameListOfScreens()
    {
        var listed = DesktopOrder().ToHashSet(StringComparer.Ordinal);
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

        foreach (string key in DesktopOrder())
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

    /// <summary>
    /// Entry 249: every stop has a Mobile side, a real screenshot from the phone with its own parts and steps written for touch, or words
    /// saying plainly that it is on the desktop only. A stop the phone alone has says so on its Desktop side.
    /// </summary>
    [Fact]
    public void EveryStopHasAMobileSide()
    {
        var screens = Tour().GetProperty("screens");
        string phone = Repo.PathTo("docs", "figures", "screens", "phone");
        var wrong = new List<string>();

        foreach (string key in Order())
        {
            var screen = screens.GetProperty(key);
            bool phoneOnly = screen.TryGetProperty("platform", out var platform) && platform.GetString() == "mobile";
            if (phoneOnly && !(screen.TryGetProperty("desktopWords", out var words) && words.GetString()!.Length > 0))
            {
                wrong.Add($"{key}: is on the phone only and its Desktop side does not say so");
            }

            if (!screen.TryGetProperty("mobile", out var mobile))
            {
                wrong.Add($"{key}: has no Mobile side");
                continue;
            }

            if (mobile.TryGetProperty("shot", out var shot))
            {
                string name = shot.GetString()!;
                bool both = File.Exists(Path.Combine(phone, name + "-light.png")) && File.Exists(Path.Combine(phone, name + "-dark.png"));
                if (!both && !File.Exists(Path.Combine(phone, name + ".png")))
                {
                    wrong.Add($"{key}: its phone screenshot {name} is not in docs/figures/screens/phone");
                }

                int parts = mobile.TryGetProperty("parts", out var p) ? p.GetArrayLength() : phoneOnly ? 3 : 0;
                int steps = mobile.TryGetProperty("steps", out var s) ? s.GetArrayLength() : phoneOnly ? 2 : 0;
                if (parts < 3 || steps is < 2 or > 5)
                {
                    wrong.Add($"{key}: its Mobile side has {parts} parts and {steps} steps of its own");
                }
            }
            // Entry 275: a stop the phone has but has not been photographed on says so in words until the next device sitting.
            else if (!(mobile.TryGetProperty("only", out var only) && only.GetString() == "desktop" && mobile.TryGetProperty("words", out _))
                && !(mobile.TryGetProperty("pending", out var pending) && pending.GetBoolean() && mobile.TryGetProperty("words", out _)))
            {
                wrong.Add($"{key}: its Mobile side has neither a phone screenshot nor words saying it is on the desktop only");
            }
        }

        Assert.True(wrong.Count == 0, "website/tour.json:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// Mouse words on the Mobile side are the wrong instructions for somebody holding a phone (entry 249 item 5): tap, not click.
    /// </summary>
    [Fact]
    public void TheMobileStepsAreWrittenForTouch()
    {
        var wrong = new List<string>();
        foreach (var screen in Tour().GetProperty("screens").EnumerateObject())
        {
            bool phoneOnly = screen.Value.TryGetProperty("platform", out var platform) && platform.GetString() == "mobile";
            var side = phoneOnly ? screen.Value : screen.Value.TryGetProperty("mobile", out var m) ? m : default;
            if (side.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (string text in Prose(side))
            {
                var mouse = System.Text.RegularExpressions.Regex.Match(text, "\\b(click|clicks|clicking|double[- ]click|right[- ]click|mouse|hover|scroll wheel)\\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // A scope's clicks are a turret's, not a mouse's.
                if (mouse.Success && !System.Text.RegularExpressions.Regex.IsMatch(text, "clicks? (up|down|left|right|value)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    wrong.Add($"{screen.Name}: \"{mouse.Value}\" on the Mobile side");
                }
            }
        }

        Assert.True(wrong.Count == 0, "the Mobile side is read by somebody holding a phone:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>Every piece of writing on one screen's page, flattened, the Mobile side's included.</summary>
    private static IEnumerable<string> Prose(JsonElement screen)
    {
        foreach (string field in new[] { "name", "blurb", "purpose", "fits", "withoutASheet", "desktopWords", "caption", "words", "note" })
        {
            if (screen.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
            {
                yield return value.GetString()!;
            }
        }

        if (screen.TryGetProperty("mobile", out var mobile))
        {
            foreach (string text in Prose(mobile))
            {
                yield return text;
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
