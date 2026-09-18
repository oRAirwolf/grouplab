using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>One shot on the composite plot: its offset from its own bull's aim point in inches at the target, and whether it is excluded.</summary>
internal sealed record PlotShot(int Id, string Label, PointD Offset, bool Excluded);

/// <summary>One disc of the bull's artwork, outermost first, at its diameter in inches.</summary>
internal sealed record PlotDisc(double DiameterInches, Color Colour);

/// <summary>
/// The composite plot, NOTES-FROM-PLANNING.md entry 103 section 1: the centre of the analysis state. One bull's artwork from the definition,
/// centred on the aim point, with every scoring shot's offset from its own bull's aim point plotted on it.
/// <list type="bullet">
/// <item>Sighters are never given to it. Marks set to not a shot are absent, because they are not shots. Excluded shots are drawn hollow
/// and dim, not removed: excluding is a human judgement, and a plot that hid it would make the group look better than the evidence.</item>
/// <item>A shot is a circle at the calibre's diameter when a calibre is set and a point when it is not, and the legend says which.</item>
/// <item>The group centre is marked, with CEP 50 and CEP 90 as dashed circles about it.</item>
/// <item>Extreme spread is the line between the two shots that make it, not a circle: a circle that size reads as a region the shots are
/// contained in, and extreme spread is the distance between two particular shots. Clicking the line picks both.</item>
/// <item>The view frames the shots with a margin and draws the rings behind them at true relative scale, running off the frame when the group
/// is much smaller than the bull, because a small group scaled to fit a large ring is a dot.</item>
/// </list>
/// </summary>
internal sealed class CompositePlot : Control
{
    /// <summary>The margin around the shots, as a share of their extent on each side.</summary>
    private const double FrameMargin = 0.35;

    /// <summary>The smallest extent the view frames, in inches, so one shot or a tight cluster is not magnified without limit.</summary>
    private const double MinimumExtentInches = 0.25;

    public IReadOnlyList<PlotDisc> Discs { get; set; } = [];

    public IReadOnlyList<PlotShot> Shots { get; set; } = [];

    public double? CalibreInches { get; set; }

    public PointD? Centre { get; set; }

    public double? Cep50Inches { get; set; }

    public double? Cep90Inches { get; set; }

    /// <summary>The two shots that make the extreme spread, by id.</summary>
    public (int First, int Second)? SpreadPair { get; set; }

    public IReadOnlySet<int> Selected { get; set; } = new HashSet<int>();

    /// <summary>Raised when a click picks shots: one shot, or the extreme spread's two.</summary>
    public event EventHandler<IReadOnlyList<int>>? ShotsClicked;

    /// <summary>The legend's entries, in the order drawn, for the headless tests.</summary>
    public IReadOnlyList<string> Legend
    {
        get
        {
            int kept = Shots.Count(s => !s.Excluded), excluded = Shots.Count - kept;
            var lines = new List<string>
            {
                CalibreInches is { } calibre
                    ? string.Create(CultureInfo.InvariantCulture, $"{kept} shots, each drawn at the {calibre:0.000} in calibre")
                    : $"{kept} shots, drawn as points: no calibre is set, so there is no hole size to draw",
            };
            if (excluded > 0)
            {
                lines.Add($"{excluded} excluded, drawn hollow");
            }

            if (SpreadPair is { } pair)
            {
                lines.Add($"extreme spread, shots {Label(pair.First)} and {Label(pair.Second)}");
            }

            if (Cep50Inches is not null)
            {
                lines.Add("CEP 50 and 90");
            }

            if (Centre is not null)
            {
                lines.Add("group centre");
            }

            if (Discs.Count == 0)
            {
                lines.Add("no bull drawn: the sheet's definition is not known");
            }

            return lines;
        }
    }

