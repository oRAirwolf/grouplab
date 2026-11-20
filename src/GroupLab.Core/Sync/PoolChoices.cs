using System.Globalization;
using GroupLab.Core.Records;

namespace GroupLab.Core.Sync;

/// <summary>How one sheet of one version stands against the other version (entry 504 section 1).</summary>
public enum PoolSheetTag
{
    /// <summary>Both versions have this sheet: plain grey text, no fill.</summary>
    Same,

    /// <summary>The other version does not have it: the amber tag, the same amber as "differs", and counted.</summary>
    OnlyInThisOne,

    /// <summary>The sheet has not reached this device yet: grey fill, dashed border, never amber and never counted by itself.</summary>
    NotHereYet,
}

/// <summary>One sheet of a version, in the pool's order: its name as Session records names it, its short name for the phone, and its tag.</summary>
public sealed record PoolChoiceSheet(string Name, string ShortName, PoolSheetTag Tag);

/// <summary>One version's card: the device, when it was made or arrived, how it was pooled, whether that differs, and its sheets in order.</summary>
public sealed record PoolChoiceCard(string Device, string When, string PooledAs, bool PooledAsDiffers, IReadOnlyList<PoolChoiceSheet> Sheets);

/// <summary>
/// The choice between two versions of a pooled set: the line under the title (the computer's and the phone's), how many differences
/// there are (only the amber items), and the two cards, this device's first.
/// </summary>
public sealed record PoolChoice(string Line, string PhoneLine, int Differences, PoolChoiceCard Here, PoolChoiceCard There);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 504 section 1, concept A: the screen for a pooled set changed on two devices, replacing entry 498's plain
/// rows. Shared by the computer's dialog and the phone's "Choose a version" page, so both count and tag alike.
/// </summary>
public static class PoolChoices
{
    public const string PhoneTitle = "Choose a version";

    public const string PooledAsLabel = "Pooled as";

    public const string SheetsLabel = "Sheets, in order";

    public const string AsSet = "The sheets of one set";

    public const string AsPictures = "Pictures, each its own sheet";

    public const string KeepBothShort = "Keep both";

    /// <summary>The words of each tag.</summary>
    public static string Words(PoolSheetTag tag) => tag switch
    {
        PoolSheetTag.Same => "same",
        PoolSheetTag.OnlyInThisOne => "only in this one",
        _ => "not here yet",
    };

    /// <summary>This device's name on its card: "This computer (Desk)" or "This phone (Pixel)".</summary>
    public static string ThisDevice(string name, bool phone) => (phone ? "This phone" : "This computer") + $" ({name})";

    /// <summary>The button that keeps this device's version.</summary>
    public static string KeepThisDevice(bool phone) => phone ? "Keep this phone's" : "Keep this computer's";

    /// <summary>The button that keeps the other device's version: "Keep the Desk's" on the computer, "Keep Desk's" on the phone.</summary>
    public static string KeepOther(string device, bool phone) => phone ? $"Keep {device}'s" : $"Keep the {device}'s";

