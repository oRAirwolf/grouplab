using Avalonia.Controls;
using GroupLab.Core.Gltd.Model;
using OpenCvSharp;

namespace GroupLab.Mobile;

/// <summary>
/// Everything the phone's screens need from the operating system underneath them, NOTES-FROM-PLANNING.md entry 290 section 2 item 1: the
/// screens live once, in this project, and each head (Android, iOS) answers these in its own way. Nothing else in this project may call
/// Android or iOS directly, and a test holds that (<c>MobileProjectTests</c>).
/// </summary>
public interface IPhonePlatform
{
    /// <summary>The application's own files, where the sessions, the database and the settings live; nothing outside it can read them.</summary>
    string FilesFolder { get; }

    /// <summary>The application's own cache, for a copy of a picture on its way in or a file on its way out.</summary>
    string CacheFolder { get; }

    /// <summary>The device in the words a shared session names it by, such as "GroupLab 0.2.0 on samsung SM-F966U1".</summary>
    string DeviceWords { get; }

    /// <summary>The operating system and the device, for the hardware survey.</summary>
    GroupLab.Core.Survey.MachineFacts Machine();

    /// <summary>Copies the sheets the application carries into <paramref name="into"/>: <c>targets</c> and <c>targets/frozen</c>.</summary>
    void CopyBundledTargets(string into);

    /// <summary>
    /// Decodes a picture at a power-of-two fraction of its size chosen so it stays under <paramref name="mostMegapixels"/>, as BGR, with the
    /// picture's own width and height before any reduction; null where it is not an image. Android decodes it reduced (entry 239), so a large
    /// scan never exists in memory at full size.
    /// </summary>
    (Mat Colour, int Width, int Height, int Sample)? DecodeReduced(string photo, double mostMegapixels);

    /// <summary>The memory, in megabytes, an analysis may use now (entry 240), logged with what the device said.</summary>
    double MemoryBudgetMegabytes();

    /// <summary>Whether the camera may be used; where not, it asks, and the person presses again once it is allowed.</summary>
    bool CameraAllowed();

    /// <summary>
    /// Whether the camera has been allowed, asking nothing: entry 312 section 6, so the line asking for it goes once it is. False where a
    /// platform cannot say, which leaves the line as it was.
    /// </summary>
    bool CameraGranted() => false;

    /// <summary>The camera screen: <paramref name="taken"/> hears the picture's path and whether the torch was on.</summary>
    Control Camera(Action<string, bool> taken, Action back, Action choose, Action? result = null);

    /// <summary>Whether a control is the camera screen this platform made.</summary>
    bool IsCamera(object? content);

    /// <summary>Whether the camera is open now: an update never installs while it is (entry 288).</summary>
    bool CameraOpen { get; }

    /// <summary>Hands a file to the system's share sheet; a sentence where it could not, else null.</summary>
    string? ShareFile(string path, string mimeType, string title);

    /// <summary>Prints a PDF through the system's print dialog; a sentence where it could not, else null.</summary>
    string? PrintPdf(byte[] pdf, string name, PageSize paper);

    /// <summary>Shares a PDF; a sentence where it could not, else null.</summary>
    string? SharePdf(byte[] pdf, string name);

    /// <summary>
    /// Saves a picture into the device's own gallery, entry 280 section 2 (Share A, "Save to gallery"); a sentence where it could not, else
    /// null. A head that has no gallery yet keeps this default, which says so, so the screens build against every head.
    /// </summary>
    string? SaveToGallery(string path, string mimeType) => "Saving to the gallery is not available on this device yet; use Share instead.";

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 292: opens the picker for <paramref name="source"/> and hands back the photographs chosen, unread, so
    /// the screens fetch each with a progress line that can be canceled. Android opens the system photo picker, or the apps that offer
    /// pictures by name; a head that has neither yet keeps this default, the system's file picker, so the screens build against every head.
    /// </summary>
    Task<IReadOnlyList<PhotoHandle>> PickPhotos(PhotoSource source, TopLevel? top) => PhotoIntake.FromFilePicker(top, several: false);

    /// <summary>
    /// Whether the phone has a connection to the internet now, so a photograph kept only in the cloud that could not be fetched is said to
    /// be for that reason (entry 292 section 1.4); null where the head cannot tell, which keeps this default.
    /// </summary>
    bool? Online => null;

    /// <summary>Copies a picture on the clipboard into <paramref name="folder"/> and returns its path, or null where there is none.</summary>
    Task<string?> PastePicture(string folder);

    /// <summary>The updater's card for Settings, About, in a build that has one (entry 288); null in every other.</summary>
    Control? UpdateCard();

    /// <summary>Whether this is the development build, which says so in Settings.</summary>
    bool IsDevBuild { get; }

    /// <summary>
    /// Opens a web address in the phone's browser (entries 299 and 309): what GroupLab sends, the phone's getting-started guide. By default
    /// through Avalonia's launcher, which both heads provide; a test phone records it instead.
    /// </summary>
    void OpenAddress(string address)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime { MainView: { } view }
            && TopLevel.GetTopLevel(view) is { } top)
        {
            _ = top.Launcher.LaunchUriAsync(new Uri(address));
        }
    }
}
