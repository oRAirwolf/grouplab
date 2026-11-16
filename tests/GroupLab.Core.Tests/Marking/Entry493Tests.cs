using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 493, a tester's suggestion: a printer's check typed in from another GroupLab. Each checked printer shows both
/// numbers plainly in Printers and paper, Numbers from another GroupLab takes them as shown, a number outside what any check gives is refused
/// in plain words, the check says it was typed in wherever its scale is described, and it travels in Export all my data and in sync like a
/// measured one.
/// </summary>
public sealed partial class Entry493Tests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 11, 16);
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-493-{Guid.NewGuid():N}");

    public Entry493Tests() => Directory.CreateDirectory(root);

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    [GeneratedRegex(@"^(?<a>[0-9.]+)% across, (?<d>[0-9.]+)% down$")]
    private static partial Regex Copied();

    /// <summary>A caliper check on another device, as its two numbers would be copied from its Printers and paper.</summary>
    private static (PrinterProfile Measured, string Across, string Down) MeasuredElsewhere()
    {
        var measured = PrinterProfile.FromLengths("Office laser", PrinterMethod.Caliper, 148.84 / 25.4, 150 / 25.4, 149.12 / 25.4, 150 / 25.4, Day.AddDays(-3))!;
        var m = Copied().Match(measured.CopyWords);
        Assert.True(m.Success, measured.CopyWords);
        return (measured, m.Groups["a"].Value, m.Groups["d"].Value);
    }

    [Fact]
    public void ATypedCheckGivesTheScaleTheMeasuredOneShowed()
    {
        var (measured, across, down) = MeasuredElsewhere();
        Assert.Equal("To type into another GroupLab: 99.23% across, 99.41% down", measured.CopyLine);
        var (typed, why) = PrinterProfile.FromTyped("Office laser", across, down, Day);
        Assert.Null(why);
        Assert.NotNull(typed);
        Assert.Equal(PrinterMethod.Typed, typed.Method);
        // Two decimals of a percent: the same scale to within half of one hundredth of a percent.
        Assert.InRange(Math.Abs(typed.Across - measured.Across), 0, 0.00005);
        Assert.InRange(Math.Abs(typed.Down - measured.Down), 0, 0.00005);
        Assert.Equal(measured.CopyWords, typed.CopyWords);
        Assert.Equal(measured.Percentages, typed.Percentages);
        Assert.Equal(PrinterProfile.TypedUncertainty, typed.Uncertainty);
        // Typed with a percent sign, or with spaces, it is the same.
        Assert.Equal(typed.Across, PrinterProfile.FromTyped("Office laser", $" {across} % ", $"{down}%", Day).Profile!.Across);
    }

    [Fact]
    public void ANumberOutsideAnyCheckOrNoNumberIsRefusedInPlainWords()
    {
        var (low, lowWhy) = PrinterProfile.FromTyped("x", "80", "99.4", Day);
        Assert.Null(low);
        Assert.Equal("GroupLab takes a printer's scale between 85% and 115%, and 80% is outside it. Check the number on the other device.", lowWhy);
        Assert.Null(PrinterProfile.FromTyped("x", "99.4", "115.5", Day).Profile);
        var (none, noneWhy) = PrinterProfile.FromTyped("x", "ninety nine", "", Day);
        Assert.Null(none);
        Assert.StartsWith("Type each number as the other device shows it", noneWhy, StringComparison.Ordinal);
        // A fraction typed where a percentage is asked for is outside the range, not taken as 99 percent.
        Assert.Null(PrinterProfile.FromTyped("x", "0.9923", "0.9941", Day).Profile);
    }

    [Fact]
    public void ATypedCheckSaysSoWhereverItsScaleIsDescribed()
    {
        var (measured, across, down) = MeasuredElsewhere();
        var typed = PrinterProfile.FromTyped("Office laser", across, down, Day).Profile!;
        Assert.Equal("measured with a caliper on 13 November", measured.Measured);
        Assert.Equal("typed in from another device's check on 16 November", typed.Measured);
        Assert.Equal("Corrected for Office laser, 99.2 by 99.4%, typed in from another device's check on 16 November", typed.Line);
        Assert.Equal("Corrected for Office laser, 99.2 by 99.4%, checked 13 November", measured.Line);
        Assert.Contains(PrinterProfile.CopyLead, PrinterProfile.TypedHelp, StringComparison.Ordinal);
    }

    [Fact]
    public void ATypedCheckReadsBackTheSameFromItsSavedForm()
    {
        var (_, across, down) = MeasuredElsewhere();
        var typed = PrinterProfile.FromTyped("Office laser", across, down, Day).Profile!;
        Assert.Equal(typed, PrinterProfile.FromJson(JsonNode.Parse(typed.ToJson().ToJsonString())));
        // Saved as a card check marked typed: a build before entry 493, which reads no "typed", keeps it as a card check of the same scale
        // rather than dropping the printer, which its next save of any printer would have taken off every synced device.
        var saved = typed.ToJson();
        Assert.Equal("card", (string?)saved["method"]);
        Assert.True((bool?)saved["typed"]);
        saved.Remove("typed");
        var older = PrinterProfile.FromJson(saved)!;
        Assert.Equal(PrinterMethod.Card, older.Method);
        Assert.Equal((typed.Across, typed.Down, typed.Uncertainty), (older.Across, older.Down, older.Uncertainty));
    }

    [Fact]
    public void ATypedCheckTravelsInExportAllMyDataAndBackInImport()
    {
        var (_, across, down) = MeasuredElsewhere();
        var typed = PrinterProfile.FromTyped("Office laser", across, down, Day).Profile!;
        var store = SessionStore.Open(Path.Combine(root, "export.db"));
        using var file = new MemoryStream();
        DataExport.Write(file, store, [], new JsonObject { ["printers"] = new JsonArray(typed.ToJson()) }, "GroupLab test", new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc));
        file.Position = 0;
        var read = DataExport.Read(file);
        var printers = Assert.IsType<JsonArray>(read.Settings["printers"]);
        Assert.Equal(typed, PrinterProfile.FromJson(printers.Single()));

        var other = SessionStore.Open(Path.Combine(root, "import.db"));
        var plan = DataExport.Plan(read, other, [], []);
        Assert.Equal(typed, PrinterProfile.FromJson(Assert.IsType<JsonArray>(plan.NewSettings["printers"]).Single()));
    }

    [Fact]
    public void ATypedCheckArrivesOnASecondDeviceBySync()
    {
        var (_, across, down) = MeasuredElsewhere();
        var typed = PrinterProfile.FromTyped("Office laser", across, down, Day).Profile!;
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        var cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
        (SessionStore Store, FileSyncLocal Local) Device(string name)
        {
            string folder = Path.Combine(root, name);
            Directory.CreateDirectory(folder);
            return (SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")));
        }

        var (deskStore, desk) = Device("desk");
        var (phoneStore, phone) = Device("phone");
        {
            desk.SaveSetting("printers", new JsonArray(typed.ToJson()));
            Assert.True(SyncEngine.Run(deskStore, desk, cloud, "desk", new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc), null).Reached);
            Assert.True(SyncEngine.Run(phoneStore, phone, cloud, "phone", new DateTime(2026, 11, 16, 12, 1, 0, DateTimeKind.Utc), null).Applied >= 1);
            Assert.Equal(typed, PrinterProfile.FromJson(Assert.IsType<JsonArray>(phone.Settings()["printers"]).Single()));
        }
    }

    [Fact]
    public void ThePercentReadsAsAPersonTypesIt()
    {
        Assert.Equal(0.9923, PrinterProfile.ReadPercent("99.23")!.Value, 10);
        Assert.Equal(0.9923, PrinterProfile.ReadPercent("99.23%")!.Value, 10);
        Assert.Null(PrinterProfile.ReadPercent("-99"));
        Assert.Null(PrinterProfile.ReadPercent(null));
    }
}
