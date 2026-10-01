using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker B item 2: an imported string that times its shots proposes the readings outside this group's
/// stretch as belonging to no shot, as row marks the person can change, with the reason above the rows.
/// </summary>
public class Entry342Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void AnImportedStringProposesItsOwnMarks()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        string store = window.SettingsStore.Path;
        try
        {
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            window.ShowBallistics();
            Settle();
            int shots = window.Session.State.Shots.Count(s => s.IsShot);

            // Two fouling shots, a pause of twenty minutes, then the group, each 40 s apart.
            var start = new TimeSpan(9, 30, 0);
            var timed = Enumerable.Range(0, shots + 2)
                .Select(i => new ChronographShot(i + 1, 2800.0 + i, Time: start + TimeSpan.FromSeconds(40 * i) + (i >= 2 ? TimeSpan.FromMinutes(20) : TimeSpan.Zero)))
                .ToList();
            var import = new ChronographImport(ChronographFormat.GarminXero, [.. timed.Select(t => t.Fps)], "A Garmin Xero export.", false, [], 1) { Name = "morning", Shots = timed };
            window.ImportChronographStrings([import], "Sessions_OCT_2026");
            Settle();

            var paired = window.ChronographPairing.Where(p => p.Shot is not null).ToList();
            Assert.Equal(shots, paired.Count);
            Assert.Equal(2802, paired[0].Reading);
            Assert.Equal([2800.0, 2801.0], window.ChronographPairing.Where(p => p.Shot is null).Select(p => p.Reading!.Value));
            Assert.Contains(window.ChronographText, t => t.Contains("only shots 3 to", StringComparison.Ordinal));

            // Read by hand again, the marks and their reasons go.
            window.ReadChronograph();
            Settle();
            Assert.DoesNotContain(window.ChronographText, t => t.Contains("only shots", StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
