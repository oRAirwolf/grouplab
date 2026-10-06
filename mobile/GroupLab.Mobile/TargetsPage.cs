using System.Globalization;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd;
using GroupLab.Core.Printing.Thermal;
using GroupLab.Core.Rendering;
using Orientation = Avalonia.Layout.Orientation;
using RadioButton = Avalonia.Controls.RadioButton;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.4: the Targets screen on the phone. "Made for your optic" first, because on a phone it is the
/// quickest way to a sheet that suits the rifle in hand, then the library by family. A sheet opens to a picture of it and two big buttons:
/// Print, which is Android's own print dialog, and Share the PDF, for printing from a computer. The PDF is the one the desktop makes, with
/// the note to print at actual size, so a sheet printed from the phone reads the same.
/// </summary>
public sealed class TargetsPage : UserControl
{
    private readonly TextBox distance = Number("100").Id("targets-distance");
    private readonly TextBox magnification = Number("").Id("targets-magnification");
    private readonly TextBox dot = Number("").Id("targets-dot");
    private readonly TextBox shots = Number("25").Id("targets-shots");
    private readonly RadioButton letter = Screens.Radio("page", "Letter", AppSettingsStore.LetterRegion(AppSettingsStore.Region()));
    private readonly RadioButton a4 = Screens.Radio("page", "A4", !AppSettingsStore.LetterRegion(AppSettingsStore.Region()));
    private readonly RadioButton disc = Screens.Radio("shape", "Disc", true);
    private readonly RadioButton diamond = Screens.Radio("shape", "Diamond", false);
    private readonly StackPanel said = new() { Spacing = 8 };

    public TargetsPage()
    {
        Content = List();
    }

    private static TextBox Number(string text)
    {
        var box = new TextBox { Text = text, MinHeight = Screens.Touch, MinWidth = 96, HorizontalAlignment = HorizontalAlignment.Left };
        return Screens.Numeric(box);
    }

    // The fields and choices are kept between visits to the list, so each is taken from the list it was last in: "Back to the targets"
    // built the list again around fields that still had a parent, which Avalonia refuses (found by entry 348's Back from its steps).
    private static Control Field(string words, TextBox box) => new StackPanel { Spacing = 4, Children = { Screens.Dim(words), Screens.Detach(box) } };

    private Control List()
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Targets"));
        column.Children.Add(Screens.Line("Print a sheet GroupLab reads by itself, or share it as a PDF to print on a computer. Print it at actual size."));

        // Entry 246, look B: the optic's form on one card, two fields to a row, the choices as cards, and the one action in amber.
        static Control Pair(Control a, Control b)
        {
            Screens.Detach(a);
            Screens.Detach(b);
            Grid.SetColumn(b, 2);
            return new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*"), Children = { a, b } };
        }

        // Entry 243 section 4 item 4: the C bull's diamond beside the disc, sized by the same rule.
        column.Children.Add(Screens.Card(
            Screens.Heading("Made for your optic"),
            Screens.Dim("Say how far, the lowest magnification you will shoot at (1 for a red dot, with the dot's size) and how many shots. GroupLab sizes a bull you can center on through that optic, and makes as many sheets as the shots need."),
            Pair(Field("Distance, yards", distance), Field("Lowest magnification", magnification)),
            Pair(Field("Red dot size in MOA, at 1x only", dot), Field("Shots", shots)),
            Screens.Dim("Paper"),
            Pair(letter, a4),
            Screens.Dim("Bull shape"),
            Pair(disc, diamond),
            Screens.Primary("Make the sheet", Generate),
            Screens.Detach(said)));

        // Entry 348: a store-bought target GroupLab does not know yet, fingerprinted from a photo in five steps.
        column.Children.Add(Screens.Card(
            Screens.Heading("Store-bought targets"),
            Screens.Dim(GroupLab.Core.StoreTargets.FingerprintWords.Offer),
            Screens.Choice(GroupLab.Core.StoreTargets.FingerprintWords.Title, () => Content = new FingerprintPage(() => Content = List())).Id("targets-add-store")));

