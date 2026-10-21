using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace GroupLab.App.Theme;

/// <summary>
/// How every mark on the image is drawn, NOTES-FROM-PLANNING.md entry 42 section 5 and entry 39 section 5. Any single colour loses against
/// something in the corpus, so every mark is a two tone stroke: a dark halo, 3 pixels of #0b0c0e at 55 percent, under the mark's own
/// colour at 1.6 pixels. The halo reads on paper and on bright rings; the core reads on ink and on a dark backer. Widths are screen pixels
/// and do not scale with zoom, so a mark stays visible at any magnification. The canvas uses these, and the composite plot will, so the
/// image and the plot cannot disagree about what a shot looks like.
/// </summary>
public static class Marks
{
    private static readonly IBrush Halo = new SolidColorBrush(Tokens.MarkHalo);

    /// <summary>A bullet hole: a ring at the true hole diameter with a one pixel pip.</summary>
    public static IBrush Impact { get; } = new SolidColorBrush(Tokens.MarkImpact);

    /// <summary>
    /// The accent rule of NOTES-FROM-PLANNING.md entry 93 section 2, applied to the marks (entry 97 section 1): a shot the software found and
    /// nobody has had to touch is teal, a shot a person placed or corrected is neutral, and a shot the review queue still wants a decision on
    /// is amber, as the concept draws it. Red is kept for what is wrong: a missing marker and a mark flagged as two holes.
    /// </summary>
    public static IBrush Found { get; } = new SolidColorBrush(Tokens.MarkTeal);

    public static IBrush Placed { get; } = new SolidColorBrush(Tokens.MarkPlaced);

    public static IBrush NeedsPerson { get; } = new SolidColorBrush(Tokens.MarkSelected);

    /// <summary>The selected shot, and a shot being placed.</summary>
    public static IBrush Selected { get; } = new SolidColorBrush(Tokens.MarkSelected);

    /// <summary>An excluded shot, dashed.</summary>
    public static IBrush Excluded { get; } = new SolidColorBrush(Tokens.MarkExcluded);

    /// <summary>The scale reference and the point of aim.</summary>
    public static IBrush Teal { get; } = new SolidColorBrush(Tokens.MarkTeal);

    /// <summary>A bull centre, and a detection marked as not a shot.</summary>
    public static IBrush Faint { get; } = new SolidColorBrush(Tokens.MarkFaint);

    /// <summary>A missing marker, and a hole that reads too large for the calibre.</summary>
    public static IBrush Alert { get; } = new SolidColorBrush(Tokens.MarkAlert);

    /// <summary>Entry 407 section 1: the chosen bull, its ring and its lines.</summary>
    public static IBrush Chosen { get; } = new SolidColorBrush(Tokens.MarkChosen);

    /// <summary>Entry 407 section 1: a line worth a second look, dashed, with a "?" beside its hole.</summary>
    public static IBrush Unsure { get; } = new SolidColorBrush(Tokens.MarkUnsure);

    /// <summary>The lines and holes of the bulls not chosen while one is, dimmed.</summary>
    public static IBrush Dimmed { get; } = new SolidColorBrush(Tokens.MarkFaint, 0.45);

    public static IDashStyle Dashed { get; } = new DashStyle([3, 2], 0);

    /// <summary>The halo's width for a core: 3 pixels at the standard 1.6 pixel core, and as much wider as a heavier core is.</summary>
    private static double HaloWidth(double core) => Tokens.MarkHaloWidth + (core - Tokens.MarkCoreWidth);

    public static void Line(DrawingContext context, IBrush brush, Point a, Point b, double core = Tokens.MarkCoreWidth, IDashStyle? dash = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.DrawLine(new Pen(Halo, HaloWidth(core), dash), a, b);
        context.DrawLine(new Pen(brush, core, dash), a, b);
    }

    public static void Ring(DrawingContext context, IBrush brush, Point centre, double radius, double core = Tokens.MarkCoreWidth, IDashStyle? dash = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.DrawEllipse(null, new Pen(Halo, HaloWidth(core), dash), centre, radius, radius);
        context.DrawEllipse(null, new Pen(brush, core, dash), centre, radius, radius);
    }

    /// <summary>A filled dot with a halo edge: the pip at an impact's centre, and the ends of a scale reference.</summary>
    public static void Dot(DrawingContext context, IBrush brush, Point centre, double radius)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.DrawEllipse(brush, new Pen(Halo, HaloWidth(Tokens.MarkCoreWidth) - Tokens.MarkCoreWidth), centre, radius, radius);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 425: the selected shot, impossible to miss. A ring of radius <paramref name="ring"/>, clearly outside the
    /// shot, and a crosshair through its centre from <paramref name="inner"/> out past the ring, so the hole itself stays uncovered; each
    /// stroke in the theme's selection ink over an edge in the other, drawn by the caller after every other mark and line.
    /// </summary>
    public static void Selection(DrawingContext context, ThemeVariant? variant, Point centre, double ring, double inner, double arm = Tokens.SelectionArm)
    {
        ArgumentNullException.ThrowIfNull(context);
        var (ink, edge) = Tokens.Selection(variant);
        double reach = ring + arm;
        var arms = new[] { new Vector(1, 0), new Vector(-1, 0), new Vector(0, 1), new Vector(0, -1) };
        foreach (var (brush, width) in new[] { (new SolidColorBrush(edge), Tokens.SelectionCoreWidth + (2 * Tokens.SelectionEdgeWidth)), (new SolidColorBrush(ink), Tokens.SelectionCoreWidth) })
        {
            var pen = new Pen(brush, width);
            context.DrawEllipse(null, pen, centre, ring, ring);
            foreach (var way in arms)
            {
                context.DrawLine(pen, centre + (way * inner), centre + (way * reach));
            }
        }
    }

