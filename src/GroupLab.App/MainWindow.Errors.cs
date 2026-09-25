using System.Globalization;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// Error reports sent by themselves, NOTES-FROM-PLANNING.md entry 194 section 2. Entry 192 is why: five "crashes" that were one caught
/// error and one button, seen only because Unholy happened to make a report. Once the person has said yes, a report of each error goes to
/// grouplab.org, which turns it into an issue in a private repository; asking each time, the default, offers it where the crash banner
/// already offers a report; never sends nothing. Nothing is asked and nothing is sent while the receiver is closed.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel errorSettings = new() { Spacing = Tokens.Space8 };

    /// <summary>The errors already sent in this session: a repeat of one is counted, not sent again (entry 194 section 2.5).</summary>
    private readonly HashSet<string> errorsSentThisSession = new(StringComparer.Ordinal);

    private DispatcherTimer? errorSendSoon;

    /// <summary>What a new window starts with in place of limits.json's switch; the test assembly sets it off, as it does for sending.</summary>
    internal static bool? ErrorsOpenByDefault { get; set; }

    private bool errorsOpen = ErrorsOpenByDefault ?? ReceiverTerms.Current.ErrorReportsOpen;

    /// <summary>Whether the error report receiver is open: the build's limits.json, which the tests can override. The banner and Settings follow it.</summary>
    internal bool ErrorsOpen
    {
        get => errorsOpen;
        set
        {
            errorsOpen = value;
            ShowPendingCrashes();
            FillErrorSettings();
        }
    }

    /// <summary>
    /// Sends what is waiting, grouped: at start, a minute after an error this session, or when the person presses Send on the banner. With
    /// the choice Always it goes by itself; asked, it goes once; Never and a closed receiver send nothing. Never a dialog.
    /// </summary>
    internal async Task<int> SendWaitingErrorsAsync(bool asked = false)
    {
        var choice = settingsStore.LoadErrorChoice();
        if (!ErrorsOpen || choice == ErrorReportChoice.Never || (!asked && choice != ErrorReportChoice.Always))
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var groups = new List<IReadOnlyList<string>>();
        foreach (var group in ErrorReports.Waiting(DiagnosticLog.Current.Directory, now))
        {
            if (errorsSentThisSession.Contains(ErrorReports.Key(group[0])))
            {
                foreach (string record in group)
                {
                    ErrorReports.MarkSent(record, "repeat");
                }

                continue;
            }

            groups.Add(group);
        }

        int budget = ReceiverTerms.Current.MaxErrorReportsPerDay - settingsStore.LoadErrorsSent(now).Today;
        if (groups.Count == 0 || budget <= 0)
        {
            return 0;
        }

        int sent = await ErrorReports.SendAsync(TheOutsideWorld.Current, ReceiverTerms.Current.ErrorReceiver, groups, budget, ActionsFor, CancellationToken.None);
        foreach (var group in groups.Where(g => ErrorReports.IsSent(g[0])))
        {
            errorsSentThisSession.Add(ErrorReports.Key(group[0]));
            foreach (string record in group)
            {
                CrashReporter.MarkHandled(record);
            }
        }

        if (sent > 0)
        {
            settingsStore.AddErrorsSent(sent, now);
            toaster.Say(sent == 1 ? "An error report went to the project." : string.Create(CultureInfo.InvariantCulture, $"{sent} error reports went to the project."));
        }

        FillErrorSettings();
        return sent;
    }

    /// <summary>The names of the last things done in the run a record came from.</summary>
    private static IReadOnlyList<string> ActionsFor(string record) => ErrorReports.LastActions(ReportPackage.LogsFor(DiagnosticLog.Current, record).RunLog);

    /// <summary>An error this session: sent a minute later, so a burst of the same one goes as one report with its count.</summary>
    private void ErrorRecorded(object? sender, string record)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!ErrorsOpen || settingsStore.LoadErrorChoice() != ErrorReportChoice.Always)
            {
                return;
            }

            errorSendSoon ??= new DispatcherTimer(TimeSpan.FromMinutes(1), DispatcherPriority.Background, (_, _) =>
            {
                errorSendSoon?.Stop();
                _ = SendWaitingErrorsAsync();
            });
            errorSendSoon.Stop();
            errorSendSoon.Start();
        });
    }

    /// <summary>The first run screen's error report question, beside the target question.</summary>
    private void FillFirstRunErrors(StackPanel part, Action answered)
    {
        part.Children.Add(new TextBlock { Text = "Send error reports to the project?", Classes = { AppStyles.Title } });
        part.Children.Add(Line("When GroupLab hits an error, a report of it can go to the project, where it is fixed. This is what a report holds:"));
        foreach (string line in ErrorReports.WhatIsSent)
        {
            part.Children.Add(Line("• " + line));
        }

        void Choose(ErrorReportChoice choice)
        {
            settingsStore.SaveErrorChoice(choice);
            DiagnosticLog.Info("errors.first-run", ("choice", choice.ToString()));
            part.IsVisible = false;
            answered();
            FillErrorSettings();
        }

        part.Children.Add(Row(Button("Send them automatically", () => Choose(ErrorReportChoice.Always)), Button("Ask me each time", () => Choose(ErrorReportChoice.Ask)), Button("Never send them", () => Choose(ErrorReportChoice.Never))));
        part.Children.Add(Line("You can change this at any time in Settings, under Error reports."));
    }

    /// <summary>Entry 194 section 2.1: Settings' own Error reports section, the same setting as the first run screen.</summary>
    private void BuildErrorSettings(StackPanel column)
    {
        column.Children.Add(Ruled("Error reports"));
        column.Children.Add(errorSettings);
        FillErrorSettings();
    }

    internal void FillErrorSettings()
    {
        errorSettings.Children.Clear();
        if (!ErrorsOpen)
        {
            errorSettings.Children.Add(Line("Sending error reports to the project is not open yet. A crash can still be reported by hand, from the banner or Report a problem."));
            return;
        }

        var choice = settingsStore.LoadErrorChoice();
        var choices = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var (value, words) in new[] { (ErrorReportChoice.Always, "Send them automatically"), (ErrorReportChoice.Ask, "Ask me each time"), (ErrorReportChoice.Never, "Never send them") })
        {
            var radio = new RadioButton { GroupName = "errorChoice", Content = words, IsChecked = choice == value || (value == ErrorReportChoice.Ask && choice == ErrorReportChoice.Unset) };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && settingsStore.LoadErrorChoice() != value)
                {
                    settingsStore.SaveErrorChoice(value);
                    DiagnosticLog.Info("errors.choice", ("choice", value.ToString()));
                }
            };
            choices.Children.Add(radio);
        }

        errorSettings.Children.Add(choices);
        errorSettings.Children.Add(FieldLabel("What a report holds"));
        foreach (string line in ErrorReports.WhatIsSent)
        {
            errorSettings.Children.Add(Line("• " + line));
        }

        int inAll = settingsStore.LoadErrorsSent(DateTime.UtcNow).InAll;
        errorSettings.Children.Add(Line(inAll == 0
            ? "No error reports have been sent from this computer."
            : string.Create(CultureInfo.InvariantCulture, $"{inAll} error report{(inAll == 1 ? " has" : "s have")} been sent from this computer.")));
    }

    /// <summary>The Error reports section's words, for the headless tests.</summary>
    internal IEnumerable<string> ErrorSettingsText => errorSettings.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")
        .Concat(errorSettings.GetLogicalDescendants().OfType<RadioButton>().Select(r => r.Content as string ?? ""));
}
