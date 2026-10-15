using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// Entry 431, issue #34: the tab strip's scrollbar in a row of its own under the tabs, concept A as Alan chose it. Fluent's pop-up bar
/// was drawn over the lower half of the tabs, so with many targets open their names and close buttons could not be clicked. This one is
/// there only while the tabs overflow and takes no space when they fit; it never hides, and is a quieter shade while the pointer is away
/// from the strip. Dragging the thumb scrolls the strip, and pressing the track beside it moves a page. The wheel and the touchpad still
/// scroll the strip itself.
/// </summary>
internal sealed class TabScrollBar : Control
{
    /// <summary>The bar's height and the gap above it, in pixels.</summary>
    public const double BarHeight = 8;

    public const double Gap = Tokens.Space4;

    private readonly ScrollViewer strip;

    /// <summary>Where on the thumb it was grabbed, while it is being dragged.</summary>
    private double? grabbed;

    public TabScrollBar(ScrollViewer strip)
    {
        this.strip = strip;
        Height = BarHeight;
        Margin = new Thickness(0, Gap, 0, 0);
        IsVisible = false;
        strip.ScrollChanged += (_, _) => Fit();
        strip.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsPointerOverProperty)
            {
                InvalidateVisual();
            }
        };
        SizeChanged += (_, _) => InvalidateVisual();
    }

    /// <summary>Whether the tabs are wider than the strip, which is when the bar is there.</summary>
    internal bool Overflows => strip.Extent.Width - strip.Viewport.Width > 0.5;

    /// <summary>Whether the bar is at its full shade: the pointer on the strip or the bar, or the thumb held.</summary>
    internal bool Lit => strip.IsPointerOver || IsPointerOver || grabbed is not null;

    /// <summary>The thumb, in the bar's own coordinates: as wide as the share of the tabs in view, where the strip is scrolled to.</summary>
    internal Rect Thumb
    {
        get
        {
            double track = Bounds.Width, extent = strip.Extent.Width, view = strip.Viewport.Width;
            if (track <= 0 || extent - view <= 0.5)
            {
                return new Rect(0, 0, Math.Max(0, track), BarHeight);
            }

            double width = Math.Min(track, Math.Max(BarHeight * 3, track * view / extent));
            return new Rect((track - width) * Math.Clamp(strip.Offset.X / (extent - view), 0, 1), 0, width, BarHeight);
        }
    }

    private void Fit()
    {
        IsVisible = Overflows;
        InvalidateVisual();
    }

    private void ScrollTo(double x) =>
        strip.Offset = new Vector(Math.Clamp(x, 0, Math.Max(0, strip.Extent.Width - strip.Viewport.Width)), strip.Offset.Y);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var at = e.GetPosition(this);
        var thumb = Thumb;
        if (thumb.Contains(at))
        {
            grabbed = at.X - thumb.X;
            e.Pointer.Capture(this);
        }
        else
        {
            ScrollTo(strip.Offset.X + (at.X < thumb.X ? -1 : 1) * strip.Viewport.Width);
        }

        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (grabbed is not { } g)
        {
            return;
        }

        var thumb = Thumb;
        double room = Bounds.Width - thumb.Width;
        ScrollTo(room <= 0 ? 0 : (e.GetPosition(this).X - g) / room * (strip.Extent.Width - strip.Viewport.Width));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        grabbed = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        grabbed = null;
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThemeVariantScope.ActualThemeVariantProperty || change.Property == IsPointerOverProperty)
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        var palette = Tokens.For(ActualThemeVariant);
        var radius = new CornerRadius(BarHeight / 2);
        context.DrawRectangle(new SolidColorBrush(palette.Sunk), null, new RoundedRect(new Rect(Bounds.Size), radius));
        context.DrawRectangle(new SolidColorBrush(Lit ? palette.Dim : palette.Faint), null, new RoundedRect(Thumb, radius));
    }
}
