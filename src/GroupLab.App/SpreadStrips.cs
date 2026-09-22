using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// Is this group wider than it is tall? NOTES-FROM-PLANNING.md entry 141 section 5.2.2.
/// <para>
/// <b>One question, answered two ways, and the second one is what stops it being a trap.</b> Every group is wider than it is tall or taller
/// than it is wide; none is ever exactly square. So the picture alone invites a person to read wind, or a bipod, or a technique, into what is
/// almost always the ordinary lopsidedness of a handful of shots. The strips show the shots on each axis, and the caption says whether the
/// shots can separate the two spreads at all, from the circularity test the analysis has already run.
/// </para>
/// <para>
/// The dots are the shots themselves rather than a summary of them, because a spread of twelve where one shot is a long way out is a
/// different thing from a spread of twelve where they are evenly placed, and a bar cannot tell those apart.
/// </para>
/// </summary>
internal sealed class SpreadStrips : Control
{
    private const double RowHeight = 42;

    private const double PadLeft = 96;

    private const double PadRight = 16;

    private const double PadTop = 8;

    /// <summary>The shots' offsets from the group's centre, inches, across and up and down.</summary>
    public IReadOnlyList<PointD> Offsets { get; set; } = [];

    /// <summary>The standard deviation on each axis, inches, as the analysis computed it.</summary>
    public double? AcrossSd { get; set; }

    public double? UpDownSd { get; set; }

    /// <summary>
    /// How likely a group this lopsided would be if the rifle's dispersion were perfectly round, from the analysis's own circularity test.
    /// Null where there were too few shots to run it.
    /// </summary>
    public double? RoundPValue { get; set; }

    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    public SpreadStrips()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, (2 * RowHeight) + (2 * PadTop));

    /// <summary>
    /// What the picture says in words, for a screen reader and for the headless tests. It never says a group is wider than it is tall
    /// without saying whether the shots can tell.
    /// </summary>
    public string Description
    {
        get
        {
            if (Offsets.Count < 2 || AcrossSd is not { } across || UpDownSd is not { } upDown)
            {
                return "Not enough shots to compare the two spreads.";
            }

            string sizes = string.Create(CultureInfo.InvariantCulture,
                $"Across {Length(across)}, up and down {Length(upDown)}.");
            string which = upDown > across ? "taller than it is wide" : "wider than it is tall";

            return RoundPValue is not { } p
                ? sizes + " Too few shots to say whether that difference is real."
                : p < 0.05
                    ? sizes + $" This group is {which}, and {Offsets.Count} shots are enough to say so."
                    : sizes + $" It measures {which}, but {Offsets.Count} shots cannot tell that from an ordinary round group.";
        }
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Offsets.Count == 0 || Bounds.Width < PadLeft + PadRight + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight;

        // Both strips share one scale, because the whole question is which of the two is larger. Two scales would answer it by drawing.
        double reach = Offsets.SelectMany(o => new[] { Math.Abs(o.X), Math.Abs(o.Y) }).DefaultIfEmpty(1).Max();
        if (reach < 1e-9)
        {
            reach = 1;
        }

        reach *= 1.1;
        double middle = (left + right) / 2;
        double scale = (right - left) / (2 * reach);

        for (int axis = 0; axis < 2; axis++)
        {
            double y = PadTop + (axis * RowHeight) + (RowHeight / 2);
            var name = new FormattedText(axis == 0 ? "Across" : "Up and down", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(palette.Dim));
            context.DrawText(name, new Point(0, y - (name.Height / 2)));

            Marks.Line(context, Marks.Faint, new Point(left, y), new Point(right, y), 1);

            // The band is one standard deviation each side of the centre: where about two thirds of the shots belong.
            if ((axis == 0 ? AcrossSd : UpDownSd) is { } sd && sd > 0)
            {
                double half = sd * scale;
                context.FillRectangle(new SolidColorBrush(Tokens.MarkTeal, 0.16), new Rect(middle - half, y - 11, 2 * half, 22));
            }

            Marks.Line(context, Marks.Faint, new Point(middle, y - 13), new Point(middle, y + 13), 1);

            foreach (var offset in Offsets)
            {
                double at = middle + ((axis == 0 ? offset.X : offset.Y) * scale);
                Marks.Dot(context, Marks.Impact, new Point(at, y), 3.5);
            }
        }
    }
}
