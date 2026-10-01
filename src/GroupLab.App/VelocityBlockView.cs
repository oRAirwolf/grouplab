using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// "Velocity and the vertical", NOTES-FROM-PLANNING.md entry 323 sections 1, 3 and 4 (Desktop B and Phone B): the desktop's block after the
/// Group block and the phone's card above All figures, one control for both so they say the same in the same order. From the top: the
/// heading with "why" at its right, closed; the share as the block's one amber figure with its interval beside it; one sentence; the meter;
/// the two bars on one scale; the chart and its sentence where shots were matched to readings; and the lines "why" opens. Every word and
/// number is <see cref="VelocityBlock"/>'s, so nothing here computes anything.
/// </summary>
internal static class VelocityBlockView
{
    /// <summary>
    /// The block. <paramref name="act"/> is what the button of states 3 and 4 does, given the state; <paramref name="whyOpen"/> whether "why"
    /// starts open, and <paramref name="whyChanged"/> hears it opened or closed so the screen can remember it.
    /// </summary>
    public static StackPanel Build(VelocityBlock block, Action<VelocityBlockState>? act, Func<double, string> speed, bool whyOpen = false, Action<bool>? whyChanged = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        ArgumentNullException.ThrowIfNull(speed);
        var column = new StackPanel { Spacing = Tokens.Space8, Name = "velocityBlock" };
        var why = new StackPanel { Spacing = Tokens.Space4, IsVisible = whyOpen };
        foreach (string line in block.Why)
        {
            why.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        }

        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        heading.Children.Add(new TextBlock { Text = VelocityBlock.Heading, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } });
        if (block.Why.Count > 0)
        {
            var open = new Button { Content = "why", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Why } };
            open.Click += (_, _) =>
            {
                why.IsVisible = !why.IsVisible;
                whyChanged?.Invoke(why.IsVisible);
            };
            Grid.SetColumn(open, 1);
            heading.Children.Add(open);
        }

        column.Children.Add(heading);
        if (!block.HasResult)
        {
            column.Children.Add(new TextBlock { Text = block.Sentence, TextWrapping = TextWrapping.Wrap });
            if (block.Action is { } words && act is not null)
            {
                var button = new Button { Content = words, MinHeight = 44, HorizontalAlignment = HorizontalAlignment.Left };
                button.Click += (_, _) => act(block.State);
                column.Children.Add(button);
            }

            return column;
        }

        // The headline and its interval: the share at the lead size in amber, the range dim beside it, wrapping beneath it on a narrow phone.
        var lead = new WrapPanel { Orientation = Orientation.Horizontal };
        lead.Children.Add(new TextBlock
        {
            Text = block.Headline,
            FontSize = block.State == VelocityBlockState.Result ? Tokens.LeadValueSize : Tokens.ValueSize,
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 0, Tokens.Space8, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            Classes = { AppStyles.HeadlineFigure },
        });
        if (block.Interval is { } interval)
        {
            lead.Children.Add(new TextBlock { Text = interval, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 4), Classes = { AppStyles.Dim } });
        }

        column.Children.Add(lead);
        column.Children.Add(new TextBlock { Text = block.Sentence, TextWrapping = TextWrapping.Wrap });
        column.Children.Add(new VelocityMeter { Share = block.Share, CannotTell = block.State == VelocityBlockState.CannotTell });
        column.Children.Add(new VelocityBars { Bars = block.Bars });
        if (block.Dots.Count > 0 && block.MeasuredSlope is { } measured && block.SolverSlope is { } solver)
        {
            column.Children.Add(new TextBlock { Text = VelocityBlock.ChartTitle, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Section } });
            column.Children.Add(new VelocityChart { Dots = block.Dots, MeasuredSlope = measured, SolverSlope = solver, Speed = speed });
        }

        if (block.SlopeSentence is { } slope)
        {
            column.Children.Add(new TextBlock { Text = slope, TextWrapping = TextWrapping.Wrap, Classes = { block.SlopeDisagrees ? AppStyles.Warn : AppStyles.Secondary } });
        }

        column.Children.Add(why);
        return column;
    }
}

/// <summary>The share from nothing to all of it: the interval a tinted segment, the value a mark, and "0%", "half" and "all of it" beneath.</summary>
internal sealed class VelocityMeter : Control
{
    private const double Pad = 8, Track = 16, Tall = 44;

    private static readonly IDashStyle Open = new DashStyle([4, 3], 0);

    public IntervalFigure? Share { get; set; }

