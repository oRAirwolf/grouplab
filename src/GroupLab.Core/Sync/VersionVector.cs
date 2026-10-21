using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json.Nodes;

namespace GroupLab.Core.Sync;

/// <summary>How two revisions of one record stand to each other.</summary>
public enum Order
{
    /// <summary>The same revision.</summary>
    Same,

    /// <summary>This one was made from the other, and carries every change in it.</summary>
    Newer,

    /// <summary>The other was made from this one.</summary>
    Older,

    /// <summary>Each was changed on a device that had not seen the other's change: changed on two devices since they last met.</summary>
    Concurrent,
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 461 section 3.1 item 1: a record's revision, so a change can be told from the version it replaced. It counts
/// the changes each device has made to the record: a change here adds one to this device's count. A revision that has every count of
/// another at least as high was made from it (<see cref="Order.Newer"/>); one higher on one device and lower on another was changed on two
/// devices since they last met (<see cref="Order.Concurrent"/>), which the merge never decides alone.
/// </summary>
public sealed class VersionVector : IEquatable<VersionVector>
{
    public static VersionVector Empty { get; } = new(ImmutableSortedDictionary<string, long>.Empty);

    private VersionVector(ImmutableSortedDictionary<string, long> counts) => Counts = counts;

    public ImmutableSortedDictionary<string, long> Counts { get; }

    /// <summary>The revision after one more change on <paramref name="device"/>.</summary>
    public VersionVector Bump(string device) =>
        new(Counts.SetItem(device, (Counts.TryGetValue(device, out long had) ? had : 0) + 1));

    /// <summary>The revision that has seen both: every device's higher count.</summary>
    public VersionVector Merge(VersionVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var counts = Counts;
        foreach (var (device, count) in other.Counts)
        {
            if (!counts.TryGetValue(device, out long mine) || mine < count)
            {
                counts = counts.SetItem(device, count);
            }
        }

        return new(counts);
    }

    public Order Compare(VersionVector other)
    {
        ArgumentNullException.ThrowIfNull(other);
        bool ahead = false, behind = false;
        foreach (string device in Counts.Keys.Union(other.Counts.Keys))
        {
            long mine = Counts.TryGetValue(device, out long m) ? m : 0;
            long theirs = other.Counts.TryGetValue(device, out long t) ? t : 0;
            ahead |= mine > theirs;
            behind |= mine < theirs;
        }

        return (ahead, behind) switch
        {
            (false, false) => Order.Same,
            (true, false) => Order.Newer,
            (false, true) => Order.Older,
            _ => Order.Concurrent,
        };
    }

    public JsonObject ToJson()
    {
        var json = new JsonObject();
        foreach (var (device, count) in Counts)
        {
            json[device] = count;
        }

        return json;
    }

    public override string ToString() => ToJson().ToJsonString();

    public static VersionVector Parse(JsonNode? node)
    {
        if (node is not JsonObject json)
        {
            return Empty;
        }

        var counts = ImmutableSortedDictionary.CreateBuilder<string, long>(StringComparer.Ordinal);
        foreach (var (device, value) in json)
        {
            if (value is JsonValue v && v.TryGetValue(out long count) && count > 0)
            {
                counts[device] = count;
            }
        }

        return new(counts.ToImmutable());
    }

    public static VersionVector Parse(string? text) =>
        string.IsNullOrWhiteSpace(text) ? Empty : Parse(JsonNode.Parse(text));

    public bool Equals(VersionVector? other) => other is not null && Compare(other) == Order.Same;

    public override bool Equals(object? obj) => obj is VersionVector other && Equals(other);

    public override int GetHashCode() =>
        Counts.Aggregate(17, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture)));
}
