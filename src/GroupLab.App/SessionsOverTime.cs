using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>One session of one load: when it was shot, what it measured, and how sure that measurement is.</summary>
/// <param name="When">The date it was shot, or the date it was recorded where the shot date is not known.</param>
/// <param name="MeanRadiusInches">The session's mean radius.</param>
/// <param name="LowerInches">The bottom of its interval, null where the session has none.</param>
/// <param name="UpperInches">The top of its interval.</param>
/// <param name="Shots">How many shots that session rests on.</param>
internal sealed record SessionPoint(DateTime When, double MeanRadiusInches, double? LowerInches, double? UpperInches, int Shots);

/// <summary>
/// Is this load getting better or worse? NOTES-FROM-PLANNING.md entry 141 section 5.2.4.
/// <para>
/// <b>The trap here is sharper than in any other picture on this screen.</b> Four sessions of the same load, plotted against the date, will
/// climb or fall. They always do. A shooter looking at that line sees a barrel wearing, or a batch of powder going off, or their own
/// technique improving, and every one of those readings is a story told about four numbers that a coin could have produced. So the line is
/// not drawn at all: the sessions are dots with their intervals, and the caption says whether the sessions can separate them.
/// </para>
/// <para>
/// <b>The test is the same one the shot order chart uses</b>, <see cref="ShotOrderTrend"/>, because it is the same question: do later
/// values in a known order sit higher than earlier ones, more than a shuffle of the same values would. Asking it of sessions rather than
/// shots changes nothing about the arithmetic, so it is not written twice.
/// </para>
/// <para>
/// Each session's own interval is drawn as well as the dot, because two sessions of five shots each are a far weaker comparison than two of
/// thirty, and nothing else on the chart would show that.
/// </para>
/// </summary>
internal sealed class SessionsOverTime : Control
{
    private const double PadLeft = 64;

    private const double PadRight = 16;

    private const double PadTop = 12;

    private const double PadBottom = 26;

    private const double Tall = 190;

    /// <summary>How far inside the axes the first and last sessions sit, so neither dot nor whisker is drawn on the axis itself.</summary>
    private const double Inset = 16;

    /// <summary>The fewest sessions that can show anything at all: one point is not a comparison.</summary>
    public const int FewestSessions = 2;

    /// <summary>The sessions of the one load, oldest first. The caller sorts them; this never reorders them.</summary>
    public IReadOnlyList<SessionPoint> Points { get; set; } = [];

