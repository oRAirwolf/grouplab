using System.Collections.Concurrent;
using System.Globalization;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Trace;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291 section 7.5: GroupLab Dev keeps every picture it takes, so a whole sitting can be pulled off the phone
/// at once and each picture becomes a measured test case. Each goes into a folder of its own under <see cref="Folder"/>, numbered in the
/// order taken: the picture with its metadata taken out (no location, no time, no camera settings), what the last live frame read before
/// it, and the analysis's trace once it has been read. Nothing is sent anywhere. Only GroupLab Dev keeps them, never GroupLab from Google
/// Play or a release build, and a switch in its Settings turns it off and deletes what was kept.
/// </summary>
internal static class SittingRecord
{
    /// <summary>The folder a sitting is kept in, under the application's own files: <c>files/sitting</c>.</summary>
    public static string Folder => Path.Combine(Phone.Platform.FilesFolder, "sitting");

    /// <summary>
    /// Whether pictures are being kept: only where the build offers it (GroupLab Dev, and the iPhone and iPad application of entry 311), and
    /// only while the switch in Settings is on.
    /// </summary>
    public static bool On => Phone.Platform.KeepsSittings is (true, var byDefault) && Phone.Settings.LoadKeepSitting(byDefault);

    private static readonly ConcurrentDictionary<string, string> ByPicture = new(StringComparer.Ordinal);

    /// <summary>
    /// A picture the camera has just taken, kept with <paramref name="live"/>, what the last live frame read. Returns the folder, or null where
    /// nothing is kept.
    /// </summary>
    public static string? Keep(string picture, string live)
    {
        if (!On || !File.Exists(picture))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(Folder);
            int next = Directory.EnumerateDirectories(Folder, "picture-*").Count() + 1;
            string folder = Path.Combine(Folder, string.Create(CultureInfo.InvariantCulture, $"picture-{next:0000}"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "picture.jpg"), JpegWithoutMetadata(File.ReadAllBytes(picture)));
            File.WriteAllText(Path.Combine(folder, "live.txt"), live);
            ByPicture[picture] = folder;
            DiagnosticLog.Info("sitting.keep", ("folder", Path.GetFileName(folder)));
            return folder;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Info("sitting.keep", ("error", e.GetType().Name));
            return null;
        }
    }

    /// <summary>The analysis of a kept picture, its trace written beside it; nothing where the picture was not kept.</summary>
    public static void Analyzed(string picture, TraceRecorder trace, string summary)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (!ByPicture.TryRemove(picture, out string? folder))
        {
            return;
        }

        try
        {
            var lines = new List<string> { summary, "" };
            lines.AddRange(trace.Records.Select(r => TraceConsole.Format(r, 2)));
            File.WriteAllLines(Path.Combine(folder, "analysis.txt"), lines);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Info("sitting.trace", ("error", e.GetType().Name));
        }
    }

    /// <summary>How many pictures are kept now.</summary>
    public static int Count() => Directory.Exists(Folder) ? Directory.EnumerateDirectories(Folder, "picture-*").Count() : 0;

    /// <summary>Everything kept, deleted.</summary>
    public static void Clear()
    {
        ByPicture.Clear();
        if (Directory.Exists(Folder))
        {
            Directory.Delete(Folder, recursive: true);
        }

        DiagnosticLog.Info("sitting.clear");
    }

    /// <summary>
    /// A JPEG with every application segment but the first (JFIF) and every comment left out: EXIF, with its location, times and camera
    /// settings, XMP and the maker's notes all live in those. The picture's own data is copied as it is, so nothing is decoded or re-encoded.
    /// Anything that is not a JPEG is returned empty, so nothing unknown is kept.
    /// </summary>
    internal static byte[] JpegWithoutMetadata(byte[] jpeg)
    {
        ArgumentNullException.ThrowIfNull(jpeg);
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
        {
            return [];
        }

        using var kept = new MemoryStream(jpeg.Length);
        kept.Write(jpeg, 0, 2);
        int at = 2;
        while (at + 4 <= jpeg.Length)
        {
            if (jpeg[at] != 0xFF)
            {
                return [];
            }

            byte marker = jpeg[at + 1];
            if (marker == 0xFF)
            {
                at++;
                continue;
            }

            int length = (jpeg[at + 2] << 8) | jpeg[at + 3];
            if (marker == 0xDA)
            {
                // The start of the scan: the picture itself, to the end.
                kept.Write(jpeg, at, jpeg.Length - at);
                return kept.ToArray();
            }

            if (at + 2 + length > jpeg.Length)
            {
                return [];
            }

            bool metadata = marker is >= 0xE1 and <= 0xEF or 0xFE;
            if (!metadata)
            {
                kept.Write(jpeg, at, 2 + length);
            }

            at += 2 + length;
        }

        return [];
    }
}
