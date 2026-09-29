using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// The printer check on the phone, NOTES-FROM-PLANNING.md entries 272 and 273, the three screens Alan approved: pick a method (name the
/// printer, print the check page, then a card and one photo, a digital caliper, a ruler or tape, or a scanner); measure, the card photo on
/// the camera screen; and the result, across and down with its uncertainty, what it means, the paper's edge, then "Save and finish" or
/// "Check again another way". Reached from Settings under Printers, from the first run, and from a result in the sheet's own inches.
/// </summary>
internal sealed class PrinterCheckPage : UserControl
{
    private readonly Action done;
    private string name;

    internal PrinterCheckPage(Action done, string? name = null)
    {
        this.done = done;
        this.name = name ?? Phone.Settings.LoadChosenPrinter()?.Name ?? PrinterProfile.DefaultName;
        DiagnosticLog.Info("printer.check", ("step", "start"));
        Start(PrinterMethod.Card);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>The check pages in the library; the one for this phone's paper first.</summary>
    private static IReadOnlyList<TargetDefinition> Pages() =>
        [.. PhoneAnalysis.Library().Where(PrinterCheck.IsCheckPage)
            .OrderBy(d => (d.Page.Size == PageSize.Letter) == AppSettingsStore.LetterRegion(AppSettingsStore.Region()) ? 0 : 1)];

    private static StackPanel Dots(int on)
    {
        var dots = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
        for (int i = 0; i < 3; i++)
        {
            dots.Children.Add(new Border { Width = 28, Height = 4, CornerRadius = new Avalonia.CornerRadius(2), Background = new SolidColorBrush(i < on ? Color.FromRgb(0xe0, 0x91, 0x2f) : Color.FromRgb(0x3a, 0x3f, 0x47)) });
        }

        return dots;
    }

    private static StackPanel Column(int step, string title) => new() { Spacing = 12, Children = { Dots(step), Screens.Title(title) } };

    /// <summary>Screen 1: name the printer, print the page, choose how to measure it.</summary>
    private void Start(PrinterMethod method)
    {
        var column = Column(1, "Check your printer");
        column.Children.Add(Screens.Dim("Printers often print a little small. Check once, and every photo of a GroupLab sheet from this printer measures in real inches."));
        var named = new TextBox { Text = name, MinHeight = Screens.Touch };
        column.Children.Add(Screens.Card(Screens.Dim("Name this printer"), named));
        column.Children.Add(Screens.Choice("Print the check page", () => Shell.Current?.Show(Shell.Place.Targets)));
        column.Children.Add(Screens.Dim("Then measure it one way:"));
        var chosen = method;
        var choices = new (PrinterMethod Method, string Title, string Detail)[]
        {
            (PrinterMethod.Card, "A card and one photo", "Easiest. Any bank, gift or ID card. About 0.3%."),
            (PrinterMethod.Caliper, "Digital caliper", "Most exact without a scanner. About 0.1%."),
            (PrinterMethod.Ruler, "Ruler or tape", "Two long lines. About 0.3 to 0.5%."),
            (PrinterMethod.Scan, "Scanner", "Best, if you have one. Scan the page."),
        };
        foreach (var (m, title, detail) in choices)
        {
            var radio = new RadioButton
            {
                GroupName = "printerMethod",
                IsChecked = m == method,
                MinHeight = Screens.Touch,
                Content = new StackPanel { Children = { new TextBlock { Text = title, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold }, Screens.Dim(detail) } },
            };
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true)
                {
                    chosen = m;
                }
            };
            column.Children.Add(radio);
        }

