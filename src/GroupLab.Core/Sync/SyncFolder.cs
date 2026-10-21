namespace GroupLab.Core.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1 item 5: the folder the person's own cloud client syncs, reached however the platform reaches
/// it: any folder on the computer, Android's own folder picker (the Storage Access Framework), and on iPhone and iPad a folder in Files kept by
/// a security-scoped bookmark. Paths are relative and use forward slashes. GroupLab never talks to the cloud; the cloud company's app moves
/// the files.
/// </summary>
public interface ISyncFolder
{
    /// <summary>Whether the folder is there to read and write now: false when it was unplugged, signed out of, or taken away.</summary>
    bool Available { get; }

    /// <summary>The files directly in <paramref name="folder"/>, by name; none where the folder does not exist.</summary>
    IReadOnlyList<string> Files(string folder);

    /// <summary>The folders directly in <paramref name="folder"/>, by name.</summary>
    IReadOnlyList<string> Folders(string folder);

    /// <summary>The file's bytes, or null where it is not there.</summary>
    byte[]? Read(string path);

    /// <summary>
    /// Writes the file whole: to a name beginning with a full stop beside it, which no reader takes, and then renamed into place, so another
    /// device's GroupLab never takes half of it for the whole.
    /// </summary>
    void Write(string path, ReadOnlySpan<byte> bytes);

    bool Exists(string path);

    /// <summary>
    /// Takes the file away; nothing where it is not there. Only tidying calls it (entry 464 section 1), and only once a whole copy of the
    /// file has been read back from the sync folder's trash.
    /// </summary>
    void Delete(string path);
}

/// <summary>A folder on this device's own file system: the computer's, or on iPhone and iPad the folder a bookmark has opened.</summary>
public sealed class DirectorySyncFolder(string root) : ISyncFolder
{
    public string Root { get; } = root;

    public bool Available => Directory.Exists(Root);

    private string Full(string path) => System.IO.Path.Combine(Root, path.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public IReadOnlyList<string> Files(string folder) =>
        Directory.Exists(Full(folder)) ? [.. Directory.EnumerateFiles(Full(folder)).Select(f => System.IO.Path.GetFileName(f)).Order(StringComparer.Ordinal)] : [];

    public IReadOnlyList<string> Folders(string folder) =>
        Directory.Exists(Full(folder)) ? [.. Directory.EnumerateDirectories(Full(folder)).Select(f => System.IO.Path.GetFileName(f)).Order(StringComparer.Ordinal)] : [];

    public byte[]? Read(string path)
    {
        try
        {
            return File.ReadAllBytes(Full(path));
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Write(string path, ReadOnlySpan<byte> bytes)
    {
        string full = Full(path);
        string folder = System.IO.Path.GetDirectoryName(full)!;

        // The folder's own folders are made on the way, never the folder itself: one that went away part way through a run (unplugged, or
        // moved) is not made again empty somewhere else.
        if (!Directory.Exists(Root))
        {
            throw new DirectoryNotFoundException("The sync folder is not there now.");
        }

        Directory.CreateDirectory(folder);
        string part = System.IO.Path.Combine(folder, "." + System.IO.Path.GetFileName(full) + ".part");
        using (var stream = new FileStream(part, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes);
            stream.Flush(flushToDisk: true);
        }

        File.Move(part, full, overwrite: true);
    }

    public bool Exists(string path) => File.Exists(Full(path));

    public void Delete(string path)
    {
        if (!Directory.Exists(Root))
        {
            throw new DirectoryNotFoundException("The sync folder is not there now.");
        }

        File.Delete(Full(path));
    }
}
