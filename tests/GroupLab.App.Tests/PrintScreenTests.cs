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
}
