using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// Found by NOTES-FROM-PLANNING.md entry 243 section 1.3: after one sheet was analyzed and saved, opening another with Open, drop or paste
/// kept the first sheet's session, so analyzing the second saved it over the first and the first sheet's record was gone. Opening an image
/// starts a new session; reopening a saved one still returns to that one.
/// </summary>
public class SecondSheetSessionTests
{
    [AvaloniaFact]
    public void AnalyzingASecondSheetAddsASessionAndLeavesTheFirstAlone()
    {
        var (window, path, result) = Entry109Tests.Sheet();
        try
        {
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();
            long first = Assert.IsType<long>(window.CurrentSession);
            var saved = window.Sessions!.Get(first)!;

            // A second sheet, opened the way Open, drop and paste open one.
            string second = Path.Combine(Path.GetDirectoryName(path)!, "second.png");
            File.Copy(path, second);
            _ = window.OpenImageSafely(second);
            Assert.True(window.Opened());
            Assert.Null(window.CurrentSession);
            window.ApplyDetection(result);
            window.Session.SetCalibre(Calibre.Of(0.308));
            window.Session.SetShotDistance(3600);
            window.CalibreAnswered();
            window.Analyse();
            Dispatcher.UIThread.RunJobs();

            Assert.NotEqual(first, window.CurrentSession);
            Assert.Equal(2, window.Sessions.List().Count);
            Assert.Equal(saved.ImagePath, window.Sessions.Get(first)!.ImagePath);

            // Reopening the first still returns to it.
            window.OpenSession(first);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(first, window.CurrentSession);
        }
        finally
        {
            window.Close();
            GroupLab.Tests.Support.Temp.Delete(Path.GetDirectoryName(path)!);
        }
    }
}
