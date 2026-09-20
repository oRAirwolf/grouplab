using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Library;
using GroupLab.Cli.Printing;
using GroupLab.Core.Gltd;
using GroupLab.Core.Gltd.Binary;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Printing;
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
    /// <summary>
    /// Entry 114 section 1: what the print screen says about the in-app path until a sheet printed through the fixed drawing has been checked
    /// on paper. The fault dropped every filled rectangle on one driver, which is every marker, both codes and the load block's rules.
    /// </summary>
    internal const string UnconfirmedWords =
        "Print from inside GroupLab lost every marker and code on a Brother printer on 19 September. The drawing is fixed, and two drivers are "
        + "held to it by a test, but no sheet from the fixed version has been looked at on paper yet, so Open to print is the safer path today.";

    internal const string ScaleWords =
        "Print at actual size. In the print dialog choose \"Actual size\" or \"100%\", never \"Fit\", \"Shrink oversized pages\" or " +
        "\"Fit to printable area\". GroupLab asks the PDF viewer for no scaling, but it cannot set your printer driver, and a sheet " +
        "printed at 97 percent measures 3 percent small.";

    private readonly List<LibrarySheet> sheets;
    private readonly OwnSheets? own;
    private readonly Button saveOwn = new() { Content = "Save to your own sheets", IsVisible = false };
    private readonly ListBox list = new();
    private readonly TextBlock title = new() { FontSize = Tokens.TitleSize, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel loadBlock = new() { Spacing = 6 };
    private readonly RadioButton blank = new() { Content = "Blank, to write on at the range", GroupName = "loadBlock", IsChecked = true };
    private readonly RadioButton filled = new() { Content = "Filled in now, from these fields", GroupName = "loadBlock" };
    private readonly StackPanel fields = new() { Spacing = 4 };
    private readonly Dictionary<string, TextBox> fieldBoxes = new(StringComparer.Ordinal);
    private readonly TextBox serial = new() { Width = 120, PlaceholderText = "optional" };
    private readonly CheckBox note = new() { Content = "Print the actual-size instruction along the bottom of each sheet", IsChecked = true };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock pageCaption = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly Image preview = new() { Stretch = Stretch.Uniform, Height = 520, HorizontalAlignment = HorizontalAlignment.Left };
    private LibrarySheet? selected;
    private int page;

    /// <summary>
    /// The parametric editor, NOTES-FROM-PLANNING.md entry 99 section 3: a form, because every sheet in the library is parametric. It sits in
    /// the print screen because designing a sheet is for printing it, and a design that passes its checks becomes the selected sheet, so the
    /// preview, Save PDF and Print are the ones the library already uses.
    /// </summary>
    private readonly StackPanel designer = new() { Spacing = 8, IsVisible = false };

    private readonly TextBox designName = new() { Width = 260, Text = "My sheet" };

    private readonly ComboBox designPage = new() { ItemsSource = ParametricSheet.Pages.Select(p => p.ToUpperInvariant() == "A4" || p.ToUpperInvariant() == "A3" ? p.ToUpperInvariant() : char.ToUpperInvariant(p[0]) + p[1..]).ToList(), SelectedIndex = 0, MinWidth = 120 };

    private readonly NumericUpDown designColumns = new() { Minimum = 1, Maximum = 12, Value = 5, Increment = 1, FormatString = "0", Width = 120 };

    private readonly NumericUpDown designRows = new() { Minimum = 1, Maximum = 12, Value = 5, Increment = 1, FormatString = "0", Width = 120 };

    private readonly TextBox designSpacing = new() { Width = 120, Text = "1.50" };

    private readonly ComboBox designRing = new() { ItemsSource = ParametricSheet.RingSizes.Select(r => string.Create(CultureInfo.InvariantCulture, $"{r / 254.0:0.00} in")).ToList(), MinWidth = 120 };

    private readonly NumericUpDown designSighters = new() { Minimum = 0, Maximum = 8, Value = 3, Increment = 1, FormatString = "0", Width = 120 };

    private readonly CheckBox designLoadBlock = new() { Content = "A load block along the bottom" };

    private readonly TextBox designGroup = new() { Width = 120, PlaceholderText = "MOA" };

    private readonly TextBox designDistance = new() { Width = 120, Text = "100" };

    private readonly StackPanel designChecks = new() { Spacing = 4 };

    private bool designing;

    public PrintWindow()
        : this((OwnSheets?)null)
    {
    }

    /// <summary>
    /// The built-in library and, NOTES-FROM-PLANNING.md entry 112 section 3, the person's own sheets after it, each printed exactly as a
    /// built-in one is, through the same refusals; with somewhere to keep them, the designer can save what it makes.
    /// </summary>
    internal PrintWindow(OwnSheets? own)
        : this([.. BuiltIn(), .. own?.List() ?? []], own)
    {
    }

    internal PrintWindow(IReadOnlyList<LibrarySheet> library, OwnSheets? own = null)
    {
        sheets = [.. library];
        this.own = own;
        Title = "GroupLab: print a target";
        Width = 1200;
        Height = 860;

        FillList();
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedIndex >= 0)
            {
                ShowDesigner(false);
                Show(sheets[list.SelectedIndex]);
            }
        };

        BuildDesigner();
        blank.Click += (_, _) => ShowFields();
        filled.Click += (_, _) => ShowFields();

        var details = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        details.Children.Add(designer);
        details.Children.Add(title);
        details.Children.Add(summary);
        details.Children.Add(loadBlock);
        details.Children.Add(note);
        details.Children.Add(new TextBlock { Text = ScaleWords, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        if (OperatingSystem.IsWindows())
        {
            // Entry 114 section 1: until a sheet from the fixed path has been checked on paper, the viewer is the path to reach for.
            details.Children.Add(new TextBlock { Text = UnconfirmedWords, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.FormWarning } });
        }

        details.Children.Add(PrintRow());
        // Entry 113 section 5: the sheet and its one page of instructions together, for a volunteer.
        details.Children.Add(Row(Button("Print a volunteer pack", PrintPack), new TextBlock { Text = "The sheet and one page telling a volunteer how to shoot, photograph and send it.", VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } }));
        details.Children.Add(status);
        details.Children.Add(Row(Button("Previous sheet", () => Turn(-1)), Button("Next sheet", () => Turn(1)), pageCaption));
        details.Children.Add(new TextBlock { Text = "The preview shows the artwork; its text is drawn in the PDF.", FontSize = Tokens.DetailSize, Opacity = 0.7 });
        details.Children.Add(preview);

        var dock = new DockPanel();
        var leftPanel = new DockPanel();
        var design = Button("Design your own sheet", () => ShowDesigner(true));
        design.Margin = new Thickness(8);
        DockPanel.SetDock(design, Dock.Top);
        leftPanel.Children.Add(design);
        leftPanel.Children.Add(new ScrollViewer { Content = list });
        var left = new Border { Child = leftPanel, Width = 420 };
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
            SetStatus("The built-in library was not found beside the application.", StatusKind.Alert);
        }
    }

    /// <summary>The built-in library beside the application.</summary>
    internal static IReadOnlyList<LibrarySheet> BuiltIn() => TargetLibrary.Load(Path.Combine(AppContext.BaseDirectory, "targets"));

    /// <summary>Raised when the designer saves a sheet into the person's own, so the target library can show it.</summary>
    internal event Action? SheetsChanged;

    /// <summary>The list, each sheet under its family, the person's own last.</summary>
    private void FillList()
    {
        list.ItemsSource = sheets.Select(s =>
        {
            var item = new StackPanel { Spacing = 1, Margin = new Thickness(2, 4) };
            item.Children.Add(new TextBlock { Text = s.Family, FontSize = Tokens.DetailSize, Opacity = 0.7 });
            item.Children.Add(new TextBlock { Text = s.Definition.Name, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            item.Children.Add(new TextBlock { Text = s.Summary, FontSize = Tokens.LabelSize, TextWrapping = TextWrapping.Wrap });
            return item;
        }).ToList();
    }

    /// <summary>
    /// Keeps the design the designer shows as one of the person's own sheets, entry 112 section 3, and selects it in the list, where it prints
    /// as any other sheet does.
    /// </summary>
    internal LibrarySheet? SaveDesign()
    {
        if (own is null || Designed is not { } design)
        {
            return null;
        }

        var saved = own.Save(design.Definition);
        DiagnosticLog.Info("sheet.save", ("sheet", saved.File));
        sheets.RemoveAll(s => s.Family == OwnSheets.Family);
        sheets.AddRange(own.List());
        FillList();
        ShowDesigner(false);
        Select(saved.File);
        SetStatus($"Saved as {saved.Definition.Name} in your own sheets.", StatusKind.Success);
        SheetsChanged?.Invoke();
        return saved;
    }

    /// <summary>Opens the designer, as the target library's Design your own sheet does.</summary>
    internal void Design() => ShowDesigner(true);

    /// <summary>The designer's checks as they read, for the headless tests.</summary>
    internal IReadOnlyList<(string Text, string Kind)> DesignChecks =>
        [.. designChecks.Children.OfType<TextBlock>().Select(t => (t.Text ?? "", t.Classes.Contains(AppStyles.FormError) ? "error" : t.Classes.Contains(AppStyles.FormWarning) ? "warning" : "fine"))];

    /// <summary>The sheet the designer made, or null while it refuses one.</summary>
    internal LibrarySheet? Designed => designing ? selected : null;

    /// <summary>Sets the form, for the headless tests; each field is set as a person would, and the design follows.</summary>
    internal void SetDesign(string pageName, int columns, int rows, string spacing, int ringDmm, int sighters, bool loadBlock, string? group = null, string? distance = null)
    {
        ShowDesigner(true);
        designPage.SelectedIndex = ParametricSheet.Pages.ToList().IndexOf(pageName);
        designColumns.Value = columns;
        designRows.Value = rows;
        designSpacing.Text = spacing;
        designRing.SelectedIndex = ParametricSheet.RingSizes.ToList().IndexOf(ringDmm);
        designSighters.Value = sighters;
        designLoadBlock.IsChecked = loadBlock;
        designGroup.Text = group ?? "";
        designDistance.Text = distance ?? "100";
        Redesign();
    }

    private void BuildDesigner()
    {
        designRing.SelectedIndex = ParametricSheet.RingSizes.ToList().IndexOf(254);
        TextBlock Label(string text) => new() { Text = text, Width = 190, VerticalAlignment = VerticalAlignment.Center };
        designer.Children.Add(new TextBlock { Text = "Design your own sheet", FontSize = Tokens.TitleSize, FontWeight = FontWeight.SemiBold });
        designer.Children.Add(new TextBlock
        {
            Text = "Every sheet in the library is a grid of bulls, so a grid is what this designs: the page, the rows and columns, the spacing, the ring, a sighter row and a load block. The sheet is laid out by the same rule the library was.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Secondary },
        });
        designer.Children.Add(Row(Label("Name"), designName));
        designer.Children.Add(Row(Label("Page"), designPage));
        designer.Children.Add(Row(Label("Columns and rows"), designColumns, designRows));
        designer.Children.Add(Row(Label("Spacing between bulls, in"), designSpacing));
        designer.Children.Add(Row(Label("Ring"), designRing));
        designer.Children.Add(Row(Label("Sighters"), designSighters));
        designer.Children.Add(designLoadBlock);
        designer.Children.Add(Row(Label("Your five-shot group, MOA"), designGroup, new TextBlock { Text = "at", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) }, designDistance, new TextBlock { Text = "yd", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) }));
        designer.Children.Add(new TextBlock { Text = "Optional: with it, the designer says what the spacing means for your rifle.", Classes = { AppStyles.Secondary } });
        designer.Children.Add(designChecks);
        saveOwn.Click += (_, _) => SaveDesign();
        designer.Children.Add(saveOwn);

        foreach (var box in new[] { designName, designSpacing, designGroup, designDistance })
        {
            box.TextChanged += (_, _) => Redesign();
        }

        foreach (var combo in new[] { designPage, designRing })
        {
            combo.SelectionChanged += (_, _) => Redesign();
        }

        foreach (var number in new[] { designColumns, designRows, designSighters })
        {
            number.ValueChanged += (_, _) => Redesign();
        }

        designLoadBlock.IsCheckedChanged += (_, _) => Redesign();
    }

    private void ShowDesigner(bool on)
    {
        if (designing == on)
        {
            return;
        }

        designing = on;
        designer.IsVisible = on;
        if (on)
        {
            list.SelectedIndex = -1;
            Redesign();
        }
    }

    /// <summary>
    /// Designs the sheet the form describes and shows every check: a refusal in red and a warning in amber, the meanings entry 93 fixed
    /// (entry 100 section 1), each a sentence with the number in it. A refused design leaves nothing to print; one that passes becomes the
    /// selected sheet, and the preview follows.
    /// </summary>
    private void Redesign()
    {
        if (!designing)
        {
            return;
        }

        designChecks.Children.Clear();
        if (!double.TryParse(designSpacing.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double spacing) || spacing <= 0)
        {
            Check(CheckLevel.Refusal, "Enter the spacing between bulls in inches, such as 1.50.");
            selected = null;
            preview.Source = null;
            return;
        }

        var spec = new SheetSpec(designName.Text ?? "", ParametricSheet.Pages[Math.Max(0, designPage.SelectedIndex)], (int)(designColumns.Value ?? 5), (int)(designRows.Value ?? 5),
            spacing, ParametricSheet.RingSizes[Math.Max(0, designRing.SelectedIndex)], (int)(designSighters.Value ?? 0), designLoadBlock.IsChecked == true);
        var design = ParametricSheet.Design(spec);
        foreach (var check in design.Checks)
        {
            Check(check.Level, check.Sentence);
        }

        if (double.TryParse(designGroup.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double group) && group > 0
            && double.TryParse(designDistance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double distance) && distance > 0)
        {
            var spacingCheck = ParametricSheet.Spacing(spacing, group, distance);
            Check(spacingCheck.Level, spacingCheck.Sentence);
        }

        saveOwn.IsVisible = design.Printable && own is not null;
        if (design.Printable)
        {
            selected = new LibrarySheet("custom.gltd.json", "Your own sheet", null, design.Definition!);
            page = 0;
            title.Text = design.Definition!.Name;
            summary.Text = design.Definition.Description;
            loadBlock.Children.Clear();
            fieldBoxes.Clear();
            ShowPreview();
        }
        else
        {
            selected = null;
            preview.Source = null;
            title.Text = "";
            summary.Text = "";
        }
    }

    private void Check(CheckLevel level, string sentence) => designChecks.Children.Add(new TextBlock
    {
        Text = sentence,
        TextWrapping = TextWrapping.Wrap,
        Classes = { level switch { CheckLevel.Refusal => AppStyles.FormError, CheckLevel.Warning => AppStyles.FormWarning, _ => AppStyles.Secondary } },
    });

    /// <summary>The sheets listed, for the headless tests.</summary>
    internal IReadOnlyList<LibrarySheet> Sheets => sheets;

    /// <summary>The preview image, for the headless tests.</summary>
    internal IImage? PreviewSource => preview.Source;

    /// <summary>The message line, for the headless tests.</summary>
    internal string StatusText => status.Text ?? "";

    internal StatusKind StatusState { get; private set; } = StatusKind.Information;

    /// <summary>
    /// Shows a message with the state it reports, NOTES-FROM-PLANNING.md entry 70 section 6. The line used to be red for everything,
    /// including a successful save, which teaches people to read past red; the next red message may be the one that matters.
    /// </summary>
    private void SetStatus(string text, StatusKind kind)
    {
        status.Text = text;
        StatusState = kind;
        status.Classes.Remove(AppStyles.Alert);
        status.Classes.Remove(AppStyles.Good);
        if (kind != StatusKind.Information)
        {
            status.Classes.Add(kind == StatusKind.Alert ? AppStyles.Alert : AppStyles.Good);
        }
    }

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
            SetStatus("This sheet cannot be printed as set: " + string.Join(" ", result.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message)), StatusKind.Alert);
            DiagnosticLog.Warn("print.render", ("sheet", selected.File), ("filled", fill), ("errors", result.Diagnostics.Count(d => d.Severity == Severity.Error)));
            return null;
        }

        SetStatus("", StatusKind.Information);
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
            SetStatus("The PDF could not be written: " + ex.Message, StatusKind.Alert);
            DiagnosticLog.Exception(LogLevel.Warn, "file.save", ex, [.. DiagnosticLog.File(path), ("kind", "pdf")]);
            return false;
        }

        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "pdf"), ("sheet", selected!.File), ("pages", result.Pages.Count)]);

        SetStatus(result.Pages.Count > 1
            ? string.Create(CultureInfo.InvariantCulture, $"Saved {result.Pages.Count} sheets to {path}. Print every page; each is numbered beside its identifier.")
            : "Saved to " + path + ".", StatusKind.Success);
        return true;
    }

    private void Show(LibrarySheet sheet)
    {
        DiagnosticLog.Info("print.select", ("sheet", sheet.File));
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
            loadBlock.Children.Add(Row(new TextBlock { Text = "Serial", Width = 140, VerticalAlignment = VerticalAlignment.Center }, Detached(serial)));
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
            fields.Children.Add(Row(new TextBlock { Text = SceneBuilder.FieldCaption(key), Width = 140, VerticalAlignment = VerticalAlignment.Center }, Detached(box)));
        }
    }

    /// <summary>Turns the preview by sheets of a tiled set.</summary>
    internal void Turn(int by)
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

        (preview.Source as IDisposable)?.Dispose();
        preview.Source = Preview(selected.Definition, page);
        pageCaption.Text = string.Create(CultureInfo.InvariantCulture, $"Sheet {page + 1} of {selected.Sheets}");
    }

    /// <summary>One sheet's artwork as a bitmap with its longer side near 900 pixels, or null when it has none.</summary>
    internal static Bitmap? Preview(TargetDefinition definition, int tile = 0)
    {
        var scenes = SceneBuilder.Build(definition, new RenderOptions(TileIndex: tile));
        if (scenes.Pages.Count == 0)
        {
            return null;
        }

        var scene = scenes.Pages[0];
        double longerInches = Math.Max(scene.Width, scene.Height) / (2.0 * 254);
        var image = SceneRasterizer.Rasterize(scene, Math.Min(100, 900 / longerInches));
        using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
        OpenCvSharp.Cv2.ImEncode(".png", mat, out byte[] png);
        using var stream = new MemoryStream(png);
        return new Bitmap(stream);
    }

    private async Task SaveDialog()
    {
        if (selected is null)
        {
            return;
        }

        DiagnosticLog.Info("dialog.open", ("dialog", "save-pdf"));
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the target as a PDF",
            SuggestedFileName = Path.GetFileName(selected.File).Replace(".gltd.json", ".pdf", StringComparison.Ordinal),
            DefaultExtension = "pdf",
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "save-pdf"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            SavePdf(path);
        }
    }

    /// <summary>
    /// Opens the target in the PDF viewer to be printed from there, NOTES-FROM-PLANNING.md entry 105 section 9 and entry 106 section 1. The PDF
    /// is written to a temporary file and opened, and a dialog says what is true: it is open in the viewer, and it must be printed at actual
    /// size. GroupLab sends nothing to a printer on this path; on Windows, printing from inside GroupLab is <see cref="PrintHere"/>.
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

        var (start, opened, kind) = PrintLaunch(path);
        try
        {
            Process.Start(start)?.Dispose();
        }
        catch (Win32Exception ex)
        {
            SetStatus("No application could open the PDF (" + ex.Message + "). It is saved at " + path + "; print it from elsewhere at actual size.", StatusKind.Alert);
            DiagnosticLog.Exception(LogLevel.Warn, "print.open", ex, ("fallback", "the saved PDF"));
            return;
        }

        // Entry 105 section 9: a launch that worked leaves a trace too, which is what was missing when the print verb printed silently.
        DiagnosticLog.Info("print.open", [.. DiagnosticLog.File(path), ("verb", "none"), ("returned", true)]);
        SetStatus(opened, kind);
        Confirm(opened);
    }

    /// <summary>
    /// Writes the volunteer pack, NOTES-FROM-PLANNING.md entry 113 section 5: every page of the sheet as the print screen renders it, blank or
    /// filled as chosen, then the instruction page at the same paper size. False with the reason in the message line.
    /// </summary>
    internal bool SaveVolunteerPack(string path)
    {
        if (selected is null || Render() is not { } result)
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllBytes(path, GroupLab.Core.Reporting.VolunteerPack.Write(selected.Definition, result.Pages));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetStatus("The pack could not be written: " + ex.Message, StatusKind.Alert);
            DiagnosticLog.Exception(LogLevel.Warn, "file.save", ex, [.. DiagnosticLog.File(path), ("kind", "pack")]);
            return false;
        }

        DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "pack"), ("sheet", selected.File), ("pages", result.Pages.Count + 1)]);
        SetStatus($"Saved the volunteer pack, {result.Pages.Count + 1} pages, to {path}.", StatusKind.Success);
        return true;
    }

    /// <summary>Opens the volunteer pack in the PDF viewer to print, with the actual-size reminder, as Open to print does for a sheet.</summary>
    private void PrintPack()
    {
        if (selected is null)
        {
            return;
        }

        string path = Path.Combine(Path.GetTempPath(), "GroupLab", selected.File.Replace(".gltd.json", " volunteer pack.pdf", StringComparison.Ordinal));
        if (!SaveVolunteerPack(path))
        {
            return;
        }

        var (start, opened, kind) = PrintLaunch(path);
        try
        {
            Process.Start(start)?.Dispose();
        }
        catch (Win32Exception ex)
        {
            SetStatus("No application could open the pack (" + ex.Message + "). It is saved at " + path + "; print it from elsewhere at actual size.", StatusKind.Alert);
            DiagnosticLog.Exception(LogLevel.Warn, "print.open", ex, ("fallback", "the saved pack"));
            return;
        }

        DiagnosticLog.Info("print.open", [.. DiagnosticLog.File(path), ("verb", "none"), ("returned", true), ("kind", "pack")]);
        SetStatus(opened, kind);
        Confirm(opened);
    }

    /// <summary>
    /// The print buttons, NOTES-FROM-PLANNING.md entry 107 section 2, and entry 114 section 1. On Windows "Print…" prints from inside
    /// GroupLab, and it was the primary until its rectangles were found missing from every sheet a Brother MFC-J430W printed: no markers, no
    /// codes and no load block, on paper that looks normal until it comes back from the range. The drawing is fixed and held by a test on two
    /// drivers, but <b>no sheet from the fixed path has been checked on paper yet</b>, so "Open to print" is the primary until one has been,
    /// and the line above the buttons says so. Nothing is hidden: a person who wants the in-app path still has it.
    /// </summary>
    private StackPanel PrintRow()
    {
        var open = Button("Open to print", Print);
        var save = Button("Save PDF…", async () => await SaveDialog());
        if (!OperatingSystem.IsWindows())
        {
            return Row(save, open);
        }

        open.Classes.Add(AppStyles.Primary);
        return Row(open, save, Button("Print…", PrintHere));
    }

    /// <summary>
    /// Prints from inside GroupLab through the Windows print dialog, entry 107 section 2: the sheet drawn as vector at actual size, or a refusal
    /// that says why and prints nothing. The confirmation names the printer, the sheet, the page count and "at actual size", and says the job
    /// was sent to the print queue, never that paper came out.
    /// </summary>
    private void PrintHere()
    {
        if (!OperatingSystem.IsWindows() || selected is null || Render() is not { } result)
        {
            return;
        }

        string sheet = selected.Definition.Name;
        DiagnosticLog.Info("dialog.open", ("dialog", "print"), ("sheet", selected.File), ("pages", result.Pages.Count));
        var outcome = WindowsPrinter.PrintWithDialog(TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, result.Pages, sheet);
        DiagnosticLog.Info("print.send", ("sheet", selected.File), ("outcome", outcome.Kind.ToString()), ("pages", outcome.Pages));
        switch (outcome.Kind)
        {
            case PrintOutcomeKind.Cancelled:
                SetStatus(outcome.Message, StatusKind.Information);
                break;
            case PrintOutcomeKind.Sent:
                SetStatus(outcome.Message, StatusKind.Success);
                Confirm(outcome.Message, "Printed");
                break;
            default:
                SetStatus(outcome.Message, StatusKind.Alert);
                Confirm(outcome.Message, "Not printed");
                break;
        }
    }

    /// <summary>
    /// The confirmation entry 106 section 1 asks for: a status line was missed, so a dialog says what GroupLab did and what the person must do,
    /// and nothing more, since on this path GroupLab only opened a file.
    /// </summary>
    private void Confirm(string text, string heading = "Open to print")
    {
        var ok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true, Classes = { AppStyles.Primary } };
        var dialog = new Window
        {
            Title = heading,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(Tokens.Space12),
                Spacing = Tokens.Space12,
                Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, ok },
            },
        };
        ok.Click += (_, _) => dialog.Close();
        Confirmation = dialog;
        _ = dialog.ShowDialog(this);
    }

    /// <summary>The last confirmation shown, for the headless tests.</summary>
    internal Window? Confirmation { get; private set; }

    /// <summary>
    /// How the PDF is handed to the system, and what to tell the person. It is opened, on every platform, with no verb.
    /// <para>
    /// <b>Why no print verb, entry 105 section 9.</b> Windows' "print" verb runs whatever command the default PDF program registered for it,
    /// and GroupLab cannot see what that does: it can show a dialog, show one out of sight, or print straight to the default printer at the
    /// program's own scaling. On Alan's machine it printed silently while the screen promised a dialog, which is the outcome the print screen
    /// exists to prevent. <b>Linux and macOS never had a verb</b> (entry 61 section 3 and its correction in entry 65): .NET refuses any verb
    /// but open there. So every platform now does the one thing that is the same everywhere and can be described truthfully.
    /// </para>
    /// </summary>
    internal static (ProcessStartInfo Start, string Status, StatusKind Kind) PrintLaunch(string path) =>
        (new ProcessStartInfo(path) { UseShellExecute = true }, OpenedText, StatusKind.Information);

    /// <summary>What the person is told once the PDF is open, in the status line and in the confirmation.</summary>
    internal const string OpenedText = "The target is open in your PDF viewer. Print it from there, choosing Actual size or 100 percent, never Fit.";

    /// <summary>
    /// A control shown again in a rebuilt row, taken out of the row it was last in. The serial box and the load block's field boxes
    /// outlive the rows that hold them, and Avalonia refuses a control that already has a parent: selecting a second sheet with a load
    /// block, or choosing Filled a second time, crashed the screen (NOTES-FROM-PLANNING.md entry 39 section 2).
    /// </summary>
    private static T Detached<T>(T control)
        where T : Control
    {
        if (control.Parent is Panel parent)
        {
            parent.Children.Remove(control);
        }

        return control;
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

/// <summary>What a status line reports, NOTES-FROM-PLANNING.md entry 70 section 6: success in teal, information in plain text, alert in red.</summary>
internal enum StatusKind
{
    Success,
    Information,
    Alert,
}
