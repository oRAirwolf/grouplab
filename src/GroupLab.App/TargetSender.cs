using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Publication;
using GroupLab.Core.Updates;

namespace GroupLab.App;

// NOTES-FROM-PLANNING.md entry 363 section 3.5: the queue that sends targets and keeps what could not go, on its own so the phone compiles
// it as it is (question 81 A, the desktop's package and queue).

/// <summary>What sending one package came to.</summary>
internal enum SendResult
{
    Sent,
    Kept,
    Refused,
}

/// <summary>The outcome and the one line a person is told.</summary>
internal sealed record SendOutcome(SendResult Result, string? Reference, string Message);

/// <summary>
/// Sends packages and keeps those that could not go, NOTES-FROM-PLANNING.md entry 165 section 6. A package the receiver refused for a
/// reason that trying again cannot fix is let go at once and the reason shown; one that met no answer, a limit or a closed receiver is kept
/// in its own folder and tried again at the next start, for seven days, then let go with a line in the log.
/// </summary>
internal sealed class TargetSender(IOutsideWorld outside, string folder)
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    /// <summary>Told the package and the reference of each that the receiver took, entry 357's link between versions of one picture.</summary>
    public Action<string, string>? Sent { get; init; }

    public async Task<SendOutcome> SendAsync(TargetPackage package, string address, CancellationToken token)
    {
        var outcome = await Post(package, address, token).ConfigureAwait(true);
        if (outcome.Result == SendResult.Kept)
        {
            Keep(package, DateTime.UtcNow);
        }

        return outcome;
    }

    private async Task<SendOutcome> Post(TargetPackage package, string address, CancellationToken token)
    {
        var answer = await outside.PostTargetAsync(address, package.Json, package.Image, package.ImageName, token).ConfigureAwait(true);
        if (answer is null)
        {
            DiagnosticLog.Warn("send.unanswered");
            return new SendOutcome(SendResult.Kept, null, "The target could not be sent just now; it is kept and will be tried again when GroupLab next starts.");
        }

        JsonNode? reply;
        try
        {
            reply = JsonNode.Parse(answer.Body);
        }
        catch (JsonException)
        {
            reply = null;
        }

        if (reply?["ok"]?.GetValueKind() == JsonValueKind.True && (string?)reply["id"] is { Length: > 0 } id)
        {
            DiagnosticLog.Info("send.sent", ("reference", id));
            Sent?.Invoke(package.Json, id);
            return new SendOutcome(SendResult.Sent, id, $"Sent to the project. Its reference is {id}.");
        }

        bool retry = reply?["retry"]?.GetValueKind() == JsonValueKind.True || answer.Status is 429 or >= 500 || reply is null;
        string why = (string?)reply?["error"] ?? string.Create(CultureInfo.InvariantCulture, $"the receiver answered {answer.Status}");
        DiagnosticLog.Warn("send.refused", ("status", answer.Status), ("retry", retry));
        return retry
            ? new SendOutcome(SendResult.Kept, null, "The target could not be sent just now (" + why.TrimEnd('.') + "); it is kept and will be tried again when GroupLab next starts.")
            : new SendOutcome(SendResult.Refused, null, "The target was not sent: " + why);
    }

    /// <summary>Keeps a package in its own folder, to be sent at the next try.</summary>
    public void Keep(TargetPackage package, DateTime now)
    {
        string into = Path.Combine(folder, now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(into);
        File.WriteAllBytes(Path.Combine(into, "image"), package.Image);
        File.WriteAllText(Path.Combine(into, "name.txt"), package.ImageName);
        File.WriteAllText(Path.Combine(into, "package.json"), package.Json);
    }

    /// <summary>The folders of the packages waiting, oldest first.</summary>
    public IReadOnlyList<string> Pending() => Directory.Exists(folder) ? [.. Directory.EnumerateDirectories(folder).Order(StringComparer.Ordinal)] : [];

    public void Discard(string pending) => Directory.Delete(pending, recursive: true);

    /// <summary>Tries every waiting package again, lets go of any seven days old, and returns what came of each tried.</summary>
    public async Task<IReadOnlyList<SendOutcome>> RetryAsync(string address, DateTime now, CancellationToken token)
    {
        var outcomes = new List<SendOutcome>();
        foreach (string pending in Pending())
        {
            string stamp = Path.GetFileName(pending).Split('-')[0];
            if (DateTime.TryParseExact(stamp, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var kept)
                && now - kept > KeptFor)
            {
                DiagnosticLog.Info("send.expired", ("days", (int)(now - kept).TotalDays));
                Discard(pending);
                continue;
            }

            try
            {
                var package = new TargetPackage(File.ReadAllBytes(Path.Combine(pending, "image")), File.ReadAllText(Path.Combine(pending, "name.txt")), File.ReadAllText(Path.Combine(pending, "package.json")));
                var outcome = await Post(package, address, token).ConfigureAwait(true);
                if (outcome.Result != SendResult.Kept)
                {
                    Discard(pending);
                }

                outcomes.Add(outcome);
            }
            catch (IOException ex)
            {
                DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "send.retry", ex);
            }
        }

        return outcomes;
    }
}
