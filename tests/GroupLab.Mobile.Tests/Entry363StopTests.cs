using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 3.4, the phone half of entry 356 section 3: a failure that stops the work is the centred sheet
/// over the page, with its choices, and dismissing it puts the page back as it was.
/// </summary>
public class Entry363StopTests
{
    [AvaloniaFact]
    public void AFailureIsASheetOverThePageAndDismissingItPutsThePageBack()
    {
        var line = new TextBlock();
        var content = new StackPanel { Children = { line } };
        var page = new UserControl { Content = content };
        var window = new Window { Width = 390, Height = 844, Content = page };
        window.Show();
        bool again = false;

        ProblemSheet.Stop(line, line, "The report could not be made", "The disk is full.", ("Try again", () => again = true));
        Dispatcher.UIThread.RunJobs();
        var texts = page.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
        Assert.Contains("The report could not be made", texts);
        Assert.Contains("The disk is full.", texts);
        Assert.Equal("", line.Text);

        var dismiss = page.GetLogicalDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == GroupLab.Core.Registration.OpeningWords.Dismiss);
        dismiss.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(content, page.Content);
        Assert.False(again);

        ProblemSheet.Stop(line, line, "The report could not be made", "The disk is full.", ("Try again", () => again = true));
        var first = page.GetLogicalDescendants().OfType<Button>().First(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Try again");
        first.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(again);
        Assert.Same(content, page.Content);
        window.Close();
    }

    [AvaloniaFact]
    public void AFailureOnNoPageYetIsSaidOnItsLine()
    {
        var line = new TextBlock();
        ProblemSheet.Stop(line, line, "Title", "Why.");
        Assert.Equal("Why.", line.Text);
    }
}
