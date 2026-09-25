// The CameraX bindings mark nearly every Java reference as possibly null; this file talks to little else, so their warnings are off here.
#nullable disable warnings
using System.Globalization;
using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;
using Java.Util.Concurrent;
using Button = Avalonia.Controls.Button;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 items A2 and A4: the capture screen, moved into the application from the spike. CameraX's preview fills
/// it; over it, docs/MOBILE-CAPTURE.md's one instruction at a time; beneath it, the lens by zoom, Take, and the way back. The shutter fires
/// by itself after three ready frames in a row (item C1), and a tap on the preview focuses and meters there. The still goes to the
/// application's cache and is handed on to be analyzed; the cache copy is deleted once it has been.
/// </summary>
public sealed class CameraView : UserControl
{
    private readonly TextBlock say = new() { FontSize = 24, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Text = "Starting the camera…" };
    private CameraSession? session;

    public CameraView(Action<string> taken, Action back)
    {
        var host = new CameraHost(s =>
        {
            session = s;
            s.Judged += verdict => Dispatcher.UIThread.Post(() => say.Text = verdict.Words);
            s.Taken += path => Dispatcher.UIThread.Post(() => taken(path));
        });

        Button Control(string words, Action act)
        {
            var b = new Button { Content = words, MinHeight = Screens.Touch, MinWidth = 64, Margin = new Thickness(4) };
            b.Click += (_, _) => act();
            return b;
        }

        var controls = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                Control("0.6x", () => session?.Zoom(0.6f)),
                Control("1x", () => session?.Zoom(1f)),
                Control("3x", () => session?.Zoom(3f)),
                Control("Take", () => session?.Take("by hand")),
                Control("Back", back),
            },
        };
        var top = new Border { Padding = new Thickness(16), Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), Child = say };
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetRowSpan(host, 3);
        Grid.SetRow(controls, 2);
        grid.Children.Add(host);
        grid.Children.Add(top);
        grid.Children.Add(controls);
        Content = grid;
    }

    /// <summary>CameraX's PreviewView inside the Avalonia screen; a tap on it focuses and meters there.</summary>
    private sealed class CameraHost(Action<CameraSession> started) : NativeControlHost
    {
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var activity = MainActivity.Current!;
            var preview = new PreviewView(activity);
            var session = new CameraSession(activity, activity, preview);
            preview.Touch += (_, e) =>
            {
                if (e.Event?.Action == global::Android.Views.MotionEventActions.Up)
                {
                    session.FocusAt(e.Event.GetX(), e.Event.GetY());
                }
            };
            started(session);
            session.Start();
            return new global::Avalonia.Android.AndroidViewControlHandle(preview);
        }
    }
}

/// <summary>The camera itself: preview, a still at the largest size in maximum quality, and the analysis stream the guidance runs on.</summary>
internal sealed class CameraSession(Context context, ILifecycleOwner owner, PreviewView preview) : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly IExecutorService analysisThread = Executors.NewSingleThreadExecutor()!;
    private ICamera? camera;
    private ImageCapture? still;
    private TargetDefinition? definition;
    private int readyInARow;
    private bool taking;

    public event Action<FrameVerdict>? Judged;

    /// <summary>A still saved: its path in the application's cache.</summary>
    public event Action<string>? Taken;

    public void Start()
    {
        var future = ProcessCameraProvider.GetInstance(context);
        future.AddListener(new Java.Lang.Runnable(() =>
        {
            var provider = (ProcessCameraProvider)future.Get()!;
            var show = new Preview.Builder().Build();
            show.SetSurfaceProvider(ContextCompat.GetMainExecutor(context), preview.SurfaceProvider);
            still = new ImageCapture.Builder().SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality).Build();
            var analysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .SetOutputImageFormat(ImageAnalysis.OutputImageFormatYuv420888)
                .Build();
            analysis.SetAnalyzer(analysisThread, this);
            provider.UnbindAll();
            camera = provider.BindToLifecycle(owner, CameraSelector.DefaultBackCamera, show, still, analysis);
            DiagnosticLog.Info("camera.start");
        }), ContextCompat.GetMainExecutor(context));
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

    /// <summary>Each frame of the stream, judged the way the desktop judges a photograph, on its luminance.</summary>
    public void Analyze(IImageProxy image)
    {
        try
        {
            var grey = Luminance(image);
            var metadata = new ImageMetadata("YUV", grey.Width, grey.Height, null, null, "camera", "analysis", 1, null, null);
            var backend = new OpenCvSharpBackend();
            definition ??= SheetIdentification.Identify(grey, PhoneAnalysis.Library(), backend, new TraceRecorder()).Definition;
            var verdict = definition is null
                ? CaptureGuidance.Judge(SheetOutline.Find(grey, out string? reason), reason, null)
                : CaptureGuidance.JudgeFrame(grey, metadata, definition, backend);
            Judged?.Invoke(verdict);
            readyInARow = verdict.Say == Instruction.Ready ? readyInARow + 1 : 0;
            if (readyInARow >= 3 && !taking)
            {
                readyInARow = 0;
                Take("automatic");
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
            DiagnosticLog.Info("camera.take", ("how", why));
            session.Taken?.Invoke(path);
        }

        public void OnError(ImageCaptureException exception)
        {
            session.taking = false;
            DiagnosticLog.Info("camera.take", ("how", why), ("error", exception.Message));
        }
    }
}
