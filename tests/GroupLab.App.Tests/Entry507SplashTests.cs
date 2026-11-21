using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GroupLab.App;
using GroupLab.App.Theme;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 507 section 2: the splash on the computer, concept C, animated by real progress. Each step finishing adds
/// a shot and the last fills the group; start-up never waits for it; with less motion asked for it shows the finished picture and only
/// the step lines change; its version line is the version in words; and the work it shows hands the main window what it opened, or the
/// failure the window always reported.
/// </summary>
public sealed class Entry507SplashTests : IDisposable
{
    private readonly string folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-507-{Guid.NewGuid():N}")).FullName;

    public void Dispose()
    {
        StartupWork.Current = null;
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(folder, recursive: true);
    }

    private static StartupSplash Shown(bool still = false)
    {
        var splash = new StartupSplash(StartupSplash.VersionLine("1.0.0-nightly.236"), still);
        splash.Show();
        Dispatcher.UIThread.RunJobs();
        return splash;
    }

    [AvaloniaFact]
    public void EachStepAddsAShotAndTheLastFillsTheGroup()
    {
        var splash = Shown();
        Assert.Equal(3, StartupSplash.Steps.Length);
        Assert.Equal(0, splash.ShotsShowing);
        Assert.False(splash.Finished);
        Assert.Equal(["Opening your records", "", ""], splash.LineTexts);

        splash.StepDone();
        Assert.Equal(1, splash.ShotsShowing);
        Assert.Equal(["Opening your records, done", "Loading the target library", ""], splash.LineTexts);
        splash.StepDone();
        Assert.Equal(2, splash.ShotsShowing);
        Assert.False(splash.Finished);
        splash.StepDone();
        Assert.Equal(5, splash.ShotsShowing);
        Assert.True(splash.Finished);
        Assert.Equal(["Opening your records, done", "Loading the target library, done", "Getting the screens ready, done"], splash.LineTexts);

        // A step reported twice changes nothing.
        splash.StepDone();
        Assert.Equal(3, splash.Done);
        splash.Close();
    }

    [Theory]
    [InlineData(3, new[] { 0, 1, 2, 5 })]
    [InlineData(4, new[] { 0, 1, 2, 3, 5 })]
    [InlineData(5, new[] { 0, 1, 2, 3, 4, 5 })]
    public void TheShotCountFollowsTheStepsWhateverTheirNumber(int steps, int[] shots)
    {
        Assert.Equal(shots, Enumerable.Range(0, steps + 1).Select(done => StartupSplash.ShotsAfter(done, steps)));
    }

    [AvaloniaFact]
    public void LessMotionShowsTheFinishedPictureAndOnlyTheLinesChange()
    {
        var splash = Shown(still: true);
        Assert.Equal(5, splash.ShotsShowing);
        Assert.True(splash.Finished);
        splash.StepDone();
        Assert.Equal(5, splash.ShotsShowing);
        Assert.Equal(["Opening your records, done", "Loading the target library", ""], splash.LineTexts);
        splash.Close();
    }

    [Fact]
    public void TheVersionLineIsTheVersionInWords()
    {
        Assert.Equal("GroupLab 1.0, build 236", StartupSplash.VersionLine("1.0.0-nightly.236"));
        Assert.Equal("GroupLab 1.0", StartupSplash.VersionLine("1.0.0"));
        Assert.Equal("GroupLab 1.0.1", StartupSplash.VersionLine("1.0.1"));
    }

    /// <summary>Work already done when the splash draws plays at once: nothing waits on a timer, and the window is shown the moment it is built.</summary>
    [AvaloniaFact]
    public void QuickStartUpAddsNoWait()
    {
        Assert.True(StartupSplash.MinimumShowing <= TimeSpan.FromMilliseconds(400));
        var splash = Shown();
        var built = new Window();
        int shown = 0;
        var run = StartupSequence.RunAsync(splash, [Task.CompletedTask, Task.FromException(new IOException("not here"))], () => built, () => Task.CompletedTask, w =>
        {
            Assert.Same(built, w);
            shown++;
        });

        // Finished in the same call: no frame, timer or animation was waited for.
        Assert.True(run.IsCompletedSuccessfully);
        Assert.Equal(1, shown);
        Assert.Equal(3, splash.Done);
        Assert.True(splash.Finished);
        splash.Close();
    }

    /// <summary>The window is built only once the work before it has finished: the steps are real progress, not a clock.</summary>
    [AvaloniaFact]
    public void TheWindowWaitsForTheWorkNotForTheSplash()
    {
        var splash = Shown();
        var records = new TaskCompletionSource();
        bool built = false;
        var run = StartupSequence.RunAsync(splash, [records.Task, Task.CompletedTask], () =>
        {
            built = true;
            return new Window();
        }, () => Task.CompletedTask, _ => { });
        Dispatcher.UIThread.RunJobs();
        Assert.False(built);
        Assert.Equal(0, splash.Done);

        records.SetResult();
        Dispatcher.UIThread.RunJobs();
        Assert.True(built);
        Assert.True(run.IsCompletedSuccessfully);
        splash.Close();
    }

    [AvaloniaFact]
    public void AWindowThatCannotBeBuiltClosesTheSplashAndIsThrown()
    {
        var splash = Shown();
        bool closed = false;
        splash.Closed += (_, _) => closed = true;
        var run = StartupSequence.RunAsync<Window>(splash, [Task.CompletedTask, Task.CompletedTask], () => throw new InvalidOperationException("broken"), () => Task.CompletedTask, _ => { });
        Dispatcher.UIThread.RunJobs();
        Assert.True(closed);
        Assert.IsType<InvalidOperationException>(run.Exception?.InnerException);
    }

