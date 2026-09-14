using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using GroupLab.Cli.Imaging;
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
    private static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Menlo, monospace");

    private readonly MarkingSession session = new();
    private readonly MarkingCanvas canvas = new();
    private readonly Dictionary<MarkingTool, ToggleButton> toolButtons = [];
    private readonly TextBlock status = new() { Margin = new Thickness(8, 4), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock problem = new() { Foreground = Brushes.OrangeRed, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly StackPanel statistics = new() { Spacing = 4 };
    private readonly StackPanel selection = new() { Spacing = 6 };
    private readonly StackPanel scaleInputs = new() { Spacing = 6 };
    private readonly ComboBox exclusionReason = new() { ItemsSource = Enum.GetNames<ExclusionReason>(), SelectedIndex = 0, MinWidth = 140 };
    private GrayImage? grey;
    private GrayImage? valueImage;
    private ImageMetadata? metadata;

    public MainWindow()
    {
        Title = "GroupLab";
        Width = 1400;
        Height = 900;
        canvas.Session = session;
        session.Changed += (_, _) => Refresh();
        canvas.SelectionChanged += (_, _) => Refresh();
        canvas.LengthTapped += (_, taps) => AskLength(taps);
        canvas.RectangleTapped += (_, taps) => AskRectangle(taps);

        var toolbar = new WrapPanel { Margin = new Thickness(6), Orientation = Orientation.Horizontal };
        toolbar.Children.Add(Button("Open image", async () => await OpenImageDialog()));
        toolbar.Children.Add(Button("Detect on a GroupLab sheet", async () => await DetectDialog()));
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
        toolbar.Children.Add(Button("Export", async () => await ExportDialog()));

        var panel = new StackPanel { Margin = new Thickness(12), Spacing = 12, Width = 380 };
        panel.Children.Add(Heading("Scale"));
        panel.Children.Add(scaleInputs);
        panel.Children.Add(Heading("Group"));
        panel.Children.Add(problem);
        panel.Children.Add(statistics);
        panel.Children.Add(Heading("Selected shot"));
        panel.Children.Add(selection);

        var side = new ScrollViewer { Content = panel };
        var dock = new DockPanel();
        DockPanel.SetDock(toolbar, Dock.Top);
        DockPanel.SetDock(status, Dock.Bottom);
        DockPanel.SetDock(side, Dock.Right);
        dock.Children.Add(toolbar);
        dock.Children.Add(status);
        dock.Children.Add(side);
        dock.Children.Add(canvas);
        Content = dock;
        SetTool(MarkingTool.Pan);
        Refresh();
    }

    /// <summary>The canvas, for the headless tests.</summary>
    internal MarkingCanvas Canvas => canvas;

    /// <summary>The session, for the headless tests.</summary>
    internal MarkingSession Session => session;

    /// <summary>
    /// Opens an image. It is decoded once through OpenCV without applying EXIF orientation, the same decode the pipeline measures, and
    /// shown from those same pixels, so a mark on the screen is a mark on the pixels the statistics and the detector use.
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
        session.Open(path);
        canvas.SetImage(new Bitmap(stream), max);
        status.Text = string.Create(CultureInfo.InvariantCulture, $"{Path.GetFileName(path)}, {image.Width} by {image.Height} px{(meta.IsCamera ? $", {meta.CameraModel}" : "")}. Set a scale, mark the point of aim, then tap each impact.");
        Refresh();
    }

    private async Task OpenImageDialog()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a photograph or scan of a target",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.jpg", "*.jpeg", "*.png"] }],
        });
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

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose the sheet's definition",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("GroupLab definitions") { Patterns = ["*.gltd.json"] }],
        });
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
        var result = await Task.Run(() => AutomaticMarking.Run(g, v, m, definition, new OpenCvSharpBackend()));
        if (result.Failure is not null || result.Scale is null)
        {
            problem.Text = "Detection failed: " + (result.Failure ?? "no registration") + ". Mark this image by hand with a reference length or rectangle.";
            status.Text = result.Summary;
            return;
        }

        canvas.MissingMarkers = result.MissingMarkers;
        session.LoadDetections(result.Scale, result.Bulls, result.Detections, result.Summary);
        SetTool(MarkingTool.Select);
        status.Text = result.Summary + (result.MissingMarkers.Count > 0 ? string.Create(CultureInfo.InvariantCulture, $"; {result.MissingMarkers.Count} markers not found, crossed in red") : "");
    }

    private async Task ExportDialog()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export the marking",
            SuggestedFileName = Path.GetFileNameWithoutExtension(session.State.ImagePath ?? "group") + ".grouplab.json",
            DefaultExtension = "json",
        });
        if (file?.TryGetLocalPath() is { } path)
        {
            await File.WriteAllTextAsync(path, GroupAnalysis.Export(session.State));
            status.Text = "Exported to " + path;
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
            MarkingTool.Length => "Tap two points a known distance apart, then enter the distance.",
            MarkingTool.Rectangle => "Tap four corners of a known rectangle, top left first and around, then enter its size. This removes perspective.",
            MarkingTool.Aim => "Tap the point of aim.",
            MarkingTool.Impact => "Tap each impact. Each tap snaps to the hole under it.",
            MarkingTool.Select => "Tap a shot to select it and drag to move it. With a shot selected, tap a bull to assign the shot to it.",
            _ => status.Text,
        };
    }

    private void AskLength(IReadOnlyList<PointD> taps)
    {
        scaleInputs.Children.Clear();
        var inches = new TextBox { Text = "1", Width = 80 };
        scaleInputs.Children.Add(new TextBlock { Text = "Distance between the two taps, inches:", TextWrapping = TextWrapping.Wrap });
        scaleInputs.Children.Add(Row(inches, Button("Use this length", () =>
        {
            if (double.TryParse(inches.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d > 0)
            {
                session.SetScale(new LengthReference(taps[0], taps[1], d));
                SetTool(MarkingTool.Aim);
            }
        })));
    }

    private void AskRectangle(IReadOnlyList<PointD> taps)
    {
        scaleInputs.Children.Clear();
        var width = new TextBox { Text = "1", Width = 70 };
        var height = new TextBox { Text = "1", Width = 70 };
        scaleInputs.Children.Add(new TextBlock { Text = "Rectangle width (first to second tap) and height, inches:", TextWrapping = TextWrapping.Wrap });
        scaleInputs.Children.Add(Row(width, height, Button("Use this rectangle", () =>
        {
            if (double.TryParse(width.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && w > 0
                && double.TryParse(height.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) && h > 0)
            {
                try
                {
                    session.SetScale(new RectangleReference(taps, w, h));
                    SetTool(MarkingTool.Aim);
                }
                catch (ArgumentException ex)
                {
                    problem.Text = ex.Message;
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

        if (scaleInputs.Children.Count == 0 || state.Scale is not null)
        {
            scaleInputs.Children.Clear();
            scaleInputs.Children.Add(new TextBlock
            {
                Text = state.Scale is null ? "No scale yet. Choose Scale: length or Scale: rectangle, or detect on a GroupLab sheet." : "From " + report.Scale + ".",
                TextWrapping = TextWrapping.Wrap,
                Foreground = report.ScaleAssumesSquareOn ? Brushes.DarkOrange : null,
            });
        }

        statistics.Children.Clear();
        if (report.AllShots is { } all)
        {
            var reduced = report.WithoutExclusions!;
            statistics.Children.Add(Figure("Mean radius", all.MeanRadius, reduced.MeanRadius, 26, FontWeight.Bold));
            statistics.Children.Add(Figure("Sigma", all.Sigma, reduced.Sigma, 16, FontWeight.Normal));
            statistics.Children.Add(Figure("Extreme spread", all.ExtremeSpread, reduced.ExtremeSpread, 12, FontWeight.Normal, subordinate: true));
            statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"{all.Shots} shots{(report.Excluded > 0 ? $", {reduced.Shots} without the {report.Excluded} excluded" : "")}{(report.NotShots > 0 ? $"; {report.NotShots} marked not a shot" : "")}")));
            if (all.CentreFromAim is { } centre)
            {
                statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"Centre from aim: {Math.Abs(centre.X):0.000} in {(centre.X >= 0 ? "right" : "left")}, {Math.Abs(centre.Y):0.000} in {(centre.Y >= 0 ? "low" : "high")}")));
            }

            if (!double.IsNaN(all.AspectRatio))
            {
                statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"Error ellipse aspect {all.AspectRatio:0.00}, major axis at {all.AngleDegrees:0} degrees")));
            }

            statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture,
                $"Worst shot at {all.WorstShotInMeanRadii:0.00} mean radii; a group of {all.Shots} is expected to put its worst at {all.ExpectedWorstInMeanRadii:0.00}, so a shot there is not a flyer by that measure alone (STATISTICS.md section 10).")));
            statistics.Children.Add(Line(string.Create(CultureInfo.InvariantCulture, $"Placed: {report.Automatic} automatic, {report.Corrected} corrected, {report.Manual} by hand")));
        }

        BuildSelection();
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
            $"Shot {id}: {shot.Provenance.ToString().ToLowerInvariant()}, bull {bull}{(shot.Exclusion is { } e ? $", excluded as {e}" : "")}{(shot.NotAShot ? ", not a shot" : "")}")));
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

    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 14, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) };

    private static TextBlock Line(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12 };

    /// <summary>
    /// One figure with its 95 percent interval, in the monospace with tabular figures DESIGN.md section 19 asks for, the value without
    /// exclusions beside it when there are any. Extreme spread is drawn smaller and dimmer: present, and visibly subordinate.
    /// </summary>
    private Control Figure(string name, Estimate all, Estimate reduced, double size, FontWeight weight, bool subordinate = false)
    {
        string Show(Estimate e) => double.IsNaN(e.Lower)
            ? string.Create(CultureInfo.InvariantCulture, $"{e.Value:0.000} in")
            : string.Create(CultureInfo.InvariantCulture, $"{e.Value:0.000} in  (95% {e.Lower:0.000} to {e.Upper:0.000})");
        var column = new StackPanel { Spacing = 0 };
        column.Children.Add(new TextBlock { Text = name, FontSize = 11, Opacity = subordinate ? 0.6 : 0.85 });
        column.Children.Add(new TextBlock { Text = Show(all), FontFamily = Mono, FontSize = size, FontWeight = weight, Opacity = subordinate ? 0.7 : 1 });
        if (session.State.Shots.Any(s => s.IsShot && s.Exclusion is not null))
        {
            column.Children.Add(new TextBlock { Text = "without exclusions: " + Show(reduced), FontFamily = Mono, FontSize = Math.Max(11, size * 0.55), Opacity = 0.8 });
        }

        return column;
    }
}
