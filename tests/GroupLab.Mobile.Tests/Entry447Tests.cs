using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.Mobile.Dev;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 447 section 2, the phones: at Android's largest text size, every control on every place along the bottom,
/// the result and the pages one press from it has a name for TalkBack and VoiceOver, and a screen is read before the bar along the bottom.
/// </summary>
public class Entry447Tests
{
    private static List<Control> Controls(Shell shell) => [.. shell.GetVisualDescendants().OfType<Control>().Where(c => c.IsEffectivelyVisible
        && c.Bounds.Width > 0 && c is Button or ToggleButton or TextBox or ComboBox or Slider or AutoCompleteBox
        && c.GetVisualAncestors().OfType<Button>().FirstOrDefault() is null && c.GetVisualAncestors().OfType<ScrollBar>().FirstOrDefault() is null
        && c.GetVisualAncestors().OfType<AutoCompleteBox>().FirstOrDefault() is null)];

    private static void Check(Shell shell, string where, List<string> faults)
    {
        Dispatcher.UIThread.RunJobs();
        var controls = Controls(shell);
        faults.AddRange(controls.Where(c => string.IsNullOrWhiteSpace(ControlAutomationPeer.CreatePeerForElement(c).GetName()))
            .Select(c => $"{where}: a {c.GetType().Name} {AutomationProperties.GetAutomationId(c)} has no name"));
        int firstTab = controls.FindIndex(c => AutomationProperties.GetAutomationId(c)?.StartsWith("tab-", StringComparison.Ordinal) == true);
        if (firstTab >= 0 && controls.Skip(firstTab).Any(c => AutomationProperties.GetAutomationId(c)?.StartsWith("tab-", StringComparison.Ordinal) != true))
        {
            faults.Add($"{where}: the bar along the bottom is read before the screen");
        }
    }

    [AvaloniaFact]
    public async Task EveryControlIsNamedAndTheScreenIsReadBeforeTheBar()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        Scenario.AnswerFirstRun(Phone.Settings);
        var shell = new Shell();
        var window = new Window { Width = 402, Height = 874, Content = shell };
        window.Show();
        AndroidText.LargestUntilClosed(window);
        var faults = new List<string>();
        try
        {
            foreach (var place in Enum.GetValues<Shell.Place>())
            {
                shell.Show(place);
                Check(shell, place.ToString(), faults);
            }

            Directory.CreateDirectory(Scenario.Folder);
            File.Copy(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), Path.Combine(Scenario.Folder, "named.png"), overwrite: true);
            await Scenario.Run("""
                { "name": "named", "caliber": ".308 Winchester", "steps": [ { "do": "picture", "file": "named.png" },
                  { "do": "wait", "screen": "FeedbackView|ResultView", "seconds": 120 }, { "do": "press", "name": "Use this picture|Use it anyway" },
                  { "do": "wait", "screen": "ResultView", "seconds": 120 } ] }
                """);
            Assert.NotEmpty(shell.GetVisualDescendants().OfType<ResultView>());
            Check(shell, "the result", faults);
            foreach (string id in new[] { "result-shots", "result-zero", "result-share-picture", "result-report", "result-how-shot" })
            {
                var button = shell.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == id);
                Assert.True(button is not null, id + " is not on the result");
                button!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Check(shell, id, faults);
                shell.WayBack()?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
            }

            // What entry 447 left: the pages two presses away, Fix holes and the readings page from the result, the fingerprint of a
            // store-bought target from Targets, and the printer check from Settings.
            void Open(string id)
            {
                var button = shell.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == id);
                Assert.True(button is not null, id + " is not on the screen");
                button!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Check(shell, id, faults);
            }

            // How you shot it closes with its Done, not a Back.
            shell.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "bulls-done")
                ?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            foreach (string id in new[] { "result-fix-holes", "velocity-type" })
            {
                Open(id);
                shell.WayBack()?.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Dispatcher.UIThread.RunJobs();
            }

            foreach (var (show, id) in new (Action Show, string Id)[] { (() => shell.Show(Shell.Place.Targets), "targets-add-store"), (() => shell.ShowSettings(GroupLab.App.SettingsCategory.Printers), "settings-add-printer") })
            {
                show();
                Dispatcher.UIThread.RunJobs();
                Open(id);
            }

            Assert.True(faults.Count == 0, string.Join(Environment.NewLine, faults));
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(Path.Combine(Scenario.Folder, "named.png"));
        }
    }
}
