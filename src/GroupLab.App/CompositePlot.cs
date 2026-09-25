using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Reporting;

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
/// <item>A shot is a solid dot at its centre, the point the statistics use, so shots can be counted and the spread seen (entry 109 section 3).
/// With a calibre set, its outline at the calibre's diameter is drawn thin and faint behind the dot, on by default and hidden from the plot's
/// toggle. Every dot has the same halo, so a mark over the paper and a mark over the dark read as the same thing (entry 105 section 3).</item>
/// <item>Entry 204: the group centre is a pair of green lines across the whole plot, and the aim point a pair of blue ones, with CEP 50, 90
/// and 95 as green circles about the centre, dotted, solid and dashed, each on its own toggle beside the plot.</item>
/// <item>Extreme spread is the line between the two shots that make it, not a circle: a circle that size reads as a region the shots are
/// contained in, and extreme spread is the distance between two particular shots. Clicking the line picks both.</item>
/// <item>The view frames the group, not the bull: the shots with their calibre outlines and a narrow margin, entry 103 section 1 and entry 109
/// section 3, with the rings behind at true relative scale running off the frame when the group is smaller than the bull.</item>
/// <item>Entry 105 section 3: a key with each mark as drawn beside its name, and a tooltip naming whatever is under the pointer.</item>
/// </list>
/// </summary>
internal sealed class CompositePlot : Control
{
    /// <summary>
    /// The margin around the shots, as a share of their extent on each side. It was 0.35, and with the calibre outlines a 25 shot group framed
    /// the whole bull, so the rings filled the plot and the group sat in its middle half (entry 109 section 3).
    /// </summary>
    private const double FrameMargin = 0.1;

    /// <summary>
    /// Entry 169 section 3: every stroke at least this wide, and every mark at full strength in one of four inks, so nothing on the plot is
    /// thinner or lighter than can be read on a laptop in daylight. The rings are outlines in the ring grey, not faded fills.
    /// </summary>
    private const double Stroke = 1.5;

    /// <summary>The inks of the theme last drawn in; the key's swatches use them too.</summary>
    private PlotInks inks = Tokens.Plot(null);

    /// <summary>The smallest extent the view frames, in inches, so one shot or a tight cluster is not magnified without limit.</summary>
    private const double MinimumExtentInches = 0.25;

    /// <summary>How near the pointer must be to a thin mark, a circle's stroke or the centre, for the tooltip to name it.</summary>
    private const double StrokeReach = 5;

    /// <summary>
    /// Entry 204 section 1: how each layer is drawn, back to front. The bull's rings are wide and pale so they read as background; a shot's
    /// outline is thin and at half strength; the CEP circles are clearly wider than any outline; the centre lines are full length. The
    /// tests hold these in this order.
    /// </summary>
    internal const double BullStroke = 4;

    /// <summary>
    /// Entry 210 section 1.1: the rings' width in page units, so it stays in proportion as the view zooms. 0.05 in is about 19 pixels on
    /// the sample's group view, five times entry 204's 4; it is held between <see cref="BullStroke"/> and <see cref="BullStrokeMost"/>
    /// pixels so a wide view keeps a visible ring and a deep zoom does not bury the shots under one.
    /// </summary>
    internal const double BullRingInches = 0.05;

    internal const double BullStrokeMost = 40;

    /// <summary>How much one notch of the wheel zooms, and the furthest in and out the view goes from its fitted framing.</summary>
    private const double WheelStep = 1.15;

    private const double ZoomLeast = 0.25;

    private const double ZoomMost = 40;

    internal const double OutlineOpacity = 0.5;

    internal const double CepStroke = 2.5;

    internal const double SpreadStroke = 2;

    internal const double CentreLineStroke = 1.5;

    /// <summary>CEP 50 dotted, CEP 90 solid, CEP 95 dashed: one green, told apart by the stroke's pattern, which the key draws as it is.</summary>
    private static readonly IDashStyle Cep50Dash = new DashStyle([1, 2], 0);

    private static readonly IDashStyle Cep95Dash = new DashStyle([6, 3], 0);

    public IReadOnlyList<PlotDisc> Discs { get; set; } = [];

    public IReadOnlyList<PlotShot> Shots { get; set; } = [];

    public double? CalibreInches { get; set; }

