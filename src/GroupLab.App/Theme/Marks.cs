using System.Globalization;
using Avalonia;
using Avalonia.Media;

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
