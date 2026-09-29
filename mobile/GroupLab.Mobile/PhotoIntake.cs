using System.Globalization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;

namespace GroupLab.Mobile;

/// <summary>Where a photograph is chosen from, NOTES-FROM-PLANNING.md entry 292 section 1.</summary>
public enum PhotoSource
{
    /// <summary>
    /// The system's photo picker: every photograph the phone's photo apps show, those kept only in the cloud included where the phone's
    /// cloud photo app offers them, with no permission asked. Where a phone has none, the apps are offered instead (section 4 item 3).
    /// </summary>
    Photos,

    /// <summary>
    /// Every app that offers pictures, by name: Google Photos, Samsung Gallery, the maker's own gallery on other phones, Drive, OneDrive,
    /// Dropbox and Files. This is how a photo editor reaches them (section 1 item 2).
    /// </summary>
    OtherApp,
}

/// <summary>
/// A photograph another app has handed over and GroupLab has not read yet: the app's name where Android says it, the size in pixels and in
/// bytes where the app states them, the file's kind, and how to open it. Nothing about where it was taken is ever part of it.
/// </summary>
public sealed record PhotoHandle(string? App, int? Width, int? Height, long? Bytes, string Extension, Func<Task<Stream?>> Open)
{
    /// <summary>
    /// Entry 292 section 2.1: where the whole photograph has to arrive before any of it can be read, as iOS's photo picker downloads one
    /// kept only in iCloud: this hears how far the download has got, from 0 to 1, and is stopped by Cancel. Null where <see cref="Open"/>
    /// streams the photograph as it arrives, as every Android app does.
    /// </summary>
    public Func<Action<double>, CancellationToken, Task<Stream?>>? Download { get; init; }
}

/// <summary>A photograph read into the cache, and a sentence where the app handed over less than the whole photograph.</summary>
public sealed record PickedPhoto(string Path, string? Reduced);

/// <summary>What reading one photograph came to: the photograph, or a sentence saying why not, or that the person canceled.</summary>
public sealed record PhotoFetch(PickedPhoto? Photo, string? Said, bool Canceled)
{
    internal static PhotoFetch Stopped { get; } = new(null, null, true);
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 sections 1 and 4: a person can reach any photograph they can see in their phone's photo apps, whichever
/// app keeps it and whether it is on the phone or only in the cloud. The decisions live here, so they are tested on any machine; the heads
/// only open the pickers and hand over streams (<see cref="IPhonePlatform.PickPhotos"/>).
/// </summary>
public static class PhotoIntake
{
    /// <summary>The actions another app starts GroupLab with to hand it pictures: share one, share several, open with, and edit with.</summary>
    public static readonly IReadOnlyList<string> Actions =
        ["android.intent.action.SEND", "android.intent.action.SEND_MULTIPLE", "android.intent.action.VIEW", "android.intent.action.EDIT"];

    /// <summary>
    /// Section 4 item 3: the system photo picker where the phone has one; where it does not (no Google Play services, or software sold for
    /// another market), the apps that offer pictures, never an error.
    /// </summary>
    public static PhotoSource Opens(PhotoSource asked, bool photoPickerAvailable) =>
        asked == PhotoSource.Photos && !photoPickerAvailable ? PhotoSource.OtherApp : asked;

    /// <summary>Whether another app's start is a picture handed to GroupLab: one of <see cref="Actions"/>, with an image type.</summary>
    public static bool IsIncoming(string? action, string? type) => action is not null && Actions.Contains(action) && IsImage(type);

    /// <summary>Whether a stated type is a picture; a stream whose type is unknown is kept and left to the decode to judge.</summary>
    public static bool IsImage(string? type) => type is null || type.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>The file's ending for a picture's stated type, so the decode knows what it is reading.</summary>
    public static string Extension(string? type) => type?.ToLowerInvariant() switch
    {
        "image/png" => ".png",
        "image/heic" or "image/heif" => ".heic",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        "image/bmp" or "image/x-ms-bmp" => ".bmp",
        _ => ".jpg",
    };

    /// <summary>
    /// The app a photograph comes from, in the words a person knows it by: the system's own media store and photo picker are not an app
    /// anyone chose, so they are not named; Google's and Samsung's galleries are named as their makers do; any other app by its own label.
    /// </summary>
    public static string? AppName(string? authority, string? package, string? label)
    {
        if (authority is null || authority == "media" || authority.StartsWith("com.android.providers.media", StringComparison.Ordinal)
            || authority.StartsWith("com.google.android.providers.media", StringComparison.Ordinal))
        {
            return null;
        }

        return package switch
        {
            "com.google.android.apps.photos" => "Google Photos",
            "com.sec.android.gallery3d" => "Samsung Gallery",
            "com.google.android.apps.docs" => "Google Drive",
            _ => string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
        };
    }