    /// <summary>Whether the key is drawn; the comparison's small plots leave it off and say what the marks are once, beneath them (entry 113 section 2).</summary>
    public bool ShowKey { get; set; } = true;

    /// <summary>Whether the calibre outlines are drawn behind the dots, entry 109 section 3: on unless the plot's toggle hides them.</summary>
    public bool ShowOutlines { get; set; } = true;

    public PointD? Centre { get; set; }

    public double? Cep50Inches { get; set; }

    public double? Cep90Inches { get; set; }

    public double? Cep95Inches { get; set; }

    /// <summary>Entry 204 section 1.4: which of the optional marks are drawn, from the toggles beside the plot; the key lists only these.</summary>
    public PlotMarks Shown { get; set; } = PlotMarks.Default;

    /// <summary>
    /// Entry 210 section 2.1: the whole target, every ring with the group inside it, rather than the group alone. Setting it returns the
    /// view to its fitted framing.
    /// </summary>
    public bool WholeTarget
    {
        get => wholeTarget;
        set
        {
            wholeTarget = value;
            ResetView();
        }
    }

    private bool wholeTarget;

    /// <summary>Entry 210 section 2.2: the free zoom and pan on top of the fitted framing, a factor and a shift in pixels.</summary>
    internal double Zoom { get; private set; } = 1;

    private Vector pan;

    private Point? dragFrom;

    private double pinchStart = 1;

    /// <summary>Back to the fitted framing, as a double click or tap does.</summary>
    public void ResetView()
    {
        Zoom = 1;
        pan = default;
        InvalidateVisual();
    }

