using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Sync;

/// <summary>One row of a check's card: what it is, its value, and whether the other check's differs.</summary>
public sealed record PrinterChoiceRow(string What, string Value, bool Differs);

/// <summary>One check's card: the device, when the check was made, and its across, down and how-made rows.</summary>
public sealed record PrinterChoiceCard(string Device, string When, IReadOnlyList<PrinterChoiceRow> Rows);

/// <summary>The choice between two checks of one printer: its name, the line under the title, how many rows differ, and the two cards, this device's first.</summary>
public sealed record PrinterChoice(string Printer, string Line, string PhoneLine, int Differences, PrinterChoiceCard Here, PrinterChoiceCard There);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 506 section 2, in the pattern Alan chose for a pooled set changed on two devices (entry 504, concept A): two
/// checks of one printer side by side, each with the device it came from and when, its across and down numbers and how it was made, and the
/// choices Use this device's, Use the other's, Keep both under two names, Decide later. Shared by the computer's dialog and the phone's page.
/// </summary>
public static class PrinterChoices
{
    public const string Title = "Two checks of this printer";

    public const string PhoneTitle = "Choose a check";

    public const string AcrossLabel = "Across";

    public const string DownLabel = "Down";

    public const string MadeLabel = "How it was made";

    public const string KeepBoth = "Keep both under two names";

    public const string KeepBothShort = "Keep both";

    /// <summary>The button that uses this device's check.</summary>
    public static string UseThisDevice(bool phone) => phone ? "Use this phone's" : "Use this computer's";

    /// <summary>The button that uses the other device's check: "Use the Desk's" on the computer, "Use Desk's" on the phone.</summary>
    public static string UseOther(string device, bool phone) => phone ? $"Use {device}'s" : $"Use the {device}'s";

    /// <summary>How a check was made, in a word or two: card, scan, typed, as entry 506 names them.</summary>
    public static string Made(PrinterMethod method) => method switch
    {
        PrinterMethod.Card => "Card",
        PrinterMethod.Scan => "Scan",
        PrinterMethod.Typed => "Typed in",
        PrinterMethod.Caliper => "Caliper",
        _ => "Ruler",
    };

    /// <summary>
    /// The two checks compared. <paramref name="here"/> is null where the printer is gone on this device; <paramref name="foundUtc"/> is when the
    /// other check arrived, which the line names.
    /// </summary>
    public static PrinterChoice Build(JsonObject? here, JsonObject there, string hereDevice, string thereDevice, string foundUtc, bool phone, TimeZoneInfo? zone = null)
    {
        ArgumentNullException.ThrowIfNull(there);
        var mine = here is null ? null : PrinterProfile.FromJson(here);
        var theirs = PrinterProfile.FromJson(there) ?? throw new ArgumentException("the other device's check is not a printer", nameof(there));
        static string Percent(double v) => string.Create(CultureInfo.InvariantCulture, $"{v * 100:0.00}%");
        bool acrossDiffers = mine is null || Percent(mine.Across) != Percent(theirs.Across);
        bool downDiffers = mine is null || Percent(mine.Down) != Percent(theirs.Down);
        bool madeDiffers = mine is null || Made(mine.Method) != Made(theirs.Method);
        PrinterChoiceCard Card(PrinterProfile? p, string device) => p is null
            ? new PrinterChoiceCard(device, phone ? "Deleted" : "Deleted on this device", [])
            : new PrinterChoiceCard(device, Day(p.MeasuredOn, phone),
            [
                new PrinterChoiceRow(AcrossLabel, Percent(p.Across), acrossDiffers),
                new PrinterChoiceRow(DownLabel, Percent(p.Down), downDiffers),
                new PrinterChoiceRow(MadeLabel, Made(p.Method), madeDiffers),
            ]);

        int differences = mine is null ? 1 : (acrossDiffers ? 1 : 0) + (downDiffers ? 1 : 0) + (madeDiffers ? 1 : 0);
        string arrived = When(foundUtc, zone ?? TimeZoneInfo.Local, phone);
        string line = $"{theirs.Name} was checked on two devices; {thereDevice}'s check arrived {arrived}. Choose which numbers GroupLab corrects this printer's sheets with. "
            + SyncScreens.BothKept;
        string phoneLine = $"{theirs.Name}, checked on two devices. Which numbers should GroupLab use?";
        return new PrinterChoice(theirs.Name, line, phoneLine, differences, Card(mine, hereDevice), Card(theirs, thereDevice));
    }

    /// <summary>
    /// The name the other device's check is kept under beside this one: "Office laser (Desk)", then "Office laser (Desk) 2" and on where that
    /// is taken too.
    /// </summary>
    public static string BothName(string name, string device, IEnumerable<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);
        var names = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        string first = $"{name} ({device})";
        if (!names.Contains(first))
        {
            return first;
        }

        for (int n = 2; ; n++)
        {
            string next = string.Create(CultureInfo.InvariantCulture, $"{first} {n}");
            if (!names.Contains(next))
            {
                return next;
            }
        }
    }

    private static string Day(DateOnly day, bool phone) =>
        (phone ? "Checked " : "Checked on ") + day.ToString(phone ? "d MMM" : "d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string When(string utc, TimeZoneInfo zone, bool phone) =>
        DateTime.TryParse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(at, DateTimeKind.Utc), zone).ToString(phone ? "d MMM, HH:mm" : "d MMMM, HH:mm", CultureInfo.InvariantCulture)
            : utc;
}
