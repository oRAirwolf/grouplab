using Avalonia;
using GroupLab.App.Diagnostics;
using GroupLab.Mobile;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A3: the application's start. Since entry 290 the start itself is the shared mobile project's
/// (<see cref="Phone.Start"/>), which the iOS head uses too; this is Android's part: its platform, its region and logcat.
/// </summary>
public sealed class App : Avalonia.Application
{
    /// <summary>The logcat tag every GroupLab line carries, entry 234 section 4: <c>adb logcat -s GroupLab</c>.</summary>
    internal const string LogTag = Phone.LogTag;

    public override void Initialize()
    {
        // Entry 411 section 2, Alan: "For android, follow the system". Set before the styles are built, so they are built at it.
        GroupLab.App.Theme.TextSize.Follow(SystemTextSize);
        Phone.Initialize(this);
    }

    /// <summary>
    /// <paramref name="size"/>, in Avalonia's units (Android's dp), at the phone's text size setting: Android's own conversion of a size in
    /// sp, which from Android 14 grows the larger sizes less than the smaller ones. Read each time, so a change of the setting is followed.
    /// </summary>
    internal static double SystemTextSize(double size) =>
        global::Android.App.Application.Context.Resources?.DisplayMetrics is { Density: > 0 } metrics
            ? global::Android.Util.TypedValue.ApplyDimension(global::Android.Util.ComplexUnitType.Sp, (float)size, metrics) / metrics.Density
            : size;

    public override void OnFrameworkInitializationCompleted()
    {
        // Entry 315 section 4: the phone's thermal status for the log and the diagnostics overlay (Android 10 and later, as GroupLab is).
        var power = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.PowerService) as global::Android.OS.PowerManager;
        DeviceHealth.Heat = () => power?.CurrentThermalStatus.ToString().ToLowerInvariant();
        Phone.Start(new AndroidPhone(), this,
            () => Java.Util.Locale.Default.Country is { Length: 2 } country ? country : null,
            (level, line) =>
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
            });

#if GROUPLAB_UPDATER
        // Entry 288: what was just installed, the six-hourly check, and a look now.
        Updates.SelfUpdate.Launched();
#endif

        base.OnFrameworkInitializationCompleted();
    }
}
