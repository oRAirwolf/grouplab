using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using OpenCvSharp;

namespace GroupLab.Tests.Support;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 475: a screen recording of GroupLab made by script, frame by frame, from the application's own drawing in the
/// headless platform. A pointer drawn over the frames glides between targets (or, on the phone, a touch is shown where a finger lands); the
/// window is drawn again only when something on it changes, and every other frame repeats the last with the pointer moved. Frames go to
/// Windows' own H.264 encoder through OpenCV, 60 a second, with one still second at the start and the end. Nothing runs unless
/// <c>GROUPLAB_VIDEO_CLIPS</c> names the folder the clips go to (<see cref="SkipUnlessAsked"/>), so CI never makes them.
/// </summary>
internal sealed class ScreenClip : IDisposable
{
    public const int Fps = 60;

    private readonly TopLevel top;
    private readonly VideoWriter writer;
    private readonly bool touches;
    private readonly string path;
    private readonly string name;
    private readonly string shows;
    private Mat screen;
    private Avalonia.Point pointer;
    private Avalonia.Point? touch;
    private int touchFrames;
    private int frames;

    /// <summary>The folder the clips go to, or null where none were asked for.</summary>
    public static string? Folder => Environment.GetEnvironmentVariable("GROUPLAB_VIDEO_CLIPS") is { Length: > 0 } folder ? folder : null;

    /// <summary>The first line of every clip: skipped everywhere but where the clips were asked for, CI included.</summary>
    public static void SkipUnlessAsked() => Xunit.Assert.SkipWhen(Folder is null, "Made only when GROUPLAB_VIDEO_CLIPS names a folder (entry 475).");

    public string Path => path;

    public double Seconds => frames / (double)Fps;

    public (int Width, int Height) Size => (screen.Width, screen.Height);

    /// <param name="shows">What the clip shows, in a line, for the manifest.</param>
    /// <param name="touches">True on the phone: no pointer, and a ring where each tap lands.</param>
    public ScreenClip(TopLevel top, string name, string shows, bool touches = false)
    {
        this.top = top;
        this.touches = touches;
        this.name = name;
        this.shows = shows;
        path = System.IO.Path.Combine(Folder ?? throw new InvalidOperationException("GROUPLAB_VIDEO_CLIPS names no folder"), name + ".mp4");
        screen = Grab();
        pointer = new Avalonia.Point(screen.Width * 0.62, screen.Height * 0.58);
        writer = new VideoWriter(path, FourCC.FromString("H264"), Fps, new OpenCvSharp.Size(screen.Width, screen.Height));
        if (!writer.IsOpened())
        {
            throw new InvalidOperationException("Windows' H.264 encoder could not be opened for " + path);
        }

        Hold(1);
    }

    /// <summary>The frames of <paramref name="seconds"/>, the screen as it is.</summary>
    public void Hold(double seconds)
    {
        for (int i = 0, n = (int)Math.Round(seconds * Fps); i < n; i++)
        {
            Write();
        }
    }

    /// <summary>Draws the window again, after something on it changed.</summary>
    public void Redraw()
    {
        screen.Dispose();
        screen = Grab();
    }

    /// <summary>Does <paramref name="change"/>, draws the window again and holds it for <paramref name="seconds"/>.</summary>
    public void Act(Action change, double seconds = 0.6)
    {
        change();
        Settle();
        Redraw();
        Hold(seconds);
    }

    /// <summary>The pointer moved slowly and smoothly to <paramref name="to"/>, eased at both ends.</summary>
    public void Glide(Avalonia.Point to, double seconds = 0.9)
    {
        var from = pointer;
        int n = Math.Max(1, (int)Math.Round(seconds * Fps));
        for (int i = 1; i <= n; i++)
        {
            double t = i / (double)n, eased = t * t * (3 - (2 * t));
            pointer = new Avalonia.Point(from.X + ((to.X - from.X) * eased), from.Y + ((to.Y - from.Y) * eased));
            Write();
        }
    }

