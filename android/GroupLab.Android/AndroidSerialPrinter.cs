using Android.Bluetooth;
using Android.Runtime;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Printing.Labels;

namespace GroupLab.Android;

/// <summary>
/// Request 73 (2026-10-05): the Phomemo app printed to the M834 over classic Bluetooth's serial port, so GroupLab does the same: a socket to
/// the printer already paired in the phone's Bluetooth settings, found by its name.
/// <para>
/// Entry 377: the first try stayed on "Connecting" for ever with nothing in the log. Cancelling discovery needs the nearby-devices scan
/// permission from Android 12, which GroupLab does not ask for, so that call threw, and the print, started and not awaited, died silently.
/// Now nothing here needs that permission, every step is logged, and each way of connecting is given <see cref="PrinterConnect.Timeout"/>:
/// the serial port's standard service, then RFCOMM channel 1 (where the Phomemo app reached the printer), each secure and then insecure.
/// The device's address is never logged.
/// </para>
/// </summary>
internal sealed class AndroidSerialPrinter : IPrinterLink
{
    private static readonly Java.Util.UUID SerialPort = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB")!;

    /// <summary>How long one block may take to be written before the link is called stuck.</summary>
    private static readonly TimeSpan BlockTimeout = TimeSpan.FromSeconds(15);

    private readonly BluetoothSocket socket;
    private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> answers = new();
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private int written;
    private int blocks;

