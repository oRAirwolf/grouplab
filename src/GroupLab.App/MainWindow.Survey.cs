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

    private readonly StackPanel surveyHistory = new() { Spacing = Tokens.Space4 };

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

    /// <summary>The benchmark in progress, wherever it was started, for the headless tests to wait on.</summary>
    internal Task? BenchmarkTask => FirstRunBenchmark?.Running is { IsCompleted: false } first ? first : SettingsBenchmark?.Running;

    /// <summary>Entry 227 section 2: the benchmark offered after Yes on the first run screen, and the one in Settings.</summary>
    internal BenchmarkPanel? FirstRunBenchmark { get; private set; }

    internal BenchmarkPanel? SettingsBenchmark { get; private set; }

    private (GroupLab.Core.Gltd.Model.TargetDefinition? Definition, GroupLab.Core.Imaging.IImagingBackend Backend) BenchmarkWork()
    {
        string file = Path.Combine(AppContext.BaseDirectory, "targets", Benchmark.SheetFile);
        var definition = File.Exists(file) ? GroupLab.Core.Gltd.Json.GltdJsonReader.ReadFile(file).Definition : null;
        return (definition, new OpenCvSharpBackend());
    }

    private BenchmarkPanel NewBenchmarkPanel(string runWords, Action? later)
    {
        var panel = new BenchmarkPanel(settingsStore, BenchmarkWork, () => SendSurveyIfDueAsync(), runWords, later);

        // A run from the first run screen is what Settings then shows as the last run.
        panel.Ended += () =>
        {
            if (SettingsBenchmark is { } settings && settings != panel && settings.Running is not { IsCompleted: false })
            {
                settings.Say(BenchmarkSaid());
            }

            FillSurveyHistory();
        };
        return panel;
    }

    private string BenchmarkSaid() => settingsStore.LoadBenchmark() is { } last
        ? SharingWords.BenchmarkLast(settingsStore.LoadBenchmarkRanAt(), last.Result, last.Sent)
        : SharingWords.BenchmarkNever;

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

        if (settingsStore.SurveyWordingChanged())
        {
            part.Children.Add(Line(SharingWords.SurveyWordingChanged));
        }

        part.Children.Add(new TextBlock { Text = SharingWords.SurveyQuestion, Classes = { AppStyles.Title } });
        part.Children.Add(Line(SharingWords.SurveyIntro));
        foreach (string line in SurveyReport.WhatIsSent)
        {
            part.Children.Add(Line("• " + line));
        }

        // Entry 227 section 2: Yes does not close the question; it asks about the benchmark, Run it now or Later, and a run shows its
        // progress and can be cancelled. The question closes on Later, or on its own once the run has ended and been read.
        void Choose(SurveyChoice choice)
        {
            ChooseSurvey(choice);
            DiagnosticLog.Info("survey.first-run", ("choice", choice.ToString()));
            if (choice != SurveyChoice.Yes)
            {
                part.IsVisible = false;
                answered();
                return;
            }

            part.Children.Clear();
            part.Children.Add(new TextBlock { Text = SharingWords.BenchmarkNowQuestion, Classes = { AppStyles.Title } });
            part.Children.Add(Line(SharingWords.BenchmarkNowExplained));
            FirstRunBenchmark = NewBenchmarkPanel(SharingWords.BenchmarkRunNow, () =>
            {
                DiagnosticLog.Info("survey.benchmark.later");
                toaster.Say(SharingWords.BenchmarkLaterSaid);
                part.IsVisible = false;
                answered();
            });
            bool doneOffered = false;
            FirstRunBenchmark.Ended += () =>
            {
                if (!doneOffered)
                {
                    doneOffered = true;
                    part.Children.Add(Row(Button("Done", () =>
                    {
                        part.IsVisible = false;
                        answered();
                    })));
                }
            };
            part.Children.Add(FirstRunBenchmark);
            part.Children.Add(Line(SharingWords.SurveyLater));
        }

        part.Children.Add(Line(SharingWords.BenchmarkOffer));
        part.Children.Add(Row([.. SharingWords.SurveyChoices.Select(c => (Control)Button(c.Words, () => Choose(c.Choice)))]));
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
        surveySettings.Children.Add(Line(SharingWords.SurveyShort));

        // Entry 299: what a report holds, under "More".
        var more = new List<Control> { Line(SharingWords.SurveyIntro), FieldLabel("What a report holds") };
        foreach (string line in SurveyReport.WhatIsSent)
        {
            more.Add(Line("• " + line));
        }

        surveySettings.Children.Add(MoreFold.Make(settingsStore, "survey", more));

        // Entry 227 section 2: whether the survey is on is the choice above; the benchmark says when it last ran and what it found, and
        // runs from here, with its progress and a Cancel.
        surveySettings.Children.Add(FieldLabel("The benchmark"));
        if (SettingsBenchmark?.Running is not { IsCompleted: false })
        {
            SettingsBenchmark = NewBenchmarkPanel(SharingWords.BenchmarkButton, later: null);
            SettingsBenchmark.Say(BenchmarkSaid());
        }

        (SettingsBenchmark.Parent as Panel)?.Children.Remove(SettingsBenchmark);
        surveySettings.Children.Add(SettingsBenchmark);

        // Entry 241 sections 1.2 and 2.4: every run this copy has made, and the two things a person can do with their number.
        surveySettings.Children.Add(FieldLabel(SharingWords.BenchmarkHistory));
        surveySettings.Children.Add(surveyHistory);
        FillSurveyHistory();
        surveySettings.Children.Add(Row(
            Button(SharingWords.ResetNumber, () =>
            {
                settingsStore.ReplaceInstallation();
                DiagnosticLog.Info("survey.installation", ("replaced", true));
                toaster.Say(SharingWords.ResetNumberSaid);
            }),
            Button(SharingWords.DeleteReports, async () =>
            {
                bool taken = await Survey.DeleteAsync(SurveyOpen, CancellationToken.None);
                toaster.Say(taken ? SharingWords.DeleteReportsSaid : SharingWords.DeleteReportsFailed);
            })));
    }

    /// <summary>The device's own runs, newest first, entry 241 section 1.2.</summary>
    private void FillSurveyHistory()
    {
        surveyHistory.Children.Clear();
        var runs = settingsStore.LoadBenchmarkRuns();
        if (runs.Count == 0)
        {
            surveyHistory.Children.Add(Line(SharingWords.BenchmarkNever));
            return;
        }

        foreach (var run in runs.Reverse())
        {
            surveyHistory.Children.Add(Line(SharingWords.BenchmarkRunLine(run)));
        }
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
}
