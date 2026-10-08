using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace GroupLab.Core.Printing.Labels;

/// <summary>A COM port Windows made for a device paired over classic Bluetooth, with the device's name.</summary>
/// <param name="Outgoing">True for the port that connects to the device; Windows also makes an incoming one, which waits to be called.</param>
public sealed record BluetoothPort(string Port, string Name, bool Outgoing);

/// <summary>What a computer says when a printer over a Bluetooth serial port cannot be reached, in words for the screen.</summary>
public static class SerialPrinterWords
{
    public static string NotHere(string name) =>
        $"Printing straight to the {name} from a computer works on Windows only so far, not on this computer yet. Use Save for a printer app, or print from the phone.";

    public static string NotPaired(string name) =>
        $"No printer named {name} is paired with this computer. Turn the printer on, pair it in Windows Settings (Bluetooth and devices, Add device), then press Print again.";

    public static string NoAnswer(string name) =>
        $"The {name} did not answer. Check it is on and that no phone is connected to it: a phone with the Phomemo app or GroupLab printing holds the printer while it is connected. Turn the printer off and on, then press Print again.";

    public static string Busy(string name) =>
        $"The {name}'s Bluetooth port is in use by another program on this computer. Close it, or wait for its print to finish, then press Print again.";

    /// <summary>The words for the Windows error opening the port gave: 2 and 3 a port that has gone, 5 a port another program holds, anything else no answer.</summary>
    public static string ForError(string name, int error) => error switch
    {
        2 or 3 => NotPaired(name),
        5 or 32 => Busy(name),
        _ => NoAnswer(name),
    };
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 389 section 1: the M834 from the computer. On Windows a printer paired over classic Bluetooth is a serial port
/// (a COM port), the same link the phone opens, so the computer writes the phone's bytes into it. The port is found by the printer's
/// Bluetooth name from what Windows keeps about paired devices, read only; the device's address is used to match the two and is never
/// logged or shown.
/// </summary>
public static class BluetoothPorts
{
    /// <summary>The Bluetooth serial port service, under which Windows lists the COM ports it made for paired devices.</summary>
    public const string SerialService = "{00001101-0000-1000-8000-00805f9b34fb}";

    private const string NoAddress = "000000000000";

    /// <summary>
    /// The device's address in a device instance under the serial service, such as <c>8&amp;1a2b3c&amp;0&amp;A1B2C3D4E5F6_C00000000</c>: the twelve
    /// hex digits after the last ampersand. Null where there are none.
    /// </summary>
    public static string? AddressOf(string instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        int amp = instance.LastIndexOf('&');
        string tail = amp < 0 ? instance : instance[(amp + 1)..];
        int underscore = tail.IndexOf('_', StringComparison.Ordinal);
        string address = underscore < 0 ? tail : tail[..underscore];
        return address.Length == 12 && address.All(Uri.IsHexDigit) ? address.ToUpperInvariant() : null;
    }

    /// <summary>A device name Windows keeps as bytes: UTF-8, ended by a zero.</summary>
    public static string NameOf(byte[] stored)
    {
        ArgumentNullException.ThrowIfNull(stored);
        int end = Array.IndexOf(stored, (byte)0);
        return Encoding.UTF8.GetString(stored, 0, end < 0 ? stored.Length : end).Trim();
    }

