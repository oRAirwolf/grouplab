using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using GroupLab.App;
using GroupLab.App.Diagnostics;

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
        Errors = new ErrorQueue(Settings);
        Survey = new SurveyQueue(Settings, platform.Machine);
        _ = SendWaitingErrorsAsync();
        _ = Survey.SendDueAsync(Shell.SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None);
        if (application.ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new Shell();
        }
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
