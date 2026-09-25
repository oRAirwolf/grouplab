using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App.Diagnostics;

/// <summary>
/// The error reports waiting to go, NOTES-FROM-PLANNING.md entry 194 section 2, and entry 219 item A3's "queues shared with the desktop's
/// code": the desktop's window and the Android application both send through this, so the two follow the same choice, the same day's
/// budget and the same grouping. Nothing here draws anything.
/// </summary>
public sealed class ErrorQueue(AppSettingsStore settings)
{
    private readonly HashSet<string> sentThisSession = new(StringComparer.Ordinal);

    /// <summary>
    /// Sends what is waiting, grouped. With the choice Always it goes by itself; asked, it goes once; Never and a closed receiver send
    /// nothing. Returns how many went.
    /// </summary>
    public async Task<int> SendWaitingAsync(bool open, bool asked, CancellationToken token)
    {
        var choice = settings.LoadErrorChoice();
        if (!open || choice == ErrorReportChoice.Never || (!asked && choice != ErrorReportChoice.Always))
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var groups = new List<IReadOnlyList<string>>();
        foreach (var group in ErrorReports.Waiting(DiagnosticLog.Current.Directory, now))
        {
            if (sentThisSession.Contains(ErrorReports.Key(group[0])))
            {
                foreach (string record in group)
                {
                    ErrorReports.MarkSent(record, "repeat");
                }

                continue;
            }

            groups.Add(group);
        }

        int budget = ReceiverTerms.Current.MaxErrorReportsPerDay - settings.LoadErrorsSent(now).Today;
        if (groups.Count == 0 || budget <= 0)
        {
            return 0;
        }

        int sent = await ErrorReports.SendAsync(TheOutsideWorld.Current, ReceiverTerms.Current.ErrorReceiver, groups, budget, ActionsFor, token);
        foreach (var group in groups.Where(g => ErrorReports.IsSent(g[0])))
        {
            sentThisSession.Add(ErrorReports.Key(group[0]));
            foreach (string record in group)
            {
                CrashReporter.MarkHandled(record);
            }
        }

        if (sent > 0)
        {
            settings.AddErrorsSent(sent, now);
        }

        return sent;
    }

    /// <summary>The names of the last things done in the run a record came from.</summary>
    private static IReadOnlyList<string> ActionsFor(string record) => ErrorReports.LastActions(ReportPackage.LogsFor(DiagnosticLog.Current, record).RunLog);
}
