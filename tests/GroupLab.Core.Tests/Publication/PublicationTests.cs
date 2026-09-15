using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Publication;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Publication;

/// <summary>Synthetic images with the metadata a phone writes, for the scrubbing and intake tests.</summary>
internal static class PhoneImages
{
    /// <summary>
    /// A little-endian JPEG like a phone's: Make, Model and Orientation 6; an EXIF block with a capture date, a focal length and a
    /// digital zoom ratio; a GPS block with a latitude; XMP carrying GPS; a comment; a scan of made-up entropy bytes with a stuffed
    /// FF 00; and a trailer after the end marker, as a motion photo appends.
    /// </summary>
    public static byte[] Jpeg()
    {
        byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s + "\0");
        byte[] Rational(params (uint N, uint D)[] values) => [.. values.SelectMany(v => BitConverter.GetBytes(v.N).Concat(BitConverter.GetBytes(v.D)))];
        var tiff = Tiff(
            [(0x010F, 2, Ascii("samsung")), (0x0110, 2, Ascii("SM-G965U")), (0x0112, 3, BitConverter.GetBytes((ushort)6))],
            [(0x829A, 5, Rational((1, 120))), (0x8827, 3, BitConverter.GetBytes((ushort)400)), (0x9003, 2, Ascii("2026:09:14 10:00:00")), (0x920A, 5, Rational((43, 10))), (0xA404, 5, Rational((164, 100))), (0xA434, 2, Ascii("Galaxy S9+ Rear Camera"))],
            [(0x0001, 2, Ascii("N")), (0x0002, 5, Rational((33, 1), (12, 1), (3456, 100)))]);
        var file = new List<byte> { 0xFF, 0xD8 };
        void Segment(byte marker, byte[] body)
        {
            file.AddRange([0xFF, marker, (byte)((body.Length + 2) >> 8), (byte)(body.Length + 2)]);
            file.AddRange(body);
        }

        Segment(0xE0, [.. "JFIF\0"u8.ToArray(), 1, 1, 0, 0, 1, 0, 1, 0, 0]);
        Segment(0xE1, [.. "Exif\0\0"u8.ToArray(), .. tiff]);
        Segment(0xE1, Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0<x:xmpmeta><exif:GPSLatitude>33,12.5N</exif:GPSLatitude></x:xmpmeta>"));
        Segment(0xFE, Encoding.ASCII.GetBytes("owner: somebody"));
        Segment(0xDB, [0, .. Enumerable.Repeat((byte)1, 64)]);
        Segment(0xC0, [8, 0, 16, 0, 16, 1, 1, 0x11, 0]);
        Segment(0xDA, [1, 1, 0, 0, 63, 0]);
        file.AddRange([0x12, 0x34, 0xFF, 0x00, 0x56, 0xFF, 0xD9]);
        file.AddRange(Encoding.ASCII.GetBytes("MotionPhoto_Data trailer"));
        return [.. file];
    }

    /// <summary>
    /// A copy of a real JPEG with a location written into it, NOTES-FROM-PLANNING.md entry 31 section 2: a GPS block holding the given
    /// latitude and longitude, an XMP segment naming GPS, a comment, and a trailer after the end marker. The GPS block hangs off a copy
    /// of the primary IFD appended to the file's own EXIF block, with the header pointed at it; the original primary IFD stays in place,
    /// unreferenced, so every value offset the file already has stays valid and the camera fields read exactly as before.
    /// </summary>
    public static byte[] WithLocation(byte[] jpeg, (uint Degrees, uint Minutes, uint CentiSeconds) latitude, (uint Degrees, uint Minutes, uint CentiSeconds) longitude)
    {
        int pos = 2, app1 = -1, app1Length = 0;
        while (pos + 4 <= jpeg.Length && jpeg[pos] == 0xFF && jpeg[pos + 1] != 0xDA)
        {
            int length = (jpeg[pos + 2] << 8) | jpeg[pos + 3];
            if (jpeg[pos + 1] == 0xE1 && jpeg.AsSpan(pos + 4, 6).SequenceEqual("Exif\0\0"u8))
            {
                app1 = pos;
                app1Length = length;
                break;
            }

            pos += 2 + length;
        }

        if (app1 < 0)
        {
            throw new InvalidDataException("the photograph has no EXIF block to write a location into");
        }

        byte[] tiff = jpeg.AsSpan(app1 + 10, app1Length - 8).ToArray();
        bool little = tiff[0] == (byte)'I';
        ushort U16(int at) => little ? BinaryPrimitives.ReadUInt16LittleEndian(tiff.AsSpan(at)) : BinaryPrimitives.ReadUInt16BigEndian(tiff.AsSpan(at));
        uint U32(int at) => little ? BinaryPrimitives.ReadUInt32LittleEndian(tiff.AsSpan(at)) : BinaryPrimitives.ReadUInt32BigEndian(tiff.AsSpan(at));
        ushort Tag(byte[] entry) => little ? BinaryPrimitives.ReadUInt16LittleEndian(entry) : BinaryPrimitives.ReadUInt16BigEndian(entry);
        byte[] W16(int v)
        {
            var b = new byte[2];
            if (little)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(b, (ushort)v);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(b, (ushort)v);
            }

            return b;
        }

