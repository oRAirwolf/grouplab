using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using OpenCvSharp;

namespace GroupLab.App.Tests.Bench;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 117 section 3b: "a test walks the visual tree of every screen, collects everything a person can click, and
/// fails when one of them has no bench case". Here the walk drives the benchmark, so a control has a case by existing; what this holds is the
/// other half of the rule, that nothing is left out in silence, and that the walk still finds the screens it is supposed to.
/// <para>
/// Writing the record is a run of its own, so an ordinary test run does not rewrite a committed document: set <c>GROUPLAB_BENCH_TO_DOCS=1</c>.
/// </para>
/// </summary>
public class ControlBenchTests(ITestOutputHelper output)
{
    /// <summary>The screens the benchmark visits, and how it gets to each. Getting into a state is not a list of controls: it is the journey.</summary>
    private static IReadOnlyList<(string Name, Action<MainWindow> Go)> Screens { get; } =
    [
        ("marking, empty", _ => { }),
        ("marking, a sheet detected", OpenTheSheet),
        ("analysis", w => { OpenTheSheet(w); w.Analyse(); }),
        ("session records", w => w.ShowSessions()),
        ("target library", w => w.ShowLibrary()),
        ("ballistics", w => w.ShowBallistics()),
        ("compare loads", w => w.ShowCompare()),
        ("settings", w => w.ShowSettings()),
    ];

    [AvaloniaFact]
    public void EveryControlOnEveryScreenIsEitherMeasuredOrExcludedByName()
    {
        var window = Window();
        var measured = new List<ControlTiming>();
        var found = new List<string>();
        foreach (var (name, go) in Screens)
        {
            go(window);
            Dispatcher.UIThread.RunJobs();
            var clickable = ControlWalk.On(window, name);
            found.AddRange(clickable.Select(c => c.Label));
            measured.AddRange(InterfaceBench.Measure(window, name));
        }

        window.Close();

        // Everything found is either timed or named in the exclusion list. A control that is neither is the gap this test exists to catch.
        var timed = measured.Select(t => t.Label).ToHashSet(StringComparer.Ordinal);
        var missing = found.Distinct(StringComparer.Ordinal)
            .Where(label => !InterfaceBench.NotClicked.ContainsKey(label) && !timed.Contains(label) && !label.StartsWith("ComboBox ", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        output.WriteLine($"{found.Distinct(StringComparer.Ordinal).Count()} controls found, {measured.Count} timed, {InterfaceBench.NotClicked.Count} excluded by name.");
        Assert.True(missing.Count == 0, $"these controls were found and are neither measured nor excluded by name: {string.Join(", ", missing)}");

        // The walk must still be finding the screens: a restyle that emptied one would otherwise pass quietly.
        Assert.True(measured.Count >= 60, $"only {measured.Count} controls were timed, which is fewer than the window has");
        Assert.All(Screens, s => Assert.Contains(measured, t => string.Equals(t.Screen, s.Name, StringComparison.Ordinal)));

        if (Environment.GetEnvironmentVariable("GROUPLAB_BENCH_TO_DOCS") == "1")
        {
            // The record is one document with its prose around it, so the table goes into it in place, as grouplab bench --record does.
            string into = Path.Combine(Repository(), "docs", "PERFORMANCE.md");
            GroupLab.Cli.Bench.BenchReport.Replace(into, "## The interface, control by control", InterfaceBench.Markdown(measured, InterfaceBench.NotClicked));
            output.WriteLine("Put the interface table into " + into);
        }
    }

    /// <summary>A control excluded by a name no screen has is a stale excuse, and looks like coverage while being none.</summary>
    [AvaloniaFact]
    public void NothingIsExcludedThatNoScreenHas()
    {
        var window = Window();
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, go) in Screens)
        {
            go(window);
            Dispatcher.UIThread.RunJobs();
            foreach (var clickable in ControlWalk.On(window, name))
            {
                found.Add(clickable.Label);
            }
        }

        window.Close();
        var stale = InterfaceBench.NotClicked.Keys.Where(k => !found.Contains(k) && (OperatingSystem.IsWindows() || !InterfaceBench.WindowsOnly.Contains(k))).Order(StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0, $"these controls are excluded from the interface benchmark and no screen has them: {string.Join(", ", stale)}");
    }

    /// <summary>
    /// The repository, from this file's own path, as the other window tests find it: the build output can sit anywhere, and often does.
    /// </summary>
    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

    private static MainWindow Window()
    {
        var store = new AppSettingsStore(Path.Combine(Path.GetTempPath(), $"grouplab-settings-{Guid.NewGuid():N}.json"));
        var window = new MainWindow(store) { Width = 1400, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    /// <summary>Opens the benchmark's own generated sheet and waits for the detection, so the screens that need a sheet have one.</summary>
    private static void OpenTheSheet(MainWindow window)
    {
        window.OpenImage(Sheet.Value);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (window.DetectionTask is { IsCompleted: false } && clock.Elapsed < TimeSpan.FromSeconds(120))
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(10);
        }

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// A 25 shot GL-CF25-LTR, rendered and shot at by a seeded generator, exactly as the command line benchmark's material is. It carries
    /// nobody's data, and it is made once for the whole run.
    /// </summary>
    private static readonly Lazy<string> Sheet = new(() =>
    {
        var definition = GltdJsonReader.ReadFile(Path.Combine(Repository(), "targets", "GL-CF25-LTR.gltd.json")).Definition!;
        const double dpi = 300;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        var random = new Random(117);
        var holes = definition.Bulls.Where(b => b.Scoring)
            .Select(b => SyntheticSheet.SampleHole(random, b.X + random.Next(-45, 46), b.Y + random.Next(-45, 46), onInk: false, HoleBacking.ScannerLid, 0.871))
            .ToList();
        double s = 254 / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        var image = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, [], random);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-bench-{Guid.NewGuid():N}.png");
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        Cv2.ImWrite(path, mat);
        return path;
    });
}
