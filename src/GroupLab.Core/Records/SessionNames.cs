using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GroupLab.Core.Records;

/// <summary>
/// What a session is called, and the sheet's name to show beneath it. <paramref name="Name"/> is what tells it from the others;
/// <paramref name="When"/> is its date, with the time where the date alone would not tell two apart.
/// </summary>
public sealed record SessionName(string Name, string When, string Sheet);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 295 section 3: two sessions shot on one sheet read the same when they are named by the sheet, so Compare
/// showed two loads both called "GroupLab 5x5 Load Development with Load Block, Letter". A session is named by what the person gave it, its
/// load, then its date, then its time, each added only where the one before it leaves two reading the same; the sheet's name goes beneath
/// it. Shared, so the phone's Sessions and Compare and the computer's Compare and Session records all name a session the same way.
/// Entry 511 section 1 (suggestion 56): a session's own name, the label typed for it ("Name this session"), comes before its load, and
/// the sheet's name beneath it is then the target's alone.
/// </summary>
public static class SessionNames
{
    /// <summary>The names of these sessions, in the order given, told apart from one another.</summary>
    public static IReadOnlyList<SessionName> For(IReadOnlyList<SessionSummary> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return For([.. sessions.Select(s => (s.Id, s.Label, s.Load, s.ShotDate, s.CreatedUtc, s.SheetName))], zone);
    }

    /// <summary>The names of these sessions, in the order given, told apart from one another.</summary>
    public static IReadOnlyList<SessionName> For(IReadOnlyList<SessionRecord> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return For([.. sessions.Select(s => (s.Id, LabelOf(s.MarkingJson), s.Load, s.ShotDate, s.CreatedUtc, s.SheetName))], zone);
    }

    /// <summary>The names of these sessions, none of them named by the person, in the order given, told apart from one another.</summary>
    public static IReadOnlyList<SessionName> For(
        IReadOnlyList<(long Id, string? Load, string? ShotDate, string CreatedUtc, string SheetName)> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return For([.. sessions.Select(s => (s.Id, (string?)null, s.Load, s.ShotDate, s.CreatedUtc, s.SheetName))], zone);
    }

    /// <summary>The session's own name kept in its marking, or null where it has none or the marking cannot be read.</summary>
    public static string? LabelOf(string? markingJson)
    {
        if (string.IsNullOrEmpty(markingJson))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(markingJson) is JsonObject file && file["sheetLabel"] is JsonValue value && value.TryGetValue<string>(out var label)
                && !string.IsNullOrWhiteSpace(label) ? label.Trim() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The target's name alone. A session's stored sheet name is the target's with its own name after it ("GroupLab 3x3 2 MOA, Rifle A"),
    /// as it has been since the label was added, so a device on an older build still reads it the same.
    /// </summary>
    public static string TargetOf(string sheetName, string? label)
    {
        ArgumentNullException.ThrowIfNull(sheetName);
        return label is { Length: > 0 } && sheetName.Length > label.Length + 2 && sheetName.EndsWith(", " + label, StringComparison.Ordinal)
            ? sheetName[..^(label.Length + 2)]
            : sheetName;
    }

    /// <summary>What a list shows for a session's sheet (entry 511 section 1): its own name first, then the target's.</summary>
    public static string Titled(string sheetName, string? label) =>
        label is { Length: > 0 } ? label + ", " + TargetOf(sheetName, label) : sheetName;

    /// <summary>The stored sheet name for a session given <paramref name="label"/> in place of <paramref name="oldLabel"/>.</summary>
    public static string Relabelled(string sheetName, string? oldLabel, string? label) =>
        TargetOf(sheetName, oldLabel) + (label is { Length: > 0 } ? ", " + label : "");

    /// <summary>
    /// The names: the session's own name where it has one, else the load where there is one, then the date where two would otherwise read
    /// the same (or always, where there is neither), then the time, then the record's number as a last resort.
    /// </summary>
    public static IReadOnlyList<SessionName> For(
        IReadOnlyList<(long Id, string? Label, string? Load, string? ShotDate, string CreatedUtc, string SheetName)> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        zone ??= TimeZoneInfo.Local;
        var facts = sessions.Select(s =>
        {
            DateTime? local = DateTime.TryParse(s.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var utc)
                ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone)
                : null;
            string date = s.ShotDate ?? local?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? s.CreatedUtc;
            string? time = local?.ToString("HH:mm", CultureInfo.InvariantCulture);
            string? label = string.IsNullOrWhiteSpace(s.Label) ? null : s.Label.Trim();
            string? load = label ?? (string.IsNullOrWhiteSpace(s.Load) ? null : s.Load.Trim());
            return (s.Id, Load: load, Date: date, Time: time, SheetName: TargetOf(s.SheetName, label));
        }).ToList();

        // Each step adds one more thing, only to the sessions that still read the same as another.
        var steps = new List<Func<int, (string Name, string When)>>
        {
            i => facts[i].Load is { } load ? (load, facts[i].Date) : (facts[i].Date, facts[i].Date),
            i => facts[i].Load is { } load ? ($"{load}, {facts[i].Date}", facts[i].Date) : (facts[i].Date, facts[i].Date),
            i => Timed(i),
            i => (Timed(i).Name + ", session " + facts[i].Id.ToString(CultureInfo.InvariantCulture), Timed(i).When),
        };

        (string Name, string When) Timed(int i)
        {
            string when = facts[i].Time is { } time ? $"{facts[i].Date}, {time}" : facts[i].Date;
            return (facts[i].Load is { } load ? $"{load}, {when}" : when, when);
        }

        int[] step = new int[facts.Count];
        for (int round = 0; round < steps.Count - 1; round++)
        {
            var names = Enumerable.Range(0, facts.Count).Select(i => steps[step[i]](i).Name).ToList();
            var clashing = Enumerable.Range(0, facts.Count).Where(i => names.Count(n => n == names[i]) > 1).ToList();
            if (clashing.Count == 0)
            {
                break;
            }

            foreach (int i in clashing)
            {
                step[i] = Math.Min(step[i] + 1, steps.Count - 1);
            }
        }

        return [.. Enumerable.Range(0, facts.Count).Select(i =>
        {
            var (name, when) = steps[step[i]](i);
            return new SessionName(name, when, facts[i].SheetName);
        })];
    }
}
