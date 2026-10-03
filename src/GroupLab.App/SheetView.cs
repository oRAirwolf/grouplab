using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.Core.Gltd.Binary;
using GroupLab.App.Theme;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;

namespace GroupLab.App;

/// <summary>
/// A sheet drawn live from the same scene its PDF is written from, NOTES-FROM-PLANNING.md entry 300. Alan, on a 48 inch 3840 by 2160 OLED:
/// the preview was "heavily compressed" and did not look good. It was a bitmap of about 900 pixels; this draws the scene's own discs, bands,
/// rectangles and letters as vectors, at the screen's own resolution and at any zoom, so it is exactly what prints and sharp on any monitor,
/// with no PDF engine to carry on four platforms. The words are the PDF's letters, from the same glyph outlines the print uses. Consecutive
/// items of one ink are drawn as one geometry, built once when the scene changes, so a page of code modules draws in a handful of calls.
/// The desktop's Targets screen and the phone's draw the same control; the Android and iOS applications compile this file as it is.
/// </summary>
internal sealed class SheetView : Control
{
    public static readonly StyledProperty<Scene?> SceneProperty = AvaloniaProperty.Register<SheetView, Scene?>(nameof(Scene));

    /// <summary>Entry 358 section 2: the page as a thermal printer will print it, drawn in place of the vectors where it is set.</summary>
    public static readonly StyledProperty<DotPreview?> DotsProperty = AvaloniaProperty.Register<SheetView, DotPreview?>(nameof(Dots));

    /// <summary>The page's longer side in device-independent pixels at a zoom of 1, where the view is not told a size.</summary>
    public const double NaturalLongerSide = 900;

    private IReadOnlyList<(IBrush Ink, Geometry Shape)> drawn = [];

    static SheetView()
    {
        AffectsRender<SheetView>(SceneProperty, DotsProperty);
        AffectsMeasure<SheetView>(SceneProperty);
    }

    public SheetView()
    {
        ClipToBounds = true;
    }

    /// <summary>The page to draw, or null for none.</summary>
    public Scene? Scene
    {
        get => GetValue(SceneProperty);
        set => SetValue(SceneProperty, value);
    }

    /// <summary>The one-bit image a thermal printer will print, with where it sits on the page; null to draw the vectors.</summary>
    public DotPreview? Dots
    {
        get => GetValue(DotsProperty);
        set => SetValue(DotsProperty, value);
    }

    /// <summary>The page's size at a zoom of 1, its longer side <see cref="NaturalLongerSide"/>.</summary>
    public Size Natural => Scene is { } s && s.Width > 0 && s.Height > 0
        ? new Size(NaturalLongerSide * s.Width / Math.Max(s.Width, s.Height), NaturalLongerSide * s.Height / Math.Max(s.Width, s.Height))
        : default;

