using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Rendering;
using Orientation = Avalonia.Layout.Orientation;
using RenderOptions = GroupLab.Core.Rendering.RenderOptions;

namespace GroupLab.App;

/// <summary>
/// The print screen, NOTES-FROM-PLANNING.md entry 25 section 2: where a new user starts, because GroupLab's premise is that you print
/// its target, shoot it and photograph it. It calls the renderer that already works.
/// <list type="number">
/// <item>Pick a sheet from the built-in library by what it is for, its bulls, its paper and its distance.</item>
/// <item>A preview of the artwork, page by page for a tiled set.</item>
/// <item>The load block blank, to write on at the range, or filled from this screen before printing.</item>
/// <item>Save a PDF, or print.</item>
/// <item>Scale handled rather than warned about, as far as the platform allows: the PDF asks its viewer for no scaling, the screen
/// says in plain words what GroupLab cannot set, and every sheet can carry the instruction along its bottom edge.</item>
/// <item>A tiled target as one PDF of every tile, in order, each numbered beside its identifier.</item>
/// </list>
/// </summary>
public sealed class PrintWindow : Window
{
    /// <summary>What GroupLab cannot do for the user, said plainly beside the buttons.</summary>
    internal const string ScaleWords =
        "Print at actual size. In the print dialog choose \"Actual size\" or \"100%\", never \"Fit\", \"Shrink oversized pages\" or " +
        "\"Fit to printable area\". GroupLab asks the PDF viewer for no scaling, but it cannot set your printer driver, and a sheet " +
        "printed at 97 percent measures 3 percent small.";

    private readonly IReadOnlyList<LibrarySheet> sheets;
    private readonly ListBox list = new();
    private readonly TextBlock title = new() { FontSize = 18, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel loadBlock = new() { Spacing = 6 };
    private readonly RadioButton blank = new() { Content = "Blank, to write on at the range", GroupName = "loadBlock", IsChecked = true };
    private readonly RadioButton filled = new() { Content = "Filled in now, from these fields", GroupName = "loadBlock" };
    private readonly StackPanel fields = new() { Spacing = 4 };
    private readonly Dictionary<string, TextBox> fieldBoxes = new(StringComparer.Ordinal);
    private readonly TextBox serial = new() { Width = 120, PlaceholderText = "optional" };
    private readonly CheckBox note = new() { Content = "Print the actual-size instruction along the bottom of each sheet", IsChecked = true };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.OrangeRed };
    private readonly TextBlock pageCaption = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Image preview = new() { Stretch = Stretch.Uniform, Height = 520, HorizontalAlignment = HorizontalAlignment.Left };
    private LibrarySheet? selected;
    private int page;

    public PrintWindow()
        : this(TargetLibrary.Load(Path.Combine(AppContext.BaseDirectory, "targets")))
    {
    }

