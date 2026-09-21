using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 120 section 9, answering question 28: there is no support address yet and Alan may register a domain later,
/// so the application keeps a clearly marked placeholder in one place and invents nothing. A made-up address in a shipped build would send
/// somebody who wants to help the project to a stranger's website, or to a page that does not exist.
/// <para>
/// This holds that: the one place stays empty until the planning session gives an address, and no other address that looks like a support or
/// donation page appears in anything a person receives. The planning documents may name candidates, because naming one is what they are for.
/// </para>
/// </summary>
public partial class SupportLinkTests
{
    /// <summary>What a support or donation address looks like. A new one of these is a new way to get it wrong.</summary>
    private static readonly string[] Shapes =
    [
        "ko-fi.com", "kofi.com", "patreon.com", "paypal.com", "paypal.me", "buymeacoffee.com",
        "github.com/sponsors", "opencollective.com", "liberapay.com", "donorbox.org", "gofundme.com",
    ];

    /// <summary>
    /// What a person receives: the application, the packaging, the front page and the two guides. The notes log and the questions file are
    /// where the planning session works out what the address should be, so a candidate written there is the process, not a shipped link.
    /// </summary>
    private static IEnumerable<string> Shipped()
    {
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("src"), "*.cs", SearchOption.AllDirectories))
        {
            if (!file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                yield return file;
            }
        }

        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("packaging"), "*", SearchOption.AllDirectories))
        {
            yield return file;
        }

        yield return Repo.PathTo("README.md");
        yield return Repo.PathTo("docs", "USER-GUIDE.md");
        yield return Repo.PathTo("docs", "TESTING-GUIDE.md");
    }

    [Fact]
    public void ThereIsNoSupportAddressAnywhereUntilOneIsGiven()
    {
        // The one place, still empty. When the planning session gives an address it goes here, and this line changes with it.
        string link = File.ReadAllText(Repo.PathTo("src", "GroupLab.App", "SupportLink.cs"));
        Assert.Contains("public static string? Address => null;", link, StringComparison.Ordinal);
        Assert.Contains("There is no support address yet", link, StringComparison.Ordinal);

        var found = new List<string>();
        foreach (string file in Shipped())
        {
            if (!File.Exists(file))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            foreach (string shape in Shapes.Where(s => text.Contains(s, StringComparison.OrdinalIgnoreCase)))
            {
                found.Add($"{Path.GetFileName(file)} names {shape}");
            }

            // Any other address with "support", "donate" or "sponsor" in its path, which is the shape a made-up one would take.
            foreach (Match m in SupportShapedUrl().Matches(text))
            {
                found.Add($"{Path.GetFileName(file)} names {m.Value}");
            }
        }

        Assert.True(found.Count == 0,
            "question 28 has no answer yet, so nothing a person receives may carry a support address. Put it in SupportLink and nowhere "
            + "else when there is one: " + string.Join("; ", found));
    }

    [GeneratedRegex(@"https?://[A-Za-z0-9._/-]*\b(support|donate|donation|sponsor)\b[A-Za-z0-9._/-]*", RegexOptions.IgnoreCase)]
    private static partial Regex SupportShapedUrl();
}
