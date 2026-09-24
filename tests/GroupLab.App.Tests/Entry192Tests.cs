using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 192: Unholy was told GroupLab "closed unexpectedly 5 times" when it never closed. Five exceptions on the
/// window's thread were recorded and survived, all one error. A record now says whether GroupLab survived it or closed, the banner says
/// which, a run that ends without its own exit is caught at the next start, and a report groups identical errors with a count.
/// </summary>
public class Entry192Tests
{
    private static (DiagnosticLog Log, string Folder) NewLog()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-entry192-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return (new DiagnosticLog(folder, verbose: false), folder);
    }

    private static Exception Thrown()
    {
        try
        {
            SetCaliber();
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("nothing was thrown");
    }

    private static void SetCaliber() => throw new ArgumentOutOfRangeException("index", "Index was out of range.");

    [Fact]
    public void AnErrorSurvivedIsNotCalledAClose()
    {
        var (log, folder) = NewLog();
        try
        {
            var records = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                string record = Path.Combine(folder, $"crash-20260924-17000{i}-4242.json");
                File.WriteAllText(record, CrashReporter.Describe(Thrown(), DateTime.UtcNow, CrashReporter.KindOf("dispatcher")).ToJsonString());
                records.Add(record);
            }

            Assert.All(records, r => Assert.Equal(CrashReporter.Survived, CrashReporter.KindOfRecord(r)));
            Assert.Equal("GroupLab hit an error 5 times and kept running, and recorded what went wrong.", MainWindow.CrashBannerWords(records));

            string closed = Path.Combine(folder, "crash-20260924-180000-4243.json");
            File.WriteAllText(closed, CrashReporter.Describe(Thrown(), DateTime.UtcNow, CrashReporter.KindOf("process")).ToJsonString());
            Assert.Equal("GroupLab closed unexpectedly last time, and recorded what went wrong.", MainWindow.CrashBannerWords([closed]));
            Assert.Equal("GroupLab closed unexpectedly once and hit an error it kept running through 5 times, and recorded what went wrong.",
                MainWindow.CrashBannerWords([.. records, closed]));

            // Section 3.4: five records of one error read as one error five times, with where it was thrown in GroupLab's own code.
            string summary = CrashReporter.Summary(records);
            Assert.Single(summary.Split('\n'));
            Assert.StartsWith("5 times: System.ArgumentOutOfRangeException in GroupLab.App.Tests.Entry192Tests.SetCaliber, survived", summary, StringComparison.Ordinal);
        }
        finally
        {
            log.Dispose();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [Fact]
    public void ARunThatEndedWithoutItsExitIsFoundAtTheNextStart()
    {
        var (log, folder) = NewLog();
        try
        {
            // A marker left by a run whose process is gone, and nothing recorded by it.
            File.WriteAllText(Path.Combine(folder, "running-999999.marker"), "");
            CrashReporter.BeginRun(log);
            var pending = CrashReporter.PendingCrashes(folder);
            string record = Assert.Single(pending);
            Assert.EndsWith("-999999.json", record, StringComparison.Ordinal);
            Assert.Equal(CrashReporter.Closed, CrashReporter.KindOfRecord(record));
            Assert.False(File.Exists(Path.Combine(folder, "running-999999.marker")));
            Assert.True(File.Exists(Path.Combine(folder, $"running-{Environment.ProcessId}.marker")));

            // A clean exit takes this run's marker away, so the next start finds nothing.
            CrashReporter.EndRun(log);
            Assert.False(File.Exists(Path.Combine(folder, $"running-{Environment.ProcessId}.marker")));
            CrashReporter.BeginRun(log);
            Assert.Single(CrashReporter.PendingCrashes(folder));
            CrashReporter.EndRun(log);
        }
        finally
        {
            log.Dispose();
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }
}
