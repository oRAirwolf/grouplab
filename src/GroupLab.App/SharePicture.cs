using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Theme;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Reporting;

namespace GroupLab.App;

/// <summary>The results box's look on a shared picture: dark with light words, light with dark words, or the words alone.</summary>
public enum ShareBoxStyle
{
    Dark,
    Light,
    Clear,
}

/// <summary>
/// Share A, NOTES-FROM-PLANNING.md entry 280 section 2 (board ShareA, entry 278 features d and g): the picture of the target with a results box
/// on it, dragged anywhere and resized by pinching it or by its corner handle, its lines chosen by tapping it (<see cref="BoxTapped"/>), the
/// mean radius circle drawn about the group's centre, a label, the box's style and the crop. The phone's page and the desktop's window both
/// show this control, and <see cref="Png"/> draws the same picture at full size for "Save to gallery", "Share" and "Save picture".
/// <para>
/// Everything is placed as a fraction of the picture shown, so the saved picture is the one on the screen at any size. Coordinates in
/// <see cref="Circles"/> and <see cref="Crop"/> are in the marking's stored pixels, the frame <c>frameWidth</c> by <c>frameHeight</c>, which
/// may be smaller than the bitmap where the marking was measured on a reduced copy.
/// </para>
/// </summary>
public sealed class SharePicture : Control
{
    private static readonly IBrush Light = new SolidColorBrush(Tokens.ShareInkLight);
    private static readonly IBrush Dark = new SolidColorBrush(Tokens.ShareInkDark);
    private readonly Bitmap image;
    private readonly double frameWidth;
    private readonly double frameHeight;
    private readonly int turns;
    private readonly Dictionary<long, Point> pointers = [];
    private Point? pressedAt;
    private (Point Start, Point Box)? dragging;
    private (Point Start, double Scale)? resizing;
    private (double Distance, double Scale)? pinching;
    private bool moved;

    public SharePicture(Bitmap image, double frameWidth, double frameHeight, int turns)
    {
        this.image = image ?? throw new ArgumentNullException(nameof(image));
        this.frameWidth = frameWidth > 0 ? frameWidth : image.PixelSize.Width;
        this.frameHeight = frameHeight > 0 ? frameHeight : image.PixelSize.Height;
        this.turns = ViewRotation.Normalise(turns);
        ClipToBounds = true;
    }

    /// <summary>The results box was tapped: the page then offers its lines to choose.</summary>
    public event Action? BoxTapped;

    /// <summary>The mean radius circle, one at each aim point, in stored pixels.</summary>
    public IReadOnlyList<ShareCircle> Circles { get; set; } = [];

    public bool ShowCircle { get; set; } = true;

    /// <summary>A label across the top of the picture, or none.</summary>
    public string? Label { get; set; }

    /// <summary>The lines the box shows, in order.</summary>
    public IReadOnlyList<string> Lines { get; set; } = [];

    public ShareBoxStyle Style { get; set; } = ShareBoxStyle.Dark;

    /// <summary>The part of the picture shown, in stored pixels, or null for the whole picture.</summary>
    public (double X, double Y, double Width, double Height)? Crop { get; set; }

    /// <summary>The box's top left corner as a fraction of the picture shown, across and down; it starts in the bottom left corner, clear of the group.</summary>
    public Point BoxAt { get; set; } = new(0.03, 1);

    /// <summary>The box's size, 1 as it starts; pinching and the corner handle change it, from a third to three times.</summary>
    public double BoxScale { get; set; } = 1;