        // Entry 365: the scale markers, printed or shared at actual size, and the boards measured.
        column.Children.Add(ScaleMarkers());

        // Entry 363 section 2a: a thermal label printer through its own app, the darkness test first.
        var darknessSaid = Screens.Line("");
        column.Children.Add(Screens.Card(
            Screens.Heading("Thermal label printers"),
            Screens.Dim("Each sheet's page offers Share for a printer app. The darkness test page shows what each darkness setting in the printer's app does to fine lines: print it once at each setting and write the setting on it."),
            Screens.Choice("Share the darkness test page", () => darknessSaid.Text = DarknessForPrinterApp(picture: true)).Id("targets-darkness"),
            darknessSaid));

        column.Children.Add(Screens.Heading("The library"));
        IReadOnlyList<LibrarySheet> sheets;
        try
        {
            PhoneAnalysis.Library();
            sheets = TargetLibrary.Load(Path.Combine(PhoneAnalysis.Files, "targets"), AppSettingsStore.LetterRegion(AppSettingsStore.Region()));
        }
        catch (IOException e)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "targets.list", e);
            sheets = [];
        }

        // Each family on a card of its own, a sheet a row: its name, and beneath it its paper and how many bulls it has.
        foreach (var family in sheets.GroupBy(s => s.Family))
        {
            column.Children.Add(Screens.Dim(family.Key));
            var rows = new StackPanel();
            foreach (var sheet in family)
            {
                int bulls = sheet.Definition.Bulls.Count(b => b.Scoring);
                string paper = sheet.Paper.Split(',')[0];
                string count = bulls == 1 ? "1 bull" : $"{bulls} bulls";
                rows.Children.Add(Screens.Row(sheet.Definition.Name, sheet.Sheets > 1 ? $"{paper} · {count} a sheet, {sheet.Sheets} sheets" : $"{paper} · {count}", () => Content = Sheet(sheet)));
            }

            column.Children.Add(new Border { Child = rows, Classes = { PhoneStyles.Card } });
        }

        return Screens.Page(column);
    }

    /// <summary>Makes the sheets "Made for your optic" describes and, where they print, opens them as a sheet of the library would.</summary>
    private void Generate()
    {
        said.Children.Clear();
        static double? Read(string? text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) && v > 0 ? v
            : double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v > 0 ? v : null;
        if (Read(distance.Text) is not { } yards || Read(magnification.Text) is not { } power)
        {
            said.Children.Add(Screens.Line("Enter the distance in yards and the lowest magnification, such as 100 and 10."));
            return;
        }

        int count = Read(shots.Text) is { } n ? (int)Math.Round(n) : 25;
        var made = TargetGenerator.Generate(new GeneratorRequest(yards, power, Read(dot.Text), count, a4.IsChecked == true ? "a4" : "letter", diamond.IsChecked == true));
        DiagnosticLog.Info("sheet.generate", ("yards", yards), ("power", power), ("shots", made.Request.Shots), ("sheets", made.Sheets), ("bulls", made.Bulls));
        foreach (string sentence in made.Explanation)
        {
            said.Children.Add(Screens.Line(sentence));
        }

        if (made.Design is { Printable: true, Definition: { } definition })
        {
            var own = new LibrarySheet("custom.gltd.json", "Made for your optic", null, definition);
            said.Children.Add(Screens.Choice(made.Sheets > 1 ? $"Print or share the {made.Sheets} sheets" : "Print or share the sheet", () => Content = Sheet(own, made.Explanation)));
        }
    }

    /// <summary>One sheet: its picture, what it is, and Print and Share the PDF.</summary>
    private Control Sheet(LibrarySheet sheet, IReadOnlyList<string>? explanation = null)
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title(sheet.Definition.Name));
        column.Children.Add(Screens.Dim(explanation is null ? sheet.Summary : string.Join(" ", explanation)));
        // Entry 258: a sheet too large for a flatbed says what that means for photographing it, as on the desktop.
        if (GroupLab.Core.Capture.PhotographLimit.ForSheet(sheet.Definition) is { } large)
        {
            column.Children.Add(Screens.Dim(large));
        }
        // Entry 297: the bulls in black, blue or red, remembered for each sheet; the picture follows the choice at once.
        string key = sheet.File == "custom.gltd.json" ? "designer" : Path.GetFileName(sheet.File);
        var colour = Phone.Settings.LoadBullColour(key);
        // Entry 300 section 6: the sheet drawn live from the scene its PDF is written from, sharp at the phone's own resolution.
        var image = new SheetView { Scene = Page(sheet.Definition, colour), Height = 480, HorizontalAlignment = HorizontalAlignment.Stretch };
        column.Children.Add(image);
        var colours = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 8 };
        foreach (var (choice, at) in GroupLab.Core.Rendering.BullColours.All.Select((c, i) => (c, i)))
        {
            var radio = Screens.Radio("bullColour", choice switch { GroupLab.Core.Rendering.BullColour.Red => "Red", GroupLab.Core.Rendering.BullColour.Blue => "Blue", _ => "Black" }, colour == choice);
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true && Phone.Settings.LoadBullColour(key) != choice)
                {
                    Phone.Settings.SaveBullColour(key, choice);
                    DiagnosticLog.Info("print.color", ("sheet", sheet.File), ("color", GroupLab.Core.Rendering.BullColours.Name(choice)));
                    image.Scene = Page(sheet.Definition, choice);
                }
            };
            Grid.SetColumn(radio, at);
            colours.Children.Add(radio);
        }

        column.Children.Add(Screens.Dim("Bulls in"));
        column.Children.Add(colours);
        column.Children.Add(Screens.Dim("Only the bulls, their rings and numbers take the color; the codes, markers, title and load block stay black. Large solid areas print as a lighter tint."));

        var result = Screens.Line("");
        var offer = new StackPanel { Spacing = 8, IsVisible = false };
        column.Children.Add(Screens.Primary("Print", () =>
        {
            Out(sheet, result, print: true);
            // Entry 273: the first time a sheet is printed, the printer check is offered, once, since this printer's sheets are the ones photographed.
            var settings = Phone.Settings;
            if (!GroupLab.Core.Marking.PrinterCheck.IsCheckPage(sheet.Definition) && settings.LoadPrinters().Count == 0 && !settings.LoadPrinterOffered())
            {
                settings.SavePrinterOffered();
                offer.Children.Add(Screens.Dim("Check this printer once, and every photo of a GroupLab sheet it printed measures in real inches."));
                offer.Children.Add(Screens.Choice("Check this printer", () => Shell.Current?.ShowPrinterCheck()));
                offer.IsVisible = true;
            }
        }));
        column.Children.Add(offer);
        column.Children.Add(Screens.Choice("Share the PDF", () => Out(sheet, result, print: false)));
        // Entry 363 section 2a: for a thermal label printer's own app, such as the Phomemo M834's, before GroupLab prints to one itself.
        column.Children.Add(Screens.Choice("Share for a printer app, as a picture", () => result.Text = ForPrinterApp(sheet, picture: true)).Id("targets-printer-app-picture"));
        column.Children.Add(Screens.Choice("Share for a printer app, as a PDF", () => result.Text = ForPrinterApp(sheet, picture: false)).Id("targets-printer-app-pdf"));
        column.Children.Add(Screens.Dim(PrinterAppWords));
        // Request 73: straight to the Phomemo M834 over Bluetooth, at true size, spoken to as the app's own recording showed.
        var stopM834 = Screens.Choice("Cancel the print", () => m834Printing?.Cancel()).Id("targets-print-m834-cancel");
        stopM834.IsVisible = false;
        column.Children.Add(Screens.Choice(M834Print, () => _ = PrintOnM834(sheet, result, stopM834)).Id("targets-print-m834"));
        column.Children.Add(stopM834);
        // Entry 258: a set of tiles as one large page with cut lines between them, for a plotter, shared rather than printed on the phone.
        if (GroupLab.Core.Rendering.CutSheet.Refusal(sheet.Definition) is null)
        {
            column.Children.Add(Screens.Choice("Share as one large page with cut lines, for a plotter", () => Out(sheet, result, print: false, oneSheet: true)));
        }
        column.Children.Add(result);
        column.Children.Add(Screens.Dim("In the print dialog, keep the scale at 100 percent, actual size. The line printed on the sheet says how to check it with a ruler."));
        column.Children.Add(Screens.Choice("Back to the targets", () => Content = List()));
        return Screens.Page(column);
    }

    /// <summary>
    /// Entry 365: Targets, "Scale markers" on the phone: the corner brackets, scale bars and board stickers on the region's paper, printed or
    /// shared; the stickers for a label printer's own app; a board measured from a photo; the boards saved, each with Forget.
    /// </summary>
    private Control ScaleMarkers()
    {
        var paper = AppSettingsStore.LetterRegion(AppSettingsStore.Region()) ? GroupLab.Core.ScaleMarkers.MarkerPaper.Letter : GroupLab.Core.ScaleMarkers.MarkerPaper.A4;
        var bars = paper == GroupLab.Core.ScaleMarkers.MarkerPaper.A4 ? GroupLab.Core.ScaleMarkers.MarkerKind.MetricBar : GroupLab.Core.ScaleMarkers.MarkerKind.InchBar;
        var said = Screens.Line("");
        var boards = new StackPanel { Spacing = 8 };
        void Fill()
        {
            boards.Children.Clear();
            var saved = Phone.Settings.LoadBoards();
            if (saved.Count == 0)
            {
                boards.Children.Add(Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.NoBoards));
            }

            foreach (var board in saved)
            {
                string name = board.Name;
                boards.Children.Add(Screens.Line(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.BoardLine(board)));
                boards.Children.Add(Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Forget + " " + name, () =>
                {
                    Phone.Settings.ForgetBoard(name);
                    Fill();
                }));
            }
        }

        void Print(GroupLab.Core.ScaleMarkers.MarkerKind kind)
        {
            var size = paper == GroupLab.Core.ScaleMarkers.MarkerPaper.A4 ? GroupLab.Core.Gltd.Model.PageSize.A4 : GroupLab.Core.Gltd.Model.PageSize.Letter;
            if (Phone.Platform.PrintPdf(GroupLab.Core.ScaleMarkers.ScaleMarkerPages.Pdf(kind, paper), GroupLab.Core.ScaleMarkers.ScaleMarkerPages.Title(kind, paper), size) is { } refused)
            {
                ProblemSheet.Stop(said, said, "The page could not be printed", refused);
            }
            DiagnosticLog.Info("markers.print", ("kind", kind.ToString()));
        }

        async Task Measure()
        {
            var picked = await PhotoPages.Pick(this, PhotoSource.Photos, "board", words => said.Text = words);
            if (picked.FirstOrDefault() is not { } photo)
            {
                return;
            }

            said.Text = "Measuring the board…";
            string name = GroupLab.Core.ScaleMarkers.ScaleMarkerWords.NextBoard(Phone.Settings.LoadBoards());
            var printer = Phone.Settings.LoadChosenPrinter();
            var (board, words) = await Task.Run(() => GroupLab.Cli.Library.ScaleMarkerFinder.MeasureBoard(photo.Path, PhoneAnalysis.Library(), printer, name,
                DateOnly.FromDateTime(DateTime.Today), GroupLab.Core.Imaging.WorkingSize.PhoneMegapixels * 2));
            if (board is not null)
            {
                Phone.Settings.SaveBoard(board);
                Fill();
                said.Text = words;
            }
            else
            {
                ProblemSheet.Stop(said, said, "The board could not be measured", words, ("Choose another photo", () => _ = Measure()));
            }
        }

        Fill();
        return Screens.Card(
            Screens.Heading(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Heading),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Intro),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Brackets + ". " + GroupLab.Core.ScaleMarkers.ScaleMarkerWords.BracketsGive),
            Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.PrintBrackets, () => Print(GroupLab.Core.ScaleMarkers.MarkerKind.Bracket)).Id("targets-markers-brackets"),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Bars + ". " + GroupLab.Core.ScaleMarkers.ScaleMarkerWords.BarsGive),
            Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.PrintBars, () => Print(bars)).Id("targets-markers-bars"),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Stickers + ". " + GroupLab.Core.ScaleMarkers.ScaleMarkerWords.StickersGive),
            Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.PrintStickers, () => Print(GroupLab.Core.ScaleMarkers.MarkerKind.Sticker)).Id("targets-markers-stickers"),
            Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.LabelStickers, () =>
            {
                var (pngs, pdf) = PrinterAppFiles.Make(GroupLab.Core.ScaleMarkers.ScaleMarkerPages.Pages(GroupLab.Core.ScaleMarkers.MarkerKind.Sticker, GroupLab.Core.ScaleMarkers.MarkerPaper.Label4x6));
                said.Text = Share(GroupLab.Core.ScaleMarkers.ScaleMarkerPages.FileName(GroupLab.Core.ScaleMarkers.MarkerKind.Sticker, GroupLab.Core.ScaleMarkers.MarkerPaper.Label4x6), pngs, pdf, picture: true, "Board stickers");
            }).Id("targets-markers-label"),
            Screens.Choice(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.MeasureBoard.TrimEnd('…'), () => _ = Measure()).Id("targets-markers-board"),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Labels + ". " + GroupLab.Core.ScaleMarkers.ScaleMarkerWords.LabelsGive),
            Screens.Choice("Share four scale labels for the printer's app", () =>
            {
                // Entry 372: the size loaded in the printer, as Settings keeps it; each label its own serial.
                var (w, h) = Phone.Settings.LoadLabelSize();
                int serial = Phone.Settings.LoadLabelSerial();
                var (pngs, pdf) = PrinterAppFiles.Make(GroupLab.Core.ScaleMarkers.ScaleLabels.Pages(w, h, serial, 4, "M220"), 203);
                said.Text = Phone.Platform.SharePdf(pdf, $"grouplab-scale-labels-{w}x{h}mm-S{serial}") ?? $"Shared four {w} x {h} mm labels, S{serial} to S{serial + 3}.";
                Phone.Settings.SaveLabelSerial(serial + 4);
            }).Id("targets-markers-labels"),
            boards,
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.Card + ". " + GroupLab.Core.ScaleMarkers.ScaleMarkerWords.CardGives),
            Screens.Dim(GroupLab.Core.ScaleMarkers.ScaleMarkerWords.PrintNote),
            said);
    }

    internal const string M834Print = "Print on the Phomemo M834 (Bluetooth, new: not yet tried on a real one)";

    /// <summary>The print to the M834 under way, for its Cancel; null when none is.</summary>
    private static CancellationTokenSource? m834Printing;

    /// <summary>
    /// Request 73: every page of the sheet drawn for the M834's head at 300 dpi, true size, and sent over the paired serial link. Nothing
    /// is fitted to the page: the Phomemo app shrank a Letter sheet to 94.7 percent when it printed one.
    /// <para>
    /// Entry 377: the pages are drawn and encoded before connecting, since the printer drops a link left idle for about half a minute;
    /// every step is logged; Cancel ends any step; and whatever stops it is said in the middle of the screen, never a line that waits for
    /// ever. Nothing escapes this method, because nothing awaits it.
    /// </para>
    /// </summary>
    private static async Task PrintOnM834(LibrarySheet sheet, TextBlock result, Button cancel)
    {
        if (m834Printing is not null)
        {
            return;
        }

        using var printing = new CancellationTokenSource();
        m834Printing = printing;
        cancel.IsVisible = true;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        string step = "drawing";
        try
        {
            var profile = GroupLab.Core.Printing.Labels.PrinterProfiles.All.Single(p => p.Id == "phomemo-m834");
            result.Text = "Drawing the page for the M834…";
            var jobs = await Task.Run(() =>
            {
                var scenes = SceneBuilder.Build(sheet.Definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote));
                var encoded = new List<byte[]>();
                foreach (var page in scenes.Pages)
                {
                    printing.Token.ThrowIfCancellationRequested();
                    var dots = GroupLab.Core.Printing.Thermal.ThermalRaster.Render(page, profile.Head);
                    var job = new GroupLab.Core.Printing.Labels.LabelJob(dots.Image, page.Width / (10.0 * Scene.UnitsPerDmm), page.Height / (10.0 * Scene.UnitsPerDmm));
                    encoded.Add(GroupLab.Core.Printing.Labels.PrinterEncoders.For(profile).Encode(job, profile));
                    DiagnosticLog.Info("print.m834", ("step", "encoded"), ("page", encoded.Count), ("bytes", encoded[^1].Length), ("ms", clock.ElapsedMilliseconds));
                }

                return encoded;
            }, printing.Token);

            step = "connecting";
            result.Text = "Connecting to the M834… (up to a minute; Cancel stops it)";
            DiagnosticLog.Info("print.m834", ("step", "connect"), ("pages", jobs.Count));
            var (link, why) = await Phone.Platform.OpenSerialPrinterAsync("M834", printing.Token);
            if (link is null)
            {
                DiagnosticLog.Info("print.m834", ("step", "connect"), ("result", "none"), ("ms", clock.ElapsedMilliseconds));
                ProblemSheet.Stop(result, result, "The M834 could not be reached", why ?? "The M834 could not be reached.");
                return;
            }

            step = "sending";
            await using (link)
            {
                for (int k = 0; k < jobs.Count; k++)
                {
                    result.Text = jobs.Count == 1 ? "Sending the page to the M834…" : $"Sending page {k + 1} of {jobs.Count} to the M834…";
                    int blocks = await GroupLab.Core.Printing.Labels.PrinterJob.SendAsync(link, profile, jobs[k], Task.Delay, printing.Token);
                    DiagnosticLog.Info("print.m834", ("step", "sent"), ("page", k + 1), ("blocks", blocks), ("bytes", jobs[k].Length), ("ms", clock.ElapsedMilliseconds));
                }
            }

            DiagnosticLog.Info("print.m834", ("pages", jobs.Count), ("ms", clock.ElapsedMilliseconds));
            result.Text = jobs.Count == 1 ? "Sent the page to the M834. Measure its ruler line: it should be true to size."
                : $"Sent {jobs.Count} pages to the M834. Measure a ruler line: it should be true to size.";
        }
        catch (OperationCanceledException)
        {
            DiagnosticLog.Info("print.m834", ("step", step), ("result", "cancelled"), ("ms", clock.ElapsedMilliseconds));
            result.Text = "The print was cancelled.";
        }
        catch (Exception e)
        {
            DiagnosticLog.Info("print.m834", ("step", step), ("error", e.GetType().Name), ("ms", clock.ElapsedMilliseconds));
            ProblemSheet.Stop(result, result, "The page did not reach the M834", $"While {step}, it stopped: {e.Message}. Turn the printer off and on, then press Print again.");
        }
        finally
        {
            m834Printing = null;
            cancel.IsVisible = false;
        }
    }

    /// <summary>What sharing for a printer app makes, said under its two choices.</summary>
    internal const string PrinterAppWords = "For a thermal label printer's own app: the sheet in black and white at 300 dots an inch, every dot where GroupLab put it, at its true size. In the app, print at 100 percent or actual size, never fit to page.";

    /// <summary>
    /// Entry 363 section 2a: the sheet as the thermal print mode draws it at 300 dpi, shared as a picture (the first page; the PDF has every
    /// page) or as a PDF. A sentence for the screen, or "" where it was handed to the share sheet.
    /// </summary>
    internal static string ForPrinterApp(LibrarySheet sheet, bool picture)
    {
        var scenes = SceneBuilder.Build(sheet.Definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote));
        if (scenes.Pages.Count == 0)
        {
            return "This sheet has no page to share.";
        }

        var (pngs, pdf) = PrinterAppFiles.Make(scenes.Pages);
        return Share(Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(sheet.File)), pngs, pdf, picture, sheet.Definition.Name);
    }

    /// <summary>The darkness test page, Letter, for a printer app: printed once at each darkness the app offers, the setting written on it.</summary>
    internal static string DarknessForPrinterApp(bool picture)
    {
        var (pngs, pdf) = PrinterAppFiles.DarknessPage();
        return Share("darkness-test", pngs, pdf, picture, "Darkness test");
    }

    private static string Share(string stem, IReadOnlyList<byte[]> pngs, byte[] pdf, bool picture, string title)
    {
        string name = $"{stem}-{PrinterAppFiles.Suffix(PrinterAppFiles.DefaultDpi)}";
        DiagnosticLog.Info("print.printer-app", ("picture", picture), ("pages", pngs.Count));
        if (!picture)
        {
            return Phone.Platform.SharePdf(pdf, name) ?? "";
        }

        // Entry 377: inside the cache's shared folder, the one the share sheet may read from; the cache's top was refused with
        // "Failed to find configured root" and the darkness test crashed.
        string folder = Path.Combine(Phone.Platform.CacheFolder, "shared");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, name + ".png");
        File.WriteAllBytes(path, pngs[0]);
        string? refused = Phone.Platform.ShareFile(path, "image/png", title);
        return refused ?? (pngs.Count > 1 ? $"Page 1 of {pngs.Count} shared; share the PDF for them all." : "");
    }

    private static void Out(LibrarySheet sheet, TextBlock result, bool print, bool oneSheet = false)
    {
        var colour = Phone.Settings.LoadBullColour(sheet.File == "custom.gltd.json" ? "designer" : Path.GetFileName(sheet.File));
        var rendered = TargetRenderer.Render(sheet.Definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote, OneSheet: oneSheet, BullColour: colour));
        if (rendered.Pdf is not { } pdf)
        {
            ProblemSheet.Stop(result, result, "This sheet cannot be printed", "This sheet cannot be printed as it is: " + string.Join(" ", rendered.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message)));
            return;
        }

        if ((print ? Phone.Platform.PrintPdf(pdf, sheet.Definition.Name, sheet.Definition.Page.Size) : Phone.Platform.SharePdf(pdf, sheet.Definition.Name)) is { } refused)
        {
            ProblemSheet.Stop(result, result, print ? "The sheet could not be printed" : "The sheet could not be shared", refused);
        }
        else
        {
            result.Text = "";
        }
    }

    /// <summary>The first sheet's artwork, its longer side near 900 pixels, as the desktop's print screen shows it.</summary>
    private static Scene? Page(TargetDefinition definition, GroupLab.Core.Rendering.BullColour colour = GroupLab.Core.Rendering.BullColour.Black)
    {
        // Entry 250 section 1: the sheet as its PDF prints it, words and the actual-size instruction included; entry 297, its bulls in color.
        var scenes = SceneBuilder.Build(definition, new RenderOptions(TileIndex: 0, PrintNote: SceneBuilder.ActualSizeNote, BullColour: colour));
        return scenes.Pages.Count == 0 ? null : scenes.Pages[0];
    }
}
