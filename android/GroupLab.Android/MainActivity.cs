using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace GroupLab.Android;

/// <summary>
/// The one activity. All four ways up, honoring the rotation lock (entry 205), and kept through folding, turning and a change of density,
/// so the layout follows the size it is given (entry 199). What a person is doing lives with the application, not the activity, so a new
/// activity after Back opens where they left off (entry 205 section 3.1).
/// </summary>
[Activity(
    Label = "GroupLab",
    Theme = "@style/GroupLabTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
    internal static MainActivity? Current { get; private set; }

    private const int CameraRequest = 219;

    /// <summary>Whether the camera may be used; asks the person once where it may not, and says false until they answer.</summary>
    internal static bool CameraAllowed()
    {
        if (Current is not { } activity)
        {
            return false;
        }

        if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.Camera) == Permission.Granted)
        {
            return true;
        }

        AndroidX.Core.App.ActivityCompat.RequestPermissions(activity, [global::Android.Manifest.Permission.Camera], CameraRequest);
        return false;
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        base.OnCreate(savedInstanceState);
    }
}

[global::Android.App.Application]
public class GroupLabApplication : AvaloniaAndroidApplication<App>
{
    protected GroupLabApplication(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        // The desktop's log goes beside the executable or in the user's folder; the phone's goes in the application's own files.
        System.Environment.SetEnvironmentVariable(GroupLab.App.Diagnostics.LogDirectory.Override, Path.Combine(FilesDir!.AbsolutePath, "logs"));
        base.OnCreate();
    }
}
