using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Trace;
using GroupLab.Core.Updates;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Records;
using GroupLab.Core.Registration;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The marking screen, docs/PHASE1-BRIEF.md section 6 and NOTES-FROM-PLANNING.md entry 21 section 3, built in code. Left to right:
/// the image with its marks, and a panel with the review queue, the selected shot and the scale. Along the top, the header's actions and
/// the tool strip, each tool an icon named with its key (NOTES-FROM-PLANNING.md entry 109); keyboard shortcuts duplicate them for speed
/// (DESIGN.md section 13) and are never the only way to do something.
/// <list type="number">
/// <item>Open an image and show it.</item>
/// <item>For a GroupLab sheet, register it and detect its holes; missing markers are drawn, and a failure is a prominent message.</item>
/// <item>The bulls and the holes are drawn over the image.</item>
/// <item>Correct by hand: move a shot, tap a shot then a bull to reassign it, mark a detection as not a shot, exclude a shot with
/// a reason, delete, and undo any of it.</item>
/// <item>The statistics: mean radius with its interval as the headline, sigma beneath, extreme spread subordinate, every figure with
/// and without exclusions.</item>
/// <item>Export the marking and its report as JSON.</item>
/// </list>
/// </summary>
public sealed partial class MainWindow : Window
{
    private static readonly FontFamily Mono = Tokens.Mono;

    private readonly MarkingSession session = new();
    private readonly MarkingCanvas canvas = new();

    /// <summary>
    /// The analysis showing its work, DESIGN.md section 19 [r3] and NOTES-FROM-PLANNING.md entry 97 section 3: every stage's record on a
    /// timeline under the image, filed as each stage lands, scrubbed with the slider or the stage buttons, with the chosen stage's parameters,
    /// decisions and rejections beside it, and a rejection clicked to find it on the image.
    /// <para>
    /// The design's two constraints hold. <b>The trace is never the only place an error appears</b>: a failed analysis still says so in the
    /// panel and the status line, and its failed stage is the detail behind that. <b>The theatre does not slow the pipeline</b>: the records
    /// are the ones every stage files anyway, nothing extra is computed for them, and a batch run has nobody listening.
    /// </para>
    /// </summary>
    private readonly List<StageRecord> stages = [];

    private readonly Slider stageSlider = new() { Minimum = 0, Maximum = 0, IsSnapToTickEnabled = true, TickFrequency = 1, MinWidth = 240 };

    private readonly WrapPanel stageButtons = new();

    private readonly TextBlock stageSummary = new() { TextWrapping = TextWrapping.Wrap, FontSize = Tokens.SecondarySize };

    private readonly StackPanel stageDetail = new() { Spacing = 2 };

    private bool showingStage;

    private readonly Dictionary<MarkingTool, ToggleButton> toolButtons = [];
    private readonly TextBlock status = new() { Margin = new Thickness(Tokens.Space16, Tokens.Space4), TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, Classes = { AppStyles.Secondary } };
    // NOTES-FROM-PLANNING.md entry 76 section 4: a detection the window starts on its own shows that it is running and can be stopped.
    private readonly ProgressBar detectionProgress = new() { IsIndeterminate = true, Width = 120, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
    private readonly Button cancelDetection = new() { Content = "Cancel detection", Margin = new Thickness(Tokens.Space8, 2), IsVisible = false };
    private CancellationTokenSource? detection;

    // The marking exactly as the last detection left it, so setting a calibre can tell untouched marks, which it may detect again, from
    // corrections it must not throw away (NOTES-FROM-PLANNING.md entry 78 section 4).
    private MarkingState? detectedState;

    // DESIGN.md section 13 and NOTES-FROM-PLANNING.md entry 83 section 4: the assignment editor's review queue, what needs a decision and how
    // to give it, at the top of the panel, driven from the keyboard.
    private readonly StackPanel review = new() { Spacing = Tokens.Space8 };
    private string? currentReview;
    private string bullTyped = "";

    private readonly TextBlock problem = new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };
    private readonly StackPanel statistics = new() { Spacing = 4 };