    /// <summary>State 2: the segment runs dashed to the end, with no mark, because the data cannot say where in it the share lies.</summary>
    public bool CannotTell { get; set; }

    protected override Size MeasureOverride(Size availableSize) => new(double.IsInfinity(availableSize.Width) ? 280 : availableSize.Width, Tall);

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Share is not { } share || Bounds.Width < 2 * Pad + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = Pad, right = Bounds.Width - Pad, top = 4;
        double X(double v) => left + (Math.Clamp(v, 0, 1) * (right - left));
        context.DrawRectangle(new SolidColorBrush(palette.Faint, 0.25), null, new Rect(left, top, right - left, Track), 3, 3);
        double from = X(share.Lower), to = CannotTell ? right : X(share.Upper);
        var amber = new SolidColorBrush(palette.MarkAmber);
        context.DrawRectangle(new SolidColorBrush(palette.MarkAmber, 0.3), CannotTell ? new Pen(amber, 1.5, Open) : null, new Rect(from, top, Math.Max(2, to - from), Track), 3, 3);
        if (!CannotTell)
        {
            double at = X(share.Value);
            context.DrawLine(new Pen(amber, 3), new Point(at, top - 3), new Point(at, top + Track + 3));
        }

        foreach (var (words, x, align) in new[] { ("0%", left, 0.0), ("half", X(0.5), 0.5), ("all of it", right, 1.0) })
        {
            var text = new FormattedText(words, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(text, new Point(Math.Clamp(x - (text.Width * align), 0, Math.Max(0, Bounds.Width - text.Width)), top + Track + 6));
        }
    }
}

/// <summary>The measured vertical SD in the dim colour and the SD from velocity alone in amber, on one scale, each with its interval as whiskers.</summary>
internal sealed class VelocityBars : Control
{
    private const double Pad = 8, BarHeight = 12;

    /// <summary>A row's height: the name and value, the bar, and the size on the paper beneath the value where there is one.</summary>
    private double Row => Bars.Any(b => b.Beneath is not null) ? 62 : 46;

    public IReadOnlyList<VelocityBar> Bars { get; set; } = [];

    protected override Size MeasureOverride(Size availableSize) => new(double.IsInfinity(availableSize.Width) ? 280 : availableSize.Width, Bars.Count * Row);

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Bars.Count == 0 || Bounds.Width < 2 * Pad + 40)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double most = Bars.Max(b => Math.Max(b.Upper, b.Value)) * 1.05;
        double left = Pad, right = Bounds.Width - Pad;
        double X(double v) => left + (v / most * (right - left));
        var typeface = new Typeface(Tokens.Sans);
        for (int i = 0; i < Bars.Count; i++)
        {
            var bar = Bars[i];
            double top = i * Row;
            var colour = bar.Amber ? palette.MarkAmber : palette.Dim;
            var label = new FormattedText(bar.Label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            var value = new FormattedText(bar.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Tokens.DetailSize, new SolidColorBrush(bar.Amber ? palette.Amber : palette.Text));
            context.DrawText(label, new Point(left, top));
            context.DrawText(value, new Point(Math.Max(left + label.Width + 6, right - value.Width), top));
            if (bar.Beneath is { } paper)
            {
                var under = new FormattedText(paper, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Tokens.DetailSize, new SolidColorBrush(palette.Dim));
                context.DrawText(under, new Point(Math.Max(left, right - under.Width), top + 16));
            }

            double y = top + (bar.Beneath is null ? 20 : 36);
            context.FillRectangle(new SolidColorBrush(colour, bar.Amber ? 0.85 : 0.55), new Rect(left, y, Math.Max(2, X(bar.Value) - left), BarHeight));
            var whisker = new Pen(new SolidColorBrush(palette.Text), 1.5);
            double mid = y + (BarHeight / 2);
            context.DrawLine(whisker, new Point(X(bar.Lower), mid), new Point(X(bar.Upper), mid));
            foreach (double end in new[] { bar.Lower, bar.Upper })
            {
                context.DrawLine(whisker, new Point(X(end), y - 2), new Point(X(end), y + BarHeight + 2));
            }
        }
    }
}

/// <summary>
/// Each matched shot's height against the velocity read for it, entry 323 section 1.6: the measured regression solid and the solver's slope
/// dashed in amber, both through the shots' mean, with a small legend.
/// </summary>
internal sealed class VelocityChart : Control
{
    private const double PadLeft = 12, PadRight = 12, PadTop = 22, PadBottom = 24, Tall = 200;

    private static readonly IDashStyle SolverDash = new DashStyle([6, 3], 0);

