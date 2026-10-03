using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.App;

/// <summary>
/// The printer check, NOTES-FROM-PLANNING.md entries 272 and 273, as Alan approved it: three screens in one window. Pick a method (name the
/// printer, print the check page, then a card and one photo, a digital caliper, a ruler or tape, or a scanner); measure; and the result,
/// across and down with its uncertainty, what it means for a group, and the paper's edge agreeing or not, then "Save and finish" or "Check
/// again another way". Offered at first run, again the first time a sheet is printed, and always in Settings, under Printers.
/// </summary>
public sealed partial class MainWindow
{
    private Window? printerWizard;

    /// <summary>What a picture of the check page opened as a target says (entry 273).</summary>
    internal const string CheckPageOpened = "This is the printer check page. The printer check measures it: choose how you measured, then open this picture there.";

    /// <summary>
    /// Entry 273: the printer check offered after a sheet is printed, once, while no printer has been checked; never after printing the check
    /// page itself, which is already the check.
    /// </summary>
    private Action? PrinterOfferAfterPrinting(TargetDefinition printed)
    {
        if (PrinterCheck.IsCheckPage(printed) || settingsStore.LoadPrinters().Count > 0 || settingsStore.LoadPrinterOffered())
        {
            return null;
        }

        settingsStore.SavePrinterOffered();
        return () => OpenPrinterCheck();
    }

    /// <summary>
    /// Entry 273: the first run's printer card, beside its questions and never holding the screen open: a person who answers the others
    /// has passed it by, and it is offered once.
    /// </summary>
    private StackPanel? FirstRunPrinterCard(Action hide)
    {
        if (settingsStore.LoadPrinters().Count > 0 || settingsStore.LoadPrinterOffered())
        {
            return null;
        }

        settingsStore.SavePrinterOffered();
        var card = new StackPanel { Spacing = Tokens.Space8 };
        card.Children.Add(new TextBlock { Text = "Check your printer", Classes = { AppStyles.Title } });
        card.Children.Add(Line("Printers often print a little small. Check once, with a card and one photo, a caliper, a ruler or a scanner, and every photo of a GroupLab sheet from that printer measures in real inches."));
        card.Children.Add(Row(Button("Check my printer", () =>
        {
            hide();
            OpenPrinterCheck();
        }), Button("Skip for now, run it later from Settings", hide)));
        return card;
    }

    /// <summary>The check page for this machine's paper: A4, except where Letter is the paper sold.</summary>
    private TargetDefinition? CheckPageDefinition() => ShippedDefinitions()
        .Where(PrinterCheck.IsCheckPage)
        .OrderBy(d => PageSizes.IsLabel(d.Page.Size) ? 2 : (d.Page.Size == PageSize.Letter) == AppSettingsStore.LetterRegion(AppSettingsStore.Region()) ? 0 : 1)
        .FirstOrDefault();

    /// <summary>Opens the printer check, or brings it forward where it is open; <paramref name="name"/> names a printer checked again.</summary>
    internal void OpenPrinterCheck(string? name = null)
    {
        if (printerWizard is { IsVisible: true })
        {
            printerWizard.Activate();
            return;
        }

        var window = new Window
        {
            Title = "Check your printer",
            Width = 520,
            Height = 760,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        printerWizard = window;
        window.Closed += (_, _) => printerWizard = null;
        DiagnosticLog.Info("printer.check", ("step", "start"));
        // A new printer takes "My printer" where no printer has it, else "Printer 2" and on, so adding one never overwrites another.
        var taken = settingsStore.LoadPrinters().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        string fresh = taken.Contains(PrinterProfile.DefaultName) ? Enumerable.Range(2, 99).Select(n => $"Printer {n}").First(n => !taken.Contains(n)) : PrinterProfile.DefaultName;
        ShowPrinterStart(window, name is "" ? fresh : name ?? settingsStore.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName, PrinterMethod.Card);
        window.Show(this);
    }

    private static StackPanel Dots(int on)
    {
        var dots = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = Tokens.Space4 };
        for (int i = 0; i < 3; i++)
        {
            dots.Children.Add(new Border { Width = 28, Height = 4, CornerRadius = new Avalonia.CornerRadius(2), Background = i < on ? new SolidColorBrush(Tokens.MarkSelected) : new SolidColorBrush(Tokens.MarkFaint) });
        }

        return dots;
    }

    private static StackPanel WizardPage(int step, string title)
    {
        var page = new StackPanel { Spacing = Tokens.Space12, Margin = new Avalonia.Thickness(Tokens.Space20) };
        page.Children.Add(Dots(step));
        page.Children.Add(new TextBlock { Text = title, Classes = { AppStyles.Title }, TextWrapping = TextWrapping.Wrap });
        return page;
    }

