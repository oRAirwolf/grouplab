using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.Threading;
using GroupLab.App.Diagnostics;
using GroupLab.App.Theme;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 507 section 2, concept C of the splash canvas ("The group, measured"): a 600 by 360 window on the dark
/// ground with a faint 30 pixel grid, the mark and name with the version under them, a five shot group on a light crosshair and ring,
/// and the start-up steps at the bottom left. Each step that finishes drops one shot in; the last fills the group, draws its extreme
/// spread and shows the center and the label. The shots and the number are decoration, the same every time, never the person's own.
/// <para>The movement runs on the render thread (composition animations), so it carries on while the window's thread builds the
/// screens, and nothing here ever waits for it: the splash closes when the main window is ready, whatever has played.</para>
/// </summary>
internal sealed class StartupSplash : Window
{
    /// <summary>The real steps, in the order they finish (entry 507: at least three, at most five).</summary>
    internal static readonly string[] Steps = ["Opening your records", "Loading the target library", "Getting the screens ready"];

    /// <summary>The shots, in the canvas's 100 unit box, in the order they drop in.</summary>
    internal static readonly Point[] Shots = [new(47, 38), new(40, 47), new(55, 49), new(45, 56), new(58, 40)];

    /// <summary>The group's center, drawn as a small teal cross, in the same box.</summary>
    internal static readonly Point Center = new(49, 46);

    /// <summary>The label beside the group: a sample of a good group, never a measurement of anybody's.</summary>
    internal const string Label = "0.47 in";

    internal static readonly TimeSpan DropTime = TimeSpan.FromMilliseconds(150);
    internal static readonly TimeSpan SpreadTime = TimeSpan.FromMilliseconds(200);

    /// <summary>The shortest it shows, only so it does not flash; the main window is never held back for it (entry 507: at most 400).</summary>
    internal static readonly TimeSpan MinimumShowing = TimeSpan.FromMilliseconds(300);

    private const double Scale = 3;
    private const double GroupLeft = 600 - 18 - 300;
    private const double GroupTop = 30;
    private const double ShotRadius = 4 * Scale;

    private static readonly IBrush Ink = new SolidColorBrush(Tokens.Dark.Text);
    private static readonly IBrush Faint = new SolidColorBrush(Tokens.Dark.Faint);

    private readonly Ellipse[] shots;
    private readonly Control spread;
    private readonly Control spreadLine;
    private readonly Control centerCross;
    private readonly Border label;
    private readonly TextBlock[] lines;
    private readonly bool still;
    private int done;
    private DateTime? firstFrame;

