using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 227 section 2: Alan pressed Yes on the survey on nightly 110, the window closed, and he was never offered the
/// benchmark. Now the question says before he answers that Yes does not run it, Yes asks Run it now or Later instead of closing, a run shows
/// its progress and can be cancelled, it says when it finished and what it found, and Settings says when it last ran.
/// </summary>
public class Entry227Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static (MainWindow Window, string Root) Open()
    {
        Outside.Forget();
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry227-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        store.SaveSending(SendingChoice.Never, null);
        store.SaveErrorChoice(ErrorReportChoice.Ask);
        var window = new MainWindow(store) { Width = 1400, Height = 900, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true };
        window.Show();
        Settle();
        window.ShowFirstRunIfDue();
        Settle();
        return (window, root);
    }

    private static void Close((MainWindow Window, string Root) opened)
    {
        opened.Window.Close();
        Outside.Forget();
        GroupLab.Tests.Support.Temp.Delete(opened.Root);
    }

    private static List<string> Said(Control root) => [.. root.GetVisualDescendants().OfType<TextBlock>().Where(b => b.IsEffectivelyVisible).Select(b => b.Text ?? "")];

    private static void Press(Control root, string words)
    {
        var button = root.GetVisualDescendants().OfType<Button>().First(b => b.IsEffectivelyVisible && Equals(b.Content, words));
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    [AvaloniaFact]
    public void YesAsksAboutTheBenchmarkAndLaterClosesWithNothingRun()
    {
        var opened = Open();
        try
        {
            var card = opened.Window.FirstRunCard;
            int offer = Said(card).IndexOf(SharingWords.BenchmarkOffer);
            Assert.True(offer >= 0, "the question says what Yes does before it is answered");
            Press(card, "Yes, take part");

            Assert.True(opened.Window.FirstRunShown);
            Assert.Contains(SharingWords.BenchmarkNowQuestion, Said(card));
            Assert.Equal(SurveyChoice.Yes, opened.Window.SettingsStore.LoadSurveyChoice());
            Press(card, SharingWords.BenchmarkLater);

            Assert.False(opened.Window.FirstRunShown);
            Assert.Null(opened.Window.SettingsStore.LoadBenchmark());
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task RunItNowShowsItsProgressThenWhenItFinishedAndWhatItFound()
    {
        var opened = Open();
        try
        {
            var card = opened.Window.FirstRunCard;
            Press(card, "Yes, take part");
            Press(card, SharingWords.BenchmarkRunNow);

            var panel = opened.Window.FirstRunBenchmark!;
            Assert.True(panel.CanCancel);
            Assert.StartsWith("Running the benchmark: stage", panel.StatusText, StringComparison.Ordinal);
            await opened.Window.BenchmarkTask!;
            Settle();

            Assert.StartsWith("The benchmark took ", panel.StatusText, StringComparison.Ordinal);
            Assert.Contains("Finished at ", panel.StatusText, StringComparison.Ordinal);
            Assert.False(panel.CanCancel);
            Assert.NotNull(opened.Window.SettingsStore.LoadBenchmark());
            Assert.NotNull(opened.Window.SettingsStore.LoadBenchmarkRanAt());
            Press(card, "Done");
            Assert.False(opened.Window.FirstRunShown);

            opened.Window.ShowSettings();
            Settle();
            Assert.StartsWith("Last run ", opened.Window.SettingsBenchmark!.StatusText, StringComparison.Ordinal);
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task ACancelledRunKeepsNothing()
    {
        var opened = Open();
        try
        {
            opened.Window.ShowSettings();
            Settle();
            var panel = opened.Window.SettingsBenchmark!;
            Assert.Equal(SharingWords.BenchmarkNever, panel.StatusText);
            panel.Start();
            panel.Cancel();
            await opened.Window.BenchmarkTask!;
            Settle();

            Assert.Equal(SharingWords.BenchmarkCancelled, panel.StatusText);
            Assert.Null(opened.Window.SettingsStore.LoadBenchmark());
        }
        finally
        {
            Close(opened);
        }
    }
}
