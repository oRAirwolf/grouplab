using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests.Bench;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 388 section 4, Phase 9's baseline: the waits in the window that <c>grouplab bench</c> cannot see, measured
/// headless on the window itself. A new window ready to use, a 600 dpi scan opened as Open opens it (decoded off the window's thread and
/// shown), and switching between three open targets, each with the scan in it. One run is thrown away and five are timed, as the bench
/// does. An ordinary run measures and holds nothing; <c>GROUPLAB_PERF_BASELINE=1</c> writes the figures into docs/performance-baseline.json,
/// and <c>GROUPLAB_PERF_GATE=1</c> holds them to it as <see cref="GroupLab.Cli.Bench.BenchGate"/> holds the bench's.
/// </summary>
public class Phase9BaselineTests(ITestOutputHelper output)
{
    private const int Runs = 5;

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", ".."));

    private static string Scan => Path.Combine(Repository(), "samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");

    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static MainWindow NewWindow(string folder)
    {
        var store = new AppSettingsStore(Path.Combine(folder, $"settings-{Guid.NewGuid():N}.json"));
        store.SaveUnits(UnitSettings.Imperial);
        return new MainWindow(store) { Width = 1400, Height = 900 };
    }

    /// <summary>The median, fastest and slowest of <see cref="Runs"/> timed runs after one thrown away.</summary>
    private static (double Median, double Low, double High) Time(Action run)
    {
        run();
        var times = new List<double>();
        for (int i = 0; i < Runs; i++)
        {
            var clock = Stopwatch.StartNew();
            run();
            times.Add(clock.Elapsed.TotalMilliseconds);
        }

        times.Sort();
        return (times[Runs / 2], times[0], times[^1]);
    }

    [AvaloniaFact]
    public void TheWaitsInTheWindowAreMeasured()
    {
        string folder = Directory.CreateDirectory(GroupLab.Tests.Support.Temp.Folder("phase9")).FullName;
        var figures = new Dictionary<string, (double Median, double Low, double High)>(StringComparer.Ordinal);
        try
        {
            figures["a new window ready to use"] = Time(() =>
            {
                var window = NewWindow(folder);
                window.Show();
                Settle();
                window.Close();
            });

            var main = NewWindow(folder);
            main.Show();
            Settle();
            figures["open a 600 dpi scan"] = Time(() =>
            {
                _ = main.OpenImageSafely(Scan);
                Assert.True(main.OpenFinished());
                Settle();
            });

            // Three targets open, each with the scan in it, and the window going round them.
            main.AddTarget(choose: false);
            main.OpenImage(Scan);
            main.AddTarget(choose: false);
            main.OpenImage(Scan);
            Settle();
            Assert.Equal(3, main.Targets.Count);
            int next = 0;
            figures["switch between three open targets"] = Time(() =>
            {
                next = (next + 1) % 3;
                Assert.True(main.ShowTarget(next));
                Settle();
            });
            main.Close();
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }

        foreach (var (name, (median, low, high)) in figures)
        {
            output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{name}: {median:0.0} ms ({low:0.0} to {high:0.0})"));
        }

        string path = Path.Combine(Repository(), "docs", "performance-baseline.json");
        if (Environment.GetEnvironmentVariable("GROUPLAB_PERF_BASELINE") == "1")
        {
            var baseline = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            var window = new JsonObject();
            foreach (var (name, (median, _, _)) in figures)
            {
                window[name] = Math.Round(median, 1);
            }

            baseline["window"] = window;
            File.WriteAllText(path, baseline.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n");
        }

        if (Environment.GetEnvironmentVariable("GROUPLAB_PERF_GATE") == "1")
        {
            var held = JsonNode.Parse(File.ReadAllText(path))!["window"]!.AsObject();
            var slow = held.Where(h => !figures.TryGetValue(h.Key, out var now)
                    || (now.Median > h.Value!.GetValue<double>() * (1 + GroupLab.Cli.Bench.BenchGate.Slower) && now.Median - h.Value!.GetValue<double>() > GroupLab.Cli.Bench.BenchGate.FloorMs))
                .Select(h => h.Key).ToList();
            Assert.True(slow.Count == 0, "slower than the baseline allows: " + string.Join(", ", slow));
        }
    }
}
