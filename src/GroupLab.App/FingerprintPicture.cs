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
/// </summary>
public sealed class FingerprintPicture : Control
{
    private Bitmap? image;
    private int dragging = -1;
    private Point pressedAt;
    private bool pressed;

    public FingerprintPicture()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Cross);
    }

    /// <summary>What is drawn, from the step's JPEG; null draws an empty well.</summary>
    public Bitmap? Image
    {
        get => image;
        set
        {
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

        double factor = Math.Min(Bounds.Width / image.PixelSize.Width, Bounds.Height / image.PixelSize.Height);
        double w = image.PixelSize.Width * factor, h = image.PixelSize.Height * factor;
        return (new Rect((Bounds.Width - w) / 2, (Bounds.Height - h) / 2, w, h), factor);
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
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerPressed(e);
        var at = e.GetPosition(this);
        pressed = true;
        pressedAt = at;
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
        if (dragging >= 0)
        {
            CornerMoved?.Invoke(dragging, FromScreen(e.GetPosition(this)));
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPointerReleased(e);
        var at = e.GetPosition(this);
        if (pressed && dragging < 0 && Math.Abs(at.X - pressedAt.X) + Math.Abs(at.Y - pressedAt.Y) < 12)
        {
            PointTapped?.Invoke(FromScreen(at));
        }

        pressed = false;
        dragging = -1;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }
}