    internal PrintWindow(IReadOnlyList<LibrarySheet> library)
    {
        sheets = library;
        Title = "GroupLab: print a target";
        Width = 1200;
        Height = 860;

        list.ItemsSource = sheets.Select(s =>
        {
            var item = new StackPanel { Spacing = 1, Margin = new Thickness(2, 4) };
            item.Children.Add(new TextBlock { Text = s.Family, FontSize = 11, Opacity = 0.7 });
            item.Children.Add(new TextBlock { Text = s.Definition.Name, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            item.Children.Add(new TextBlock { Text = s.Summary, FontSize = 12, TextWrapping = TextWrapping.Wrap });
            return item;
        }).ToList();
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedIndex >= 0)
            {
                Show(sheets[list.SelectedIndex]);
            }
        };

        blank.Click += (_, _) => ShowFields();
        filled.Click += (_, _) => ShowFields();

        var details = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        details.Children.Add(title);
        details.Children.Add(summary);
        details.Children.Add(loadBlock);
        details.Children.Add(note);
        details.Children.Add(new TextBlock { Text = ScaleWords, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        details.Children.Add(Row(Button("Save PDF…", async () => await SaveDialog()), Button("Print…", Print)));
        details.Children.Add(status);
        details.Children.Add(Row(Button("Previous sheet", () => Turn(-1)), Button("Next sheet", () => Turn(1)), pageCaption));
        details.Children.Add(new TextBlock { Text = "The preview shows the artwork; its text is drawn in the PDF.", FontSize = 12, Opacity = 0.7 });
        details.Children.Add(preview);

        var dock = new DockPanel();
        var left = new ScrollViewer { Content = list, Width = 420 };
        DockPanel.SetDock(left, Dock.Left);
        dock.Children.Add(left);
        dock.Children.Add(new ScrollViewer { Content = details });
        Content = dock;

        if (sheets.Count > 0)
        {
            list.SelectedIndex = 0;
        }
        else
        {
            status.Text = "The built-in library was not found beside the application.";
        }
    }

    /// <summary>The sheets listed, for the headless tests.</summary>
    internal IReadOnlyList<LibrarySheet> Sheets => sheets;

    /// <summary>The preview image, for the headless tests.</summary>
    internal IImage? PreviewSource => preview.Source;

    /// <summary>The message line, for the headless tests.</summary>
    internal string StatusText => status.Text ?? "";

    /// <summary>Selects a sheet by its file name.</summary>
    internal void Select(string file) => list.SelectedIndex = sheets.ToList().FindIndex(s => s.File == file);

    internal void SetFilled(bool value)
    {
        filled.IsChecked = value;
        blank.IsChecked = !value;
        ShowFields();
    }

    internal void SetField(string key, string value) => fieldBoxes[key].Text = value;

    /// <summary>Renders the selected sheet as this screen is set, or null with the reason in the message line.</summary>
    internal RenderResult? Render()
    {
        if (selected is null)
        {
            return null;
        }

        bool fill = filled.IsChecked == true && selected.Definition.DataBlock is not null;
        string? serialText = string.IsNullOrWhiteSpace(serial.Text) ? null : serial.Text.Trim();
        Instance? instance = fill
            ? new Instance(serialText, DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                [.. fieldBoxes.Where(f => !string.IsNullOrWhiteSpace(f.Value.Text)).Select(f => new KeyValuePair<string, string>(f.Key, f.Value.Text!.Trim()))])
            : serialText is null ? null : new Instance(serialText, null, null);
        var result = TargetRenderer.Render(selected.Definition, new RenderOptions(
            Mode: fill ? DataBlockMode.Filled : DataBlockMode.Blank,
            Instance: instance,
            PrintNote: note.IsChecked == true ? SceneBuilder.ActualSizeNote : null));
        if (result.Pdf is null)
        {
            status.Text = "This sheet cannot be printed as set: " + string.Join(" ", result.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message));
            return null;
        }

        status.Text = "";
        return result;
    }

    /// <summary>Writes the PDF, every sheet of a tiled set in order. Returns false with the reason in the message line.</summary>
    internal bool SavePdf(string path)
    {
        if (Render() is not { Pdf: { } pdf } result)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllBytes(path, pdf);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            status.Text = "The PDF could not be written: " + ex.Message;
            return false;
        }

        status.Text = result.Pages.Count > 1
            ? string.Create(CultureInfo.InvariantCulture, $"Saved {result.Pages.Count} sheets to {path}. Print every page; each is numbered beside its identifier.")
            : "Saved to " + path + ".";
        return true;
    }

    private void Show(LibrarySheet sheet)
    {
        selected = sheet;
        page = 0;
        title.Text = sheet.Definition.Name;
        summary.Text = sheet.Family + ". " + sheet.Summary;
        loadBlock.Children.Clear();
        fieldBoxes.Clear();
        if (sheet.Definition.DataBlock is { } block)
        {
            loadBlock.Children.Add(new TextBlock { Text = "Load block", FontWeight = FontWeight.SemiBold });
            loadBlock.Children.Add(blank);
            loadBlock.Children.Add(filled);
            foreach (string key in FieldKeys(block))
            {
                fieldBoxes[key] = new TextBox { Width = 260, Text = key == "date" ? DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "" };
            }

            loadBlock.Children.Add(fields);
            loadBlock.Children.Add(Row(new TextBlock { Text = "Serial", Width = 140, VerticalAlignment = VerticalAlignment.Center }, serial));
        }
        else
        {
            loadBlock.Children.Add(new TextBlock { Text = "This sheet has no load block.", Opacity = 0.8 });
        }

        ShowFields();
        ShowPreview();
    }

