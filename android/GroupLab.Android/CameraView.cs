// The CameraX bindings mark nearly every Java reference as possibly null; this file talks to little else, so their warnings are off here.
#nullable disable warnings
using System.Globalization;
using Android.Content;
using Android.Hardware;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia.Controls;
using Avalonia.Platform;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using Java.Util.Concurrent;

using GroupLab.Mobile;
#if GROUPLAB_DEV
using GroupLab.Mobile.Dev;
#endif

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 items A2 and A4, and entry 260: the capture screen. Everything on it is <see cref="CaptureScreen"/>, Android's
/// own views around CameraX's preview, because a native view hosted in an Avalonia screen covers whatever Avalonia draws in its place: on
/// 2026-09-28 the instruction, the Take button and Back were all there and none could be seen. In Guided mode the shutter fires by itself once
/// the frames have been judged ready for <see cref="AutoShutter.SteadyMs"/> (docs/MOBILE-CAPTURE.md item C1) and can be pressed sooner; in Manual it fires
/// only when pressed, with the guidance still shown as a hint. The mode and the torch are remembered. The still goes to the application's
/// cache and is handed on to be analyzed and checked; the cache copy is deleted once it has been.
/// </summary>
public sealed class CameraView : UserControl
{
    /// <param name="taken">The still's path and whether the torch was on when it was taken.</param>
    /// <param name="back">Leave the camera.</param>
    /// <param name="pick">Leave the camera for a photograph already on the phone.</param>
    /// <param name="result">Back to the last result, where there is one (entry 281 section 1.3).</param>
    public CameraView(Action<string, bool> taken, Action back, Action pick, Action? result = null)
    {
        Content = new CameraHost(taken, back, pick, result);
    }

    /// <summary>The native screen inside the Avalonia one, filling it.</summary>
    private sealed class CameraHost(Action<string, bool> taken, Action back, Action pick, Action? result) : NativeControlHost
    {
        private CameraSession session;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var activity = MainActivity.Current!;
            var preview = new PreviewView(activity);
            var screen = new CaptureScreen(activity, preview);
            session = new CameraSession(activity, activity, preview, screen);
            session.Taken += (path, torch) => activity.RunOnUiThread(() => taken(path, torch));
            screen.BackPressed += () => back();
            screen.PickerPressed += () => pick();
            screen.ShowResultButton(result is not null);
            screen.ResultPressed += () => result?.Invoke();
            preview.Touch += (_, e) =>
            {
                if (e.Event?.Action == global::Android.Views.MotionEventActions.Up)
                {
                    session.FocusAt(e.Event.GetX(), e.Event.GetY());
                }
            };
            session.Start();
            return new global::Avalonia.Android.AndroidViewControlHandle(screen);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            session?.Stop();
            base.DestroyNativeControlCore(control);
        }
    }
}

/// <summary>The camera itself: preview, a still at the largest size in maximum quality, and the analysis stream the guidance runs on.</summary>
internal sealed class CameraSession : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    /// <summary>
    /// The analysis stream's size. CameraX's default, 640 by 480, left a sheet's square codes at about a pixel a module, so the Fold 7 never
    /// knew the sheet; at 1920 by 1440 a Letter sheet across half the frame gives its markers about 20 pixels a side.
    /// </summary>
    private static readonly global::Android.Util.Size AnalysisSize = new(1920, 1440);

    private static readonly float[] Lenses = [1f, 3f, 0.6f];

    private readonly Context context;
    private readonly ILifecycleOwner owner;
    private readonly PreviewView preview;
    private readonly CaptureScreen screen;
    private readonly IExecutorService analysisThread = Executors.NewSingleThreadExecutor()!;
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly SensorManager sensors;
    private readonly Level level;
    private ICamera camera;
    private ImageCapture still;

    /// <summary>Entry 315 section 3: the sheet found, the words, the torch on Auto and the shutter's timing, the code iOS runs too.</summary>
    private readonly CameraJudge judge = new();
    private bool taking;
    private bool manual;
    private int torchChoice;
    private bool torchOn;
    /// <summary>Entry 302: the torch's highest level, 1 where the phone offers only on and off, 0 with no torch; and the level it had at the start.</summary>
    private int torchMax;
    private int torchStartLevel;
    private int lens;

    /// <summary>Entry 315 section 4: the overlay, where Show diagnostics on the camera is on; the frame rate, the tilt and the torch it shows.</summary>
    private readonly bool showDiagnostics;
    private readonly FrameRate frameRate = new();
    private long overlayAt = -DiagnosticsOverlay.EveryMs;
    private double? tilt;
    private int torchLevel;
    private bool? levelWasReady;
    private long levelLogged = -LevelLogMs;

    /// <summary>How often the level is written to the log while it holds, in milliseconds; a change between flat and not is written at once.</summary>
    private const long LevelLogMs = 2000;

    /// <summary>Entry 291 section 3.3: the last frame judged, its size, its crop and the picture's scale to it, for the record kept at the press.</summary>
    private volatile string lastLive = "no live frame was judged";
    private ProcessCameraProvider provider;
    private ImageAnalysis analysis;
    private volatile bool stopped;
    private volatile bool capturing;
    private long pressedAt;
    private readonly global::Android.Media.MediaActionSound shutterSound = new();
