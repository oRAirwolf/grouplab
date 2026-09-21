using System.Collections.Immutable;
using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// One line in the right-hand panel: what it is called, what it says, and the explanation behind its <b>?</b>.
/// </summary>
/// <param name="Key">The figure's name in <see cref="FigureExplanations"/>, or a name of its own for a line that is not a figure.</param>
/// <param name="Label">What the row is called.</param>
/// <param name="Value">The number as it is written, in the units this panel was built for, or the sentence saying why there is no number.</param>
/// <param name="Unit">The unit, set beside the number in a lighter weight (NOTES-FROM-PLANNING.md entry 131 section 5.5), or null where the value carries its own.</param>
/// <param name="Interval">The interval around the figure, where one exists.</param>
/// <param name="Explanation">The two or three sentences behind the <b>?</b>, entry 131 section 4.</param>
/// <param name="More">Where "More" goes, entry 131 section 4.2.</param>
/// <param name="Headline">True for the one figure of the block set large and in orange.</param>
/// <param name="Withheld">True where <see cref="Value"/> is a reason rather than a number, so the screen sets it in words and not in figures.</param>
public sealed record PanelFigure(
    string Key,
    string Label,
    string Value,
    string? Unit = null,
    string? Interval = null,
    string? Explanation = null,
    string? More = null,
    bool Headline = false,
    bool Withheld = false);

/// <summary>One block of the right-hand panel: a heading, its rows, and a note beneath them where the block has something to say.</summary>
public sealed record PanelBlock(string Key, string Heading, ImmutableList<PanelFigure> Figures, string? Note = null);

