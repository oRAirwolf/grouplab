using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>What a tap on the image does.</summary>
public enum MarkingTool
{
    /// <summary>Drag to move the view. Always available with the middle button or two fingers' worth of patience.</summary>
    Pan,

    /// <summary>
    /// Tap two points a known distance apart (NOTES-FROM-PLANNING.md entry 21 section 4, the fast scale). The line is drawn as it is
    /// made and either end can be dragged, before and after the length is used (entry 39 section 3).
    /// </summary>
    Length,

    /// <summary>Tap four corners of a known rectangle, top left first and around (entry 21 section 4, the accurate scale); any corner can be dragged.</summary>
    Rectangle,

    /// <summary>Tap the point of aim.</summary>
    Aim,

    /// <summary>Press on an impact, drag it to where it belongs, and let go to set it; it snaps to the hole under it then (entry 39 section 4).</summary>
    Impact,

    /// <summary>Tap a shot to select it, drag it to move it, then tap a bull to assign it there (DESIGN.md section 13); drag an end of the scale to adjust it.</summary>
    Select,
}

/// <summary>
/// The image with everything marked on it, and the taps that mark it. It holds a view (a zoom and an offset), the taps of a scale
/// reference being made or waiting for its size, and an impact being placed; every mark itself lives in the <see cref="MarkingSession"/>,
/// so undo covers it, and so does the view's rotation, which the canvas follows. Every position it hands the session is in stored pixels,
/// whatever the rotation.
/// <para>
/// Nothing here needs a mouse. Every action is a single tap or a drag, which a finger does as well as a pointer, and zoom has
/// buttons as well as the wheel: entry 21 section 6 asks that the marking screen not be gratuitously desktop-only.
/// </para>
/// <para>
/// Every mark is legible on a photograph of a target, NOTES-FROM-PLANNING.md entry 39 section 5. A photograph is mostly white paper with
/// black printing and coloured rings, sometimes over a dark backer, so no mark is drawn in white, and every stroke and label is drawn over
/// a dark outline: the outline reads on paper, and the colour reads on ink and on a dark backer. That is a stronger constraint than
/// looking right against the application's own dark chrome.
/// </para>
/// </summary>
public sealed class MarkingCanvas : Control, ICustomHitTest
{
    private const double MarkRadius = 11, HitRadius = 18, HandleRadius = 6;
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Menlo, monospace");
    private static readonly IBrush Outline = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
    private static readonly IBrush ScaleBrush = Brushes.LimeGreen;
    private static readonly IBrush SelectedBrush = Brushes.HotPink;

    private Bitmap? bitmap;
    private GrayImage? value;

    // The image's size in its own pixels, from the decode the pipeline measures, never from the bitmap: a bitmap's size follows the
    // file's DPI tag and would scale every mark with it.
    private double imageWidth, imageHeight;
    private double zoom = 1;
    private Vector offset;
    private int turns;
    private Point? panFrom;
    private int? dragging;
    private PointD dragAt;
    private readonly List<PointD> pending = [];
    private readonly List<PointD> awaiting = [];
    private PointD? hover;
    private (Handle Kind, int Index)? handle;
    private PointD handleAt;
    private PointD? placing;

    /// <summary>Which scale points a dragged end belongs to: one being made, one waiting for its size, or the reference in use.</summary>
    private enum Handle
    {
        Pending,
        Awaiting,
        Committed,
    }

    public MarkingCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public MarkingSession? Session { get; set; }

    public MarkingTool Tool
    {
        get;
        set
        {
            field = value;
            pending.Clear();
            hover = null;
            placing = null;
            InvalidateVisual();
        }
    } = MarkingTool.Pan;

    /// <summary>The selected shot, if any.</summary>
    public int? Selected { get; set; }

    /// <summary>Printed markers the registration expected and did not find, image pixels, drawn so the user sees what is missing.</summary>
    public IReadOnlyList<PointD> MissingMarkers { get; set; } = [];

