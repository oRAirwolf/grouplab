using System.Text.RegularExpressions;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 126 section 1, which answers question 28 for good: grouplab.org is live, the support page is
/// <c>https://grouplab.org/support/</c> and <c>support@grouplab.org</c> forwards to Alan.
/// <para>
/// Entry 120 section 9 held that <b>no</b> support address appeared anywhere, because there was none and a made-up one in a shipped build
/// would send somebody who wants to help the project to a stranger's website. That risk did not go away when the domain went live: it
/// changed shape. A stale address, a typo, or a second address somewhere nobody looks is the same harm. So this now holds exactly these two
/// and fails on any other support address or domain.
/// </para>
/// </summary>
public partial class SupportLinkTests
{
    /// <summary>The two addresses, and the only two, that anything a person receives may carry.</summary>
    private const string ThePage = "https://grouplab.org/support/";

    private const string TheEmail = "support@grouplab.org";

    /// <summary>What a support or donation address looks like somewhere else. A new one of these is a new way to get it wrong.</summary>
    private static readonly string[] Shapes =
    [
        "ko-fi.com", "kofi.com", "patreon.com", "paypal.com", "paypal.me", "buymeacoffee.com",
        "github.com/sponsors", "opencollective.com", "liberapay.com", "donorbox.org", "gofundme.com",
    ];

    /// <summary>
    /// What a person receives: the application, the packaging, the front page and the two guides. The notes log and the questions file are
    /// where the planning session works out what an address should be, so a candidate written there is the process, not a shipped link.
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
        yield return Repo.PathTo("docs", "UPDATES.md");
    }

    [Fact]
    public void TheSupportAddressIsWrittenInExactlyOnePlace()
    {
        string link = File.ReadAllText(Repo.PathTo("src", "GroupLab.App", "SupportLink.cs"));
        Assert.Contains($"Address => \"{ThePage}\"", link, StringComparison.Ordinal);
        Assert.Contains($"Email = \"{TheEmail}\"", link, StringComparison.Ordinal);

        // Every other source file must get it from there rather than writing it again.
        var wrotItThemselves = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("src"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.GetFileName(file) == "SupportLink.cs")
            {
                continue;
            }

            string text = File.ReadAllText(file);
            if (text.Contains(ThePage, StringComparison.OrdinalIgnoreCase) || text.Contains(TheEmail, StringComparison.OrdinalIgnoreCase))
            {
                wrotItThemselves.Add(Path.GetFileName(file));
            }
        }

        Assert.True(wrotItThemselves.Count == 0,
            "the support address belongs to SupportLink and nowhere else, so that changing it is changing one line: "
            + string.Join("; ", wrotItThemselves));
    }

    [Fact]
    public void NothingAPersonReceivesCarriesAnyOtherSupportAddress()
    {
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

            // Any other address with "support", "donate" or "sponsor" in it, which is the shape a stale or invented one would take.
            foreach (Match m in SupportShapedUrl().Matches(text))
            {
                if (!string.Equals(m.Value.TrimEnd('.', ',', ')'), ThePage, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(m.Value.TrimEnd('.', ',', ')', '/'), ThePage.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                {
                    found.Add($"{Path.GetFileName(file)} names {m.Value}");
                }
            }

            // And any other address at an address that is not GroupLab's own.
            foreach (Match m in SupportShapedEmail().Matches(text))
            {
                if (!string.Equals(m.Value, TheEmail, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add($"{Path.GetFileName(file)} names {m.Value}");
                }
            }
        }

        Assert.True(found.Count == 0,
            $"entry 126 gives exactly two support addresses, {ThePage} and {TheEmail}. Anything else in something a person receives is "
            + "a stale address, a typo or somebody else's website: " + string.Join("; ", found));
    }

    /// <summary>
    /// The site is built from this repository, so a domain that is not grouplab.org pretending to be it would be published. Only the real
    /// one may appear.
    /// </summary>
    [Fact]
    public void TheOnlyGroupLabDomainIsTheRealOne()
    {
        var found = new List<string>();
        foreach (string file in Shipped())
        {
            if (!File.Exists(file))
            {
                continue;
            }

            foreach (Match m in GroupLabDomain().Matches(File.ReadAllText(file)))
            {
                if (!string.Equals(m.Value, "grouplab.org", StringComparison.OrdinalIgnoreCase))
                {
                    found.Add($"{Path.GetFileName(file)} names {m.Value}");
                }
            }
        }

        Assert.True(found.Count == 0, "grouplab.org is the project's domain and nothing else is: " + string.Join("; ", found));
    }

    [GeneratedRegex(@"https?://[A-Za-z0-9._/-]*\b(support|donate|donation|sponsor)\b[A-Za-z0-9._/-]*", RegexOptions.IgnoreCase)]
    private static partial Regex SupportShapedUrl();

    [GeneratedRegex(@"\b(support|donate|donations|sponsor|help)@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex SupportShapedEmail();

    /// <summary>
    /// A GroupLab domain, and only a domain. Case matters: the namespaces are <c>GroupLab.App</c> and <c>GroupLab.Core</c>, and a domain is
    /// written in lower case, so matching case-sensitively keeps every namespace out of it. The extension is a list rather than "any
    /// letters", because that is what separates grouplab.com from grouplab.json and grouplab.db.
    /// </summary>
    [GeneratedRegex(@"\bgrouplab\.(?:org|com|net|io|app|dev|co|uk|info|site|shop|online|store)\b")]
    private static partial Regex GroupLabDomain();
}
