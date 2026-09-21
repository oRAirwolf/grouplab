using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 119 section 4: a rolling test build changes whenever the code does, so the application has to say which one
/// it is, where a tester can read it and select it. A report that says "the latest build" names nothing.
/// </summary>
public class BuildNameTests
{
    [AvaloniaFact]
    public void TheSettingsScreenNamesTheBuildAndItsCommit()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        window.ShowSettings();
        Dispatcher.UIThread.RunJobs();

        // Selectable, because the point of it is being copied into a bug report.
        var lines = window.GetLogicalDescendants().OfType<SelectableTextBlock>().Select(t => t.Text ?? "").ToList();
        string line = Assert.Single(lines, t => t.StartsWith("GroupLab ", StringComparison.Ordinal));
        Assert.Equal(MainWindow.BuildLine(), line);
        Assert.Contains(AppInfo.Channel, line, StringComparison.Ordinal);

        // The version on screen is the assembly's, without the commit the informational version carries after a plus sign, and the commit
        // is beside it in the short form a person can read back.
        Assert.DoesNotContain('+', line);
        if (AppInfo.Commit is { } commit)
        {
            Assert.Contains("commit " + commit, line, StringComparison.Ordinal);
            Assert.Equal(7, commit.Length);
        }
        else
        {
            Assert.Contains("no commit recorded", line, StringComparison.Ordinal);
        }

        window.Close();
    }
}