    /// <summary>A plus-shaped cross: the point of aim, which must never read as a shot, and a bull centre.</summary>
    public static void Cross(DrawingContext context, IBrush brush, Point centre, double half, double core = Tokens.MarkCoreWidth)
    {
        Line(context, brush, centre + new Vector(-half, 0), centre + new Vector(half, 0), core);
        Line(context, brush, centre + new Vector(0, -half), centre + new Vector(0, half), core);
    }

    /// <summary>A diagonal cross: a missing marker, and a detection marked as not a shot.</summary>
    public static void Saltire(DrawingContext context, IBrush brush, Point centre, double half, double core = Tokens.MarkCoreWidth)
    {
        Line(context, brush, centre + new Vector(-half, -half), centre + new Vector(half, half), core);
        Line(context, brush, centre + new Vector(-half, half), centre + new Vector(half, -half), core);
    }

    private static readonly IBrush Plate = new SolidColorBrush(Tokens.MarkPlate);
    private static readonly IBrush LabelText = new SolidColorBrush(Tokens.MarkLabelText);
    private static readonly IPen NumberHalo = new Pen(new SolidColorBrush(Tokens.Paper, 0.75), 3, lineJoin: PenLineJoin.Round);

    private static FormattedText TagText(string text, IBrush brush) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.TableRowSize, brush);

    private static FormattedText NumberText(string text, IBrush brush) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.TableRowSize, brush);

    /// <summary>The size of a hole's tag, its plate and text, as <see cref="Tag"/> draws it, for the tags' placing.</summary>
    public static (double Width, double Height) TagSize(string text)
    {
        var formatted = TagText(text, LabelText);
        return (formatted.Width + 7, formatted.Height + 2);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 465 section 1A: a hole's tag in the box the placing gave it, as <see cref="Label"/> draws one, light text
    /// on a dark plate with a bar in the mark's colour; and the selected shot's tag in the selection's magenta with its text in the opposite
    /// ink, so the tag and the ring round the hole are found together.
    /// </summary>
    public static void Tag(DrawingContext context, string text, IBrush brush, GroupLab.Core.Marking.TagBox box, ThemeVariant? selectedIn = null, bool selected = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        var plate = new Rect(box.X, box.Y, box.Width, box.Height);
        if (selected)
        {
            var (ink, edge) = Tokens.Selection(selectedIn);
            context.FillRectangle(new SolidColorBrush(ink), plate);
            context.DrawText(TagText(text, new SolidColorBrush(edge)), new Point(box.X + 5, box.Y + 1));
            return;
        }

        context.FillRectangle(Plate, plate);
        context.FillRectangle(brush, new Rect(plate.X, plate.Y, 2, plate.Height));
        context.DrawText(TagText(text, LabelText), new Point(box.X + 5, box.Y + 1));
    }

    /// <summary>The size of a bull's number as <see cref="Number"/> draws it.</summary>
    public static (double Width, double Height) NumberSize(string text)
    {
        var formatted = NumberText(text, LabelText);
        return (formatted.Width + 2, formatted.Height);
    }

    /// <summary>
    /// Entry 465 section 1A: a bull's number, plain muted text outside its ring with a thin edge of paper round each letter, so it reads on the
    /// paper and on a bull's black alike and never looks like a hole's tag.
    /// </summary>
    public static void Number(DrawingContext context, string text, IBrush brush, GroupLab.Core.Marking.TagBox box)
    {
        ArgumentNullException.ThrowIfNull(context);
        var formatted = NumberText(text, brush);
        if (formatted.BuildGeometry(new Point(box.X + 1, box.Y)) is { } outline)
        {
            context.DrawGeometry(null, NumberHalo, outline);
        }

        context.DrawText(formatted, new Point(box.X + 1, box.Y));
    }

    /// <summary>Entry 465 section 1A: the short line from a hole's edge to its tag, where the tag stands in a column away from it.</summary>
    public static void Leader(DrawingContext context, IBrush brush, GroupLab.Core.Imaging.PointD from, GroupLab.Core.Imaging.PointD to) =>
        Line(context, brush, new Point(from.X, from.Y), new Point(to.X, to.Y), 1);

    /// <summary>
    /// A mark's label: light mono text on a dark plate, with a bar in the mark's colour down its left edge. Text in the mark's own colour on
    /// the halo was unreadable in the first screenshots, red on near black at 11.5 point, so the colour moved to the bar and the text is light.
    /// </summary>
    public static void Label(DrawingContext context, string text, IBrush brush, Point at)
    {
        ArgumentNullException.ThrowIfNull(context);
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.TableRowSize, LabelText);
        var plate = new Rect(at.X - 3, at.Y - 1, formatted.Width + 7, formatted.Height + 2);
        context.FillRectangle(Plate, plate);
        context.FillRectangle(brush, new Rect(plate.X, plate.Y, 2, plate.Height));
        context.DrawText(formatted, at + new Vector(2, 0));
    }
}
