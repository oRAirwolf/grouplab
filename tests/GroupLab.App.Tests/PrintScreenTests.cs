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
    /// NOTES-FROM-PLANNING.md entry 105 section 9: the launch uses no verb on any platform, since Windows' print verb printed Alan's sheets
    /// silently at his viewer's own scaling, and the words say the file is open in the viewer and never promise a print dialog.
    /// </summary>
    [Fact]
    public void ThePrintLaunchOpensThePdfWithNoVerbAndPromisesNoDialog()
    {
        string path = Path.Combine(Path.GetTempPath(), "grouplab-print-launch.pdf");
        var (start, status, kind) = PrintWindow.PrintLaunch(path);
        Assert.Equal(string.Empty, start.Verb);
        Assert.True(start.UseShellExecute);
        Assert.Equal(path, start.FileName);
        Assert.Equal(StatusKind.Information, kind);
        Assert.Contains("open in your PDF viewer", status, StringComparison.Ordinal);
        Assert.Contains("Actual size", status, StringComparison.Ordinal);
        Assert.DoesNotContain("dialog", status, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sent", status, StringComparison.Ordinal);
    }

    /// <summary>
    /// Entry 106 section 1: the viewer path says what it does, "Open to print", with no ellipsis promising a dialog. NOTES-FROM-PLANNING.md
    /// entry 107 section 2: on Windows "Print…" opens the real print dialog, so it has the ellipsis. <b>Entry 114 section 1 moves which one
    /// is the primary:</b> the in-app path printed sheets with every marker and code missing on a Brother driver, and until a sheet from the
    /// fixed drawing has been checked on paper, Open to print is the amber button and comes first, with the reason on the screen above it.
    /// Print stays, because nothing is hidden. Linux and macOS keep the viewer path alone.
    /// </summary>
    [AvaloniaFact]
    public void OpenToPrintIsThePrimaryUntilTheInAppPathIsCheckedOnPaper()
    {
        var window = new PrintWindow { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Select("GL-CF25-LTR.gltd.json");
        Dispatcher.UIThread.RunJobs();
        var buttons = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(window).OfType<Avalonia.Controls.Button>().ToList();
        var open = Assert.Single(buttons, b => b.Content as string == "Open to print");
        if (OperatingSystem.IsWindows())
        {
            Assert.Contains(GroupLab.App.Theme.AppStyles.Primary, open.Classes);
            var print = Assert.Single(buttons, b => b.Content as string == "Print…");
            Assert.DoesNotContain(GroupLab.App.Theme.AppStyles.Primary, print.Classes);
            var row = Assert.IsType<Avalonia.Controls.StackPanel>(print.Parent);
            Assert.Same(open, row.Children[0]);
            Assert.Same(row, open.Parent);

            // The screen says why, in the warning role, naming what was lost and where the safe path is.
            var texts = Avalonia.LogicalTree.LogicalExtensions.GetLogicalDescendants(window).OfType<Avalonia.Controls.TextBlock>().ToList();
            var warning = Assert.Single(texts, t => t.Text == PrintWindow.UnconfirmedWords);
            Assert.Contains(GroupLab.App.Theme.AppStyles.FormWarning, warning.Classes);
            Assert.Contains("Open to print", warning.Text, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain(GroupLab.App.Theme.AppStyles.Primary, open.Classes);
            Assert.DoesNotContain(buttons, b => b.Content as string == "Print…");
        }

        window.Close();
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 70 section 6: the status line says which state it reports. A save that fails is an alert, and a save
    /// that works afterwards is a success, not a red line left over from the failure.
    /// </summary>
    [AvaloniaFact]
    public void TheStatusLineIsAnAlertOnlyWhenSomethingFailed()
    {
        var window = new PrintWindow { Width = 1200, Height = 800 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Select("GL-CF25-LTR.gltd.json");
        string blocker = Path.Combine(Path.GetTempPath(), $"grouplab-status-{Guid.NewGuid():N}");
        string good = blocker + ".pdf";
        File.WriteAllText(blocker, "a file where a directory is needed");
        try
        {
            Assert.False(window.SavePdf(Path.Combine(blocker, "sheet.pdf")));
            Assert.Equal(StatusKind.Alert, window.StatusState);

            Assert.True(window.SavePdf(good), window.StatusText);
            Assert.Equal(StatusKind.Success, window.StatusState);
        }
        finally
        {
            File.Delete(blocker);
            File.Delete(good);
            window.Close();
        }
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
            Assert.Equal(StatusKind.Success, window.StatusState);
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
