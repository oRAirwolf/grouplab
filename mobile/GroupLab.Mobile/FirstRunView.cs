using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3 and entry 208: the first run's questions, together on one screen, in the desktop's order and
/// words (<see cref="SharingWords"/>), each with what is sent and its own answer. Nothing is chosen for the person (entry 203 section 3), a
/// question is asked only while the project takes what it asks about, and the screen gives way to the application when each one asked
/// has been answered. The survey, entry 208's third question, joins them when its receiver opens (docs/SURVEY.md).
/// </summary>
public sealed class FirstRunView : UserControl
{
    /// <summary>Whether any question is still open and unanswered.</summary>
    public static bool Due(AppSettingsStore settings) => TargetsDue(settings) || ErrorsDue(settings) || SurveyDue(settings);

    private static bool SurveyDue(AppSettingsStore settings) => Shell.SurveyOpen && settings.LoadSurveyChoice() == SurveyChoice.Unset;

    private static bool TargetsDue(AppSettingsStore settings) => Shell.TargetsOpen && settings.LoadSending().Choice == SendingChoice.Unset;

    private static bool ErrorsDue(AppSettingsStore settings) => Shell.ErrorsOpen && settings.LoadErrorChoice() == ErrorReportChoice.Unset;

    /// <summary>Sends the survey report when one is due, after a benchmark the person ran.</summary>
    internal static Task SendSurvey() => Phone.Survey?.SendDueAsync(Shell.SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None) ?? Task.CompletedTask;

    public FirstRunView(AppSettingsStore settings, Action done)
    {
        var column = new StackPanel { Spacing = 24 };

        // Entry 246, look B: the page's own title, then each question with what it sends on a card of its own.
        column.Children.Add(Screens.Title("Before you start"));
        var targets = new StackPanel { Spacing = 8, IsVisible = TargetsDue(settings) };
        var errors = new StackPanel { Spacing = 8, IsVisible = ErrorsDue(settings) };
        var survey = new StackPanel { Spacing = 8, IsVisible = SurveyDue(settings) };
        column.Children.Add(targets);
        column.Children.Add(errors);
        column.Children.Add(survey);

        // Entry 273: the printer check, offered once beside the questions and never holding the page open.
        if (settings.LoadPrinters().Count == 0 && !settings.LoadPrinterOffered())
        {
            settings.SavePrinterOffered();
            var printer = new StackPanel { Spacing = 8 };
            printer.Children.Add(Screens.Heading("Check your printer"));
            printer.Children.Add(Screens.Dim("Printers often print a little small. Check once, with a card and one photo, a caliper, a ruler or a scanner, and every photo of a GroupLab sheet from that printer measures in real inches. It is also under Settings, Printers."));
            printer.Children.Add(Screens.Choice("Skip for now", () => printer.IsVisible = false));
            column.Children.Add(printer);
        }
        void Answered()
        {
            if (!targets.IsVisible && !errors.IsVisible && !survey.IsVisible)
            {
                done();
            }
        }

        var terms = ReceiverTerms.Current;
        targets.Children.Add(Screens.Heading(SharingWords.TargetsQuestion));
        targets.Children.Add(Screens.Card([Screens.Dim(SharingWords.TargetsIntro), .. TargetPackages.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line))]));

        var testing = Screens.Radio("firstRunLevel", SharingWords.TestingOnly + terms.TestingText, false);
        var publishable = Screens.Radio("firstRunLevel", SharingWords.MayBePublished + terms.PublishableText, false);
        targets.Children.Add(testing);
        targets.Children.Add(publishable);
        var why = Screens.Line("");
        targets.Children.Add(why);
        foreach (var (choice, words) in SharingWords.TargetChoices)
        {
            targets.Children.Add(Screens.Choice(words, () =>
            {
                ConsentLevel? level = testing.IsChecked == true ? ConsentLevel.Testing : publishable.IsChecked == true ? ConsentLevel.Publishable : null;
                if (choice == SendingChoice.Always && level is null)
                {
                    why.Text = SharingWords.LevelFirst;
                    return;
                }

                settings.SaveSending(choice, level);
                DiagnosticLog.Info("send.first-run", ("choice", choice.ToString()), ("level", level?.ToString()));
                targets.IsVisible = false;
                Answered();
            }));
        }

        targets.Children.Add(Screens.Dim(SharingWords.TargetsLater));

        errors.Children.Add(Screens.Heading(SharingWords.ErrorsQuestion));
        errors.Children.Add(Screens.Card([Screens.Dim(SharingWords.ErrorsIntro), .. ErrorReports.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line))]));

        foreach (var (choice, words) in SharingWords.ErrorChoices)
        {
            errors.Children.Add(Screens.Choice(words, () =>
            {
                settings.SaveErrorChoice(choice);
                DiagnosticLog.Info("errors.first-run", ("choice", choice.ToString()));
                errors.IsVisible = false;
                _ = Phone.SendWaitingErrorsAsync();
                Answered();
            }));
        }

        errors.Children.Add(Screens.Dim(SharingWords.ErrorsLater));

        // Entry 208: the survey, third, in the desktop's words; somebody who answered the other two before is told their answers are kept.
        if (!targets.IsVisible && !errors.IsVisible)
        {
            survey.Children.Add(Screens.Dim(SharingWords.EarlierKept));
        }

        // Entry 241 section 2.5: a yes given to the earlier wording is asked again, and says why.
        if (settings.SurveyWordingChanged())
        {
            survey.Children.Add(Screens.Line(SharingWords.SurveyWordingChanged));
        }

        survey.Children.Add(Screens.Heading(SharingWords.SurveyQuestion));
        survey.Children.Add(Screens.Card([Screens.Dim(SharingWords.SurveyIntro), .. SurveyReport.WhatIsSent.Select(line => (Control)Screens.Dim("• " + line))]));

        foreach (var (choice, words) in SharingWords.SurveyChoices)
        {
            survey.Children.Add(Screens.Choice(words, () =>
            {
                settings.SaveSurveyChoice(choice);
                if (choice != SurveyChoice.Yes)
                {
                    Phone.Survey?.Forget();
                }

                DiagnosticLog.Info("survey.first-run", ("choice", choice.ToString()));
                if (choice != SurveyChoice.Yes)
                {
                    survey.IsVisible = false;
                    Answered();
                    return;
                }

                // Entry 227 section 2: Yes asks about the benchmark, as on the desktop, rather than closing.
                survey.Children.Clear();
                survey.Children.Add(Screens.Heading(SharingWords.BenchmarkNowQuestion));
                survey.Children.Add(Screens.Line(SharingWords.BenchmarkNowExplained));
                var benchmark = new BenchmarkPanel(settings, PhoneAnalysis.BenchmarkWork, SendSurvey, SharingWords.BenchmarkRunNow, () =>
                {
                    survey.IsVisible = false;
                    Answered();
                });
                bool doneOffered = false;
                benchmark.Ended += () =>
                {
                    if (!doneOffered)
                    {
                        doneOffered = true;
                        survey.Children.Add(Screens.Choice("Done", () =>
                        {
                            survey.IsVisible = false;
                            Answered();
                        }));
                    }
                };
                survey.Children.Add(benchmark);
                survey.Children.Add(Screens.Dim(SharingWords.SurveyLater));
            }));
        }

        survey.Children.Insert(survey.Children.Count - SharingWords.SurveyChoices.Count, Screens.Dim(SharingWords.BenchmarkOffer));
        survey.Children.Add(Screens.Dim(SharingWords.SurveyLater));
        Content = Screens.Page(column);
    }
}
