using System.Globalization;
using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// Grid style 4, the printer check page of NOTES-FROM-PLANNING.md entries 272 and 273: not a measurement grid but the page a printer's
/// scale is checked on, drawn as Alan approved it (the concept "The Scale check page"). Everything on it is derived here from the page's
/// size, the way style 3's strokes and legend are, so the definition carries only the style and the page reads itself like any sheet: its
/// codes name it and its markers register it.
/// <list type="bullet">
/// <item>Three crosshairs in an L, <see cref="CaliperDmm"/> center to center across and down, for a digital caliper; each crosshair's circle
/// is a bull of the definition that is not scored, which is how the format's one-bull minimum is met without changing it.</item>
/// <item>Two ruler lines, <see cref="RulerDownDmm"/> down the left side and <see cref="RulerAcrossDmm"/> across the bottom, tick to tick.</item>
/// <item>A card outline, the size of an ISO/IEC 7810 ID-1 card, <see cref="CardWidthDmm"/> by <see cref="CardHeightDmm"/> as near as whole
/// dmm allow (the card, not the outline, is the reference; the outline only says where to lay it).</item>
/// <item>The title, "Print at Actual size (100%). Never Fit to page.", the page's name, and the four numbered instructions.</item>
/// </list>
/// Lengths are in dmm; the page's own coordinates, 0 at the top left.
/// </summary>
public static class GridStyle4
{
    public const int Style = 4;

    /// <summary>The caliper crosshairs' spacing, 150.00 mm.</summary>
    public const int CaliperDmm = 1500;

    /// <summary>The ruler line down the side, 250.0 mm.</summary>
    public const int RulerDownDmm = 2500;

    /// <summary>The ruler line across the bottom, 190.0 mm, which also fits A4 with room at each side.</summary>
    public const int RulerAcrossDmm = 1900;

    /// <summary>An ID-1 card is 85.60 by 53.98 mm; the outline is drawn to the nearest whole dmm, 85.6 by 54.0.</summary>
    public const int CardWidthDmm = 856;

    public const int CardHeightDmm = 540;

    /// <summary>The card's true size in mm, which is what a card photo measures against.</summary>
    public const double CardWidthMm = 85.60;

    public const double CardHeightMm = 53.98;

    /// <summary>The card's thickness, which lifts its face above the page toward the camera.</summary>
    public const double CardThicknessMm = 0.76;

    /// <summary>Line weights: the rulers and crosshairs, and the card outline.</summary>
    public const int LineStroke = 4;

    public const int OutlineStroke = 8;

    /// <summary>
    /// The white left between the card's size and the outline, 3 mm all round: a card laid inside the line has its edges on white paper,
    /// where a dark card's edge can be seen. On the line, a dark card's edge and the black line are one dark band, and the card could be read
    /// as much as the line's width larger, about 1 percent.
    /// </summary>
    public const int OutlineGap = 30;

    /// <summary>Half a crosshair's arm, and the length of a ruler's end tick.</summary>
    public const int CrossArm = 60;

    public const int EndTick = 50;

    /// <summary>The crosshair circle's ring set: a thin ring, outer and inner diameters, drawn as a bull that is not scored.</summary>
    public const int RingOuter = 46;

    public const int RingInner = 38;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 358 section 3: the printer check label, on 4x6, A6 and 100 x 150 mm. A thermal printer's feed scale
    /// depends on its paper as well as on the printer, so the label is the check for that printer on that paper, across and along the feed.
    /// The same parts as the page, smaller: one code in the band at the top with the words beside it, the card outline, crosshairs
    /// <see cref="LabelAcrossDmm"/> apart across and <see cref="LabelFeedDmm"/> along the feed, and a millimetre ruler along the feed.
    /// </summary>
    public static bool IsLabel(Page page) => page is not null && PageSizes.IsLabel(page.Size);

    /// <summary>The label's crosshairs across, 80.00 mm, and along the feed, 40.00 mm.</summary>
    public const int LabelAcrossDmm = 800, LabelFeedDmm = 400;

    /// <summary>The label's graduated ruler along the feed, 30 mm.</summary>
    public const int LabelRulerDmm = 300;

    /// <summary>The label's code: version 7 at level Q, 4 dmm modules, its footprint's corner this far from the label's top left.</summary>
    public const int LabelCodeVersion = 7, LabelCodeModule = 4, LabelInset = 30;

    /// <summary>The label code's footprint, quiet zone and all: 45 modules and 4 each side, 212 dmm.</summary>
    public const int LabelCodeFootprint = (((4 * LabelCodeVersion) + 17) * LabelCodeModule) + (2 * 4 * LabelCodeModule);

    /// <summary>The label code's centre.</summary>
    public static PointDmm LabelCode(Page page) => new(LabelInset + (LabelCodeFootprint / 2), LabelInset + (LabelCodeFootprint / 2));

    /// <summary>The caliper's span across: 150.00 mm on the page, 80.00 mm on a label.</summary>
    public static int CaliperAcross(Page page) => IsLabel(page) ? LabelAcrossDmm : CaliperDmm;

    /// <summary>The caliper's span down the page, which on a label is along the feed: 150.00 mm, or 40.00 mm.</summary>
    public static int CaliperDown(Page page) => IsLabel(page) ? LabelFeedDmm : CaliperDmm;

    /// <summary>The three crosshair centers: the corner of the L at the top left, then across, then down.</summary>
    public static IReadOnlyList<PointDmm> Crosshairs(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (IsLabel(page))
        {
            int middle = page.Width / 2, y = Card(page).Y + CardHeightDmm + OutlineGap + 80;
            return [new(middle - (LabelAcrossDmm / 2), y), new(middle + (LabelAcrossDmm / 2), y), new(middle + (LabelAcrossDmm / 2), y + LabelFeedDmm)];
        }

        int x0 = 380, y0 = 794;
        return [new(x0, y0), new(x0 + CaliperDmm, y0), new(x0 + CaliperDmm, y0 + CaliperDmm)];
    }

