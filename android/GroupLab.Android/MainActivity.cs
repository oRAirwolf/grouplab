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
    Label = MainActivity.Name,
    Theme = "@style/GroupLabTheme",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public class MainActivity : AvaloniaMainActivity
{
    /// <summary>The name under the icon, entry 234: the development build says it is one there too.</summary>
#if GROUPLAB_DEV
    internal const string Name = "GroupLab Dev";
#else
    internal const string Name = "GroupLab";
#endif

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

        // Entry 236: a new activity that starts before the old one has gone, as a task cleared and restarted, or a change of window mode on
        // a tablet does, found the one Shell still inside the old activity's view, and the application stopped with "already has a visual
        // parent". The Shell is let go of first, so it moves to the new activity with everything the person was doing.
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime { MainView: { } shell })
        {
            switch (shell.Parent)
            {
                case Avalonia.Controls.Presenters.ContentPresenter presenter:
                    presenter.Content = null;
                    break;
                case Avalonia.Controls.ContentControl holder:
                    holder.Content = null;
                    break;
            }
        }

        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Error report issue 6: every time Android ended GroupLab in the background, which it does whenever it wants the memory, the next start
    /// reported a close without shutting down. The run's marker is held only while the application is on screen, so what is reported is a
    /// close in front of the person.
    /// </summary>
    protected override void OnStart()
    {
        base.OnStart();
        if (GroupLab.App.Diagnostics.DiagnosticLog.Current is { } log)
        {
            GroupLab.App.Diagnostics.CrashReporter.ResumeRun(log);
        }
    }

    protected override void OnStop()
    {
        if (GroupLab.App.Diagnostics.DiagnosticLog.Current is { } log)
        {
            GroupLab.App.Diagnostics.CrashReporter.EndRun(log);
        }

        base.OnStop();
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
