using System.IO.Compression;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 41 section 6: the report carries the log of the run that crashed and the one before; the dialog says in one
/// sentence what the report holds and does not, lists every entry before anything is written, saves exactly those, and saving a crash's
/// report stops the next launch offering it.
/// </summary>
public sealed class ReportWindowTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-report-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        GroupLab.Tests.Support.Temp.Delete(root);
        GC.SuppressFinalize(this);
    }

    private string Write(string name, string content)
    {
        string path = Path.Combine(root, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ACrashesReportCarriesTheLogOfTheRunThatCrashedAndTheRunBefore()
    {
        string previous = Write("grouplab-20260914-201500-3100.log", "previous run");
        string crashed = Write("grouplab-20260915-064100-4242.log", "the run that crashed");
        Write("grouplab-20260915-080000-5000.log", "a later run, this one");
        string crash = Write("crash-20260915-064212-4242.json", "{}");

        var (runLog, previousLog) = ReportPackage.LogsFor(DiagnosticLog.Disabled("test"), crash);
        Assert.Equal(crashed, runLog);
        Assert.Equal(previous, previousLog);
    }

    [AvaloniaFact]
    public void TheDialogSaysWhatTheReportHoldsListsItAndSavesExactlyThat()
    {
        string previous = Write("grouplab-20260914-201500-3100.log", "previous run");
        string run = Write("grouplab-20260915-064100-4242.log", "the run that crashed");
        string crash = Write("crash-20260915-064212-4242.json", "{\"schema\":1}");

        var window = new ReportWindow(crash, run, previous);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var text = window.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
        Assert.Contains(ReportWindow.Contents, text);
        Assert.Contains(text, t => t.StartsWith("crash-20260915-064212-4242.json", StringComparison.Ordinal));
        Assert.Contains(text, t => t.StartsWith("grouplab-20260914-201500-3100.log", StringComparison.Ordinal));
        Assert.DoesNotContain(window.GetLogicalDescendants().OfType<Button>(), b => (b.Content as string) == "Send" && b.IsVisible);

        window.SetDescription("It closed when I chose a second sheet.");
        string zip = Path.Combine(root, "grouplab-report-20260915-070000.zip");
        var result = window.SaveTo(zip);

        Assert.NotNull(result);
        using (var archive = ZipFile.OpenRead(zip))
        {
            Assert.Equal(["crash-20260915-064212-4242.json", "grouplab-20260915-064100-4242.log", "grouplab-20260914-201500-3100.log", "environment.txt", "description.txt"], archive.Entries.Select(e => e.FullName));
        }

        Assert.Contains("Saved to", window.StatusText, StringComparison.Ordinal);
        Assert.Empty(CrashReporter.PendingCrashes(root));
        window.Close();
    }
}
