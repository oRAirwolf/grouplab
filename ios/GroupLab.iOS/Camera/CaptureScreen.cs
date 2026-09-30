#nullable disable warnings
using System.Globalization;
using System.Runtime.InteropServices;
using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using GroupLab.Core.Capture;
using UIKit;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: Capture B on iPhone and iPad, drawn as UIKit's own views, for the reason Android draws
/// it with Android's (android/GroupLab.Android/CaptureScreen.cs): a native view hosted in an Avalonia screen covers whatever Avalonia draws
/// in its place, so nothing Avalonia draws could be seen over the camera. The panel at the top (Back, the one instruction, the torch, the
/// live checks and the forecast bar), the level in the middle of the camera, and under the camera the shutter with the photo picker to its
/// left, and the two modes beneath. The iPad mini has one camera at the back, so there is no lens button.
/// <para>
/// NOTES-FROM-PLANNING.md entry 313 section 2: on the iPad mini the preview filled from the top of the screen with the panel floating over
/// it, so its top edge showed between the status bar and the panel and the panel hid the top of the picture. The panel now sits above the
/// preview, which starts under it and is centred in what is left, so everything the preview shows is what the picture will hold.
/// </para>
/// Each control carries its name as its accessibility identifier, as Android's carry theirs as their descriptions.
/// </summary>
internal sealed class CaptureScreen : UIView
{
    public const string BackName = "Back";
    public const string ShutterName = "Shutter";
    public const string InstructionName = "Instruction";
    public const string TorchName = "Torch";
    public const string PickerName = "Choose a photo";
    public const string GuidedName = "Guided mode";
    public const string ManualName = "Manual mode";
    public const string LevelName = "Level";
    public const string ResultName = "Result";
    public const string DiagnosticsName = "Diagnostics";

    internal static readonly UIColor Amber = UIColor.FromRGB(232, 150, 46);
    private static readonly UIColor Ground = UIColor.FromRGB(16, 20, 24);
    private static readonly UIColor Panel = UIColor.FromRGBA(16, 20, 24, 214);
    private static readonly UIColor Dim = UIColor.FromRGB(168, 176, 184);
    private static readonly UIColor Glass = UIColor.FromRGBA(255, 255, 255, 60);

    private readonly UIView camera = new() { ClipsToBounds = true, BackgroundColor = UIColor.Black };
    private readonly UIView panel = new() { BackgroundColor = Panel };
    private readonly UIButton back;
    private readonly UILabel say;
    private readonly UIButton torch;
    private readonly UILabel checks;
    private readonly UILabel score;
    private readonly UIButton picker;
    private readonly UIButton guided;
    private readonly UIButton manual;
    private readonly UIButton result;
    private readonly BubbleView level = new();

    /// <summary>Entry 315 section 4: Show diagnostics on the camera, in the camera's bottom corner, hidden until the first values come.</summary>
    private readonly UIView diagnosticsBox = new() { BackgroundColor = UIColor.FromRGBA(0, 0, 0, 170), Hidden = true, UserInteractionEnabled = false };
    private readonly UILabel diagnostics = new() { TextColor = UIColor.White, Font = UIFont.MonospacedDigitSystemFontOfSize(11, UIFontWeight.Regular), Lines = 0 };
    private string? laidOut;

