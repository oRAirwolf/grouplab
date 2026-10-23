using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Core.Sync;

/// <summary>What one device keeps about its synchronization, beside its settings, never synchronized itself (it is this device's).</summary>
/// <param name="Folder">The folder on the computer; on a phone the platform keeps its own handle and this holds the name shown.</param>
/// <param name="DeviceName">This device's name as the other devices see it.</param>
/// <param name="Originals">Whether the original photographs travel too (entry 463 section 3, the second card).</param>
/// <param name="LastUtc">When the last run that reached the folder finished.</param>
/// <param name="LastIn">How many changes it brought in.</param>
/// <param name="LastOut">How many it wrote.</param>
/// <param name="TidiedUtc">When tidying last moved or took away anything (entry 464 section 1), and what: <paramref name="TidiedFiles"/>
/// old change files of <paramref name="TidiedBytes"/> moved to the folder's trash, and <paramref name="FreedBytes"/> taken away from it.</param>
public sealed record SyncSetup(string? Folder, string DeviceName, bool Originals = false, string? LastUtc = null, int LastIn = 0, int LastOut = 0,
    string? TidiedUtc = null, int TidiedFiles = 0, long TidiedBytes = 0, long FreedBytes = 0)
{
    public bool On => !string.IsNullOrEmpty(Folder);
}

/// <summary>Another device seen in the folder, by its name in the folder (<paramref name="Id"/>) and as it calls itself, and when it last wrote.</summary>
public sealed record SyncDeviceSeen(string Id, string Name, DateTime? LastWrittenUtc);

/// <summary>One row of the side by side: what it is, each version's value, and whether they differ.</summary>
public sealed record SyncRow(string What, string Here, string There, bool Differs);

/// <summary>
/// Entry 463 section 2 (2A): a session changed on two devices, laid out for the side by side: the heading's rifle or load, sheet and date,
/// each version's device and time, its holes as offsets for a small plot, and the rows, each that differs marked.
/// </summary>
public sealed record SyncComparison(
    string Heading,
    string HereDevice,
    string HereWhen,
    string ThereDevice,
    string ThereWhen,
    IReadOnlyList<PointD> HereHoles,
    IReadOnlyList<PointD> ThereHoles,
    IReadOnlyList<SyncRow> Rows);

/// <summary>One of the two cards of entry 463 section 3 (3B): its size now and a year at the person's pace.</summary>
public sealed record SyncCard(string Title, string Says, long NowBytes, long YearBytes);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 463: the words and figures of the three synchronization screens Alan chose (1A, 2A, 3B), shared by the
/// computer and the phone, so both say the same.
/// </summary>
public static class SyncScreens
{
    public const string Title = "Sync between devices";

    public const string Explained = "GroupLab keeps your sessions, rifles, loads and sheets the same on each of your devices through a folder your own cloud app " +
        "already keeps in step, such as OneDrive, Dropbox, Google Drive or iCloud Drive. There is no GroupLab account; nothing goes anywhere but that folder.";

    public const string ChooseFolder = "Choose a folder";

    public const string ChangeFolder = "Change folder";

    public const string DeviceNameLabel = "This device's name, as the others see it";

    public const string SyncNow = "Sync now";

    public const string OtherDevicesHeading = "Other devices in the folder";

    public const string NoOtherDevices = "No other device has written to this folder yet. Choose the same folder on another device to start.";

    /// <summary>Entry 471 section 2, question 109: beside each other device, for one that will not come back.</summary>
    public const string ForgetDevice = "Forget this device";

    public const string LeaveIt = "Leave it as it is";

    /// <summary>What forgetting <paramref name="name"/> does, asked once on the page before it is done.</summary>
    public static string ForgetAsks(string name) =>
        $"Forget {name}? Use this for a device that will not sync again. GroupLab stops waiting for it before tidying old change files. " +
        $"Nothing in the folder is deleted: {name}'s records stay, and if it syncs again it is listed here again.";

    public const string OriginalsHeading = "Also copy the original photographs";

    public const string StopSyncing = "Stop syncing";

    public const string StopsHereOnly = "Stops on this device only. Nothing here or in the folder is deleted.";

    public const string UnreachableTitle = "The sync folder cannot be reached";

    public const string NothingLost = "Nothing is lost: changes made here wait on this device and go as soon as the folder is back.";