    /// <summary>
    /// Entry 381: the printer's answers are read from the moment the link opens, each logged as hex with its time, and kept for
    /// <see cref="TakeAnswers"/>, so the print can wait for the printer to say its page has printed.
    /// </summary>
    private AndroidSerialPrinter(BluetoothSocket socket)
    {
        this.socket = socket;
        _ = Task.Factory.StartNew(Listen, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void Listen()
    {
        var buffer = new byte[256];
        try
        {
            var input = socket.InputStream!;
            while (true)
            {
                int n = input.Read(buffer, 0, buffer.Length);
                if (n <= 0)
                {
                    break;
                }

                var answer = buffer[..n];
                answers.Enqueue(answer);
                DiagnosticLog.Info("print.serial", ("step", "heard"), ("hex", Convert.ToHexString(answer)), ("ms", clock.ElapsedMilliseconds));
            }
        }
        catch (Exception e) when (e is Java.IO.IOException or IOException or ObjectDisposedException)
        {
            // The socket was closed, which is how listening ends.
        }
    }

    /// <summary>The link, or why there is none, in words for the screen.</summary>
    public static async Task<(IPrinterLink? Link, string? Why)> OpenAsync(string nameHint, CancellationToken token)
    {
        if (!MainActivity.BluetoothAllowed())
        {
            DiagnosticLog.Info("print.serial", ("step", "permission"), ("result", "refused"));
            return (null, "GroupLab needs permission to connect to nearby devices to print. Allow it, then press Print again.");
        }

        var adapter = (MainActivity.Current?.GetSystemService(global::Android.Content.Context.BluetoothService) as BluetoothManager)?.Adapter;
        if (adapter is null || !adapter.IsEnabled)
        {
            DiagnosticLog.Info("print.serial", ("step", "adapter"), ("result", adapter is null ? "none" : "off"));
            return (null, "Bluetooth is off. Turn it on, then press Print again.");
        }

        var named = adapter.BondedDevices?.Where(d => d.Name?.Contains(nameHint, StringComparison.OrdinalIgnoreCase) == true).ToList() ?? [];
        foreach (var d in named)
        {
            DiagnosticLog.Info("print.serial", ("step", "paired"), ("name", d.Name ?? ""), ("type", d.Type.ToString()), ("bond", d.BondState.ToString()));
        }

        if (named.Count == 0)
        {
            return (null, $"No printer named {nameHint} is paired with this phone. Turn the printer on, pair it in the phone's Bluetooth settings, then press Print again.");
        }

        // A bond made over Bluetooth LE alone has no serial port; a classic or dual one does.
        var usable = named.Where(d => d.Type != BluetoothDeviceType.Le).ToList();
        if (usable.Count == 0)
        {
            DiagnosticLog.Info("print.serial", ("step", "paired"), ("result", "LE only"));
            return (null, $"This phone is paired with the {nameHint} over Bluetooth LE only, which has no serial port. In Android's Bluetooth settings, forget the {nameHint}, turn it off and on, and pair it again from there, then press Print again.");
        }

        foreach (var device in usable)
        {
            BluetoothSocket? open = null;
            var attempts = Ways(device).Select(way => new ConnectAttempt(way.Name, () =>
            {
                var made = way.Make() ?? throw new IOException("no socket");
                open = made;
                made.Connect();
            }, () =>
            {
                try
                {
                    open?.Close();
                }
                catch (Java.IO.IOException)
                {
                    // Already closed.
                }
            })).ToList();

            string? connected = await PrinterConnect.FirstThatConnects(attempts, PrinterConnect.Timeout,
                (way, outcome, ms) => DiagnosticLog.Info("print.serial", ("step", "connect"), ("way", way), ("result", outcome), ("ms", ms)), token).ConfigureAwait(false);
            if (connected is not null && open is { } socket)
            {
                return (new AndroidSerialPrinter(socket), null);
            }
        }

        return (null, $"The {nameHint} did not answer in {PrinterConnect.Timeout.TotalSeconds:0} seconds, in any of four ways of connecting. Check it is on, close the Phomemo app (or anything else connected to it), turn the printer off and on, then press Print again.");
    }

    /// <summary>The four ways of opening the serial link, in the order tried.</summary>
    private static IEnumerable<(string Name, Func<BluetoothSocket?> Make)> Ways(BluetoothDevice device)
    {
        yield return ("serial port, secure", () => device.CreateRfcommSocketToServiceRecord(SerialPort));
        yield return ("channel 1, secure", () => Channel(device, "createRfcommSocket"));
        yield return ("channel 1, insecure", () => Channel(device, "createInsecureRfcommSocket"));
        yield return ("serial port, insecure", () => device.CreateInsecureRfcommSocketToServiceRecord(SerialPort));
    }

    /// <summary>A socket on RFCOMM channel 1, through Android's hidden method of that name, which skips the service lookup.</summary>
    private static BluetoothSocket? Channel(BluetoothDevice device, string method)
    {
        var found = device.Class.GetMethod(method, Java.Lang.Integer.Type!);
        return found.Invoke(device, Java.Lang.Integer.ValueOf(1))?.JavaCast<BluetoothSocket>();
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
    {
        var bytes = chunk.ToArray();
        var writing = Task.Run(() =>
        {
            socket.OutputStream!.Write(bytes, 0, bytes.Length);
            socket.OutputStream.Flush();
        }, CancellationToken.None);
        var first = await Task.WhenAny(writing, Task.Delay(BlockTimeout, token)).ConfigureAwait(false);
        if (first != writing)
        {
            // A write blocked on a printer that stopped reading ends only when the socket is closed.
            socket.Close();
            token.ThrowIfCancellationRequested();
            throw new IOException($"the printer stopped taking the page after {written} bytes");
        }

        await writing.ConfigureAwait(false);
        written += bytes.Length;
        blocks++;
        // Entry 381: the time each block was taken, so a log shows whether Android buffered the page or the printer paced it.
        DiagnosticLog.Info("print.serial", ("step", "wrote"), ("block", blocks), ("bytes", written), ("ms", clock.ElapsedMilliseconds));
    }

    public IReadOnlyList<byte[]> TakeAnswers()
    {
        var taken = new List<byte[]>();
        while (answers.TryDequeue(out var answer))
        {
            taken.Add(answer);
        }

        return taken;
    }

    public async ValueTask DisposeAsync()
    {
        // Entry 381: the print waits for the printer to say it has finished before it comes here (PrinterFinish); this short pause is
        // only so a last answer is read before the socket goes.
        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        socket.Close();
        socket.Dispose();
        DiagnosticLog.Info("print.serial", ("step", "closed"), ("bytes", written), ("blocks", blocks), ("ms", clock.ElapsedMilliseconds));
    }
}
