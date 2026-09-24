using System.Text.Json;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 173: the target upload page opens at <c>grouplab.org/targets/</c> and the top bar says "Send a target". Alan:
/// "when will the target page be ready to use? Can you put a link to it at the top bar on the website?" The site build checks the same
/// things before it publishes; this holds them where a test run can see them, against the site as last built here.
/// </summary>
public class SendATargetTests
{
    private static bool Open()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("website", "api", "limits.json")));
        return doc.RootElement.GetProperty("open").GetBoolean();
    }

    private static string? Built(params string[] parts)
    {
        string path = Path.Combine([Repo.PathTo("website", "_site"), .. parts]);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    /// <summary>Section 3.1: the top bar offers "Send a target" only while the page is open, and the page is built whenever the link is.</summary>
    [Fact]
    public void TheTopBarOffersTheSendPageOnlyWhileItIsThere()
    {
        string? home = Built("index.html");
        if (home is null)
        {
            Assert.True(true, "skipped: website/_site has not been built here");
            return;
        }

        bool linked = home.Contains("<a href=\"/targets/\">Send a target</a>", StringComparison.Ordinal);
        Assert.Equal(Open(), linked);
        Assert.Equal(linked, Built("targets", "index.html") is not null);

        // The donor pack keeps its place in the footer either way.
        Assert.Contains("<a href=\"/shoot-a-target/\">Shoot a target</a>", home, StringComparison.Ordinal);
    }

    /// <summary>Section 3.2: the old path answers with a page that links to the new one, and does not move anybody on its own.</summary>
    [Fact]
    public void TheOldAddressLinksToTheNewOneAndMovesNobody()
    {
        string? moved = Built("shoot-a-target", "send", "index.html");
        if (moved is null || !Open())
        {
            Assert.True(true, "skipped: the send page is closed or the site has not been built here");
            return;
        }

        Assert.Contains("href=\"/targets/\"", moved, StringComparison.Ordinal);
        Assert.DoesNotContain("http-equiv=\"refresh\"", moved, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Section 3.3: the consent on the page is the one in limits.json, and the page says in plain words what happens to a photograph: rebuilt
    /// from its pixels, location data removed, kept until the developer has read it, then deleted from the server.
    /// </summary>
    [Fact]
    public void ThePageSaysWhatHappensToAPhotograph()
    {
        string? page = Built("targets", "index.html");
        if (page is null)
        {
            Assert.True(true, "skipped: the send page is closed or the site has not been built here");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(Repo.PathTo("website", "api", "limits.json")));
        string consent = System.Net.WebUtility.HtmlEncode(doc.RootElement.GetProperty("consentText").GetString()!);
        Assert.Contains(consent, page.Replace("&#x27;", "&#39;", StringComparison.Ordinal), StringComparison.Ordinal);
        foreach (string words in new[] { "rebuilt from its pixels", "Location, GPS and the date and time are not", "until the developer has read them", "deleted from the server" })
        {
            Assert.Contains(words, page, StringComparison.Ordinal);
        }
    }
}
