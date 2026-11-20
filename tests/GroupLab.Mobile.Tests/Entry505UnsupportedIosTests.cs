using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 505 section 1: below iOS 26 the person is warned once, before the first run's questions, and goes on only
/// after saying they understand; the acceptance is kept with the version and the warning is shown again only when the version changes,
/// never on 26 or later; Settings, About says it in one line on such a device.
/// </summary>
public class Entry505UnsupportedIosTests
{
    private static TestPhone Started()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        return (TestPhone)Phone.Platform!;
    }

    private static void Settle(Window window)
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static Button Pressable(Control view, string words) =>
        view.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible && b.Content is TextBlock t && t.Text == words);

    private static void Press(Window window, Button button)
    {
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle(window);
    }

    private static bool Says(Control view, string words) =>
        view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsEffectivelyVisible && t.Text == words);

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("", null, false)]
    [InlineData("26.0", null, false)]
    [InlineData("26.1.2", null, false)]
    [InlineData("27.0", "18.6", false)]
    [InlineData("18.6", null, true)]
    [InlineData("18.0", null, true)]
    [InlineData("25.9.1", null, true)]
    [InlineData("18.6", "18.6", false)]
    [InlineData("18.6.2", "18.6", true)]
    [InlineData("18.7", "18.6", true)]
    [InlineData("not a version", null, false)]
    public void TheWarningIsDueOnlyBelow26AndOnlyOnAVersionNotYetAccepted(string? version, string? acceptedOn, bool due) =>
        Assert.Equal(due, UnsupportedIos.Due(version, acceptedOn));

    [Fact]
    public void TheWordsAreAlansWithTheVersionInThem()
    {
        Assert.Equal("iOS 26 is the minimum supported version", UnsupportedIos.Title);
        Assert.StartsWith("This device has iOS 18.6. GroupLab is built and tested for iOS 26 and later.", UnsupportedIos.Says("18.6"), StringComparison.Ordinal);
        Assert.EndsWith("If your device can update, Settings, General, Software Update installs iOS 26 for free.", UnsupportedIos.Says("18.6"), StringComparison.Ordinal);
        Assert.Equal("I understand there is no support and no guarantee on this version.", UnsupportedIos.Understand);
        Assert.Equal("Try GroupLab anyway", UnsupportedIos.TryAnyway);
        Assert.Equal("Not now", UnsupportedIos.NotNow);
        Assert.Equal("Unsupported iOS version (18.6): no support, no guarantee.", UnsupportedIos.AboutLine("18.6"));
    }

    [AvaloniaFact]
    public void TryIsOffUntilTheSwitchIsOnAndNotNowSavesNothing()
    {
        Started();
        int accepted = 0;
        var view = new UnsupportedIosView("18.6", () => accepted++);
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Settle(window);
        try
        {
            Assert.True(Says(view, UnsupportedIos.Title));
            var go = Pressable(view, UnsupportedIos.TryAnyway);
            Assert.False(go.IsEnabled);
            Press(window, go);
            Assert.Equal(0, accepted);

            Press(window, Pressable(view, UnsupportedIos.NotNow));
            Assert.Equal(0, accepted);
            Assert.True(Says(view, UnsupportedIos.NotStartedTitle));
            Assert.False(Says(view, UnsupportedIos.Title));

            Press(window, Pressable(view, UnsupportedIos.ReadAgain));
            var understand = view.GetVisualDescendants().OfType<CheckBox>().Single(c => c.IsEffectivelyVisible);
            Assert.False(understand.IsChecked == true);
            go = Pressable(view, UnsupportedIos.TryAnyway);
            Assert.False(go.IsEnabled);

            understand.IsChecked = true;
            Settle(window);
            Assert.True(go.IsEnabled);
            Press(window, go);
            Assert.Equal(1, accepted);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TheShellWarnsFirstRemembersTheVersionAndAsksAgainOnlyWhenItChanges()
    {
        var phone = Started();
        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        string? before = Phone.Settings.LoadUnsupportedOsAccepted();
        try
        {
            phone.IosVersion = "18.6";
            Phone.Settings.SaveUnsupportedOsAccepted("");
            var (shell, window) = Open();
            Assert.IsType<UnsupportedIosView>(shell.Content);
            var understand = shell.GetVisualDescendants().OfType<CheckBox>().Single(c => c.IsEffectivelyVisible);
            understand.IsChecked = true;
            Settle(window);
            Press(window, Pressable(shell, UnsupportedIos.TryAnyway));
            Assert.IsNotType<UnsupportedIosView>(shell.Content);
            Assert.Equal("18.6", Phone.Settings.LoadUnsupportedOsAccepted());
            window.Close();

            // The same version again: straight in.
            (shell, window) = Open();
            Assert.IsNotType<UnsupportedIosView>(shell.Content);
            window.Close();

            // Updated, still below 26: asked again.
            phone.IosVersion = "18.7";
            (shell, window) = Open();
            Assert.IsType<UnsupportedIosView>(shell.Content);
            window.Close();

            // On 26 or later: never, whatever was accepted.
            phone.IosVersion = "26.1";
            (shell, window) = Open();
            Assert.IsNotType<UnsupportedIosView>(shell.Content);
            window.Close();

            // Android has no iOS version: never.
            phone.IosVersion = null;
            (shell, window) = Open();
            Assert.IsNotType<UnsupportedIosView>(shell.Content);
            window.Close();
        }
        finally
        {
            phone.IosVersion = null;
            Phone.Settings.SaveUnsupportedOsAccepted(before ?? "");
        }

        static (Shell, Window) Open()
        {
            var shell = new Shell();
            var window = new Window { Width = 412, Height = 915, Content = shell };
            window.Show();
            Settle(window);
            return (shell, window);
        }
    }

    [AvaloniaFact]
    public void AboutSaysItInOneLineOnlyBelow26()
    {
        var phone = Started();
        try
        {
            foreach (var (version, shown) in new (string?, bool)[] { ("18.6", true), ("26.0", false), (null, false) })
            {
                phone.IosVersion = version;
                var view = new SettingsView(Phone.Settings, GroupLab.App.SettingsCategory.Updates);
                var window = new Window { Width = 412, Height = 915, Content = view };
                window.Show();
                Settle(window);
                bool found = view.GetLogicalDescendants().OfType<TextBlock>().Any(t => t.Text == UnsupportedIos.AboutLine("18.6"));
                Assert.Equal(shown, found);
                window.Close();
            }
        }
        finally
        {
            phone.IosVersion = null;
        }
    }
}
