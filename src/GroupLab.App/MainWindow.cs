using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Trace;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Registration;
using GroupLab.Core.Statistics;

namespace GroupLab.App;

/// <summary>
/// The marking screen, docs/PHASE1-BRIEF.md section 6 and NOTES-FROM-PLANNING.md entry 21 section 3, built in code. Left to right:
/// the image with its marks, and a panel with the scale, the statistics and the selected shot. Along the top, every action as a
/// button; keyboard shortcuts duplicate them for speed (DESIGN.md section 13) and are never the only way to do something.
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
public sealed class MainWindow : Window
{
    private static readonly FontFamily Mono = Tokens.Mono;

    private readonly MarkingSession session = new();
    private readonly MarkingCanvas canvas = new();
    private readonly Dictionary<MarkingTool, ToggleButton> toolButtons = [];
    private readonly TextBlock status = new() { Margin = new Thickness(Tokens.Space14, Tokens.Space4), TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };
    // NOTES-FROM-PLANNING.md entry 76 section 4: a detection the window starts on its own shows that it is running and can be stopped.
    private readonly ProgressBar detectionProgress = new() { IsIndeterminate = true, Width = 120, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
    private readonly Button cancelDetection = new() { Content = "Cancel detection", Margin = new Thickness(Tokens.Space8, 2), IsVisible = false };
    private CancellationTokenSource? detection;

    // The marking exactly as the last detection left it, so setting a calibre can tell untouched marks, which it may detect again, from
    // corrections it must not throw away (NOTES-FROM-PLANNING.md entry 78 section 4).
    private MarkingState? detectedState;

    // DESIGN.md section 13 and NOTES-FROM-PLANNING.md entry 83 section 4: the assignment editor's review queue, what needs a decision and how
    // to give it, at the top of the panel, driven from the keyboard.
    private readonly StackPanel review = new() { Spacing = Tokens.Space6 };
    private string? currentReview;
    private string bullTyped = "";

    private readonly TextBlock problem = new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };
    private readonly StackPanel statistics = new() { Spacing = 4 };

    // DESIGN.md section 19 and NOTES-FROM-PLANNING.md entry 73 section 7: the figures that change decisions stay in view with their
    // intervals, and the reference figures sit one click away in a panel that remembers whether it was opened.
    private readonly StackPanel moreFigures = new() { Spacing = 4 };
    private readonly Expander moreFiguresPanel = new() { Header = "More figures", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly StackPanel selection = new() { Spacing = 6 };
    private readonly StackPanel shotList = new() { Spacing = 2 };
    private readonly StackPanel crashBanner = new() { Spacing = Tokens.Space6, IsVisible = false };
    private readonly StackPanel scaleInputs = new() { Spacing = 6 };
    private readonly ComboBox exclusionReason = new() { ItemsSource = Enum.GetNames<ExclusionReason>(), SelectedIndex = 0, MinWidth = 140 };
    private GrayImage? grey;
    private GrayImage? valueImage;
    private ImageMetadata? metadata;
    private IReadOnlyList<HoleSizeFlag> holeFlags = [];

    // The sheet's printed artwork in image pixels, from the last detection on this image, so the snap and the size check can tell printed
    // ink from a hole (NOTES-FROM-PLANNING.md entry 40 section 1). Null on any image not detected as a GroupLab sheet.
    private GrayImage? artwork;

    // A sheet size the person who shot it stated, from a provenance record beside the image (NOTES-FROM-PLANNING.md entry 37 section 5).
    private StatedSheetSize? statedSize;
    private readonly AutoCompleteBox calibreBox = new() { ItemsSource = Calibre.Common.Select(c => c.Name).ToList(), FilterMode = AutoCompleteFilterMode.Contains, MinWidth = 180, PlaceholderText = "optional, e.g. .308" };
    private readonly TextBlock calibreNote = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Opacity = 0.85 };
    private readonly AppSettingsStore settingsStore;
    private readonly ComboBox linearUnit = new() { ItemsSource = Enum.GetValues<LinearUnit>().Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 70 };
    private readonly ComboBox angularUnit = new() { ItemsSource = UnitSettings.AngularChoices.Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 90 };
    private readonly ComboBox distanceUnit = new() { ItemsSource = Enum.GetValues<DistanceUnit>().Select(u => UnitSettings.Symbol(u)).ToList(), MinWidth = 70 };
    private readonly TextBox shotDistance = new() { Width = 90 };
    private readonly TextBlock shotDistanceUnit = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly ComboBox themeChoice = new() { ItemsSource = new[] { "Follow system", "Dark", "Light" }, MinWidth = 140 };
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