        byte[] W32(uint v)
        {
            var b = new byte[4];
            if (little)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b, v);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(b, v);
            }

            return b;
        }

        int ifd0 = (int)U32(4), count = U16(ifd0);
        uint next = U32(ifd0 + 2 + (12 * count));
        var entries = Enumerable.Range(0, count).Select(k => tiff.AsSpan(ifd0 + 2 + (12 * k), 12).ToArray()).ToList();
        entries.RemoveAll(e => Tag(e) == 0x8825);

        var output = new List<byte>(tiff);
        if (output.Count % 2 != 0)
        {
            output.Add(0);
        }

        int newIfd0 = output.Count;
        int gpsIfd = newIfd0 + 2 + (12 * (entries.Count + 1)) + 4;
        int gpsValues = gpsIfd + 2 + (12 * 4) + 4;
        entries.Add([.. W16(0x8825), .. W16(4), .. W32(1), .. W32((uint)gpsIfd)]);
        entries.Sort((a, b) => Tag(a).CompareTo(Tag(b)));
        output.AddRange(W16(entries.Count));
        entries.ForEach(output.AddRange);
        output.AddRange(W32(next));
        output.AddRange(W16(4));
        output.AddRange([.. W16(0x0001), .. W16(2), .. W32(2), (byte)'N', 0, 0, 0]);
        output.AddRange([.. W16(0x0002), .. W16(5), .. W32(3), .. W32((uint)gpsValues)]);
        output.AddRange([.. W16(0x0003), .. W16(2), .. W32(2), (byte)'W', 0, 0, 0]);
        output.AddRange([.. W16(0x0004), .. W16(5), .. W32(3), .. W32((uint)gpsValues + 24)]);
        output.AddRange(W32(0));
        foreach (var (degrees, minutes, centiSeconds) in new[] { latitude, longitude })
        {
            output.AddRange([.. W32(degrees), .. W32(1), .. W32(minutes), .. W32(1), .. W32(centiSeconds), .. W32(100)]);
        }

        byte[] header = W32((uint)newIfd0);
        for (int i = 0; i < 4; i++)
        {
            output[4 + i] = header[i];
        }

        if (output.Count + 8 > 0xFFFF)
        {
            throw new InvalidDataException("the EXIF block with a location added no longer fits one segment");
        }

        var file = new List<byte>(jpeg.Length + 512);
        void Segment(byte marker, byte[] body)
        {
            file.AddRange([0xFF, marker, (byte)((body.Length + 2) >> 8), (byte)(body.Length + 2)]);
            file.AddRange(body);
        }

        file.AddRange(jpeg.AsSpan(0, app1));
        Segment(0xE1, [.. "Exif\0\0"u8.ToArray(), .. output]);
        Segment(0xE1, Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0<x:xmpmeta><exif:GPSLatitude>33,12.5N</exif:GPSLatitude></x:xmpmeta>"));
        Segment(0xFE, Encoding.ASCII.GetBytes("owner: somebody"));
        file.AddRange(jpeg.AsSpan(app1 + 2 + app1Length));
        file.AddRange(Encoding.ASCII.GetBytes("MotionPhoto_Data trailer"));
        return [.. file];
    }

    /// <summary>A PNG with an eXIf chunk holding a GPS block and a text chunk naming a location.</summary>
    public static byte[] Png()
    {
        var tiff = Tiff([(0x0112, 3, BitConverter.GetBytes((ushort)1))], [], [(0x0001, 2, Encoding.ASCII.GetBytes("N\0"))]);
        var file = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        void Chunk(string type, byte[] body)
        {
            file.AddRange(BitConverter.GetBytes(body.Length).Reverse());
            file.AddRange(Encoding.ASCII.GetBytes(type));
            file.AddRange(body);
            file.AddRange(new byte[4]);
        }

        Chunk("IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 0, 0, 0, 0]);
        Chunk("eXIf", tiff);
        Chunk("tEXt", Encoding.ASCII.GetBytes("Comment\0GPS 33.2N 96.6W"));
        Chunk("IDAT", [0x78, 0x9C, 0x63, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01]);
        Chunk("IEND", []);
        return [.. file];
    }

    /// <summary>A little-endian TIFF with a primary IFD, an EXIF IFD and a GPS IFD, pointers filled in and values laid out after the IFDs.</summary>
    private static byte[] Tiff(List<(int Tag, int Type, byte[] Value)> primary, List<(int Tag, int Type, byte[] Value)> exif, List<(int Tag, int Type, byte[] Value)> gps)
    {
        var ifds = new List<List<(int Tag, int Type, byte[] Value)>> { new(primary), exif, gps };
        if (exif.Count > 0)
        {
            ifds[0].Add((0x8769, 4, new byte[4]));
        }

        if (gps.Count > 0)
        {
            ifds[0].Add((0x8825, 4, new byte[4]));
        }

        int[] starts = new int[3];
        int at = 8;
        for (int i = 0; i < 3; i++)
        {
            starts[i] = at;
            at += ifds[i].Count == 0 ? 0 : 2 + (12 * ifds[i].Count) + 4;
        }

        var data = new List<byte>();
        var output = new List<byte>();
        output.AddRange("II"u8.ToArray());
        output.AddRange(BitConverter.GetBytes((ushort)42));
        output.AddRange(BitConverter.GetBytes(8u));
        for (int i = 0; i < 3; i++)
        {
            if (ifds[i].Count == 0)
            {
                continue;
            }

            output.AddRange(BitConverter.GetBytes((ushort)ifds[i].Count));
            foreach (var (tag, type, value) in ifds[i].OrderBy(e => e.Tag))
            {
                int size = type switch { 3 => 2, 4 => 4, 5 => 8, _ => 1 };
                output.AddRange(BitConverter.GetBytes((ushort)tag));
                output.AddRange(BitConverter.GetBytes((ushort)type));
                output.AddRange(BitConverter.GetBytes((uint)(value.Length / size)));
                if (tag is 0x8769 or 0x8825)
                {
                    output.AddRange(BitConverter.GetBytes((uint)starts[tag == 0x8769 ? 1 : 2]));
                }
                else if (value.Length <= 4)
                {
                    output.AddRange([.. value, .. new byte[4 - value.Length]]);
                }
                else
                {
                    output.AddRange(BitConverter.GetBytes((uint)(at + data.Count)));
                    data.AddRange(value);
                }
            }

            output.AddRange(BitConverter.GetBytes(0u));
        }

        return [.. output, .. data];
    }
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 22 sections 2 and 3: scrubbing removes every place a location can hide and changes no pixel, and the
/// repository fails its tests when an image under public test data carries a location, is not cleared for publication, or lacks
/// provenance, or when any committed image carries GPS at all.
/// </summary>
public class PublicationTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public void ScrubbingRemovesEveryLocationAndKeepsTheCameraFactsAndThePixels()
    {
        byte[] original = PhoneImages.Jpeg();
        Assert.Equal(3, PublicationCheck.LocationProblems(original).Count);

        var scrubbed = ImageScrubber.Scrub(original);

        Assert.Empty(PublicationCheck.LocationProblems(scrubbed.Bytes));
        Assert.Contains("GPS", scrubbed.Removed);
        Assert.Contains("XMP", scrubbed.Removed);
        Assert.Contains("comment", scrubbed.Removed);
        Assert.Contains(scrubbed.Removed, r => r.Contains("after the image's end marker", StringComparison.Ordinal));
        Assert.DoesNotContain("2026:09:14", Encoding.ASCII.GetString(scrubbed.Bytes), StringComparison.Ordinal);
        Assert.DoesNotContain("somebody", Encoding.ASCII.GetString(scrubbed.Bytes), StringComparison.Ordinal);

        var before = ImageMetadataReader.Read(original);
        var after = ImageMetadataReader.Read(scrubbed.Bytes);
        Assert.Equal((before.CameraMake, before.CameraModel, before.Orientation, before.FocalLengthMm, before.DigitalZoomRatio), (after.CameraMake, after.CameraModel, after.Orientation, after.FocalLengthMm, after.DigitalZoomRatio));
        Assert.Equal(1.64, after.DigitalZoomRatio!.Value, 12);

        static byte[] Scan(byte[] f) => f[f.AsSpan().IndexOf((ReadOnlySpan<byte>)[0xFF, 0xDA])..(f.AsSpan().IndexOf((ReadOnlySpan<byte>)[0xFF, 0xD9]) + 2)];
        Assert.Equal(Scan(original), Scan(scrubbed.Bytes));

        var png = ImageScrubber.Scrub(PhoneImages.Png());
        Assert.Equal(2, PublicationCheck.LocationProblems(PhoneImages.Png()).Count);
        Assert.Empty(PublicationCheck.LocationProblems(png.Bytes));
        Assert.Equal(["eXIf chunk", "tEXt chunk"], png.Removed);
    }

    /// <summary>
    /// A real camera JPEG with a location the test writes into it, NOTES-FROM-PLANNING.md entry 31 section 2. No committed image carries
    /// a location any more, so a copy of <c>main1.jpg</c> is given a GPS block with the test's own coordinates, GPS in XMP, a comment and
    /// a motion-photo trailer. The scrubber must remove all four, keep every camera field GroupLab reads including the digital zoom, and
    /// leave the decoded pixels identical to the committed file's.
    /// <para>
    /// What this no longer covers, and why: the committed photographs were scrubbed in the 2026-09-14 rewrite, so the maker notes,
    /// thumbnails, application segments and multi-picture indexes a phone writes are no longer in the repository for this test to
    /// remove. Their removal was shown on the original files before the rewrite (docs/PHASE1-RESULTS.md "Entries 22 and 27"), and the
    /// synthetic phone image keeps the segment handling covered.
    /// </para>
    /// </summary>
    [Fact]
    public void ARealPhonePhotographWithALocationWrittenIntoItScrubsToIdenticalPixels()
    {
        byte[] committed = File.ReadAllBytes(Repo.PathTo("scans", "phase0", "main1.jpg"));
        byte[] located = PhoneImages.WithLocation(committed, latitude: (33, 12, 3456), longitude: (96, 36, 1234));
        var problems = PublicationCheck.LocationProblems(located);
        Assert.Contains("an EXIF GPS block", problems);
        Assert.Contains("GPS fields in XMP", problems);
        Assert.Contains(problems, p => p.Contains("after the image's end marker", StringComparison.Ordinal));

        var scrubbed = ImageScrubber.Scrub(located);
        Assert.Empty(PublicationCheck.LocationProblems(scrubbed.Bytes));
        Assert.Contains("GPS", scrubbed.Removed);
        Assert.Contains("XMP", scrubbed.Removed);
        Assert.Contains("comment", scrubbed.Removed);
        Assert.Contains(scrubbed.Removed, r => r.Contains("after the image's end marker", StringComparison.Ordinal));

        var before = ImageMetadataReader.Read(committed);
        Assert.Equal(before, ImageMetadataReader.Read(located));
        Assert.Equal(before, ImageMetadataReader.Read(scrubbed.Bytes));
        Assert.NotNull(before.DigitalZoomRatio);
        Assert.NotNull(before.FocalLengthMm);

        using var original = OpenCvSharp.Cv2.ImDecode(committed, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
        using var withLocation = OpenCvSharp.Cv2.ImDecode(located, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
        using var clean = OpenCvSharp.Cv2.ImDecode(scrubbed.Bytes, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
        Assert.False(original.Empty());
        Assert.Equal(0, OpenCvSharp.Cv2.Norm(original, withLocation, OpenCvSharp.NormTypes.INF));
        Assert.Equal(0, OpenCvSharp.Cv2.Norm(original, clean, OpenCvSharp.NormTypes.INF));
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 29 section 4 step 3, kept as a standing check: every committed JPEG, scrubbed, decodes to exactly the
    /// same colour pixels as before, and carries no location afterwards. One file proves the method; every file proves the job.
    /// </summary>
    [Fact]
    public void EveryCommittedPhotographScrubsToIdenticalPixels()
    {
        var git = Process.Start(new ProcessStartInfo("git", "ls-files") { WorkingDirectory = Repo.PathTo(), RedirectStandardOutput = true, UseShellExecute = false })!;
        var jpegs = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".jpg" or ".jpeg").ToList();
        git.WaitForExit();

        var changed = new List<string>();
        foreach (string file in jpegs)
        {
            byte[] original = File.ReadAllBytes(Repo.PathTo(file));
            byte[] scrubbed = ImageScrubber.Scrub(original).Bytes;
            using var before = OpenCvSharp.Cv2.ImDecode(original, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
            using var after = OpenCvSharp.Cv2.ImDecode(scrubbed, OpenCvSharp.ImreadModes.Color | OpenCvSharp.ImreadModes.IgnoreOrientation);
            bool same = before.Size() == after.Size() && before.Type() == after.Type() && OpenCvSharp.Cv2.Norm(before, after, OpenCvSharp.NormTypes.INF) == 0;
            if (!same || PublicationCheck.LocationProblems(scrubbed).Any(p => p.Contains("GPS", StringComparison.Ordinal)))
            {
                changed.Add(file);
            }
        }

        output.WriteLine($"{jpegs.Count} committed JPEGs scrubbed; {jpegs.Count - changed.Count} decode to identical pixels with no location left");
        Assert.True(jpegs.Count >= 16, $"git ls-files listed {jpegs.Count} JPEGs");
        Assert.True(changed.Count == 0, "pixels changed or a location survived: " + string.Join(", ", changed));
    }

    /// <summary>
    /// The donated test data checkout, NOTES-FROM-PLANNING.md entry 28 section 4 and the README's "Test data": the directory
    /// <c>GROUPLAB_TESTDATA</c> names, or <c>grouplab-testdata</c> beside this repository; null when there is neither.
    /// </summary>
    private static string? TestDataRoot()
    {
        string? configured = Environment.GetEnvironmentVariable("GROUPLAB_TESTDATA");
        if (configured is not null)
        {
            Assert.True(Directory.Exists(configured), $"GROUPLAB_TESTDATA names {configured}, which does not exist");
            return configured;
        }

        string beside = Path.GetFullPath(Path.Combine(Repo.PathTo(), "..", "grouplab-testdata"));
        return Directory.Exists(beside) ? beside : null;
    }

    /// <summary>
    /// Entry 22 section 3 and entry 34 sections 2, 3 and 5, for the public test data checkout:
    /// <list type="bullet">
    /// <item>every donated submission has a complete provenance record with the consent text, is cleared for publication, and is named
    /// as the upload page names it;</item>
    /// <item><c>owner/</c> has a provenance record saying who took the photographs and on what terms;</item>
    /// <item>no image carries a location, every image is in its record with the hash it was published at, every file recorded as
    /// published is present, and no image lies anywhere a record does not cover;</item>
    /// <item>everyone who gave a credit name is in <c>CONTRIBUTORS.md</c>.</item>
    /// </list>
    /// Without a checkout it does nothing and says so, so CI does not need the data.
    /// </summary>
    [Fact]
    public void PublicTestDataCarriesNoLocationNoOptOutAndFullProvenance()
    {
        if (TestDataRoot() is not { } data)
        {
            output.WriteLine("not run: there is no grouplab-testdata checkout beside this repository, and GROUPLAB_TESTDATA is not set");
            return;
        }

        var failures = new List<string>();
        var credited = new List<(string Submission, string Name)>();
        string donated = Path.Combine(data, "donated");
        int submissions = 0;
        foreach (string submission in Directory.Exists(donated) ? Directory.EnumerateDirectories(donated) : [])
        {
            submissions++;
            string name = "donated/" + Path.GetFileName(submission);
            var provenance = Provenance(submission);
            string? id = (string?)provenance?["submissionId"], submitted = (string?)provenance?["submittedUtc"];
            if (provenance?["consent"]?["text"] is null || provenance["consent"]?["version"] is null || id is null || submitted is null)
            {
                failures.Add($"{name}: no complete provenance record");
            }
            else if (Intake.DirectoryName(id, submitted) != Path.GetFileName(submission))
            {
                failures.Add($"{name}: not named as the upload page names it, {Intake.DirectoryName(id, submitted) ?? "its UTC date and identifier"}");
            }

            if (provenance?["excludeFromPublicDataset"]?.GetValueKind() != System.Text.Json.JsonValueKind.False)
            {
                failures.Add($"{name}: not recorded as cleared for publication");
            }

            if ((string?)provenance?["answers"]?["credit_name"] is { Length: > 0 } credit)
            {
                credited.Add((name, credit.Trim()));
            }

            CheckPublishedFiles(name, submission, provenance, failures);
        }

        string owner = Path.Combine(data, "owner");
        if (Directory.Exists(owner))
        {
            var provenance = Provenance(owner);
            if ((string?)provenance?["format"] != Intake.OwnerProvenanceFormat || (string?)provenance?["takenBy"] is not { Length: > 0 } || (string?)provenance?["statement"] is not { Length: > 0 })
            {
                failures.Add("owner: no complete provenance record saying who took the photographs and on what terms they are published");
            }

            CheckPublishedFiles("owner", owner, provenance, failures);
        }

        foreach (string image in Directory.EnumerateFiles(data, "*", SearchOption.AllDirectories).Where(IsImage))
        {
            string[] parts = Path.GetRelativePath(data, image).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!(parts is ["donated", _, _] || parts is ["owner", _]))
            {
                failures.Add($"{string.Join('/', parts)}: an image outside donated/<submission>/ and owner/, where no provenance record covers it");
            }
        }

        string contributorsPath = Path.Combine(data, "CONTRIBUTORS.md");
        string contributors = File.Exists(contributorsPath) ? File.ReadAllText(contributorsPath) : "";
        if (!File.Exists(contributorsPath))
        {
            failures.Add("CONTRIBUTORS.md is missing");
        }

        failures.AddRange(credited.Where(c => !contributors.Contains(c.Name, StringComparison.Ordinal)).Select(c => $"{c.Submission}: gave the credit name \"{c.Name}\", which CONTRIBUTORS.md does not list"));
        output.WriteLine($"checked {submissions} donated submissions{(Directory.Exists(owner) ? " and owner/" : "")} in {data}");
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    private static bool IsImage(string path) => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    private static JsonNode? Provenance(string directory)
    {
        string path = Path.Combine(directory, PublicationCheck.ProvenanceFile);
        return File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) : null;
    }

    /// <summary>No location in any image; each image in the record at the hash it was published at; each file recorded as published present.</summary>
    private static void CheckPublishedFiles(string name, string directory, JsonNode? provenance, List<string> failures)
    {
        var entries = (provenance?["files"] as JsonArray)?.OfType<JsonObject>().ToList() ?? [];
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (string image in Directory.EnumerateFiles(directory).Where(IsImage))
        {
            string file = Path.GetFileName(image);
            present.Add(file);
            byte[] bytes = File.ReadAllBytes(image);
            failures.AddRange(PublicationCheck.LocationProblems(bytes).Select(p => $"{name}/{file}: {p}"));
            if ((string?)entries.FirstOrDefault(e => (string?)e["storedName"] == file)?["publishedSha256"] != Intake.Sha256(bytes))
            {
                failures.Add($"{name}/{file}: not in the provenance record with its published hash");
            }
        }

        failures.AddRange(entries
            .Where(e => (string?)e["publishedSha256"] is not null && !present.Contains((string?)e["storedName"] ?? ""))
            .Select(e => $"{name}/{(string?)e["storedName"]}: recorded as published, and not present"));
    }

    /// <summary>
    /// No committed image anywhere in the repository carries a GPS block of any kind, and there is no allowlist: NOTES-FROM-PLANNING.md
    /// entry 29 section 3, after the sixteen Phase 0 photographs were replaced in history with scrubbed copies. A rule with no
    /// exceptions cannot go stale.
    /// </summary>
    [Fact]
    public void NoCommittedImageCarriesGps()
    {
        var git = Process.Start(new ProcessStartInfo("git", "ls-files") { WorkingDirectory = Repo.PathTo(), RedirectStandardOutput = true, UseShellExecute = false })!;
        var tracked = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(f => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
        git.WaitForExit();
        Assert.True(tracked.Count > 50, $"git ls-files listed {tracked.Count} images");

        var carrying = tracked.Where(f => PublicationCheck.LocationProblems(File.ReadAllBytes(Repo.PathTo(f))).Any(p => p.Contains("GPS", StringComparison.Ordinal))).Order(StringComparer.Ordinal).ToList();
        Assert.True(carrying.Count == 0, "committed images carrying GPS: " + string.Join(", ", carrying));
    }
}
