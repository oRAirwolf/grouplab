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
    private readonly TextBlock problem = new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap, Classes = { AppStyles.Alert } };
    private readonly StackPanel statistics = new() { Spacing = 4 };
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
        toolbar.Children.Add(Button("Detect on a GroupLab sheet", async () => await DetectDialog()));
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

        var panel = new StackPanel { Margin = Tokens.SectionPadding, Spacing = Tokens.Space12 };
        panel.Children.Add(crashBanner);

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
        var statusBar = new Border { Child = status, Classes = { AppStyles.StatusBar } };
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

    /// <summary>A shot's number as the image and the list show it, or its id when it is marked as not a shot and has no number.</summary>
    private string ShotLabel(int id) => MarkingCanvas.ShotNumbers(session.State).TryGetValue(id, out int number)
        ? number.ToString(CultureInfo.InvariantCulture)
        : id.ToString(CultureInfo.InvariantCulture);

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
        session.Open(path, meta.Orientation);
        // Entry 41 section 2: the file's name, a salted hash of its path, and the whitelisted image facts, never its metadata block.
        DiagnosticLog.Info("image.open", [.. DiagnosticLog.File(path), .. ImageFacts.Of(meta)]);
        canvas.SetImage(new Bitmap(stream), max);
        int turns = session.State.ViewQuarterTurns;
        string orientation = ViewRotation.ExifMirrors(meta.Orientation)
            ? " Its orientation tag asks for a mirror image, which is not applied; rotate it if it needs turning."
            : turns != 0 ? string.Create(CultureInfo.InvariantCulture, $" Turned {90 * turns} degrees as its orientation tag asks.") : "";
        status.Text = string.Create(CultureInfo.InvariantCulture, $"{Path.GetFileName(path)}, {image.Width} by {image.Height} px{(meta.IsCamera ? $", {meta.CameraModel}" : "")}.{orientation} Set a scale, mark the point of aim, then tap each impact.");
        Refresh();
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

        session.SetCalibre(calibre);
    }

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
    /// The automatic path for a GroupLab sheet: choose the sheet's definition, then register, detect and assign on a background thread,
    /// and load the result as ordinary marks the user can correct. A failure is shown as a prominent message, never only in the trace
    /// (DESIGN.md section 19).
    /// </summary>
    private async Task DetectDialog()
    {
        if (grey is null || valueImage is null || metadata is null)
        {
            status.Text = "Open an image first.";
            return;
        }

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

        var read = GltdJsonReader.ReadFile(path);
        if (read.Definition is not { } definition)
        {
            problem.Text = "That file is not a readable GroupLab definition.";
            return;
        }

        status.Text = "Registering and detecting…";
        var (g, v, m) = (grey, valueImage, metadata);
        var trace = new TraceRecorder();
        var clock = System.Diagnostics.Stopwatch.StartNew();

        // Entry 41 section 5: a crash during detection carries the stages that ran, which already hold the resolved parameters and decisions.
        CrashReporter.InFlight = trace;
        AutomaticResult result;
        try
        {
            result = await Task.Run(() => AutomaticMarking.Run(g, v, m, definition, new OpenCvSharpBackend(), trace));
        }
        finally
        {
            CrashReporter.InFlight = null;
        }

        LogDetection(result, trace, clock.ElapsedMilliseconds);
        ApplyDetection(result);
    }

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
        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Summary);
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

    private void AskRectangle()
    {
        scaleInputs.Children.Clear();
        var width = new TextBox { Text = "1", Width = 70 };
        var height = new TextBox { Text = "1", Width = 70 };
        var unit = units.Linear;
        scaleInputs.Children.Add(new TextBlock { Text = $"Drag any corner onto its mark if it is not on it, then enter the rectangle's width (first to second corner) and height, {UnitSettings.Symbol(unit)}:", TextWrapping = TextWrapping.Wrap });
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
        canvas.FlaggedShots = holeFlags.Select(f => f.ShotId).ToHashSet();

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
        if (report.AllShots is { } all)
        {
            var reduced = report.WithoutExclusions!;
            bool excluded = report.Excluded > 0;
            statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"{all.Shots} shots{(excluded ? $", {reduced.Shots} without the {report.Excluded} excluded" : "")}{(report.NotShots > 0 ? $"; {report.NotShots} marked not a shot" : "")}")));
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
                statistics.Children.Add(new TextBlock
                {
                    Text = all.ExtremeSpreadEdgeToEdge is { } edgeToEdge
                        ? $"Edge to edge, across the outsides of the holes: {units.Length(edgeToEdge)}{(units.AngleText(edgeToEdge, state.ShotDistanceInches) is { } angle ? ", " + angle : "")}, which is centre to centre plus one {units.Length(state.Calibre!.DiameterInches)} bullet."
                        : $"Edge to edge: {all.ExtremeSpreadEdgeToEdgeUnavailable}.",
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { AppStyles.Secondary },
                });
                if (state.ShotDistanceInches is null)
                {
                    statistics.Children.Add(Line("Angular figures need the shot distance."));
                }

                if (all.Shots < GroupAnalysis.SmallGroupShots && all.TrueSizeRange is { } range)
                {
                    statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                        $"From {all.Shots} shots the true group size could be anywhere from {range.Lower:0.00} to {range.Upper:0.00} times what they measure (STATISTICS.md section 9.1).")));
                }

                statistics.Children.Add(Line(all.AspectRatio is { } aspect
                    ? string.Create(CultureInfo.InvariantCulture, $"Error ellipse aspect {aspect:0.00}, major axis at {DisplayedAngle(all.AngleDegrees ?? 0):0} degrees")
                    : $"Error ellipse: {all.AspectRatioUnavailable}."));
                if (all.WorstShotInMeanRadii is { } worst)
                {
                    statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                        $"Worst shot at {worst:0.00} mean radii; a group of {all.Shots} is expected to put its worst at {all.ExpectedWorstInMeanRadii:0.00}, so a shot there is not a flyer by that measure alone (STATISTICS.md section 10).")));
                }
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

            statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"Placed: {report.Automatic} automatic, {report.Corrected} corrected, {report.Manual} by hand")));
        }

        BuildShotList();
        BuildSelection();
    }

    /// <summary>
    /// Every shot as a row, NOTES-FROM-PLANNING.md entry 39 section 4: its number as the image shows it, its bull, and whether it is
    /// excluded or not a shot. A row selects its shot on the image, and carries the exclusion control, so excluding a flyer does not
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

        var numbers = MarkingCanvas.ShotNumbers(state);
        foreach (var shot in state.Shots)
        {
            int id = shot.Id;
            string bull = shot.Bull is { } b ? state.Bulls.FirstOrDefault(x => x.Index == b)?.Label ?? b.ToString(CultureInfo.InvariantCulture) : "none";
            string text = shot.NotAShot
                ? $"not a shot, bull {bull}"
                : string.Create(CultureInfo.InvariantCulture, $"{numbers[id]}  bull {bull}{(shot.Exclusion is { } e ? $", excluded as {e}" : "")}");
            var select = new Button
            {
                Content = text,
                Tag = id,
                FontFamily = Mono,
                MinWidth = 230,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontWeight = id == canvas.Selected ? FontWeight.Bold : FontWeight.Normal,
                Margin = new Thickness(0),
            };
            select.Click += (_, _) =>
            {
                canvas.Selected = id;
                Refresh();
            };
            var row = Row(select);
            if (!shot.NotAShot)
            {
                row.Children.Add(Button(shot.Exclusion is null ? "Exclude" : "Restore", () =>
                    session.SetExclusion(id, shot.Exclusion is null ? Enum.Parse<ExclusionReason>((string)exclusionReason.SelectedItem!) : null)));
            }

            shotList.Children.Add(row);
        }
    }

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

        string bull = shot.Bull is { } b ? session.State.Bulls.FirstOrDefault(x => x.Index == b)?.Label ?? b.ToString(CultureInfo.InvariantCulture) : "none";
        selection.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
            $"Shot {ShotLabel(id)}: {shot.Provenance.ToString().ToLowerInvariant()}, bull {bull}{(shot.Exclusion is { } e ? $", excluded as {e}" : "")}{(shot.NotAShot ? ", not a shot" : "")}")));
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