        var toolbar = new WrapPanel { Margin = new Thickness(Tokens.Space8, Tokens.Space6), Orientation = Orientation.Horizontal };
        toolbar.Children.Add(Button("Open image", async () => await OpenImageDialog()));
        toolbar.Children.Add(Button("Open marking", async () => await OpenMarkingDialog()));
        toolbar.Children.Add(Button("Detect on a GroupLab sheet", async () => await Detect(automatic: false)));
        toolbar.Children.Add(Button("Print a target", () => new PrintWindow().Show()));
        toolbar.Children.Add(new Separator { Width = 12 });
        foreach (var (tool, label) in new[] { (MarkingTool.Pan, "Pan (P)"), (MarkingTool.Length, "Scale: length (L)"), (MarkingTool.Rectangle, "Scale: rectangle (R)"), (MarkingTool.Aim, "Point of aim (A)"), (MarkingTool.Impact, "Impact (I)"), (MarkingTool.Select, "Select (V)") })
        {
            var button = new ToggleButton { Content = label, Margin = new Thickness(2) };
            button.Click += (_, _) => SetTool(tool);
            toolButtons[tool] = button;
            toolbar.Children.Add(button);
        }

        toolbar.Children.Add(new Separator { Width = 12 });
        toolbar.Children.Add(Button("Undo", () => session.Undo()));
        toolbar.Children.Add(Button("Redo", () => session.Redo()));
        toolbar.Children.Add(Button("Zoom in", () => canvas.ZoomBy(1.25)));
        toolbar.Children.Add(Button("Zoom out", () => canvas.ZoomBy(0.8)));
        toolbar.Children.Add(Button("Fit", canvas.FitToView));
        toolbar.Children.Add(Button("Rotate left ([)", () => session.Rotate(-1)));
        toolbar.Children.Add(Button("Rotate right (])", () => session.Rotate(1)));
        toolbar.Children.Add(Button("Export", async () => await ExportDialog()));
        toolbar.Children.Add(Button("Report a problem", () => OpenReport(null)));

