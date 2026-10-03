using System.IO.Compression;
using System.Text.Json.Nodes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Updates;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 357 section 2: an automatic error report carries the log package Report a problem builds, in both states
/// of its switch. Off, which is how the build ships: a report is what it was, with no package, whatever the settings say. On: a choice made
/// under the thinner wording still sends only what it promised and is asked again; one made under the new wording sends the package first,
/// with typed text replaced by its length, and the report names it; and a picture GroupLab could not read is a report of its own, matched
/// to the picture by its code. Every request goes to the recording outside world.
/// </summary>
public class Entry357ReportTests
{
    private static RecordedOutsideWorld Outside => TestDefaults.Outside;

    private static void Settle() => Dispatcher.UIThread.RunJobs();

    /// <summary>Words a person typed, as an older build's log may hold them under the fields that carry typed text.</summary>
    private static readonly string[] Typed = ["Grandpa Joe's back forty", "my secret load notes", "Shirley at the range"];

    private static (MainWindow Window, string Root, DiagnosticLog Previous, DiagnosticLog Log) Open(bool fullLog, bool newWording)
    {
        Outside.Forget();
        SharingSwitches.FullLogOverride = fullLog;
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry357-{Guid.NewGuid():N}");
        var log = new DiagnosticLog(Path.Combine(root, "logs"), verbose: false);
        var previous = DiagnosticLog.Current;
        DiagnosticLog.Current = log;
        DiagnosticLog.Info("library.save", ("sheet", Typed[0] + ".gltd.json"), ("shots", 5));
        DiagnosticLog.Info("equipment.save", ("notes", Typed[1]), ("distance", 300));
        DiagnosticLog.Info("store.decided", ("decided", Typed[2]));
        log.Flush();
        File.WriteAllText(Path.Combine(log.Directory!, $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}.json"),
            CrashReporter.Describe(new InvalidOperationException("a test error"), DateTime.UtcNow, CrashReporter.Survived).ToJsonString());
        var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
        store.SaveUnits(UnitSettings.Imperial);
        store.SaveErrorChoice(ErrorReportChoice.Always, fullLogWording: newWording);
        var window = new MainWindow(store) { Width = 1400, Height = 900, ErrorsOpen = true };
        return (window, root, previous, log);
    }

    private static void Close((MainWindow Window, string Root, DiagnosticLog Previous, DiagnosticLog Log) opened)
    {
        opened.Window.Close();
        DiagnosticLog.Current = opened.Previous;
        opened.Log.Dispose();
        Outside.Forget();
        SharingSwitches.FullLogOverride = null;
        GroupLab.Tests.Support.Temp.Delete(opened.Root);
    }

    [Fact]
    public void TypedTextInALogIsReplacedByItsLength()
    {
        string log = "2026-10-03T05:00:00.000Z  INFO   library.save   sheet=\"Grandpa Joe's back forty.gltd.json\" shots=5\n"
            + "2026-10-03T05:00:01.000Z  INFO   equipment.save notes=secret distance=300 caliber=.308\n";
        string clean = ReportPackage.WithoutTypedText(log);
        Assert.DoesNotContain("Grandpa", clean, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", clean, StringComparison.Ordinal);
        Assert.Contains("sheet=\"<34 characters typed>\"", clean, StringComparison.Ordinal);
        Assert.Contains("notes=\"<6 characters typed>\"", clean, StringComparison.Ordinal);
        Assert.Contains("distance=300", clean, StringComparison.Ordinal);
        Assert.Contains("caliber=.308", clean, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public async Task WithTheSwitchOffAReportIsWhatItWas()
    {
        var opened = Open(fullLog: false, newWording: true);
        try
        {
            Outside.ReportAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"id\":\"x\"}");
            Outside.PackageAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"reference\":\"2026-10-03_0000aaaa\"}");
            opened.Window.Show();
            Settle();
            await opened.Window.SendWaitingErrorsAsync();
            Assert.Empty(Outside.Packages);
            var report = JsonNode.Parse(Assert.Single(Outside.Reports).Report)!.AsObject();
            Assert.False(report.ContainsKey("package"));
            Assert.Contains("nothing typed into them", string.Join(" ", ErrorReports.WhatIsSentNow), StringComparison.Ordinal);
        }
        finally
        {
            Close(opened);
        }
    }

    /// <summary>A choice made under the thinner wording sends only what it promised, and is asked again before anything larger goes.</summary>
    [AvaloniaFact]
    public async Task AChoiceMadeUnderTheOldWordingSendsNothingLargerAndIsAskedAgain()
    {
        var opened = Open(fullLog: true, newWording: false);
        try
        {
            Outside.ReportAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"id\":\"x\"}");
            opened.Window.Show();
            Settle();
            await opened.Window.SendWaitingErrorsAsync();
            Assert.Empty(Outside.Packages);
            Assert.Single(Outside.Reports);
            Assert.True(opened.Window.SettingsStore.ErrorWordingDue());
            opened.Window.ShowFirstRunIfDue();
            Settle();
            Assert.True(opened.Window.FirstRunShown);
        }
        finally
        {
            Close(opened);
        }
    }

