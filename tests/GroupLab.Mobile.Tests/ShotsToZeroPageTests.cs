using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// The build 134 error report from an iPhone: a second calculation on Shots Needed to Zero put the seed box, still held by the first
/// answer's row, into a new row, and the page threw. The same held for the typed click value when the chips rebuilt the card.
/// </summary>
public class ShotsToZeroPageTests
{
    private static readonly PointD[] Group = [new(0.3, 0.1), new(0.1, -0.2), new(0.5, 0.2), new(0.2, 0.3), new(0.4, -0.1), new(0.25, 0.05)];

    private static Button? Find(Control root, string words) =>
        root.GetLogicalDescendants().OfType<Button>().FirstOrDefault(b => b.Content as string == words || b.Content is TextBlock t && t.Text == words);

    /// <summary>Runs the interface thread until the answer's Calculate again is on screen, as the work finishes off it.</summary>
    private static Button Answered(Control page)
    {
        for (int i = 0; i < 600; i++)
        {
            Dispatcher.UIThread.RunJobs();
            if (Find(page, "Calculate again") is { } again && page.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == "Within 1 click"))
            {
                return again;
            }

            Thread.Sleep(50);
        }

        throw new TimeoutException("Shots Needed to Zero did not answer.");
    }

    [AvaloniaFact]
    public void CalculatingTwiceKeepsOneSeedBox()
    {
        var state = ShotCsv.Marking([.. Group], 3600);
        var page = new ShotsToZeroPage(state, UnitSettings.Imperial, () => { });
        var window = new Window { Width = 412, Height = 915, Content = page };
        window.Show();

        Answered(page).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Answered(page).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Answered(page);

        Assert.Single(page.GetLogicalDescendants().OfType<TextBox>(), t => t.Text == "41");

        // The typed click value survives the chips rebuilding the card, twice.
        Find(page, "Other")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Find(page, "mil")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Find(page, "MOA")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
        Assert.Single(page.GetLogicalDescendants().OfType<TextBox>(), t => t.PlaceholderText == "click, e.g. 0.2");
        window.Close();
    }
}
