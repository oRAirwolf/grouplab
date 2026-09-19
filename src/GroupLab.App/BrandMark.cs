using System.Globalization;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// GroupLab's mark, NOTES-FROM-PLANNING.md entry 105 section 4: two grey rings and three amber holes up and to the right of centre, which is
/// the idea, since GroupLab exists to show where a rifle hits against where it was aimed. <see cref="Lockup"/> adds the wordmark, GROUP in
/// grey and LAB in amber, as outlines, so no font is needed at runtime.
/// <para>
/// The committed SVG files are the source of truth, and this draws them rather than a copy of their geometry: the dark file in the dark
/// and high-contrast themes, the light file in the light theme, each in its own colours (question 20 of docs/QUESTIONS-FOR-PLANNING.md asks
/// whether those colours become tokens). It reads only what the files use: circles and filled paths, with fill, stroke and stroke width.
/// </para>
/// </summary>
internal sealed class BrandMark : Control
{
    private static readonly Dictionary<string, Artwork> Cache = [];

    /// <summary>The mark and the wordmark together, where true; the mark alone otherwise.</summary>
    public bool Lockup { get; init; }

    /// <summary>The file this control draws in the current theme, for the headless tests.</summary>
    public string Source => $"grouplab-{(Lockup ? "lockup" : "mark")}{(ActualThemeVariant == ThemeVariant.Light ? "-light" : "")}.svg";

    /// <summary>The artwork as read from <see cref="Source"/>.</summary>
    public Artwork Art => Load(Source);

    protected override Size MeasureOverride(Size availableSize)
    {
        var box = Art.ViewBox;
        double height = double.IsNaN(Height) ? Math.Min(availableSize.Height, 26) : Height;
        return new Size(height * box.Width / box.Height, height);
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var art = Art;
        double scale = Math.Min(Bounds.Width / art.ViewBox.Width, Bounds.Height / art.ViewBox.Height);
        using (context.PushTransform(Matrix.CreateTranslation(-art.ViewBox.X, -art.ViewBox.Y) * Matrix.CreateScale(scale, scale)))
        {
            foreach (var shape in art.Shapes)
            {
                IBrush? fill = shape.Fill is { } f ? new SolidColorBrush(f) : null;
                IPen? pen = shape.Stroke is { } s ? new Pen(new SolidColorBrush(s), shape.StrokeWidth) : null;
                context.DrawGeometry(fill, pen, shape.Geometry);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThemeVariantScope.ActualThemeVariantProperty)
        {
            InvalidateVisual();
        }
    }

    /// <summary>One shape of the artwork, in the file's own units.</summary>
    internal sealed record Shape(Geometry Geometry, Color? Fill, Color? Stroke, double StrokeWidth);

    /// <summary>The artwork of one file: its view box and its shapes in drawing order.</summary>
    internal sealed record Artwork(Rect ViewBox, IReadOnlyList<Shape> Shapes);

    /// <summary>Reads a committed SVG from the application's resources, once.</summary>
    internal static Artwork Load(string file)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(file, out var cached))
            {
                return cached;
            }

            using var stream = AssetLoader.Open(new Uri($"avares://GroupLab.App/Assets/{file}"));
            var art = Parse(XDocument.Load(stream));
            Cache[file] = art;
            return art;
        }
    }

    /// <summary>The circles and paths of an SVG document, the only elements the mark files use.</summary>
    internal static Artwork Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = document.Root ?? throw new InvalidDataException("an empty SVG");
        double[] box = [.. ((string?)root.Attribute("viewBox") ?? "0 0 100 100").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Number)];
        var shapes = new List<Shape>();
        foreach (var element in root.Elements())
        {
            Geometry? geometry = element.Name.LocalName switch
            {
                "circle" => new EllipseGeometry(new Rect(
                    Number(element, "cx") - Number(element, "r"), Number(element, "cy") - Number(element, "r"), 2 * Number(element, "r"), 2 * Number(element, "r"))),
                "path" => Geometry.Parse((string?)element.Attribute("d") ?? ""),
                _ => null,
            };
            if (geometry is null)
            {
                continue;
            }

            // An absent fill is SVG's default, black; the mark's files name every fill.
            shapes.Add(new Shape(geometry, Colour((string?)element.Attribute("fill") ?? "black"), Colour((string?)element.Attribute("stroke")), Number(element, "stroke-width", 1)));
        }

        return new Artwork(new Rect(box[0], box[1], box[2], box[3]), shapes);
    }

    private static Color? Colour(string? value) => value is null || value == "none" ? null : Tokens.Ink(value);

    private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static double Number(XElement element, string name, double fallback = 0) =>
        element.Attribute(name) is { } a ? Number(a.Value) : fallback;
}
