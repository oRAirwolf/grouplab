using System.IO.Compression;
using System.Text;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Tests.Publication;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 219 item A5, docs/ANDROID.md section 8 stage A: a session as one file, shared by hand between the phone and
/// the desktop. It comes back as it went, it says who wrote which revision, a stranger's file is read only as far as its known entries and
/// their sizes, and the picture in it carries no location.
/// </summary>
public sealed class SessionPackageTests
{
    private static MarkingState Marked()
    {
        var session = new MarkingSession();
        session.Open("C:/somewhere/private/IMG_1234.jpg", 6);
        session.SetCalibre(Calibre.Of(0.264));
        session.SetShotDistance(3600);
        session.AddShot(new PointD(100, 120));
        session.AddShot(new PointD(140, 118));
        return session.State;
    }

    private static byte[] Package(MarkingState state, byte[] image, string device = "Pixel 9", int revision = 3)
    {
        using var stream = new MemoryStream();
        SessionPackage.Write(stream, state, BuiltIns.Load("GL-CF25-LTR.gltd.json"), UnitSettings.Imperial, image, ".jpg", device, revision, new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc));
        return stream.ToArray();
    }

    [Fact]
    public void ASessionComesBackAsItWentWithWhoWroteIt()
    {
        byte[] image = PhoneImages.Jpeg();
        var back = SessionPackage.Read(new MemoryStream(Package(Marked(), image)));
        Assert.Equal(2, back.State.Shots.Count);
        Assert.Equal(new PointD(140, 118), back.State.Shots[1].Image);
        Assert.Equal(0.264, back.State.Calibre?.DiameterInches);
        Assert.Equal(3600, back.State.ShotDistanceInches);
        Assert.NotNull(back.Definition);
        Assert.Equal(image, back.Image);
        Assert.Equal(("Pixel 9", 3), (back.Device, back.Revision));
        Assert.Null(back.State.ImagePath);
    }

    [Fact]
    public void NoPathFromTheWritersMachineTravels()
    {
        using var zip = new ZipArchive(new MemoryStream(Package(Marked(), PhoneImages.Jpeg())));
        string marking = new StreamReader(zip.GetEntry("marking.json")!.Open()).ReadToEnd();
        Assert.DoesNotContain("somewhere", marking, StringComparison.Ordinal);
        Assert.DoesNotContain("IMG_1234", marking, StringComparison.Ordinal);
        Assert.Equal(["image.jpg", "marking.json", "session.json", "sheet.gltd.json"], zip.Entries.Select(e => e.FullName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void UnpackingPutsTheImageBesideTheMarking()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"grouplab-package-{Guid.NewGuid():N}");
        try
        {
            var state = SessionPackage.Unpack(SessionPackage.Read(new MemoryStream(Package(Marked(), PhoneImages.Jpeg()))), folder);
            Assert.Equal(Path.Combine(folder, "target.jpg"), state.ImagePath);
            Assert.True(File.Exists(state.ImagePath));
        }
        finally
        {
            GroupLab.Tests.Support.Temp.Delete(folder);
        }
    }

    [Fact]
    public void AStrangersFileIsRefusedWithTheReason()
    {
        var notZip = Assert.Throws<SessionPackageException>(() => SessionPackage.Read(new MemoryStream(Encoding.ASCII.GetBytes("not a zip at all"))));
        Assert.Contains("not a GroupLab session file", notZip.Message, StringComparison.Ordinal);

        using var huge = new MemoryStream();
        using (var zip = new ZipArchive(huge, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var w = new StreamWriter(zip.CreateEntry("session.json").Open()))
            {
                w.Write("{\"schema\":\"grouplab-session-1\"}");
            }

            // Five megabytes of one letter compress to almost nothing; the reader stops at its limit rather than trusting the size claimed.
            using var marking = zip.CreateEntry("marking.json").Open();
            marking.Write(Encoding.ASCII.GetBytes(new string('x', 5 * 1024 * 1024)));
        }

        huge.Position = 0;
        var tooBig = Assert.Throws<SessionPackageException>(() => SessionPackage.Read(huge));
        Assert.Contains("larger than GroupLab reads", tooBig.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePictureInASessionFileCarriesNoLocation()
    {
        using var pixels = new Mat(64, 96, MatType.CV_8UC3, new Scalar(200, 180, 160));
        byte[] located = PhoneImages.WithLocation(WithExif(pixels.ImEncode(".jpg")), (51, 30, 1200), (0, 7, 3900));
        Assert.Contains("GPS", Encoding.ASCII.GetString(located), StringComparison.Ordinal);
        string path = Path.Combine(Path.GetTempPath(), $"grouplab-located-{Guid.NewGuid():N}.jpg");
        try
        {
            File.WriteAllBytes(path, located);
            var (clean, extension) = CleanImage.From(path)!.Value;
            Assert.Equal(".jpg", extension);
            string text = Encoding.ASCII.GetString(clean);
            Assert.DoesNotContain("GPS", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Exif", text, StringComparison.Ordinal);
            Assert.Null(ImageMetadataReader.Read(clean).Orientation);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A real, decodable JPEG with the fixture photograph's EXIF block spliced in after its start marker, for a location to hang off.</summary>
    private static byte[] WithExif(byte[] jpeg)
    {
        byte[] camera = PhoneImages.Jpeg();
        int pos = 2;
        while (pos + 4 <= camera.Length && !(camera[pos + 1] == 0xE1 && camera.AsSpan(pos + 4, 6).SequenceEqual("Exif\0\0"u8)))
        {
            pos += 2 + ((camera[pos + 2] << 8) | camera[pos + 3]);
        }

        int length = 2 + ((camera[pos + 2] << 8) | camera[pos + 3]);
        return [.. jpeg.AsSpan(0, 2), .. camera.AsSpan(pos, length), .. jpeg.AsSpan(2)];
    }
}
