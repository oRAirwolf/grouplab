using Foundation;
using GroupLab.Core.Sync;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1 item 5 on iPhone and iPad: the sync folder is a folder in Files (in iCloud Drive or a
/// OneDrive folder, say) that the person picks once. iOS lets GroupLab back into it later only through a security-scoped bookmark, kept
/// beside the settings; each run resolves it, starts access, works through the folder's own path, and stops access again. Nothing a person
/// can reach picks the folder yet: the screen is waiting on its design (for-alan.md, DESIGN NEEDED of entry 461 section 3.2).
/// </summary>
internal static class BookmarkSyncFolder
{
    /// <summary>The bookmark for a folder the document picker gave, to keep; null where iOS refuses one.</summary>
    internal static byte[]? Bookmark(NSUrl picked)
    {
        ArgumentNullException.ThrowIfNull(picked);
        bool started = picked.StartAccessingSecurityScopedResource();
        try
        {
            var data = picked.CreateBookmarkData(NSUrlBookmarkCreationOptions.MinimalBookmark, null, null, out NSError? error);
            return error is null && data is not null ? data.ToArray() : null;
        }
        finally
        {
            if (started)
            {
                picked.StopAccessingSecurityScopedResource();
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/> on the folder a bookmark names, with access started and stopped around it. A bookmark that no longer
    /// resolves (the folder was moved, or the cloud app signed out) gives a folder that is not there, so the run changes nothing and says so.
    /// <paramref name="renewed"/> is given a fresh bookmark where iOS says the kept one is stale.
    /// </summary>
    internal static T With<T>(byte[] bookmark, Func<ISyncFolder, T> work, Action<byte[]>? renewed = null)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        ArgumentNullException.ThrowIfNull(work);
        var url = NSUrl.FromBookmarkData(NSData.FromArray(bookmark), NSUrlBookmarkResolutionOptions.WithoutUI, null, out bool stale, out NSError? error);
        if (url is null || error is not null || url.Path is not { } path)
        {
            return work(new DirectorySyncFolder(Path.Combine(Path.GetTempPath(), "grouplab-sync-folder-gone")));
        }

        bool started = url.StartAccessingSecurityScopedResource();
        try
        {
            if (stale && Bookmark(url) is { } fresh)
            {
                renewed?.Invoke(fresh);
            }

            return work(new DirectorySyncFolder(path));
        }
        finally
        {
            if (started)
            {
                url.StopAccessingSecurityScopedResource();
            }
        }
    }
}
