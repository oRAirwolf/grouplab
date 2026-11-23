using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 512 section 3, report 57's suggestion (Alan's yes to the narrow case): a loose 3x3 2 MOA C bull sheet ticked
/// with sheets of the 3x3 2 MOA C bull set of 3 counts as the set's sheet missing from those ticked, so a spoiled sheet can be replaced.
/// With one place it takes it and says so; with several it asks which; with none it is refused in amber. The pool saved with it opens again
/// with the loose sheet in its place. The sheets here are one analyzed sheet's marking saved under each design, which is all pooling reads.
/// </summary>
public class Entry512StandInPoolTests
{
    private static string Targets([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "targets"));

    [AvaloniaFact]
    public void ALooseSheetWithTheSetsBullsStandsInForTheMissingSheet()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            var saved = window.Sessions!.Get(window.CurrentSession!.Value)!;
            var state = MarkingFile.Read(saved.MarkingJson).State;
            string setJson = File.ReadAllText(Path.Combine(Targets(), "GL-CF9-T-C.gltd.json"));
            string looseJson = File.ReadAllText(Path.Combine(Targets(), "GL-CF9-LTR-C.gltd.json"));
            long Save(string id, string json, string name, int? sheet, int day) => window.Sessions.Save(saved with
            {
                Id = 0,
                CreatedUtc = $"2099-01-0{day}T00:00:00Z",
                DefinitionId = id,
                DefinitionJson = json,
                SheetName = name,
                MarkingJson = MarkingFile.Write(state with { SetSheet = sheet }),
            });
            const string SetName = "GroupLab 3x3 2 MOA, C Bull, Set of 3 Letter Sheets";
            long first = Save("GL-SET", setJson, SetName, 0, 1);
            long third = Save("GL-SET", setJson, SetName, 2, 2);
            long loose = Save("GL-LOOSE", looseJson, "GroupLab 3x3 2 MOA, C Bull, Letter", null, 3);
            long second = Save("GL-SET", setJson, SetName, 1, 4);

            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            foreach (long id in new[] { first, third, loose })
            {
                window.ChooseSession(id, true);
            }

            int tabs = window.Targets.Count;
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            string said = string.Join(" | ", window.PoolText);
            Assert.False(window.ProblemOpen);
            Assert.Contains("All 3 sheets of the set are here.", said, StringComparison.Ordinal);
            Assert.Contains("counts as sheet 2 of the set.", said, StringComparison.Ordinal);
            Assert.False(window.PoolRefusedInAmber);
            Assert.Equal(tabs + 1, window.Targets.Count);
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);

            // Saved, the pool keeps the loose sheet's place, and opens again with it there.
            Assert.True(window.ShowingPool);
            window.BackToEditor();
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            var pool = Assert.Single(window.Sessions.Pools());
            window.OpenSavedPool(pool.Id);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal([0, 1, 2], window.Plot.PooledSheets);

            // With two of the set's sheets missing from those ticked, it asks which one the loose sheet replaces.
            window.ShowSessions();
            window.ChooseSession(third, false);
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.True(window.ProblemOpen);
            Assert.Equal(PoolRefusal.WhichSheetTitle, window.ProblemTitle);
            Assert.Equal(["Sheet 2", "Sheet 3"], window.ProblemChoices);
            window.PressProblemChoice("Sheet 3");
            Dispatcher.UIThread.RunJobs();
            Assert.Contains("counts as sheet 3 of the set.", string.Join(" | ", window.PoolText), StringComparison.Ordinal);
            Assert.Equal([0, 2], window.Plot.PooledSheets);

            // With every sheet of the set ticked, there is nothing for it to stand in for: refused in amber, saying what to untick.
            window.ShowSessions();
            foreach (long id in new[] { second, third })
            {
                window.ChooseSession(id, true);
            }

            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.StartsWith($"Every sheet of {SetName} is ticked already", Assert.Single(window.PoolText), StringComparison.Ordinal);
            Assert.True(window.PoolRefusedInAmber);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
