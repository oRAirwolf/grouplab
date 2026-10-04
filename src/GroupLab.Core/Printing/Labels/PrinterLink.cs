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