    /// <summary>The label's ruler along the feed: its x, and its first and last millimetre.</summary>
    public static (int X, int Top, int Bottom) LabelRuler(Page page)
    {
        var c = Crosshairs(page);
        return (c[0].X, c[0].Y + 80, c[0].Y + 80 + LabelRulerDmm);
    }

    /// <summary>The ruler down the left side: its x, and its top and bottom ends.</summary>
    public static (int X, int Top, int Bottom) RulerDown(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        int top = 159;
        return (90, top, top + RulerDownDmm);
    }

    /// <summary>The ruler across the bottom: its y, and its left and right ends, pulled in on a narrow page such as A4.</summary>
    public static (int Y, int Left, int Right) RulerAcross(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        int left = Math.Min(159, page.Width - 60 - RulerAcrossDmm);
        return (2606, left, left + RulerAcrossDmm);
    }

    /// <summary>The card outline's top left corner: centered under the two top crosshairs; on a label, centred under the code band.</summary>
    public static PointDmm Card(Page page)
    {
        if (IsLabel(page))
        {
            return new PointDmm((page.Width / 2) - (CardWidthDmm / 2), LabelInset + LabelCodeFootprint + OutlineGap + 30);
        }

        var c = Crosshairs(page);
        return new PointDmm(((c[0].X + c[1].X) / 2) - (CardWidthDmm / 2), 1058);
    }

    /// <summary>
    /// Where the markers stand, in the spaces the artwork leaves: a row under the title and one above the bottom ruler, a column down each side
    /// outside the crosshairs, so every crosshair lies inside the markers (section 7's bracket rule), and two each side of the card.
    /// </summary>
    public static IReadOnlyList<PointDmm> MarkerSpots(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (IsLabel(page))
        {
            // On a label: one at the top right beside the words, a row between the across crosshairs and the bottom one, and a row along the bottom.
            int middle = page.Width / 2, between = Crosshairs(page)[0].Y + 200, bottom = page.Height - 60;
            return
            [
                new(page.Width - 70, 70),
                new(middle - 250, between), new(middle - 50, between), new(middle + 100, between), new(middle + 250, between),
                new(middle - (LabelAcrossDmm / 2), bottom), new(middle, bottom), new(middle + 250, bottom),
            ];
        }

        var card = Card(page);
        int left = card.X - 150, right = card.X + CardWidthDmm + 150;
        int[] rows = [794, 1300, 1800, 2294];
        return
        [
            new(560, 470), new(900, 470), new(1240, 470), new(1580, 470),
            .. rows.Select(y => new PointDmm(250, y)),
            .. rows.Select(y => new PointDmm(2000, y)),
            new(left, 1120), new(left, 1480), new(right, 1120), new(right, 1480),
            new(560, 2470), new(900, 2470), new(1240, 2470), new(1580, 2470),
        ];
    }

    /// <summary>The page's short name, printed under its title: GL-SCALE-LTR-1 on Letter, GL-SCALE-A4-1 on A4.</summary>
    public static string PageName(Page page) => page.Size switch
    {
        PageSize.A4 => "GL-SCALE-A4-1 · A4",
        PageSize.Label4x6 => "GL-SCALE-4X6-1 · 4x6 label",
        PageSize.A6 => "GL-SCALE-A6-1 · A6 label",
        PageSize.Label100x150 => "GL-SCALE-100X150-1 · 100 x 150 mm label",
        _ => "GL-SCALE-LTR-1 · Letter",
    };

    /// <summary>The line under a check label's name: what it measures.</summary>
    public const string LabelPurpose = "This printer, on this paper";

    /// <summary>The words beside a label's crosshairs.</summary>
    public static string LabelAcrossWords => string.Create(CultureInfo.InvariantCulture, $"{LabelAcrossDmm / 10.0:0.00} mm across, center to center");

    public static string LabelFeedWords => string.Create(CultureInfo.InvariantCulture, $"{LabelFeedDmm / 10.0:0.00} mm along the feed");

    public const string Title = "GroupLab printer check";

    public const string PrintAtActualSize = "Print at Actual size (100%). Never Fit to page.";

    public static string CaliperLabel => string.Create(CultureInfo.InvariantCulture, $"CALIPER: {CaliperDmm / 10.0:0.00} mm ({CaliperDmm / 254.0:0.000} in) CENTER TO CENTER");

    public static string RulerLabel(int dmm) => string.Create(CultureInfo.InvariantCulture, $"RULER: {dmm / 10.0:0.0} mm ({dmm / 254.0:0.000} in) END TO END");

    public const string CardHeading = "Lay any bank, gift or ID card here";

    public static string CardSize => string.Create(CultureInfo.InvariantCulture, $"{CardWidthMm:0.00} × {CardHeightMm:0.00} mm");

    public const string CardNote = "either side up, inside the line";

    public const string InstructionsHeading = "Use one of these, then open GroupLab's printer check";

    /// <summary>The four numbered instructions: the bold lead, then the words.</summary>
    public static readonly IReadOnlyList<(string Lead, string Words)> Instructions =
    [
        ("1. A card, one photo.", "Lay any card inside its outline and take one photo in GroupLab. It measures the rest."),
        ("2. A digital caliper.", "Measure between the centers of the crosshairs, across and down, and type both numbers."),
        ("3. A ruler or tape.", "Measure the two long lines end to end and type both numbers."),
        ("4. A scanner.", "Scan this page, or any GroupLab sheet, and open the scan in GroupLab."),
    ];
}
