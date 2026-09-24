namespace GroupLab.Core.Marking;

/// <summary>One figure explained: what it is, and the term it is called on the glossary page.</summary>
/// <param name="Term">The glossary anchor, lower case with hyphens.</param>
/// <param name="Name">What the figure is called on the screen.</param>
/// <param name="Plain">Two or three sentences a person who shoots can read, ending with what the sample size does to it.</param>
public sealed record FigureExplanation(string Term, string Name, string Plain);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 4: a small question mark beside every figure, explaining it in plain words.
/// <para>
/// <b>Every one of these says what the sample size does to it, and that is not padding.</b> Every figure here is an estimate from a handful
/// of shots, and the single most common mistake in group shooting is treating one five shot group as a measurement of a rifle. A figure
/// shown without that caveat invites exactly that mistake, and this application exists partly to stop it.
/// </para>
/// <para>
/// The explanations are the same text wherever a figure appears: the screen, the report and the glossary page. One list, so they cannot
/// drift into saying different things in different places.
/// </para>
/// </summary>
public static class FigureExplanations
{
    /// <summary>Where the longer explanation lives once the website is published.</summary>
    public const string Glossary = "https://grouplab.org/guides/glossary/";

    /// <summary>
    /// Where it lives until then. Entry 131 section 4.2 allows either; the GitHub copy is chosen because it works today, and a link that
    /// works today is worth more than one that will work later.
    /// </summary>
    public const string GlossaryUntilPublished = "https://github.com/oRAirwolf/grouplab/blob/main/docs/GLOSSARY.md";

    /// <summary>
    /// Every figure the analysis screen shows, by the key the screen uses for it. Entry 154: they are the glossary's terms that are figures,
    /// read from the one list in <c>glossary.json</c>, so the figure's explanation and the glossary's entry are the same words.
    /// </summary>
    public static IReadOnlyDictionary<string, FigureExplanation> All { get; } = GroupLab.Core.Marking.Glossary.All
        .Where(t => t.Figure is not null)
        .ToDictionary(t => t.Figure!, t => new FigureExplanation(t.Term, t.Name, t.Plain), StringComparer.Ordinal);

    /// <summary>The explanation for a figure, or null where there is none.</summary>
    public static FigureExplanation? For(string key) =>
        key is not null && All.TryGetValue(key, out var found) ? found : null;

    /// <summary>Where "More" goes for a figure.</summary>
    public static string MoreAbout(string term, bool sitePublished = false) =>
        (sitePublished ? Glossary : GlossaryUntilPublished) + "#" + term;
}
