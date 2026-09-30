// The AVFoundation bindings mark nearly every Objective-C reference as possibly null; this file talks to little else, so their warnings are
// off here, as they are in the Android camera for CameraX's.
#nullable disable warnings
using System.Diagnostics;
using System.Runtime.InteropServices;
using AVFoundation;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreMotion;
using CoreVideo;
using Foundation;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Mobile;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the camera itself on iPhone and iPad, the Android CameraSession's twin in AVFoundation
/// (android/GroupLab.Android/CameraView.cs). The back wide camera in a 4:3 mode chosen by <see cref="PhoneCamera.ChooseMode"/>, whose
/// stream is as near 1920 by 1440 as the camera offers; its preview on the screen, whole (fit, not fill); a photo output taking the
/// largest 4:3 picture that mode gives, as a JPEG; and the video stream judged frame by frame on its luminance exactly as Android judges
/// it, with the same Core rules (<see cref="LiveSheet"/>, <see cref="CaptureGuidance"/>, <see cref="GuidanceSteadier"/>). The level comes
/// from Core Motion's gravity. The torch goes off the moment the picture is taken, and the camera is let go when the application goes to
/// the background and taken again when it comes back.
/// </summary>
internal sealed class CameraSession : AVCaptureVideoDataOutputSampleBufferDelegate
{
    private readonly AVCaptureDevice device;
    private readonly AVCaptureSession session;
    private readonly CaptureScreen screen;
    private readonly DispatchQueue sessionQueue = new("org.grouplab.camera.session");
    private readonly DispatchQueue analysisQueue = new("org.grouplab.camera.analysis");
    private readonly CMMotionManager motion = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly GuidanceSteadier steadier = new();
    private AVCapturePhotoOutput still;
    private AVCaptureVideoDataOutput analysis;
    private StillSaved saving;
    private bool configured;
    private (int Width, int Height) picture;
    private NSObject toBackground;
    private NSObject toForeground;
    private TargetDefinition definition;
    private bool definitionFromCodes;
    private readonly AutoShutter auto = new();
    private bool taking;
    private bool manual;
    private int torchChoice;
    private bool torchOn;

    /// <summary>
    /// Entry 302 on iOS: the torch's levels. An iPhone or iPad sets its torch anywhere from a little above 0 to 1, so the torch on Auto
    /// moves in fifths of full strength, as a phone with five strength levels does on Android; On is full strength.
    /// </summary>
    internal const int TorchSteps = 5;

    private int torchLevel;
    private volatile TorchGovernor torchAuto = new(0);
    private long frames;
    private int? codesRead;
    private Instruction? lastSay;
    private long readySince;
    private volatile bool stopped = true;
    private volatile bool capturing;
    private long pressedAt;
    private ScreenTurn turn;

