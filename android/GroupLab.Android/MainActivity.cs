using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

using GroupLab.Mobile;

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

    // Entry 246: a start aimed at GroupLab while it runs reaches this one through OnNewIntent rather than stacking a second activity on it,
    // which moved the one application view between them mid-analysis on the tablet ("InvalidateArrange on wrong LayoutManager").
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.FullUser,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize
        | ConfigChanges.UiMode | ConfigChanges.Density | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]

// Entry 258: a picture shared into GroupLab from another application, or opened with it, is read as a chosen photograph: the phone's
// version of dropping a file on the desktop's window.
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
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
        Shared(Intent);
#if GROUPLAB_DEV
        TestPicture(Intent);
        TestShotsToZero(Intent);
        TestCamera(Intent);
        TestIdle(Intent);
#endif
    }

    /// <summary>
    /// Entry 258: a picture sent or opened from another application. Android hands a content address, not a path, so the picture is copied
    /// into the cache and read as a chosen photograph once the Capture screen is there. Nothing about where it came from is kept.
    /// </summary>
    private void Shared(Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionSend or Intent.ActionView) || intent.Type?.StartsWith("image/", StringComparison.Ordinal) != true)
        {
            return;
        }

        var uri = intent.Action == Intent.ActionSend
            ? (OperatingSystem.IsAndroidVersionAtLeast(33) ? intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(global::Android.Net.Uri))) as global::Android.Net.Uri : null)
                ?? GetStream(intent)
            : intent.Data;
        if (uri is null || ContentResolver?.OpenInputStream(uri) is not { } from)
        {
            return;
        }

        string copy = Path.Combine(CacheDir!.AbsolutePath, "shared" + (intent.Type == "image/png" ? ".png" : ".jpg"));
        using (from)
        using (var to = File.Create(copy))
        {
            from.CopyTo(to);
        }

        GroupLab.App.Diagnostics.DiagnosticLog.Info("phone.shared", ("action", intent.Action == Intent.ActionSend ? "send" : "view"));
        Avalonia.Threading.DispatcherTimer.RunOnce(() => CapturePage.SharedPicture?.Invoke(copy), TimeSpan.FromSeconds(1));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "The typed call is used from Android 13; this is the older form for Android 10 to 12.")]
    private static global::Android.Net.Uri? GetStream(Intent intent) =>
#pragma warning disable CA1422
        intent.GetParcelableExtra(Intent.ExtraStream) as global::Android.Net.Uri;
#pragma warning restore CA1422

#if !GROUPLAB_DEV
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Shared(intent);
    }
#endif

