using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 472 section 3: sync as close to proven as it can be without two real devices. Each simulated device sees its
/// own copy of the folder, as a cloud client gives it, and <see cref="Cloud"/> moves files between the copies as OneDrive, Google Drive
/// and Dropbox do: minutes late, in any order, half a file before the whole of it, two writes of one name kept as a conflicted copy named
/// the way that company names them, and the folder gone for a while, even part way through a run. Devices run with clocks hours apart and
/// one forgets another while the other is still writing. At the end, once everything has arrived and the questions are answered, every
/// device holds the same records, and nothing saved anywhere is lost.
/// </summary>
public sealed class CloudClientTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-cloud-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    /// <summary>How a cloud company names the second of two files written to one name at once.</summary>
    public enum Style
    {
        /// <summary>"000012-PHONE.json": the device's name after the file's.</summary>
        OneDrive,

        /// <summary>"000012 (1).json".</summary>
        GoogleDrive,

        /// <summary>"000012 (Alan's conflicted copy 2026-10-24).json".</summary>
        Dropbox,
    }

    /// <summary>The cloud company's copy of the folder, and each device's own copy, which it brings up to date minutes later.</summary>
    internal sealed class Cloud(Random random, Style style, int mostDelayMinutes)
    {
        private readonly Dictionary<string, (byte[] Bytes, int Number)> server = new(StringComparer.Ordinal);
        private readonly List<(int Due, Replica From, string Path, byte[]? Bytes, int Base)> uploads = [];
        private readonly List<(int Due, Replica To, string Path, byte[]? Bytes, int Number, bool Partial)> downloads = [];
        private int number;

        internal List<Replica> Replicas { get; } = [];

        internal int Minute { get; private set; }

        internal int Conflicts { get; private set; }

        internal int Partials { get; private set; }

        internal Replica Join(string name)
        {
            var replica = new Replica(this, name);
            foreach (var (path, (bytes, n)) in server)
            {
                replica.Arrive(path, bytes, n);
            }


            Replicas.Add(replica);
            return replica;
        }

        internal void Upload(Replica from, string path, byte[]? bytes, int seen) => uploads.Add((Minute + random.Next(0, mostDelayMinutes + 1), from, path, bytes, seen));

        /// <summary>A minute passes: what is due goes up and comes down, in no particular order.</summary>
        internal void Step()
        {
            Minute++;
            foreach (var up in Due(uploads, u => u.Due).OrderBy(_ => random.Next()))
            {
                Receive(up.From, up.Path, up.Bytes, up.Base);
            }

            foreach (var down in Due(downloads, d => d.Due).OrderBy(_ => random.Next()))
            {
                if (down.Bytes is null)
                {
                    down.To.Gone(down.Path, down.Number);
                }
                else
                {
                    down.To.Arrive(down.Path, down.Partial ? down.Bytes[..Math.Max(1, down.Bytes.Length * random.Next(1, 10) / 10)] : down.Bytes, down.Number, whole: !down.Partial);
                }
            }
        }

        private List<T> Due<T>(List<T> queue, Func<T, int> due)
        {
            var now = queue.Where(q => due(q) <= Minute).ToList();
            queue.RemoveAll(q => due(q) <= Minute);
            return now;
        }

        /// <summary>Until nothing is moving.</summary>
        internal void Settle()
        {
            for (int i = 0; i < 10_000 && (uploads.Count > 0 || downloads.Count > 0); i++)
            {
                Step();
            }

            Assert.True(uploads.Count == 0 && downloads.Count == 0, "the cloud never settled");
        }

        private void Receive(Replica from, string path, byte[]? bytes, int seen)
        {
            if (bytes is null)
            {
                if (server.Remove(path))
                {
                    Send(from, path, null, ++number);
                }

                return;
            }

            // Written where the copy the device last had is no longer the newest: the company keeps both, the second under its own name, and
            // the writer's copy of the name is the newest again.
            if (server.TryGetValue(path, out var there) && there.Number != seen && !there.Bytes.AsSpan().SequenceEqual(bytes))
            {
                Conflicts++;
                string copy = ConflictName(path, from.Name);
                server[copy] = (bytes, ++number);
                Send(null, copy, bytes, number);
                downloads.Add((Minute + random.Next(0, mostDelayMinutes + 1), from, path, there.Bytes, there.Number, false));
                return;
            }

            server[path] = (bytes, ++number);
            Send(from, path, bytes, number);

            // Now and then a client sends one file up twice and the company keeps the second as a conflicted copy of the same bytes.
            if (random.Next(15) == 0)
            {
                Conflicts++;
                string twice = ConflictName(path, from.Name);
                server[twice] = (bytes, ++number);
                Send(null, twice, bytes, number);
            }
        }

        private void Send(Replica? from, string path, byte[]? bytes, int n)
        {
            foreach (var to in Replicas.Where(r => r != from))
            {
                int due = Minute + random.Next(0, mostDelayMinutes + 1);
                if (bytes is not null && random.Next(4) == 0)
                {
                    Partials++;
                    downloads.Add((due, to, path, bytes, n, true));
                    due += 1 + random.Next(0, mostDelayMinutes + 1);
                }

                downloads.Add((due, to, path, bytes, n, false));
            }

            // The writer's own copy learns the number its file now has.
            from?.Numbered(path, n);
        }

        private string ConflictName(string path, string device)
        {
            int dot = path.LastIndexOf('.');
            string stem = path[..dot], ext = path[dot..];
            return style switch
            {
                Style.OneDrive => $"{stem}-{device.ToUpperInvariant()}{ext}",
                Style.GoogleDrive => $"{stem} (1){ext}",
                _ => $"{stem} (Alan's conflicted copy 2026-10-24){ext}",
            };
        }
    }

    /// <summary>One device's copy of the folder: what its cloud client has brought down so far, written at once and sent up later.</summary>
    internal sealed class Replica(Cloud cloud, string name) : ISyncFolder
    {
        private readonly Dictionary<string, (byte[] Bytes, int Number, bool Whole)> files = new(StringComparer.Ordinal);

        internal string Name { get; } = name;

        /// <summary>False while the folder is unplugged, signed out of or moved.</summary>
        internal bool Here { get; set; } = true;

        /// <summary>Where set, the folder goes after this many more reads and writes, part way through a run.</summary>
        internal int? GoesAfter { get; set; }

        public bool Available => Here;

        private void Touch()
        {
            if (!Here)
            {
                throw new DirectoryNotFoundException("The sync folder is not there now.");
            }

            if (GoesAfter is { } left)
            {
                GoesAfter = left - 1;
                if (left <= 0)
                {
                    Here = false;
                    GoesAfter = null;
                    throw new DirectoryNotFoundException("The sync folder is not there now.");
                }
            }
        }

        internal void Numbered(string path, int number)
        {
            if (files.TryGetValue(path, out var mine))
            {
                files[path] = (mine.Bytes, number, mine.Whole);
            }
        }

        internal void Arrive(string path, byte[] bytes, int number, bool whole = true)
        {
            // A cloud client never takes an older copy over a newer one, nor part of a copy over the whole of it.
            if (files.TryGetValue(path, out var had) && (had.Number > number || (had.Number == number && had.Whole && !whole)))
            {
                return;
            }

            files[path] = (bytes, number, whole);
        }

        internal void Gone(string path, int number) => files.Remove(path);

        private static string Parent(string path) => path.Contains('/', StringComparison.Ordinal) ? path[..path.LastIndexOf('/')] : "";

        public IReadOnlyList<string> Files(string folder)
        {
            Touch();
            return [.. files.Keys.Where(p => Parent(p) == folder).Select(p => p[(p.LastIndexOf('/') + 1)..]).Order(StringComparer.Ordinal)];
        }

        public IReadOnlyList<string> Folders(string folder)
        {
            Touch();
            string prefix = folder.Length == 0 ? "" : folder + "/";
            return [.. files.Keys.Where(p => p.StartsWith(prefix, StringComparison.Ordinal) && p.IndexOf('/', prefix.Length) > 0)
                .Select(p => p[prefix.Length..p.IndexOf('/', prefix.Length)]).Distinct().Order(StringComparer.Ordinal)];
        }

        public byte[]? Read(string path)
        {
            Touch();
            return files.TryGetValue(path, out var f) ? f.Bytes : null;
        }

        public void Write(string path, ReadOnlySpan<byte> bytes)
        {
            Touch();
            int seen = files.TryGetValue(path, out var had) ? had.Number : 0;
            files[path] = (bytes.ToArray(), seen, true);
            cloud.Upload(this, path, bytes.ToArray(), seen);
        }

        public bool Exists(string path)
        {
            Touch();
            return files.ContainsKey(path);
        }

        public void Delete(string path)
        {
            Touch();
            if (files.Remove(path, out var had))
            {
                cloud.Upload(this, path, null, had.Number);
            }
        }
    }

    private sealed record Device(string Name, SessionStore Store, FileSyncLocal Local, Replica Folder, TimeSpan ClockOff);

    private Device NewDevice(Cloud cloud, string name, TimeSpan clockOff)
    {
        string folder = Directory.CreateDirectory(Path.Combine(root, $"{name}-{Guid.NewGuid():N}"[..20])).FullName;
        return new Device(name, SessionStore.Open(Path.Combine(folder, "grouplab.db")), new FileSyncLocal(Path.Combine(folder, "sheets"), Path.Combine(folder, "settings.json")),
            cloud.Join(name), clockOff);
    }

    private static DateTime Now(Cloud cloud, Device d) => new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc).AddMinutes(cloud.Minute) + d.ClockOff;

    private static SyncReport Sync(Cloud cloud, Device d) => SyncEngine.Run(d.Store, d.Local, d.Folder, d.Name, Now(cloud, d));

    private static SessionRecord Session(string created, string token) => new(
        0, created, created[..10], "GroupLab 5x5 Load Development, Letter", "GL-TEST-0000", "{\"name\":\"test\"}", 3600, "Tikka", null, "H4350", 0.264,
        $"{{\"marks\":\"{token}\"}}", 25, 0.3, 0.25, 0.4, null, new string('a', 64), [1, 2, 3], "image/jpeg");

    /// <summary>What a device holds that must be the same everywhere: its sessions by when they were made and their marks, rifles and settings.</summary>
    private static string Holds(Device d) =>
        string.Join("; ", d.Store.List().Select(s => d.Store.Get(s.Id)!).Select(s => $"{s.CreatedUtc}={s.MarkingJson}").Order(StringComparer.Ordinal))
        + " | " + string.Join(",", d.Store.LoadBook().Rifles.Select(r => r.Name).Order(StringComparer.Ordinal))
        + " | " + d.Local.Settings()["scope"];

    /// <summary>Every token a device has anywhere: in its sessions, in what merges kept, or in a question still to answer.</summary>
    private static string Everything(Device d) =>
        string.Join("\n", d.Store.List().Select(s => d.Store.Get(s.Id)!.MarkingJson)
            .Concat(d.Store.Kept().Select(k => k.Payload))
            .Concat(d.Store.SyncConflicts().Select(c => c.Payload ?? "")));

    public static TheoryData<Style, int> Runs() => new()
    {
        { Style.OneDrive, 1 },
        { Style.GoogleDrive, 2 },
        { Style.Dropbox, 3 },
        { Style.OneDrive, 4 },
        { Style.GoogleDrive, 5 },
        { Style.Dropbox, 6 },
    };

    /// <summary>
    /// Three devices, two hours of work each made of new targets, changes to them, a deletion now and then, rifles and a setting, with a sync
    /// after each and the cloud as above between them; one device's clock is five hours ahead and another's three behind; one device's
    /// folder goes away twice, once part way through a run; and the third device is forgotten by the first while it is still writing.
    /// </summary>
    [Theory]
    [MemberData(nameof(Runs))]
    public void ThreeDevicesEndTheSameAndNothingIsLost(Style style, int seed)
    {
        var random = new Random(seed);
        var cloud = new Cloud(random, style, mostDelayMinutes: 6);
        var devices = new[]
        {
            NewDevice(cloud, "desk", TimeSpan.Zero),
            NewDevice(cloud, "phone", TimeSpan.FromHours(5)),
            NewDevice(cloud, "tablet", TimeSpan.FromHours(-3)),
        };
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal); // token -> the session it was saved to, by when it was made
        var deleted = new HashSet<string>(StringComparer.Ordinal);
        // A version a person changed again on the device it was on is replaced by them, not by sync, and need not be kept.
        var replaced = new HashSet<string>(StringComparer.Ordinal);
        int made = 0, failures = 0;

        for (int minute = 0; minute < 120; minute++)
        {
            cloud.Step();
            var d = devices[random.Next(devices.Length)];
            int what = random.Next(10);
            var mine = d.Store.List();
            if (what < 4 || mine.Count == 0)
            {
                string created = new DateTime(2026, 10, 20, 8, 0, 0, DateTimeKind.Utc).AddMinutes(made++).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);
                string token = $"t{made}-{d.Name}-new";
                d.Store.Save(Session(created, token));
                tokens[token] = created;
            }
            else if (what < 8)
            {
                var s = d.Store.Get(mine[random.Next(mine.Count)].Id)!;
                string token = $"t{made++}-{d.Name}-edit";
                replaced.Add(s.MarkingJson[(s.MarkingJson.IndexOf(":\"", StringComparison.Ordinal) + 2)..^2]);
                d.Store.Save(s with { MarkingJson = $"{{\"marks\":\"{token}\"}}" });
                tokens[token] = s.CreatedUtc;
            }
            else if (what == 8)
            {
                var s = mine[random.Next(mine.Count)];
                deleted.Add(d.Store.Get(s.Id)!.CreatedUtc);
                d.Store.Delete(s.Id);
            }
            else
            {
                d.Store.SaveBook(d.Store.LoadBook().With(new Rifle($"Rifle {made++} {d.Name}", 0.1, AngularUnit.Mrad)));
                d.Local.SaveSetting("scope", random.Next(2) == 0 ? "Mil" : "MOA");
            }

            // The tablet's folder goes away for twenty minutes at 30, and part way through a run at 70.
            if (minute == 30)
            {
                devices[2].Folder.Here = false;
            }
            else if (minute == 50)
            {
                devices[2].Folder.Here = true;
            }
            else if (minute == 70)
            {
                devices[2].Folder.GoesAfter = 5;
            }
            else if (minute == 75)
            {
                devices[2].Folder.Here = true;
            }

            // The desk forgets the tablet at 90, while the tablet goes on writing.
            if (minute == 90 && devices[0].Folder.Here)
            {
                string tablet = devices[2].Store.SyncDevice();
                SyncForget.Forget(devices[0].Folder, devices[0].Store.SyncDevice(), tablet, Now(cloud, devices[0]));
            }

            var report = Sync(cloud, d);
            failures += report.Reached ? 0 : 1;
        }

        devices[2].Folder.Here = true;
        devices[2].Folder.GoesAfter = null;

        var trace = new List<string>();
        // Everything arrives, every device runs until there is nothing new, and the questions are answered on one device and travel.
        for (int round = 0; round < 12; round++)
        {
            // Twice round, so an answer given on one device has reached every other before the next question is picked: answering the
            // same question on two devices before they have synced makes each a new version the other is asked about, as it would for a person.
            for (int pass = 0; pass < 2; pass++)
            {
                cloud.Settle();
                var line = new List<string> { $"round {round}.{pass}" };
                foreach (var d in devices)
                {
                    var rep = Sync(cloud, d);
                    line.Add($"{d.Name} read {rep.FilesRead} wait {rep.FilesWaiting} applied {rep.Applied} wrote {rep.Written} asks {d.Store.SyncConflicts().Count}");
                }

                trace.Add(string.Join(" | ", line));
            }

            cloud.Settle();
            if (devices.FirstOrDefault(d => d.Store.SyncConflicts().Count > 0) is { } asked)
            {
                foreach (var question in asked.Store.SyncConflicts())
                {
                    SyncEngine.Resolve(asked.Store, asked.Local, question.Id, SyncChoice.KeepThisOne, Now(cloud, asked));
                }
            }
            else if (round > 2 && devices.Select(Holds).Distinct().Count() == 1)
            {
                break;
            }
        }

        var held = devices.Select(d => (d.Name, Holds: $"[{string.Join(" ", Sync(cloud, d).Said)}; " + string.Join(" / ", d.Store.SyncConflicts().Select(c => $"{c.Kind} {c.Key[..Math.Min(20, c.Key.Length)]} {c.Vector} from {c.FromDevice} ledger {d.Store.SyncEntryOf(c.Kind, c.Key)?.Vector} written {d.Store.SyncEntryOf(c.Kind, c.Key)?.Written}")) + "] ")).ToList();
        Assert.True(devices.Select(Holds).Distinct().Count() == 1,
            $"{style}, seed {seed}: the devices differ{Environment.NewLine}" + string.Join(Environment.NewLine, trace) + Environment.NewLine + string.Join(Environment.NewLine, held.Select(h => $"{h.Name}: {h.Holds}")));
        Assert.All(devices, d => Assert.Empty(d.Store.SyncConflicts()));

        // Nothing lost: every version of a target nobody deleted is somewhere a person can reach it, as the target on every device or, where
        // a merge or an answer set it aside, kept on the device that did. Kept versions stay on their device; they do not travel.
        string all = string.Join(Environment.NewLine, devices.Select(Everything));
        var lost = tokens.Where(t => !deleted.Contains(t.Value) && !replaced.Contains(t.Key) && !all.Contains(t.Key, StringComparison.Ordinal)).Select(t => t.Key).ToList();
        Assert.True(lost.Count == 0, $"{style}, seed {seed}: lost {lost.Count}: " + string.Join(", ", lost.Take(30)));
        Assert.True(cloud.Partials > 0, "no file arrived half written, so the run proved nothing about it");
        Assert.True(cloud.Conflicts > 0, "no conflicted copy was made, so the run proved nothing about them");
        Assert.True(failures > 0, "the folder never went away during a run");
    }
}
