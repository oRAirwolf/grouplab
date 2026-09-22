using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 41 sections 5 and 8, and entry 45 section 2: an exception thrown in a click handler is caught and leaves a
/// crash record of the agreed shape, naming the thrown type, carrying no path, and carrying the stages of an analysis in flight; and a crash
/// not yet dealt with is offered on the next launch until it is.
/// </summary>
public class CrashTests
{
    private static (DiagnosticLog Log, string Root) NewLog()
    {
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-crash-{Guid.NewGuid():N}");
        return (new DiagnosticLog(Path.Combine(root, "logs"), verbose: false), root);
    }

    [AvaloniaFact]
    public void AnExceptionThrownFromAClickHandlerLeavesACrashRecordThatNamesIt()
    {
        var (log, root) = NewLog();
        var previous = DiagnosticLog.Current;
        DiagnosticLog.Current = log;
        try
        {
            using (CrashReporter.Install(log))
            {
                var button = new Button();
                button.Click += (_, _) => throw new InvalidOperationException(@"thrown from a click handler, reading C:\Users\Jane Q\Range photos\target.jpg");
                DiagnosticLog.Info("print.select", ("sheet", "GL-CF25-LTR.gltd.json"));
                Dispatcher.UIThread.Post(() => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                Dispatcher.UIThread.RunJobs();
            }

            string crash = Assert.Single(Directory.GetFiles(log.Directory!, "crash-*.json"));
            Assert.Matches(@"^crash-\d{8}-\d{6}-\d+\.json$", Path.GetFileName(crash));
            var record = JsonNode.Parse(File.ReadAllText(crash))!;
            Assert.Equal(1, (int)record["schema"]!);
            Assert.Equal("System.InvalidOperationException", (string?)record["exceptions"]![0]!["type"]);
            Assert.Equal("thrown from a click handler, reading <path>", (string?)record["exceptions"]![0]!["message"]);
            Assert.Equal("print.select", (string?)record["last_action"]);
            Assert.Empty(record["stages"]!.AsArray());
            Assert.False(string.IsNullOrEmpty((string?)record["app"]!["version"]));
            Assert.DoesNotContain("Jane", record.ToJsonString(), StringComparison.Ordinal);

            log.Dispose();
            string text = File.ReadAllText(log.FilePath!);
            Assert.Contains("ERROR  app.crash", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Jane", text, StringComparison.Ordinal);
        }
        finally
        {
            DiagnosticLog.Current = previous;
            log.Dispose();
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }

    [Fact]
    public void ACrashRecordCarriesTheWholeChainAndTheStagesOfAnAnalysisInFlight()
    {
        var trace = new TraceRecorder();
        using (var stage = trace.Begin("S1.markers"))
        {
            stage.Metric("markers", 34, "count");
            stage.Done(StageStatus.Ok, "34 of 34 markers found");
        }

        CrashReporter.InFlight = trace;
        try
        {
            var record = CrashReporter.Describe(new InvalidOperationException("outer", new IOException("inner")), DateTime.UtcNow);
            Assert.Equal(["System.InvalidOperationException", "System.IO.IOException"], record["exceptions"]!.AsArray().Select(e => (string?)e!["type"]));
            var stage = Assert.Single(record["stages"]!.AsArray())!;
            Assert.Equal("S1.markers", (string?)stage["stage"]);
            Assert.Equal("Ok", (string?)stage["status"]);
            Assert.Equal(34, (double)stage["metrics"]![0]!["value"]!);
        }
        finally
        {
            CrashReporter.InFlight = null;
        }
    }

    [AvaloniaFact]
    public void ACrashNotYetDealtWithIsOfferedOnTheNextLaunchUntilItIsDismissed()
    {
        var (log, root) = NewLog();
        var previous = DiagnosticLog.Current;
        DiagnosticLog.Current = log;
        File.WriteAllText(Path.Combine(log.Directory!, "crash-20260915-064212-4242.json"), CrashReporter.Describe(new InvalidOperationException("earlier"), DateTime.UtcNow).ToJsonString());
        try
        {
            var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
            store.SaveUnits(UnitSettings.Imperial);
            var window = new MainWindow(store) { Width = 1400, Height = 900 };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("closed unexpectedly", window.CrashBannerText, StringComparison.Ordinal);

            window.CrashBanner.GetLogicalDescendants().OfType<Button>().Single(b => (b.Content as string) == "Dismiss").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Empty(CrashReporter.PendingCrashes(log.Directory));
            Assert.Equal("", window.CrashBannerText);
            window.Close();

            var again = new MainWindow(store) { Width = 1400, Height = 900 };
            again.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("", again.CrashBannerText);
            again.Close();
        }
        finally
        {
            DiagnosticLog.Current = previous;
            log.Dispose();
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }
}