    public const string ChooseAgain = "Choose the folder again";

    public const string ConflictTitle = "This session was changed on two devices";

    public const string BothKept = "Both versions are kept until you choose.";

    public const string KeepBoth = "Keep both as two sessions";

    public const string DecideLater = "Decide later";

    public const string Differs = "differs";

    /// <summary>The tag on a session waiting to be chosen between, on Sessions.</summary>
    public const string TwoVersions = "2 versions";

    /// <summary>The likely reason the folder cannot be reached, in plain words.</summary>
    public static string LikelyReason(bool phone) => phone
        ? "The cloud app that keeps it may have signed out, or the folder was moved or deleted."
        : "The drive it is on may be unplugged, its cloud app signed out, or the folder moved or deleted.";

    /// <summary>The Sessions banner: "1 session to choose between".</summary>
    public static string ToChooseBanner(int n) => n == 1 ? "1 session to choose between" : $"{n} sessions to choose between";

    public static string KeepThis(string device) => $"Keep {device}'s";

    /// <summary>"Synced 2 minutes ago: 3 changes in, 1 out." Null before the first run.</summary>
    public static string? Status(SyncSetup setup, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(setup);
        if (setup.LastUtc is not { } last || !DateTime.TryParse(last, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at))
        {
            return setup.On ? "Not synced yet." : null;
        }

        string changes = setup.LastIn == 0 && setup.LastOut == 0
            ? "nothing new either way"
            : $"{setup.LastIn} change{(setup.LastIn == 1 ? "" : "s")} in, {setup.LastOut} out";
        return $"Synced {Ago(at, nowUtc)}: {changes}.";
    }

    /// <summary>
    /// Entry 464 section 1, what a tidying did, in a line: "Tidied: 12 old change files, 3.4 MB, moved to the sync folder's trash for 14 days;
    /// 1.2 MB freed from the trash." Null when it did nothing. <paramref name="ago"/>, where given, says when.
    /// </summary>
    public static string? Tidied(SyncTidied tidied, string? ago = null)
    {
        ArgumentNullException.ThrowIfNull(tidied);
        var parts = new List<string>();
        if (tidied.Moved > 0)
        {
            parts.Add($"{tidied.Moved} old change file{(tidied.Moved == 1 ? "" : "s")}, {Size(tidied.MovedBytes)}, moved to the sync folder's trash for {SyncEngine.TrashDays} days");
        }

        if (tidied.Removed > 0)
        {
            parts.Add($"{Size(tidied.FreedBytes)} freed from the trash");
        }

        return parts.Count == 0 ? null : $"Tidied{(ago is null ? "" : " " + ago)}: {string.Join("; ", parts)}.";
    }

    /// <summary>The Sync block's line for the last tidying that did anything, with when; null before there was one.</summary>
    public static string? TidyLine(SyncSetup setup, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(setup);
        return setup.TidiedUtc is { } utc && DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? Tidied(new SyncTidied(setup.TidiedFiles, setup.TidiedBytes, setup.FreedBytes > 0 ? 1 : 0, setup.FreedBytes), Ago(at, nowUtc))
            : null;
    }

    /// <summary>What a run's tidying did, kept on the setup where it did anything, so the Sync block says it until the next one that does.</summary>
    public static SyncSetup WithTidied(SyncSetup setup, SyncReport report, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(report);
        return report.Tidied is { Any: true } t
            ? setup with { TidiedUtc = nowUtc.ToString("o", CultureInfo.InvariantCulture), TidiedFiles = t.Moved, TidiedBytes = t.MovedBytes, FreedBytes = t.FreedBytes }
            : setup;
    }

    /// <summary>A different folder chosen: what tidying did in the last one is no longer said.</summary>
    public static SyncSetup InFolder(SyncSetup setup, string folder)
    {
        ArgumentNullException.ThrowIfNull(setup);
        return setup with { Folder = folder, TidiedUtc = null, TidiedFiles = 0, TidiedBytes = 0, FreedBytes = 0 };
    }

