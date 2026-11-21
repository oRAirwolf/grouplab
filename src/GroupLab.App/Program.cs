using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>The desktop entry point: an Avalonia application with one window, the marking screen.</summary>
internal static class Program
{
    /// <summary>
    /// Starts the diagnostic log before anything else, NOTES-FROM-PLANNING.md entry 41 section 3, so that whatever goes wrong afterwards
    /// leaves evidence: <c>app.start</c> with the environment block, and <c>app.exit</c> with the exit code and how long the run lasted.
    /// <c>--verbose</c> turns on DEBUG lines for this run, as the remembered setting does for every run.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        // Entry 422: a section landed on flashes, or holds a steady tint where the computer asks for less motion.
        Landing.ReducedMotion = SystemMotion.Reduced;
        var settings = AppSettingsStore.Default;
        bool verbose = args.Contains("--verbose", StringComparer.Ordinal) || settings.LoadVerbose();
        var (directory, described) = LogDirectory.Resolve(AppInfo.IsDebugBuild, AppContext.BaseDirectory);
        using var log = new DiagnosticLog(directory, verbose) { DescribedDirectory = described };
        DiagnosticLog.Current = log;

        // Entry 41 section 5: the handlers go in before the window exists, so nothing the window does can fail unrecorded.
        using var crashes = CrashReporter.Install(log);
        CrashReporter.BeginRun(log);
        var units = settings.LoadUnits();
        // Entry 501: in the Mac App Store copy's sandbox, links and files open through NSWorkspace.
        if (AppInfo.Sandboxed)
        {
            GroupLab.Core.Updates.TheOutsideWorld.OpensThings = MacSandbox.Open;
        }

        App.SelfCheck = args.Contains("--self-check", StringComparer.Ordinal);
        DiagnosticLog.Info("app.start", [.. AppInfo.EnvironmentFields(), ("units", $"{units.Linear} {units.Angular} {units.Distance}"), ("verbose", verbose), ("logdir", described)]);
        var clock = Stopwatch.StartNew();
        // Entry 507 section 2: the records and the target library open beside Avalonia's own start-up, which the splash then shows.
        Landing.MotionChosen = settings.LoadReduceMotion;
        StartupWork.Current = StartupWork.Begin(settings);
        int code = -1;
        try
        {
            code = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            // A window that could not be built stops start-up as it did before there was a splash: thrown from here, and recorded.
            App.StartFailure?.Throw();
            return code;
        }
        finally
        {
            DiagnosticLog.Info("app.exit", ("code", code), ("seconds", Math.Round(clock.Elapsed.TotalSeconds, 1)));
            CrashReporter.EndRun(log);
        }
    }

    /// <summary>The application builder, also used by the headless tests.</summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

/// <summary>
/// The application. Its look is Avalonia's Fluent theme with GroupLab's own palette, type and control styles over it,
/// <see cref="AppStyles"/> (NOTES-FROM-PLANNING.md entry 42). It follows the system's light or dark setting unless the user chooses one;
/// high contrast, DESIGN.md section 19's fourth theme, is later work.
/// </summary>
public sealed class App : Application
{
    /// <summary>Entry 501: <c>--self-check</c>, the run that proves a build on a machine nobody sits at (<see cref="GroupLab.App.SelfCheck"/>).</summary>
    internal static bool SelfCheck { get; set; }

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        AppStyles.Apply(this);
        SpokenNames.Attach();
    }

    /// <summary>Entry 507 section 2: the main window could not be built behind the splash; <c>Main</c> throws it once the loop has ended.</summary>
    internal static ExceptionDispatchInfo? StartFailure { get; private set; }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (StartupWork.Current is { } work)
            {
                _ = StartBehindSplashAsync(desktop, work);
            }
            else
            {
                Opening(desktop, new MainWindow());
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Entry 507 section 2: the splash first, then the main window as soon as it is built. The splash has no taskbar entry, no close
    /// button and never takes the focus; it closes once the main window is open, and if start-up fails it closes and the failure is
    /// thrown as before.
    /// </summary>
    private static async Task StartBehindSplashAsync(IClassicDesktopStyleApplicationLifetime desktop, StartupWork work)
    {
        StartupSequence.Mark("framework");
        var splash = new StartupSplash(StartupSplash.VersionLine(AppInfo.Version), Landing.Still());
        StartupSequence.Mark("splash");
        splash.Show();
        StartupSequence.Mark("shown");
        try
        {
            await StartupSequence.RunAsync(splash, [work.Records, work.Library], () => new MainWindow(), () => StartupSequence.NextFrame(splash), main =>
            {
                Opening(desktop, main);
                main.Opened += (_, _) =>
                {
                    StartupSequence.Mark("opened");
                    DiagnosticLog.Info("app.startup", [.. StartupSequence.Marks.Select(m => (m.Part, (object?)m.Ms))]);
                };
                main.Show();
            });
        }
#pragma warning disable CA1031 // Whatever stopped the window is thrown again from Main, where it was always thrown from.
        catch (Exception e)
#pragma warning restore CA1031
        {
            StartFailure = ExceptionDispatchInfo.Capture(e);
            desktop.Shutdown(1);
        }
    }

    /// <summary>The main window becoming the application's, with what runs once it is open.</summary>
    private static void Opening(IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.MainWindow = window;

        // Entry 414 section 1.2: once the window is up, the solver warmed on a thread of the lowest priority, so Ballistics' first
        // tables do not wait for it.
        window.Opened += (_, _) => GroupLab.Core.Ballistics.SolverUse.WarmInBackground();
        if (SelfCheck)
        {
            window.Opened += async (_, _) => desktop.Shutdown(await GroupLab.App.SelfCheck.RunAsync(window));
        }
    }
}