    /// <summary>Shots whose hole reads too large for the group's calibre, ringed in red (entry 24 section 5 point 3).</summary>
    public IReadOnlySet<int> FlaggedShots { get; set; } = new HashSet<int>();

    /// <summary>Raised when two taps complete a reference length; the window asks for its size.</summary>
    public event EventHandler<IReadOnlyList<PointD>>? LengthTapped;

    /// <summary>Raised when four taps complete a reference rectangle; the window asks for its size.</summary>
    public event EventHandler<IReadOnlyList<PointD>>? RectangleTapped;

    /// <summary>Raised when the selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised with a sentence the user should see about the last action, such as a tap not snapped onto printed artwork.</summary>
    public event EventHandler<string>? Notice;

    /// <summary>
    /// The sheet's printed artwork in image pixels when the image was detected as a GroupLab sheet, so a tap never snaps onto a printed
    /// ring or numeral (NOTES-FROM-PLANNING.md entry 40 section 1). Null otherwise.
    /// </summary>
    public GrayImage? Artwork { get; set; }

    /// <summary>The taps of an incomplete scale reference, image pixels.</summary>
    public IReadOnlyList<PointD> PendingTaps => pending;

    /// <summary>
    /// The taps of a scale reference that is complete and waiting for its size, image pixels. It stays drawn and draggable until it is
    /// used, because a measurement you cannot see is one you cannot check (entry 39 section 3).
    /// </summary>
    public IReadOnlyList<PointD> AwaitingTaps => awaiting;

    /// <summary>Where the pointer is over the image while a scale reference is being made, which the line being made is drawn to.</summary>
    internal PointD? Hover => hover;

    /// <summary>Forgets a completed scale reference once it has been used; the reference in use is drawn from the session.</summary>
    public void ClearAwaiting()
    {
        awaiting.Clear();
        InvalidateVisual();
    }

    /// <summary>The number each counted shot is drawn and listed with, in marking order. A shot marked as not a shot has none.</summary>
    public static IReadOnlyDictionary<int, int> ShotNumbers(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var numbers = new Dictionary<int, int>();
        foreach (var shot in state.Shots.Where(s => !s.NotAShot))
        {
            numbers[shot.Id] = numbers.Count + 1;
        }

        return numbers;
    }

    public void SetImage(Bitmap? image, GrayImage? valueImage)
    {
        bitmap?.Dispose();
        bitmap = image;
        value = valueImage;
        imageWidth = valueImage?.Width ?? image?.PixelSize.Width ?? 0;
        imageHeight = valueImage?.Height ?? image?.PixelSize.Height ?? 0;
        pending.Clear();
        awaiting.Clear();
        hover = null;
        placing = null;
        handle = null;
        Selected = null;
        MissingMarkers = [];
        Artwork = null;
        FitToView();
    }

    public void FitToView()
    {
        Fit();
        InvalidateVisual();
    }

    /// <summary>Fits the turned image to the control without asking for a redraw, which may not be asked for during one.</summary>
    private void Fit()
    {
        turns = Session?.State.ViewQuarterTurns ?? 0;
        var (width, height) = ViewRotation.DisplaySize(turns, imageWidth, imageHeight);
        if (bitmap is null || width <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            zoom = 1;
            offset = default;
        }
        else
        {
            zoom = Math.Min(Bounds.Width / width, Bounds.Height / height);
            offset = new Vector((Bounds.Width - (width * zoom)) / 2, (Bounds.Height - (height * zoom)) / 2);
        }
    }

    /// <summary>Zooms by a factor about a point of the control, the centre when none is given.</summary>
    public void ZoomBy(double factor, Point? about = null)
    {
        EnsureView();
        var at = about ?? new Point(Bounds.Width / 2, Bounds.Height / 2);
        double x = (at.X - offset.X) / zoom, y = (at.Y - offset.Y) / zoom;
        zoom = Math.Clamp(zoom * factor, 0.02, 40);
        offset = new Vector(at.X - (x * zoom), at.Y - (y * zoom));
        InvalidateVisual();
    }

    /// <summary>The whole canvas takes taps, including where nothing is drawn yet, so a first tap on an empty area still marks.</summary>
    public bool HitTest(Point point) => true;

