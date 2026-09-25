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
    /// <summary>The settings file, in the application's own files; nothing outside it can read it.</summary>
    internal static AppSettingsStore Settings { get; private set; } = AppSettingsStore.Default;

    internal static ErrorQueue? Errors { get; private set; }

    /// <summary>The hardware survey's queue, the desktop's own; it keeps and sends nothing until the person says yes.</summary>
    internal static SurveyQueue? Survey { get; private set; }

    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        string files = global::Android.App.Application.Context.FilesDir!.AbsolutePath;
        Settings = new AppSettingsStore(Path.Combine(files, "settings.json"));
        var (directory, described) = LogDirectory.Resolve(false, AppContext.BaseDirectory);
        var log = new DiagnosticLog(directory, Settings.LoadVerbose()) { DescribedDirectory = described };
        DiagnosticLog.Current = log;
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
