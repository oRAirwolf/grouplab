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
    private TargetDefinition definition;
    private bool definitionFromCodes;
    private readonly AutoShutter auto = new();
    private bool taking;
    private bool manual;
    private int torchChoice;
    private bool torchOn;
    /// <summary>Entry 302: the torch's highest level, 1 where the phone offers only on and off, 0 with no torch; and the level it had at the start.</summary>
    private int torchMax;
    private int torchStartLevel;
    private volatile TorchGovernor torchAuto = new(0);
    private int lens;
    private long frames;
    private int? codesRead;
    private Instruction? lastSay;
    private long readySince;
    private readonly GuidanceSteadier steadier = new();

    /// <summary>Entry 291 section 3.3: the last frame judged, its size, its crop and the picture's scale to it, for the record kept at the press.</summary>
    private volatile string lastLive = "no live frame was judged";
    private ProcessCameraProvider provider;
    private ImageAnalysis analysis;
    private volatile bool stopped;
    private volatile bool capturing;
    private long pressedAt;
    private readonly global::Android.Media.MediaActionSound shutterSound = new();

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
        screen.ShowMode(manual);
        screen.ShowTorch(torchChoice);
        screen.ShowLens(Lenses[0]);
        screen.ShutterPressed += () => Take(manual ? "manual" : "guided, pressed");
        screen.ModeChosen += chosen =>
        {
            manual = chosen;
            auto.Reset();
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
            torchAuto = new TorchGovernor(torchMax);
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
        level = new Level((x, y, z) => screen.Post(() => screen.ShowLevel(x, y, z)));
    }

    /// <summary>A still saved: its path in the application's cache, and whether the torch was on.</summary>
    public event Action<string, bool> Taken;

    public void Start()
    {
        Active = this;
        stopped = false;
        if (sensors?.GetDefaultSensor(SensorType.Gravity) is { } gravity)
        {
            sensors.RegisterListener(level, gravity, SensorDelay.Ui);
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
            analysis.SetAnalyzer(analysisThread, this);
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
            torchAuto = new TorchGovernor(torchMax);
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

        steadier.Reset();
        auto.Reset();
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
            var frameClock = System.Diagnostics.Stopwatch.StartNew();
            var grey = Luminance(image);
            var metadata = new ImageMetadata("YUV", grey.Width, grey.Height, null, null, "camera", "analysis", 1, null, null);
            var backend = new OpenCvSharpBackend();
            frames++;
            FrameVerdict verdict;
            if (definition is null || (!definitionFromCodes && frames % 5 == 0))
            {
                var search = LiveSheet.Find(grey, PhoneAnalysis.Library(), backend);
                codesRead = search.CodesRead;
                if (search.Definition is not null && (definition is null || search.FromCodes))
                {
                    (definition, definitionFromCodes) = (search.Definition, search.FromCodes);
                    DiagnosticLog.Info("camera.sheet", ("from", search.FromCodes ? "codes" : "markers"), ("markers", search.MarkersFound));
                }

                verdict = definition is null
                    ? CaptureGuidance.Search(search, SheetOutline.Find(grey, out string? reason), reason)
                    : CaptureGuidance.JudgeFrame(grey, metadata, definition, backend, codesRead);
            }
            else
            {
                if (frames % 3 == 0)
                {
                    codesRead = backend.ReadCodes(grey, 1.0).Count;
                }

                verdict = CaptureGuidance.JudgeFrame(grey, metadata, definition, backend, codesRead);
            }

            // Entry 302, torch on Auto: it starts at the lowest level, steps up only while the paper is dim, and steps down or goes off on
            // glare, a hotspot or paper already bright, with a settling time between changes so it never flickers (TorchGovernor).
            if (torchChoice == 0 && torchAuto.Next(verdict.Quality, verdict.Evenness, clock.ElapsedMilliseconds) is { } change)
            {
                ContextCompat.GetMainExecutor(context).Execute(new Java.Lang.Runnable(() => SetTorchLevel(change.Level)));
                DiagnosticLog.Info("camera.torch", ("auto", change.Level == 0 ? "off" : "on"), ("level", change.Level), ("of", torchMax), ("reason", change.Reason),
                    ("paper", verdict.Quality?.PaperLevel), ("clipped", verdict.Quality?.ClippedShare), ("evenness", verdict.Evenness));
            }

            long now = clock.ElapsedMilliseconds;
            // Entry 281 section 1.4: the words held steady, with resolution judged at the size the picture is measured at.
            double scale = MeasuredScale(grey);
            verdict = steadier.Next(verdict, now, scale);
            lastLive = LiveRecord(verdict, grey, image, scale);
            if (verdict.Say != lastSay)
            {
                lastSay = verdict.Say;
                readySince = now;
                DiagnosticLog.Info("camera.say", ("say", verdict.Say.ToString()), ("ms", now), ("frameMs", frameClock.ElapsedMilliseconds),
                    ("markers", verdict.MarkersRead), ("codes", verdict.CodesRead), ("score", verdict.Quality?.Score), ("mode", manual ? "manual" : "guided"));
            }

            // Entry 273: on the printer check page the card is looked for too, and the shutter waits for it.
            bool? card = null;
            if (PrinterCheck.IsCheckPage(definition))
            {
                card = verdict.Mapping is { } mapping && verdict.PixelsPerMm is { } perMm && CardCheck.Measure(grey, mapping, definition!, perMm, 0) is not null;
                if (verdict.Say == Instruction.Ready)
                {
                    verdict = verdict with { Words = card == true ? "Card found. Hold still." : "Lay the card inside the outline, flat." };
                }
            }

            // Entry 311 section 1: taken once every frame for AutoShutter.SteadyMs has been judged ready on its own, the same on iOS.
            bool fire = auto.Next(steadier.Decided, card != false, now);
            int? forecast = verdict.Quality is { } quality ? PictureCheck.Forecast(quality) : null;
            float progress = manual ? 0 : fire ? 1 : (float)auto.Progress;
            screen.Post(() =>
            {
                screen.Show(verdict, forecast, torchOn, card);
                screen.Shutter.Progress = progress;
            });
            if (!manual && fire && !taking)
            {
                DiagnosticLog.Info("camera.auto", ("afterReadyMs", now - readySince), ("steadyMs", auto.ReadyMs), ("ms", now));
                Take("guided, by itself");
            }
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
        var crop = image.CropRect;
        var stillSize = still?.ResolutionInfo?.Resolution;
        var stillCrop = still?.ResolutionInfo?.CropRect;
        return string.Create(inv,
            $"say={verdict.Say} markers={verdict.MarkersRead} of {verdict.MarkersExpected} inFrame={verdict.MarkersInFrame} predicted={verdict.MarkersPredicted(scale)} codes={verdict.CodesRead} " +
            $"module={verdict.ModulePixels * scale:0.0}px printedRoom={verdict.PrintedRoom:0.000} shake={(verdict.Quality is { } q ? GuidanceSteadier.ShakePixels(q, scale) : null):0.0}px score={verdict.Quality?.Score} " +
            $"frame={frame.Width}x{frame.Height} frameCrop={crop?.Left},{crop?.Top},{crop?.Right},{crop?.Bottom} rotation={image.ImageInfo?.RotationDegrees} " +
            $"still={stillSize?.Width}x{stillSize?.Height} stillCrop={stillCrop?.Left},{stillCrop?.Top},{stillCrop?.Right},{stillCrop?.Bottom} scale={scale:0.00} lens={Lenses[lens]:0.0}");
    }

    public void Take(string why)
    {
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
            session.taking = false;
            session.capturing = false;
            bool torch = session.torchOn;
            DiagnosticLog.Info("camera.take", ("how", why), ("mode", session.manual ? "manual" : "guided"), ("torch", torch));
            // Entry 281 section 1.2: the torch goes off the moment the picture is taken, and the camera is let go before the result.
            ContextCompat.GetMainExecutor(session.context).Execute(new Java.Lang.Runnable(session.Stop));
            // Entry 291 section 7.5: GroupLab Dev keeps the picture, without its metadata, with what the live frame read before it.
            SittingRecord.Keep(path, $"taken {why}, torch {(torch ? "on" : "off")}{Environment.NewLine}{session.lastLive}{Environment.NewLine}");
            session.Taken?.Invoke(path, torch);
        }

        public void OnError(ImageCaptureException exception)
        {
            session.taking = false;
            session.capturing = false;
            DiagnosticLog.Info("camera.take", ("how", why), ("error", exception.Message));
        }
    }
}
