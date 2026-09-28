using System.Globalization;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Hardware;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.View;
using GroupLab.Core.Capture;

namespace GroupLab.Android;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 260, Capture B, drawn as Android's own views. On 2026-09-28 the capture screen showed the camera and nothing
/// else on the Fold 7: its words and buttons were Avalonia controls laid over CameraX's preview, and a native view hosted in an Avalonia
/// screen is drawn above everything Avalonia draws, so the preview covered them. Here nothing is drawn over the preview but views of the same
/// kind, in the same layout: the floating panel at the top (Back, the one instruction, the torch, the live checks and the forecast bar), the
/// level near the bottom of the camera, and under the camera, not over it, the shutter with the photo picker to its left and the lens to its
/// right, and the two modes beneath. Each control can be found by its description, which is how the device check reads them.
/// </summary>
internal sealed class CaptureScreen : LinearLayout
{
    public const string BackName = "Back";
    public const string ShutterName = "Shutter";
    public const string InstructionName = "Instruction";
    public const string TorchName = "Torch";
    public const string PickerName = "Choose a photograph";
    public const string LensName = "Lens";
    public const string GuidedName = "Guided mode";
    public const string ManualName = "Manual mode";

    private static readonly Color Panel = Color.Argb(214, 16, 20, 24);
    private static readonly Color Ground = Color.Rgb(16, 20, 24);
    private static readonly Color Dim = Color.Rgb(168, 176, 184);
    internal static readonly Color Amber = Color.Rgb(232, 150, 46);

    private readonly float density;
    private readonly TextView say;
    private readonly TextView checks;
    private readonly TextView score;
    private readonly TextView torch;
    private readonly TextView lens;
    private readonly TextView level;
    private readonly TextView guided;
    private readonly TextView manual;