    public CaptureScreen(AVCaptureVideoPreviewLayer preview)
    {
        Preview = preview;
        BackgroundColor = Ground;
        AccessibilityIdentifier = "Capture screen";
        preview.VideoGravity = AVLayerVideoGravity.ResizeAspect;
        camera.Layer.AddSublayer(preview);
        AddSubview(camera);

        panel.Layer.CornerRadius = 16;
        back = Round("‹", BackName);
        say = new UILabel
        {
            Text = "Starting the camera…",
            TextColor = UIColor.White,
            Font = UIFont.SystemFontOfSize(19, UIFontWeight.Bold),
            Lines = 3,
            AccessibilityIdentifier = InstructionName,
        };
        torch = Pill("Torch: Auto", TorchName);
        checks = new UILabel { Text = "", TextColor = Dim, Font = UIFont.SystemFontOfSize(13, UIFontWeight.Regular), Lines = 2 };
        Bar = new QualityBar();
        // Entry 313 section 2: nothing beside the bar until there is a score; the dash it showed read as a stray character.
        score = new UILabel { Text = "", TextColor = UIColor.White, Font = UIFont.SystemFontOfSize(13, UIFontWeight.Regular) };
        panel.AddSubviews(back, say, torch, checks, Bar, score);
        AddSubview(panel);

        // Entry 281 section 1.1: the level is a crosshair in the middle of the camera with a dot that moves like a bubble.
        level.AccessibilityIdentifier = LevelName;
        camera.AddSubview(level);
        diagnosticsBox.Layer.CornerRadius = 6;
        diagnosticsBox.AccessibilityIdentifier = DiagnosticsName;
        diagnosticsBox.AddSubview(diagnostics);
        camera.AddSubview(diagnosticsBox);

        picker = Pill("Photos", PickerName);
        Shutter = new ShutterButton { AccessibilityIdentifier = ShutterName, AccessibilityLabel = ShutterName };
        guided = Mode("GUIDED", GuidedName);
        manual = Mode("MANUAL", ManualName);
        // Entry 281 section 1.3: back to the last result in one press, without leaving through the start of the tab.
        result = Pill("Result", ResultName);
        result.Hidden = true;
        AddSubviews(picker, Shutter, guided, manual, result);

        back.TouchUpInside += (_, _) => BackPressed?.Invoke();
        torch.TouchUpInside += (_, _) => TorchPressed?.Invoke();
        picker.TouchUpInside += (_, _) => PickerPressed?.Invoke();
        Shutter.TouchUpInside += (_, _) => ShutterPressed?.Invoke();
        guided.TouchUpInside += (_, _) => ModeChosen?.Invoke(false);
        manual.TouchUpInside += (_, _) => ModeChosen?.Invoke(true);
        result.TouchUpInside += (_, _) => ResultPressed?.Invoke();
        camera.AddGestureRecognizer(new UITapGestureRecognizer(tap =>
        {
            if (tap.State == UIGestureRecognizerState.Ended)
            {
                // The preview layer fills the camera view, so a point in the one is the same point in the other; the panel is above it.
                CGPoint at = tap.LocationInView(camera);
                Tapped?.Invoke(Preview.CaptureDevicePointOfInterestForPoint(at));
            }
        }));
    }

    public AVCaptureVideoPreviewLayer Preview { get; }

    public ShutterButton Shutter { get; }

    public QualityBar Bar { get; }

    public event Action? BackPressed;

    public event Action? TorchPressed;

    public event Action? PickerPressed;

    public event Action? ShutterPressed;

    public event Action? ResultPressed;

    public event Action<bool>? ModeChosen;

    /// <summary>After each layout, on the interface thread: the preview is stood the way the screen now is.</summary>
    public event Action? LaidOut;

    /// <summary>A tap on the camera, at the point of the picture it falls on (0 to 1 each way), where focus and exposure are held.</summary>
    public event Action<CGPoint>? Tapped;

    /// <summary>Which way up the screen is now, for the level and for the picture's orientation.</summary>
    public ScreenTurn Turn => (Window?.WindowScene?.EffectiveGeometry?.InterfaceOrientation ?? UIInterfaceOrientation.Portrait) switch
    {
        UIInterfaceOrientation.PortraitUpsideDown => ScreenTurn.UpsideDown,
        UIInterfaceOrientation.LandscapeLeft => ScreenTurn.LandscapeLeft,
        UIInterfaceOrientation.LandscapeRight => ScreenTurn.LandscapeRight,
        _ => ScreenTurn.Upright,
    };

    /// <summary>
    /// Entry 313 section 2, for the self-test: where the panel, the camera and the shutter are, the safe area's top, and what shows beside
    /// the quality bar.
    /// </summary>
    internal (CGRect Panel, CGRect Camera, CGRect Shutter, double SafeTop, string Score) Geometry() => (panel.Frame, camera.Frame, Shutter.Frame, SafeAreaInsets.Top, score.Text ?? "");

    /// <summary>Shows the Result button where there is a result to go back to.</summary>
    public void ShowResultButton(bool shown) => result.Hidden = !shown;

