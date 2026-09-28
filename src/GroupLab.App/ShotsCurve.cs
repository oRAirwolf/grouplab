using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>The chance against shots, both axes, for the closest click and for within one click, on a scale of shots from 1 to 1,000.</summary>
internal sealed class ShotsCurve : Control
{
    public IReadOnlyList<(int Shots, double ClosestClick, double WithinOneClick)> Points { get; set; } = [];

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var palette = Tokens.For(ActualThemeVariant);
        context.FillRectangle(new SolidColorBrush(palette.Sunk), new Rect(Bounds.Size));
        if (Points.Count < 2)
        {
            return;
        }

        double left = 44, right = Bounds.Width - 12, top = 26, bottom = Bounds.Height - 22;
        double X(int shots) => left + ((right - left) * Math.Log10(shots) / 3);
        double Y(double p) => bottom - ((bottom - top) * p);
        var dim = new SolidColorBrush(palette.Dim);
        foreach (double p in new[] { 0.5, 0.9, 1.0 })
        {
            context.DrawLine(new Pen(new SolidColorBrush(palette.Line), 1), new Point(left, Y(p)), new Point(right, Y(p)));
            if (p < 1)
            {
                context.DrawText(new FormattedText($"{100 * p:0}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, dim), new Point(4, Y(p) - 8));
            }
        }

        foreach (int n in new[] { 1, 10, 100, 1000 })
        {
            context.DrawText(new FormattedText(n.ToString("N0", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Mono), Tokens.DetailSize, dim), new Point(X(n) - 8, bottom + 4));
        }

        void Curve(Func<(int Shots, double ClosestClick, double WithinOneClick), double> value, Color colour)
        {
            var pen = new Pen(new SolidColorBrush(colour), 2);
            for (int i = 1; i < Points.Count; i++)
            {
                context.DrawLine(pen, new Point(X(Points[i - 1].Shots), Y(value(Points[i - 1]))), new Point(X(Points[i].Shots), Y(value(Points[i]))));
            }
        }

        Curve(p => p.WithinOneClick, palette.Teal);
        Curve(p => p.ClosestClick, palette.Amber);
        context.DrawText(new FormattedText("amber: closest click    teal: within 1 click", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Tokens.Sans), Tokens.DetailSize, dim), new Point(left, 4));
    }
}
