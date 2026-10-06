using Avalonia.LogicalTree;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 313 section 1: on the iPad mini reading stayed on "Reading the sheet's codes" and Cancel did nothing, then
/// threw because what it pressed had been disposed. Cancel always works, at once, from any stage and after the reading has ended; a reading
/// has a time limit that the time the screen was locked does not count towards; and every error ends the reading with a message.
/// </summary>
public class Entry313Tests
{
    private static readonly TestPhone ThePhone = new();

    [Fact]
    public async Task CancelReturnsAtOnceWhileTheReadingIsStuckInOneLongStep()
    {
        using var release = new ManualResetEventSlim();
        var reading = new Reading("test");
        bool cleaned = false;
        var run = reading.Run(_ =>
        {
            release.Wait(TimeSpan.FromSeconds(30)); // a native step no cancellation can interrupt
            return 1;
        }, late: _ => cleaned = true);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var clock = Stopwatch.StartNew();
        reading.Stop();
        var outcome = await run;
        Assert.Equal(ReadEnd.Canceled, outcome.End);
        Assert.True(clock.ElapsedMilliseconds < 1000, $"{clock.ElapsedMilliseconds} ms");
        Assert.False(cleaned);
        release.Set();
        for (int i = 0; i < 100 && !cleaned; i++)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }

        Assert.True(cleaned, "what the stopped reading made is cleaned up when it ends");
    }

    [Fact]
    public async Task CancelNeverThrowsHoweverOftenOrLateItIsPressed()
    {
        var reading = new Reading("test");
        var outcome = await reading.Run(_ => 7);
        Assert.Equal(ReadEnd.Done, outcome.End);
        Assert.Equal(7, outcome.Value);
        reading.Stop();
        reading.Stop();
        reading.Stop();
        Assert.True(reading.Stopping);
    }

    [Fact]
    public async Task AReadingPastItsLimitIsStopped()
    {
        var reading = new Reading("test");
        var outcome = await reading.Run(token =>
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(10);
            }

#pragma warning disable CS0162
            return 0;
#pragma warning restore CS0162
        }, TimeSpan.FromMilliseconds(600));
        Assert.Equal(ReadEnd.TimedOut, outcome.End);
        Assert.True(reading.Token.IsCancellationRequested);
    }

    [Fact]
    public async Task TimeTheScreenWasLockedIsNotCounted()
    {
        // The clock jumps two minutes once, as it does across a screen lock while the application is suspended.
        var real = Stopwatch.StartNew();
        var away = TimeSpan.Zero;
        var reading = new Reading("test", () => real.Elapsed + away);
        var outcome = await reading.Run(_ =>
        {
            Thread.Sleep(400);
            away = TimeSpan.FromMinutes(2);
            Thread.Sleep(800);
            return 3;
        }, TimeSpan.FromSeconds(5));
        Assert.Equal(ReadEnd.Done, outcome.End);
        Assert.Equal(3, outcome.Value);
    }

    [Fact]
    public async Task AnErrorNobodyExpectedEndsTheReadingWithIt()
    {
        var reading = new Reading("test");
        var outcome = await reading.Run<int>(_ => throw new ArgumentOutOfRangeException("index"));
        Assert.Equal(ReadEnd.Failed, outcome.End);
        Assert.IsType<ArgumentOutOfRangeException>(outcome.Error);
    }

    /// <summary>
    /// Entry 313 section 1.5 on every desktop: a large picture shared into Capture, Cancel pressed while it is being read, and the start
    /// showing again within a second with the picture kept, to read again or to choose its sheet.
    /// </summary>
    [AvaloniaFact]
    public void CancelOnCaptureGoesBackWithinASecondAndKeepsThePicture()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(ThePhone, Avalonia.Application.Current!, () => "US", null);
        }

        Phone.Settings.SaveShotSetup(".308", 3600);
        var window = new Window { Width = 412, Height = 915 };
        window.Show();
        var capture = new CapturePage();
        window.Content = capture;
        Dispatcher.UIThread.RunJobs();
        // Entry 376 item B3: the caliber is chosen for each target, never carried over, so it is typed into the page's box.
        capture.TypedCalibre = ".308";
        string sample = Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png");
        var handle = new PhotoHandle(null, null, null, new FileInfo(sample).Length, ".png", () => Task.FromResult<Stream?>(File.OpenRead(sample)));
        CapturePage.SharedPicture!([handle]);

        // Pressed as soon as the reading says what it is doing past loading the picture.
        TextBlock? line = null;
        Assert.True(Until(() => (line = capture.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text is { } w && w != GroupLab.Core.Trace.StageWords.Starting
            && GroupLab.Core.Trace.StageWords.During("S0.identify") == w)) is not null, TimeSpan.FromSeconds(60)), "the reading reached its codes");
        var cancel = capture.GetVisualDescendants().OfType<Button>().First(b => b.Content is TextBlock { Text: "Cancel" });
        var clock = Stopwatch.StartNew();
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.True(Until(() => capture.AtStart, TimeSpan.FromSeconds(5)), "back on the start");
        Assert.True(clock.ElapsedMilliseconds < 1000, $"back after {clock.ElapsedMilliseconds} ms");
        Assert.True(capture.KeepingPicture);
        cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // pressed again after it has gone: nothing, and nothing thrown
        var buttons = capture.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible).Select(Words).ToList();
        Assert.Contains("Read it again", buttons);
        Assert.Contains("Choose which sheet it is", buttons);
        Assert.Contains("Forget it", buttons);
        window.Close();
    }

    private static bool Until(Func<bool> condition, TimeSpan most)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < most)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
            {
                return true;
            }

            Thread.Sleep(5);
        }

        return false;
    }

    private static string Words(Button button) =>
        button.Content as string ?? string.Join(" ", button.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
}
