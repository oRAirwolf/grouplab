using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A4: what a photograph came to, on the phone. The group's figures in the person's own units, the
/// photograph with every hole GroupLab found, and the desktop's composite plot, filled the same way from the same marking. Where the sheet
/// could not be read, the reason and what to do next, never a blank screen.
/// </summary>
public sealed class ResultView : UserControl
{
    internal ResultView(PhoneResult result, UnitSettings units, Action again)
    {
        var column = new StackPanel { Spacing = 12 };
        var state = result.State;
        column.Children.Add(Screens.Heading(result.Definition?.Name ?? "The sheet"));
        if (result.Failure is { } failure)
        {
            column.Children.Add(Screens.Line(failure));
            column.Children.Add(Screens.Choice("Try another picture", again));
            Content = Screens.Page(column);
            return;
        }

        var labels = ShotLabels.For(state);
        string Label(int id) => labels.FirstOrDefault(l => l.ShotId == id) is { Text: { } text } ? text : id.ToString(CultureInfo.InvariantCulture);
        string Bull(int index) => state.Bulls.FirstOrDefault(b => b.Index == index)?.Label ?? index.ToString(CultureInfo.InvariantCulture);
        var report = GroupAnalysis.Analyse(state);
        column.Children.Add(Screens.Line(Figures(report.AllShots, units)));

        var plot = new CompositePlot { Height = 360, HorizontalAlignment = HorizontalAlignment.Stretch };
        plot.Show(state, result.Definition, units, Label, Bull);
        column.Children.Add(plot);

        if (state.ImagePath is { } path && File.Exists(path))
        {
            column.Children.Add(new LayoutTransformControl
            {
                LayoutTransform = new RotateTransform(90 * state.ViewQuarterTurns),
                Child = new SheetPreview(new Bitmap(path), [.. state.Shots.Where(s => s.IsShot).Select(s => s.Image)]),
            });
        }

        column.Children.Add(Screens.Line(result.SessionId is null
            ? "This session could not be saved on the phone."
            : "Saved in Sessions."));
        column.Children.Add(Screens.Choice("Another target", again));
        Content = Screens.Page(column);
    }

    /// <summary>The group in one or two sentences: how many shots, the extreme spread and the mean radius, where there are enough shots.</summary>
    internal static string Figures(GroupFigures? figures, UnitSettings units)
    {
        if (figures is null || figures.Shots == 0)
        {
            return "No holes were found on this sheet. Check the picture shows the holes clearly, or take it again closer.";
        }

        string said = figures.Shots == 1 ? "1 shot." : $"{figures.Shots} shots.";
        if (figures.ExtremeSpread is { } spread)
        {
            said += $" Extreme spread {units.Length(spread.Value)}, center to center.";
        }

        if (figures.MeanRadius is { } radius)
        {
            said += $" Mean radius {units.Length(radius.Value)}.";
        }

        return said;
    }

    /// <summary>The working image, fitted to the width, with a ring on every hole found.</summary>
    private sealed class SheetPreview(Bitmap image, IReadOnlyList<PointD> holes) : Control
    {
        protected override Size MeasureOverride(Size available)
        {
            double width = double.IsFinite(available.Width) ? available.Width : image.Size.Width;
            return new Size(width, width * image.Size.Height / image.Size.Width);
        }

        public override void Render(DrawingContext context)
        {
            double scale = Bounds.Width / image.Size.Width;
            context.DrawImage(image, new Rect(0, 0, Bounds.Width, Bounds.Height));
            var pen = new Pen(Brushes.OrangeRed, 2);
            foreach (var hole in holes)
            {
                context.DrawEllipse(null, pen, new Point(hole.X * scale, hole.Y * scale), 9, 9);
            }
        }
    }
}
