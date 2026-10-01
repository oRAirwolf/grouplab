using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.StoreTargets;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 340 section 2: the small drawing beside each answer to "Which target is this?", drawn by GroupLab and never
/// the maker's art: the printed area's outline, the bull as a ring, and its aim point, at <see cref="PointsPerInch"/> so every answer in
/// the question is drawn at the same scale and the larger target looks larger. The desktop's dialog and the phone's page both show it.
/// </summary>
public sealed class TargetOutline : Control
{
    /// <summary>Screen points an inch of the target: an 8 inch target is drawn 64 points across.</summary>
    public const double PointsPerInch = 8;

    private readonly FamilyAnswer answer;

    public TargetOutline(FamilyAnswer answer)
    {
        this.answer = answer ?? throw new ArgumentNullException(nameof(answer));
        var (w, h) = answer.Inches;
        Width = (w * PointsPerInch) + 2;
        Height = (h * PointsPerInch) + 2;
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var palette = Tokens.For(ActualThemeVariant);
        var (w, h) = answer.Inches;
        var outline = new Pen(new SolidColorBrush(palette.Dim), 1);
        var ring = new Pen(new SolidColorBrush(palette.Text), 1.5);
        context.DrawRectangle(null, outline, new Rect(1, 1, w * PointsPerInch, h * PointsPerInch));
        double radius = 0.49 * Math.Min(w, h) * PointsPerInch / Math.Max(1, answer.Bulls.Count == 1 ? 1 : 3);
        foreach (var bull in answer.Bulls)
        {
            var centre = new Point(1 + (bull.X * PointsPerInch), 1 + (bull.Y * PointsPerInch));
            context.DrawEllipse(null, ring, centre, radius, radius);
            context.DrawEllipse(new SolidColorBrush(palette.Amber), null, centre, 2.5, 2.5);
        }
    }
}
