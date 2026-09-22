using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>One row of the chart: what it is called, the measured value, and the range the true value is likely to lie in.</summary>
/// <param name="Label">The load or session this row is for.</param>
/// <param name="Value">The measurement.</param>
/// <param name="Lower">The bottom of its interval, or null where it has none.</param>
/// <param name="Upper">The top of its interval.</param>
internal sealed record IntervalRow(string Label, double Value, double? Lower, double? Upper);

/// <summary>
/// Mean radius and sigma as dots with their intervals, NOTES-FROM-PLANNING.md entry 131 section 10.
/// <para>
/// <b>This is the one picture that makes this project's central argument visible.</b> Two loads read 0.42 in and 0.51 in and the second
/// looks worse, and a table of those two numbers says so. Draw the intervals and the answer changes in an instant: if they run from 0.34 to
/// 0.58 and from 0.41 to 0.70, the comparison settles nothing, and anybody can see that without being told.
/// </para>
/// <para>
/// So the whiskers are the point and the dot is the detail, which is the opposite of how these charts are usually read. Where every interval
/// overlaps every other, the chart says so in words underneath rather than leaving a reader to measure it by eye.
/// </para>
/// </summary>
internal sealed class IntervalChart : Control
{
    private const double RowHeight = 34;

    private const double PadLeft = 110;

    private const double PadRight = 16;

    private const double PadTop = 10;

    public IReadOnlyList<IntervalRow> Rows { get; set; } = [];

    /// <summary>How a value reads in the person's units, set by the window.</summary>
    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    public IntervalChart()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(availableSize.Width, (Rows.Count * RowHeight) + (2 * PadTop));

    /// <summary>
    /// Whether every interval here overlaps every other, which is the case worth saying out loud: the measurements differ and the evidence
    /// does not separate them.
    /// </summary>
    public bool AllOverlap
    {
        get
        {
            var withIntervals = Rows.Where(r => r.Lower is not null && r.Upper is not null).ToList();
            if (withIntervals.Count < 2)
            {
                return false;
            }

            // They all share a point if the highest bottom is still below the lowest top.
            return withIntervals.Max(r => r.Lower!.Value) <= withIntervals.Min(r => r.Upper!.Value);
        }
    }

    /// <summary>What the chart says in words, for a screen reader and for the headless tests.</summary>
    public string Description => Rows.Count == 0
        ? "Nothing to compare yet."
        : AllOverlap
            ? "Every one of these overlaps every other, so these shots do not tell them apart."
            : "Some of these do not overlap, so there is a difference these shots can see.";

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Rows.Count == 0 || Bounds.Width < PadLeft + PadRight + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight;

        double lo = Rows.Min(r => Math.Min(r.Value, r.Lower ?? r.Value));
        double hi = Rows.Max(r => Math.Max(r.Value, r.Upper ?? r.Value));
        if (hi - lo < 1e-9)
        {
            hi = lo + 1;
        }

        // A little air at each end so a whisker never ends exactly on the edge, where it would read as cut off.
        double pad = (hi - lo) * 0.12;
        lo -= pad;
        hi += pad;

        double At(double value) => left + (((value - lo) / (hi - lo)) * (right - left));

        for (int i = 0; i < Rows.Count; i++)
        {
            var row = Rows[i];
            double y = PadTop + (i * RowHeight) + (RowHeight / 2);

            var name = new FormattedText(row.Label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(palette.Dim));
            context.DrawText(name, new Point(0, y - (name.Height / 2)));

            // The interval first and heavier than the dot, because the interval is the thing that decides whether a comparison means
            // anything and the dot is only where the measurement happened to land.
            if (row.Lower is { } low && row.Upper is { } high)
            {
                double a = At(low), b = At(high);
                Marks.Line(context, Marks.Teal, new Point(a, y), new Point(b, y), 2.5);
                Marks.Line(context, Marks.Teal, new Point(a, y - 5), new Point(a, y + 5), 1.5);
                Marks.Line(context, Marks.Teal, new Point(b, y - 5), new Point(b, y + 5), 1.5);
            }

            Marks.Dot(context, Marks.Impact, new Point(At(row.Value), y), 4);

            var value = new FormattedText(Length(row.Value), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(value, new Point(Math.Min(right - value.Width, At(row.Value) + 8), y - (value.Height) - 2));
        }
    }
}
