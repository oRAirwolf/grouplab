using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 194 section 2: error reports sent by themselves once the person says yes, asked for by default, never
/// while the receiver is closed. Identical errors go once with a count, nothing typed goes in an automatic report, a report is never sent
/// twice, one that cannot go is kept for seven days, and a day's reports are capped. Every request goes to the recording outside world.
/// </summary>
public class Entry194Tests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    private static PostAnswer Took() => new(200, "{\"ok\":true,\"id\":\"x\"}");

    private static Exception Thrown(string message)
    {
        try
        {
            throw new ArgumentOutOfRangeException("index", message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ex;
        }
    }

    /// <summary>A window over its own log folder, with the given choice, the error receiver open or not, and the records written first.</summary>
    private static (MainWindow Window, string Root, DiagnosticLog Previous, DiagnosticLog Log) Open(ErrorReportChoice choice, bool open, Action<string> records)
    {
        Outside.Forget();
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry194-{Guid.NewGuid():N}");
        var log = new DiagnosticLog(Path.Combine(root, "logs"), verbose: false);
        var previous = DiagnosticLog.Current;
        DiagnosticLog.Current = log;
        records(log.Directory!);
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        if (choice != ErrorReportChoice.Unset)
        {
            store.SaveErrorChoice(choice);
        }

        var window = new MainWindow(store) { Width = 1400, Height = 900, ErrorsOpen = open };
        return (window, root, previous, log);
    }

    private static void Close((MainWindow Window, string Root, DiagnosticLog Previous, DiagnosticLog Log) opened)
    {
        opened.Window.Close();
        DiagnosticLog.Current = opened.Previous;
        opened.Log.Dispose();
        Outside.Forget();
        GroupLab.Tests.Support.Temp.Delete(opened.Root);
    }

    private static void Records(string folder)
    {
        for (int i = 0; i < 2; i++)
        {
            File.WriteAllText(Path.Combine(folder, $"crash-20260924-17000{i}-4242.json"),
                CrashReporter.Describe(Thrown(@"failed on C:\Users\someone\Pictures\Secret Range\IMG_20260920_150000.jpg"), DateTime.UtcNow, CrashReporter.Survived).ToJsonString());
        }

        File.WriteAllText(Path.Combine(folder, "crash-20260924-170100-4243.json"),
            CrashReporter.Describe(new InvalidOperationException("closed"), DateTime.UtcNow, CrashReporter.Closed).ToJsonString());
    }

    [AvaloniaFact]
    public async Task NothingIsSentWhileTheReceiverIsClosed()
    {
        var opened = Open(ErrorReportChoice.Always, open: false, Records);
        try
        {
            opened.Window.Show();
            Settle();
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync());
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync(asked: true));
            Assert.Empty(Outside.Reports);
            opened.Window.ShowFirstRunIfDue();
            Assert.False(opened.Window.FirstRunShown);
            opened.Window.FillErrorSettings();
            Assert.Contains(opened.Window.ErrorSettingsText, t => t.Contains("not open yet", StringComparison.Ordinal));
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task SentAutomaticallyTheSameErrorGoesOnceWithItsCountAndNothingTyped()
    {
        var opened = Open(ErrorReportChoice.Always, open: true, Records);
        try
        {
            Outside.ReportAnswer = _ => Took();
            opened.Window.Show();
            Settle();
            await opened.Window.SendWaitingErrorsAsync();
            Settle();
            Assert.Equal(2, Outside.Reports.Count);
            var reports = Outside.Reports.Select(r => JsonNode.Parse(r.Report)!.AsObject()).ToList();
            var survived = reports.Single(r => (string?)r["kind"] == "survived");
            Assert.Equal(2, (int)survived["count"]!);
            Assert.All(reports, r =>
            {
                Assert.Equal("automatic", (string?)r["made"]);
                Assert.False(r.ContainsKey("description"));
                Assert.Equal("grouplab-error-report-1", (string?)r["schema"]);
            });
            Assert.All(Outside.Reports, r =>
            {
                // Section 2.2: nothing of the photograph the session had open, its name, its folder or anything read out of it.
                Assert.DoesNotContain("IMG_20260920_150000", r.Report, StringComparison.Ordinal);
                Assert.DoesNotContain("Secret Range", r.Report, StringComparison.Ordinal);
                Assert.DoesNotContain("someone", r.Report, StringComparison.Ordinal);
                Assert.DoesNotContain("GPS", r.Report, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Latitude", r.Report, StringComparison.OrdinalIgnoreCase);
            });
            Assert.All(Outside.Reports, r => Assert.Equal(GroupLab.Core.Publication.ReceiverTerms.Current.ErrorReceiver, r.Address));

            // Never twice: a second pass has nothing to send.
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync());
            Assert.Equal(2, Outside.Reports.Count);
            Assert.Empty(CrashReporter.PendingCrashes(opened.Log.Directory));
            Assert.Equal(2, opened.Window.SettingsStore.LoadErrorsSent(DateTime.UtcNow).InAll);
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task AskingEachTimeSendsOnlyWhenSendIsPressed()
    {
        var opened = Open(ErrorReportChoice.Unset, open: true, Records);
        try
        {
            Outside.ReportAnswer = _ => Took();
            opened.Window.Show();
            Settle();
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync());
            Assert.Empty(Outside.Reports);
            var send = opened.Window.CrashBanner.GetLogicalDescendants().OfType<Button>().Single(b => b.Content as string == "Send the error report");
            send.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (int i = 0; i < 20 && Outside.Reports.Count < 2; i++)
            {
                await Task.Delay(25);
                Settle();
            }

            Assert.Equal(2, Outside.Reports.Count);
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task NeverSendsNothing()
    {
        var opened = Open(ErrorReportChoice.Never, open: true, Records);
        try
        {
            Outside.ReportAnswer = _ => Took();
            opened.Window.Show();
            Settle();
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync(asked: true));
            Assert.Empty(Outside.Reports);
            Assert.DoesNotContain(opened.Window.CrashBanner.GetLogicalDescendants().OfType<Button>(), b => b.Content as string == "Send the error report");
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task AReportThatCannotGoIsKeptThenSentAndAWeekOldOneIsLetGo()
    {
        var opened = Open(ErrorReportChoice.Always, open: true, folder =>
        {
            Records(folder);
            string old = Path.Combine(folder, "crash-20260901-120000-4244.json");
            File.WriteAllText(old, CrashReporter.Describe(Thrown("old"), DateTime.UtcNow.AddDays(-8), CrashReporter.Survived).ToJsonString());
            File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-8));
        });
        try
        {
            // Opening sends what is waiting; nothing answers, so both reports are kept.
            Outside.ReportAnswer = null;
            opened.Window.Show();
            for (int i = 0; i < 20 && Outside.Reports.Count < 2; i++)
            {
                await Task.Delay(25);
                Settle();
            }

            Assert.Equal(2, Outside.Reports.Count);
            Assert.Equal(0, opened.Window.SettingsStore.LoadErrorsSent(DateTime.UtcNow).InAll);

            Outside.ReportAnswer = _ => Took();
            await opened.Window.SendWaitingErrorsAsync();
            Assert.Equal(4, Outside.Reports.Count);
            Assert.DoesNotContain(Outside.Reports, r => r.Report.Contains("\"old\"", StringComparison.Ordinal));
            Assert.True(ErrorReports.IsSent(Path.Combine(opened.Log.Directory!, "crash-20260901-120000-4244.json")));
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public async Task ADaysReportsAreCapped()
    {
        var opened = Open(ErrorReportChoice.Always, open: true, Records);
        try
        {
            Outside.ReportAnswer = _ => Took();
            opened.Window.SettingsStore.AddErrorsSent(GroupLab.Core.Publication.ReceiverTerms.Current.MaxErrorReportsPerDay, DateTime.UtcNow);
            opened.Window.Show();
            Settle();
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync());
            Assert.Empty(Outside.Reports);
        }
        finally
        {
            Close(opened);
        }
    }

    [AvaloniaFact]
    public void TheFirstRunScreenAsksAndSettingsShowsTheAnswer()
    {
        var opened = Open(ErrorReportChoice.Unset, open: true, _ => { });
        try
        {
            opened.Window.Show();
            Settle();
            opened.Window.ShowFirstRunIfDue();
            Assert.True(opened.Window.FirstRunShown);
            opened.Window.PressSend("Send them automatically");
            Settle();
            Assert.False(opened.Window.FirstRunShown);
            Assert.Equal(ErrorReportChoice.Always, opened.Window.SettingsStore.LoadErrorChoice());
            opened.Window.FillErrorSettings();
            var text = opened.Window.ErrorSettingsText.ToList();
            Assert.Contains("Send them automatically", text);
            Assert.Contains("• " + ErrorReports.WhatIsSent[^1], text);
            Assert.Contains("No error reports have been sent from this computer.", text);
        }
        finally
        {
            Close(opened);
        }
    }

    /// <summary>The application, the receiver and the worker read one schema, by the same name and the same fields.</summary>
    [Fact]
    public void TheApplicationTheReceiverAndTheWorkerReadOneSchema()
    {
        string repository = Entry109Tests.Repository();
        string php = File.ReadAllText(Path.Combine(repository, "website", "api", "error-report.php"));
        string worker = File.ReadAllText(Path.Combine(repository, "website", "server", "grouplab-error-worker.py"));
        Assert.Contains($"const SCHEMA          = '{ErrorReports.Schema}';", php, StringComparison.Ordinal);
        Assert.Contains($"\"{ErrorReports.Schema}\"", worker, StringComparison.Ordinal);
        foreach (string field in new[] { "report_id", "kind", "made", "count", "app", "environment", "exceptions", "last_actions" })
        {
            Assert.Contains($"'{field}'", php, StringComparison.Ordinal);
        }
    }
}
