using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>The desktop entry point: an Avalonia application with one window, the marking screen.</summary>
internal static class Program
{
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

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
