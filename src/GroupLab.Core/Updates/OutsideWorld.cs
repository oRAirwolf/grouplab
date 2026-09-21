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
    /// Downloads a file, reporting the share done as it goes. It returns the bytes written, so a caller can tell a short download from a
    /// whole one, and throws nothing on a refusal: it returns null.
    /// </summary>
    Task<long?> DownloadAsync(string address, string into, IProgress<double>? progress, CancellationToken token);

    /// <summary>
    /// Starts an installer with its own arguments and does not wait. It is separate from <see cref="OpenFile"/> because an installer is
    /// never opened with the shell's idea of what to do with a file: it is run, with the switches that keep it silent.
    /// </summary>
    void StartInstaller(string path, string arguments);
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

    /// <summary>Written down when an installer is started: the file and its switches.</summary>
    public (string Path, string Arguments)? Installer { get; private set; }

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
    }

    public void OpenAddress(string address) => _asked.Add(("address", address));

    public void OpenFile(string path) => _asked.Add(("file", path));

    public void OpenFolder(string path) => _asked.Add(("folder", path));

    public Task<string?> GetTextAsync(string address, CancellationToken token)
    {
        _asked.Add(("get", address));
        return Task.FromResult(Text.TryGetValue(address, out string? text) ? text : null);
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
