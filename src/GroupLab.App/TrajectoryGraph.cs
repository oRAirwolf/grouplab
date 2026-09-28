using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;

namespace GroupLab.App;

/// <summary>Which of the trajectory's four series the graph is drawing.</summary>
internal enum TrajectorySeries
{
    Drop,
    Wind,
    Velocity,
    Energy,
}

/// <summary>
/// The trajectory as a curve against range, NOTES-FROM-PLANNING.md entry 131 section 8.
/// <para>
/// <b>What it adds to the dope table beside it.</b> A table answers "what do I dial at 600" exactly, and a graph cannot. What the table
/// cannot show is shape: where the drop starts running away, how far out the velocity is still supersonic, whether the wind is worth more
/// than the drop at the distance somebody actually shoots. Those are read off a curve in a second and pieced together from a column of
/// numbers slowly, or not at all.
/// </para>
/// <para>
/// One series at a time, because four curves on one pair of axes with four different units is a chart nobody reads. The zero range is
/// marked, because it is the one place on the curve the shooter chose.
/// </para>
/// </summary>
internal sealed class TrajectoryGraph : Control
{
    private const double PadLeft = 54;

    private const double PadRight = 12;

    private const double PadTop = 12;

    private const double PadBottom = 28;

    public IReadOnlyList<TrajectoryPoint> Points { get; set; } = [];

    public TrajectorySeries Series { get; set; } = TrajectorySeries.Drop;

    /// <summary>The distance the rifle is zeroed at, in yards, marked on the curve.</summary>
    public double? ZeroYards { get; set; }

    /// <summary>How a distance reads in the person's units, set by the window.</summary>
    public Func<double, string> Distance { get; set; } = yards => string.Create(CultureInfo.InvariantCulture, $"{yards:0} yd");

    /// <summary>
    /// Entry 247: the range chosen on the screen's right, marked on the curve in amber with its value, the one place besides the zero the
    /// person asked about. Null draws nothing.
    /// </summary>
    public double? ChosenYards { get; set; }

    /// <summary>How the chosen point's value reads beside it, set by the window.</summary>
    public Func<double, string> ValueText { get; set; } = value => value.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>A point on the curve clicked: the range of the row nearest it, in yards.</summary>
    public event Action<double>? RangeChosen;

