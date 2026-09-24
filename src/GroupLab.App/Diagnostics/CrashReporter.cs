using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia.Threading;
using GroupLab.Core.Trace;
using GroupLab.Core.Updates;

namespace GroupLab.App.Diagnostics;

/// <summary>
/// What happens when the application fails, NOTES-FROM-PLANNING.md entry 41 section 5, writing the record entry 45 section 2 fixes.
/// <list type="number">
/// <item><b>Three handlers,</b> installed before the window exists: the process's unhandled exceptions, unobserved task exceptions, and
/// the dispatcher's, where an exception thrown in a click handler is marked handled so it does not tear the process down.</item>
/// <item><b>The smallest amount of work first,</b> because the process may be moments from dying: an ERROR line with the whole exception
/// chain and its stacks, flushed, and then a sibling <c>crash-YYYYMMDD-HHmmss-pid.json</c> holding the chain, the environment block, the
/// last action and the stage records of any analysis in flight.</item>
/// <item><b>The dialog is best effort and the file is not.</b> A crashing application often cannot draw, so the reliable path to a report
/// is the next launch: <see cref="PendingCrashes"/> lists every record not yet dealt with, and the window offers them.</item>
/// </list>
/// Every message and stack in the record has anything shaped like a path replaced, as the log's lines do.
/// </summary>
public static class CrashReporter
{
    public const int Schema = 1;
    private const string HandledSuffix = ".handled";
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    /// <summary>The analysis in flight, whose stage records a crash record carries: the seam between the trace and diagnostics (section 5).</summary>
    public static TraceRecorder? InFlight { get; set; }

    /// <summary>The window's display scale, once it has one.</summary>
    public static double? DisplayScale { get; set; }

    /// <summary>Raised with the crash record's path after a crash is recorded, on whatever thread the crash happened.</summary>
    public static event EventHandler<string>? Recorded;

