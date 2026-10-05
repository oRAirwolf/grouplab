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
// version of dropping a file on the desktop's window. Entry 292 section 1.3: from any app, several at once as a set, and from an app's
// Edit with list too, so GroupLab appears wherever a photo editor does; it reads the picture and never writes it back.
[IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
[IntentFilter([Intent.ActionSendMultiple], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
[IntentFilter([Intent.ActionEdit], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
// Entry 307: a GroupLab data file opened or shared from Files, a mail or a chat, offered for import. Its type is usually unknown, so the
// file itself is checked before anything is read from it.
[IntentFilter([Intent.ActionView, Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/octet-stream")]
[IntentFilter([Intent.ActionView, Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "application/json")]
#if GROUPLAB_DEV
// Entry 318 section 3: Firebase Test Lab starts a "game loop" test with this action; GroupLab Dev answers it with a scenario.
[IntentFilter([MainActivity.TestLoopAction], Categories = [Intent.CategoryDefault], DataMimeType = "application/javascript")]
#endif
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

    private const int BluetoothRequest = 373;

    /// <summary>
    /// Request 73: whether GroupLab may connect to a paired printer, asked the first time Print is pressed and never at start. Before Android 12
    /// the permission is granted with the application and this is always true.
    /// </summary>
    internal static bool BluetoothAllowed()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return true;
        }

        if (Current is not { } activity)
        {
            return false;
        }

        if (AndroidX.Core.Content.ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.BluetoothConnect) == Permission.Granted)
        {
            return true;
        }

        AndroidX.Core.App.ActivityCompat.RequestPermissions(activity, [global::Android.Manifest.Permission.BluetoothConnect], BluetoothRequest);
        return false;
    }

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
        // parent". The Shell is let go of first, so it moves to the new activity with everything the person was doing. Crash reports 9 and
        // 10: the old activity then runs the layout it still had queued for the Shell's pieces, so none of it is left to run after the move,
        // as a second activity started over the idle screen found.
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime { MainView: { } shell })
        {
            Shell.LetGo(shell);
        }

#if GROUPLAB_DEV
        // Entry 315 section 2: a scenario named by the extra, or waiting in files/scenario, read before the application starts.
        bool scenario = GroupLab.Mobile.Dev.Scenario.Prepare(FilesDir!.AbsolutePath, TestLoop(Intent) ?? Intent?.GetStringExtra(TestScenarioExtra));

        // Entry 353: where GroupLab's view sits on the screen, in pixels, so a script tapping with adb's input taps where a control is.
        GroupLab.Mobile.Dev.Scenario.ScreenPlace = () =>
        {
            int[] at = new int[2];
            FindViewById(global::Android.Resource.Id.Content)?.GetLocationOnScreen(at);
            return (at[0], at[1], true);
        };
#endif
        base.OnCreate(savedInstanceState);
        Shared(Intent);
#if GROUPLAB_DEV
        if (scenario)
        {
            GroupLab.Mobile.Dev.Scenario.StartIfPrepared();
        }

        TestPicture(Intent);
        TestShotsToZero(Intent);
        TestCamera(Intent);
        TestIdle(Intent);
#endif
    }

    /// <summary>
    /// Entry 258: a picture sent or opened from another application. Android hands a content address, not a path. Entry 292 sections 1.3 and
    /// 1.4: several at once are a set, and each is fetched by the Capture screen with a progress line once it is there, never on this thread,
    /// so a picture kept only in the cloud cannot stop the application while it downloads. Nothing about where it came from is kept.
    /// </summary>
    private void Shared(Intent? intent)
    {
        if (intent is not null && DataFile(intent))
        {
            return;
        }

        if (intent is null || !PhotoIntake.IsIncoming(intent.Action, intent.Type))
        {
            return;
        }

        var uris = PhotoPickers.Shared(intent);
        if (uris.Count == 0)
        {
            return;
        }

        GroupLab.App.Diagnostics.DiagnosticLog.Info("phone.shared", ("action", intent.Action switch
        {
            Intent.ActionSend => "send",
            Intent.ActionSendMultiple => "send several",
            Intent.ActionEdit => "edit",
            _ => "view",
        }), ("pictures", uris.Count));
        Task.Run(() => PhotoPickers.Handles(this, uris)).ContinueWith(
            handed =>
            {
                if (handed.IsCompletedSuccessfully)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        Avalonia.Threading.DispatcherTimer.RunOnce(() => CapturePage.SharedPicture?.Invoke(handed.Result), TimeSpan.FromSeconds(1)));
                }
            },
            TaskScheduler.Default);
    }

    /// <summary>
    /// Entry 307: a file that is not a picture, opened or shared with GroupLab. It is copied into the cache and kept only when it begins as a
    /// GroupLab data file does; then Settings opens with what importing it would do, and nothing is written until Import is pressed.
    /// </summary>
    private bool DataFile(Intent intent)
    {
        if (intent.Action is not (Intent.ActionView or Intent.ActionSend) || intent.Type is null || intent.Type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var uri = PhotoPickers.Shared(intent).FirstOrDefault();
        if (uri is null)
        {
            return true;
        }

        string copy = System.IO.Path.Combine(CacheDir!.AbsolutePath, "incoming" + GroupLab.Core.Records.DataExport.Extension);
        try
        {
            using (var from = ContentResolver!.OpenInputStream(uri))
            using (var to = System.IO.File.Create(copy))
            {
                from?.CopyTo(to);
            }

            using var head = new System.IO.StreamReader(copy);
            var start = new char[64];
            int read = head.Read(start, 0, start.Length);
            if (!new string(start, 0, read).Replace(" ", "", StringComparison.Ordinal).Contains("\"format\":\"grouplab-data\"", StringComparison.Ordinal))
            {
                head.Dispose();
                System.IO.File.Delete(copy);
                GroupLab.App.Diagnostics.DiagnosticLog.Info("data.incoming", ("grouplab", false));
                return true;
            }
        }
        catch (Exception e) when (e is System.IO.IOException or Java.Lang.Exception or UnauthorizedAccessException)
        {
            GroupLab.App.Diagnostics.DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "data.incoming", e);
            return true;
        }

        GroupLab.App.Diagnostics.DiagnosticLog.Info("data.incoming", ("grouplab", true));
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            DataSection.Waiting = copy;
            Shell.Current?.Show(Shell.Place.Settings);
        }, TimeSpan.FromSeconds(1)));
        return true;
    }

    /// <summary>The pickers waiting for their answer, by request (entry 292).</summary>
    private readonly Dictionary<int, TaskCompletionSource<Intent?>> waiting = [];

    /// <summary>Starts a picker and hears what it chose: its answer, or null where the person came back without choosing.</summary>
    internal Task<Intent?> ForResult(Intent intent, int request)
    {
        var done = new TaskCompletionSource<Intent?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (waiting.Remove(request, out var earlier))
        {
            earlier.TrySetResult(null);
        }

        waiting[request] = done;
        try
        {
#pragma warning disable CS0618, CA1422 // The activity result API wants a launcher registered at creation; one call from the screens is this.
            StartActivityForResult(intent, request);
#pragma warning restore CS0618, CA1422
        }
        catch
        {
            waiting.Remove(request);
            throw;
        }

        return done.Task;
    }