    /// <summary>One frame's verdict on the panel: the instruction, the live checks, and the forecast of the picture this frame would make.</summary>
    public void Show(FrameVerdict verdict, int? forecast, bool torchOn, bool? card = null)
    {
        say.Text = verdict.Words;
        var parts = new List<string>();
        if (card is { } seen)
        {
            parts.Add(seen ? "Card edges found" : "No card yet");
            parts.Add(verdict.Mapping is null ? "Outline not read" : "Printed outline read");
        }

        if (verdict.Quality is { } q)
        {
            parts.Add(q.FocusPart is >= CaptureGuidance.Holds ? "Focus sharp" : "Focus soft");
            parts.Add(verdict.Evenness is < PictureCheck.EvenLight ? "Light uneven" : q.ExposurePart is >= CaptureGuidance.Holds ? "Light good" : q.ClippedShare > CaptureQualities.FineClipped ? "Light too bright" : "Light dim");
        }

        if (verdict.MarkersRead is { } read)
        {
            parts.Add(verdict.MarkersExpected is { } expected ? string.Create(CultureInfo.InvariantCulture, $"Tags {read} of {expected}") : string.Create(CultureInfo.InvariantCulture, $"Tags {read}"));
        }

        if (verdict.CodesRead is { } codes)
        {
            parts.Add(verdict.CodesExpected is { } all ? string.Create(CultureInfo.InvariantCulture, $"QR {Math.Min(codes, all)} of {all}") : string.Create(CultureInfo.InvariantCulture, $"QR {codes}"));
        }

        parts.Add(torchOn ? "Torch on" : "Torch off");
        checks.Text = string.Join(" · ", parts);
        Bar.Score = forecast;
        score.Text = forecast is { } s ? string.Create(CultureInfo.InvariantCulture, $"{s} {PictureCheck.BandWord(PictureCheck.Band(s))}") : "";
        SetNeedsLayout();
    }

    public void ShowTorch(int choice) => torch.SetTitle(choice switch { 1 => "Torch: On", 2 => "Torch: Off", _ => "Torch: Auto" }, UIControlState.Normal);

    public void ShowMode(bool isManual)
    {
        guided.SetTitleColor(isManual ? Dim : Amber, UIControlState.Normal);
        manual.SetTitleColor(isManual ? Amber : Dim, UIControlState.Normal);
        Shutter.Manual = isManual;
    }

    /// <summary>The level from gravity, already in the screen's axes as Android's sensor gives it.</summary>
    public void ShowLevel(double x, double y, double z) => level.Show(x, y, z);

    /// <summary>Whether the level shows flat now, for the self-test.</summary>
    internal bool LevelReady => level.Ready;

    /// <summary>Entry 315 section 4: the diagnostics overlay's lines, shown from the first; only the overlay is laid out again.</summary>
    public void ShowDiagnostics(string text)
    {
        diagnostics.Text = text;
        diagnosticsBox.Hidden = false;
        PlaceDiagnostics();
    }

    /// <summary>The overlay sized to its lines, in the camera's bottom left corner.</summary>
    private void PlaceDiagnostics()
    {
        var size = diagnostics.SizeThatFits(new CGSize(Math.Max(0, camera.Bounds.Width - 36), 400));
        diagnostics.Frame = new CGRect(8, 6, size.Width, size.Height);
        diagnosticsBox.Frame = new CGRect(10, Math.Max(0, camera.Bounds.Height - size.Height - 12 - 10), size.Width + 16, size.Height + 12);
    }

    public void Say(string words)
    {
        say.Text = words;
        SetNeedsLayout();
    }

    /// <summary>Entry 283: a white flash over the camera the moment the shutter is pressed, so the press is answered at once.</summary>
    public void Flash()
    {
        var white = new UIView(camera.Bounds) { BackgroundColor = UIColor.White, Alpha = 0.85f, UserInteractionEnabled = false };
        camera.AddSubview(white);
        UIView.Animate(0.18, () => white.Alpha = 0, white.RemoveFromSuperview);
    }

    /// <summary>
    /// Entry 313 section 2: the panel at the top, under the status bar; the camera takes the height between it and the controls, the
    /// preview centred in it and the level in its middle; the controls sit under it, inside the safe area, so neither the home indicator nor
    /// the status bar covers them.
    /// </summary>
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        var safe = SafeAreaInsets;
        double width = Bounds.Width, height = Bounds.Height;
        const double shutterSize = 76, modeHeight = 44, gap = 10;
        double controls = 14 + shutterSize + 6 + modeHeight + 8;

