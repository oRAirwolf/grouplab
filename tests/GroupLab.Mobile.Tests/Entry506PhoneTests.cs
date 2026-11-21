using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;
using GroupLab.Tests.Support;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 506 section 2 on the phone: two checks of one printer, made on this phone and on a computer, wait on Sessions'
/// banner and under the printer in Printers and paper, and open as a full page, "Choose a check", in concept A's pattern (entry 504): the two
/// checks as cards with the device, when it was checked, across, down and how it was made, and the four choices in two rows of two.
/// </summary>
public sealed class Entry506PhoneTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-506p-{Guid.NewGuid():N}");

    public Entry506PhoneTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current!, () => "US", null);
        }

        GroupLab.Mobile.Dev.Scenario.AnswerFirstRun(Phone.Settings);
    }

    public void Dispose()
    {
        PhoneSync.Stop();
        TestPhone.SyncFolderToPick = null;
        Temp.Delete(root);
    }

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

    private static IEnumerable<string> Words(Control c) => c.GetLogicalDescendants()
        .Select(d => d switch { TextBlock t => t.Text, ContentControl { Content: string s } => s, _ => null }).OfType<string>();

    [AvaloniaFact]
    public void TwoChecksOfOnePrinterAreChosenBetweenOnAPageOfTheirOwn()
    {
        var mine = PhoneAnalysis.Store();
        foreach (var c in mine.SyncConflicts())
        {
            mine.RemoveSyncConflict(c.Id);
        }

        foreach (var p in Phone.Settings.LoadPrinters())
        {
            Phone.Settings.DeletePrinter(p.Name);
        }

        Phone.Settings.SavePrinter(new PrinterProfile("Office laser", 0.981, 0.983, PrinterMethod.Scan, new DateOnly(2026, 11, 12), 0.002));
        string desk = Path.Combine(root, "desk");
        Directory.CreateDirectory(desk);
        var other = SessionStore.Open(Path.Combine(desk, "grouplab.db"));
        var otherLocal = new FileSyncLocal(Path.Combine(desk, "sheets"), Path.Combine(desk, "settings.json"));
        otherLocal.SaveSetting("printers", new System.Text.Json.Nodes.JsonArray(
            new PrinterProfile("Office laser", 0.992, 0.994, PrinterMethod.Card, new DateOnly(2026, 11, 10), 0.003).ToJson()));
        TestPhone.SyncFolderToPick = Path.Combine(root, "cloud");
        var shell = new Shell();
        var window = new Window { Width = 412, Height = 915, Content = shell };
        window.Show();
        try
        {
            SyncEngine.Run(other, otherLocal, new DirectorySyncFolder(Path.Combine(root, "cloud")), "Desk", DateTime.UtcNow);
            Wait(PhoneSync.Choose(window));
            Wait(PhoneSync.Now());

            var waiting = Assert.Single(PhoneSync.PrintersToChoose());
            shell.Show(Shell.Place.Sessions);
            Dispatcher.UIThread.RunJobs();
            Assert.Contains(SyncScreens.ToChooseBanner(0, 0, 1), Words(window));

            var page = Assert.IsType<SessionsPage>(shell.PageContent);
            PhoneSync.ShowConflict(page, waiting.Conflict.Id, () => { });
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.Contains(PrinterChoices.PhoneTitle, Words(window));
            Assert.Contains("Office laser, checked on two devices. Which numbers should GroupLab use?", Words(window));
            Assert.Contains("98.10%", Words(window));
            Assert.Contains("99.20%", Words(window));
            Assert.Contains("Checked 12 Nov", Words(window));
            Assert.Equal(6, Words(window).Count(w => w == SyncScreens.Differs));
            Assert.Contains(Words(window), w => w.StartsWith("This phone (", StringComparison.Ordinal));
            var buttons = window.GetLogicalDescendants().OfType<Button>().ToList();
            string Said(Button b) => b.Content as string ?? (b.Content as TextBlock)?.Text ?? "";
            foreach (string label in new[] { "Use this phone's", "Use Desk's", PrinterChoices.KeepBothShort, SyncScreens.DecideLater })
            {
                Assert.True(buttons.Single(b => Said(b) == label).Bounds.Height >= 44, label);
            }

            // Back puts Sessions back and decides nothing.
            buttons.Single(b => b.Classes.Contains(PhoneStyles.PageBack)).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Single(PhoneSync.PrintersToChoose());
            Assert.Contains(SyncScreens.ToChooseBanner(0, 0, 1), Words(window));

            PhoneSync.ShowConflict(page, waiting.Conflict.Id, () => { });
            Dispatcher.UIThread.RunJobs();
            window.GetLogicalDescendants().OfType<Button>().Single(b => Said(b) == PrinterChoices.KeepBothShort)
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Empty(PhoneSync.PrintersToChoose());
            Assert.Equal(["Office laser", "Office laser (Desk)"], Phone.Settings.LoadPrinters().Select(p => p.Name).Order());
        }
        finally
        {
            window.Close();
            foreach (var p in Phone.Settings.LoadPrinters())
            {
                Phone.Settings.DeletePrinter(p.Name);
            }
        }
    }
}