    /// <summary>Holds for <paramref name="seconds"/>, drawing the window again a few times a second, for work that finishes in the background.</summary>
    public void Watch(double seconds)
    {
        for (int i = 0, n = (int)Math.Ceiling(seconds / 0.25); i < n; i++)
        {
            Thread.Sleep(120);
            Settle();
            Redraw();
            Hold(0.25);
        }
    }

    /// <summary>Glides to the middle of <paramref name="control"/>, presses it, and holds what it opened.</summary>
    public void Click(Control control, double hold = 0.8)
    {
        var at = Middle(control);
        if (!touches)
        {
            Glide(at);
            if (top is Avalonia.Controls.Window w)
            {
                w.MouseMove(at);
            }

            Hold(0.15);
        }

        Press(at);
        Hold(hold);
    }

    /// <summary>A press where the pointer or finger is: on the phone, a ring where it lands.</summary>
    public void Press(Avalonia.Point at)
    {
        pointer = at;
        if (touches)
        {
            touch = at;
            touchFrames = (int)(0.45 * Fps);
            Hold(0.2);
        }

        if (top is Avalonia.Controls.Window window)
        {
            window.MouseDown(at, MouseButton.Left);
            Settle();
            window.MouseUp(at, MouseButton.Left);
        }

        Settle();
        Redraw();
    }

    /// <summary>Clicks into <paramref name="box"/> and types <paramref name="text"/> a letter at a time, as a person types.</summary>
    public void Type(TextBox box, string text, double perLetter = 0.07)
    {
        Click(box, hold: 0.2);
        box.Text = "";
        foreach (char c in text)
        {
            box.Text += c;
            box.CaretIndex = box.Text.Length;
            Settle();
            Redraw();
            Hold(perLetter);
        }

        Hold(0.3);
    }

    /// <summary>Hovers the pointer over <paramref name="control"/> and draws what the hover shows.</summary>
    public void Hover(Control control, double hold = 1.5)
    {
        var at = Middle(control);
        Glide(at);
        if (top is Avalonia.Controls.Window window)
        {
            window.MouseMove(at);
        }

        Settle();
        Redraw();
        Hold(hold);
    }

    /// <summary>The middle of a control, in the window's own coordinates.</summary>
    public Avalonia.Point Middle(Control control)
    {
        var middle = new Avalonia.Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
        return control.TranslatePoint(middle, top) ?? throw new InvalidOperationException($"{control.GetType().Name} is not in the window");
    }

