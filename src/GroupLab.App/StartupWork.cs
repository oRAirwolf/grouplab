using GroupLab.App.Diagnostics;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Rendering;

namespace GroupLab.App;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 507 section 2: the start-up work that needs no window, begun in <c>Main</c> before Avalonia starts and
/// run beside it, so the splash's first two steps are real work and the main window finds them done instead of doing them itself.
/// Opening the records took about 70 ms and the target library's files are read from disk; both used to run inside the window's
/// constructor, on the window's thread, one after the other.
/// </summary>
internal sealed class StartupWork
{
    private readonly string databasePath;
    private readonly string recordsPath;
    private readonly string sheetsFolder;
    private int recordsTaken;

    private StartupWork(AppSettingsStore settings, Func<bool> letterPaper)
    {
        databasePath = settings.DatabasePath;
        recordsPath = settings.RecordsPath;
        sheetsFolder = settings.SheetsFolder;
        Records = Task.Run(() => Timed("records", () =>
        {
            var store = SessionStore.Open(databasePath, recordsPath);
            return new OpenedRecords(store, store.LoadBook());
        }));
        Library = Task.Run(() => Timed("library", () => (IReadOnlyList<LibrarySheet>)[.. GroupLab.Core.Rendering.TargetLibrary.Load(
            Path.Combine(AppContext.BaseDirectory, "targets"), letterPaper())]));

        // A failure is the main window's to report, as it always was; this only keeps it from also being an unobserved task's error.
        _ = Records.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        _ = Library.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>The work begun for this run, or none (the tests, and every window after the first).</summary>
    internal static StartupWork? Current { get; set; }

    /// <summary>The records as the window's constructor used to open them, or the exception it would have met.</summary>
    internal Task<OpenedRecords> Records { get; }

    /// <summary>The built-in target sheets, as the library screen reads them.</summary>
    internal Task<IReadOnlyList<LibrarySheet>> Library { get; }

    /// <summary>Begins the work for the settings given, on the thread pool.</summary>
    internal static StartupWork Begin(AppSettingsStore settings) => new(settings, () => settings.LoadPaperLetter() ?? AppSettingsStore.LetterPaper());

    /// <summary>
    /// The opened records for a window built over <paramref name="settings"/>, waiting for them if they are not ready; null where nothing
    /// was begun for those settings, or they were already taken, so that window opens its own as before. A failure is thrown here, as
    /// the window's own open would have thrown it.
    /// </summary>
    internal static OpenedRecords? TakeRecords(AppSettingsStore settings) =>
        Current is { } work && work.For(settings) && Interlocked.Exchange(ref work.recordsTaken, 1) == 0 ? work.Records.GetAwaiter().GetResult() : null;

    /// <summary>The built-in sheets read already for these settings, or null where they were not, or could not be, read.</summary>
    internal static IReadOnlyList<LibrarySheet>? TakeLibrary(AppSettingsStore settings) =>
        Current is { } work && work.For(settings) && work.Library.IsCompletedSuccessfully ? work.Library.Result : null;

    private bool For(AppSettingsStore settings) => settings.DatabasePath == databasePath && settings.SheetsFolder == sheetsFolder;

    private static T Timed<T>(string what, Func<T> work)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var result = work();
        DiagnosticLog.Info("app.startup.work", ("what", what), ("ms", Math.Round(clock.Elapsed.TotalMilliseconds)));
        return result;
    }

    /// <summary>What the window's constructor opened for itself before entry 507.</summary>
    internal sealed record OpenedRecords(SessionStore Store, RecordBook Book);
}