#if GROUPLAB_DEV
    /// <summary>The extra a test names a picture with, a file name in the application's own <c>test</c> folder (entry 246).</summary>
    internal const string TestPictureExtra = "org.grouplab.test.picture";

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Shared(intent);
        TestPicture(intent);
        TestShotsToZero(intent);
        TestCamera(intent);
        TestIdle(intent);
    }

    /// <summary>The extra that shows GroupLab Dev's black idle screen (entry 268), with any value.</summary>
    internal const string TestIdleExtra = "org.grouplab.test.idle";

    /// <summary>GroupLab Dev only: the black idle screen over everything, for an OLED device left on overnight.</summary>
    private void TestIdle(Intent? intent)
    {
        if (intent?.GetStringExtra(TestIdleExtra) is null)
        {
            return;
        }

        StartActivity(new Intent(this, typeof(IdleActivity)));
    }

    /// <summary>The extra that asks GroupLab Dev to open the capture screen's camera (entry 260), with any value; "manual" opens it in Manual.</summary>
    internal const string TestCameraExtra = "org.grouplab.test.camera";

    /// <summary>The extra that picks the capture mode for a timing run (entry 283): "quality", or anything else for minimum latency.</summary>
    internal const string TestCaptureModeExtra = "org.grouplab.test.capturemode";

    /// <summary>The extra that presses the shutter this many seconds after the camera starts, through the same path as a finger (entry 283).</summary>
    internal const string TestPressExtra = "org.grouplab.test.press";

    /// <summary>
    /// GroupLab Dev only: opens the camera as Take a picture does, so the device check (scripts/device-capture-check.py) can read the capture
    /// screen with nobody holding the phone.
    /// </summary>
    private static void TestCamera(Intent? intent)
    {
        if (intent?.GetStringExtra(TestCameraExtra) is not { } mode)
        {
            return;
        }

        // Entry 283: "quality" as the second word takes the picture in the maximum quality mode, to time it against minimum latency.
        CameraSession.QualityMode = intent.GetStringExtra(TestCaptureModeExtra) == "quality";
        CameraSession.TestPressAfterSeconds = double.TryParse(intent.GetStringExtra(TestPressExtra), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double after) ? after : 0;
        Avalonia.Threading.DispatcherTimer.RunOnce(() => CapturePage.TestCamera?.Invoke(mode == "manual"), TimeSpan.FromSeconds(2));
    }

    /// <summary>The extra that asks GroupLab Dev to time "Shots Needed to Zero" (entry 252 section 4), with any value.</summary>
    internal const string TestShotsToZeroExtra = "org.grouplab.test.shotstozero";

    /// <summary>
    /// GroupLab Dev only: works out Shots Needed to Zero for 5, 10, 25 and 100 shot groups, at sigma of 0.3, 1 and 3 clicks, off the
    /// interface thread, and logs each time and the most memory held, so a device sitting can measure it without the desktop's screen.
    /// </summary>
    private static void TestShotsToZero(Intent? intent)
    {
        if (intent?.GetStringExtra(TestShotsToZeroExtra) is null)
        {
            return;
        }

        Task.Run(() =>
        {
            GroupLab.Core.Statistics.ShotsToZero.Work(1.0, 8, false, 1);
            foreach (int shots in new[] { 5, 10, 25, 100 })
            {
                foreach (double sigma in new[] { 0.3, 1.0, 3.0 })
                {
                    GC.Collect();
                    long before = GC.GetTotalMemory(true);
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    GroupLab.Core.Statistics.ShotsToZero.Work(sigma, (2 * shots) - 2, false, 41);
                    clock.Stop();
                    GroupLab.App.Diagnostics.DiagnosticLog.Info("dev.shotstozero", ("shots", (object?)shots), ("sigma", sigma), ("ms", Math.Round(clock.Elapsed.TotalMilliseconds)),
                        ("kb", Math.Max(0, GC.GetTotalMemory(false) - before) / 1024), ("cores", System.Environment.ProcessorCount));
                }
            }
        });
    }

    /// <summary>GroupLab Dev only: the named picture is read as a chosen photograph once the Capture screen is there.</summary>
    private static void TestPicture(Intent? intent)
    {
        if (intent?.GetStringExtra(TestPictureExtra) is not { Length: > 0 } name || name.Contains('/') || name.Contains('\\'))
        {
            return;
        }

        string file = Path.Combine(global::Android.App.Application.Context.FilesDir!.AbsolutePath, "test", name);
        Avalonia.Threading.DispatcherTimer.RunOnce(() => CapturePage.TestPicture?.Invoke(file), TimeSpan.FromSeconds(2));
    }
#endif

    /// <summary>
    /// Error report issue 6: every time Android ended GroupLab in the background, which it does whenever it wants the memory, the next start
    /// reported a close without shutting down. The run's marker is held only while the application is on screen, so what is reported is a
    /// close in front of the person.
    /// </summary>
    protected override void OnStart()
    {
        base.OnStart();
        WorkInProgress.OnScreen = true;
        if (GroupLab.App.Diagnostics.DiagnosticLog.Current is { } log)
        {
            GroupLab.App.Diagnostics.CrashReporter.ResumeRun(log);
        }
    }

    protected override void OnStop()
    {
        // Entry 288: leaving is when an automatic update may install, since nothing then closes under the person.
        WorkInProgress.OnScreen = false;
#if GROUPLAB_UPDATER
        Updates.SelfUpdate.Left();
#endif

        if (GroupLab.App.Diagnostics.DiagnosticLog.Current is { } log)
        {
            GroupLab.App.Diagnostics.CrashReporter.EndRun(log);
        }

        base.OnStop();
    }

    /// <summary>
    /// Entry 281 section 1.5: after GroupLab was minimized and opened again, the camera did not start. The camera is let go when the
    /// application leaves the screen, the torch with it, and taken again when it comes back.
    /// </summary>
    protected override void OnPause()
    {
        CameraSession.Active?.Pause();
        base.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();
        CameraSession.Active?.Resume();

        // Entry 282 section 3: after a return the Retake screen's buttons were blank and the bar's names gone, the icons half drawn. The
        // whole screen is laid out and drawn again, every text with it, and not only the camera taken back.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime { MainView: { } view })
            {
                foreach (var visual in Avalonia.VisualTree.VisualExtensions.GetSelfAndVisualDescendants(view))
                {
                    (visual as Avalonia.Layout.Layoutable)?.InvalidateMeasure();
                    visual.InvalidateVisual();
                }

                GroupLab.App.Diagnostics.DiagnosticLog.Info("app.resume.redraw");
            }
        });
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
