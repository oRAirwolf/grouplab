using System.Diagnostics;

namespace GroupLab.Core.Updates;

/// <summary>
/// Every way GroupLab reaches out of its own process, behind one interface, NOTES-FROM-PLANNING.md entry 122, widened by entry 123 section
/// 2.5 to cover fetching and starting the installer.
/// <para>
/// <b>Why this exists.</b> The settings page's link to the repository opened the real default browser, and entry 117's control walk clicks
/// every control it finds, so a test run opened tabs on Alan's machine. That is the same fault as entry 114's print to the OneNote driver:
/// a test reaching out of the process and into the person's own applications. One way out, and the tests replace it.
/// </para>
/// </summary>
/// <summary>
/// What the clipboard holds that could be an image, NOTES-FROM-PLANNING.md entry 137 section 2.
/// </summary>
/// <param name="Files">Paths of image files copied in a file manager, in the order the clipboard gives them. Empty where there are none.</param>
/// <param name="Bytes">Image data with no file behind it, such as a screenshot or an image copied from a browser. Null where there is none.</param>
/// <param name="Extension">The extension the data should be written with, including its dot, such as ".png". Null with no data.</param>
/// <summary>What a receiver answered to a POST: its HTTP status and its body.</summary>
public sealed record PostAnswer(int Status, string Body);

public sealed record ClipboardContents(IReadOnlyList<string> Files, byte[]? Bytes, string? Extension)
{
    /// <summary>Nothing on the clipboard that could be an image.</summary>
    public static ClipboardContents Nothing { get; } = new([], null, null);

    /// <summary>Whether there is anything here to open at all.</summary>
    public bool Empty => Files.Count == 0 && (Bytes is null || Bytes.Length == 0);
}

public interface IOutsideWorld
{
    /// <summary>Opens a web address in whatever the person uses for one.</summary>
    void OpenAddress(string address);

    /// <summary>Opens a file in whatever the person uses for its kind: a PDF in their reader, an image in their viewer.</summary>
    void OpenFile(string path);

    /// <summary>Opens a folder in the file manager.</summary>
    void OpenFolder(string path);

    /// <summary>Reads a small public file, which is how an update check asks a train what its newest build is. Null where it cannot be read.</summary>
    Task<string?> GetTextAsync(string address, CancellationToken token);

    /// <summary>
    /// Sends a target to the project, NOTES-FROM-PLANNING.md entry 165: one multipart POST of the package's JSON in the field
    /// <c>package</c> and the image in the file field <c>image</c>. The answer is the status and the body as the receiver wrote them, or
    /// null where nothing answered at all.
    /// </summary>
    Task<PostAnswer?> PostTargetAsync(string address, string package, byte[] image, string imageName, CancellationToken token);

    /// <summary>
    /// Sends an error report, NOTES-FROM-PLANNING.md entry 194: its JSON in the form field <c>report</c>, to grouplab.org. Null where
    /// nothing answered.
    /// </summary>
    Task<PostAnswer?> PostErrorReportAsync(string address, string report, CancellationToken token);

    /// <summary>
    /// Sends a hardware survey report, NOTES-FROM-PLANNING.md entries 207 and 208: its JSON in the form field <c>report</c>, to
    /// grouplab.org. Null where nothing answered.
    /// </summary>
    Task<PostAnswer?> PostSurveyAsync(string address, string report, CancellationToken token);

    /// <summary>
    /// Downloads a file, reporting the share done as it goes. It returns the bytes written, so a caller can tell a short download from a
    /// whole one, and throws nothing on a refusal: it returns null.
    /// </summary>
    Task<long?> DownloadAsync(string address, string into, IProgress<double>? progress, CancellationToken token);

    /// <summary>
    /// Starts an installer with its own arguments and does not wait. It is separate from <see cref="OpenFile"/> because an installer is
    /// never opened with the shell's idea of what to do with a file: it is run, with the switches that keep it silent.
    /// </summary>
    void StartInstaller(string path, string arguments);

    /// <summary>
    /// Reads the clipboard, NOTES-FROM-PLANNING.md entry 137 section 2. It is here for the same reason the browser is: a test that read the
    /// real clipboard would take whatever happened to be on the machine at that moment, and a test that wrote one would take something away
    /// from the person running it.
    /// <para>
    /// It is only ever called from an explicit Paste. Nothing in GroupLab reads the clipboard on its own.
    /// </para>
    /// </summary>
    Task<ClipboardContents> ReadClipboardAsync(CancellationToken token);
}