    /// <summary>"just now", "2 minutes ago", "3 hours ago", "4 days ago".</summary>
    public static string Ago(DateTime thenUtc, DateTime nowUtc)
    {
        var gone = nowUtc - thenUtc;
        static string Of(int n, string unit) => $"{n} {unit}{(n == 1 ? "" : "s")} ago";
        return gone.TotalMinutes < 1 ? "just now"
            : gone.TotalHours < 1 ? Of((int)gone.TotalMinutes, "minute")
            : gone.TotalDays < 1 ? Of((int)gone.TotalHours, "hour")
            : Of((int)gone.TotalDays, "day");
    }

    /// <summary>The other devices that have written to the folder, newest first; this one and the forgotten ones (entry 471) left out.</summary>
    public static IReadOnlyList<SyncDeviceSeen> Others(ISyncFolder folder, string me)
    {
        ArgumentNullException.ThrowIfNull(folder);
        var seen = new List<SyncDeviceSeen>();
        if (!folder.Available)
        {
            return seen;
        }

        foreach (string file in folder.Files($"{SyncEngine.Root}/devices"))
        {
            string id = Path.GetFileNameWithoutExtension(file);
            if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || file.StartsWith('.') || id == me || SyncForget.IsForgotten(folder, id)
                || folder.Read($"{SyncEngine.Root}/devices/{file}") is not { } bytes)
            {
                continue;
            }

            try
            {
                var device = JsonNode.Parse(bytes)?.AsObject();
                string name = (string?)device?["name"] is { Length: > 0 } n ? n : id;
                DateTime? when = DateTime.TryParse((string?)device?["lastWrittenUtc"], CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var w) ? w : null;
                seen.Add(new SyncDeviceSeen(id, name, when));
            }
            catch (System.Text.Json.JsonException)
            {
                // A device file another device is still writing is read next time.
            }
        }

