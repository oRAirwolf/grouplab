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

/// <summary>Where one row's parts are drawn: its name, its value, and the line its dot and range sit on.</summary>
internal sealed record IntervalRowPlace(Rect Name, Rect Value, Rect Bar);

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
/// <para>
/// Entry 295 section 1: each row's name has a line of its own, wrapped across the full width, and the range and its value share the line
/// beneath, the value in a column of its own at the right. Nothing is drawn over anything else at any width or text size, and the chart is
/// exactly as tall as its rows. On the phone a name drawn in the range's row sat under the bar, the dot and the value.
/// </para>
/// </summary>
internal sealed class IntervalChart : Control
{
    /// <summary>Space between a row's name and its range, and between one row and the next.</summary>
    private const double NameGap = 2;

    private const double RowGap = 10;

    /// <summary>How far a whisker's end reaches above and below its line.</summary>
    private const double Whisker = 5;

    /// <summary>Space between the range and the value column.</summary>
    private const double ValueGap = 10;

    /// <summary>The narrowest a range may be drawn beside its value; narrower, and the value goes on a line of its own.</summary>
    private const double NarrowestBar = 60;

    public IReadOnlyList<IntervalRow> Rows { get; set; } = [];

    /// <summary>How a value reads in the person's units, set by the window.</summary>
    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    /// <summary>The size the names and values are drawn at: the desktop's secondary size unless the screen asks for another.</summary>
    public double TextSize { get; set; } = Tokens.SecondarySize;

    /// <summary>
    /// What the comparison says this chart shows, from <c>LoadComparison.ChartSays</c>, so the sentence and the verdict are one decision
    /// (entry 295 section 1.4). Without one the chart reads its own rows.
    /// </summary>
    public string? Says { get; set; }

    /// <summary>
    /// Entry 309 section 2.3: the rows are loads, each named after its marker in its load's color (<see cref="LoadGroups"/>), as on the
    /// plots above; the interval stays teal and the dot the impact's, as on every chart.
    /// </summary>
    public bool RowsAreLoads { get; set; }

    /// <summary>The room a load's marker takes before its name.</summary>
    private double Indent => RowsAreLoads ? 16 : 0;

    public IntervalChart()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : 400;
        return new Size(width, Places(width).Height);
    }

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

    /// <summary>Whether the rows have ranges to compare at all: two or more of them with an interval.</summary>
    public bool HasRanges => Rows.Count(r => r.Lower is not null && r.Upper is not null) >= 2;

    /// <summary>What the chart says in words, for a screen reader and for the headless tests.</summary>
    public string Description => Says ?? (Rows.Count == 0
        ? "Nothing to compare yet."
        : !HasRanges
            ? "These have no range to compare, so this chart cannot say whether they differ."
            : AllOverlap
                ? "Every one of these overlaps every other, so these shots do not tell them apart."
                : "Some of these do not overlap, so there is a difference these shots can see.");

    private FormattedText NameText(string words, double width, IBrush? brush) =>
        new(words, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), TextSize, brush) { MaxTextWidth = Math.Max(1, width) };

    private FormattedText ValueText(double value, IBrush? brush) =>
        new(Length(value), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), TextSize, brush);

    /// <summary>
    /// Where every row's parts go at this width, and the height they take: each name on its own line or lines, and under it the range with
    /// the value in a column at the right, all rows sharing one scale; where the range would be too short beside the value, the value goes
    /// on a line of its own beneath it.
    /// </summary>
    internal (IReadOnlyList<IntervalRowPlace> Rows, double Height) Places(double width)
    {
        var places = new List<IntervalRowPlace>();
        if (Rows.Count == 0)
        {
            return (places, 0);
        }

        double column = Rows.Max(r => ValueText(r.Value, null).WidthIncludingTrailingWhitespace);
        bool beside = width - column - ValueGap >= NarrowestBar;
        double barRight = beside ? width - column - ValueGap : width;
        double y = 0;
        foreach (var row in Rows)
        {
            var name = NameText(row.Label, width - Indent, null);
            var nameBox = new Rect(Indent, y, Math.Min(width - Indent, name.WidthIncludingTrailingWhitespace), name.Height);
            y += name.Height + NameGap;
            var value = ValueText(row.Value, null);
            double line = Math.Max(2 * Whisker + 2, beside ? value.Height : 0);
            var bar = new Rect(0, y, barRight, line);
            Rect valueBox;
            if (beside)
            {
                valueBox = new Rect(width - value.WidthIncludingTrailingWhitespace, y + ((line - value.Height) / 2), value.WidthIncludingTrailingWhitespace, value.Height);
                y += line;
            }
            else
            {
                y += line + NameGap;
                valueBox = new Rect(0, y, Math.Min(width, value.WidthIncludingTrailingWhitespace), value.Height);
                y += value.Height;
            }

            places.Add(new IntervalRowPlace(nameBox, valueBox, bar));
            y += RowGap;
        }

        return (places, y - RowGap);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Rows.Count == 0 || Bounds.Width < 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        var dim = new SolidColorBrush(palette.Dim);
        var (places, _) = Places(Bounds.Width);

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

        for (int i = 0; i < Rows.Count; i++)
        {
            var row = Rows[i];
            var place = places[i];
            double left = place.Bar.Left + Whisker, right = place.Bar.Right - Whisker;
            double At(double value) => left + (((value - lo) / (hi - lo)) * (right - left));
            double y = place.Bar.Center.Y;

            context.DrawText(NameText(row.Label, Bounds.Width - Indent, dim), place.Name.TopLeft);
            if (RowsAreLoads)
            {
                var load = LoadGroups.Brush(i, ActualThemeVariant);
                GroupDots.Marker(context, i, load, new Pen(load, 1.5), new Point(Indent / 2, place.Name.Top + (TextSize * 0.7)), 4.5);
            }

            // The interval first and heavier than the dot, because the interval is the thing that decides whether a comparison means
            // anything and the dot is only where the measurement happened to land.
            if (row.Lower is { } low && row.Upper is { } high)
            {
                double a = At(low), b = At(high);
                Marks.Line(context, Marks.Teal, new Point(a, y), new Point(b, y), 2.5);
                Marks.Line(context, Marks.Teal, new Point(a, y - Whisker), new Point(a, y + Whisker), 1.5);
                Marks.Line(context, Marks.Teal, new Point(b, y - Whisker), new Point(b, y + Whisker), 1.5);
            }

            Marks.Dot(context, Marks.Impact, new Point(At(row.Value), y), 4);
            context.DrawText(ValueText(row.Value, dim), place.Value.TopLeft);
        }
    }
}
