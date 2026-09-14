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

    /// <summary>Tap two points a known distance apart (NOTES-FROM-PLANNING.md entry 21 section 4, the fast scale).</summary>
    Length,

    /// <summary>Tap four corners of a known rectangle, top left first and around (entry 21 section 4, the accurate scale).</summary>
    Rectangle,

    /// <summary>Tap the point of aim.</summary>
    Aim,

    /// <summary>Tap an impact; the tap snaps to the hole under it.</summary>
    Impact,

    /// <summary>Tap a shot to select it, drag it to move it, then tap a bull to assign it there (DESIGN.md section 13).</summary>
    Select,
}

/// <summary>
/// The image with everything marked on it, and the taps that mark it. It holds a view (a zoom and an offset) and the taps of a
/// scale reference not yet complete; every mark itself lives in the <see cref="MarkingSession"/>, so undo covers it.
/// <para>
/// Nothing here needs a mouse. Every action is a single tap or a drag, which a finger does as well as a pointer, and zoom has
/// buttons as well as the wheel: entry 21 section 6 asks that the marking screen not be gratuitously desktop-only.
/// </para>
/// </summary>
public sealed class MarkingCanvas : Control, ICustomHitTest
{
    private const double MarkRadius = 11, HitRadius = 18;
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Menlo, monospace");

    private Bitmap? bitmap;
    private GrayImage? value;