    /// <summary>A control point as stored image pixels, through the view's zoom, offset and rotation.</summary>
    public PointD ToImage(Point control)
    {
        EnsureView();
        return ViewRotation.ToImage(new PointD((control.X - offset.X) / zoom, (control.Y - offset.Y) / zoom), turns, imageWidth, imageHeight);
    }

    /// <summary>A stored image point as a control point, through the view's rotation, zoom and offset.</summary>
    public Point ToControl(PointD image)
    {
        EnsureView();
        var display = ViewRotation.ToDisplay(image, turns, imageWidth, imageHeight);
        return new Point((display.X * zoom) + offset.X, (display.Y * zoom) + offset.Y);
    }

    /// <summary>
    /// Refits the view when the session's rotation has changed, by a button, a key or an undo, before anything is drawn or tapped.
    /// The rotation lives in the session so undo covers it (NOTES-FROM-PLANNING.md entry 26 point 5); the canvas only follows it.
    /// </summary>
    private void EnsureView()
    {
        // Every session change already redraws the canvas through the window, so this only refits.
        if ((Session?.State.ViewQuarterTurns ?? 0) != turns)
        {
            Fit();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (e.PreviousSize.Width <= 0)
        {
            FitToView();
        }
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(38, 38, 42)), new Rect(Bounds.Size));
        if (bitmap is null)
        {
            var hint = new FormattedText("Open a photograph or scan of a target to start marking.", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 16, Brushes.Gainsboro);
            context.DrawText(hint, new Point(24, 24));
            return;
        }

        // The image is drawn through the same map as every mark, stored pixels to the turned frame, then zoom and offset. The pixels
        // themselves are never turned or re-encoded (entry 26 point 1), and the marks' labels stay upright.
        EnsureView();
        var (xFromX, xFromY, x0, yFromX, yFromY, y0) = ViewRotation.Affine(turns, imageWidth, imageHeight);
        using (context.PushTransform(new Matrix(xFromX * zoom, yFromX * zoom, xFromY * zoom, yFromY * zoom, (x0 * zoom) + offset.X, (y0 * zoom) + offset.Y)))
        {
            context.DrawImage(bitmap, new Rect(0, 0, imageWidth, imageHeight));
        }

        if (Session is not { } session)
        {
            return;
        }

        var state = session.State;
        foreach (var marker in MissingMarkers)
        {
            var c = ToControl(marker);
            Line(context, Brushes.OrangeRed, 2.5, c + new Vector(-9, -9), c + new Vector(9, 9));
            Line(context, Brushes.OrangeRed, 2.5, c + new Vector(-9, 9), c + new Vector(9, -9));
        }

        foreach (var bull in state.Bulls)
        {
            var c = ToControl(bull.Image);
            Line(context, Brushes.DeepSkyBlue, 2, c + new Vector(-12, 0), c + new Vector(12, 0));
            Line(context, Brushes.DeepSkyBlue, 2, c + new Vector(0, -12), c + new Vector(0, 12));
            DrawLabel(context, bull.Label, Brushes.DeepSkyBlue, 12, c + new Vector(8, 6));
        }

        // The scale: the reference in use, one complete and waiting for its size, and one being made, each with a circle at every end, and
        // the one being made drawn out to the pointer (entry 39 section 3).
        var committed = ScalePoints(state.Scale);
        DrawReference(context, [.. committed.Select((p, i) => Shown(Handle.Committed, i, p))], closed: committed.Count == 4);
        DrawReference(context, [.. awaiting.Select((p, i) => Shown(Handle.Awaiting, i, p))], closed: awaiting.Count == 4);
        var making = pending.Select((p, i) => Shown(Handle.Pending, i, p)).ToList();
        DrawReference(context, making, closed: false);
        if (making.Count > 0 && hover is { } towards && handle is null && Tool is MarkingTool.Length or MarkingTool.Rectangle)
        {
            Line(context, ScaleBrush, 2, ToControl(making[^1]), ToControl(towards), new DashStyle([4, 3], 0));
            Ring(context, ScaleBrush, 2, ToControl(towards), HandleRadius);
        }

