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

    /// <summary>Every figure the analysis screen shows, by the key the screen uses for it.</summary>
    public static IReadOnlyDictionary<string, FigureExplanation> All { get; } = new Dictionary<string, FigureExplanation>(StringComparer.Ordinal)
    {
        ["meanRadius"] = new("mean-radius", "Mean radius",
            "The average distance from each shot to the centre of the group. It uses every shot, so it is the steadiest measure of how well "
            + "a rifle and load shoot, and it changes less from group to group than the extreme spread does. With few shots it is still an "
            + "estimate: the interval beside it says how much it could move if you shot the same group again."),

        ["sigma"] = new("sigma", "Sigma",
            "The spread of the shots around their centre, in the same units as the group. It describes the pattern the shots are drawn from "
            + "rather than the particular shots you fired. Like every figure here it is estimated from the shots you have, so fewer shots "
            + "means a wider interval."),

        ["extremeSpread"] = new("extreme-spread", "Extreme spread",
            "The distance between the two shots furthest apart. It is the number most people quote, and the least reliable one, because it "
            + "uses only two shots and throws the rest away. Adding shots can only make it larger, so groups of different sizes cannot be "
            + "compared by it at all."),

        ["centreFromAim"] = new("centre-from-aim", "Centre from aim",
            "How far the middle of your group is from where you aimed, and in which direction. This is what a zero correction is worked out "
            + "from. With few shots the centre itself is uncertain, so a small offset may be the group moving about rather than the rifle "
            + "being off."),

        ["zero"] = new("zero-correction", "Zero correction",
            "How far to move your scope so the group's centre lands where you aimed, in the units your scope adjusts in. It is worked out "
            + "from where the group is now, so it is only as good as the centre it came from: with a handful of shots, dialling a small "
            + "correction can easily move you further from where you want to be."),

        ["cep"] = new("cep", "CEP",
            "The radius of a circle that would hold that share of your shots: CEP50 holds half of them, CEP90 nine in ten. It answers "
            + "\"where will the next shot go\" rather than \"how big was this group\". It is estimated from your shots, so it is less certain "
            + "with fewer of them."),

        ["interval"] = new("confidence-interval", "The interval",
            "The range the true value is likely to lie in, given how many shots you fired. A wide interval does not mean the measurement is "
            + "wrong; it means a handful of shots cannot pin it down. Firing more shots narrows it, and nothing else does."),

        ["worstShot"] = new("worst-shot", "Worst shot",
            "How far the furthest shot was from the centre, measured in mean radii. It says whether one shot was unusual for this group "
            + "rather than whether it was a flyer, which is a judgement only you can make. Expect the worst of twenty shots to be further "
            + "out than the worst of five, simply because there are more of them."),

        ["aspect"] = new("aspect-ratio", "Shape",
            "Whether the group is round or stretched in one direction, and by how much. A stretched group can mean wind, a bipod loading "
            + "unevenly, or nothing at all. With few shots a round pattern often looks stretched by chance, which is why the test beside it "
            + "says how surprising the shape actually is."),

        ["trueSize"] = new("true-size", "True size range",
            "What the group would likely measure if you fired it again, given what these shots show. It is wider than the group you actually "
            + "shot, because the group you shot is one sample of what the rifle does. This is the honest answer to \"how well does it shoot\"."),
    };

    /// <summary>The explanation for a figure, or null where there is none.</summary>
    public static FigureExplanation? For(string key) =>
        key is not null && All.TryGetValue(key, out var found) ? found : null;

    /// <summary>Where "More" goes for a figure.</summary>
    public static string MoreAbout(string term, bool sitePublished = false) =>
        (sitePublished ? Glossary : GlossaryUntilPublished) + "#" + term;
}
