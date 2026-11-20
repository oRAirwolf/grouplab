using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 504 section 1: the "not here yet" tag on the choice between two versions of a pooled set, a grey fill
/// inside a dashed border, so it never reads as the amber "only in this one". The words are the child, styled by the host as its quiet
/// text. Shared with the phone, which compiles this file too.
/// </summary>
public sealed class DashedTag : Decorator
{
    public DashedTag(Control words)
    {
        Child = words;
        Padding = new Thickness(7, 1);
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        // The theme's own field border colour, as SyncHolesPlot takes it, so the tag follows the theme; with none, the words stand alone.
        if (!this.TryFindResource("TextControlBorderBrush", ActualThemeVariant, out var b) || b is not ISolidColorBrush line)
        {
            return;
        }

        var box = new Rect(Bounds.Size).Deflate(0.5);
        var fill = new SolidColorBrush(line.Color, 0.18);
        var pen = new Pen(line, 1, new DashStyle([3, 2], 0));
        context.DrawRectangle(fill, pen, box, 6, 6);
    }
}
