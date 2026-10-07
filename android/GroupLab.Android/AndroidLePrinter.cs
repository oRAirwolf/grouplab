using Android.Bluetooth;
using Android.Bluetooth.LE;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Printing.Labels;

namespace GroupLab.Android;

/// <summary>
/// Entry 386, after question 90 (Alan, 2026-10-07): a label printer over Bluetooth LE, the way entry 358 section 5's one direct print reached
/// the Phomemo M220 (60.00 mm both ways, every dot where it was drawn). The printer is found among the phone's paired devices by the
/// profile's name hints first; failing that, from Android 12, by a ten second scan for the profile's service, which asks only for the
/// nearby-devices permission and is declared never to be used for location. Android 10 and 11 cannot scan without the location permission,
/// which GroupLab never asks for, so there the printer has to be paired in Bluetooth settings first. Each chunk is written with a response,
/// so the next goes only when the printer's radio has taken it; its answers on the notify characteristics are logged as hex and kept. The
/// device's address is never logged.
/// </summary>
internal sealed class AndroidLePrinter : IPrinterLink
{
    private static readonly Java.Util.UUID ClientConfig = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")!;

    private readonly BluetoothGatt gatt;
    private readonly BluetoothGattCharacteristic write;
    private readonly Callback callback;

    private AndroidLePrinter(BluetoothGatt gatt, BluetoothGattCharacteristic write, Callback callback)
    {
        this.gatt = gatt;
        this.write = write;
        this.callback = callback;
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> chunk, CancellationToken token)
    {
        var done = callback.NextWrite();
        byte[] bytes = chunk.ToArray();
        bool started;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            started = gatt.WriteCharacteristic(write, bytes, (int)GattWriteType.Default) == 0;
        }
        else
        {
#pragma warning disable CA1422, CS0618 // Android 12 and earlier have only this form.
            write.SetValue(bytes);
            write.WriteType = GattWriteType.Default;
            started = gatt.WriteCharacteristic(write);
#pragma warning restore CA1422, CS0618
        }

        if (!started)
        {
            throw new IOException("the printer did not take the write");
        }

        await done.WaitAsync(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
    }

    public IReadOnlyList<byte[]> TakeAnswers() => callback.TakeAnswers();

    public ValueTask DisposeAsync()
    {
        try
        {
            gatt.Disconnect();
            gatt.Close();
        }
        catch (Java.Lang.Exception)
        {
            // Already gone.
        }

        DiagnosticLog.Info("print.le", ("step", "closed"), ("written", callback.Written));
        return ValueTask.CompletedTask;
    }

    /// <summary>The link to a printer of <paramref name="profile"/>, or why there is none, in words for the screen.</summary>
    public static async Task<(IPrinterLink? Link, string? Why)> OpenAsync(PrinterProfile profile, CancellationToken token)
    {
        if (!MainActivity.BluetoothAllowed())
        {
            DiagnosticLog.Info("print.le", ("step", "permission"), ("result", "refused"));
            return (null, "GroupLab needs permission to connect to nearby devices to print. Allow it, then press Print again.");
        }

        var context = MainActivity.Current;
        var adapter = (context?.GetSystemService(global::Android.Content.Context.BluetoothService) as BluetoothManager)?.Adapter;
        if (context is null || adapter is null || !adapter.IsEnabled)
        {
            DiagnosticLog.Info("print.le", ("step", "adapter"), ("result", adapter is null ? "none" : "off"));
            return (null, "Bluetooth is off. Turn it on, then press Print again.");
        }

        var device = adapter.BondedDevices?.FirstOrDefault(d => profile.NameHints.Any(h => d.Name?.Contains(h, StringComparison.OrdinalIgnoreCase) == true));
        DiagnosticLog.Info("print.le", ("step", "paired"), ("found", device is not null));
        if (device is null)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                return (null, $"On this version of Android, pair the {profile.Name} in the phone's Bluetooth settings first, then press Print again.");
            }

            if (!MainActivity.BluetoothScanAllowed())
            {
                return (null, "GroupLab needs permission to find nearby devices to reach the printer. Allow it, then press Print again.");
            }