    [AvaloniaFact]
    public void TheSplashHasNoTaskbarEntryNoCloseButtonAndTakesNoFocus()
    {
        var splash = new StartupSplash("GroupLab 1.0", still: false);
        Assert.False(splash.ShowInTaskbar);
        Assert.False(splash.ShowActivated);
        Assert.False(splash.CanResize);
        Assert.Equal(WindowDecorations.None, splash.WindowDecorations);
        Assert.Equal(WindowStartupLocation.CenterScreen, splash.WindowStartupLocation);
        Assert.Equal((600, 360), (splash.Width, splash.Height));
    }

    /// <summary>The drawn picture: the shots in amber where the canvas puts them, the center in teal, on GroupLab's dark ground.</summary>
    [AvaloniaFact]
    public void TheFinishedPictureIsDrawnWhereTheCanvasHasIt()
    {
        var splash = Shown(still: true);
        var content = (Control)splash.Content!;
        using var bitmap = new RenderTargetBitmap(new PixelSize(600, 360));
        bitmap.Render(content);
        Color At(Point box)
        {
            int x = (int)Math.Round(282 + (box.X * 3)), y = (int)Math.Round(30 + (box.Y * 3));
            var buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(4);
            try
            {
                bitmap.CopyPixels(new PixelRect(x, y, 1, 1), buffer, 4, 4);
                byte[] px = new byte[4];
                System.Runtime.InteropServices.Marshal.Copy(buffer, px, 0, 4);
                // The bitmap's own byte order: BGRA on Windows and Linux, RGBA on macOS (Entry204Tests learned it first; this test on 7474fc9e again).
                return bitmap.Format == Avalonia.Platform.PixelFormat.Rgba8888 ? Color.FromRgb(px[0], px[1], px[2]) : Color.FromRgb(px[2], px[1], px[0]);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }

        foreach (var shot in StartupSplash.Shots.Where(s => s != StartupSplash.Shots[0]))
        {
            Assert.Equal(Tokens.Dark.Amber, At(shot + new Point(0, 2)));
        }

        Assert.Equal(Tokens.Dark.Teal, At(StartupSplash.Center + new Point(1, 0)));
        Assert.Equal(Tokens.Dark.Bg, At(new Point(21, 22)));
        splash.Close();
    }

    /// <summary>The work begun beside Avalonia is handed to the first window over the same settings, once, and to no other.</summary>
    [AvaloniaFact]
    public void TheRecordsOpenedAtStartUpAreTheWindowsOnce()
    {
        var settings = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        StartupWork.Current = StartupWork.Begin(settings);
        Assert.Null(StartupWork.TakeRecords(new AppSettingsStore(Path.Combine(folder, "other", "settings.json"))));
        var opened = StartupWork.TakeRecords(settings);
        Assert.NotNull(opened);
        Assert.Null(StartupWork.TakeRecords(settings));
        StartupWork.Current.Library.Wait();
        Assert.NotEmpty(StartupWork.TakeLibrary(settings)!);
    }

    /// <summary>A database that cannot be opened is reported by the window exactly as it was before the work moved off its thread.</summary>
    [AvaloniaFact]
    public void ARecordsFailureAtStartUpIsTheWindowsToReport()
    {
        var settings = new AppSettingsStore(Path.Combine(folder, "settings.json"));
        Directory.CreateDirectory(settings.DatabasePath);
        StartupWork.Current = StartupWork.Begin(settings);
        var window = new MainWindow(settings);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        Assert.Null(window.Sessions);
        window.Close();
    }

    /// <summary>The phones keep their system start screens, on GroupLab's dark ground whatever the phone's own light or dark setting.</summary>
    [Fact]
    public void ThePhonesStartScreensStandOnTheDarkGround()
    {
        string root = Entry109Tests.Repository();
        string android = File.ReadAllText(Path.Combine(root, "android", "GroupLab.Android", "Resources", "values-v31", "styles.xml"));
        Assert.Contains("<item name=\"android:windowSplashScreenBackground\">@color/ic_launcher_background</item>", android, StringComparison.Ordinal);
        Assert.Contains("#131417", File.ReadAllText(Path.Combine(root, "android", "GroupLab.Android", "Resources", "values", "ic_launcher_background.xml")), StringComparison.Ordinal);
        foreach (string plist in new[] { "Info.plist", "Info.dev.plist" })
        {
            string text = File.ReadAllText(Path.Combine(root, "ios", "GroupLab.iOS", plist));
            int screen = text.IndexOf("<key>UILaunchScreen</key>", StringComparison.Ordinal);
            Assert.True(screen >= 0, plist);
            string launch = text[screen..text.IndexOf("</dict>", screen, StringComparison.Ordinal)];
            Assert.Contains("<key>UIColorName</key>", launch, StringComparison.Ordinal);
            Assert.Contains("<string>LaunchBackground</string>", launch, StringComparison.Ordinal);
        }

        string color = File.ReadAllText(Path.Combine(root, "ios", "GroupLab.iOS", "Assets.xcassets", "LaunchBackground.colorset", "Contents.json"));
        Assert.Contains("\"red\" : \"0x13\"", color, StringComparison.Ordinal);
        Assert.Contains("\"green\" : \"0x14\"", color, StringComparison.Ordinal);
        Assert.Contains("\"blue\" : \"0x17\"", color, StringComparison.Ordinal);
    }
}
