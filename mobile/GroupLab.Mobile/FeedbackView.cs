using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.Core.Capture;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260, Feedback B: what GroupLab thought of the picture, before the result. The photograph large with each note's
/// place outlined and numbered on it; beneath it the verdict word, the quality bar with its number and band word, the sentence that leads,
/// the numbered notes, a line of what was fine, and "Take it again" beside "Use this picture". A picture GroupLab cannot measure offers only
/// the retake as its main action; one it can offers using it, since most notes say what GroupLab already corrected.
/// </summary>
internal sealed class FeedbackView : UserControl
{
    /// <param name="turns">Quarter turns clockwise that show the picture upright, the marking's view (entry 281 section 1.7).</param>
    public FeedbackView(PictureVerdict check, string? photo, Action use, Action again, int turns = 0)
    {
        ArgumentNullException.ThrowIfNull(check);
        var column = new StackPanel { Spacing = 12 };
        if (photo is not null && File.Exists(photo))
        {
            column.Children.Add(Picture(new Bitmap(photo), check.Notes, turns));
        }

        var verdict = Screens.Title(check.Verdict);
        column.Children.Add(Screens.Card(verdict, Bar(check.Score), Screens.Line(check.Lead)));
        if (check.Notes.Count > 0)
        {
            var notes = new StackPanel { Spacing = 8 };
            foreach (var note in check.Notes)
            {
                // Entry 282 section 1: the words wrap to the card. A horizontal stack gave them all the width they asked for, so each note ran
                // off the card's edge; a grid gives the badge its own width and the words the rest.
                var words = new TextBlock { Text = note.Words, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(words, 1);
                notes.Children.Add(new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 10, Children = { Badge(note.Number), words } });
            }

            column.Children.Add(Screens.Card(notes));
        }

        column.Children.Add(Screens.Dim("Fine: " + string.Join(", ", check.Fine) + "."));
        var retake = check.CanMeasure ? Screens.Choice("Take it again", again) : Screens.Primary("Take it again", again);
        var keep = check.CanMeasure ? Screens.Primary("Use this picture", use) : Screens.Choice("Use it anyway", use);
        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12, Children = { retake, keep } };
        Grid.SetColumn(keep, 1);
        column.Children.Add(buttons);
        Content = Screens.Page(column);
    }

    /// <summary>The bar: red to 40, amber to 70, green beyond, a mark at the score, and the number and band word beside it.</summary>
    internal static Control Bar(int score)
    {
        var bands = new Grid { ColumnDefinitions = new ColumnDefinitions("40*,30*,30*"), Height = 10 };
        var colours = new[] { Color.FromRgb(208, 69, 58), Color.FromRgb(232, 150, 46), Color.FromRgb(46, 160, 90) };
        for (int i = 0; i < 3; i++)
        {
            var band = new Border
            {
                Background = new SolidColorBrush(colours[i]),
                CornerRadius = i == 0 ? new CornerRadius(5, 0, 0, 5) : i == 2 ? new CornerRadius(0, 5, 5, 0) : default,
            };
            Grid.SetColumn(band, i);
            bands.Children.Add(band);
        }

        int at = Math.Clamp(score, 1, 99);
        var mark = new Ellipse { Width = 18, Height = 18, Fill = Brushes.White, Stroke = new SolidColorBrush(Color.FromRgb(16, 20, 24)), StrokeThickness = 4 };
        var marks = new Grid { ColumnDefinitions = new ColumnDefinitions($"{at}*,Auto,{100 - at}*"), Margin = new Thickness(0, -4, 0, -4) };
        Grid.SetColumn(mark, 1);
        marks.Children.Add(mark);
        var track = new Grid { VerticalAlignment = VerticalAlignment.Center, Children = { bands, marks } };
        var words = new TextBlock // one line on purpose: a score and one word
        {
            Text = $"{score} {PictureCheck.BandWord(PictureCheck.Band(score))}",
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { PhoneStyles.Heading },
        };
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12, Children = { track, words } };
        Grid.SetColumn(words, 1);
        return row;
    }

    /// <summary>The photograph, scaled to fit, with each note's outline and number drawn in its own pixels.</summary>
    private static Control Picture(Bitmap image, IReadOnlyList<PictureNote> notes, int turns)
    {
        double w = image.PixelSize.Width, h = image.PixelSize.Height;
        var canvas = new Canvas { Width = w, Height = h };
        canvas.Children.Add(new Image { Source = image, Width = w, Height = h });
        double stroke = Math.Max(3, Math.Max(w, h) / 300);
        foreach (var note in notes.Where(n => n.Outline is { Count: >= 3 }))
        {
            var points = note.Outline!.Select(p => new Point(p.X, p.Y)).ToList();
            canvas.Children.Add(new Polygon { Points = points, Stroke = new SolidColorBrush(Color.FromRgb(232, 150, 46)), StrokeThickness = stroke });
            var badge = Badge(note.Number, Math.Max(24, Math.Max(w, h) / 40));
            Canvas.SetLeft(badge, points.Min(p => p.X));
            Canvas.SetTop(badge, Math.Max(0, points.Min(p => p.Y) - (Math.Max(24, Math.Max(w, h) / 40) * 1.2)));
            canvas.Children.Add(badge);
        }

        // Turned upright as the picture was taken, and scaled alike across and down.
        var upright = new LayoutTransformControl { LayoutTransform = new RotateTransform(90 * turns), Child = canvas };
        return new Viewbox { Child = upright, Stretch = Stretch.Uniform, MaxHeight = 560 };
    }

    private static Border Badge(int number, double size = 24) => new()
    {
        Width = size,
        Height = size,
        CornerRadius = new CornerRadius(size / 2),
        Background = new SolidColorBrush(Color.FromRgb(232, 150, 46)),
        VerticalAlignment = VerticalAlignment.Top,
        Child = new TextBlock // one line on purpose: a note's number
        {
            Text = number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            FontSize = size * 0.55,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(16, 20, 24)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };
}