/// <summary>The real one, used by the running application and by nothing else.</summary>
public sealed class TheOutsideWorld : IOutsideWorld
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    /// <summary>What the application uses unless something has replaced it. A test replaces it; nothing else may.</summary>
    public static IOutsideWorld Current { get; set; } = new TheOutsideWorld();

    /// <summary>
    /// What an update check says it is. Entry 119 section 3.4: a plain GET with a User-Agent naming GroupLab and its version, and nothing
    /// else about the person or their machine.
    /// </summary>
    public static string UserAgent { get; set; } = "GroupLab";

    public void OpenAddress(string address) => Start(address);

    public void OpenFile(string path) => Start(path);

    public void OpenFolder(string path) => Start(path);

    public async Task<string?> GetTextAsync(string address, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, address);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using var response = await Http.SendAsync(request, token).ConfigureAwait(false);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(token).ConfigureAwait(false) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            return null;
        }
    }

    public async Task<PostAnswer?> PostTargetAsync(string address, string package, byte[] image, string imageName, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, address);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(package, System.Text.Encoding.UTF8), "package");
            var file = new ByteArrayContent(image);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "image", imageName);
            request.Content = form;
            using var response = await Http.SendAsync(request, token).ConfigureAwait(false);
            return new PostAnswer((int)response.StatusCode, await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            return null;
        }
    }

    public Task<PostAnswer?> PostSurveyAsync(string address, string report, CancellationToken token) => PostErrorReportAsync(address, report, token);

    public async Task<PostAnswer?> PostErrorReportAsync(string address, string report, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, address);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("report", report)]);
            using var response = await Http.SendAsync(request, token).ConfigureAwait(false);
            return new PostAnswer((int)response.StatusCode, await response.Content.ReadAsStringAsync(token).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            return null;
        }
    }

    public async Task<long?> DownloadAsync(string address, string into, IProgress<double>? progress, CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, address);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            long? total = response.Content.Headers.ContentLength;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(into))!);

            // A part file, renamed only when the whole thing has arrived, so a dropped connection never leaves something that looks finished.
            string part = into + ".part";
            long written = 0;
            await using (var source = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false))
            await using (var file = File.Create(part))
            {
                byte[] buffer = new byte[128 * 1024];
                int read;
                while ((read = await source.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
                    written += read;
                    if (total is > 0)
                    {
                        progress?.Report(Math.Clamp(written / (double)total.Value, 0, 1));
                    }
                }
            }

            File.Move(part, into, overwrite: true);
            progress?.Report(1);
            return written;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or InvalidOperationException or UriFormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// How the clipboard is actually read. The application installs this at startup, because a clipboard belongs to a window and this
    /// assembly has none. With nothing installed there is nothing on the clipboard, which is what a command line run should see.
    /// </summary>
    public static Func<CancellationToken, Task<ClipboardContents>>? ReadsTheClipboard { get; set; }

    public async Task<ClipboardContents> ReadClipboardAsync(CancellationToken token) =>
        ReadsTheClipboard is { } read ? await read(token).ConfigureAwait(false) : ClipboardContents.Nothing;

    public void StartInstaller(string path, string arguments)
    {
        using var started = Process.Start(new ProcessStartInfo(path, arguments) { UseShellExecute = false });
    }

    private static void Start(string what)
    {
        using var started = Process.Start(new ProcessStartInfo(what) { UseShellExecute = true });
    }
}

/// <summary>
/// What the tests and the benchmark use instead: it writes down what would have happened and does nothing. Entry 122 section 3: the control
/// walk clicks these buttons against this, so they are measured rather than excluded by name.
/// <para>
/// A test can also hand it answers, so an update can be driven end to end without a network: <see cref="Text"/> for what a check reads and
/// <see cref="Download"/> for what a download writes.
/// </para>
/// </summary>
public sealed class RecordedOutsideWorld : IOutsideWorld
{
    private readonly List<(string What, string Target)> _asked = [];

    /// <summary>Everything that was asked for, in order.</summary>
    public IReadOnlyList<(string What, string Target)> Asked => _asked;

    /// <summary>What a check reads, by address. Anything not here reads as unreachable.</summary>
    public Dictionary<string, string> Text { get; } = new(StringComparer.Ordinal);

    /// <summary>What a download writes, by address: the bytes, or null for a refusal.</summary>
    public Dictionary<string, byte[]?> Download { get; } = new(StringComparer.Ordinal);

