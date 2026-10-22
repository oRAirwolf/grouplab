using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468, Alan: Settings "is very cluttered". Concept A: a side menu of eight categories with a search box, one
/// page at a time, "Put this page back as it came" on each, and the daily copies of the records first among the new settings.
/// </summary>
public sealed class Entry468Tests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-468-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(folder, recursive: true);
    }

    private MainWindow Window(string name = "settings.json")
    {
        var window = new MainWindow(new AppSettingsStore(Path.Combine(folder, name))) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static bool Shown(Control c) => c.IsEffectivelyVisible;

    private static List<string> Titles(MainWindow window) =>
        [.. window.SettingsBody.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(Theme.AppStyles.Title) && Shown(t)).Select(t => t.Text ?? "")];

    [AvaloniaFact]
    public void EachCategoryIsItsOwnPageAndTheLastShownIsRemembered()
    {
        var window = Window();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SettingsCategory.General, window.SettingsPage);
        Assert.Equal(["General"], Titles(window));
        foreach (var category in SettingsMenu.All)
        {
            window.ShowSettings(category);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([SettingsMenu.Name(category)], Titles(window));
        }

        window.ShowSettings(SettingsCategory.Printers);
        window.Close();

        var again = Window();
        again.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(SettingsCategory.Printers, again.SettingsPage);
        Assert.Equal(["Printers and paper"], Titles(again));
        again.Close();
    }

    [AvaloniaFact]
    public void UpAndDownInTheSideMenuMoveFromPageToPage()
    {
        var window = Window();
        window.ShowSettings(SettingsCategory.Units);
        Dispatcher.UIThread.RunJobs();
        var menu = window.SettingsBody.GetLogicalDescendants().OfType<Button>().Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == "Units and figures");
        // The key goes to the side menu's row for the page showing, where focus is after a move.
        void Press(Key key)
        {
            var on = window.SettingsBody.GetLogicalDescendants().OfType<Button>().Single(b => Avalonia.Automation.AutomationProperties.GetName(b) == SettingsMenu.Name(window.SettingsPage));
            on.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = on });
            Dispatcher.UIThread.RunJobs();
        }

        Assert.True(menu.Focusable && KeyboardNavigation.GetIsTabStop(menu));
        Press(Key.Down);
        Assert.Equal(SettingsCategory.Shooting, window.SettingsPage);
        Press(Key.Up);
        Press(Key.Up);
        Assert.Equal(SettingsCategory.General, window.SettingsPage);
        window.Close();
    }

    [AvaloniaFact]
    public void ASearchShowsTheMatchingRowsOfEveryPageAndNothingElse()
    {
        var window = Window();
        window.ShowSettings(SettingsCategory.General);
        Dispatcher.UIThread.RunJobs();

        // "metric" is none of the units' names but finds all three, on their own page, under its title.
        Assert.Equal(3, window.FindSetting("metric"));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["Units and figures"], Titles(window));

        Assert.True(window.FindSetting("dark") >= 1);
        Assert.Contains("General", Titles(window));
        Assert.True(window.FindSetting("backup") >= 1);
        Assert.Contains("Sync and your data", Titles(window));

        Assert.Equal(0, window.FindSetting("zzzz nothing"));
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(Titles(window));
        Assert.Contains(window.SettingsBody.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == SettingsMenu.NothingFound && Shown(t));

        // Clearing the search shows the page that was showing.
        window.FindSetting("");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["General"], Titles(window));
        window.Close();
    }

    [AvaloniaFact]
    public void PuttingAPageBackChangesOnlyThatPage()
    {
        var window = Window();
        var store = window.SettingsStore;
        window.ShowSettings(SettingsCategory.Units);
        Dispatcher.UIThread.RunJobs();
        window.GetLogicalDescendants().OfType<CheckBox>().Single(c => Avalonia.Automation.AutomationProperties.GetName(c) == "Size on the paper first").IsChecked = true;
        window.GetLogicalDescendants().OfType<RadioButton>().First(r => r.GroupName == "caliberList").IsChecked = true;
        window.GetLogicalDescendants().OfType<RadioButton>().Last(r => r.GroupName == "insideScatter").IsChecked = true;
        window.SetTheme(ThemeChoice.Dark);
        Dispatcher.UIThread.RunJobs();
        Assert.True(store.LoadSizeOnPaperFirst());
        Assert.Equal(CaliberList.Calibers, store.LoadCaliberList());
        Assert.Equal(InsideScatter.ClicksAnyway, store.LoadInsideScatter());

        window.PutSettingsPageBack(SettingsCategory.Units);
        Dispatcher.UIThread.RunJobs();
        Assert.False(store.LoadSizeOnPaperFirst());
        Assert.Equal(CaliberList.Both, store.LoadCaliberList());
        Assert.Equal(InsideScatter.LeaveIt, store.LoadInsideScatter());
        Assert.Equal(UnitSettings.ForRegion(AppSettingsStore.Region()), store.LoadUnits());
        // The General page's theme is not this page's.
        Assert.Equal(ThemeChoice.Dark, store.LoadTheme());
        window.SetTheme(ThemeChoice.System);
        window.Close();
    }

    [AvaloniaFact]
    public async Task TheRecordsAreCopiedAtTheFirstOpenAndCanBeRestored()
    {
        var first = Window();
        await first.Copying;
        Dispatcher.UIThread.RunJobs();
        var copies = first.RecordsCopies!;
        Assert.Single(copies.List());
        Assert.StartsWith("The last copy was made on ", first.CopiesLine, StringComparison.Ordinal);
        Assert.True(first.SettingsStore.LoadDailyCopies());
        int before = first.Sessions!.List().Count;
        first.Close();

        // A second open the same day makes none.
        var second = Window();
        await second.Copying;
        Assert.Single(second.RecordsCopies!.List());

        // Back up now makes one at once; a session saved afterwards goes when that copy is restored.
        Thread.Sleep(1100);
        second.BackUpNow();
        var copy = second.RecordsCopies!.List(countSessions: true)[0];
        Assert.Equal(before, copy.Sessions);
        second.Sessions!.Save(new SessionRecord(0, "2026-10-22T07:40:00Z", "2026-10-22", "Test sheet", null, null, 3600, null, null, null, 0.264,
            "{\"marking\":1}", 5, 0.5, null, null, null, null, null, null));
        Assert.Equal(before + 1, second.Sessions.List().Count);
        Thread.Sleep(1100);
        Assert.True(second.RestoreCopy(copy));
        Assert.Equal(before, second.Sessions.List().Count);
        Assert.Equal(3, second.RecordsCopies.List().Count);

        // Turned off, nothing is made, and the line says so.
        second.SettingsStore.SaveDailyCopies(false);
        second.Close();
        var third = Window();
        Assert.StartsWith("Daily copies are off.", third.CopiesLine, StringComparison.Ordinal);
        third.Close();
    }
}
