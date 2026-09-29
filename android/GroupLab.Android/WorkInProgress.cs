using Avalonia.Controls;

namespace GroupLab.Android;

/// <summary>
/// What the person is in the middle of, NOTES-FROM-PLANNING.md entry 288: the updater never replaces GroupLab while the camera is open, an
/// analysis is running or a change is unsaved, because installing closes the application and the work would go with it. Kept in every
/// build, updater or not, so the rule reads the same facts wherever it is asked.
/// </summary>
internal static class WorkInProgress
{
    private static int analysing;
    private static int unsaved;

    /// <summary>Whether GroupLab is on screen: between the activity's start and its stop.</summary>
    internal static bool OnScreen { get; set; }

    internal static bool CameraOpen => CameraSession.Active is not null;

    internal static bool Analysing => Volatile.Read(ref analysing) > 0;

    internal static bool Unsaved => Volatile.Read(ref unsaved) > 0;

    /// <summary>Counts an analysis while it runs.</summary>
    internal static IDisposable Analysis()
    {
        Interlocked.Increment(ref analysing);
        return new Release(() => Interlocked.Decrement(ref analysing));
    }

    /// <summary>
    /// Marks a page whose work is kept only when the person finishes it (marking by hand, a CSV being imported) as unsaved for as long as
    /// it is on screen.
    /// </summary>
    internal static void HoldWhileShown(Control page)
    {
        page.AttachedToVisualTree += (_, _) => Interlocked.Increment(ref unsaved);
        page.DetachedFromVisualTree += (_, _) => Interlocked.Decrement(ref unsaved);
    }

    private sealed class Release(Action release) : IDisposable
    {
        private Action? _release = release;

        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
