using System.Runtime.CompilerServices;

namespace GroupLab.Tests.Support;

/// <summary>
/// One temporary folder for the whole of a test run, removed when the run ends, NOTES-FROM-PLANNING.md entry 179.
/// <para>
/// <b>Why.</b> Alan's temporary folder held 14,987 <c>grouplab-settings-*.json</c> files, 956 MB, left by tests that made a settings store
/// in <see cref="Path.GetTempPath"/> and never removed it, with 60 bench folders and thousands of empty randomly named folders beside
/// them. Asking every test to clean up after itself had already failed, one forgotten line at a time. So the whole process is given its
/// own folder instead: before any test runs, <c>TMP</c>, <c>TEMP</c> and <c>TMPDIR</c> point at a fresh folder under
/// <c>grouplab-tests</c>, so everything that asks for the temporary directory, a test, the code under test, a library or a child
/// process, writes there, and the folder goes when the process exits.
/// </para>
/// <para>
/// A process that is killed cannot clean up, so each run also removes the folders of earlier runs that are more than a day old. Nothing
/// outside <c>grouplab-tests</c> is touched. <c>TestTempLeakTests</c> checks the folder is in use, and CI checks after the suite that it
/// left nothing behind.
/// </para>
/// </summary>
public static class TestTempRoot
{
    /// <summary>The name of the folder, in the machine's own temporary directory, that holds every run's folder.</summary>
    public const string Parent = "grouplab-tests";

    /// <summary>The system temporary directory as it was before this run redirected it.</summary>
    public static string Original { get; private set; } = Path.GetTempPath();

    /// <summary>This run's folder, which is what <see cref="Path.GetTempPath"/> returns for the rest of the process.</summary>
    public static string Folder { get; private set; } = "";

#pragma warning disable CA2255 // A test assembly is exactly where a process-wide setting belongs; nothing outside the tests loads this.
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Start()
    {
        if (Folder.Length > 0)
        {
            return;
        }

        Original = Path.GetTempPath();
        string parent = Path.Combine(Original, Parent);
        Sweep(parent);
        Folder = Path.Combine(parent, $"{Environment.ProcessId}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Folder);
        foreach (string name in new[] { "TMP", "TEMP", "TMPDIR" })
        {
            Environment.SetEnvironmentVariable(name, Folder);
        }

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(Folder, recursive: true);
            }
            catch (IOException)
            {
                // Something outside the process still holds a file; the next run's sweep removes the folder.
            }
            catch (UnauthorizedAccessException)
            {
            }
        };
    }

    /// <summary>Removes earlier runs' folders that are more than a day old: a run still going is never that old.</summary>
    private static void Sweep(string parent)
    {
        if (!Directory.Exists(parent))
        {
            return;
        }

        foreach (string run in Directory.EnumerateDirectories(parent))
        {
            try
            {
                if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(run) > TimeSpan.FromDays(1))
                {
                    Directory.Delete(run, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
