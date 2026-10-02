using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using GroupLab.App;
using GroupLab.App.Diagnostics;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Updates;

namespace GroupLab.Mobile;

/// <summary>
/// The phone application's start and what every screen shares, NOTES-FROM-PLANNING.md entry 219 item A3, moved here from the Android head
/// by entry 290 so the iPhone and iPad head starts the same way. The log and the crash records go in the application's own files, as the
/// desktop's go in its own folder, and the error reports waiting from an earlier run are sent by the desktop's own queue when the person has
/// said they may go.
/// </summary>
public static class Phone
{
    /// <summary>The tag every GroupLab line carries in the system log, entry 234 section 4: <c>adb logcat -s GroupLab</c> on Android.</summary>
    public const string LogTag = "GroupLab";

    /// <summary>The platform underneath, set by the head before anything else runs.</summary>
    public static IPhonePlatform Platform { get; private set; } = null!;

    /// <summary>The settings file, in the application's own files; nothing outside it can read it.</summary>
    internal static AppSettingsStore Settings { get; private set; } = AppSettingsStore.Default;

    internal static ErrorQueue? Errors { get; private set; }

    /// <summary>The hardware survey's queue, the desktop's own; it keeps and sends nothing until the person says yes.</summary>
    internal static SurveyQueue? Survey { get; private set; }

    /// <summary>The application's styles: the desktop's look B with the phone's cards on top (entry 246, request 49).</summary>
    public static void Initialize(Avalonia.Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.Styles.Add(new FluentTheme());
        PhoneStyles.Apply(application);

        // Entry 290 section 6: the plot's Key button, tapped with a thumb, gets a thumb's height to land on.
        GroupLab.App.CompositePlot.KeyStrip = Screens.Touch;
    }

    /// <summary>
    /// Starts the phone application on <paramref name="platform"/>: the settings, the log and its mirror into the system log, the crash
    /// records, the queues, and the one Shell. <paramref name="region"/> is the phone's own region, since invariant globalization hides it.
    /// </summary>
    public static void Start(IPhonePlatform platform, Avalonia.Application application, Func<string?> region, Action<LogLevel, string>? mirror)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(application);
        Platform = platform;
        string files = platform.FilesFolder;

        // Entry 232: the phone's own region, since invariant globalization hides it from .NET and a US phone started in metric.
        AppSettingsStore.RegionSource = region;
        Settings = new AppSettingsStore(Path.Combine(files, "settings.json"));
        var (directory, described) = LogDirectory.Resolve(false, AppContext.BaseDirectory);
        var log = new DiagnosticLog(directory, Settings.LoadVerbose()) { DescribedDirectory = described };
        DiagnosticLog.Current = log;

        // Entry 234 section 4: every line but DEBUG also goes to the system log, scrubbed as the file's are, because a copy from a store is
        // not debuggable and its own files cannot be read; errors, timings and what was being done are enough to diagnose from there.
        DiagnosticLog.Mirror = mirror;
        CrashReporter.Install(log);
        CrashReporter.BeginRun(log);
        DiagnosticLog.Info("app.start", [.. AppInfo.EnvironmentFields()]);
        // Entry 347: the store-bought fingerprint library kept from an earlier fetch, then a look for a newer one where it is due.
        LibraryFolder = Path.Combine(files, "library");
        DiagnosticLog.Info("library.kept", ("result", StoreLibraryUpdate.LoadSaved(LibraryFolder)));
        _ = RefreshLibraryAsync(DateTimeOffset.UtcNow);
        Errors = new ErrorQueue(Settings);
        Survey = new SurveyQueue(Settings, platform.Machine);
        _ = SendWaitingErrorsAsync();
        _ = Survey.SendDueAsync(Shell.SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None);
        if (application.ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new Shell();
        }
#if GROUPLAB_DEV

        // Entry 315 section 1, GroupLab Dev only: the automation bridge on the device's own loopback address, unless turned off.
        Dev.Bridge.StartUnlessTurnedOff();
#endif
    }

    /// <summary>Where a fetched library of store-bought fingerprints is kept between starts (entry 347).</summary>
    internal static string LibraryFolder { get; private set; } = "";

    /// <summary>
    /// Entry 347: the newest signed library on every phone copy, GroupLab Dev, Google Play's and the iPhone's alike, under entry 343's rules:
    /// only on an unmetered connection, with the battery and the storage not low, and at most every six hours. Says what it did, for the
    /// log; never throws.
    /// </summary>
    internal static async Task<string> RefreshLibraryAsync(DateTimeOffset now, IOutsideWorld? outside = null)
    {
        string said;
        try
        {
            string stamp = Path.Combine(LibraryFolder, "checked");
            if (Platform.Unmetered != true)
            {
                said = "not on an unmetered connection";
            }
            else if (Platform.BatteryLow != false || Platform.StorageLow == true)
            {
                said = "the battery or the storage is low, or the phone cannot say";
            }
            else if (File.Exists(stamp) && now - new DateTimeOffset(File.GetLastWriteTimeUtc(stamp), TimeSpan.Zero) < AndroidUpdates.Every)
            {
                said = "looked within six hours";
            }
            else
            {
                Directory.CreateDirectory(LibraryFolder);
                File.WriteAllText(stamp, now.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
                File.SetLastWriteTimeUtc(stamp, now.UtcDateTime);
                said = await StoreLibraryUpdate.CheckAsync(outside ?? TheOutsideWorld.Current, LibraryFolder).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            said = ex.GetType().Name;
        }

        DiagnosticLog.Info("library.check", ("result", said));
        return said;
    }

    /// <summary>What is waiting goes when the person chose Always; never a dialog, and a failure waits for the next start.</summary>
    internal static async Task SendWaitingErrorsAsync()
    {
        if (Errors is { } queue)
        {
            await queue.SendWaitingAsync(Shell.ErrorsOpen, asked: false, CancellationToken.None);
        }
    }
}
