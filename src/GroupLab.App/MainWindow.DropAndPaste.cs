using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;
using GroupLab.Core.Updates;

namespace GroupLab.App;

/// <summary>
/// Opening an image by dropping it on the window or pasting it, NOTES-FROM-PLANNING.md entry 137.
/// <para>
/// <b>Both are the same act as Open</b>, and go through the same call, so a dropped file and a chosen file cannot behave differently. What
/// is new is where the file comes from, and one of the two sources has no file at all: image data copied from a browser or a screenshot tool
/// has no name, no path and no metadata, and GroupLab says so where that matters.
/// </para>
/// <para>
/// <b>The clipboard is read only on an explicit Paste</b>, never on its own, and only through <see cref="IOutsideWorld"/>. A test that read
/// the real clipboard would take whatever happened to be on the machine at that moment; one that wrote it would take something away from the
/// person running the tests.
/// </para>
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>
    /// What Open accepts, and therefore what a drop and a paste accept. One list, so the three routes cannot drift apart.
    /// </summary>
    internal static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    /// <summary>Where a pasted image is written: GroupLab's own folder, never beside the person's files.</summary>
    internal static string PastedFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupLab", "pasted");

    /// <summary>The overlay shown while a file is dragged over the window.</summary>
    private readonly Border dropTarget = new()
    {
        IsVisible = false,
        IsHitTestVisible = false,
        Classes = { AppStyles.DropTarget },
        Child = new TextBlock
        {
            Text = "Drop to open this image",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { AppStyles.Title },
        },
    };

    /// <summary>
    /// Wires up dropping and pasting, and tells the outside world how to read this window's clipboard. Called once, as the window is built.
    /// </summary>
    private void ListenForDropsAndPastes()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) => ShowDropTarget(e), Avalonia.Interactivity.RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragEnterEvent, (_, e) => ShowDropTarget(e), Avalonia.Interactivity.RoutingStrategies.Bubble);
        AddHandler(DragDrop.DragLeaveEvent, (_, _) => dropTarget.IsVisible = false, Avalonia.Interactivity.RoutingStrategies.Bubble);
        AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            dropTarget.IsVisible = false;
            OpenDropped(Dropped(e));
            e.Handled = true;
        }, Avalonia.Interactivity.RoutingStrategies.Bubble);

        // The clipboard belongs to a window, and GroupLab.Core has none, so the application hands it the way to read this one.
        TheOutsideWorld.ReadsTheClipboard = ReadThisWindowsClipboard;
    }

    /// <summary>Whether a drag carries something GroupLab could open, and the overlay that says so.</summary>
    private void ShowDropTarget(DragEventArgs e)
    {
        bool any = Dropped(e).Count > 0;
        e.DragEffects = any ? DragDropEffects.Copy : DragDropEffects.None;
        dropTarget.IsVisible = any;
        e.Handled = true;
    }

    /// <summary>The image files in a drag, in the order given, ignoring everything else.</summary>
    private static IReadOnlyList<string> Dropped(DragEventArgs e) =>
        e.DataTransfer.TryGetFiles() is { } files
            ? [.. files.Select(f => f.TryGetLocalPath()).OfType<string>().Where(LooksLikeAnImage)]
            : [];

    /// <summary>Whether a path has one of the extensions Open accepts. The file itself still has to decode.</summary>
    internal static bool LooksLikeAnImage(string path) =>
        ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Opens what was dropped: the first image, saying how many were ignored. Entry 137 section 1. A drop of several is not a question,
    /// because the answer is always the first one and asking would put a dialog in the way of a gesture whose whole point is speed.
    /// </summary>
    internal void OpenDropped(IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0)
        {
            status.Text = $"That is not an image GroupLab can open. It opens {ImageWords}.";
            return;
        }

        string first = paths[0];
        Leaving(() =>
        {
            if (OpenImageSafely(first) && paths.Count > 1)
            {
                status.Text += string.Create(CultureInfo.InvariantCulture,
                    $" {paths.Count - 1} other {(paths.Count == 2 ? "file was" : "files were")} dropped with it and not opened.");
            }
        });
    }

    /// <summary>
    /// Ctrl+V, entry 137 section 2: an image file copied in a file manager, or image data with no file behind it, such as a screenshot or an
    /// image copied from a browser.
    /// </summary>
    internal async Task PasteImage()
    {
        var clipboard = await TheOutsideWorld.Current.ReadClipboardAsync(CancellationToken.None).ConfigureAwait(true);
        var files = clipboard.Files.Where(LooksLikeAnImage).ToList();
        if (files.Count > 0)
        {
            OpenDropped(files);
            return;
        }

        if (clipboard.Bytes is not { Length: > 0 } bytes)
        {
            status.Text = $"There is no image on the clipboard. Copy an image file or an image itself, then press Ctrl+V. GroupLab opens {ImageWords}.";
            return;
        }

        // Image data has no file behind it, so it gets one inside GroupLab's own folder, and is an ordinary opened file from then on.
        string into = Path.Combine(PastedFolder, "pasted-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + (clipboard.Extension ?? ".png"));
        try
        {
            Directory.CreateDirectory(PastedFolder);
            await File.WriteAllBytesAsync(into, bytes).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            status.Text = "The pasted image could not be written to GroupLab's own folder, so it was not opened.";
            return;
        }

        Leaving(() =>
        {
            if (OpenImageSafely(into))
            {
                // Entry 137 section 2: pasted data carries no file name and no metadata, and the one place that costs something is the scale
                // on a blank sheet, which otherwise comes from the scan's own resolution.
                status.Text += " It was pasted, so it has no file name and no resolution of its own: set the scale by measuring a known distance.";
            }
        });
    }

    /// <summary>
    /// Opens an image and says plainly where it cannot, rather than letting a decode failure reach the crash reporter. All three routes in,
    /// Open, drop and paste, come through here, so a file refused one way is refused the same way by the others.
    /// </summary>
    /// <returns>Whether it opened.</returns>
    internal bool OpenImageSafely(string path)
    {
        try
        {
            OpenImage(path);
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or OpenCvSharp.OpenCVException)
        {
            DiagnosticLog.Info("image.refused", DiagnosticLog.File(path));
            status.Text = $"That file could not be opened as an image. GroupLab opens {ImageWords}.";
            return false;
        }
    }

    /// <summary>The accepted types as a person reads them, used in every message about a refusal.</summary>
    private const string ImageWords = "JPEG and PNG images";

    /// <summary>
    /// Reads this window's clipboard. It is the only place in GroupLab that touches a real clipboard, and it is reached only through
    /// <see cref="IOutsideWorld"/>, which the tests replace.
    /// </summary>
    private async Task<ClipboardContents> ReadThisWindowsClipboard(CancellationToken token)
    {
        if (Clipboard is not { } board)
        {
            return ClipboardContents.Nothing;
        }

        using var transfer = await board.TryGetDataAsync().ConfigureAwait(true);
        if (transfer is null)
        {
            return ClipboardContents.Nothing;
        }

        // Files first: a file copied in a file manager has a name, a path and its own metadata, all of which are worth more than the pixels.
        if (await transfer.TryGetValuesAsync(DataFormat.File).ConfigureAwait(true) is { } items)
        {
            var paths = items.Select(i => i.TryGetLocalPath()).OfType<string>().ToList();
            if (paths.Count > 0)
            {
                return new ClipboardContents(paths, null, null);
            }
        }

        // Image data with no file behind it: a screenshot, or an image copied from a browser. The platform hands it over as a bitmap, so it
        // is written out as a PNG, which is lossless and is what GroupLab would rather read anyway.
        if (await transfer.TryGetValueAsync(DataFormat.Bitmap).ConfigureAwait(true) is { } bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, new PngBitmapEncoderOptions());
            return new ClipboardContents([], memory.ToArray(), ".png");
        }

        return ClipboardContents.Nothing;
    }
}
