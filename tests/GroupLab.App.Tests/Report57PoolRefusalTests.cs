using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// Error report 57 (2026-11-23, nightly 240 on a Mac): with a loose sheet ticked beside two sheets of a set, Pool the chosen seemed to do
/// nothing. It had refused, in a small grey line under the buttons that the person did not see. A refusal is now in amber and said in the
/// status bar too. Since Alan's yes to the general case of the report's suggestion (2026-11-24), sheets of different designs, or loose
/// sheets of one design, are no longer refused: they pool as separate sheets, each shot about its own bull, in their own tab, and the pool
/// is saved as one of pictures. What is still refused: one loose sheet, and sheets shot at different distances or with different calibers.
/// </summary>
public class Report57PoolRefusalTests
{
    [AvaloniaFact]
    public void SheetsOfDifferentDesignsPoolAsSeparateSheetsAndWhatCannotPoolIsRefusedInAmber()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            long first = window.CurrentSession!.Value;
            var saved = window.Sessions!.Get(first)!;
            var state = MarkingFile.Read(saved.MarkingJson).State;
            long loose = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", DefinitionId = "GL-OTHER-0000", SheetName = "GroupLab 3x3 2 MOA, C Bull, Letter",
                DistanceInches = 3600, CalibreInches = 0.308, MarkingJson = MarkingFile.Write(state with { ShotDistanceInches = 3600, Calibre = Calibre.Of(0.308) }) });
            long second = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-02T00:00:00Z" });
            long far = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-03T00:00:00Z", DistanceInches = 7200, MarkingJson = MarkingFile.Write(state with { ShotDistanceInches = 7200 }) });
            long small = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-04T00:00:00Z", MarkingJson = MarkingFile.Write(state with { Calibre = Calibre.Of(0.224) }) });

            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            foreach (long id in new[] { first, second, loose })
            {
                window.ChooseSession(id, true);
            }

            int tabs = window.Targets.Count;
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            string said = string.Join(" | ", window.PoolText);
            Assert.Contains("Pooled set of 3 sheets", said, StringComparison.Ordinal);
            Assert.Contains("The 3 sheets are pooled as separate sheets, in the order ticked", said, StringComparison.Ordinal);
            Assert.False(window.PoolRefusedInAmber);
            Assert.Equal(tabs + 1, window.Targets.Count);
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);
            Assert.True(window.ShowingPool);

            // Saved when the tab is accepted, as a pool of pictures, and opened again the same.
            window.BackToEditor();
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var pool = Assert.Single(window.Sessions.Pools());
            Assert.True(pool.Pictures);
            Assert.Equal(3, pool.Sheets.Count);
            window.OpenSavedPool(pool.Id);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);

            // One loose sheet alone: nothing to pool it with, refused in amber and in the status bar.
            window.ShowSessions();
            foreach (long id in new[] { first, second })
            {
                window.ChooseSession(id, false);
            }

            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(PoolRefusal.OneSheet, Assert.Single(window.PoolText));
            Assert.True(window.PoolRefusedInAmber);
            Assert.Equal(PoolRefusal.OneSheet, window.StatusText);

            // Different distances, and different calibers: refused, because their inches do not pool.
            window.ChooseSession(far, true);
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(PoolRefusal.DifferentDistances, Assert.Single(window.PoolText));
            Assert.True(window.PoolRefusedInAmber);
            window.ChooseSession(far, false);
            window.ChooseSession(small, true);
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(PoolRefusal.DifferentCalibres, Assert.Single(window.PoolText));
            Assert.True(window.PoolRefusedInAmber);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
