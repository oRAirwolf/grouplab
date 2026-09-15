using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GroupLab.App.Diagnostics;

/// <summary>What sending a report came to: the server's reference to quote, or the reason it was not accepted, shown as the server wrote it.</summary>
public sealed record UploadResult(bool Sent, string? Reference, string Message);

/// <summary>
/// The client half of crash report upload, NOTES-FROM-PLANNING.md entry 41 section 7 and entry 45 sections 3 and 4, written in
/// <c>docs/CRASH-REPORTING.md</c>.
/// <list type="bullet">
/// <item><b>The request:</b> one HTTPS <c>POST</c> of <c>multipart/form-data</c> to the configured address, the zip in the file field
/// <c>report</c>, and the application version in the text field <c>version</c>. No headers are required, and there is no authentication,
/// because any secret in an open source client is public.</item>
/// <item><b>One attempt, a short timeout, no retry.</b> A crash reporter that retries in the background eventually sends something the
/// user has forgotten about. Every failure keeps the zip where it was saved.</item>
/// <item><b>The reply</b> is JSON with <c>ok</c>. On success its <c>reference</c> is shown so the user can quote it; on failure its
/// <c>error</c> is shown verbatim, because the server writes it for a person whose application has just crashed.</item>
/// </list>
/// </summary>
public static class ReportUploader
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static async Task<UploadResult> SendAsync(string url, string zipPath, string version, HttpMessageHandler? handler = null, CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(zipPath);
        string kept = $" The report is kept at {zipPath}.";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || address.Scheme != Uri.UriSchemeHttps)
        {
            DiagnosticLog.Warn("report.send", ("reason", "the address is not https"));
            return new UploadResult(false, null, "The crash report address is not an https address, so nothing was sent." + kept);
        }

        try
        {
            using var client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            client.Timeout = Timeout;
            using var form = new MultipartFormDataContent();
            await using var zip = File.OpenRead(zipPath);
            var file = new StreamContent(zip);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            form.Add(file, "report", Path.GetFileName(zipPath));
            form.Add(new StringContent(AppInfo.Version), "version");
            DiagnosticLog.Info("report.send", ("bytes", zip.Length));
            using var response = await client.PostAsync(address, form, cancellation).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false);
            var reply = Parse(body);
            if (reply?["ok"]?.GetValueKind() == JsonValueKind.True && (string?)reply["reference"] is { Length: > 0 } reference)
            {
                DiagnosticLog.Info("report.sent", ("status", (int)response.StatusCode), ("reference", reference));
                return new UploadResult(true, reference, $"Sent. The report's reference is {reference}.");
            }

            DiagnosticLog.Warn("report.refused", ("status", (int)response.StatusCode));
            return (string?)reply?["error"] is { Length: > 0 } error
                ? new UploadResult(false, null, error)
                : new UploadResult(false, null, string.Create(System.Globalization.CultureInfo.InvariantCulture, $"The server's reply could not be read (HTTP {(int)response.StatusCode}), so the report may not have arrived.") + kept);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "report.send", ex);
            string why = ex is TaskCanceledException ? "The server did not answer in time." : "The report could not be sent: " + ex.Message;
            return new UploadResult(false, null, why + kept);
        }
    }

    private static JsonNode? Parse(string body)
    {
        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
