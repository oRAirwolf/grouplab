using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GroupLab.Core.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 471 section 2, question 109 (a): forgetting another device in the sync folder. Tidying moves a change file
/// only once every device that has written to the folder has read it (entry 464 section 1), so a device that never comes back, a phone
/// GroupLab was taken off or a reinstall that became a new device, would hold tidying back for good. Forgetting it writes one small file,
/// <c>GroupLab sync/forgotten/&lt;device&gt;.json</c>; from then on no device's tidying waits for it and no Sync block lists it. Nothing in
/// the folder is deleted: its change files and its records stay, and every device still reads them. A forgotten device that syncs again is
/// seen again, because it writes into its own device file that it has seen the file forgetting it.
/// </summary>
public static class SyncForget
{
    public const string Format = "grouplab-sync-forgotten";

    /// <summary>Forgets <paramref name="device"/>, by its name in the folder, on behalf of this device, <paramref name="me"/>.</summary>
    public static void Forget(ISyncFolder folder, string me, string device, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(folder);
        if (string.IsNullOrEmpty(device) || device == me || device.StartsWith('.') || device.IndexOfAny(['/', '\\']) >= 0)
        {
            throw new ArgumentException("Only another device in the folder can be forgotten.", nameof(device));
        }

        var marker = new JsonObject
        {
            ["format"] = Format, ["version"] = SyncEngine.Version, ["device"] = device, ["by"] = me, ["forgottenUtc"] = SyncEngine.Utc(nowUtc),
            ["id"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant(), ["end"] = true,
        };
        folder.Write(PathOf(device), Encoding.UTF8.GetBytes(marker.ToJsonString()));
    }

    /// <summary>Whether <paramref name="device"/> is forgotten now: a whole file forgets it, and its own device file has not seen that file.</summary>
    public static bool IsForgotten(ISyncFolder folder, string device)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return Marker(folder, device) is { } id && Seen(folder, device) != id;
    }

    /// <summary>The identity of the file forgetting <paramref name="device"/>, or null where there is none or it is not whole.</summary>
    internal static string? Marker(ISyncFolder folder, string device)
    {
        var file = Read(folder.Read(PathOf(device)));
        return file is not null && Text(file["format"]) == Format && file["end"]?.GetValueKind() == JsonValueKind.True && Text(file["device"]) == device
            ? Text(file["id"])
            : null;
    }

    /// <summary>The forgetting <paramref name="device"/>'s own device file says it has seen, or null.</summary>
    internal static string? Seen(ISyncFolder folder, string device) => Text(Read(folder.Read($"{SyncEngine.Root}/devices/{device}.json"))?["forgotten"]);

    private static string PathOf(string device) => $"{SyncEngine.Root}/forgotten/{device}.json";

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonObject? Read(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(bytes) as JsonObject;
        }
        catch (JsonException)
        {
            // A file another device is still writing is read next time.
            return null;
        }
    }
}