    public CaptureScreen(Context context, PreviewView preview) : base(context)
    {
        density = context.Resources!.DisplayMetrics!.Density;
        ContentDescription = "Capture screen";
        Orientation = global::Android.Widget.Orientation.Vertical;
        SetBackgroundColor(Ground);

        // The camera, with the panel over its top and the level over its bottom edge.
        var camera = new FrameLayout(context);
        camera.AddView(preview, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

        var back = Round(context, "‹", BackName, 26);
        back.Tag = BackName;
        say = new TextView(context) { Text = "Starting the camera…", TextSize = 19, ContentDescription = InstructionName };
        say.SetTextColor(Color.White);
        say.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
        torch = Pill(context, "Torch: Auto", TorchName);
        var row = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Horizontal };
        row.SetGravity(GravityFlags.CenterVertical);
        row.AddView(back, new LinearLayout.LayoutParams(Dp(48), Dp(48)));
        row.AddView(say, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f) { LeftMargin = Dp(10), RightMargin = Dp(8) });
        row.AddView(torch, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(44)));

        checks = new TextView(context) { TextSize = 13, Text = "" };
        checks.SetTextColor(Dim);
        Bar = new QualityBar(context);
        score = new TextView(context) { TextSize = 13, Text = "–" };
        score.SetTextColor(Color.White);
        var barRow = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Horizontal };
        barRow.SetGravity(GravityFlags.CenterVertical);
        barRow.AddView(Bar, new LinearLayout.LayoutParams(0, Dp(10), 1f));
        barRow.AddView(score, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent) { LeftMargin = Dp(10) });

        var panel = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Vertical, Background = Rounded(Panel, 16) };
        panel.SetPadding(Dp(12), Dp(10), Dp(12), Dp(12));
        panel.AddView(row);
        panel.AddView(checks, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(6) });
        panel.AddView(barRow, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(8) });
        camera.AddView(panel, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Top)
        {
            LeftMargin = Dp(10), RightMargin = Dp(10), TopMargin = Dp(10),
        });

        level = new TextView(context) { TextSize = 13, Text = "", Background = Rounded(Panel, 12) };
        level.SetTextColor(Color.White);
        level.SetPadding(Dp(10), Dp(4), Dp(10), Dp(4));
        camera.AddView(level, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Bottom | GravityFlags.CenterHorizontal)
        {
            BottomMargin = Dp(10),
        });
        AddView(camera, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f));

        // Under the camera: picker, shutter, lens; the modes beneath.
        var picker = Pill(context, "Photos", PickerName);
        Shutter = new ShutterButton(context) { ContentDescription = ShutterName, Clickable = true };
        lens = Pill(context, "1x", LensName);
        var shutterRow = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Horizontal };
        shutterRow.SetGravity(GravityFlags.Center);
        shutterRow.AddView(picker, new LinearLayout.LayoutParams(Dp(88), Dp(44)));
        shutterRow.AddView(Shutter, new LinearLayout.LayoutParams(Dp(76), Dp(76)) { LeftMargin = Dp(28), RightMargin = Dp(28) });
        shutterRow.AddView(lens, new LinearLayout.LayoutParams(Dp(88), Dp(44)));

        guided = Mode(context, "GUIDED", GuidedName);
        manual = Mode(context, "MANUAL", ManualName);
        var modes = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Horizontal };
        modes.SetGravity(GravityFlags.Center);
        modes.AddView(guided, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(44)));
        modes.AddView(manual, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(44)) { LeftMargin = Dp(24) });

        var controls = new LinearLayout(context) { Orientation = global::Android.Widget.Orientation.Vertical };
        controls.SetPadding(Dp(12), Dp(14), Dp(12), Dp(8));
        controls.AddView(shutterRow);
        controls.AddView(modes, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = Dp(6) });
        AddView(controls, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        back.Click += (_, _) => BackPressed?.Invoke();
        torch.Click += (_, _) => TorchPressed?.Invoke();
        picker.Click += (_, _) => PickerPressed?.Invoke();
        lens.Click += (_, _) => LensPressed?.Invoke();
        Shutter.Click += (_, _) => ShutterPressed?.Invoke();
        guided.Click += (_, _) => ModeChosen?.Invoke(false);
        manual.Click += (_, _) => ModeChosen?.Invoke(true);
    }

    public event Action? BackPressed;

    public event Action? TorchPressed;

    public event Action? PickerPressed;

    public event Action? LensPressed;

    public event Action? ShutterPressed;

    public event Action<bool>? ModeChosen;

    public ShutterButton Shutter { get; }

    public QualityBar Bar { get; }

    /// <summary>One frame's verdict on the panel: the instruction, the live checks, and the forecast of the picture this frame would make.</summary>
    public void Show(FrameVerdict verdict, int? forecast, bool torchOn)
    {
        say.Text = verdict.Words;
        var parts = new List<string>();
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
        score.Text = forecast is { } s ? string.Create(CultureInfo.InvariantCulture, $"{s} {PictureCheck.BandWord(PictureCheck.Band(s))}") : "–";
    }

    public void ShowTorch(int choice) => torch.Text = choice switch { 1 => "Torch: On", 2 => "Torch: Off", _ => "Torch: Auto" };

    public void ShowLens(float ratio) => lens.Text = ratio.ToString("0.#", CultureInfo.InvariantCulture) + "x";

    public void ShowLevel(double degrees) =>
        level.Text = string.Create(CultureInfo.InvariantCulture, $"Level {degrees:0}°{(degrees > CaptureQualities.FineDegrees ? ", hold it flatter" : "")}");

    public void ShowMode(bool isManual)
    {
        guided.SetTextColor(isManual ? Dim : Amber);
        manual.SetTextColor(isManual ? Amber : Dim);
        Shutter.Manual = isManual;
    }

    public void Say(string words) => say.Text = words;

    private string? laidOut;

    /// <summary>
    /// Entry 260's device check: after each layout, what of the instruction, the shutter and Back can be seen, and where the screen ends,
    /// written to the log. A UI dump cannot see these, because a native view hosted in an Avalonia screen is not in Avalonia's accessibility
    /// tree, so scripts/device-capture-check.py reads this line instead.
    /// </summary>
    protected override void OnLayout(bool changed, int l, int t, int r, int b)
    {
        base.OnLayout(changed, l, t, r, b);
        string Seen(View? view)
        {
            var rect = new Rect();
            return view is not null && view.IsShown && view.GetGlobalVisibleRect(rect) ? string.Create(CultureInfo.InvariantCulture, $"{rect.Width()}x{rect.Height()}") : "hidden";
        }

        var screen = new Rect();
        GetGlobalVisibleRect(screen);
        string now = string.Join(" ", $"instruction={Seen(say)}", $"shutter={Seen(Shutter)}", $"back={Seen(FindViewWithTag(BackName))}",
            string.Create(CultureInfo.InvariantCulture, $"bottom={screen.Bottom}"), string.Create(CultureInfo.InvariantCulture, $"window={RootView?.Height}"));
        if (now != laidOut)
        {
            laidOut = now;
            GroupLab.App.Diagnostics.DiagnosticLog.Info("camera.layout", ("seen", now));
        }
    }

    private int Dp(float dp) => (int)Math.Round(dp * density);

    private GradientDrawable Rounded(Color color, float radiusDp)
    {
        var shape = new GradientDrawable();
        shape.SetColor(color);
        shape.SetCornerRadius(radiusDp * density);
        return shape;
    }

    private TextView Round(Context context, string text, string name, float size)
    {
        var view = new TextView(context) { Text = text, TextSize = size, ContentDescription = name, Clickable = true, Background = Rounded(Color.Argb(60, 255, 255, 255), 24) };
        view.SetTextColor(Color.White);
        view.Gravity = GravityFlags.Center;
        return view;
    }

    private TextView Pill(Context context, string text, string name)
    {
        var view = new TextView(context) { Text = text, TextSize = 14, ContentDescription = name, Clickable = true, Background = Rounded(Color.Argb(60, 255, 255, 255), 22) };
        view.SetTextColor(Color.White);
        view.Gravity = GravityFlags.Center;
        view.SetPadding(Dp(12), 0, Dp(12), 0);
        return view;
    }

    private static TextView Mode(Context context, string text, string name)
    {
        var view = new TextView(context) { Text = text, TextSize = 14, ContentDescription = name, Clickable = true, Gravity = GravityFlags.Center };
        view.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
        view.SetPadding(12, 0, 12, 0);
        return view;
    }
}

