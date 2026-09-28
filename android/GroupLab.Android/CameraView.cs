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

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 items A2 and A4, and entry 260: the capture screen. Everything on it is <see cref="CaptureScreen"/>, Android's
/// own views around CameraX's preview, because a native view hosted in an Avalonia screen covers whatever Avalonia draws in its place: on
/// 2026-09-28 the instruction, the Take button and Back were all there and none could be seen. In Guided mode the shutter fires by itself after
/// <see cref="CameraSession.ReadyFrames"/> ready frames in a row (docs/MOBILE-CAPTURE.md item C1) and can be pressed sooner; in Manual it fires
/// only when pressed, with the guidance still shown as a hint. The mode and the torch are remembered. The still goes to the application's
/// cache and is handed on to be analyzed and checked; the cache copy is deleted once it has been.
/// </summary>
public sealed class CameraView : UserControl
{
    /// <param name="taken">The still's path and whether the torch was on when it was taken.</param>
    /// <param name="back">Leave the camera.</param>
    /// <param name="pick">Leave the camera for a photograph already on the phone.</param>
    public CameraView(Action<string, bool> taken, Action back, Action pick)
    {
        Content = new CameraHost(taken, back, pick);
    }

    /// <summary>The native screen inside the Avalonia one, filling it.</summary>
    private sealed class CameraHost(Action<string, bool> taken, Action back, Action pick) : NativeControlHost
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
    /// <summary>Ready frames in a row before Guided mode fires the shutter.</summary>
    public const int ReadyFrames = 3;

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
    private int readyInARow;
    private bool taking;
    private bool manual;
    private int torchChoice;
    private bool torchOn;
    private int lens;
    private long frames;
    private int? codesRead;
    private Instruction? lastSay;
    private long readySince;

    public CameraSession(Context context, ILifecycleOwner owner, PreviewView preview, CaptureScreen screen)
    {
        this.context = context;
        this.owner = owner;
        this.preview = preview;
        this.screen = screen;
        manual = App.Settings.LoadCaptureManual();
        torchChoice = App.Settings.LoadCaptureTorch();
        screen.ShowMode(manual);
        screen.ShowTorch(torchChoice);
        screen.ShowLens(Lenses[0]);
        screen.ShutterPressed += () => Take(manual ? "manual" : "guided, pressed");
        screen.ModeChosen += chosen =>
        {
            manual = chosen;
            readyInARow = 0;
            screen.Shutter.Progress = 0;
            screen.ShowMode(manual);
            App.Settings.SaveCaptureManual(manual);
            DiagnosticLog.Info("camera.mode", ("mode", manual ? "manual" : "guided"));
        };
        screen.TorchPressed += () =>
        {
            torchChoice = (torchChoice + 1) % 3;
            App.Settings.SaveCaptureTorch(torchChoice);
            screen.ShowTorch(torchChoice);
            SetTorch(torchChoice == 1);
            DiagnosticLog.Info("camera.torch", ("choice", torchChoice switch { 1 => "on", 2 => "off", _ => "auto" }));
        };
        screen.LensPressed += () =>
        {
            lens = (lens + 1) % Lenses.Length;
            Zoom(Lenses[lens]);
            screen.ShowLens(Lenses[lens]);
        };
        sensors = (SensorManager)context.GetSystemService(Context.SensorService);
        level = new Level(degrees => screen.Post(() => screen.ShowLevel(degrees)));
    }

    /// <summary>A still saved: its path in the application's cache, and whether the torch was on.</summary>
    public event Action<string, bool> Taken;

    public void Start()
    {
        if (sensors?.GetDefaultSensor(SensorType.Gravity) is { } gravity)
        {
            sensors.RegisterListener(level, gravity, SensorDelay.Ui);
        }

        var future = ProcessCameraProvider.GetInstance(context);
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            var provider = (ProcessCameraProvider)future.Get()!;
            var show = new AndroidX.Camera.Core.Preview.Builder().Build();
            show.SetSurfaceProvider(ContextCompat.GetMainExecutor(context), preview.SurfaceProvider);
            still = new ImageCapture.Builder().SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality).Build();
            var size = new ResolutionSelector.Builder()
                .SetResolutionStrategy(new ResolutionStrategy(AnalysisSize, ResolutionStrategy.FallbackRuleClosestHigherThenLower))
                .Build();
            var analysis = new ImageAnalysis.Builder()
                .SetResolutionSelector(size)
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888)
                .Build();
            analysis.SetAnalyzer(analysisThread, this);
            provider.UnbindAll();
            camera = provider.BindToLifecycle(owner, CameraSelector.DefaultBackCamera, show, still, analysis);
            SetTorch(torchChoice == 1);
            DiagnosticLog.Info("camera.start", ("mode", manual ? "manual" : "guided"), ("torch", torchChoice));
        }), ContextCompat.GetMainExecutor(context));
    }

    public void Stop()
    {
        sensors?.UnregisterListener(level);
        SetTorch(false);
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
    /// Each frame of the stream, judged the way the desktop judges a photograph, on its luminance. Until the sheet is known, it is looked for
    /// by its codes and its markers (<see cref="LiveSheet"/>); once known from its markers only, its codes are still tried now and then.
    /// </summary>
    public void Analyze(IImageProxy image)
    {
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

            // Torch on Auto: on when the paper is dim or the light uneven, and left on for the rest of the session so it does not flicker.
            if (torchChoice == 0 && !torchOn && (verdict.Quality is { ExposurePart: < CaptureGuidance.Holds, ClippedShare: <= CaptureQualities.FineClipped } || verdict.Evenness is < PictureCheck.EvenLight))
            {
                ContextCompat.GetMainExecutor(context).Execute(new Java.Lang.Runnable(() => SetTorch(true)));
                DiagnosticLog.Info("camera.torch", ("auto", "on"), ("evenness", verdict.Evenness));
            }

            long now = clock.ElapsedMilliseconds;
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

            readyInARow = verdict.Say == Instruction.Ready && card != false ? readyInARow + 1 : 0;
            int? forecast = verdict.Quality is { } quality ? PictureCheck.Forecast(quality) : null;
            screen.Post(() =>
            {
                screen.Show(verdict, forecast, torchOn, card);
                screen.Shutter.Progress = manual ? 0 : (float)readyInARow / ReadyFrames;
            });
            if (!manual && readyInARow >= ReadyFrames && !taking)
            {
                readyInARow = 0;
                DiagnosticLog.Info("camera.auto", ("afterReadyMs", now - readySince), ("ms", now));
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

    public void Take(string why)
    {
        if (still is null || taking)
        {
            return;
        }

        taking = true;
        screen.Post(() => screen.Say("Taking the picture…"));
        string path = Path.Combine(context.CacheDir!.AbsolutePath, $"still-{DateTime.Now:HHmmss}.jpg");
        var options = new ImageCapture.OutputFileOptions.Builder(new Java.IO.File(path)).Build();
        still.TakePicture(options, analysisThread, new Saved(this, path, why));
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
        public void OnImageSaved(ImageCapture.OutputFileResults output)
        {
            session.taking = false;
            DiagnosticLog.Info("camera.take", ("how", why), ("mode", session.manual ? "manual" : "guided"), ("torch", session.torchOn));
            session.Taken?.Invoke(path, session.torchOn);
        }

        public void OnError(ImageCaptureException exception)
        {
            session.taking = false;
            DiagnosticLog.Info("camera.take", ("how", why), ("error", exception.Message));
        }
    }
}
