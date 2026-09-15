using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace GroupLab.App.Diagnostics;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

/// <summary>
/// The application's diagnostic log, NOTES-FROM-PLANNING.md entry 41 section 3. The first version of the application logged nothing, so
/// when the print screen crashed nobody could say why.
/// <list type="bullet">
/// <item><b>One file per run,</b> <c>grouplab-YYYYMMDD-HHmmss-pid.log</c>, named in UTC so the files sort.</item>
/// <item><b>Plain text, one event per line.</b> A UTC timestamp with milliseconds, the level, the event name, then <c>key=value</c> pairs,
/// quoted only when a value holds a space. An exception's stack follows on indented continuation lines.</item>
/// <item><b>Rotation at startup:</b> the newest twenty files or 20 MB in total, whichever bites first. Never unbounded.</item>
/// <item><b>A background writer</b> behind a bounded queue. When the queue is full, DEBUG lines are dropped first; what is dropped is
/// counted and recorded in one WARN. ERROR is flushed at once; everything else within two seconds.</item>
/// <item><b>Failure is silent and total.</b> If the directory cannot be created or written, logging is off, <see cref="DisabledReason"/>
/// says why for the settings panel, and nothing inside the logger can throw into the application.</item>
/// </list>
/// <para>
/// <b>What it never writes (section 2):</b> a location, a photograph's metadata block, or a path. A file is recorded by its name and a
/// salted hash of its full path, <see cref="FileFields"/>; image facts come only through <see cref="ImageFacts"/>; and every value and every
/// exception message and stack has anything shaped like a path replaced before it is queued.
/// </para>
/// </summary>
public sealed partial class DiagnosticLog : IDisposable
{
    public const int KeepFiles = 20;
    public const long KeepBytes = 20L * 1024 * 1024;
    private const int QueueCapacity = 4096;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly BlockingCollection<(LogLevel Level, string Text)>? queue;
    private readonly Thread? writer;
    private readonly StreamWriter? stream;
    private readonly Lock gate = new();
    private readonly byte[] salt = RandomNumberGenerator.GetBytes(16);
    private long dropped;
    private long pending;
    private int disposed;

    private DiagnosticLog(string reason)
    {
        DisabledReason = reason;
        StartedUtc = DateTime.UtcNow;
    }