#if GROUPLAB_DEV

    /// <summary>Entry 315 section 3, GroupLab Dev only: the clip played in place of the camera, and the recorder keeping the last seconds.</summary>
    private ClipPlayer replay;
    private ClipRecorder recorder;
    private (double X, double Y, double Z)? gravity;

    private bool Replaying => replay is not null;
#else
    private static bool Replaying => false;
#endif

    /// <summary>
    /// Entry 283: the capture mode. Maximum quality runs the phone's multi-frame processing after the press, which is where Alan's lag was
    /// suspected; minimum latency takes the frame at the press. GroupLab Dev can be started with the quality mode instead, so the two can be
    /// timed against each other on the same phone (scripts/shutter-timing.py).
    /// </summary>
    public static bool QualityMode { get; set; }

    /// <summary>GroupLab Dev's timing run (entry 283): press the shutter this long after the camera starts; 0 leaves it to a finger.</summary>
    public static double TestPressAfterSeconds { get; set; }

    /// <summary>The camera showing now, which the activity pauses and resumes with the application (entry 281 section 1.5).</summary>
    public static CameraSession Active { get; private set; }

    public CameraSession(Context context, ILifecycleOwner owner, PreviewView preview, CaptureScreen screen)
    {
        this.context = context;
        this.owner = owner;
        this.preview = preview;
        this.screen = screen;
        manual = Phone.Settings.LoadCaptureManual();
        torchChoice = Phone.Settings.LoadCaptureTorch();
        showDiagnostics = DiagnosticsOverlay.On;
        screen.ShowMode(manual);
        screen.ShowTorch(torchChoice);
        screen.ShowLens(Lenses[0]);
        screen.ShutterPressed += () => Take(manual ? "manual" : "guided, pressed");
        screen.ModeChosen += chosen =>
        {
            manual = chosen;
            judge.ResetShutter();
            screen.Shutter.Progress = 0;
            screen.ShowMode(manual);
            Phone.Settings.SaveCaptureManual(manual);
            DiagnosticLog.Info("camera.mode", ("mode", manual ? "manual" : "guided"));
        };
        screen.TorchPressed += () =>
        {
            torchChoice = (torchChoice + 1) % 3;
            Phone.Settings.SaveCaptureTorch(torchChoice);
            screen.ShowTorch(torchChoice);
            judge.ResetTorch(torchMax);
            SetTorchLevel(torchChoice == 1 ? torchStartLevel : 0);
            DiagnosticLog.Info("camera.torch", ("choice", torchChoice switch { 1 => "on", 2 => "off", _ => "auto" }));
        };
        screen.LensPressed += () =>
        {
            lens = (lens + 1) % Lenses.Length;
            Zoom(Lenses[lens]);
            screen.ShowLens(Lenses[lens]);
        };
        sensors = (SensorManager)context.GetSystemService(Context.SensorService);
        level = new Level(Levelled);
    }

    /// <summary>The level's reading, from the gravity sensor or from a replayed clip.</summary>
    private void Levelled(double x, double y, double z)
    {
        tilt = BubbleLevel.Tilt(x, y, z);
#if GROUPLAB_DEV
        gravity = (x, y, z);
#endif
        LogLevel(x, y, z);
        screen.Post(() => screen.ShowLevel(x, y, z));
    }

    /// <summary>A still saved: its path in the application's cache, and whether the torch was on.</summary>
    public event Action<string, bool> Taken;

    public void Start()
    {
        Active = this;
        stopped = false;
#if GROUPLAB_DEV
        // Entry 315 section 3: a clip asked for is played in place of the analysis stream and the gravity sensor; the preview and the torch
        // are still the camera's.
        replay = CameraReplay.NewPlayer(Replayed);
        recorder = CameraReplay.NewRecorder("android");
        replay?.Start();
#endif
        if (!Replaying && sensors?.GetDefaultSensor(SensorType.Gravity) is { } sensor)
        {
            sensors.RegisterListener(level, sensor, SensorDelay.Ui);
        }

        // Entry 281 section 1.6: what the shooter framed is what is saved. Preview, analysis and picture are all 4:3, the sensor's own shape,
        // and the preview shows the whole frame (fit, not fill), so nothing the picture holds is cut from the preview or the other way round;
        // the viewport, where the preview has one, crops all three alike.
        preview.SetScaleType(PreviewView.ScaleType.FitCenter);
        var future = ProcessCameraProvider.GetInstance(context);
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            if (stopped)
            {
                return;
            }

            provider = (ProcessCameraProvider)future.Get()!;
            var fourByThree = new ResolutionSelector.Builder().SetAspectRatioStrategy(AspectRatioStrategy.Ratio43FallbackAutoStrategy).Build();
            var show = new AndroidX.Camera.Core.Preview.Builder().SetResolutionSelector(fourByThree).Build();
            show.SetSurfaceProvider(ContextCompat.GetMainExecutor(context), preview.SurfaceProvider);
            still = new ImageCapture.Builder()
                .SetCaptureMode(QualityMode ? ImageCapture.CaptureModeMaximizeQuality : ImageCapture.CaptureModeMinimizeLatency)
                .SetJpegQuality(95)
                .SetResolutionSelector(fourByThree)
                .Build();
            var size = new ResolutionSelector.Builder()
                .SetAspectRatioStrategy(AspectRatioStrategy.Ratio43FallbackAutoStrategy)
                .SetResolutionStrategy(new ResolutionStrategy(AnalysisSize, ResolutionStrategy.FallbackRuleClosestHigherThenLower))
                .Build();
            analysis = new ImageAnalysis.Builder()
                .SetResolutionSelector(size)
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888)
                .Build();
            if (!Replaying)
            {
                analysis.SetAnalyzer(analysisThread, this);
            }

            provider.UnbindAll();
            var group = new UseCaseGroup.Builder().AddUseCase(show).AddUseCase(still).AddUseCase(analysis);
            if (preview.ViewPort is { } viewPort)
            {
                group.SetViewPort(viewPort);
            }

            camera = provider.BindToLifecycle(owner, CameraSelector.DefaultBackCamera, group.Build());
            torchOn = false;
            bool strength = camera.CameraInfo.HasFlashUnit && camera.CameraInfo.IsTorchStrengthSupported;
            torchMax = !camera.CameraInfo.HasFlashUnit ? 0 : strength ? camera.CameraInfo.MaxTorchStrengthLevel : 1;
            torchStartLevel = strength && camera.CameraInfo.TorchStrengthLevel.Value is Java.Lang.Integer start ? start.IntValue() : torchMax;
            judge.ResetTorch(torchMax);
            SetTorchLevel(torchChoice == 1 ? torchStartLevel : 0);
            if (TestPressAfterSeconds > 0)
            {
                double after = TestPressAfterSeconds;
                TestPressAfterSeconds = 0;
                screen.PostDelayed(() => Take("test press"), (long)(after * 1000));
            }

            DiagnosticLog.Info("camera.start", ("mode", manual ? "manual" : "guided"), ("torch", torchChoice), ("torchLevels", torchMax), ("torchDefault", torchStartLevel),
                ("analysis", analysis.ResolutionInfo?.Resolution?.ToString()), ("still", still.ResolutionInfo?.Resolution?.ToString()));
        }), ContextCompat.GetMainExecutor(context));
    }

    /// <summary>
    /// The camera let go: the torch off, the analysis stopped and every use case unbound. Entry 281 sections 1.2 and 1.5: the torch stayed
    /// on after the picture, frames were still analyzed after the camera had closed, and after the application was minimized and opened
    /// again the camera never started.
    /// </summary>
    public void Stop()
    {
        stopped = true;
        sensors?.UnregisterListener(level);
#if GROUPLAB_DEV
        replay?.Stop();
        if (recorder is { } keeping)
        {
            recorder = null;
            _ = Task.Run(() => keeping.Write(DateTime.Now));
        }
#endif
        SetTorch(false);
        analysis?.ClearAnalyzer();
        provider?.UnbindAll();
        camera = null;
        if (Active == this)
        {
            Active = null;
        }
    }

    /// <summary>The application went to the background: let the camera go, and remember to take it again.</summary>
    public void Pause()
    {
        Stop();
        Active = this;
        DiagnosticLog.Info("camera.pause");
    }

    /// <summary>The application came back: take the camera again, as it was.</summary>
    public void Resume()
    {
        if (!stopped)
        {
            return;
        }

        judge.Reset();
        Start();
        DiagnosticLog.Info("camera.resume");
    }

    /// <summary>The lens by zoom: 0.6 the ultrawide, 1 the wide, 3 the telephoto, where the phone has them.</summary>
    public void Zoom(float ratio)
    {
        camera?.CameraControl.SetZoomRatio(ratio);
        DiagnosticLog.Info("camera.zoom", ("ratio", ratio.ToString("0.0", CultureInfo.InvariantCulture)));
    }

    /// <summary>Tap to focus and meter at a point of the preview, held there until the next tap.</summary>
    public void FocusAt(float x, float y)
    {
        if (camera is null)
        {
            return;
        }

        var point = preview.MeteringPointFactory.CreatePoint(x, y);
        camera.CameraControl.StartFocusAndMetering(new FocusMeteringAction.Builder(point).DisableAutoCancel().Build());
    }

    private void SetTorch(bool on)
    {
        if (camera?.CameraInfo.HasFlashUnit != true || on == torchOn)
        {
            return;
        }

        torchOn = on;
        camera.CameraControl.EnableTorch(on);
    }

    /// <summary>
    /// Entry 302: the torch at <paramref name="level"/>, 0 for off. Where the phone offers strength levels (Android 15 and later, where the
    /// maker supports it; the Fold 7 offers 1 to 5) the level is set while the camera runs; elsewhere any level above 0 is simply on.
    /// </summary>
    private void SetTorchLevel(int level)
    {
        if (camera?.CameraInfo.HasFlashUnit != true)
        {
            return;
        }

        if (level > 0 && torchMax > 1)
        {
            camera.CameraControl.SetTorchStrengthLevel(Math.Clamp(level, 1, torchMax));
        }

        torchLevel = Math.Clamp(level, 0, Math.Max(1, torchMax));
        SetTorch(level > 0);
    }

    /// <summary>
    /// Each frame of the stream, judged the way the desktop judges a photograph, on its luminance. Until the sheet is known, it is looked for
    /// by its codes and its markers (<see cref="LiveSheet"/>); once known from its markers only, its codes are still tried now and then.
    /// </summary>
    public void Analyze(IImageProxy image)
    {
        // Entry 283: from the press to the saved picture the live analysis stands aside, so it does not compete for the camera or the processor.
        if (stopped || capturing)
        {
            image.Close();
            return;
        }

        try
        {
            var grey = Luminance(image);
            var step = Judge(grey, () => clock.ElapsedMilliseconds, MeasuredScale(grey), image);
#if GROUPLAB_DEV
            recorder?.Offer(grey, step.NowMs, gravity, torchOn ? torchLevel : 0, torchMax,
                still?.ResolutionInfo?.Resolution is { } size ? (size.Width, size.Height) : (0, 0));
#endif
        }
        catch (Exception e)
        {
            DiagnosticLog.Info("camera.frame", ("error", e.GetType().Name));
        }
        finally
        {
            image.Close();
        }
    }

    /// <summary>
    /// One frame, from the camera or from a replayed clip, judged by the code both phones share (<see cref="CameraJudge"/>), then shown, the
    /// torch set where Auto changed it, and the picture taken where Guided mode says so.
    /// </summary>
    private CameraStep Judge(GrayImage grey, Func<long> now, double scale, IImageProxy image)
    {
        var step = judge.Next(grey, now, scale, torchChoice == 0, manual);
        var verdict = step.Verdict;
        if (step.Torch is { } change)
        {
            ContextCompat.GetMainExecutor(context).Execute(new Java.Lang.Runnable(() => SetTorchLevel(change.Level)));
        }

        lastLive = LiveRecord(verdict, grey, image, scale);
        float progress = (float)step.Progress;
        double? fps = frameRate.Next(step.NowMs);
        string overlay = null;
        if (showDiagnostics && step.NowMs - overlayAt >= DiagnosticsOverlay.EveryMs)
        {
            overlayAt = step.NowMs;
            overlay = DiagnosticsOverlay.Camera(fps, step.FrameMs, verdict, tilt, torchOn ? torchLevel : 0, torchMax);
        }

        screen.Post(() =>
        {
            screen.Show(verdict, step.Forecast, torchOn, step.Card);
            screen.Shutter.Progress = progress;
            if (overlay is not null)
            {
                screen.ShowDiagnostics(overlay);
            }
        });
        if (step.Fire && !taking)
        {
            DiagnosticLog.Info("camera.auto", ("afterReadyMs", step.SaidForMs), ("steadyMs", step.SteadyMs), ("ms", step.NowMs));
            Take("guided, by itself");
        }

        return step;
    }
