using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 142 section 5: an article that quotes one of GroupLab's own constants stops being true the day somebody
/// changes the constant, and nothing about the article says so.
/// <para>
/// <b>This is the failure mode of every "how it works" page ever written.</b> It is correct on the day, nobody reads it again, the code moves
/// underneath it, and a year later the site is explaining something the software stopped doing. The article is published and the reader has no
/// way at all to know. So the numbers an article states about GroupLab's own behaviour are held against the code that produces them.
/// </para>
/// <para>
/// Only the constants are held here, not the prose. A sentence can be rewritten; a number is either the one in the code or it is wrong.
/// </para>
/// </summary>
public class ResearchArticleTests
{
    private static string Folder => Repo.PathTo("website", "research");

    private static IEnumerable<(string Name, string Text)> Articles() =>
        Directory.Exists(Folder)
            ? Directory.EnumerateFiles(Folder, "*.md").Select(f => (Path.GetFileName(f), File.ReadAllText(f)))
            : [];

    /// <summary>
    /// Every number an article states about how GroupLab reads a sheet is the number the code uses. Each one is named here with where it
    /// comes from, so a constant that moves fails with the article and the code side by side.
    /// </summary>
    [Fact]
    public void AnArticleNeverQuotesAConstantTheCodeNoLongerUses()
    {
        var quoted = new (string Phrase, string What, double Value)[]
        {
            ("about 0.94 of the bullet", "the hole to calibre ratio", AutomaticMarking.HoleToCalibre),
            ("twelve or more", "how many marks let a sheet outrank a stated calibre", new GroupLab.Core.Detection.RenderDifferenceOptions().MarksToOutrankACalibre),
        };

        var wrong = new List<string>();
        foreach (var (name, text) in Articles())
        {
            foreach (var (phrase, what, value) in quoted)
            {
                if (!text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // The phrase is a rounded English form, so what is checked is that the code's value still rounds to it.
                bool still = phrase switch
                {
                    "about 0.94 of the bullet" => Math.Abs(value - 0.94) < 0.01,
                    "twelve or more" => Math.Abs(value - 12) < 0.5,
                    _ => false,
                };

                if (!still)
                {
                    wrong.Add($"{name} says \"{phrase}\" and {what} is now {value.ToString(CultureInfo.InvariantCulture)}");
                }
            }
        }

        Assert.True(wrong.Count == 0, "an article states a number the code no longer uses:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>
    /// The rules that hold for every word on the site hold here too, checked before a build rather than by it: no em dash, and none of the
    /// terms that mean nothing or claim too much.
    /// </summary>
    [Fact]
    public void NoArticleUsesAnEmDashOrABannedTerm()
    {
        string[] banned = ["—", "–", "ontarget", "harmonic", "barrel time", "velocity node", "accuracy node"];
        var wrong = new List<string>();
        foreach (var (name, text) in Articles())
        {
            foreach (string term in banned)
            {
                if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    wrong.Add($"{name} contains {term}");
                }
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }

    /// <summary>
    /// An article that says how many shots something rests on has to say a number, because "a few" is how a claim escapes its evidence.
    /// Entry 142 section 5: every figure states its sample size.
    /// </summary>
    [Fact]
    public void EveryArticleSaysHowManyShotsItRestsOn()
    {
        var wrong = new List<string>();
        foreach (var (name, text) in Articles())
        {
            var front = Regex.Match(text, @"\A---\r?\n(.*?)\r?\n---", RegexOptions.Singleline);
            Assert.True(front.Success, $"{name} has no front matter");
            var samples = Regex.Match(front.Groups[1].Value, @"^samples:\s*(?<it>.+)$", RegexOptions.Multiline);
            string it = samples.Success ? samples.Groups["it"].Value.Trim().Trim('"') : "";

            // A number in digits or in words, or a plain statement that there is nothing to count yet. An article whose test has not been
            // shot cannot state a sample size, and making it invent one would be worse than letting it say so.
            bool counted = Regex.IsMatch(it, @"\d")
                || Regex.IsMatch(it, @"\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|twenty|thirty)\b", RegexOptions.IgnoreCase);
            bool saysThereIsNoneYet = it.Contains("pending", StringComparison.OrdinalIgnoreCase)
                || it.Contains("see each entry", StringComparison.OrdinalIgnoreCase);

            if (it.Length == 0 || (!counted && !saysThereIsNoneYet))
            {
                wrong.Add($"{name}: its samples line neither names a number nor says the results are not in yet: {it}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join("; ", wrong));
    }
}
