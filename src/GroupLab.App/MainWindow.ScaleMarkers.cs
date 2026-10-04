using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Library;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.ScaleMarkers;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 365: Targets, "Scale markers". The corner brackets, the scale bars and the board stickers, printed at actual
/// size on Letter or A4 (the stickers on a 4 by 6 label too, for a label printer's own app), and the boards measured, each with Forget. A bank
/// card needs nothing printed: it is a choice of the Size step of Add a store-bought target.
/// </summary>
public partial class MainWindow
{
    private readonly StackPanel boardList = new() { Spacing = 4 };
    private readonly TextBlock markerSaid = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };

    private Control ScaleMarkersSection()
    {
        var paper = new ComboBox { ItemsSource = new[] { "Letter", "A4" }, SelectedIndex = AppSettingsStore.LetterRegion(AppSettingsStore.Region()) ? 0 : 1, MinWidth = 100 };
        Avalonia.Automation.AutomationProperties.SetName(paper, "Paper for scale markers");
        MarkerPaper Paper() => paper.SelectedIndex == 1 ? MarkerPaper.A4 : MarkerPaper.Letter;
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(Line(ScaleMarkerWords.Intro));
        foreach (var (name, gives) in new[] { (ScaleMarkerWords.Brackets, ScaleMarkerWords.BracketsGive), (ScaleMarkerWords.Bars, ScaleMarkerWords.BarsGive),
            (ScaleMarkerWords.Stickers, ScaleMarkerWords.StickersGive), (ScaleMarkerWords.Card, ScaleMarkerWords.CardGives) })
        {
            body.Children.Add(Line(name + ". " + gives));
        }

        body.Children.Add(Row(paper,
            Button(ScaleMarkerWords.PrintBrackets, () => PrintMarkers(MarkerKind.Bracket, Paper())),
            Button(ScaleMarkerWords.PrintBars, () => PrintMarkers(Paper() == MarkerPaper.A4 ? MarkerKind.MetricBar : MarkerKind.InchBar, Paper())),
            Button(ScaleMarkerWords.PrintStickers, () => PrintMarkers(MarkerKind.Sticker, Paper()))));
        body.Children.Add(Row(Button(ScaleMarkerWords.LabelStickers + "…", SaveLabelStickers), Button(ScaleMarkerWords.MeasureBoard, MeasureBoardDialog)));
        body.Children.Add(Line(ScaleMarkerWords.PrintNote));
        body.Children.Add(boardList);
        body.Children.Add(markerSaid);
        FillBoards();
        return new Expander { Header = ScaleMarkerWords.Heading, Content = body, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
    }

    private void FillBoards()
    {
        boardList.Children.Clear();
        var boards = settingsStore.LoadBoards();
        if (boards.Count == 0)
        {
            boardList.Children.Add(Line(ScaleMarkerWords.NoBoards));
        }

        foreach (var board in boards)
        {
            string name = board.Name;
            boardList.Children.Add(Row(new TextBlock { Text = ScaleMarkerWords.BoardLine(board), TextWrapping = Avalonia.Media.TextWrapping.Wrap, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center },
                Button(ScaleMarkerWords.Forget, () =>
                {
                    settingsStore.ForgetBoard(name);
                    DiagnosticLog.Info("markers.board.forget");
                    FillBoards();
                })));
        }
    }

    /// <summary>Writes the page's PDF where the print path writes and opens it to print, at actual size.</summary>
    private void PrintMarkers(MarkerKind kind, MarkerPaper paper)
    {
        string path = Path.Combine(Path.GetTempPath(), "GroupLab", ScaleMarkerPages.FileName(kind, paper) + ".pdf");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, ScaleMarkerPages.Pdf(kind, paper));
            TheOutsideWorld.Current.OpenFile(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception)
        {
            markerSaid.Text = "The page could not be opened to print (" + e.Message + "). It is saved at " + path + ".";
            DiagnosticLog.Exception(LogLevel.Warn, "markers.print", e);
            return;
        }

        DiagnosticLog.Info("markers.print", ("kind", kind.ToString()), ("paper", paper.ToString()));
        markerSaid.Text = "Opened " + ScaleMarkerPages.Title(kind, paper) + " to print. Print at Actual size (100%), never Fit to page.";
    }

    /// <summary>The stickers of set A on a 4 by 6 label, as a label printer's own app takes them: a 300 dpi picture and a PDF.</summary>
    private async Task SaveLabelStickers()
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Choose where to save the stickers for a label printer", AllowMultiple = false });
        if (folder.Count == 0 || folder[0].TryGetLocalPath() is not { } into)
        {
            return;
        }

        var (pngs, pdf) = PrinterAppFiles.Make(ScaleMarkerPages.Pages(MarkerKind.Sticker, MarkerPaper.Label4x6));
        string stem = Path.Combine(into, ScaleMarkerPages.FileName(MarkerKind.Sticker, MarkerPaper.Label4x6) + "-" + PrinterAppFiles.Suffix(PrinterAppFiles.DefaultDpi));
        try
        {
            File.WriteAllBytes(stem + ".png", pngs[0]);
            File.WriteAllBytes(stem + ".pdf", pdf);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            markerSaid.Text = "The stickers could not be saved: " + e.Message;
            return;
        }

        DiagnosticLog.Info("markers.label");
        markerSaid.Text = "Saved the stickers for a label printer's app as a picture and a PDF in " + into + ". In the app, print at 100 percent or actual size, never fit to page.";
    }

    /// <summary>Entry 365 section C: a photo of the board with a GroupLab sheet on it, measured off the screen's thread and saved by name.</summary>
    private async Task MeasureBoardDialog()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "A photo of the board, its four stickers and a GroupLab sheet on it",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.jpg", "*.jpeg", "*.png"] }],
        });
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
        {
            return;
        }

        markerSaid.Text = "Measuring the board…";
        var printer = settingsStore.LoadChosenPrinter();
        string name = ScaleMarkerWords.NextBoard(settingsStore.LoadBoards());
        var (board, said) = await Task.Run(() => ScaleMarkerFinder.MeasureBoard(path, ShippedDefinitions(), printer, name, DateOnly.FromDateTime(DateTime.Today)));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (board is not null)
            {
                settingsStore.SaveBoard(board);
                FillBoards();
            }

            DiagnosticLog.Info("markers.board.measure", ("saved", board is not null));
            markerSaid.Text = said;
        });
    }
}
