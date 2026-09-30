using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using GroupLab.Core.Updates;

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

    /// <summary>
    /// <see cref="Version"/> as a person reads it on a phone, entry 290 section 6: the commit after the plus sign cut to its first seven
    /// characters, as git shortens it, since the whole forty wrapped onto a line of their own. Logs, error reports, exported files and the
    /// update check keep <see cref="Version"/> whole.
    /// </summary>
    public static string ShortVersion => Shortened(Version);

    /// <summary>Any version string with its commit cut to seven characters, such as the version an exported file says it came from.</summary>
    public static string Shortened(string version)
    {
        int plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 || version.Length - plus - 1 <= 7 ? version : version[..(plus + 8)];
    }

    /// <summary>The train the build was stamped with, or null for a build nobody published.</summary>
    public static string? Train { get; } =
        typeof(AppInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GroupLabTrain")?.Value;

    /// <summary>
    /// What this build is, NOTES-FROM-PLANNING.md entry 119 section 1.2: its version, the train it was published on, and its commit, all
    /// stamped in at build time. A build made on somebody's own machine has no train and says "development build".
    /// <para>
    /// <b>It is worked out on first use, not in a field initialiser, and that is not a style choice.</b> Entry 125 section 1: this was
    /// <c>= BuildIdentity.Read(Version, Train)</c> declared above <see cref="Train"/>, and C# runs static initialisers in the order they are
    /// written, so it read <see cref="Train"/> while it was still null. Every published build called itself a development build and so would
    /// never have updated itself. Read on demand, no declaration order can bring that back.
    /// </para>
    /// </summary>
    public static GroupLab.Core.Updates.BuildIdentity Build => TheBuild.Value;

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 224 section 3.1: whether this copy came from the Microsoft Store, which updates it, so GroupLab's own
    /// updater is off. Stamped in at build time; the tests set <see cref="FromStoreOverride"/>.
    /// </summary>
    public static bool FromStore => FromStoreOverride ?? string.Equals(Distribution, "store", StringComparison.Ordinal);

    public static string Distribution { get; } =
        typeof(AppInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "GroupLabDistribution")?.Value ?? "download";

    internal static bool? FromStoreOverride { get; set; }

    private static readonly Lazy<GroupLab.Core.Updates.BuildIdentity> TheBuild =
        new(() => GroupLab.Core.Updates.BuildIdentity.Read(Version, Train));

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
        ("train", Build.Train.Words().ToLowerInvariant()),
        ("channel", Channel),
        ("os", OperatingSystemName),
        ("framework", Framework),
        ("renderer", Renderer),
        ("culture", CultureInfo.CurrentCulture.Name),
    ];
}
