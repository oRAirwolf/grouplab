using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Records;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Updates;
using GroupLab.Tests.Support;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 352 item 3: every file a person gives GroupLab is untrusted. The chronograph readers (a generic CSV,
/// LabRadar, Garmin Xero as CSV, xls and xlsx in both units, BulletSeeker), the fingerprint reference (.glref) and the signed library are
/// given truncated, huge, deeply nested, wrongly encoded and hostile input. Each must end in what it read or a refusal of the one kind its
/// callers catch, with words a person can read, quickly and without holding more memory than its limit; a library that fails its check is
/// never loaded. Seeded, so a failure is the same on every run.
/// </summary>
public class UntrustedFilesTests
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    /// <summary>Generous for one file, so a slow machine never fails it, and far less than a hang.</summary>
    private static readonly TimeSpan Quick = TimeSpan.FromSeconds(5);

    /// <summary>What a chronograph file gave: its strings, or the words of its refusal. Anything but a <see cref="FormatException"/> fails.</summary>
    private static string Chronograph(byte[] bytes, string name)
    {
        var clock = Stopwatch.StartNew();
        try
        {
            var strings = ChronographFiles.ReadFile(new MemoryStream(bytes, writable: false), name, out _);
            return $"{strings.Count} strings";
        }
        catch (FormatException e)
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Message));
            Assert.DoesNotContain("Exception", e.Message, StringComparison.Ordinal);
            return e.Message;
        }
        finally
        {
            Assert.True(clock.Elapsed < Quick, $"{name} took {clock.Elapsed}");
        }
    }

    private static byte[] Bytes(MemoryStream stream) => stream.ToArray();

    private static byte[] XeroWorkbook() => Bytes(ChronographWorkbookTests.Workbook(("s", ChronographWorkbookTests.XeroString("a string"), false)));

    private static string XeroCsv(string speed = "Speed (FPS)") =>
        "Range day\n" + string.Join(",", ChronographWorkbookTests.XeroHeader).Replace("Speed (FPS)", speed, StringComparison.Ordinal)
        + "\n1,\"2,701.4\",-1.1,2833,472.7,14:03:11,,,\n2,2695.9,-6.6,2822,471.8,14:04:02,,,\n-,,,\nAVERAGE SPEED,2698.7\n";

    private const string LabRadarCsv = "sep=;\nDevice ID;LBR-0001\nUnits velocity;fps\nShot ID;V0;V10\n1;2801,5;2790\n2;2795.0;2786\n";

    private const string GenericCsv = "shot,velocity (fps),note\n1,2801,\n2,2795,cold\n3,2810,\n";

    /// <summary>Every way in: the readers by name, the same bytes given each extension, so a file that lies about its kind is covered too.</summary>
    private static readonly string[] Names = ["f.csv", "f.txt", "f.xls", "f.xlsx", "f.xlsm"];

    [Fact]
    public void TruncatedFilesAreReadOrRefusedInWordsAtEveryLength()
    {
        foreach (byte[] whole in new[] { XeroWorkbook(), Encoding.UTF8.GetBytes(XeroCsv()), Encoding.UTF8.GetBytes(LabRadarCsv), Encoding.UTF8.GetBytes(GenericCsv) })
        {
            for (int cut = 0; cut < whole.Length; cut += Math.Max(1, whole.Length / 24))
            {
                foreach (string name in Names)
                {
                    Chronograph(whole[..cut], name);
                }
            }
        }
    }

    [Fact]
    public void DamagedFilesAreReadOrRefusedInWords()
    {
        var random = new Random(352);
        foreach (byte[] whole in new[] { XeroWorkbook(), Encoding.UTF8.GetBytes(XeroCsv()), Encoding.UTF8.GetBytes(LabRadarCsv) })
        {
            for (int trial = 0; trial < 40; trial++)
            {
                byte[] damaged = (byte[])whole.Clone();
                for (int k = 0; k < 1 + random.Next(8); k++)
                {
                    damaged[random.Next(damaged.Length)] = (byte)random.Next(256);
                }

                Chronograph(damaged, Names[trial % Names.Length]);
            }
        }

        // Noise, and a picture with a chronograph file's name.
        byte[] noise = new byte[64 * 1024];
        random.NextBytes(noise);
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52, .. noise[..1000]];
        foreach (string name in Names)
        {
            Chronograph(noise, name);
            Chronograph(png, name);
        }

        Assert.StartsWith("It is not a text file", Chronograph(png, "velocities.csv"), StringComparison.Ordinal);
        Assert.Equal(ChronographFiles.NotAWorkbookWords, Chronograph(Encoding.UTF8.GetBytes(GenericCsv), "renamed.xlsx"));
    }

    /// <summary>A file larger than the limit is refused after the limit is read, however much more there is.</summary>
    [Fact]
    public async Task AHugeFileIsRefusedWithoutReadingAllOfIt()
    {
        var endless = new EndlessStream((byte)'7');
        var refused = Assert.Throws<FormatException>(() => ChronographFiles.ReadFile(endless, "huge.csv", out _));
        Assert.Equal(ChronographFiles.TooLargeWords, refused.Message);
        Assert.InRange(endless.Given, ChronographFiles.MostBytes, ChronographFiles.MostBytes + (1 << 20));
        Assert.Throws<FormatException>(() => ChronographFiles.ReadFile(new EndlessStream((byte)'P'), "huge.xlsx", out _));
        Assert.Throws<FormatException>(() => ChronographFiles.Read(new string('1', ChronographFiles.MostBytes + 1)));
        Assert.Equal(ChronographFiles.TooLargeWords, (await Assert.ThrowsAsync<FormatException>(() => ChronographFiles.ReadBoundedAsync(new EndlessStream(0)))).Message);
    }

    /// <summary>A workbook whose sheet unpacks to 70 MB of nothing, from well under a megabyte, is refused before the workbook reader holds it.</summary>
    [Fact]
    public void AZipBombInsideAWorkbookIsRefused()
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var part = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.SmallestSize).Open();
            part.Write("<?xml version=\"1.0\"?><worksheet><sheetData>"u8);
            byte[] spaces = new byte[1 << 20];
            Array.Fill(spaces, (byte)' ');
            for (int i = 0; i < 70; i++)
            {
                part.Write(spaces);
            }

            part.Write("</sheetData></worksheet>"u8);
        }

        byte[] bomb = stream.ToArray();
        Assert.True(bomb.Length < 1 << 20, $"the bomb is {bomb.Length} bytes");
        Assert.StartsWith("It unpacks to far more than any chronograph export", Chronograph(bomb, "bomb.xlsx"), StringComparison.Ordinal);
    }

    /// <summary>Nesting, a sheet of many thousands of nested elements and a CSV of quotes inside quotes, ends as a refusal or a reading.</summary>
    [Fact]
    public void DeeplyNestedInputEnds()
    {
        string deep = string.Concat(Enumerable.Repeat("<x>", 50_000)) + string.Concat(Enumerable.Repeat("</x>", 50_000));
        var nested = Bytes(ChronographWorkbookTests.Workbook(("s", [["1"]], false)));
        using (var zipStream = new MemoryStream())
        {
            zipStream.Write(nested);
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                zip.GetEntry("xl/worksheets/sheet1.xml")!.Delete();
                using var w = new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open());
                w.Write("<?xml version=\"1.0\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\"><v>1</v>" + deep + "</c></row></sheetData></worksheet>");
            }

            Chronograph(zipStream.ToArray(), "nested.xlsx");
        }

        Chronograph(Encoding.UTF8.GetBytes("velocity\n" + new string('"', 200_000) + "\n2800\n"), "quotes.csv");
        Chronograph(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("Shot ID;V0\n", 20_000)) + "Units velocity;fps\n"), "labradar.csv");
        Chronograph(Encoding.UTF8.GetBytes(new string(',', 2_000_000) + "\n" + new string(',', 2_000_000) + "\n"), "commas.csv");
    }

    /// <summary>The same Xero export in UTF-16 with and without its byte order mark, and in Latin-1, reads; the speeds are what they were.</summary>
    [Fact]
    public void WrongEncodingsReadOrAreRefusedInWords()
    {
        string csv = XeroCsv();
        foreach (var bytes in new[]
        {
            Encoding.Unicode.GetBytes(csv),
            [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(csv)],
            [.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes(csv)],
            Encoding.BigEndianUnicode.GetBytes(csv),
        })
        {
            var read = Assert.Single(ChronographFiles.ReadFile(new MemoryStream(bytes), "xero.csv", out _));
            Assert.Equal([2701.4, 2695.9], read.VelocitiesFps);
        }

        // Latin-1: "Δ" cannot be written, so the header is the metric export's; the bytes are not UTF-8 and are read as Latin-1.
        byte[] latin = Encoding.Latin1.GetBytes(XeroCsv("Speed (MPS)").Replace("Δ", "D", StringComparison.Ordinal) + "Session Note,café °F\n");
        Assert.Equal(2, Assert.Single(ChronographFiles.ReadFile(new MemoryStream(latin), "xero.csv", out _)).VelocitiesFps.Count);

        // An odd byte count in UTF-16 and a lone byte order mark end too.
        Chronograph([.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(csv), 0x41], "odd.csv");
        Chronograph([0xFF, 0xFE], "bom.csv");
        Chronograph([0xEF, 0xBB, 0xBF], "bom.csv");
    }

    /// <summary>
    /// A formula is never worked out: in a CSV it is not a number, and in a workbook only the value the spreadsheet saved with it is read, where
    /// it saved one. Its text, kept as a name or a note, is only text.
    /// </summary>
    [Fact]
    public void FormulaCellsAreNeverWorkedOut()
    {
        string csv = "velocity\n=1+2799\n=HYPERLINK(\"https://example.invalid\",\"2800\")\n@SUM(A1:A2)\n2801\n";
        var read = Assert.Single(ChronographFiles.ReadFile(new MemoryStream(Encoding.UTF8.GetBytes(csv)), "f.csv", out _));
        Assert.Equal([2801.0], read.VelocitiesFps);

        var rows = ChronographWorkbookTests.XeroString("=cmd|' /C calc'!A0");
        rows[2][8] = "=WEBSERVICE(\"https://example.invalid\")";
        var strings = ChronographFiles.ReadFile(ChronographWorkbookTests.Workbook(("s", rows, false)), "f.xlsx", out _);
        Assert.Equal("=cmd|' /C calc'!A0", strings[0].Name);
        Assert.Equal("=WEBSERVICE(\"https://example.invalid\")", strings[0].Shots[0].Note);

        // A formula cell with a saved value gives that value; one without gives no shot.
        var plain = ChronographWorkbookTests.XeroString("formulas");
        (plain[2][1], plain[3][1], plain[4][1], plain[5][1]) = ("2853.4", "2861", "2851.8", "2790.2");
        var bytes = Bytes(ChronographWorkbookTests.Workbook(("s", plain, true)));
        using var stream = new MemoryStream();
        stream.Write(bytes);
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
        {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml")!;
            string xml;
            using (var r = new StreamReader(entry.Open()))
            {
                xml = r.ReadToEnd();
            }

            entry.Delete();
            xml = xml.Replace("<c r=\"B3\"><v>2853.4</v></c>", "<c r=\"B3\"><f>SUM(Z1:Z9)</f><v>2853.4</v></c>", StringComparison.Ordinal)
                .Replace("<c r=\"B4\"><v>2861</v></c>", "<c r=\"B4\"><f>1/0</f></c>", StringComparison.Ordinal);
            using var w = new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open());
            w.Write(xml);
        }

        var formulas = Assert.Single(ChronographFiles.ReadFile(new MemoryStream(stream.ToArray()), "f.xlsx", out _));
        Assert.Equal([1, 4, 5], formulas.Shots.Select(s => s.Number));
        Assert.Equal(2853.4, formulas.VelocitiesFps[0]);
    }

    private static string Reference() =>
        new TargetReference(StoreTargetLibrary.Shipped[0], StoreTargetLibrary.Shipped[0].Fingerprint, ScaleSource.Scan, 0.01, "measured").Write();

    /// <summary>What a reference gave: read, or refused as an <see cref="InvalidDataException"/> in words. Anything else fails.</summary>
    private static string ReadReference(string json)
    {
        var clock = Stopwatch.StartNew();
        try
        {
            return TargetReference.Read(json).Target.Id;
        }
        catch (InvalidDataException e)
        {
            Assert.DoesNotContain("Exception", e.Message, StringComparison.Ordinal);
            return e.Message;
        }
        finally
        {
            Assert.True(clock.Elapsed < Quick, $"took {clock.Elapsed}");
        }
    }

    [Fact]
    public void AFingerprintReferenceIsReadOrRefusedWhateverItHolds()
    {
        string good = Reference();
        Assert.Equal(StoreTargetLibrary.Shipped[0].Id, ReadReference(good));
        for (int cut = 0; cut < good.Length; cut += Math.Max(1, good.Length / 40))
        {
            ReadReference(good[..cut]);
        }

        var random = new Random(3521);
        for (int trial = 0; trial < 60; trial++)
        {
            char[] damaged = good.ToCharArray();
            for (int k = 0; k < 1 + random.Next(6); k++)
            {
                damaged[random.Next(damaged.Length)] = (char)random.Next(32, 127);
            }

            ReadReference(new string(damaged));
        }

        // Fields of the wrong kind, nesting past any parser's depth, and a fingerprint that is not one.
        var node = JsonNode.Parse(good)!.AsObject();
        foreach (var (field, value) in new (string, JsonNode?)[] { ("format", 5), ("id", new JsonObject()), ("fingerprint", "not base64!"), ("fingerprint", 7), ("scaleUncertainty", "wide"), ("fingerprint", Convert.ToBase64String(new byte[100])) })
        {
            var copy = node.DeepClone().AsObject();
            copy[field] = value?.DeepClone();
            Assert.NotEqual(StoreTargetLibrary.Shipped[0].Id, ReadReference(copy.ToJsonString()));
        }

        Assert.StartsWith("not a GroupLab", ReadReference(new string('[', 100_000) + new string(']', 100_000)), StringComparison.Ordinal);
        Assert.StartsWith("not a GroupLab", ReadReference("\u0000\u0001 binary"), StringComparison.Ordinal);
    }

    /// <summary>
    /// A fingerprint that says it holds billions of features, or unpacks without end, is refused before anything is made for it; a damaged one
    /// is refused as damaged.
    /// </summary>
    [Fact]
    public void AHostileFingerprintCannotAskForMemory()
    {
        static byte[] Packed(Action<BinaryWriter> write)
        {
            using var packed = new MemoryStream();
            using (var zip = new GZipStream(packed, CompressionLevel.Fastest, leaveOpen: true))
            using (var w = new BinaryWriter(zip, Encoding.UTF8, leaveOpen: true))
            {
                write(w);
            }

            return packed.ToArray();
        }

        var hostile = new[]
        {
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write("p"); w.Write(int.MaxValue); }),
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write("p"); w.Write(-1); }),
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write("p"); w.Write(0); w.Write(0.0); w.Write(0.0); w.Write(65536); w.Write(65536); }),
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write("p"); w.Write(0); w.Write(0.0); w.Write(0.0); w.Write(0); w.Write(0); w.Write(int.MaxValue); w.Write(32); }),
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write("p"); w.Write(0); w.Write(0.0); w.Write(0.0); w.Write(0); w.Write(0); w.Write(10); w.Write(-5); }),
            Packed(w => { w.Write(TargetFingerprint.Magic); w.Write7BitEncodedInt(100_000_000); w.Write(new byte[20 << 20]); }),
            Packed(w => w.Write("GLFP1")),
            [0x1F, 0x8B, 8, 0, 0, 0, 0, 0, 0, 0xFF, 1, 2, 3],
            [],
        };
        foreach (byte[] bytes in hostile)
        {
            var clock = Stopwatch.StartNew();
            Assert.Throws<InvalidDataException>(() => TargetFingerprint.Read(new MemoryStream(bytes)));
            Assert.True(clock.Elapsed < Quick);
        }

        byte[] good = StoreTargetLibrary.Shipped[0].Fingerprint.ToBytes();
        for (int cut = 0; cut < good.Length; cut += Math.Max(1, good.Length / 30))
        {
            Assert.Throws<InvalidDataException>(() => TargetFingerprint.Read(new MemoryStream(good[..cut])));
        }
    }

    /// <summary>A library signed with the test key, carrying one product no other test has, so whether it was loaded can be seen.</summary>
    private static string Library(Func<byte[], byte[]>? payloadChange = null, string? privateKey = null)
    {
        var fingerprint = new TargetFingerprint("untrusted-files-test", [new PointD(1, 1)], new byte[32], 32, [new PointD(2, 2)], new ColourLayout(0, 0, 1, 1, [1, 2, 3]));
        var target = new StoreTarget("untrusted-files-test", "Nobody", "A test target", "1 in", "0", null);
        byte[] payload = StoreLibraryFile.Payload(StoreTargetLibrary.BuiltInVersion + 1, [new TargetReference(target, fingerprint, ScaleSource.Scan, 0, "")]);
        return StoreLibraryFile.Sign(payloadChange?.Invoke(payload) ?? payload, Convert.FromBase64String(privateKey ?? Key.PrivateKeyBase64));
    }

    [Fact]
    public void ALibraryThatFailsItsCheckIsNeverLoaded()
    {
        string good = Library();
        Assert.Equal(StoreLibraryFile.Refusal.None, StoreLibraryFile.Read(good, Key.PublicKeyBase64).Refusal);

        // One byte of the payload changed after signing, another key, the wrong algorithm, a signature that is not one.
        var file = JsonNode.Parse(good)!.AsObject();
        byte[] payload = Convert.FromBase64String((string)file["payload"]!);
        payload[payload.Length / 2] ^= 1;
        var tampered = file.DeepClone().AsObject();
        tampered["payload"] = Convert.ToBase64String(payload);
        var wrongAlgorithm = file.DeepClone().AsObject();
        wrongAlgorithm["algorithm"] = "none";
        var noSignature = file.DeepClone().AsObject();
        noSignature["signature"] = "AAAA";
        var cases = new (string Json, StoreLibraryFile.Refusal Refusal)[]
        {
            (tampered.ToJsonString(), StoreLibraryFile.Refusal.BadSignature),
            (Library(privateKey: UpdateSignature.NewKeyPair().PrivateKeyBase64), StoreLibraryFile.Refusal.BadSignature),
            (wrongAlgorithm.ToJsonString(), StoreLibraryFile.Refusal.WrongAlgorithm),
            (noSignature.ToJsonString(), StoreLibraryFile.Refusal.BadSignature),
        };
        string folder = Temp.Folder("untrusted-library");
        Directory.CreateDirectory(folder);
        try
        {
            foreach (var (json, refusal) in cases)
            {
                Assert.Equal((refusal, (StoreLibraryFile.Contents?)null), StoreLibraryFile.Read(json, Key.PublicKeyBase64));
                File.WriteAllText(StoreLibraryUpdate.SavedPath(folder), json);
                Assert.Equal("the kept library was refused: " + refusal, StoreLibraryUpdate.LoadSaved(folder, Key.PublicKeyBase64));
                Assert.Null(StoreTargetLibrary.Find("untrusted-files-test"));
            }

            // A kept file larger than any library is refused without being read.
            using (var huge = File.Create(StoreLibraryUpdate.SavedPath(folder)))
            {
                huge.SetLength(StoreLibraryFile.MostChars + 1L);
            }

            Assert.Equal("the kept library was refused: TooLarge", StoreLibraryUpdate.LoadSaved(folder, Key.PublicKeyBase64));
            Assert.Null(StoreTargetLibrary.Find("untrusted-files-test"));
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    /// <summary>
    /// Whatever the file holds, truncated, damaged, the wrong kinds in its fields or nested past any depth, the library reader answers with a
    /// refusal and never throws; and a payload the key really signed but that is not a library is refused too.
    /// </summary>
    [Fact]
    public void ALibraryIsReadOrRefusedWhateverItHolds()
    {
        string good = Library();
        for (int cut = 0; cut < good.Length; cut += Math.Max(1, good.Length / 40))
        {
            Assert.NotEqual(StoreLibraryFile.Refusal.None, StoreLibraryFile.Read(good[..cut], Key.PublicKeyBase64).Refusal);
        }

        var random = new Random(3522);
        for (int trial = 0; trial < 60; trial++)
        {
            char[] damaged = good.ToCharArray();
            for (int k = 0; k < 1 + random.Next(6); k++)
            {
                damaged[random.Next(damaged.Length)] = (char)random.Next(32, 127);
            }

            var (refusal, contents) = StoreLibraryFile.Read(new string(damaged), Key.PublicKeyBase64);
            Assert.True(refusal != StoreLibraryFile.Refusal.None || contents is not null);
        }

        foreach (string json in new[]
        {
            "{\"payload\": 5, \"signature\": \"AAAA\", \"algorithm\": \"x\"}",
            "{\"payload\": \"AAAA\", \"signature\": {}, \"algorithm\": []}",
            "{\"payload\": \"AAAA\", \"signature\": \"AAAA\", \"algorithm\": 7}",
            "[1, 2, 3]",
            new string('{', 100_000),
            "\u0000",
        })
        {
            Assert.NotEqual(StoreLibraryFile.Refusal.None, StoreLibraryFile.Read(json, Key.PublicKeyBase64).Refusal);
        }

        foreach (string payload in new[]
        {
            "not json",
            "{\"format\": \"grouplab-target-library-1\"}",
            "{\"format\": \"grouplab-target-library-1\", \"targets\": \"x\"}",
            "{\"format\": \"grouplab-target-library-1\", \"targets\": [{\"format\": 1}], \"version\": 9}",
            "{\"format\": \"grouplab-target-library-1\", \"targets\": [], \"version\": \"nine\"}",
            "{\"format\": 5}",
        })
        {
            string signed = StoreLibraryFile.Sign(Encoding.UTF8.GetBytes(payload), Convert.FromBase64String(Key.PrivateKeyBase64));
            Assert.Equal(StoreLibraryFile.Refusal.UnknownFormat, StoreLibraryFile.Read(signed, Key.PublicKeyBase64).Refusal);
        }
    }

    /// <summary>A stream that never ends, counting what it gave.</summary>
    private sealed class EndlessStream(byte value) : Stream
    {
        public long Given { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => Given; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, value, offset, count);
            Given += count;
            return count;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
