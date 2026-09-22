using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 6.2: where the group actually landed against where it was aimed, drawn rather than described.
/// <para>
/// <b>Why a picture earns its place here.</b> The zero block already says the correction in four units and in clicks, and a shooter can read
/// it. What the numbers cannot show is the thing that decides whether to dial at all: how far the group's centre sits from the aim compared
/// with how well that centre is known. Two figures that read "0.10 in low" mean opposite things when one carries an uncertainty of 0.02 in
/// and the other of 0.30 in. Drawn, the answer is immediate: either the ellipse is clear of the aim or it is sitting on top of it.
/// </para>
/// <para>
/// Everything is at one scale, and the frame is chosen so that both the offset and the ellipse fit with room around them. Nothing here is
/// decoration: the aim cross, the centre, the ellipse and the two arrows are the whole drawing.
/// </para>
/// </summary>
internal sealed class ZeroOffsetPicture : Control
{
    /// <summary>The share of the frame left clear around whatever has to fit, on each side.</summary>
    private const double FrameMargin = 0.28;

    /// <summary>The smallest extent the view frames, in inches, so a group sitting on its aim is not magnified without limit.</summary>
    private const double MinimumExtentInches = 0.2;

    /// <summary>How far the group's centre sits from the point of aim, in inches, across and down. Null where there is nothing to draw.</summary>
    public PointD? Centre { get; set; }

    /// <summary>The half width of the centre's uncertainty, in inches, across and down, at the level the zero block quotes.</summary>
    public PointD? Uncertainty { get; set; }

    /// <summary>What the across arrow says, such as "0.10 in right, 4 clicks".</summary>
    public string? AcrossSays { get; set; }

    /// <summary>What the down arrow says.</summary>
    public string? DownSays { get; set; }

    /// <summary>
    /// Whether the offset is far enough from zero to be told from chance. Where it is not, the drawing says so by leaving the arrows out:
    /// an arrow is an instruction, and there is nothing to instruct.
    /// </summary>
    public bool Worth { get; set; }

    /// <summary>The bull's outer diameter in inches, drawn faintly behind everything for scale, or null where the sheet has no bull.</summary>
    public double? BullInches { get; set; }

    public ZeroOffsetPicture()
    {
        ClipToBounds = true;
        MinHeight = 190;
    }

    /// <summary>What the picture is showing, in words, for a person using a screen reader and for the headless tests.</summary>
    public string Description
    {
        get
        {
            if (Centre is not { } centre)
            {
                return "No centre to show yet.";
            }

            string where = string.Create(CultureInfo.InvariantCulture,
                $"The group's centre is {Math.Abs(centre.X):0.000} in {(centre.X >= 0 ? "right" : "left")} and {Math.Abs(centre.Y):0.000} in {(centre.Y >= 0 ? "low" : "high")} of the aim.");
            return Worth
                ? where + " That is far enough from the aim to be worth dialling."
                : where + " The uncertainty covers the aim, so it cannot be told from chance.";
        }
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));

        if (Centre is not { } centre)
        {
            return;
        }

        var area = new Rect(Bounds.Size);
        var aim = new Point(area.Width / 2, area.Height / 2);

        // Everything that has to fit: the offset itself, its uncertainty, and the bull where there is one.
        double reach = Math.Max(
            Math.Max(Math.Abs(centre.X), Math.Abs(centre.Y)) + (Uncertainty is { } u ? Math.Max(u.X, u.Y) : 0),
            (BullInches ?? 0) / 2);
        reach = Math.Max(reach, MinimumExtentInches);
        double scale = Math.Min(area.Width, area.Height) * (0.5 - FrameMargin) / reach;

        Point At(double x, double y) => new(aim.X + (x * scale), aim.Y + (y * scale));

        // The bull, faint, so the offset has something to be a distance against.
        if (BullInches is { } bull && bull > 0)
        {
            Marks.Ring(context, Marks.Faint, aim, bull * scale / 2, 1);
        }

        var impact = At(centre.X, centre.Y);

        // The uncertainty first, so the centre sits on top of it.
        if (Uncertainty is { } spread && spread.X > 0 && spread.Y > 0)
        {
            context.DrawEllipse(null, new Pen(Marks.Teal, 1, Marks.Dashed), impact, spread.X * scale, spread.Y * scale);
        }

        // The aim is a cross and never a dot: it is not a shot, and the two must not read alike.
        Marks.Cross(context, Marks.Faint, aim, 7, 1.2);

        // The move from aim to impact, then the impact itself.
        Marks.Line(context, Marks.Faint, aim, impact, 1, Marks.Dashed);
        Marks.Dot(context, Marks.Impact, impact, 4);

        if (!Worth)
        {
            return;
        }

        // The correction, as the two things a turret can actually do. Each arrow points from the impact back towards the aim, because that
        // is the direction the shot has to move, which is not the direction the group is offset in.
        Arrow(context, new Point(impact.X, impact.Y), new Point(aim.X, impact.Y), AcrossSays, palette, above: false);
        Arrow(context, new Point(impact.X, impact.Y), new Point(impact.X, aim.Y), DownSays, palette, above: true);
    }

    /// <summary>One correction arrow, with its words set clear of the line.</summary>
    private static void Arrow(DrawingContext context, Point from, Point to, string? says, Palette palette, bool above)
    {
        if (Math.Abs(to.X - from.X) < 1 && Math.Abs(to.Y - from.Y) < 1)
        {
            return;
        }

        Marks.Line(context, Marks.Teal, from, to, 1.6);

        // The head, two short strokes back along the line.
        var along = to - from;
        double length = Math.Sqrt((along.X * along.X) + (along.Y * along.Y));
        var unit = new Vector(along.X / length, along.Y / length);
        var side = new Vector(-unit.Y, unit.X);
        const double head = 6;
        Marks.Line(context, Marks.Teal, to, to - (unit * head) + (side * head * 0.5), 1.6);
        Marks.Line(context, Marks.Teal, to, to - (unit * head) - (side * head * 0.5), 1.6);

        if (string.IsNullOrWhiteSpace(says))
        {
            return;
        }

        var text = new FormattedText(says, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(palette.Dim));
        var middle = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        var at = above
            ? new Point(middle.X + 8, middle.Y - (text.Height / 2))
            : new Point(middle.X - (text.Width / 2), middle.Y - text.Height - 4);
        context.DrawText(text, at);
    }
}
