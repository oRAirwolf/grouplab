using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Shot coordinates in and out as CSV, NOTES-FROM-PLANNING.md entry 169 section 8. The first outside user could not see what the JSON
/// export was for in any other program; a spreadsheet reads CSV. Import takes any program's CSV through one mapping step, which column is
/// across, which is up and down, and what unit, so no format is named or assumed.
/// </summary>
public sealed partial class MainWindow
{
    private static readonly (string Name, CoordinateUnit Unit)[] ImportUnits =
    [
        ("inches", CoordinateUnit.Inch), ("millimeters", CoordinateUnit.Millimetre), ("centimeters", CoordinateUnit.Centimetre), ("MOA", CoordinateUnit.Moa), ("mil", CoordinateUnit.Mil),
    ];

    /// <summary>Writes the shots as CSV, one row per shot, for spreadsheets and other tools.</summary>
    private async Task WriteCsv(string path)
    {
        await File.WriteAllTextAsync(path, ShotCsv.Write(session.State));
        status.Text = "Exported the shot coordinates to " + path;
        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "csv"), ("shots", session.State.Shots.Count)]);
    }

    private async Task ImportCsvDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "import-csv"));
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import shot coordinates from a CSV file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("CSV") { Patterns = ["*.csv", "*.txt"] }, FilePickerFileTypes.All],
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } path)
        {
            return;
        }

        CsvTable table;
        try
        {
            table = ShotCsv.Read(await File.ReadAllTextAsync(path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
        {
            status.Text = "That file could not be read as CSV: " + e.Message;
            return;
        }

        await ShowMapping(table, Path.GetFileName(path));
    }

    /// <summary>The mapping step: which column is across, which is up and down, the unit, which way is up, and the distance for an angle.</summary>
    private async Task ShowMapping(CsvTable table, string name)
    {
        var columns = table.Headers.Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"column {i + 1}" : h).ToList();
        var across = new ComboBox { ItemsSource = columns, SelectedIndex = ShotCsv.Guess(table, across: true) ?? 0, MinWidth = 220 };
        var upDown = new ComboBox { ItemsSource = columns, SelectedIndex = ShotCsv.Guess(table, across: false) ?? Math.Min(1, columns.Count - 1), MinWidth = 220 };
        var unit = new ComboBox { ItemsSource = ImportUnits.Select(u => u.Name).ToList(), SelectedIndex = 0, MinWidth = 220 };
        var upPositive = new CheckBox { Content = "A larger number is higher on the target", IsChecked = true };
        var distance = new TextBox { Text = session.State.ShotDistanceInches is { } d ? (d / 36).ToString("0.#", CultureInfo.InvariantCulture) : "", PlaceholderText = "yards", Width = 100 };
        var problem = new TextBlock { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };
        var dialog = new Window
        {
            Title = "Import shot coordinates",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var body = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        body.Children.Add(new TextBlock { Text = $"{name}: {table.Rows.Count} rows. Which columns hold the shots?", TextWrapping = TextWrapping.Wrap });
        body.Children.Add(Row(new TextBlock { Text = "Across", Width = 110, VerticalAlignment = VerticalAlignment.Center }, across));
        body.Children.Add(Row(new TextBlock { Text = "Up and down", Width = 110, VerticalAlignment = VerticalAlignment.Center }, upDown));
        body.Children.Add(Row(new TextBlock { Text = "Unit", Width = 110, VerticalAlignment = VerticalAlignment.Center }, unit));
        body.Children.Add(upPositive);
        body.Children.Add(Row(new TextBlock { Text = "Shot at", Width = 110, VerticalAlignment = VerticalAlignment.Center }, distance, new TextBlock { Text = "yards, needed for MOA or mil", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } }));
        body.Children.Add(Note("Each row is one shot, measured from the point of aim. Rows without two numbers, such as a total line, are left out and counted."));
        body.Children.Add(problem);
        body.Children.Add(Row(
            Button("Import", () =>
            {
                double? yards = double.TryParse(distance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) && y > 0 ? y : null;
                if (ImportShots(table, across.SelectedIndex, upDown.SelectedIndex, ImportUnits[unit.SelectedIndex].Unit, upPositive.IsChecked == true, yards * 36) is { } refused)
                {
                    problem.Text = refused;
                    return;
                }

                dialog.Close();
            }),
            Button("Cancel", dialog.Close)));
        dialog.Content = new ScrollViewer { Content = body };
        await dialog.ShowDialog(this);
    }

    /// <summary>
    /// Makes a marking from a mapped table and shows its analysis, asking first where the current sheet has unsaved work. Returns why nothing
    /// was imported, or null.
    /// </summary>
    internal string? ImportShots(CsvTable table, int across, int upDown, CoordinateUnit unit, bool upIsPositive, double? distanceInches)
    {
        if (across < 0 || upDown < 0 || across == upDown)
        {
            return "Choose two different columns, one across and one up and down.";
        }

        IReadOnlyList<Core.Imaging.PointD> offsets;
        int skipped;
        try
        {
            (offsets, skipped) = ShotCsv.Shots(table, across, upDown, unit, upIsPositive, distanceInches);
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }

        if (offsets.Count == 0)
        {
            return "No row has a number in both of those columns.";
        }

        Leaving(() =>
        {
            ClearSheet(ShotCsv.Marking(offsets, distanceInches));
            DiagnosticLog.Info("file.import", ("kind", "csv"), ("shots", offsets.Count), ("skipped", skipped));
            Analyse();
            status.Text = string.Create(CultureInfo.InvariantCulture, $"Imported {offsets.Count} shots") + (skipped > 0 ? string.Create(CultureInfo.InvariantCulture, $", leaving out {skipped} rows without two numbers") : "") + ". There is no image, so the figures are the whole of it.";
        });
        return null;
    }
}