    public CompositePlot()
    {
        ClipToBounds = true;
        MinHeight = 200;
        PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Pick(e.GetPosition(this)) is { Count: > 0 } picked)
            {
                ShotsClicked?.Invoke(this, picked);
                e.Handled = true;
            }
        };
    }

    /// <summary>Where an offset in inches lands on the control.</summary>
    public Point ToScreen(PointD inches)
    {
        var (scale, origin) = Frame(PlotArea(Bounds.Size));
        return new Point(origin.X + (inches.X * scale), origin.Y + (inches.Y * scale));
    }

    /// <summary>
    /// What a click at <paramref name="at"/> picks: the shot under it, within its drawn circle or a few pixels of a point, and otherwise both
    /// shots of the extreme spread when it lands on that line.
    /// </summary>
    public IReadOnlyList<int> Pick(Point at)
    {
        var (scale, _) = Frame(PlotArea(Bounds.Size));
        double reach = Math.Max(8, (CalibreInches ?? 0) * scale / 2);
        var nearest = Shots
            .Select(s => (Shot: s, Distance: Distance(ToScreen(s.Offset), at)))
            .Where(x => x.Distance <= reach)
            .OrderBy(x => x.Distance)
            .FirstOrDefault();
        if (nearest.Shot is not null)
        {
            return [nearest.Shot.Id];
        }

        if (SpreadPair is { } pair && Shots.FirstOrDefault(s => s.Id == pair.First) is { } a && Shots.FirstOrDefault(s => s.Id == pair.Second) is { } b
            && DistanceToSegment(at, ToScreen(a.Offset), ToScreen(b.Offset)) <= 6)
        {
            return [a.Id, b.Id];
        }

        return [];
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.FillRectangle(new SolidColorBrush(Tokens.For(ActualThemeVariant).Sunk), new Rect(Bounds.Size));
        var area = PlotArea(Bounds.Size);
        var (scale, origin) = Frame(area);
        using (context.PushClip(area))
        {
            // The bull at true relative scale, centred on the aim point, whether or not it fits.
            foreach (var disc in Discs)
            {
                context.DrawEllipse(new SolidColorBrush(disc.Colour), null, origin, disc.DiameterInches * scale / 2, disc.DiameterInches * scale / 2);
            }

            Marks.Cross(context, Marks.Faint, origin, 6, 1);

            if (Centre is { } centre)
            {
                var c = ToScreen(centre);
                foreach (double? radius in new[] { Cep50Inches, Cep90Inches })
                {
                    if (radius is { } r)
                    {
                        Marks.Ring(context, Marks.Teal, c, r * scale, 1, Marks.Dashed);
                    }
                }
            }

            foreach (var shot in Shots.Where(s => s.Excluded))
            {
                DrawShot(context, shot, scale);
            }

            foreach (var shot in Shots.Where(s => !s.Excluded))
            {
                DrawShot(context, shot, scale);
            }

            if (SpreadPair is { } pair && Shots.FirstOrDefault(s => s.Id == pair.First) is { } a && Shots.FirstOrDefault(s => s.Id == pair.Second) is { } b)
            {
                Marks.Line(context, Marks.Selected, ToScreen(a.Offset), ToScreen(b.Offset), 1.5, Marks.Dashed);
            }

            if (Centre is { } groupCentre)
            {
                Marks.Cross(context, Marks.Teal, ToScreen(groupCentre), 8, 1.5);
            }
        }

        var legend = LegendText(Bounds.Size);
        context.DrawText(legend, new Point(8, Bounds.Height - legend.Height - 6));
    }

    private void DrawShot(DrawingContext context, PlotShot shot, double scale)
    {
        var at = ToScreen(shot.Offset);
        bool selected = Selected.Contains(shot.Id);
        IBrush brush = selected ? Marks.Selected : shot.Excluded ? Marks.Excluded : Marks.Impact;
        if (CalibreInches is { } calibre)
        {
            double radius = Math.Max(2, calibre * scale / 2);
            if (!shot.Excluded)
            {
                context.DrawEllipse(new SolidColorBrush(((ISolidColorBrush)brush).Color, 0.08), null, at, radius, radius);
            }

            Marks.Ring(context, brush, at, radius, selected ? 2 : 1, shot.Excluded ? Marks.Dashed : null);
        }
        else if (shot.Excluded)
        {
            Marks.Ring(context, brush, at, 3.5, 1);
        }
        else
        {
            Marks.Dot(context, brush, at, selected ? 4.5 : 3.5);
        }
    }

    private string Label(int id) => Shots.FirstOrDefault(s => s.Id == id)?.Label ?? id.ToString(CultureInfo.InvariantCulture);

    /// <summary>The legend as drawn, wrapped to the control's width, so it is never cut off at the edge.</summary>
    private FormattedText LegendText(Size size) => new(string.Join("   \u00b7   ", Legend), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.SecondarySize, Marks.Faint)
    {
        MaxTextWidth = Math.Max(40, size.Width - 16),
        TextAlignment = TextAlignment.Center,
    };

    private Rect PlotArea(Size size) => new(0, 0, size.Width, Math.Max(1, size.Height - LegendText(size).Height - 12));

    /// <summary>The scale in pixels per inch and where the aim point lands, framing the shots with a margin.</summary>
    private (double Scale, Point Origin) Frame(Rect area)
    {
        double half = (CalibreInches ?? 0) / 2;
        double minX = 0, maxX = 0, minY = 0, maxY = 0;
        if (Shots.Count > 0)
        {
            minX = Shots.Min(s => s.Offset.X) - half;
            maxX = Shots.Max(s => s.Offset.X) + half;
            minY = Shots.Min(s => s.Offset.Y) - half;
            maxY = Shots.Max(s => s.Offset.Y) + half;
        }

        double width = Math.Max(maxX - minX, MinimumExtentInches) * (1 + (2 * FrameMargin));
        double height = Math.Max(maxY - minY, MinimumExtentInches) * (1 + (2 * FrameMargin));
        double scale = Math.Min(area.Width / width, area.Height / height);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1;
        }

        var middle = new PointD((minX + maxX) / 2, (minY + maxY) / 2);
        return (scale, new Point(area.Center.X - (middle.X * scale), area.Center.Y - (middle.Y * scale)));
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, length = (dx * dx) + (dy * dy);
        double t = length == 0 ? 0 : Math.Clamp((((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / length, 0, 1);
        return Distance(p, new Point(a.X + (t * dx), a.Y + (t * dy)));
    }
}
