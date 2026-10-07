using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.Core.Updates;

namespace GroupLab.App.Diagnostics;

/// <summary>Whether error reports go by themselves, NOTES-FROM-PLANNING.md entry 194 section 2.1. Unset is asking, until the person chooses.</summary>
public enum ErrorReportChoice
{
    Unset,
    Always,
    Ask,
    Never,
}

/// <summary>
/// Error reports sent to grouplab.org, NOTES-FROM-PLANNING.md entry 194 section 2. A report is built from the crash records GroupLab
/// already writes, which carry no path, no image and nothing read out of one, and it carries only the build, the system, the error and
/// the names of the last things done: never a word anybody typed. Identical errors go as one report with a count, a report is never sent
/// twice, one that cannot go now is kept and tried again for seven days, and a day's reports are capped.
/// </summary>
internal static class ErrorReports
{
    public const string Schema = "grouplab-error-report-1";

    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    private const string SentSuffix = ".sent";

    /// <summary>What a report holds, in the words the first run screen and Settings show.</summary>
    public static IReadOnlyList<string> WhatIsSent { get; } =
    [
        "The version of GroupLab, and the system it runs on.",
        "What went wrong: the error, and where in GroupLab it happened.",
        "The names of the last few things done in GroupLab, such as setting the caliber, and nothing typed into them.",
        "Never a photograph or scan, a file name, a location, or anything you wrote.",
    ];

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 357 section 2: what a report holds where it carries the log, in the words the first run screen and
    /// Settings show while <see cref="SharingSwitches.FullLogOpen"/> is on.
    /// </summary>
    public static IReadOnlyList<string> WhatIsSentFullLog { get; } =
    [
        "The version of GroupLab, and the system it runs on.",
        "What went wrong: the error, and where in GroupLab it happened.",
        "GroupLab's log of this run and the one before, the same log Report a problem sends: what was done, in order, with the numbers you entered such as the caliber and the distance. Anything you typed in words, such as a sheet's name or a note, is replaced by how many characters it had.",
        "The record of each stage of reading a picture, where GroupLab was reading one, and under Send everything I open a report for each picture it could not read, matched to that picture by a code.",
        "Never a photograph or scan, a file name, a path, a location, or the settings file.",
    ];

    /// <summary>What a report holds, as the switch stands.</summary>
    public static IReadOnlyList<string> WhatIsSentNow => SharingSwitches.FullLogOpen ? WhatIsSentFullLog : WhatIsSent;

    /// <summary>
    /// Entry 357 section 2: the wording an automatic choice was made under. A choice made under the thinner wording sends what it promised
    /// until the person answers again under this one.
    /// </summary>
    public const int FullLogWording = 2;

    /// <summary>The kind of record a picture GroupLab could not read writes, under "Send everything I open".</summary>
    public const string ReadFailure = "read-failure";

    public static bool IsSent(string record) => File.Exists(record + SentSuffix);

    /// <summary>
    /// Entry 357 section 2: a picture GroupLab could not read, under "Send everything I open" with automatic reports that carry the log,
    /// recorded as a report of its own: the reason, the stage records, and the picture code its submission carries, so the two are matched.
    /// Returns the record's path, or null where it could not be written.
    /// </summary>
    public static string? RecordReadFailure(string? directory, string failure, JsonArray stages, string picture)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(stages);
        if (directory is null)
        {
            return null;
        }

