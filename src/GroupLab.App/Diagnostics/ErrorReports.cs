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

    public static bool IsSent(string record) => File.Exists(record + SentSuffix);

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
            return CrashReporter.GroupKey(JsonNode.Parse(File.ReadAllText(record))) + "|" + CrashReporter.KindOfRecord(record);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return record;
        }
    }

    /// <summary>The records not yet sent, grouped by error, oldest first. A record kept past seven days is let go, with a line in the log.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> Waiting(string? directory, DateTime now)
    {
        if (directory is null || !Directory.Exists(directory))
        {
            return [];
        }

        var fresh = new List<string>();
        foreach (string record in Directory.EnumerateFiles(directory, "crash-*.json").Where(r => !IsSent(r)).Order(StringComparer.Ordinal))
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
    public static string Build(IReadOnlyList<string> records, IReadOnlyList<string> lastActions)
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
        return new JsonObject
        {
            ["schema"] = Schema,
            ["report_id"] = id,
            ["kind"] = CrashReporter.KindOfRecord(records[0]),
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
        }.ToJsonString();
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
    public static async Task<int> SendAsync(IOutsideWorld outside, string address, IReadOnlyList<IReadOnlyList<string>> groups, int budget,
        Func<string, IReadOnlyList<string>> actionsFor, CancellationToken token)
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

            string report = Build(group, actionsFor(group[0]));
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
