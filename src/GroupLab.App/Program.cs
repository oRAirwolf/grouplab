using System.Diagnostics;
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
        var settings = AppSettingsStore.Default;
        bool verbose = args.Contains("--verbose", StringComparer.Ordinal) || settings.LoadVerbose();
        var (directory, described) = LogDirectory.Resolve(AppInfo.IsDebugBuild, AppContext.BaseDirectory);
        using var log = new DiagnosticLog(directory, verbose) { DescribedDirectory = described };
        DiagnosticLog.Current = log;
        var units = settings.LoadUnits();
        DiagnosticLog.Info("app.start", [.. AppInfo.EnvironmentFields(), ("units", $"{units.Linear} {units.Angular} {units.Distance}"), ("verbose", verbose), ("logdir", described)]);
        var clock = Stopwatch.StartNew();
        int code = -1;
        try
        {
            code = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return code;
        }
        finally
        {
            DiagnosticLog.Info("app.exit", ("code", code), ("seconds", Math.Round(clock.Elapsed.TotalSeconds, 1)));
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
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        AppStyles.Apply(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
