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
    private const string Tag = "GroupLabSpike";
    private readonly TextBlock _sizes = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBlock _runs = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly Button _run = new() { Content = "Run detection", MinHeight = 48, MinWidth = 160 };
    private readonly Grid _layout = new();
    private string? _widthClass;

    public SpikeView()
    {
        _run.Click += async (_, _) => await RunAll();
        var sizes = new StackPanel { Spacing = 8, Margin = new Thickness(16), Children = { new TextBlock { Text = "Screen", FontSize = 20 }, _sizes } };
        var runs = new StackPanel { Spacing = 8, Margin = new Thickness(16), Children = { _run, _runs } };
        _layout.Children.Add(sizes);
        _layout.Children.Add(runs);
        Content = new ScrollViewer { Content = _layout };
        SizeChanged += (_, e) => Measured(e.NewSize);
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
        Log(string.Create(CultureInfo.InvariantCulture,
            $"{DateTime.Now:HH:mm:ss} {size.Width:0} by {size.Height:0} dp, {widthClass}, {scale:0.###} pixels a dp, {size.Width * scale:0} by {size.Height * scale:0} pixels"), _sizes);
        if (widthClass == _widthClass)
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
        foreach (string folder in new[] { "targets", "images" })
        {
            foreach (string name in context.Assets!.List(folder) ?? [])
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
                    images.Add(to);
                }
            }
        }

        if (context.GetExternalFilesDir(null)?.AbsolutePath is { } pushed && Directory.Exists(pushed))
        {
            images.AddRange(Directory.EnumerateFiles(pushed).Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).Order());
        }

        return (targets, images);
    }

    private static void Log(string line, TextBlock into)
    {
        global::Android.Util.Log.Info(Tag, line);
        Dispatcher.UIThread.Post(() => into.Text = string.IsNullOrEmpty(into.Text) ? line : into.Text + "\n" + line);
    }
}
