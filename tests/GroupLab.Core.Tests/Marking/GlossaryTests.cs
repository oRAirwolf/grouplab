using System.Text.RegularExpressions;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 154: every word a shooter may not know explains itself, from one list that the application and the website
/// both read. These hold the list: section 5.4's plain sentence with no other jargon and no symbols, one term to each word, articles that
/// exist, and a glossary document that says what the list says.
/// </summary>
public class GlossaryTests
{
    [Fact]
    public void EveryPlainSentenceIsPlain()
    {
        var failures = new List<string>();
        foreach (var term in Glossary.All)
        {
            // No symbols: letters, digits and ordinary punctuation only.
            if (Regex.Match(term.Plain, @"[^A-Za-z0-9 ,.;:'""()?\-]") is { Success: true } symbol)
            {
                failures.Add($"{term.Term}: the symbol {symbol.Value}");
            }

            // No capitalized jargon: a capital only where a sentence starts, and in GroupLab's own name.
            foreach (Match word in Regex.Matches(term.Plain, @"(?<![.;:?] |^)\b[A-Z][A-Za-z0-9']*"))
            {
                if (!word.Value.StartsWith("GroupLab", StringComparison.Ordinal))
                {
                    failures.Add($"{term.Term}: \"{word.Value}\" is capitalized in the middle of a sentence");
                }
            }

            int sentences = Regex.Matches(term.Plain, @"[.?](\s|$)").Count;
            if (sentences is < 2 or > 4)
            {
                failures.Add($"{term.Term}: {sentences} sentences, where two or three are asked for");
            }
        }

        Assert.True(failures.Count == 0, "a plain sentence written in jargon is the usual way a glossary fails:\n" + string.Join("\n", failures));
    }

    [Fact]
    public void EachWordBelongsToOneTermAndEachTermIsWhole()
    {
        Assert.True(Glossary.All.Count >= 40, "section 1.2's starting list alone is over forty terms");
        Assert.Equal(Glossary.All.Count, Glossary.All.Select(t => t.Term).Distinct(StringComparer.Ordinal).Count());
        var words = Glossary.All.SelectMany(t => t.Words.Select(w => (Word: w.ToLowerInvariant(), t.Term))).ToList();
        var shared = words.GroupBy(w => w.Word).Where(g => g.Select(x => x.Term).Distinct().Count() > 1).Select(g => g.Key).ToList();
        Assert.True(shared.Count == 0, "a word explained two ways: " + string.Join(", ", shared));
        Assert.All(Glossary.All, t =>
        {
            Assert.Matches("^[a-z0-9-]+$", t.Term);
            Assert.NotEmpty(t.Words);
            Assert.False(string.IsNullOrWhiteSpace(t.Name));
        });

        // Section 1.2's terms are all there, found by their own words.
        foreach (string word in new[] { "sigma", "rayleigh sigma", "radial standard deviation", "standard deviation", "mean radius", "cep", "extreme spread",
                     "group size", "moa", "mil", "confidence interval", "sample size", "degrees of freedom", "chi squared", "f test", "bias correction", "c4",
                     "dispersion", "point of aim", "point of impact", "zeroed", "zero correction", "scale", "calibration", "dpi", "perspective correction",
                     "keystone", "quarter point", "doubles", "review queue", "detection", "marker", "printed code", "bull", "subgroup", "load",
                     "chronograph string", "hit probability", "ballistic coefficient", "muzzle velocity", "velocity sd", "bullet drop", "wind deflection" })
        {
            Assert.True(Glossary.Find(word) is not null, $"\"{word}\" from entry 154 section 1.2 is not in the glossary");
        }
    }

    [Fact]
    public void AnArticleLinkGoesToAPublishedArticle()
    {
        foreach (var term in Glossary.All.Where(t => t.Article is not null))
        {
            var slug = Regex.Match(term.Article!, "^/research/([a-z0-9-]+)/$");
            Assert.True(slug.Success, $"{term.Term}: {term.Article} is not a research article's address");
            string article = Repo.PathTo("website", "research", slug.Groups[1].Value + ".md");
            Assert.True(File.Exists(article), $"{term.Term}: {term.Article} has no article");
            Assert.Matches(new Regex(@"^state:\s*published\s*$", RegexOptions.Multiline), File.ReadAllText(article));
        }
    }

    [Fact]
    public void TheGlossaryDocumentSaysWhatTheListSays()
    {
        string glossary = File.ReadAllText(Repo.PathTo("docs", "GLOSSARY.md")).ReplaceLineEndings("\n");
        foreach (var term in Glossary.All)
        {
            Assert.Contains($"<a id=\"{term.Term}\"></a>", glossary, StringComparison.Ordinal);
            Assert.True(glossary.Contains(term.Plain, StringComparison.Ordinal), $"docs/GLOSSARY.md does not say what the list says for {term.Term}; run grouplab glossary");
        }
    }

    [Fact]
    public void AWordIsFoundWholeAndTheLongerFormWins()
    {
        Assert.Equal("sigma", Glossary.Find("Sigma across 0.12 in")!.Term);
        Assert.Equal("cep", Glossary.Find("CEP 90")!.Term);
        Assert.Equal("mil", Glossary.Find("0.4 mil left")!.Term);
        Assert.Null(Glossary.Find("similar, milestones and bulldozers"));
        Assert.Equal("sigma", Glossary.Matches("the rayleigh sigma of it").Single().Term.Term);
    }
}
