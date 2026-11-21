using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 506 section 2, on the computer: two checks of one printer, made on this computer and on a phone, wait on Session
/// records' banner and under the printer in Printers and paper, and open side by side in concept A's pattern (entry 504): each card with the
/// device, when it was checked, across, down and how it was made, then Decide later, Keep both under two names and each check.
/// </summary>
public sealed class Entry506Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-506-{Guid.NewGuid():N}");

    public Entry506Tests() => Directory.CreateDirectory(Path.Combine(root, "cloud"));

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private string Cloud => Path.Combine(root, "cloud");

    private static void Wait(Task task)
    {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (!task.IsCompleted && DateTime.UtcNow < until)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Thread.Sleep(5);
        }

        Dispatcher.UIThread.RunJobs();
        Assert.True(task.IsCompleted, "the sync did not finish");
    }

    [AvaloniaFact]
    public void TwoChecksOfOnePrinterAreChosenBetweenSideBySideAndKeptUnderTwoNames()
    {
        var settings = new AppSettingsStore(Path.Combine(root, "desk.json"));
        settings.SaveUnits(UnitSettings.Imperial);
        settings.SavePrinter(new PrinterProfile("Office laser", 0.992, 0.994, PrinterMethod.Card, new DateOnly(2026, 11, 10), 0.003));
        var window = new MainWindow(settings) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        string phone = Path.Combine(root, "phone");
        Directory.CreateDirectory(phone);
        var other = SessionStore.Open(Path.Combine(phone, "grouplab.db"));
        var otherLocal = new FileSyncLocal(Path.Combine(phone, "sheets"), Path.Combine(phone, "settings.json"));
        otherLocal.SaveSetting("printers", new System.Text.Json.Nodes.JsonArray(
            new PrinterProfile("Office laser", 0.981, 0.983, PrinterMethod.Scan, new DateOnly(2026, 11, 12), 0.002).ToJson()));
        try
        {
            SyncEngine.Run(other, otherLocal, new DirectorySyncFolder(Cloud), "Alan's phone", DateTime.UtcNow);
            window.ChooseSyncFolder(Cloud);
            Wait(window.SyncNowAsync());

            var waiting = Assert.Single(window.PrintersToChoose());
            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(SyncScreens.ToChooseBanner(0, 0, 1), window.SessionsText);

            window.ShowSyncConflict(waiting.Conflict.Id);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(PrinterChoices.Title, window.ProblemTitle);
            Assert.Equal([SyncScreens.DecideLater, PrinterChoices.KeepBoth, "Use this computer's", "Use the Alan's phone's"], window.ProblemChoices);
            var words = window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToList();
            Assert.Contains("99.20%", words);
            Assert.Contains("98.10%", words);
            Assert.Contains("Card", words);
            Assert.Contains("Scan", words);
            Assert.Contains("Checked on 12 November 2026", words);
            Assert.Equal(6, words.Count(w => w == SyncScreens.Differs));
            Assert.Contains(words, w => w.StartsWith("This computer (", StringComparison.Ordinal));

            window.PressProblemChoice(PrinterChoices.KeepBoth);
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(window.PrintersToChoose());
            Assert.Equal(["Office laser", "Office laser (Alan's phone)"], settings.LoadPrinters().Select(p => p.Name).Order());
            Assert.Equal(0.981, settings.LoadPrinters().Single(p => p.Name == "Office laser (Alan's phone)").Across, 6);
        }
        finally
        {
            window.Close();
        }
    }
}
