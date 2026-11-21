using Avalonia.Controls;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 507 section 2: start-up behind the splash, tied to real progress and never to a clock. The work begun
/// beside Avalonia (<see cref="StartupWork"/>) finishes the first steps, building the main window is the last, and the main window is
/// shown the moment it is built: the splash's movement is never waited for, and work already done when the splash first draws plays at
/// once. Between steps it waits for one frame only, so the shots already dropped reach the render thread before the window's thread is
/// busy building, and never longer than <see cref="FrameWaitLimit"/> if no frame comes.
/// </summary>
internal static class StartupSequence
{
    /// <summary>The longest a wait for a frame lasts, for a splash that is not drawn (hidden, or a platform that gives no frames).</summary>
    internal static readonly TimeSpan FrameWaitLimit = TimeSpan.FromMilliseconds(100);

    /// <summary>When each part of start-up ended, in milliseconds from the process starting, for the log's <c>app.startup</c> line.</summary>
    internal static List<(string Part, double Ms)> Marks { get; } = [];

    /// <summary>Notes the time now under <paramref name="part"/>.</summary>
    internal static void Mark(string part) => Marks.Add((part, Math.Round((DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds)));

    /// <summary>
    /// Runs start-up: each of <paramref name="background"/> finishing is one step, <paramref name="build"/> the last, and
    /// <paramref name="show"/> is called with the built window at once. A failure to build closes the splash and is thrown, as it was
    /// thrown before there was a splash.
    /// </summary>
    internal static async Task<T> RunAsync<T>(StartupSplash splash, IReadOnlyList<Task> background, Func<T> build, Func<Task> nextFrame, Action<T> show)
        where T : Window
    {
        ArgumentNullException.ThrowIfNull(splash);
        ArgumentNullException.ThrowIfNull(background);
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(nextFrame);
        ArgumentNullException.ThrowIfNull(show);
        if (background.Count + 1 != StartupSplash.Steps.Length)
        {
            throw new ArgumentException($"start-up has {StartupSplash.Steps.Length} steps and {background.Count + 1} were given", nameof(background));
        }

        await nextFrame();
        splash.Drawn();
        Mark("drawn");
        foreach (var work in background)
        {
            // A failure here is the main window's to report, as it always was: the step is over either way.
            await Task.WhenAny(work);
            splash.StepDone();
        }

        Mark("worked");
        await nextFrame();
        Mark("frame");
        T main;
        try
        {
            main = build();
        }
        catch
        {
            splash.Close();
            throw;
        }

        Mark("built");
        splash.StepDone();
        await nextFrame();
        Mark("frame2");
        main.Opened += (_, _) => splash.CloseSoon();
        show(main);
        return main;
    }

    /// <summary>The next frame of <paramref name="top"/>, or <see cref="FrameWaitLimit"/>, whichever comes first.</summary>
    internal static Task NextFrame(TopLevel top)
    {
        ArgumentNullException.ThrowIfNull(top);
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        top.RequestAnimationFrame(_ => frame.TrySetResult());
        return Task.WhenAny(frame.Task, Task.Delay(FrameWaitLimit));
    }
}
