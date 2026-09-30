using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 299: Settings' sharing sections show the choice and one short line; the full explanation is behind "More",
/// closed at first and remembered once opened, and "What GroupLab sends" is always in view.
/// </summary>
public class Entry299Tests
{
    private static AppSettingsStore Fresh(out string folder)
    {
        folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry299-{Guid.NewGuid():N}");
        return new AppSettingsStore(Path.Combine(folder, "settings.json"));
    }

    private static void Delete(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [AvaloniaFact]
    public void TheFoldIsClosedAtFirstKeepsEveryWordAndIsRememberedOnceOpened()
    {
        var store = Fresh(out string folder);
        try
        {
            var fold = MoreFold.Make(store, "sending", [new TextBlock { Text = "The whole explanation." }]);
            var toggle = fold.GetLogicalDescendants().OfType<Button>().Single();
            var body = fold.Children[1];
            Assert.Equal(MoreFold.Closed, toggle.Content);
            Assert.False(body.IsVisible);
            Assert.Contains(fold.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "The whole explanation.");

            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(body.IsVisible);
            Assert.Equal(MoreFold.Open, toggle.Content);

            // The next time Settings is drawn, the fold is open as it was left.
            var again = MoreFold.Make(store, "sending", [new TextBlock { Text = "The whole explanation." }]);
            Assert.True(again.Children[1].IsVisible);
            Assert.False(MoreFold.Make(store, "errors", []).Children[1].IsVisible);
        }
        finally
        {
            Delete(folder);
        }
    }

    [AvaloniaFact]
    public void WhatGroupLabSendsIsAlwaysInViewAndOpensItsPage()
    {
        var store = Fresh(out string folder);
        try
        {
            if (Phone.Platform is null)
            {
                Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
            }

            var view = new SettingsView(store);
            var window = new Window { Width = 412, Height = 915, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var link = view.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == SharingWords.WhatIsSentLabel);
            Assert.True(link.IsEffectivelyVisible);
            link.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Contains(((TestPhone)Phone.Platform!).Asked, a => a == ("open", SharingWords.WhatIsSentAddress));
            window.Close();
        }
        finally
        {
            Delete(folder);
        }
    }
}
