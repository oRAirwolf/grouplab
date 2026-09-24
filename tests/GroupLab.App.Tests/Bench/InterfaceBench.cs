using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using GroupLab.App;

namespace GroupLab.App.Tests.Bench;

/// <summary>
/// What one control cost, NOTES-FROM-PLANNING.md entry 117 section 3b. Three times, because they answer different complaints: the freeze a
/// person feels, the moment nothing further is coming, and the moment the screen is finished. The layout passes and whether the work ran on
/// the interface thread are recorded beside them, so a control that is fast because it painted nothing, or slow because it did its arithmetic
/// on the interface thread, is visible in the record as such.
/// </summary>
public sealed record ControlTiming(string Screen, string Label, string Kind, double ResponsiveMs, double SettledMs, double PaintedMs, int LayoutPasses, bool OnTheInterfaceThread, string? Note);

/// <summary>
/// The interface half of <c>grouplab bench</c>, entry 117 section 3b: every control, found by walking the window rather than by a list, each
/// clicked and timed from the click to the moment the thing it asked for is finished.
/// <para>
/// It lives in the test project because it needs a windowing platform, which the command line build does not carry. The rest of the benchmark
/// is <c>grouplab bench</c>, and the two tables sit side by side in <c>docs/PERFORMANCE.md</c>.
/// </para>
/// </summary>
public static class InterfaceBench
{
    /// <summary>How long a control is given to settle before the record says it did not.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);

    /// <summary>
    /// What is not clicked, by name with its reason, entry 117 section 3b: "a named exclusion list, short and justified in one line each, for
    /// the controls that cannot run unattended. Exclusion is by name in the record, never by silence."
    /// </summary>
    public static IReadOnlyDictionary<string, string> NotClicked { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Open image…"] = "It opens the operating system's file picker, which waits for a person. Opening the file afterwards is measured as its own case.",
        ["Open, export or report a problem"] = "Its items open file pickers, which wait for a person.",
        ["Report a problem…"] = "It writes a report package and then opens a file picker.",
        ["Print…"] = "It prints to a device. Since entry 155 it is the Targets panel's own, and it is there on Windows only.",
        ["Design your own sheet"] = "It opens the designer in the Targets panel, which is walked as its own screen when the editor benchmark is written.",
        ["Duplicate"] = "It writes a sheet into the person's own library, which is data a benchmark must not add to.",
        ["Read the list"] = "It reads the clipboard, which belongs to whoever is at the machine.",
    };

    /// <summary>Excluded controls that only one operating system shows, so their absence elsewhere is not a stale excuse.</summary>
    public static IReadOnlySet<string> WindowsOnly { get; } = new HashSet<string>(StringComparer.Ordinal) { "Print…" };

    /// <summary>Times every control the walk finds on the window as it stands, leaving the excluded ones out by name.</summary>
    public static IReadOnlyList<ControlTiming> Measure(MainWindow window, string screen)
    {
        ArgumentNullException.ThrowIfNull(window);
        var timings = new List<ControlTiming>();
        var unnamed = 0;
        foreach (var clickable in ControlWalk.On(window, screen))
        {
            if (NotClicked.ContainsKey(clickable.Label))
            {
                continue;
            }

            timings.Add(Time(window, clickable, ref unnamed));
        }

        return timings;
    }

    /// <summary>
    /// One control, clicked and timed. To responsive is the click handler's own synchronous time, which is exactly the freeze; to settled adds
    /// everything that click started; to final paint adds the layout that follows it.
    /// </summary>
    private static ControlTiming Time(MainWindow window, Clickable clickable, ref int unnamed)
    {
        int passes = 0;
        void Counted(object? sender, EventArgs e) => passes++;
        window.LayoutUpdated += Counted;
        string? note = null;
        var clock = Stopwatch.StartNew();
        double responsive, settled, painted;
        try
        {
            Invoke(clickable);
            responsive = clock.Elapsed.TotalMilliseconds;
            settled = Settle(window, clock);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            painted = clock.Elapsed.TotalMilliseconds;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            window.LayoutUpdated -= Counted;
            return new ControlTiming(clickable.Screen, Name(clickable, ref unnamed), clickable.Kind, 0, 0, 0, 0, false,
                "not measured: " + ex.GetType().Name + ", " + ex.Message.ReplaceLineEndings(" "));
        }

        window.LayoutUpdated -= Counted;
        if (settled >= Patience.TotalMilliseconds)
        {
            note = string.Create(CultureInfo.InvariantCulture, $"did not settle within {Patience.TotalSeconds:0} s, so the figure is a floor and not a measurement");
        }

        // Work done on the interface thread is work a person feels: everything before the first await is in the responsive figure.
        bool onTheInterfaceThread = responsive > 16;
        return new ControlTiming(clickable.Screen, Name(clickable, ref unnamed), clickable.Kind, responsive, settled, painted, passes, onTheInterfaceThread, note);
    }

