using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;
using GroupLab.Core.Sync;
using GroupLab.Core.Tests.Publication;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 472 section 2: every importer against bad files. A session file (.grouplab), a marking, a CSV of shots, a
/// GroupLab data file, a photograph or scan and its orientation, and a sync folder's change file are each given nothing, every truncation a
/// cloud client can leave half written, damaged bytes, another kind of file under the right name, odd encodings and line endings, and very
/// large input. Each ends in what it read or in the one refusal its callers catch, in words a person can read, quickly. A data file that
/// reads is then imported whole or not at all. Seeded, so a failure is the same on every run. The chronograph readers had the same in entry
/// 352 (<see cref="UntrustedFilesTests"/>); their line endings are added here.
/// </summary>
public sealed class BadFilesTests : IDisposable
{
    private readonly string root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"grouplab-bad-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => GroupLab.Tests.Support.Temp.Delete(root);

    private static readonly TimeSpan Quick = TimeSpan.FromSeconds(30);

    /// <summary>The bad copies of a good file: empty, cut short at many lengths, damaged here and there, and another kind of file.</summary>
    internal static IEnumerable<(string What, byte[] Bytes)> Bad(byte[] good, int seed)
    {
        yield return ("empty", []);
        yield return ("one byte", good[..1]);
        int step = Math.Max(1, good.Length / 60);
        for (int n = 1; n < good.Length; n += step)
        {
            yield return ($"cut at {n} of {good.Length}", good[..n]);
        }

        yield return ($"cut one short of {good.Length}", good[..^1]);
        var random = new Random(seed);
        for (int i = 0; i < 120; i++)
        {
            byte[] copy = (byte[])good.Clone();
            int at = random.Next(copy.Length);
            copy[at] = (byte)random.Next(256);
            yield return ($"byte {at} changed", copy);
        }

        for (int i = 0; i < 30; i++)
        {
            int from = random.Next(good.Length), length = Math.Min(good.Length - from, 1 + random.Next(64));
            yield return ($"{length} bytes from {from} missing", [.. good[..from], .. good[(from + length)..]]);
        }

        // A cloud client's placeholder: the right size, nothing in it yet.
        yield return ("all zeros", new byte[good.Length]);
        yield return ("a picture", PhoneImages.Jpeg());
        yield return ("words", Encoding.UTF8.GetBytes("This is a note, not the file it is named as."));
        yield return ("a zip of something else", Zip(("readme.txt", "hello")));
        yield return ("the file twice", [.. good, .. good]);
    }