    // The image's size in its own pixels, from the decode the pipeline measures, never from the bitmap: a bitmap's size follows the
    // file's DPI tag and would scale every mark with it.
    private double imageWidth, imageHeight;
    private double zoom = 1;
    private Vector offset;
    private Point? panFrom;
    private int? dragging;
    private PointD dragAt;
    private readonly List<PointD> pending = [];

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
            InvalidateVisual();
        }
    } = MarkingTool.Pan;

    /// <summary>The selected shot, if any.</summary>
    public int? Selected { get; set; }

    /// <summary>Printed markers the registration expected and did not find, image pixels, drawn so the user sees what is missing.</summary>
    public IReadOnlyList<PointD> MissingMarkers { get; set; } = [];

    /// <summary>Raised when two taps complete a reference length; the window asks for its size.</summary>
    public event EventHandler<IReadOnlyList<PointD>>? LengthTapped;

    /// <summary>Raised when four taps complete a reference rectangle; the window asks for its size.</summary>
    public event EventHandler<IReadOnlyList<PointD>>? RectangleTapped;

    /// <summary>Raised when the selection changes.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>The taps of an incomplete scale reference, image pixels.</summary>
    public IReadOnlyList<PointD> PendingTaps => pending;

    public void SetImage(Bitmap? image, GrayImage? valueImage)
    {
        bitmap?.Dispose();
        bitmap = image;
        value = valueImage;
        imageWidth = valueImage?.Width ?? image?.PixelSize.Width ?? 0;
        imageHeight = valueImage?.Height ?? image?.PixelSize.Height ?? 0;
        pending.Clear();
        Selected = null;
        MissingMarkers = [];
        FitToView();
    }

    public void FitToView()
    {
        if (bitmap is null || imageWidth <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            zoom = 1;
            offset = default;
        }
        else
        {
            zoom = Math.Min(Bounds.Width / imageWidth, Bounds.Height / imageHeight);
            offset = new Vector((Bounds.Width - (imageWidth * zoom)) / 2, (Bounds.Height - (imageHeight * zoom)) / 2);
        }

        InvalidateVisual();
    }

    /// <summary>Zooms by a factor about a point of the control, the centre when none is given.</summary>
    public void ZoomBy(double factor, Point? about = null)
    {
        var at = about ?? new Point(Bounds.Width / 2, Bounds.Height / 2);
        var image = ToImage(at);
        zoom = Math.Clamp(zoom * factor, 0.02, 40);
        offset = new Vector(at.X - (image.X * zoom), at.Y - (image.Y * zoom));
        InvalidateVisual();
    }

    /// <summary>The whole canvas takes taps, including where nothing is drawn yet, so a first tap on an empty area still marks.</summary>
    public bool HitTest(Point point) => true;

    /// <summary>A control point as image pixels.</summary>
    public PointD ToImage(Point control) => new((control.X - offset.X) / zoom, (control.Y - offset.Y) / zoom);

    /// <summary>An image point as a control point.</summary>
    public Point ToControl(PointD image) => new((image.X * zoom) + offset.X, (image.Y * zoom) + offset.Y);

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

        context.DrawImage(bitmap, new Rect(offset.X, offset.Y, imageWidth * zoom, imageHeight * zoom));
        if (Session is not { } session)
        {
            return;
        }

        var state = session.State;
        var thin = new Pen(Brushes.White, 1.5);

        foreach (var marker in MissingMarkers)
        {
            var c = ToControl(marker);
            var red = new Pen(Brushes.OrangeRed, 2.5);
            context.DrawLine(red, c + new Vector(-9, -9), c + new Vector(9, 9));
            context.DrawLine(red, c + new Vector(-9, 9), c + new Vector(9, -9));
        }

        foreach (var bull in state.Bulls)
        {
            var c = ToControl(bull.Image);
            var cyan = new Pen(Brushes.DeepSkyBlue, 2);
            context.DrawLine(cyan, c + new Vector(-12, 0), c + new Vector(12, 0));
            context.DrawLine(cyan, c + new Vector(0, -12), c + new Vector(0, 12));
            context.DrawText(Label(bull.Label, Brushes.DeepSkyBlue, 12), c + new Vector(8, 6));
        }

        switch (state.Scale)
        {
            case LengthReference length:
                DrawSegment(context, length.A, length.B, Brushes.LimeGreen);
                break;
            case RectangleReference rectangle:
                for (int i = 0; i < 4; i++)
                {
                    DrawSegment(context, rectangle.Corners[i], rectangle.Corners[(i + 1) % 4], Brushes.LimeGreen);
                }

                break;
        }

        for (int i = 0; i < pending.Count; i++)
        {
            var c = ToControl(pending[i]);
            context.DrawEllipse(null, new Pen(Brushes.LimeGreen, 2), c, 6, 6);
            if (i > 0)
            {
                DrawSegment(context, pending[i - 1], pending[i], Brushes.LimeGreen);
            }
        }

        if (state.PointOfAim is { } aim)
        {
            var c = ToControl(aim);
            var magenta = new Pen(Brushes.Magenta, 2);
            context.DrawEllipse(null, magenta, c, 14, 14);
            context.DrawLine(magenta, c + new Vector(-20, 0), c + new Vector(20, 0));
            context.DrawLine(magenta, c + new Vector(0, -20), c + new Vector(0, 20));
        }

        int number = 0;
        foreach (var shot in state.Shots)
        {
            var at = dragging == shot.Id ? dragAt : shot.Image;
            var c = ToControl(at);
            if (shot.NotAShot)
            {
                var grey = new Pen(Brushes.Gray, 2);
                context.DrawLine(grey, c + new Vector(-8, -8), c + new Vector(8, 8));
                context.DrawLine(grey, c + new Vector(-8, 8), c + new Vector(8, -8));
                continue;
            }

            number++;
            IBrush colour = shot.Provenance switch
            {
                ShotProvenance.Automatic => Brushes.Gold,
                ShotProvenance.Corrected => Brushes.Orange,
                _ => Brushes.LawnGreen,
            };
            var pen = shot.Exclusion is null ? new Pen(colour, 2.5) : new Pen(colour, 2, new DashStyle([2, 2], 0));
            if (shot.Id == Selected)
            {
                context.DrawEllipse(null, new Pen(Brushes.White, 3), c, MarkRadius + 5, MarkRadius + 5);
            }

            context.DrawEllipse(null, pen, c, MarkRadius, MarkRadius);
            context.DrawEllipse(colour, null, c, 1.5, 1.5);
            context.DrawText(Label(number.ToString(CultureInfo.InvariantCulture) + (shot.Exclusion is null ? "" : " excluded"), colour, 12), c + new Vector(MarkRadius + 2, -MarkRadius - 4));
            if (shot.Bull is { } b && state.Bulls.FirstOrDefault(x => x.Index == b) is { } bull)
            {
                context.DrawLine(new Pen(colour, 1, new DashStyle([4, 4], 0)), c, ToControl(bull.Image));
            }
        }

        _ = thin;
    }

    private void DrawSegment(DrawingContext context, PointD a, PointD b, IBrush brush) => context.DrawLine(new Pen(brush, 2), ToControl(a), ToControl(b));

    private static FormattedText Label(string text, IBrush brush, double size) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Mono), size, brush);

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
                pending.Add(image);
                if (Tool == MarkingTool.Length && pending.Count == 2)
                {
                    LengthTapped?.Invoke(this, [.. pending]);
                    pending.Clear();
                }
                else if (Tool == MarkingTool.Rectangle && pending.Count == 4)
                {
                    RectangleTapped?.Invoke(this, [.. pending]);
                    pending.Clear();
                }

                break;

            case MarkingTool.Aim:
                session.SetPointOfAim(image);
                break;

            case MarkingTool.Impact:
                var snapped = value is null ? image : Snapping.ToDarkCentroid(value, image, 2 * HitRadius / zoom);
                int? nearestBull = session.State.Bulls.Count == 0 ? null : session.State.Bulls.MinBy(b => Distance(b.Image, snapped))!.Index;
                Selected = session.AddShot(snapped, nearestBull);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
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

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        if (panFrom is { } from)
        {
            offset += position - from;
            panFrom = position;
            InvalidateVisual();
        }
        else if (dragging is not null)
        {
            dragAt = ToImage(position);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (dragging is { } id && Session is { } session && session.State.Find(id) is { } shot && Distance(ToControl(shot.Image), ToControl(dragAt)) > 3)
        {
            // One move, one undo step, however long the drag.
            session.MoveShot(id, dragAt);
        }

        dragging = null;
        panFrom = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        ZoomBy(e.Delta.Y > 0 ? 1.2 : 1 / 1.2, e.GetPosition(this));
        e.Handled = true;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
}
