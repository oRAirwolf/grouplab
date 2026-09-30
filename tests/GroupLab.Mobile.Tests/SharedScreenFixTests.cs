using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// Two faults the iOS self-test found in the shared phone screens, which Android shows the same way: a figure's tile cut its words off
/// mid-word ("not quoted be") where a mean radius is withheld for few shots, and the radio circles of Settings sat in the top-left corner of
/// their cards instead of beside their words.
/// </summary>
public class SharedScreenFixTests
{
    private static Window Show(Control content, double width = 320)
    {
        var window = new Window { Width = width, Height = 900, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void ATilesWordsWrapRatherThanBeingCutOffAt320Wide()
    {
        var tiles = Screens.Tiles([("Mean radius", "not quoted below 5 shots", "3 shots", true), ("Extreme spread", "0.412 in", "", false)]);
        var window = Show(new StackPanel { Children = { tiles } });
        foreach (var text in tiles.GetVisualDescendants().OfType<TextBlock>())
        {
            var natural = new TextBlock { Text = text.Text, FontSize = text.FontSize, FontFamily = text.FontFamily, FontWeight = text.FontWeight };
            natural.Measure(Size.Infinity);
            Assert.True(natural.DesiredSize.Width <= text.Bounds.Width + 0.5 || text.TextWrapping == TextWrapping.Wrap,
                $"\"{text.Text}\" needs {natural.DesiredSize.Width:0} and is given {text.Bounds.Width:0} on one line, so it is cut off");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void ARadioCircleSitsBesideItsWordsInsideItsCard()
    {
        var radio = Screens.Radio("test", "Send every target automatically", true);
        var window = Show(new StackPanel { Children = { radio } });
        var circle = radio.GetVisualDescendants().OfType<Ellipse>().OrderByDescending(e => e.Bounds.Width).First();
        var words = radio.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Send every target automatically");
        var c = circle.TranslatePoint(new Point(circle.Bounds.Width / 2, circle.Bounds.Height / 2), radio)!.Value;
        var w = words.TranslatePoint(new Point(0, words.Bounds.Height / 2), radio)!.Value;
        Assert.True(c.X >= 12, $"the circle's center is {c.X:0} from the card's left edge");
        Assert.True(Math.Abs(c.Y - w.Y) <= 6, $"the circle's center is at {c.Y:0} and the words' middle at {w.Y:0}");
        window.Close();
    }
}
