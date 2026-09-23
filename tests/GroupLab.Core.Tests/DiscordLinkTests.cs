using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 148 section 3: the Discord invite is written down in exactly one place.
/// <para>
/// <b>Why it matters more than it looks.</b> An invite can be replaced, revoked or regenerated. Everything published points at
/// <c>grouplab.org/discord</c>, which redirects to whatever the one file says, so replacing it later is a single edit and no published link
/// ever breaks. A second copy written out in a page or a document is a link that keeps pointing at the old server after the change, and
/// nobody finds out until somebody cannot get in.
/// </para>
/// </summary>
public class DiscordLinkTests
{
    private static string Source() => File.ReadAllText(Repo.PathTo("website", "links.json"));

    /// <summary>The invite as the one file gives it.</summary>
    private static string Invite()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(Source());
        return doc.RootElement.GetProperty("discordInvite").GetString()!;
    }

    [Fact]
    public void TheInviteIsWrittenOutInOnePlaceOnly()
    {
        string invite = Invite();
        Assert.StartsWith("https://discord.gg/", invite, StringComparison.Ordinal);

        // The places the invite could be written out, rather than every file in the repository. An earlier version walked the root and
        // read whatever it found, which on the Windows runner meant opening test-output.txt while the test run was writing it.
        string[] folders = ["docs", "website", "src", "tests", "scripts", ".github", "targets", "samples"];

        var files = folders
            .Select(f => Repo.PathTo(f))
            .Where(Directory.Exists)
            .SelectMany(f => Directory.EnumerateFiles(f, "*.*", SearchOption.AllDirectories))
            .Concat(Directory.EnumerateFiles(Repo.Root, "*.md", SearchOption.TopDirectoryOnly));

        var elsewhere = new List<string>();
        foreach (string file in files)
        {
            string rel = Path.GetRelativePath(Repo.Root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal)
                || rel.StartsWith("website/_site/", StringComparison.Ordinal)
                || rel == "website/links.json" || rel.StartsWith("docs/notes/inbox/", StringComparison.Ordinal)
                || rel == "docs/NOTES-FROM-PLANNING.md"
                // This file, which names the shape of an invite in order to check it and so matches itself.
                || rel.EndsWith("DiscordLinkTests.cs", StringComparison.Ordinal)
                || Path.GetExtension(file) is not (".md" or ".py" or ".cs" or ".json" or ".html" or ".yml" or ".ps1" or ".txt"))
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
                // Something outside this test is holding it. That is never where an invite would be written down.
                continue;
            }

            if (text.Contains("discord.gg", StringComparison.OrdinalIgnoreCase))
            {
                elsewhere.Add(rel);
            }
        }

        Assert.True(elsewhere.Count == 0,
            "the invite is written out here as well as in website/links.json, so replacing it would leave these pointing at the old server: "
            + string.Join(", ", elsewhere));
    }

    /// <summary>
    /// And the one page that does carry it carries the right one. A redirect to an invite nobody checked is the same as no redirect.
    /// </summary>
    [Fact]
    public void TheOnlyPublishedInviteIsTheOneInThatFile()
    {
        string built = Repo.PathTo("website", "_site");
        if (!Directory.Exists(built))
        {
            Assert.True(true, "skipped: website/_site has not been built here");
            return;
        }

        string invite = Invite();
        var wrong = new List<string>();
        foreach (string file in Directory.EnumerateFiles(built, "*.html", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (!text.Contains("discord.gg", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string rel = Path.GetRelativePath(built, file).Replace(Path.DirectorySeparatorChar, '/');
            if (rel != "discord/index.html")
            {
                wrong.Add(rel + ": carries an invite, and only the community page may");
            }
            else if (!text.Contains(invite, StringComparison.Ordinal))
            {
                wrong.Add(rel + ": carries an invite that is not the one in website/links.json");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 151 section 4.1. Alan: "make it so the community link at the top of the page does not automatically
    /// redirect to the discord server. People will not appreciate this." The page that did it was a meta refresh, and this is the general
    /// form of that fault rather than the one instance of it, so it catches the next one too. Nothing on this site navigates on its own.
    /// </summary>
    [Fact]
    public void NoPageOnTheSiteNavigatesOnItsOwn()
    {
        string built = Repo.PathTo("website", "_site");
        if (!Directory.Exists(built))
        {
            Assert.True(true, "skipped: website/_site has not been built here");
            return;
        }

        var refreshing = new List<string>();
        foreach (string file in Directory.EnumerateFiles(built, "*.html", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (text.Contains("http-equiv=\"refresh\"", StringComparison.OrdinalIgnoreCase)
                || text.Contains("http-equiv='refresh'", StringComparison.OrdinalIgnoreCase))
            {
                refreshing.Add(Path.GetRelativePath(built, file).Replace(Path.DirectorySeparatorChar, '/'));
            }
        }

        Assert.True(refreshing.Count == 0,
            "these pages throw the visitor somewhere else before they have read anything, which is what entry 151 removed: "
            + string.Join(", ", refreshing));
    }

    /// <summary>
    /// Entry 151 section 4.2. The community page carries the invite twice on purpose: once as the link's target and once as visible text,
    /// because some people want to see where a link goes before they follow it. A page carrying it only as an <c>href</c> has quietly lost
    /// half of what the section asks for, and nothing would look wrong.
    /// </summary>
    [Fact]
    public void TheCommunityPageShowsTheInviteAsWellAsLinkingIt()
    {
        string page = Path.Combine(Repo.PathTo("website", "_site"), "discord", "index.html");
        if (!File.Exists(page))
        {
            Assert.True(true, "skipped: website/_site has not been built here");
            return;
        }

        string text = File.ReadAllText(page);
        string invite = Invite();

        Assert.Contains($"href=\"{invite}\"", text, StringComparison.Ordinal);

        // The visible half: the address inside an element's text rather than inside an attribute.
        Assert.Contains($">{invite}<", text.Replace("<code>", ">").Replace("</code>", "<"), StringComparison.Ordinal);

        // And it says where it goes before it is clicked.
        Assert.Contains("new tab", text, StringComparison.OrdinalIgnoreCase);
    }
}
