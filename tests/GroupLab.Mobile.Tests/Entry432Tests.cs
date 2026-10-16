using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 432: since entry 376 item B3 the caliber is asked for every new target and emptied after each result.
/// The question sits on the Capture start, so a second picture shared or opened into GroupLab while the first one's result was showing
/// raised it where nobody could see it, and the picture was never read; the iOS self-test found it. The start now comes back with it.
/// </summary>
public class Entry432Tests
{
    private static readonly TestPhone ThePhone = new();

    [AvaloniaFact]
    public void TheCaliberQuestionIsOnScreenWhenAResultWasShowing()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        var (was, wasInches) = Phone.Settings.LoadShotSetup();
        Phone.Settings.SaveShotSetup("", null);
        var window = new Window { Width = 412, Height = 915 };
        try
        {
            var page = new CapturePage();
            window.Content = page;
            window.Show();
            page.ShowResult(new TextBlock { Text = "the first picture's result" });
            Dispatcher.UIThread.RunJobs();

            bool went = false;
            page.AskFirst(() => went = true);
            page.UpdateLayout();
            Dispatcher.UIThread.RunJobs();

            Assert.True(page.Asking);
            Assert.False(went);
            static string Words(Button b) => b.Content as string ?? (b.Content as TextBlock)?.Text ?? "";
            var go = page.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => Words(b) == "Continue");
            Assert.NotNull(go);
            Assert.True(go.IsEffectivelyVisible, "Continue is in the page but not on screen");

            page.TypedCalibre = ".308";
            go.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Assert.False(page.Asking);
            Assert.True(went);
        }
        finally
        {
            window.Close();
            Phone.Settings.SaveShotSetup(was, wasInches);
        }
    }
}
