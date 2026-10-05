using Android.Bluetooth;
using GroupLab.Core.Printing.Labels;

namespace GroupLab.Android;

/// <summary>
/// Request 73 (2026-10-05): the Phomemo app printed to the M834 over classic Bluetooth's serial port, so GroupLab does the same: a socket to
/// the printer already paired in the phone's Bluetooth settings, found by its name, on the serial port's standard service.
/// </summary>
internal sealed class AndroidSerialPrinter : IPrinterLink
{
    private static readonly Java.Util.UUID SerialPort = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB")!;

    private readonly BluetoothSocket socket;

    private AndroidSerialPrinter(BluetoothSocket socket) => this.socket = socket;

    /// <summary>The link, or why there is none, in words for the screen.</summary>
    public static async Task<(IPrinterLink? Link, string? Why)> OpenAsync(string nameHint, CancellationToken token)
    {
        if (!MainActivity.BluetoothAllowed())
        {
            return (null, "GroupLab needs permission to connect to nearby devices to print. Allow it, then press Print again.");
        }

        var adapter = (MainActivity.Current?.GetSystemService(global::Android.Content.Context.BluetoothService) as BluetoothManager)?.Adapter;
        if (adapter is null || !adapter.IsEnabled)
        {
            return (null, "Bluetooth is off. Turn it on, then press Print again.");
        }

        var device = adapter.BondedDevices?.FirstOrDefault(d => d.Name?.Contains(nameHint, StringComparison.OrdinalIgnoreCase) == true);
        if (device is null)
        {
            return (null, $"No printer named {nameHint} is paired with this phone. Turn the printer on, pair it in the phone's Bluetooth settings, then press Print again.");
        }

        var socket = device.CreateRfcommSocketToServiceRecord(SerialPort);
        if (socket is null)
        {
            return (null, "The printer's serial port could not be opened.");
        }

        try
        {
            adapter.CancelDiscovery();
            await Task.Run(socket.Connect, token).ConfigureAwait(false);
            return (new AndroidSerialPrinter(socket), null);
        }
        catch (Java.IO.IOException)
        {
            socket.Dispose();
            return (null, $"The {nameHint} did not answer. Check it is on and not printing for another phone, then press Print again.");
        }
    }

    public Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token) => Task.Run(() =>
    {
        socket.OutputStream!.Write(chunk.ToArray(), 0, chunk.Length);
        socket.OutputStream.Flush();
    }, token);

    public IReadOnlyList<byte[]> TakeAnswers() => [];

    public async ValueTask DisposeAsync()
    {
        // The printer takes the last of the page from its buffer after the phone has written it; closing at once can cut it off.
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        socket.Close();
        socket.Dispose();
    }
}