#if GROUPLAB_DEV

    /// <summary>Entry 315 section 3: a replayed frame, with the level's reading recorded beside it, through the same steps as a live one.</summary>
    private void Replayed(int index, GrayImage grey, ClipFrame frame, long ms)
    {
        if (stopped || capturing)
        {
            return;
        }

        try
        {
            if (frame.Gravity is var (x, y, z))
            {
                Levelled(x, y, z);
            }

            CameraReplay.Saw(index, Judge(grey, () => ms, replay.Clip.MeasuredScale(grey), null));
        }
        catch (Exception e)
        {
            DiagnosticLog.Info("camera.frame", ("error", e.GetType().Name), ("replay", index));
        }
    }

    /// <summary>The shutter while a clip plays: the frame showing is the picture, handed on as a taken one is.</summary>
    private void TakeReplayed(string why)
    {
        if (taking || replay.CurrentFile is not { } frame)
        {
            return;
        }

        taking = true;
        capturing = true;
        screen.Post(() =>
        {
            screen.Flash();
            screen.Say("Taking the picture…");
        });
        string path = Path.Combine(context.CacheDir!.AbsolutePath, $"still-{DateTime.Now:HHmmss}{Path.GetExtension(frame)}");
        File.Copy(frame, path, overwrite: true);
        DiagnosticLog.Info("camera.shutter", ("step", "replayed"), ("frame", replay.Current), ("guided", !manual));
        CameraReplay.Took(why);
        Delivered(path, why);
    }
