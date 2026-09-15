using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GroupLab.App.Diagnostics;

/// <summary>
/// What the application is and where it is running, for the first line of every log and for a crash record (NOTES-FROM-PLANNING.md entry
/// 41 section 3 and entry 45 section 2). None of it identifies a person: no machine name, no user name, no path.
/// </summary>
public static class AppInfo
{
    /// <summary>The informational version, which carries the commit after a plus sign when the build knows it.</summary>
    public static string Version { get; } =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(AppInfo).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>The first seven characters of the commit in <see cref="Version"/>, or null when the build did not record one.</summary>
    public static string? Commit
    {
        get
        {
            int plus = Version.IndexOf('+', StringComparison.Ordinal);
            if (plus < 0)
            {
                return null;
            }

            string commit = Version[(plus + 1)..];
            return commit.Length > 7 ? commit[..7] : commit;
        }
    }

    public static string Channel =>
#if DEBUG
        "debug";
#else
        "release";
#endif

    public static bool IsDebugBuild => Channel == "debug";

    public static string OperatingSystemName => RuntimeInformation.OSDescription;

    public static string Framework => RuntimeInformation.FrameworkDescription;

    /// <summary>Avalonia 12 draws through Skia on every desktop platform it runs on.</summary>
    public const string Renderer = "Skia";

    /// <summary>The environment block of <c>app.start</c>, entry 41 section 3.</summary>
    public static IReadOnlyList<(string Key, object? Value)> EnvironmentFields() =>
    [
        ("version", Version),
        ("channel", Channel),
        ("os", OperatingSystemName),
        ("framework", Framework),
        ("renderer", Renderer),
        ("culture", CultureInfo.CurrentCulture.Name),
    ];
}
