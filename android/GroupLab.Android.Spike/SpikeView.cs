using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace GroupLab.Android.Spike;

public sealed class App : Avalonia.Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new SpikeView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>
/// Entries 198 section 2.4 and 199 section 1: a button that runs the engine on every image the spike has, and a record of every size
/// the screen has been. Each line also goes to the device log under the tag GroupLabSpike, so a run is read with
/// <c>adb logcat -s GroupLabSpike</c> without a screenshot.
/// </summary>
public sealed class SpikeView : UserControl
{
    internal const string LogTag = "GroupLabSpike";

    private static int attached;
    private readonly TextBlock _sizes = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBlock _runs = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly Avalonia.Controls.Button _run = new() { Content = "Run detection", MinHeight = 48, MinWidth = 160 };
    private readonly Avalonia.Controls.Button _folder = new() { Content = "Choose a folder", MinHeight = 48, MinWidth = 160 };
    private readonly Avalonia.Controls.Button _camera = new() { Content = "Camera", MinHeight = 48, MinWidth = 160 };
    private static SpikeView? shown;
    private readonly Grid _layout = new();
    private string? _widthClass;

    public SpikeView()
    {
        _run.Click += async (_, _) => await RunAll();
        shown = this;
        _camera.Click += async (_, _) =>
        {
            // Entry 219 item A2: the capture screen. The camera permission is asked for once; the preview needs it.
            if (!MainActivity.CameraAllowed())
            {
                Log("the camera needs permission: allow it, then press Camera again", _runs);
                return;
            }

            var (targets, _) = await Task.Run(Prepare);
            var owner = TopLevel.GetTopLevel(this);
            var before = Content;
            Content = new CaptureView(targets, () => Content = before);
        };
        _folder.Click += (_, _) => MainActivity.Current?.StartActivityForResult(
            new global::Android.Content.Intent(global::Android.Content.Intent.ActionOpenDocumentTree), MainActivity.FolderRequest);
        var sizes = new StackPanel { Spacing = 8, Margin = new Thickness(16), Children = { new TextBlock { Text = "Screen", FontSize = 20 }, _sizes } };
        var runs = new StackPanel { Spacing = 8, Margin = new Thickness(16), Children = { _run, _folder, _camera, _runs } };
        _layout.Children.Add(sizes);
        _layout.Children.Add(runs);
        Content = new ScrollViewer { Content = _layout };
        SizeChanged += (_, e) => Measured(e.NewSize);
        AttachedToVisualTree += (_, _) => Log($"{DateTime.Now:HH:mm:ss} view shown, the {++attached} time in this process", _sizes);
    }

    /// <summary>
    /// Entry 199 section 1.1: the layout follows the width it is given, in density independent units, not the kind of device. Compact is
    /// under 600, the phone and the Fold 7's cover screen; medium is under 840, the Fold 7 open; expanded is the tablet. Compact stacks the
    /// two panels; wider puts them side by side.
    /// </summary>
    private void Measured(Size size)
    {
        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        string widthClass = size.Width < 600 ? "compact" : size.Width < 840 ? "medium" : "expanded";

        // Entry 205 section 3.2: while the view is being made, and during a fold, Avalonia reports sizes of nothing, and sizes at 1 pixel a
        // dp before the screen's own density is known. They are logged as ignored and never laid out, not even for a frame.
        float density = global::Android.App.Application.Context.Resources?.DisplayMetrics?.Density ?? 1;
        bool degenerate = size.Width < 2 || size.Height < 2 || Math.Abs(scale - density) > 0.01;
        Log(string.Create(CultureInfo.InvariantCulture,
            $"{DateTime.Now:HH:mm:ss} {size.Width:0} by {size.Height:0} dp, {(degenerate ? "ignored" : widthClass)}, {scale:0.###} pixels a dp, {size.Width * scale:0} by {size.Height * scale:0} pixels"), _sizes);
        if (!degenerate && MainActivity.PendingTask is { } task)
        {
            MainActivity.PendingTask = null;
            _ = RunTask(task);
        }

        if (degenerate || widthClass == _widthClass)
        {
            return;
        }

        _widthClass = widthClass;
        _layout.ColumnDefinitions.Clear();
        _layout.RowDefinitions.Clear();
        bool stacked = widthClass == "compact";
        if (stacked)
        {
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _layout.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }
        else
        {
            _layout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            _layout.ColumnDefinitions.Add(new ColumnDefinition(2, GridUnitType.Star));
        }

        for (int i = 0; i < 2; i++)
        {
            Grid.SetRow((Control)_layout.Children[i], stacked ? i : 0);
            Grid.SetColumn((Control)_layout.Children[i], stacked ? 0 : i);
        }

        _layout.VerticalAlignment = VerticalAlignment.Top;
    }