    /// <summary>
    /// The two versions compared. <paramref name="hereSheets"/> and <paramref name="thereSheets"/> are each version's sheets in its own order,
    /// as <see cref="SessionPools.InOrder"/> reads them on this device, null where a sheet has not reached it; <paramref name="here"/> is null
    /// where the pool is gone on this device.
    /// </summary>
    public static PoolChoice Build(PoolRecord? here, IReadOnlyList<SessionRecord?> hereSheets, PoolRecord there, IReadOnlyList<SessionRecord?> thereSheets,
        string hereDevice, string thereDevice, string foundUtc, bool phone, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(hereSheets);
        ArgumentNullException.ThrowIfNull(there);
        ArgumentNullException.ThrowIfNull(thereSheets);
        zone ??= TimeZoneInfo.Local;
        IReadOnlyList<string> hereKeys = here?.Sheets ?? [];
        if (here is null)
        {
            hereSheets = [];
        }

        // One name per sheet across both versions, so a sheet in both reads the same on both cards.
        var present = hereSheets.Concat(thereSheets).OfType<SessionRecord>().DistinctBy(s => s.Id).ToList();
        var names = SessionNames.For(present, zone);
        var nameOf = present.Select((s, i) => (s.Id, names[i].Name)).ToDictionary(p => p.Id, p => p.Name);

        var amber = new SortedSet<int>();
        IReadOnlyList<PoolChoiceSheet> Sheets(IReadOnlyList<string> keys, IReadOnlyList<SessionRecord?> records, IReadOnlyList<string> otherKeys)
        {
            var sheets = new List<PoolChoiceSheet>();
            for (int i = 0; i < keys.Count; i++)
            {
                var record = i < records.Count ? records[i] : null;
                int place = i + 1;
                string numbered = "Sheet " + place.ToString(CultureInfo.InvariantCulture);
                if (record is null)
                {
                    sheets.Add(new PoolChoiceSheet(numbered, numbered, PoolSheetTag.NotHereYet));
                    continue;
                }

                // A pool may name one sheet twice; the k-th time it is the same only where the other version names it k times too.
                int seen = keys.Take(i).Count(k => k == keys[i]);
                bool same = otherKeys.Count(k => k == keys[i]) > seen;
                if (!same)
                {
                    amber.Add(place);
                }

                sheets.Add(new PoolChoiceSheet(nameOf[record.Id], numbered + ", " + Day(record, zone, phone: true), same ? PoolSheetTag.Same : PoolSheetTag.OnlyInThisOne));
            }

            return sheets;
        }

        bool asDiffers = here is null || here.Choices != there.Choices;
        var mine = Sheets(hereKeys, hereSheets, there.Sheets);
        var theirs = Sheets(there.Sheets, thereSheets, hereKeys);
        var hereCard = new PoolChoiceCard(ThisDevice(hereDevice, phone), here is null ? "not on this device now" : "made " + When(here.CreatedUtc, zone, phone),
            here is null ? "none" : As(here), asDiffers, mine);
        var thereCard = new PoolChoiceCard(thereDevice, "arrived " + When(foundUtc, zone, phone), As(there), asDiffers, theirs);

        int count = (asDiffers ? 1 : 0) + amber.Count;
        string name = Name(present, there);
        var newest = present.OrderBy(s => s.ShotDate ?? s.CreatedUtc[..Math.Min(10, s.CreatedUtc.Length)], StringComparer.Ordinal).ThenBy(s => s.CreatedUtc, StringComparer.Ordinal).LastOrDefault();
        string longDate = newest is null ? DayOf(there.CreatedUtc, zone, false) : Day(newest, zone, false);
        string shortDate = newest is null ? DayOf(there.CreatedUtc, zone, true) : Day(newest, zone, true);

        string Items(bool small)
        {
            var items = new List<string>();
            if (asDiffers)
            {
                items.Add(small ? "how pooled" : "how the sheets were pooled");
            }

            if (amber.Count > 0)
            {
                var numbers = amber.Select(n => n.ToString(CultureInfo.InvariantCulture)).ToList();
                items.Add(numbers.Count == 1 ? "sheet " + numbers[0] : "sheets " + string.Join(", ", numbers.Take(numbers.Count - 1)) + " and " + numbers[^1]);
            }

            return string.Join(small ? ", " : ", and ", items);
        }

        string counted = count switch
        {
            0 => "No differences among the sheets here yet.",
            1 => "1 difference: ",
            _ => count.ToString(CultureInfo.InvariantCulture) + " differences: ",
        };
        string line = $"{name}, {longDate}. " + (count == 0 ? counted : counted + Items(false) + ".") + " " + SyncScreens.BothKept;
        string phoneLine = $"{name}, {shortDate}. " + (count == 0 ? counted : counted + Items(true) + ".");
        return new PoolChoice(line, phoneLine, count, hereCard, thereCard);
    }

    private static string As(PoolRecord pool) => pool.Pictures ? AsPictures : AsSet;

    /// <summary>The pool's name: the rifle and the load its sheets share, or "Pooled set of N sheets" where they share neither.</summary>
    private static string Name(IReadOnlyList<SessionRecord> sheets, PoolRecord there)
    {
        string? Shared(Func<SessionRecord, string?> of) =>
            sheets.Select(of).Select(v => string.IsNullOrWhiteSpace(v) ? null : v.Trim()).Distinct().ToList() is [{ } only] ? only : null;
        var parts = new[] { Shared(s => s.Rifle), Shared(s => s.Load) }.OfType<string>().ToList();
        return parts.Count > 0 ? string.Join(", ", parts) : SessionPools.RowName(there.Sheets.Count);
    }

    private static string Day(SessionRecord sheet, TimeZoneInfo zone, bool phone) =>
        sheet.ShotDate is { Length: >= 10 } shot && DateTime.TryParseExact(shot[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day.ToString(phone ? "d MMM" : "d MMMM", CultureInfo.InvariantCulture)
            : DayOf(sheet.CreatedUtc, zone, phone);

    private static string DayOf(string utc, TimeZoneInfo zone, bool phone) =>
        Local(utc, zone) is { } at ? at.ToString(phone ? "d MMM" : "d MMMM", CultureInfo.InvariantCulture) : utc;

    /// <summary>"19 November, 14:05" on the computer, "19 Nov, 14:05" on the phone.</summary>
    private static string When(string utc, TimeZoneInfo zone, bool phone) =>
        Local(utc, zone) is { } at ? at.ToString(phone ? "d MMM, HH:mm" : "d MMMM, HH:mm", CultureInfo.InvariantCulture) : utc;

    private static DateTime? Local(string utc, TimeZoneInfo zone) =>
        DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(at, DateTimeKind.Utc), zone)
            : null;
}
