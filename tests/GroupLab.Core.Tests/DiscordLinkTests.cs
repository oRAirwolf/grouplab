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

        var elsewhere = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.Root, "*.*", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(Repo.Root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (rel.StartsWith(".git/", StringComparison.Ordinal) || rel.Contains("/bin/", StringComparison.Ordinal)
                || rel.Contains("/obj/", StringComparison.Ordinal) || rel.StartsWith("website/_site/", StringComparison.Ordinal)
                || rel == "website/links.json" || rel.StartsWith("docs/notes/inbox/", StringComparison.Ordinal)
                // This file, which names the shape of an invite in order to check it and so matches itself.
                || rel.EndsWith("DiscordLinkTests.cs", StringComparison.Ordinal)
                || rel == "docs/NOTES-FROM-PLANNING.md"
                || Path.GetExtension(file) is not (".md" or ".py" or ".cs" or ".json" or ".html" or ".yml" or ".ps1" or ".txt"))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains("discord.gg", StringComparison.OrdinalIgnoreCase))
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
                wrong.Add(rel + ": carries an invite, and only the redirect page may");
            }
            else if (!text.Contains(invite, StringComparison.Ordinal))
            {
                wrong.Add(rel + ": carries an invite that is not the one in website/links.json");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }
}
