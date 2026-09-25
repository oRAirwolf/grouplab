using System.Text.Json.Nodes;
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
/// NOTES-FROM-PLANNING.md entries 207 and 208, docs/SURVEY.md: the hardware survey is the third question on the first run screen, asked
/// with nothing chosen, shown once more to people who answered the other two, and mirrored in Settings under Sharing. Nothing is kept or
/// sent until the person says yes, a report goes at most weekly, and one the receiver did not take is kept. Every request goes to the
/// recording outside world.
/// </summary>
public class Entry208Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static (MainWindow Window, string Root) Open(bool answeredBefore)
    {
        Outside.Forget();
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry208-{Guid.NewGuid():N}");
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        if (answeredBefore)
        {
            store.SaveSending(SendingChoice.Never, null);
            store.SaveErrorChoice(ErrorReportChoice.Ask);
        }

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

    private static AnalysisFacts Analysis() => new(4000, 3000, 4000, 3000, [new StageTime("detection", 900)], 800);

    [AvaloniaFact]
    public void TheSurveyIsTheThirdQuestionAndNothingIsChosen()
    {
        var opened = Open(answeredBefore: false);
        try
        {
            opened.Window.ShowFirstRunIfDue();
            Settle();
            var said = Said(opened.Window.FirstRunCard);
            int targets = said.IndexOf(SharingWords.TargetsQuestion), errors = said.IndexOf(SharingWords.ErrorsQuestion), survey = said.IndexOf(SharingWords.SurveyQuestion);
            Assert.True(targets >= 0 && targets < errors && errors < survey, string.Join(" | ", said));
            Assert.Contains(SharingWords.BenchmarkOffer, said);
            Assert.All(SurveyReport.WhatIsSent, line => Assert.Contains("• " + line, said));
            Assert.DoesNotContain(SharingWords.EarlierKept, said);
            Assert.All(opened.Window.FirstRunCard.GetVisualDescendants().OfType<RadioButton>(), r => Assert.NotEqual(true, r.IsChecked));
            Assert.Equal(SurveyChoice.Unset, opened.Window.SettingsStore.LoadSurveyChoice());
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public void SomebodyWhoAnsweredBeforeIsAskedOnlyTheSurveyAndTheirAnswersAreKept()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            opened.Window.ShowFirstRunIfDue();
            Settle();
            Assert.True(opened.Window.FirstRunShown);
            var said = Said(opened.Window.FirstRunCard);
            Assert.Contains(SharingWords.EarlierKept, said);
            Assert.Contains(SharingWords.SurveyQuestion, said);
            Assert.DoesNotContain(SharingWords.TargetsQuestion, said);
            Assert.DoesNotContain(SharingWords.ErrorsQuestion, said);

            var no = opened.Window.FirstRunCard.GetVisualDescendants().OfType<Button>().Single(b => b.IsEffectivelyVisible && Equals(b.Content, "No"));
            no.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.False(opened.Window.FirstRunShown);
            Assert.Equal(SurveyChoice.No, opened.Window.SettingsStore.LoadSurveyChoice());
            Assert.Equal(SendingChoice.Never, opened.Window.SettingsStore.LoadSending().Choice);
            Assert.Equal(ErrorReportChoice.Ask, opened.Window.SettingsStore.LoadErrorChoice());
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public void SettingsHasTheSurveyUnderSharing()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            opened.Window.ShowSettings();
            Settle();
            var said = Said(opened.Window.SettingsBody);
            int sharing = said.IndexOf("Sharing"), targets = said.IndexOf("Sending targets"), errors = said.IndexOf("Error reports"), survey = said.IndexOf("Hardware survey");
            Assert.True(sharing >= 0 && sharing < targets && targets < errors && errors < survey, string.Join(" | ", said));
            Assert.Contains(SharingWords.BenchmarkButton, opened.Window.SettingsBody.GetVisualDescendants().OfType<Button>().Select(b => b.Content as string));
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task NothingIsKeptOrSentUntilThePersonSaysYes()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            var queue = opened.Window.Survey;
            queue.Record(Analysis());
            Assert.Empty(queue.Waiting());
            Assert.False(await opened.Window.SendSurveyIfDueAsync());
            Assert.Empty(Outside.Surveys);
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task AReportGoesWeeklyWithWhatWasKeptAndOneNotTakenIsKept()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            var store = opened.Window.SettingsStore;
            store.SaveSurveyChoice(SurveyChoice.Yes);
            var queue = opened.Window.Survey;
            queue.Record(Analysis());
            queue.Record(Analysis());

            Outside.SurveyAnswer = _ => new PostAnswer(503, "{}");
            Assert.False(await queue.SendDueAsync(true, DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Equal(2, queue.Waiting().Count);

            Outside.SurveyAnswer = _ => new PostAnswer(200, "{\"ok\":true}");
            var now = DateTimeOffset.UtcNow;
            Assert.True(await queue.SendDueAsync(true, now, CancellationToken.None));
            var (address, report) = Outside.Surveys[^1];
            Assert.Equal(ReceiverTerms.Current.SurveyReceiver, address);
            var sent = JsonNode.Parse(report)!;
            Assert.Equal(2, sent["analyses"]!.AsArray().Count);
            Assert.Equal(store.LoadInstallation(), (string?)sent["installation"]);
            Assert.Empty(queue.Waiting());

            queue.Record(Analysis());
            Assert.False(await queue.SendDueAsync(true, now.AddDays(3), CancellationToken.None));
            Assert.True(await queue.SendDueAsync(true, now.AddDays(7), CancellationToken.None));
            Assert.False(await queue.SendDueAsync(false, now.AddDays(30), CancellationToken.None));
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task SayingNoForgetsWhatWasKept()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            opened.Window.SettingsStore.SaveSurveyChoice(SurveyChoice.Yes);
            opened.Window.Survey.Record(Analysis());
            Assert.Single(opened.Window.Survey.Waiting());
            opened.Window.ShowSettings();
            Settle();
            var no = opened.Window.SettingsBody.GetVisualDescendants().OfType<RadioButton>().Single(r => r.GroupName == "surveyChoice" && ((TextBlock)r.Content!).Text == "No");
            no.IsChecked = true;
            Settle();
            Assert.Equal(SurveyChoice.No, opened.Window.SettingsStore.LoadSurveyChoice());
            Assert.Empty(opened.Window.Survey.Waiting());
            await Task.CompletedTask;
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task TheBenchmarkRunsOnlyWhenAskedAndGoesWithTheNextReport()
    {
        var opened = Open(answeredBefore: true);
        try
        {
            var store = opened.Window.SettingsStore;
            Assert.Null(store.LoadBenchmark());
            store.SaveSurveyChoice(SurveyChoice.Yes);
            store.SaveSurveySent(DateTimeOffset.UtcNow);
            Outside.SurveyAnswer = _ => new PostAnswer(200, "{\"ok\":true}");
            var outcome = new TextBlock();
            opened.Window.RunBenchmark(outcome);
            await opened.Window.BenchmarkTask!;
            Settle();
            Assert.StartsWith("The benchmark took ", outcome.Text, StringComparison.Ordinal);
            var sent = JsonNode.Parse(Outside.Surveys.Single().Report)!;
            Assert.Equal(Benchmark.Workload, (string?)sent["benchmark"]!["workload"]);
            Assert.Equal(true, store.LoadBenchmark()?.Sent);
        }
        finally
        {
            Close(opened);
        }
    }
}
