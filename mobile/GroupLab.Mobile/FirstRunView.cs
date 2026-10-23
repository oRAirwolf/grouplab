using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using GroupLab.App;
using GroupLab.App.Theme;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Survey;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3 and entry 208: the first run's questions in the desktop's order and words
/// (<see cref="SharingWords"/>), each with what is sent and its own answer. Nothing is chosen for the person (entry 203 section 3), a
/// question is asked only while the project takes what it asks about, and the screen gives way to the application when the last one
/// asked has been answered or skipped. The survey, entry 208's third question, joins them when its receiver opens (docs/SURVEY.md).
/// Entry 465 item 5A: one question a step, as on the computer (<see cref="FirstRunSteps"/>), with "Question N of M" and the step bar at
/// the top and Back, Skip for now and Next at the bottom; Skip for now records nothing, and a skipped question is asked at the next start.
/// </summary>
public sealed class FirstRunView : UserControl
{
    /// <summary>Whether any question is still open and unanswered.</summary>
    public static bool Due(AppSettingsStore settings) => ScopeDue(settings) || TargetsDue(settings) || ErrorsDue(settings) || SurveyDue(settings);

    /// <summary>Entry 294 section 1: "Is your scope in mil or MOA?" until it is answered, asked of an existing install once too.</summary>
    internal static bool ScopeDue(AppSettingsStore settings) => settings.LoadScopeAnswer() == ScopeAnswer.Unset;

    private static bool SurveyDue(AppSettingsStore settings) => Shell.SurveyOpen && settings.LoadSurveyChoice() == SurveyChoice.Unset;

    private static bool TargetsDue(AppSettingsStore settings) => Shell.TargetsOpen && settings.LoadSending().Choice == SendingChoice.Unset || EverythingDue(settings);

    /// <summary>Entry 357 section 1: somebody who chose every target automatically is asked once about everything they open.</summary>
    private static bool EverythingDue(AppSettingsStore settings) => Shell.TargetsOpen && SharingSwitches.EverythingOpen && settings.EverythingQuestionDue();

    private static bool ErrorsDue(AppSettingsStore settings) => Shell.ErrorsOpen
        && (settings.LoadErrorChoice() == ErrorReportChoice.Unset || SharingSwitches.FullLogOpen && settings.ErrorWordingDue());

    /// <summary>Sends the survey report when one is due, after a benchmark the person ran.</summary>
    internal static Task SendSurvey() => Phone.Survey?.SendDueAsync(Shell.SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None) ?? Task.CompletedTask;

