using Avalonia.Controls;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3 and entry 208: the first run's questions, together on one screen, in the desktop's order and
/// words (<see cref="SharingWords"/>), each with what is sent and its own answer. Nothing is chosen for the person (entry 203 section 3), a
/// question is asked only while the project takes what it asks about, and the screen gives way to the application when each one asked
/// has been answered. The survey, entry 208's third question, joins them when its receiver opens (docs/SURVEY.md).
/// </summary>
public sealed class FirstRunView : UserControl
{
    /// <summary>Whether any question is still open and unanswered.</summary>
    public static bool Due(AppSettingsStore settings) => TargetsDue(settings) || ErrorsDue(settings);

    private static bool TargetsDue(AppSettingsStore settings) => Shell.TargetsOpen && settings.LoadSending().Choice == SendingChoice.Unset;

    private static bool ErrorsDue(AppSettingsStore settings) => Shell.ErrorsOpen && settings.LoadErrorChoice() == ErrorReportChoice.Unset;

    public FirstRunView(AppSettingsStore settings, Action done)
    {
        var column = new StackPanel { Spacing = 24 };
        var targets = new StackPanel { Spacing = 8, IsVisible = TargetsDue(settings) };
        var errors = new StackPanel { Spacing = 8, IsVisible = ErrorsDue(settings) };
        column.Children.Add(targets);
        column.Children.Add(errors);
        void Answered()
        {
            if (!targets.IsVisible && !errors.IsVisible)
            {
                done();
            }
        }

        var terms = ReceiverTerms.Current;
        targets.Children.Add(Screens.Heading(SharingWords.TargetsQuestion));
        targets.Children.Add(Screens.Line(SharingWords.TargetsIntro));
        foreach (string line in TargetPackages.WhatIsSent)
        {
            targets.Children.Add(Screens.Line("• " + line));
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

        targets.Children.Add(Screens.Line(SharingWords.TargetsLater));

        errors.Children.Add(Screens.Heading(SharingWords.ErrorsQuestion));
        errors.Children.Add(Screens.Line(SharingWords.ErrorsIntro));
        foreach (string line in ErrorReports.WhatIsSent)
        {
            errors.Children.Add(Screens.Line("• " + line));
        }

        foreach (var (choice, words) in SharingWords.ErrorChoices)
        {
            errors.Children.Add(Screens.Choice(words, () =>
            {
                settings.SaveErrorChoice(choice);
                DiagnosticLog.Info("errors.first-run", ("choice", choice.ToString()));
                errors.IsVisible = false;
                _ = App.SendWaitingErrorsAsync();
                Answered();
            }));
        }

        errors.Children.Add(Screens.Line(SharingWords.ErrorsLater));
        Content = Screens.Page(column);
    }
}
