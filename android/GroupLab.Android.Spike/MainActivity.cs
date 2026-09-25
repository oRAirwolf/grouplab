using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace GroupLab.Android.Spike;

/// <summary>
/// The one screen. Entry 199 section 1.2: folding, unfolding and turning the device are ordinary events, so the activity keeps running
/// through every one of them and the layout follows the size it is given, rather than the activity being torn down and rebuilt.
/// <para>
/// Entry 205 section 2: all four ways up, upside down portrait included, which Android leaves out unless asked. <c>FullUser</c> rather than
/// <c>FullSensor</c>, because FullSensor turns the screen even when the person has locked rotation, and a lock is theirs to set.
/// </para>
/// <para>
/// Entry 205 section 3.1: each create and destroy goes to the log with a count, so a list that starts again can be told apart as the
/// activity being made again or only its view.
/// </para>
/// </summary>
[Activity(
    Label = "GroupLab spike",
    Theme = "@style/GroupLabTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
    private static int created;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        created++;
        global::Android.Util.Log.Info(SpikeView.LogTag, $"{DateTime.Now:HH:mm:ss} activity created, the {created} time in this process{(savedInstanceState is null ? "" : ", restoring saved state")}");
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        global::Android.Util.Log.Info(SpikeView.LogTag, $"{DateTime.Now:HH:mm:ss} activity destroyed{(IsChangingConfigurations ? ", for a configuration change" : IsFinishing ? ", finishing" : "")}");
        base.OnDestroy();
    }
}

[global::Android.App.Application]
public class SpikeApplication : AvaloniaAndroidApplication<App>
{
    protected SpikeApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
}