        return [.. seen.OrderByDescending(d => d.LastWrittenUtc ?? DateTime.MinValue)];
    }

    /// <summary>A device seen, in a line: "Alan's phone, last wrote 2 hours ago".</summary>
    public static string SeenLine(SyncDeviceSeen device, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(device);
        return device.LastWrittenUtc is { } at ? $"{device.Name}, last wrote {Ago(at, nowUtc)}" : device.Name;
    }

    /// <summary>The side by side for a session changed on two devices; the other device's name comes from the folder where it is known.</summary>
    public static SyncComparison Compare(SessionRecord? here, SessionRecord there, string hereDevice, string thereDevice, string foundUtc, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(there);
        ArgumentNullException.ThrowIfNull(units);
        var (hereHoles, hereIds, hereNotes) = Holes(here);
        var (thereHoles, thereIds, thereNotes) = Holes(there);
        var rows = new List<SyncRow>();
        void Row(string what, string? a, string? b) => rows.Add(new SyncRow(what, a ?? "none", b ?? "none", !string.Equals(a ?? "", b ?? "", StringComparison.Ordinal)));

        // Holes: the counts, and where they differ, what: added, taken away, moved.
        var said = new List<string>();
        int added = thereIds.Keys.Count(k => !hereIds.ContainsKey(k)), gone = hereIds.Keys.Count(k => !thereIds.ContainsKey(k));
        if (added > 0)
        {
            said.Add(added == 1 ? "one added" : $"{added} added");
        }

        if (gone > 0)
        {
            said.Add(gone == 1 ? "one taken away" : $"{gone} taken away");
        }

        foreach (var (id, at) in thereIds.Where(p => hereIds.ContainsKey(p.Key)).OrderBy(p => p.Key))
        {
            var was = hereIds[id];
            double moved = Math.Sqrt(((at.X - was.X) * (at.X - was.X)) + ((at.Y - was.Y) * (at.Y - was.Y)));
            if (moved >= 0.005)
            {
                said.Add($"hole {id} moved {units.Length(moved)}");
            }
        }

        string thereHolesText = thereHoles.Count.ToString(CultureInfo.InvariantCulture) + (said.Count > 0 ? ": " + string.Join(", ", said) : "");
        rows.Add(new SyncRow("Holes", here is null ? "none" : hereHoles.Count.ToString(CultureInfo.InvariantCulture), thereHolesText, said.Count > 0 || here is null));
        Row("Mean radius", here?.MeanRadiusInches is { } a ? units.Length(a) : null, there.MeanRadiusInches is { } b ? units.Length(b) : null);
        Row("Rifle", here?.Rifle, there.Rifle);
        Row("Barrel", here?.Barrel, there.Barrel);
        Row("Load", here?.Load, there.Load);
        Row("Caliber", here?.CalibreInches is { } c ? units.Length(c) : null, there.CalibreInches is { } d ? units.Length(d) : null);
        Row("Distance", here?.DistanceInches is { } e ? units.DistanceText(e) : null, there.DistanceInches is { } f ? units.DistanceText(f) : null);
        Row("Notes", hereNotes, thereNotes);

        string heading = string.Join(", ", new[] { there.Load ?? there.Rifle, there.SheetName, there.ShotDate ?? there.CreatedUtc[..Math.Min(10, there.CreatedUtc.Length)] }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        return new SyncComparison(heading, hereDevice, When(here?.CreatedUtc), thereDevice, When(foundUtc), hereHoles, thereHoles, rows);
    }

    private static string When(string? utc) =>
        utc is not null && DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? at.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "";

    /// <summary>A session's counted holes as offsets in inches about their own bulls, by shot number, and its notes in a line.</summary>
    private static (IReadOnlyList<PointD> Holes, Dictionary<int, PointD> ById, string? Notes) Holes(SessionRecord? session)
    {
        if (session is null)
        {
            return ([], [], null);
        }

        try
        {
            var (state, notes) = MarkingFile.Read(session.MarkingJson);
            var shots = state.Shots.Where(s => s.IsShot && s.Exclusion is null).ToList();
            var offsets = GroupAnalysis.CompositeOffsets(state, shots);
            var byId = new Dictionary<int, PointD>();
            for (int i = 0; i < shots.Count && i < offsets.Count; i++)
            {
                byId[shots[i].Id] = offsets[i];
            }

            return (offsets, byId, notes.Count > 0 ? string.Join(" ", notes) : null);
        }
        catch (Exception e) when (e is MarkingFileException or System.Text.Json.JsonException or InvalidDataException)
        {
            return ([], [], null);
        }
    }

    /// <summary>
    /// Entry 463 section 3 (3B): the two cards, worked out from this device's own records. The year is at the person's own rate of sessions
    /// over their records where there are at least ten over at least sixty days, or 200 a year otherwise, and <paramref name="basis"/> says
    /// which.
    /// </summary>
    public static (SyncCard Marks, SyncCard Originals, string Basis) Cards((int Sessions, long MarksBytes, long OriginalsBytes, DateTime? First, DateTime? Last) measured)
    {
        var (n, marks, originals, first, last) = measured;
        double days = first is { } f && last is { } l ? (l - f).TotalDays : 0;
        bool own = n >= 10 && days >= 60;
        double perYear = own ? n / days * 365.25 : 200;
        double perSessionMarks = n > 0 ? (double)marks / n : 60_000, perSessionOriginals = n > 0 && originals > 0 ? (double)originals / n : 4_000_000;
        string basis = own
            ? $"A year at your pace is about {perYear:0} sessions, from your {n} over {days:0} days."
            : $"A year at your pace assumes 200 sessions, since there are too few here to tell your own pace yet.";
        var marksCard = new SyncCard("Marks and proof pictures",
            "Every session, its sheet and a proof picture: enough to see and work with it anywhere.", marks, (long)(perSessionMarks * perYear));
        var originalsCard = new SyncCard("Also the original photographs",
            "The same, plus each photograph and scan as taken, so another device can open the full picture.",
            marks + originals, (long)((perSessionMarks + perSessionOriginals) * perYear));
        return (marksCard, originalsCard, basis);
    }

    /// <summary>The line under the cards: the bars' scale, and that a changed session is written again whole.</summary>
    public static string ScaleLine(long largest) =>
        $"Each row's bars are on one scale, the longest {Size(largest)}. A session changed again is written again whole, so the folder grows " +
        "by a few times its size until old copies are tidied.";

    /// <summary>A size in words: "48 KB", "3.2 MB", "1.4 GB".</summary>
    public static string Size(long bytes) => bytes switch
    {
        < 1_000_000 => $"{Math.Max(1, (int)Math.Round(bytes / 1000.0))} KB",
        < 1_000_000_000 => (bytes / 1e6).ToString(bytes < 10_000_000 ? "0.0" : "0", CultureInfo.InvariantCulture) + " MB",
        _ => (bytes / 1e9).ToString("0.0", CultureInfo.InvariantCulture) + " GB",
    };
}
