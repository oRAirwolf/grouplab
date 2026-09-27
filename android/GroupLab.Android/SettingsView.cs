using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3 and entry 208 section 4: Settings, with the first run's questions together under Sharing, in
/// the same order and words, so every answer is changed in one place. The settings file is the desktop's own format, read and written by
/// the desktop's own code.
/// </summary>
public sealed class SettingsView : UserControl
{
    public SettingsView(AppSettingsStore settings)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Heading("Sharing"));

        column.Children.Add(Screens.Heading("Sending targets"));
        if (!Shell.TargetsOpen)
        {
            column.Children.Add(Screens.Line(SharingWords.TargetsClosed));
        }
        else
        {
            var (choice, level) = settings.LoadSending();
            foreach (var (value, words) in SharingWords.TargetChoices)
            {
                var radio = Screens.Radio("sendingChoice", words, choice == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadSending().Choice != value)
                    {
                        settings.SaveSending(value, settings.LoadSending().Level);
                        DiagnosticLog.Info("send.choice", ("choice", value.ToString()));
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Line(SharingWords.LevelHeading));
            foreach (var (value, words) in SharingWords.Levels(ReceiverTerms.Current))
            {
                var radio = Screens.Radio("sendingLevel", words, level == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    // A change applies to targets sent from now on and never re-labels one already sent.
                    if (radio.IsChecked == true && settings.LoadSending().Level != value)
                    {
                        settings.SaveSending(settings.LoadSending().Choice, value);
                    }
                };
                column.Children.Add(radio);
            }
        }

        column.Children.Add(Screens.Heading("Error reports"));
        if (!Shell.ErrorsOpen)
        {
            column.Children.Add(Screens.Line("Sending error reports to the project is not open yet. When it is, GroupLab will ask once whether you want to."));
        }
        else
        {
            var choice = settings.LoadErrorChoice();
            foreach (var (value, words) in SharingWords.ErrorChoices)
            {
                var radio = Screens.Radio("errorChoice", words, choice == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadErrorChoice() != value)
                    {
                        settings.SaveErrorChoice(value);
                        DiagnosticLog.Info("errors.choice", ("choice", value.ToString()));
                        _ = App.SendWaitingErrorsAsync();
                    }
                };
                column.Children.Add(radio);
            }

            column.Children.Add(Screens.Line(SharingWords.ErrorsIntro));
            foreach (string line in ErrorReports.WhatIsSent)
            {
                column.Children.Add(Screens.Line("• " + line));
            }
        }

        column.Children.Add(Screens.Heading("Hardware survey"));
        if (!Shell.SurveyOpen)
        {
            column.Children.Add(Screens.Line(SharingWords.SurveyClosed));
        }
        else
        {
            var chosen = settings.LoadSurveyChoice();
            foreach (var (value, words) in SharingWords.SurveyChoices)
            {
                var radio = Screens.Radio("surveyChoice", words, chosen == value);
                radio.IsCheckedChanged += (_, _) =>
                {
                    if (radio.IsChecked == true && settings.LoadSurveyChoice() != value)
                    {
                        settings.SaveSurveyChoice(value);
                        if (value != SurveyChoice.Yes)
                        {
                            App.Survey?.Forget();
                        }

                        DiagnosticLog.Info("survey.choice", ("choice", value.ToString()));
                    }
                };
                column.Children.Add(radio);
            }

            foreach (string line in SurveyReport.WhatIsSent)
            {
                column.Children.Add(Screens.Line("• " + line));
            }

            // Entry 227 section 2: when the benchmark last ran and what it found, and a button to run it now, with its progress.
            column.Children.Add(Screens.Heading("The benchmark"));
            var benchmark = new BenchmarkPanel(settings, PhoneAnalysis.BenchmarkWork, FirstRunView.SendSurvey, SharingWords.BenchmarkButton, later: null);
            benchmark.Say(settings.LoadBenchmark() is { } last
                ? SharingWords.BenchmarkLast(settings.LoadBenchmarkRanAt(), last.Result, last.Sent)
                : SharingWords.BenchmarkNever);
            column.Children.Add(benchmark);

            // Entry 241 sections 1.2 and 2.4: every run this copy has made, newest first, and the two things a person can do with their number.
            column.Children.Add(Screens.Heading(SharingWords.BenchmarkHistory));
            var history = new StackPanel { Spacing = 4 };
            void FillHistory()
            {
                history.Children.Clear();
                var runs = settings.LoadBenchmarkRuns();
                if (runs.Count == 0)
                {
                    history.Children.Add(Screens.Line(SharingWords.BenchmarkNever));
                }

                foreach (var run in runs.Reverse())
                {
                    history.Children.Add(Screens.Line(SharingWords.BenchmarkRunLine(run)));
                }
            }

            FillHistory();
            benchmark.Ended += FillHistory;
            column.Children.Add(history);
            var said = Screens.Line("");
            column.Children.Add(Screens.Choice(SharingWords.ResetNumber, () =>
            {
                settings.ReplaceInstallation();
                DiagnosticLog.Info("survey.installation", ("replaced", true));
                said.Text = SharingWords.ResetNumberSaid;
            }));
            async Task Delete()
            {
                bool taken = App.Survey is { } queue && await queue.DeleteAsync(Shell.SurveyOpen, CancellationToken.None);
                said.Text = taken ? SharingWords.DeleteReportsSaid : SharingWords.DeleteReportsFailed;
            }

            column.Children.Add(Screens.Choice(SharingWords.DeleteReports, () => _ = Delete()));
            column.Children.Add(said);
        }

        column.Children.Add(Screens.Heading("About"));
        column.Children.Add(Screens.Line($"GroupLab {AppInfo.Version}"));
#if GROUPLAB_DEV
        // Entry 234 section 1: said plainly, so a screenshot or a report from it is never mistaken for the published application.
        column.Children.Add(Screens.Line("This is GroupLab Dev, the development build. It installs beside GroupLab from Google Play, can be debugged over adb, and marks its error and survey reports as coming from a development build."));
#endif
        Content = Screens.Page(column);
    }
}
