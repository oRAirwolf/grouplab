using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// Error report 57 (2026-11-23, nightly 240 on a Mac): with a loose sheet ticked beside two sheets of a set, Pool the chosen seemed to do
/// nothing. It had refused, in a small grey line under the buttons that the person did not see. The refusal is now in amber, names each
/// design ticked with how many of its sheets, and is said in the status bar too.
/// </summary>
public class Report57PoolRefusalTests
{
    [AvaloniaFact]
    public void SheetsOfDifferentDesignsAreRefusedInAmberNamingEachDesign()
    {
        var (window, path, _) = Entry109Tests.Sheet();
        try
        {
            window.CalibreAnswered();
            window.Analyse();
            long first = window.CurrentSession!.Value;
            var saved = window.Sessions!.Get(first)!;
            long loose = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-01T00:00:00Z", DefinitionId = "GL-OTHER-0000", SheetName = "GroupLab 3x3 2 MOA, C Bull, Letter" });
            long second = window.Sessions.Save(saved with { Id = 0, CreatedUtc = "2099-01-02T00:00:00Z" });

            window.ShowSessions();
            Dispatcher.UIThread.RunJobs();
            foreach (long id in new[] { first, second, loose })
            {
                window.ChooseSession(id, true);
            }

            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            string said = Assert.Single(window.PoolText);
            Assert.StartsWith("These are sheets of different designs: ", said, StringComparison.Ordinal);
            Assert.Contains("GroupLab 3x3 2 MOA, C Bull, Letter (1 sheet)", said, StringComparison.Ordinal);
            Assert.Contains(" (2 sheets)", said, StringComparison.Ordinal);
            Assert.Contains("tick only those", said, StringComparison.Ordinal);
            Assert.True(window.PoolRefusedInAmber);
            Assert.Equal(said, window.StatusText);

            // Two sheets of one design that is not a set: refused the same way, in amber.
            window.ChooseSession(loose, false);
            window.PoolChosen();
            Dispatcher.UIThread.RunJobs();
            Assert.StartsWith("These are not sheets of a set", Assert.Single(window.PoolText), StringComparison.Ordinal);
            Assert.True(window.PoolRefusedInAmber);
            window.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
