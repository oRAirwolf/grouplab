using System.Diagnostics;
using GroupLab.App.Diagnostics;

namespace GroupLab.Mobile;

/// <summary>How a reading ended: its answer, Cancel pressed, the time limit reached, or an error, which is kept.</summary>
internal enum ReadEnd
{
    Done,
    Canceled,
    TimedOut,
    Failed,
}

/// <summary>A reading's end, with its answer where it has one and its error where it failed.</summary>
internal sealed record ReadOutcome<T>(ReadEnd End, T? Value, Exception? Error, TimeSpan Took);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 313 section 1: a picture read off the interface thread with a Cancel that always works. On the iPad mini the
/// line stayed on "Reading the sheet's codes" after the reading had thrown an error nobody caught, and Cancel then threw too, because the
/// cancellation it pressed had been disposed with the method that made it. So:
/// <list type="bullet">
/// <item>Cancel never throws, at any stage, before or after the reading ends, however often it is pressed.</item>
/// <item>Cancel does not wait for the reading: the screen goes back at once, and the reading, which may be inside one long step the
/// cancellation cannot interrupt, is left to stop at its next check; what it leaves behind is cleaned up when it does.</item>
/// <item>Every error ends the reading with a message, and one nobody expected is recorded as an error report with where it happened.</item>
/// <item>A time limit, counted only while the application runs: iOS suspends it while the screen is locked, and the time spent locked is
/// not the reading's. On coming back the reading carries on; the gap is logged.</item>
/// </list>
/// </summary>
internal sealed class Reading
{
    /// <summary>
    /// The longest a reading may run before it is stopped and the person told, counted while the application runs. The slowest reading in
    /// the Fold 7's sittings took 45.5 seconds (2026-09-28, a picture that named no sheet, before entry 291 made reading the codes square
    /// on); a good picture takes 3 to 20. Sixty seconds stops only what would otherwise spin. The iOS self-test raises it, and nothing else
    /// does: the CI simulator reads the committed scan in 33 to 59 seconds (run 36726281165, 2026-09-30), so a minute cut one of its readings
    /// short. A person's phone keeps the minute.
    /// </summary>
    public static TimeSpan Limit { get; internal set; } = TimeSpan.FromSeconds(60);

    /// <summary>How often the reading is looked at while it runs.</summary>
    public static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(250);

    /// <summary>A tick that took longer than this was the application suspended, and the time is not counted.</summary>
    public static readonly TimeSpan Suspended = TimeSpan.FromSeconds(3);

    // Never disposed: a CancellationTokenSource with no timer holds nothing that needs it, and a disposed one throws from Cancel, which is
    // what the error report of build 134 showed (entry 313 section 1).
    private readonly CancellationTokenSource cancel = new();
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly string name;
    private readonly Func<TimeSpan> clock;
    private readonly TimeSpan created;
    private int pressed;

    /// <param name="name">What the log calls this reading, such as phone.detect.</param>
    /// <param name="clock">The time since some fixed moment; a test passes its own, so a screen lock can be imitated.</param>
    public Reading(string name, Func<TimeSpan>? clock = null)
    {
        this.name = name;
        var watch = Stopwatch.StartNew();
        this.clock = clock ?? (() => watch.Elapsed);
        created = this.clock();
    }

    /// <summary>The time since the reading was made, screen lock included, for the diagnostics overlay (entry 315 section 4).</summary>
    public TimeSpan Elapsed => clock() - created;

    /// <summary>The cancellation the reading checks between its stages and inside its long loops.</summary>
    public CancellationToken Token => cancel.Token;

    /// <summary>Whether Cancel was pressed or the time limit reached.</summary>
    public bool Stopping => Volatile.Read(ref pressed) != 0;

    /// <summary>Cancel: never throws, and only the first press does anything.</summary>
    public void Stop()
    {
        if (Interlocked.Exchange(ref pressed, 1) != 0)
        {
            return;
        }

        try
        {
            cancel.Cancel();
        }
        catch (Exception e) when (e is ObjectDisposedException or AggregateException)
        {
            // Something listening to the cancellation threw; the reading is stopping regardless.
            DiagnosticLog.Info("read.cancel", ("name", name), ("error", e.GetType().Name));
        }

        stopped.TrySetResult();
    }

