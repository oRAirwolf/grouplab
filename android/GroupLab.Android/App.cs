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

    public override void Initialize() => Phone.Initialize(this);

    public override void OnFrameworkInitializationCompleted()
    {
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
