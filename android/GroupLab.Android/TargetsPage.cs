using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Library;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Gltd;
using GroupLab.Core.Rendering;
using Orientation = Avalonia.Layout.Orientation;
using RadioButton = Avalonia.Controls.RadioButton;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.4: the Targets screen on the phone. "Made for your optic" first, because on a phone it is the
/// quickest way to a sheet that suits the rifle in hand, then the library by family. A sheet opens to a picture of it and two big buttons:
/// Print, which is Android's own print dialog, and Share the PDF, for printing from a computer. The PDF is the one the desktop makes, with
/// the note to print at actual size, so a sheet printed from the phone reads the same.
/// </summary>
public sealed class TargetsPage : UserControl
{
    private readonly TextBox distance = Number("100");
    private readonly TextBox magnification = Number("");
    private readonly TextBox dot = Number("");
    private readonly TextBox shots = Number("25");
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
        TextInputOptions.SetContentType(box, TextInputContentType.Number);
        return box;
    }

    private static Control Field(string words, TextBox box) => new StackPanel { Spacing = 4, Children = { Screens.Dim(words), box } };

    private Control List()
    {
        var column = new StackPanel { Spacing = 12 };
        column.Children.Add(Screens.Title("Targets"));
        column.Children.Add(Screens.Line("Print a sheet GroupLab reads by itself, or share it as a PDF to print on a computer. Print it at actual size."));

        // Entry 246, look B: the optic's form on one card, two fields to a row, the choices as cards, and the one action in amber.
        static Control Pair(Control a, Control b)
        {
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
            said));

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
        if (Preview(sheet.Definition) is { } picture)
        {
            column.Children.Add(new Image { Source = picture, MaxHeight = 480, HorizontalAlignment = HorizontalAlignment.Center });
        }

        var result = Screens.Line("");
        var offer = new StackPanel { Spacing = 8, IsVisible = false };
        column.Children.Add(Screens.Primary("Print", () =>
        {
            Out(sheet, result, print: true);
            // Entry 273: the first time a sheet is printed, the printer check is offered, once, since this printer's sheets are the ones photographed.
            var settings = App.Settings;
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
        column.Children.Add(result);
        column.Children.Add(Screens.Dim("In the print dialog, keep the scale at 100 percent, actual size. The line printed on the sheet says how to check it with a ruler."));
        column.Children.Add(Screens.Choice("Back to the targets", () => Content = List()));
        return Screens.Page(column);
    }

    private static void Out(LibrarySheet sheet, TextBlock result, bool print)
    {
        var rendered = TargetRenderer.Render(sheet.Definition, new RenderOptions(PrintNote: SceneBuilder.ActualSizeNote));
        if (rendered.Pdf is not { } pdf)
        {
            result.Text = "This sheet cannot be printed as it is: " + string.Join(" ", rendered.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message));
            return;
        }

        result.Text = (print ? PdfOut.Print(pdf, sheet.Definition.Name, sheet.Definition.Page.Size) : PdfOut.Share(pdf, sheet.Definition.Name)) ?? "";
    }

    /// <summary>The first sheet's artwork, its longer side near 900 pixels, as the desktop's print screen shows it.</summary>
    private static Bitmap? Preview(TargetDefinition definition)
    {
        // Entry 250 section 1: the sheet as its PDF prints it, words and the actual-size instruction included.
        var scenes = SceneBuilder.Build(definition, new RenderOptions(TileIndex: 0, PrintNote: SceneBuilder.ActualSizeNote));
        if (scenes.Pages.Count == 0)
        {
            return null;
        }

        var scene = scenes.Pages[0];
        double longerInches = Math.Max(scene.Width, scene.Height) / (2.0 * 254);
        var image = SceneRasterizer.Rasterize(scene, Math.Min(100, 900 / longerInches), words: true);
        using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
        OpenCvSharp.Cv2.ImEncode(".png", mat, out byte[] png);
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }
}