    /// <summary>The picture's size as it is shown, turned and cropped, in stored pixels.</summary>
    private Rect Shown
    {
        get
        {
            var (w, h) = ViewRotation.DisplaySize(turns, frameWidth, frameHeight);
            if (Crop is not { } crop)
            {
                return new Rect(0, 0, w, h);
            }

            // The crop is in stored pixels; its corners turned give the shown rectangle.
            var a = ViewRotation.ToDisplay(new PointD(crop.X, crop.Y), turns, frameWidth, frameHeight);
            var b = ViewRotation.ToDisplay(new PointD(crop.X + crop.Width, crop.Y + crop.Height), turns, frameWidth, frameHeight);
            return new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var shown = Shown;
        double width = double.IsFinite(availableSize.Width) ? availableSize.Width : Math.Min(640, shown.Width);
        double height = width * shown.Height / shown.Width;
        if (double.IsFinite(availableSize.Height) && height > availableSize.Height)
        {
            height = availableSize.Height;
            width = height * shown.Width / shown.Height;
        }

        return new Size(width, height);
    }

    public override void Render(DrawingContext context) => Draw(context, Bounds.Size, interactive: true);

    /// <summary>The box's rectangle and its font size on a picture drawn <paramref name="size"/> across.</summary>
    private (Rect Box, double Font, IReadOnlyList<FormattedText> Texts) Box(Size size, IBrush ink)
    {
        double font = Math.Max(8, size.Width * 0.034 * BoxScale);
        var texts = Lines.Select((line, i) => new FormattedText(line, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface(Tokens.Sans, FontStyle.Normal, i == 0 ? FontWeight.SemiBold : FontWeight.Normal), font, ink)).ToList();
        double pad = font * 0.6;
        double width = texts.Count == 0 ? font * 4 : texts.Max(t => t.Width) + (2 * pad);
        double height = texts.Sum(t => t.Height) + (2 * pad);
        // A little way in from every edge, so the box never touches the picture's border.
        double inset = font * 0.5;
        double x = Math.Clamp(BoxAt.X * size.Width, Math.Min(inset, size.Width / 2), Math.Max(Math.Min(inset, size.Width / 2), size.Width - width - inset));
        double y = Math.Clamp(BoxAt.Y * size.Height, Math.Min(inset, size.Height / 2), Math.Max(Math.Min(inset, size.Height / 2), size.Height - height - inset));
        return (new Rect(x, y, width, height), font, texts);
    }

    private Rect Handle(Rect box) => new(box.Right - 22, box.Bottom - 22, 44, 44);

    /// <summary>The whole picture on <paramref name="context"/> at <paramref name="size"/>; the corner handle only where it can be dragged.</summary>
    private void Draw(DrawingContext context, Size size, bool interactive)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var shown = Shown;
        double k = size.Width / shown.Width;
        var (a, b, c, d, e, f) = ViewRotation.Affine(turns, frameWidth, frameHeight);
        // Stored pixels to the view: the turn, then the crop's corner to the origin, then k.
        var toView = new Matrix(a * k, d * k, b * k, e * k, (c - shown.X) * k, (f - shown.Y) * k);
        using (context.PushClip(new Rect(size)))
        {
            using (context.PushTransform(toView))
            {
                context.DrawImage(image, new Rect(image.Size), new Rect(0, 0, frameWidth, frameHeight));
            }

            Point View(PointD stored)
            {
                var p = ViewRotation.ToDisplay(stored, turns, frameWidth, frameHeight);
                return new Point((p.X - shown.X) * k, (p.Y - shown.Y) * k);
            }

            double stroke = Math.Max(2, size.Width / 320);
            if (ShowCircle)
            {
                var halo = new Pen(new SolidColorBrush(Tokens.ShareHalo), stroke * 2.2);
                var ring = new Pen(new SolidColorBrush(Tokens.ShareRing), stroke);
                foreach (var circle in Circles)
                {
                    var at = View(circle.Centre);
                    context.DrawEllipse(null, halo, at, circle.Radius * k, circle.Radius * k);
                    context.DrawEllipse(null, ring, at, circle.Radius * k, circle.Radius * k);
                }
            }

            if (!string.IsNullOrWhiteSpace(Label))
            {
                var text = new FormattedText(Label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans, FontStyle.Normal, FontWeight.SemiBold),
                    Math.Max(10, size.Width * 0.045), Light);
                var place = new Rect((size.Width - text.Width) / 2 - (text.Height * 0.4), size.Height * 0.02, text.Width + (text.Height * 0.8), text.Height * 1.2);
                context.DrawRectangle(new SolidColorBrush(Tokens.ShareShade), null, place, text.Height * 0.3, text.Height * 0.3);
                context.DrawText(text, new Point(place.X + (text.Height * 0.4), place.Y + (text.Height * 0.1)));
            }

            if (Lines.Count > 0)
            {
                var (ink, fill, edge) = Style switch
                {
                    ShareBoxStyle.Light => ((IBrush)Dark, (IBrush?)new SolidColorBrush(Tokens.ShareBoxLight), (IPen?)null),
                    ShareBoxStyle.Clear => (Light, null, null),
                    _ => (Light, new SolidColorBrush(Tokens.ShareBoxDark), new Pen(new SolidColorBrush(Tokens.ShareBoxEdge), 1)),
                };
                var (box, font, texts) = Box(size, ink);
                context.DrawRectangle(fill, edge, box, font * 0.35, font * 0.35);
                double y = box.Y + (font * 0.6);
                foreach (var text in texts)
                {
                    if (Style == ShareBoxStyle.Clear)
                    {
                        // Words alone keep a dark edge so they read on a light target.
                        if (text.BuildGeometry(new Point(box.X + (font * 0.6), y)) is { } outline)
                        {
                            context.DrawGeometry(null, new Pen(Dark, font / 5), outline);
                        }
                    }

                    context.DrawText(text, new Point(box.X + (font * 0.6), y));
                    y += text.Height;
                }

                if (interactive)
                {
                    var handle = Handle(box);
                    context.DrawEllipse(new SolidColorBrush(Tokens.ShareHandle), new Pen(Light, 2), handle.Center, 9, 9);
                }
            }
        }
    }

    /// <summary>
    /// The picture as a PNG, <paramref name="longest"/> pixels on its longer side at most and never larger than the part of the picture shown:
    /// the same drawing as on the screen, without the corner handle.
    /// </summary>
    public byte[] Png(int longest = 2400)
    {
        var shown = Shown;
        double k = Math.Min(1, longest / Math.Max(shown.Width, shown.Height)) * Math.Max(1, image.PixelSize.Width / frameWidth);
        var pixels = new PixelSize(Math.Max(1, (int)Math.Round(shown.Width * k)), Math.Max(1, (int)Math.Round(shown.Height * k)));
        if (Math.Max(pixels.Width, pixels.Height) > longest)
        {
            double fit = (double)longest / Math.Max(pixels.Width, pixels.Height);
            pixels = new PixelSize(Math.Max(1, (int)(pixels.Width * fit)), Math.Max(1, (int)(pixels.Height * fit)));
        }

        using var bitmap = new RenderTargetBitmap(pixels, new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            Draw(context, new Size(pixels.Width, pixels.Height), interactive: false);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var p = e.GetPosition(this);
        pointers[e.Pointer.Id] = p;
        e.Pointer.Capture(this);
        var (box, _, _) = Box(Bounds.Size, Light);
        if (pointers.Count == 2)
        {
            // Two fingers: a pinch resizes the box wherever they are.
            var two = pointers.Values.ToList();
            pinching = (Point.Distance(two[0], two[1]), BoxScale);
            dragging = null;
            resizing = null;
        }
        else if (Lines.Count > 0 && Handle(box).Contains(p))
        {
            resizing = (p, BoxScale);
        }
        else if (Lines.Count > 0 && box.Contains(p))
        {
            dragging = (p, new Point(box.X, box.Y));
        }

        pressedAt = p;
        moved = false;
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!pointers.ContainsKey(e.Pointer.Id))
        {
            return;
        }

        var p = e.GetPosition(this);
        pointers[e.Pointer.Id] = p;
        if (pressedAt is { } start && Point.Distance(start, p) > 8)
        {
            moved = true;
        }

        var size = Bounds.Size;
        if (pinching is { } pinch && pointers.Count >= 2 && pinch.Distance > 0)
        {
            var two = pointers.Values.Take(2).ToList();
            BoxScale = Math.Clamp(pinch.Scale * Point.Distance(two[0], two[1]) / pinch.Distance, 0.33, 3);
        }
        else if (resizing is { } resize)
        {
            var (box, _, _) = Box(size, Light);
            double grow = 1 + ((p.X - resize.Start.X) / Math.Max(40, box.Width));
            BoxScale = Math.Clamp(resize.Scale * grow, 0.33, 3);
            resizing = (p, BoxScale);
        }
        else if (dragging is { } drag && size.Width > 0 && size.Height > 0)
        {
            BoxAt = new Point((drag.Box.X + p.X - drag.Start.X) / size.Width, (drag.Box.Y + p.Y - drag.Start.Y) / size.Height);
        }
        else
        {
            return;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        bool tapped = dragging is not null && !moved && pointers.Count == 1;
        pointers.Remove(e.Pointer.Id);
        e.Pointer.Capture(null);
        if (pointers.Count == 0)
        {
            dragging = null;
            resizing = null;
            pinching = null;
            pressedAt = null;
        }

        if (tapped)
        {
            BoxTapped?.Invoke();
        }

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        pointers.Remove(e.Pointer.Id);
    }

    /// <summary>For the tests: a tap on the box as a finger would make it.</summary>
    internal void TapBox() => BoxTapped?.Invoke();
}