    /// <summary>A control that says nothing keeps a name that does not move between runs: its kind and its place on the screen.</summary>
    private static string Name(Clickable clickable, ref int unnamed) =>
        clickable.Label.StartsWith("ComboBox ", StringComparison.Ordinal) || clickable.Label.StartsWith("Button ", StringComparison.Ordinal)
            ? clickable.Kind + " " + (++unnamed) + ", unnamed"
            : clickable.Label;

    private static void Invoke(Clickable clickable)
    {
        switch (clickable.Control)
        {
            case ComboBox box when box.ItemCount > 1:
                box.SelectedIndex = (box.SelectedIndex + 1) % box.ItemCount;
                break;
            // A dropdown is measured by choosing from it, which is the work. Opening the popup itself is the platform's, and headlessly it
            // has no screen to open onto.
            case ComboBox box:
                box.SelectedIndex = box.ItemCount > 0 ? 0 : -1;
                break;
            case ListBox list when list.ItemCount > 0:
                list.SelectedIndex = 0;
                break;
            case Expander expander:
                expander.IsExpanded = !expander.IsExpanded;
                break;
            case TabItem tab:
                tab.IsSelected = true;
                break;
            case ToggleButton toggle:
                toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                break;
            case Button button:
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                break;
            case MenuItem item:
                item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Runs the interface until nothing further is coming: no dispatcher job left to run, and no detection or analysis still in flight. Not
    /// the moment something appeared.
    /// </summary>
    private static double Settle(MainWindow window, Stopwatch clock)
    {
        while (clock.Elapsed < Patience)
        {
            Dispatcher.UIThread.RunJobs();
            if (window.DetectionTask is null or { IsCompleted: true })
            {
                // One more pump: a task that has just completed leaves its continuation on the interface thread.
                Dispatcher.UIThread.RunJobs();
                if (window.DetectionTask is null or { IsCompleted: true })
                {
                    break;
                }
            }

            Thread.Sleep(5);
        }

        return clock.Elapsed.TotalMilliseconds;
    }

    /// <summary>The interface's own table for the record, worst first, which is the work queue.</summary>
    public static string Markdown(IReadOnlyList<ControlTiming> timings, IReadOnlyDictionary<string, string> notClicked)
    {
        ArgumentNullException.ThrowIfNull(timings);
        ArgumentNullException.ThrowIfNull(notClicked);
        var text = new System.Text.StringBuilder();
        text.Append("## The interface, control by control\n\n");
        text.Append("Every control a person can click, found by walking the window rather than from a list. **To responsive** is the freeze: the\n");
        text.Append("click handler's own time on the interface thread. **To settled** is the moment nothing further is coming. **To painted** is the\n");
        text.Append("screen finished. Passes is how many layout passes the click caused.\n\n");
        text.Append("| Screen | Control | Responsive | Settled | Painted | Passes | |\n|---|---|---|---|---|---|---|\n");
        foreach (var t in timings.OrderByDescending(t => t.SettledMs))
        {
            string flag = t.Note ?? (t.OnTheInterfaceThread ? "on the interface thread" : "");
            text.Append(CultureInfo.InvariantCulture, $"| {t.Screen} | {t.Label} | {t.ResponsiveMs:0.0} | {t.SettledMs:0.0} | {t.PaintedMs:0.0} | {t.LayoutPasses} | {flag} |\n");
        }

        text.Append("\n### Controls not clicked, by name\n\n| Control | Why |\n|---|---|\n");
        foreach (var (name, why) in notClicked.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            text.Append(CultureInfo.InvariantCulture, $"| {name} | {why} |\n");
        }

        return text.ToString();
    }
}
