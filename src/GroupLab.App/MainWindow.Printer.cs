using System.Globalization;
using Avalonia.Controls;
using GroupLab.App.Theme;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// Real inches on photographs, NOTES-FROM-PLANNING.md entry 271: the print scale measured once per printer, from a scan or one ruler
/// distance, and applied to photographs of that printer's sheets from then on. A scan's result offers to keep the scale it measured; a
/// photograph's offers the ruler, for that one sheet or to keep; Settings chooses which printer photographs are corrected for.
/// </summary>
public sealed partial class MainWindow
{
    private readonly StackPanel printerPanel = new() { Spacing = Tokens.Space8, IsVisible = false };

    private readonly ComboBox printerChoice = new() { MinWidth = 280 };

    private bool showingPrinters;

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>What a sheet's result offers about its print scale, under the line that says what the scale is.</summary>
    private void ShowPrinterOffer(AutomaticResult result)
    {
        printerPanel.Children.Clear();
        printerPanel.IsVisible = false;
        if (result.Scale is not SheetReference || result.Definition is not { } definition)
        {
            return;
        }

        if (PrinterProfile.FromScan(null, result.Measurement.Scale, Today()) is { } measured)
        {
            // Entry 271 section 2: a scan measured the printer, so offer to keep it for photographs, under a name the person chooses.
            var name = new TextBox { Width = 200, Text = settingsStore.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName };
            printerPanel.Children.Add(Line(PrinterProfile.Offer(measured.Scale)));
            printerPanel.Children.Add(Row(name, Button("Use it for photos", () =>
            {
                var kept = measured with { Name = string.IsNullOrWhiteSpace(name.Text) ? PrinterProfile.DefaultName : name.Text.Trim() };
                settingsStore.SavePrinter(kept);
                ShowPrinters();
                printerPanel.Children.Clear();
                printerPanel.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                    $"Kept. Photographs of sheets from {kept.Name} are now corrected for its {kept.Scale * 100:0.0} percent; Settings can change which printer is used.")));
            })));
            printerPanel.IsVisible = true;
            return;
        }

        if (RulerSpan.Of(definition) is { } span)
        {
            printerPanel.Children.Add(Row(Button("Measure this sheet with a ruler", () => ShowRuler(span))));
            printerPanel.IsVisible = true;
        }
    }

    /// <summary>Entry 271 section 2: one ruler distance corrects this sheet, and can be kept for the printer.</summary>
    private void ShowRuler(RulerSpan span)
    {
        printerPanel.Children.Clear();
        var reading = new TextBox { Width = 120, PlaceholderText = "5 3/4 or 146 mm" };
        var keep = new CheckBox { Content = "Keep it for photos of sheets from this printer, named", IsChecked = true };
        var name = new TextBox { Width = 180, Text = settingsStore.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName };
        var said = Line("");
        said.IsVisible = false;
        printerPanel.Children.Add(Line(span.Ask));
        printerPanel.Children.Add(Row(reading, Button("Correct this sheet", () =>
        {
            if (RulerSpan.ReadInches(reading.Text) is not { } inches)
            {
                said.Text = "Type the distance as it reads on the ruler, such as 5 3/4, 5.75 in or 146 mm.";
                said.IsVisible = true;
                return;
            }

            if (PrinterProfile.FromRuler(name.Text, inches, span.DrawnInches, Today()) is not { } profile)
            {
                said.Text = string.Create(CultureInfo.InvariantCulture,
                    $"That makes the sheet {inches / span.DrawnInches * 100:0} percent of its size, which no printer does. Measure again, from the center of bull {span.From} to the center of bull {span.To}.");
                said.IsVisible = true;
                return;
            }

            bool kept = keep.IsChecked == true;
            string line = kept
                ? profile.Line
                : string.Create(CultureInfo.InvariantCulture, $"Corrected for this sheet's {profile.Scale * 100:0.0} percent, measured with a ruler.");
            if (kept)
            {
                settingsStore.SavePrinter(profile);
                ShowPrinters();
            }

            if (session.State.Scale is SheetReference sheet)
            {
                session.SetScale(sheet with { PrintScale = profile.Scale, ScaleFrom = line });
            }

            printScale.Text = line;
            printScale.IsVisible = true;
            printerPanel.Children.Clear();
            printerPanel.IsVisible = false;
            Refresh();
        })));
        printerPanel.Children.Add(Row(keep, name));
        printerPanel.Children.Add(said);
    }

    /// <summary>Settings' printer section: which saved printer photographs are corrected for, or none.</summary>
    private void BuildPrinterSettings(StackPanel column)
    {
        column.Children.Add(Ruled("Printer scale"));
        column.Children.Add(Line(DetectionAdvice.OncePerPrinter));
        column.Children.Add(printerChoice);
        printerChoice.SelectionChanged += (_, _) =>
        {
            if (!showingPrinters && printerChoice.SelectedIndex >= 0)
            {
                var printers = settingsStore.LoadPrinters();
                settingsStore.ChoosePrinter(printerChoice.SelectedIndex == 0 ? null : printers[printerChoice.SelectedIndex - 1].Name);
            }
        };
        ShowPrinters();
    }

    private void ShowPrinters()
    {
        showingPrinters = true;
        var printers = settingsStore.LoadPrinters();
        var chosen = settingsStore.LoadChosenPrinter();
        printerChoice.ItemsSource = new[] { "None: photographs stay in the sheet's own inches" }
            .Concat(printers.Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Name}: {p.Scale * 100:0.0} percent, measured {p.How}")))
            .ToList();
        printerChoice.SelectedIndex = chosen is null ? 0 : 1 + printers.ToList().FindIndex(p => p.Name == chosen.Name);
        showingPrinters = false;
    }
}