    /// <summary>The load these sessions are of, which the caption names so the chart cannot be read as being about another one.</summary>
    public string? Load { get; set; }

    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    public SessionsOverTime()
    {
        ClipToBounds = true;
    }

    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Tall);

    /// <summary>The trend across the sessions in date order, or null where there are too few sessions to test.</summary>
    public OrderTrend? Trend =>
        Points.Count >= ShotOrderTrend.FewestShots ? ShotOrderTrend.Of([.. Points.Select(p => p.MeanRadiusInches)]) : null;

    /// <summary>Whether every session's interval overlaps every other's, which is the case worth saying out loud.</summary>
    public bool AllOverlap
    {
        get
        {
            var known = Points.Where(p => p.LowerInches is not null && p.UpperInches is not null).ToList();
            return known.Count >= 2 && known.Max(p => p.LowerInches!.Value) <= known.Min(p => p.UpperInches!.Value);
        }
    }

    /// <summary>
    /// What the chart says in words, for a screen reader and for the headless tests. It never says a load is improving or going off without
    /// saying whether the sessions can tell.
    /// </summary>
    public string Description
    {
        get
        {
            string load = string.IsNullOrWhiteSpace(Load) ? "this load" : Load;
            if (Points.Count == 0)
            {
                return $"No sessions of {load} yet.";
            }

            if (Points.Count < FewestSessions)
            {
                return $"One session of {load}. Two or more are needed to see whether it is getting better or worse.";
            }

            string sizes = $"{Points.Count} sessions of {load}, from {Length(Points.Min(p => p.MeanRadiusInches))} to {Length(Points.Max(p => p.MeanRadiusInches))} mean radius.";

            if (Trend is not { } trend)
            {
                return sizes + $" {Points.Count} sessions cannot show a trend at all; {ShotOrderTrend.FewestShots} is the fewest that could.";
            }

            if (trend.PValue < 0.05)
            {
                string which = trend.Correlation < 0 ? "tightening as the sessions go on" : "opening up as the sessions go on";
                return sizes + $" {load} is {which}, and {Points.Count} sessions are enough to say so.";
            }

            return AllOverlap
                ? sizes + $" Every session's interval overlaps every other, so these {Points.Count} sessions cannot separate them at all."
                : sizes + $" The sessions go up and down, but {Points.Count} of them cannot tell that from chance.";
        }
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Points.Count == 0 || Bounds.Width < PadLeft + PadRight + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight;
        double top = PadTop, bottom = Tall - PadBottom;

        // The scale covers every interval, not only every dot, so a whisker is never drawn off the top of the picture.
        double low = Points.Min(p => p.LowerInches ?? p.MeanRadiusInches);
        double high = Points.Max(p => p.UpperInches ?? p.MeanRadiusInches);
        if (high - low < 1e-9)
        {
            low -= 0.05;
            high += 0.05;
        }

        double margin = (high - low) * 0.1;
        low -= margin;
        high += margin;

        // Zero belongs on this axis wherever it fits: a mean radius is a distance from a centre, and a scale starting at 0.40 in makes an
        // ordinary difference between sessions look enormous. It is dropped only where including it would flatten them into one line.
        if (low > 0 && low < (high - low) * 0.6)
        {
            low = 0;
        }

        double Y(double inches) => bottom - ((inches - low) / (high - low) * (bottom - top));

        // Time along the bottom, spaced by date, because sessions a year apart and sessions an hour apart are different evidence and even
        // spacing would hide that. Sessions all at one moment fall back to even spacing rather than stacking on one pixel.
        double first = Points.Min(p => p.When).ToOADate(), last = Points.Max(p => p.When).ToOADate();
        bool byDate = last - first > 1e-6;
        double from = left + Inset, to = right - Inset;
        double X(int index) => Points.Count == 1
            ? (from + to) / 2
            : byDate
                ? from + ((Points[index].When.ToOADate() - first) / (last - first) * (to - from))
                : from + ((double)index / (Points.Count - 1) * (to - from));

        Marks.Line(context, Marks.Faint, new Point(left, bottom), new Point(right, bottom), 1);
        Marks.Line(context, Marks.Faint, new Point(left, top), new Point(left, bottom), 1);

        foreach (double at in new[] { low, (low + high) / 2, high })
        {
            var text = new FormattedText(Length(at), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(text, new Point(Math.Max(0, left - text.Width - 6), Y(at) - (text.Height / 2)));
        }

        for (int i = 0; i < Points.Count; i++)
        {
            var point = Points[i];
            double x = X(i);
            // Teal for the interval and the impact colour for the measurement, the same way round as the compare charts, and heavier than
            // the dot for the same reason: the interval decides whether a comparison means anything, the dot is only where it landed.
            if (point.LowerInches is { } lower && point.UpperInches is { } upper)
            {
                Marks.Line(context, Marks.Teal, new Point(x, Y(lower)), new Point(x, Y(upper)), 2.5);
                Marks.Line(context, Marks.Teal, new Point(x - 5, Y(lower)), new Point(x + 5, Y(lower)), 1.5);
                Marks.Line(context, Marks.Teal, new Point(x - 5, Y(upper)), new Point(x + 5, Y(upper)), 1.5);
            }

            Marks.Dot(context, Marks.Impact, new Point(x, Y(point.MeanRadiusInches)), 4);
        }

        // The first and last dates only. A label under every dot is unreadable at six sessions and useless at twenty.
        foreach ((int index, bool start) in new[] { (0, true), (Points.Count - 1, false) })
        {
            if (Points.Count < 2 && !start)
            {
                continue;
            }

            var when = new FormattedText(Points[index].When.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize,
                new SolidColorBrush(palette.Dim));
            double at = start ? X(index) : X(index) - when.Width;
            context.DrawText(when, new Point(Math.Clamp(at, 0, Math.Max(0, Bounds.Width - when.Width)), bottom + 6));
        }
    }
}
