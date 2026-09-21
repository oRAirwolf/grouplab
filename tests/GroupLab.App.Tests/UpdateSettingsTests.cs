using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 119 section 6: the settings page says what this build is, which train it follows, how often it looks, and
/// what a check sends. Nothing here touches the network: what is tested is what the screen offers and what it says.
/// </summary>
public class UpdateSettingsTests
{
    private static MainWindow Settings()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void TheSettingsPageNamesTheBuildAndItsTrain()
    {
        var window = Settings();
        var headings = window.GetLogicalDescendants().OfType<TextBlock>()
            .Where(t => t.Classes.Contains(AppStyles.Section) && t.IsVisible).Select(t => t.Text).ToList();
        Assert.Contains("This build", headings);
        Assert.Contains("Updates", headings);

        // The three trains are offered, and the two that have no builds say so rather than looking choosable.
        var trains = window.GetLogicalDescendants().OfType<ComboBox>()
            .Select(c => c.ItemsSource?.Cast<object>().Select(i => i?.ToString() ?? "").ToList())
            .FirstOrDefault(items => items is not null && items.Any(i => i.StartsWith("Nightly", StringComparison.Ordinal)));
        Assert.NotNull(trains);
        Assert.Contains("Nightly", trains!);
        Assert.Contains(trains!, t => t.StartsWith("Release", StringComparison.Ordinal) && t.Contains("not available yet", StringComparison.Ordinal));
        Assert.Contains(trains!, t => t.StartsWith("Beta", StringComparison.Ordinal) && t.Contains("not available yet", StringComparison.Ordinal));

        // And how often to look, with every choice entry 119 section 4.2 names.
        var intervals = window.GetLogicalDescendants().OfType<ComboBox>()
            .Select(c => c.ItemsSource?.Cast<object>().Select(i => i?.ToString() ?? "").ToList())
            .FirstOrDefault(items => items is not null && items.Contains("On every launch"));
        Assert.NotNull(intervals);
        Assert.Equal(["On every launch", "Once a day", "Once a week", "Never, only when I ask"], intervals!);

        window.Close();
    }

    [AvaloniaFact]
    public void ChoosingATrainThatHasNoBuildsChangesNothingAndSaysWhy()
    {
        var window = Settings();
        var trains = window.GetLogicalDescendants().OfType<ComboBox>()
            .First(c => c.ItemsSource?.Cast<object>().Any(i => (i?.ToString() ?? "").StartsWith("Nightly", StringComparison.Ordinal)) == true);

        var before = window.UpdatePreferencesNow.Train;
        trains.SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(UpdateTrain.Release);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(before, window.UpdatePreferencesNow.Train);
        Assert.Contains("not available yet", window.UpdateStateText, StringComparison.OrdinalIgnoreCase);
        window.Close();
    }

    [AvaloniaFact]
    public void CheckNowOnABuildWithNoKeySaysSoRatherThanPretending()
    {
        var window = Settings();
        window.CheckForUpdates();
        Dispatcher.UIThread.RunJobs();

        // A development build has nothing to update to; a published build with no key compiled in refuses every update.
        string said = window.UpdateStateText;
        Assert.False(string.IsNullOrWhiteSpace(said));
        if (AppInfo.Build.IsDevelopment)
        {
            Assert.Contains("development build", said, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains(UpdateKeys.Trusts ? "Looking" : "no update key", said, StringComparison.Ordinal);
        }

        // Whatever it said, it recorded when it looked.
        Assert.NotNull(window.UpdatePreferencesNow.LastCheckUtc);
        window.Close();
    }

    [AvaloniaFact]
    public void HowOftenToLookIsWhatWasChosen()
    {
        var window = Settings();
        var intervals = window.GetLogicalDescendants().OfType<ComboBox>()
            .First(c => c.ItemsSource?.Cast<object>().Any(i => (i?.ToString() ?? "") == "Once a week") == true);
        intervals.SelectedIndex = UpdateCheckIntervals.All.ToList().IndexOf(UpdateCheckInterval.Weekly);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(UpdateCheckInterval.Weekly, window.UpdatePreferencesNow.Interval);
        Assert.Contains("once a week", window.UpdateStateText, StringComparison.OrdinalIgnoreCase);
        window.Close();
    }
}
