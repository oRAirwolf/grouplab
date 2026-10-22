using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Tests.Support;

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

    private string Picture()
    {
        string path = Path.Combine(folder, $"sheet-{Guid.NewGuid():N}.png");
        using var image = new OpenCvSharp.Mat(600, 800, OpenCvSharp.MatType.CV_8UC3, new OpenCvSharp.Scalar(235, 235, 235));
        OpenCvSharp.Cv2.Circle(image, new OpenCvSharp.Point(200, 200), 9, new OpenCvSharp.Scalar(30, 30, 30), -1);
        OpenCvSharp.Cv2.ImWrite(path, image);
        return path;
    }

    [AvaloniaFact]
    public void ANewTargetStartsWithTheShootingDefaults()
    {
        var store = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        var window = Window();

        // As before, until chosen: no distance, rounds fired asked for, no rifle or load.
        window.OpenImage(Picture());
        Dispatcher.UIThread.RunJobs();
        Assert.Null(window.Session.State.ShotDistanceInches);
        window.Session.SetEquipment(null, null, "Test load");
        window.Session.SetShotDistance(3600);
        window.Session.AddShot(new GroupLab.Core.Imaging.PointD(200, 200));

        store.SaveNewTargetDistance(1800);
        store.SaveNewTargetRounds(RoundsStart.NotKnown);
        store.SaveNewTargetEquipment(true);
        window.OpenImage(Picture());
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1800, window.Session.State.ShotDistanceInches);
        Assert.Equal("Test load", window.Session.State.Load);
        window.Session.AddShot(new GroupLab.Core.Imaging.PointD(200, 200));
        Assert.DoesNotContain("rounds fired", window.StillNeeded());

        // Put back as it came, the next target starts as before.
        window.PutSettingsPageBack(SettingsCategory.Shooting);
        Assert.Null(store.LoadNewTargetDistance());
        Assert.Equal(RoundsStart.Ask, store.LoadNewTargetRounds());
        Assert.False(store.LoadNewTargetEquipment());
        window.OpenImage(Picture());
        Dispatcher.UIThread.RunJobs();
        Assert.Null(window.Session.State.ShotDistanceInches);
        Assert.Null(window.Session.State.Load);
        window.Close();
    }

    [AvaloniaFact]
    public void ThePaperIsOneSettingOnTwoPages()
    {
        var window = Window();
        var store = window.SettingsStore;
        Assert.Null(store.LoadPaperLetter());
        Assert.Equal(AppSettingsStore.LetterRegion(AppSettingsStore.Region()), AppSettingsStore.LetterPaper());
        window.ShowSettings(SettingsCategory.Printers);
        Dispatcher.UIThread.RunJobs();
        var papers = window.SettingsBody.GetLogicalDescendants().OfType<ComboBox>().Where(c => Avalonia.Automation.AutomationProperties.GetName(c) == DefaultsWords.Paper).ToList();
        Assert.Equal(2, papers.Count);
        int other = 1 - papers[0].SelectedIndex;
        papers[0].SelectedIndex = other;
        Assert.Equal(other == 0, store.LoadPaperLetter());
        Assert.Equal(other == 0, AppSettingsStore.LetterPaper());
        Assert.Equal(other, papers[1].SelectedIndex);
        window.PutSettingsPageBack(SettingsCategory.Printers);
        Assert.Null(store.LoadPaperLetter());
        window.Close();
    }

    [AvaloniaFact]
    public void EveryFigureOpensTheFoldedOnes()
    {
        var (window, path) = AnalysisStateTests.Marked();
        try
        {
            window.SettingsStore.SaveEveryFigure(true);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.AdvancedPanel.IsExpanded);
            Assert.True(window.FullFiguresPanel.IsExpanded);
        }
        finally
        {
            window.SettingsStore.SaveEveryFigure(false);
            window.Close();
            File.Delete(path);
        }
    }

    [AvaloniaFact]
    public void ReduceMotionAsChosenOverridesTheSystem()
    {
        var window = Window();
        var was = Landing.ReducedMotion;
        try
        {
            Landing.ReducedMotion = () => false;
            Assert.False(Landing.Still());
            window.SettingsStore.SaveReduceMotion(MotionChoice.On);
            Assert.True(Landing.Still());
            Landing.ReducedMotion = () => true;
            window.SettingsStore.SaveReduceMotion(MotionChoice.Off);
            Assert.False(Landing.Still());
            window.SettingsStore.SaveReduceMotion(MotionChoice.System);
            Assert.True(Landing.Still());
        }
        finally
        {
            Landing.ReducedMotion = was;
            window.Close();
        }
    }

    /// <summary>Entry 468 section 2.6: the layout audit at all three text sizes, on every screen and every Settings page; nothing cut off.</summary>
    [AvaloniaFact]
    public void NothingIsCutOffAtAnyTextSize()
    {
        var found = new List<string>();
        foreach (var size in Enum.GetValues<TextSizeChoice>())
        {
            var (window, path) = AnalysisStateTests.Marked();
            try
            {
                window.SettingsStore.SaveTextSize(size);
                Theme.TextSize.Choose(size);
                window.CalibreAnswered();
                window.Analyse();
                Dispatcher.UIThread.RunJobs();
                found.AddRange(LayoutAudit.Findings(window).Select(f => $"{size}, analysis: {f}"));
                foreach (var place in new[] { Destination.Sessions, Destination.Library, Destination.Ballistics, Destination.Compare })
                {
                    window.Go(place);
                    Dispatcher.UIThread.RunJobs();
                    found.AddRange(LayoutAudit.Findings(window).Select(f => $"{size}, {place}: {f}"));
                }

                foreach (var category in SettingsMenu.All)
                {
                    window.ShowSettings(category);
                    Dispatcher.UIThread.RunJobs();
                    found.AddRange(LayoutAudit.Findings(window).Select(f => $"{size}, settings {category}: {f}"));
                }
            }
            finally
            {
                Theme.TextSize.Choose(TextSizeChoice.Normal);
                window.Close();
                File.Delete(path);
            }
        }

        Assert.True(found.Count == 0, string.Join(Environment.NewLine, found));
    }
}