    /// <summary>Entry 209: one named measurement, run once in a fresh process, its lines to the screen and the log.</summary>
    private async Task RunTask(string task)
    {
        if (task == "cameras")
        {
            foreach (string line in await Task.Run(() => SpikeCameras.Report(global::Android.App.Application.Context).ToList()))
            {
                Log(line, _runs);
            }

            return;
        }

        if (task.StartsWith("scale:", StringComparison.Ordinal) && double.TryParse(task[6..], NumberStyles.Float, CultureInfo.InvariantCulture, out double scale))
        {
            var (targets, images) = await Task.Run(Prepare);
            string sample = images.Last();
            Log(await Task.Run(() => SpikeScaled.Run(sample, [targets], scale)), _runs);
        }
    }

    private async Task RunAll()
    {
        _run.IsEnabled = false;
        try
        {
            var (targets, images) = await Task.Run(Prepare);
            Log($"{images.Count} images; the engine runs on each in turn.", _runs);
            foreach (string image in images)
            {
                string line = await Task.Run(() => SpikeRun.Run(image, [targets]));
                Log(line, _runs);
            }
        }
        finally
        {
            _run.IsEnabled = true;
        }
    }

    /// <summary>
    /// The sheets and the sample scan are copied out of the package into the cache, because the engine reads files. Any image put in the
    /// application's own folder with <c>adb push</c> is run as well: that is how a phone photograph reaches the spike without one being
    /// packaged, since photographs from the range are never committed.
    /// </summary>
    private static (string Targets, List<string> Images) Prepare()
    {
        var context = global::Android.App.Application.Context;
        string cache = context.CacheDir!.AbsolutePath;
        string targets = Path.Combine(cache, "targets");
        Directory.CreateDirectory(targets);
        var images = new List<string>();
        var samples = new List<string>();
        foreach (string folder in new[] { "targets", "images" })
        {
            // The asset list for a folder also holds the system's own files of the same folder name (the Fold 7 listed clock_font.png
            // among the images), so only the spike's own are taken: the sheets, and the published sample.
            foreach (string name in (context.Assets!.List(folder) ?? []).Where(n => n.EndsWith(".gltd.json", StringComparison.Ordinal) || n.StartsWith("gl-", StringComparison.Ordinal)))
            {
                string to = Path.Combine(folder == "targets" ? targets : cache, name);
                if (!File.Exists(to))
                {
                    using var from = context.Assets.Open($"{folder}/{name}");
                    using var file = File.Create(to);
                    from.CopyTo(file);
                }

                if (folder == "images")
                {
                    samples.Add(to);
                }
            }
        }

        if (context.GetExternalFilesDir(null)?.AbsolutePath is { } pushed && Directory.Exists(pushed))
        {
            images.AddRange(Directory.EnumerateFiles(pushed).Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Order());
        }

        // Pushed photographs first and the sample last: the peak memory is the whole process's highest so far, so an image run before the
        // large scan reports its own peak (entry 205 section 1).
        images.AddRange(samples);
        return (targets, images);
    }

    /// <summary>A line for the log from anywhere in the spike, shown under the runs as well.</summary>
    public static void Note(string line)
    {
        if (shown is { } view)
        {
            Log(line, view._runs);
        }
        else
        {
            global::Android.Util.Log.Info(LogTag, line);
        }
    }

    private static void Log(string line, TextBlock into)
    {
        global::Android.Util.Log.Info(LogTag, line);

        // Entry 219 item A2: every line also goes to spike-log.txt in the application's own folder, so a sitting's measurements can be
        // pulled over adb afterwards (adb pull /sdcard/Android/data/org.grouplab.app.spike/files/spike-log.txt) without anyone watching.
        try
        {
            if (global::Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath is { } folder)
            {
                File.AppendAllText(Path.Combine(folder, "spike-log.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
            // The log on screen and in logcat still has it.
        }

        Dispatcher.UIThread.Post(() => into.Text = string.IsNullOrEmpty(into.Text) ? line : into.Text + "\n" + line);
    }
}