        column.Children.Add(Screens.Primary("Next", () =>
        {
            name = string.IsNullOrWhiteSpace(named.Text) ? PrinterProfile.DefaultName : named.Text.Trim();
            Measure(chosen);
        }));
        column.Children.Add(Screens.Choice("Skip for now, run it later from Settings", done));
        Content = Screens.Page(column);
    }

    /// <summary>Screen 2: the card on the camera, a scan from the phone's files, or two typed lengths.</summary>
    private void Measure(PrinterMethod method)
    {
        var column = Column(2, method switch
        {
            PrinterMethod.Card => "A card and one photo",
            PrinterMethod.Caliper => "Measure with a caliper",
            PrinterMethod.Ruler => "Measure with a ruler or tape",
            _ => "Scan the check page",
        });
        var said = Screens.Line("");
        if (method is PrinterMethod.Card or PrinterMethod.Scan)
        {
            column.Children.Add(Screens.Dim(method == PrinterMethod.Card
                ? "Lay any bank, gift or ID card inside the outline, flat. Hold the phone over the whole page, straight down, in good light. A card with color works best."
                : "Scan the check page at 300 dpi or more, then choose the scan from the phone's files."));
            if (method == PrinterMethod.Card)
            {
                column.Children.Add(Screens.Primary("Take the picture", () =>
                {
                    if (!Phone.Platform.CameraAllowed())
                    {
                        said.Text = "GroupLab needs the camera to take the picture. Allow it, then press Take the picture again.";
                        return;
                    }

                    var back = Content;
                    Shell.Current?.Immersive(true);
                    Content = Phone.Platform.Camera((path, torch) =>
                    {
                        Shell.Current?.Immersive(false);
                        Content = back;
                        _ = Read(path, said);
                    }, () =>
                    {
                        Shell.Current?.Immersive(false);
                        Content = back;
                    }, () =>
                    {
                        Shell.Current?.Immersive(false);
                        Content = back;
                        _ = Choose(said);
                    });
                }));
            }

            column.Children.Add(Screens.Choice(method == PrinterMethod.Card ? "Choose a photo instead" : "Choose the scan", () => _ = Choose(said)));
        }
        else
        {
            bool caliper = method == PrinterMethod.Caliper;
            double acrossDrawn = (caliper ? GridStyle4.CaliperDmm : GridStyle4.RulerAcrossDmm) / 254.0;
            double downDrawn = (caliper ? GridStyle4.CaliperDmm : GridStyle4.RulerDownDmm) / 254.0;
            column.Children.Add(Screens.Dim(caliper
                ? "Measure between the centers of the crosshairs: across the top, then down the right side. Each is drawn 150.00 mm apart."
                : "Measure the two long lines end to end, tick to tick: across the bottom, drawn 190.0 mm, and down the left side, drawn 250.0 mm."));
            var unit = new ComboBox { ItemsSource = new[] { "mm", "in" }, SelectedIndex = 0, MinHeight = Screens.Touch };
            var across = new TextBox { MinHeight = Screens.Touch, PlaceholderText = caliper ? "Across, 150.00" : "Across, 190.0" };
            var down = new TextBox { MinHeight = Screens.Touch, PlaceholderText = caliper ? "Down, 150.00" : "Down, 250.0" };
            column.Children.Add(Screens.Card(across, down, unit));
            column.Children.Add(Screens.Primary("Work it out", () =>
            {
                double per = unit.SelectedIndex == 1 ? 1 : 1 / 25.4;
                double? Inches(string? text) => double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out double v)
                    || double.TryParse((text ?? "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v * per : null;
                if (Inches(across.Text) is not { } a || Inches(down.Text) is not { } d)
                {
                    said.Text = "Type both numbers as the tool reads them, such as 148.8, and choose the unit.";
                    return;
                }

                if (PrinterProfile.FromLengths(name, method, a, acrossDrawn, d, downDrawn, Today()) is not { } profile)
                {
                    said.Text = "Those make the page far from its true size, which no printer does. Check each length is between the right marks, and the unit.";
                    return;
                }

                Result(profile, null);
            }));
        }

        column.Children.Add(said);
        column.Children.Add(Screens.Choice("Back", () => Start(method)));
        Content = Screens.Page(column);
    }

    private async Task Choose(TextBlock said)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "The picture of the check page",
            AllowMultiple = false,
            FileTypeFilter = [Avalonia.Platform.Storage.FilePickerFileTypes.ImageAll],
        });
        if (files.Count == 0)
        {
            return;
        }

        string copy = Path.Combine(Phone.Platform.CacheFolder, "check" + Path.GetExtension(files[0].Name));
        await using (var from = await files[0].OpenReadAsync())
        await using (var to = File.Create(copy))
        {
            await from.CopyToAsync(to);
        }

        await Read(copy, said);
    }

    /// <summary>A picture of the check page measured: by its resolution if it is a scan, by the card if it is a photograph.</summary>
    private async Task Read(string path, TextBlock said)
    {
        said.Text = "Measuring…";
        var result = await Task.Run(() =>
        {
            if (PhoneAnalysis.Prepare(path) is not { } working)
            {
                return new PrinterCheckResult(null, "That file is not a picture GroupLab can read.", null, null);
            }

            try
            {
                var (grey, _) = ImageLoader.Load(working.Path);
                var pages = Pages();
                var identity = GroupLab.Core.Registration.SheetIdentification.Identify(grey, pages, new OpenCvSharpBackend(), new GroupLab.Core.Trace.TraceRecorder());
                var page = pages.FirstOrDefault(p => p.Id == identity.DefinitionId) ?? pages.FirstOrDefault();
                return page is null
                    ? new PrinterCheckResult(null, PrinterCheck.NoPage, null, null)
                    : PrinterCheck.Measure(grey, working.Metadata, page, new OpenCvSharpBackend(), name, Today());
            }
            finally
            {
                PhoneAnalysis.Forget(working);
            }
        });
        DiagnosticLog.Info("printer.check", ("step", "measured"), ("found", result.Profile is not null));
        Dispatcher.UIThread.Post(() =>
        {
            if (result.Profile is { } profile)
            {
                Result(profile, result.Paper);
            }
            else
            {
                said.Text = result.Why ?? PrinterCheck.NoPage;
            }
        });
    }

    /// <summary>Screen 3: the figures, what they mean, and saving them.</summary>
    private void Result(PrinterProfile profile, PaperEdge? paper)
    {
        var column = Column(3, profile.Headline);
        var amber = new SolidColorBrush(Color.FromRgb(0xe0, 0x91, 0x2f));
        TextBlock Big(double v) => new() { Text = string.Create(CultureInfo.InvariantCulture, $"{v * 100:0.0}%"), FontSize = 30, Foreground = amber };
        var figures = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto") };
        void Add(Control c, int col, int row)
        {
            Grid.SetColumn(c, col);
            Grid.SetRow(c, row);
            figures.Children.Add(c);
        }

        Add(Screens.Dim("Across"), 0, 0);
        Add(Screens.Dim("Down"), 1, 0);
        Add(Big(profile.Across), 0, 1);
        Add(Big(profile.Down), 1, 1);
        column.Children.Add(Screens.Card(figures, Screens.Dim(string.Create(CultureInfo.InvariantCulture, $"Give or take {profile.Uncertainty * 100:0.0}%, measured {profile.How}."))));
        column.Children.Add(Screens.Card(
            Screens.Row("What it means", PrinterCheck.WhatItMeans(profile), () => { }),
            Screens.Row("From now on", $"photos from {profile.Name} are corrected", () => { }),
            Screens.Row("Paper edge check", paper is null ? "not measured on this picture" : PaperEdgeCheck.Agreement(paper, profile), () => { })));
        column.Children.Add(Screens.Dim($"Each result from a photo will say \"{profile.Line}\". You can check again, add another printer, or turn this off in Settings, under Printers."));
        column.Children.Add(Screens.Primary("Save and finish", () =>
        {
            Phone.Settings.SavePrinter(profile);
            DiagnosticLog.Info("printer.check", ("step", "saved"), ("method", profile.Method.ToString()));
            done();
        }));
        column.Children.Add(Screens.Choice("Check again another way", () => Start(profile.Method == PrinterMethod.Card ? PrinterMethod.Caliper : PrinterMethod.Card)));
        Content = Screens.Page(column);
    }
}
