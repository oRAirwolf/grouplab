using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 348: the picture in the middle of each fingerprint step, on the computer and the phone alike. The photograph
/// with the target's outline found in it and its four corners as amber handles to drag; the two ends of a measured length in teal; or the
/// straightened target with its bulls as numbered teal rings. A tap or click away from a handle is handed to the step, which decides what
/// it means there: an end placed, a ring removed or a bull added.
/// <para>
/// Entry 362 section 3: it zooms, with the wheel about the pointer on the computer (as <see cref="SheetGestures"/> decides, like the sheet)
/// and a pinch on the phone, and a drag away from the handles pans once zoomed. While a corner is dragged a magnifier above the finger shows
/// the picture under the corner, four times larger, with a crosshair on it, so the finger never hides the point being placed.
/// </para>
/// </summary>
public sealed class FingerprintPicture : Control
{
    private Bitmap? image;
    private int dragging = -1;
    private Point pressedAt;
    private Point lastAt;
    private bool pressed;
    private bool panning;
    private double zoom = 1;
    private Vector offset;
    private double pinchScale = 1;

    /// <summary>How much larger the magnifier shows the picture than the view does, and its radius on screen.</summary>
    public const double MagnifierPower = 4;

    public const double MagnifierRadius = 64;

    /// <summary>The most the picture zooms in, over fitting the space.</summary>
    public const double MostZoom = 12;