    public static void Settle()
    {
        for (int i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Dispatcher.UIThread.RunJobs();
    }

    private Mat Grab()
    {
        Settle();
        using var frame = (top as Avalonia.Controls.Window)?.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing was drawn");
        using var locked = frame.Lock();
        using var bgra = Mat.FromPixelData(locked.Size.Height, locked.Size.Width, MatType.CV_8UC4, locked.Address, locked.RowBytes);
        var bgr = new Mat();
        Cv2.CvtColor(bgra, bgr, locked.Format == Avalonia.Platform.PixelFormat.Rgba8888 ? ColorConversionCodes.RGBA2BGR : ColorConversionCodes.BGRA2BGR);
        // H.264 wants even sides.
        if (bgr.Width % 2 == 1 || bgr.Height % 2 == 1)
        {
            var even = new Mat(bgr, new OpenCvSharp.Rect(0, 0, bgr.Width & ~1, bgr.Height & ~1)).Clone();
            bgr.Dispose();
            bgr = even;
        }

        if (screen is { } before && !before.IsDisposed && (before.Width != bgr.Width || before.Height != bgr.Height))
        {
            Cv2.Resize(bgr, bgr, before.Size(), 0, 0, InterpolationFlags.Area);
        }

        return bgr;
    }

    private void Write()
    {
        using var frame = screen.Clone();
        if (touches)
        {
            if (touch is { } t && touchFrames > 0)
            {
                // A soft ring that widens and fades, as Android's "show taps" draws a touch.
                double life = 1 - (touchFrames / (0.45 * Fps));
                int radius = (int)(28 + (22 * life));
                using var overlay = frame.Clone();
                Cv2.Circle(overlay, new OpenCvSharp.Point(t.X, t.Y), radius, new Scalar(235, 235, 235), -1, LineTypes.AntiAlias);
                Cv2.Circle(overlay, new OpenCvSharp.Point(t.X, t.Y), radius, new Scalar(90, 90, 90), 3, LineTypes.AntiAlias);
                Cv2.AddWeighted(overlay, 0.55 * (1 - (life * 0.7)), frame, 1 - (0.55 * (1 - (life * 0.7))), 0, frame);
                touchFrames--;
            }
        }
        else
        {
            DrawPointer(frame, pointer);
        }

        writer.Write(frame);
        frames++;
    }

    /// <summary>An arrow pointer, white with a dark edge, its tip at <paramref name="at"/>, sized for the frame.</summary>
    private static void DrawPointer(Mat frame, Avalonia.Point at)
    {
        double s = Math.Max(1, frame.Height / 900.0);
        var shape = new[] { (0.0, 0.0), (0.0, 17.0), (4.2, 13.2), (7.0, 19.5), (9.6, 18.4), (6.9, 12.3), (12.2, 12.3) }
            .Select(p => new OpenCvSharp.Point(at.X + (p.Item1 * s), at.Y + (p.Item2 * s))).ToArray();
        Cv2.FillPoly(frame, new[] { shape }, Scalar.White, LineTypes.AntiAlias);
        Cv2.Polylines(frame, new[] { shape }, true, new Scalar(25, 25, 25), Math.Max(1, (int)Math.Round(1.2 * s)), LineTypes.AntiAlias);
    }

    public void Dispose()
    {
        Hold(1);
        // The last frame, beside the clip, for checking what it shows without a player.
        Cv2.ImWrite(System.IO.Path.ChangeExtension(path, ".png"), screen);
        writer.Release();
        writer.Dispose();
        screen.Dispose();
        Manifest();
    }

    /// <summary>
    /// The clip opened again to see that it plays, and its line in <c>clips.csv</c>: file, video and chapter, what it shows, length, size and
    /// the build it came from, replacing any line an earlier run wrote for it.
    /// </summary>
    private void Manifest()
    {
        using var played = new VideoCapture(path);
        int count = played.IsOpened() ? (int)played.Get(VideoCaptureProperties.FrameCount) : 0;
        int width = (int)played.Get(VideoCaptureProperties.FrameWidth), height = (int)played.Get(VideoCaptureProperties.FrameHeight);
        if (count < Fps)
        {
            throw new InvalidOperationException($"{name}.mp4 does not play back: {count} frames read");
        }

        string[] parts = name.Split('-');
        string video = parts[0], chapter = parts.Length > 1 && (parts[1].StartsWith("ch", StringComparison.Ordinal) || parts[1].StartsWith("step", StringComparison.Ordinal) || parts[1] == "help") ? parts[1] : "";
        static string Quoted(string s) => "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        string line = string.Join(',', name + ".mp4", video, chapter, Quoted(shows),
            (count / (double)Fps).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s", $"{width}x{height}", Quoted(GroupLab.App.Diagnostics.AppInfo.Version));
        string csv = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "clips.csv");
        lock (typeof(ScreenClip))
        {
            var lines = File.Exists(csv) ? File.ReadAllLines(csv).ToList() : ["file,video,chapter,shows,length,resolution,build"];
            lines.RemoveAll(l => l.StartsWith(name + ".mp4,", StringComparison.Ordinal));
            lines.Add(line);
            File.WriteAllLines(csv, [lines[0], .. lines.Skip(1).Order(StringComparer.Ordinal)]);
        }
    }
}