    /// <summary>The line under the progress bar while a photograph is fetched: which app, which photograph of several, and how much so far.</summary>
    public static string Getting(string? app, int index, int count, long read, long? size)
    {
        string what = count > 1 ? string.Create(CultureInfo.CurrentCulture, $"Getting photo {index + 1} of {count}") : "Getting the photo";
        string from = app is { Length: > 0 } ? " from " + app : "";
        string far = size is > 0
            ? string.Create(CultureInfo.CurrentCulture, $", {Megabytes(read)} of {Megabytes(size.Value)} MB")
            : read > 0 ? string.Create(CultureInfo.CurrentCulture, $", {Megabytes(read)} MB so far") : "";
        return what + from + far;
    }

    /// <summary>
    /// The line while a photograph is downloaded whole before it can be read (entry 292 section 2.1), where only the share done is known.
    /// </summary>
    public static string Downloading(string? app, int index, int count, double done) =>
        Getting(app, index, count, 0, null) + (done > 0 ? string.Create(CultureInfo.CurrentCulture, $", {Math.Min(done, 1) * 100:0} percent") : "");

    private static string Megabytes(long bytes) => (bytes / 1048576.0).ToString("0.0", CultureInfo.CurrentCulture);

    /// <summary>
    /// Section 1.4: the photograph could not be read. Most often it is kept only in the cloud and the phone is offline: where the phone says
    /// it is, the sentence says so; where it cannot tell (<paramref name="online"/> null), it says what to do in either case.
    /// </summary>
    public static string CouldNotGet(string? app, bool? online = null)
    {
        string from = app is { Length: > 0 } ? " from " + app : "";
        return online == false
            ? $"The phone is offline, so GroupLab could not get the photo{from}: a photo kept only in the cloud needs the internet. Connect, then choose it again, or choose one kept on the phone."
            : $"GroupLab could not get the photo{from}. If it is kept only in the cloud, the phone has to be online to fetch it: connect to the internet and choose it again, or choose one kept on the phone.";
    }

    /// <summary>
    /// Section 1.4: whether the picture received is smaller than the photograph the app says it has, either way up, allowing a few pixels
    /// for a decoder that rounds. Unknown either side is not reduced: the picture check still scores the resolution it has.
    /// </summary>
    public static bool Reduced(int receivedWidth, int receivedHeight, int? statedWidth, int? statedHeight)
    {
        if (statedWidth is not > 0 || statedHeight is not > 0 || receivedWidth <= 0 || receivedHeight <= 0)
        {
            return false;
        }

        int receivedLong = Math.Max(receivedWidth, receivedHeight), receivedShort = Math.Min(receivedWidth, receivedHeight);
        int statedLong = Math.Max(statedWidth.Value, statedHeight.Value), statedShort = Math.Min(statedWidth.Value, statedHeight.Value);
        return receivedLong < statedLong * 0.95 || receivedShort < statedShort * 0.95;
    }

    /// <summary>Section 1.4: the sentence for a reduced copy, and another way to reach the whole photograph.</summary>
    public static string ReducedWords(string? app, int receivedWidth, int receivedHeight, int statedWidth, int statedHeight)
    {
        string who = app is { Length: > 0 } ? app : "The app";
        return string.Create(CultureInfo.CurrentCulture,
            $"{who} handed over a smaller copy of this photo, {receivedWidth} by {receivedHeight} pixels of its {statedWidth} by {statedHeight}, so GroupLab sees less detail and small holes can be missed. For the whole photo, share it into GroupLab from {(app is { Length: > 0 } ? app : "the app")}, or download it to the phone first and choose it again.");
    }

