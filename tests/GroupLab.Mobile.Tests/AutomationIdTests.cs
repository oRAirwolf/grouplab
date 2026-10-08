using System.Text.RegularExpressions;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 1: the controls a developer's script presses, types in or turns on keep a stable automation id,
/// so a script written today still finds them after their words change. None may go missing, and no two controls may share one.
/// </summary>
public partial class AutomationIdTests
{
    /// <summary>The ids that matter, each in the file that sets it. A script and <c>docs/ANDROID.md</c> name them.</summary>
    private static readonly (string File, string[] Ids)[] Kept =
    [
        ("CapturePage.cs", ["capture-take-picture", "capture-choose-photo", "capture-print-target", "capture-change", "capture-caliber", "capture-distance",
            "capture-ask-continue", "capture-ask-cancel", "capture-show-camera", "capture-show-result"]),
        ("ResultView.cs", ["result-fix-holes", "result-ballistics", "result-shots", "result-zero", "result-share-picture", "result-report",
            "result-share-session", "result-share-csv", "result-another-target", "result-as-likely-sheet", "result-mark-by-hand", "result-open-targets"]),
        ("OpenTargetsSheet.cs", ["open-targets-another", "open-targets-close-confirm", "open-targets-keep"]),
        ("FixHolesPage.cs", ["fix-main", "fix-move", "fix-remove", "fix-undo", "fix-done", "fix-keep", "fix-throw-away", "fix-go-on", "fix-back"]),
        ("SessionsPage.cs", ["sessions-open-file", "sessions-import-csv", "sessions-compare-loads", "sessions-back"]),
        ("PhotoPages.cs", ["photo-read-anyway", "photo-choose-another"]),
        ("MarkingAPage.cs", ["marking-find-holes", "marking-open-targets"]),
        ("SettingsView.cs", ["settings-send-diagnostics", "settings-add-printer", "settings-printer-correction", "settings-keep-pictures", "settings-dev-bridge"]),
    ];

    [GeneratedRegex("""\.Id\("([a-z0-9-]+)"\)""")]
    private static partial Regex Given();

    [Fact]
    public void EveryControlThatMattersKeepsItsIdAndNoIdIsUsedTwice()
    {
        var everywhere = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Repo.PathTo("mobile", "GroupLab.Mobile"), "*.cs", SearchOption.AllDirectories))
        {
            everywhere.AddRange(Given().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value));
        }

        foreach (var (file, ids) in Kept)
        {
            string source = File.ReadAllText(Repo.PathTo("mobile", "GroupLab.Mobile", file));
            foreach (string id in ids)
            {
                Assert.True(source.Contains($".Id(\"{id}\")", StringComparison.Ordinal), $"{file} no longer gives a control the id {id}");
            }
        }

        var twice = everywhere.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.True(twice.Count == 0, "ids used twice: " + string.Join(", ", twice));
    }

    [AvaloniaFact]
    public void TheTabsCaptureAndSettingsShowTheirIds()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        try
        {
            HashSet<string> Showing()
            {
                shell.UpdateLayout();
                return shell.GetVisualDescendants().OfType<Control>().Select(AutomationProperties.GetAutomationId).OfType<string>().ToHashSet();
            }

            var capture = Showing();
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                Assert.Contains("tab-" + place.ToString().ToLowerInvariant(), capture);
            }

            Assert.Contains("capture-take-picture", capture);
            Assert.Contains("capture-choose-photo", capture);
            shell.Show(Shell.Place.Settings);
            Dispatcher.UIThread.RunJobs();
            var settings = Showing();
            Assert.Contains("settings-send-diagnostics", settings);
            Assert.Contains("settings-printer-correction", settings);
        }
        finally
        {
            window.Close();
        }
    }
}