        var panel = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space12 };
        panel.Children.Add(crashBanner);
        panel.Children.Add(Heading("Review"));
        panel.Children.Add(review);
        AddHandler(KeyDownEvent, OnReviewKey, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // Entry 25 section 1: one application-wide unit setting on three axes, which every figure obeys and no stored value does.
        panel.Children.Add(Heading("Units"));
        panel.Children.Add(Row(linearUnit, angularUnit, distanceUnit));
        foreach (var combo in new[] { linearUnit, angularUnit, distanceUnit })
        {
            combo.SelectionChanged += (_, _) => UnitsChosen();
        }

        // Entry 42 section 2: dark, light, or following the system, remembered like the units.
        panel.Children.Add(Heading("Theme"));
        panel.Children.Add(themeChoice);
        themeChoice.SelectionChanged += (_, _) =>
        {
            if (!showingTheme && themeChoice.SelectedIndex >= 0)
            {
                SetTheme((ThemeChoice)themeChoice.SelectedIndex);
            }
        };

        // Entry 41 section 3: the log's DEBUG switch, remembered, and where the log is, or why there is none.
        panel.Children.Add(Heading("Diagnostics"));
        var detailedLogging = new CheckBox { Content = "Detailed logging", IsChecked = DiagnosticLog.Current.Verbose || settings.LoadVerbose() };
        detailedLogging.IsCheckedChanged += (_, _) =>
        {
            DiagnosticLog.Current.Verbose = detailedLogging.IsChecked == true;
            settingsStore.SaveVerbose(detailedLogging.IsChecked == true);
        };
        panel.Children.Add(detailedLogging);
        panel.Children.Add(Line(DiagnosticLog.Current.IsEnabled
            ? "The log is in " + DiagnosticLog.Current.DescribedDirectory + "."
            : "Logging is off: " + DiagnosticLog.Current.DisabledReason + "."));

        panel.Children.Add(Heading("Scale"));
        panel.Children.Add(scaleInputs);
        panel.Children.Add(Heading("Group"));

        // Entry 24 section 5: the calibre is a property of the group, entered once, from the list or typed.
        panel.Children.Add(new TextBlock { Text = "Calibre", FontSize = 12 });
        panel.Children.Add(Row(calibreBox, Button("Set", SetCalibreFromBox), Button("Clear", () =>
        {
            calibreBox.Text = "";
            session.SetCalibre(null);
        })));
        panel.Children.Add(calibreNote);
        panel.Children.Add(new TextBlock { Text = "Shot distance", FontSize = 12 });
        panel.Children.Add(Row(shotDistance, shotDistanceUnit, Button("Set", SetShotDistanceFromBox), Button("Clear", () =>
        {
            shotDistance.Text = "";
            session.SetShotDistance(null);
        })));
        panel.Children.Add(problem);
        panel.Children.Add(statistics);
        panel.Children.Add(Heading("Shots"));
        panel.Children.Add(shotList);
        panel.Children.Add(Heading("Selected shot"));
        panel.Children.Add(selection);

        // Entry 42 section 4: the bar across the top, a right column 372 wide, and a status line, each separated by one pixel of line.
        var bar = new Border { Child = toolbar, Classes = { AppStyles.Bar } };
        var side = new Border { Width = Tokens.RightColumnWidth, Child = new ScrollViewer { Content = panel }, Classes = { AppStyles.Side } };
        cancelDetection.Click += (_, _) => CancelDetection();
        var statusLine = new DockPanel();
        var running = new StackPanel { Orientation = Orientation.Horizontal, Children = { detectionProgress, cancelDetection } };
        DockPanel.SetDock(running, Dock.Right);
        statusLine.Children.Add(running);
        statusLine.Children.Add(status);
        var statusBar = new Border { Child = statusLine, Classes = { AppStyles.StatusBar } };
        var dock = new DockPanel();
        DockPanel.SetDock(bar, Dock.Top);
        DockPanel.SetDock(statusBar, Dock.Bottom);
        DockPanel.SetDock(side, Dock.Right);
        dock.Children.Add(bar);
        dock.Children.Add(statusBar);
        dock.Children.Add(side);
        dock.Children.Add(canvas);
        Content = dock;
        SetTool(MarkingTool.Pan);
        ShowUnits();
        Refresh();
        ShowPendingCrashes();
    }

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
        var pending = CrashReporter.PendingCrashes(DiagnosticLog.Current.Directory);
        crashBanner.IsVisible = pending.Count > 0;
        if (pending.Count == 0)
        {
            return;
        }

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
        var calibre = Calibre.Parse(calibreBox.Text, out string? why);
        if (why is not null)
        {
            calibreNote.Text = why;
            return;
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
        var identity = await Task.Run(() => SheetIdentification.Identify(g, ShippedDefinitions(), new OpenCvSharpBackend(), trace, token), token);
        CrashReporter.InFlight = null;
        token.ThrowIfCancellationRequested();
        DiagnosticLog.Info("detect.identify", ("definition", identity.DefinitionId), ("tile", identity.TileIndex), ("codes", identity.CodesRead), ("failure", identity.Failure), ("automatic", automatic));

        // Entry 35 section 6 item 3: the sheet names its own definition, and only when its codes cannot is the user asked for one.
        if (identity.Definition is null && automatic)
        {
            status.Text = $"Nothing detected: this image is not a GroupLab sheet GroupLab recognises ({identity.Failure}). Set a scale and mark it by hand, or choose Detect on a GroupLab sheet if it is one.";
            return;
        }

        if (identity.Definition is not { } named)
        {
            status.Text = $"The sheet's codes did not give its definition: {identity.Failure}. Choose the definition.";
            DiagnosticLog.Info("dialog.open", ("dialog", "definition"));
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Choose the sheet's definition",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("GroupLab definitions") { Patterns = ["*.gltd.json"] }],
            });
            DiagnosticLog.Info("dialog.result", ("dialog", "definition"), ("chosen", files.Count > 0));
            if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path)
            {
                return;
            }

            if (GltdJsonReader.ReadFile(path).Definition is not { } chosen)
            {
                problem.Text = "That file is not a readable GroupLab definition.";
                return;
            }

            named = chosen;
        }

        status.Text = automatic ? $"Recognised {named.Name}. Registering and detecting…" : "Registering and detecting…";
        CrashReporter.InFlight = trace;
        var calibre = session.State.Calibre;
        var result = await Task.Run(() => AutomaticMarking.Run(g, v, m, named, new OpenCvSharpBackend(), trace, token, calibre), token);
        CrashReporter.InFlight = null;
        token.ThrowIfCancellationRequested();
        LogDetection(result, trace, clock.ElapsedMilliseconds);
        if (ReferenceEquals(g, grey))
        {
            ApplyDetection(result);
        }
    }

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
            problem.Text = "Detection failed: " + (result.Failure ?? "no registration") + ". Mark this image by hand with a reference length or rectangle.";
            status.Text = result.Summary;
            return;
        }

        canvas.MissingMarkers = result.MissingMarkers;
        canvas.Artwork = artwork = result.ExpectedArtwork;
        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        RememberDetected();
        SetTool(MarkingTool.Select);
        status.Text = result.Summary + (result.MissingMarkers.Count > 0 ? string.Create(CultureInfo.InvariantCulture, $"; {result.MissingMarkers.Count} markers not found, crossed out") : "");
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
            await File.WriteAllTextAsync(path, MarkingFile.Write(session.State, holeFlags, units));
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

        holeFlags = valueImage is null ? [] : HoleSize.Check(state, valueImage, artwork);
        canvas.FlaggedShots = holeFlags.ToDictionary(f => f.ShotId, f => f.ApparentInches);
        canvas.DetectorFlags = state.Shots.Where(s => s.IsShot && s.Oversize is not null).ToDictionary(s => s.Id, s => s.Oversize!.Tentative);

        if (canvas.AwaitingTaps.Count == 0 && (scaleInputs.Children.Count == 0 || state.Scale is not null))
        {
            scaleInputs.Children.Clear();

            // Entry 42 section 4's status pill: every figure depends on the scale, so it says where the scale came from, in teal when the sheet
            // registered, in amber when it is a reference drawn by hand, and plainly when there is none.
            var pillText = new TextBlock
            {
                Text = state.Scale is null ? "No scale yet. Choose Scale: length or Scale: rectangle, or detect on a GroupLab sheet." : "From " + state.Scale.Describe(units) + ".",
                TextWrapping = TextWrapping.Wrap,
                Classes = { AppStyles.PillText },
            };
            var pill = new Border { Child = pillText, Classes = { AppStyles.Pill } };
            if (state.Scale is not null)
            {
                string standing = state.Scale is SheetReference ? AppStyles.Good : AppStyles.Warn;
                pill.Classes.Add(standing);
                pillText.Classes.Add(standing);
            }

            scaleInputs.Children.Add(pill);
        }

        statistics.Children.Clear();
        moreFigures.Children.Clear();
        if (report.AllShots is { } all)
        {
            var reduced = report.WithoutExclusions!;
            bool excluded = report.Excluded > 0;
            // Entry 46 section 3: where a human judgement entered the measurement, in one line above the figures.
            statistics.Children.Add(Line(PlacedLine(all.Shots, report.Automatic, report.Corrected, report.Manual)
                + string.Create(CultureInfo.InvariantCulture, $"{(excluded ? $"; {reduced.Shots} without the {report.Excluded} excluded" : "")}{(report.NotShots > 0 ? $"; {report.NotShots} marked not a shot" : "")}.")));
            statistics.Children.Add(Line(all.CentreFromAim is { } offsetFromAim && AsDisplayed(offsetFromAim) is var centre
                ? CentreLine(centre)
                : $"Centre from aim: {all.CentreFromAimUnavailable}."));

            if (all.DispersionWithheld is { } withheld)
            {
                // Entry 24 section 1: below the minimum there is no headline figure to misread, only what is missing.
                statistics.Children.Add(new TextBlock { Text = withheld, TextWrapping = TextWrapping.Wrap, FontSize = Tokens.BodySize, FontWeight = FontWeight.SemiBold });
            }
            else
            {
                statistics.Children.Add(Figure("Mean radius", all.MeanRadius!, excluded ? reduced : null, f => f.MeanRadius, Tokens.LeadFigureSize, FontWeight.Medium));
                statistics.Children.Add(Figure("Sigma", all.Sigma!, excluded ? reduced : null, f => f.Sigma, Tokens.FigureSize, FontWeight.Medium));
                statistics.Children.Add(Figure("Extreme spread, centre to centre", all.ExtremeSpread!, excluded ? reduced : null, f => f.ExtremeSpread, Tokens.BodySize, FontWeight.Normal, subordinate: true));
                moreFigures.Children.Add(new TextBlock
                {
                    Text = all.ExtremeSpreadEdgeToEdge is { } edgeToEdge
                        ? $"Edge to edge, across the outsides of the holes: {units.Length(edgeToEdge)}{(units.AngleText(edgeToEdge, state.ShotDistanceInches) is { } angle ? ", " + angle : "")}, which is centre to centre plus one {units.Length(state.Calibre!.DiameterInches)} bullet."
                        : $"Edge to edge: {all.ExtremeSpreadEdgeToEdgeUnavailable}.",
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { AppStyles.Secondary },
                });
                if (state.ShotDistanceInches is null)
                {
                    moreFigures.Children.Add(Line("Angular figures need the shot distance."));
                }

                if (all.Shots < GroupAnalysis.SmallGroupShots && all.TrueSizeRange is { } range)
                {
                    moreFigures.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                        $"From {all.Shots} shots the true group size could be anywhere from {range.Lower:0.00} to {range.Upper:0.00} times what they measure (STATISTICS.md section 9.1).")));
                }

                moreFigures.Children.Add(Line(all.AspectRatio is { } aspect
                    ? string.Create(CultureInfo.InvariantCulture, $"Error ellipse aspect {aspect:0.00}, major axis at {DisplayedAngle(all.AngleDegrees ?? 0):0} degrees; {all.Shots} circular shots give about {all.CircularMedianAspect:0.0} and exceed {aspect:0.00} {HowOften(all.CircularAspectExceedance ?? 1)} (STATISTICS.md section 7).")
                    : $"Error ellipse: {all.AspectRatioUnavailable}."));
                if (all.WorstShotInMeanRadii is { } worst)
                {
                    moreFigures.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                        $"Worst shot at {worst:0.00} mean radii; a group of {all.Shots} is expected to put its worst at {all.ExpectedWorstInMeanRadii:0.00}, so a shot there is not a flyer by that measure alone (STATISTICS.md section 10).")));
                }
            }

            // Entry 82 section 6: the detector's flag on a mark that covers about two holes, in the panel as well as on the canvas.
            foreach (var shot in state.Shots.Where(s => s.IsShot && s.Oversize is not null && !holeFlags.Any(f => f.ShotId == s.Id)))
            {
                statistics.Children.Add(new TextBlock
                {
                    Text = shot.Oversize!.Describe(ShotLabel(shot.Id)),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = Tokens.SecondarySize,
                    Classes = { shot.Oversize.Tentative ? AppStyles.Secondary : AppStyles.Alert },
                });
            }

            foreach (var flag in holeFlags)
            {
                statistics.Children.Add(new TextBlock
                {
                    Text = $"Shot {ShotLabel(flag.ShotId)} {HoleSize.Describe(flag, state.Calibre!.DiameterInches, units.Length)}",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = Tokens.SecondarySize,
                    Classes = { AppStyles.Alert },
                });
            }

            if (moreFigures.Children.Count > 0)
            {
                statistics.Children.Add(moreFiguresPanel);
            }
        }

        BuildShotList();
        BuildSelection();
    }

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

            string text = string.Create(CultureInfo.InvariantCulture, $"{name}{(shot.Exclusion is { } e ? $", excluded as {e}" : "")}");
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
                    session.SetExclusion(id, shot.Exclusion is null ? Enum.Parse<ExclusionReason>((string)exclusionReason.SelectedItem!) : null)), 2);
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

        selection.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
            $"Shot {ShotLabel(id)}: {shot.Provenance.ToString().ToLowerInvariant()}{(shot.Exclusion is { } e ? $", excluded as {e}" : "")}")));
        selection.Children.Add(Row(exclusionReason, Button(shot.Exclusion is null ? "Exclude" : "Restore", () =>
            session.SetExclusion(id, shot.Exclusion is null ? Enum.Parse<ExclusionReason>((string)exclusionReason.SelectedItem!) : null))));
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
        ReviewItems = ReviewQueue.For(state);
        int open = ReviewQueue.Open(ReviewItems);
        var current = ReviewItems.FirstOrDefault(i => i.Key == currentReview && !i.Resolved) ?? ReviewItems.FirstOrDefault(i => !i.Resolved);
        currentReview = current?.Key;
        int shots = state.Shots.Count(s => s.IsShot);
        var count = new TextBlock
        {
            Text = ReviewItems.Count == 0 ? (state.Assignment is null ? "Nothing detected to review." : "Nothing needs review.") : $"{open} of {shots} need review",
            FontFamily = Mono,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { open > 0 ? AppStyles.Alert : AppStyles.Secondary },
        };
        var header = Row(count);
        if (detectedState is not null && !ReferenceEquals(state, detectedState))
        {
            header.Children.Add(Button("Discard edits", () =>
            {
                session.Restore(detectedState);
                status.Text = "Edits discarded: the marking is as detection left it. Undo brings the edits back.";
            }));
        }

        review.Children.Add(header);
        if (current is not null)
        {
            var card = new StackPanel { Spacing = Tokens.Space6 };
            card.Children.Add(new TextBlock { Text = ReviewTitle(current.Kind), FontWeight = FontWeight.SemiBold, Classes = { AppStyles.Alert } });
            card.Children.Add(new TextBlock { Text = current.Sentence, TextWrapping = TextWrapping.Wrap });
            var choices = new WrapPanel();
            foreach (var choice in current.Choices)
            {
                choices.Children.Add(Button(choice.Label, () => Choose(current, choice)));
            }

            card.Children.Add(choices);
            card.Children.Add(Line("Enter takes the first choice, Space moves to the next item, a bull's number then Enter reassigns the selected shot, N marks it not a shot."));
            review.Children.Add(new Border { Child = card, Padding = new Thickness(Tokens.Space8), BorderThickness = new Thickness(1), BorderBrush = Marks.Alert, CornerRadius = new CornerRadius(4) });
        }

        int n = 0;
        foreach (var item in ReviewItems)
        {
            n++;
            string state_ = item.Resolved ? "DONE" : item.Key == currentReview ? "NOW" : "NEXT";
            var line = new Button
            {
                Content = $"{n}.  {ReviewTitle(item.Kind)}{(item.ShotId is { } id ? ", shot " + ShotLabel(id) : item.Bull is { } b ? ", bull " + BullLabel(b) : "")}   {state_}",
                HorizontalAlignment = HorizontalAlignment.Stretch,
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
        _ => "Refused candidate",
    };

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
        var items = ReviewQueue.For(session.State);
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

    private static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var child in children)
        {
            row.Children.Add(child);
        }

        return row;
    }

    /// <summary>A section label, entry 42 section 3: uppercase, 10 point semibold, spaced, in faint.</summary>
    private static TextBlock Heading(string text) => new() { Text = text.ToUpperInvariant(), Margin = new Thickness(0, Tokens.Space8, 0, 0), Classes = { AppStyles.Section } };

    private static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } };

    /// <summary>The text of the statistics panel, for the headless tests.</summary>
    internal IEnumerable<string> StatisticsText => statistics.GetLogicalDescendants().OfType<TextBlock>().Select(t => t.Text ?? "");

    /// <summary>
    /// One figure in the monospace with tabular figures DESIGN.md section 19 asks for, and beneath it, smaller, its interval labelled
    /// with the coverage it actually has (NOTES-FROM-PLANNING.md entry 24 section 1), then the figure without exclusions when there
    /// are any. Every line wraps, so the largest type cannot clip at the panel's edge (entry 24 section 2). Extreme spread is drawn
    /// smaller and dimmer: present, and visibly subordinate.
    /// </summary>
    private Control Figure(string name, ReportedEstimate all, GroupFigures? reduced, Func<GroupFigures, ReportedEstimate?> pick, double size, FontWeight weight, bool subordinate = false)
    {
        double? distance = session.State.ShotDistanceInches;
        string Interval(ReportedEstimate e) => e is { Lower: { } lower, Upper: { } upper, Coverage: { } coverage }
            ? string.Create(CultureInfo.InvariantCulture, $"{100 * coverage:0.0}% interval {units.Number(lower)} to {units.Length(upper)}")
            : $"no interval: {e.IntervalUnavailable}";
        string? Angle(ReportedEstimate e) => units.AngleText(e.Value, distance) is { } value
            ? value + (e is { Lower: { } lower, Upper: { } upper } ? $", interval {units.Angle(lower, distance)!.Value.ToString("0.00", CultureInfo.InvariantCulture)} to {units.AngleText(upper, distance)}" : "")
            : null;
        // Entry 42 section 3: the name in dim, the value in mono at the figure size, and every interval line in mono at 11.5 in dim.
        TextBlock Detail(string text) => new() { Text = text, FontFamily = Mono, FontSize = Tokens.SecondarySize, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Dim } };
        var column = new StackPanel { Spacing = 0 };
        column.Children.Add(new TextBlock { Text = name, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Secondary } });
        var value = new TextBlock { Text = units.Length(all.Value), FontFamily = Mono, FontSize = size, FontWeight = weight, LetterSpacing = subordinate ? 0 : Tokens.FigureSpacing, TextWrapping = TextWrapping.Wrap };
        if (subordinate)
        {
            value.Classes.Add(AppStyles.Dim);
        }

        column.Children.Add(value);
        column.Children.Add(Detail(Interval(all)));
        if (Angle(all) is { } angle)
        {
            column.Children.Add(Detail(angle));
        }

        if (reduced is not null)
        {
            column.Children.Add(Detail(pick(reduced) is { } r
                ? $"without exclusions: {units.Length(r.Value)}, {Interval(r)}"
                : "without exclusions: " + reduced.DispersionWithheld));
        }

        return column;
    }
}
