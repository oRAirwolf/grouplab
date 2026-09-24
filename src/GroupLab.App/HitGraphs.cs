using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// The simulated impacts with the target drawn on them, NOTES-FROM-PLANNING.md entry 156 section 3 item 3: the first round in teal and the
/// second round, corrected from where the first landed, in the impact color, so the tighter second cloud is seen beside the first.
/// </summary>
internal sealed class HitScatter : Control
{
    private const double Pad = 12;

    public IReadOnlyList<PointD> First { get; set; } = [];

    public IReadOnlyList<PointD> Second { get; set; } = [];

    public HitTarget? Target { get; set; }

    /// <summary>How a length at the target reads in the person's units, set by the window.</summary>
    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.0} in");

    public HitScatter()
    {
        ClipToBounds = true;
        MinHeight = 280;
        MaxWidth = 520;
    }

    /// <summary>What the drawing shows, in words, for a screen reader and for the headless tests.</summary>
    public string Description => Target is null || First.Count == 0
        ? "No simulated impacts to draw yet."
        : string.Create(CultureInfo.InvariantCulture,
            $"{First.Count} first-round impacts and {Second.Count} second-round impacts over the target, {Length(Target.WidthInches)} wide.");

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        if (Target is not { } target || First.Count == 0 || Bounds.Width < 60 || Bounds.Height < 60)
        {
            return;
        }

        // The view holds the target and the middle of the cloud: the spread of the impacts past the 97th percentile would shrink everything else.
        static double Reach(IEnumerable<double> values)
        {
            var sorted = values.Select(Math.Abs).Order().ToList();
            return sorted.Count == 0 ? 0 : sorted[(int)(0.97 * (sorted.Count - 1))];
        }

        double halfWidth = Math.Max(target.WidthInches / 2, Reach(First.Select(p => p.X))) * 1.1;
        double halfHeight = Math.Max(target.HeightInches / 2, Reach(First.Select(p => p.Y))) * 1.1;
        double half = Math.Max(halfWidth, halfHeight);
        double size = Math.Min(Bounds.Width, Bounds.Height) - (2 * Pad);
        var centre = new Point(Bounds.Width / 2, Bounds.Height / 2);
        double scale = size / (2 * half);
        Point At(double across, double up) => new(centre.X + (across * scale), centre.Y - (up * scale));

        var outline = new Pen(new SolidColorBrush(palette.Text), 1.5);
        switch (target.Shape)
        {
            case HitShape.Circle:
                context.DrawEllipse(null, outline, centre, target.WidthInches / 2 * scale, target.WidthInches / 2 * scale);
                break;
            case HitShape.Rectangle:
                context.DrawRectangle(null, outline, new Rect(At(-target.WidthInches / 2, target.HeightInches / 2), At(target.WidthInches / 2, -target.HeightInches / 2)));
                break;
            default:
                double w = target.WidthInches / 2, body = target.HeightInches * HitTarget.IpscBodyShare / 2, cut = target.WidthInches / 6, head = target.WidthInches / 6;
                double top = body + (target.HeightInches * (1 - HitTarget.IpscBodyShare));
                var shape = new StreamGeometry();
                using (var draw = shape.Open())
                {
                    draw.BeginFigure(At(-w + cut, -body), false);
                    foreach (var (x, y) in new[] { (w - cut, -body), (w, -body + cut), (w, body - cut), (w - cut, body), (head, body), (head, top), (-head, top), (-head, body), (-w + cut, body), (-w, body - cut), (-w, -body + cut) })
                    {
                        draw.LineTo(At(x, y));
                    }

                    draw.EndFigure(true);
                }

                context.DrawGeometry(null, outline, shape);
                break;
        }

        foreach (var p in Second)
        {
            context.FillRectangle(Marks.Impact, new Rect(At(p.X, p.Y) - new Point(1, 1), new Size(2, 2)));
        }

        foreach (var p in First)
        {
            context.FillRectangle(Marks.Teal, new Rect(At(p.X, p.Y) - new Point(1, 1), new Size(2, 2)));
        }

        Marks.Cross(context, Marks.Faint, centre, 6, 1);
        var legend = new FormattedText("teal: first round    red: second round, corrected from the first", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
        context.DrawText(legend, new Point(Pad, Bounds.Height - legend.Height - 4));
    }
}

