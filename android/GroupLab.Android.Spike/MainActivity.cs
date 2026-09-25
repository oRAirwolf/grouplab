using Android.App;
using Android.Content;
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

    /// <summary>The activity showing, for the folder picker, which is started from it.</summary>
    internal static MainActivity? Current { get; private set; }

    /// <summary>
    /// Entry 209: a measurement to run by itself as soon as the screen has its size, named by the launch, so each runs unattended in a
    /// fresh process: <c>adb shell am start -n ... --es task scale:0.5</c>, or <c>cameras</c>.
    /// </summary>
    internal static string? PendingTask { get; set; }

    internal const int FolderRequest = 209;

    internal const int CameraRequest = 219;

    /// <summary>Whether the camera may be used; asks the person once where it may not, and says false until they answer.</summary>
    internal static bool CameraAllowed()
    {
        if (Current is not { } activity)
        {
            return false;
        }

        if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.Camera) == global::Android.Content.PM.Permission.Granted)
        {
            return true;
        }

        AndroidX.Core.App.ActivityCompat.RequestPermissions(activity, [global::Android.Manifest.Permission.Camera], CameraRequest);
        return false;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        created++;
        Current = this;
        PendingTask = Intent?.GetStringExtra("task");
        global::Android.Util.Log.Info(SpikeView.LogTag, $"{DateTime.Now:HH:mm:ss} activity created, the {created} time in this process{(savedInstanceState is null ? "" : ", restoring saved state")}");
        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Entry 209 section 1.4: what the folder picker gave. Only the provider is logged, never the folder's name or path: which of Google
    /// Drive, OneDrive or the phone's own storage offered a folder is the whole question.
    /// </summary>
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == FolderRequest)
        {
            global::Android.Util.Log.Info(SpikeView.LogTag, resultCode == Result.Ok && data?.Data is { } uri
                ? $"{DateTime.Now:HH:mm:ss} folder picker: a folder was chosen from the provider {uri.Authority}"
                : $"{DateTime.Now:HH:mm:ss} folder picker: nothing chosen");
        }
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
