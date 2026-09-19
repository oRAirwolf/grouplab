using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// One shot on the composite plot: its offset from its own bull's aim point in inches at the target, the bull it came from, and whether it
/// is excluded. The bull is what gets a reader from a mark on the plot back to a hole on the sheet (entry 105 section 3).
/// </summary>
internal sealed record PlotShot(int Id, string Label, string? Bull, PointD Offset, bool Excluded);

/// <summary>One disc of the bull's artwork, outermost first, at its diameter in inches, and whether its ink is the paper.</summary>
internal sealed record PlotDisc(double DiameterInches, Color Colour, bool Paper = false);

/// <summary>
/// The composite plot, NOTES-FROM-PLANNING.md entry 103 section 1: the centre of the analysis state. One bull's artwork from the definition,
/// centred on the aim point, with every scoring shot's offset from its own bull's aim point plotted on it.
/// <list type="bullet">
/// <item>Sighters are never given to it. Marks set to not a shot are absent, because they are not shots. Excluded shots are drawn hollow
/// and dim, not removed: excluding is a human judgement, and a plot that hid it would make the group look better than the evidence.</item>
/// <item>A shot is a circle at the calibre's diameter when a calibre is set and a point when it is not, and the key says which. Every shot
/// has the same outline and a halo, so a mark over the paper and a mark over the dark read as the same thing (entry 105 section 3).</item>
/// <item>The group centre is marked, with CEP 50 and CEP 90 as circles about it, dotted and dashed so the key can tell them apart.</item>
/// <item>Extreme spread is the line between the two shots that make it, not a circle: a circle that size reads as a region the shots are
/// contained in, and extreme spread is the distance between two particular shots. Clicking the line picks both.</item>
/// <item>The view frames the shots with a margin and draws the rings behind them at true relative scale, running off the frame when the group
/// is much smaller than the bull, because a small group scaled to fit a large ring is a dot.</item>
/// <item>Entry 105 section 3: a key with each mark as drawn beside its name, and a tooltip naming whatever is under the pointer.</item>
/// </list>
/// </summary>
internal sealed class CompositePlot : Control
{
    /// <summary>The margin around the shots, as a share of their extent on each side.</summary>
    private const double FrameMargin = 0.35;

    /// <summary>
    /// How strongly the bull's inked rings are drawn, entry 104 section 4: they are context for the shots and recede behind them, where at
    /// full strength the black ring was the heaviest mark on the screen. The paper stays at full strength, because a paper-white document on
    /// dark chrome is most of the concept's character, and the concept draws the rings lighter too.
    /// </summary>
    private const double ArtworkOpacity = 0.35;

    /// <summary>The smallest extent the view frames, in inches, so one shot or a tight cluster is not magnified without limit.</summary>
    private const double MinimumExtentInches = 0.25;

    /// <summary>How near the pointer must be to a thin mark, a circle's stroke or the centre, for the tooltip to name it.</summary>
    private const double StrokeReach = 5;

    private static readonly IDashStyle Cep50Dash = new DashStyle([1, 2.5], 0);

    private static readonly IDashStyle Cep90Dash = new DashStyle([5, 3], 0);

    public IReadOnlyList<PlotDisc> Discs { get; set; } = [];

    public IReadOnlyList<PlotShot> Shots { get; set; } = [];

    public double? CalibreInches { get; set; }

    public PointD? Centre { get; set; }

    public double? Cep50Inches { get; set; }

    public double? Cep90Inches { get; set; }

    /// <summary>The two shots that make the extreme spread, by id.</summary>
    public (int First, int Second)? SpreadPair { get; set; }

    public IReadOnlySet<int> Selected { get; set; } = new HashSet<int>();

    /// <summary>How a length reads, in the person's units; the window sets it.</summary>
    public Func<double, string> Length { get; set; } = inches => string.Create(CultureInfo.InvariantCulture, $"{inches:0.000} in");

    /// <summary>Raised when a click picks shots: one shot, or the extreme spread's two.</summary>
    public event EventHandler<IReadOnlyList<int>>? ShotsClicked;

    /// <summary>The key's entries, in the order drawn, for the headless tests.</summary>
    public IReadOnlyList<string> Legend => [.. Key().Select(k => k.Text)];

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