/// <summary>
/// The analysis page's right-hand panel, NOTES-FROM-PLANNING.md entry 131 section 5: clear blocks, each with a heading, a table of figures
/// and one headline figure.
/// <para>
/// <b>Why the panel is built here and not on the screen.</b> Entry 131 section 5 opens with "today the text is many sizes and busy", and that
/// is what happens when each figure is laid out where it is computed: the rules drift apart one control at a time. Built as a list of blocks
/// of rows, the screen has one thing to draw and the rules are all in one place, which is also what lets the rule that matters be a test:
/// <b>no number is shown without an explanation behind it</b>, entry 131 section 4.1. A figure added later with nothing to say about itself
/// fails <c>EveryFigureCanExplainItself</c> rather than quietly appearing on the page with no <b>?</b>.
/// </para>
/// </summary>
public sealed record AnalysisPanel(ImmutableList<PanelBlock> Blocks, string? Problem)
{
    /// <summary>
    /// Builds the panel for a marking.
    /// </summary>
    /// <param name="state">The marking.</param>
    /// <param name="units">The units the page is being read in, <see cref="UnitSettings.ForAnalysis"/>.</param>
    /// <param name="sitePublished">Whether grouplab.org is published, which decides where the "More" links point (entry 131 section 4.2).</param>
    public static AnalysisPanel Build(MarkingState state, UnitSettings units, bool sitePublished = false)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);

        var report = GroupAnalysis.Analyse(state);
        var blocks = ImmutableList.CreateBuilder<PanelBlock>();
        blocks.Add(Group(report, units, sitePublished));
        if (Zero(state, units, sitePublished) is { } zero)
        {
            blocks.Add(zero);
        }

        blocks.Add(Shots(state, report));
        blocks.Add(Equipment(state));
        return new AnalysisPanel(blocks.ToImmutable(), report.Problem);
    }

    /// <summary>Every row of every block, for anything that wants to walk the whole panel.</summary>
    public IEnumerable<PanelFigure> AllFigures => Blocks.SelectMany(b => b.Figures);

    /// <summary>
    /// The group: shots, mean radius as the headline, sigma, extreme spread and the rest, each with its interval where one exists
    /// (entry 131 section 5.1).
    /// </summary>
    private static PanelBlock Group(GroupReport report, UnitSettings units, bool sitePublished)
    {
        var rows = ImmutableList.CreateBuilder<PanelFigure>();
        var figures = report.AllShots;
        rows.Add(new PanelFigure("shots", "Shots", (figures?.Shots ?? 0).ToString(CultureInfo.InvariantCulture)));

        rows.Add(Measure("meanRadius", "Mean radius", figures?.MeanRadius, figures?.MeanRadiusUnavailable, units, sitePublished, headline: true));
        rows.Add(Measure("sigma", "Sigma", figures?.Sigma, figures?.SigmaUnavailable, units, sitePublished));
        rows.Add(Measure("extremeSpread", "Extreme spread", figures?.ExtremeSpread, figures?.ExtremeSpreadUnavailable, units, sitePublished));

        string? note = report.Problem ?? figures?.DispersionWithheld;
        if (report.Excluded > 0)
        {
            string left = string.Create(CultureInfo.InvariantCulture, $"{report.Excluded} shot{(report.Excluded == 1 ? " is" : "s are")} left out of the reduced figures.");
            note = note is null ? left : note + " " + left;
        }

        return new PanelBlock("group", "Group", rows.ToImmutable(), note);
    }

    /// <summary>
    /// The zero: the correction as the headline, in the scope's units where the rifle records them, with the other three beneath
    /// (entry 131 sections 3.1 and 5.2). Null where the marking cannot say where the group sits against where it was aimed, because a
    /// heading with nothing but "needs a point of aim" under it is the busy page section 5 is trying to undo.
    /// </summary>
    private static PanelBlock? Zero(MarkingState state, UnitSettings units, bool sitePublished)
    {
        if (Zeroing.For(state) is not { } zero || state.ShotDistanceInches is not { } distanceInches)
        {
            return null;
        }

        double yards = distanceInches / 36;
        string? scope = state.Rifle is { } rifle ? (rifle.ClickUnit == Statistics.AngularUnit.Mrad ? "mil" : "moa") : null;
        string headline = FourUnits.Headline(scope);
        var rows = ImmutableList.CreateBuilder<PanelFigure>();

        foreach (var (axis, label) in new[] { (zero.Elevation, "Elevation"), (zero.Windage, "Windage") })
        {
            var four = FourUnits.Of(
                Math.Abs(axis.OffsetInches),
                yards,
                FourUnits.Clicks(Math.Abs(axis.OffsetInches), yards, scope, state.Rifle?.ClickValue));

            rows.Add(new PanelFigure(
                "zero",
                label,
                axis.Distinguishable ? four.Say(headline) : axis.Sits,
                Unit: null,
                Interval: four.Clicks is { } clicks ? axis.Dial + ", " + clicks : axis.Dial,
                Explanation: FigureExplanations.For("zero")?.Plain,
                More: FigureExplanations.MoreAbout("zero-correction", sitePublished),
                Headline: label == "Elevation",
                Withheld: !axis.Distinguishable));

            // The other three units beneath, entry 131 section 3.1: they stay visible rather than hiding behind a toggle, because a shooter
            // reading a correction in a unit their turret does not use needs the one it does without another click.
            rows.Add(new PanelFigure(
                "zero",
                label + ", other units",
                string.Join("  ", FourUnits.Beneath(headline).Select(four.Say)),
                Explanation: FigureExplanations.For("zero")?.Plain,
                More: FigureExplanations.MoreAbout("zero-correction", sitePublished)));
        }

        string note = string.Create(CultureInfo.InvariantCulture,
            $"From {zero.Shots} shots at {units.DistanceText(distanceInches)}. {(zero.Worth ? "Worth dialling." : "Neither axis is far enough from centre to be worth dialling at this many shots.")}");
        return new PanelBlock("zero", "Zero", rows.ToImmutable(), note);
    }

    /// <summary>The shots, each with what a person would do to it: entry 131 section 5.3's list with Edit and Delete.</summary>
    private static PanelBlock Shots(MarkingState state, GroupReport report)
    {
        var labels = ShotLabels.For(state);
        var rows = labels.Select(l =>
        {
            var shot = state.Find(l.ShotId)!;
            var marks = new List<string>();
            if (shot.Flyer)
            {
                marks.Add("flyer");
            }

            if (shot.Exclusion is { } reason)
            {
                marks.Add("left out, " + reason.InSentence());
            }

            if (shot.Sighter || GroupAnalysis.OnSighter(state, shot))
            {
                marks.Add("sighter");
            }

            return new PanelFigure(
                string.Create(CultureInfo.InvariantCulture, $"shot{l.ShotId}"),
                l.Text ?? "shot",
                marks.Count == 0 ? "" : string.Join(", ", marks),
                Withheld: l.Abnormal);
        });

        string? note = report.NotShots > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{report.NotShots} mark{(report.NotShots == 1 ? " is" : "s are")} marked as not a shot.")
            : null;
        return new PanelBlock("shots", "Shots", [.. rows], note);
    }

    /// <summary>The rifle, barrel and load the sheet was shot with, entry 131 section 5.4, each saying plainly when it is not recorded.</summary>
    private static PanelBlock Equipment(MarkingState state)
    {
        static PanelFigure Row(string key, string label, string? value) =>
            new(key, label, value ?? "not recorded", Withheld: value is null);

        return new PanelBlock("equipment", "Load and rifle",
        [
            Row("rifle", "Rifle", state.Rifle?.Name),
            Row("click", "Click", state.Rifle?.DescribeClick()),
            Row("barrel", "Barrel", state.Barrel),
            Row("load", "Load", state.Load),
        ]);
    }

    /// <summary>
    /// One measured figure: its value in the page's units with its interval, or the reason it cannot be quoted in its place. A figure that
    /// cannot be quoted keeps its row rather than vanishing, so the panel does not change shape as shots are added and a person can see what
    /// the group is not yet big enough to say.
    /// </summary>
    private static PanelFigure Measure(string key, string label, ReportedEstimate? estimate, string? unavailable, UnitSettings units, bool sitePublished, bool headline = false)
    {
        var explanation = FigureExplanations.For(key);
        string? more = explanation is null ? null : FigureExplanations.MoreAbout(explanation.Term, sitePublished);
        if (estimate is null)
        {
            return new PanelFigure(key, label, unavailable ?? "not available", null, null, explanation?.Plain, more, headline, Withheld: true);
        }

        string? interval = estimate.Lower is { } low && estimate.Upper is { } high
            ? string.Create(CultureInfo.InvariantCulture, $"{units.Number(low)} to {units.Number(high)}")
            : estimate.IntervalUnavailable;

        return new PanelFigure(key, label, units.Number(estimate.Value), UnitSettings.Symbol(units.Linear), interval, explanation?.Plain, more, headline);
    }
}
