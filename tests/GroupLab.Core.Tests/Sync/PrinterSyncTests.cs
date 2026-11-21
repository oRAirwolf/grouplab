using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 506 section 2, question 117 (a): each printer's check travels on its own, keyed by its name, with the whole
/// list still written beside it for GroupLab 1.0 (build 226) and the nightlies before this, which know only the list. Every mix of old and new
/// builds is here, a phone on 1.0 and a computer on a nightly among them, and in none may either lose a printer it did not delete. The older
/// build is played by <see cref="OldBuild"/>, which does exactly what 1.0 does with the list: writes it whole, takes a newer one whole, and
/// keeps its own where the two were changed apart.
/// </summary>
public sealed class PrinterSyncTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-printers-{Guid.NewGuid():N}");
    private readonly DirectorySyncFolder cloud;
    private DateTime clock = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    public PrinterSyncTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "cloud"));
        cloud = new DirectorySyncFolder(Path.Combine(root, "cloud"));
    }

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private sealed record Device(string Name, SessionStore Store, FileSyncLocal Local);

    private Device NewDevice(string name)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        return new Device(name, SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")));
    }

    private SyncReport Sync(Device d)
    {
        clock = clock.AddMinutes(1);
        return SyncEngine.Run(d.Store, d.Local, cloud, d.Name, clock);
    }

    private static readonly DateOnly Day = new(2026, 11, 10);

    private static JsonObject Printer(string name, double across, PrinterMethod method = PrinterMethod.Card, DateOnly? on = null) =>
        new PrinterProfile(name, across, across + 0.002, method, on ?? Day, 0.003).ToJson();

    private static void Save(Device d, params JsonObject[] printers) => d.Local.SaveSetting("printers", new JsonArray([.. printers]));

    private static List<PrinterProfile> Printers(Device d) =>
        d.Local.Settings()["printers"] is JsonArray all ? [.. all.Select(PrinterProfile.FromJson).OfType<PrinterProfile>()] : [];

    private static PrinterProfile? Named(Device d, string name) => Printers(d).SingleOrDefault(p => p.Name == name);

    /// <summary>
    /// GroupLab 1.0's handling of the <c>printers</c> setting, and of nothing else: its own list and the revision it last wrote or took, a
    /// change file holding the whole list, a newer list from another device taken whole, a list changed apart from its own left waiting (its
    /// own kept), and records of a kind it does not know left alone.
    /// </summary>
    private sealed class OldBuild(DirectorySyncFolder cloud, string id)
    {
        private readonly HashSet<string> seen = [];
        private int files;

        public JsonArray List { get; private set; } = [];

        public VersionVector Vector { get; private set; } = VersionVector.Empty;

        public int Waiting { get; private set; }

        public List<string> Names => [.. List.OfType<JsonObject>().Select(p => (string)p["name"]!)];

        public void Change(params JsonObject[] printers)
        {
            List = new JsonArray([.. printers]);
            Vector = Vector.Bump(id);
            var record = new JsonObject { ["kind"] = "setting", ["key"] = "printers", ["vector"] = Vector.ToJson(), ["data"] = new JsonObject { ["key"] = "printers", ["value"] = List.DeepClone() } };
            var file = new JsonObject { ["format"] = SyncEngine.Format, ["version"] = 1, ["device"] = id, ["records"] = new JsonArray(record), ["end"] = true };
            cloud.Write($"{SyncEngine.Root}/changes/{id}/20261121T120000{files++:000}Z-000-abcdef.json", Encoding.UTF8.GetBytes(file.ToJsonString()));
        }

        public void Read()
        {
            foreach (string device in cloud.Folders(SyncEngine.Root + "/changes").Where(d => d != id))
            {
                foreach (string name in cloud.Files($"{SyncEngine.Root}/changes/{device}").Where(n => n.EndsWith(".json", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
                {
                    if (!seen.Add(device + "/" + name) || JsonNode.Parse(cloud.Read($"{SyncEngine.Root}/changes/{device}/{name}")!) is not JsonObject file)
                    {
                        continue;
                    }

                    foreach (var record in ((JsonArray)file["records"]!).OfType<JsonObject>().Where(r => (string?)r["kind"] == "setting" && (string?)r["key"] == "printers"))
                    {
                        var vector = VersionVector.Parse(record["vector"]);
                        switch (vector.Compare(Vector))
                        {
                            case Order.Newer:
                                List = record["data"]?["value"] is JsonArray value ? (JsonArray)value.DeepClone() : [];
                                Vector = vector;
                                break;
                            case Order.Same or Order.Older:
                                break;
                            default:
                                Waiting++;
                                break;
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void APrinterAddedOrDeletedOnOneNewBuildIsAddedOrDeletedOnTheOtherAndStaysDeleted()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        Save(desk, Printer("Office laser", 0.992), Printer("Label printer", 1.004));
        Sync(desk);
        Sync(phone);
        Assert.Equal(["Label printer", "Office laser"], Printers(phone).Select(p => p.Name).Order());

        Save(desk, Printer("Office laser", 0.992));
        Sync(desk);
        Sync(phone);
        Assert.Equal(["Office laser"], Printers(phone).Select(p => p.Name));

        // The phone's next runs do not bring it back, on either device.
        Sync(phone);
        Sync(desk);
        Assert.Equal(["Office laser"], Printers(desk).Select(p => p.Name));
        Assert.Empty(desk.Store.SyncConflicts());
        Assert.Empty(phone.Store.SyncConflicts());
    }

    [Fact]
    public void TwoChecksOfOnePrinterWaitSideBySideAndKeepBothKeepsThemUnderTwoNames()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        Save(desk, Printer("Office laser", 0.992));
        Save(phone, Printer("Office laser", 0.981, PrinterMethod.Scan));
        Sync(desk);
        Sync(phone);

        // The phone keeps its own check in place, and the desk's waits beside it.
        Assert.Equal(0.981, Named(phone, "Office laser")!.Across, 6);
        var waiting = Assert.Single(phone.Store.SyncConflicts());
        Assert.Equal(SyncEngine.PrinterKind, waiting.Kind);
        var (here, there) = SyncEngine.PrinterVersions(phone.Local, waiting)!.Value;
        var choice = PrinterChoices.Build(here, there, "This phone (Pixel)", "Desk", waiting.FoundUtc, phone: true, TimeZoneInfo.Utc);
        Assert.Equal("98.10%", choice.Here.Rows[0].Value);
        Assert.Equal("99.20%", choice.There.Rows[0].Value);
        Assert.Equal("Scan", choice.Here.Rows[2].Value);
        Assert.Equal("Card", choice.There.Rows[2].Value);
        Assert.Equal(3, choice.Differences);

        SyncEngine.Resolve(phone.Store, phone.Local, waiting.Id, SyncChoice.KeepBoth, clock, "Desk");
        Assert.Equal(["Office laser", "Office laser (Desk)"], Printers(phone).Select(p => p.Name).Order());
        Assert.Empty(phone.Store.SyncConflicts());

        // The second name travels back, and the desk is asked nothing more.
        Sync(phone);
        Sync(desk);
        Assert.Equal(["Office laser", "Office laser (Desk)"], Printers(desk).Select(p => p.Name).Order());
        Assert.Equal(0.981, Named(desk, "Office laser")!.Across, 6);
        Assert.Equal(0.992, Named(desk, "Office laser (Desk)")!.Across, 6);
    }

    [Fact]
    public void UseTheOthersTakesItAndKeepsThisDevicesCheckWhereItCanBeGotBack()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        Save(desk, Printer("Office laser", 0.992));
        Save(phone, Printer("Office laser", 0.981));
        Sync(desk);
        Sync(phone);
        var waiting = Assert.Single(phone.Store.SyncConflicts());

        SyncEngine.Resolve(phone.Store, phone.Local, waiting.Id, SyncChoice.KeepTheOther, clock);
        Assert.Equal(0.992, Named(phone, "Office laser")!.Across, 6);
        Assert.Contains(phone.Store.Kept(), k => k.Kind == SyncEngine.PrinterKind && k.Payload.Contains("0.981", StringComparison.Ordinal));
        Sync(phone);
        Sync(desk);
        Assert.Equal(0.992, Named(desk, "Office laser")!.Across, 6);
        Assert.Empty(desk.Store.SyncConflicts());
    }

    [Fact]
    public void ThePhoneOnOnePointOhAndTheComputerOnANightlyBothEndWithEveryPrinter()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        phone.Change(Printer("Label printer", 1.004));
        Save(desk, Printer("Office laser", 0.992));

        Sync(desk);
        Assert.Equal(["Label printer", "Office laser"], Printers(desk).Select(p => p.Name).Order());

        // The computer's list goes back as the newer one, so 1.0 takes it whole and has both.
        phone.Read();
        Assert.Equal(["Label printer", "Office laser"], phone.Names.Order());
        Assert.Equal(0, phone.Waiting);
    }

    [Fact]
    public void OnePointOhLeavingAPrinterOutOfItsListNeverDeletesItOnTheComputer()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        Save(desk, Printer("Office laser", 0.992), Printer("Label printer", 1.004));
        Sync(desk);
        phone.Read();
        Assert.Equal(2, phone.Names.Count);

        phone.Change(Printer("Label printer", 1.004));
        Sync(desk);
        Assert.Equal(["Label printer", "Office laser"], Printers(desk).Select(p => p.Name).Order());
        phone.Read();
        Assert.Equal(["Label printer", "Office laser"], phone.Names.Order());
    }

    [Fact]
    public void APrinterDeletedOnTheComputerIsNotBroughtBackByOnePointOhsOlderList()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        Save(desk, Printer("Office laser", 0.992), Printer("Label printer", 1.004));
        Sync(desk);
        phone.Read();

        // Deleted on the computer while 1.0 changed its list apart, still holding the printer.
        Save(desk, Printer("Label printer", 1.004));
        Sync(desk);
        phone.Change(Printer("Office laser", 0.992), Printer("Label printer", 1.006, on: Day.AddDays(1)));
        Sync(desk);
        Assert.Null(Named(desk, "Office laser"));
        Assert.Equal(1.006, Named(desk, "Label printer")!.Across, 6);
    }

    [Fact]
    public void OnePointOhsNewerCheckReplacesTheComputersAndAnOlderOneDoesNot()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        Save(desk, Printer("Office laser", 0.992), Printer("Label printer", 1.004));
        Sync(desk);
        phone.Read();

        phone.Change(Printer("Office laser", 0.985, on: Day.AddDays(5)), Printer("Label printer", 1.010, on: Day.AddDays(-5)));
        Sync(desk);
        Assert.Equal(0.985, Named(desk, "Office laser")!.Across, 6);
        Assert.Equal(1.004, Named(desk, "Label printer")!.Across, 6);
        Assert.Contains(desk.Store.Kept(), k => k.Kind == SyncEngine.PrinterKind && k.Payload.Contains("0.992", StringComparison.Ordinal));
        Assert.Empty(desk.Store.SyncConflicts());

        phone.Read();
        Assert.Equal(1.004, PrinterProfile.FromJson(phone.List.OfType<JsonObject>().Single(p => (string)p["name"]! == "Label printer"))!.Across, 6);
    }

    [Fact]
    public void OnePointOhsCheckOfTheSameDayWaitsForThePersonAndTheComputerKeepsItsOwn()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        Save(desk, Printer("Office laser", 0.992));
        Sync(desk);
        phone.Read();

        phone.Change(Printer("Office laser", 0.981, PrinterMethod.Scan));
        Sync(desk);
        Assert.Equal(0.992, Named(desk, "Office laser")!.Across, 6);
        var waiting = Assert.Single(desk.Store.SyncConflicts());
        Assert.Equal(SyncEngine.PrinterKind, waiting.Kind);
        Assert.NotNull(SyncEngine.PrinterVersions(desk.Local, waiting));

        // Keep both: 1.0 then has both checks too, under the two names.
        SyncEngine.Resolve(desk.Store, desk.Local, waiting.Id, SyncChoice.KeepBoth, clock, "Phone");
        Sync(desk);
        phone.Read();
        Assert.Equal(["Office laser", "Office laser (Phone)"], phone.Names.Order());
    }

    [Fact]
    public void TwoNightliesMergingOnePointOhsListAreNotAskedAboutTheSamePrinter()
    {
        var phone = new OldBuild(cloud, "phone-1-0");
        var desk = NewDevice("desk");
        var laptop = NewDevice("laptop");
        phone.Change(Printer("Label printer", 1.004));
        Sync(desk);
        Sync(laptop);
        Sync(desk);
        Sync(laptop);
        Assert.Equal("Label printer", Assert.Single(Printers(desk)).Name);
        Assert.Equal("Label printer", Assert.Single(Printers(laptop)).Name);
        Assert.Empty(desk.Store.SyncConflicts());
        Assert.Empty(laptop.Store.SyncConflicts());
    }

    [Fact]
    public void TheNameKeptBesideTakesANumberWhereThatNameIsTaken()
    {
        Assert.Equal("Office laser (Desk)", PrinterChoices.BothName("Office laser", "Desk", ["Office laser"]));
        Assert.Equal("Office laser (Desk) 2", PrinterChoices.BothName("Office laser", "Desk", ["Office laser", "office laser (desk)"]));
        Assert.Equal("Use this computer's", PrinterChoices.UseThisDevice(phone: false));
        Assert.Equal("Use Desk's", PrinterChoices.UseOther("Desk", phone: true));
    }
}
