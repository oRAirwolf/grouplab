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
            // Entry 273 section 5: in the sheet's own inches, with the way to real inches beside it.
            printerPanel.Children.Add(Row(Button("Check your printer", () => OpenPrinterCheck()), Button("Measure this sheet with a ruler", () => ShowRuler(span))));
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
                : string.Create(CultureInfo.InvariantCulture, $"Corrected for this sheet, {profile.Percentages}, measured with a ruler");
            if (kept)
            {
                settingsStore.SavePrinter(profile);
                ShowPrinters();
            }

            if (session.State.Scale is SheetReference sheet)
            {
                session.SetScale(sheet.CorrectedBy(profile, line));
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

    /// <summary>
    /// Settings, under Printers (entry 273 section 4): every printer saved with its figures and date, which one photographs are corrected for,
    /// Check again, Delete, Add a printer, and a switch to turn correction off.
    /// </summary>
    private void BuildPrinterSettings(StackPanel column)
    {
        column.Children.Add(Ruled("Printers"));
        column.Children.Add(Line(DetectionAdvice.OncePerPrinter));
        column.Children.Add(printerList);
        var correcting = new CheckBox { Content = Wrapped("Correct photographs by the chosen printer's scale"), IsChecked = settingsStore.LoadPrinterCorrection() };
        correcting.IsCheckedChanged += (_, _) => settingsStore.SavePrinterCorrection(correcting.IsChecked == true);
        column.Children.Add(correcting);
        column.Children.Add(Row(Button("Add a printer", () => OpenPrinterCheck(""))));
        ShowPrinters();
    }

    private readonly StackPanel printerList = new() { Spacing = Tokens.Space8 };

    private void ShowPrinters()
    {
        showingPrinters = true;
        printerList.Children.Clear();
        var printers = settingsStore.LoadPrinters();
        var chosen = settingsStore.LoadChosenPrinter();
        if (printers.Count == 0)
        {
            printerList.Children.Add(Line("No printer checked yet, so photographs are measured in the sheet's own inches."));
        }

        foreach (var p in printers)
        {
            var use = new RadioButton
            {
                GroupName = "printerChosen",
                IsChecked = p.Name == chosen?.Name,
                Content = Wrapped(string.Create(CultureInfo.InvariantCulture, $"{p.Name}: {p.Percentages}, measured {p.How}")),
            };
            use.IsCheckedChanged += (_, _) =>
            {
                if (!showingPrinters && use.IsChecked == true)
                {
                    settingsStore.ChoosePrinter(p.Name);
                }
            };
            printerList.Children.Add(use);
            printerList.Children.Add(Row(
                Button("Check again", () => OpenPrinterCheck(p.Name)),
                Button("Delete", () =>
                {
                    settingsStore.DeletePrinter(p.Name);
                    ShowPrinters();
                })));
        }

        showingPrinters = false;
    }
}