    /// <summary>How many geometries the page is drawn with, for the tests.</summary>
    internal int Geometries => drawn.Count;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SceneProperty)
        {
            drawn = Scene is { } scene ? Build(scene) : [];
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var natural = Natural;
        if (natural == default)
        {
            return default;
        }

        // Fitted, like an image stretched uniformly: as large as the room allows, the page's own shape.
        double scale = Math.Min(
            double.IsInfinity(availableSize.Width) ? 1 : availableSize.Width / natural.Width,
            double.IsInfinity(availableSize.Height) ? 1 : availableSize.Height / natural.Height);
        return new Size(natural.Width * scale, natural.Height * scale);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (Scene is not { } scene || scene.Width <= 0 || scene.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        double scale = Math.Min(Bounds.Width / scene.Width, Bounds.Height / scene.Height);
        double left = (Bounds.Width - (scene.Width * scale)) / 2, top = (Bounds.Height - (scene.Height * scale)) / 2;
        var page = new Rect(left, top, scene.Width * scale, scene.Height * scale);
        context.FillRectangle(new SolidColorBrush(Tokens.SheetPaper), page);
        if (Dots is { } dots)
        {
            // Every dot as a sharp square, never smoothed: the preview is the print, dot for dot.
            var at = new Rect(left + (dots.Left * scale), top, dots.Width * scale, dots.Height * scale);
            using (context.PushRenderOptions(new Avalonia.Media.RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                context.DrawImage(dots.Image, new Rect(dots.Image.Size), at);
            }

            context.DrawRectangle(null, new Pen(new SolidColorBrush(Tokens.SheetEdge), 1), page);
            return;
        }

        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(left, top)))
        {
            foreach (var (ink, shape) in drawn)
            {
                context.DrawGeometry(ink, null, shape);
            }
        }

        // The paper's edge, so a white page reads as a page on a white panel.
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Tokens.SheetEdge), 1), page);
    }

    /// <summary>The page as geometries, consecutive items of one ink and kind together, in the order they are painted.</summary>
    internal static IReadOnlyList<(IBrush Ink, Geometry Shape)> Build(Scene scene)
    {
        var groups = new List<(IBrush, Geometry)>();
        StreamGeometry? current = null;
        StreamGeometryContext? pen = null;
        Rgb? ink = null;
        bool? bands = null;
        void Close()
        {
            pen?.Dispose();
            pen = null;
            current = null;
        }

        foreach (var item in scene.Items)
        {
            if (item is ImageBox || (item is not DiscBand and not RectFill and not TextRun))
            {
                continue;
            }

            bool band = item is DiscBand;
            if (current is null || ink != item.Colour || bands != band)
            {
                Close();
                current = new StreamGeometry();
                pen = current.Open();
                pen.SetFillRule(band ? FillRule.EvenOdd : FillRule.NonZero);
                groups.Add((new SolidColorBrush(Tokens.SheetInk(item.Colour.R, item.Colour.G, item.Colour.B)), current));
                (ink, bands) = (item.Colour, band);
            }

            switch (item)
            {
                case DiscBand disc:
                    Outline(pen!, disc.CentreX, disc.CentreY, disc.Outer);
                    if (disc.InnerRadius > 0)
                    {
                        Outline(pen!, disc.CentreX, disc.CentreY, disc.Inner);
                    }

                    break;
                case RectFill rect:
                    Polygon(pen!, [(rect.X, rect.Y), (rect.X + rect.Width, rect.Y), (rect.X + rect.Width, rect.Y + rect.Height), (rect.X, rect.Y + rect.Height)]);
                    break;
                case TextRun text:
                    foreach (var contour in SheetGlyphs.Contours(text))
                    {
                        Polygon(pen!, contour);
                    }

                    break;
            }
        }

        Close();
        return groups;
    }

    private static void Outline(StreamGeometryContext pen, double x, double y, Outline outline)
    {
        if (outline.IsCircle)
        {
            double r = outline.Radius;
            pen.BeginFigure(new Point(x + r, y), true);
            pen.ArcTo(new Point(x - r, y), new Size(r, r), 0, false, SweepDirection.Clockwise);
            pen.ArcTo(new Point(x + r, y), new Size(r, r), 0, false, SweepDirection.Clockwise);
            pen.EndFigure(true);
            return;
        }

        Polygon(pen, [.. outline.Corners().Select(c => (x + c.X, y + c.Y))]);
    }

    private static void Polygon(StreamGeometryContext pen, IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count < 3)
        {
            return;
        }

        pen.BeginFigure(new Point(points[0].X, points[0].Y), true);
        for (int i = 1; i < points.Count; i++)
        {
            pen.LineTo(new Point(points[i].X, points[i].Y));
        }

        pen.EndFigure(true);
    }
}

/// <summary>A thermal print's dots for <see cref="SheetView"/>: the image, and where it lies on the page in half-dmm.</summary>
internal sealed record DotPreview(Bitmap Image, double Left, double Width, double Height);