    /// <summary>The section's own test: a report built from a log holding typed notes carries none of them, and carries the log.</summary>
    [AvaloniaFact]
    public async Task UnderTheNewWordingTheLogGoesFirstWithNothingTyped()
    {
        var opened = Open(fullLog: true, newWording: true);
        try
        {
            Outside.ReportAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"id\":\"x\"}");
            Outside.PackageAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"reference\":\"2026-10-03_0000aaaa\"}");
            opened.Window.Show();
            Settle();
            await opened.Window.SendWaitingErrorsAsync();
            var (address, zip) = Assert.Single(Outside.Packages);
            Assert.Equal(GroupLab.Core.Publication.ReceiverTerms.Current.CrashReceiver, address);
            var report = JsonNode.Parse(Assert.Single(Outside.Reports).Report)!;
            Assert.Equal("2026-10-03_0000aaaa", (string?)report["package"]);

            using var archive = new ZipArchive(new MemoryStream(zip));
            Assert.All(archive.Entries, e => Assert.True(ReportPackage.IsPermitted(e.Name), e.Name));
            Assert.Contains(archive.Entries, e => e.Name.EndsWith(".log", StringComparison.Ordinal));
            Assert.Contains(archive.Entries, e => e.Name.StartsWith("crash-", StringComparison.Ordinal));
            foreach (var entry in archive.Entries)
            {
                using var reader = new StreamReader(entry.Open());
                string text = reader.ReadToEnd();
                foreach (string typed in Typed)
                {
                    Assert.DoesNotContain(typed, text, StringComparison.Ordinal);
                }
            }

            foreach (string typed in Typed)
            {
                Assert.DoesNotContain(typed, report.ToJsonString(), StringComparison.Ordinal);
            }
        }
        finally
        {
            Close(opened);
        }
    }

    /// <summary>A package that met no answer is kept, and the report waits with it rather than going without the log it promised.</summary>
    [AvaloniaFact]
    public async Task AReportWaitsForItsLogPackage()
    {
        var opened = Open(fullLog: true, newWording: true);
        try
        {
            Outside.ReportAnswer = _ => new PostAnswer(200, "{\"ok\":true,\"id\":\"x\"}");
            opened.Window.Show();
            Settle();
            Assert.Equal(0, await opened.Window.SendWaitingErrorsAsync());
            Assert.NotEmpty(Outside.Packages);
            Assert.Empty(Outside.Reports);
        }
        finally
        {
            Close(opened);
        }
    }

    /// <summary>A picture GroupLab could not read is a report of its own, with its stage records and the picture code its submission has.</summary>
    [AvaloniaFact]
    public void AReadFailureIsAReportMatchedToItsPicture()
    {
        var opened = Open(fullLog: true, newWording: true);
        try
        {
            string code = new('c', 32);
            string? record = ErrorReports.RecordReadFailure(opened.Log.Directory, "the sheet's codes could not be read", [new JsonObject { ["stage"] = "S0.identify" }], code);
            Assert.NotNull(record);
            Assert.True(ReportPackage.IsPermitted(Path.GetFileName(record!)));
            Assert.DoesNotContain(ErrorReports.Waiting(opened.Log.Directory, DateTime.UtcNow), g => g.Contains(record));
            var group = Assert.Single(ErrorReports.Waiting(opened.Log.Directory, DateTime.UtcNow, withReads: true), g => g.Contains(record));
            var report = JsonNode.Parse(ErrorReports.Build(group, []))!;
            Assert.Equal(ErrorReports.ReadFailure, (string?)report["kind"]);
            Assert.Equal(code, (string?)report["picture_code"]);
            Assert.DoesNotContain(record, CrashReporter.PendingCrashes(opened.Log.Directory));
        }
        finally
        {
            Close(opened);
        }
    }
}