    /// <param name="version">The version words, "GroupLab 1.0, build 236".</param>
    /// <param name="still">The system or Settings asks for less motion: the finished picture from the start, only the lines change.</param>
    internal StartupSplash(string version, bool still)
    {
        this.still = still;
        Title = "GroupLab is starting";
        Width = 600;
        Height = 360;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        WindowDecorations = WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Dark;
        Background = new SolidColorBrush(Tokens.Dark.Bg);
        Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri(MainWindow.IconUri)));

        var canvas = new Canvas { Width = 600, Height = 360 };
        canvas.Children.Add(new Ground());

        shots = [.. Shots.Select(p =>
        {
            var shot = new Ellipse { Width = 2 * ShotRadius, Height = 2 * ShotRadius, Fill = new SolidColorBrush(Tokens.Dark.Amber), IsVisible = still };
            Canvas.SetLeft(shot, GroupLeft + (p.X * Scale) - ShotRadius);
            Canvas.SetTop(shot, GroupTop + (p.Y * Scale) - ShotRadius);
            canvas.Children.Add(shot);
            return shot;
        })];

        // The extreme spread, between the two shots farthest apart, drawn across from one to the other: a bar in a turned holder, so the
        // bar's own scale can grow along its length.
        var (from, to) = Widest();
        var vector = (to - from) * Scale;
        double length = Math.Sqrt((vector.X * vector.X) + (vector.Y * vector.Y));
        spreadLine = new Rectangle { Width = length, Height = 1.5, Fill = Ink };
        spread = new Canvas { Width = length, Height = 1.5, Children = { spreadLine }, IsVisible = still, RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative), RenderTransform = new RotateTransform(Math.Atan2(vector.Y, vector.X) * 180 / Math.PI) };
        Canvas.SetLeft(spread, GroupLeft + (from.X * Scale));
        Canvas.SetTop(spread, GroupTop + (from.Y * Scale) - 0.75);
        canvas.Children.Add(spread);

        centerCross = new Cross { IsVisible = still };
        Canvas.SetLeft(centerCross, GroupLeft + (Center.X * Scale) - Cross.Arm);
        Canvas.SetTop(centerCross, GroupTop + (Center.Y * Scale) - Cross.Arm);
        canvas.Children.Add(centerCross);

        label = new Border
        {
            Background = new SolidColorBrush(Tokens.Dark.Panel),
            BorderBrush = new SolidColorBrush(Tokens.Dark.Line),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(7, 3),
            Child = new TextBlock { Text = Label, FontFamily = Tokens.Mono, FontSize = Tokens.DetailSize, Foreground = Ink },
            IsVisible = still,
        };
        Canvas.SetRight(label, 54);
        Canvas.SetTop(label, 92);
        canvas.Children.Add(label);

        // The mark and name are the brand's own lockup, the same art as the header's, at the canvas's 40 pixels.
        var name = new BrandMark { Lockup = true, Height = 40, HorizontalAlignment = HorizontalAlignment.Left };
        var top = new StackPanel { Spacing = Tokens.Space8, Children = { name, new TextBlock { Text = version, FontFamily = Tokens.Mono, FontSize = Tokens.DetailSize, Foreground = Faint } } };
        Canvas.SetLeft(top, 30);
        Canvas.SetTop(top, 34);
        canvas.Children.Add(top);

        lines = [.. Steps.Select(_ => new TextBlock { FontFamily = Tokens.Mono, FontSize = Tokens.DetailSize })];
        var steps = new StackPanel { Spacing = Tokens.Space4 };
        foreach (var line in lines)
        {
            steps.Children.Add(line);
        }

        Canvas.SetLeft(steps, 30);
        Canvas.SetBottom(steps, 26);
        canvas.Children.Add(steps);
        Content = canvas;
        ShowLines();
    }

    /// <summary>The version line under the name: "GroupLab 1.0, build 236" for 1.0.0-nightly.236.</summary>
    internal static string VersionLine(string? version) => "GroupLab " + GroupLab.Core.Updates.SemanticVersion.WordsFor(version);

    /// <summary>How many shots a group shows once <paramref name="finished"/> of <paramref name="steps"/> steps are done: one a step, the last filling the rest.</summary>
    internal static int ShotsAfter(int finished, int steps) => finished >= steps ? Shots.Length : Math.Clamp(finished, 0, Shots.Length);

    /// <summary>The steps finished so far.</summary>
    internal int Done => done;

    /// <summary>The shots showing now.</summary>
    internal int ShotsShowing => shots.Count(s => s.IsVisible);

    /// <summary>Whether the spread, the center and the label are showing: the finished picture.</summary>
    internal bool Finished => spread.IsVisible && centerCross.IsVisible && label.IsVisible;

    /// <summary>Each step's line as it reads now, the empty ones not yet begun.</summary>
    internal IReadOnlyList<string> LineTexts => [.. lines.Select(l => l.IsVisible ? l.Text ?? "" : "")];

    /// <summary>The time of the splash's first frame, once it has one.</summary>
    internal DateTime? FirstFrame => firstFrame;

    /// <summary>Marks the splash as drawn, for the shortest showing time.</summary>
    internal void Drawn() => firstFrame ??= DateTime.UtcNow;

    /// <summary>One more step has finished: its line is dimmed and done, the next one lit, and its shot drops in. Never waits.</summary>
    internal void StepDone()
    {
        if (done >= Steps.Length)
        {
            return;
        }

        int before = ShotsAfter(done, Steps.Length);
        done++;
        int after = ShotsAfter(done, Steps.Length);
        ShowLines();
        if (still)
        {
            return;
        }

        for (int i = before; i < after; i++)
        {
            Appear(shots[i], TimeSpan.FromMilliseconds(60 * (i - before)), grow: true);
        }

        if (done == Steps.Length)
        {
            var settled = TimeSpan.FromMilliseconds(60 * (after - before - 1)) + DropTime;
            spread.IsVisible = true;
            DrawAcross(spreadLine, settled);
            Appear(centerCross, settled + SpreadTime, grow: false);
            Appear(label, settled + SpreadTime, grow: false);
        }
    }

    /// <summary>Closes the splash now, or once it has shown for <see cref="MinimumShowing"/>; the main window is already up either way.</summary>
    internal void CloseSoon()
    {
        var left = firstFrame is { } shown ? MinimumShowing - (DateTime.UtcNow - shown) : TimeSpan.Zero;
        if (left <= TimeSpan.Zero)
        {
            Close();
            return;
        }

        DispatcherTimer.RunOnce(Close, left);
    }

    private void ShowLines()
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].IsVisible = i <= done;
            lines[i].Text = Steps[i] + (i < done ? ", done" : "");
            lines[i].Foreground = i < done ? Faint : Ink;
        }
    }

    /// <summary>Shows a control at once, and where the render thread can be reached, fades it in, a shot dropping from twice its size.</summary>
    private static void Appear(Control control, TimeSpan delay, bool grow)
    {
        control.IsVisible = true;
        if (ElementComposition.GetElementVisual(control) is not { } visual)
        {
            return;
        }

        var compositor = visual.Compositor;
        var fade = compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f);
        fade.Duration = DropTime;
        fade.DelayTime = delay;
        fade.DelayBehavior = Avalonia.Rendering.Composition.Animations.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation("Opacity", fade);
        if (grow)
        {
            visual.CenterPoint = new Vector3D(ShotRadius, ShotRadius, 0);
            var drop = compositor.CreateVector3DKeyFrameAnimation();
            drop.InsertKeyFrame(0f, new Vector3D(2, 2, 1));
            drop.InsertKeyFrame(1f, new Vector3D(1, 1, 1), new Avalonia.Animation.Easings.CubicEaseOut());
            drop.Duration = DropTime;
            drop.DelayTime = delay;
            drop.DelayBehavior = Avalonia.Rendering.Composition.Animations.AnimationDelayBehavior.SetInitialValueBeforeDelay;
            visual.StartAnimation("Scale", drop);
        }
    }

    /// <summary>Grows the spread's bar from its first shot to its second.</summary>
    private static void DrawAcross(Control bar, TimeSpan delay)
    {
        if (ElementComposition.GetElementVisual(bar) is not { } visual)
        {
            return;
        }

        visual.CenterPoint = new Vector3D(0, 0, 0);
        var across = visual.Compositor.CreateVector3DKeyFrameAnimation();
        across.InsertKeyFrame(0f, new Vector3D(0, 1, 1));
        across.InsertKeyFrame(1f, new Vector3D(1, 1, 1), new Avalonia.Animation.Easings.CubicEaseInOut());
        across.Duration = SpreadTime;
        across.DelayTime = delay;
        across.DelayBehavior = Avalonia.Rendering.Composition.Animations.AnimationDelayBehavior.SetInitialValueBeforeDelay;
        visual.StartAnimation("Scale", across);
    }

    /// <summary>The two shots farthest apart, which the extreme spread joins.</summary>
    private static (Point From, Point To) Widest()
    {
        var pairs = from a in Enumerable.Range(0, Shots.Length)
                    from b in Enumerable.Range(a + 1, Shots.Length - a - 1)
                    let d = Shots[a] - Shots[b]
                    orderby (d.X * d.X) + (d.Y * d.Y) descending
                    select (Shots[a], Shots[b]);
        var (p, q) = pairs.First();
        return p.X <= q.X ? (p, q) : (q, p);
    }

    /// <summary>The ground: the dark field, its faint 30 pixel grid and edge, and the group's crosshair and ring.</summary>
    private sealed class Ground : Control
    {
        public Ground()
        {
            Width = 600;
            Height = 360;
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(new SolidColorBrush(Tokens.Dark.Bg), new Rect(0, 0, 600, 360));
            var grid = new Pen(new SolidColorBrush(Tokens.SplashGrid), 1);
            for (double x = 0.5; x < 600; x += 30)
            {
                context.DrawLine(grid, new Point(x, 0), new Point(x, 360));
            }

            for (double y = 0.5; y < 360; y += 30)
            {
                context.DrawLine(grid, new Point(0, y), new Point(600, y));
            }

            context.DrawRectangle(null, new Pen(new SolidColorBrush(Tokens.Dark.Line), 1), new Rect(0.5, 0.5, 599, 359));
            var rings = new SolidColorBrush(Tokens.Dark.Line2);
            Point At(double x, double y) => new(GroupLeft + (x * Scale), GroupTop + (y * Scale));
            context.DrawLine(new Pen(rings, 0.4 * Scale), At(50, 6), At(50, 94));
            context.DrawLine(new Pen(rings, 0.4 * Scale), At(6, 50), At(94, 50));
            context.DrawEllipse(null, new Pen(rings, 0.6 * Scale), At(50, 50), 36 * Scale, 36 * Scale);
        }
    }

    /// <summary>The group's center: a small teal cross.</summary>
    private sealed class Cross : Control
    {
        internal const double Arm = 1.5 * Scale;

        public Cross()
        {
            Width = 2 * Arm;
            Height = 2 * Arm;
        }

        public override void Render(DrawingContext context)
        {
            var pen = new Pen(new SolidColorBrush(Tokens.Dark.Teal), 0.6 * Scale);
            context.DrawLine(pen, new Point(0, Arm), new Point(2 * Arm, Arm));
            context.DrawLine(pen, new Point(Arm, 0), new Point(Arm, 2 * Arm));
        }
    }
}
