namespace GroupLab.Tests.Support;

/// <summary>
/// Throwing away a test's temporary folder, which is not the same thing as testing it.
/// <para>
/// <b>Why this exists.</b> On 2026-09-22 CI went red on `FolderVerbsTests`, and the failure was not in the test at all: it was
/// `Directory.Delete` in the finally block, unable to remove a PNG the test had written seconds earlier because something outside the
/// process still had it open. On Windows that something is usually the virus scanner or the search indexer looking at a newly written
/// file, and there is nothing the test can do about it.
/// </para>
/// <para>
/// <b>A cleanup that cannot delete is not a failing test.</b> Reporting it as one sends somebody to read a test that was working, and
/// teaches everybody that a red on that test means nothing, which is worse. So this waits a little, tries again, and then gives up
/// quietly: the folder is under the system's temporary directory and the machine will clear it.
/// </para>
/// </summary>
public static class Temp
{
    /// <summary>How many times to try, and how long to wait between. Under a second in total, and almost always one attempt.</summary>
    public const int Tries = 5;

    public const int WaitMilliseconds = 120;

    /// <summary>
    /// Waits until a file a test has just written can actually be opened for reading, and returns it.
    /// <para>
    /// <b>The other half of the same problem as <see cref="Delete"/>.</b> On 2026-09-22 the end to end test failed on its own assertion,
    /// not in cleanup: it wrote a PNG and handed the path straight to the analysis, and the analysis could not open it because something
    /// outside the process had taken the newly written file. On Windows that is the virus scanner or the search indexer, and it is nothing
    /// to do with the code under test.
    /// </para>
    /// <para>
    /// Unlike a cleanup, this cannot give up quietly: the test really does need the file. So it waits, and if it still cannot be read it
    /// lets the failure happen, where at least the message says what was going on.
    /// </para>
    /// </summary>
    public static string Readable(string path)
    {
        for (int attempt = 1; attempt <= Tries; attempt++)
        {
            try
            {
                using var open = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return path;
            }
            catch (IOException) when (attempt < Tries)
            {
                Thread.Sleep(WaitMilliseconds);
            }
        }

        return path;
    }

    /// <summary>A folder in the system's temporary directory, named so its owner can be told from the others.</summary>
    public static string Folder(string what) =>
        Path.Combine(Path.GetTempPath(), $"grouplab-{what}-{Guid.NewGuid():N}");

    /// <summary>
    /// Removes one file, and does nothing at all where it cannot. The same reasoning as <see cref="Delete"/>: on 2026-09-22 the end to end
    /// test failed in its finally block because a PNG it had written seconds earlier was still open outside the process, and a cleanup that
    /// cannot delete is not a failing test.
    /// </summary>
    public static void DeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        for (int attempt = 1; attempt <= Tries; attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == Tries)
                {
                    return;
                }

                Thread.Sleep(WaitMilliseconds);
            }
        }
    }

    /// <summary>Removes a folder and everything under it, and does nothing at all where it cannot.</summary>
    public static void Delete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        for (int attempt = 1; attempt <= Tries; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (attempt == Tries)
                {
                    return;
                }

                Thread.Sleep(WaitMilliseconds);
            }
        }
    }
}
