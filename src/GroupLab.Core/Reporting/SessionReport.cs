using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Reporting;

/// <summary>A label and its value, as the report's particulars and footer carry them.</summary>
public sealed record ReportPair(string Label, string Value);

/// <summary>One figure as the analysis screen shows it: its label, its value, and the lines beneath, interval and without-exclusions included.</summary>
public sealed record ReportFigure(string Label, string Value, IReadOnlyList<string> Details);

/// <summary>A verdict with the lines that stay beside it on screen and the reasoning behind its "why", which the report prints on page 2.</summary>
public sealed record ReportCard(string Title, string Verdict, IReadOnlyList<string> Evidence, IReadOnlyList<string> Why);

/// <summary>One disc of the scoring bull, outermost first, as the composite plot draws it.</summary>
public sealed record ReportDisc(double DiameterInches, Rgb Colour, bool Paper, GroupLab.Core.Gltd.Model.DiscShape Shape = GroupLab.Core.Gltd.Model.DiscShape.Circle, int Rotation = 0);

/// <summary>One shot on the composite plot, its offset from its own bull's centre in inches, x right and y down.</summary>
public sealed record ReportShot(string Label, PointD OffsetInches, bool Excluded);

/// <summary>The composite plot: every scoring shot on one bull, excluded ones drawn hollow, with CEP 50 and 90 about the centre.</summary>
public sealed record ReportPlot(
    IReadOnlyList<ReportDisc> Discs,
    IReadOnlyList<ReportShot> Shots,
    double? CalibreInches,
    PointD? Centre,
    double? Cep50Inches,
    double? Cep90Inches,
    string Caption,
    string LengthSymbol = "in",
    double LengthPerInch = 1,
    double? Cep95Inches = null,
    PlotMarks? Shown = null,
    (PointD From, PointD To)? Spread = null,
    double? Cep99Inches = null,
    double? CustomCepInches = null);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 204 section 1.4: which of the composite plot's optional marks are drawn, from the toggles beside it, and
/// remembered between sessions. The report draws the same ones.
/// </summary>
/// <param name="Cep99">Entry 227 section 3: CEP 99, off until chosen.</param>
/// <param name="CustomPercent">Entry 227 section 3.2: a circle for any percent from 1 to 99.9, set under Advanced; none until one is typed.</param>
public sealed record PlotMarks(bool Cep50, bool Cep90, bool Cep95, bool Spread, bool Cep99 = false, double? CustomPercent = null)
{
    /// <summary>CEP 50 and 90 and the extreme spread on, CEP 95 and 99 off, no percent of one's own.</summary>
    public static PlotMarks Default { get; } = new(true, true, false, true);

    /// <summary>The percents a custom circle may be: above 0 and below 100, to one decimal, 1 to 99.9.</summary>
    public const double LeastPercent = 1, MostPercent = 99.9;
}

/// <summary>One row of the shot table; an excluded shot's row is struck through, as on screen, and never left out.</summary>
public sealed record ReportRow(IReadOnlyList<string> Cells, bool Struck);

/// <summary>A heading and its lines, for the report's second page.</summary>
public sealed record ReportSection(string Heading, IReadOnlyList<string> Lines);

/// <summary>
/// Everything one session's report prints, NOTES-FROM-PLANNING.md entry 112 section 2, as text already worded by the analysis screen, so the
/// paper can say nothing the screen does not: page 1 carries the particulars, the composite plot, the headline figures with their intervals,
/// the zero correction with its verdict and the two judgement cards; page 2 the shot table with bulls, the exclusions with their reasons, the
/// decisions left unmade, the registration, every "why", and the version and identifiers.
/// </summary>
public sealed record SessionReport(
    string Title,
    IReadOnlyList<ReportPair> Particulars,
    ReportPlot Plot,
    IReadOnlyList<string> Summary,
    IReadOnlyList<ReportFigure> Figures,
    ReportCard Zero,
    IReadOnlyList<ReportCard> Cards,
    IReadOnlyList<string> ShotHeadings,
    IReadOnlyList<ReportRow> Shots,
    IReadOnlyList<string> Exclusions,
    IReadOnlyList<string> Unmade,
    IReadOnlyList<string> Registration,
    IReadOnlyList<ReportSection> Why,
    IReadOnlyList<ReportPair> Identity);