            device = await Scan(adapter, profile, token).ConfigureAwait(false);
            if (device is null)
            {
                return (null, $"No {profile.Name} answered in ten seconds. Turn it on, close the Phomemo app, then press Print again.");
            }
        }

        var callback = new Callback();
        var gatt = device.ConnectGatt(context, false, callback, BluetoothTransports.Le);
        if (gatt is null)
        {
            return (null, $"The {profile.Name} could not be connected to.");
        }

        try
        {
            await callback.Connected.Task.WaitAsync(PrinterConnect.Timeout, token).ConfigureAwait(false);
            gatt.RequestMtu(profile.ChunkBytes + 3);
            await callback.Mtu.Task.WaitAsync(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
            gatt.DiscoverServices();
            await callback.Discovered.Task.WaitAsync(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
            var service = profile.Services.Select(s => gatt.GetService(Java.Util.UUID.FromString(s))).FirstOrDefault(s => s is not null);
            var characteristic = profile.Write is { } w ? service?.GetCharacteristic(Java.Util.UUID.FromString(w)) : null;
            if (service is null || characteristic is null)
            {
                DiagnosticLog.Info("print.le", ("step", "services"), ("result", "missing"));
                gatt.Close();
                return (null, $"The printer that answered is not a {profile.Name}: it has no place to send a label.");
            }

            foreach (string uuid in profile.Notify)
            {
                if (service.GetCharacteristic(Java.Util.UUID.FromString(uuid)) is not { } notify || !gatt.SetCharacteristicNotification(notify, true))
                {
                    continue;
                }

                if (notify.GetDescriptor(ClientConfig) is { } config)
                {
                    var written = callback.NextDescriptor();
                    if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    {
                        gatt.WriteDescriptor(config, [.. BluetoothGattDescriptor.EnableNotificationValue!]);
                    }
                    else
                    {
#pragma warning disable CA1422, CS0618 // Android 12 and earlier have only this form.
                        config.SetValue([.. BluetoothGattDescriptor.EnableNotificationValue!]);
                        gatt.WriteDescriptor(config);
#pragma warning restore CA1422, CS0618
                    }

                    await written.WaitAsync(TimeSpan.FromSeconds(5), token).ConfigureAwait(false);
                }
            }

            DiagnosticLog.Info("print.le", ("step", "connected"), ("mtu", callback.MtuSize));
            return (new AndroidLePrinter(gatt, characteristic, callback), null);
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException or IOException or Java.Lang.Exception)
        {
            DiagnosticLog.Info("print.le", ("step", "connect"), ("result", e.GetType().Name));
            gatt.Close();
            if (e is OperationCanceledException)
            {
                throw;
            }

            return (null, $"The {profile.Name} did not answer. Check it is on and not connected to the Phomemo app, then press Print again.");
        }
    }

    /// <summary>Ten seconds of scanning, Android 12 and later, for the profile's service or one of its name hints.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private static async Task<BluetoothDevice?> Scan(BluetoothAdapter adapter, PrinterProfile profile, CancellationToken token)
    {
        if (adapter.BluetoothLeScanner is not { } scanner)
        {
            return null;
        }

        var found = new TaskCompletionSource<BluetoothDevice>(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen = new Seen(profile, found);
        scanner.StartScan(null, new ScanSettings.Builder().SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency)!.Build(), seen);
        try
        {
            return await found.Task.WaitAsync(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            scanner.StopScan(seen);
            DiagnosticLog.Info("print.le", ("step", "scan"), ("found", found.Task.IsCompletedSuccessfully));
        }
    }

    private sealed class Seen(PrinterProfile profile, TaskCompletionSource<BluetoothDevice> found) : ScanCallback
    {
        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (result?.Device is not { } device)
            {
                return;
            }

            var services = result.ScanRecord?.ServiceUuids?.Select(u => u.ToString().ToLowerInvariant()).ToList() ?? [];
            string name = result.ScanRecord?.DeviceName ?? "";
            if (profile.Services.Any(services.Contains) || profile.NameHints.Any(h => name.Contains(h, StringComparison.OrdinalIgnoreCase)))
            {
                found.TrySetResult(device);
            }
        }
    }

    private sealed class Callback : BluetoothGattCallback
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<byte[]> answers = new();
        private TaskCompletionSource writeDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource descriptorDone = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Connected { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Mtu { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Discovered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int MtuSize { get; private set; } = 23;

        public int Written { get; private set; }

        public Task NextWrite()
        {
            writeDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return writeDone.Task;
        }

        public Task NextDescriptor()
        {
            descriptorDone = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return descriptorDone.Task;
        }

        public IReadOnlyList<byte[]> TakeAnswers()
        {
            var taken = new List<byte[]>();
            while (answers.TryDequeue(out var a))
            {
                taken.Add(a);
            }

            return taken;
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            if (newState == ProfileState.Connected && status == GattStatus.Success)
            {
                Connected.TrySetResult();
            }
            else if (newState == ProfileState.Disconnected)
            {
                var gone = new IOException("the printer disconnected");
                Connected.TrySetException(gone);
                writeDone.TrySetException(gone);
            }
        }

        public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, GattStatus status)
        {
            MtuSize = mtu;
            Mtu.TrySetResult();
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status) => Discovered.TrySetResult();

        public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            if (status == GattStatus.Success)
            {
                Written++;
                writeDone.TrySetResult();
            }
            else
            {
                writeDone.TrySetException(new IOException("the printer refused a write: " + status));
            }
        }

        public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status) => descriptorDone.TrySetResult();

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value) => Answer(value);

#pragma warning disable CA1422, CS0618, CS0672 // Android 12 and earlier call only this form.
        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(33) && characteristic?.GetValue() is { } value)
            {
                Answer(value);
            }
        }
#pragma warning restore CA1422, CS0618, CS0672

        private void Answer(byte[] value)
        {
            answers.Enqueue(value);
            DiagnosticLog.Info("print.le", ("step", "answer"), ("hex", Convert.ToHexString(value)));
        }
    }
}