        // The panel: Back, the instruction and the torch in a row; the checks; the bar and its score.
        double panelWidth = Math.Max(0, width - safe.Left - safe.Right - (2 * gap));
        double inner = panelWidth - 24;
        var torchSize = torch.SizeThatFits(new CGSize(200, 44));
        double torchWidth = Math.Min(160, torchSize.Width + 24);
        double sayWidth = Math.Max(0, inner - 48 - 10 - 8 - torchWidth);
        var saySize = say.SizeThatFits(new CGSize(sayWidth, 200));
        double row = Math.Max(48, saySize.Height);
        back.Frame = new CGRect(12, 10 + ((row - 48) / 2), 48, 48);
        say.Frame = new CGRect(12 + 48 + 10, 10, sayWidth, row);
        torch.Frame = new CGRect(12 + inner - torchWidth, 10 + ((row - 44) / 2), torchWidth, 44);
        var checksSize = checks.SizeThatFits(new CGSize(inner, 100));
        checks.Frame = new CGRect(12, 10 + row + 6, inner, checksSize.Height);
        var scoreSize = score.SizeThatFits(new CGSize(120, 30));
        double barTop = checks.Frame.Bottom + 8;
        double barRow = Math.Max(10, scoreSize.Height);
        Bar.Frame = new CGRect(12, barTop + ((barRow - 10) / 2), Math.Max(0, inner - scoreSize.Width - 10), 10);
        score.Frame = new CGRect(12 + inner - scoreSize.Width, barTop, scoreSize.Width, barRow);
        panel.Frame = new CGRect(safe.Left + gap, safe.Top + gap, panelWidth, barTop + barRow + 12);

        // The camera under the panel, down to the controls; the preview keeps the picture's shape, centred both ways in it.
        double cameraTop = panel.Frame.Bottom + gap;
        double cameraHeight = Math.Max(0, height - controls - safe.Bottom - cameraTop);
        camera.Frame = new CGRect(0, cameraTop, width, cameraHeight);
        CATransaction.Begin();
        CATransaction.DisableActions = true;
        Preview.Frame = camera.Bounds;
        CATransaction.Commit();

        double levelSize = 132;
        level.Frame = new CGRect((width - levelSize) / 2, (cameraHeight - levelSize) / 2, levelSize, levelSize);
        PlaceDiagnostics();

        // Under the camera: the picker, the shutter; the modes beneath.
        double middle = width / 2, top = camera.Frame.Bottom + 14;
        Shutter.Frame = new CGRect(middle - (shutterSize / 2), top, shutterSize, shutterSize);
        picker.Frame = new CGRect(middle - (shutterSize / 2) - 28 - 88, top + ((shutterSize - 44) / 2), 88, 44);
        double modesTop = top + shutterSize + 6;
        var guidedSize = guided.SizeThatFits(new CGSize(200, modeHeight));
        var manualSize = manual.SizeThatFits(new CGSize(200, modeHeight));
        var resultSize = result.Hidden ? CGSize.Empty : new CGSize(result.SizeThatFits(new CGSize(200, modeHeight)).Width + 24, modeHeight);
        double modes = guidedSize.Width + 24 + manualSize.Width + (result.Hidden ? 0 : 24 + resultSize.Width);
        double x = middle - (modes / 2);
        guided.Frame = new CGRect(x, modesTop, guidedSize.Width, modeHeight);
        x += guidedSize.Width + 24;
        manual.Frame = new CGRect(x, modesTop, manualSize.Width, modeHeight);
        x += manualSize.Width + 24;
        result.Frame = new CGRect(x, modesTop, resultSize.Width, modeHeight);

