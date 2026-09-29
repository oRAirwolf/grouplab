using System.Globalization;

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
/// </summary>
public static class SessionNames
{
    /// <summary>The names of these sessions, in the order given, told apart from one another.</summary>
    public static IReadOnlyList<SessionName> For(IReadOnlyList<SessionSummary> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return For([.. sessions.Select(s => (s.Id, s.Load, s.ShotDate, s.CreatedUtc, s.SheetName))], zone);
    }

    /// <summary>The names of these sessions, in the order given, told apart from one another.</summary>
    public static IReadOnlyList<SessionName> For(IReadOnlyList<SessionRecord> sessions, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        return For([.. sessions.Select(s => (s.Id, s.Load, s.ShotDate, s.CreatedUtc, s.SheetName))], zone);
    }

    /// <summary>
    /// The names: the load where there is one, then the date where two would otherwise read the same (or always, where there is no load),
    /// then the time, then the record's number as a last resort.
    /// </summary>
    public static IReadOnlyList<SessionName> For(
        IReadOnlyList<(long Id, string? Load, string? ShotDate, string CreatedUtc, string SheetName)> sessions, TimeZoneInfo? zone = null)
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
            string? load = string.IsNullOrWhiteSpace(s.Load) ? null : s.Load.Trim();
            return (s.Id, Load: load, Date: date, Time: time, s.SheetName);
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
