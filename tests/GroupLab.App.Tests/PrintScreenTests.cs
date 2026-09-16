using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests;

/// <summary>
/// The print screen headless, NOTES-FROM-PLANNING.md entry 25 section 2: the library shipped beside the application lists all 22
/// built-ins, a tiled target renders as every tile with a preview, and a filled load block saves as a PDF that asks for no scaling,
/// carries the typed value and carries the actual-size instruction on the sheet.
/// </summary>
public class PrintScreenTests
{
    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 61 section 3: Windows gets the shell print verb, and Linux and macOS are not asked for one. .NET
    /// refuses any verb but open there, throwing Win32Exception(ERROR_NO_ASSOCIATION), so asking would throw on every press, log a
    /// warning, and then blame the PDF viewer for a platform fact. The words shown differ with the platform for the same reason.
    /// </summary>
    [Fact]
    public void ThePrintLaunchAsksWindowsForAPrintVerbAndAsksTheOthersOnlyToOpenTheFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "grouplab-print-launch.pdf");

        var (onWindows, windowsStatus) = PrintWindow.PrintLaunch(path, windows: true);
        Assert.Equal("print", onWindows.Verb);
        Assert.True(onWindows.UseShellExecute);
        Assert.Equal(path, onWindows.FileName);
        Assert.Contains("print command", windowsStatus, StringComparison.Ordinal);

        var (elsewhere, elsewhereStatus) = PrintWindow.PrintLaunch(path, windows: false);
        Assert.Equal(string.Empty, elsewhere.Verb);
        Assert.True(elsewhere.UseShellExecute);
        Assert.Equal(path, elsewhere.FileName);
        Assert.Contains("no print command", elsewhereStatus, StringComparison.Ordinal);
        Assert.Contains("open in your viewer", elsewhereStatus, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void TheLibraryListsEverySheetAndSavesAPdfThatAsksForNoScaling()
    {
        var window = new PrintWindow { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(22, window.Sheets.Count);
        Assert.NotNull(window.PreviewSource);

        window.Select("GL-LR300-T.gltd.json");
        var tiled = window.Render();
        Assert.NotNull(tiled);
        Assert.Equal(4, tiled.Pages.Count);
        Assert.Equal([0, 1, 2, 3], tiled.Pages.Select(p => p.TileIndex));

        window.Select("GL-CF25-LTR-D.gltd.json");
        window.SetFilled(true);
        window.SetField("cartridge", "6.5 Creedmoor");
        window.SetField("powder", "H4350 41.5 gr");
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-print-{Guid.NewGuid():N}.pdf");
        try
        {
            Assert.True(window.SavePdf(path), window.StatusText);
            string pdf = Encoding.Latin1.GetString(File.ReadAllBytes(path));
            Assert.StartsWith("%PDF", pdf, StringComparison.Ordinal);
            Assert.Contains("/PrintScaling /None", pdf, StringComparison.Ordinal);
            Assert.Contains("(6.5 Creedmoor)", pdf, StringComparison.Ordinal);
            Assert.Contains("Print at actual size", pdf, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }

        window.Close();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 39 section 2: selecting more than one target crashed the screen, and it has been used by a person
    /// once before the session pack is printed from it. Every sheet is selected in turn and then in reverse, its load block switched
    /// between blank and filled more than once, its pages turned, and every sheet saved, with a PDF or a stated reason.
    /// </summary>
    [AvaloniaFact]
    public void EverySheetCanBeSelectedInTurnToggledPagedAndSavedWithoutACrash()
    {
        var window = new PrintWindow { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        string directory = Path.Combine(Path.GetTempPath(), $"grouplab-print-{Guid.NewGuid():N}");
        try
        {
            foreach (var (sheet, pass) in window.Sheets.Select(s => (s, 1)).Concat(window.Sheets.Reverse().Select(s => (s, 2))))
            {
                window.Select(sheet.File);
                Dispatcher.UIThread.RunJobs();
                Assert.NotNull(window.PreviewSource);
                window.SetFilled(true);
                window.SetFilled(false);
                window.SetFilled(true);
                window.Turn(1);
                window.Turn(-1);
                if (pass == 1)
                {
                    string path = Path.Combine(directory, sheet.File.Replace(".gltd.json", ".pdf", StringComparison.Ordinal));
                    Assert.True(window.SavePdf(path) || window.StatusText.Length > 0, $"{sheet.File}: neither saved nor refused with a reason");
                }
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        window.Close();
    }
}