    /// <summary>The breadcrumb header's text: what is open and what is on it, NOTES-FROM-PLANNING.md entry 93 section 2.</summary>
    private readonly TextBlock breadcrumb = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };

    /// <summary>The header's review count, "2 of 26 need review", as the concept puts it beside the actions: amber while anything is open.</summary>
    private readonly TextBlock reviewCount = new() { Classes = { AppStyles.PillText } };

    private readonly Border reviewPill = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space8, 0), Classes = { AppStyles.Pill } };

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 103 section 1: one destination in two states, as the two concept screens behind the rail's first icon show
    /// it. The editor marks and reviews; the analysis state shows the composite plot, the figures and the judgements. Accept and analyse goes
    /// forward, and the breadcrumb's sheet crumb comes back with every edit intact.
    /// </summary>
    private bool analysing;

    private readonly DockPanel editorBody = new();

    private readonly DockPanel analysisBody = new() { IsVisible = false };

    private readonly StackPanel editorActions = new() { Orientation = Orientation.Horizontal };

    private readonly StackPanel analysisActions = new() { Orientation = Orientation.Horizontal, IsVisible = false };

    private readonly Button acceptButton = new() { Content = "Accept and analyse", Margin = new Thickness(2), Classes = { AppStyles.Primary } };

    private readonly Button discardButton = new() { Content = "Discard edits", Margin = new Thickness(2) };

    /// <summary>Discard edits asks first: this row stands in for the button until the person confirms or keeps the edits.</summary>
    private readonly StackPanel discardConfirm = new() { Orientation = Orientation.Horizontal, IsVisible = false, VerticalAlignment = VerticalAlignment.Center };

    /// <summary>The analysis state's pill: the registration and its residual, where the editor has the review count.</summary>
    private readonly TextBlock registrationText = new() { Classes = { AppStyles.PillText } };

    private readonly Border registrationPill = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space8, 0), Classes = { AppStyles.Pill } };

    /// <summary>The analysis state's breadcrumb: the sheet crumb is a button back to the editor, one click from a figure to the marks behind it.</summary>
    private readonly StackPanel analysisCrumbs = new() { Orientation = Orientation.Horizontal, IsVisible = false, VerticalAlignment = VerticalAlignment.Center };

    private readonly Button sheetCrumb = new() { Margin = new Thickness(Tokens.Space4, 0), VerticalAlignment = VerticalAlignment.Center };

    /// <summary>
    /// Entry 103 section 1's rule the concept does not show: a person may accept with items still open, and then every figure inherits the
    /// decisions not made, so the analysis says how many, in amber, above the figures.
    /// </summary>
    private readonly TextBlock unsettled = new() { TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } };

    /// <summary>
    /// Entry 109 section 3: the decisions left unmade as a compact banner, amber and at the top, "15 decisions left unmade. Review them", the
    /// second half a link back to the editor, and the sentence that explains it behind its "why".
    /// </summary>
    private readonly StackPanel unsettledBanner = new() { Spacing = 0, IsVisible = false };

    private readonly WrapPanel bannerLine = new() { Orientation = Orientation.Horizontal, ItemSpacing = Tokens.Space4 };

    /// <summary>
    /// The settings screen, NOTES-FROM-PLANNING.md entry 109 section 2: units, theme, the log and crash records, reached by the gear at the foot of
    /// the rail. They are not part of the task, so they no longer sit in the marking panel between the review queue and the scale.
    /// </summary>
    private readonly Control settingsBody;

    /// <summary>The rail's destination showing, NOTES-FROM-PLANNING.md entry 112: the analysis screen, the session records, the library or the settings.</summary>
    private Destination destination = Destination.Analyse;

    private bool showingSettings => destination == Destination.Settings;

    /// <summary>
    /// The sessions and the records, NOTES-FROM-PLANNING.md entry 112 section 1 and DESIGN.md section 15: one SQLite database beside the
    /// settings. Null only when it could not be opened, which the panel says; the window still works without it.
    /// </summary>
    private readonly SessionStore? sessions;

    /// <summary>The session this marking was saved as, so a second Accept and analyse updates it rather than adding another.</summary>
    private long? currentSession;

    /// <summary>The Session records screen, the rail's destination, entry 112 section 1.</summary>
    private readonly Control sessionsBody;

    private readonly StackPanel sessionRows = new() { Spacing = 0 };

    private readonly ComboBox sessionRifle = new() { MinWidth = 180 };

    private readonly ComboBox sessionLoad = new() { MinWidth = 180 };

    private bool fillingSessions;

    private Button railSessions = null!;
    private Button railLibrary = null!;
    private Button railCompare = null!;
    private readonly Control libraryBody;
    private GroupLab.Core.Rendering.OwnSheets ownSheets = null!;

    private readonly TextBlock settingsCrumb = new() { Text = "\u203a  Settings", VerticalAlignment = VerticalAlignment.Center, IsVisible = false };

    private readonly StackPanel settingsCrashes = new() { Spacing = Tokens.Space8 };

    /// <summary>What the canvas says before any image is open: what to do first, since opening is in the header's menu.</summary>
    private readonly StackPanel emptyCanvas = new() { Spacing = Tokens.Space12, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

    /// <summary>The rail's first and last buttons, one lit for the screen showing.</summary>
    private Button railHere = null!;

    private Button railSettings = null!;

    /// <summary>The composite plot's toggle for the calibre outlines, entry 109 section 3: on by default, shown only when a calibre is set.</summary>
    private readonly CheckBox outlinesBox = new() { Content = "Calibre outlines", IsChecked = true };

    private readonly Border outlinesToggle = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Classes = { AppStyles.ViewCluster } };

    /// <summary>Every "why" open or closed at once, for the renders of entry 109 section 4; null in use, when each keeps its own remembered state.</summary>
    private bool? whyOverride;

    /// <summary>Each "why" as it was last left, read from the settings once and kept, since the panel is rebuilt on every change.</summary>
    private readonly Dictionary<string, bool> whyOpen = [];

    /// <summary>The two judgement cards, entry 103 section 2: whether the group is round, and whether its worst shot is a flyer.</summary>
    private readonly StackPanel judgements = new() { Spacing = Tokens.Space8 };

    /// <summary>
    /// The detector's size flags and the reference figures, below the cards. Each flag is also a review item, which the amber line above the
    /// figures counts, so here they are the detail rather than the first thing read.
    /// </summary>
    private readonly StackPanel flags = new() { Spacing = 4 };

    /// <summary>The last detection's registration residual in inches, for the analysis pill; null for a marking scaled by hand or reopened.</summary>
    private double? registrationResidual;

    private readonly CompositePlot plot = new();

    /// <summary>The shots the plot has picked: one clicked, or the two an extreme spread line joins.</summary>
    private HashSet<int> plotSelection = [];

    /// <summary>The definition of the sheet last detected on this image, whose bull the composite plot draws. Null for a marking done by hand.</summary>
    private GroupLab.Core.Gltd.Model.TargetDefinition? plotDefinition;

    /// <summary>The analysis state's shot table: each scoring shot's offset from its own bull, a row that selects it.</summary>
    private readonly StackPanel offsetTable = new() { Spacing = 0 };

    private readonly StackPanel loadLines = new() { Spacing = 2 };

    private readonly Expander timelineExpander = new() { HorizontalAlignment = HorizontalAlignment.Stretch, Padding = new Thickness(0) };

    /// <summary>
    /// The work bar, the stage timeline, NOTES-FROM-PLANNING.md entry 105 section 6: shown and hidden by Show work in either state's header,
    /// and remembered. Hidden, a failed stage is still a prominent error (DESIGN.md section 19) and Show work says so.
    /// </summary>
    private readonly Border workBar = new() { Classes = { AppStyles.StatusBar } };

    private readonly ToggleButton showWorkEditor = new() { Margin = new Thickness(2) };

    private readonly ToggleButton showWorkAnalysis = new() { Margin = new Thickness(2) };

    private bool workShown;

    /// <summary>Whether sighters are analysed, entry 105 section 8: off unless chosen, remembered.</summary>
    private bool analyseSighters;

    private readonly CheckBox analyseSightersBox = new() { Content = "Analyse sighters" };

    /// <summary>The sighters' own group, when they are analysed: their zero readout and what their count allows.</summary>
    private readonly StackPanel sighterPanel = new() { Spacing = 2 };

    /// <summary>Entry 105 section 1: the side columns' limits, so neither can be dragged shut, and the centre keeps room for the sheet or plot.</summary>
    private const double SideMinimum = 260, SideMaximum = 760, CentreMinimum = 320;

    /// <summary>
    /// The zero correction, NOTES-FROM-PLANNING.md entry 92 section 1. It sits above the group statistics and not among them because it
    /// answers a different question at a different moment: what to dial now, read standing at a bench with a turret cap in one hand, where
    /// the statistics say how well the rifle shoots, read afterwards sitting down. It also has a different truth condition, being a claim
    /// about what the rifle will do next rather than a description of the shots on the sheet.
    /// </summary>
    private readonly StackPanel zeroPanel = new() { Spacing = 2 };

    // DESIGN.md section 19 and NOTES-FROM-PLANNING.md entry 73 section 7: the figures that change decisions stay in view with their
    // intervals, and the reference figures sit one click away in a panel that remembers whether it was opened.
    private readonly StackPanel moreFigures = new() { Spacing = 4 };
    private readonly Expander moreFiguresPanel = new() { Header = "More figures", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel selection = new() { Spacing = 6 };
    private readonly StackPanel shotList = new() { Spacing = 2 };
    private readonly StackPanel crashBanner = new() { Spacing = Tokens.Space8, IsVisible = false };
    private readonly StackPanel scaleInputs = new() { Spacing = 6 };
    // Entry 111 section 3: the reasons in words, "Called flyer", not the enum's names; the list is in the enum's order, so its index is the reason.
    private readonly ComboBox exclusionReason = new() { ItemsSource = Enum.GetValues<ExclusionReason>().Select(r => r.Words()).ToList(), SelectedIndex = 0, MinWidth = 140 };

    /// <summary>The reason chosen in the list.</summary>
    private ExclusionReason ChosenReason => Enum.GetValues<ExclusionReason>()[Math.Max(0, exclusionReason.SelectedIndex)];
    private GrayImage? grey;
    private GrayImage? valueImage;
    private ImageMetadata? metadata;

    /// <summary>The metadata of the image detection last ran on, which the advice of entry 115 section 4 reads.</summary>
    private ImageMetadata? detectionMetadata;

    /// <summary>The image waiting for a person to say which sheet it is (entry 115 section 4).</summary>
    private (GrayImage Grey, GrayImage Value, ImageMetadata Metadata)? pendingDetection;

    private IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> pendingSheets = [];

    private readonly ComboBox sheetChoice = new() { HorizontalAlignment = HorizontalAlignment.Stretch };

    private readonly StackPanel sheetChooser = new() { Spacing = Tokens.Space4, IsVisible = false };

    /// <summary>What the print scale says, when a sheet did not print at its own size (entry 115 section 4).</summary>
    private readonly TextBlock printScale = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false, Classes = { AppStyles.FormWarning } };

    // The sheet's printed artwork in image pixels, from the last detection on this image, so the snap and the size check can tell printed
    // ink from a hole (NOTES-FROM-PLANNING.md entry 40 section 1). Null on any image not detected as a GroupLab sheet.
    private GrayImage? artwork;

    // A sheet size the person who shot it stated, from a provenance record beside the image (NOTES-FROM-PLANNING.md entry 37 section 5).
    private StatedSheetSize? statedSize;
    private readonly AutoCompleteBox calibreBox = new() { ItemsSource = Calibre.Common.Select(c => c.Name).ToList(), FilterMode = AutoCompleteFilterMode.Contains, MinWidth = 180, PlaceholderText = "optional, e.g. .308" };
    private readonly TextBlock calibreNote = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    private readonly AppSettingsStore settingsStore;
    private readonly ComboBox linearUnit = new() { ItemsSource = Enum.GetValues<LinearUnit>().Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 70 };
    private readonly ComboBox angularUnit = new() { ItemsSource = UnitSettings.AngularChoices.Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 90 };
    private readonly ComboBox distanceUnit = new() { ItemsSource = Enum.GetValues<DistanceUnit>().Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 70 };
    private readonly TextBox shotDistance = new() { Width = 90 };

    /// <summary>
    /// The rifle, barrel and load the sheet was shot with, from the person's record book (NOTES-FROM-PLANNING.md entry 97 section 2). The book
    /// is one small file beside the settings; the marking keeps its own copy of the rifle so its clicks stay right if the book changes.
    /// </summary>
    private readonly ComboBox rifleChoice = new() { MinWidth = 200 };

    private readonly ComboBox barrelChoice = new() { MinWidth = 200 };

    private readonly ComboBox loadChoice = new() { MinWidth = 200 };

    private readonly TextBox newName = new() { Width = 150, PlaceholderText = "name" };

    private readonly TextBox newDetail = new() { Width = 150, PlaceholderText = "rounds or components" };

    private readonly ComboBox newClick = new() { ItemsSource = new[] { "0.25 MOA", "0.125 MOA", "0.5 MOA", "0.1 mil", "0.05 mil" }, SelectedIndex = 0, MinWidth = 110 };

    private RecordBook book = RecordBook.Empty;

    private bool showingEquipment;

    /// <summary>How many rounds the person fired at the group, NOTES-FROM-PLANNING.md entry 95 section 2: the one fact the detector never has.</summary>
    private readonly TextBox roundsFired = new() { Width = 90 };
    private readonly TextBlock shotDistanceUnit = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox themeChoice = new() { ItemsSource = new[] { "Follow system", "Dark", "Light", "High contrast" }, MinWidth = 140 };
    private bool showingTheme;
    private UnitSettings units;
    private bool showingUnits;

    public MainWindow()
        : this(AppSettingsStore.Default)
    {
    }

    /// <summary>The window with its settings kept in <paramref name="settings"/>, which the headless tests point at a file of their own.</summary>
    internal MainWindow(AppSettingsStore settings)
    {
        settingsStore = settings;
        units = settings.LoadUnits();
        moreFiguresPanel.Content = moreFigures;
        moreFiguresPanel.IsExpanded = settings.LoadMoreFigures();
        moreFiguresPanel.PropertyChanged += (_, e) =>
        {
            if (e.Property == Expander.IsExpandedProperty)
            {
                settingsStore.SaveMoreFigures(moreFiguresPanel.IsExpanded);
            }
        };
        ApplyTheme(settings.LoadTheme());
        Title = "GroupLab";
        // Entry 105 section 5: the window's own icon, the mark alone, from the same file the executable carries.
        Icon = new WindowIcon(AssetLoader.Open(new Uri(IconUri)));
        Width = 1400;
        Height = 900;
        canvas.Session = session;
        session.Changed += (_, _) => Refresh();
        canvas.SelectionChanged += (_, _) => Refresh();
        canvas.LengthTapped += (_, _) => AskLength();
        canvas.RectangleTapped += (_, _) => AskRectangle();
        canvas.Notice += (_, note) => status.Text = note;
        Opened += (_, _) =>
        {
            CrashReporter.DisplayScale = RenderScaling;
            DiagnosticLog.Info("app.window", ("scale", RenderScaling), ("width", Width), ("height", Height));
        };
        CrashReporter.Recorded += OnCrashRecorded;
        Closed += (_, _) => CrashReporter.Recorded -= OnCrashRecorded;

        // Entry 109 section 2: the strip holds the tools as icons alone, each named with its key in a tooltip and the active one lit, then Undo
        // and Redo at its end, as the concept draws it, and on the right the keys the review answers to. The view controls float over the canvas,
        // opening, exporting and reporting are the header's menu, and printing is the rail's.
        var tools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var (tool, icon, name, key) in new[]
        {
            (MarkingTool.Pan, Icons.Pan, "Pan", "P"),
            (MarkingTool.Length, Icons.Length, "Scale: length", "L"),
            (MarkingTool.Rectangle, Icons.Rectangle, "Scale: rectangle", "R"),
            (MarkingTool.Aim, Icons.Aim, "Point of aim", "A"),
            (MarkingTool.Impact, Icons.Impact, "Impact", "I"),
            (MarkingTool.Select, Icons.Select, "Select", "V"),
        })
        {
            var button = new ToggleButton { Content = Icons.Draw(icon), Classes = { AppStyles.IconButton } };
            ToolTip.SetTip(button, $"{name} ({key})");
            Avalonia.Automation.AutomationProperties.SetName(button, name);
            button.Click += (_, _) => SetTool(tool);
            toolButtons[tool] = button;
            tools.Children.Add(button);
        }

        tools.Children.Add(new Border { Width = 1, Margin = new Thickness(Tokens.Space8, Tokens.Space4), Classes = { AppStyles.Divider } });
        tools.Children.Add(IconButton(Icons.Undo, "Undo (Ctrl+Z)", () => session.Undo()));
        tools.Children.Add(IconButton(Icons.Redo, "Redo (Ctrl+Y)", () => session.Redo()));
        var hints = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        foreach (var (key, what) in new[] { ("Space", "next item"), ("Enter", "first choice"), ("N", "not a shot"), ("Ctrl Z", "undo") })
        {
            hints.Children.Add(Keycap(key));
            hints.Children.Add(new TextBlock { Text = what, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space4, 0, Tokens.Space8, 0), Classes = { AppStyles.Secondary } });
        }

        var toolbar = new DockPanel { Margin = new Thickness(Tokens.Space8, Tokens.Space4) };
        DockPanel.SetDock(hints, Dock.Right);
        toolbar.Children.Add(hints);
        toolbar.Children.Add(tools);

        // Entry 109 section 2: the panel is the task. The review queue, the selected shot and the scale lead, as the concept has them, then what
        // the group was shot with and the shots; the units, the theme and the log are settings, on their own screen.
        var panel = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space12 };
        panel.Children.Add(crashBanner);
        panel.Children.Add(Heading("Review"));
        panel.Children.Add(review);
        AddHandler(KeyDownEvent, OnReviewKey, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        panel.Children.Add(Ruled("Selected shot"));
        panel.Children.Add(selection);
        panel.Children.Add(Ruled("Scale"));
        panel.Children.Add(scaleInputs);
        panel.Children.Add(Ruled("Group"));

        // Entry 24 section 5: the calibre is a property of the group, entered once, from the list or typed.
        panel.Children.Add(FieldLabel("Calibre"));
        panel.Children.Add(Row(calibreBox, Button("Set", SetCalibreFromBox), Button("Clear", () =>
        {
            calibreBox.Text = "";
            session.SetCalibre(null);
        })));
        panel.Children.Add(calibreNote);
        panel.Children.Add(FieldLabel("Shot distance"));
        panel.Children.Add(Row(shotDistance, shotDistanceUnit, Button("Set", SetShotDistanceFromBox), Button("Clear", () =>
        {
            shotDistance.Text = "";
            session.SetShotDistance(null);
        })));
        // Entry 97 section 2: which rifle, barrel and load, from a record book kept deliberately small.
        // Entry 112 section 1: the records live in the database now; the old file is read in on the first open and kept as a backup.
        try
        {
            ownSheets = new GroupLab.Core.Rendering.OwnSheets(settings.SheetsFolder);
            sessions = SessionStore.Open(settings.DatabasePath, RecordsPath);
            book = sessions.LoadBook();
        }
        catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or InvalidDataException or IOException or UnauthorizedAccessException)
        {
            sessions = null;
            problem.Text = "The session database could not be opened (" + ex.Message + "), so sessions and records are not kept this time.";
            DiagnosticLog.Exception(LogLevel.Warn, "store.open", ex);
        }
        panel.Children.Add(FieldLabel("Rifle, barrel and load"));
        panel.Children.Add(Row(rifleChoice));
        panel.Children.Add(Row(barrelChoice, Button("Add this sheet's shots", AddSheetToBarrel)));
        panel.Children.Add(Row(loadChoice));
        foreach (var combo in new[] { rifleChoice, barrelChoice, loadChoice })
        {
            combo.SelectionChanged += (_, _) => EquipmentChosen();
        }

        // Entry 105 section 1: the form was wider than the 372 pixel column and clipped its own button to "Add rif" on a first run. Each field
        // now has the column's width and its buttons sit beneath it, and every row wraps rather than running past the edge.
        var adding = new StackPanel { Spacing = Tokens.Space4 };
        newName.Width = newDetail.Width = double.NaN;
        newName.HorizontalAlignment = newDetail.HorizontalAlignment = HorizontalAlignment.Stretch;
        adding.Children.Add(newName);
        adding.Children.Add(Row(newClick, Button("Add rifle", () => AddRecord("rifle"))));
        adding.Children.Add(newDetail);
        adding.Children.Add(Row(Button("Add barrel", () => AddRecord("barrel")), Button("Add load", () => AddRecord("load"))));
        adding.Children.Add(Line("A rifle needs a name and its scope's click. A barrel's detail is its round count so far, a load's is its components."));
        panel.Children.Add(new Expander { Header = "New rifle, barrel or load", Content = adding, HorizontalAlignment = HorizontalAlignment.Stretch });
        // Entry 105 section 8: sighters are found and matched and then set aside, unless a person asks for them to be analysed.
        analyseSighters = settings.LoadAnalyseSighters();
        analyseSightersBox.IsChecked = analyseSighters;
        analyseSightersBox.IsCheckedChanged += (_, _) =>
        {
            analyseSighters = analyseSightersBox.IsChecked == true;
            settingsStore.SaveAnalyseSighters(analyseSighters);
            Refresh();
        };
        panel.Children.Add(analyseSightersBox);
        panel.Children.Add(FieldLabel("Rounds fired at the group, sighters not counted"));
        panel.Children.Add(Row(roundsFired, Button("Set", SetRoundsFiredFromBox), Button("Clear", () =>
        {
            roundsFired.Text = "";
            session.SetExpectedShots(null);
        })));
        BuildShotsPerBull(panel);
        BuildBullLoads(panel);
        sheetChooser.Children.Add(FieldLabel("Which sheet is this?"));
        sheetChooser.Children.Add(sheetChoice);
        sheetChooser.Children.Add(Row(Button("Detect with this sheet", async () => await DetectAsTheChosenSheet()), Button("Mark it by hand", () =>
        {
            sheetChooser.IsVisible = false;
            pendingDetection = null;
            status.Text = "Mark it by hand: set a scale with a length or a rectangle, then place the shots.";
        })));
        panel.Children.Add(sheetChooser);
        panel.Children.Add(printScale);
        panel.Children.Add(problem);
        panel.Children.Add(Ruled("Shots"));
        panel.Children.Add(shotList);

        // Entry 42 section 4: the bar across the top, a right column 372 wide, and a status line, each separated by one pixel of line.
        // Entry 93 section 2 adds the concept's chrome: the breadcrumb header above the tool strip, and the icon rail down the left.
        // Entry 109 section 2: the editor's header is the review pill, Detect, Show work, Discard edits, Accept and analyse and one menu, on
        // one line. Opening an image or a marking, exporting and reporting a problem are in the menu, because they are not the task's next step.
        var crumbs = new DockPanel();
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        reviewPill.Child = reviewCount;
        editorActions.Children.Add(reviewPill);
        editorActions.Children.Add(Button("Detect on a GroupLab sheet", async () => await Detect(automatic: false)));
        editorActions.Children.Add(showWorkEditor);
        discardButton.Click += (_, _) => AskDiscard();
        editorActions.Children.Add(discardButton);
        discardConfirm.Children.Add(new TextBlock { Text = "Discard every edit since detection?", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(Tokens.Space4, 0), Classes = { AppStyles.Warn } });
        discardConfirm.Children.Add(Button("Discard", ConfirmDiscard));
        discardConfirm.Children.Add(Button("Keep them", () =>
        {
            discardConfirm.IsVisible = false;
            Refresh();
        }));
        editorActions.Children.Add(discardConfirm);
        acceptButton.Click += (_, _) => Analyse();
        editorActions.Children.Add(acceptButton);
        editorActions.Children.Add(Overflow());
        registrationPill.Child = registrationText;
        analysisActions.Children.Add(registrationPill);
        analysisActions.Children.Add(showWorkAnalysis);
        foreach (var toggle in new[] { showWorkEditor, showWorkAnalysis })
        {
            toggle.Click += (_, _) => SetShowWork(!workShown);
        }
        analysisActions.Children.Add(Button("Report", async () => await ReportDialog()));
        analysisActions.Children.Add(Button("Export", async () => await ExportDialog()));
        analysisActions.Children.Add(Overflow());
        actions.Children.Add(editorActions);
        actions.Children.Add(analysisActions);
        DockPanel.SetDock(actions, Dock.Right);
        crumbs.Children.Add(actions);
        sheetCrumb.Click += (_, _) => BackToEditor();
        analysisCrumbs.Children.Add(new TextBlock { Text = "\u203a", VerticalAlignment = VerticalAlignment.Center });
        analysisCrumbs.Children.Add(sheetCrumb);
        analysisCrumbs.Children.Add(new TextBlock { Text = "\u203a  analysis", VerticalAlignment = VerticalAlignment.Center });
        var crumbTexts = new Panel();
        crumbTexts.Children.Add(breadcrumb);
        crumbTexts.Children.Add(analysisCrumbs);
        crumbTexts.Children.Add(settingsCrumb);

        // Entry 105 section 4: the lockup, mark and wordmark, at about 26 pixels, where the concept has "GL" and a plain title; the crumbs
        // continue after it.
        var lockup = new BrandMark { Lockup = true, Height = 26, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space12, 0) };
        DockPanel.SetDock(lockup, Dock.Left);
        crumbs.Children.Add(lockup);
        crumbs.Children.Add(crumbTexts);
        var header = new Border { Child = crumbs, Classes = { AppStyles.Breadcrumb } };
        var bar = new Border { Child = toolbar, Classes = { AppStyles.Bar } };
        var side = new Border { Child = new ScrollViewer { Content = panel }, Classes = { AppStyles.Side } };
        cancelDetection.Click += (_, _) => CancelDetection();
        var statusLine = new DockPanel();
        var running = new StackPanel { Orientation = Orientation.Horizontal, Children = { detectionProgress, cancelDetection } };
        DockPanel.SetDock(running, Dock.Right);
        statusLine.Children.Add(running);
        statusLine.Children.Add(status);
        status.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBlock.TextProperty)
            {
                ToolTip.SetTip(status, string.IsNullOrEmpty(status.Text) ? null : status.Text);
            }
        };
        var statusBar = new Border { Child = statusLine, Classes = { AppStyles.StatusBar } };
        // One row while closed, so the image keeps its height: the slider and the stage it is on. The stages and their work open beneath it.
        var timelineBody = new StackPanel { Spacing = 0, Margin = new Thickness(Tokens.Space12, 0) };
        var opened = new StackPanel { Spacing = Tokens.Space4 };
        opened.Children.Add(stageButtons);
        opened.Children.Add(new ScrollViewer { Content = stageDetail, MaxHeight = 220 });
        var scrub = new DockPanel();
        var label = new TextBlock { Text = "How it was analysed", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, Tokens.Space12, 0), Classes = { AppStyles.Section } };
        DockPanel.SetDock(label, Dock.Left);
        DockPanel.SetDock(stageSlider, Dock.Left);
        scrub.Children.Add(label);
        scrub.Children.Add(stageSlider);
        stageSummary.VerticalAlignment = VerticalAlignment.Center;
        stageSummary.Margin = new Thickness(Tokens.Space12, 0, 0, 0);
        stageSummary.TextWrapping = TextWrapping.NoWrap;
        stageSummary.TextTrimming = TextTrimming.CharacterEllipsis;
        scrub.Children.Add(stageSummary);
        timelineExpander.Header = scrub;
        timelineExpander.Content = opened;
        timelineBody.Children.Add(timelineExpander);
        workBar.Child = timelineBody;
        stageSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty && !showingStage)
            {
                ShowStage((int)Math.Round(stageSlider.Value));
            }
        };

        // Entry 109 section 2: the view controls float over the canvas's bottom right corner, the usual place, since they act on the view and
        // not the document. Before an image is open the canvas says what to do first.
        var view = new StackPanel { Orientation = Orientation.Horizontal };
        view.Children.Add(IconButton(Icons.ZoomIn, "Zoom in", () => canvas.ZoomBy(1.25)));
        view.Children.Add(IconButton(Icons.ZoomOut, "Zoom out", () => canvas.ZoomBy(0.8)));
        view.Children.Add(IconButton(Icons.Fit, "Fit the image to the view", canvas.FitToView));
        view.Children.Add(IconButton(Icons.RotateLeft, "Rotate left ([)", () => session.Rotate(-1)));
        view.Children.Add(IconButton(Icons.RotateRight, "Rotate right (])", () => session.Rotate(1)));
        var openFirst = Button("Open image\u2026", async () => await OpenImageDialog());
        openFirst.Classes.Add(AppStyles.Primary);
        openFirst.HorizontalAlignment = HorizontalAlignment.Center;
        emptyCanvas.Children.Add(new TextBlock { Text = "Open a photograph or scan of a target", HorizontalAlignment = HorizontalAlignment.Center, Classes = { AppStyles.Title } });
        emptyCanvas.Children.Add(new TextBlock { Text = "A GroupLab sheet is read and its holes found on its own; any other target is marked by hand.", HorizontalAlignment = HorizontalAlignment.Center, Classes = { AppStyles.Label } });
        emptyCanvas.Children.Add(openFirst);
        var canvasArea = new Panel();
        canvasArea.Children.Add(canvas);
        canvasArea.Children.Add(emptyCanvas);
        canvasArea.Children.Add(new Border { Child = view, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Classes = { AppStyles.ViewCluster } });

        // The editor state: the tool strip above the sheet and the review column, with a splitter between them (entry 105 section 1).
        DockPanel.SetDock(bar, Dock.Top);
        editorBody.Children.Add(bar);
        editorBody.Children.Add(Columns("editor", null, canvasArea, side));

        // The analysis state, as analysis-dark.png lays it out: the shots and what they were fired with on the left, the composite plot in the
        // centre, and the figure stack with the judgement cards on the right.
        var figures = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space12 };
        // Entry 92 put the zero correction above the group statistics, a different question read at a different moment, and entry 104
        // section 3 found the split had moved it below the cards and the flags, off the bottom of the column.
        // Entry 105 section 2: the headings carry the column's hierarchy, so each has a rule above it.
        bannerLine.Children.Add(unsettled);
        bannerLine.Children.Add(Link("Review them", BackToEditor));
        figures.Children.Add(unsettledBanner);
        figures.Children.Add(Ruled("Zero correction"));
        figures.Children.Add(zeroPanel);
        figures.Children.Add(Ruled("Group"));
        figures.Children.Add(statistics);
        figures.Children.Add(judgements);
        figures.Children.Add(flags);
        figures.Children.Add(sighterPanel);
        var figureColumn = new Border { Child = new ScrollViewer { Content = figures }, Classes = { AppStyles.Side } };
        var shotsColumn = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space8 };
        shotsColumn.Children.Add(Heading("Load"));
        shotsColumn.Children.Add(loadLines);
        shotsColumn.Children.Add(Ruled("Shots, from their own bull"));
        shotsColumn.Children.Add(offsetTable);
        var leftColumn = new Border { Child = new ScrollViewer { Content = shotsColumn }, Classes = { AppStyles.Side } };
        BuildFigureExtras(shotsColumn, figures);
        outlinesToggle.Child = outlinesBox;
        outlinesBox.IsCheckedChanged += (_, _) =>
        {
            plot.ShowOutlines = outlinesBox.IsChecked == true;
            plot.InvalidateVisual();
        };
        var plotArea = new Panel();
        plotArea.Children.Add(plot);
        plotArea.Children.Add(outlinesToggle);
        analysisBody.Children.Add(Columns("analysis", leftColumn, plotArea, figureColumn));
        plot.ShotsClicked += (_, ids) => PickShots(ids);

        settingsBody = BuildSettings(settings);
        sessionsBody = BuildSessions();
        libraryBody = BuildLibrary();
        ballisticsBody = BuildBallistics();
        compareBody = BuildCompare();
        var body = new Panel();
        body.Children.Add(editorBody);
        body.Children.Add(analysisBody);
        body.Children.Add(settingsBody);
        body.Children.Add(sessionsBody);
        body.Children.Add(libraryBody);
        body.Children.Add(ballisticsBody);
        body.Children.Add(compareBody);
        // The work bar sits above the status line in both states, shown by Show work (entry 105 section 6).
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        // Entry 123 section 2: the update bar sits under the header, above the work, so it is seen without covering anything.
        var updateLine = BuildUpdateBar();
        DockPanel.SetDock(updateLine, Dock.Top);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        DockPanel.SetDock(workBar, Dock.Bottom);
        dock.Children.Add(header);
        dock.Children.Add(updateLine);
        dock.Children.Add(statusBar);
        dock.Children.Add(workBar);
        dock.Children.Add(body);
        SetShowWork(settings.LoadShowWork(), remember: false);

        var whole = new DockPanel();
        var rail = Rail();
        DockPanel.SetDock(rail, Dock.Left);
        whole.Children.Add(rail);
        whole.Children.Add(dock);
        Content = whole;
        SetTool(MarkingTool.Pan);
        ShowUnits();
        Refresh();
        ShowPendingCrashes();

        // Entry 123 section 2.4: if the last thing this machine did was update, the new version says so, once, and goes back to the screen
        // the person was on. It is read and cleared here, before anything can look for another update.
        updates = settingsStore.LoadUpdatePreferences(OwnTrain);
        SayIfUpdated();

        // Entry 119 sections 4.1 and 4.2: it looks as often as the person asked, on every launch unless they said otherwise, and a check
        // that finds nothing says nothing. It is not awaited, so a slow or unreachable train never holds the window closed.
        if (CheckOnLaunchByDefault && UpdatePolicy.ShouldCheck(updates, DateTimeOffset.UtcNow, launching: true))
        {
            _ = CheckForUpdatesAsync(byHand: false);
        }
    }

    /// <summary>The icon the window and the executable carry, entry 105 section 5.</summary>
    internal const string IconUri = "avares://GroupLab.App/Assets/icons/grouplab.ico";

    /// <summary>The crash banner's text when it is showing, and empty otherwise, for the headless tests.</summary>
    internal string CrashBannerText => crashBanner.IsVisible ? string.Join(" ", crashBanner.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text)) : "";

    /// <summary>The crash banner, for the headless tests.</summary>
    internal StackPanel CrashBanner => crashBanner;

    /// <summary>
    /// The next-launch offer, NOTES-FROM-PLANNING.md entry 41 section 5: a crashing application often cannot draw, so the reliable moment to
    /// say that something went wrong is the next time GroupLab opens. Every crash record not yet dealt with is offered here until the user
    /// deals with it.
    /// </summary>
    private void ShowPendingCrashes()
    {
        crashBanner.Children.Clear();
        settingsCrashes.Children.Clear();
        var pending = CrashReporter.PendingCrashes(DiagnosticLog.Current.Directory);
        crashBanner.IsVisible = pending.Count > 0;
        if (pending.Count == 0)
        {
            settingsCrashes.Children.Add(Line("No crash records are waiting."));
            return;
        }

        settingsCrashes.Children.Add(Line(pending.Count == 1
            ? "One crash record has not been dealt with. The marking panel offers it too."
            : string.Create(CultureInfo.InvariantCulture, $"{pending.Count} crash records have not been dealt with. The marking panel offers them too.")));
        settingsCrashes.Children.Add(Row(Button("Make a report\u2026", () => OpenReport(pending[^1])), Button("Show the record", () => CrashReporter.Reveal(pending[^1]))));

        DiagnosticLog.Info("crash.offered", ("pending", pending.Count));
        crashBanner.Children.Add(new TextBlock
        {
            Text = pending.Count == 1
                ? "GroupLab closed unexpectedly last time, and recorded what went wrong."
                : string.Create(CultureInfo.InvariantCulture, $"GroupLab closed unexpectedly {pending.Count} times, and recorded what went wrong."),
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeight.SemiBold,
            Classes = { AppStyles.Alert },
        });
        crashBanner.Children.Add(Row(
            Button("Make a report…", () => OpenReport(pending[^1])),
            Button("Show the record", () => CrashReporter.Reveal(pending[^1])),
            Button("Dismiss", () =>
            {
                foreach (string crash in pending)
                {
                    CrashReporter.MarkHandled(crash);
                }

                ShowPendingCrashes();
            })));
    }

    /// <summary>
    /// Opens the report dialog, NOTES-FROM-PLANNING.md entry 41 section 6: about a crash from the banner, with the log of the run that crashed and
    /// the one before, or about whatever the user wants to report from the toolbar, with this run's log and the one before.
    /// </summary>
    private void OpenReport(string? crash)
    {
        var (runLog, previousLog) = ReportPackage.LogsFor(DiagnosticLog.Current, crash);
        DiagnosticLog.Info("report.open", ("crash", crash is not null));
        var report = new ReportWindow(crash, runLog, previousLog, settingsStore.LoadCrashReportUrl());
        report.Closed += (_, _) => ShowPendingCrashes();
        report.Show(this);
    }

    /// <summary>The in-the-moment notice of entry 41 section 5, best effort: the window may not be able to draw, and the record is already written.</summary>
    private void OnCrashRecorded(object? sender, string crash) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        problem.Text = "Something went wrong, and GroupLab recorded what. The window carried on, but check your last change. The next time GroupLab opens it offers the record.");

    /// <summary>The unit setting in use, for the headless tests.</summary>
    internal UnitSettings Units => units;

    /// <summary>The scale inputs, for the headless tests.</summary>
    internal StackPanel ScaleInputs => scaleInputs;

    /// <summary>Chooses the units every figure is shown in, and remembers the choice. Nothing stored changes (entry 25 section 1).</summary>
    internal void SetUnits(UnitSettings chosen)
    {
        units = chosen;
        if (!settingsStore.SaveUnits(chosen))
        {
            status.Text = "The unit choice could not be saved to " + settingsStore.Path + ", so it lasts until GroupLab closes.";
        }

        ShowUnits();
        Refresh();
    }

    private void ShowUnits()
    {
        showingUnits = true;
        linearUnit.SelectedIndex = (int)units.Linear;
        angularUnit.SelectedIndex = Math.Max(0, UnitSettings.AngularChoices.ToList().IndexOf(units.Angular));
        distanceUnit.SelectedIndex = (int)units.Distance;
        shotDistanceUnit.Text = UnitSettings.Symbol(units.Distance);
        showingUnits = false;
    }

    /// <summary>Chooses the theme, dark, light or following the system, and remembers the choice (NOTES-FROM-PLANNING.md entry 42 section 2).</summary>
    internal void SetTheme(ThemeChoice theme)
    {
        ApplyTheme(theme);
        if (!settingsStore.SaveTheme(theme))
        {
            status.Text = "The theme choice could not be saved to " + settingsStore.Path + ", so it lasts until GroupLab closes.";
        }
    }

    private void ApplyTheme(ThemeChoice theme)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                ThemeChoice.Dark => ThemeVariant.Dark,
                ThemeChoice.Light => ThemeVariant.Light,
                ThemeChoice.HighContrast => Tokens.HighContrastVariant,
                _ => ThemeVariant.Default,
            };
        }

        showingTheme = true;
        themeChoice.SelectedIndex = (int)theme;
        showingTheme = false;
    }

    private void UnitsChosen()
    {
        if (showingUnits || linearUnit.SelectedIndex < 0 || angularUnit.SelectedIndex < 0 || distanceUnit.SelectedIndex < 0)
        {
            return;
        }

        SetUnits(new UnitSettings((LinearUnit)linearUnit.SelectedIndex, UnitSettings.AngularChoices[angularUnit.SelectedIndex], (DistanceUnit)distanceUnit.SelectedIndex));
    }

    private void SetShotDistanceFromBox()
    {
        if (double.TryParse(shotDistance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d > 0)
        {
            session.SetShotDistance(UnitSettings.DistanceToInches(d, units.Distance));
        }
        else
        {
            problem.Text = "Enter the shot distance as a number of " + UnitSettings.Symbol(units.Distance) + ".";
        }
    }

    /// <summary>The centre's offset from the aim in the screen's units, with its angle when the shot distance is known.</summary>
    private string CentreLine(PointD centre)
    {
        double? distance = session.State.ShotDistanceInches;
        string across = centre.X >= 0 ? "right" : "left", down = centre.Y >= 0 ? "low" : "high";
        string text = $"Centre from aim: {units.Length(Math.Abs(centre.X))} {across}, {units.Length(Math.Abs(centre.Y))} {down}";
        return units.AngleText(Math.Abs(centre.X), distance) is { } x ? $"{text} ({x} {across}, {units.AngleText(Math.Abs(centre.Y), distance)} {down})" : text;
    }

    /// <summary>The centre from aim as a figure row: the offsets in the length unit as its value, and in the angular unit beneath when there is a distance.</summary>
    private Control CentreRow(PointD centre)
    {
        var (value, detail) = CentreTexts(centre);
        var row = new StackPanel { Spacing = 0 };
        row.Children.Add(Readout("Centre from aim", value));
        if (detail is not null)
        {
            row.Children.Add(Detail(detail));
        }

        return row;
    }

    /// <summary>The canvas, for the headless tests.</summary>
    internal MarkingCanvas Canvas => canvas;

    /// <summary>The status line, for the headless tests.</summary>
    internal string StatusText => status.Text ?? "";

    /// <summary>The list of shots, for the headless tests.</summary>
    internal StackPanel ShotList => shotList;

    /// <summary>The disclosure holding the reference figures, for the headless tests.</summary>
    internal Expander MoreFigures => moreFiguresPanel;

    /// <summary>
    /// A shot's name as the image and the list show it, NOTES-FROM-PLANNING.md entry 75: its bull's number, lettered when the bull holds more
    /// than one, or the word unassigned. On a plain group with no bulls there is no printed number, and the shot is named by where it is.
    /// </summary>
    private string ShotLabel(int id)
    {
        var state = session.State;
        return ShotLabels.For(state).FirstOrDefault(l => l.ShotId == id) is { Text: { } text } ? text : state.Find(id) is { } shot ? WhereOnTarget(state, shot.Image) : "";
    }

    /// <summary>Where a shot is, on the target in the display unit when there is a scale and in image pixels otherwise.</summary>
    private string WhereOnTarget(MarkingState state, PointD image) => state.Scale is { } scale && scale.ToTarget(image) is var at
        ? $"at {units.Number(at.X)}, {units.Length(at.Y)}"
        : string.Create(CultureInfo.InvariantCulture, $"at {image.X:0}, {image.Y:0} px");

    /// <summary>The session, for the headless tests.</summary>
    internal MarkingSession Session => session;

    /// <summary>
    /// Opens an image. It is decoded once through OpenCV without applying EXIF orientation, the same decode the pipeline measures, and
    /// shown from those same pixels, so a mark on the screen is a mark on the pixels the statistics and the detector use. The view then
    /// turns as the image's orientation tag asks (NOTES-FROM-PLANNING.md entry 24 section 4), which moves no pixel and no position;
    /// Rotate left and Rotate right turn it further on any image, tagged or not (entry 26).
    /// </summary>
    public void OpenImage(string path)
    {
        var (image, meta) = ImageLoader.Load(path);
        var (max, _) = ImageLoader.LoadMaxChannel(path);
        using var colour = OpenCvSharp.Cv2.ImRead(path, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
        OpenCvSharp.Cv2.ImEncode(".png", colour, out byte[] png);
        using var stream = new MemoryStream(png);
        grey = image;
        valueImage = max;
        metadata = meta;
        artwork = null;
        statedSize = StatedSheetSize.Beside(path);
        // Entry 78 section 4: the calibre is used in finding holes, so the one named stays named for the next sheet, where detection
        // on opening can use it. It stays in view in the calibre box.
        var calibre = session.State.Calibre;
        session.Open(path, meta.Orientation);
        if (calibre is not null)
        {
            session.Load(session.State with { Calibre = calibre });
        }

        detectedState = null;
        plotDefinition = null;
        registrationResidual = null;
        SetAnalysing(false);
        // Entry 41 section 2: the file's name, a salted hash of its path, and the whitelisted image facts, never its metadata block.
        DiagnosticLog.Info("image.open", [.. DiagnosticLog.File(path), .. ImageFacts.Of(meta)]);
        canvas.SetImage(new Bitmap(stream), max);
        int turns = session.State.ViewQuarterTurns;
        string orientation = ViewRotation.ExifMirrors(meta.Orientation)
            ? " Its orientation tag asks for a mirror image, which is not applied; rotate it if it needs turning."
            : turns != 0 ? string.Create(CultureInfo.InvariantCulture, $" Turned {90 * turns} degrees as its orientation tag asks.") : "";
        status.Text = string.Create(CultureInfo.InvariantCulture, $"{Path.GetFileName(path)}, {image.Width} by {image.Height} px{(meta.IsCamera ? $", {meta.CameraModel}" : "")}.{orientation} Set a scale, mark the point of aim, then tap each impact.");
        if (statedSize is { } stated)
        {
            status.Text += $" Its provenance record gives the sheet as {stated.Text}, which the rectangle tool offers as the reference.";
        }

        Refresh();
        if (DetectOnOpen)
        {
            DetectionTask = Detect(automatic: true);
        }
    }

    /// <summary>Whether opening an image runs detection on it, NOTES-FROM-PLANNING.md entry 76 section 4. The headless tests turn it off by default.</summary>
    internal static bool DetectOnOpenByDefault { get; set; } = true;

    internal bool DetectOnOpen { get; set; } = DetectOnOpenByDefault;

    /// <summary>The detection started last, for the headless tests to wait on.</summary>
    internal Task? DetectionTask { get; private set; }

    /// <summary>
    /// What the status says the moment Cancel is pressed, NOTES-FROM-PLANNING.md entry 77 section 5: cancelling takes effect between stages, so
    /// the text names the longest a stage runs, lest a Cancel that has worked look ignored. The longest measured is hole detection on Alan's
    /// 35 megapixel scan at 600 DPI, 5.6 s on the development machine.
    /// </summary>
    internal const string CancellingText = "Cancelling. The step in progress finishes first, which can take up to about 6 seconds on a 600 DPI scan.";

    /// <summary>Stops the detection in progress at its next stage; what it had not yet applied is dropped.</summary>
    internal void CancelDetection()
    {
        if (detection is null)
        {
            return;
        }

        detection.Cancel();
        cancelDetection.IsEnabled = false;
        status.Text = CancellingText;
    }

    /// <summary>
    /// Reopens a saved marking: its image, its marks, and the view turned the way it was left (NOTES-FROM-PLANNING.md entry 26 point 4).
    /// A file whose frame convention is unknown is refused with the reason, and a version 1 file is migrated with a note.
    /// </summary>
    public void OpenMarking(string path)
    {
        MarkingState state;
        IReadOnlyList<string> notes;
        try
        {
            (state, notes) = MarkingFile.Read(File.ReadAllText(path));
        }
        catch (MarkingFileException ex)
        {
            problem.Text = ex.Message;
            DiagnosticLog.Exception(LogLevel.Warn, "file.open", ex, [.. DiagnosticLog.File(path), ("kind", "marking")]);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A marking moved or deleted since it was chosen: say so rather than let the read tear the window down.
            problem.Text = "The marking could not be read: " + ex.Message;
            DiagnosticLog.Exception(LogLevel.Warn, "file.open", ex, [.. DiagnosticLog.File(path), ("kind", "marking")]);
            return;
        }

        if (state.ImagePath is not { } image || !File.Exists(image))
        {
            problem.Text = $"The marking's image, {state.ImagePath ?? "(none recorded)"}, is not there. Put it back at that path to reopen the marking.";
            DiagnosticLog.Warn("file.open", [.. DiagnosticLog.File(path), ("kind", "marking"), ("reason", "its image is missing")]);
            return;
        }

        DiagnosticLog.Info("file.open", [.. DiagnosticLog.File(path), ("kind", "marking"), ("shots", state.Shots.Count)]);

        OpenImage(image);
        if (metadata?.Orientation != state.ExifOrientation)
        {
            notes = [.. notes, "The image's orientation tag is not the one recorded in the marking. The marks are in stored pixels, so they stand, but check it is the same image."];
        }

        session.Load(state);
        status.Text = "Reopened " + Path.GetFileName(path) + "." + (notes.Count > 0 ? " " + string.Join(" ", notes) : "");
    }

    private void SetCalibreFromBox()
    {
        // Entry 107 section 1: a diameter in inches, or in millimetres marked mm, and nothing else; anything else is refused with what to type.
        var calibre = Calibre.Parse(calibreBox.Text, out string? why);
        if (why is not null)
        {
            calibreNote.Text = why;
            return;
        }

        if (calibre is not null)
        {
            calibreBox.Text = calibre.Name;
        }

        bool untouched = detectedState is not null && ReferenceEquals(session.State, detectedState);
        bool changed = calibre?.DiameterInches != session.State.Calibre?.DiameterInches;
        session.SetCalibre(calibre);
        if (!changed || calibre is null || detectedState is null)
        {
            return;
        }

        // Entry 78 section 4: the calibre decides whether a blob too small for two holes is split, so marks found without it are found again
        // with it, but only while nobody has corrected them.
        if (untouched)
        {
            DetectionTask = Detect(automatic: false);
            status.Text = CalibreRedetectText;
        }
        else
        {
            status.Text = CalibreAfterCorrectionsText;
        }
    }

    /// <summary>Types a calibre into the box and presses Set, for the headless tests.</summary>
    internal void EnterCalibre(string text)
    {
        calibreBox.Text = text;
        SetCalibreFromBox();
    }

    internal const string CalibreRedetectText = "Detecting again with the calibre, which keeps a mark too small to be two holes in one piece.";

    internal const string CalibreAfterCorrectionsText = "The calibre is used in finding holes from the next Detect, which would replace the corrections made here.";

    private async Task OpenMarkingDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "open-marking"));
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a saved marking",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("GroupLab markings") { Patterns = ["*.json"] }],
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "open-marking"), ("chosen", files.Count > 0));
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            OpenMarking(path);
        }
    }

    /// <summary>
    /// A direction in target axes as the screen shows it. A single reference length's axes are the stored image's, so "right" and
    /// "low" turn with the view; a rectangle's axes are its own tapped sides and a sheet's are its page, so they do not (entry 26).
    /// </summary>
    private PointD AsDisplayed(PointD vector) =>
        session.State.Scale?.AxesFollowImage == true ? ViewRotation.VectorToDisplay(vector, session.State.ViewQuarterTurns) : vector;

    /// <summary>A probability as a person says it, "one time in forty", for the aspect line of NOTES-FROM-PLANNING.md entry 76 section 1.</summary>
    internal static string HowOften(double probability) => probability switch
    {
        < 0.001 => "less than one time in a thousand",
        < 0.5 => string.Create(CultureInfo.InvariantCulture, $"one time in {Math.Round(1 / probability):0}"),
        < 0.995 => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(probability * 100):0} times in a hundred"),
        _ => "almost always",
    };

    /// <summary>An axis angle, degrees from x toward y, as the screen shows it, in [0, 180).</summary>
    private double DisplayedAngle(double degrees)
    {
        var v = AsDisplayed(new PointD(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180)));
        double angle = Math.Atan2(v.Y, v.X) * 180 / Math.PI;
        return ((angle % 180) + 180) % 180;
    }

    private async Task OpenImageDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "open-image"));
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a photograph or scan of a target",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.jpg", "*.jpeg", "*.png"] }],
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "open-image"), ("chosen", files.Count > 0));
        if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
        {
            OpenImage(path);
        }
    }

    /// <summary>
    /// The automatic path for a GroupLab sheet: the sheet names its definition, then registration, detection and assignment run on a
    /// background thread, and the result loads as ordinary marks the user can correct. A failure is shown as a prominent message, never only
    /// in the trace (DESIGN.md section 19).
    /// <para>
    /// NOTES-FROM-PLANNING.md entry 76 section 4: opening an image runs this on its own. When the image is not a sheet GroupLab recognises it
    /// does nothing and says so, rather than asking for a definition; the button runs it again and still asks. While it runs the status line
    /// shows it and it can be cancelled. Opening another image cancels it, and a result for an image no longer open is never applied.
    /// </para>
    /// </summary>
    private async Task Detect(bool automatic)
    {
        if (grey is null || valueImage is null || metadata is null)
        {
            status.Text = "Open an image first.";
            return;
        }

        detection?.Cancel();
        using var cancel = new CancellationTokenSource();
        detection = cancel;
        var token = cancel.Token;
        var (g, v, m) = (grey, valueImage, metadata);
        var trace = new TraceRecorder();
        ClearStages();
        trace.Filed += record => Avalonia.Threading.Dispatcher.UIThread.Post(() => AddStage(record));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        detectionProgress.IsVisible = cancelDetection.IsVisible = cancelDetection.IsEnabled = true;
        try
        {
            await Detect(automatic, g, v, m, trace, clock, token);
        }
        catch (OperationCanceledException)
        {
            DiagnosticLog.Info("detect.cancel", ("automatic", automatic));
            if (ReferenceEquals(g, grey))
            {
                status.Text = "Detection cancelled. Choose Detect on a GroupLab sheet to run it again.";
            }
        }
        finally
        {
            CrashReporter.InFlight = null;
            if (ReferenceEquals(detection, cancel))
            {
                detection = null;
                detectionProgress.IsVisible = cancelDetection.IsVisible = false;
            }
        }
    }

    private async Task Detect(bool automatic, GrayImage g, GrayImage v, ImageMetadata m, TraceRecorder trace, System.Diagnostics.Stopwatch clock, CancellationToken token)
    {
        status.Text = "Reading the sheet's codes…";

        // Entry 41 section 5: a crash during detection carries the stages that ran, which already hold the resolved parameters and decisions.
        CrashReporter.InFlight = trace;
        detectionMetadata = m;
        var identity = await Task.Run(() => SheetIdentification.Identify(g, ShippedDefinitions(), new OpenCvSharpBackend(), trace, token), token);
        CrashReporter.InFlight = null;
        token.ThrowIfCancellationRequested();
        DiagnosticLog.Info("detect.identify", ("definition", identity.DefinitionId), ("tile", identity.TileIndex), ("codes", identity.CodesRead), ("failure", identity.Failure), ("automatic", automatic));

        // Entry 35 section 6 item 3: the sheet names its own definition. Entry 115 section 4: when its codes cannot, because they did not
        // print or the picture cut them off, the screen asks which sheet it is by name, rather than stopping with the identity as the reason.
        if (identity.Definition is not { } named)
        {
            OfferTheSheet(g, v, m, identity.Failure);
            return;
        }

        status.Text = automatic ? $"Recognised {named.Name}. Registering and detecting…" : "Registering and detecting…";
        CrashReporter.InFlight = trace;
        var calibre = session.State.Calibre;
        // An interactive run keeps each stage's picture for the timeline; a batch run never asks, so it pays nothing (DESIGN.md section 19).
        var result = await Task.Run(() => AutomaticMarking.Run(g, v, m, named, new OpenCvSharpBackend(), trace, token, calibre, artefacts: true), token);
        CrashReporter.InFlight = null;
        token.ThrowIfCancellationRequested();
        LogDetection(result, trace, clock.ElapsedMilliseconds);
        if (ReferenceEquals(g, grey))
        {
            ApplyDetection(result);
        }
    }

    /// <summary>
    /// Entry 115 section 4: the sheet's codes could not be read, so the screen asks which sheet it is, by name, from the library and the
    /// person's own sheets. A sheet with no codes registers off its markers exactly as any other does. Marking it by hand stays beside it.
    /// </summary>
    private void OfferTheSheet(GrayImage g, GrayImage v, ImageMetadata m, string? why)
    {
        pendingDetection = (g, v, m);
        var sheets = ShippedDefinitions().Concat(ownSheets.List().Select(s => s.Definition))
            .GroupBy(d => d.Name, StringComparer.Ordinal).Select(group => group.First()).OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        pendingSheets = sheets;
        sheetChoice.ItemsSource = sheets.Select(d => d.Name).ToList();
        sheetChoice.SelectedIndex = sheets.Count > 0 ? 0 : -1;
        sheetChooser.IsVisible = true;
        status.Text = "GroupLab could not read this sheet's codes. Which sheet is it?";
        problem.Text = $"The codes did not give the sheet's definition ({why}). A sheet whose codes did not print, or that the picture cut off, still registers from its markers: choose which sheet it is, or mark it by hand.";
        DiagnosticLog.Info("detect.offer", ("sheets", sheets.Count), ("failure", why));
    }

    /// <summary>Detects the image waiting for a sheet, with the sheet the person chose.</summary>
    internal async Task DetectAsTheChosenSheet()
    {
        if (pendingDetection is not { } waiting || sheetChoice.SelectedIndex < 0 || pendingSheets.Count == 0)
        {
            return;
        }

        var chosen = pendingSheets[Math.Min(sheetChoice.SelectedIndex, pendingSheets.Count - 1)];
        sheetChooser.IsVisible = false;
        pendingDetection = null;
        problem.Text = "";
        status.Text = $"Registering and detecting as {chosen.Name}…";
        var trace = new TraceRecorder();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var calibre = session.State.Calibre;
        detectionMetadata = waiting.Metadata;
        var result = await Task.Run(() => AutomaticMarking.Run(waiting.Grey, waiting.Value, waiting.Metadata, chosen, new OpenCvSharpBackend(), trace, CancellationToken.None, calibre, artefacts: true));
        LogDetection(result, trace, clock.ElapsedMilliseconds);
        ApplyDetection(result);
    }

    /// <summary>Chooses the sheet by name and detects with it, as the panel's button does, for the headless tests.</summary>
    internal async Task ChooseTheSheet(string name)
    {
        sheetChoice.SelectedIndex = Math.Max(0, pendingSheets.ToList().FindIndex(d => d.Name == name));
        await DetectAsTheChosenSheet();
    }

    /// <summary>Whether the screen is asking which sheet this is, for the headless tests.</summary>
    internal bool AskingWhichSheet => sheetChooser.IsVisible;

    /// <summary>
    /// The definitions shipped beside the application, the built-in library with the frozen Phase 0 definitions below it, which a sheet's
    /// codes are matched against (NOTES-FROM-PLANNING.md entry 35 section 6 item 3).
    /// </summary>
    private static IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> ShippedDefinitions() => SheetIdentification.Candidates([Path.Combine(AppContext.BaseDirectory, "targets")]);

    /// <summary>
    /// A detection run in the log, NOTES-FROM-PLANNING.md entry 41 section 3: one line with its summary and how long it took, and at DEBUG every
    /// stage record in the console form DETECTION-PIPELINE.md section 6.3 gives, which already carries the resolved parameters and decisions.
    /// </summary>
    private static void LogDetection(AutomaticResult result, TraceRecorder trace, long milliseconds)
    {
        DiagnosticLog.Current.Write(result.Failure is null ? LogLevel.Info : LogLevel.Warn, "detect.run", [("ms", milliseconds), ("stages", trace.Records.Count), ("summary", result.Summary), ("failure", result.Failure)]);
        foreach (var record in trace.Records)
        {
            DiagnosticLog.Current.Write(LogLevel.Debug, "detect.stage", [("stage", record.Stage), ("status", record.Status), ("ms", record.DurationMs)], TraceConsole.Format(record, 3).Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    /// <summary>
    /// Loads what the automatic path found on this image as ordinary marks the user can correct, with the sheet's printed artwork for the snap
    /// and the size check, or shows the failure prominently. The Detect button and the screenshot test both come through here.
    /// </summary>
    internal void ApplyDetection(AutomaticResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Failure is not null || result.Scale is null)
        {
            // Entry 115 section 4: what to do next, not what failed. Marking it by hand is the way out of every one of them.
            string advice = detectionMetadata is { } read && result.Definition is { } sheet
                ? DetectionAdvice.Failure(result.Measurement, read, sheet) ?? result.Failure ?? "no registration"
                : result.Failure ?? "no registration";
            // Entry 120 section 1: a failure that arrives without a full stop used to run into the next sentence.
            problem.Text = advice.TrimEnd() + (advice.TrimEnd().EndsWith('.') ? " " : ". ") + "You can mark this image by hand instead, against a reference length or rectangle.";
            status.Text = "The sheet could not be read. What to do next is in the panel, and each stage is in Show work.";
            return;
        }

        canvas.MissingMarkers = result.MissingMarkers;
        canvas.Artwork = artwork = result.ExpectedArtwork;
        plotDefinition = result.Definition;
        registrationResidual = result.Measurement.Registration?.RmsResidual / 254;
        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        RememberDetected();
        SetTool(MarkingTool.Select);
        // Entry 115 section 4: a sheet its printer shrank is named as such, with the figure, rather than analysed silently.
        printScale.Text = DetectionAdvice.PrintScale(result.Measurement) ?? "";
        printScale.IsVisible = printScale.Text.Length > 0;
        // Entry 115 section 4: a sheet whose evidence says it may be another sheet is doubted out loud, rather than measured silently.
        problem.Text = result.Definition is { } against ? DetectionAdvice.Suspect(result.Measurement, against) ?? "" : "";
        status.Text = DetectedLine(result);
    }

    /// <summary>
    /// The status line after a detection, entry 109 section 2e: one line saying what happened, "Detected 25 holes on 25 bulls", and markers
    /// that were not found. How it was done is the scale's "why" and Show work.
    /// </summary>
    internal static string DetectedLine(AutomaticResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        int holes = result.Detections.Count, bulls = result.Bulls.Count(b => b.Scoring), sighters = result.Bulls.Count(b => !b.Scoring);
        string line = string.Create(CultureInfo.InvariantCulture, $"Detected {holes} {(holes == 1 ? "hole" : "holes")} on {bulls} {(bulls == 1 ? "bull" : "bulls")}{(sighters > 0 ? $" and {sighters} {(sighters == 1 ? "sighter" : "sighters")}" : "")}");
        return line + (result.MissingMarkers.Count > 0 ? string.Create(CultureInfo.InvariantCulture, $"; {result.MissingMarkers.Count} markers not found, crossed out") : "") + ".";
    }

    private async Task ExportDialog()
    {
        DiagnosticLog.Info("dialog.open", ("dialog", "export"));
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export the marking",
            SuggestedFileName = Path.GetFileNameWithoutExtension(session.State.ImagePath ?? "group") + ".grouplab.json",
            DefaultExtension = "json",
        });
        DiagnosticLog.Info("dialog.result", ("dialog", "export"), ("chosen", file is not null));
        if (file?.TryGetLocalPath() is { } path)
        {
            await File.WriteAllTextAsync(path, MarkingFile.Write(session.State, units));
            status.Text = "Exported to " + path;
            DiagnosticLog.Info("file.save", [.. DiagnosticLog.File(path), ("kind", "marking"), ("shots", session.State.Shots.Count)]);
        }
    }

    private void SetTool(MarkingTool tool)
    {
        canvas.Tool = tool;
        foreach (var (t, button) in toolButtons)
        {
            button.IsChecked = t == tool;
        }

        status.Text = tool switch
        {
            MarkingTool.Pan => "Drag to move the image. Zoom with the wheel or the buttons.",
            MarkingTool.Length => "Tap two points a known distance apart. The line is drawn as you make it; drag either end onto its mark, then enter the distance. The ends stay draggable afterwards.",
            MarkingTool.Rectangle => "Tap four corners of a known rectangle, top left first and around, then enter its size. Drag any corner onto its mark. This removes perspective.",
            MarkingTool.Aim => "Tap the point of aim.",
            MarkingTool.Impact => "Press on each impact, drag it to where it belongs, and let go to set it. It snaps to the hole under it, never onto a detected sheet's printed target, and on a sheet of bulls it is assigned to its nearest bull.",
            MarkingTool.Select => "Tap a shot to select it and drag to move it. With a shot selected, tap a bull to assign the shot to it. Drag an end of the scale to adjust it.",
            _ => status.Text,
        };
    }

    /// <summary>
    /// Asks for the size of a reference length. The ends are read from the canvas when the length is used, not when it was tapped, so
    /// an end dragged onto its mark in between is the one used (NOTES-FROM-PLANNING.md entry 39 section 3).
    /// </summary>
    private void AskLength()
    {
        scaleInputs.Children.Clear();
        var length = new TextBox { Text = "1", Width = 80 };
        var unit = units.Linear;
        scaleInputs.Children.Add(new TextBlock { Text = $"Drag either end onto its mark if it is not on it, then enter the distance between the ends, {UnitSettings.Symbol(unit)}:", TextWrapping = TextWrapping.Wrap });
        scaleInputs.Children.Add(Row(length, Button("Use this length", () =>
        {
            if (canvas.AwaitingTaps is { Count: 2 } ends && double.TryParse(length.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d > 0)
            {
                var reference = new LengthReference(ends[0], ends[1], UnitSettings.ToInches(d, unit));
                canvas.ClearAwaiting();
                session.SetScale(reference);
                SetTool(MarkingTool.Aim);
            }
        })));
    }

    /// <summary>
    /// Asks for the size of a reference rectangle, and offers the sheet size a contributor stated when the image has one (NOTES-FROM-PLANNING.md
    /// entry 37 section 5): offered and never assumed, in the order it was written.
    /// </summary>
    internal void AskRectangle()
    {
        scaleInputs.Children.Clear();
        var width = new TextBox { Text = "1", Width = 70 };
        var height = new TextBox { Text = "1", Width = 70 };
        var unit = units.Linear;
        scaleInputs.Children.Add(new TextBlock { Text = $"Drag any corner onto its mark if it is not on it, then enter the rectangle's width (first to second corner) and height, {UnitSettings.Symbol(unit)}:", TextWrapping = TextWrapping.Wrap });
        if (statedSize is { } stated)
        {
            scaleInputs.Children.Add(Button($"Use the stated sheet size, {stated.Text}", () =>
            {
                width.Text = UnitSettings.FromInches(stated.WidthInches, unit).ToString("0.###", CultureInfo.InvariantCulture);
                height.Text = UnitSettings.FromInches(stated.HeightInches, unit).ToString("0.###", CultureInfo.InvariantCulture);
                status.Text = $"The contributor's notes give the sheet as {stated.Text}, the first number as width. Tap the sheet's own corners, and swap the two numbers if the first side you tapped is the other one.";
            }));
        }
        scaleInputs.Children.Add(Row(width, height, Button("Use this rectangle", () =>
        {
            if (canvas.AwaitingTaps is { Count: 4 } corners
                && double.TryParse(width.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && w > 0
                && double.TryParse(height.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) && h > 0)
            {
                try
                {
                    var reference = new RectangleReference([.. corners], UnitSettings.ToInches(w, unit), UnitSettings.ToInches(h, unit));
                    canvas.ClearAwaiting();
                    session.SetScale(reference);
                    SetTool(MarkingTool.Aim);
                }
                catch (ArgumentException ex)
                {
                    problem.Text = ex.Message;
                    DiagnosticLog.Exception(LogLevel.Warn, "scale.rectangle", ex);
                }
            }
        })));
    }

    /// <summary>
    /// The scale in one line, NOTES-FROM-PLANNING.md entry 109 section 2c: where it came from and, for a sheet, how many of its markers were
    /// found, with a mark that says whether it is good, teal when the sheet registered on every marker and amber otherwise. Entry 42 section 4's
    /// reason stands, that every figure depends on the scale, so it stays at the top of its section; how it was made moves behind its "why",
    /// and the detection's stages are in Show work.
    /// </summary>
    private Control ScaleReadout(ScaleReference? scale)
    {
        var column = new StackPanel { Spacing = 0 };
        if (scale is null)
        {
            column.Children.Add(new TextBlock { Text = "No scale yet", Classes = { AppStyles.Label } });
            column.Children.Add(Line("Choose Scale: length or Scale: rectangle, or detect on a GroupLab sheet."));
            return column;
        }

        (string text, bool good) = scale switch
        {
            SheetReference { MarkersFound: { } found, MarkersExpected: { } expected } =>
                (string.Create(CultureInfo.InvariantCulture, $"Scale from the printed markers, {found} of {expected}"), found == expected),
            SheetReference => ("Scale from the sheet's printed markers", true),
            LengthReference => ("Scale from a reference length, set by hand", false),
            _ => ("Scale from a reference rectangle, set by hand", false),
        };
        var line = new DockPanel();
        var mark = new TextBlock { Text = good ? "\u2713" : "!", FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, Tokens.Space8, 0), Classes = { good ? AppStyles.Good : AppStyles.Warn } };
        DockPanel.SetDock(mark, Dock.Left);
        line.Children.Add(mark);
        line.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        column.Children.Add(Explained(line, "scale", "From " + scale.Describe(units) + "."));
        return column;
    }

    /// <summary>Redraws the canvas and rebuilds the panel from the session's current state.</summary>
    private void Refresh()
    {
        canvas.InvalidateVisual();
        var state = session.State;
        var report = GroupAnalysis.Analyse(state);
        problem.Text = report.Problem ?? "";
        ShowReview(state);

        if (!calibreBox.IsKeyboardFocusWithin && (calibreBox.Text ?? "") != (state.Calibre?.Name ?? ""))
        {
            calibreBox.Text = state.Calibre?.Name ?? "";
        }

        calibreNote.Text = state.Calibre is { } calibre
            ? $"Read as a {units.Length(calibre.DiameterInches)} bullet diameter. Type the diameter itself if that is not right."
            : "No calibre: extreme spread is centre to centre only, and a tap snaps within its default reach.";
        if (!shotDistance.IsKeyboardFocusWithin)
        {
            shotDistance.Text = state.ShotDistanceInches is { } inches ? UnitSettings.DistanceFromInches(inches, units.Distance).ToString("0.###", CultureInfo.InvariantCulture) : "";
        }

        canvas.DetectorFlags = state.Shots.Where(s => s.IsShot && s.Oversize is not null).ToDictionary(s => s.Id, s => s.Oversize!.Tentative);
        var open = ReviewQueue.For(state, analyseSighters).Where(i => !i.Resolved).ToList();
        canvas.NeedsPerson = open.Where(i => i.ShotId is not null).Select(i => i.ShotId!.Value).ToHashSet();
        canvas.ReviewShot = open.FirstOrDefault(i => i.Key == currentReview)?.ShotId ?? open.FirstOrDefault()?.ShotId;

        if (canvas.AwaitingTaps.Count == 0 && (scaleInputs.Children.Count == 0 || state.Scale is not null))
        {
            scaleInputs.Children.Clear();
            scaleInputs.Children.Add(ScaleReadout(state.Scale));
        }

        emptyCanvas.IsVisible = state.ImagePath is null;
        statistics.Children.Clear();
        moreFigures.Children.Clear();
        judgements.Children.Clear();
        flags.Children.Clear();
        ShowBreadcrumb(state);
        ShowEquipment(state);
        ShowZero(state);
        if (report.AllShots is { } all)
        {
            var reduced = report.WithoutExclusions!;
            bool excluded = report.Excluded > 0;
            // Entry 46 section 3: where a human judgement entered the measurement, as the first row's detail. Entry 109 section 3: every figure
            // is one row of one shape, the label left, the value right in mono, one detail line beneath and a hairline under it.
            // Entry 111 section 3: the count once, in the sentence that also says how the shots were placed.
            statistics.Children.Add(Rowed(Line(PlacedLine(all.Shots, report.Automatic, report.Corrected, report.Manual)
                + string.Create(CultureInfo.InvariantCulture, $"{(excluded ? $"; {reduced.Shots} without the {report.Excluded} excluded" : "")}{(report.NotShots > 0 ? $"; {report.NotShots} marked not a shot" : "")}."))));
            statistics.Children.Add(Rowed(all.CentreFromAim is { } offsetFromAim && AsDisplayed(offsetFromAim) is var centre
                ? CentreRow(centre)
                : Line($"Centre from aim: {all.CentreFromAimUnavailable}.")));

            if (all.DispersionWithheld is { } withheld)
            {
                // Entry 24 section 1: below the minimum there is no headline figure to misread, only what is missing.
                statistics.Children.Add(new TextBlock { Text = withheld, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold });
            }
            else
            {
                // Entry 92 section 3: one row per figure, label left and value right, with the angular conversion beneath its linear value.
                // Mean radius keeps its lead size, entries 73 and 92; the others are the value size. Extreme spread is dimmer: present, and
                // visibly subordinate, its interval behind the More figures disclosure because it changes no decision.
                statistics.Children.Add(Rowed(Figure("Mean radius", all.MeanRadius!, excluded ? reduced : null, f => f.MeanRadius, Tokens.LeadValueSize, FontWeight.Medium)));
                statistics.Children.Add(Rowed(Figure("Sigma", all.Sigma!, excluded ? reduced : null, f => f.Sigma, Tokens.ValueSize, FontWeight.Medium)));
                statistics.Children.Add(Rowed(Figure("Extreme spread", all.ExtremeSpread!, excluded ? reduced : null, f => f.ExtremeSpread, Tokens.ValueSize, FontWeight.Normal, subordinate: true, interval: false)));

                // Entry 103 section 1: the two figures the concept's stack has and the screen lacked, after the existing ones.
                if (all is { Cep90: { } cep90, Cep50: not null, Cep95: not null })
                {
                    var cep = new StackPanel { Spacing = 0 };
                    cep.Children.Add(Readout("CEP 90", units.Length(cep90.Value), Tokens.ValueSize));
                    var cepLines = CepDetails(all, excluded ? reduced : null);
                    cep.Children.Add(Explained(Detail(cepLines[0]), "cep", CepWhy));
                    foreach (string line in cepLines.Skip(1))
                    {
                        cep.Children.Add(Detail(line));
                    }

                    statistics.Children.Add(Rowed(cep));
                }

                if (all is { SdX: not null, SdY: not null } && SizeValue(all) is { } widthByHeight)
                {
                    var size = new StackPanel { Spacing = 0 };
                    size.Children.Add(Readout("Group width \u00d7 height", widthByHeight, Tokens.ValueSize));
                    foreach (string line in SizeDetails(all, excluded ? reduced : null))
                    {
                        size.Children.Add(Detail(line));
                    }

                    statistics.Children.Add(Rowed(size));
                }

                foreach (string line in MoreFigureLines(state, all))
                {
                    moreFigures.Children.Add(Line(line));
                }

                ShowJudgements(state, all);
            }

            // Entry 82 section 6: the detector's flag on a mark that covers about two holes, in the panel as well as on the canvas. It is the
            // only size opinion the screen carries, and the review queue counts every one of them (entry 87 section 1).
            // Entry 104 section 4: the flags sit behind one disclosure that counts them, so however many there are the cards stay in view.
            // Each is a review item as well, which the amber line above counts.
            var flagged = new StackPanel { Spacing = 4 };
            foreach (var shot in state.Shots.Where(s => s.IsShot && s.Oversize is not null))
            {
                flagged.Children.Add(new TextBlock
                {
                    Text = shot.Oversize!.Describe(ShotLabel(shot.Id)),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = Tokens.SecondarySize,
                    Classes = { shot.Oversize.Tentative ? AppStyles.Secondary : AppStyles.Alert },
                });
            }

            if (flagged.Children.Count > 0)
            {
                flags.Children.Add(new Expander
                {
                    Header = flagged.Children.Count == 1 ? "1 mark flagged as possibly two holes" : $"{flagged.Children.Count} marks flagged as possibly two holes",
                    Content = flagged,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                });
            }

            if (moreFigures.Children.Count > 0)
            {
                flags.Children.Add(moreFiguresPanel);
            }
        }

        BuildShotList();
        BuildSelection();
        ShowSighters(state);
        canvas.SetAside = analyseSighters ? new HashSet<int>() : state.Shots.Where(s => GroupAnalysis.OnSighter(state, s)).Select(s => s.Id).ToHashSet();
        ShowAnalysis(state);
    }

    /// <summary>
    /// The two judgement cards, NOTES-FROM-PLANNING.md entry 103 section 2. A card is a bold verdict and then its evidence, never the verdict
    /// alone. Two errors in the concept screenshot are not inherited: its round card named Pitman-Morgan, which is the stringing test, where
    /// the circularity test is the likelihood ratio (docs/STATISTICS.md section 7, "label them differently in the interface, because they answer
    /// different questions"); and it printed no evidence of stringing without what its shot count could have detected, which the same section
    /// forbids. The flyer card keeps its hedge, "by that measure alone".
    /// </summary>
    private void ShowJudgements(MarkingState state, GroupFigures all)
    {
        foreach (var card in JudgementCardsFor(state, all))
        {
            judgements.Children.Add(Card(card.Name, card.Verdict, [.. card.Evidence], [.. card.Why]));
        }
    }

    /// <summary>A judgement card's words: its name, the verdict, the lines that stay in view, and those behind its "why".</summary>
    private sealed record JudgementWords(string Name, string Verdict, IReadOnlyList<string> Evidence, IReadOnlyList<string> Why);

    private List<JudgementWords> JudgementCardsFor(MarkingState state, GroupFigures all)
    {
        var cards = new List<JudgementWords>();
        int n = all.Shots;
        if (all.Circularity is { } circular && all.Stringing is { } stringing)
        {
            string aspect = all.AspectRatio is { } a
                ? string.Create(CultureInfo.InvariantCulture, $"Error ellipse aspect {a:0.00}, major axis at {DisplayedAngle(all.AngleDegrees ?? 0):0} degrees; {n} circular shots give about {all.CircularMedianAspect:0.0} and exceed {a:0.00} {HowOften(all.CircularAspectExceedance ?? 1)}.")
                : $"Error ellipse: {all.AspectRatioUnavailable}.";
            bool round = circular.PValue >= 0.05;
            string verdict = round ? $"Round, as far as {n} shots can tell." : "Not round.";
            string test = string.Create(CultureInfo.InvariantCulture, $"Circularity test, {circular.Method}: p = {circular.PValue:0.000}");
            string reading = round
                ? "So there is no evidence the group is anything but circular."
                : string.Create(CultureInfo.InvariantCulture, $"A circular group of {n} is this far from round {HowOften(circular.PValue)}.");
            bool strings = stringing.PValueVertical < 0.05;
            string stringingLine = string.Create(CultureInfo.InvariantCulture, $"Vertical stringing, a separate question, by Pitman-Morgan: p = {stringing.PValueVertical:0.000} one-sided, ")
                + (strings
                    ? "so the spread up and down is larger than across beyond what chance gives."
                    : "so no evidence of vertical stringing. " + ShapeTests.StringingPowerSentence(n) + " No evidence is not evidence of none.");

            // Entry 109 section 3: the verdict and one line naming the test with its p value; the stringing line with its power statement stays
            // in view, because STATISTICS.md section 7 requires the power beside the result and not in a footnote.
            cards.Add(new("shape", verdict, [test, stringingLine], [reading, aspect]));
        }
        else if (all.ShapeTestsUnavailable is { } why)
        {
            cards.Add(new("shape", "No shape judgement.", [$"The shape tests are {why}."], []));
        }

        // Entry 104 section 2: judged against circular groups measured the way this one is, by its own mean radius about its own centre,
        // not by section 10's closed form, which assumes the true ones and at five shots could never flag anything.
        if (all.WorstShot is { } calibrated && WorstShot(state) is { } worstId)
        {
            double beyond = calibrated.PValue;
            string label = ShotLabel(worstId);
            string sits = string.Create(CultureInfo.InvariantCulture,
                $"It sits at {calibrated.Observed:0.00} of the group's own mean radii from its centre. Circular groups of {n}, measured the same way, put their worst at {calibrated.Expected:0.00} on average, and this far out or farther {HowOften(beyond)}, from {calibrated.Resamples} simulated groups.");
            string evidence = string.Create(CultureInfo.InvariantCulture, $"Worst shot against {calibrated.Resamples} simulated circular groups: p = {beyond:0.000}");
            // The hedge stays in view with the verdict: without it "not a flyer" would say more than the test can.
            cards.Add(beyond >= 0.05
                ? new("flyer", $"Shot {label} is not a flyer.", [evidence, "So a shot there is not a flyer by that measure alone (STATISTICS.md section 10)."], [sits])
                : new("flyer", $"Shot {label} is further out than a group of {n} usually puts its worst.",
                    [evidence, "That makes it worth a look, not a flyer by that measure alone: whether it was called or pulled is yours to say, and excluding it shows every figure both ways (STATISTICS.md section 10)."],
                    [sits]));
        }

        return cards;
    }

    /// <summary>
    /// One judgement card: the verdict in bold, the lines that stay in view beneath it, and the rest behind its "why". Entry 109 section 1
    /// principle 4: a card is a section divided by a rule, not a box.
    /// </summary>
    private Border Card(string name, string verdict, string[] evidence, string[] why)
    {
        var column = new StackPanel { Spacing = Tokens.Space4 };
        column.Children.Add(new TextBlock { Text = verdict, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        for (int i = 0; i < evidence.Length; i++)
        {
            var line = new TextBlock { Text = evidence[i], TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
            // The card's "why" sits beside its last line in view.
            column.Children.Add(i == evidence.Length - 1 && why.Length > 0 ? Explained(line, name, why) : line);
        }

        return new Border { Child = column, Name = name + "Card", Classes = { AppStyles.JudgementCard } };
    }

    /// <summary>The group's shots as the figures count them: every shot, sighters left out, excluded ones included.</summary>
    private static List<MarkedShot> GroupShots(MarkingState state)
    {
        var sighters = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        return [.. state.Shots.Where(s => s.IsShot && !(s.Bull is { } b && sighters.Contains(b)))];
    }

    /// <summary>The shot furthest from the group's centre, which the flyer card is about.</summary>
    private static int? WorstShot(MarkingState state)
    {
        var shots = GroupShots(state);
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        if (offsets.Count == 0)
        {
            return null;
        }

        var centre = GroupStatistics.Centre(offsets);
        return shots[Enumerable.Range(0, offsets.Count).MaxBy(i => Math.Pow(offsets[i].X - centre.X, 2) + Math.Pow(offsets[i].Y - centre.Y, 2))].Id;
    }

    /// <summary>
    /// Everything the analysis state shows that is not a figure: the header's pill and crumb, the amber line for decisions left unmade, the
    /// composite plot, the shot table and the load.
    /// </summary>
    private void ShowAnalysis(MarkingState state)
    {
        bool here = destination == Destination.Analyse, marking = !analysing && here, analysis = analysing && here;
        editorActions.IsVisible = breadcrumb.IsVisible = editorBody.IsVisible = marking;
        analysisActions.IsVisible = analysisCrumbs.IsVisible = analysisBody.IsVisible = analysis;
        settingsBody.IsVisible = destination == Destination.Settings;
        sessionsBody.IsVisible = destination == Destination.Sessions;
        libraryBody.IsVisible = destination == Destination.Library;
        ballisticsBody.IsVisible = destination == Destination.Ballistics;
        compareBody.IsVisible = destination == Destination.Compare;
        settingsCrumb.IsVisible = !here;
        settingsCrumb.Text = destination switch { Destination.Sessions => "\u203a  Session records", Destination.Library => "\u203a  Target library", Destination.Ballistics => "\u203a  Ballistics", Destination.Compare => "\u203a  Compare loads", _ => "\u203a  Settings" };
        workBar.IsVisible = workShown && here;
        railHere.Classes.Set(AppStyles.Warn, here);
        railSettings.Classes.Set(AppStyles.Warn, destination == Destination.Settings);
        railSessions.Classes.Set(AppStyles.Warn, destination == Destination.Sessions);
        railLibrary.Classes.Set(AppStyles.Warn, destination == Destination.Library);
        railBallistics.Classes.Set(AppStyles.Warn, destination == Destination.Ballistics);
        railCompare.Classes.Set(AppStyles.Warn, destination == Destination.Compare);

        // Entry 109 section 3e: the crumb is the way back, so it names what it goes back to, the image's file as the editor's crumb does, or
        // the sheet's name for a marking with no image recorded.
        sheetCrumb.Content = state.ImagePath is { } path ? Path.GetFileName(path) : plotDefinition?.Name ?? "the sheet";
        ToolTip.SetTip(sheetCrumb, "Back to the sheet, with every edit as you left it");
        registrationText.Text = state.Scale switch
        {
            null => "no scale",
            SheetReference when registrationResidual is { } residual => "registered, residual " + units.Length(residual),
            SheetReference => "registered from the sheet's markers",
            _ => "scale set by hand",
        };
        ToolTip.SetTip(registrationPill, state.Scale is null ? null : "From " + state.Scale.Describe(units) + ".");
        foreach (var classes in new[] { registrationPill.Classes, registrationText.Classes })
        {
            classes.Remove(AppStyles.Good);
            classes.Remove(AppStyles.Warn);
            classes.Add(state.Scale is SheetReference ? AppStyles.Good : AppStyles.Warn);
        }

        // Entry 109 section 3a: a compact amber banner at the top, with a link back to the editor; entry 103 section 1's sentence behind "why".
        int open = ReviewQueue.Open(ReviewQueue.For(state, analyseSighters));
        unsettledBanner.IsVisible = open > 0;
        unsettled.Text = open == 1 ? "1 decision left unmade." : $"{open} decisions left unmade.";
        unsettledBanner.Children.Clear();
        unsettledBanner.Children.Add(Explained(bannerLine, "unsettled", open == 1
            ? "1 decision was left unmade when this was accepted, and every figure here inherits it. The sheet crumb goes back to it."
            : $"{open} decisions were left unmade when this was accepted, and every figure here inherits them. The sheet crumb goes back to them."));

        loadLines.Children.Clear();
        loadLines.Children.Add(Readout("Rifle", state.Rifle?.Name ?? "not chosen", Tokens.SecondarySize, labelAtTop: true));
        loadLines.Children.Add(Readout("Barrel", state.Barrel ?? "not chosen", Tokens.SecondarySize, labelAtTop: true));
        loadLines.Children.Add(Readout("Load", state.Load ?? "not chosen", Tokens.SecondarySize, labelAtTop: true));
        // Entry 104 section 4: a calibre set after detection ran without one would otherwise sit beside a status line saying there was none.
        loadLines.Children.Add(Readout("Calibre", state.Calibre is null ? "not set"
            : state.Detection is { Calibre: null } ? $"{state.Calibre.Name}, set after detection" : state.Calibre.Name, Tokens.SecondarySize, labelAtTop: true));

        // The plot: scoring shots only, each from its own bull; excluded ones kept and drawn hollow, marks set to not a shot absent.
        var shots = GroupShots(state);
        var offsets = GroupAnalysis.CompositeOffsets(state, shots);
        var plotted = offsets.Count == shots.Count
            ? shots.Select((s, i) => new PlotShot(s.Id, ShotLabel(s.Id), s.Bull is { } b ? BullLabel(b) : null, offsets[i], s.Exclusion is not null)).ToList()
            : [];
        plot.Shots = plotted;
        plot.CalibreInches = state.Calibre?.DiameterInches;
        outlinesToggle.IsVisible = state.Calibre is not null;
        plot.Length = inches => units.Length(inches);
        var kept = plotted.Where(p => !p.Excluded).ToList();
        plot.Centre = kept.Count > 0 ? GroupStatistics.Centre([.. kept.Select(p => p.Offset)]) : null;
        if (kept.Count >= GroupAnalysis.MinimumShotsForDispersion)
        {
            var rayleigh = GroupStatistics.Rayleigh([.. kept.Select(p => p.Offset)]);
            plot.Cep50Inches = rayleigh.Cep(0.5).Value;
            plot.Cep90Inches = rayleigh.Cep(0.9).Value;
        }
        else
        {
            plot.Cep50Inches = plot.Cep90Inches = null;
        }

        plot.SpreadPair = kept.Count >= 2 && GroupGeometry.MaximumPairDistance([.. kept.Select(p => p.Offset)]) is var (_, first, second)
            ? (kept[first].Id, kept[second].Id)
            : null;
        plot.Discs = PlotDiscs(state);
        plotSelection.RemoveWhere(id => plotted.All(p => p.Id != id));
        if (canvas.Selected is { } selected && !plotSelection.Contains(selected))
        {
            plotSelection = [selected];
        }

        plot.Selected = plotSelection;
        plot.InvalidateVisual();

        offsetTable.Children.Clear();
        // Entry 109 section 3c: plain rows with every other one shaded, no border round each. A shot is named by its bull, so the table shows
        // one number, and a bull column only where some shot sits on a bull other than its name. The numbers are right-aligned under their
        // headings, so with a fixed number of decimals the decimal points line up.
        var centre = plot.Centre;
        // Entry 104 section 4: a column headed with the shot's number is read as sorted by it, so it is, not in detection order.
        var ordered = plotted.OrderBy(p => int.TryParse(p.Label, NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) ? k : int.MaxValue).ThenBy(p => p.Label, StringComparer.Ordinal).ToList();
        bool bullColumn = ordered.Any(p => p.Bull is { } b && b != p.Label);
        string columns = bullColumn ? "44,44,*,*,*" : "44,*,*,*";
        Grid Cells(IEnumerable<string> texts, bool heading, bool dim, bool struck)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(columns) };
            int c = 0;
            foreach (string text in texts)
            {
                var cell = new TextBlock
                {
                    Text = text,
                    FontFamily = heading ? Tokens.Sans : Mono,
                    FontSize = Tokens.DetailSize,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 0, Tokens.Space8, 0),
                    TextDecorations = struck ? TextDecorations.Strikethrough : null,
                    Classes = { heading || dim ? AppStyles.Dim : AppStyles.Secondary },
                };
                Grid.SetColumn(cell, c++);
                grid.Children.Add(cell);
            }

            return grid;
        }

        string[] Heads() => bullColumn ? ["shot", "bull", "across", "up/down", "radius"] : ["shot", "across", "up/down", "radius"];
        var head = Cells(Heads(), heading: true, dim: true, struck: false);
        head.Margin = new Thickness(Tokens.Space4, 0, Tokens.Space4, Tokens.Space4);
        offsetTable.Children.Add(head);
        int index = 0;
        foreach (var shot in ordered)
        {
            double r = centre is { } c ? Math.Sqrt(Math.Pow(shot.Offset.X - c.X, 2) + Math.Pow(shot.Offset.Y - c.Y, 2)) : 0;
            var shown = AsDisplayed(shot.Offset);
            var texts = new List<string> { shot.Label };
            if (bullColumn)
            {
                texts.Add(shot.Bull is { } b && b != shot.Label ? b : "");
            }

            texts.AddRange([units.Number(shown.X), units.Number(-shown.Y), units.Number(r)]);
            var row = new Button
            {
                Content = Cells(texts, heading: false, dim: shot.Excluded, struck: shot.Excluded),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                FontWeight = plotSelection.Contains(shot.Id) ? FontWeight.SemiBold : FontWeight.Normal,
                Classes = { AppStyles.TableRow },
            };
            if (index++ % 2 == 1)
            {
                row.Classes.Add(AppStyles.Shaded);
            }

            if (plotSelection.Contains(shot.Id))
            {
                row.Classes.Add(AppStyles.Warn);
            }

            ToolTip.SetTip(row, $"Shot {shot.Label}{(shot.Bull is { } bull ? ", bull " + bull : "")}{(shot.Excluded ? ", excluded" : "")}");
            int id = shot.Id;
            row.Click += (_, _) => PickShots([id]);
            offsetTable.Children.Add(row);
        }

        ShowThumbnail(state);
        ShowFullFigures(state);
        ShowBullLoads(state);
    }

    /// <summary>One scoring bull's discs from the sheet's definition, in inches, outermost first; none for a marking the definition is not known for.</summary>
    private IReadOnlyList<PlotDisc> PlotDiscs(MarkingState state)
    {
        if (plotDefinition is not { } definition || definition.Bulls.FirstOrDefault(b => b.Scoring) is not { } bull
            || definition.RingSets.FirstOrDefault(r => r.Key == bull.RingSet) is not { } rings || state.Scale is null)
        {
            return [];
        }

        var inks = definition.Inks.ToDictionary(i => i.Key);
        return [.. rings.Discs.Select(d => inks.TryGetValue(d.Ink, out var ink)
            ? new PlotDisc(d.Diameter / 254.0, Tokens.Ink(ink.Srgb), ink.Role == GroupLab.Core.Gltd.Model.InkRole.Paper)
            : new PlotDisc(d.Diameter / 254.0, Tokens.Paper, true))];
    }

    /// <summary>A click on the plot or a row of its table: one shot, or the extreme spread's two, picked on the plot and selected on the sheet.</summary>
    internal void PickShots(IReadOnlyList<int> ids)
    {
        plotSelection = [.. ids];
        canvas.Selected = ids.Count > 0 ? ids[0] : null;
        status.Text = ids.Count == 2
            ? $"Shots {ShotLabel(ids[0])} and {ShotLabel(ids[1])}: the extreme spread is the distance between them."
            : ids.Count == 1 ? $"Shot {ShotLabel(ids[0])}." : status.Text;
        Refresh();
    }

    /// <summary>Accept and analyse: the analysis state, whatever is still open, which the amber line then names.</summary>
    internal void Analyse()
    {
        int open = ReviewQueue.Open(ReviewQueue.For(session.State, analyseSighters));
        DiagnosticLog.Info("analysis.accept", ("open", open));
        SetAnalysing(true);
        SaveSession();
    }

    /// <summary>
    /// Accept and analyse saves the session, entry 112 section 1: the marking with every edit and exclusion, the figures as computed, the
    /// definition it was analysed against, a proof image of about 150 dpi, and the original's path and SHA-256, never a copy of it. A second
    /// Accept on the same marking updates the same session.
    /// </summary>
    private void SaveSession()
    {
        if (sessions is null || session.State.Shots.Count == 0)
        {
            return;
        }

        var state = session.State;
        var figures = GroupAnalysis.Analyse(state).AllShots?.MeanRadius;
        string? path = state.ImagePath is { } p && File.Exists(p) ? p : null;
        var (proof, proofType) = ProofImage(path, state.Scale);
        // A second Accept on the same session updates it and keeps the day it was first saved.
        var existing = currentSession is { } saved ? sessions.Get(saved) : null;
        var record = new SessionRecord(
            existing?.Id ?? 0,
            existing?.CreatedUtc ?? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            existing?.ShotDate ?? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            plotDefinition?.Name ?? (state.ImagePath is { } named ? Path.GetFileName(named) : "Marked by hand"),
            plotDefinition is { } d ? GroupLab.Core.Gltd.Binary.GltdBinary.Encode(d).Encoding?.DefinitionId : null,
            plotDefinition is { } defined ? System.Text.Encoding.UTF8.GetString(CanonicalJsonWriter.Write(defined)) : null,
            state.ShotDistanceInches,
            state.Rifle?.Name,
            state.Barrel,
            state.Load,
            state.Calibre?.DiameterInches,
            MarkingFile.Write(state, units),
            CountedShots(state),
            figures?.Value,
            figures?.Lower,
            figures?.Upper,
            state.ImagePath,
            path is null ? null : Sha256(path),
            proof,
            proofType);
        try
        {
            currentSession = sessions.Save(record);
            DiagnosticLog.Info("session.save", ("session", currentSession), ("shots", record.ShotCount), ("proof", proof?.Length ?? 0));
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            status.Text = "The session could not be saved: " + ex.Message;
            DiagnosticLog.Exception(LogLevel.Warn, "session.save", ex);
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(stream));
    }

    /// <summary>
    /// The proof image of DESIGN.md section 18: the original at about 150 dpi, from the scale where there is one and otherwise at 1650 pixels on
    /// the long side, as a JPEG of a few hundred kilobytes, enough to eyeball and audit. Null when there is no image to make it from.
    /// </summary>
    private static (byte[]? Bytes, string? Type) ProofImage(string? path, ScaleReference? scale)
    {
        if (path is null)
        {
            return (null, null);
        }

        using var colour = OpenCvSharp.Cv2.ImRead(path, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            return (null, null);
        }

        double ppi = scale is null ? double.NaN : HoleSize.PixelsPerInch(scale, new PointD(colour.Width / 2.0, colour.Height / 2.0));
        double factor = double.IsFinite(ppi) && ppi > 0 ? Math.Min(1, 150 / ppi) : Math.Min(1, 1650.0 / Math.Max(colour.Width, colour.Height));
        using var small = new OpenCvSharp.Mat();
        OpenCvSharp.Cv2.Resize(colour, small, new OpenCvSharp.Size(), factor, factor, OpenCvSharp.InterpolationFlags.Area);
        OpenCvSharp.Cv2.ImEncode(".jpg", small, out byte[] jpeg, new OpenCvSharp.ImageEncodingParam(OpenCvSharp.ImwriteFlags.JpegQuality, 80));
        return (jpeg, "image/jpeg");
    }

    /// <summary>
    /// Reopens a saved session to its analysis, entry 112 section 1: the marking as it was saved, against the definition saved with it. The
    /// original image is opened when it is still at its path with the same SHA-256; when it is not, nothing needs it, and the screen says so.
    /// </summary>
    internal void OpenSession(long id)
    {
        if (sessions?.Get(id) is not { } record)
        {
            return;
        }

        MarkingState state;
        try
        {
            (state, _) = MarkingFile.Read(record.MarkingJson);
        }
        catch (MarkingFileException ex)
        {
            problem.Text = "The session's marking could not be read: " + ex.Message;
            return;
        }

        bool original = record.ImagePath is { } image && File.Exists(image) && Sha256(image) == record.ImageSha256;
        if (original)
        {
            bool detect = DetectOnOpen;
            DetectOnOpen = false;
            OpenImage(record.ImagePath!);
            DetectOnOpen = detect;
        }
        else
        {
            grey = valueImage = artwork = null;
            metadata = null;
            canvas.SetImage(null, null);
            canvas.Artwork = null;
        }

        plotDefinition = record.DefinitionJson is { } json ? GltdJsonReader.Read(System.Text.Encoding.UTF8.GetBytes(json)).Definition : null;
        registrationResidual = null;
        detectedState = null;
        session.Load(state);
        currentSession = id;
        destination = Destination.Analyse;
        SetAnalysing(true);
        DiagnosticLog.Info("session.open", ("session", id), ("original", original));
        status.Text = original
            ? $"Reopened the session of {record.ShotDate}, {record.SheetName}."
            : $"Reopened the session of {record.ShotDate}, {record.SheetName}. The original image is not where it was, so everything shown is from the saved marking.";
    }

    /// <summary>The session this marking was saved as, for the headless tests.</summary>
    internal long? CurrentSession => currentSession;

    /// <summary>The store, for the headless tests.</summary>
    internal SessionStore? Sessions => sessions;

    /// <summary>
    /// The Session records screen, entry 112 section 1: every session newest first, its date, sheet, rifle, load, distance, shot count and mean
    /// radius with its interval, filtered by rifle and by load. A row opens its session; delete asks first. The concept draws no such screen,
    /// so it is the shot table's style, with entry 109's rows and hairlines, and nothing new.
    /// </summary>
    private Control BuildSessions()
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12 };
        column.Children.Add(new TextBlock { Text = "Session records", Classes = { AppStyles.Title } });
        column.Children.Add(Line("Every sheet saved by Accept and analyse, newest first. A row opens its analysis."));
        var filters = Row(FieldLabel("Rifle"), sessionRifle, FieldLabel("Load"), sessionLoad);
        foreach (var combo in new[] { sessionRifle, sessionLoad })
        {
            combo.SelectionChanged += (_, _) =>
            {
                if (!fillingSessions)
                {
                    FillSessions();
                }
            };
        }

        column.Children.Add(filters);
        // Entry 113 section 2: each row's box chooses it for comparing, and two or more chosen compare side by side.
        column.Children.Add(Row(Button("Compare the chosen", CompareChosen), new TextBlock { Text = "Tick two or more sessions to compare their loads.", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } }));
        column.Children.Add(sessionRows);
        return new ScrollViewer { Content = column, IsVisible = false };
    }

    private const string SessionColumns = "96,*,150,150,76,56,220,Auto";

    private const double SessionBoxWidth = 32;

    private const double SessionDeleteWidth = 76;

    /// <summary>Fills the Session records list from the store, with the filters as chosen.</summary>
    private void FillSessions()
    {
        sessionRows.Children.Clear();
        if (sessions is null)
        {
            sessionRows.Children.Add(Line("The session database could not be opened, so there are no records to show."));
            return;
        }

        fillingSessions = true;
        string? rifle = sessionRifle.SelectedIndex > 0 ? sessionRifle.SelectedItem as string : null;
        string? load = sessionLoad.SelectedIndex > 0 ? sessionLoad.SelectedItem as string : null;
        var all = sessions.List();
        sessionRifle.ItemsSource = new[] { "Every rifle" }.Concat(all.Select(s => s.Rifle).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)).ToList();
        sessionLoad.ItemsSource = new[] { "Every load" }.Concat(all.Select(s => s.Load).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)).ToList();
        sessionRifle.SelectedItem = rifle ?? "Every rifle";
        sessionLoad.SelectedItem = load ?? "Every load";
        fillingSessions = false;

        var list = sessions.List(rifle, load);
        if (list.Count == 0)
        {
            sessionRows.Children.Add(Line(all.Count == 0 ? "No sessions yet. Accept and analyse on a marked sheet saves one." : "No session matches the rifle and load chosen."));
            return;
        }

        Grid Cells(IEnumerable<string> texts, bool heading)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions(SessionColumns) };
            int c = 0;
            foreach (string text in texts)
            {
                var cell = new TextBlock
                {
                    Text = text,
                    FontSize = Tokens.DetailSize,
                    FontFamily = heading || c is 1 or 2 or 3 ? Tokens.Sans : Mono,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Margin = new Thickness(0, 0, Tokens.Space8, 0),
                    HorizontalAlignment = c >= 4 ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    Classes = { heading ? AppStyles.Dim : AppStyles.Secondary },
                };
                Grid.SetColumn(cell, c++);
                grid.Children.Add(cell);
            }

            return grid;
        }

        // The heading has the rows' shape, a box's width on the left and Delete's on the right, so each heading sits over its column.
        var head = Cells(["date", "sheet", "rifle", "load", "distance", "shots", "mean radius, interval"], heading: true);
        head.Margin = new Thickness(Tokens.Space4, 0, Tokens.Space4, Tokens.Space4);
        var heading = new DockPanel();
        var boxSpace = new Border { Width = SessionBoxWidth };
        var deleteSpace = new Border { Width = SessionDeleteWidth };
        DockPanel.SetDock(boxSpace, Dock.Left);
        DockPanel.SetDock(deleteSpace, Dock.Right);
        heading.Children.Add(boxSpace);
        heading.Children.Add(deleteSpace);
        heading.Children.Add(head);
        sessionRows.Children.Add(heading);
        int index = 0;
        foreach (var s in list)
        {
            string distance = s.DistanceInches is { } d ? string.Create(CultureInfo.InvariantCulture, $"{UnitSettings.DistanceFromInches(d, units.Distance):0} {UnitSettings.Symbol(units.Distance)}") : "";
            string radius = s.MeanRadiusInches is { } r
                ? units.Length(r) + (s.MeanRadiusLowerInches is { } lo && s.MeanRadiusUpperInches is { } hi ? $" ({units.Number(lo)} to {units.Number(hi)})" : "")
                : "";
            var cells = Cells([s.ShotDate ?? s.CreatedUtc[..10], s.SheetName, s.Rifle ?? "", s.Load ?? "", distance, s.ShotCount.ToString(CultureInfo.InvariantCulture), radius], heading: false);
            long id = s.Id;
            var open = new Button { Content = cells, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch, Classes = { AppStyles.TableRow } };
            if (index++ % 2 == 1)
            {
                open.Classes.Add(AppStyles.Shaded);
            }

            ToolTip.SetTip(open, $"Open the session of {s.ShotDate}, {s.SheetName}");
            open.Click += (_, _) => OpenSession(id);
            var delete = Button("Delete", () => { });
            delete.Width = SessionDeleteWidth;
            var row = new DockPanel();
            var choose = new CheckBox { IsChecked = sessionChosen.Contains(id), VerticalAlignment = VerticalAlignment.Center, Width = SessionBoxWidth };
            choose.IsCheckedChanged += (_, _) => ChooseSession(id, choose.IsChecked == true);
            ToolTip.SetTip(choose, "Choose for comparing");
            Avalonia.Automation.AutomationProperties.SetName(choose, $"Compare the session of {s.ShotDate}, {s.SheetName}");
            DockPanel.SetDock(choose, Dock.Left);
            DockPanel.SetDock(delete, Dock.Right);
            row.Children.Add(choose);
            row.Children.Add(delete);
            row.Children.Add(open);
            // Delete asks first: the button gives way to the question and its two answers.
            delete.Click += (_, _) =>
            {
                row.Children.Remove(delete);
                var confirm = Row(new TextBlock { Text = "Delete this session?", VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Warn } },
                    Button("Delete", () =>
                    {
                        sessions.Delete(id);
                        DiagnosticLog.Info("session.delete", ("session", id));
                        if (currentSession == id)
                        {
                            currentSession = null;
                        }

                        FillSessions();
                    }),
                    Button("Keep it", FillSessions));
                DockPanel.SetDock(confirm, Dock.Right);
                row.Children.Insert(0, confirm);
            };
            sessionRows.Children.Add(row);
        }
    }

    /// <summary>The Session records rows' texts, for the headless tests.</summary>
    internal IReadOnlyList<string> SessionRowTexts =>
        [.. sessionRows.Children.OfType<DockPanel>().Where(r => r.Children.OfType<Button>().Any(b => b.Classes.Contains(AppStyles.TableRow))).Select(r => string.Join(" | ", r.Children.OfType<Button>().Where(b => b.Classes.Contains(AppStyles.TableRow))
            .SelectMany(b => ((Grid)b.Content!).Children.OfType<TextBlock>()).Select(t => t.Text)))];

    /// <summary>Picks a rifle or load in the Session records filters, for the headless tests.</summary>
    internal void FilterSessions(string? rifle, string? load)
    {
        FillSessions();
        sessionRifle.SelectedItem = rifle ?? "Every rifle";
        sessionLoad.SelectedItem = load ?? "Every load";
        FillSessions();
    }

    /// <summary>The sheet crumb: back to the editor, every edit as it was left.</summary>
    internal void BackToEditor() => SetAnalysing(false);

    /// <summary>
    /// Show work, entry 105 section 6: shows or hides the work bar in whichever state is showing, and remembers it. The two toggles, one in
    /// each state's header, always agree.
    /// </summary>
    internal void SetShowWork(bool shown, bool remember = true)
    {
        workShown = shown;
        workBar.IsVisible = shown && destination == Destination.Analyse;
        showWorkEditor.IsChecked = showWorkAnalysis.IsChecked = shown;
        if (remember)
        {
            settingsStore.SaveShowWork(shown);
        }

        ShowWorkAttention();
    }

    /// <summary>
    /// DESIGN.md section 19: the trace is never the only place an error appears. The error itself is in the panel and the status line either
    /// way; with the bar hidden, Show work also says a stage failed or was degraded, in red or amber, so the detail behind it is not silent.
    /// </summary>
    private void ShowWorkAttention()
    {
        int failed = stages.Count(r => r.Status == StageStatus.Failed), degraded = stages.Count(r => r.Status == StageStatus.Degraded);
        string label = workShown || failed + degraded == 0
            ? "Show work"
            : failed > 0
                ? $"Show work: {failed} stage{(failed == 1 ? "" : "s")} failed"
                : $"Show work: {degraded} stage{(degraded == 1 ? "" : "s")} degraded";
        foreach (var toggle in new[] { showWorkEditor, showWorkAnalysis })
        {
            toggle.Content = label;
            toggle.Classes.Remove(AppStyles.Alert);
            toggle.Classes.Remove(AppStyles.Warn);
            if (!workShown && failed > 0)
            {
                toggle.Classes.Add(AppStyles.Alert);
            }
            else if (!workShown && degraded > 0)
            {
                toggle.Classes.Add(AppStyles.Warn);
            }
        }
    }

    /// <summary>Whether the work bar is showing, and what Show work says, for the headless tests.</summary>
    internal (bool Shown, string Label) WorkBar => (workBar.IsVisible, showWorkEditor.Content as string ?? "");

    /// <summary>
    /// A body's columns, entry 105 section 1: an optional left column, the centre, and a right column, with a splitter beside each side
    /// column. Each side column opens at the width a person last dragged it to, or the design's width, and cannot be dragged shut.
    /// </summary>
    private Grid Columns(string name, Control? left, Control centre, Control right)
    {
        var grid = new Grid();
        ColumnDefinition Side(string which, double fallback)
        {
            double width = Math.Clamp(settingsStore.LoadColumnWidth($"{name}.{which}") ?? fallback, SideMinimum, SideMaximum);
            return new ColumnDefinition(width, GridUnitType.Pixel) { MinWidth = SideMinimum, MaxWidth = SideMaximum };
        }

        void Splitter(int column, string which, ColumnDefinition side)
        {
            var splitter = new GridSplitter
            {
                Width = 5,
                ResizeDirection = GridResizeDirection.Columns,
                Cursor = new Cursor(StandardCursorType.SizeWestEast),
            };
            ToolTip.SetTip(splitter, "Drag to widen or narrow this column");
            splitter.DragCompleted += (_, _) => settingsStore.SaveColumnWidth($"{name}.{which}", side.ActualWidth);
            Grid.SetColumn(splitter, column);
            grid.Children.Add(splitter);
        }

        int at = 0;
        if (left is not null)
        {
            var leftColumn = Side("left", 300);
            grid.ColumnDefinitions.Add(leftColumn);
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);
            Splitter(1, "left", leftColumn);
            at = 2;
        }

        grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = CentreMinimum });
        Grid.SetColumn(centre, at);
        grid.Children.Add(centre);
        var rightColumn = Side("right", Tokens.RightColumnWidth);
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(rightColumn);
        Splitter(at + 1, "right", rightColumn);
        Grid.SetColumn(right, at + 2);
        grid.Children.Add(right);
        return grid;
    }

    private void SetAnalysing(bool on)
    {
        analysing = on;
        discardConfirm.IsVisible = false;
        Refresh();
    }

    /// <summary>Discard edits asks first: the button gives way to a question with the two answers.</summary>
    private void AskDiscard()
    {
        discardConfirm.IsVisible = true;
        discardButton.IsVisible = false;
    }

    private void ConfirmDiscard()
    {
        discardConfirm.IsVisible = false;
        if (detectedState is { } detected)
        {
            session.Restore(detected);
            status.Text = "Edits discarded: the marking is as detection left it. Undo brings the edits back.";
        }
    }

    /// <summary>Whether the window is in the analysis state, for the headless tests.</summary>
    internal bool Analysing => analysing;

    /// <summary>The composite plot, for the headless tests.</summary>
    internal CompositePlot Plot => plot;

    /// <summary>The amber banner naming decisions left unmade, or empty when there are none, for the headless tests.</summary>
    internal string UnsettledText => unsettledBanner.IsVisible ? unsettled.Text ?? "" : "";

    /// <summary>Every line of the banner, its "why" included, for the headless tests.</summary>
    internal IEnumerable<string> UnsettledLines => unsettledBanner.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The header pill's text in whichever state is showing, for the headless tests.</summary>
    internal string PillText => analysing ? registrationText.Text ?? "" : reviewCount.Text ?? "";

    /// <summary>Each judgement card's lines, verdict first, for the headless tests.</summary>
    internal IReadOnlyList<IReadOnlyList<string>> JudgementCards => [.. judgements.Children.OfType<Border>().Select(b => (IReadOnlyList<string>)[.. b.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "")])];

    /// <summary>The analysis state's right column's section headings, in order, for the headless tests.</summary>
    internal IEnumerable<string> FigureColumnHeadings =>
        analysisBody.GetLogicalDescendants().OfType<TextBlock>().Where(t => t.Classes.Contains(AppStyles.Section)).Select(t => t.Text ?? "");

    /// <summary>The shots the plot has picked, for the headless tests.</summary>
    internal IReadOnlySet<int> PlotSelection => plotSelection;

    /// <summary>
    /// Every shot as a row, NOTES-FROM-PLANNING.md entry 39 section 4: its number as the image shows it, its bull, and whether it is
    /// excluded or not a shot, with where it came from beside it in faint text (entry 46 section 3). A row selects its shot on the image, and carries the exclusion control, so excluding a flyer does not
    /// wait for the shot to be selected. At twenty-five shots finding one on the image is a hunt; finding it in the list is not.
    /// </summary>
    private void BuildShotList()
    {
        shotList.Children.Clear();
        var state = session.State;
        if (state.Shots.Count == 0)
        {
            shotList.Children.Add(Line("None yet."));
            return;
        }

        // NOTES-FROM-PLANNING.md entry 75: rows in the sheet's order, each named by its bull, with no per-detection index.
        foreach (var label in ShotLabels.For(state))
        {
            var shot = state.Find(label.ShotId)!;
            int id = shot.Id;
            // A named shot is its bull's label; a word, unassigned or not a shot, is followed by where the mark is, because the word alone
            // does not say which one; a plain group's shot is named by where it is.
            string name = label.Text is not { } named ? WhereOnTarget(state, shot.Image)
                : shot.NotAShot || shot.Bull is null ? $"{named}, {WhereOnTarget(state, shot.Image)}"
                : named;

            string text = string.Create(CultureInfo.InvariantCulture, $"{name}{(shot.Exclusion is { } e ? $", excluded as {e.InSentence()}" : "")}");
            var select = new Button
            {
                Content = new TextBlock
                {
                    Text = text,
                    FontFamily = Mono,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontWeight = id == canvas.Selected ? FontWeight.Bold : FontWeight.Normal,
                },
                Tag = id,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0),
            };
            ToolTip.SetTip(select, text);
            select.Click += (_, _) =>
            {
                canvas.Selected = id;
                Refresh();
            };

            // NOTES-FROM-PLANNING.md entry 73 section 6: the row fits the column, its text giving way first rather than its buttons being cut
            // off, and a detection that is not a shot can be taken out from here. "Not a shot" keeps the mark and its provenance and can be
            // undone from the same row; deleting stays in the selection panel.
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto") };
            AddCell(row, select, 0);
            AddCell(row, new TextBlock { Text = ProvenanceWord(shot.Provenance), VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Faint } }, 1);
            if (!shot.NotAShot)
            {
                AddCell(row, Button(shot.Exclusion is null ? "Exclude" : "Restore", () =>
                    session.SetExclusion(id, shot.Exclusion is null ? ChosenReason : null)), 2);
            }

            AddCell(row, Button(shot.NotAShot ? "It is a shot" : "Not a shot", () => session.SetNotAShot(id, !shot.NotAShot)), 3);
            shotList.Children.Add(row);
        }
    }

    /// <summary>
    /// The count line, NOTES-FROM-PLANNING.md entry 46 section 3: how many shots, and how many were detected, corrected after detection, or
    /// placed by hand, naming only the kinds present. It records where a human judgement entered the measurement: a group of nine detected
    /// shots and one of nine placed by hand deserve the same figures and a different amount of confidence, and only the application knows
    /// which it is showing.
    /// </summary>
    internal static string PlacedLine(int shots, int detected, int corrected, int byHand)
    {
        var parts = new List<string>();
        if (detected > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{detected} detected"));
        }

        if (corrected > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{corrected} corrected"));
        }

        if (byHand > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"{byHand} placed by hand"));
        }

        return string.Create(CultureInfo.InvariantCulture, $"{shots} {(shots == 1 ? "shot" : "shots")}: {string.Join(", ", parts)}");
    }

    /// <summary>A shot's provenance as the shot list shows it (entry 46 section 3).</summary>
    internal static string ProvenanceWord(ShotProvenance provenance) => provenance switch
    {
        ShotProvenance.Automatic => "detected",
        ShotProvenance.Corrected => "corrected",
        _ => "by hand",
    };

    private void BuildSelection()
    {
        // The reason picker is kept across rebuilds so its choice survives; it must leave its old row before joining a new one.
        if (exclusionReason.Parent is Panel previous)
        {
            previous.Children.Remove(exclusionReason);
        }

        selection.Children.Clear();
        if (canvas.Selected is not { } id || session.State.Find(id) is not { } shot)
        {
            selection.Children.Add(new TextBlock { Text = "None. Use Select and tap a shot.", TextWrapping = TextWrapping.Wrap });
            return;
        }

        // Entry 97 section 1: the concept's selected-detection panel, label and value rows and the provenance as a chip, teal where the software
        // found it on its own.
        selection.Children.Add(Readout("Shot", ShotLabel(id)));
        if (session.State.Scale is { } scale)
        {
            var at = scale.ToTarget(shot.Image);
            selection.Children.Add(Readout("Position", units.Length(at.X) + ", " + units.Length(at.Y)));
        }

        if (shot.MeasuredDiameterInches is { } measured)
        {
            selection.Children.Add(Readout("Diameter", units.Length(measured)));
        }

        if (shot.Size is { } size)
        {
            selection.Children.Add(Readout("Size", string.Create(CultureInfo.InvariantCulture, $"{size.Holes:0.00} holes")));
        }

        if (session.State.Assignment?.For(id) is { } detail)
        {
            selection.Children.Add(Readout("Margin", units.Length(detail.MarginInches)));
        }

        var chips = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, Tokens.Space4, 0, 0) };
        foreach (var provenance in new[] { ShotProvenance.Automatic, ShotProvenance.Corrected, ShotProvenance.Manual })
        {
            // The shot list's own words for the three provenances, so the panel and the list never name one thing two ways.
            string word = provenance switch { ShotProvenance.Automatic => "detected", ShotProvenance.Corrected => "corrected", _ => "by hand" };
            var chip = new Border { Child = new TextBlock { Text = word, FontSize = Tokens.SectionLabelSize, Classes = { provenance == shot.Provenance ? AppStyles.Good : AppStyles.Dim } }, Classes = { AppStyles.Chip } };
            if (provenance == shot.Provenance)
            {
                chip.Classes.Add(AppStyles.Good);
            }

            chips.Children.Add(chip);
        }

        selection.Children.Add(chips);
        if (shot.Exclusion is { } e)
        {
            selection.Children.Add(Line($"Excluded as {e.InSentence()}."));
        }
        selection.Children.Add(Row(exclusionReason, Button(shot.Exclusion is null ? "Exclude" : "Restore", () =>
            session.SetExclusion(id, shot.Exclusion is null ? ChosenReason : null))));
        selection.Children.Add(Row(
            Button(shot.NotAShot ? "It is a shot" : "Not a shot", () => session.SetNotAShot(id, !shot.NotAShot)),
            Button("Unassign", () => session.AssignBull(id, null)),
            Button("Delete", () =>
            {
                session.DeleteShot(id);
                canvas.Selected = null;
            })));
    }

    /// <summary>The review queue as it stands, for the headless tests.</summary>
    internal IReadOnlyList<ReviewItem> ReviewItems { get; private set; } = [];

    /// <summary>The item the editor is on, or null when nothing needs a decision.</summary>
    internal ReviewItem? CurrentReview => ReviewItems.FirstOrDefault(i => i.Key == currentReview);

    /// <summary>Every line of the review panel, for the headless tests.</summary>
    internal IEnumerable<string> ReviewText => review.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>
    /// The review panel: how many items still need a decision, the current one as a card with its choices, and the whole queue in order with
    /// each item's state. Discard edits puts back what detection found, as one step that can be undone.
    /// </summary>
    private void ShowReview(MarkingState state)
    {
        review.Children.Clear();
        ReviewItems = ReviewQueue.For(state, analyseSighters);
        int open = ReviewQueue.Open(ReviewItems);
        var current = ReviewItems.FirstOrDefault(i => i.Key == currentReview && !i.Resolved) ?? ReviewItems.FirstOrDefault(i => !i.Resolved);
        currentReview = current?.Key;

        // Entry 97 section 5's window rehearsal found this: straight after detection the first item was current and its shot was not
        // selected, so a bull typed for it went nowhere until the person clicked the shot. The item's shot is selected whenever nothing else is.
        if (canvas.Selected is null && current?.ShotId is { } shotOfItem)
        {
            canvas.Selected = shotOfItem;
        }
        int shots = CountedShots(state);
        var count = new TextBlock
        {
            Text = ReviewItems.Count == 0 ? (state.Assignment is null ? "Nothing detected to review." : "Nothing needs review.") : $"{open} of {shots} need review",
            FontFamily = Mono,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { open > 0 ? AppStyles.Warn : AppStyles.Secondary },
        };
        // Entry 103 section 1: Discard edits is in the header beside Accept and analyse, and asks first.
        if (detectedState is null || ReferenceEquals(state, detectedState))
        {
            discardConfirm.IsVisible = false;
        }

        discardButton.IsVisible = detectedState is not null && !ReferenceEquals(state, detectedState) && !discardConfirm.IsVisible;
        review.Children.Add(Row(count));
        if (current is not null)
        {
            // Entry 97 section 1: the card is amber because it is the thing that needs a person, and its first choice, the one Enter takes, is
            // the amber primary button, as the concept draws it. Red stays for what is wrong.
            var card = new StackPanel { Spacing = Tokens.Space8 };
            card.Children.Add(new TextBlock { Text = ReviewTitle(current.Kind), FontWeight = FontWeight.SemiBold, Classes = { AppStyles.Warn } });
            card.Children.Add(new TextBlock { Text = current.Sentence, TextWrapping = TextWrapping.Wrap });
            var choices = new WrapPanel();
            foreach (var choice in current.Choices)
            {
                var button = Button(choice.Label, () => Choose(current, choice));
                if (ReferenceEquals(choice, current.Choices[0]))
                {
                    button.Classes.Add(AppStyles.Primary);
                }

                choices.Children.Add(button);
            }

            card.Children.Add(choices);
            card.Children.Add(Line("Enter takes the first choice, Space moves to the next item, a bull's number then Enter reassigns the selected shot, N marks it not a shot."
                + (current.Choices.Any(c => c.Action == ReviewAction.SplitIntoTwo) ? " T takes this mark as two shots." : "")));
            review.Children.Add(new Border { Child = card, Classes = { AppStyles.ReviewCard } });
        }

        int n = 0;
        foreach (var item in ReviewItems)
        {
            n++;
            string state_ = item.Resolved ? "DONE" : item.Key == currentReview ? "NOW" : "NEXT";
            var word = new TextBlock { Text = state_, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.StatusWord, item.Resolved ? AppStyles.Good : AppStyles.Warn } };
            var row = new DockPanel();
            DockPanel.SetDock(word, Dock.Right);
            row.Children.Add(word);
            row.Children.Add(new TextBlock { Text = $"{n}.  {ReviewTitle(item.Kind)}{(item.ShotId is { } id ? ", shot " + ShotLabel(id) : item.Bull is { } b ? ", bull " + BullLabel(b) : "")}", TextWrapping = TextWrapping.Wrap });
            var line = new Button
            {
                Content = row,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0),
                Opacity = item.Resolved ? 0.6 : 1,
            };
            line.Click += (_, _) => FocusReview(item);
            review.Children.Add(line);
        }
    }

    private static string ReviewTitle(ReviewKind kind) => kind switch
    {
        ReviewKind.Contested => "Contested assignment",
        ReviewKind.Oversized => "Possibly two holes",
        ReviewKind.Doubled => "Two shots on one bull",
        ReviewKind.Unassigned => "No bull",
        ReviewKind.Count => "Count differs from rounds fired",
        _ => "Refused candidate",
    };

    /// <summary>The timeline's records, for the headless tests.</summary>
    internal IReadOnlyList<StageRecord> Stages => stages;

    /// <summary>The chosen stage's detail lines, for the headless tests.</summary>
    internal IEnumerable<string> StageText => stageDetail.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    private void ClearStages()
    {
        stages.Clear();
        stageButtons.Children.Clear();
        stageDetail.Children.Clear();
        stageSummary.Text = "Stages appear here as each one lands.";
        ShowWorkAttention();
        canvas.StageRejections = [];
        canvas.Highlight = null;
        showingStage = true;
        stageSlider.Maximum = 0;
        stageSlider.Value = 0;
        showingStage = false;
    }

    /// <summary>Puts a whole analysis on the timeline at once, as a test or a reopened trace does; a live run adds each stage as it lands.</summary>
    internal void ShowTrace(IEnumerable<StageRecord> records)
    {
        ClearStages();
        foreach (var record in records)
        {
            AddStage(record);
        }
    }

    /// <summary>One stage lands: its button in the colour of its outcome, and the timeline moves to it, so a live run plays out as it happens.</summary>
    internal void AddStage(StageRecord record)
    {
        stages.Add(record);
        int index = stages.Count - 1;
        var word = new TextBlock
        {
            Text = record.Stage,
            FontFamily = Mono,
            FontSize = Tokens.SecondarySize,
            Classes = { record.Status switch { StageStatus.Ok => AppStyles.Good, StageStatus.Degraded => AppStyles.Warn, _ => AppStyles.Alert } },
        };
        var button = new Button { Content = word };
        button.Click += (_, _) => ShowStage(index);
        stageButtons.Children.Add(button);
        ShowWorkAttention();
        showingStage = true;
        stageSlider.Maximum = index;
        showingStage = false;
        ShowStage(index);
    }

    /// <summary>
    /// Scrubs to one stage: its outcome and summary on the timeline, its parameters, decisions and rejections beside it, and its rejections on
    /// the image where the registration can place them. A rejection with a position is a button that finds it.
    /// </summary>
    internal void ShowStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            return;
        }

        var record = stages[index];
        showingStage = true;
        stageSlider.Value = index;
        showingStage = false;
        string outcome = record.Status switch { StageStatus.Ok => "ok", StageStatus.Degraded => "degraded", _ => "FAILED" };
        stageSummary.Text = string.Create(CultureInfo.InvariantCulture, $"{index + 1} of {stages.Count}: {record.Stage}, {outcome}, {record.DurationMs} ms. {record.Summary}");
        stageDetail.Children.Clear();
        foreach (var parameter in record.Parameters)
        {
            stageDetail.Children.Add(Readout(parameter.Name, parameter.Value, Tokens.SecondarySize));
        }

        foreach (var metric in record.Metrics)
        {
            stageDetail.Children.Add(Readout(metric.Name, string.Create(CultureInfo.InvariantCulture, $"{metric.Value:0.#####} {metric.Unit}"), Tokens.SecondarySize));
        }

        foreach (var decision in record.Decisions)
        {
            stageDetail.Children.Add(Line($"Decided {decision.What}: {decision.Chosen}, because {decision.Because}{(decision.Alternatives.Count > 0 ? " (not " + string.Join(", ", decision.Alternatives) + ")" : "")}."));
        }

        foreach (string detail in record.Details)
        {
            stageDetail.Children.Add(Detail(detail));
        }

        var placed = new List<PointD>();
        foreach (var rejection in record.Rejections)
        {
            string text = $"Rejected {rejection.What}: {rejection.Why}";
            if (ImageOf(rejection) is { } at)
            {
                placed.Add(at);
                var find = new Button { Content = text + ". Find it", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
                find.Click += (_, _) => HighlightRejection(at, text);
                stageDetail.Children.Add(find);
            }
            else
            {
                stageDetail.Children.Add(Line(text + "."));
            }
        }

        if (stageDetail.Children.Count == 0)
        {
            stageDetail.Children.Add(Line("This stage recorded no parameters, decisions or rejections."));
        }

        canvas.StageRejections = placed;
        canvas.Highlight = null;
        ShowStagePicture(record);
        canvas.InvalidateVisual();
    }

    /// <summary>
    /// A stage's own picture, entry 98 section 5: the markers light up at the fiducial stage, each corner is ringed by its residual at the
    /// registration, twenty times true size so a tenth of a millimetre can be seen, and at the difference stage the photograph gives way to
    /// the residual, where the printed artwork has vanished and the holes emerge. Stages with no picture show the photograph.
    /// <para>
    /// The picture comes from the stage's own record, which carries it when it files (entry 101 section 5), so during a live run each picture
    /// appears as its stage lands rather than when the analysis finishes. A batch run's records carry none.
    /// </para>
    /// </summary>
    private void ShowStagePicture(StageRecord record)
    {
        (canvas.StageImage as IDisposable)?.Dispose();
        canvas.StageImage = null;
        canvas.StageMarkers = [];
        canvas.StageCorners = [];
        if (record.Artefact is MarkerArtefact markers)
        {
            canvas.StageMarkers = [.. markers.Markers];
        }
        else if (record.Artefact is CornerArtefact corners)
        {
            var scale = new SheetReference(corners.Mapping, "");
            canvas.StageCorners = [.. corners.Corners.Select(c => (c.Image, 20 * c.Error / 254 * HoleSize.PixelsPerInch(scale, c.Image), c.Inlier))];
        }
        else if (record.Artefact is ResidualArtefact { Residual: var residual })
        {
            // Shown as paper and ink: no difference is white, a strong one dark, so the holes emerge on a blank sheet.
            var inverted = residual.Pixels.Select(v => (byte)(255 - v)).ToArray();
            using var mat = OpenCvSharp.Mat.FromPixelData(residual.Height, residual.Width, OpenCvSharp.MatType.CV_8UC1, inverted);
            OpenCvSharp.Cv2.ImEncode(".png", mat, out byte[] png);
            using var stream = new MemoryStream(png);
            canvas.StageImage = new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }

    /// <summary>The picture the timeline is showing, for the headless tests: which of the three a stage drew, if any.</summary>
    internal string StagePicture => canvas.StageImage is not null ? "residual" : canvas.StageMarkers.Count > 0 ? "markers" : canvas.StageCorners.Count > 0 ? "corners" : "none";

    /// <summary>Where a rejection sits on the image, through the sheet's own registration, or null where it has no page position or there is no registration.</summary>
    private PointD? ImageOf(Rejection rejection) =>
        rejection is { XInches: { } x, YInches: { } y } && session.State.Scale is SheetReference sheet
            ? sheet.Mapping.ToImage(new PointD(x * 254, y * 254))
            : null;

    /// <summary>Clicking a rejection finds it on the image: centred, ringed in amber, and named in the status line.</summary>
    internal void HighlightRejection(PointD image, string text)
    {
        canvas.Highlight = image;
        canvas.CentreOn(image);
        canvas.InvalidateVisual();
        status.Text = text + ".";
    }

    /// <summary>The record book's file, beside the settings file.</summary>
    private string RecordsPath => settingsStore.RecordsPath;

    /// <summary>The three pickers, filled from the book and showing what the marking names.</summary>
    private void ShowEquipment(MarkingState state)
    {
        showingEquipment = true;
        rifleChoice.ItemsSource = new[] { "No rifle" }.Concat(book.Rifles.Select(r => $"{r.Name}, {r.DescribeClick()}")).ToList();
        barrelChoice.ItemsSource = new[] { "No barrel" }.Concat(book.Barrels.Select(b => string.Create(CultureInfo.InvariantCulture, $"{b.Name}, {b.Rounds} rounds"))).ToList();
        loadChoice.ItemsSource = new[] { "No load" }.Concat(book.Loads.Select(l => l.Name)).ToList();
        rifleChoice.SelectedIndex = state.Rifle is { } rifle ? book.Rifles.FindIndex(r => string.Equals(r.Name, rifle.Name, StringComparison.OrdinalIgnoreCase)) + 1 : 0;
        barrelChoice.SelectedIndex = book.Barrels.FindIndex(b => string.Equals(b.Name, state.Barrel, StringComparison.OrdinalIgnoreCase)) + 1;
        loadChoice.SelectedIndex = book.Loads.FindIndex(l => string.Equals(l.Name, state.Load, StringComparison.OrdinalIgnoreCase)) + 1;
        showingEquipment = false;
    }

    private void EquipmentChosen()
    {
        if (showingEquipment)
        {
            return;
        }

        var rifle = rifleChoice.SelectedIndex > 0 ? book.Rifles[rifleChoice.SelectedIndex - 1] : null;
        var barrel = barrelChoice.SelectedIndex > 0 ? book.Barrels[barrelChoice.SelectedIndex - 1].Name : null;
        var load = loadChoice.SelectedIndex > 0 ? book.Loads[loadChoice.SelectedIndex - 1].Name : null;
        session.SetEquipment(rifle, barrel, load);
    }

    /// <summary>Adds a rifle, barrel or load to the book from the two fields, and keeps the book.</summary>
    internal void AddRecord(string kind)
    {
        string name = newName.Text?.Trim() ?? "";
        if (name.Length == 0)
        {
            status.Text = "Give the " + kind + " a name first.";
            return;
        }

        switch (kind)
        {
            case "rifle":
                string click = (string)newClick.SelectedItem!;
                double value = double.Parse(click.Split(' ')[0], CultureInfo.InvariantCulture);
                book = book.With(new Rifle(name, value, click.EndsWith("mil", StringComparison.Ordinal) ? AngularUnit.Mrad : AngularUnit.Moa));
                break;
            case "barrel":
                book = book.With(new Barrel(name, session.State.Rifle?.Name, int.TryParse(newDetail.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rounds) ? Math.Max(0, rounds) : 0));
                break;
            default:
                book = book.With(new Load(name, string.IsNullOrWhiteSpace(newDetail.Text) ? null : newDetail.Text.Trim()));
                break;
        }

        SaveBook();
        status.Text = $"Added the {kind} {name}.";
        Refresh();
    }

    /// <summary>A barrel's count grows when a person says so, never on its own, so reopening a marking cannot count a sheet twice.</summary>
    private void AddSheetToBarrel()
    {
        if (session.State.Barrel is not { } barrel)
        {
            status.Text = "Choose the barrel first.";
            return;
        }

        int shots = session.State.Shots.Count(s => s.IsShot);
        book = book.Fired(barrel, shots);
        SaveBook();
        status.Text = string.Create(CultureInfo.InvariantCulture, $"Added {shots} rounds to {barrel}.");
        Refresh();
    }

    private void SaveBook()
    {
        try
        {
            if (sessions is null)
            {
                status.Text = "The session database could not be opened, so the records last until GroupLab closes.";
                return;
            }

            sessions.SaveBook(book);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            status.Text = "The records could not be saved to the database (" + ex.Message + "), so they last until GroupLab closes.";
        }
    }

    /// <summary>Entry 95 section 2: the rounds fired, which the review queue checks the marks against.</summary>
    internal void SetRoundsFiredFromBox()
    {
        if (int.TryParse(roundsFired.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rounds) && rounds > 0)
        {
            session.SetExpectedShots(rounds);
            status.Text = $"Checking the marks against {rounds} rounds fired.";
        }
        else
        {
            status.Text = "Enter the number of rounds fired as a whole number.";
        }
    }

    private string BullLabel(int index) => session.State.Bulls.FirstOrDefault(b => b.Index == index)?.Label ?? index.ToString(CultureInfo.InvariantCulture);

    /// <summary>Takes the marking as it stands as what detection left, the state Discard edits returns to.</summary>
    internal void RememberDetected()
    {
        detectedState = session.State;
        Refresh();
    }

    /// <summary>Carries out a review choice and moves to the next item that still needs one.</summary>
    internal void Choose(ReviewItem item, ReviewChoice choice)
    {
        DiagnosticLog.Info("review.choose", ("kind", item.Kind.ToString()), ("action", choice.Action.ToString()));
        var shot = ReviewQueue.Apply(session, item, choice);
        status.Text = $"{ReviewTitle(item.Kind)}: {choice.Label}.";
        canvas.Selected = shot ?? canvas.Selected;
        NextReview();
    }

    /// <summary>Makes an item current, selects its shot and brings it to the middle of the view.</summary>
    internal void FocusReview(ReviewItem item)
    {
        currentReview = item.Key;
        canvas.Selected = item.ShotId ?? canvas.Selected;
        canvas.CentreOn(item.Image);
        Refresh();
    }

    /// <summary>The next item that still needs a decision after the current one, wrapping round, as Space does.</summary>
    internal void NextReview()
    {
        var items = ReviewQueue.For(session.State, analyseSighters);
        var open = items.Where(i => !i.Resolved).ToList();
        if (open.Count == 0)
        {
            currentReview = null;
            Refresh();
            status.Text = items.Count == 0 ? status.Text : "Every review item is decided.";
            return;
        }

        int at = items.ToList().FindIndex(i => i.Key == currentReview);
        var next = items.Skip(at + 1).FirstOrDefault(i => !i.Resolved && i.Key != currentReview) ?? open[0];
        FocusReview(next);
    }

    /// <summary>
    /// The editor's keys, DESIGN.md section 13's keyboard-driven verification: Space for the next item, Enter for the current item's first
    /// choice, a bull's label typed and then Enter to reassign the selected shot, N for not a shot, Escape to clear what was typed. They are
    /// taken before a focused button sees them, so Space never presses the last button clicked.
    /// </summary>
    internal void OnReviewKey(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox || e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            return;
        }

        char? typed = e.Key switch
        {
            >= Key.D0 and <= Key.D9 => (char)('0' + (e.Key - Key.D0)),
            >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (e.Key - Key.NumPad0)),
            Key.S => 'S',
            _ => null,
        };
        if (typed is { } c)
        {
            bullTyped += c;
            status.Text = canvas.Selected is { } selected
                ? $"Bull {bullTyped}: Enter puts shot {ShotLabel(selected)} on it, Escape clears."
                : $"Bull {bullTyped}: select a shot, then Enter puts it on that bull.";
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Back when bullTyped.Length > 0:
                bullTyped = bullTyped[..^1];
                break;
            case Key.Escape when bullTyped.Length > 0:
                bullTyped = "";
                status.Text = "Cleared.";
                break;
            case Key.Enter when bullTyped.Length > 0:
                AssignTyped();
                break;
            case Key.Enter when CurrentReview is { Choices.Count: > 0 } item:
                Choose(item, item.Choices[0]);
                break;
            case Key.Space:
                NextReview();
                break;
            // Entry 94 section 4: the one item that used to need the mouse. T takes an oversized mark as the two shots the detector says it is.
            case Key.T when CurrentReview is { } two && two.Choices.FirstOrDefault(c => c.Action == ReviewAction.SplitIntoTwo) is { } split:
                Choose(two, split);
                break;
            case Key.N when canvas.Selected is { } id:
                session.SetNotAShot(id, true);
                status.Text = $"Shot {ShotLabel(id)} marked not a shot.";
                NextReview();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void AssignTyped()
    {
        string label = bullTyped;
        bullTyped = "";
        if (canvas.Selected is not { } id)
        {
            status.Text = "Select a shot first, then type its bull and press Enter.";
            return;
        }

        if (session.State.Bulls.FirstOrDefault(b => string.Equals(b.Label, label, StringComparison.OrdinalIgnoreCase)) is not { } bull)
        {
            status.Text = $"There is no bull {label} on this sheet.";
            return;
        }

        string was = ShotLabel(id);
        session.AssignBull(id, bull.Index);
        DiagnosticLog.Info("review.typed", ("bull", bull.Label));
        status.Text = $"Shot {was} is now on bull {bull.Label}.";
        NextReview();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Source is TextBox)
        {
            return;
        }

        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        switch (e.Key)
        {
            case Key.Z when control:
                session.Undo();
                break;
            case Key.Y when control:
                session.Redo();
                break;
            case Key.P:
                SetTool(MarkingTool.Pan);
                break;
            case Key.L:
                SetTool(MarkingTool.Length);
                break;
            case Key.R:
                SetTool(MarkingTool.Rectangle);
                break;
            case Key.A:
                SetTool(MarkingTool.Aim);
                break;
            case Key.I:
                SetTool(MarkingTool.Impact);
                break;
            case Key.V:
                SetTool(MarkingTool.Select);
                break;
            case Key.OemOpenBrackets:
                session.Rotate(-1);
                break;
            case Key.OemCloseBrackets:
                session.Rotate(1);
                break;
            case Key.Delete when canvas.Selected is { } id:
                session.DeleteShot(id);
                canvas.Selected = null;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private static Button Button(string label, Action action)
    {
        var button = new Button { Content = label, Margin = new Thickness(2) };
        button.Click += (_, _) => action();
        return button;
    }

    private static Button Button(string label, Func<Task> action)
    {
        var button = new Button { Content = label, Margin = new Thickness(2) };
        button.Click += async (_, _) => await action();
        return button;
    }

    private static void AddCell(Grid grid, Control child, int column)
    {
        Grid.SetColumn(child, column);
        child.Margin = new Thickness(column == 0 ? 0 : 6, 0, 0, 0);
        grid.Children.Add(child);
    }

    /// <summary>A row of controls that wraps onto a second line rather than running past its column (entry 105 section 1).</summary>
    private static WrapPanel Row(params Control[] children)
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 4 };
        foreach (var child in children)
        {
            row.Children.Add(child);
        }

        return row;
    }

    /// <summary>A section heading with a hairline above it, entry 105 section 2, where a column's hierarchy rests on its headings.</summary>
    private static Border Ruled(string text) => new()
    {
        Child = Heading(text),
        BorderThickness = new Thickness(0, 1, 0, 0),
        Padding = new Thickness(0, Tokens.Space4, 0, 0),
        Classes = { AppStyles.Ruled },
    };

    /// <summary>A section heading, entry 109 section 1 principle 2: the heading style, in sentence case, where entry 42 set dim capitals.</summary>
    private static TextBlock Heading(string text) => new() { Text = text, Margin = new Thickness(0, Tokens.Space8, 0, 0), Classes = { AppStyles.Section } };

    /// <summary>What a person follows and how often, entry 119 sections 4.2 and 4.5. Kept in memory until the settings file carries it.</summary>
    private UpdatePreferences updates = UpdatePreferences.Default(AppInfo.Build.Train == UpdateTrain.Development ? UpdateTrain.Nightly : AppInfo.Build.Train);

    /// <summary>The train a build with no train of its own follows: nightly, which is the only one with builds on it.</summary>
    private static UpdateTrain OwnTrain => AppInfo.Build.Train == UpdateTrain.Development ? UpdateTrain.Nightly : AppInfo.Build.Train;

    private readonly TextBlock updateState = new() { TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    /// <summary>The one line above the train choice, which says what this build is on and what the other trains are.</summary>
    private static string UpdateTrainHelp() =>
        AppInfo.Build.IsDevelopment
            ? "This is a development build, so it does not update itself. A build from the nightly train does."
            : "Release and Beta are " + UpdateTrains.NotAvailableYet.ToLowerInvariant() + ". Nightly is every change that passes the tests, and may be broken.";

    private static string TrainLabel(UpdateTrain train) => train.IsAvailable() ? train.Words() : train.Words() + " (" + UpdateTrains.NotAvailableYet.ToLowerInvariant() + ")";

    /// <summary>What the settings page is showing about updates, for the headless tests.</summary>
    internal string UpdateStateText => updateState.Text ?? "";

    /// <summary>What a person has chosen about updates, for the headless tests.</summary>
    internal UpdatePreferences UpdatePreferencesNow => updates;

    /// <summary>Opens a page in whatever the system uses for one. Nothing in GroupLab handles a payment or shows a page of its own.</summary>
    private void OpenInTheBrowser(string address)
    {
        try
        {
            // Entry 122: one way out of the process, which a test replaces with a recorder. This used to start the browser itself, and the
            // control walk of entry 117 opened tabs on Alan's machine every time it ran.
            TheOutsideWorld.Current.OpenAddress(address);
            DiagnosticLog.Info("outside.open", ("what", "address"));
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            status.Text = "That page could not be opened here: " + address;
        }
    }

    private static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    /// <summary>
    /// What this build is, in one line a tester can select and paste: the version, the commit it was made from, and the configuration.
    /// NOTES-FROM-PLANNING.md entry 119 section 4. A build made outside a repository has no commit, and says so rather than inventing one.
    /// </summary>
    internal static string BuildLine() => AppInfo.Build.Line;

    /// <summary>The text of the statistics panel, for the headless tests.</summary>
    internal IEnumerable<string> StatisticsText => statistics.GetLogicalDescendants().Concat(flags.GetLogicalDescendants()).OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The zero correction section's lines, for the headless tests (NOTES-FROM-PLANNING.md entries 91 and 92).</summary>
    internal IEnumerable<string> ZeroText => zeroPanel.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>The breadcrumb header's line, for the headless tests (entry 93 section 2).</summary>
    internal string BreadcrumbText => breadcrumb.Text ?? "";

    /// <summary>
    /// One figure in the monospace with tabular figures DESIGN.md section 19 asks for, and beneath it, smaller, its interval labelled
    /// with the coverage it actually has (NOTES-FROM-PLANNING.md entry 24 section 1), then the figure without exclusions when there
    /// are any. Every line wraps, so the largest type cannot clip at the panel's edge (entry 24 section 2). Extreme spread is drawn
    /// smaller and dimmer: present, and visibly subordinate.
    /// </summary>
    private Control Figure(string name, ReportedEstimate all, GroupFigures? reduced, Func<GroupFigures, ReportedEstimate?> pick, double size, FontWeight weight, bool subordinate = false, bool interval = true)
    {
        // Entry 105 section 2: the value alone beside its label, and the angular conversion on the line beneath with the interval. At the lead
        // size 372 pixels do not hold a number and two units, and the unit wrapped onto a line of its own below the label.
        var column = new StackPanel { Spacing = 0 };
        column.Children.Add(Readout(name, units.Length(all.Value), size, weight, subordinate));
        foreach (string line in FigureDetails(all, reduced, pick, interval))
        {
            column.Children.Add(Detail(line));
        }

        return column;
    }

    /// <summary>
    /// The lines beneath a figure: its angular value and its interval, then the figure without exclusions when there are any. The report
    /// prints these same lines (NOTES-FROM-PLANNING.md entry 112 section 2), so paper can never say more than the screen.
    /// </summary>
    private List<string> FigureDetails(ReportedEstimate all, GroupFigures? reduced, Func<GroupFigures, ReportedEstimate?> pick, bool interval)
    {
        double? distance = session.State.ShotDistanceInches;
        string Interval(ReportedEstimate e) => e is { Lower: { } lower, Upper: { } upper, Coverage: { } coverage }
            ? string.Create(CultureInfo.InvariantCulture, $"{100 * coverage:0.0}% interval {units.Number(lower)} to {units.Length(upper)}")
            : $"no interval: {e.IntervalUnavailable}";

        var lines = new List<string>();
        string? angle = units.AngleText(all.Value, distance);
        if (interval || angle is not null)
        {
            lines.Add(string.Join("  \u00b7  ", new[] { angle, interval ? Interval(all) : null }.Where(t => t is not null)));
        }

        if (reduced is not null)
        {
            lines.Add(pick(reduced) is { } r
                ? $"without exclusions: {units.Length(r.Value)}, {Interval(r)}"
                : "without exclusions: " + reduced.DispersionWithheld);
        }

        return lines;
    }

    /// <summary>The lines beneath CEP 90: CEP 50 and 95, and all three without exclusions when there are any (STATISTICS.md section 10).</summary>
    private List<string> CepDetails(GroupFigures all, GroupFigures? reduced)
    {
        var lines = new List<string> { $"CEP 50 {units.Length(all.Cep50!.Value)}  \u00b7  CEP 95 {units.Length(all.Cep95!.Value)}" };
        if (reduced is not null)
        {
            lines.Add(reduced is { Cep90: { } r90, Cep50: { } r50, Cep95: { } r95 }
                ? $"without exclusions: CEP 90 {units.Length(r90.Value)}, CEP 50 {units.Length(r50.Value)}, CEP 95 {units.Length(r95.Value)}"
                : "without exclusions: " + (reduced.DispersionWithheld ?? "no CEP"));
        }

        return lines;
    }

    private const string CepWhy = "from sigma under the circular normal model";

    private string? SizeValue(GroupFigures f) => f is { Width: { } width, Height: { } height } ? $"{units.Number(width)} \u00d7 {units.Length(height)}" : null;

    /// <summary>The lines beneath width by height: the two standard deviations, and the same without exclusions when there are any.</summary>
    private List<string> SizeDetails(GroupFigures all, GroupFigures? reduced)
    {
        string Sd(GroupFigures f) => f is { SdX: { } sdX, SdY: { } sdY } ? $"sd across {units.Length(sdX)}  \u00b7  sd up and down {units.Length(sdY)}" : "";
        var lines = new List<string> { Sd(all) };
        if (reduced is not null)
        {
            lines.Add(SizeValue(reduced) is { } size
                ? $"without exclusions: {size}, {Sd(reduced).Replace("  \u00b7  ", ", ", StringComparison.Ordinal)}"
                : "without exclusions: " + (reduced.DispersionWithheld ?? "no width or height"));
        }

        return lines;
    }

    /// <summary>The More figures disclosure's lines, which the report prints with the reasoning on its second page.</summary>
    private List<string> MoreFigureLines(MarkingState state, GroupFigures all)
    {
        var lines = new List<string>
        {
            all.ExtremeSpread is { Lower: { } esLower, Upper: { } esUpper, Coverage: { } esCoverage }
                ? string.Create(CultureInfo.InvariantCulture, $"Extreme spread is centre to centre, and its {100 * esCoverage:0.0} percent interval runs {units.Number(esLower)} to {units.Length(esUpper)}.")
                : $"Extreme spread has no interval: {all.ExtremeSpread!.IntervalUnavailable}.",
            all.ExtremeSpreadEdgeToEdge is { } edgeToEdge
                ? $"Edge to edge, across the outsides of the holes: {units.Length(edgeToEdge)}{(units.AngleText(edgeToEdge, state.ShotDistanceInches) is { } angle ? ", " + angle : "")}, which is centre to centre plus one {units.Length(state.Calibre!.DiameterInches)} bullet."
                : $"Edge to edge: {all.ExtremeSpreadEdgeToEdgeUnavailable}.",
        };
        if (state.ShotDistanceInches is null)
        {
            lines.Add("Angular figures need the shot distance.");
        }

        if (all.Shots < GroupAnalysis.SmallGroupShots && all.TrueSizeRange is { } range)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture,
                $"From {all.Shots} shots the true group size could be anywhere from {range.Lower:0.00} to {range.Upper:0.00} times what they measure (STATISTICS.md section 9.1)."));
        }

        return lines;
    }

    /// <summary>
    /// One readout, NOTES-FROM-PLANNING.md entry 92 section 3 and entry 93 section 2: the label on the left and the value on the right, as
    /// the concept's selected-detection panel has it. Three headline figures with two lines of interval each was nine lines of prose before
    /// a reader reached anything else.
    /// </summary>
    private static Control Readout(string label, string value, double size = Tokens.BodySize, FontWeight weight = FontWeight.Normal, bool subordinate = false, bool labelAtTop = false)
    {
        // Entry 111 section 3: where a value can wrap, its label sits at the top of the row, beside the value's first line, not under its last.
        var name = new TextBlock { Text = label, VerticalAlignment = labelAtTop ? VerticalAlignment.Top : VerticalAlignment.Bottom, Margin = new Thickness(0, 0, Tokens.Space8, 0), Classes = { AppStyles.Label } };
        var figure = new TextBlock
        {
            Text = value,
            FontFamily = Mono,
            FontSize = size,
            FontWeight = weight,
            LetterSpacing = subordinate ? 0 : Tokens.FigureSpacing,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextAlignment = TextAlignment.Right,
            TextWrapping = TextWrapping.Wrap,
        };
        if (subordinate)
        {
            figure.Classes.Add(AppStyles.Dim);
        }

        var row = new DockPanel();
        DockPanel.SetDock(name, Dock.Left);
        row.Children.Add(name);
        row.Children.Add(figure);
        return row;
    }

    /// <summary>One key drawn as a key: a bordered box with the letter in mono, as the concept shows its hints.</summary>
    private static Control Keycap(string key) => new Border
    {
        Child = new TextBlock { Text = key, Classes = { AppStyles.KeycapText } },
        VerticalAlignment = VerticalAlignment.Center,
        Classes = { AppStyles.Keycap },
    };

    /// <summary>
    /// The left rail, NOTES-FROM-PLANNING.md entry 93 sections 2 and 4, and entry 109 section 2. The concept's rail implies five destinations.
    /// The marking screen is the first; the print screen is built, so the Print slot opens it; the target library, session records and reports
    /// say which phase builds them rather than opening an empty screen. The gear at the foot, the usual place, opens the settings.
    /// </summary>
    private Control Rail()
    {
        var top = new StackPanel();
        // Entry 105 section 4: the mark in the rail's top slot, where a plain symbol stood in.
        railHere = new Button { Content = new BrandMark { Height = 20, Width = 20 }, Classes = { AppStyles.RailButton } };
        railHere.Click += (_, _) =>
        {
            if (destination != Destination.Analyse)
            {
                Go(Destination.Analyse);
            }
            else
            {
                status.Text = "You are on the analysis screen.";
            }
        };
        ToolTip.SetTip(railHere, "Analyse");
        top.Children.Add(railHere);
        foreach (var (icon, tip, action) in new (string, string, Action)[]
        {
            (Icons.Library, "Target library", () => Go(destination == Destination.Library ? Destination.Analyse : Destination.Library)),
            (Icons.Print, "Print a target", () => OpenPrint(null, design: false)),
            (Icons.Records, "Session records", () => Go(destination == Destination.Sessions ? Destination.Analyse : Destination.Sessions)),
            // Entry 112 section 4: the dope table and the solver's fields on the records have no place in the concept's rail, so a slot of their own.
            (Icons.Ballistics, "Ballistics", () => Go(destination == Destination.Ballistics ? Destination.Analyse : Destination.Ballistics)),
            // Entry 113 section 2: the chart slot is the concept's Compare loads. A report is written from its analysis, by its Report button.
            (Icons.Reports, "Compare loads", () => Go(destination == Destination.Compare ? Destination.Analyse : Destination.Compare)),
        })
        {
            var button = new Button { Content = Icons.Draw(icon), Classes = { AppStyles.RailButton } };
            button.Click += (_, _) => action();
            ToolTip.SetTip(button, tip);
            top.Children.Add(button);
            if (icon == Icons.Records)
            {
                railSessions = button;
            }
            else if (icon == Icons.Library)
            {
                railLibrary = button;
            }
            else if (icon == Icons.Ballistics)
            {
                railBallistics = button;
            }
            else if (icon == Icons.Reports)
            {
                railCompare = button;
            }
        }

        railSettings = new Button { Content = Icons.Draw(Icons.Settings), Classes = { AppStyles.RailButton } };
        railSettings.Click += (_, _) => ShowSettings(!showingSettings);
        ToolTip.SetTip(railSettings, "Settings");
        var rail = new DockPanel { Width = 52, Margin = new Thickness(0, 0, 0, Tokens.Space8), LastChildFill = false };
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(railSettings, Dock.Bottom);
        rail.Children.Add(top);
        rail.Children.Add(railSettings);
        return new Border { Child = rail, Classes = { AppStyles.Rail } };
    }

    /// <summary>Shows or leaves the settings screen, entry 109 section 2.</summary>
    internal void ShowSettings(bool on = true) => Go(on ? Destination.Settings : Destination.Analyse);

    /// <summary>Shows or leaves the Session records screen, entry 112 section 1.</summary>
    internal void ShowSessions(bool on = true) => Go(on ? Destination.Sessions : Destination.Analyse);

    /// <summary>Goes to one of the rail's destinations.</summary>
    private void Go(Destination to)
    {
        destination = to;
        DiagnosticLog.Info("destination.show", ("destination", to.ToString()));
        if (to == Destination.Sessions)
        {
            FillSessions();
        }
        else if (to == Destination.Library)
        {
            FillLibrary();

            // Entry 120 section 10.3: the status line used to carry the marking screen's words about buttons this screen does not have.
            status.Text = LibraryStatus;
        }
        else if (to == Destination.Ballistics)
        {
            FillBallistics();
        }
        else if (to == Destination.Compare)
        {
            FillCompare();
        }

        Refresh();
    }

    /// <summary>Whether the Session records screen is showing, for the headless tests.</summary>
    internal bool ShowingSessions => destination == Destination.Sessions;

    /// <summary>Whether the settings screen is showing, for the headless tests.</summary>
    internal bool ShowingSettings => showingSettings;

    /// <summary>
    /// The settings screen, entry 109 section 2: the units every figure is shown in, the theme, the log's detail and where it is, reporting a
    /// problem, and the crash records not yet dealt with. Each is remembered as it was before it moved here.
    /// </summary>
    private Control BuildSettings(AppSettingsStore settings)
    {
        var column = new StackPanel { Margin = new Thickness(Tokens.Space24, Tokens.Space20), Spacing = Tokens.Space12, MaxWidth = 640, HorizontalAlignment = HorizontalAlignment.Left };
        column.Children.Add(new TextBlock { Text = "Settings", Classes = { AppStyles.Title } });

        // Entry 25 section 1: one application-wide unit setting on three axes, which every figure obeys and no stored value does.
        column.Children.Add(Ruled("Units"));
        column.Children.Add(Line("Every figure is shown in these units. Nothing stored changes."));
        var unitGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("160,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), RowSpacing = Tokens.Space8 };
        int row = 0;
        foreach (var (name, combo) in new[] { ("Lengths", linearUnit), ("Angles", angularUnit), ("Distances", distanceUnit) })
        {
            var label = FieldLabel(name);
            label.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(label, row);
            Grid.SetRow(combo, row);
            Grid.SetColumn(combo, 1);
            unitGrid.Children.Add(label);
            unitGrid.Children.Add(combo);
            combo.SelectionChanged += (_, _) => UnitsChosen();
            row++;
        }

        column.Children.Add(unitGrid);

        // Entry 42 section 2: dark, light, or following the system, remembered like the units.
        column.Children.Add(Ruled("Theme"));
        column.Children.Add(themeChoice);
        themeChoice.SelectionChanged += (_, _) =>
        {
            if (!showingTheme && themeChoice.SelectedIndex >= 0)
            {
                SetTheme((ThemeChoice)themeChoice.SelectedIndex);
            }
        };

        // Entry 119 section 4: the build says what it is, where a tester can read it and select it, so a report from a rolling test build
        // names the exact commit rather than "the latest one". The same string is the first line of every log and of every crash record.
        column.Children.Add(Ruled("This build"));
        column.Children.Add(new SelectableTextBlock
        {
            Text = BuildLine(),
            TextWrapping = TextWrapping.Wrap,
            Classes = { AppStyles.Secondary },
        });
        column.Children.Add(Line("Copy that line into a bug report: it names the commit this build was made from."));
        column.Children.Add(Row(Button("The project on GitHub", () => OpenInTheBrowser("https://github.com/oRAirwolf/grouplab"))));

        // Entry 119 section 6.2: the train, how often to look, what happened last time, and what a check sends. Nothing here reaches the
        // network by itself; Check now is the only button that would, and it says what it found in the line below it.
        column.Children.Add(Ruled("Updates"));
        column.Children.Add(Line(UpdateTrainHelp()));
        var train = new ComboBox { ItemsSource = UpdateTrains.Choosable.Select(TrainLabel).ToList(), SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(updates.Train) };
        train.SelectionChanged += (_, _) =>
        {
            var chosen = UpdateTrains.Choosable[Math.Max(0, train.SelectedIndex)];
            if (!chosen.IsAvailable())
            {
                // Entry 119 section 4.5: the other trains are shown so a person knows they are coming, and cannot be chosen yet.
                train.SelectedIndex = UpdateTrains.Choosable.ToList().IndexOf(updates.Train);
                updateState.Text = chosen.Words() + ": " + UpdateTrains.NotAvailableYet + ". Nightly is the only train with builds on it.";
                return;
            }

            updates = updates with { Train = chosen };
            updateState.Text = "Following the " + chosen.Words().ToLowerInvariant() + " train.";
        };
        column.Children.Add(Row(FieldLabel("Train"), train));

        var often = new ComboBox { ItemsSource = UpdateCheckIntervals.All.Select(i => i.Words()).ToList(), SelectedIndex = UpdateCheckIntervals.All.ToList().IndexOf(updates.Interval) };
        often.SelectionChanged += (_, _) =>
        {
            updates = updates with { Interval = UpdateCheckIntervals.All[Math.Max(0, often.SelectedIndex)] };
            updateState.Text = "GroupLab will look " + updates.Interval.Words().ToLowerInvariant() + ".";
        };
        column.Children.Add(Row(FieldLabel("Check"), often, Button("Check now", CheckForUpdates)));
        column.Children.Add(updateState);
        column.Children.Add(Line("A check is one request for one public file. It sends nothing about you, your rifles or your targets. docs/UPDATES.md says exactly what it does."));

        // Entry 41 section 3: the log's DEBUG switch, remembered, and where the log is, or why there is none.
        column.Children.Add(Ruled("Diagnostics"));
        var detailedLogging = new CheckBox { Content = "Detailed logging", IsChecked = DiagnosticLog.Current.Verbose || settings.LoadVerbose() };
        detailedLogging.IsCheckedChanged += (_, _) =>
        {
            DiagnosticLog.Current.Verbose = detailedLogging.IsChecked == true;
            settingsStore.SaveVerbose(detailedLogging.IsChecked == true);
        };
        column.Children.Add(detailedLogging);
        column.Children.Add(Line(DiagnosticLog.Current.IsEnabled
            ? "The log is in " + DiagnosticLog.Current.DescribedDirectory + "."
            : "Logging is off: " + DiagnosticLog.Current.DisabledReason + "."));
        column.Children.Add(Row(Button("Report a problem\u2026", () => OpenReport(null))));

        // Entry 120 section 9, question 28 answered: there is no support address yet, so the item says that rather than opening anything.
        // The address lives in SupportLink and nowhere else, and SupportLinkTests fails if one appears somewhere else in the application.
        column.Children.Add(Row(Button(SupportLink.Label, () =>
        {
            if (SupportLink.Address is { } address)
            {
                OpenInTheBrowser(address);
            }
            else
            {
                status.Text = SupportLink.NoAddressYet;
            }
        })));
        column.Children.Add(Line(SupportLink.Exists ? "Opens a page in your browser. GroupLab takes no payment itself." : SupportLink.NoAddressYet));

        // Entry 41 sections 5 and 6: the crash records not yet dealt with, which the marking panel's banner also offers until they are.
        column.Children.Add(Ruled("Crash records"));
        column.Children.Add(settingsCrashes);
        return new ScrollViewer { Content = column, IsVisible = false };
    }

    /// <summary>
    /// The header's menu, entry 109 section 2: the document's actions, opening an image or a saved marking and exporting, and reporting a
    /// problem, behind one button beside the primary action, since none of them is the task's next step.
    /// </summary>
    private Button Overflow()
    {
        var menu = new MenuFlyout();
        foreach (var (label, action) in new (string, Func<Task>)[]
        {
            ("Open image\u2026", OpenImageDialog),
            ("Open marking\u2026", OpenMarkingDialog),
            ("Export\u2026", ExportDialog),
            ("Report a problem\u2026", () =>
            {
                OpenReport(null);
                return Task.CompletedTask;
            }),
        })
        {
            var item = new MenuItem { Header = label };
            item.Click += async (_, _) => await action();
            menu.Items.Add(item);
        }

        var button = new Button { Content = Icons.Draw(Icons.More), Flyout = menu, Margin = new Thickness(Tokens.ControlMargin), Classes = { AppStyles.IconButton } };
        ToolTip.SetTip(button, "Open, export or report a problem");
        Avalonia.Automation.AutomationProperties.SetName(button, "More");
        return button;
    }

    /// <summary>The header menu's items, for the headless tests.</summary>
    internal IReadOnlyList<string> MenuItems => [.. editorActions.Children.OfType<Button>().Last().Flyout is MenuFlyout menu ? menu.Items.OfType<MenuItem>().Select(i => i.Header as string ?? "") : []];

    /// <summary>An icon alone as a button, entry 109 section 2, named in its tooltip.</summary>
    private static Button IconButton(string icon, string name, Action action)
    {
        var button = new Button { Content = Icons.Draw(icon), Classes = { AppStyles.IconButton } };
        ToolTip.SetTip(button, name);
        Avalonia.Automation.AutomationProperties.SetName(button, name);
        button.Click += (_, _) => action();
        return button;
    }

    /// <summary>A link: words that act, in amber, with no button drawn round them.</summary>
    private static Button Link(string text, Action action)
    {
        var button = new Button { Content = text, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Link } };
        button.Click += (_, _) => action();
        return button;
    }

    /// <summary>A field's name above it, in the label style.</summary>
    private static TextBlock FieldLabel(string text) => new() { Text = text, Classes = { AppStyles.Label } };

    /// <summary>
    /// An item with its "why", NOTES-FROM-PLANNING.md entry 109 section 1 principle 1 and entry 111 section 3: the sentences that explain the
    /// item, moved as they stand, one click away. The small "why" sits beside the item's last line, in space the line leaves for it, so it takes
    /// no row of its own; the explanation opens beneath the item. Each remembers whether it was opened, by the name of what it explains. It is a
    /// plain button rather than a toggle, because the theme paints a checked toggle amber, and amber means something needs a person.
    /// </summary>
    private StackPanel Explained(Control line, string item, params string[] lines)
    {
        var body = new StackPanel { Spacing = Tokens.Space4, Margin = new Thickness(0, Tokens.Space4, 0, 0), Classes = { AppStyles.WhyBody } };
        foreach (string text in lines)
        {
            body.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        }

        bool open = whyOverride ?? WhyOpen(item);
        var why = new Button { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Classes = { AppStyles.Why } };
        void Show()
        {
            body.IsVisible = open;
            why.Content = open ? "why \u25be" : "why \u25b8";
            ToolTip.SetTip(why, open ? "Hide the reasoning" : "Show the reasoning behind this");
        }

        why.Click += (_, _) =>
        {
            open = !open;
            whyOpen[item] = open;
            settingsStore.SaveWhyOpen(item, open);
            Show();
        };
        Show();

        // The line leaves room at its right for the "why", which sits beside its last line.
        line.Margin = new Thickness(line.Margin.Left, line.Margin.Top, Math.Max(line.Margin.Right, WhyWidth), line.Margin.Bottom);
        var row = new Grid();
        if (line.Parent is Panel previous)
        {
            previous.Children.Remove(line);
        }

        row.Children.Add(line);
        row.Children.Add(why);
        return new StackPanel { Spacing = 0, Children = { row, body } };
    }

    /// <summary>The room an item leaves at its right for its "why".</summary>
    private const double WhyWidth = 44;

    private bool WhyOpen(string item)
    {
        if (!whyOpen.TryGetValue(item, out bool open))
        {
            whyOpen[item] = open = settingsStore.LoadWhyOpen(item);
        }

        return open;
    }

    /// <summary>Opens or closes every "why" at once, or with null gives each its remembered state back, for the renders of entry 109 section 4.</summary>
    internal void SetEveryWhy(bool? open)
    {
        whyOverride = open;
        Refresh();
    }

    /// <summary>
    /// One figure's row, entry 109 section 1 principle 3: whatever the row holds, with a hairline beneath it, so every figure in the column has
    /// the same shape and rows are divided by a rule rather than boxed.
    /// </summary>
    private static Border Rowed(Control content) => new()
    {
        Child = content,
        BorderThickness = new Thickness(0, 0, 0, 1),
        Padding = new Thickness(0, 0, 0, Tokens.Space8),
        Classes = { AppStyles.Ruled },
    };

    /// <summary>
    /// The shots a count means, entry 105 section 8    /// <summary>
    /// The shots a count means, entry 105 section 8: every shot, or with sighters not analysed only those off the sighter bulls, so a sheet of
    /// ten scoring shots and four sighters reads ten.
    /// </summary>
    private int CountedShots(MarkingState state) => state.Shots.Count(s => s.IsShot && (analyseSighters || !GroupAnalysis.OnSighter(state, s)));

    /// <summary>
    /// The sighters' own section of the analysis state, entry 105 section 8, when they are analysed: their count, where their centre sits
    /// from aim, and their zero readout, which is the useful part, since sighters are fired to confirm zero. They are measured on a view of
    /// the marking that holds only them, so they are never pooled with the scoring shots.
    /// </summary>
    private void ShowSighters(MarkingState state)
    {
        sighterPanel.Children.Clear();
        bool has = GroupAnalysis.HasSighters(state);
        analyseSightersBox.IsVisible = has;
        if (!has || !analyseSighters)
        {
            return;
        }

        var view = GroupAnalysis.Sighters(state);
        sighterPanel.Children.Add(Heading("Sighters, their own group"));
        var report = GroupAnalysis.Analyse(view);
        if (report.AllShots is not { } sighters)
        {
            sighterPanel.Children.Add(Line(report.Problem ?? "No sighter shots are marked."));
            return;
        }

        sighterPanel.Children.Add(Line(sighters.CentreFromAim is { } centre ? CentreLine(AsDisplayed(centre)) : $"Centre from aim: {sighters.CentreFromAimUnavailable}."));
        if (sighters.DispersionWithheld is { } withheld)
        {
            sighterPanel.Children.Add(Line(withheld));
        }

        var zero = new StackPanel { Spacing = 2 };
        ShowZero(view, zero, "sighter-zero");
        sighterPanel.Children.Add(zero);
    }

    /// <summary>The breadcrumb's line: the application, the document, and what is on it.</summary>
    private void ShowBreadcrumb(MarkingState state)
    {
        string document = state.ImagePath is { } path ? Path.GetFileName(path) : "no image open";
        int shots = CountedShots(state);
        int open = ReviewQueue.Open(ReviewQueue.For(state, analyseSighters));
        reviewPill.IsVisible = shots > 0;
        reviewCount.Text = FormattableString.Invariant($"{open} of {shots} need review");
        foreach (var (target, classes) in new (Avalonia.StyledElement, Classes)[] { (reviewPill, reviewPill.Classes), (reviewCount, reviewCount.Classes) })
        {
            classes.Remove(AppStyles.Warn);
            classes.Remove(AppStyles.Good);
            classes.Add(open > 0 ? AppStyles.Warn : AppStyles.Good);
        }

        // Entry 105 section 4: the lockup stands before the crumbs, so they begin with the document rather than repeating the name.
        breadcrumb.Text = shots == 0
            ? $"\u203a  {document}"
            : FormattableString.Invariant($"\u203a  {document}  \u203a  {shots} shots{(open > 0 ? $", {open} to review" : "")}");
    }

    /// <summary>
    /// A note, entry 105 section 2: a sentence under a readout, in the UI sans at the secondary size and faint. DESIGN.md section 19 keeps
    /// the monospace for numeric readouts; a sentence with a number in it is still a sentence.
    /// </summary>
    private static TextBlock Note(string text) =>
        new() { Text = text, FontSize = Tokens.SecondarySize, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Faint } };

    /// <summary>One cell of the zero readouts, in the tabular mono, right-aligned so the columns line up.</summary>
    private static TextBlock ZeroCell(string text) => new()
    {
        Text = text,
        FontFamily = Mono,
        HorizontalAlignment = HorizontalAlignment.Right,
        Margin = new Thickness(Tokens.Space8, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <summary>Entry 42 section 3: a second line under a readout, in mono at the secondary size and dim.</summary>
    private static TextBlock Detail(string text) =>
        new() { Text = text, FontFamily = Mono, FontSize = Tokens.SecondarySize, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Dim } };

    /// <summary>
    /// The zero correction section, NOTES-FROM-PLANNING.md entries 91 and 92: windage and elevation as separate rows because a turret has
    /// two knobs and nobody dials a diagonal, each in a linear and an angular unit at once, the uncertainty in the same units, and either a
    /// correction or a refusal with the shot count that would settle it. Never a bare number.
    /// </summary>
    private void ShowZero(MarkingState state) => ShowZero(state, zeroPanel, "zero");

    private void ShowZero(MarkingState state, StackPanel zeroPanel, string item)
    {
        zeroPanel.Children.Clear();
        var view = ZeroFor(state);
        if (view.Refusal is { } refusal)
        {
            zeroPanel.Children.Add(Line(refusal));
            return;
        }

        // Entry 105 section 2: the two readouts in columns, linear under linear and angular under angular, where laid out as text they
        // started at different places; the uncertainty is a sentence, set as one.
        var readouts = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto") };
        int row = 0;
        foreach (var (label, linear, angular, sits) in view.Rows)
        {
            var cells = new Control[]
            {
                new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Classes = { AppStyles.Secondary } },
                ZeroCell(linear),
                ZeroCell(angular),
                ZeroCell(sits),
            };
            for (int c = 0; c < cells.Length; c++)
            {
                Grid.SetRow(cells[c], row);
                Grid.SetColumn(cells[c], c);
                readouts.Children.Add(cells[c]);
            }

            row++;
        }

        zeroPanel.Children.Add(readouts);
        zeroPanel.Children.Add(Note(view.Note!));

        // The block's finding, at the label size and full strength: the line that matters. Entry 109 section 3a: one line in view, "Not
        // distinguishable from zero at 25 shots. About 90 shots would settle it.", and the rest of it, the degrees of freedom and the solver,
        // behind its "why".
        var verdict = new TextBlock { Text = view.Verdict, TextWrapping = TextWrapping.Wrap, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, Tokens.Space4) };
        if (view.Dial)
        {
            verdict.Classes.Add(AppStyles.Good);
        }

        zeroPanel.Children.Add(Explained(verdict, item, [.. view.Why]));
        if (item == "zero")
        {
            ShowCarry(state, zeroPanel);
        }
    }

    /// <summary>
    /// The zero correction in words, which the screen lays out and the report prints unchanged: a refusal when there is nothing to correct
    /// from, or the two readouts, the uncertainty, the verdict (a correction to dial or the refusal with the shots that would settle it) and
    /// the reasoning behind its "why".
    /// </summary>
    private sealed record ZeroView(string? Refusal, IReadOnlyList<(string Label, string Linear, string Angular, string Sits)> Rows, string? Note, string Verdict, bool Dial, IReadOnlyList<string> Why);

    private ZeroView ZeroFor(MarkingState state)
    {
        if (Zeroing.For(state) is not { } zero)
        {
            string refusal = state.Scale is null
                ? "Set a scale, then mark at least five shots, and the correction to dial appears here."
                : $"Needs at least {GroupAnalysis.MinimumShotsForDispersion} shots on bulls or a point of aim: an offset cannot be told from noise without the group's own spread.";
            return new ZeroView(refusal, [], null, refusal, false, []);
        }

        double? distance = state.ShotDistanceInches;
        string Both(double inches) => units.AngleText(Math.Abs(inches), distance) is { } angle
            ? $"{units.Length(Math.Abs(inches))}  {angle}"
            : units.Length(Math.Abs(inches));

        var rows = new List<(string, string, string, string)>();
        foreach (var (label, axis) in new[] { ("Group centre, windage", zero.Windage), ("Group centre, elevation", zero.Elevation) })
        {
            rows.Add((label, units.Length(Math.Abs(axis.OffsetInches)), units.AngleText(Math.Abs(axis.OffsetInches), distance) ?? "", axis.Sits));
        }

        string note = $"give or take {Both(zero.Windage.HalfWidthInches)} across and {Both(zero.Elevation.HalfWidthInches)} up and down, at {100 * Zeroing.Level:0} percent";

        // Entry 97 section 2: in clicks where the marking names a rifle and the distance is set, with what rounding leaves, and otherwise in
        // the linear and angular figures, which every turret is marked in one of.
        string Dial(ZeroAxis axis) => axis.Clicks is { } clicks
            ? clicks.Describe() + string.Create(CultureInfo.InvariantCulture, $" ({Both(axis.OffsetInches)}, leaving {Math.Abs(clicks.ResidualAngle):0.00} {(clicks.Unit == AngularUnit.Mrad ? "mil" : "MOA")})")
            : $"{Both(axis.OffsetInches)} {axis.Dial}";
        var dial = new List<string>();
        if (zero.Windage.Distinguishable)
        {
            dial.Add(Dial(zero.Windage));
        }

        if (zero.Elevation.Distinguishable)
        {
            dial.Add(Dial(zero.Elevation));
        }

        string verdict;
        var why = new List<string>();
        if (dial.Count > 0)
        {
            verdict = "Dial " + string.Join(" and ", dial) + ".";
        }
        else
        {
            int? settle = zero.Windage.ShotsToSettle is { } w && zero.Elevation.ShotsToSettle is { } e ? Math.Min(w, e) : zero.Windage.ShotsToSettle ?? zero.Elevation.ShotsToSettle;
            verdict = FormattableString.Invariant($"Not distinguishable from zero at {zero.Shots} shots.")
                + (settle is { } more ? FormattableString.Invariant($" About {more} shots would settle it.") : " Nothing this rifle can shoot would settle an offset this small.");
            why.Add(FormattableString.Invariant($"The smallest offset these shots can call is {Both(zero.DetectableInches)}.") + (settle is not null ? " Shoot more before touching the turret." : ""));
        }

        why.Add(zero.Circular
            ? $"sigma pooled over both axes on {zero.DegreesOfFreedom} degrees of freedom, the group being circular"
            : $"each axis on its own, {zero.DegreesOfFreedom} degrees of freedom, the group not being circular");
        why.Add(distance is null
            ? "Angular figures and clicks need the shot distance. It corrects the zero at the distance shot; moving a zero between distances needs the solver."
            : state.Rifle is null
                ? "Choose a rifle to have this in clicks. It corrects the zero at the distance shot; moving a zero between distances needs the ballistic solver."
                : $"In clicks of {state.Rifle.Name}'s scope, {state.Rifle.DescribeClick()}, at the distance shot. Moving a zero between distances needs the ballistic solver.");
        return new ZeroView(null, rows, note, verdict, dial.Count > 0, why);
    }
}

/// <summary>The rail's destinations that open in the main window.</summary>
internal enum Destination
{
    Analyse,
    Sessions,
    Library,
    Ballistics,
    Compare,
    Settings,
}
