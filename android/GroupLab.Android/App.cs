using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using GroupLab.App;
using GroupLab.App.Diagnostics;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3: the application's start. The log and the crash records go in the application's own files,
/// as the desktop's go in its own folder, and the error reports waiting from an earlier run are sent by the desktop's own queue when the
/// person has said they may go.
/// </summary>
public sealed class App : Avalonia.Application
{
    /// <summary>The logcat tag every GroupLab line carries, entry 234 section 4: <c>adb logcat -s GroupLab</c>.</summary>
    internal const string LogTag = "GroupLab";

    /// <summary>The settings file, in the application's own files; nothing outside it can read it.</summary>
    internal static AppSettingsStore Settings { get; private set; } = AppSettingsStore.Default;

    internal static ErrorQueue? Errors { get; private set; }

    /// <summary>The hardware survey's queue, the desktop's own; it keeps and sends nothing until the person says yes.</summary>
    internal static SurveyQueue? Survey { get; private set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());

        // Entry 246, request 49: look B. The desktop's styles and palette, the accent its amber, and the phone's cards on top of them.
        PhoneStyles.Apply(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        string files = global::Android.App.Application.Context.FilesDir!.AbsolutePath;

        // Entry 232: the phone's own region, since invariant globalization hides it from .NET and a US phone started in metric.
        AppSettingsStore.RegionSource = () => Java.Util.Locale.Default.Country is { Length: 2 } country ? country : null;
        Settings = new AppSettingsStore(Path.Combine(files, "settings.json"));
        var (directory, described) = LogDirectory.Resolve(false, AppContext.BaseDirectory);
        var log = new DiagnosticLog(directory, Settings.LoadVerbose()) { DescribedDirectory = described };
        DiagnosticLog.Current = log;

        // Entry 234 section 4: every line but DEBUG also goes to logcat, scrubbed as the file's are, because a copy from Google Play is not
        // debuggable and its own files cannot be read; errors, timings and what was being done are enough to diagnose from there.
        DiagnosticLog.Mirror = (level, line) =>
        {
            if (level == LogLevel.Error)
            {
                global::Android.Util.Log.Error(LogTag, line);
            }
            else if (level == LogLevel.Warn)
            {
                global::Android.Util.Log.Warn(LogTag, line);
            }
            else
            {
                global::Android.Util.Log.Info(LogTag, line);
            }
        };
        CrashReporter.Install(log);
        CrashReporter.BeginRun(log);
        DiagnosticLog.Info("app.start", [.. AppInfo.EnvironmentFields()]);
        Errors = new ErrorQueue(Settings);
        Survey = new SurveyQueue(Settings, Machine);
        _ = SendWaitingErrorsAsync();
        _ = Survey.SendDueAsync(Shell.SurveyOpen, DateTimeOffset.UtcNow, CancellationToken.None);
        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new Shell();
        }

#if GROUPLAB_UPDATER
        // Entry 288: what was just installed, the six-hourly check, and a look now.
        Updates.SelfUpdate.Launched();
#endif

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>The phone as the survey describes it: Android's own version and the model, as docs/SURVEY.md section 2 lists them.</summary>
    private static GroupLab.Core.Survey.MachineFacts Machine() =>
        GroupLab.Core.Survey.SurveyReport.ThisMachine(device: global::Android.OS.Build.Manufacturer + " " + global::Android.OS.Build.Model)
            with { OperatingSystem = "Android " + global::Android.OS.Build.VERSION.Release };

    /// <summary>What is waiting goes when the person chose Always; never a dialog, and a failure waits for the next start.</summary>
    internal static async Task SendWaitingErrorsAsync()
    {
        if (Errors is { } queue)
        {
            await queue.SendWaitingAsync(Shell.ErrorsOpen, asked: false, CancellationToken.None);
        }
    }
}