    public FirstRunView(AppSettingsStore settings, Action done)
    {
        var column = new StackPanel { Spacing = 24 };

        // Entry 246, look B: the page's own title, then the question showing with what it sends on a card of its own.
        column.Children.Add(Screens.Title("Before you start"));
        var scope = new StackPanel { Spacing = 8, IsVisible = false };
        var targets = new StackPanel { Spacing = 8, IsVisible = false };
        var errors = new StackPanel { Spacing = 8, IsVisible = false };
        var survey = new StackPanel { Spacing = 8, IsVisible = false };
        var asked = new List<StackPanel>();
        foreach (var (part, due) in new[] { (scope, ScopeDue(settings)), (targets, TargetsDue(settings)), (errors, ErrorsDue(settings)), (survey, SurveyDue(settings)) })
        {
            column.Children.Add(part);
            if (due)
            {
                asked.Add(part);
            }
        }

        // Entry 273: the printer check, offered once under the last question and never holding the page open. Its own button is not the
        // steps' Skip for now, so it says what it skips, in the computer's words.
        StackPanel? printer = null;
        if (settings.LoadPrinters().Count == 0 && !settings.LoadPrinterOffered())
        {
            settings.SavePrinterOffered();
            printer = new StackPanel { Spacing = 8 };
            printer.Children.Add(Screens.Heading("Check your printer"));
            printer.Children.Add(Screens.Dim("Printers often print a little small. Check once, with a card and one photo, a caliper, a ruler or a scanner, and every photo of a GroupLab sheet from that printer measures in real inches. It is also under Settings, Printers."));
            printer.Children.Add(Screens.Choice("Skip for now, run it later from Settings", () =>
            {
                printerGone = true;
                printer!.IsVisible = false;
            }));
            column.Children.Add(printer);
        }

        var flow = new FirstRunSteps(asked.Count);
        var page = (ScrollViewer)Screens.Page(column);
        var of = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Classes = { PhoneStyles.Dim } };
        var bar = new Avalonia.Controls.Primitives.UniformGrid { Columns = asked.Count, Rows = 1, Margin = new Thickness(0, 4, 0, 0) };
        // The buttons are made before the step they show, so they reach it through this.
        Action show = () => { };
        var back = Screens.BackButton(FirstRunSteps.Back, () =>
        {
            flow.MoveBack();
            show();
        }).Id("first-run-back");
        var skip = Screens.Choice(FirstRunSteps.Skip, () =>
        {
            // Nothing is recorded for a skipped question: its setting stays unset, and it is asked again at the next start.
            DiagnosticLog.Info("first-run.skipped", ("question", flow.At + 1));
            flow.SkipForNow();
            show();
        }).Id("first-run-skip");
        var next = Screens.Primary(FirstRunSteps.Next, () =>
        {
            flow.MoveNext();
            show();
        }).Id("first-run-next");
        void ShowStep()
        {
            if (flow.Finished)
            {
                done();
                return;
            }

            for (int i = 0; i < asked.Count; i++)
            {
                asked[i].IsVisible = i == flow.At;
            }

            if (printer is not null)
            {
                printer.IsVisible = flow.IsLast && !printerGone;
            }

            of.Text = flow.Of;
            var palette = Tokens.For(ActualThemeVariant);
            bar.Children.Clear();
            for (int i = 0; i < flow.Count; i++)
            {
                var segment = new Border
                {
                    Height = 4,
                    Margin = new Thickness(2, 0),
                    CornerRadius = new CornerRadius(2),
                    Background = new SolidColorBrush(i == flow.At ? palette.Amber : flow.Answered(i) ? palette.Teal : palette.Line2),
                };
                AutomationProperties.SetName(segment, flow.BarName(i));
                bar.Children.Add(segment);
            }

            back.IsEnabled = flow.CanBack;
            skip.IsEnabled = flow.CanSkip;
            next.IsEnabled = flow.CanNext;
            page.Offset = default;
        }

        void Answered()
        {
            flow.Answer();
            ShowStep();
        }

        show = ShowStep;

        // Entry 294 section 1: the scope's unit first, with the length beside it; nothing about the angle is guessed, and nothing is chosen
        // for the person (entry 203 section 3): a length left unchosen is the region's.
        scope.Children.Add(Screens.Heading(GroupLab.Core.Marking.UnitSettings.ScopeQuestion));
        scope.Children.Add(Screens.Dim(GroupLab.Core.Marking.UnitSettings.ScopeQuestionSays));
        var inInches = Screens.Radio("firstRunLength", "Sizes in inches", false);
        var inMillimeters = Screens.Radio("firstRunLength", "Sizes in millimeters", false);
        scope.Children.Add(inInches);
        scope.Children.Add(inMillimeters);
        foreach (var (answer, words) in new[] { (ScopeAnswer.Mil, "Mil"), (ScopeAnswer.Moa, "MOA"), (ScopeAnswer.Both, GroupLab.Core.Marking.UnitSettings.ScopeBoth) })
        {
            scope.Children.Add(Screens.Choice(words, () =>
            {
                settings.SaveScopeAnswer(answer, inMillimeters.IsChecked == true ? GroupLab.Core.Marking.LinearUnit.Millimetre
                    : inInches.IsChecked == true ? GroupLab.Core.Marking.LinearUnit.Inch : AppSettingsStore.LengthForRegion(AppSettingsStore.Region()));
                DiagnosticLog.Info("scope.first-run", ("answer", answer.ToString()));
                scope.IsVisible = false;
                Answered();
            }));
        }