    /// <summary>
    /// Reads one photograph into <paramref name="folder"/>: through its stream a piece at a time, saying how far it has got, so a photograph
    /// kept only in the cloud downloads with a progress line and can be canceled; then its size is compared with what the app stated, and
    /// with the size the camera recorded in the picture, so a reduced copy is said to be one. The location in a photograph is never read.
    /// </summary>
    public static async Task<PhotoFetch> Fetch(PhotoHandle photo, string folder, string name, int index, int count, Action<string>? progress,
        CancellationToken cancel)
    {
        ArgumentNullException.ThrowIfNull(photo);
        string path = Path.Combine(folder, (count > 1 ? string.Create(CultureInfo.InvariantCulture, $"{name}-{index + 1}") : name) + photo.Extension);
        progress?.Invoke(Getting(photo.App, index, count, 0, photo.Bytes));
        var work = Task.Run(() => Copy(photo, path, index, count, progress, cancel), CancellationToken.None);

        // A stream from a cloud app can sit in a read that nothing interrupts; the person is not kept waiting for it, and the copy deletes
        // whatever it wrote when it finishes.
        var stopped = Task.Delay(Timeout.Infinite, cancel);
        if (await Task.WhenAny(work, stopped).ConfigureAwait(false) != work)
        {
            _ = work.ContinueWith(_ => Delete(path), TaskScheduler.Default);
            DiagnosticLog.Info("phone.pick.fetch", ("outcome", "canceled"));
            return PhotoFetch.Stopped;
        }

        long bytes;
        try
        {
            bytes = await work.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Delete(path);
            return PhotoFetch.Stopped;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException
            || e.GetType().FullName?.StartsWith("Java.", StringComparison.Ordinal) == true)
        {
            Delete(path);
            bool? online = Phone.Platform?.Online;
            DiagnosticLog.Info("phone.pick.fetch", ("outcome", "failed"), ("error", e.GetType().Name), ("online", online));
            return new PhotoFetch(null, CouldNotGet(photo.App, online), false);
        }

        if (bytes == 0)
        {
            Delete(path);
            return new PhotoFetch(null, CouldNotGet(photo.App, Phone.Platform?.Online), false);
        }

        string? reduced = null;
        if (Received(path) is { } received)
        {
            var stated = Stated(photo, path);
            if (stated is { } s && Reduced(received.Width, received.Height, s.Width, s.Height))
            {
                reduced = ReducedWords(photo.App, received.Width, received.Height, s.Width, s.Height);
            }

            DiagnosticLog.Info("phone.pick.fetch", ("outcome", "read"), ("kb", bytes / 1024), ("size", $"{received.Width}x{received.Height}"),
                ("stated", stated is { } t ? $"{t.Width}x{t.Height}" : "none"), ("reduced", reduced is not null));
        }

        return new PhotoFetch(new PickedPhoto(path, reduced), null, false);
    }

    private static async Task<long> Copy(PhotoHandle photo, string path, int index, int count, Action<string>? progress, CancellationToken cancel)
    {
        try
        {
            var opening = photo.Download is { } download
                ? download(done => progress?.Invoke(Downloading(photo.App, index, count, done)), cancel)
                : photo.Open();
            await using var from = await opening.ConfigureAwait(false) ?? throw new IOException("the app gave no stream");
            await using var registration = cancel.Register(() =>
            {
                try
                {
                    from.Dispose();
                }
                catch (Exception e) when (e is IOException or ObjectDisposedException or InvalidOperationException)
                {
                    // The read in progress ends with its own error, which is the one reported.
                }
            });
            await using var to = File.Create(path);
            var buffer = new byte[81920];
            long read = 0;
            long said = 0;
            int got;
            while ((got = await from.ReadAsync(buffer, cancel).ConfigureAwait(false)) > 0)
            {
                cancel.ThrowIfCancellationRequested();
                await to.WriteAsync(buffer.AsMemory(0, got), cancel).ConfigureAwait(false);
                read += got;
                if (read - said >= 262144)
                {
                    said = read;
                    progress?.Invoke(Getting(photo.App, index, count, read, photo.Bytes));
                }
            }

            return read;
        }
        catch
        {
            Delete(path);
            throw;
        }
    }

    /// <summary>The picture's own width and height as received, from its header, or from a small decode where the header is not one read here.</summary>
    private static (int Width, int Height)? Received(string path)
    {
        var header = ImageMetadataReader.Read(File.ReadAllBytes(path));
        if (header.Width is > 0 && header.Height is > 0)
        {
            return (header.Width.Value, header.Height.Value);
        }

        if (Phone.Platform?.DecodeReduced(path, 0.05) is { } decoded)
        {
            decoded.Colour.Dispose();
            return (decoded.Width, decoded.Height);
        }

        return null;
    }

    /// <summary>The size the app stated, or else the size the camera recorded in the picture itself.</summary>
    private static (int Width, int Height)? Stated(PhotoHandle photo, string path)
    {
        if (photo.Width is > 0 && photo.Height is > 0)
        {
            return (photo.Width.Value, photo.Height.Value);
        }

        var recorded = ImageMetadataReader.Read(File.ReadAllBytes(path));
        return recorded.StatesRecordedSize ? (recorded.RecordedWidth!.Value, recorded.RecordedHeight!.Value) : null;
    }

    private static void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Left in the cache, which the phone empties itself; nothing of it is read again.
        }
    }

    /// <summary>
    /// The default for a head without pickers of its own: the system's file picker through Avalonia, as Choose a photograph was before
    /// entry 292. It reaches the same photographs by either source; the iOS head has its own (section 2).
    /// </summary>
    public static async Task<IReadOnlyList<PhotoHandle>> FromFilePicker(TopLevel? top, bool several)
    {
        if (top?.StorageProvider is not { } storage)
        {
            return [];
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a photograph of a target",
            AllowMultiple = several,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });
        var handles = new List<PhotoHandle>();
        foreach (var file in files)
        {
            var properties = await file.GetBasicPropertiesAsync();
            string ending = Path.GetExtension(file.Name);
            handles.Add(new PhotoHandle(null, null, null, properties.Size is { } size ? (long)size : null,
                ending.Length > 0 ? ending.ToLowerInvariant() : ".jpg", async () => await file.OpenReadAsync()));
        }

        return handles;
    }
}