    public CameraSession(AVCaptureDevice device, AVCaptureSession session, CaptureScreen screen)
    {
        this.device = device;
        this.session = session;
        this.screen = screen;
        manual = Phone.Settings.LoadCaptureManual();
        torchChoice = Phone.Settings.LoadCaptureTorch();
        screen.ShowMode(manual);
        screen.ShowTorch(torchChoice);
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
            int level = torchChoice == 1 ? TorchSteps : 0;
            sessionQueue.DispatchAsync(() =>
            {
                torchAuto = new TorchGovernor(TorchMax());
                SetTorch(level);
            });
            DiagnosticLog.Info("camera.torch", ("choice", torchChoice switch { 1 => "on", 2 => "off", _ => "auto" }));
        };
        screen.Tapped += FocusAt;
        screen.LaidOut += Orient;
    }

    /// <summary>
    /// Entry 283's capture mode: speed first, the frame at the press, as Android's minimum latency; the quality mode runs the device's
    /// multi-frame processing after the press, so the two can be timed against each other.
    /// </summary>
    public static bool QualityMode { get; set; }

    /// <summary>The camera showing now, which is let go in the background and taken again in the foreground (entry 281 section 1.5).</summary>
    public static CameraSession Active { get; private set; }

    /// <summary>A still saved: its path in the application's cache, and whether the torch was on.</summary>
    public event Action<string, bool> Taken;

    public void Start()
    {
        Active = this;
        stopped = false;
        Watch();
        StartLevel();
        Orient();
        sessionQueue.DispatchAsync(() =>
        {
            if (stopped)
            {
                return;
            }

            if (!configured)
            {
                configured = Configure();
            }

            if (!configured)
            {
                screen.BeginInvokeOnMainThread(() => screen.Say("The camera could not be started. Press Back, then choose a photograph."));
                return;
            }

            session.StartRunning();
            torchOn = false;
            torchLevel = 0;
            torchAuto = new TorchGovernor(TorchMax());
            SetTorch(torchChoice == 1 ? TorchSteps : 0);
            DiagnosticLog.Info("camera.start", ("mode", manual ? "manual" : "guided"), ("torch", torchChoice), ("device", device.LocalizedName),
                ("picture", $"{picture.Width}x{picture.Height}"), ("running", session.Running));
        });
    }

    /// <summary>
    /// The camera let go: the torch off, the analysis stopped and the session stopped. Entry 281 sections 1.2 and 1.5, as on Android: the
    /// torch stayed on after the picture, frames were still analyzed after the camera had closed, and after the application was minimized
    /// and opened again the camera never started.
    /// </summary>
    public void Stop()
    {
        stopped = true;
        motion.StopDeviceMotionUpdates();
        sessionQueue.DispatchAsync(() =>
        {
            SetTorch(0);
            if (session.Running)
            {
                session.StopRunning();
            }
        });
        if (Active == this)
        {
            Active = null;
        }
    }

    /// <summary>The camera screen gone: the camera let go for good, and the application's comings and goings no longer heard.</summary>
    public void Close()
    {
        Stop();
        toBackground?.Dispose();
        toForeground?.Dispose();
        toBackground = toForeground = null;
        sessionQueue.DispatchAsync(() => analysis?.SetSampleBufferDelegate(null, null));
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

    /// <summary>The application's own notices of going to the background and coming back, heard while this camera screen exists.</summary>
    private void Watch()
    {
        toBackground ??= UIApplication.Notifications.ObserveDidEnterBackground((_, _) =>
        {
            if (Active == this && !stopped)
            {
                Pause();
            }
        });
        toForeground ??= UIApplication.Notifications.ObserveWillEnterForeground((_, _) =>
        {
            if (Active == this)
            {
                Resume();
            }
        });
    }

    /// <summary>
    /// The session put together once: the camera, the photo output and the analysis stream, and the camera's mode chosen so all three are
    /// 4:3 (entry 281 section 1.6, what the shooter framed is what is saved).
    /// </summary>
    private bool Configure()
    {
        session.BeginConfiguration();
        try
        {
            var input = AVCaptureDeviceInput.FromDevice(device, out NSError error);
            if (input is null || !session.CanAddInput(input))
            {
                DiagnosticLog.Info("camera.start", ("error", error?.LocalizedDescription ?? "the camera cannot be added"));
                return false;
            }

            session.AddInput(input);
            still = new AVCapturePhotoOutput();
            if (!session.CanAddOutput(still))
            {
                DiagnosticLog.Info("camera.start", ("error", "the photo output cannot be added"));
                return false;
            }

            session.AddOutput(still);

            // Luminance in full range, as Android's YUV stream gives it, in the first plane of each frame.
            analysis = new AVCaptureVideoDataOutput
            {
                AlwaysDiscardsLateVideoFrames = true,
                UncompressedVideoSetting = new AVVideoSettingsUncompressed { PixelFormatType = CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange },
            };
            analysis.SetSampleBufferDelegate(this, analysisQueue);
            if (!session.CanAddOutput(analysis))
            {
                DiagnosticLog.Info("camera.start", ("error", "the analysis stream cannot be added"));
                return false;
            }

            session.AddOutput(analysis);

            var formats = device.Formats ?? [];
            int chosen = PhoneCamera.ChooseMode(formats.Select(Mode).ToArray());
            if (device.LockForConfiguration(out error))
            {
                try
                {
                    if (chosen >= 0)
                    {
                        device.ActiveFormat = formats[chosen];
                    }

                    if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
                    {
                        device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                    }
                }
                finally
                {
                    device.UnlockForConfiguration();
                }
            }

            if (chosen < 0 && session.CanSetSessionPreset(AVCaptureSession.PresetPhoto))
            {
                session.SessionPreset = AVCaptureSession.PresetPhoto;
            }

            var mode = chosen >= 0 ? Mode(formats[chosen]) : default;
            DiagnosticLog.Info("camera.mode", ("modes", formats.Length), ("chosen", chosen), ("stream", $"{mode.StreamWidth}x{mode.StreamHeight}"),
                ("largestPicture", $"{mode.PictureWidth}x{mode.PictureHeight}"), ("fullRange", mode.FullRange));
        }
        finally
        {
            session.CommitConfiguration();
        }

        // The largest picture the chosen mode gives, asked for with the quality the capture mode may use.
        if (device.ActiveFormat?.SupportedMaxPhotoDimensions is { Length: > 0 } sizes)
        {
            var largest = sizes.MaxBy(d => (long)d.Width * d.Height);
            still.MaxPhotoDimensions = largest;
            picture = (largest.Width, largest.Height);
        }

        still.MaxPhotoQualityPrioritization = AVCapturePhotoQualityPrioritization.Quality;
        return true;
    }

    /// <summary>A camera mode as the choice sees it: its stream's size, its largest picture and whether its luminance is full range.</summary>
    private static CameraMode Mode(AVCaptureDeviceFormat format)
    {
        var description = format.FormatDescription;
        var size = description is CMVideoFormatDescription video ? video.Dimensions : default;
        var largest = format.SupportedMaxPhotoDimensions is { Length: > 0 } sizes ? sizes.MaxBy(d => (long)d.Width * d.Height) : default;
        bool fullRange = description?.MediaSubType == (uint)CVPixelFormatType.CV420YpCbCr8BiPlanarFullRange;
        return new CameraMode(size.Width, size.Height, largest.Width, largest.Height, fullRange);
    }

    /// <summary>The level from Core Motion's gravity, turned into the screen's axes, thirty times a second.</summary>
    private void StartLevel()
    {
        if (!motion.DeviceMotionAvailable || motion.DeviceMotionActive)
        {
            return;
        }

        motion.DeviceMotionUpdateInterval = 1.0 / 30;
        motion.StartDeviceMotionUpdates(NSOperationQueue.MainQueue, (data, _) =>
        {
            if (data is null)
            {
                return;
            }

            var g = data.Gravity;
            var (x, y, z) = PhoneCamera.LevelFromGravity(g.X, g.Y, g.Z, turn);
            screen.ShowLevel(x, y, z);
            LogLevel(g.X, g.Y, g.Z, x, y, z);
        });
    }

    private bool? levelWasReady;
    private long levelLogged = -LevelLogMs;

    /// <summary>How often the level is written to the log while it holds, in milliseconds; a change between flat and not is written at once.</summary>
    private const long LevelLogMs = 2000;

    /// <summary>
    /// Entry 311 section 2: Core Motion's gravity as it came, the screen's turn, the reading in the screen's axes and the tilt the level
    /// computes (camera.level), when it turns green or stops being green and every two seconds besides, so a sitting's log shows whether the
    /// axes follow the iPad's orientation and how far from the tolerance a steady hand is.
    /// </summary>
    private void LogLevel(double gx, double gy, double gz, double x, double y, double z)
    {
        bool ready = BubbleLevel.Ready(x, y, z);
        long now = clock.ElapsedMilliseconds;
        if (ready == levelWasReady && now - levelLogged < LevelLogMs)
        {
            return;
        }

        levelWasReady = ready;
        levelLogged = now;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        DiagnosticLog.Info("camera.level", ("gravity", string.Create(inv, $"{gx:0.000},{gy:0.000},{gz:0.000}")), ("turn", turn),
            ("screen", string.Create(inv, $"{x:0.000},{y:0.000},{z:0.000}")), ("tilt", Math.Round(BubbleLevel.Tilt(x, y, z), 1)), ("green", ready), ("ms", now));
    }

    /// <summary>The preview stood the way the screen is; on the interface thread, after each layout.</summary>
    private void Orient()
    {
        turn = screen.Turn;
        var angle = (NFloat)PhoneCamera.RotationDegrees(turn);
        if (screen.Preview.Connection is { } connection && connection.IsVideoRotationAngleSupported(angle))
        {
            connection.VideoRotationAngle = angle;
        }
    }

    /// <summary>Tap to focus and meter at a point of the picture, held there until the next tap.</summary>
    private void FocusAt(CGPoint point)
    {
        sessionQueue.DispatchAsync(() =>
        {
            if (!device.LockForConfiguration(out NSError _))
            {
                return;
            }

            try
            {
                if (device.FocusPointOfInterestSupported && device.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
                {
                    device.FocusPointOfInterest = point;
                    device.FocusMode = AVCaptureFocusMode.AutoFocus;
                }

                if (device.ExposurePointOfInterestSupported && device.IsExposureModeSupported(AVCaptureExposureMode.AutoExpose))
                {
                    device.ExposurePointOfInterest = point;
                    device.ExposureMode = AVCaptureExposureMode.AutoExpose;
                }
            }
            finally
            {
                device.UnlockForConfiguration();
            }
        });
    }

    /// <summary>The highest torch level this camera offers: <see cref="TorchSteps"/>, or 0 where it has no torch.</summary>
    private int TorchMax() => device is { HasTorch: true } && device.IsTorchModeSupported(AVCaptureTorchMode.On) ? TorchSteps : 0;

    /// <summary>
    /// The torch at <paramref name="level"/> fifths of full strength, 0 for off, and never above what iOS allows while the device is warm;
    /// on the session's queue, since the device is locked to change it.
    /// </summary>
    private void SetTorch(int level)
    {
        level = Math.Clamp(level, 0, TorchSteps);
        bool on = level > 0;
        var mode = on ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
        if (!device.HasTorch || level == torchLevel || !device.IsTorchModeSupported(mode))
        {
            return;
        }

        if (!device.LockForConfiguration(out NSError error))
        {
            DiagnosticLog.Info("camera.torch", ("error", error?.LocalizedDescription));
            return;
        }

        try
        {
            if (on)
            {
                float strength = Math.Min(level / (float)TorchSteps, AVCaptureDevice.MaxAvailableTorchLevel);
                if (!device.SetTorchModeLevel(strength, out NSError levelError))
                {
                    DiagnosticLog.Info("camera.torch", ("error", levelError?.LocalizedDescription), ("level", level));
                    return;
                }
            }
            else
            {
                device.TorchMode = mode;
            }

            torchOn = on;
            torchLevel = level;
        }
        finally
        {
            device.UnlockForConfiguration();
        }
    }

    /// <summary>
    /// Each frame of the stream, judged the way the desktop judges a photograph, on its luminance, exactly as Android's
    /// CameraSession.Analyze judges it. Until the sheet is known, it is looked for by its codes and its markers (<see cref="LiveSheet"/>);
    /// once known from its markers only, its codes are still tried now and then.
    /// </summary>
    public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        try
        {
            // Entry 283: from the press to the saved picture the live analysis stands aside, so it does not compete for the processor.
            if (stopped || capturing)
            {
                return;
            }

            using var pixels = sampleBuffer.GetImageBuffer() as CVPixelBuffer;
            if (pixels is null)
            {
                return;
            }

            Judge(Luminance(pixels));
        }
        catch (Exception e)
        {
            DiagnosticLog.Info("camera.frame", ("error", e.GetType().Name));
        }
        finally
        {
            // A frame not given back holds one of the camera's few buffers, and the stream stops once they are all held.
            sampleBuffer.Dispose();
        }
    }

    private void Judge(GrayImage grey)
    {
        var frameClock = Stopwatch.StartNew();
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
                ? CaptureGuidance.Search(search, SheetOutline.Find(grey, out string reason), reason)
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

        // Entry 302, torch on Auto, as on Android: it starts at the lowest level, steps up only while the paper is dim, and steps down or goes
        // off on glare, a hotspot or paper already bright, with a settling time between changes so it never flickers (TorchGovernor).
        if (torchChoice == 0 && torchAuto.Next(verdict.Quality, verdict.Evenness, clock.ElapsedMilliseconds) is { } change)
        {
            sessionQueue.DispatchAsync(() => SetTorch(change.Level));
            DiagnosticLog.Info("camera.torch", ("auto", change.Level == 0 ? "off" : "on"), ("level", change.Level), ("of", TorchSteps), ("reason", change.Reason),
                ("paper", verdict.Quality?.PaperLevel), ("clipped", verdict.Quality?.ClippedShare), ("evenness", verdict.Evenness));
        }

        long now = clock.ElapsedMilliseconds;
        // Entry 281 section 1.4: the words held steady, with resolution judged at the size the picture is measured at.
        verdict = steadier.Next(verdict, now, PhoneCamera.MeasuredScale(picture.Width, picture.Height, grey.Width, grey.Height));
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

        // Entry 311 section 1: taken once every frame for AutoShutter.SteadyMs has been judged ready on its own, the same as on Android.
        bool fire = auto.Next(steadier.Decided, card != false, now);
        int? forecast = verdict.Quality is { } quality ? PictureCheck.Forecast(quality) : null;
        bool torchNow = torchOn;
        double progress = manual ? 0 : fire ? 1 : auto.Progress;
        var shown = verdict;
        screen.BeginInvokeOnMainThread(() =>
        {
            if (!stopped && !capturing)
            {
                screen.Show(shown, forecast, torchNow, card);
                screen.Shutter.Progress = progress;
            }
        });
        if (!manual && fire && !taking)
        {
            DiagnosticLog.Info("camera.auto", ("afterReadyMs", now - readySince), ("steadyMs", auto.ReadyMs), ("ms", now));
            screen.BeginInvokeOnMainThread(() => Take("guided, by itself"));
        }
    }

    /// <summary>The frame's luminance, the first plane of the full range stream, row by row, since a row may be padded beyond its width.</summary>
    private static GrayImage Luminance(CVPixelBuffer pixels)
    {
        pixels.Lock(CVPixelBufferLock.ReadOnly);
        try
        {
            int width = (int)pixels.GetWidthOfPlane(0), height = (int)pixels.GetHeightOfPlane(0), stride = (int)pixels.GetBytesPerRowOfPlane(0);
            IntPtr start = pixels.GetBaseAddress(0);
            byte[] grey = new byte[width * height];
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(start + (y * stride), grey, y * width, width);
            }

            return new GrayImage(width, height, grey);
        }
        finally
        {
            pixels.Unlock(CVPixelBufferLock.ReadOnly);
        }
    }

    /// <summary>The shutter: on the interface thread, from the button or from Guided mode.</summary>
    public void Take(string why)
    {
        if (still is null || taking || stopped)
        {
            return;
        }

        taking = true;
        capturing = true;
        pressedAt = clock.ElapsedMilliseconds;
        // Entry 283: the shutter answers at once, a flash, and the reading follows with its progress shown; iOS makes the sound itself.
        screen.Flash();
        screen.Say("Taking the picture…");
        screen.Shutter.Progress = 0;
        DiagnosticLog.Info("camera.shutter", ("step", "press"), ("ms", 0), ("mode", QualityMode ? "quality" : "latency"), ("torch", torchOn), ("guided", !manual));
        string path = Path.Combine(IosPhone.Caches, $"still-{DateTime.Now:HHmmss}.jpg");
        var angle = (NFloat)PhoneCamera.RotationDegrees(turn);
        sessionQueue.DispatchAsync(() =>
        {
            try
            {
                // The picture as JPEG where the camera offers it, which it does; otherwise its own HEIC, turned into a JPEG once saved,
                // since OpenCV on iOS reads no HEIC.
                bool jpeg = still.AvailablePhotoCodecTypes?.Contains(AVVideoCodecType.Jpeg.GetConstant()?.ToString()) == true;
                var settings = jpeg
                    ? AVCapturePhotoSettings.FromFormat(new NSDictionary<NSString, NSObject>(AVVideo.CodecKey, AVVideoCodecType.Jpeg.GetConstant()))
                    : AVCapturePhotoSettings.Create();
                settings.MaxPhotoDimensions = still.MaxPhotoDimensions;
                settings.PhotoQualityPrioritization = QualityMode ? AVCapturePhotoQualityPrioritization.Quality : AVCapturePhotoQualityPrioritization.Speed;
                if (still.ConnectionFromMediaType(AVMediaTypes.Video.GetConstant()) is { } connection && connection.IsVideoRotationAngleSupported(angle))
                {
                    connection.VideoRotationAngle = angle;
                }

                saving = new StillSaved(this, path, why);
                still.CapturePhoto(settings, saving);
                DiagnosticLog.Info("camera.shutter", ("step", "requested"), ("ms", clock.ElapsedMilliseconds - pressedAt), ("jpeg", jpeg));
            }
            catch (Exception e)
            {
                Failed(why, e.GetType().Name);
            }
        });
    }

    /// <summary>The press's delegate let go once it has answered; the photo output holds it only weakly, so it is kept until then.</summary>
    private void Done()
    {
        if (saving is not null)
        {
            saving = null;
        }
    }

    private void Exposed() => DiagnosticLog.Info("camera.shutter", ("step", "exposed"), ("ms", clock.ElapsedMilliseconds - pressedAt));

    private void Saved(string path, string why)
    {
        DiagnosticLog.Info("camera.shutter", ("step", "saved"), ("ms", clock.ElapsedMilliseconds - pressedAt));
        Done();
        taking = false;
        capturing = false;
        bool torch = torchOn;
        DiagnosticLog.Info("camera.take", ("how", why), ("mode", manual ? "manual" : "guided"), ("torch", torch));
        // Entry 281 section 1.2: the torch goes off the moment the picture is taken, and the camera is let go before the result.
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            Stop();
            Taken?.Invoke(path, torch);
        });
    }

    private void Failed(string why, string error)
    {
        Done();
        taking = false;
        capturing = false;
        DiagnosticLog.Info("camera.take", ("how", why), ("error", error));
        screen.BeginInvokeOnMainThread(() => screen.Say("The picture was not taken. Press the shutter again."));
    }

    /// <summary>What becomes of one press: the picture written as a JPEG in the application's cache, or the reason it was not.</summary>
    private sealed class StillSaved(CameraSession owner, string path, string why) : AVCapturePhotoCaptureDelegate
    {
        public override void WillCapturePhoto(AVCapturePhotoOutput captureOutput, AVCaptureResolvedPhotoSettings resolvedSettings) => owner.Exposed();

        public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError error)
        {
            if (error is not null || photo?.FileDataRepresentation is not { } data)
            {
                owner.Failed(why, error?.LocalizedDescription ?? "the camera gave no picture");
                return;
            }

            if (!StillFile.WriteJpeg(data, path))
            {
                owner.Failed(why, "the picture could not be written as a JPEG");
                return;
            }

            owner.Saved(path, why);
        }
    }
}