        scope.Children.Add(Screens.Dim("You can change either in Settings, under Units, and give each rifle its own scope unit and click."));

        var terms = ReceiverTerms.Current;
        bool everythingDue = EverythingDue(settings);
        targets.Children.Add(Screens.Heading(everythingDue ? SharingWords.EverythingQuestion : SharingWords.TargetsQuestion));
        if (everythingDue)
        {
            targets.Children.Add(Screens.Line(SharingWords.EverythingQuestionSays));
        }

        targets.Children.Add(Screens.Card([Screens.Dim(SharingWords.TargetsIntroNow), .. SharingWords.TargetsWhatIsSentNow.Select(line => (Control)Screens.Dim("• " + line))]));
        if (SharingSwitches.EverythingOpen)
        {
            targets.Children.Add(Screens.Dim(SharingWords.EverythingWaitsForWifi));
        }

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
                // Entry 357: nothing is chosen for the person, and somebody answering the one-time question keeps the level they chose before.
                ConsentLevel? level = testing.IsChecked == true ? ConsentLevel.Testing : publishable.IsChecked == true ? ConsentLevel.Publishable
                    : everythingDue ? settings.LoadSending().Level : null;
                if (choice is SendingChoice.Always or SendingChoice.Everything && level is null)
                {
                    why.Text = SharingWords.LevelFirst;
                    return;
                }

                settings.SaveSending(choice, level);
                if (SharingSwitches.EverythingOpen)
                {
                    settings.SaveEverythingAsked();
                }

                DiagnosticLog.Info("send.first-run", ("choice", choice.ToString()), ("level", level?.ToString()));
                targets.IsVisible = false;
                Answered();
            }));
        }

        targets.Children.Add(Screens.Dim(SharingWords.TargetsLater));

        errors.Children.Add(Screens.Heading(SharingWords.ErrorsQuestion));
        if (SharingSwitches.FullLogOpen && settings.ErrorWordingDue())
        {
            errors.Children.Add(Screens.Line(SharingWords.ErrorsWordingChanged));
        }

        errors.Children.Add(Screens.Card([Screens.Dim(SharingWords.ErrorsIntroNow), .. ErrorReports.WhatIsSentNow.Select(line => (Control)Screens.Dim("• " + line))]));

        foreach (var (choice, words) in SharingWords.ErrorChoices)
        {
            errors.Children.Add(Screens.Choice(words, () =>
            {
                settings.SaveErrorChoice(choice, SharingSwitches.FullLogOpen);
                DiagnosticLog.Info("errors.first-run", ("choice", choice.ToString()));
                errors.IsVisible = false;
                _ = Phone.SendWaitingErrorsAsync();
                Answered();
            }));
        }

        errors.Children.Add(Screens.Dim(SharingWords.ErrorsLater));

        // Entry 208: the survey, third, in the desktop's words; somebody who answered the other two before is told their answers are kept.
        if (!asked.Contains(targets) && !asked.Contains(errors))
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
                }, runHeight: Screens.Touch);
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

        // The top: where the person is, and the bar, answered in teal and the one showing in amber. The bottom: the three ways on.
        var head = new StackPanel { Spacing = 4, Margin = new Thickness(16, 12, 16, 0), Children = { of, bar } };
        var foot = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 8, Margin = new Thickness(16, 8, 16, 12), Children = { back, skip, next } };
        Grid.SetColumn(skip, 1);
        Grid.SetColumn(next, 2);
        var whole = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Children = { head, page, foot } };
        Grid.SetRow(page, 1);
        Grid.SetRow(foot, 2);
        Content = whole;
        ShowStep();
        ActualThemeVariantChanged += (_, _) =>
        {
            if (!flow.Finished)
            {
                ShowStep();
            }
        };
    }

    private bool printerGone;
}