        if (state.PointOfAim is { } aim)
        {
            var c = ToControl(aim);
            Ring(context, Brushes.Magenta, 2, c, 14);
            Line(context, Brushes.Magenta, 2, c + new Vector(-20, 0), c + new Vector(20, 0));
            Line(context, Brushes.Magenta, 2, c + new Vector(0, -20), c + new Vector(0, 20));
        }

        var numbers = ShotNumbers(state);
        foreach (var shot in state.Shots)
        {
            var at = dragging == shot.Id ? dragAt : shot.Image;
            var c = ToControl(at);
            if (shot.NotAShot)
            {
                Line(context, Brushes.Gray, 2, c + new Vector(-8, -8), c + new Vector(8, 8));
                Line(context, Brushes.Gray, 2, c + new Vector(-8, 8), c + new Vector(8, -8));
                continue;
            }

            IBrush colour = shot.Provenance switch
            {
                ShotProvenance.Automatic => Brushes.Gold,
                ShotProvenance.Corrected => Brushes.Orange,
                _ => Brushes.LawnGreen,
            };
            if (shot.Bull is { } b && state.Bulls.FirstOrDefault(x => x.Index == b) is { } bull)
            {
                Line(context, colour, 1, c, ToControl(bull.Image), new DashStyle([4, 4], 0));
            }

            if (shot.Id == Selected)
            {
                Ring(context, SelectedBrush, 3, c, MarkRadius + 5);
            }

            Ring(context, colour, 2.5, c, MarkRadius, shot.Exclusion is null ? null : new DashStyle([2, 2], 0));
            if (FlaggedShots.Contains(shot.Id))
            {
                Ring(context, Brushes.Red, 2, c, MarkRadius + 9, new DashStyle([3, 2], 0));
            }

            context.DrawEllipse(colour, new Pen(Outline, 1), c, 2, 2);
            DrawLabel(context, numbers[shot.Id].ToString(CultureInfo.InvariantCulture) + (shot.Exclusion is null ? "" : " excluded"), colour, 12, c + new Vector(MarkRadius + 3, -MarkRadius - 6));
        }