    public TrajectoryGraph()
    {
        ClipToBounds = true;
        MinHeight = 240;
        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand);
        PointerPressed += (_, e) =>
        {
            if (Points.Count < 2 || Bounds.Width <= PadLeft + PadRight)
            {
                return;
            }

            double yards = (e.GetPosition(this).X - PadLeft) / (Bounds.Width - PadLeft - PadRight) * Points[^1].RangeYards;
            var nearest = Points.Where(p => p.RangeYards > 0).MinBy(p => Math.Abs(p.RangeYards - yards));
            if (nearest is not null)
            {
                RangeChosen?.Invoke(nearest.RangeYards);
            }
        };
    }

    /// <summary>What this series is called and what its numbers are in, for the axis and the button.</summary>
    public static string Title(TrajectorySeries series) => series switch
    {
        TrajectorySeries.Drop => "Drop, in",
        TrajectorySeries.Wind => "Wind drift, in",
        TrajectorySeries.Velocity => "Velocity, ft/s",
        _ => "Energy, ft lb",
    };

    private static double Value(TrajectoryPoint p, TrajectorySeries series) => series switch
    {
        TrajectorySeries.Drop => p.DropInches,
        TrajectorySeries.Wind => p.WindInches,
        TrajectorySeries.Velocity => p.VelocityFps,
        _ => p.EnergyFtLb,
    };

    /// <summary>What the graph is showing, in words, for a screen reader and for the headless tests.</summary>
    public string Description
    {
        get
        {
            if (Points.Count < 2)
            {
                return "No trajectory to draw yet.";
            }

            var last = Points[^1];
            return string.Create(CultureInfo.InvariantCulture,
                $"{Title(Series)} against range, out to {last.RangeYards:0} yd, ending at {Value(last, Series):0.#}.");
        }
    }

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
        double maxRange = Points[^1].RangeYards;
        double lo = Points.Min(p => Value(p, Series)), hi = Points.Max(p => Value(p, Series));
        if (Math.Abs(hi - lo) < 1e-9)
        {
            hi = lo + 1;
        }

        // A little air above and below, so the curve never runs along an edge.
        double pad = (hi - lo) * 0.08;
        lo -= pad;
        hi += pad;

        Point At(double yards, double value) => new(
            left + ((yards / maxRange) * (right - left)),
            bottom - (((value - lo) / (hi - lo)) * (bottom - top)));

        var axis = new Pen(new SolidColorBrush(palette.Line), 1);
        context.DrawLine(axis, new Point(left, top), new Point(left, bottom));
        context.DrawLine(axis, new Point(left, bottom), new Point(right, bottom));

        // Three labelled gridlines on the value axis, which is enough to read a curve by and few enough to stay quiet.
        for (int i = 0; i <= 2; i++)
        {
            double value = lo + ((hi - lo) * i / 2);
            double y = At(0, value).Y;
            context.DrawLine(new Pen(new SolidColorBrush(palette.Line), 1, Marks.Dashed), new Point(left, y), new Point(right, y));
            var text = new FormattedText($"{value:0.#}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(text, new Point(Math.Max(0, left - text.Width - 6), y - (text.Height / 2)));
        }

        // The range axis, at each end.
        foreach (double yards in new[] { 0, maxRange })
        {
            var text = new FormattedText(Distance(yards), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            double x = At(yards, lo).X;
            context.DrawText(text, new Point(Math.Min(right - text.Width, Math.Max(left, x - (text.Width / 2))), bottom + 6));
        }

        // The zero, which is the one place on this curve the shooter chose.
        if (ZeroYards is { } zero && zero > 0 && zero <= maxRange)
        {
            double x = At(zero, lo).X;
            Marks.Line(context, Marks.Faint, new Point(x, top), new Point(x, bottom), 1, Marks.Dashed);
            var text = new FormattedText("zero", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, new SolidColorBrush(palette.Dim));
            context.DrawText(text, new Point(Math.Min(right - text.Width, x + 4), top));
        }

        var curve = new StreamGeometry();
        using (var draw = curve.Open())
        {
            draw.BeginFigure(At(Points[0].RangeYards, Value(Points[0], Series)), false);
            foreach (var p in Points.Skip(1))
            {
                draw.LineTo(At(p.RangeYards, Value(p, Series)));
            }

            draw.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(Marks.Teal, 2), curve);

        // Entry 247: the chosen range, its line and its point in amber, and what the curve says there.
        if (ChosenYards is { } chosen && chosen > 0 && chosen <= maxRange && Points.MinBy(p => Math.Abs(p.RangeYards - chosen)) is { } at)
        {
            var amber = new SolidColorBrush(palette.Amber);
            var point = At(at.RangeYards, Value(at, Series));
            context.DrawLine(new Pen(amber, 1), new Point(point.X, top), new Point(point.X, bottom));
            context.DrawEllipse(amber, null, point, 5, 5);
            var text = new FormattedText($"{Distance(at.RangeYards)}, {ValueText(Value(at, Series))}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Mono), Tokens.DetailSize, amber);
            bool flipped = point.X + 8 + text.Width > right;
            double x = flipped ? point.X - 8 - text.Width : point.X + 8;

            // Turned to the left, the label goes on the side of the point the curve arrives from least, so the line never runs through it.
            var before = Points.LastOrDefault(p => p.RangeYards < at.RangeYards);
            bool below = flipped && before is not null && At(before.RangeYards, Value(before, Series)).Y < point.Y;
            double y = below ? Math.Min(bottom - text.Height, point.Y + 6) : Math.Max(top, point.Y - text.Height - 4);
            context.DrawText(text, new Point(x, y));
        }
    }
}