    /// <summary>Screen 1: name the printer, print the page, choose how to measure it.</summary>
    private void ShowPrinterStart(Window window, string name, PrinterMethod method)
    {
        var page = WizardPage(1, "Check your printer");
        page.Children.Add(Line("Printers often print a little small. Check once, and every photo of a GroupLab sheet from this printer measures in real inches."));
        page.Children.Add(FieldLabel("Name this printer"));
        var named = new TextBox { Text = name, MinWidth = 260, HorizontalAlignment = HorizontalAlignment.Left };
        page.Children.Add(named);
        page.Children.Add(Row(Button("Print the check page", () =>
        {
            if (CheckPageDefinition() is { } check && LibrarySheets().FirstOrDefault(s => s.Definition.Id == check.Id) is { } sheet)
            {
                OpenPrint(sheet, design: false);
                Activate();
            }
        })));
        page.Children.Add(Line("Then measure it one way:"));
        var choices = new (PrinterMethod Method, string Title, string Detail)[]
        {
            (PrinterMethod.Card, "A card and one photo", "Easiest. Any bank, gift or ID card. About 0.3%."),
            (PrinterMethod.Caliper, "Digital caliper", "Most exact without a scanner. About 0.1%."),
            (PrinterMethod.Ruler, "Ruler or tape", "Two long lines. About 0.3 to 0.5%."),
            (PrinterMethod.Scan, "Scanner", "Best, if you have one. Scan the page."),
        };
        var chosen = method;
        foreach (var (m, title, detail) in choices)
        {
            var radio = new RadioButton { GroupName = "printerMethod", IsChecked = m == method, Content = new StackPanel { Children = { new TextBlock { Text = title, FontWeight = FontWeight.SemiBold }, Line(detail) } } };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    chosen = m;
                }
            };
            page.Children.Add(radio);
        }

        page.Children.Add(Row(
            Button("Next", () => ShowPrinterMeasure(window, string.IsNullOrWhiteSpace(named.Text) ? PrinterProfile.DefaultName : named.Text.Trim(), chosen)),
            Button("Skip for now, run it later from Settings", window.Close)));
        window.Content = new ScrollViewer { Content = page };
    }

    /// <summary>Screen 2: the measuring, by the method chosen.</summary>
    private void ShowPrinterMeasure(Window window, string name, PrinterMethod method)
    {
        var page = WizardPage(2, method switch
        {
            PrinterMethod.Card => "A card and one photo",
            PrinterMethod.Caliper => "Measure with a caliper",
            PrinterMethod.Ruler => "Measure with a ruler or tape",
            _ => "Scan the check page",
        });
        var said = new TextBlock { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert }, IsVisible = false };
        void Wrong(string words)
        {
            said.Text = words;
            said.IsVisible = true;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        if (method is PrinterMethod.Card or PrinterMethod.Scan)
        {
            page.Children.Add(Line(method == PrinterMethod.Card
                ? "Lay any bank, gift or ID card inside the outline, flat, and take one photo of the whole page, straight down, in good light. A card with color works best. Then open the photo here."
                : "Scan the check page, at 300 dpi or more, as it lies on the glass. Then open the scan here."));
            page.Children.Add(Row(Button(method == PrinterMethod.Card ? "Open the photo" : "Open the scan", async () =>
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = method == PrinterMethod.Card ? "The photo of the check page" : "The scan of the check page",
                    AllowMultiple = false,
                    FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.tif", "*.tiff", "*.heic"] }],
                });
                if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path || CheckPageDefinition() is not { } check)
                {
                    return;
                }

                said.Text = "Measuring…";
                said.IsVisible = true;
                var result = await Task.Run(() =>
                {
                    var (grey, metadata) = ImageLoader.Load(path);
                    // The page the picture shows, Letter or A4, from its own codes; the machine's own paper when it cannot be told.
                    var pages = ShippedDefinitions().Where(PrinterCheck.IsCheckPage).ToList();
                    var identity = GroupLab.Core.Registration.SheetIdentification.Identify(grey, pages, new OpenCvSharpBackend(), new GroupLab.Core.Trace.TraceRecorder());
                    var shown = pages.FirstOrDefault(p => p.Id == identity.DefinitionId) ?? check;
                    return PrinterCheck.Measure(grey, metadata, shown, new OpenCvSharpBackend(), name, today);
                });
                DiagnosticLog.Info("printer.check", ("step", "measured"), ("method", method.ToString()), ("found", result.Profile is not null));
                if (result.Profile is { } profile)
                {
                    ShowPrinterResult(window, profile, result.Paper);
                }
                else
                {
                    Wrong(result.Why ?? PrinterCheck.NoPage);
                }
            })));
        }
        else
        {
            // Two lengths and a unit, with the page itself beside them to show where to measure.
            bool caliper = method == PrinterMethod.Caliper;
            double acrossDrawn = (caliper ? GridStyle4.CaliperDmm : GridStyle4.RulerAcrossDmm) / 254.0;
            double downDrawn = (caliper ? GridStyle4.CaliperDmm : GridStyle4.RulerDownDmm) / 254.0;
            page.Children.Add(Line(caliper
                ? "Measure between the centers of the crosshairs: across the top, then down the right side. Each is drawn 150.00 mm apart."
                : "Measure the two long lines end to end, tick to tick: across the bottom, drawn 190.0 mm, and down the left side, drawn 250.0 mm."));
            if (CheckPageDefinition() is { } check && PrintPanel.Preview(check, note: false) is { } picture)
            {
                page.Children.Add(new Image { Source = picture, MaxHeight = 280, HorizontalAlignment = HorizontalAlignment.Left });
            }

            var unit = new ComboBox { ItemsSource = new[] { "mm", "in" }, SelectedIndex = 0, MinWidth = 90 };
            var across = new TextBox { Width = 120, PlaceholderText = caliper ? "150.00" : "190.0" };
            var down = new TextBox { Width = 120, PlaceholderText = caliper ? "150.00" : "250.0" };
            page.Children.Add(Row(new TextBlock { Text = "Across", Width = 70, VerticalAlignment = VerticalAlignment.Center }, across));
            page.Children.Add(Row(new TextBlock { Text = "Down", Width = 70, VerticalAlignment = VerticalAlignment.Center }, down));
            page.Children.Add(Row(new TextBlock { Text = "Unit", Width = 70, VerticalAlignment = VerticalAlignment.Center }, unit));
            page.Children.Add(Row(Button("Work it out", () =>
            {
                double per = unit.SelectedIndex == 1 ? 1 : 1 / 25.4;
                bool Read(string? text, out double inches)
                {
                    inches = 0;
                    if (!double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out double v)
                        && !double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                    {
                        return false;
                    }

                    inches = v * per;
                    return v > 0;
                }

                if (!Read(across.Text, out double a) || !Read(down.Text, out double d))
                {
                    Wrong("Type both numbers as the tool reads them, such as 148.8, and choose the unit.");
                    return;
                }

                if (PrinterProfile.FromLengths(name, method, a, acrossDrawn, d, downDrawn, today) is not { } profile)
                {
                    Wrong("Those make the page far from its true size, which no printer does. Check that each length is measured between the right marks, and the unit.");
                    return;
                }

                DiagnosticLog.Info("printer.check", ("step", "measured"), ("method", method.ToString()), ("found", true));
                ShowPrinterResult(window, profile, null);
            })));
        }

        page.Children.Add(said);
        page.Children.Add(Row(Button("Back", () => ShowPrinterStart(window, name, method))));
        window.Content = new ScrollViewer { Content = page };
    }

    /// <summary>Screen 3: the result, and saving it.</summary>
    private void ShowPrinterResult(Window window, PrinterProfile profile, PaperEdge? paper)
    {
        var page = WizardPage(3, profile.Headline);
        var figures = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto") };
        void Figure(int column, string label, double value)
        {
            var l = Line(label);
            Grid.SetColumn(l, column);
            var v = new TextBlock { Text = string.Create(CultureInfo.InvariantCulture, $"{value * 100:0.0}%"), Classes = { AppStyles.HeadlineFigure } };
            Grid.SetColumn(v, column);
            Grid.SetRow(v, 1);
            figures.Children.Add(l);
            figures.Children.Add(v);
        }

        Figure(0, "Across", profile.Across);
        Figure(1, "Down", profile.Down);
        page.Children.Add(figures);
        page.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"Give or take {profile.Uncertainty * 100:0.0}%, measured {profile.How}.")));
        var rows = new Grid { ColumnDefinitions = new ColumnDefinitions("160,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = Tokens.Space8 };
        void Kv(int row, string key, string value)
        {
            var k = Line(key);
            Grid.SetRow(k, row);
            var v = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
            Grid.SetRow(v, row);
            Grid.SetColumn(v, 1);
            rows.Children.Add(k);
            rows.Children.Add(v);
        }

        Kv(0, "What it means", PrinterCheck.WhatItMeans(profile));
        Kv(1, "From now on", $"photos from {profile.Name} are corrected");
        Kv(2, "Paper edge check", paper is null ? "not measured on this picture" : PaperEdgeCheck.Agreement(paper, profile));
        page.Children.Add(rows);
        page.Children.Add(Line($"Each result from a photo will say \"{profile.Line}\". You can check again, add another printer, or turn this off in Settings, under Printers."));
        page.Children.Add(Row(
            Button("Save and finish", () =>
            {
                settingsStore.SavePrinter(profile);
                ShowPrinters();
                DiagnosticLog.Info("printer.check", ("step", "saved"), ("method", profile.Method.ToString()));
                status.Text = profile.Result + ".";
                window.Close();
            }),
            Button("Check again another way", () => ShowPrinterStart(window, profile.Name, profile.Method == PrinterMethod.Card ? PrinterMethod.Caliper : PrinterMethod.Card))));
        window.Content = new ScrollViewer { Content = page };
    }
}