    /// <summary>Zooms by <paramref name="factor"/> about a point on the control, which stays where it is.</summary>
    internal void ZoomAbout(Point at, double factor)
    {
        var area = DataRect;
        var (_, before) = Frame(area);
        double next = Math.Clamp(Zoom * factor, ZoomLeast, ZoomMost);
        factor = next / Zoom;
        var after = new Point(at.X - ((at.X - before.X) * factor), at.Y - ((at.Y - before.Y) * factor));
        Zoom = next;
        var (_, fitted) = Frame(area);
        pan += after - fitted;
        InvalidateVisual();
    }

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
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && KeyLayout() is { Collapsed: true } layout && layout.Key.Contains(e.GetPosition(this)))
            {
                keyOpen = !keyOpen;
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Pick(e.GetPosition(this)) is { Count: > 0 } picked)
            {
                ShotsClicked?.Invoke(this, picked);
                e.Handled = true;
            }
            else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                // Entry 210 section 2.2: a drag that starts on empty paper pans the view; one that starts on a shot picks it.
                dragFrom = e.GetPosition(this);
                e.Pointer.Capture(this);
            }
        };
        PointerReleased += (_, e) =>
        {
            dragFrom = null;
            e.Pointer.Capture(null);
        };
        PointerWheelChanged += (_, e) =>
        {
            ZoomAbout(e.GetPosition(this), Math.Pow(WheelStep, e.Delta.Y));
            e.Handled = true;
        };
        DoubleTapped += (_, e) =>
        {
            ResetView();
            e.Handled = true;
        };

        // A touchpad's pinch on the desktop, and a pinch on a touch screen.
        AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, (_, e) =>
        {
            ZoomAbout(e.GetPosition(this), 1 + e.Delta.X);
            e.Handled = true;
        });
        GestureRecognizers.Add(new PinchGestureRecognizer());
        AddHandler(InputElement.PinchEvent, (_, e) =>
        {
            ZoomAbout(e.ScaleOrigin, e.Scale / pinchStart);
            pinchStart = e.Scale;
            e.Handled = true;
        });
        AddHandler(InputElement.PinchEndedEvent, (_, _) => pinchStart = 1);

        // Entry 105 section 3: the tooltip names what is under the pointer, and goes when nothing is.
        PointerMoved += (_, e) =>
        {
            if (dragFrom is { } from)
            {
                var now = e.GetPosition(this);
                pan += now - from;
                dragFrom = now;
                InvalidateVisual();
                return;
            }

            ToolTip.SetTip(this, Describe(e.GetPosition(this)));
        };
        PointerExited += (_, _) => ToolTip.SetTip(this, null);
    }

    /// <summary>Where an offset in inches lands on the control.</summary>
    public Point ToScreen(PointD inches)
    {
        var (scale, origin) = Frame(DataRect);
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

        return Shown.Spread && SpreadAt(at) is var (a, b) ? [a.Id, b.Id] : [];
    }

    /// <summary>
    /// What is under the pointer, in words, entry 105 section 3: in the order <see cref="Pick"/> resolves and then past it, a shot, the extreme
    /// spread's line, the group centre, the CEP circles near their stroke, the aim point, the bull's rings, and nothing on empty paper. Each
    /// says what the thing is as well as its value, which is what connects the mark on the screen to the figure in the stack.
    /// </summary>
    public string? Describe(Point at)
    {
        var (scale, _) = Frame(DataRect);
        if (ShotAt(at) is { } shot)
        {
            var o = shot.Offset;
            string place = $"{Length(Math.Abs(o.X))} {(o.X >= 0 ? "right" : "left")} and {Length(Math.Abs(o.Y))} {(o.Y > 0 ? "low" : "high")} of its bull's aim point";
            string fromCentre = Centre is { } c ? $", {Length(Distance(o, c))} from the group center" : "";
            string bull = shot.Bull is { } b ? $", bull {b}" : "";
            return $"Shot {shot.Label}{bull}. {Capital(place)}{fromCentre}."
                + (shot.Excluded ? " Excluded: drawn hollow, and left out of every figure except the side-by-side ones that show it both ways." : "");
        }

        if (Shown.Spread && SpreadAt(at) is var (a, b2))
        {
            return $"Extreme spread, {Length(Distance(a.Offset, b2.Offset))}: the distance between shots {a.Label} and {b2.Label}, the two furthest apart. It is a distance between two shots, not a region the group sits inside.";
        }

        if (Centre is { } centre)
        {
            var cp = ToScreen(centre);
            double fromCentre = Distance(cp, at);
            if (fromCentre <= StrokeReach + 3)
            {
                return $"Group center: the mean of the {Shots.Count(s => !s.Excluded)} shots not excluded, {Length(Math.Abs(centre.X))} {(centre.X >= 0 ? "right" : "left")} and {Length(Math.Abs(centre.Y))} {(centre.Y > 0 ? "low" : "high")} of the aim point.";
            }

            foreach (var (radius, percent, often) in new[] { (Shown.Cep50 ? Cep50Inches : null, 50, "half the time"), (Shown.Cep90 ? Cep90Inches : null, 90, "nine times in ten"), (Shown.Cep95 ? Cep95Inches : null, 95, "19 times in 20") })
            {
                if (radius is { } r && Math.Abs(fromCentre - (r * scale)) <= StrokeReach)
                {
                    return $"CEP {percent}, {Length(r)} in radius about the group center: a shot from this rifle would land inside it {often}, reckoned from sigma under the circular normal model.";
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
        inks = Tokens.Plot(ActualThemeVariant);
        var accent = new SolidColorBrush(inks.Accent);
        context.FillRectangle(new SolidColorBrush(inks.Paper), new Rect(Bounds.Size));
        var area = DataRect;
        var (scale, origin) = Frame(area);
        using (context.PushClip(area))
        {
            // Entry 204 section 2, back to front: the rings, the shots, the CEP circles, the extreme spread, the centre lines, the selection.
            // The bull at true relative scale, centred on the aim point: each ring's edge a wide pale band, background to everything else.
            var bullPen = new Pen(new SolidColorBrush(inks.Bull), RingStroke(scale));
            foreach (var disc in Discs.Where(d => !d.Paper))
            {
                context.DrawEllipse(null, bullPen, origin, disc.DiameterInches * scale / 2, disc.DiameterInches * scale / 2);
            }

            // The outlines first, all of them, then every dot over them, so no outline covers another shot's centre. A picked shot waits
            // for the top.
            var unpicked = Shots.Where(s => !Selected.Contains(s.Id)).OrderBy(s => !s.Excluded).ToList();
            foreach (var shot in unpicked)
            {
                DrawOutline(context, shot, scale);
            }

            foreach (var shot in unpicked)
            {
                DrawShot(context, shot);
            }

            if (Centre is { } centre)
            {
                var c = ToScreen(centre);
                var green = new SolidColorBrush(inks.Group);
                foreach (var (radius, dash, on) in new[] { (Cep50Inches, Cep50Dash, Shown.Cep50), (Cep90Inches, (IDashStyle?)null, Shown.Cep90), (Cep95Inches, Cep95Dash, Shown.Cep95) })
                {
                    if (on && radius is { } r)
                    {
                        Marks.Ring(context, green, c, r * scale, CepStroke, dash);
                    }
                }
            }

            if (Shown.Spread && SpreadPair is { } pair && Shots.FirstOrDefault(s => s.Id == pair.First) is { } a && Shots.FirstOrDefault(s => s.Id == pair.Second) is { } b)
            {
                Marks.Line(context, accent, ToScreen(a.Offset), ToScreen(b.Offset), SpreadStroke, Marks.Dashed);
            }

            // Entry 204 sections 1.6 and 1.7: the aim point and the group centre as lines across the whole plot, blue and green, so each
            // can be followed to the edge and read against the other without a small cross to find.
            FullLines(context, new SolidColorBrush(inks.Aim), origin, area);
            if (Centre is { } groupCentre)
            {
                FullLines(context, new SolidColorBrush(inks.Group), ToScreen(groupCentre), area);
            }

            foreach (var shot in Shots.Where(s => Selected.Contains(s.Id)))
            {
                DrawOutline(context, shot, scale);
                DrawShot(context, shot);
            }
        }

        if (ShowKey)
        {
            DrawKey(context);
        }
    }

    /// <summary>One horizontal and one vertical line through a point, across the whole plot.</summary>
    private static void FullLines(DrawingContext context, IBrush brush, Point at, Rect area)
    {
        var pen = new Pen(brush, CentreLineStroke);
        context.DrawLine(pen, new Point(area.Left, at.Y), new Point(area.Right, at.Y));
        context.DrawLine(pen, new Point(at.X, area.Top), new Point(at.X, area.Bottom));
    }

    /// <summary>A shot's hole at the calibre's size, an outline with no fill at half strength, so twenty-five of them never merge into one mass.</summary>
    private void DrawOutline(DrawingContext context, PlotShot shot, double scale)
    {
        if (CalibreInches is not { } calibre || !ShowOutlines)
        {
            return;
        }

        bool selected = Selected.Contains(shot.Id);
        var colour = selected ? inks.Accent : shot.Excluded ? inks.Ring : inks.Ink;
        var pen = new Pen(new SolidColorBrush(colour, selected || shot.Excluded ? 1 : OutlineOpacity), selected ? 2.5 : Stroke, shot.Excluded ? Marks.Dashed : null);
        double radius = Math.Max(2, calibre * scale / 2);
        context.DrawEllipse(null, pen, ToScreen(shot.Offset), radius, radius);
    }

    /// <summary>
    /// One shot's centre as drawn: a solid dot with a halo, larger when picked, and hollow and dashed when excluded. With no calibre the dot
    /// is all there is of the shot, and it is drawn at half strength as the outline would be (entry 204 section 1.1).
    /// </summary>
    private void DrawShot(DrawingContext context, PlotShot shot)
    {
        var at = ToScreen(shot.Offset);
        bool selected = Selected.Contains(shot.Id);
        double opacity = selected || shot.Excluded || CalibreInches is not null ? 1 : OutlineOpacity;
        IBrush brush = new SolidColorBrush(selected ? inks.Accent : shot.Excluded ? inks.Ring : inks.Ink, opacity);
        if (shot.Excluded)
        {
            Marks.Ring(context, brush, at, 3.5, selected ? 2.5 : Stroke, Marks.Dashed);
        }
        else
        {
            Marks.Dot(context, brush, at, selected ? 4.5 : 3);
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
            CalibreInches is { } calibre && ShowOutlines
                ? new KeyEntry(string.Create(CultureInfo.InvariantCulture, $"{kept} shots, a dot at each center, the outline drawn at the {calibre:0.000} in caliber"), (c, p) =>
                {
                    c.DrawEllipse(null, new Pen(new SolidColorBrush(inks.Ink, OutlineOpacity), Stroke), p, 6, 6);
                    Marks.Dot(c, new SolidColorBrush(inks.Ink), p, 3);
                })
                : CalibreInches is not null
                    ? new KeyEntry($"{kept} shots, a dot at each center; the caliber outlines are hidden", (c, p) => Marks.Dot(c, new SolidColorBrush(inks.Ink), p, 3))
                    : new KeyEntry($"{kept} shots, drawn as points: no caliber is set, so there is no hole size to draw", (c, p) => Marks.Dot(c, new SolidColorBrush(inks.Ink, OutlineOpacity), p, 3)),
        };
        if (excluded > 0)
        {
            entries.Add(new KeyEntry($"{excluded} excluded, drawn hollow", (c, p) => Marks.Ring(c, new SolidColorBrush(inks.Ring), p, 3.5, Stroke, Marks.Dashed)));
        }

        if (Shown.Spread && SpreadPair is { } pair)
        {
            entries.Add(new KeyEntry($"extreme spread, shots {Label(pair.First)} and {Label(pair.Second)}, the red dashed line", (c, p) => Marks.Line(c, new SolidColorBrush(inks.Accent), p + new Vector(-7, 0), p + new Vector(7, 0), SpreadStroke, Marks.Dashed)));
        }

        foreach (var (radius, percent, dash, look, on) in new[] { (Cep50Inches, 50, Cep50Dash, "dotted", Shown.Cep50), (Cep90Inches, 90, (IDashStyle?)null, "solid", Shown.Cep90), (Cep95Inches, 95, Cep95Dash, "dashed", Shown.Cep95) })
        {
            if (on && radius is not null && Centre is not null)
            {
                entries.Add(new KeyEntry($"CEP {percent}, the green {look} circle", (c, p) => Marks.Ring(c, new SolidColorBrush(inks.Group), p, 6, CepStroke, dash)));
            }
        }

        if (Centre is not null)
        {
            entries.Add(new KeyEntry("group center, the green lines", (c, p) => FullLinesSwatch(c, inks.Group, p)));
        }

        entries.Add(new KeyEntry("where you aimed, the blue lines", (c, p) => FullLinesSwatch(c, inks.Aim, p)));

        if (Discs.Count == 0)
        {
            entries.Add(new KeyEntry("no bull drawn: the sheet's definition is not known", (_, _) => { }));
        }

        return entries;
    }

    /// <summary>The key's swatch for a pair of full length lines: a small cross in their colour and weight.</summary>
    private static void FullLinesSwatch(DrawingContext context, Color colour, Point at)
    {
        var pen = new Pen(new SolidColorBrush(colour), CentreLineStroke);
        context.DrawLine(pen, at + new Vector(-7, 0), at + new Vector(7, 0));
        context.DrawLine(pen, at + new Vector(0, -7), at + new Vector(0, 7));
    }

    private const double KeyLine = 20, KeySwatch = 22, KeyPad = 10, KeyGap = 8, SmallestData = 240, ChipHeight = 28;

    /// <summary>Whether a collapsed key has been opened from its button.</summary>
    private bool keyOpen;

    private List<FormattedText> KeyTexts(IEnumerable<KeyEntry> entries) => [.. entries.Select(e => new FormattedText(e.Text, CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(inks.Ink)))];

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 213 section 2: the key never covers the data. It sits beside the plot or below it, whichever leaves the
    /// plot the larger square, and otherwise collapses to a small Key button in a strip of its own, which opens the key over the plot only
    /// when the person asks for it. <see cref="DataRect"/> is what is left for the plot, and every mark is drawn and clipped inside it.
    /// </summary>
    internal (Rect Data, Rect Key, bool Collapsed) KeyLayout()
    {
        var all = new Rect(Bounds.Size);
        if (!ShowKey)
        {
            return (all, default, false);
        }

        var entries = Key();
        var texts = KeyTexts(entries);
        double width = KeySwatch + texts.Max(t => t.Width) + (2 * KeyPad), height = (entries.Count * KeyLine) + KeyPad;
        // Beside or below, whichever leaves the plot the larger square, since a group is about as tall as it is wide.
        var beside = (Data: new Rect(0, 0, Math.Max(0, all.Width - width - (2 * KeyGap)), all.Height), Key: new Rect(all.Width - width - KeyGap, KeyGap, width, height));
        double w = Math.Min(width, all.Width - (2 * KeyGap));
        var below = (Data: new Rect(0, 0, all.Width, Math.Max(0, all.Height - height - (2 * KeyGap))), Key: new Rect(KeyGap, all.Height - height - KeyGap, w, height));
        static double Square(Rect r) => Math.Min(r.Width, r.Height);
        var best = Square(beside.Data) >= Square(below.Data) ? beside : below;
        if (Square(best.Data) >= SmallestData && best.Key.Width > 0)
        {
            return (best.Data, best.Key, false);
        }

        return (new Rect(0, ChipHeight, all.Width, Math.Max(0, all.Height - ChipHeight)), new Rect(KeyGap / 2, 2, 56, ChipHeight - 4), true);
    }

    /// <summary>The rectangle the plot itself is drawn in, clear of the key.</summary>
    internal Rect DataRect => KeyLayout().Data;

    private void DrawKey(DrawingContext context)
    {
        var entries = Key();
        const double line = KeyLine, swatch = KeySwatch, pad = KeyPad;
        var texts = KeyTexts(entries);
        var (_, place, collapsed) = KeyLayout();
        if (collapsed)
        {
            // The button: a small labelled box in its own strip above the plot.
            context.DrawRectangle(new SolidColorBrush(inks.Paper), new Pen(new SolidColorBrush(inks.Ring), 1), place, 3, 3);
            var label = new FormattedText(keyOpen ? "Key ▴" : "Key ▾", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.SecondarySize, new SolidColorBrush(inks.Ink));
            context.DrawText(label, new Point(place.X + ((place.Width - label.Width) / 2), place.Y + ((place.Height - label.Height) / 2)));
            if (!keyOpen)
            {
                return;
            }
        }

        double width = Math.Min(Math.Max(0, Bounds.Width - (2 * pad)), swatch + texts.Max(t => t.Width) + (2 * pad));
        var box = collapsed ? new Rect(pad, ChipHeight + 2, width, (entries.Count * line) + pad) : place;
        context.DrawRectangle(new SolidColorBrush(inks.Paper), new Pen(new SolidColorBrush(inks.Ring), 1), box, 3, 3);
        for (int i = 0; i < entries.Count; i++)
        {
            double y = box.Y + (pad / 2) + (i * line) + (line / 2);
            entries[i].Swatch(context, new Point(box.X + pad + (swatch / 2) - 4, y));
            context.DrawText(texts[i], new Point(box.X + pad + swatch, y - (texts[i].Height / 2)));
        }
    }

    private PlotShot? ShotAt(Point at)
    {
        var (scale, _) = Frame(DataRect);
        double reach = Math.Max(8, ShowOutlines ? (CalibreInches ?? 0) * scale / 2 : 0);
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

    /// <summary>The rings' stroke at a scale in pixels per inch: <see cref="BullRingInches"/> on the page, within its pixel bounds.</summary>
    internal static double RingStroke(double scale) => Math.Clamp(BullRingInches * scale, BullStroke, BullStrokeMost);

    /// <summary>The rings' stroke as drawn now, for the tests.</summary>
    internal double RingStrokeNow => RingStroke(Frame(DataRect).Scale);

    /// <summary>
    /// The scale in pixels per inch and where the aim point lands: the shots framed with a margin, or with <see cref="WholeTarget"/> every
    /// ring as well, then the free zoom and pan on top.
    /// </summary>
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

        if (WholeTarget && Discs.Count > 0)
        {
            double r = (Discs.Max(d => d.DiameterInches) / 2) + (BullRingInches / 2);
            (minX, maxX, minY, maxY) = (Math.Min(minX, -r), Math.Max(maxX, r), Math.Min(minY, -r), Math.Max(maxY, r));
        }

        double width = Math.Max(maxX - minX, MinimumExtentInches) * (1 + (2 * FrameMargin));
        double height = Math.Max(maxY - minY, MinimumExtentInches) * (1 + (2 * FrameMargin));
        double scale = Math.Min(area.Width / width, area.Height / height);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1;
        }

        var middle = new PointD((minX + maxX) / 2, (minY + maxY) / 2);
        var fitted = new Point(area.Center.X - (middle.X * scale), area.Center.Y - (middle.Y * scale));
        var zoomed = new Point(area.Center.X + ((fitted.X - area.Center.X) * Zoom), area.Center.Y + ((fitted.Y - area.Center.Y) * Zoom));
        return (scale * Zoom, zoomed + pan);
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
