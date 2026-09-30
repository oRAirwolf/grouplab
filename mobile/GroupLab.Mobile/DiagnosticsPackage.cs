using System.Globalization;
using System.IO.Compression;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 311 section 3 item 1: Settings, About, "Send diagnostics". One zip file with GroupLab's newest logs, its crash
/// records, and the sitting's kept pictures with what each read, handed to the system's share sheet so the person can AirDrop it, save it to
/// Files or email it. Nothing is sent by GroupLab itself: the person chooses where it goes. The logs are already scrubbed of paths and
/// personal words as they are written, and a kept picture has had its metadata, location included, taken out when it was kept.
/// </summary>
internal static class DiagnosticsPackage
{
    /// <summary>How many of the newest logs go in: this run's and the four before it, which is where a crash's cause usually is.</summary>
    internal const int Logs = 5;

    /// <summary>The folder the zip is written in: the cache's shared folder, the only one Android's share sheet may read from.</summary>
    private static string Shared => Path.Combine(Phone.Platform.CacheFolder, "shared");

    /// <summary>
    /// Writes the zip and returns its path with how many logs, crash records and pictures it holds; null where there is nothing to put in it.
    /// </summary>
    internal static (string Path, int Logs, int Crashes, int Pictures)? Write(DateTime now)
    {
        DiagnosticLog.Current.Flush();
        string? logs = DiagnosticLog.Current.Directory;
        var logFiles = logs is not null && Directory.Exists(logs)
            ? new DirectoryInfo(logs).EnumerateFiles("grouplab-*.log").OrderByDescending(f => f.Name, StringComparer.Ordinal).Take(Logs).ToList()
            : [];
        var crashes = logs is not null && Directory.Exists(logs)
            ? new DirectoryInfo(logs).EnumerateFiles("crash-*.json").OrderByDescending(f => f.Name, StringComparer.Ordinal).Take(Logs).ToList()
            : [];
        var sitting = Directory.Exists(SittingRecord.Folder)
            ? new DirectoryInfo(SittingRecord.Folder).EnumerateFiles("*", SearchOption.AllDirectories).ToList()
            : [];
        if (logFiles.Count == 0 && crashes.Count == 0 && sitting.Count == 0)
        {
            return null;
        }

        // Only the newest package is kept; the share sheet has read the last one by the time another is made.
        if (Directory.Exists(Shared))
        {
            Directory.Delete(Shared, recursive: true);
        }

        Directory.CreateDirectory(Shared);
        string path = Path.Combine(Shared, "GroupLab diagnostics " + now.ToString("yyyy-MM-dd HHmm", CultureInfo.InvariantCulture) + ".zip");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var about = zip.CreateEntry("about.txt", CompressionLevel.Optimal);
            using (var writer = new StreamWriter(about.Open()))
            {
                writer.WriteLine(Phone.Platform.DeviceWords);
                writer.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Made {now:yyyy-MM-dd HH:mm}, {logFiles.Count} logs, {crashes.Count} crash records, {sitting.Count(f => f.Name == "picture.jpg")} kept pictures."));
            }

            foreach (var file in logFiles.Concat(crashes))
            {
                Add(zip, file.FullName, "logs/" + file.Name, CompressionLevel.Optimal);
            }

            foreach (var file in sitting)
            {
                string relative = Path.GetRelativePath(SittingRecord.Folder, file.FullName).Replace('\\', '/');
                // A JPEG is compressed already, so it is stored as it is.
                Add(zip, file.FullName, "sitting/" + relative, file.Extension == ".jpg" ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
            }
        }

        return (path, logFiles.Count, crashes.Count, sitting.Count(f => f.Name == "picture.jpg"));
    }

    /// <summary>Copies a file in, read beside whoever is writing it: this run's log is still open.</summary>
    private static void Add(ZipArchive zip, string from, string name, CompressionLevel level)
    {
        try
        {
            using var source = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var into = zip.CreateEntry(name, level).Open();
            source.CopyTo(into);
        }
        catch (IOException)
        {
            // A file that went while the package was made (a log rotated away) is left out rather than stopping the rest.
        }
    }

    /// <summary>The button's work: the zip made and handed to the share sheet; the sentence to show under the button.</summary>
    internal static string Send(DateTime now)
    {
        try
        {
            if (Write(now) is not { } made)
            {
                return "There is nothing to send yet: GroupLab has written no log on this device.";
            }

            DiagnosticLog.Info("diagnostics.share", ("logs", made.Logs), ("crashes", made.Crashes), ("pictures", made.Pictures),
                ("kb", new FileInfo(made.Path).Length / 1024));
            if (Phone.Platform.ShareFile(made.Path, "application/zip", "Send diagnostics") is { } failed)
            {
                return failed;
            }

            string pictures = made.Pictures == 1 ? "1 kept picture" : $"{made.Pictures} kept pictures";
            return $"Ready to send: {made.Logs} logs, {made.Crashes} crash records and {pictures}, in one file. Choose where it goes; GroupLab sends nothing itself.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Exception(LogLevel.Warn, "diagnostics.share", e);
            return "The diagnostics file could not be made on this device; the log says why.";
        }
    }
}