/// <summary>Hit probability against distance, entry 156 section 3 item 4: the first round as a line, its interval as a band, and the chosen distance marked.</summary>
internal sealed class HitCurve : Control
{
    private const double PadLeft = 44;

    private const double PadRight = 12;

    private const double PadTop = 12;

    private const double PadBottom = 28;

    public IReadOnlyList<HitCurvePoint> Points { get; set; } = [];

    /// <summary>The distance the answer above it is for, marked on the curve.</summary>
    public double? MarkYards { get; set; }

    /// <summary>How a distance reads in the person's units, set by the window.</summary>
    public Func<double, string> Distance { get; set; } = yards => string.Create(CultureInfo.InvariantCulture, $"{yards:0} yd");

    public HitCurve()
    {
        ClipToBounds = true;
        MinHeight = 200;
    }

    /// <summary>What the curve shows, in words, for a screen reader and for the headless tests.</summary>
    public string Description => Points.Count < 2
        ? "No curve to draw yet."
        : string.Create(CultureInfo.InvariantCulture,
            $"First-round hit probability against distance, from {Distance(Points[0].Yards)} to {Distance(Points[^1].Yards)}, falling from {100 * Points[0].Value:0} to {100 * Points[^1].Value:0} percent, with its interval as a band.");

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        if (Points.Count < 2 || Bounds.Width < PadLeft + PadRight + 20 || Bounds.Height < PadTop + PadBottom + 20)
        {
            return;
        }

        double left = PadLeft, right = Bounds.Width - PadRight, top = PadTop, bottom = Bounds.Height - PadBottom;
        double from = Points[0].Yards, to = Points[^1].Yards;
        Point At(double yards, double p) => new(left + ((yards - from) / (to - from) * (right - left)), bottom - (p * (bottom - top)));

        var axis = new Pen(new SolidColorBrush(palette.Line), 1);
        context.DrawLine(axis, new Point(left, top), new Point(left, bottom));
        context.DrawLine(axis, new Point(left, bottom), new Point(right, bottom));
        foreach (double p in new[] { 0.0, 0.5, 1.0 })
        {
            double y = At(from, p).Y;
            context.DrawLine(new Pen(new SolidColorBrush(palette.Line), 1, Marks.Dashed), new Point(left, y), new Point(right, y));
            var text = new FormattedText($"{100 * p:0}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(text, new Point(Math.Max(0, left - text.Width - 6), y - (text.Height / 2)));
        }

        foreach (double yards in new[] { from, to })
        {
            var text = new FormattedText(Distance(yards), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            double x = At(yards, 0).X;
            context.DrawText(text, new Point(Math.Min(right - text.Width, Math.Max(left, x - (text.Width / 2))), bottom + 6));
        }

        var band = new StreamGeometry();
        using (var draw = band.Open())
        {
            draw.BeginFigure(At(Points[0].Yards, Points[0].Upper), true);
            foreach (var p in Points.Skip(1))
            {
                draw.LineTo(At(p.Yards, p.Upper));
            }

            foreach (var p in Points.Reverse())
            {
                draw.LineTo(At(p.Yards, p.Lower));
            }

            draw.EndFigure(true);
        }

        context.DrawGeometry(new SolidColorBrush(Tokens.MarkTeal, 0.22), null, band);
        var line = new StreamGeometry();
        using (var draw = line.Open())
        {
            draw.BeginFigure(At(Points[0].Yards, Points[0].Value), false);
            foreach (var p in Points.Skip(1))
            {
                draw.LineTo(At(p.Yards, p.Value));
            }

            draw.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Marks.Teal, 2), line);
        if (MarkYards is { } mark && mark >= from && mark <= to)
        {
            double x = At(mark, 0).X;
            Marks.Line(context, Marks.Faint, new Point(x, top), new Point(x, bottom), 1, Marks.Dashed);
        }
    }
}