        // Entry 105 section 3: the tooltip names what is under the pointer, and goes when nothing is.
        PointerMoved += (_, e) => ToolTip.SetTip(this, Describe(e.GetPosition(this)));
        PointerExited += (_, _) => ToolTip.SetTip(this, null);
    }

    /// <summary>Where an offset in inches lands on the control.</summary>
    public Point ToScreen(PointD inches)
    {
        var (scale, origin) = Frame(new Rect(Bounds.Size));
        return new Point(origin.X + (inches.X * scale), origin.Y + (inches.Y * scale));
    }

    /// <summary>
    /// What a click at <paramref name="at"/> picks: the shot under it, within its drawn circle or a few pixels of a point, and otherwise both
    /// shots of the extreme spread when it lands on that line.
    /// </summary>
    public IReadOnlyList<int> Pick(Point at)
    {
        if (ShotAt(at) is { } shot)
        {
            return [shot.Id];
        }

        return SpreadAt(at) is var (a, b) ? [a.Id, b.Id] : [];
    }

    /// <summary>
    /// What is under the pointer, in words, entry 105 section 3: in the order <see cref="Pick"/> resolves and then past it, a shot, the extreme
    /// spread's line, the group centre, the CEP circles near their stroke, the aim point, the bull's rings, and nothing on empty paper. Each
    /// says what the thing is as well as its value, which is what connects the mark on the screen to the figure in the stack.
    /// </summary>
    public string? Describe(Point at)
    {
        var (scale, _) = Frame(new Rect(Bounds.Size));
        if (ShotAt(at) is { } shot)
        {
            var o = shot.Offset;
            string place = $"{Length(Math.Abs(o.X))} {(o.X >= 0 ? "right" : "left")} and {Length(Math.Abs(o.Y))} {(o.Y > 0 ? "low" : "high")} of its bull's aim point";
            string fromCentre = Centre is { } c ? $", {Length(Distance(o, c))} from the group centre" : "";
            string bull = shot.Bull is { } b ? $", bull {b}" : "";
            return $"Shot {shot.Label}{bull}. {Capital(place)}{fromCentre}."
                + (shot.Excluded ? " Excluded: drawn hollow, and left out of every figure except the side-by-side ones that show it both ways." : "");
        }

        if (SpreadAt(at) is var (a, b2))
        {
            return $"Extreme spread, {Length(Distance(a.Offset, b2.Offset))}: the distance between shots {a.Label} and {b2.Label}, the two furthest apart. It is a distance between two shots, not a region the group sits inside.";
        }

        if (Centre is { } centre)
        {
            var cp = ToScreen(centre);
            double fromCentre = Distance(cp, at);
            if (fromCentre <= StrokeReach + 3)
            {
                return $"Group centre: the mean of the {Shots.Count(s => !s.Excluded)} shots not excluded, {Length(Math.Abs(centre.X))} {(centre.X >= 0 ? "right" : "left")} and {Length(Math.Abs(centre.Y))} {(centre.Y > 0 ? "low" : "high")} of the aim point.";
            }

            foreach (var (radius, percent, often) in new[] { (Cep50Inches, 50, "half the time"), (Cep90Inches, 90, "nine times in ten") })
            {
                if (radius is { } r && Math.Abs(fromCentre - (r * scale)) <= StrokeReach)
                {
                    return $"CEP {percent}, {Length(r)} in radius about the group centre: a shot from this rifle would land inside it {often}, reckoned from sigma under the circular normal model.";
                }
            }
        }

        double fromAim = Distance(ToScreen(new PointD(0, 0)), at);
        if (fromAim <= StrokeReach + 3)
        {
            return "The aim point: every shot is plotted by its offset from its own bull's aim point, so twenty-five bulls read as one group.";
        }

        if (Discs.Count > 0 && fromAim <= Discs.Max(d => d.DiameterInches) * scale / 2)
        {
            return "The bull's rings, drawn from the sheet's definition at their true size behind the shots.";
        }

        return null;
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        var area = new Rect(Bounds.Size);
        var (scale, origin) = Frame(area);
        using (context.PushClip(area))
        {
            // The bull at true relative scale, centred on the aim point, whether or not it fits: each inked disc faded over the paper, so the
            // shots lead and the sheet still reads as paper.
            foreach (var disc in Discs)
            {
                var colour = disc.Paper ? disc.Colour : Tokens.Faded(disc.Colour, Discs.FirstOrDefault(d => d.Paper)?.Colour ?? Tokens.Paper, ArtworkOpacity);
                context.DrawEllipse(new SolidColorBrush(colour), null, origin, disc.DiameterInches * scale / 2, disc.DiameterInches * scale / 2);
            }

            Marks.Cross(context, Marks.Faint, origin, 6, 1);

            if (Centre is { } centre)
            {
                var c = ToScreen(centre);
                if (Cep50Inches is { } r50)
                {
                    Marks.Ring(context, Marks.Teal, c, r50 * scale, 1, Cep50Dash);
                }

                if (Cep90Inches is { } r90)
                {
                    Marks.Ring(context, Marks.Teal, c, r90 * scale, 1, Cep90Dash);
                }
            }

            foreach (var shot in Shots.OrderBy(s => !s.Excluded))
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

        DrawKey(context, palette);
    }

    /// <summary>One shot as drawn: the same outline and halo whatever is behind it, a ring at the calibre or a dot, dashed when excluded.</summary>
    private void DrawShot(DrawingContext context, PlotShot shot, double scale)
    {
        var at = ToScreen(shot.Offset);
        bool selected = Selected.Contains(shot.Id);
        IBrush brush = selected ? Marks.Selected : shot.Excluded ? Marks.Excluded : Marks.Impact;
        double core = selected ? Tokens.MarkSelectedCoreWidth : Tokens.MarkCoreWidth;
        if (CalibreInches is { } calibre)
        {
            Marks.Ring(context, brush, at, Math.Max(2, calibre * scale / 2), core, shot.Excluded ? Marks.Dashed : null);
        }
        else if (shot.Excluded)
        {
            Marks.Ring(context, brush, at, 3.5, core, Marks.Dashed);
        }
        else
        {
            Marks.Dot(context, brush, at, selected ? 4.5 : 3.5);
        }
    }

    /// <summary>One entry of the key: its words, and how its swatch is drawn at a point.</summary>
    private sealed record KeyEntry(string Text, Action<DrawingContext, Point> Swatch);

    /// <summary>
    /// The key, entry 105 section 3: each mark drawn as it is on the plot, then its name, in a panel at the plot's top left, beside what it
    /// names rather than stranded under it. A tooltip helps a person who suspects there is something to hover over; the key tells them.
    /// </summary>
    private List<KeyEntry> Key()
    {
        int kept = Shots.Count(s => !s.Excluded), excluded = Shots.Count - kept;
        var entries = new List<KeyEntry>
        {
            CalibreInches is { } calibre
                ? new KeyEntry(string.Create(CultureInfo.InvariantCulture, $"{kept} shots, each drawn at the {calibre:0.000} in calibre"), (c, p) => Marks.Ring(c, Marks.Impact, p, 5, Tokens.MarkCoreWidth))
                : new KeyEntry($"{kept} shots, drawn as points: no calibre is set, so there is no hole size to draw", (c, p) => Marks.Dot(c, Marks.Impact, p, 3.5)),
        };
        if (excluded > 0)
        {
            entries.Add(new KeyEntry($"{excluded} excluded, drawn hollow", (c, p) => Marks.Ring(c, Marks.Excluded, p, 5, Tokens.MarkCoreWidth, Marks.Dashed)));
        }

        if (SpreadPair is { } pair)
        {
            entries.Add(new KeyEntry($"extreme spread, shots {Label(pair.First)} and {Label(pair.Second)}", (c, p) => Marks.Line(c, Marks.Selected, p + new Vector(-7, 0), p + new Vector(7, 0), 1.5, Marks.Dashed)));
        }

        if (Cep50Inches is not null)
        {
            entries.Add(new KeyEntry("CEP 50, the dotted circle", (c, p) => Marks.Ring(c, Marks.Teal, p, 6, 1, Cep50Dash)));
            entries.Add(new KeyEntry("CEP 90, the dashed circle", (c, p) => Marks.Ring(c, Marks.Teal, p, 6, 1, Cep90Dash)));
        }

        if (Centre is not null)
        {
            entries.Add(new KeyEntry("group centre", (c, p) => Marks.Cross(c, Marks.Teal, p, 6, 1.5)));
        }

        if (Discs.Count == 0)
        {
            entries.Add(new KeyEntry("no bull drawn: the sheet's definition is not known", (_, _) => { }));
        }

        return entries;
    }

    private void DrawKey(DrawingContext context, Palette palette)
    {
        var entries = Key();
        const double line = 20, swatch = 22, pad = 10;
        var texts = entries.Select(e => new FormattedText(e.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(palette.Dim))).ToList();
        double width = Math.Min(Math.Max(0, Bounds.Width - (2 * pad)), swatch + texts.Max(t => t.Width) + (2 * pad));
        var box = new Rect(pad, pad, width, (entries.Count * line) + pad);
        context.DrawRectangle(new SolidColorBrush(palette.Panel, 0.9), new Pen(new SolidColorBrush(palette.Line2), 1), box, 3, 3);
        for (int i = 0; i < entries.Count; i++)
        {
            double y = box.Y + (pad / 2) + (i * line) + (line / 2);
            entries[i].Swatch(context, new Point(box.X + pad + (swatch / 2) - 4, y));
            context.DrawText(texts[i], new Point(box.X + pad + swatch, y - (texts[i].Height / 2)));
        }
    }

    private PlotShot? ShotAt(Point at)
    {
        var (scale, _) = Frame(new Rect(Bounds.Size));
        double reach = Math.Max(8, (CalibreInches ?? 0) * scale / 2);
        return Shots
            .Select(s => (Shot: s, Distance: Distance(ToScreen(s.Offset), at)))
            .Where(x => x.Distance <= reach)
            .OrderBy(x => x.Distance)
            .FirstOrDefault().Shot;
    }

    private (PlotShot First, PlotShot Second)? SpreadAt(Point at) =>
        SpreadPair is { } pair && Shots.FirstOrDefault(s => s.Id == pair.First) is { } a && Shots.FirstOrDefault(s => s.Id == pair.Second) is { } b
            && DistanceToSegment(at, ToScreen(a.Offset), ToScreen(b.Offset)) <= 6
            ? (a, b)
            : null;

    private string Label(int id) => Shots.FirstOrDefault(s => s.Id == id)?.Label ?? id.ToString(CultureInfo.InvariantCulture);

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

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

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private static double DistanceToSegment(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, length = (dx * dx) + (dy * dy);
        double t = length == 0 ? 0 : Math.Clamp((((p.X - a.X) * dx) + ((p.Y - a.Y) * dy)) / length, 0, 1);
        return Distance(p, new Point(a.X + (t * dx), a.Y + (t * dy)));
    }
}