    public IReadOnlyList<VelocityDot> Dots { get; set; } = [];

    public double MeasuredSlope { get; set; }

    public double SolverSlope { get; set; }

    public Func<double, string> Speed { get; set; } = fps => string.Create(CultureInfo.InvariantCulture, $"{fps:0} ft/s");

    protected override Size MeasureOverride(Size availableSize) => new(double.IsInfinity(availableSize.Width) ? 280 : availableSize.Width, Tall);

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Dots.Count < 2 || Bounds.Width < PadLeft + PadRight + 60)
        {
            return;
        }

        var palette = Tokens.For(ActualThemeVariant);
        double left = PadLeft, right = Bounds.Width - PadRight, top = PadTop, bottom = Bounds.Height - PadBottom;
        double mx = Dots.Average(d => d.Fps), my = Dots.Average(d => d.UpInches);
        double lowX = Dots.Min(d => d.Fps), highX = Dots.Max(d => d.Fps);
        if (highX - lowX < 1e-9)
        {
            (lowX, highX) = (lowX - 5, highX + 5);
        }

        double padX = (highX - lowX) * 0.08;
        (lowX, highX) = (lowX - padX, highX + padX);
        var heights = Dots.Select(d => d.UpInches).Concat(new[] { lowX, highX }.SelectMany(x => new[] { my + (MeasuredSlope * (x - mx)), my + (SolverSlope * (x - mx)) })).ToList();
        double lowY = heights.Min(), highY = heights.Max();
        if (highY - lowY < 1e-9)
        {
            (lowY, highY) = (lowY - 0.5, highY + 0.5);
        }

        double padY = (highY - lowY) * 0.08;
        (lowY, highY) = (lowY - padY, highY + padY);
        Point P(double fps, double up) => new(left + ((fps - lowX) / (highX - lowX) * (right - left)), bottom - ((up - lowY) / (highY - lowY) * (bottom - top)));

        var axis = new Pen(new SolidColorBrush(palette.Faint), 1);
        context.DrawLine(axis, new Point(left, bottom), new Point(right, bottom));
        context.DrawLine(axis, new Point(left, top), new Point(left, bottom));
        using (context.PushClip(new Rect(left, top, right - left, bottom - top)))
        {
            context.DrawLine(new Pen(new SolidColorBrush(palette.Text), 2), P(lowX, my + (MeasuredSlope * (lowX - mx))), P(highX, my + (MeasuredSlope * (highX - mx))));
            context.DrawLine(new Pen(new SolidColorBrush(palette.MarkAmber), 2, SolverDash), P(lowX, my + (SolverSlope * (lowX - mx))), P(highX, my + (SolverSlope * (highX - mx))));
            foreach (var dot in Dots)
            {
                Marks.Dot(context, new SolidColorBrush(palette.Text), P(dot.Fps, dot.UpInches), 3.5);
            }
        }

        var typeface = new Typeface(Tokens.Sans);
        FormattedText Text(string words, Color colour) => new(words, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, Tokens.DetailSize, new SolidColorBrush(colour));

        // The legend at the top, the two ends of the velocity axis at the bottom, and which way is up.
        var measured = Text("measured", palette.Dim);
        var solver = Text("solver", palette.Dim);
        double x = left + 4;
        context.DrawLine(new Pen(new SolidColorBrush(palette.Text), 2), new Point(x, 9), new Point(x + 16, 9));
        context.DrawText(measured, new Point(x + 20, 1));
        x += 20 + measured.Width + 14;
        context.DrawLine(new Pen(new SolidColorBrush(palette.MarkAmber), 2, SolverDash), new Point(x, 9), new Point(x + 16, 9));
        context.DrawText(solver, new Point(x + 20, 1));
        var higher = Text("higher on the target", palette.Dim);
        context.DrawText(higher, new Point(Math.Max(x + 20 + solver.Width + 12, right - higher.Width), 1));
        var slow = Text(Speed(Dots.Min(d => d.Fps)), palette.Dim);
        var fast = Text(Speed(Dots.Max(d => d.Fps)), palette.Dim);
        context.DrawText(slow, new Point(P(Dots.Min(d => d.Fps), lowY).X - (slow.Width / 2) < 0 ? 0 : P(Dots.Min(d => d.Fps), lowY).X - (slow.Width / 2), bottom + 4));
        context.DrawText(fast, new Point(Math.Min(Bounds.Width - fast.Width, P(Dots.Max(d => d.Fps), lowY).X - (fast.Width / 2)), bottom + 4));
    }
}