    public FingerprintPicture()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Cross);
        AddHandler(PointerTouchPadGestureMagnifyEvent, (_, e) =>
        {
            ZoomBy(SheetGestures.MagnifyFactor(e.Delta.X != 0 ? e.Delta.X : e.Delta.Y), e.GetPosition(this));
            e.Handled = true;
        });
        GestureRecognizers.Add(new PinchGestureRecognizer());
        AddHandler(PinchEvent, (_, e) =>
        {
            if (pinchScale > 0 && e.Scale > 0)
            {
                ZoomBy(e.Scale / pinchScale, e.ScaleOrigin);
            }

            pinchScale = e.Scale;
            // Two fingers on the picture are a pinch, never a corner dragged by the first of them.
            dragging = -1;
            e.Handled = true;
        });
        AddHandler(PinchEndedEvent, (_, _) => pinchScale = 1);
    }

    /// <summary>How far the view is zoomed in over fitting the space, 1 when it fits.</summary>
    public double Zoom => zoom;

    /// <summary>The corner being dragged, or -1, and where the magnifier was last drawn while it was: for the headless tests.</summary>
    public int Dragging => dragging;

    public Point? MagnifierAt { get; private set; }

    /// <summary>What is drawn, from the step's JPEG; null draws an empty well.</summary>
    public Bitmap? Image
    {
        get => image;
        set
        {
            if (!ReferenceEquals(image, value))
            {
                // A new picture, or the straightened one in place of the photo, starts fitted again.
                (zoom, offset) = (1, default);
            }

            image = value;
            InvalidateVisual();
        }
    }

    /// <summary>The picture's pixels per unit of the points given: photograph pixels on the photograph, inches on the straightened target.</summary>
    public double PerUnit { get; set; } = 1;

    /// <summary>The target's corners, top left first, clockwise.</summary>
    public IReadOnlyList<PointD> Corners { get; set; } = [];

    /// <summary>Whether the corners can be dragged.</summary>
    public bool CornersMove { get; set; }

    /// <summary>The ends of a measured length, none to two.</summary>
    public IReadOnlyList<PointD> Ends { get; set; } = [];

    /// <summary>The bulls' centers, numbered from 1 in this order, and their rings' radius in the same units.</summary>
    public IReadOnlyList<PointD> Rings { get; set; } = [];

    public double RingRadius { get; set; }

    /// <summary>A corner dragged: which, and to where in the points' units.</summary>
    public event Action<int, PointD>? CornerMoved;

    /// <summary>A corner let go after a drag: which.</summary>
    public event Action<int>? CornerReleased;

    /// <summary>A tap or click that was not a drag, in the points' units.</summary>
    public event Action<PointD>? PointTapped;

    /// <summary>How far from a handle a press still takes it, screen units: a thumb's reach.</summary>
    public double Reach { get; set; } = 24;

    /// <summary>The picture's place in the control and its screen units per picture pixel.</summary>
    private (Rect Area, double Factor) Fit()
    {
        if (image is null || image.PixelSize.Width == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return (default, 0);
        }

        double factor = Math.Min(Bounds.Width / image.PixelSize.Width, Bounds.Height / image.PixelSize.Height) * zoom;
        double w = image.PixelSize.Width * factor, h = image.PixelSize.Height * factor;
        return (new Rect(((Bounds.Width - w) / 2) + offset.X, ((Bounds.Height - h) / 2) + offset.Y, w, h), factor);
    }

    /// <summary>Zooms by <paramref name="factor"/> keeping the picture under <paramref name="about"/> where it is; back to fitting at 1.</summary>
    public void ZoomBy(double factor, Point about)
    {
        if (image is null || factor <= 0 || !double.IsFinite(factor))
        {
            return;
        }

        var under = FromScreen(about);
        zoom = Math.Clamp(zoom * factor, 1, MostZoom);
        if (zoom <= 1.0001)
        {
            (zoom, offset) = (1, default);
        }
        else
        {
            var now = ToScreen(under);
            offset += new Vector(about.X - now.X, about.Y - now.Y);
            Keep();
        }

        InvalidateVisual();
    }

    /// <summary>Moves the zoomed picture by a distance on screen, as the content of any scrolled view follows the fingers.</summary>
    public void PanBy(Vector by)
    {
        if (zoom <= 1)
        {
            return;
        }

        offset += by;
        Keep();
        InvalidateVisual();
    }

    /// <summary>The zoomed picture kept covering the space, so it cannot be dragged away and lost.</summary>
    private void Keep()
    {
        var (area, _) = Fit();
        double x = area.Width <= Bounds.Width ? 0 : Math.Clamp(offset.X, (Bounds.Width - area.Width) / 2, (area.Width - Bounds.Width) / 2);
        double y = area.Height <= Bounds.Height ? 0 : Math.Clamp(offset.Y, (Bounds.Height - area.Height) / 2, (area.Height - Bounds.Height) / 2);
        offset = new Vector(x, y);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerWheelChanged(e);
        Scroll(e.Delta, e.KeyModifiers, e.GetPosition(this));
        e.Handled = true;
    }

    /// <summary>A scroll, zooming about the pointer or panning as <see cref="SheetGestures"/> decides for this platform.</summary>
    internal WheelAction Scroll(Vector delta, KeyModifiers modifiers, Point about)
    {
        var action = SheetGestures.ForWheel(delta, CommandKey.Held(modifiers), OperatingSystem.IsMacOS());
        if (action == WheelAction.Zoom)
        {
            ZoomBy(SheetGestures.ZoomFactor(delta.Y), about);
        }
        else
        {
            PanBy(delta * SheetGestures.PixelsPerUnit);
        }

        return action;
    }

    /// <summary>A point in the units given, on the screen.</summary>
    public Point ToScreen(PointD p)
    {
        var (area, factor) = Fit();
        return new Point(area.X + (p.X * PerUnit * factor), area.Y + (p.Y * PerUnit * factor));
    }

    /// <summary>A point on the screen, in the units given.</summary>
    public PointD FromScreen(Point p)
    {
        var (area, factor) = Fit();
        double k = Math.Max(1e-9, PerUnit * factor);
        return new PointD((p.X - area.X) / k, (p.Y - area.Y) / k);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        var (area, factor) = Fit();
        if (image is null || factor <= 0)
        {
            return;
        }

        context.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), area);
        var amber = new SolidColorBrush(palette.Amber);
        var teal = new SolidColorBrush(palette.Teal);
        var halo = new Pen(new SolidColorBrush(palette.Bg), 4);
        if (Corners.Count == 4)
        {
            var points = Corners.Select(ToScreen).ToList();
            var outline = new StreamGeometry();
            using (var g = outline.Open())
            {
                g.BeginFigure(points[0], false);
                foreach (var p in points.Skip(1))
                {
                    g.LineTo(p);
                }

                g.EndFigure(true);
            }

            context.DrawGeometry(null, halo, outline);
            context.DrawGeometry(null, new Pen(amber, 2, new DashStyle([4, 3], 0)), outline);
            if (CornersMove)
            {
                foreach (var p in points)
                {
                    context.DrawEllipse(amber, halo, p, 9, 9);
                }
            }
        }

        var ends = Ends.Select(ToScreen).ToList();
        if (ends.Count == 2)
        {
            context.DrawLine(halo, ends[0], ends[1]);
            context.DrawLine(new Pen(teal, 2), ends[0], ends[1]);
        }

        foreach (var p in ends)
        {
            context.DrawEllipse(teal, halo, p, 7, 7);
        }

        double radius = Math.Max(10, RingRadius * PerUnit * factor);
        var ring = new Pen(teal, 2.5);
        for (int i = 0; i < Rings.Count; i++)
        {
            var c = ToScreen(Rings[i]);
            context.DrawEllipse(null, halo, c, radius, radius);
            context.DrawEllipse(null, ring, c, radius, radius);
            var number = new FormattedText((i + 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Tokens.Mono, FontStyle.Normal, FontWeight.SemiBold), 14, teal);
            var at = new Point(c.X + (radius * 0.75), c.Y - (radius * 0.75) - number.Height);
            context.FillRectangle(new SolidColorBrush(palette.Bg), new Rect(at.X - 2, at.Y, number.Width + 4, number.Height));
            context.DrawText(number, at);
        }

        if (dragging >= 0 && dragging < Corners.Count)
        {
            Magnifier(context, ToScreen(Corners[dragging]), area, factor, palette);
        }
    }

    /// <summary>
    /// The picture round <paramref name="corner"/>, larger, in a circle above it (below it near the top), with a crosshair on the corner and
    /// the outline's two sides through it.
    /// </summary>
    private void Magnifier(DrawingContext context, Point corner, Rect area, double factor, Palette palette)
    {
        double r = MagnifierRadius, gap = r + 56;
        var centre = new Point(Math.Clamp(corner.X, r + 4, Math.Max(r + 4, Bounds.Width - r - 4)), corner.Y - gap >= r + 4 ? corner.Y - gap : corner.Y + gap);
        MagnifierAt = centre;
        var circle = new Rect(centre.X - r, centre.Y - r, 2 * r, 2 * r);
        // The picture's pixel under the corner, and the piece of the picture the circle shows round it.
        double px = (corner.X - area.X) / factor, py = (corner.Y - area.Y) / factor, half = r / (factor * MagnifierPower);
        var source = new Rect(px - half, py - half, 2 * half, 2 * half);
        context.DrawEllipse(new SolidColorBrush(palette.Sunk), null, centre, r, r);
        using (context.PushGeometryClip(new EllipseGeometry(circle)))
        {
            context.DrawImage(image!, source, circle);
            if (Corners.Count == 4)
            {
                var pen = new Pen(new SolidColorBrush(palette.Amber), 1.5);
                foreach (int neighbour in new[] { (dragging + 3) % 4, (dragging + 1) % 4 })
                {
                    var n = ToScreen(Corners[neighbour]);
                    context.DrawLine(pen, centre, new Point(centre.X + ((n.X - corner.X) * MagnifierPower), centre.Y + ((n.Y - corner.Y) * MagnifierPower)));
                }
            }
        }

        var cross = new Pen(new SolidColorBrush(palette.Text), 1);
        context.DrawLine(cross, new Point(centre.X - r, centre.Y), new Point(centre.X - 6, centre.Y));
        context.DrawLine(cross, new Point(centre.X + 6, centre.Y), new Point(centre.X + r, centre.Y));
        context.DrawLine(cross, new Point(centre.X, centre.Y - r), new Point(centre.X, centre.Y - 6));
        context.DrawLine(cross, new Point(centre.X, centre.Y + 6), new Point(centre.X, centre.Y + r));
        context.DrawEllipse(null, new Pen(new SolidColorBrush(palette.Amber), 2.5), centre, r, r);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var at = e.GetPosition(this);
        pressed = true;
        pressedAt = lastAt = at;
        panning = false;
        dragging = -1;
        if (CornersMove)
        {
            double nearest = Reach;
            for (int i = 0; i < Corners.Count; i++)
            {
                var c = ToScreen(Corners[i]);
                double d = Math.Sqrt(Math.Pow(c.X - at.X, 2) + Math.Pow(c.Y - at.Y, 2));
                if (d <= nearest)
                {
                    (dragging, nearest) = (i, d);
                }
            }
        }

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerMoved(e);
        var at = e.GetPosition(this);
        if (dragging >= 0)
        {
            CornerMoved?.Invoke(dragging, FromScreen(at));
            InvalidateVisual();
        }
        else if (pressed && zoom > 1 && (panning || Math.Abs(at.X - pressedAt.X) + Math.Abs(at.Y - pressedAt.Y) >= 12))
        {
            // A drag away from the handles moves the zoomed picture, and is then no tap.
            panning = true;
            PanBy(at - lastAt);
        }

        lastAt = at;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        var at = e.GetPosition(this);
        if (pressed && dragging < 0 && !panning && Math.Abs(at.X - pressedAt.X) + Math.Abs(at.Y - pressedAt.Y) < 12)
        {
            PointTapped?.Invoke(FromScreen(at));
        }

        int released = dragging;
        pressed = false;
        panning = false;
        dragging = -1;
        if (released >= 0)
        {
            CornerReleased?.Invoke(released);
        }

        e.Pointer.Capture(null);
        InvalidateVisual();
    }
}
