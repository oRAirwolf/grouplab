namespace GroupLab.App.Diagnostics;

/// <summary>
/// Where the diagnostic log lives, NOTES-FROM-PLANNING.md entry 41 sections 3 and 4, resolved once at startup:
/// <list type="number">
/// <item><c>GROUPLAB_LOG_DIR</c>, everywhere, when it is set.</item>
/// <item>In a Debug build, <c>out/logs</c> under the repository the executable was built from, found by walking up to
/// <c>GroupLab.slnx</c>, so the planning session can read a log without anybody pasting it. <c>out/</c> is ignored by git.</item>
/// <item>Otherwise the platform's convention: <c>%LOCALAPPDATA%\GroupLab\logs</c> on Windows, <c>~/Library/Logs/GroupLab</c> on macOS,
/// and <c>$XDG_STATE_HOME/grouplab/logs</c> on Linux, falling back to <c>~/.local/state/grouplab/logs</c>.</item>
/// </list>
/// The directory is also returned described in those symbolic terms, which is how the first line of the log names it: the resolved path
/// begins with the user's name on every platform, and a path is never written to the log.
/// </summary>
public static class LogDirectory
{
    public const string Override = "GROUPLAB_LOG_DIR";

    public static (string Path, string Described) Resolve(bool debugBuild, string executableDirectory)
    {
        if (Environment.GetEnvironmentVariable(Override) is { Length: > 0 } configured)
        {
            return (configured, "$" + Override);
        }

        if (debugBuild && RepositoryRoot(executableDirectory) is { } root)
        {
            return (System.IO.Path.Combine(root, "out", "logs"), "<repository>/out/logs");
        }

        return Platform();
    }

    /// <summary>The nearest directory at or above the given one that holds <c>GroupLab.slnx</c>, or null.</summary>
    public static string? RepositoryRoot(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "GroupLab.slnx")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    private static (string Path, string Described) Platform()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsWindows())
        {
            return (System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GroupLab", "logs"), @"%LOCALAPPDATA%\GroupLab\logs");
        }

        if (OperatingSystem.IsMacOS())
        {
            return (System.IO.Path.Combine(home, "Library", "Logs", "GroupLab"), "~/Library/Logs/GroupLab");
        }

        return Environment.GetEnvironmentVariable("XDG_STATE_HOME") is { Length: > 0 } state
            ? (System.IO.Path.Combine(state, "grouplab", "logs"), "$XDG_STATE_HOME/grouplab/logs")
            : (System.IO.Path.Combine(home, ".local", "state", "grouplab", "logs"), "~/.local/state/grouplab/logs");
    }
}