        try
        {
            var now = DateTime.UtcNow;
            string path = Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"read-{now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.json"));
            var record = CrashReporter.Describe(new InvalidDataException(DiagnosticLog.Scrub(failure)), now, ReadFailure);
            record["exceptions"] = new JsonArray(new JsonObject { ["type"] = "GroupLab.ReadFailure", ["message"] = DiagnosticLog.Scrub(failure), ["stack"] = "" });
            record["stages"] = stages.DeepClone();
            record["picture_code"] = picture;
            File.WriteAllText(path, record.ToJsonString());
            DiagnosticLog.Info("errors.read-failure", ("stages", stages.Count));
            return path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "errors.read-failure", ex);
            return null;
        }
    }

    /// <summary>The kind a record says, a read failure where it is one and otherwise as the crash reporter reads it.</summary>
    public static string KindOf(string record)
    {
        try
        {
            if ((string?)JsonNode.Parse(File.ReadAllText(record))?["kind"] == ReadFailure)
            {
                return ReadFailure;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
        }

        return CrashReporter.KindOfRecord(record);
    }

    /// <summary>Marks a record done with, sent or let go, so it is never sent again.</summary>
    public static void MarkSent(string record, string how)
    {
        try
        {
            File.WriteAllText(record + SentSuffix, how);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "errors.mark", ex);
        }
    }

    /// <summary>A record's error and kind, which is what makes two records one error.</summary>
    public static string Key(string record)
    {
        try
        {
            var node = JsonNode.Parse(File.ReadAllText(record));
            // Entry 357: a picture that could not be read is a report of its own, matched to its picture.
            return (string?)node?["kind"] == ReadFailure
                ? ReadFailure + "|" + (string?)node?["picture_code"] + "|" + Path.GetFileName(record)
                : CrashReporter.GroupKey(node) + "|" + CrashReporter.KindOfRecord(record);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return record;
        }
    }

    /// <summary>The records not yet sent, grouped by error, oldest first. A record kept past seven days is let go, with a line in the log.</summary>
    /// <param name="withReads">Entry 357: whether read failure records are reports too, which they are only while reports carry the log.</param>
    public static IReadOnlyList<IReadOnlyList<string>> Waiting(string? directory, DateTime now, bool withReads = false)
    {
        if (directory is null || !Directory.Exists(directory))
        {
            return [];
        }

        var fresh = new List<string>();
        var records = Directory.EnumerateFiles(directory, "crash-*.json");
        if (withReads)
        {
            records = records.Concat(Directory.EnumerateFiles(directory, "read-*.json"));
        }

        foreach (string record in records.Where(r => !IsSent(r)).Order(StringComparer.Ordinal))
        {
            if (now - File.GetLastWriteTimeUtc(record) > KeptFor)
            {
                MarkSent(record, "expired");
                DiagnosticLog.Info("errors.expired", DiagnosticLog.File(record));
                continue;
            }

            fresh.Add(record);
        }

        return [.. fresh.GroupBy(Key).Select(g => (IReadOnlyList<string>)[.. g])];
    }

    /// <summary>
    /// The report for the records of one error: the first record's build, system and error, the count, and the names of the last things
    /// done. Its identifier comes from the records themselves, so the same records always make the same report and the receiver takes it once.
    /// </summary>
    /// <param name="package">Entry 357 section 2: the crash receiver's reference for the log package sent with this report, or null.</param>
    public static string Build(IReadOnlyList<string> records, IReadOnlyList<string> lastActions, string? package = null)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(lastActions);
        var first = JsonNode.Parse(File.ReadAllText(records[0]))!.AsObject();
        string id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", records.Select(Path.GetFileName)))))[..32];
        var exceptions = new JsonArray();
        foreach (var e in (first["exceptions"] as JsonArray ?? []).Take(5))
        {
            exceptions.Add(new JsonObject
            {
                ["type"] = (string?)e?["type"] ?? "",
                ["message"] = DiagnosticLog.Scrub((string?)e?["message"] ?? ""),
                ["stack"] = DiagnosticLog.Scrub((string?)e?["stack"] ?? ""),
            });
        }

        var app = first["app"];
        var environment = first["environment"];
        var report = new JsonObject
        {
            ["schema"] = Schema,
            ["report_id"] = id,
            ["kind"] = KindOf(records[0]),
            ["made"] = "automatic",
            ["count"] = records.Count,
            ["app"] = new JsonObject { ["version"] = (string?)app?["version"] ?? AppInfo.Version, ["commit"] = (string?)app?["commit"] ?? "", ["channel"] = (string?)app?["channel"] ?? "" },
            ["environment"] = new JsonObject
            {
                ["os"] = (string?)environment?["os"] ?? "",
                ["framework"] = (string?)environment?["framework"] ?? "",
                ["renderer"] = (string?)environment?["renderer"] ?? "",
                ["display_scale"] = environment?["display_scale"]?.GetValueKind() == JsonValueKind.Number ? (double?)environment["display_scale"] : null,
            },
            ["exceptions"] = exceptions,
            ["last_actions"] = new JsonArray([.. lastActions.Select(a => (JsonNode?)a)]),
        };
        if (package is not null)
        {
            report["package"] = package;
        }

        if ((string?)first["picture_code"] is { Length: 32 } picture)
        {
            report["picture_code"] = picture;
        }

        return report.ToJsonString();
    }

    /// <summary>
    /// Entry 357 section 2: the log package for a group's first record, the zip Report a problem builds with typed text replaced by its
    /// length, sent to the crash receiver. Returns its reference; null with Keep where it should be tried again with the report; null
    /// without Keep where it cannot go (over the cap, or refused for what it is), and the report goes without it.
    /// </summary>
    public static async Task<(bool Keep, string? Reference)> SendPackageAsync(IOutsideWorld outside, string address, string record, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(outside);
        var (runLog, previousLog) = ReportPackage.LogsFor(DiagnosticLog.Current, KindOf(record) == ReadFailure ? null : record);
        string zip = Path.Combine(Path.GetTempPath(), $"grouplab-report-{Guid.NewGuid():N}.zip");
        try
        {
            var made = ReportPackage.Build(zip, record, runLog, previousLog, ReportPackage.EnvironmentText(null), null, null, withoutTypedText: true);
            if (made.TooLargeToSend)
            {
                DiagnosticLog.Info("errors.package", ("sent", false), ("reason", "over the cap"));
                return (false, null);
            }

            var answer = await outside.PostReportPackageAsync(address, File.ReadAllBytes(zip), AppInfo.Version, token).ConfigureAwait(true);
            if (answer is null)
            {
                return (true, null);
            }

            JsonNode? reply = null;
            try
            {
                reply = JsonNode.Parse(answer.Body);
            }
            catch (JsonException)
            {
            }

            if (reply?["ok"]?.GetValueKind() == JsonValueKind.True && (string?)reply["reference"] is { Length: > 0 } reference)
            {
                DiagnosticLog.Info("errors.package", ("sent", true), ("reference", reference));
                return (false, reference);
            }

            bool again = reply?["retry"]?.GetValueKind() == JsonValueKind.True || answer.Status is 429 or >= 500;
            DiagnosticLog.Info("errors.package", ("sent", false), ("status", answer.Status), ("retry", again));
            return (again, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "errors.package", ex);
            return (false, null);
        }
        finally
        {
            try
            {
                File.Delete(zip);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>Entry 386 section 4: the kind of report Send to GroupLab makes, the person's diagnostics with an optional note.</summary>
    public const string Diagnostics = "diagnostics";

    /// <summary>The longest note Send to GroupLab takes, in characters, under the receiver's own limit for a description.</summary>
    public const int MostNote = 2000;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 376 B7, built in entry 386 section 4: the person's diagnostics sent straight to GroupLab, through the same
    /// two receivers as an error report, with the same rules. The newest two logs (paths already taken out as they were written, and every
    /// typed name or note replaced by its length) and the newest crash record go to the crash receiver as a package; then a report of the
    /// kind <see cref="Diagnostics"/>, made by hand, carrying the package's reference and the note, goes to the error receiver, which opens
    /// an issue of its own for it. Nothing about where a picture was taken, no picture and no path is in either. Returns the sentence for the
    /// screen.
    /// </summary>
    public static async Task<string> SendDiagnosticsAsync(IOutsideWorld outside, string packageAddress, string reportAddress, string? note, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(outside);
        var (runLog, previousLog) = ReportPackage.LogsFor(DiagnosticLog.Current, null);
        string? crash = CrashReporter.PendingCrashes(DiagnosticLog.Current.Directory).LastOrDefault();
        string? typed = string.IsNullOrWhiteSpace(note) ? null : DiagnosticLog.Scrub(note.Trim());
        if (typed is { Length: > MostNote })
        {
            typed = typed[..MostNote];
        }

        string zip = Path.Combine(Path.GetTempPath(), $"grouplab-diagnostics-{Guid.NewGuid():N}.zip");
        try
        {
            var made = ReportPackage.Build(zip, crash, runLog, previousLog, ReportPackage.EnvironmentText(null), typed, null, withoutTypedText: true);
            if (made.TooLargeToSend)
            {
                DiagnosticLog.Info("diagnostics.send", ("sent", false), ("reason", "over the cap"));
                return "The diagnostics are too large to send this way. Use Share diagnostics instead.";
            }

            var answer = await outside.PostReportPackageAsync(packageAddress, File.ReadAllBytes(zip), AppInfo.Version, token).ConfigureAwait(true);
            string? reference = null;
            try
            {
                reference = answer is null ? null : (string?)JsonNode.Parse(answer.Body)?["reference"];
            }
            catch (JsonException)
            {
            }

            if (reference is not { Length: > 0 })
            {
                DiagnosticLog.Info("diagnostics.send", ("sent", false), ("status", answer?.Status ?? 0));
                return "The diagnostics could not be sent just now. Try again later, or use Share diagnostics.";
            }

            var report = new JsonObject
            {
                ["schema"] = Schema,
                ["report_id"] = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16)),
                ["kind"] = Diagnostics,
                ["made"] = "by hand",
                ["count"] = 1,
                ["app"] = new JsonObject { ["version"] = AppInfo.Version, ["commit"] = AppInfo.Commit, ["channel"] = AppInfo.Channel },
                ["environment"] = new JsonObject { ["os"] = AppInfo.OperatingSystemName, ["framework"] = AppInfo.Framework, ["renderer"] = AppInfo.Renderer },
                ["exceptions"] = new JsonArray(),
                ["last_actions"] = new JsonArray([.. LastActions(runLog).Select(a => (JsonNode?)a)]),
                ["package"] = reference,
            };
            if (typed is not null)
            {
                report["description"] = typed;
            }

            var sent = await outside.PostErrorReportAsync(reportAddress, report.ToJsonString(), token).ConfigureAwait(true);
            bool ok = false;
            try
            {
                ok = sent is not null && JsonNode.Parse(sent.Body)?["ok"]?.GetValueKind() == JsonValueKind.True;
            }
            catch (JsonException)
            {
            }

            DiagnosticLog.Info("diagnostics.send", ("sent", ok), ("reference", reference), ("note", typed is not null));
            return ok
                ? $"Sent to GroupLab, reference {reference}. Thank you; quote the reference if you write about it."
                : $"The logs went (reference {reference}), but the note did not. Try again later, or use Share diagnostics.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "diagnostics.send", ex);
            return "The diagnostics could not be put together on this device; the log says why.";
        }
        finally
        {
            try
            {
                File.Delete(zip);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>The names of the last events in a log, what the person did last, and none of the values that follow each name.</summary>
    public static IReadOnlyList<string> LastActions(string? log, int most = 20)
    {
        if (log is null || !File.Exists(log))
        {
            return [];
        }

        try
        {
            using var stream = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var names = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0].Length >= 20 && char.IsDigit(parts[0][0]) && parts[2] is not "app.crash" && parts[2].All(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_'))
                {
                    names.Add(parts[2]);
                }
            }

            return [.. names.TakeLast(most)];
        }
        catch (IOException)
        {
            return [];
        }
    }

    /// <summary>
    /// Sends each group as one report until the day's budget is spent. A report the receiver took is marked sent; one that met no answer,
    /// a limit or a closed receiver is kept for the next try; one refused for what it is, which trying again cannot mend, is let go. Never a
    /// dialog: the person is not interrupted by a report failing. Returns how many went.
    /// </summary>
    /// <param name="packageAddress">Entry 357 section 2: where each report's log package goes first, or null for a report without one.</param>
    public static async Task<int> SendAsync(IOutsideWorld outside, string address, IReadOnlyList<IReadOnlyList<string>> groups, int budget,
        Func<string, IReadOnlyList<string>> actionsFor, CancellationToken token, string? packageAddress = null)
    {
        ArgumentNullException.ThrowIfNull(outside);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(actionsFor);
        int sent = 0;
        foreach (var group in groups)
        {
            if (sent >= budget)
            {
                DiagnosticLog.Info("errors.budget", ("left", groups.Count - sent));
                break;
            }

            string? package = null;
            if (packageAddress is not null)
            {
                var (keep, reference) = await SendPackageAsync(outside, packageAddress, group[0], token).ConfigureAwait(true);
                if (keep)
                {
                    DiagnosticLog.Info("errors.kept", ("reason", "the log package did not go"));
                    continue;
                }

                package = reference;
            }

            string report = Build(group, actionsFor(group[0]), package);
            var answer = await outside.PostErrorReportAsync(address, report, token).ConfigureAwait(true);
            if (answer is null)
            {
                DiagnosticLog.Info("errors.kept", ("reason", "no answer"));
                continue;
            }

            JsonNode? reply = null;
            try
            {
                reply = JsonNode.Parse(answer.Body);
            }
            catch (JsonException)
            {
            }

            if (reply?["ok"]?.GetValueKind() == JsonValueKind.True)
            {
                foreach (string record in group)
                {
                    MarkSent(record, "sent");
                }

                sent++;
                DiagnosticLog.Info("errors.sent", ("count", group.Count));
            }
            else if (reply?["retry"]?.GetValueKind() == JsonValueKind.True || answer.Status is 429 or >= 500)
            {
                DiagnosticLog.Info("errors.kept", ("status", answer.Status));
            }
            else
            {
                foreach (string record in group)
                {
                    MarkSent(record, "refused");
                }

                DiagnosticLog.Warn("errors.refused", ("status", answer.Status));
            }
        }

        return sent;
    }

    internal static string Today(DateTime now) => now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