    /// <summary>
    /// Runs <paramref name="work"/> on a pool thread and waits for the first of its answer, Cancel, and <paramref name="limit"/> of running
    /// time. Where Cancel or the limit comes first the work is left to finish on its own, and <paramref name="late"/> is called when it
    /// does, with its answer where it gave one and the default where it stopped or failed, so what nobody will see is cleaned up.
    /// </summary>
    public async Task<ReadOutcome<T>> Run<T>(Func<CancellationToken, T> work, TimeSpan? limit = null, Action<T?>? late = null)
    {
        ArgumentNullException.ThrowIfNull(work);
        var began = clock();
        var running = Task.Run(() => work(cancel.Token), CancellationToken.None);
        var counted = TimeSpan.Zero;
        var last = began;
        while (true)
        {
            var first = await Task.WhenAny(running, stopped.Task, Task.Delay(Tick)).ConfigureAwait(true);
            if (first == running)
            {
                break;
            }

            var now = clock();
            var step = now - last;
            last = now;
            if (first == stopped.Task)
            {
                DiagnosticLog.Info("read.cancel", ("name", name), ("ms", (long)(now - began).TotalMilliseconds));
                Abandon(running, late);
                return new ReadOutcome<T>(ReadEnd.Canceled, default, null, now - began);
            }

            if (step > Suspended)
            {
                // The application was suspended, the screen locked most likely: that time was not the reading's, and it carries on.
                DiagnosticLog.Info("read.resumed", ("name", name), ("away_ms", (long)step.TotalMilliseconds), ("counted_ms", (long)counted.TotalMilliseconds));
                continue;
            }

            counted += step;
            if (limit is { } most && counted > most)
            {
                DiagnosticLog.Info("read.limit", ("name", name), ("ms", (long)counted.TotalMilliseconds));
                Stop();
                Abandon(running, late);
                return new ReadOutcome<T>(ReadEnd.TimedOut, default, null, now - began);
            }
        }

        var took = clock() - began;
        try
        {
            return new ReadOutcome<T>(ReadEnd.Done, await running.ConfigureAwait(true), null, took);
        }
        catch (OperationCanceledException)
        {
            return new ReadOutcome<T>(ReadEnd.Canceled, default, null, took);
        }
        catch (Exception e)
        {
            Failed(name, e);
            return new ReadOutcome<T>(ReadEnd.Failed, default, e, took);
        }
    }

    /// <summary>
    /// An error the reading ended with: logged with where it happened, and one nobody expected recorded as an error report as well, since
    /// it is caught here and would otherwise never reach one.
    /// </summary>
    internal static void Failed(string name, Exception e)
    {
        DiagnosticLog.Exception(LogLevel.Warn, name, e);
        if (e is not (IOException or InvalidOperationException or OpenCvSharp.OpenCVException) && DiagnosticLog.Current is { } log)
        {
            CrashReporter.Record(log, e, "task");
        }
    }

    /// <summary>Work nobody waits for any more, watched to its end so its error is seen and its late answer cleaned up.</summary>
    private void Abandon<T>(Task<T> running, Action<T?>? late)
    {
        var from = clock();
        _ = running.ContinueWith(t =>
        {
            DiagnosticLog.Info("read.abandoned", ("name", name), ("stopped_ms", (long)(clock() - from).TotalMilliseconds),
                ("ended", t.IsCanceled || t.Exception?.InnerException is OperationCanceledException ? "canceled" : t.IsFaulted ? t.Exception!.InnerException!.GetType().Name : "answered"));
            if (late is not null)
            {
                try
                {
                    late(t.IsCompletedSuccessfully ? t.Result : default);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or Microsoft.Data.Sqlite.SqliteException)
                {
                    DiagnosticLog.Info("read.abandoned", ("name", name), ("cleanup", e.GetType().Name));
                }
            }
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
}

/// <summary>What <see cref="IPhonePlatform.KeepRunning"/> gives where the system asks nothing to keep an application running.</summary>
internal sealed class NothingHeld : IDisposable
{
    public static readonly NothingHeld Instance = new();

    public void Dispose()
    {
    }
}
