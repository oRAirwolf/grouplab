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
using GroupLab.Core.Marking;
using GroupLab.Core.Printing;
using GroupLab.Core.Rendering;
using Orientation = Avalonia.Layout.Orientation;
using RenderOptions = GroupLab.Core.Rendering.RenderOptions;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// The selected sheet's half of the Targets screen, NOTES-FROM-PLANNING.md entry 155, which merged the target library and the print window
/// because they did the same job: the library's grouped list chooses the sheet, and this panel beside it holds everything the print window
/// held, so no second window opens at any point. Before the merge it was the print screen of entry 25 section 2: where a new user starts,
/// because GroupLab's premise is that you print its target, shoot it and photograph it. It calls the renderer that already works.
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
public sealed class PrintPanel : UserControl
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
        "\"Fit to printable area\". GroupLab asks the PDF viewer for no scaling, but it cannot set your printer driver. " +
        DetectionAdvice.WhyActualSize + " A sheet printed at 97 percent and photographed makes every group read about 3 percent large.";

    private readonly OwnSheets? own;
    private readonly Button saveOwn = new() { Content = "Save to your own sheets", IsVisible = false };
    private readonly Func<IReadOnlyList<LibrarySheet>> library;
    private readonly TextBlock title = new() { FontSize = Tokens.TitleSize, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel loadBlock = new() { Spacing = Tokens.Space8 };
    private readonly RadioButton blank = new() { Content = "Blank, to write on at the range", GroupName = "loadBlock", IsChecked = true };
    private readonly RadioButton filled = new() { Content = "Filled in now, from these fields", GroupName = "loadBlock" };
    private readonly StackPanel fields = new() { Spacing = 4 };
    private readonly Dictionary<string, TextBox> fieldBoxes = new(StringComparer.Ordinal);
    private readonly TextBox serial = new() { Width = 120, PlaceholderText = "optional" };
    private readonly CheckBox note = new() { Content = "Print the actual-size instruction along the bottom of each sheet", IsChecked = true };

    /// <summary>Entry 226 section 5.1: a tiled target's sheets on one large page, with cut lines, for a plotter.</summary>
    private readonly CheckBox oneSheet = new() { Content = "Print every sheet on one large page, with cut lines between them, for a plotter", IsVisible = false };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock pageCaption = new() { VerticalAlignment = VerticalAlignment.Center };
    /// <summary>Entry 300: the page showing, drawn live from its scene by the screen's one preview.</summary>
    private Scene? shown;
    private LibrarySheet? selected;
    private int page;

    /// <summary>
    /// The parametric editor, NOTES-FROM-PLANNING.md entry 99 section 3: a form, because every sheet in the library is parametric. It sits in
    /// the print screen because designing a sheet is for printing it, and a design that passes its checks becomes the selected sheet, so the
    /// preview, Save PDF and Print are the ones the library already uses.
    /// </summary>
    private readonly StackPanel designer = new() { Spacing = 8, IsVisible = false };

    private readonly TextBox designName = new() { Width = 260, Text = "My sheet" };

    private readonly ComboBox designPage = new() { ItemsSource = ParametricSheet.Pages.Select(ParametricSheet.PageWords).ToList(), SelectedIndex = 0, MinWidth = 120 };

    private readonly NumericUpDown designColumns = new() { Minimum = 1, Maximum = 12, Value = 5, Increment = 1, FormatString = "0", Width = 120 };

    private readonly NumericUpDown designRows = new() { Minimum = 1, Maximum = 12, Value = 5, Increment = 1, FormatString = "0", Width = 120 };

    private readonly TextBox designSpacing = new() { Width = 120, Text = "1.50" };

    private readonly ComboBox designRing = new() { ItemsSource = ParametricSheet.RingSizes.Select(r => string.Create(CultureInfo.InvariantCulture, $"{r / 254.0:0.00} in")).ToList(), MinWidth = 120 };

    /// <summary>
    /// Entry 243 section 4 item 3: the bull the grid is drawn with. The rings are the default; E and C are offered beside them, each as its
    /// own ring set, and neither becomes the default until Alan has shot them and chooses.
    /// </summary>
    internal static readonly string[] BullChoices = ["Rings", "E: a black disc, white center and dot", "C: a black diamond on a point, white center and dot"];

    private readonly ComboBox designBull = new() { ItemsSource = BullChoices, SelectedIndex = 0, MinWidth = 120 };

    /// <summary>Entry 303 item 1: the bull's size in inches, MOA or mil at the sheet's distance, in place of the ring's size.</summary>
    private readonly TextBox designSize = new() { Width = 120, PlaceholderText = "optional" };

    private readonly ComboBox designSizeUnit = new() { ItemsSource = ParametricSheet.BullUnits, SelectedIndex = 2, MinWidth = 90 };

    /// <summary>Made for your optic's shape, entry 243 section 4 item 4: the disc, or C's diamond, sized by the same rule.</summary>
    private readonly ComboBox genShape = new() { ItemsSource = new[] { "Disc", "Diamond" }, SelectedIndex = 0, MinWidth = 120 };

    private readonly NumericUpDown designSighters = new() { Minimum = 0, Maximum = 8, Value = 3, Increment = 1, FormatString = "0", Width = 120 };

    private readonly CheckBox designLoadBlock = new() { Content = "A load block along the bottom" };

    private readonly TextBox designGroup = new() { Width = 120, PlaceholderText = "MOA" };

    private readonly TextBox designDistance = new() { Width = 120, Text = "100" };

    private readonly StackPanel designChecks = new() { Spacing = 4 };

    // Entry 226 section 4: the target generator, above the grid form.
    private readonly TextBox genDistance = new() { Width = 120, Text = "100" };
    private readonly TextBox genMagnification = new() { Width = 120, Text = "10" };
    private readonly TextBox genDot = new() { Width = 120, PlaceholderText = "MOA" };
    private readonly NumericUpDown genShots = new() { Minimum = 1, Maximum = 200, Value = 25, Increment = 1, FormatString = "0", Width = 120 };
    private readonly StackPanel genSaid = new() { Spacing = 4 };

    private bool designing;

    public PrintPanel()
        : this((OwnSheets?)null)
    {
    }

    /// <summary>
    /// The built-in library and, NOTES-FROM-PLANNING.md entry 112 section 3, the person's own sheets after it, each printed exactly as a
    /// built-in one is, through the same refusals; with somewhere to keep them, the designer can save what it makes.
    /// </summary>
    internal PrintPanel(OwnSheets? own)
        : this(() => [.. BuiltIn(), .. own?.List() ?? []], own)
    {
    }

    internal PrintPanel(IReadOnlyList<LibrarySheet> library, OwnSheets? own = null)
        : this(() => library, own)
    {
    }

    /// <summary>A panel reading its sheets from the library that shows it, so a sheet saved, renamed or deleted there is the same sheet here.</summary>
    internal PrintPanel(Func<IReadOnlyList<LibrarySheet>> library, OwnSheets? own = null)
    {
        this.library = library;
        this.own = own;

        BuildDesigner();
        blank.Click += (_, _) => ShowFields();
        filled.Click += (_, _) => ShowFields();

        var details = new StackPanel { Margin = new Thickness(16), Spacing = Tokens.Space12 };
        details.Children.Add(designer);
        details.Children.Add(title);
        details.Children.Add(summary);

        // Entry 297: the bulls in black, blue or red, remembered for each sheet; the preview and the PDF follow at once.
        bullColour.ItemsSource = BullColours.All.Select(c => ColourWords(c)).ToList();
        bullColour.SelectedIndex = 0;
        bullColour.SelectionChanged += (_, _) =>
        {
            if (showingColour || selected is null)
            {
                return;
            }

            var chosen = Colour;
            Settings?.SaveBullColour(ColourKey(selected), chosen);
            DiagnosticLog.Info("print.color", ("sheet", selected.File), ("color", BullColours.Name(chosen)));
            ShowPreview();
        };
        var colourLabel = new TextBlock { Text = "Bulls in", VerticalAlignment = VerticalAlignment.Center };
        details.Children.Add(new StackPanel
        {
            Spacing = Tokens.Space4,
            Children =
            {
                Row(colourLabel, bullColour),
                new TextBlock { Text = ColourNote, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } },
            },
        });
        details.Children.Add(loadBlock);
        details.Children.Add(note);
        details.Children.Add(oneSheet);
        details.Children.Add(new TextBlock { Text = ScaleWords, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
        if (OperatingSystem.IsWindows())
        {
            // Entry 114 section 1: until a sheet from the fixed path has been checked on paper, the viewer is the path to reach for.
            details.Children.Add(new TextBlock { Text = UnconfirmedWords, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.FormWarning } });
        }

        details.Children.Add(PrintRow());
        // Entry 113 section 5: the sheet and its one page of instructions together, for a volunteer.
        // Entry 155: the words under the button rather than beside it, so they wrap in the Targets screen's narrower column.
        var pack = Button("Print a volunteer pack", PrintPack);
        pack.HorizontalAlignment = HorizontalAlignment.Left;
        details.Children.Add(new StackPanel { Spacing = Tokens.Space4, Children = { pack, new TextBlock { Text = "The sheet and one page telling a volunteer how to shoot, photograph and send it.", TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } } } });
        details.Children.Add(status);
        // The pages of a tiled set turn here; the preview itself is the library's, beside this panel, which shows every page this raises.
        details.Children.Add(Row(Button("Previous sheet", () => Turn(-1)), Button("Next sheet", () => Turn(1)), pageCaption));
        note.IsCheckedChanged += (_, _) => ShowPreview();
        details.Margin = new Thickness(0);
        Content = details;

        if (library().Count == 0)
        {
            Fail("The library is missing", "The built-in library was not found beside the application.");
        }
    }

    /// <summary>The settings file the bull color is remembered in, per sheet (entry 297); none in a panel a test makes alone.</summary>
    internal AppSettingsStore? Settings { get; init; }

    private readonly ComboBox bullColour = new() { MinWidth = 140, [Avalonia.Automation.AutomationProperties.NameProperty] = "Bulls in" };
    private bool showingColour;

    /// <summary>The bull color chosen for the sheet showing.</summary>
    internal BullColour Colour => BullColours.All[Math.Max(0, bullColour.SelectedIndex)];

    /// <summary>Chooses a bull color as the drop-down does, for the headless tests.</summary>
    internal void ChooseColour(BullColour colour) => bullColour.SelectedIndex = BullColours.All.ToList().IndexOf(colour);

    /// <summary>What the drop-down offers for each color.</summary>
    internal static string ColourWords(BullColour colour) => colour switch
    {
        BullColour.Red => "Red",
        BullColour.Blue => "Blue",
        _ => "Black",
    };

    /// <summary>The line under the choice: what takes the color, and why the rest stays black.</summary>
    internal const string ColourNote = "Only the bulls, their rings and numbers take the color; the corner codes, markers, title and load block stay black. Large solid areas print as a lighter tint, which saves ink and shows a hole better. On a black and white printer the colors print as gray.";

    /// <summary>The key a sheet's color is remembered under: its file, or the designer's own for a design not yet saved.</summary>
    private static string ColourKey(LibrarySheet sheet) => sheet.File == "custom.gltd.json" ? "designer" : Path.GetFileName(sheet.File);

    /// <summary>The drop-down set to a sheet's remembered color without saving it again.</summary>
    private void ShowColour(LibrarySheet sheet)
    {
        showingColour = true;
        ChooseColour(Settings?.LoadBullColour(ColourKey(sheet)) ?? BullColour.Black);
        showingColour = false;
    }

    /// <summary>
    /// Raised with each page this panel shows, as the scene its PDF is written from, so the screen's one preview draws it live and sharp
    /// (entry 300); null where there is none.
    /// </summary>
    internal event Action<Scene?>? PageShown;

    /// <summary>Raised when the panel chooses a sheet itself, a design it has just saved, so the library can select it too.</summary>
    internal event Action<LibrarySheet>? SheetChosen;

    /// <summary>The sheets this panel can show, for the headless tests.</summary>
    internal IReadOnlyList<LibrarySheet> Sheets => library();

    /// <summary>Shows a sheet: its load block, its settings and its first page. The library calls this when a sheet is chosen.</summary>
    internal void ShowSheet(LibrarySheet sheet)
    {
        ShowDesigner(false);
        Show(sheet);
    }

    /// <summary>The built-in library beside the application.</summary>
    internal static IReadOnlyList<LibrarySheet> BuiltIn() =>
        TargetLibrary.Load(Path.Combine(AppContext.BaseDirectory, "targets"), AppSettingsStore.LetterRegion(AppSettingsStore.Region()));

    /// <summary>Raised when the designer saves a sheet into the person's own, so the target library can show it.</summary>
    internal event Action? SheetsChanged;

    /// <summary>
    /// Keeps the design the designer shows as one of the person's own sheets, entry 112 section 3, and selects it, in the library as well,
    /// where it prints as any other sheet does.
    /// </summary>
    internal LibrarySheet? SaveDesign()
    {
        if (own is null || Designed is not { } design)
        {
            return null;
        }

        var saved = own.Save(design.Definition);
        DiagnosticLog.Info("sheet.save", ("sheet", saved.File));
        SheetsChanged?.Invoke();
        ShowDesigner(false);
        Select(saved.File);
        if (library().FirstOrDefault(s => s.File == saved.File) is { } chosen)
        {
            SheetChosen?.Invoke(chosen);
        }

        SetStatus($"Saved as {saved.Definition.Name} in your own sheets.", StatusKind.Success);
        return saved;
    }

    /// <summary>Opens the designer, as the target library's Design your own sheet does.</summary>
    internal void Design() => ShowDesigner(true);

    /// <summary>The designer's checks as they read, for the headless tests.</summary>
    internal IReadOnlyList<(string Text, string Kind)> DesignChecks =>
        [.. designChecks.Children.OfType<TextBlock>().Select(t => (t.Text ?? "", t.Classes.Contains(AppStyles.FormError) ? "error" : t.Classes.Contains(AppStyles.FormWarning) ? "warning" : "fine"))];

    /// <summary>The sheet the designer made, or null while it refuses one.</summary>
    internal LibrarySheet? Designed => designing ? selected : null;

    /// <summary>Chooses the bull the designer draws, by its place in <see cref="BullChoices"/>, for the headless tests.</summary>
    internal void ChooseBull(int index) => designBull.SelectedIndex = index;

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

    /// <summary>Gives the bull's size and its unit, and the bull's style (0 rings, 1 E, 2 C), as a person would, for the headless tests.</summary>
    internal void SetBullSize(string size, string unit, int style)
    {
        designBull.SelectedIndex = style;
        designSizeUnit.SelectedIndex = ParametricSheet.BullUnits.ToList().IndexOf(unit);
        designSize.Text = size;
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
        designer.Children.Add(new TextBlock { Text = "Made for your optic", FontWeight = FontWeight.SemiBold });
        designer.Children.Add(new TextBlock
        {
            Text = "Say how far, the lowest magnification you will shoot at (1 for a red dot, with the dot's size) and how many shots. GroupLab sizes a bull you can center on through that optic, and makes as many sheets as the shots need.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Secondary },
        });
        designer.Children.Add(Row(Label("Distance, yd"), genDistance));
        designer.Children.Add(Row(Label("Lowest magnification"), genMagnification, new TextBlock { Text = "x", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) }));
        designer.Children.Add(Row(Label("Red dot size, at 1x"), genDot));
        designer.Children.Add(Row(Label("Shots"), genShots));
        designer.Children.Add(Row(Label("Bull shape"), genShape));
        designer.Children.Add(Row(Button("Make the sheet", () => Generate())));
        designer.Children.Add(genSaid);
        designer.Children.Add(new TextBlock { Text = "Or lay out a grid yourself", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        designer.Children.Add(Row(Label("Name"), designName));
        designer.Children.Add(Row(Label("Page"), designPage));
        designer.Children.Add(Row(Label("Columns and rows"), designColumns, designRows));
        designer.Children.Add(Row(Label("Spacing between bulls, in"), designSpacing));
        designer.Children.Add(Row(Label("Ring"), designRing));
        designer.Children.Add(Row(Label("Bull"), designBull));
        designer.Children.Add(Row(Label("Or the bull's size"), designSize, designSizeUnit));
        designer.Children.Add(new TextBlock { Text = "Optional: a size in MOA or mil is read at the distance below, so a 1 mil bull is 3.60 in across at 100 yd.", TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        designer.Children.Add(Row(Label("Sighters"), designSighters));
        designer.Children.Add(designLoadBlock);
        designer.Children.Add(Row(Label("Your five-shot group, MOA"), designGroup, new TextBlock { Text = "at", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) }, designDistance, new TextBlock { Text = "yd", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) }));
        designer.Children.Add(new TextBlock { Text = "Optional: with it, the designer says what the spacing means for your rifle.", Classes = { AppStyles.Secondary } });
        designer.Children.Add(designChecks);
        saveOwn.Click += (_, _) => SaveDesign();
        designer.Children.Add(saveOwn);

        foreach (var box in new[] { designName, designSpacing, designGroup, designDistance, designSize })
        {
            box.TextChanged += (_, _) => Redesign();
        }

        foreach (var combo in new[] { designPage, designRing, designBull, designSizeUnit })
        {
            combo.SelectionChanged += (_, _) => Redesign();
        }

        foreach (var number in new[] { designColumns, designRows, designSighters })
        {
            number.ValueChanged += (_, _) => Redesign();
        }

        designLoadBlock.IsCheckedChanged += (_, _) => Redesign();
    }

    internal void ShowDesigner(bool on)
    {
        if (designing == on)
        {
            return;
        }

        designing = on;
        designer.IsVisible = on;
        if (on)
        {
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
            ShowNothing();
            return;
        }

        int ringDmm = ParametricSheet.RingSizes[Math.Max(0, designRing.SelectedIndex)];

        // Entry 303 item 1: a size given in inches, MOA or mil sets the bull's size; the E and C bulls take it exactly, and the rings, whose
        // stacks come in set sizes, take the nearest, and say so.
        if (!string.IsNullOrWhiteSpace(designSize.Text))
        {
            string unit = ParametricSheet.BullUnits[Math.Max(0, designSizeUnit.SelectedIndex)];
            double? yards = double.TryParse(designDistance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d > 0 ? d : null;
            if (!double.TryParse(designSize.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double size)
                || ParametricSheet.BullInches(size, unit, yards) is not { } inches)
            {
                Check(CheckLevel.Refusal, unit == "in"
                    ? "Enter the bull's size in inches, such as 2.00."
                    : $"Enter the bull's size in {unit} and the distance in yards below; a size in {unit} is read at that distance.");
                selected = null;
                ShowNothing();
                return;
            }

            int wanted = (int)Math.Round(inches * 254);
            ringDmm = designBull.SelectedIndex is 1 or 2 ? wanted : ParametricSheet.RingSizes.MinBy(r => Math.Abs(r - wanted));
            string across = string.Create(CultureInfo.InvariantCulture, $"{inches:0.00} in");
            Check(CheckLevel.Fine, unit == "in"
                ? $"The bull is {across} across{(ringDmm == wanted ? "" : string.Create(CultureInfo.InvariantCulture, $"; the nearest ring set is {ringDmm / 254.0:0.00} in"))}."
                : string.Create(CultureInfo.InvariantCulture, $"{size:0.##} {unit} at {yards:0} yd is {across} across{(ringDmm == wanted ? "" : $"; the nearest ring set is {ringDmm / 254.0:0.00} in, so choose the E or C bull for the exact size")}."));
        }

        var spec = new SheetSpec(designName.Text ?? "", ParametricSheet.Pages[Math.Max(0, designPage.SelectedIndex)], (int)(designColumns.Value ?? 5), (int)(designRows.Value ?? 5),
            spacing, ringDmm, (int)(designSighters.Value ?? 0), designLoadBlock.IsChecked == true,
            designBull.SelectedIndex switch { 1 => LibraryBuilder.EDiscs(ringDmm), 2 => LibraryBuilder.CDiscs(ringDmm), _ => null });
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
            ShowNothing();
            title.Text = "";
            summary.Text = "";
        }
    }

    /// <summary>
    /// Entry 226 section 4: makes the sheet the generator's fields describe, on the page the form has chosen, says why it is the size it is,
    /// and shows it as the design, every sheet of a set a page of the preview. Returns what it made, for the headless tests.
    /// </summary>
    internal GeneratedTargets? Generate(string? distance = null, string? magnification = null, string? dot = null, int? shots = null, bool? diamond = null)
    {
        ShowDesigner(true);
        genShape.SelectedIndex = diamond is { } d ? (d ? 1 : 0) : genShape.SelectedIndex;
        genDistance.Text = distance ?? genDistance.Text;
        genMagnification.Text = magnification ?? genMagnification.Text;
        genDot.Text = dot ?? genDot.Text;
        genShots.Value = shots ?? genShots.Value;
        genSaid.Children.Clear();
        static double? Number(string? text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0 ? v : null;
        if (Number(genDistance.Text) is not { } yards || Number(genMagnification.Text) is not { } power)
        {
            genSaid.Children.Add(new TextBlock { Text = "Enter the distance in yards and the lowest magnification, such as 100 and 10.", TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.FormError } });
            return null;
        }

        var made = TargetGenerator.Generate(new GeneratorRequest(yards, power, Number(genDot.Text), (int)(genShots.Value ?? 25), ParametricSheet.Pages[Math.Max(0, designPage.SelectedIndex)], genShape.SelectedIndex == 1));
        foreach (string sentence in made.Explanation)
        {
            genSaid.Children.Add(new TextBlock { Text = sentence, TextWrapping = TextWrapping.Wrap, Classes = { made.Design is null ? AppStyles.FormError : AppStyles.Secondary } });
        }

        DiagnosticLog.Info("sheet.generate", ("yards", yards), ("power", power), ("shots", made.Request.Shots), ("sheets", made.Sheets), ("bulls", made.Bulls));
        saveOwn.IsVisible = made.Design?.Printable == true && own is not null;
        if (made.Design is { Printable: true, Definition: { } definition })
        {
            selected = new LibrarySheet("custom.gltd.json", "Your own sheet", null, definition);
            page = 0;
            title.Text = definition.Name;
            summary.Text = string.Join(" ", made.Explanation);
            loadBlock.Children.Clear();
            fieldBoxes.Clear();
            ShowPreview();
        }
        else
        {
            selected = null;
            ShowNothing();
        }

        return made;
    }

    private void Check(CheckLevel level, string sentence) => designChecks.Children.Add(new TextBlock
    {
        Text = sentence,
        TextWrapping = TextWrapping.Wrap,
        Classes = { level switch { CheckLevel.Refusal => AppStyles.FormError, CheckLevel.Warning => AppStyles.FormWarning, _ => AppStyles.Secondary } },
    });


    /// <summary>The page the preview draws, for the headless tests.</summary>
    internal Scene? PreviewSource => shown;

    /// <summary>No page to show: the preview clears.</summary>
    private void ShowNothing()
    {
        shown = null;
        PageShown?.Invoke(null);
    }

    /// <summary>What the screen says about the selected sheet, and whether it offers one large page with cut lines, for the headless tests.</summary>
    internal string SummaryText => summary.Text ?? "";

    internal bool OneSheetOffered => oneSheet.IsVisible;

    /// <summary>The message line, for the headless tests.</summary>
    internal string StatusText => status.Text ?? "";

    internal StatusKind StatusState { get; private set; } = StatusKind.Information;

    /// <summary>
    /// Shows a message with the state it reports, NOTES-FROM-PLANNING.md entry 70 section 6. The line used to be red for everything,
    /// including a successful save, which teaches people to read past red; the next red message may be the one that matters.
    /// </summary>
    /// <summary>Entry 356 section 3: where a failure goes to be said in the middle of the window; the window sets it.</summary>
    internal Action<string, string>? Problem { get; set; }

    /// <summary>A failure: in the status line as before, and said in the middle of the window with <paramref name="title"/>.</summary>
    private void Fail(string title, string text)
    {
        SetStatus(text, StatusKind.Alert);
        Problem?.Invoke(title, text);
    }

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
    internal void Select(string file)
    {
        if (library().FirstOrDefault(s => s.File == file) is { } sheet)
        {
            ShowSheet(sheet);
        }
    }

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
            PrintNote: note.IsChecked == true ? SceneBuilder.ActualSizeNote : null,
            OneSheet: oneSheet.IsVisible && oneSheet.IsChecked == true,
            BullColour: Colour));
        if (result.Pdf is null)
        {
            Fail("This sheet cannot be printed as set", "This sheet cannot be printed as set: " + string.Join(" ", result.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message)));
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
            Fail("The PDF could not be written", "The PDF could not be written: " + ex.Message);
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
        ShowColour(sheet);
        title.Text = sheet.Definition.Name;
        summary.Text = sheet.Family + ". " + sheet.Summary + LargeSheetWords(sheet.Definition);
        oneSheet.IsVisible = CutSheet.Refusal(sheet.Definition) is null;
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

    /// <summary>
    /// Entry 226 section 5: what a sheet too large for a flatbed means for scanning or photographing it, and that tiled Letter or A4 pages
    /// are the better choice for a large target; nothing for a sheet a flatbed takes.
    /// </summary>
    internal static string LargeSheetWords(GroupLab.Core.Gltd.Model.TargetDefinition definition) =>
        GroupLab.Core.Capture.PhotographLimit.ForSheet(definition) is { } advice ? " " + advice : "";

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

        // Entry 300: the scene itself, not a picture of it; the preview draws it as vectors at the screen's own resolution.
        var scenes = SceneBuilder.Build(selected.Definition, new RenderOptions(TileIndex: page, PrintNote: note.IsChecked == true ? SceneBuilder.ActualSizeNote : null, BullColour: Colour));
        shown = scenes.Pages.Count > 0 ? scenes.Pages[0] : null;
        PageShown?.Invoke(shown);
        pageCaption.Text = string.Create(CultureInfo.InvariantCulture, $"Sheet {page + 1} of {selected.Sheets}");
    }

    /// <summary>
    /// One sheet as it prints, words and all (entry 250 section 1), as a bitmap with its longer side near 900 pixels, or null when it has
    /// none. The actual-size instruction is on it when the PDF will carry it.
    /// </summary>
    internal static Bitmap? Preview(TargetDefinition definition, int tile = 0, bool note = true, BullColour colour = BullColour.Black)
    {
        var scenes = SceneBuilder.Build(definition, new RenderOptions(TileIndex: tile, PrintNote: note ? SceneBuilder.ActualSizeNote : null, BullColour: colour));
        if (scenes.Pages.Count == 0)
        {
            return null;
        }

        return Picture(scenes.Pages[0]);
    }

    /// <summary>
    /// A page as a bitmap with its longer side near 900 pixels, words and all; in color where its bulls are (entry 297), grey otherwise, as
    /// it always was.
    /// </summary>
    internal static Bitmap Picture(Scene scene)
    {
        double longerInches = Math.Max(scene.Width, scene.Height) / (2.0 * 254);
        double dpi = Math.Min(100, 900 / longerInches);
        byte[] png;
        if (scene.Items.Any(i => i.Colour.R != i.Colour.G || i.Colour.G != i.Colour.B))
        {
            var (width, height, bgr) = SceneRasterizer.RasterizeBgr(scene, dpi, words: true);
            using var colour = OpenCvSharp.Mat.FromPixelData(height, width, OpenCvSharp.MatType.CV_8UC3, bgr);
            OpenCvSharp.Cv2.ImEncode(".png", colour, out png);
        }
        else
        {
            var image = SceneRasterizer.Rasterize(scene, dpi, words: true);
            using var mat = OpenCvSharp.Mat.FromPixelData(image.Height, image.Width, OpenCvSharp.MatType.CV_8UC1, image.Pixels);
            OpenCvSharp.Cv2.ImEncode(".png", mat, out png);
        }

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
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
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

        var (opening, opened, kind) = PrintLaunch(path);
        try
        {
            // Entry 122: one way out of the process, which a test replaces with a recorder rather than opening a PDF viewer on somebody's
            // machine.
            TheOutsideWorld.Current.OpenFile(opening);
        }
        catch (Win32Exception ex)
        {
            Fail("The PDF could not be opened to print", "No application could open the PDF (" + ex.Message + "). It is saved at " + path + "; print it from elsewhere at actual size.");
            DiagnosticLog.Exception(LogLevel.Warn, "print.open", ex, ("fallback", "the saved PDF"));
            return;
        }

        // Entry 105 section 9: a launch that worked leaves a trace too, which is what was missing when the print verb printed silently.
        DiagnosticLog.Info("print.open", [.. DiagnosticLog.File(path), ("verb", "none"), ("returned", true)]);
        SetStatus(opened, kind);
        Confirm(opened, checkPrinter: selected is { } printed ? PrinterOffer?.Invoke(printed.Definition) : null);
    }

    /// <summary>
    /// Entry 300 section 3: the sheet as the real PDF, opened in the system's viewer for anyone who wants it, with nothing else asked. It is
    /// written where the print path writes it and opened the one way out of the process.
    /// </summary>
    internal void OpenAsPdf()
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
            TheOutsideWorld.Current.OpenFile(path);
            DiagnosticLog.Info("print.pdf", [.. DiagnosticLog.File(path), ("opened", true)]);
        }
        catch (Win32Exception ex)
        {
            Fail("The PDF could not be opened", "No application could open the PDF (" + ex.Message + "). It is saved at " + path + ".");
            DiagnosticLog.Exception(LogLevel.Warn, "print.pdf", ex);
        }
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
            Fail("The pack could not be written", "The pack could not be written: " + ex.Message);
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

        var (opening, opened, kind) = PrintLaunch(path);
        try
        {
            // Entry 122: one way out of the process, which a test replaces with a recorder rather than opening a PDF viewer on somebody's
            // machine.
            TheOutsideWorld.Current.OpenFile(opening);
        }
        catch (Win32Exception ex)
        {
            Fail("The pack could not be opened to print", "No application could open the pack (" + ex.Message + "). It is saved at " + path + "; print it from elsewhere at actual size.");
            DiagnosticLog.Exception(LogLevel.Warn, "print.open", ex, ("fallback", "the saved pack"));
            return;
        }

        DiagnosticLog.Info("print.open", [.. DiagnosticLog.File(path), ("verb", "none"), ("returned", true), ("kind", "pack")]);
        SetStatus(opened, kind);
        Confirm(opened, checkPrinter: selected is { } printed ? PrinterOffer?.Invoke(printed.Definition) : null);
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
        var outcome = WindowsPrinter.PrintWithDialog((TopLevel.GetTopLevel(this) as Window)?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero, result.Pages, sheet);
        DiagnosticLog.Info("print.send", ("sheet", selected.File), ("outcome", outcome.Kind.ToString()), ("pages", outcome.Pages));
        switch (outcome.Kind)
        {
            case PrintOutcomeKind.Cancelled:
                SetStatus(outcome.Message, StatusKind.Information);
                break;
            case PrintOutcomeKind.Sent:
                SetStatus(outcome.Message, StatusKind.Success);
                Confirm(outcome.Message, "Printed", PrinterOffer?.Invoke(selected.Definition));
                break;
            default:
                Fail("The sheet was not printed", outcome.Message);
                Confirm(outcome.Message, "Not printed");
                break;
        }
    }

    /// <summary>
    /// The confirmation entry 106 section 1 asks for: a status line was missed, so a dialog says what GroupLab did and what the person must do,
    /// and nothing more, since on this path GroupLab only opened a file.
    /// </summary>
    private void Confirm(string text, string heading = "Open to print", Action? checkPrinter = null)
    {
        var ok = new Button { Content = "OK", HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true, Classes = { AppStyles.Primary } };
        var body = new StackPanel { Margin = new Thickness(Tokens.Space12), Spacing = Tokens.Space12, Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } } };
        var dialog = new Window
        {
            Title = heading,
            Width = 440,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = body,
        };
        // Entry 273: the first time a sheet is printed, the printer check is offered, since this is the printer its photos will come from.
        if (checkPrinter is not null)
        {
            var check = new Button { Content = "Check this printer" };
            check.Click += (_, _) =>
            {
                dialog.Close();
                checkPrinter();
            };
            body.Children.Add(new TextBlock { Text = "Check this printer once, and every photo of a GroupLab sheet it printed measures in real inches.", TextWrapping = TextWrapping.Wrap });
            body.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8, HorizontalAlignment = HorizontalAlignment.Right, Children = { check, ok } });
        }
        else
        {
            body.Children.Add(ok);
        }

        ok.Click += (_, _) => dialog.Close();
        Confirmation = dialog;
        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _ = dialog.ShowDialog(owner);
        }
    }

    /// <summary>
    /// Entry 273: given the sheet just printed, the printer check to offer after it, or null where it is not due: it is offered once, the
    /// first time a sheet is printed with no printer checked, and never after printing the check page itself.
    /// </summary>
    internal Func<TargetDefinition, Action?>? PrinterOffer { get; set; }

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
    /// <remarks>
    /// Entry 122: it names the file and says nothing about how it is opened, because opening is the one way out of the process and belongs
    /// to <see cref="IOutsideWorld"/>. No verb is possible from here, which is the property the paragraph above is about.
    /// </remarks>
    internal static (string Path, string Status, StatusKind Kind) PrintLaunch(string path) =>
        (path, OpenedText, StatusKind.Information);

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
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = Tokens.Space8 };
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