    private static byte[] Zip(params (string Name, string Text)[] entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, text) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(text);
            }
        }

        return stream.ToArray();
    }

    /// <summary>The text of a good file in every encoding and line ending a person's tools write it in.</summary>
    internal static IEnumerable<(string What, byte[] Bytes)> Texts(string good)
    {
        string lf = good.Replace("\r\n", "\n", StringComparison.Ordinal);
        foreach (var (ending, text) in new[] { ("LF", lf), ("CRLF", lf.Replace("\n", "\r\n", StringComparison.Ordinal)), ("CR", lf.Replace("\n", "\r", StringComparison.Ordinal)) })
        {
            yield return ($"UTF-8, {ending}", Encoding.UTF8.GetBytes(text));
            yield return ($"UTF-8 with a mark, {ending}", [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)]);
            yield return ($"UTF-16, {ending}", [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)]);
            yield return ($"UTF-16 big-endian, {ending}", [.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes(text)]);
            yield return ($"Latin-1, {ending}", Encoding.Latin1.GetBytes(text));
        }
    }

    /// <summary>
    /// Reads every one of <paramref name="files"/>, failing on anything but what was read or a <typeparamref name="TRefusal"/> in words: no
    /// type names, no stack, nothing empty. Returns how many were refused.
    /// </summary>
    private static int ReadOrRefused<TRefusal>(string kind, IEnumerable<(string What, byte[] Bytes)> files, Action<byte[]> read)
        where TRefusal : Exception
    {
        var wrong = new List<string>();
        int refused = 0;
        foreach (var (what, bytes) in files)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                read(bytes);
            }
            catch (TRefusal e)
            {
                refused++;
                if (string.IsNullOrWhiteSpace(e.Message) || e.Message.Contains("Exception", StringComparison.Ordinal) || e.Message.Contains(" at ", StringComparison.Ordinal) && e.Message.Contains(".cs:", StringComparison.Ordinal))
                {
                    wrong.Add($"{what}: refused in words nobody can read: {e.Message}");
                }
            }
            catch (Exception e)
            {
                wrong.Add($"{what}: {e.GetType().Name}: {e.Message}");
            }

            if (clock.Elapsed > Quick)
            {
                wrong.Add($"{what}: took {clock.Elapsed.TotalSeconds:0} s");
            }
        }

        Assert.True(wrong.Count == 0, $"{kind}:{Environment.NewLine}" + string.Join(Environment.NewLine, wrong.Distinct().Take(40)));
        return refused;
    }

    private static MarkingState Marked()
    {
        var session = new MarkingSession();
        session.Open("target.jpg", 6);
        session.SetCalibre(Calibre.Of(0.264));
        session.SetShotDistance(3600);
        session.AddShot(new PointD(100, 120));
        session.AddShot(new PointD(140, 118));
        session.AddShot(new PointD(131, 140));
        return session.State;
    }

    private static byte[] Package()
    {
        using var stream = new MemoryStream();
        SessionPackage.Write(stream, Marked(), BuiltIns.Load("GL-CF25-LTR.gltd.json"), UnitSettings.Imperial, PhoneImages.Jpeg(), ".jpg", "Pixel 9", 2, new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc));
        return stream.ToArray();
    }

    [Fact]
    public void ASessionFileIsOpenedOrRefusedInWords()
    {
        byte[] good = Package();
        SessionPackage.Read(new MemoryStream(good));
        var zip = new List<(string, byte[])>(Bad(good, 4721))
        {
            ("no marking", Zip(("session.json", "{\"schema\":\"grouplab-session-1\"}"))),
            ("a schema that is a number", Zip(("session.json", "{\"schema\":1}"))),
            ("a device that is a list", Zip(("session.json", "{\"schema\":\"grouplab-session-1\",\"device\":[1]}"), ("marking.json", MarkingFile.Write(Marked())), ("image.jpg", "x"))),
            ("a sheet that is not one", Zip(("session.json", "{\"schema\":\"grouplab-session-1\"}"), ("marking.json", MarkingFile.Write(Marked())), ("sheet.gltd.json", "{\"bulls\":7}"), ("image.jpg", "x"))),
            ("a marking that is a list", Zip(("session.json", "{\"schema\":\"grouplab-session-1\"}"), ("marking.json", "[1,2]"), ("image.jpg", "x"))),
        };
        int refused = ReadOrRefused<SessionPackageException>("a session file", zip, bytes => SessionPackage.Read(new MemoryStream(bytes)));
        Assert.True(refused > 60, $"only {refused} refused");
    }

    [Fact]
    public void AMarkingIsReadOrRefusedInWords()
    {
        string good = MarkingFile.Write(Marked(), UnitSettings.Imperial);
        MarkingFile.Read(good);
        var files = Bad(Encoding.UTF8.GetBytes(good), 4722).Concat(Texts(good)).Concat(
        [
            ("shots that are a number", Encoding.UTF8.GetBytes(good.Replace("\"shots\":[", "\"shots\":7,\"x\":[", StringComparison.Ordinal))),
            ("deeply nested", Encoding.UTF8.GetBytes(new string('[', 5000) + new string(']', 5000))),
            ("very large", Encoding.UTF8.GetBytes("{\"note\":\"" + new string('a', 20_000_000) + "\"}")),
        ]);
        ReadOrRefused<MarkingFileException>("a marking", files, bytes => MarkingFile.Read(Text(bytes)));
    }

    /// <summary>A file's text as GroupLab's own readers take it: by its byte order mark, else UTF-8.</summary>
    private static string Text(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    [Fact]
    public void ACsvOfShotsIsReadOrRefusedInWords()
    {
        string good = "Sheet,X (in),Y (in)\nA,0.10,-0.20\nA,-0.05,0.31\nB,0.22,0.07\n";
        var table = ShotCsv.Read(good);
        Assert.Equal(3, table.Rows.Count);
        var files = Bad(Encoding.UTF8.GetBytes(good), 4723).Concat(Texts(good)).Concat(
        [
            ("quotes never closed", Encoding.UTF8.GetBytes("X,Y\n\"1,2\n3,4\n")),
            ("one column", Encoding.UTF8.GetBytes("X\n1\n2\n")),
            ("numbers with commas for points", Encoding.UTF8.GetBytes("X;Y\n0,1;0,2\n")),
            ("very large", Encoding.UTF8.GetBytes("X,Y\n" + string.Concat(Enumerable.Repeat("0.1,0.2\n", 2_000_000)))),
            ("one very long line", Encoding.UTF8.GetBytes("X,Y\n" + new string('9', 10_000_000) + ",1\n")),
        ]);
        // What a page does with the table after it reads: guess the columns and take the shots, which must refuse in the same way.
        ReadOrRefused<FormatException>("a CSV of shots", files, bytes =>
        {
            var read = ShotCsv.Read(Text(bytes));
            if (ShotCsv.Guess(read, across: true) is { } x && ShotCsv.Guess(read, across: false) is { } y)
            {
                ShotCsv.Shots(read, x, y, CoordinateUnit.Inch, yUpIsPositive: true, 3600);
            }
        });
    }

    [Fact]
    public void AChronographExportReadsWhateverItsLineEndings()
    {
        string good = "Shot,Speed (fps)\n1,2710\n2,2715\n3,2702\n";
        foreach (var (what, bytes) in Texts(good).Where(t => !t.What.StartsWith("Latin-1", StringComparison.Ordinal)))
        {
            var read = ChronographFiles.ReadFile(new MemoryStream(bytes), "string.csv", out _);
            Assert.True(read.Count == 1 && read[0].VelocitiesFps.Count == 3, $"{what}: read {read.Count} strings");
        }
    }

    [Fact]
    public void AGroupLabDataFileIsImportedWholeOrNotAtAll()
    {
        string folder = Directory.CreateDirectory(Path.Combine(root, "from")).FullName;
        var store = SessionStore.Open(Path.Combine(folder, "grouplab.db"));
        store.SaveBook(RecordBook.Empty.With(new Rifle("Tikka", 0.1, AngularUnit.Mrad)));
        for (int i = 0; i < 3; i++)
        {
            long id = store.Save(new SessionRecord(0, $"2026-10-2{i}T10:00:00Z", "2026-10-20", $"Sheet {i}", "GL-CF25-LTR", "{}", 3600, "Tikka", null, "H4350", 0.308,
                $"{{\"shots\":{i}}}", 5, 0.4, 0.3, 0.6, null, $"sha{i}", [1, 2, 3, (byte)i], "image/jpeg"));
            store.AddChronographString(id, "LabRadar", null, [2710, 2715, 2705]);
        }

        byte[] good;
        using (var stream = new MemoryStream())
        {
            DataExport.Write(stream, store, [], [], "test", new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc));
            good = stream.ToArray();
        }

        int n = 0;
        ReadOrRefused<InvalidDataException>("a data file", Bad(good, 4724), bytes =>
        {
            var file = DataExport.Read(new MemoryStream(bytes));
            // Read: it is imported into an empty GroupLab, and either every session in it arrives or none does.
            var into = SessionStore.Open(Path.Combine(Directory.CreateDirectory(Path.Combine(root, $"into-{n++}")).FullName, "grouplab.db"));
            var plan = DataExport.Plan(file, into, [], []);
            DataExport.Apply(plan, into);
            Assert.Equal(plan.NewSessions.Count, into.List().Count);
        });
    }

    [Fact]
    public void APictureIsOpenedOrRefusedInWords()
    {
        using var sheet = new Mat(300, 400, MatType.CV_8UC3, Scalar.White);
        Cv2.Circle(sheet, new Point(200, 150), 40, Scalar.Black, -1);
        var files = new List<(string, byte[])>();
        foreach (string ext in new[] { ".jpg", ".png", ".tif", ".bmp" })
        {
            Cv2.ImEncode(ext, sheet, out byte[] good);
            files.AddRange(Bad(good, 4725).Select(b => ($"{ext}, {b.What}", b.Bytes)));
        }

        // A header that claims more than any scan is refused before anything is decoded (question 43).
        Cv2.ImEncode(".png", sheet, out byte[] png);
        byte[] huge = (byte[])png.Clone();
        huge[16] = 0x7f;
        huge[20] = 0x7f;
        files.Add(("a header claiming a huge picture", huge));
        int k = 0;
        ReadOrRefused<InvalidDataException>("a picture", files, bytes =>
        {
            string path = Path.Combine(root, $"picture-{k++}.jpg");
            File.WriteAllBytes(path, bytes);
            ImageLoader.Load(path, 12);
        });
    }

    /// <summary>Every Orientation a file can carry, missing, mirrored, out of range or said twice, gives a quarter turn and never a fault.</summary>
    [Fact]
    public void APhotographsOrientationIsReadWhateverItSays()
    {
        byte[] good = PhoneImages.Jpeg();
        Assert.Equal(1, ViewRotation.FromExifOrientation(ImageMetadataReader.Read(good).Orientation));
        int at = Find(good, [0x12, 0x01, 0x03, 0x00]);
        Assert.True(at > 0, "the sample has no Orientation tag");
        foreach (int value in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 255 })
        {
            byte[] copy = (byte[])good.Clone();
            copy[at + 8] = (byte)value;
            int turns = ViewRotation.FromExifOrientation(ImageMetadataReader.Read(copy).Orientation);
            Assert.InRange(turns, 0, 3);
        }

        byte[] missing = (byte[])good.Clone();
        missing[at] = 0x99;
        missing[at + 1] = 0x99;
        Assert.Equal(0, ViewRotation.FromExifOrientation(ImageMetadataReader.Read(missing).Orientation));

        // The Orientation said as a long, a rational and a text rather than a short, and every damaged copy: read, never a fault.
        foreach (byte type in new byte[] { 0x04, 0x05, 0x02, 0x07, 0xff })
        {
            byte[] copy = (byte[])good.Clone();
            copy[at + 2] = type;
            Assert.InRange(ViewRotation.FromExifOrientation(ImageMetadataReader.Read(copy).Orientation), 0, 3);
        }

        foreach (var (_, bytes) in Bad(good, 4726))
        {
            Assert.InRange(ViewRotation.FromExifOrientation(ImageMetadataReader.Read(bytes).Orientation), 0, 3);
        }
    }

    private static int Find(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= haystack.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// A change file in the sync folder, as a cloud client leaves it: half there, damaged or something else under its name. Reading it never
    /// faults, and a device given one changes nothing until it has arrived whole, and then takes all of it.
    /// </summary>
    [Fact]
    public void ASyncChangeFileIsTakenWholeOrLeftForLater()
    {
        var cloud = new DirectorySyncFolder(Directory.CreateDirectory(Path.Combine(root, "cloud")).FullName);
        var writer = SessionStore.Open(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "writer")).FullName, "grouplab.db"));
        var writerLocal = new FileSyncLocal(Path.Combine(root, "writer", "sheets"), Path.Combine(root, "writer", "settings.json"));
        writer.Save(new SessionRecord(0, "2026-10-20T10:00:00Z", "2026-10-20", "Sheet", "GL-CF25-LTR", "{}", 3600, "Tikka", null, "H4350", 0.308,
            "{\"shots\":3}", 3, 0.4, 0.3, 0.6, null, new string('a', 64), [1, 2, 3], "image/jpeg"));
        writer.SaveBook(RecordBook.Empty.With(new Rifle("Tikka", 0.1, AngularUnit.Mrad)));
        var clock = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
        SyncEngine.Run(writer, writerLocal, cloud, "writer", clock);
        string changes = SyncEngineFiles(cloud).Single(f => SyncEngine.ReadChangeFile(cloud.Read(f)) is { Count: > 0 });
        byte[] good = cloud.Read(changes)!;
        Assert.Equal(2, SyncEngine.ReadChangeFile(good)!.Count);

        int k = 0;
        var wrong = new List<string>();
        foreach (var (what, bytes) in Bad(good, 4727).Concat(Texts(Encoding.UTF8.GetString(good)).Where(t => !t.What.StartsWith("UTF-8, LF", StringComparison.Ordinal))))
        {
            // A device on its own folder, holding only this change file, as its cloud client delivered it.
            string here = Directory.CreateDirectory(Path.Combine(root, $"reader-{k++}")).FullName;
            var folder = new DirectorySyncFolder(Directory.CreateDirectory(Path.Combine(here, "cloud")).FullName);
            foreach (string f in SyncEngineFiles(cloud))
            {
                folder.Write(f, f == changes ? bytes : cloud.Read(f)!);
            }

            var store = SessionStore.Open(Path.Combine(here, "grouplab.db"));
            var local = new FileSyncLocal(Path.Combine(here, "sheets"), Path.Combine(here, "settings.json"));
            try
            {
                SyncEngine.Run(store, local, folder, "reader", clock.AddMinutes(1));
                int sessions = store.List().Count, rifles = store.LoadBook().Rifles.Count;
                bool whole = SyncEngine.ReadChangeFile(bytes) is { Count: 2 };
                if (sessions + rifles is not (0 or 2) || (sessions + rifles == 0 && whole && bytes.SequenceEqual(good)))
                {
                    wrong.Add($"{what}: {sessions} sessions and {rifles} rifles taken from a change file of two");
                }

                // The file arrives whole later: everything in it is taken.
                folder.Write(changes, good);
                SyncEngine.Run(store, local, folder, "reader", clock.AddMinutes(2));
                if (store.List().Count != 1 || store.LoadBook().Rifles.Count != 1)
                {
                    wrong.Add($"{what}: once whole, {store.List().Count} sessions and {store.LoadBook().Rifles.Count} rifles");
                }
            }
            catch (Exception e)
            {
                wrong.Add($"{what}: {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong.Distinct().Take(40)));
    }

    /// <summary>Every file in the folder, at any depth, by its path.</summary>
    internal static IEnumerable<string> SyncEngineFiles(ISyncFolder folder, string at = "")
    {
        foreach (string f in folder.Files(at))
        {
            yield return at.Length == 0 ? f : at + "/" + f;
        }

        foreach (string d in folder.Folders(at))
        {
            foreach (string f in SyncEngineFiles(folder, at.Length == 0 ? d : at + "/" + d))
            {
                yield return f;
            }
        }
    }
}