#endif

    /// <summary>
    /// Entry 315 section 4, as entry 311 section 2 does on iOS: the gravity reading and the tilt the level computes (camera.level), when it
    /// turns green or stops being green and every two seconds besides, with the memory in use and the phone's heat.
    /// </summary>
    private void LogLevel(double x, double y, double z)
    {
        bool ready = BubbleLevel.Ready(x, y, z);
        long now = clock.ElapsedMilliseconds;
        if (ready == levelWasReady && now - levelLogged < LevelLogMs)
        {
            return;
        }

        levelWasReady = ready;
        levelLogged = now;
        DiagnosticLog.Info("camera.level", [("gravity", string.Create(CultureInfo.InvariantCulture, $"{x:0.000},{y:0.000},{z:0.000}")),
            ("tilt", Math.Round(BubbleLevel.Tilt(x, y, z), 1)), ("green", ready), ("ms", now), .. DeviceHealth.Fields()]);
    }

    /// <summary>
    /// The picture's pixels, as the phone measures it, per pixel of this analysis frame: the still's size, cut to the phone's working copy
    /// (<see cref="WorkingSize.PhoneMegapixels"/>), over the frame's.
    /// </summary>
    private double MeasuredScale(GrayImage frame)
    {
        if (still?.ResolutionInfo?.Resolution is not { } size)
        {
            return 1;
        }

        double working = Math.Max(size.Width, size.Height) * Math.Min(1, WorkingSize.Scale(size.Width, size.Height, WorkingSize.PhoneMegapixels));
        return working / Math.Max(frame.Width, frame.Height);
    }

    /// <summary>
    /// Entry 291 section 3.3: what the last live frame read, with its size and field of view, and the markers it foretells the picture will
    /// read; set beside the picture's own count (phone.markers) in the log, and kept with the picture in GroupLab Dev.
    /// </summary>
    private string LiveRecord(FrameVerdict verdict, GrayImage frame, IImageProxy image, double scale)
    {
        var inv = CultureInfo.InvariantCulture;
        var crop = image?.CropRect;
        var stillSize = still?.ResolutionInfo?.Resolution;
        var stillCrop = still?.ResolutionInfo?.CropRect;
        return string.Create(inv,
            $"say={verdict.Say} markers={verdict.MarkersRead} of {verdict.MarkersExpected} inFrame={verdict.MarkersInFrame} predicted={verdict.MarkersPredicted(scale)} codes={verdict.CodesRead} " +
            $"module={verdict.ModulePixels * scale:0.0}px printedRoom={verdict.PrintedRoom:0.000} shake={(verdict.Quality is { } q ? GuidanceSteadier.ShakePixels(q, scale) : null):0.0}px score={verdict.Quality?.Score} " +
            $"frame={frame.Width}x{frame.Height} frameCrop={crop?.Left},{crop?.Top},{crop?.Right},{crop?.Bottom} rotation={image?.ImageInfo?.RotationDegrees} " +
            $"still={stillSize?.Width}x{stillSize?.Height} stillCrop={stillCrop?.Left},{stillCrop?.Top},{stillCrop?.Right},{stillCrop?.Bottom} scale={scale:0.00} lens={Lenses[lens]:0.0}");
    }

    public void Take(string why)
    {
#if GROUPLAB_DEV
        if (Replaying)
        {
            TakeReplayed(why);
            return;
        }
#endif
        if (still is null || taking)
        {
            return;
        }

        taking = true;
        capturing = true;
        pressedAt = clock.ElapsedMilliseconds;
        // Entry 283: the shutter answers at once, a sound and a flash, and the reading follows with its progress shown.
        screen.Post(() =>
        {
            shutterSound.Play(global::Android.Media.MediaActionSoundType.ShutterClick);
            screen.Flash();
            screen.Say("Taking the picture…");
        });
        DiagnosticLog.Info("camera.shutter", ("step", "press"), ("ms", 0), ("mode", QualityMode ? "quality" : "latency"), ("torch", torchOn), ("guided", !manual));
        DiagnosticLog.Info("camera.live", ("frame", lastLive));
        string path = Path.Combine(context.CacheDir!.AbsolutePath, $"still-{DateTime.Now:HHmmss}.jpg");
        var options = new ImageCapture.OutputFileOptions.Builder(new Java.IO.File(path)).Build();
        still.TakePicture(options, analysisThread, new Saved(this, path, why));
        DiagnosticLog.Info("camera.shutter", ("step", "requested"), ("ms", clock.ElapsedMilliseconds - pressedAt));
    }

    /// <summary>A picture saved, or a replayed frame taken as one: the camera let go and the picture handed on.</summary>
    private void Delivered(string path, string why)
    {
        taking = false;
        capturing = false;
        bool torch = torchOn;
        DiagnosticLog.Info("camera.take", ("how", why), ("mode", manual ? "manual" : "guided"), ("torch", torch));
        // Entry 281 section 1.2: the torch goes off the moment the picture is taken, and the camera is let go before the result.
        ContextCompat.GetMainExecutor(context).Execute(new Java.Lang.Runnable(Stop));
        // Entry 291 section 7.5: GroupLab Dev keeps the picture, without its metadata, with what the live frame read before it.
        SittingRecord.Keep(path, $"taken {why}, torch {(torch ? "on" : "off")}{Environment.NewLine}{lastLive}{Environment.NewLine}");
        Taken?.Invoke(path, torch);
    }

    private static GrayImage Luminance(IImageProxy image)
    {
        var plane = image.GetPlanes()[0];
        var buffer = plane.Buffer;
        int width = image.Width, height = image.Height, stride = plane.RowStride;
        byte[] all = new byte[buffer.Remaining()];
        buffer.Get(all);
        byte[] pixels = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            Array.Copy(all, y * stride, pixels, y * width, width);
        }

        return new GrayImage(width, height, pixels);
    }

    private sealed class Saved(CameraSession session, string path, string why) : Java.Lang.Object, ImageCapture.IOnImageSavedCallback
    {
        public void OnCaptureStarted()
        {
            DiagnosticLog.Info("camera.shutter", ("step", "exposed"), ("ms", session.clock.ElapsedMilliseconds - session.pressedAt));
        }

        public void OnImageSaved(ImageCapture.OutputFileResults output)
        {
            DiagnosticLog.Info("camera.shutter", ("step", "saved"), ("ms", session.clock.ElapsedMilliseconds - session.pressedAt));
            session.Delivered(path, why);
        }

        public void OnError(ImageCaptureException exception)
        {
            session.taking = false;
            session.capturing = false;
            DiagnosticLog.Info("camera.take", ("how", why), ("error", exception.Message));
        }
    }
}
