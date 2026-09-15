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
            [(0x9003, 2, Ascii("2026:09:14 10:00:00")), (0x920A, 5, Rational((43, 10))), (0xA404, 5, Rational((164, 100)))],
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
/// repository fails its tests when an image under public test data carries a location, sits in an opted-out submission, or lacks
/// provenance, or when any committed image carries GPS other than the ones question 13 is about.
/// </summary>
public class PublicationTests(Xunit.Abstractions.ITestOutputHelper output)
{
    /// <summary>
    /// The committed Phase 0 phone photographs that carry an EXIF GPS block, 13 of them with a non-zero position. They are in history,
    /// and what to do about that is `docs/QUESTIONS-FOR-PLANNING.md` question 13. They are named so that no other image can join them
    /// unnoticed, and so that scrubbing one fails this test until it is taken off the list.
    /// </summary>
    private static readonly string[] AwaitingQuestion13 =
    [
        "scans/phase0/20260913_130543.jpg", "scans/phase0/20260913_130550.jpg", "scans/phase0/20260913_130554.jpg", "scans/phase0/20260913_130559.jpg",
        "scans/phase0/main1.jpg", "scans/phase0/main2.jpg", "scans/phase0/main3.jpg",
        "scans/phase0/main_flat1.jpg", "scans/phase0/main_flat2.jpg", "scans/phase0/main_flat3.jpg",
        "scans/phase0/telephoto1.jpg", "scans/phase0/telephoto2.jpg", "scans/phase0/telephoto3.jpg",
        "scans/phase0/ultrawide1.jpg", "scans/phase0/ultrawide2.jpg", "scans/phase0/ultrawide3.jpg",
    ];

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

    /// <summary>On a real committed phone photograph with coordinates: none survive, the lens facts do, and the decoded pixels are identical.</summary>
    [Fact]
    public void ARealPhonePhotographScrubsToIdenticalPixels()
    {
        string path = Repo.PathTo("scans", "phase0", "main1.jpg");
        byte[] original = File.ReadAllBytes(path);
        Assert.Contains("an EXIF GPS block", PublicationCheck.LocationProblems(original));

        var scrubbed = ImageScrubber.Scrub(original);
        Assert.DoesNotContain(PublicationCheck.LocationProblems(scrubbed.Bytes), p => p.Contains("GPS", StringComparison.Ordinal));

        var before = ImageMetadataReader.Read(original);
        var after = ImageMetadataReader.Read(scrubbed.Bytes);
        Assert.Equal(before with { }, after with { DpiX = before.DpiX, DpiY = before.DpiY });

        string copy = Path.Combine(Path.GetTempPath(), $"grouplab-scrubbed-{Guid.NewGuid():N}.jpg");
        try
        {
            File.WriteAllBytes(copy, scrubbed.Bytes);
            Assert.True(ImageLoader.Load(path).Image.Pixels.AsSpan().SequenceEqual(ImageLoader.Load(copy).Image.Pixels));
        }
        finally
        {
            File.Delete(copy);
        }
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
    /// Entry 22 section 3, for the public test data: no location, nothing not cleared for publication, and for every image a provenance
    /// record with the consent text and its published hash. Without a checkout it does nothing and says so.
    /// </summary>
    [Fact]
    public void PublicTestDataCarriesNoLocationNoOptOutAndFullProvenance()
    {
        if (TestDataRoot() is not { } data)
        {
            output.WriteLine("not run: there is no grouplab-testdata checkout beside this repository, and GROUPLAB_TESTDATA is not set");
            return;
        }

        string root = Path.Combine(data, "donated");
        var failures = new List<string>();
        foreach (string submission in Directory.Exists(root) ? Directory.EnumerateDirectories(root) : [])
        {
            string name = Path.GetFileName(submission);
            string provenancePath = Path.Combine(submission, PublicationCheck.ProvenanceFile);
            var provenance = File.Exists(provenancePath) ? JsonNode.Parse(File.ReadAllText(provenancePath)) : null;
            if (provenance?["consent"]?["text"] is null || provenance["consent"]?["version"] is null || provenance["submissionId"] is null || provenance["submittedUtc"] is null)
            {
                failures.Add($"{name}: no complete provenance record");
            }

            if (provenance?["excludeFromPublicDataset"]?.GetValueKind() != System.Text.Json.JsonValueKind.False)
            {
                failures.Add($"{name}: not recorded as cleared for publication");
            }

            foreach (string image in Directory.EnumerateFiles(submission, "*", SearchOption.AllDirectories).Where(p => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant())))
            {
                byte[] bytes = File.ReadAllBytes(image);
                failures.AddRange(PublicationCheck.LocationProblems(bytes).Select(p => $"{name}/{Path.GetFileName(image)}: {p}"));
                var entry = (provenance?["files"] as JsonArray)?.FirstOrDefault(f => (string?)f?["storedName"] == Path.GetFileName(image));
                if ((string?)entry?["publishedSha256"] != Intake.Sha256(bytes))
                {
                    failures.Add($"{name}/{Path.GetFileName(image)}: not in the provenance record with its published hash");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>No committed image anywhere in the repository carries GPS, except the ones question 13 is about, which still must.</summary>
    [Fact]
    public void NoCommittedImageCarriesGpsBeyondTheOnesQuestion13IsAbout()
    {
        var git = Process.Start(new ProcessStartInfo("git", "ls-files") { WorkingDirectory = Repo.PathTo(), RedirectStandardOutput = true, UseShellExecute = false })!;
        var tracked = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(f => PublicationCheck.ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
        git.WaitForExit();
        Assert.True(tracked.Count > 50, $"git ls-files listed {tracked.Count} images");

        var carrying = tracked.Where(f => PublicationCheck.LocationProblems(File.ReadAllBytes(Repo.PathTo(f))).Any(p => p.Contains("GPS", StringComparison.Ordinal))).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(AwaitingQuestion13.Order(StringComparer.Ordinal), carrying);
    }
}
