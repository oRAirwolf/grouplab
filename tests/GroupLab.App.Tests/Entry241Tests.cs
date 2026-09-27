using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
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
/// NOTES-FROM-PLANNING.md entry 241: every benchmark run goes, each with the version that ran it, and the device lists them; a random number
/// counts one machine once, which the person can reset or have deleted; and a yes given to the earlier wording is asked again.
/// </summary>
public class Entry241Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static BenchmarkResult Result(long ms) => new(Benchmark.Workload, 2550, 3300, ms, [new StageTime("S5-S8.holes", ms / 2)], 480, 25, 25);

    private static (MainWindow Window, string Root) Open(Action<AppSettingsStore, string>? before = null)
    {
        Outside.Forget();
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry241-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        before?.Invoke(store, root);
        store.SaveUnits(UnitSettings.Imperial);
        store.SaveSending(SendingChoice.Never, null);
        store.SaveErrorChoice(ErrorReportChoice.Ask);
        var window = new MainWindow(store) { Width = 1400, Height = 900, ReceiverOpen = true, ErrorsOpen = true, SurveyOpen = true };
        window.Show();
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

    [AvaloniaFact]
    public void AYesToTheEarlierWordingIsAskedAgainAndSendsNothingMeanwhile()
    {
        var opened = Open((_, root) => File.WriteAllText(Path.Combine(root, "settings.json"), "{\"survey\":{\"choice\":\"Yes\"}}"));
        try
        {
            var store = opened.Window.SettingsStore;
            Assert.Equal(SurveyChoice.Unset, store.LoadSurveyChoice());
            Assert.True(store.SurveyWordingChanged());
            Assert.False(opened.Window.Survey.Due(DateTimeOffset.UtcNow));
            opened.Window.ShowFirstRunIfDue();
            Settle();
            var said = Said(opened.Window.FirstRunCard);
            Assert.Contains(SharingWords.SurveyWordingChanged, said);
            Assert.All(SurveyReport.WhatIsSent, line => Assert.Contains("• " + line, said));

            store.SaveSurveyChoice(SurveyChoice.Yes);
            Assert.Equal(SurveyChoice.Yes, store.LoadSurveyChoice());
            Assert.False(store.SurveyWordingChanged());
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task EveryRunGoesWithItsVersionAndARunMadeWhileOffNever()
    {
        var opened = Open();
        try
        {
            var store = opened.Window.SettingsStore;
            store.SaveSurveyChoice(SurveyChoice.Yes);
            store.SaveSurveySent(DateTimeOffset.UtcNow);
            var start = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
            store.SaveBenchmark(Result(1800), sent: false, ranAt: start);
            store.SaveBenchmark(Result(1700), sent: false, ranAt: start.AddMinutes(1), toSend: false);
            store.SaveBenchmark(Result(1750), sent: false, ranAt: start.AddMinutes(2));
            Assert.Equal(3, store.LoadBenchmarkRuns().Count);
            Assert.All(store.LoadBenchmarkRuns(), r => Assert.Equal(AppInfo.Version, r.Version));

            // The receiver's daily limit holds the report back: both runs wait, and nothing is marked sent.
            Outside.SurveyAnswer = _ => new PostAnswer(429, "{\"ok\":false,\"code\":\"rate_limit\"}");
            Assert.False(await opened.Window.Survey.SendDueAsync(true, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Equal(2, opened.Window.Survey.Unsent().Count);

            Outside.SurveyAnswer = _ => new PostAnswer(200, "{\"ok\":true}");
            Assert.True(await opened.Window.Survey.SendDueAsync(true, DateTimeOffset.UtcNow, CancellationToken.None));
            var sent = JsonNode.Parse(Outside.Surveys.Last().Report)!;
            Assert.Equal(SurveyReport.Schema, (string?)sent["schema"]);
            var runs = sent["benchmarks"]!.AsArray();
            Assert.Equal([1800L, 1750L], runs.Select(r => (long)r!["totalMilliseconds"]!));
            Assert.All(runs, r => Assert.Equal(AppInfo.Version, (string?)r!["version"]));
            Assert.Empty(opened.Window.Survey.Unsent());
            Assert.Equal([true, false, true], store.LoadBenchmarkRuns().Select(r => r.Sent));
            Assert.False(opened.Window.Survey.Due(DateTimeOffset.UtcNow));
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task SettingsListsEveryRunAndDeletesWhatTheServerKeeps()
    {
        var opened = Open();
        try
        {
            var store = opened.Window.SettingsStore;
            store.SaveSurveyChoice(SurveyChoice.Yes);
            var start = new DateTimeOffset(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
            store.SaveBenchmark(Result(1800), sent: true, ranAt: start);
            store.SaveBenchmark(Result(1750), sent: false, ranAt: start.AddDays(1));
            opened.Window.ShowSettings();
            Settle();
            var said = Said(opened.Window.SettingsBody);
            Assert.Contains(SharingWords.BenchmarkHistory, said);
            foreach (var run in store.LoadBenchmarkRuns())
            {
                Assert.Contains(SharingWords.BenchmarkRunLine(run), said);
            }

            Assert.True(said.IndexOf(SharingWords.BenchmarkRunLine(store.LoadBenchmarkRuns()[1])) < said.IndexOf(SharingWords.BenchmarkRunLine(store.LoadBenchmarkRuns()[0])),
                "the newest run is listed first");

            string before = store.LoadInstallation();
            Outside.SurveyAnswer = _ => new PostAnswer(200, "{\"ok\":true}");
            var delete = opened.Window.SettingsBody.GetVisualDescendants().OfType<Button>().Single(b => Said(b).Contains(SharingWords.DeleteReports));
            delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; i < 50 && Outside.Surveys.Count == 0; i++)
            {
                await Task.Delay(20);
                Settle();
            }

            var request = JsonNode.Parse(Outside.Surveys.Single().Report)!.AsObject();
            Assert.Equal(SurveyReport.DeleteSchema, (string?)request["schema"]);
            Assert.Equal(before, (string?)request["installation"]);
            Assert.Equal(before, store.LoadInstallation());

            var reset = opened.Window.SettingsBody.GetVisualDescendants().OfType<Button>().Single(b => Said(b).Contains(SharingWords.ResetNumber));
            reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.NotEqual(before, store.LoadInstallation());
        }
        finally
        {
            Close(opened);
        }
    }
}
