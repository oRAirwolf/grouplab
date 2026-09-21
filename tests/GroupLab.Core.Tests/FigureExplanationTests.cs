using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 4: a small question mark beside every figure, explaining it in plain words, with a More link to
/// the glossary.
/// <para>
/// <b>Every explanation says what the sample size does to the figure, and that is not padding.</b> The single most common mistake in group
/// shooting is treating one five shot group as a measurement of a rifle. A figure shown without that caveat invites exactly that mistake,
/// and this application exists partly to stop it, so the caveat is held by a test rather than left to whoever writes the next one.
/// </para>
/// </summary>
public class FigureExplanationTests
{
    /// <summary>Words that would make an explanation useless to the person it is for.</summary>
    private static readonly string[] Jargon =
    [
        "estimator", "variance", "gaussian", "rayleigh", "bootstrap", "quantile", "heteroscedastic", "covariance",
    ];

    [Fact]
    public void EveryExplanationSaysWhatFewerShotsDoesToIt()
    {
        var silent = new List<string>();
        foreach (var (key, explanation) in FigureExplanations.All)
        {
            string plain = explanation.Plain.ToLowerInvariant();
            bool saysSo =
                plain.Contains("few shots", StringComparison.Ordinal)
                || plain.Contains("fewer", StringComparison.Ordinal)
                || plain.Contains("more shots", StringComparison.Ordinal)
                || plain.Contains("estimate", StringComparison.Ordinal)
                || plain.Contains("sample", StringComparison.Ordinal)
                || plain.Contains("interval", StringComparison.Ordinal)
                || plain.Contains("cannot be compared", StringComparison.Ordinal)
                || plain.Contains("more of them", StringComparison.Ordinal)
                || plain.Contains("handful", StringComparison.Ordinal);

            if (!saysSo)
            {
                silent.Add(key);
            }
        }

        Assert.True(silent.Count == 0,
            "every figure is an estimate from a handful of shots, and an explanation that does not say so invites the mistake this "
            + "application exists to prevent: " + string.Join(", ", silent));
    }

    [Fact]
    public void NoExplanationUsesAWordAShooterWouldNotKnow()
    {
        var found = new List<string>();
        foreach (var (key, explanation) in FigureExplanations.All)
        {
            string plain = explanation.Plain.ToLowerInvariant();
            found.AddRange(Jargon.Where(j => plain.Contains(j, StringComparison.Ordinal)).Select(j => $"{key} uses {j}"));
        }

        Assert.True(found.Count == 0, "an explanation nobody can read is worse than none: " + string.Join(", ", found));
    }

    [Fact]
    public void EveryExplanationIsTwoOrThreeSentencesAndNoMore()
    {
        foreach (var (key, explanation) in FigureExplanations.All)
        {
            int sentences = explanation.Plain.Count(c => c == '.');
            Assert.True(sentences is >= 2 and <= 4, $"{key} has {sentences} sentences, and section 4.1 asks for two or three");
            Assert.False(string.IsNullOrWhiteSpace(explanation.Name));
            Assert.Matches("^[a-z0-9-]+$", explanation.Term);
        }
    }

    /// <summary>The extreme spread's explanation has to say the thing people get wrong about it, or it is not worth showing.</summary>
    [Fact]
    public void TheExtremeSpreadSaysWhyItIsTheLeastReliableFigure()
    {
        var spread = FigureExplanations.For("extremeSpread");

        Assert.NotNull(spread);
        Assert.Contains("two shots", spread.Plain, StringComparison.Ordinal);
        Assert.Contains("cannot be compared", spread.Plain, StringComparison.Ordinal);
    }

    /// <summary>The zero correction is the one a person acts on, so its explanation says what acting on too few shots costs.</summary>
    [Fact]
    public void TheZeroCorrectionSaysWhatDiallingOnFewShotsCosts()
    {
        var zero = FigureExplanations.For("zero");

        Assert.NotNull(zero);
        Assert.Contains("further from where you want to be", zero.Plain, StringComparison.Ordinal);
    }

    /// <summary>
    /// The glossary page is generated from this list, so the screen and the page cannot say different things. A page that drifts is always
    /// the one nobody looks at, which is exactly the page somebody reaches when they did not understand the figure.
    /// </summary>
    [Fact]
    public void TheGlossaryPageCarriesEveryFigureAndItsWords()
    {
        string glossary = File.ReadAllText(Repo.PathTo("docs", "GLOSSARY.md"));

        foreach (var (key, explanation) in FigureExplanations.All)
        {
            Assert.Contains($"<a id=\"{explanation.Term}\"></a>", glossary, StringComparison.Ordinal);
            Assert.True(glossary.Contains(explanation.Plain, StringComparison.Ordinal),
                $"docs/GLOSSARY.md does not carry the words shown for {key}. Run: grouplab glossary docs");
        }
    }

    /// <summary>Until the site is published the More link goes somewhere that works today, which is worth more than one that will work later.</summary>
    [Fact]
    public void TheMoreLinkWorksBeforeTheSiteIsPublished()
    {
        string now = FigureExplanations.MoreAbout("mean-radius");
        Assert.StartsWith("https://github.com/oRAirwolf/grouplab/blob/main/docs/GLOSSARY.md#", now, StringComparison.Ordinal);

        string later = FigureExplanations.MoreAbout("mean-radius", sitePublished: true);
        Assert.Equal("https://grouplab.org/guides/glossary/#mean-radius", later);
    }
}