        // An impact being placed: a cross where the pointer is, and a ring where it will snap when let go (entry 39 section 4).
        if (placing is { } placed)
        {
            var c = ToControl(placed);
            var snapped = ToControl(Snap(session, placed).At);
            Line(context, Brushes.LawnGreen, 1.5, c + new Vector(-10, 0), c + new Vector(10, 0));
            Line(context, Brushes.LawnGreen, 1.5, c + new Vector(0, -10), c + new Vector(0, 10));
            if (Distance(c, snapped) > 1)
            {
                Line(context, Brushes.LawnGreen, 1, c, snapped, new DashStyle([2, 2], 0));
            }

            Ring(context, Brushes.LawnGreen, 2.5, snapped, MarkRadius, new DashStyle([3, 2], 0));
        }
    }

    /// <summary>A line in a colour over a dark outline, so it reads on paper, on ink and on a dark backer.</summary>
    private static void Line(DrawingContext context, IBrush brush, double thickness, Point a, Point b, IDashStyle? dash = null)
    {
        context.DrawLine(new Pen(Outline, thickness + 2, dash), a, b);
        context.DrawLine(new Pen(brush, thickness, dash), a, b);
    }

    /// <summary>A circle in a colour over a dark outline.</summary>
    private static void Ring(DrawingContext context, IBrush brush, double thickness, Point centre, double radius, IDashStyle? dash = null)
    {
        context.DrawEllipse(null, new Pen(Outline, thickness + 2, dash), centre, radius, radius);
        context.DrawEllipse(null, new Pen(brush, thickness, dash), centre, radius, radius);
    }

    /// <summary>A label in a colour on a dark plate.</summary>
    private static void DrawLabel(DrawingContext context, string text, IBrush brush, double size, Point at)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Mono), size, brush);
        context.FillRectangle(Outline, new Rect(at.X - 2, at.Y - 1, formatted.Width + 4, formatted.Height + 2));
        context.DrawText(formatted, at);
    }

    /// <summary>A scale reference's segments and a circle at every end.</summary>
    private void DrawReference(DrawingContext context, IReadOnlyList<PointD> points, bool closed)
    {
        for (int i = 1; i < points.Count; i++)
        {
            Line(context, ScaleBrush, 2, ToControl(points[i - 1]), ToControl(points[i]));
        }

        if (closed)
        {
            Line(context, ScaleBrush, 2, ToControl(points[^1]), ToControl(points[0]));
        }

        foreach (var point in points)
        {
            Ring(context, ScaleBrush, 2, ToControl(point), HandleRadius);
        }
    }

    /// <summary>A scale point as drawn: where it is being dragged to, when it is the one being dragged.</summary>
    private PointD Shown(Handle kind, int index, PointD point) => handle is { } h && h.Kind == kind && h.Index == index ? handleAt : point;

    /// <summary>The draggable points of the reference in use: a length's two ends or a rectangle's four corners. A registered sheet has none.</summary>
    private static IReadOnlyList<PointD> ScalePoints(ScaleReference? scale) => scale switch
    {
        LengthReference length => [length.A, length.B],
        RectangleReference rectangle => rectangle.Corners,
        _ => [],
    };

    /// <summary>The nearest scale point within reach of a control point, of a reference being made, waiting for its size, or in use.</summary>
    private (Handle Kind, int Index)? HitHandle(MarkingSession session, Point position)
    {
        (Handle Kind, int Index)? best = null;
        double nearest = HitRadius;
        foreach (var (kind, points) in new (Handle, IReadOnlyList<PointD>)[] { (Handle.Pending, pending), (Handle.Awaiting, awaiting), (Handle.Committed, ScalePoints(session.State.Scale)) })
        {
            for (int i = 0; i < points.Count; i++)
            {
                double d = Distance(ToControl(points[i]), position);
                if (d <= nearest)
                {
                    nearest = d;
                    best = (kind, i);
                }
            }
        }

        return best;
    }

    /// <summary>
    /// The snap for an impact at an image point. With a calibre and a scale it reaches one bullet diameter (entry 24 section 5 point 2);
    /// otherwise a finger's width on screen. On a detected sheet it never lands on printed artwork (entry 40 section 1).
    /// </summary>
    private SnapResult Snap(MarkingSession session, PointD image) => value is null
        ? new SnapResult(image, null)
        : Snapping.ToHole(value, image, HoleSize.SnapRadiusPixels(session.State, image) ?? (2 * HitRadius / zoom), Artwork);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        if (bitmap is null || Session is not { } session)
        {
            return;
        }

        var point = e.GetCurrentPoint(this);
        var image = ToImage(point.Position);
        if (Tool == MarkingTool.Pan || point.Properties.IsMiddleButtonPressed)
        {
            panFrom = point.Position;
            e.Pointer.Capture(this);
            return;
        }

        switch (Tool)
        {
            case MarkingTool.Length:
            case MarkingTool.Rectangle:
                if (HitHandle(session, point.Position) is { } grabbed)
                {
                    StartHandle(session, grabbed, e);
                    break;
                }

                if (pending.Count == 0)
                {
                    awaiting.Clear();
                }

                pending.Add(image);
                hover = image;
                if (pending.Count == (Tool == MarkingTool.Length ? 2 : 4))
                {
                    awaiting.AddRange(pending);
                    pending.Clear();
                    hover = null;
                    (Tool == MarkingTool.Length ? LengthTapped : RectangleTapped)?.Invoke(this, [.. awaiting]);
                }

                break;

            case MarkingTool.Aim:
                session.SetPointOfAim(image);
                break;

            case MarkingTool.Impact:
                // Placed when let go, so a press that missed is dragged onto the hole rather than tapped again blind (entry 39 section 4).
                placing = image;
                e.Pointer.Capture(this);
                break;

            case MarkingTool.Select:
                var hit = session.State.Shots.Where(s => Distance(ToControl(s.Image), point.Position) <= HitRadius).MinBy(s => Distance(ToControl(s.Image), point.Position));
                if (hit is not null)
                {
                    Selected = hit.Id;
                    dragging = hit.Id;
                    dragAt = hit.Image;
                    e.Pointer.Capture(this);
                }
                else if (HitHandle(session, point.Position) is { } end)
                {
                    StartHandle(session, end, e);
                }
                else if (Selected is { } selected && session.State.Bulls.FirstOrDefault(b => Distance(ToControl(b.Image), point.Position) <= 3 * HitRadius) is { } bull)
                {
                    // Click a hole, then click a bull: DESIGN.md section 13's reassignment.
                    session.AssignBull(selected, bull.Index);
                }
                else
                {
                    Selected = null;
                }

                SelectionChanged?.Invoke(this, EventArgs.Empty);
                break;
        }

        InvalidateVisual();
    }

    private void StartHandle(MarkingSession session, (Handle Kind, int Index) grabbed, PointerPressedEventArgs e)
    {
        handle = grabbed;
        handleAt = grabbed.Kind switch
        {
            Handle.Pending => pending[grabbed.Index],
            Handle.Awaiting => awaiting[grabbed.Index],
            _ => ScalePoints(session.State.Scale)[grabbed.Index],
        };
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        if (panFrom is { } from)
        {
            offset += position - from;
            panFrom = position;
        }
        else if (dragging is not null)
        {
            dragAt = ToImage(position);
        }
        else if (handle is not null)
        {
            handleAt = ToImage(position);
        }
        else if (placing is not null)
        {
            placing = ToImage(position);
        }
        else if (pending.Count > 0 && Tool is MarkingTool.Length or MarkingTool.Rectangle)
        {
            hover = ToImage(position);
        }
        else
        {
            return;
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Session is { } session)
        {
            if (dragging is { } id && session.State.Find(id) is { } shot && Distance(ToControl(shot.Image), ToControl(dragAt)) > 3)
            {
                // One move, one undo step, however long the drag.
                session.MoveShot(id, dragAt);
            }

            if (handle is { } grabbed)
            {
                ReleaseHandle(session, grabbed);
            }

            if (placing is { } placed)
            {
                placing = null;
                var snap = Snap(session, placed);
                Selected = session.AddShot(snap.At);
                if (snap.NotSnapped is { } note)
                {
                    Notice?.Invoke(this, note);
                }

                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        dragging = null;
        handle = null;
        placing = null;
        panFrom = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    /// <summary>
    /// Puts a dragged scale point where it was let go: in the taps being made or waiting, or, for the reference in use, as a new reference
    /// of the same size, one undo step. A press on an end that does not move it changes nothing.
    /// </summary>
    private void ReleaseHandle(MarkingSession session, (Handle Kind, int Index) grabbed)
    {
        switch (grabbed.Kind)
        {
            case Handle.Pending:
                pending[grabbed.Index] = handleAt;
                break;
            case Handle.Awaiting:
                awaiting[grabbed.Index] = handleAt;
                break;
            case Handle.Committed when Distance(ToControl(ScalePoints(session.State.Scale)[grabbed.Index]), ToControl(handleAt)) <= 1:
                break;
            case Handle.Committed when session.State.Scale is LengthReference length:
                session.SetScale(grabbed.Index == 0 ? length with { A = handleAt } : length with { B = handleAt });
                break;
            case Handle.Committed when session.State.Scale is RectangleReference rectangle:
                var corners = rectangle.Corners.ToArray();
                corners[grabbed.Index] = handleAt;
                try
                {
                    session.SetScale(new RectangleReference(corners, rectangle.WidthInches, rectangle.HeightInches));
                }
                catch (ArgumentException ex)
                {
                    Notice?.Invoke(this, "The corner was not moved: " + ex.Message);
                }

                break;
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomBy(e.Delta.Y > 0 ? 1.2 : 1 / 1.2, e.GetPosition(this));
        e.Handled = true;
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
}
