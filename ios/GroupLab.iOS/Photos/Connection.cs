using CoreFoundation;
using Network;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2.4, as section 1.4 on Android: whether the device can reach the internet now, so a photograph
/// kept only in iCloud that could not be fetched is said to be for that reason. Network's path monitor says it, and says again whenever it
/// changes; it is started with the application.
/// </summary>
internal static class Connection
{
    private static NWPathMonitor? monitor;
    private static volatile int state;
    private static volatile int cost;

    /// <summary>True where there is a way to the internet, false where there is none, null until the monitor has said.</summary>
    internal static bool? Online => state switch
    {
        1 => true,
        2 => false,
        _ => null,
    };

    /// <summary>
    /// Entry 347: true on Wi-Fi or a wired line, false on a mobile connection, an expensive one or one in Low Data Mode, null until the
    /// monitor has said.
    /// </summary>
    internal static bool? Unmetered => cost switch
    {
        1 => true,
        2 => false,
        _ => null,
    };

    /// <summary>Starts the monitor once; its answers arrive on a background queue.</summary>
    internal static void Start()
    {
        if (monitor is not null)
        {
            return;
        }

        monitor = new NWPathMonitor
        {
            SnapshotHandler = path =>
            {
                state = path.Status == NWPathStatus.Satisfied ? 1 : 2;
                cost = path.Status == NWPathStatus.Satisfied && !path.IsExpensive && !path.IsConstrained ? 1 : 2;
            },
        };
        monitor.SetQueue(DispatchQueue.DefaultGlobalQueue);
        monitor.Start();
    }
}