#pragma warning disable CS0618, CA1422 // Avalonia's own storage provider hears its answers here too, through the base.
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
#pragma warning restore CS0618, CA1422
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (waiting.Remove(requestCode, out var done))
        {
            done.TrySetResult(resultCode == Result.Ok ? data : null);
        }
    }

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

    /// <summary>
    /// The extra that runs a scenario (entry 315 section 2): a file name in files/scenario, such as <c>read-sample.json</c>. With no extra,
    /// a waiting files/scenario/scenario.json runs at the start. The older extras stay as shortcuts to one step each.
    /// </summary>
    internal const string TestScenarioExtra = "org.grouplab.test.scenario";

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Shared(intent);
        if (intent?.GetStringExtra(TestScenarioExtra) is { } named && GroupLab.Mobile.Dev.Scenario.Prepare(FilesDir!.AbsolutePath, named))
        {
            GroupLab.Mobile.Dev.Scenario.StartIfPrepared();
        }

        TestPicture(intent);
        TestShotsToZero(intent);
        TestCamera(intent);
        TestIdle(intent);
    }

    /// <summary>The action Firebase Test Lab starts a game loop test with (entry 318 section 3).</summary>
    internal const string TestLoopAction = "com.google.intent.action.TEST_LOOP";

    /// <summary>
    /// GroupLab Dev only, entry 318 section 3: a Test Lab game loop. Test Lab pushes the scenarios and the picture they read into this
    /// application's own folder on shared storage (<c>Android/data/org.grouplab.app.dev/files/testlab</c>, which it reads with no permission),
    /// and starts GroupLab Dev with this action and a loop number. Scenario N is <c>scenario-N.json</c> there. Everything in that folder is
    /// copied into the scenario folder, the results are copied back to <c>testlab/results</c> when the run ends, for Test Lab to pull, and
    /// GroupLab Dev then closes, which is how a game loop says it has finished. Returns the scenario's name, or null for any other start.
    /// </summary>
    private string? TestLoop(Intent? intent)
    {
        if (intent?.Action != TestLoopAction || GetExternalFilesDir(null)?.AbsolutePath is not { } shared)
        {
            return null;
        }

        int loop = intent.GetIntExtra("scenario", 1);
        string from = Path.Combine(shared, "testlab"), into = Path.Combine(FilesDir!.AbsolutePath, "scenario");
        try
        {
            Directory.CreateDirectory(into);
            foreach (string file in Directory.Exists(from) ? Directory.GetFiles(from) : [])
            {
                File.Copy(file, Path.Combine(into, Path.GetFileName(file)), overwrite: true);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            GroupLab.App.Diagnostics.DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "testlab.copy", e);
        }

        GroupLab.App.Diagnostics.DiagnosticLog.Info("testlab.loop", ("scenario", loop));
        GroupLab.Mobile.Dev.Scenario.Finished += results =>
        {
            try
            {
                string back = Path.Combine(from, "results");
                Directory.CreateDirectory(back);
                foreach (string file in Directory.GetFiles(results, "*", SearchOption.AllDirectories))
                {
                    string to = Path.Combine(back, Path.GetRelativePath(results, file));
                    Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                    File.Copy(file, to, overwrite: true);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                GroupLab.App.Diagnostics.DiagnosticLog.Exception(GroupLab.App.Diagnostics.LogLevel.Warn, "testlab.results", e);
            }

            RunOnUiThread(FinishAndRemoveTask);
        };
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"scenario-{loop}.json");
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
