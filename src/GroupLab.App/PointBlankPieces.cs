using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using GroupLab.App.Theme;
using GroupLab.Core.Ballistics;

namespace GroupLab.App;

/// <summary>NOTES-FROM-PLANNING.md entry 455: what the computer's and the phone's point blank views share, the target list and its picture.</summary>
internal static class PointBlankPieces
{
    /// <summary>The targets in the list, then your own circle and your own rectangle.</summary>
    public static readonly IReadOnlyList<PointBlankTarget?> Targets = [.. PointBlankTargets.All, null, null];

    public const int OwnCircle = 21, OwnRectangle = 22;

    /// <summary>One line of the list: what the person reads and the zone that is drawn beside it.</summary>
    public sealed record Choice(string Words, PointBlankZone Zone);

    /// <summary>The list's lines, in the order of <see cref="Targets"/>.</summary>
    public static IReadOnlyList<Choice> Choices { get; } = [.. Targets.Select((t, i) => new Choice(
        t is null ? (i == OwnCircle ? "Your own circle" : "Your own rectangle") : t.IsEstimate ? t.Name + ", estimate" : t.Name,
        t?.Zone ?? (i == OwnCircle ? PointBlankZone.Circle(8) : PointBlankZone.Rectangle(8, 12))))];

    /// <summary>
    /// A target in the list drawn to scale beside its name, so a prairie dog's 2 in reads as small as it is beside an elk's 14.5. A template
    /// rather than ready-made items, so the chosen one shows in the box as clearly as in the list.
    /// </summary>
    public static IDataTemplate Template { get; } = new FuncDataTemplate<Choice>((choice, _) =>
    {
        if (choice is null)
        {
            return new TextBlock();
        }

        const double scale = 1.4;
        var zone = choice.Zone;
        Shape shape = zone.Shape == HitShape.Circle
            ? new Ellipse { Width = zone.WidthInches * scale, Height = zone.WidthInches * scale }
            : new Rectangle { Width = zone.WidthInches * scale, Height = zone.HeightInches * scale };
        shape.AttachedToVisualTree += (_, _) => shape.Stroke = new SolidColorBrush(Tokens.For(shape.ActualThemeVariant).Dim);
        shape.StrokeThickness = 1;
        shape.HorizontalAlignment = HorizontalAlignment.Center;
        shape.VerticalAlignment = VerticalAlignment.Center;
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Tokens.Space8,
            Children = { new Border { Width = 46, Height = 46, Child = shape, ClipToBounds = true }, new TextBlock { Text = choice.Words, VerticalAlignment = VerticalAlignment.Center } },
        };
    });
}

/// <summary>
/// The point blank picture: the path's height against distance with the zone's top and bottom, the stretch held dead on shaded amber, and
/// the rifle's present zero dashed beside it, so what the numbers say can be seen at a glance.
/// </summary>
internal sealed class PointBlankGraph : Control
{
    private PointBlankModel? model;
    private PointBlankZone? zone;
    private PointBlankResult? result;

    public void Show(PointBlankModel shown, PointBlankZone of, PointBlankResult with)
    {
        (model, zone, result) = (shown, of, with);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        if (model is null || zone is null || result is null)
        {
            return;
        }

        const double pad = 28;
        double far = Math.Max(100, Math.Min(PointBlankModel.MaxYards, Math.Max(result.Classic.EndYards, result.Ending.EndYards) * 1.25));
        double half = zone.HalfHeight * 2.2;
        double width = Bounds.Width - (2 * pad), height = Bounds.Height - (2 * pad);
        Point At(double yards, double inches) => new(pad + (yards / far * width), pad + ((half - inches) / (2 * half) * height));

        var line = new Pen(new SolidColorBrush(palette.Line2), 1);
        if (result.Band.Exists)
        {
            context.FillRectangle(new SolidColorBrush(palette.AmberTint), new Rect(At(result.Band.StartYards, half), At(result.Ending.EndYards, -half)));
        }

        context.DrawLine(line, At(0, zone.HalfHeight), At(far, zone.HalfHeight));
        context.DrawLine(line, At(0, -zone.HalfHeight), At(far, -zone.HalfHeight));
        context.DrawLine(new Pen(new SolidColorBrush(palette.Faint), 1, new DashStyle([2, 4], 0)), At(0, 0), At(far, 0));

        void Path(double delta, IPen pen)
        {
            var geometry = new StreamGeometry();
            using (var draw = geometry.Open())
            {
                draw.BeginFigure(At(0, Math.Clamp(model.Height(0, delta), -half, half)), false);
                for (double y = 2; y <= far; y += 2)
                {
                    draw.LineTo(At(y, Math.Clamp(model.Height(y, delta), -half, half)));
                }

                draw.EndFigure(false);
            }

            context.DrawGeometry(null, pen, geometry);
        }

        Path(0, new Pen(new SolidColorBrush(palette.Dim), 1.2, new DashStyle([5, 4], 0)));
        Path(result.Band.LaunchDeltaMoa, new Pen(new SolidColorBrush(palette.Amber), 2));

        var text = new SolidColorBrush(palette.Faint);
        for (double y = 0; y <= far; y += far > 600 ? 200 : 100)
        {
            var label = new FormattedText(y.ToString("0", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 10, text);
            context.DrawText(label, At(y, -half) + new Point(-label.Width / 2, 4));
        }
    }
}
