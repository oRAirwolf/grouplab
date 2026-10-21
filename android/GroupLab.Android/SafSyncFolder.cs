using Android.Content;
using Android.Database;
using Android.Provider;
using GroupLab.Core.Sync;
using Uri = Android.Net.Uri;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1 item 5 on Android: the sync folder reached through Android's own folder picker, the Storage
/// Access Framework, which reaches a OneDrive, Dropbox or Google Drive folder through their apps. The picker gives a tree address; GroupLab
/// keeps its permission across restarts and reads and writes documents under it, never a path. Settings, Sync between devices, opens it
/// (entry 463, <see cref="AndroidPhone.ChooseSyncFolder"/>).
/// </summary>
internal sealed class SafSyncFolder(Context context, Uri tree) : ISyncFolder
{
    /// <summary>The request the folder picker answers through <see cref="MainActivity.ForResult"/>.</summary>
    internal const int PickRequest = 4611;

    /// <summary>Android's folder picker.</summary>
    internal static Intent PickIntent() =>
        new Intent(Intent.ActionOpenDocumentTree).AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

    /// <summary>Keeps the permission the picker granted, so the folder is still reachable after GroupLab or the phone restarts.</summary>
    internal static void Remember(Context context, Uri tree) =>
        context.ContentResolver!.TakePersistableUriPermission(tree, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

    private const string Folder = DocumentsContract.Document.MimeTypeDir;

    private ContentResolver Resolver => context.ContentResolver!;

    private Uri Document(string id) => DocumentsContract.BuildDocumentUriUsingTree(tree, id)!;

    public bool Available
    {
        get
        {
            try
            {
                using var cursor = Resolver.Query(Document(DocumentsContract.GetTreeDocumentId(tree)!), [DocumentsContract.Document.ColumnDocumentId], null, null, null);
                return cursor is not null && cursor.MoveToFirst();
            }
            catch (Exception e) when (e is Java.Lang.SecurityException or Java.Lang.IllegalArgumentException or Java.IO.FileNotFoundException)
            {
                return false;
            }
        }
    }

    /// <summary>The children of a document, by name, with their ids and whether each is a folder.</summary>
    private List<(string Name, string Id, bool IsFolder)> Children(string parentId)
    {
        var all = new List<(string, string, bool)>();
        var children = DocumentsContract.BuildChildDocumentsUriUsingTree(tree, parentId)!;
        using ICursor? cursor = Resolver.Query(children,
            [DocumentsContract.Document.ColumnDisplayName, DocumentsContract.Document.ColumnDocumentId, DocumentsContract.Document.ColumnMimeType], null, null, null);
        while (cursor is not null && cursor.MoveToNext())
        {
            all.Add((cursor.GetString(0) ?? "", cursor.GetString(1) ?? "", cursor.GetString(2) == Folder));
        }

        return all;
    }

    /// <summary>The id of the document at a relative path, or null; with <paramref name="make"/>, its folders are made on the way.</summary>
    private string? Find(string path, bool make, bool lastIsFolder)
    {
        string id = DocumentsContract.GetTreeDocumentId(tree)!;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            bool folder = i < parts.Length - 1 || lastIsFolder;
            var match = Children(id).FirstOrDefault(c => c.Name == parts[i] && c.IsFolder == folder);
            if (match.Id is { Length: > 0 })
            {
                id = match.Id;
                continue;
            }

            if (!make || !folder)
            {
                return null;
            }

            id = DocumentsContract.GetDocumentId(DocumentsContract.CreateDocument(Resolver, Document(id), Folder, parts[i])!)!;
        }

        return id;
    }

    public IReadOnlyList<string> Files(string folder) =>
        Find(folder, false, true) is { } id ? [.. Children(id).Where(c => !c.IsFolder).Select(c => c.Name).Order(StringComparer.Ordinal)] : [];

    public IReadOnlyList<string> Folders(string folder) =>
        Find(folder, false, true) is { } id ? [.. Children(id).Where(c => c.IsFolder).Select(c => c.Name).Order(StringComparer.Ordinal)] : [];

    public byte[]? Read(string path)
    {
        if (Find(path, false, false) is not { } id)
        {
            return null;
        }

        using var input = Resolver.OpenInputStream(Document(id));
        if (input is null)
        {
            return null;
        }

        using var copy = new MemoryStream();
        input.CopyTo(copy);
        return copy.ToArray();
    }

    public void Write(string path, ReadOnlySpan<byte> bytes)
    {
        int slash = path.LastIndexOf('/');
        string folder = slash < 0 ? "" : path[..slash];
        string name = path[(slash + 1)..];
        string parent = Find(folder, true, true) ?? throw new IOException("The sync folder could not be made.");

        // Written beside its name and renamed into place, as on the computer, so another device never reads half of it.
        var part = DocumentsContract.CreateDocument(Resolver, Document(parent), "application/octet-stream", "." + name + ".part")
            ?? throw new IOException("The sync folder refused a new file.");
        using (var output = Resolver.OpenOutputStream(part, "wt") ?? throw new IOException("The sync folder refused to be written."))
        {
            output.Write(bytes);
        }

        if (Children(parent).FirstOrDefault(c => c.Name == name && !c.IsFolder).Id is { Length: > 0 } existing)
        {
            DocumentsContract.DeleteDocument(Resolver, Document(existing));
        }

        DocumentsContract.RenameDocument(Resolver, part, name);
    }

    public bool Exists(string path) => Find(path, false, false) is not null;
}
