using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 376 item B1, Alan: Android's back returns to the previous screen wherever you are, and never closes the
/// application from an inner screen. Back presses a sheet's × first, then the page's own Back.
/// </summary>
public class BackEverywhereTests
{
    [AvaloniaFact]
    public void BackPressesThePagesOwnWayBackAndThenASheetsCrossFirst()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        try
        {
            shell.Show(Shell.Place.Settings);
            Dispatcher.UIThread.RunJobs();

            // An inner page with its own way back, as Shots, Report and the rest have.
            bool wentBack = false;
            var inner = Screens.Page(new StackPanel { Children = { Screens.Title("Inner"), Screens.Choice("Back to the result", () => wentBack = true) } });
            var host = new ContentControl { Content = inner };

            var page = new UserControl { Content = host };
            var frame = new Panel { Children = { page } };
            window.Content = frame;
            Dispatcher.UIThread.RunJobs();
            var way = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(frame).OfType<Button>().Single(b => b.Classes.Contains(PhoneStyles.PageBack));
            way.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.True(wentBack);

            // A sheet over a page: its × is what back presses.
            bool dismissed = false;
            var sheet = ProblemSheet.Over(new TextBlock { Text = "under" }, "A question", new TextBlock { Text = "words" }, [], () => dismissed = true);
            window.Content = sheet;
            Dispatcher.UIThread.RunJobs();
            var cross = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(sheet).OfType<Button>().Single(b => b.Classes.Contains(PhoneStyles.SheetDismiss));
            cross.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.True(dismissed);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OnAnInnerPageOfATabBackStaysInTheApplication()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        try
        {
            shell.Show(Shell.Place.Ballistics);
            Dispatcher.UIThread.RunJobs();
            Assert.True(shell.Back(), "back on a tab other than Capture closed the application");
        }
        finally
        {
            window.Close();
        }
    }
}
