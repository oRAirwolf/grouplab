using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 407 section 7 (concept 7A, Alan's pick, which answers question 88): chronograph readings entered on the analysis,
/// in "Velocity and the vertical", where the block that needs them is. With no readings the block says what they add and offers Import a file
/// first, Type them in second, and a drop area naming only the formats GroupLab reads. Typing opens a box: the speeds in the order fired, a
/// count against the shots, and Match to shots, which proposes the pairing exactly as Ballistics' chronograph section does and keeps it; the
/// pairing is changed a reading at a time there, as before. With readings, the block is what it was.
/// <para>
/// A workbook holding several strings is the one case that still goes to Ballistics, where the string is chosen by its name.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    private readonly TextBox readingsHere = new()
    {
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        MinHeight = 72,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        PlaceholderText = "2705 2711 2698",
    };

    private readonly TextBlock readingsCount = new() { Classes = { AppStyles.Secondary } };
    private bool readingsOpen;
    private bool readingsWired;

    /// <summary>The block's part where there are no readings yet.</summary>
    private Control ReadingsHere()
    {
        if (!readingsWired)
        {
            readingsWired = true;
            readingsHere.TextChanged += (_, _) => CountReadingsHere();
            Avalonia.Automation.AutomationProperties.SetName(readingsHere, "Chronograph speeds, in the order fired");
        }

        var part = new StackPanel { Spacing = Tokens.Space8, Name = "readingsHere" };
        part.Children.Add(new TextBlock { Text = VelocityBlock.ReadingsAdd, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        var type = Button(VelocityBlock.TypeThemIn, () =>
        {
            readingsOpen = !readingsOpen;
            ShowVelocity(session.State);
            if (readingsOpen)
            {
                readingsHere.Focus();
            }
        });
        part.Children.Add(Row(Primary(VelocityBlock.ImportAFile, () => _ = ImportReadingsHere()), type));

        var drop = new Border
        {
            Child = new TextBlock { Text = VelocityBlock.DropHere, TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center, Classes = { AppStyles.Secondary } },
            Padding = new Thickness(Tokens.Space12),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            BorderBrush = new SolidColorBrush(Tokens.For(ActualThemeVariant).Line),
            Name = "readingsDrop",
        };
        DragDrop.SetAllowDrop(drop, true);
        drop.AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = ChronographDropped(e) is null ? DragDropEffects.None : DragDropEffects.Copy;
            e.Handled = true;
        });
        drop.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            e.Handled = true;
            if (ChronographDropped(e) is { } path)
            {
                _ = ReadingsFromFile(path);
            }
        });
        part.Children.Add(drop);

        if (readingsOpen)
        {
            if (readingsHere.Parent is Panel was)
            {
                was.Children.Remove(readingsHere);
            }

            if (readingsCount.Parent is Panel had)
            {
                had.Children.Remove(readingsCount);
            }

            part.Children.Add(new TextBlock { Text = VelocityBlock.TypeHow, TextWrapping = TextWrapping.Wrap });
            part.Children.Add(readingsHere);
            CountReadingsHere();
            part.Children.Add(readingsCount);
            part.Children.Add(Row(Primary(VelocityBlock.MatchToShots, MatchReadingsHere)));
        }

        return part;
    }

    /// <summary>The first chronograph file in a drag, or null.</summary>
    private static string? ChronographDropped(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles() is { } files
            ? files.Select(f => f.TryGetLocalPath()).OfType<string>().FirstOrDefault(p => ChronographFiles.Extensions.Contains(Path.GetExtension(p), StringComparer.OrdinalIgnoreCase))
            : null;

    /// <summary>"12 of 23 entered", under the box, as it is typed.</summary>
    private void CountReadingsHere()
    {
        var (velocities, _) = Chronograph.Read(readingsHere.Text ?? "");
        readingsCount.Text = VelocityBlock.Entered(velocities.Count, ChronographShots().Count);
    }

    /// <summary>Import a file, from the block: the file is read as Ballistics reads it and its speeds put in the box, ready to match.</summary>
    private async Task ImportReadingsHere()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "import-chronograph-analysis"));
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import a chronograph file",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Chronograph files") { Patterns = [.. ChronographFiles.Extensions.Select(e => "*" + e)] }, FilePickerFileTypes.All],
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is { } path)
        {
            await ReadingsFromFile(path);
        }
    }

    /// <summary>A file picked or dropped on the block: read, and its speeds in the box; a workbook of several strings goes to Ballistics to choose.</summary>
    internal async Task ReadingsFromFile(string path)
    {
        await ImportChronographPath(path);
        if (chronoValues.Count == 0)
        {
            if (chronoImport.Children.Count > 0)
            {
                status.Text = "That file holds more than one string: choose the one for this sheet in Ballistics, Chronograph.";
                ShowChronographSection();
            }

            return;
        }

        readingsHere.Text = chronoReadings.Text;
        readingsOpen = true;
        status.Text = $"{chronoValues.Count} readings from {Path.GetFileName(path)}. Press {VelocityBlock.MatchToShots}.";
        ShowVelocity(session.State);
    }

    /// <summary>
    /// Match to shots: the speeds read and paired with the shots as Ballistics' chronograph section proposes, and kept on the session, so the
    /// block shows what they say. A file's own evidence (its pauses, the shots it left out) is kept in the proposal when the box still holds it.
    /// </summary>
    internal void MatchReadingsHere()
    {
        if (chronoValues.Count == 0 || !string.Equals(readingsHere.Text?.Trim(), chronoReadings.Text?.Trim(), StringComparison.Ordinal))
        {
            ReadChronograph("Typed in", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), readingsHere.Text ?? "");
        }

        if (chronoValues.Count < 2)
        {
            problem.Text = "Type at least two speeds, in the order the shots were fired.";
            return;
        }

        int readings = chronoValues.Count;
        AcceptChronograph();
        readingsOpen = false;
        readingsHere.Text = "";
        status.Text += $" Matched as Ballistics would propose; change any reading there, under Chronograph.";
        DiagnosticLog.Info("chronograph.analysis", ("readings", readings));
    }

    /// <summary>For the headless tests: types into the block's box, as a person does.</summary>
    internal void TypeReadingsHere(string text)
    {
        readingsOpen = true;
        readingsHere.Text = text;
        CountReadingsHere();
    }

    internal string ReadingsCountText => readingsCount.Text ?? "";
}