    private static IReadOnlyList<string> FieldKeys(DataBlock block)
    {
        if (block.FieldSet == FieldSet.Explicit)
        {
            return block.Fields?.Select(f => f.Key).ToList() ?? [];
        }

        return InstanceCodec.FieldKeys(block.FieldSet);
    }

    private void ShowFields()
    {
        fields.Children.Clear();
        if (filled.IsChecked != true)
        {
            return;
        }

        foreach (var (key, box) in fieldBoxes)
        {
            fields.Children.Add(Row(new TextBlock { Text = SceneBuilder.FieldCaption(key), Width = 140, VerticalAlignment = VerticalAlignment.Center }, box));
        }
    }

    private void Turn(int by)
    {
        if (selected is null)
        {
            return;
        }

        page = Math.Clamp(page + by, 0, selected.Sheets - 1);
        ShowPreview();
    }

    /// <summary>Rasterises the current page's artwork at a resolution that puts its longer side near 900 pixels.</summary>
    private void ShowPreview()
    {
        if (selected is null)
        {
            return;
        }

        var scenes = SceneBuilder.Build(selected.Definition, new RenderOptions(TileIndex: page));
        if (scenes.Pages.Count == 0)
        {
            preview.Source = null;
            return;
        }

        var scene = scenes.Pages[0];
        double longerInches = Math.Max(scene.Width, scene.Height) / (2.0 * 254);
        var image = SceneRasterizer.Rasterize(scene, Math.Min(100, 900 / longerInches));
        using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
        OpenCvSharp.Cv2.ImEncode(".png", mat, out byte[] png);
        using var stream = new MemoryStream(png);
        (preview.Source as IDisposable)?.Dispose();
        preview.Source = new Bitmap(stream);
        pageCaption.Text = string.Create(CultureInfo.InvariantCulture, $"Sheet {page + 1} of {selected.Sheets}");
    }

    private async Task SaveDialog()
    {
        if (selected is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the target as a PDF",
            SuggestedFileName = Path.GetFileName(selected.File).Replace(".gltd.json", ".pdf", StringComparison.Ordinal),
            DefaultExtension = "pdf",
        });
        if (file?.TryGetLocalPath() is { } path)
        {
            SavePdf(path);
        }
    }

    /// <summary>
    /// Prints through the system: the PDF is written to a temporary file and handed to the print command of whatever opens PDFs. GroupLab
    /// cannot reach the driver's scaling from there, which is what the plain words beside the button are for. Where no print command is
    /// registered, the PDF is opened instead.
    /// </summary>
    private void Print()
    {
        if (selected is null)
        {
            return;
        }

        string path = Path.Combine(Path.GetTempPath(), "GroupLab", selected.File.Replace(".gltd.json", ".pdf", StringComparison.Ordinal));
        if (!SavePdf(path))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "print" })?.Dispose();
            status.Text = "Sent to your PDF viewer's print command. In its print dialog choose Actual size, or 100%.";
        }
        catch (Win32Exception)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
                status.Text = "Your PDF viewer has no print command GroupLab can call, so the PDF is open in it. Print from there at Actual size, or 100%.";
            }
            catch (Win32Exception ex)
            {
                status.Text = "No application could open the PDF (" + ex.Message + "). It is saved at " + path + "; print it from elsewhere at actual size.";
            }
        }
    }

    private static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var child in children)
        {
            row.Children.Add(child);
        }

        return row;
    }

    private static Button Button(string label, Action action)
    {
        var button = new Button { Content = label };
        button.Click += (_, _) => action();
        return button;
    }

    private static Button Button(string label, Func<Task> action)
    {
        var button = new Button { Content = label };
        button.Click += async (_, _) => await action();
        return button;
    }
}
