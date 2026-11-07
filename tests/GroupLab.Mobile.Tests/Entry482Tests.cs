using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 482 section 2, Alan's 2A for question 112: Settings on the phones has a "Rifles and loads" row between
/// Shooting defaults and Printers and paper that opens Ballistics at the rifle form, "‹ Settings" and the phone's Back return to Settings,
/// "Find a setting" finds it by the words a person would use, and a screen reader reads it as one control with both lines.
/// </summary>
public class Entry482Tests
{
    private static Shell Open(out Window window)
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return shell;
    }

    private static Control? ById(Control root, string id) =>
        root.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id && c.IsEffectivelyVisible);

    private static void Press(Control button)
    {
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void TheRowSitsBetweenShootingDefaultsAndPrintersAndReadsAsOneControl()
    {
        var shell = Open(out var window);
        try
        {
            shell.ShowSettings(SettingsCategory.General);
            Dispatcher.UIThread.RunJobs();
            Press(shell.WayBack()!);
            var view = shell.GetLogicalDescendants().OfType<SettingsView>().First();
            var row = (Button)ById(view, "settings-rifles")!;
            var rows = ((Panel)row.Parent!).Children.Select(AutomationProperties.GetAutomationId).ToList();
            int at = rows.IndexOf("settings-rifles");
            Assert.Equal("settings-shooting", rows[at - 1]);
            Assert.Equal("settings-printers", rows[at + 1]);
            Assert.Equal("Rifles and loads, Kept in Ballistics. Tap to open them there.", AutomationProperties.GetName(row));
            Assert.Contains(row.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == "Open Ballistics");
            Assert.True(row.Bounds.Height >= Screens.Touch, $"the row is {row.Bounds.Height} high");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ItOpensBallisticsAtTheRifleFormAndBackReturnsToSettings()
    {
        var shell = Open(out var window);
        try
        {
            shell.Show(Shell.Place.Settings);
            Dispatcher.UIThread.RunJobs();
            Press(ById(shell, "settings-rifles")!);
            Assert.Equal(Shell.Place.Ballistics, shell.Showing);
            Assert.NotNull(ById(shell, "ballistics-rifle-reading"));
            Assert.Equal("ballistics-back-to-settings", AutomationProperties.GetAutomationId(shell.WayBack()!));
            Assert.True(shell.Back());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Shell.Place.Settings, shell.Showing);
            Assert.Null(shell.GetLogicalDescendants().OfType<SettingsView>().First().Open);

            // From the tab, Ballistics is a place of its own with no way back to Settings, as before.
            shell.Show(Shell.Place.Ballistics);
            Dispatcher.UIThread.RunJobs();
            Assert.Null(ById(shell, "ballistics-back-to-settings"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void FindASettingFindsItByTheWordsAPersonWouldUse()
    {
        var shell = Open(out var window);
        try
        {
            shell.Show(Shell.Place.Settings);
            Dispatcher.UIThread.RunJobs();
            var view = shell.GetLogicalDescendants().OfType<SettingsView>().First();
            foreach (var word in new[] { "rifle", "rifles", "load", "loads", "barrel", "equipment", "Rifles and loads" })
            {
                Assert.True(view.Find(word) >= 1, word);
                Dispatcher.UIThread.RunJobs();
                Assert.NotNull(ById(view, "settings-rifles-found"));
            }

            view.Find("metric");
            Dispatcher.UIThread.RunJobs();
            Assert.Null(ById(view, "settings-rifles-found"));
            view.Find("barrel");
            Dispatcher.UIThread.RunJobs();
            Press(ById(view, "settings-rifles-found")!);
            Assert.Equal(Shell.Place.Ballistics, shell.Showing);
        }
        finally
        {
            window.Close();
        }
    }
}
