using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GroupLab.Core.Survey;

/// <summary>Whether the hardware survey may send, entry 208. Unset is not asked yet; nothing is sent until the person says yes.</summary>
public enum SurveyChoice
{
    Unset,
    Yes,
    No,
}

/// <summary>The machine, as docs/SURVEY.md section 2 lists it. Anything not known is null and is left out, never guessed.</summary>
public sealed record MachineFacts(
    string OperatingSystem,
    string Architecture,
    string? Processor,
    int Cores,
    long? MemoryMegabytes,
    string? Screen,
    double? ScreenScale,
    string? Device,
    double? CameraMegapixels);

/// <summary>One analysis: the image's size, the size it was analyzed at, each stage's time and the peak memory.</summary>
public sealed record AnalysisFacts(int Width, int Height, int WorkingWidth, int WorkingHeight, IReadOnlyList<StageTime> Stages, long PeakMegabytes);

/// <summary>
/// The hardware survey's report, docs/SURVEY.md section 2 and NOTES-FROM-PLANNING.md entry 207 section 3: what GroupLab runs on and how
/// fast, so the minimums rest on reports rather than guesses. <see cref="Keys"/> is every name the report can hold and the receiver
/// takes nothing else; <see cref="WhatIsSent"/> is how the question says it, in the same order. No name, account, file name, path,
/// photograph, location, time of day, serial number or advertising identifier is ever in it: none of them is even read.
/// </summary>
public static class SurveyReport
{
    public const string Schema = "grouplab-survey-1";

    /// <summary>Only the analyses since the last report go, and never more than this many.</summary>
    public const int MostAnalyses = 50;

    private const int LongestText = 120;

    /// <summary>Every name a report can hold, at every level. The receiver refuses a report with any other.</summary>
    public static IReadOnlyList<string> Keys { get; } =
    [
        "schema", "installation", "version", "machine", "benchmark", "analyses",
        "os", "architecture", "processor", "cores", "memoryMegabytes", "screen", "screenScale", "device", "cameraMegapixels",
        "workload", "width", "height", "totalMilliseconds", "stages", "peakMegabytes", "holesPlaced", "holesFound",
        "workingWidth", "workingHeight", "stage", "milliseconds",
    ];

    /// <summary>What a report holds, in plain words, for the question and for Settings.</summary>
    public static IReadOnlyList<string> WhatIsSent { get; } =
    [
        "Your operating system and its version, your processor's model and number of cores, and how much memory the machine has.",
        "Your screen's size and scale; on a phone, its model and the camera's resolution.",
        "GroupLab's version.",
        "For each analysis: the image's size, the size it was analyzed at, how long each step took, and the most memory it used.",
        "The benchmark's times, when you run it.",
        "A random number standing for this copy of GroupLab, so one machine is counted once. You can replace it in Settings at any time.",
        "Never your name, an account, a file name, a photograph, a location, or anything else that identifies you or the device.",
    ];

    /// <summary>A new random installation number: 32 hexadecimal digits, from nothing about the machine.</summary>
    public static string NewInstallation() => Guid.NewGuid().ToString("N");

    /// <summary>The report's JSON. Analyses beyond <see cref="MostAnalyses"/> are dropped, oldest first.</summary>
    public static string Build(string installation, string version, MachineFacts machine, BenchmarkResult? benchmark, IReadOnlyList<AnalysisFacts> analyses)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(analyses);
        var facts = new JsonObject
        {
            ["os"] = Cut(machine.OperatingSystem),
            ["architecture"] = Cut(machine.Architecture),
            ["cores"] = machine.Cores,
        };
        Add(facts, "processor", machine.Processor is { } p ? Cut(p) : null);
        Add(facts, "memoryMegabytes", machine.MemoryMegabytes);
        Add(facts, "screen", machine.Screen is { } s ? Cut(s) : null);
        Add(facts, "screenScale", machine.ScreenScale is { } k ? Math.Round(k, 2) : null);
        Add(facts, "device", machine.Device is { } d ? Cut(d) : null);
        Add(facts, "cameraMegapixels", machine.CameraMegapixels is { } c ? Math.Round(c, 1) : null);
        var report = new JsonObject
        {
            ["schema"] = Schema,
            ["installation"] = installation,
            ["version"] = Cut(version),
            ["machine"] = facts,
        };
        if (benchmark is { } b)
        {
            report["benchmark"] = new JsonObject
            {
                ["workload"] = b.Workload,
                ["width"] = b.Width,
                ["height"] = b.Height,
                ["totalMilliseconds"] = b.TotalMilliseconds,
                ["stages"] = Stages(b.Stages),
                ["peakMegabytes"] = b.PeakMegabytes,
                ["holesPlaced"] = b.HolesPlaced,
                ["holesFound"] = b.HolesFound,
            };
        }

        var kept = new JsonArray();
        foreach (var a in analyses.Skip(Math.Max(0, analyses.Count - MostAnalyses)))
        {
            kept.Add(new JsonObject
            {
                ["width"] = a.Width,
                ["height"] = a.Height,
                ["workingWidth"] = a.WorkingWidth,
                ["workingHeight"] = a.WorkingHeight,
                ["stages"] = Stages(a.Stages),
                ["peakMegabytes"] = a.PeakMegabytes,
            });
        }

        report["analyses"] = kept;
        return report.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>This machine as .NET sees it; the application adds the screen, and a phone its model and camera.</summary>
    public static MachineFacts ThisMachine(string? processor = null, string? screen = null, double? screenScale = null, string? device = null, double? cameraMegapixels = null)
    {
        long total = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        return new MachineFacts(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            processor ?? ProcessorName(),
            Environment.ProcessorCount,
            total > 0 ? total / (1024 * 1024) : null,
            screen,
            screenScale,
            device,
            cameraMegapixels);
    }

    /// <summary>
    /// The processor's model where the system says it without running anything: the registry on Windows, <c>/proc/cpuinfo</c> on Linux.
    /// Elsewhere null; macOS keeps it behind a program, and running one is not worth a name.
    /// </summary>
    public static string? ProcessorName()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return (key?.GetValue("ProcessorNameString") as string)?.Trim();
            }

            if (OperatingSystem.IsLinux() && File.Exists("/proc/cpuinfo"))
            {
                return File.ReadLines("/proc/cpuinfo")
                    .Where(l => l.StartsWith("model name", StringComparison.Ordinal) || l.StartsWith("Hardware", StringComparison.Ordinal))
                    .Select(l => l[(l.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim())
                    .FirstOrDefault(l => l.Length > 0);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }

        return null;
    }

    private static JsonArray Stages(IReadOnlyList<StageTime> stages) =>
        [.. stages.Select(s => (JsonNode)new JsonObject { ["stage"] = Cut(s.Stage), ["milliseconds"] = s.Milliseconds })];

    private static void Add(JsonObject o, string key, JsonNode? value)
    {
        if (value is not null)
        {
            o[key] = value;
        }
    }

    private static string Cut(string text) => text.Length <= LongestText ? text : text[..LongestText];
}
