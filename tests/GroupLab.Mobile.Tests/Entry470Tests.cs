using System.Text.RegularExpressions;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 470, Alan: the back gesture did not leave Settings, Sync and your data. A button was taken for a way back
/// when its words began with "Back", so "Back up now" was the last one showing and every back made a copy of the records instead.
/// </summary>
public class Entry470Tests
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

    private static T Find<T>(Control root) where T : Control =>
        root.GetLogicalDescendants().OfType<T>().First(c => c.IsEffectivelyVisible);

    private static int Copies() => Phone.Copies?.List().Count ?? 0;

    [AvaloniaFact]
    public void BackOnEveryCategoryPageReturnsToTheListAndMakesNoCopy()
    {
        var shell = Open(out var window);
        try
        {
            foreach (var category in SettingsMenu.All)
            {
                shell.ShowSettings(category);
                Dispatcher.UIThread.RunJobs();
                var view = Find<SettingsView>(shell);
                Assert.Equal(category, view.Open);
                Assert.Equal("settings-back", AutomationProperties.GetAutomationId(shell.WayBack()!));
                var before = Copies();
                Assert.True(shell.Back());
                Dispatcher.UIThread.RunJobs();
                Assert.Null(Find<SettingsView>(shell).Open);
                Assert.Equal(before, Copies());
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void BackFromSyncReturnsToSyncAndYourDataThenTheListThenCapture()
    {
        var shell = Open(out var window);
        try
        {
            shell.ShowSync();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("sync-back", AutomationProperties.GetAutomationId(shell.WayBack()!));
            Assert.True(shell.Back());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(SettingsCategory.Data, Find<SettingsView>(shell).Open);
            Assert.True(shell.Back());
            Dispatcher.UIThread.RunJobs();
            Assert.Null(Find<SettingsView>(shell).Open);
            Assert.True(shell.Back());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Shell.Place.Capture, shell.Showing);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// A way back is said, never guessed: a button made with <c>Screens.Choice</c> or <c>Screens.Primary</c> whose words read as a way back
    /// ("Back", "Back to ...") is one Android's back would miss, and a <c>Screens.BackButton</c> whose words are not one ("Back up now")
    /// is one Android's back would press by mistake.
    /// </summary>
    [Fact]
    public void EveryWayBackIsMarkedAndNothingElseIs()
    {
        var folder = Path.Combine(Repo.Root, "mobile", "GroupLab.Mobile");
        var missed = new List<string>();
        var wrong = new List<string>();
        var wayBack = new Regex(@"^(Back|Back to .+|‹ .+)$");
        var constants = new Dictionary<string, string>
        {
            ["SettingsMenu.BackToList"] = SettingsMenu.BackToList,
            ["FirstRunSteps.Back"] = FirstRunSteps.Back,
            ["CopiesWords.BackUpNow"] = CopiesWords.BackUpNow,
        };
        foreach (var file in Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match call in Regex.Matches(text, @"Screens\.(Choice|Primary|BackButton)\((""(?<words>[^""]*)""|(?<name>[A-Za-z.]+))"))
            {
                var words = call.Groups["words"].Success ? call.Groups["words"].Value : constants.GetValueOrDefault(call.Groups["name"].Value);
                if (words is null)
                {
                    continue;
                }

                var where = $"{Path.GetFileName(file)}: {words}";
                if (call.Groups[1].Value == "BackButton" && !wayBack.IsMatch(words))
                {
                    wrong.Add(where);
                }
                else if (call.Groups[1].Value != "BackButton" && wayBack.IsMatch(words))
                {
                    missed.Add(where);
                }
            }
        }

        Assert.True(wrong.Count == 0, "marked as a way back but is not one: " + string.Join("; ", wrong));
        Assert.True(missed.Count == 0, "a way back Android's back would miss: " + string.Join("; ", missed));
    }
}
