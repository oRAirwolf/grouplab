using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 331 section 2: a chronograph file read into the hand-entry box beside it and reconciled with the shots the
/// same way; a generic CSV's column and unit can be chosen again, and LabRadar's and Xero's readers say they are Experimental.
/// </summary>
public class Entry331Tests
{
    private static void Settle() => Dispatcher.UIThread.RunJobs();

    [AvaloniaFact]
    public void AChronographFileGoesThroughTheSameReconciliation()
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
            string csv = "Shot,Velocity (fps)\n" + string.Join("\n", Enumerable.Range(1, shots).Select(i => $"{i},{2700 + (i % 7)}")) + "\n";
            window.ImportChronograph(csv, "range day");
            Settle();
            Assert.Equal(shots, window.ChronographPairing.Count);
            Assert.All(window.ChronographPairing, p => Assert.NotNull(p.Reading));
            Assert.Contains(window.ChronographImportText, t => t.StartsWith($"{shots} velocities from \"Velocity (fps)\"", StringComparison.Ordinal));

            // The same file read as m/s by the person's choice: every reading is 3.28 times as many ft/s.
            window.ImportChronograph(csv, "range day", column: 1, metres: true);
            Settle();
            Assert.Equal((2700 + 1) / 0.3048, window.ChronographPairing[0].Reading!.Value, 0);

            const string report = "Device ID;LBR-0000000;;\nUnits velocity;fps;;\nShot ID;V0;V10;\n0001;2701,40;2690,10;\n0002;2695,90;2684,70;\n";
            window.ImportChronograph(report, "LabRadar");
            Settle();
            Assert.Contains(window.ChronographImportText, t => t.Contains(GroupLab.Core.Records.ChronographFiles.ExperimentalWords, StringComparison.Ordinal));
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.DeleteFile(store);
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