        Laid();
        LaidOut?.Invoke();
    }

    /// <summary>
    /// Entry 260's device check, as on Android: after each layout, the sizes of the instruction, the shutter and Back, and where the screen
    /// ends, written to the log, since a native view hosted in an Avalonia screen is not in Avalonia's accessibility tree.
    /// </summary>
    private void Laid()
    {
        static string Seen(UIView view) =>
            view.Hidden || view.Window is null ? "hidden" : string.Create(CultureInfo.InvariantCulture, $"{(int)view.Frame.Width}x{(int)view.Frame.Height}");
        string now = string.Join(" ", $"instruction={Seen(say)}", $"shutter={Seen(Shutter)}", $"back={Seen(back)}",
            string.Create(CultureInfo.InvariantCulture, $"bottom={(int)Frame.Bottom}"), string.Create(CultureInfo.InvariantCulture, $"window={(int)(Window?.Bounds.Height ?? 0)}"));
        if (now != laidOut)
        {
            laidOut = now;
            GroupLab.App.Diagnostics.DiagnosticLog.Info("camera.layout", ("seen", now));
        }
    }

    private static UIButton Round(string text, string name)
    {
        var button = UIButton.FromType(UIButtonType.Custom);
        button.SetTitle(text, UIControlState.Normal);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.TitleLabel.Font = UIFont.SystemFontOfSize(26, UIFontWeight.Regular);
        button.BackgroundColor = Glass;
        button.Layer.CornerRadius = 24;
        button.AccessibilityIdentifier = name;
        button.AccessibilityLabel = name;
        return button;
    }

    private static UIButton Pill(string text, string name)
    {
        var button = UIButton.FromType(UIButtonType.Custom);
        button.SetTitle(text, UIControlState.Normal);
        button.SetTitleColor(UIColor.White, UIControlState.Normal);
        button.TitleLabel.Font = UIFont.SystemFontOfSize(14, UIFontWeight.Regular);
        button.BackgroundColor = Glass;
        button.Layer.CornerRadius = 22;
        button.AccessibilityIdentifier = name;
        button.AccessibilityLabel = name;
        return button;
    }

    private static UIButton Mode(string text, string name)
    {
        var button = UIButton.FromType(UIButtonType.Custom);
        button.SetTitle(text, UIControlState.Normal);
        button.TitleLabel.Font = UIFont.SystemFontOfSize(14, UIFontWeight.Bold);
        button.AccessibilityIdentifier = name;
        button.AccessibilityLabel = name;
        return button;
    }
}

/// <summary>
/// Entry 281 section 1.1, Alan's level, as on Android: four arms and a dot that drifts toward the raised side the way a bubble does, green
/// within <see cref="BubbleLevel.ReadyDegrees"/> of flat and white beyond. The arms are thin and half white, so the preview shows through.
/// <para>
/// NOTES-FROM-PLANNING.md entry 311 section 2, Alan on the iPad: "The level on the camera screen does not turn green when it is level." Only
/// the dot turned green here, a spot 18 points across over the picture, where Android had turned the whole crosshair and its ring green and
/// thicker since entry 291 section 3.4. The iPad now draws it as Android does.
/// </para>
/// </summary>
internal sealed class BubbleView : UIView
{
    private (double Right, double Down) dot;
    private bool ready;

    public BubbleView()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;
        UserInteractionEnabled = false;
    }

    /// <summary>Whether the level shows flat, green, now.</summary>
    public bool Ready => ready;

    public void Show(double x, double y, double z)
    {
        dot = BubbleLevel.Dot(x, y, z);
        ready = BubbleLevel.Ready(x, y, z);
        SetNeedsDisplay();
    }

    public override void Draw(CGRect rect)
    {
        double cx = Bounds.Width / 2, cy = Bounds.Height / 2, arm = Math.Min(Bounds.Width, Bounds.Height) / 2 * 0.86, dotRadius = arm * 0.16;
        // Entry 311 section 2, as Android since entry 291 section 3.4: level, the whole crosshair and its ring turn green with the dot, and
        // thicker, so it is plain at a glance.
        var green = UIColor.FromRGB(46, 160, 90);
        (ready ? green : UIColor.FromRGBA(255, 255, 255, 150)).SetStroke();
        double line = ready ? Math.Max(4, arm * 0.06) : Math.Max(2, arm * 0.03);
        var cross = new UIBezierPath { LineWidth = (NFloat)line };
        cross.MoveTo(new CGPoint(cx - arm, cy));
        cross.AddLineTo(new CGPoint(cx + arm, cy));
        cross.MoveTo(new CGPoint(cx, cy - arm));
        cross.AddLineTo(new CGPoint(cx, cy + arm));
        cross.Stroke();

        // The ready ring: the dot inside it is within the ready tolerance.
        double ring = (arm * BubbleLevel.ReadyDegrees / BubbleLevel.FullScaleDegrees) + dotRadius;
        var circle = UIBezierPath.FromOval(new CGRect(cx - ring, cy - ring, 2 * ring, 2 * ring));
        circle.LineWidth = (NFloat)line;
        circle.Stroke();

        (ready ? green : UIColor.White).SetFill();
        double dx = cx + (dot.Right * arm), dy = cy + (dot.Down * arm);
        UIBezierPath.FromOval(new CGRect(dx - dotRadius, dy - dotRadius, 2 * dotRadius, 2 * dotRadius)).Fill();
    }
}

