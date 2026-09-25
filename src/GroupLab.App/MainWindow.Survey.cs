using System.Globalization;
using Avalonia.Controls;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;
using GroupLab.Core.Trace;

namespace GroupLab.App;

/// <summary>
/// The hardware survey on the desktop, docs/SURVEY.md and NOTES-FROM-PLANNING.md entries 207 and 208: the third question on the first run
/// screen, with the benchmark offered under it; its place in Settings under Sharing; each analysis kept for the next report once the
/// person has said yes; and the report, at most weekly, when the receiver is open. The queue is shared with the Android application.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel surveySettings = new() { Spacing = Tokens.Space8 };

    private SurveyQueue? surveyQueue;

    /// <summary>What a new window starts with in place of limits.json's switch; the test assembly sets it off, as it does for the others.</summary>
    internal static bool? SurveyOpenByDefault { get; set; }

    private bool surveyOpen = SurveyOpenByDefault ?? ReceiverTerms.Current.SurveyOpen;

    /// <summary>Whether the survey's receiver is open: the build's limits.json, which the tests can override. Settings follows it.</summary>
    internal bool SurveyOpen
    {
        get => surveyOpen;
        set
        {
            surveyOpen = value;
            FillSurveySettings();
        }
    }

    internal SurveyQueue Survey => surveyQueue ??= new SurveyQueue(settingsStore, Machine);

    /// <summary>The benchmark in progress, for the headless tests to wait on.</summary>
    internal Task? BenchmarkTask { get; private set; }

    /// <summary>This machine, with the screen the window is on.</summary>
    private GroupLab.Core.Survey.MachineFacts Machine()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        return SurveyReport.ThisMachine(
            screen: screen is null ? null : string.Create(CultureInfo.InvariantCulture, $"{screen.Bounds.Width} by {screen.Bounds.Height}"),
            screenScale: screen?.Scaling);
    }

    /// <summary>Keeps one analysis for the next report, where the person has said yes. The desktop analyzes at the image's own size.</summary>
    private void RecordAnalysis(TraceRecorder trace)
    {
        if (detectionMetadata is { Width: int w, Height: int h })
        {
            Survey.Record(new AnalysisFacts(w, h, w, h, Benchmark.Stages(trace), Benchmark.PeakMegabytes()));
        }
    }

    /// <summary>Sends the survey report when one is due. Never a dialog, and a failure waits for the next start.</summary>
    internal Task<bool> SendSurveyIfDueAsync() => Survey.SendDueAsync(SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None);

    /// <summary>The first run screen's survey question, after the other two.</summary>
    private void FillFirstRunSurvey(StackPanel part, Action answered, bool earlierKept)
    {
        if (earlierKept)
        {
            part.Children.Add(Line(SharingWords.EarlierKept));
        }

        part.Children.Add(new TextBlock { Text = SharingWords.SurveyQuestion, Classes = { AppStyles.Title } });
        part.Children.Add(Line(SharingWords.SurveyIntro));
        foreach (string line in SurveyReport.WhatIsSent)
        {
            part.Children.Add(Line("• " + line));
        }

        var outcome = Line("");
        void Choose(SurveyChoice choice)
        {
            ChooseSurvey(choice);
            DiagnosticLog.Info("survey.first-run", ("choice", choice.ToString()));
            part.IsVisible = false;
            answered();
        }

        part.Children.Add(Row([.. SharingWords.SurveyChoices.Select(c => (Control)Button(c.Words, () => Choose(c.Choice)))]));
        part.Children.Add(Line(SharingWords.BenchmarkOffer));
        part.Children.Add(Row(Button(SharingWords.BenchmarkButton, () => RunBenchmark(outcome))));
        part.Children.Add(outcome);
        part.Children.Add(Line(SharingWords.SurveyLater));
    }

    /// <summary>Entry 208 section 4: the survey's place in Settings, under Sharing, after the other two.</summary>
    private void BuildSurveySettings(StackPanel column)
    {
        column.Children.Add(FieldLabel("Hardware survey"));
        column.Children.Add(surveySettings);
        FillSurveySettings();
    }

    internal void FillSurveySettings()
    {
        surveySettings.Children.Clear();
        if (!SurveyOpen)
        {
            surveySettings.Children.Add(Line(SharingWords.SurveyClosed));
            return;
        }

        var choice = settingsStore.LoadSurveyChoice();
        var choices = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var (value, words) in SharingWords.SurveyChoices)
        {
            var radio = new RadioButton { GroupName = "surveyChoice", Content = Wrapped(words), IsChecked = choice == value };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settingsStore.LoadSurveyChoice() != value)
                {
                    ChooseSurvey(value);
                    DiagnosticLog.Info("survey.choice", ("choice", value.ToString()));
                }
            };
            choices.Children.Add(radio);
        }

        surveySettings.Children.Add(choices);
        surveySettings.Children.Add(FieldLabel("What a report holds"));
        foreach (string line in SurveyReport.WhatIsSent)
        {
            surveySettings.Children.Add(Line("• " + line));
        }

        var outcome = Line(settingsStore.LoadBenchmark() is { } last ? SharingWords.BenchmarkDone(last.Result, goes: false) : "");
        surveySettings.Children.Add(Row(
            Button(SharingWords.BenchmarkButton, () => RunBenchmark(outcome)),
            Button("Replace the installation number", () =>
            {
                settingsStore.ReplaceInstallation();
                DiagnosticLog.Info("survey.installation", ("replaced", true));
                toaster.Say("This copy of GroupLab has a new installation number.");
            })));
        surveySettings.Children.Add(outcome);
    }

    /// <summary>Saying no forgets every kept analysis; saying yes sends the first report when one is due.</summary>
    private void ChooseSurvey(SurveyChoice choice)
    {
        settingsStore.SaveSurveyChoice(choice);
        if (choice != SurveyChoice.Yes)
        {
            Survey.Forget();
        }
        else
        {
            _ = SendSurveyIfDueAsync();
        }

        FillSurveySettings();
    }

    /// <summary>
    /// Runs the benchmark away from the window's thread and says what it found. It goes with the next report only if the person has said
    /// yes to the survey; otherwise it is only for them.
    /// </summary>
    internal void RunBenchmark(TextBlock outcome)
    {
        if (BenchmarkTask is { IsCompleted: false })
        {
            return;
        }

        string file = Path.Combine(AppContext.BaseDirectory, "targets", Benchmark.SheetFile);
        if (!File.Exists(file) || GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(file).Definition is not { } definition)
        {
            outcome.Text = "The benchmark's target is missing from this installation.";
            return;
        }

        outcome.Text = SharingWords.BenchmarkRunning;
        BenchmarkTask = Run();

        async Task Run()
        {
            var result = await Task.Run(() => Benchmark.Run(definition, new OpenCvSharpBackend()));
            bool goes = settingsStore.LoadSurveyChoice() == SurveyChoice.Yes;
            settingsStore.SaveBenchmark(result, sent: false);
            DiagnosticLog.Info("survey.benchmark", ("ms", result.TotalMilliseconds), ("peak", result.PeakMegabytes), ("found", result.HolesFound));
            outcome.Text = SharingWords.BenchmarkDone(result, goes);
            if (goes)
            {
                await SendSurveyIfDueAsync();
            }
        }
    }
}