    /// <summary>Installs the three handlers, recording into the given log's directory. Disposing the result removes them.</summary>
    public static IDisposable Install(DiagnosticLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        UnhandledExceptionEventHandler domain = (_, e) =>
            Record(log, e.ExceptionObject as Exception ?? new InvalidOperationException("a value that is not an exception was thrown"), "process");
        EventHandler<UnobservedTaskExceptionEventArgs> tasks = (_, e) =>
        {
            Record(log, e.Exception, "task");
            e.SetObserved();
        };
        // NOTES-FROM-PLANNING.md entry 192 section 3.3: an exception in a handler on the window's thread is recorded and swallowed, and the
        // application carries on. That is kept, and it is not always safe. A handler that throws part way leaves whatever it had changed
        // before the throw: the caliber Set button of entry 192 threw before it told the session anything, so nothing was left half done,
        // but a handler that has changed the marking and not yet saved it, or has changed one of two settings that go together, would leave
        // them apart. Whether to keep swallowing or offer to save and restart is for the planning session; this records which it was.
        DispatcherUnhandledExceptionEventHandler dispatcher = (_, e) =>
        {
            Record(log, e.Exception, "dispatcher");
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += domain;
        TaskScheduler.UnobservedTaskException += tasks;
        Dispatcher.UIThread.UnhandledException += dispatcher;
        return new Installed(() =>
        {
            AppDomain.CurrentDomain.UnhandledException -= domain;
            TaskScheduler.UnobservedTaskException -= tasks;
            Dispatcher.UIThread.UnhandledException -= dispatcher;
        });
    }

    /// <summary>Records one crash: the ERROR line, flushed, then the crash record. Returns the record's path, or null when it could not be written.</summary>
    public static string? Record(DiagnosticLog log, Exception exception, string source)
    {
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(exception);
        try
        {
            log.WriteException(LogLevel.Error, "app.crash", exception, ("source", source), ("last_action", DiagnosticLog.LastAction));
            log.Flush();
            if (log.Directory is not { } directory)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            string path = Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"crash-{now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.json"));
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Describe(exception, now, KindOf(source)).ToJsonString(Indented));
            }

            Recorded?.Invoke(null, path);
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            // A crash reporter that crashes helps nobody. The ERROR line, if it was written, is the record.
            return null;
        }
    }

    /// <summary>The crash record, entry 45 section 2, schema 1: exceptions outermost first, and the stages of an analysis in flight or none.</summary>
    internal static JsonObject Describe(Exception exception, DateTime utc, string kind = Closed)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var exceptions = new JsonArray();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            exceptions.Add(new JsonObject
            {
                ["type"] = current.GetType().FullName,
                ["message"] = DiagnosticLog.Scrub(current.Message),
                ["stack"] = DiagnosticLog.Scrub(current.StackTrace ?? ""),
            });
        }

        return new JsonObject
        {
            ["schema"] = Schema,
            ["kind"] = kind,
            ["created_utc"] = utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            ["app"] = new JsonObject { ["version"] = AppInfo.Version, ["commit"] = AppInfo.Commit, ["channel"] = AppInfo.Channel },
            ["environment"] = new JsonObject
            {
                ["os"] = AppInfo.OperatingSystemName,
                ["framework"] = AppInfo.Framework,
                ["renderer"] = AppInfo.Renderer,
                ["culture"] = CultureInfo.CurrentCulture.Name,
                ["display_scale"] = DisplayScale,
            },
            ["last_action"] = DiagnosticLog.LastAction,
            ["exceptions"] = exceptions,
            ["stages"] = Stages(InFlight),
        };
    }

    /// <summary>
    /// The two kinds of record, entry 192 section 3.2. Unholy was told GroupLab "closed unexpectedly 5 times" when it had closed not once:
    /// five exceptions on the window's thread were recorded and survived. A record now says which it was: an error GroupLab survived, or an
    /// exit without a clean shutdown, which is a crash of the process or a run that ended without reaching its own exit.
    /// </summary>
    public const string Survived = "survived";

    public const string Closed = "closed";

    /// <summary>The kind a handler's source means: the window's thread and unobserved tasks carry on; the process does not.</summary>
    internal static string KindOf(string source) => source is "dispatcher" or "task" ? Survived : Closed;

    /// <summary>A record's kind, read from it; a record written before kinds were recorded is taken as a close, as it was described then.</summary>
    public static string KindOfRecord(string crash)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(crash))?["kind"]?.GetValue<string>() is Survived ? Survived : Closed;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or FormatException)
        {
            return Closed;
        }
    }

    /// <summary>
    /// A run that ends without reaching its own exit leaves no exception to record, so each run leaves a marker named with its process
    /// id and removes it on a clean exit (entry 192 section 3.2). A marker whose process is gone at the next start is a run that closed
    /// without shutting down, and is recorded as one, unless the run recorded its own crash first.
    /// </summary>
    public static void BeginRun(DiagnosticLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        if (log.Directory is not { } directory)
        {
            return;
        }

        try
        {
            foreach (string marker in Directory.EnumerateFiles(directory, "running-*.marker"))
            {
                if (!int.TryParse(Path.GetFileNameWithoutExtension(marker)["running-".Length..], NumberStyles.None, CultureInfo.InvariantCulture, out int pid) || Alive(pid))
                {
                    continue;
                }

                DateTime began = File.GetLastWriteTimeUtc(marker);
                bool recorded = Directory.EnumerateFiles(directory, $"crash-*-{pid}.json").Any(c => File.GetLastWriteTimeUtc(c) >= began && KindOfRecord(c) == Closed);
                if (!recorded)
                {
                    string path = Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"crash-{began:yyyyMMdd-HHmmss}-{pid}.json"));
                    var record = Describe(new InvalidOperationException("GroupLab did not shut down cleanly, and nothing was recorded at the time."), began, Closed);
                    record["exceptions"] = new JsonArray();
                    record["stopped"] = "the run ended without reaching its own exit";
                    File.WriteAllText(path, record.ToJsonString(Indented));
                    log.Write(LogLevel.Warn, "app.unclean", [("pid", pid)]);
                }

                File.Delete(marker);
            }

            File.WriteAllText(Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"running-{Environment.ProcessId}.marker")), "");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "crash.marker", ex);
        }
    }

    /// <summary>A clean exit: this run's marker goes.</summary>
    public static void EndRun(DiagnosticLog log)
    {
        ArgumentNullException.ThrowIfNull(log);
        if (log.Directory is { } directory)
        {
            try
            {
                File.Delete(Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"running-{Environment.ProcessId}.marker")));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                DiagnosticLog.Exception(LogLevel.Warn, "crash.marker", ex);
            }
        }
    }

    private static bool Alive(int pid)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Entry 192 section 3.4: the records grouped by what went wrong, so five records of one error read as one error five times. The
    /// grouping is the exception's type and the first GroupLab frame in its stack, not Avalonia's, and each line says the kind and the last
    /// thing the person did.
    /// </summary>
    public static string Summary(IEnumerable<string> crashes)
    {
        ArgumentNullException.ThrowIfNull(crashes);
        var groups = new Dictionary<string, (int Count, string Kind, string? Last)>(StringComparer.Ordinal);
        foreach (string crash in crashes)
        {
            try
            {
                var record = JsonNode.Parse(File.ReadAllText(crash));
                string key = GroupKey(record);
                string kind = KindOfRecord(crash);
                groups[key] = groups.TryGetValue(key, out var seen) ? (seen.Count + 1, seen.Kind, (string?)record?["last_action"] ?? seen.Last) : (1, kind, (string?)record?["last_action"]);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                DiagnosticLog.Exception(LogLevel.Warn, "crash.summary", ex);
            }
        }

        return string.Join("\n", groups.OrderByDescending(g => g.Value.Count).Select(g => string.Create(CultureInfo.InvariantCulture,
            $"{g.Value.Count} time{(g.Value.Count == 1 ? "" : "s")}: {g.Key}, {(g.Value.Kind == Survived ? "survived" : "closed")}{(g.Value.Last is { } last ? ", after " + last : "")}")));
    }

    /// <summary>What went wrong, as the grouping reads it: the exception's type and the first GroupLab frame, without its line.</summary>
    internal static string GroupKey(JsonNode? record)
    {
        var first = record?["exceptions"]?.AsArray().FirstOrDefault();
        string type = (string?)first?["type"] ?? "no exception";
        string frame = ((string?)first?["stack"] ?? "").Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.StartsWith("at GroupLab.", StringComparison.Ordinal)) ?? "";
        return type + (frame.Length == 0 ? "" : " in " + frame[3..].Split('(')[0]);
    }

    /// <summary>Every crash record in the directory not yet dealt with, oldest first.</summary>
    public static IReadOnlyList<string> PendingCrashes(string? directory)
    {
        if (directory is null || !Directory.Exists(directory))
        {
            return [];
        }

        try
        {
            return [.. Directory.EnumerateFiles(directory, "crash-*.json").Where(f => !File.Exists(f + HandledSuffix)).Order(StringComparer.Ordinal)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "crash.list", ex);
            return [];
        }
    }

    /// <summary>Marks a crash record dealt with, so the next launch does not offer it again. The record itself stays.</summary>
    public static void MarkHandled(string crash)
    {
        ArgumentNullException.ThrowIfNull(crash);
        try
        {
            File.WriteAllText(crash + HandledSuffix, "");
            DiagnosticLog.Info("crash.handled", DiagnosticLog.File(crash));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "crash.handled", ex);
        }
    }

    /// <summary>Opens the folder holding a file in the system's file manager, the "Show me the file" of entry 41 section 6.</summary>
    public static void Reveal(string file)
    {
        ArgumentNullException.ThrowIfNull(file);
        try
        {
            // Entry 122: one way out of the process, so a test never opens a file manager on somebody's machine.
            TheOutsideWorld.Current.OpenFolder(Path.GetDirectoryName(Path.GetFullPath(file))!);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "crash.reveal", ex);
        }
    }

    private static JsonArray Stages(TraceRecorder? trace)
    {
        static JsonNode? Number(double value) => double.IsFinite(value) ? value : null;
        var stages = new JsonArray();
        foreach (var record in trace?.Records.ToList() ?? [])
        {
            stages.Add(new JsonObject
            {
                ["stage"] = record.Stage,
                ["sequence"] = record.Sequence,
                ["status"] = record.Status.ToString(),
                ["duration_ms"] = record.DurationMs,
                ["summary"] = DiagnosticLog.Scrub(record.Summary),
                ["parameters"] = new JsonArray([.. record.Parameters.Select(p => (JsonNode?)new JsonObject { ["name"] = p.Name, ["value"] = DiagnosticLog.Scrub(p.Value) })]),
                ["metrics"] = new JsonArray([.. record.Metrics.Select(m => (JsonNode?)new JsonObject { ["name"] = m.Name, ["value"] = Number(m.Value), ["unit"] = m.Unit })]),
                ["decisions"] = new JsonArray([.. record.Decisions.Select(d => (JsonNode?)new JsonObject { ["what"] = d.What, ["chosen"] = d.Chosen, ["alternatives"] = new JsonArray([.. d.Alternatives.Select(a => (JsonNode?)a)]), ["because"] = d.Because })]),
                ["rejections"] = new JsonArray([.. record.Rejections.Select(r => (JsonNode?)new JsonObject { ["what"] = r.What, ["x_inches"] = r.XInches, ["y_inches"] = r.YInches, ["why"] = r.Why })]),
                ["details"] = new JsonArray([.. record.Details.Select(line => (JsonNode?)DiagnosticLog.Scrub(line))]),
            });
        }

        return stages;
    }

    private sealed class Installed(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }
}