    /// <param name="startedUtc">The run's start, which names the file; the tests of rotation pass their own.</param>
    /// <param name="processId">The process in the file name; the tests pass their own so twenty five runs make twenty five files.</param>
    public DiagnosticLog(string directory, bool verbose, DateTime? startedUtc = null, int? processId = null)
    {
        StartedUtc = startedUtc ?? DateTime.UtcNow;
        Verbose = verbose;
        try
        {
            System.IO.Directory.CreateDirectory(directory);
            PreviousFilePath = LogFiles(directory).FirstOrDefault()?.FullName;
            string name = string.Create(CultureInfo.InvariantCulture, $"grouplab-{StartedUtc:yyyyMMdd-HHmmss}-{processId ?? Environment.ProcessId}.log");
            FilePath = Path.Combine(directory, name);
            stream = new StreamWriter(new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), new UTF8Encoding(false));
            Directory = directory;
            Rotate(directory, FilePath);
            queue = new BlockingCollection<(LogLevel, string)>(QueueCapacity);
            writer = new Thread(WriteQueued) { IsBackground = true, Name = "GroupLab log writer" };
            writer.Start();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            stream?.Dispose();
            stream = null;
            queue = null;
            FilePath = null;
            Directory = null;
            DisabledReason = "the log directory could not be used: " + Scrub(ex.Message);
        }
    }

    /// <summary>The log in use. It starts disabled, and <c>Program.Main</c> replaces it before the window exists.</summary>
    public static DiagnosticLog Current { get; set; } = Disabled("logging has not started");

    public static DiagnosticLog Disabled(string reason) => new(reason);

    public string? Directory { get; }

    /// <summary>The directory in symbolic terms, such as <c>%LOCALAPPDATA%\GroupLab\logs</c>, for the first log line and the settings panel.</summary>
    public string? DescribedDirectory { get; init; }

    public string? FilePath { get; }

    /// <summary>The newest log in the directory from before this run, which is often where a crash's real cause is (entry 41 section 6).</summary>
    public string? PreviousFilePath { get; }

    public string? DisabledReason { get; private set; }

    public bool IsEnabled => queue is not null && DisabledReason is null;

    /// <summary>Whether DEBUG lines are written: the <c>--verbose</c> flag or the remembered setting.</summary>
    public bool Verbose { get; set; }

    public DateTime StartedUtc { get; }

    public static void Debug(string name, params (string Key, object? Value)[] fields) => Current.Write(LogLevel.Debug, name, fields);

    public static void Info(string name, params (string Key, object? Value)[] fields) => Current.Write(LogLevel.Info, name, fields);

    public static void Warn(string name, params (string Key, object? Value)[] fields) => Current.Write(LogLevel.Warn, name, fields);

    public static void Error(string name, params (string Key, object? Value)[] fields) => Current.Write(LogLevel.Error, name, fields);

    /// <summary>A line naming an exception by type and message, with its stack and every inner exception on continuation lines.</summary>
    public static void Exception(LogLevel level, string name, Exception exception, params (string Key, object? Value)[] fields) =>
        Current.WriteException(level, name, exception, fields);

    public void WriteException(LogLevel level, string name, Exception exception, params (string Key, object? Value)[] fields)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write(level, name, [.. fields, ("ex", exception.GetType().FullName), ("message", exception.Message)], Chain(exception));
    }

    /// <summary>
    /// The name of the last INFO or WARN event, which is what the user was last doing: a short stable identifier such as <c>print.select</c>,
    /// so repeated crash reports group (entry 45 section 2). Kept even when logging is off, because a crash record is still worth one.
    /// </summary>
    public static string? LastAction { get; private set; }

    /// <summary>A file as the log records it: its name, and a salted hash of its full path, never the directory (entry 41 section 3).</summary>
    public static (string Key, object? Value)[] File(string path) => Current.FileFields(path);

    public (string Key, object? Value)[] FileFields(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            full = path;
        }

        string id = Convert.ToHexStringLower(SHA256.HashData([.. salt, .. Encoding.UTF8.GetBytes(full)]))[..8];
        return [("file", Path.GetFileName(path)), ("pathid", id)];
    }

    public void Write(LogLevel level, string name, IEnumerable<(string Key, object? Value)> fields, IEnumerable<string>? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (level is LogLevel.Info or LogLevel.Warn && !name.StartsWith("app.", StringComparison.Ordinal) && !name.StartsWith("crash.", StringComparison.Ordinal))
        {
            LastAction = name;
        }

        if (queue is null || DisabledReason is not null || (level == LogLevel.Debug && !Verbose))
        {
            return;
        }

        try
        {
            string text = Format(DateTime.UtcNow, level, name, fields);
            if (continuation is not null)
            {
                text = string.Join(Environment.NewLine, [text, .. continuation.Select(line => "    " + Clean(Scrub(line), 2000))]);
            }

            Interlocked.Increment(ref pending);
            bool added = level == LogLevel.Debug ? queue.TryAdd((level, text)) : queue.TryAdd((level, text), 50);
            if (!added)
            {
                Interlocked.Decrement(ref pending);
                Interlocked.Increment(ref dropped);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // The log is closing. A line that arrives now is lost, and the application carries on.
        }
    }

    /// <summary>Writes everything queued and flushes it to disk, waiting at most two seconds. A crash record calls this before anything else.</summary>
    public void Flush()
    {
        if (stream is null)
        {
            return;
        }

        var clock = Stopwatch.StartNew();
        while (Interlocked.Read(ref pending) > 0 && clock.Elapsed < TimeSpan.FromSeconds(2) && writer is { IsAlive: true })
        {
            Thread.Sleep(5);
        }

        try
        {
            lock (gate)
            {
                stream.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            DisabledReason ??= "the log could not be flushed: " + Scrub(ex.Message);
        }
    }

    public void Dispose()
    {
        if (queue is null || Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        try
        {
            queue.CompleteAdding();
            writer?.Join(TimeSpan.FromSeconds(3));
            lock (gate)
            {
                stream?.Dispose();
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // Closing is best effort: the application is exiting either way.
        }

        queue.Dispose();
    }

    /// <summary>One line: the UTC time with milliseconds, the level, the event name, and the fields that have a value.</summary>
    internal static string Format(DateTime utc, LogLevel level, string name, IEnumerable<(string Key, object? Value)> fields)
    {
        var line = new StringBuilder();
        line.Append(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture))
            .Append("  ")
            .Append(level.ToString().ToUpperInvariant().PadRight(5))
            .Append("  ")
            .Append(name.PadRight(14));
        foreach (var (key, value) in fields)
        {
            if (value is null)
            {
                continue;
            }

            string text = Clean(Scrub(value switch
            {
                double d => d.ToString("0.######", CultureInfo.InvariantCulture),
                float f => f.ToString("0.######", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "",
            }), 400);
            line.Append(' ').Append(key).Append('=').Append(text.Length == 0 || text.Contains(' ', StringComparison.Ordinal) || text.Contains('"', StringComparison.Ordinal) ? "\"" + text.Replace('"', '\'') + "\"" : text);
        }

        return line.ToString();
    }

    /// <summary>
    /// Replaces anything shaped like a path with <c>&lt;path&gt;</c>: a drive-letter path, a UNC path, or an absolute path under a directory
    /// that holds user files. Exception messages and stack traces carry paths, and a path begins with a person's name.
    /// </summary>
    public static string Scrub(string text) => string.IsNullOrEmpty(text) ? text ?? "" : PathLike().Replace(text, "<path>");

    private static string Clean(string text, int limit)
    {
        var cleaned = new string([.. text.Where(c => !char.IsControl(c))]);
        return cleaned.Length > limit ? cleaned[..limit] + "..." : cleaned;
    }

    /// <summary>Every exception in the chain, outermost first, as continuation lines: type and message, then its stack.</summary>
    private static IEnumerable<string> Chain(Exception exception)
    {
        int depth = 0;
        for (var current = exception; current is not null; current = current.InnerException, depth++)
        {
            if (depth > 0)
            {
                yield return $"inner {current.GetType().FullName}: {current.Message}";
            }

            foreach (string frame in (current.StackTrace ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return frame;
            }
        }
    }

    private void WriteQueued()
    {
        var sinceFlush = Stopwatch.StartNew();
        try
        {
            while (!queue!.IsCompleted)
            {
                if (queue.TryTake(out var item, 500))
                {
                    lock (gate)
                    {
                        stream!.WriteLine(item.Text);
                        if (item.Level == LogLevel.Error)
                        {
                            stream.Flush();
                            sinceFlush.Restart();
                        }
                    }

                    Interlocked.Decrement(ref pending);
                }

                long lost = Interlocked.Exchange(ref dropped, 0);
                if (lost > 0)
                {
                    lock (gate)
                    {
                        stream!.WriteLine(Format(DateTime.UtcNow, LogLevel.Warn, "log.dropped", [("count", lost), ("reason", "the queue was full")]));
                    }
                }

                if (sinceFlush.Elapsed >= FlushInterval)
                {
                    lock (gate)
                    {
                        stream!.Flush();
                    }

                    sinceFlush.Restart();
                }
            }

            lock (gate)
            {
                stream!.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            DisabledReason = "the log stopped writing: " + Scrub(ex.Message);
        }
    }

    /// <summary>The log files in a directory, newest first by name, which sorts by the UTC time in it.</summary>
    private static IEnumerable<FileInfo> LogFiles(string directory) =>
        new DirectoryInfo(directory).EnumerateFiles("grouplab-*.log").OrderByDescending(f => f.Name, StringComparer.Ordinal);

    /// <summary>Keeps the newest <see cref="KeepFiles"/> files or <see cref="KeepBytes"/> in total, whichever bites first, and never the current one.</summary>
    private static void Rotate(string directory, string current)
    {
        int kept = 0;
        long bytes = 0;
        foreach (var file in LogFiles(directory))
        {
            kept++;
            bytes += file.Length;
            if (file.FullName == current || (kept <= KeepFiles && bytes <= KeepBytes))
            {
                continue;
            }

            try
            {
                file.Delete();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Another run holds it open. It goes next time.
            }
        }
    }

    // A path runs on through a space when a separator follows it, since folders such as "Range photos" and user names such as "Jane Q" hold
    // spaces, and it stops at a colon, so a stack frame's ":line 42" survives.
    [GeneratedRegex(@"(?:[A-Za-z]:[\\/]|\\\\)(?:[^\s""'<>|:]|\s(?=[^\s""'<>|:]*[\\/]))*|/(?:home|Users|root|tmp|var|private|mnt|media|Volumes|run|opt|srv)(?:/(?:[^\s""'<>|:]|\s(?=[^\s""'<>|:]*/))*)?")]
    private static partial Regex PathLike();
}
