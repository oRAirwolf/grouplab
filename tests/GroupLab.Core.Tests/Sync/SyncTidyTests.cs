using System.Security.Cryptography;
using System.Text.Json.Nodes;
using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 464 section 1: tidying old change files. A device moves a change file of its own to the sync folder's trash
/// only when every other device's read position, written by that device into the folder, lists it as read, a later file of its own carries a
/// newer version of every record in it, and none of those records waits for the person on any device; the trash is emptied 14 days after a
/// file was moved, and a folder that goes away part way leaves every file either moved or where it was.
/// </summary>
public sealed class SyncTidyTests : IDisposable
{
    private static readonly DateTime Start = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-tidy-{Guid.NewGuid():N}");
    private readonly DirectorySyncFolder cloud;
    private DateTime clock = DateTime.MinValue;

    public SyncTidyTests()
    {
        Directory.CreateDirectory(Cloud);
        cloud = new DirectorySyncFolder(Cloud);
    }

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private string Cloud => Path.Combine(root, "cloud");

    private sealed record Device(string Name, SessionStore Store, FileSyncLocal Local)
    {
        public string Id => Store.SyncDevice();
    }

    private Device NewDevice(string name)
    {
        string folder = Path.Combine(root, name);
        Directory.CreateDirectory(folder);
        return new Device(name, SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")));
    }

    /// <summary>One run on <paramref name="day"/> days after the start, a minute later than the last run that day.</summary>
    private SyncReport Sync(Device d, double day, ISyncFolder? folder = null)
    {
        var at = Start.AddDays(day);
        clock = at > clock ? at : clock.AddMinutes(1);
        return SyncEngine.Run(d.Store, d.Local, folder ?? cloud, d.Name, clock);
    }

    private static SessionRecord Session(string created, string marking = "{\"marks\":0}") => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        marking, 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    private static void Edit(Device d, string created, string marking)
    {
        var s = d.Store.List().Select(x => d.Store.Get(x.Id)!).Single(x => x.CreatedUtc == created);
        d.Store.Save(s with { MarkingJson = marking });
    }

    private string Folder(string part, Device d) => Path.Combine(Cloud, SyncEngine.Root, part, d.Id);

    private List<string> Changes(Device d) => Directory.Exists(Folder("changes", d)) ? [.. Directory.GetFiles(Folder("changes", d)).Select(f => Path.GetFileName(f)!).Order(StringComparer.Ordinal)] : [];

    private List<string> Trash(Device d) => Directory.Exists(Folder("trash", d)) ? [.. Directory.GetFiles(Folder("trash", d)).Select(f => Path.GetFileName(f)!).Order(StringComparer.Ordinal)] : [];

    private static string Marking(Device d, string created) => d.Store.List().Select(x => d.Store.Get(x.Id)!).Single(x => x.CreatedUtc == created).MarkingJson;

    private static HashSet<string> Hashes(string folder) => Directory.Exists(folder)
        ? [.. Directory.GetFiles(folder).Where(f => !Path.GetFileName(f).StartsWith('.')).Select(f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))))]
        : [];

    [Fact]
    public void TwoDevicesOneOfflineForAMonthKeepEveryFileItHasNotReadAndTidyOnceItHas()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string made = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(made));
        Sync(desk, 0);
        Sync(phone, 0);
        Assert.True(File.Exists(Path.Combine(Cloud, SyncEngine.Root, "read", phone.Id + ".json")), "the phone writes what it has read into the folder");

        // The phone goes away for a month; the desk changes the session every day.
        var reports = new List<SyncReport>();
        for (int day = 1; day <= 30; day++)
        {
            Edit(desk, made, $"{{\"marks\":{day}}}");
            reports.Add(Sync(desk, day));
        }

        // Only the first file, which the phone read before it went, was tidied (on day 1), and it left the trash 14 days after it was moved.
        Assert.Equal(1, reports[0].Tidied!.Moved);
        Assert.All(reports.Skip(1), r => Assert.Equal(0, r.Tidied!.Moved));
        Assert.Equal(1, reports[14].Tidied!.Removed);
        Assert.All(reports.Take(14), r => Assert.Equal(0, r.Tidied!.Removed));
        Assert.Equal(30, Changes(desk).Count);
        Assert.Empty(Trash(desk));

        // Back after a month, the phone reads all thirty and ends with the newest, with nothing to choose.
        var back = Sync(phone, 31);
        Assert.Equal(30, back.FilesRead);
        Assert.Equal("{\"marks\":30}", Marking(phone, made));
        Assert.Empty(phone.Store.SyncConflicts());

        // Now every file but the newest has been read and has a newer version after it.
        var tidy = Sync(desk, 31);
        Assert.Equal(29, tidy.Tidied!.Moved);
        Assert.True(tidy.Tidied.MovedBytes > 0);
        Assert.Contains(tidy.Said, s => s.StartsWith("Tidied: 29 old change files, ", StringComparison.Ordinal) && s.Contains("moved to the sync folder's trash for 14 days", StringComparison.Ordinal));
        Assert.Single(Changes(desk));
        Assert.Equal(29, Trash(desk).Count);

        // A device that joins now still ends with the newest version.
        var late = NewDevice("tablet");
        Sync(late, 32);
        Assert.Equal("{\"marks\":30}", Marking(late, made));

        // The trash is emptied by when the files were moved, not by their own dates: their names say days 2 to 30, all over 14 days ago.
        Assert.Equal(0, Sync(desk, 44).Tidied!.Removed);
        Assert.Equal(29, Trash(desk).Count);
        var emptied = Sync(desk, 45.1);
        Assert.Equal(29, emptied.Tidied!.Removed);
        Assert.Contains(emptied.Said, s => s.Contains("freed from the trash", StringComparison.Ordinal));
        Assert.Empty(Trash(desk));
    }

    [Fact]
    public void ADeviceNotSeenHoldsEveryTidyingBack()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string made = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(made));
        Sync(desk, 0);
        Sync(phone, 0);

        // A third device has written its name to the folder but no read position, as a GroupLab from before tidying would.
        const string old = "0123456789ab";
        string devices = Path.Combine(Cloud, SyncEngine.Root, "devices");
        File.WriteAllText(Path.Combine(devices, old + ".json"), "{\"format\":\"grouplab-sync\",\"version\":1,\"device\":\"0123456789ab\",\"name\":\"Old laptop\"}");
        Edit(desk, made, "{\"marks\":1}");
        Sync(desk, 1);
        Sync(phone, 1);
        Assert.Equal(0, Sync(desk, 2).Tidied!.Moved);
        Assert.Equal(2, Changes(desk).Count);

        // A position that cannot be read, or is not whole, is the same as none.
        string read = Path.Combine(Cloud, SyncEngine.Root, "read");
        File.WriteAllText(Path.Combine(read, old + ".json"), "{\"format\":\"grouplab-sync-read\",");
        Assert.Equal(0, Sync(desk, 3).Tidied!.Moved);
        var files = Changes(desk);
        var list = new JsonArray([.. files.Select(f => (JsonNode)f)]);
        File.WriteAllText(Path.Combine(read, old + ".json"), new JsonObject
        {
            ["format"] = "grouplab-sync-read", ["version"] = 1, ["device"] = old, ["read"] = new JsonObject { [desk.Id] = list }, ["waiting"] = new JsonArray(),
        }.ToJsonString());
        Assert.Equal(0, Sync(desk, 4).Tidied!.Moved);
        Assert.Equal(2, Changes(desk).Count);

        // Once it says, whole, that it has read them, the old one goes.
        File.WriteAllText(Path.Combine(read, old + ".json"), new JsonObject
        {
            ["format"] = "grouplab-sync-read", ["version"] = 1, ["device"] = old, ["read"] = new JsonObject { [desk.Id] = list.DeepClone() }, ["waiting"] = new JsonArray(), ["end"] = true,
        }.ToJsonString());
        Assert.Equal(1, Sync(desk, 5).Tidied!.Moved);
        Assert.Equal([files[^1]], Changes(desk));
    }

    [Fact]
    public void NothingWaitingForAChoiceOnAnyDeviceIsTidied()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string made = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(made));
        Sync(desk, 0);
        Sync(phone, 0);

        // Changed on both since they last met: waiting on the desk, and then on the phone.
        Edit(phone, made, "{\"marks\":\"phone\"}");
        Sync(phone, 1);
        Edit(desk, made, "{\"marks\":\"desk\"}");
        Assert.Equal(0, Sync(desk, 1).Tidied!.Moved);
        Assert.Single(desk.Store.SyncConflicts());
        Sync(phone, 2);
        Assert.Single(phone.Store.SyncConflicts());
        string position = File.ReadAllText(Path.Combine(Cloud, SyncEngine.Root, "read", phone.Id + ".json"));
        Assert.Contains("\"waiting\":[{\"kind\":\"session\"", position, StringComparison.Ordinal);

        // The desk changes it again: its first two files are read by the phone and have a newer version after them, and still stay.
        Edit(desk, made, "{\"marks\":\"desk again\"}");
        Assert.Equal(0, Sync(desk, 2).Tidied!.Moved);
        Assert.Equal(3, Changes(desk).Count);

        // Settled on the desk, still waiting on the phone: still nothing goes, and the phone's side by side still has both.
        SyncEngine.Resolve(desk.Store, desk.Local, desk.Store.SyncConflicts().Single().Id, SyncChoice.KeepThisOne, Start.AddDays(3));
        Assert.Equal(0, Sync(desk, 3).Tidied!.Moved);
        Assert.Equal(4, Changes(desk).Count);
        Assert.NotNull(SyncEngine.Versions(phone.Store, phone.Store.SyncConflicts().Single()));

        // The phone takes the desk's settled version, which settles its own choice; then the desk tidies all but its newest.
        Sync(phone, 4);
        Assert.Empty(phone.Store.SyncConflicts());
        Assert.Equal("{\"marks\":\"desk again\"}", Marking(phone, made));
        Assert.Equal(3, Sync(desk, 5).Tidied!.Moved);
        Assert.Single(Changes(desk));
    }

    [Fact]
    public void ARecordNeverChangedKeepsItsFileAndTheTrashIsNeverReadAsChanges()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string once = "2026-10-19T10:00:00Z", often = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(once));
        Sync(desk, 0);
        desk.Store.Save(Session(often));
        Sync(desk, 0);
        Sync(phone, 0);
        var files = Changes(desk);
        Assert.Equal(2, files.Count);

        // The phone has read both; only the second has a newer version after it, so only the second goes.
        Edit(desk, often, "{\"marks\":2}");
        Assert.Equal(1, Sync(desk, 1).Tidied!.Moved);
        Assert.Equal(0, Sync(desk, 2).Tidied!.Moved);
        var left = Changes(desk);
        Assert.Equal(2, left.Count);
        Assert.Equal(files[0], left[0]);

        // A whole change file put in a trash folder, as a tidied one is, is never read by another device.
        string stray = Path.Combine(Cloud, SyncEngine.Root, "trash", "ffffffffffff");
        Directory.CreateDirectory(stray);
        File.Copy(Path.Combine(Cloud, SyncEngine.Root, "trash", desk.Id, Trash(desk).Single()), Path.Combine(stray, "20261021T120000000Z~stray.json"));
        var late = NewDevice("tablet");
        var joined = Sync(late, 3);
        Assert.Equal(2, joined.FilesRead);
        Assert.Equal(2, late.Store.List().Count);
        Assert.Equal("{\"marks\":2}", Marking(late, often));
        Assert.Equal("{\"marks\":0}", Marking(late, once));
    }

    /// <summary>A folder that goes away (moved, or its drive unplugged) just before the <c>n</c>th step of tidying: a copy into the trash or a removal.</summary>
    private sealed class Vanishing(DirectorySyncFolder inner, string path, string gone, int n) : ISyncFolder
    {
        private int steps;

        public bool Available => inner.Available;

        public IReadOnlyList<string> Files(string folder) => inner.Files(folder);

        public IReadOnlyList<string> Folders(string folder) => inner.Folders(folder);

        public byte[]? Read(string p) => inner.Read(p);

        public bool Exists(string p) => inner.Exists(p);

        public void Write(string p, ReadOnlySpan<byte> bytes)
        {
            if (p.Contains("/trash/", StringComparison.Ordinal))
            {
                Step();
            }

            inner.Write(p, bytes);
        }

        public void Delete(string p)
        {
            Step();
            inner.Delete(p);
        }

        private void Step()
        {
            if (++steps == n)
            {
                Directory.Move(path, gone);
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(8)]
    public void AFolderThatGoesMissingDuringTidyingLosesNoFile(int step)
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        const string made = "2026-10-20T10:00:00Z";
        desk.Store.Save(Session(made));
        Sync(desk, 0);
        for (int edit = 1; edit <= 4; edit++)
        {
            Edit(desk, made, $"{{\"marks\":{edit}}}");
            Sync(desk, 0);
        }

        Sync(phone, 0);
        var before = Hashes(Folder("changes", desk));
        Assert.Equal(5, before.Count);

        // Four files may go, each a copy into the trash and then a removal: eight steps, and the folder goes away before one of them.
        string gone = Path.Combine(root, "gone");
        var report = Sync(desk, 1, new Vanishing(cloud, Cloud, gone, step));
        Assert.True(report.Reached);
        Assert.Contains(report.Said, s => s.Contains("stopped answering part way", StringComparison.Ordinal));
        Assert.Equal((step - 1) / 2, report.Tidied!.Moved);
        Assert.False(Directory.Exists(Cloud), "nothing made the folder again somewhere else");
        string Moved(string part) => Path.Combine(gone, SyncEngine.Root, part, desk.Id);
        var after = Hashes(Moved("changes"));
        after.UnionWith(Hashes(Moved("trash")));
        Assert.True(before.IsSubsetOf(after), "every change file is either where it was or whole in the trash");
        Assert.Equal(5 - ((step - 1) / 2), Hashes(Moved("changes")).Count);

        // Back again, the next run finishes the tidying, and still nothing is lost.
        Directory.Move(gone, Cloud);
        var again = Sync(desk, 1);
        Assert.Equal(4 - ((step - 1) / 2), again.Tidied!.Moved);
        Assert.Single(Changes(desk));
        var end = Hashes(Folder("changes", desk));
        end.UnionWith(Hashes(Folder("trash", desk)));
        Assert.True(before.IsSubsetOf(end));
        Assert.Equal("{\"marks\":4}", Marking(phone, made));
    }

    [Fact]
    public void TheReadPositionListsWhatThisDeviceHasReadAndIsWrittenOnlyWhenItChanges()
    {
        var desk = NewDevice("desk");
        var phone = NewDevice("phone");
        desk.Store.Save(Session("2026-10-20T10:00:00Z"));
        Sync(desk, 0);
        Sync(phone, 0);
        string path = Path.Combine(Cloud, SyncEngine.Root, "read", phone.Id + ".json");
        var position = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("grouplab-sync-read", (string?)position["format"]);
        Assert.True((bool)position["end"]!);
        Assert.Equal(Changes(desk), position["read"]![desk.Id]!.AsArray().Select(n => (string)n!).ToList());
        Assert.Empty(position["waiting"]!.AsArray());

        var written = File.GetLastWriteTimeUtc(path);
        File.SetLastWriteTimeUtc(path, written.AddHours(-1));
        Sync(phone, 0);
        Assert.Equal(written.AddHours(-1), File.GetLastWriteTimeUtc(path));
    }
}