    /// <summary>Stop a download part way, to test cancelling and a connection that drops.</summary>
    public Func<double, CancellationToken, Task>? WhileDownloading { get; set; }

    /// <summary>What a POST is answered with, from what was sent. Nothing set answers nothing, which is an unreachable receiver.</summary>
    public Func<string, string, byte[], PostAnswer?>? Answer { get; set; }

    /// <summary>What an error report is answered with. Nothing set answers nothing, which is an unreachable receiver.</summary>
    public Func<string, PostAnswer?>? ReportAnswer { get; set; }

    /// <summary>Every error report posted: the address and its JSON, in order.</summary>
    public List<(string Address, string Report)> Reports { get; } = [];

    /// <summary>What a survey report is answered with. Nothing set answers nothing, which is an unreachable receiver.</summary>
    public Func<string, PostAnswer?>? SurveyAnswer { get; set; }

    /// <summary>Every survey report posted: the address and its JSON, in order.</summary>
    public List<(string Address, string Report)> Surveys { get; } = [];

    /// <summary>Every target posted: the address, the package's JSON and the image, in order.</summary>
    public List<(string Address, string Package, byte[] Image)> Posted { get; } = [];

    /// <summary>Written down when an installer is started: the file and its switches.</summary>
    public (string Path, string Arguments)? Installer { get; private set; }

    /// <summary>What the clipboard holds, as a test has set it. Nothing by default, which is what an empty clipboard is.</summary>
    public ClipboardContents Clipboard { get; set; } = ClipboardContents.Nothing;

    /// <summary>
    /// Forgets everything: what was asked for, the answers it was given, and the installer it was told to start. The recorder is in place
    /// for the whole test run, so a test that cares what was asked for during it clears this first rather than counting from an offset.
    /// </summary>
    public void Forget()
    {
        _asked.Clear();
        Text.Clear();
        Download.Clear();
        WhileDownloading = null;
        Installer = null;
        Answer = null;
        Posted.Clear();
        ReportAnswer = null;
        Reports.Clear();
        SurveyAnswer = null;
        Surveys.Clear();
        Clipboard = ClipboardContents.Nothing;
    }

    public void OpenAddress(string address) => _asked.Add(("address", address));

    public void OpenFile(string path) => _asked.Add(("file", path));

    public void OpenFolder(string path) => _asked.Add(("folder", path));

    public Task<ClipboardContents> ReadClipboardAsync(CancellationToken token)
    {
        _asked.Add(("clipboard", Clipboard.Empty ? "empty" : Clipboard.Files.Count > 0 ? "files" : "data"));
        return Task.FromResult(Clipboard);
    }

    public Task<string?> GetTextAsync(string address, CancellationToken token)
    {
        _asked.Add(("get", address));
        return Task.FromResult(Text.TryGetValue(address, out string? text) ? text : null);
    }

    public Task<PostAnswer?> PostSurveyAsync(string address, string report, CancellationToken token)
    {
        _asked.Add(("survey", address));
        Surveys.Add((address, report));
        return Task.FromResult(SurveyAnswer?.Invoke(report));
    }

    public Task<PostAnswer?> PostErrorReportAsync(string address, string report, CancellationToken token)
    {
        _asked.Add(("report", address));
        Reports.Add((address, report));
        return Task.FromResult(ReportAnswer?.Invoke(report));
    }

    public Task<PostAnswer?> PostTargetAsync(string address, string package, byte[] image, string imageName, CancellationToken token)
    {
        _asked.Add(("post", address));
        Posted.Add((address, package, image));
        return Task.FromResult(Answer?.Invoke(address, package, image));
    }

    public async Task<long?> DownloadAsync(string address, string into, IProgress<double>? progress, CancellationToken token)
    {
        _asked.Add(("download", address));
        if (!Download.TryGetValue(address, out byte[]? bytes) || bytes is null)
        {
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(into))!);
        progress?.Report(0.5);
        if (WhileDownloading is { } pause)
        {
            await pause(0.5, token).ConfigureAwait(false);
        }

        token.ThrowIfCancellationRequested();
        await File.WriteAllBytesAsync(into, bytes, token).ConfigureAwait(false);
        progress?.Report(1);
        return bytes.LongLength;
    }

    public void StartInstaller(string path, string arguments)
    {
        _asked.Add(("installer", path));
        Installer = (path, arguments);
    }
}