    /// <summary>The ports that connect to a device whose name holds <paramref name="nameHint"/>, in the order Windows listed them.</summary>
    public static IReadOnlyList<BluetoothPort> Named(IEnumerable<BluetoothPort> ports, string nameHint)
    {
        ArgumentNullException.ThrowIfNull(ports);
        return [.. ports.Where(p => p.Outgoing && p.Name.Contains(nameHint, StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>Every Bluetooth serial port on this computer, from the registry, read only.</summary>
    [SupportedOSPlatform("windows")]
    public static IReadOnlyList<BluetoothPort> OnThisComputer()
    {
        using var bthenum = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\BTHENUM");
        if (bthenum is null)
        {
            return [];
        }

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (var devices = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices"))
        {
            foreach (var address in devices?.GetSubKeyNames() ?? [])
            {
                using var device = devices!.OpenSubKey(address);
                if (device?.GetValue("Name") is byte[] stored && NameOf(stored) is { Length: > 0 } name)
                {
                    names[address] = name;
                }
            }
        }

        var ports = new List<BluetoothPort>();
        foreach (var service in bthenum.GetSubKeyNames())
        {
            if (service.StartsWith("Dev_", StringComparison.OrdinalIgnoreCase))
            {
                // Where the radio's own list has no name, the device's node has it as its friendly name.
                string address = service[4..];
                using var node = bthenum.OpenSubKey(service);
                foreach (var instance in node?.GetSubKeyNames() ?? [])
                {
                    using var key = node!.OpenSubKey(instance);
                    if (!names.ContainsKey(address) && key?.GetValue("FriendlyName") is string friendly && friendly.Length > 0)
                    {
                        names[address] = friendly;
                    }
                }
            }
        }

        foreach (var service in bthenum.GetSubKeyNames().Where(s => s.StartsWith(SerialService, StringComparison.OrdinalIgnoreCase)))
        {
            using var serviceKey = bthenum.OpenSubKey(service);
            foreach (var instance in serviceKey?.GetSubKeyNames() ?? [])
            {
                using var parameters = serviceKey!.OpenSubKey(instance + @"\Device Parameters");
                if (parameters?.GetValue("PortName") is string port && AddressOf(instance) is { } address)
                {
                    ports.Add(new BluetoothPort(port, names.GetValueOrDefault(address) ?? "", address != NoAddress));
                }
            }
        }

        return ports;
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 389 section 1: the link to a printer through a Windows Bluetooth COM port. Opening the port is what makes
/// Windows connect to the printer, so the open is given <see cref="PrinterConnect.Timeout"/> like the phone's. A write that the printer
/// stops taking ends after <see cref="BlockTimeout"/>; the printer's answers are read without waiting, when they are asked for.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSerialPrinter : IPrinterLink
{
    /// <summary>How long one block may take to be written before the link is called stuck, as on the phone.</summary>
    public static readonly TimeSpan BlockTimeout = TimeSpan.FromSeconds(15);

    private readonly SafeFileHandle port;
    private readonly Action<string> log;
    private int written;

    private WindowsSerialPrinter(SafeFileHandle port, Action<string> log)
    {
        this.port = port;
        this.log = log;
    }

    /// <summary>The link to the first paired printer whose name holds <paramref name="nameHint"/>, or why there is none in words for the screen.</summary>
    public static async Task<(IPrinterLink? Link, string? Why)> OpenAsync(string nameHint, Action<string> log, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(log);
        var named = BluetoothPorts.Named(BluetoothPorts.OnThisComputer(), nameHint);
        log($"paired ports named {nameHint}: {named.Count}");
        if (named.Count == 0)
        {
            return (null, SerialPrinterWords.NotPaired(nameHint));
        }

        int error = 0;
        foreach (var found in named)
        {
            SafeFileHandle? open = null;
            bool abandoned = false;
            var gate = new Lock();
            var attempt = new ConnectAttempt(found.Port, () =>
            {
                var handle = Open(found.Port, out int why);
                if (handle is null)
                {
                    error = why;
                    throw new IOException($"Windows error {why}");
                }

                lock (gate)
                {
                    if (abandoned)
                    {
                        handle.Dispose();
                        return;
                    }

                    open = handle;
                }
            }, () =>
            {
                // A port still opening cannot be stopped; when it opens late, Begin closes it.
                lock (gate)
                {
                    abandoned = true;
                    open?.Dispose();
                    open = null;
                }
            });
            string? connected = await PrinterConnect.FirstThatConnects([attempt], PrinterConnect.Timeout,
                (way, outcome, ms) => log($"connect {way}: {outcome} in {ms} ms"), token).ConfigureAwait(false);
            if (connected is not null && open is { } handle)
            {
                return (new WindowsSerialPrinter(handle, log), null);
            }
        }

        return (null, SerialPrinterWords.ForError(nameHint, error));
    }

    private static SafeFileHandle? Open(string port, out int error)
    {
        var handle = CreateFileW(@"\\.\" + port, GenericRead | GenericWrite, 0, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            error = Marshal.GetLastWin32Error();
            handle.Dispose();
            return null;
        }

        // Reads return at once with whatever has arrived; a write gives up after BlockTimeout.
        var timeouts = new CommTimeouts { ReadIntervalTimeout = uint.MaxValue, WriteTotalTimeoutConstant = (uint)BlockTimeout.TotalMilliseconds };
        if (!SetCommTimeouts(handle, ref timeouts))
        {
            error = Marshal.GetLastWin32Error();
            handle.Dispose();
            return null;
        }

        error = 0;
        return handle;
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        byte[] bytes = chunk.ToArray();
        int took = await Task.Run(() => WriteFile(port, bytes, bytes.Length, out int n, IntPtr.Zero) ? n : -1, CancellationToken.None).ConfigureAwait(false);
        if (took != bytes.Length)
        {
            throw new IOException($"the printer stopped taking the page after {written + Math.Max(0, took)} bytes");
        }

        written += took;
    }

    public IReadOnlyList<byte[]> TakeAnswers()
    {
        var taken = new List<byte[]>();
        var buffer = new byte[256];
        while (ReadFile(port, buffer, buffer.Length, out int n, IntPtr.Zero) && n > 0)
        {
            taken.Add(buffer[..n]);
            log($"heard {Convert.ToHexString(buffer, 0, n)}");
        }

        return taken;
    }

    public async ValueTask DisposeAsync()
    {
        // As on the phone: a short pause so a last answer is read before the port closes.
        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        port.Dispose();
        log($"closed after {written} bytes");
    }

    private const uint GenericRead = 0x80000000, GenericWrite = 0x40000000, OpenExisting = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCommTimeouts(SafeFileHandle file, ref CommTimeouts timeouts);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteFile(SafeFileHandle file, byte[] buffer, int count, out int written, IntPtr overlapped);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadFile(SafeFileHandle file, byte[] buffer, int count, out int read, IntPtr overlapped);
}
