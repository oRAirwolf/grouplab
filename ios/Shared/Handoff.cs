using System.Globalization;
using Foundation;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2.3: how the share extension hands pictures to the application. iOS gives one app no way into
/// another's library, so Google Photos, Photos and every other app reach GroupLab by sharing. The extension copies what was shared into the
/// app group's container, one folder a share, named by the time and hidden by a leading dot until every picture in it is written, then
/// opens <c>grouplab://shared</c>; the application takes each finished folder in turn as a set, one picture a sheet. Compiled into both the
/// application and the extension, so the two cannot disagree about where the pictures are.
/// </summary>
internal static class Handoff
{
    /// <summary>The app group the application and the extension share; it has to be registered with Apple for a signed build.</summary>
    internal const string Group = "group.org.grouplab.app";

    /// <summary>The address scheme GroupLab answers, declared in the application's Info.plist.</summary>
    internal const string Scheme = "grouplab";

    /// <summary>The address's host that says pictures are waiting in the app group.</summary>
    internal const string Shared = "shared";

    /// <summary>The address the extension opens the application with.</summary>
    internal static string Address => Scheme + "://" + Shared;

    /// <summary>The folder shared pictures wait in, made if it is not there; null where the app group is not available to this build.</summary>
    internal static string? Incoming()
    {
        string? container = NSFileManager.DefaultManager.GetContainerUrl(Group)?.Path;
        if (string.IsNullOrEmpty(container))
        {
            return null;
        }

        return Directory.CreateDirectory(Path.Combine(container, "incoming")).FullName;
    }

    /// <summary>A new share's folder, hidden until <see cref="Finish"/>, so the application never reads a picture still being written.</summary>
    internal static string Begin(string incoming) =>
        Directory.CreateDirectory(Path.Combine(incoming, "." + DateTime.UtcNow.Ticks.ToString("D20", CultureInfo.InvariantCulture))).FullName;

    /// <summary>The share's folder shown to the application once every picture in it is written.</summary>
    internal static void Finish(string batch)
    {
        string name = Path.GetFileName(batch);
        Directory.Move(batch, Path.Combine(Path.GetDirectoryName(batch)!, name.TrimStart('.')));
    }

    /// <summary>The finished shares waiting, oldest first.</summary>
    internal static IReadOnlyList<string> Waiting(string incoming) =>
        Directory.Exists(incoming)
            ? [.. Directory.EnumerateDirectories(incoming).Where(d => !Path.GetFileName(d).StartsWith('.')).Order(StringComparer.Ordinal)]
            : [];

    /// <summary>The pictures of one share, in the order they were shared.</summary>
    internal static IReadOnlyList<string> Pictures(string batch) =>
        [.. Directory.EnumerateFiles(batch).Where(f => !Path.GetFileName(f).StartsWith('.')).Order(StringComparer.Ordinal)];

    /// <summary>The name the <paramref name="index"/>th picture of a share is written under, keeping its own ending.</summary>
    internal static string Name(int index, string extension) =>
        index.ToString("D3", CultureInfo.InvariantCulture) + (extension.StartsWith('.') ? extension : "." + extension);
}
