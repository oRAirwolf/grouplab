// The CameraX bindings mark nearly every Java reference as possibly null; this file talks to little else, so their warnings are off here.
#nullable disable warnings
using System.Diagnostics;
using System.Globalization;
using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;
using Java.Util.Concurrent;

namespace GroupLab.Android.Spike;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A2: the capture screen's camera, through CameraX. A preview on the camera's own surface, a still
/// capture at the largest size in maximum quality, and an analysis stream of smaller frames that the capture screen's judgement
/// (<see cref="CaptureGuidance"/>) runs on. The lens is chosen by zoom: CameraX moves between the Fold 7's ultrawide, wide and telephoto
/// cameras as the ratio passes 1 and 3. Every step is timed and logged under GroupLabSpike, so one sitting with the phone measures it.
/// </summary>
public sealed class CameraSession : Java.Lang.Object, ImageAnalysis.IAnalyzer
{
    private readonly Context context;
    private readonly ILifecycleOwner owner;
    private readonly PreviewView preview;
    private readonly IExecutorService analysisThread = Executors.NewSingleThreadExecutor()!;
    private readonly string targets;
    private ICamera? camera;
    private ImageCapture? still;
    private TargetDefinition? definition;
    private int readyInARow;
    private int frames;
    private readonly Stopwatch since = Stopwatch.StartNew();
    private bool taking;

    /// <summary>Each frame's verdict and timing, on the analysis thread.</summary>
    public event Action<FrameVerdict, string>? Judged;

    /// <summary>A still taken and analyzed: what it found, in one line.</summary>
    public event Action<string>? Taken;

    public CameraSession(Context context, ILifecycleOwner owner, PreviewView preview, string targets)
    {
        this.context = context;
        this.owner = owner;
        this.preview = preview;
        this.targets = targets;
    }

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
            SpikeView.Note("camera started: preview, a still at the largest size, and an analysis stream");
        }), ContextCompat.GetMainExecutor(context));
    }

    /// <summary>Item L2: the lens by zoom, 0.6 the ultrawide, 1 the wide, 3 the telephoto.</summary>
    public void Zoom(float ratio)
    {
        camera?.CameraControl.SetZoomRatio(ratio);
        SpikeView.Note(string.Create(CultureInfo.InvariantCulture, $"zoom {ratio:0.0}"));
    }

    /// <summary>Tap to focus and meter at a point of the preview, and hold it there until the next tap.</summary>
    public void FocusAt(float x, float y)
    {
        if (camera is null)
        {
            return;
        }

        var point = preview.MeteringPointFactory.CreatePoint(x, y);
        var action = new FocusMeteringAction.Builder(point).DisableAutoCancel().Build();
        camera.CameraControl.StartFocusAndMetering(action);
        SpikeView.Note(string.Create(CultureInfo.InvariantCulture, $"focus and exposure locked at {x:0}, {y:0}"));
    }

    /// <summary>The analysis stream: the frame's luminance, judged the way the desktop judges a photograph.</summary>
    public void Analyze(IImageProxy image)
    {
        try
        {
            var clock = Stopwatch.StartNew();
            var grey = Luminance(image);
            var metadata = new ImageMetadata("YUV", grey.Width, grey.Height, null, null, "camera", "analysis", 1, null, null);
            var backend = new OpenCvSharpBackend();
            if (definition is null)
            {
                var named = SheetIdentification.Identify(grey, SheetIdentification.Candidates([targets]), backend, new TraceRecorder());
                definition = named.Definition;
                if (definition is not null)
                {
                    SpikeView.Note($"sheet named from its codes in the stream: {definition.Name}");
                }
            }

            var verdict = definition is null
                ? CaptureGuidance.Judge(SheetOutline.Find(grey, out string? reason), reason, null)
                : CaptureGuidance.JudgeFrame(grey, metadata, definition, backend);
            frames++;
            string timing = string.Create(CultureInfo.InvariantCulture,
                $"{grey.Width} by {grey.Height}, judged in {clock.ElapsedMilliseconds} ms, {frames / Math.Max(0.001, since.Elapsed.TotalSeconds):0.0} frames a second");
            Judged?.Invoke(verdict, timing);

            // Item C1: the shutter fires by itself when every condition has held for three frames in a row.
            readyInARow = verdict.Say == Instruction.Ready ? readyInARow + 1 : 0;
            if (readyInARow >= 3 && !taking)
            {
                readyInARow = 0;
                Take("automatic");
            }
        }
        catch (Exception e)
        {
            SpikeView.Note($"frame: {e.GetType().Name}: {e.Message}");
        }
        finally
        {
            image.Close();
        }
    }

    /// <summary>The full resolution still, saved to the application's own cache, then analyzed at the phone's working size.</summary>
    public void Take(string why)
    {
        if (still is null || taking)
        {
            return;
        }

        taking = true;
        string path = Path.Combine(context.CacheDir!.AbsolutePath, $"still-{DateTime.Now:HHmmss}.jpg");
        var options = new ImageCapture.OutputFileOptions.Builder(new Java.IO.File(path)).Build();
        var clock = Stopwatch.StartNew();
        still.TakePicture(options, analysisThread, new Saved(this, path, clock, why));
    }

    private void Analyzed(string path, Stopwatch clock, string why)
    {
        try
        {
            long saved = clock.ElapsedMilliseconds;
            var (grey, metadata) = ImageLoader.Load(path, WorkingSize.PhoneMegapixels);
            var (value, _) = ImageLoader.LoadMaxChannel(path, WorkingSize.PhoneMegapixels);
            var full = ImageMetadataReader.Read(File.ReadAllBytes(path));
            string line;
            if (definition is null)
            {
                line = $"{why} still {full.Width} by {full.Height}, saved in {saved} ms; no sheet named yet, so not analyzed";
            }
            else
            {
                var result = AutomaticMarking.Run(grey, value, metadata, definition, new OpenCvSharpBackend());
                line = string.Create(CultureInfo.InvariantCulture,
                    $"{why} still {full.Width} by {full.Height} ({full.Width * (double)(full.Height ?? 0) / 1e6:0.0} MP), saved in {saved} ms, analyzed at {grey.Width} by {grey.Height} in {clock.ElapsedMilliseconds - saved} ms: {result.Detections.Count} holes{(result.Failure is null ? "" : $", {result.Failure}")}{(result.Capture is { } c ? $", {c.Clause}" : "")}");
            }

            Taken?.Invoke(line);
        }
        catch (Exception e)
        {
            Taken?.Invoke($"{why} still: {e.GetType().Name}: {e.Message}");
        }
        finally
        {
            taking = false;
        }
    }

    /// <summary>The Y plane, row by row, since a row can be longer than the image is wide.</summary>
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

    private sealed class Saved(CameraSession session, string path, Stopwatch clock, string why) : Java.Lang.Object, ImageCapture.IOnImageSavedCallback
    {
        public void OnImageSaved(ImageCapture.OutputFileResults output) => session.Analyzed(path, clock, why);

        public void OnError(ImageCaptureException exception)
        {
            session.taking = false;
            SpikeView.Note($"{why} still failed: {exception.Message}");
        }
    }
}
