namespace GroupLab.Core.Printing.Labels;

/// <summary>A printer seen nearby or attached: what it advertised, and its services once connected.</summary>
/// <param name="Address">The platform's own handle for it, opaque here.</param>
public sealed record FoundPrinter(string Address, string? AdvertisedName, PrinterTransport Transport, IReadOnlyList<string> Services);

/// <summary>
/// A connection to one printer, NOTES-FROM-PLANNING.md entry 358 section 4. It is made only through <see cref="Updates.IOutsideWorld"/>,
/// so no test ever opens a real one; the tests hand <see cref="PrinterJob"/> a recording link instead.
/// </summary>
public interface IPrinterLink : IAsyncDisposable
{
    /// <summary>Writes one chunk; it returns when the platform has taken it.</summary>
    Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token);

    /// <summary>What the printer has said back since the last call (battery, paper, credits), each answer as it came.</summary>
    IReadOnlyList<byte[]> TakeAnswers();
}

/// <summary>Which profile a printer found is, by its services first; its advertised name only where no service decides.</summary>
public static class PrinterRecognition
{
    public static PrinterProfile? Match(FoundPrinter found, IReadOnlyList<PrinterProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(profiles);
        var services = found.Services.Select(s => s.ToLowerInvariant()).ToHashSet();
        var byService = profiles.Where(p => p.Transports.Contains(found.Transport) && p.Services.Any(services.Contains)).ToList();
        // The same Bluetooth module's service appears across brands, so where several share it the name breaks the tie.
        var named = (byService.Count > 0 ? byService : profiles.Where(p => p.Transports.Contains(found.Transport)))
            .Where(p => found.AdvertisedName is { } name && p.NameHints.Any(h => name.StartsWith(h, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return byService.Count == 1 ? byService[0] : named.Count == 1 ? named[0] : byService.FirstOrDefault();
    }
}

/// <summary>
/// One print sent: the encoder's bytes cut into the profile's chunks, each written and then paced, as the M220's direct print needed
/// (128 bytes every 20 ms, entry 358 section 5). The pause is handed in, so a test sends a whole label at once.
/// </summary>
public static class PrinterJob
{
    public static async Task<int> SendAsync(IPrinterLink link, PrinterProfile profile, byte[] bytes, Func<TimeSpan, CancellationToken, Task> pause, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(pause);
        int size = Math.Max(1, profile.ChunkBytes), chunks = 0;
        for (int at = 0; at < bytes.Length; at += size)
        {
            token.ThrowIfCancellationRequested();
            await link.WriteAsync(bytes.AsMemory(at, Math.Min(size, bytes.Length - at)), token).ConfigureAwait(false);
            chunks++;
            if (profile.PacingMs > 0 && at + size < bytes.Length)
            {
                await pause(TimeSpan.FromMilliseconds(profile.PacingMs), token).ConfigureAwait(false);
            }
        }

        return chunks;
    }
}

/// <summary>
/// Entry 377: one way of opening a link, such as a socket of one kind: <see cref="Begin"/> blocks until it is open or throws, and
/// <see cref="Abort"/>, called from another thread, ends a <see cref="Begin"/> still waiting (closing the socket is the only way to end
/// Android's blocking connect).
/// </summary>
public sealed record ConnectAttempt(string Name, Action Begin, Action Abort);

/// <summary>
/// Entry 377: GroupLab's print to the M834 stayed on "Connecting" for ever, because nothing put a limit on the connect. Each way of
/// connecting is tried in turn, each given <c>timeout</c>, ended by its abort when the time runs out or the person presses Cancel, and every
/// one is reported with how long it took and how it ended.
/// </summary>
public static class PrinterConnect
{
    /// <summary>How long one way of connecting is given: the serial terminal app connected the M834 in 1.3 s.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(12);

    /// <summary>
    /// The name of the first attempt that connected, or null where none did; <paramref name="said"/> hears each attempt's name, its outcome
    /// ("connected", "timed out", or the exception's type) and its milliseconds. Cancelling aborts the attempt under way and throws.
    /// </summary>
    public static async Task<string?> FirstThatConnects(IReadOnlyList<ConnectAttempt> attempts, TimeSpan timeout, Action<string, string, long> said, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(attempts);
        ArgumentNullException.ThrowIfNull(said);
        foreach (var attempt in attempts)
        {
            token.ThrowIfCancellationRequested();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var connecting = Task.Run(attempt.Begin, CancellationToken.None);
            var limit = Task.Delay(timeout, token);
            var first = await Task.WhenAny(connecting, limit).ConfigureAwait(false);
            if (first == connecting && connecting.IsCompletedSuccessfully)
            {
                said(attempt.Name, "connected", clock.ElapsedMilliseconds);
                return attempt.Name;
            }

            if (first != connecting)
            {
                attempt.Abort();
                try
                {
                    await connecting.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The abort is what ended it; the outcome said below is the timeout or the cancel, not this.
                }

                said(attempt.Name, token.IsCancellationRequested ? "canceled" : "timed out", clock.ElapsedMilliseconds);
                token.ThrowIfCancellationRequested();
                continue;
            }

            said(attempt.Name, connecting.Exception?.InnerException?.GetType().Name ?? "failed", clock.ElapsedMilliseconds);
            attempt.Abort();
        }

        return null;
    }
}

/// <summary>
/// Entry 381: GroupLab's direct print to the M834 gave a strip of a few millimetres and stopped. Android takes the whole page into its own
/// buffer in a few milliseconds, and the printer then draws it across the link one frame at a time as it has room (in request 73's
/// recording it granted one credit per 666-byte frame, and 139 KB took 22.6 s to cross); GroupLab closed the link 2 s after the last write,
/// which cut the page off. In the recording the Phomemo app kept the link open until the printer's only answer after the page,
/// <c>1A 0F 0C</c>, 22.5 s after the last frame, which is taken here as "printed". So the link stays open until that answer, or failing
/// one, for <see cref="Limit"/>; Cancel ends the wait at once.
/// </summary>
public static class PrinterFinish
{
    /// <summary>What the M834 said when its page had printed, in request 73's recording.</summary>
    public static readonly byte[] M834Printed = [0x1A, 0x0F, 0x0C];

    /// <summary>How often the answers are looked at while waiting.</summary>
    public static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// The longest a page of <paramref name="bytes"/> is waited for when the printer never says it has finished: 30 s, and a second for
    /// each 2,500 bytes (the recording crossed at about 6,100 bytes a second and printed in 45 s all told, so this is twice that and more).
    /// </summary>
    public static TimeSpan Limit(int bytes) => TimeSpan.FromSeconds(30 + Math.Max(0, bytes) / 2500.0);

    /// <summary>
    /// Waits until the printer's answers, read together, contain <paramref name="done"/>: true when they did, false when
    /// <paramref name="limit"/> ran out first. <paramref name="heard"/> is given every answer as it is taken, to log. Cancelling throws.
    /// </summary>
    public static async Task<bool> WaitAsync(IPrinterLink link, byte[] done, TimeSpan limit, Func<TimeSpan, CancellationToken, Task> pause,
        Action<byte[]> heard, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(done);
        ArgumentNullException.ThrowIfNull(pause);
        ArgumentNullException.ThrowIfNull(heard);
        var said = new List<byte>();
        for (var waited = TimeSpan.Zero; ; waited += Poll)
        {
            token.ThrowIfCancellationRequested();
            foreach (var answer in link.TakeAnswers())
            {
                heard(answer);
                said.AddRange(answer);
            }

            // An answer can arrive in two reads, so the bytes are searched as one run.
            if (Contains(said, done))
            {
                return true;
            }

            if (waited >= limit)
            {
                return false;
            }

            await pause(Poll, token).ConfigureAwait(false);
        }
    }

    private static bool Contains(List<byte> said, byte[] done)
    {
        for (int at = 0; at + done.Length <= said.Count; at++)
        {
            if (said.Skip(at).Take(done.Length).SequenceEqual(done))
            {
                return true;
            }
        }

        return false;
    }
}