/// <summary>The shutter, as on Android: a white disc in a ring that, in Guided mode, fills with amber as the ready frames come.</summary>
internal sealed class ShutterButton : UIControl
{
    private double progress;
    private bool manual;

    public ShutterButton()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;
        IsAccessibilityElement = true;
        AccessibilityTraits = UIAccessibilityTrait.Button;
    }

    /// <summary>The share of the ready frames seen, 0 to 1.</summary>
    public double Progress
    {
        get => progress;
        set
        {
            progress = Math.Clamp(value, 0, 1);
            SetNeedsDisplay();
        }
    }

    public bool Manual
    {
        get => manual;
        set
        {
            manual = value;
            SetNeedsDisplay();
        }
    }

    public override bool Highlighted
    {
        get => base.Highlighted;
        set
        {
            base.Highlighted = value;
            SetNeedsDisplay();
        }
    }

    public override void Draw(CGRect rect)
    {
        double cx = Bounds.Width / 2, cy = Bounds.Height / 2, r = Math.Min(Bounds.Width, Bounds.Height) / 2;
        double ring = r * 0.12;
        UIColor.FromRGBA(255, 255, 255, 110).SetStroke();
        var outer = UIBezierPath.FromOval(new CGRect(cx - r + (ring / 2), cy - r + (ring / 2), (2 * r) - ring, (2 * r) - ring));
        outer.LineWidth = (NFloat)ring;
        outer.Stroke();
        if (!manual && progress > 0)
        {
            CaptureScreen.Amber.SetStroke();
            double start = -Math.PI / 2;
            var arc = UIBezierPath.FromArc(new CGPoint(cx, cy), (NFloat)(r - (ring / 2)), (NFloat)start, (NFloat)(start + (2 * Math.PI * progress)), true);
            arc.LineWidth = (NFloat)ring;
            arc.Stroke();
        }

        (Highlighted ? UIColor.FromRGB(210, 210, 210) : UIColor.White).SetFill();
        double disc = r - (2.2 * ring);
        UIBezierPath.FromOval(new CGRect(cx - disc, cy - disc, 2 * disc, 2 * disc)).Fill();
    }
}

/// <summary>Entry 260's quality bar, as on Android: red for Retake, amber for Usable, green for Good, with a mark at the score; grey with none.</summary>
internal sealed class QualityBar : UIView
{
    private int? score;

    public QualityBar()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;
    }

    public int? Score
    {
        get => score;
        set
        {
            score = value;
            SetNeedsDisplay();
        }
    }

    public override void Draw(CGRect rect)
    {
        double h = Bounds.Height, w = Bounds.Width, r = h / 2;
        if (score is null)
        {
            UIColor.FromRGBA(255, 255, 255, 90).SetFill();
            UIBezierPath.FromRoundedRect(new CGRect(0, 0, w, h), (NFloat)r).Fill();
            return;
        }

        // The bands meet at the score's own thresholds, 40 and 70, so the colour under the mark is the band the words beside it name.
        double usable = w * CaptureQualities.Usable / 100.0, good = w * CaptureQualities.Good / 100.0;
        UIColor.FromRGB(208, 69, 58).SetFill();
        UIBezierPath.FromRoundedRect(new CGRect(0, 0, usable + r, h), (NFloat)r).Fill();
        UIColor.FromRGB(46, 160, 90).SetFill();
        UIBezierPath.FromRoundedRect(new CGRect(good - r, 0, w - good + r, h), (NFloat)r).Fill();
        CaptureScreen.Amber.SetFill();
        UIBezierPath.FromRect(new CGRect(usable, 0, good - usable, h)).Fill();

        double x = Math.Clamp(w * score.Value / 100.0, r, w - r);
        UIColor.White.SetFill();
        UIBezierPath.FromOval(new CGRect(x - (h * 0.9), (h / 2) - (h * 0.9), h * 1.8, h * 1.8)).Fill();
        UIColor.FromRGB(16, 20, 24).SetFill();
        UIBezierPath.FromOval(new CGRect(x - (h * 0.45), (h / 2) - (h * 0.45), h * 0.9, h * 0.9)).Fill();
    }
}
