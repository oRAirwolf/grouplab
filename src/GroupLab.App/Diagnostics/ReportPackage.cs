using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace GroupLab.App.Diagnostics;

/// <summary>A built report package: where it is, what is in it, how large it is, and whether it can be sent.</summary>
/// <param name="DroppedPreviousLog">True when the previous run's log was left out to bring the package under the client's cap.</param>
/// <param name="TooLargeToSend">True when even without the previous run's log the package is over the cap: it can be saved, not sent.</param>
public sealed record PackageResult(string Path, IReadOnlyList<string> Entries, long Bytes, bool DroppedPreviousLog, bool TooLargeToSend);

/// <summary>
/// The report package, NOTES-FROM-PLANNING.md entry 41 section 6 with entry 45 section 1's checked contract: one flat zip holding
/// <list type="bullet">
/// <item>the crash record, when there was a crash;</item>
/// <item>the log of the run it concerns, and the log of the run before, which is often where the real cause is;</item>
/// <item><c>environment.txt</c>, the expanded environment block;</item>
/// <item><c>description.txt</c> and <c>contact.txt</c>, only when the user typed something.</item>
/// </list>
/// That list is exhaustive. <see cref="PermittedEntryPatterns"/> is the receiver's whitelist, anchored and case sensitive, and no entry is added
/// that does not match it, so a photograph, an EXIF block, a marking, a settings file or a path cannot get in. The builder takes no
/// arbitrary file, and adding a file to the package means changing this list and the receiver's at the same time: that friction is the
/// point. The client's cap is 2 MB: over it the previous run's log is dropped first, and a package still over it is saved but not sent.
/// </summary>
public static partial class ReportPackage
{
    public const long ClientCapBytes = 2L * 1024 * 1024;

    /// <summary>The receiver's <c>ALLOWED_ENTRIES</c>, entry 45 section 1, as its patterns are written there.</summary>
    public static IReadOnlyList<string> PermittedEntryPatterns { get; } =
    [
        @"^crash-\d{8}-\d{6}-\d+\.json$",
        @"^grouplab-\d{8}-\d{6}-\d+\.log$",
        @"^environment\.txt$",
        @"^description\.txt$",
        @"^contact\.txt$",
    ];

    /// <summary>
    /// Whether a name may be an entry: flat, with no slash, backslash or <c>..</c>, and matching one of the patterns. It uses ASCII digits, as
    /// the receiver's PCRE <c>\d</c> does.
    /// </summary>
    public static bool IsPermitted(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.Length is > 0 and <= 120 && name.IndexOfAny(['/', '\\', '\0']) < 0 && !name.Contains("..", StringComparison.Ordinal)
            && PermittedEntryPatterns.Any(p => Regex.IsMatch(name, p, RegexOptions.ECMAScript, TimeSpan.FromSeconds(1)));
    }

    /// <summary>Writes the package, dropping the previous run's log if the package is otherwise over the cap.</summary>
    /// <param name="crashRecord">The crash record's path, or null for a report without a crash.</param>
    /// <param name="runLog">The log of the run the report concerns: the crashed run, or this one.</param>
    /// <param name="previousLog">The log of the run before, or null.</param>
    public static PackageResult Build(string zipPath, string? crashRecord, string? runLog, string? previousLog, string environment, string? description, string? contact, long capBytes = ClientCapBytes)
    {
        ArgumentNullException.ThrowIfNull(zipPath);
        ArgumentNullException.ThrowIfNull(environment);
        var entries = Write(zipPath, crashRecord, runLog, previousLog, environment, description, contact);
        long bytes = new FileInfo(zipPath).Length;
        bool dropped = false;
        if (bytes > capBytes && previousLog is not null)
        {
            entries = Write(zipPath, crashRecord, runLog, null, environment, description, contact);
            bytes = new FileInfo(zipPath).Length;
            dropped = true;
        }

        DiagnosticLog.Info("report.package", ("entries", entries.Count), ("bytes", bytes), ("dropped_previous_log", dropped), ("too_large_to_send", bytes > capBytes));
        return new PackageResult(zipPath, entries, bytes, dropped, bytes > capBytes);
    }