/// <summary>The shutter: a white disc in a ring that, in Guided mode, fills with amber as the ready frames come.</summary>
internal sealed class ShutterButton(Context context) : View(context)
{
    private readonly Paint paint = new(PaintFlags.AntiAlias);
    private float progress;
    private bool manual;

    /// <summary>The share of the ready frames seen, 0 to 1.</summary>
    public float Progress
    {
        get => progress;
        set
        {
            progress = Math.Clamp(value, 0, 1);
            Invalidate();
        }
    }

    public bool Manual
    {
        get => manual;
        set
        {
            manual = value;
            Invalidate();
        }
    }

    protected override void OnDraw(Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        float cx = Width / 2f, cy = Height / 2f, r = Math.Min(Width, Height) / 2f;
        float ring = r * 0.12f;
        paint.SetStyle(Paint.Style.Stroke);
        paint.StrokeWidth = ring;
        paint.Color = Color.Argb(110, 255, 255, 255);
        canvas.DrawCircle(cx, cy, r - (ring / 2), paint);
        if (!manual && progress > 0)
        {
            paint.Color = CaptureScreen.Amber;
            canvas.DrawArc(new RectF(cx - r + (ring / 2), cy - r + (ring / 2), cx + r - (ring / 2), cy + r - (ring / 2)), -90, 360 * progress, false, paint);
        }

        paint.SetStyle(Paint.Style.Fill);
        paint.Color = Pressed ? Color.Rgb(210, 210, 210) : Color.White;
        canvas.DrawCircle(cx, cy, r - (2.2f * ring), paint);
    }
}

/// <summary>Entry 260's quality bar: red for Retake, amber for Usable, green for Good, with a mark at the score; grey with none.</summary>
internal sealed class QualityBar(Context context) : View(context)
{
    private readonly Paint paint = new(PaintFlags.AntiAlias);
    private int? score;

    public int? Score
    {
        get => score;
        set
        {
            score = value;
            Invalidate();
        }
    }

    protected override void OnDraw(Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        float h = Height, w = Width, r = h / 2;
        paint.SetStyle(Paint.Style.Fill);
        if (score is null)
        {
            paint.SetShader(null);
            paint.Color = Color.Argb(90, 255, 255, 255);
            canvas.DrawRoundRect(new RectF(0, 0, w, h), r, r, paint);
            return;
        }

        // The bands meet at the score's own thresholds, 40 and 70, so the colour under the mark is the band the words beside it name.
        float usable = w * CaptureQualities.Usable / 100f, good = w * CaptureQualities.Good / 100f;
        paint.SetShader(null);
        paint.Color = Color.Rgb(208, 69, 58);
        canvas.DrawRoundRect(new RectF(0, 0, usable + r, h), r, r, paint);
        paint.Color = Color.Rgb(46, 160, 90);
        canvas.DrawRoundRect(new RectF(good - r, 0, w, h), r, r, paint);
        paint.Color = CaptureScreen.Amber;
        canvas.DrawRect(new RectF(usable, 0, good, h), paint);

        float x = Math.Clamp(w * score.Value / 100f, r, w - r);
        paint.Color = Color.White;
        canvas.DrawCircle(x, h / 2, h * 0.9f, paint);
        paint.Color = Color.Rgb(16, 20, 24);
        canvas.DrawCircle(x, h / 2, h * 0.45f, paint);
    }
}

/// <summary>How far the phone is from flat, from gravity: 0 when it lies square over a sheet on a table.</summary>
internal sealed class Level(Action<double> changed) : Java.Lang.Object, ISensorEventListener
{
    public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
    {
    }

    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Values is not { Count: >= 3 } v)
        {
            return;
        }

        double x = v[0], y = v[1], z = v[2], g = Math.Sqrt((x * x) + (y * y) + (z * z));
        if (g > 0)
        {
            changed(Math.Acos(Math.Min(1, Math.Abs(z) / g)) * 180 / Math.PI);
        }
    }
}
