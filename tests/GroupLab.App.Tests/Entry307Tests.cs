using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 307 on the desktop: Settings, Your data. Export all my data writes one file; Import data on another computer
/// says what it would add and writes nothing until Import; a second import adds nothing.
/// </summary>
public class Entry307Tests
{
    private static MainWindow Window(out AppSettingsStore store)
    {
        store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [AvaloniaFact]
    public void ExportOnOneComputerAndImportOnAnother()
    {
        var from = Window(out var fromStore);
        var to = Window(out var toStore);
        string file = Path.Combine(Path.GetTempPath(), $"grouplab-data-{Guid.NewGuid():N}{DataExport.Extension}");
        try
        {
            var sessions = SessionStore.Open(fromStore.DatabasePath);
            sessions.SaveBook(RecordBook.Empty.With(new Rifle("Tikka", 0.1, AngularUnit.Mrad)));
            sessions.Save(new SessionRecord(0, "2026-09-30T10:00:00Z", null, "GL-CF25-LTR", null, null, 3600, "Tikka", null, null, 0.308, "{}", 5, 0.4, null, null, null, "abc", null, null));
            Assert.True(from.ExportAll(file));

            var plan = to.PlanImport(file)!;
            Assert.Single(plan.NewSessions);
            Assert.Empty(SessionStore.Open(toStore.DatabasePath).List());
            Assert.Equal(1, to.ApplyImport());
            Assert.Single(SessionStore.Open(toStore.DatabasePath).List());
            Assert.Equal(0, to.PlanImport(file)!.Adds);
        }
        finally
        {
            from.Close();
            to.Close();
            GroupLab.Tests.Support.Temp.DeleteFile(file);
        }
    }
}