    /// <summary>The expanded environment block for <c>environment.txt</c>: the <c>app.start</c> fields, the .NET runtime, the processor and the display scale.</summary>
    public static string EnvironmentText(double? displayScale)
    {
        var text = new StringBuilder();
        foreach (var (key, value) in AppInfo.EnvironmentFields())
        {
            text.Append(key).Append(": ").AppendLine(value?.ToString());
        }

        text.Append("runtime: ").AppendLine(Environment.Version.ToString());
        text.Append("process architecture: ").AppendLine(RuntimeInformation.ProcessArchitecture.ToString());
        text.Append("os architecture: ").AppendLine(RuntimeInformation.OSArchitecture.ToString());
        text.Append("display scale: ").AppendLine(displayScale?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "not known");
        return text.ToString();
    }

    private static List<string> Write(string zipPath, string? crashRecord, string? runLog, string? previousLog, string environment, string? description, string? contact)
    {
        var names = new List<string>();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(zipPath))!);
        File.Delete(zipPath);
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        void AddFile(string? path)
        {
            if (path is null || !File.Exists(path))
            {
                return;
            }

            string name = Path.GetFileName(path);
            Require(name);
            var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var target = entry.Open();
            source.CopyTo(target);
            names.Add(name);
        }

        void AddText(string name, string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            Require(name);
            using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
            writer.Write(content);
            names.Add(name);
        }

        AddFile(crashRecord);
        AddFile(runLog);
        if (previousLog is not null && previousLog != runLog)
        {
            AddFile(previousLog);
        }

        AddText("environment.txt", environment);
        AddText("description.txt", description);
        AddText("contact.txt", contact);
        return names;
    }

    /// <summary>
    /// The logs a report carries, entry 41 section 6. For a crash: the log of the run that crashed, the newest log with the crash record's
    /// process id from no later than the crash, and the log before it. For a report without a crash: this run's log and the one before.
    /// </summary>
    public static (string? RunLog, string? PreviousLog) LogsFor(DiagnosticLog current, string? crash)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (crash is null)
        {
            return (current.FilePath, current.PreviousFilePath);
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(crash))!;
        var logs = Directory.Exists(directory)
            ? Directory.GetFiles(directory, "grouplab-*.log").Select(Path.GetFileName).OfType<string>().Where(n => LogName().IsMatch(n)).Order(StringComparer.Ordinal).ToList()
            : [];
        var crashName = CrashName().Match(Path.GetFileName(crash));
        if (!crashName.Success)
        {
            return (null, logs.LastOrDefault() is { } newest ? Path.Combine(directory, newest) : null);
        }

        string when = crashName.Groups["date"].Value + "-" + crashName.Groups["time"].Value, pid = crashName.Groups["pid"].Value;
        string? run = logs.LastOrDefault(n => LogName().Match(n) is var m && m.Groups["pid"].Value == pid
            && string.CompareOrdinal(m.Groups["date"].Value + "-" + m.Groups["time"].Value, when) <= 0);
        string bound = run ?? $"grouplab-{when}";
        string? previous = logs.LastOrDefault(n => string.CompareOrdinal(n, bound) < 0);
        return (run is null ? null : Path.Combine(directory, run), previous is null ? null : Path.Combine(directory, previous));
    }

    [GeneratedRegex(@"^crash-(?<date>[0-9]{8})-(?<time>[0-9]{6})-(?<pid>[0-9]+)\.json$")]
    private static partial Regex CrashName();

    [GeneratedRegex(@"^grouplab-(?<date>[0-9]{8})-(?<time>[0-9]{6})-(?<pid>[0-9]+)\.log$")]
    private static partial Regex LogName();

    private static void Require(string name)
    {
        if (!IsPermitted(name))
        {
            throw new InvalidOperationException($"{name} is not on the report package's list of permitted entries, and nothing else may be packaged (NOTES-FROM-PLANNING.md entry 45 section 1).");
        }
    }
}
