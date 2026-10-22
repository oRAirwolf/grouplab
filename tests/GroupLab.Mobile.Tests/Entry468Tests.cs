using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 468 section 1, concept A's phone boards: Settings opens the list of the eight categories, each with its
/// current choices in one line, and the search box; a tap opens the category's page with "‹ Settings" back; each page can be put back as it
/// came; and the daily copies of the records are on the Sync and your data page.
/// </summary>
public sealed class Entry468Tests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-468-phone-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(folder, recursive: true);
    }

    private static void Start()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }
    }

    private static Control? ById(Control root, string id) =>
        root.GetLogicalDescendants().OfType<Control>().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == id && c.IsEffectivelyVisible);

    private static List<string> Titles(Control root) =>
        [.. root.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(PhoneStyles.Title) && t.IsEffectivelyVisible).Select(t => t.Text ?? "")];

    [AvaloniaFact]
    public void TheListOpensEachPageAndBackComesToTheList()
    {
        Start();
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveUnits(GroupLab.Core.Marking.UnitSettings.Imperial);
        var view = new SettingsView(store);
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["Settings"], Titles(view));
        Assert.NotNull(ById(view, "settings-find"));
        Assert.Equal("MOA · in · yd", view.Summary(SettingsCategory.Units));
        foreach (var category in SettingsMenu.All)
        {
            var row = (Button)ById(view, "settings-" + category.ToString().ToLowerInvariant())!;
            row.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([SettingsMenu.Name(category)], Titles(view));
            Assert.Equal(category, view.Open);
            var back = (Button)ById(view, "settings-back")!;
            Assert.Contains(PhoneStyles.PageBack, back.Classes);
            back.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(["Settings"], Titles(view));
            Assert.Null(view.Open);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void ASearchShowsTheMatchingGroupsInPlace()
    {
        Start();
        var view = new SettingsView(new AppSettingsStore(Path.Combine(folder, "settings.json")));
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Assert.True(view.Find("metric") >= 1);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains("Units and figures", Titles(view));
        Assert.Null(ById(view, "settings-units"));
        Assert.True(view.Find("backup") >= 1);
        Assert.Contains("Sync and your data", Titles(view));
        Assert.NotNull(ById(view, "settings-back-up-now"));
        Assert.Equal(0, view.Find("zzzz nothing"));
        Assert.Contains(view.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == SettingsMenu.NothingFound && t.IsEffectivelyVisible);
        view.Find("");
        Dispatcher.UIThread.RunJobs();
        Assert.NotNull(ById(view, "settings-units"));
        window.Close();
    }

    [AvaloniaFact]
    public void PuttingThePageBackChangesOnlyItsOwnSettings()
    {
        Start();
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        store.SaveCaliberList(GroupLab.Core.Marking.CaliberList.Calibers);
        store.SaveInsideScatter(GroupLab.Core.Marking.InsideScatter.ClicksAnyway);
        store.SaveShowDiagnostics(true);
        var view = new SettingsView(store, SettingsCategory.Units);
        view.PutBack(SettingsCategory.Units);
        Assert.Equal(GroupLab.Core.Marking.CaliberList.Both, store.LoadCaliberList());
        Assert.Equal(GroupLab.Core.Marking.InsideScatter.LeaveIt, store.LoadInsideScatter());
        Assert.True(store.LoadShowDiagnostics());
    }

    [AvaloniaFact]
    public void TheDailyCopiesAreOnTheDataPage()
    {
        Start();
        var view = new SettingsView(Phone.Settings, SettingsCategory.Data);
        var window = new Window { Width = 412, Height = 915, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var keep = (CheckBox)ById(view, "settings-daily-copies")!;
        Assert.Equal(CopiesWords.Keep, keep.Content);
        Assert.Contains(view.GetLogicalDescendants().OfType<TextBlock>(), t => t.Text == CopiesWords.OnThePhone && t.IsEffectivelyVisible);
        Assert.NotNull(ById(view, "settings-back-up-now"));
        Assert.NotNull(ById(view, "settings-restore"));
        window.Close();
    }
}
