using System.Text;
using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Publication;
using GroupLab.Core.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Publication;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 165 section 7, the package half: the image leaves with its pixels untouched and no location, time or serial
/// in it; a copy too large is made again without loss or not sent; the detected and corrected lists give back the person's final marks
/// exactly; and the consent it carries is limits.json's, in the words the page shows.
/// </summary>
public class TargetSendingTests
{
    // A real phone photograph, which has the EXIF block a location can be written into.
    private static byte[] RealJpeg() => File.ReadAllBytes(Repo.PathTo("scans", "phase0", "main1.jpg"));

    private static void SamePixels(byte[] expected, byte[] actual)
    {
        using var a = Cv2.ImDecode(expected, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        using var b = Cv2.ImDecode(actual, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        Assert.Equal((a.Width, a.Height, a.Channels()), (b.Width, b.Height, b.Channels()));
        Assert.Equal(0, Cv2.Norm(a, b, NormTypes.INF));
    }

    private static byte[] Lossless(byte[] file)
    {
        using var decoded = Cv2.ImDecode(file, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        return decoded.ImEncode(".png");
    }

    /// <summary>Section 7 item 2: the sent image carries no location, time or serial, and its pixels are the original's exactly.</summary>
    [Fact]
    public void TheImageLeavesWithItsPixelsAndWithoutItsPlace()
    {
        byte[] original = PhoneImages.WithLocation(RealJpeg(), (33, 12, 3456), (117, 1, 2345));
        var (image, name, refusal) = TargetPackages.PrepareImage(original, "IMG_20260920_150000.jpg", 30 * 1024 * 1024, Lossless);
        Assert.Null(refusal);
        Assert.Equal("IMG_20260920_150000.jpg", name);
        string text = Encoding.Latin1.GetString(image!);
        Assert.DoesNotContain("GPS", text, StringComparison.Ordinal);
        Assert.DoesNotContain("xmpmeta", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MotionPhoto", text, StringComparison.Ordinal);
        SamePixels(original, image!);

        // A kind of file the scrubber does not rewrite, here a TIFF, is made again as PNG without loss: the same pixels, and nothing else.
        byte[] tiff;
        using (var decoded = Cv2.ImDecode(original, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation))
        {
            tiff = decoded.ImEncode(".tiff");
        }

        var (again, pngName, _) = TargetPackages.PrepareImage(tiff, "target.tiff", 30 * 1024 * 1024, Lossless);
        Assert.Equal("target.png", pngName);
        SamePixels(original, again!);

        // And where even that does not fit, not sent, with the reason: never a lossy copy.
        var (none, _, why) = TargetPackages.PrepareImage(original, "target.jpg", 100, Lossless);
        Assert.Null(none);
        Assert.Contains("a smaller copy would not be the real pixels", why, StringComparison.Ordinal);
    }

    /// <summary>Section 7 item 3: the package's corrected list reproduces the person's final marks exactly, and says what each change was.</summary>
    [Fact]
    public void TheCorrectedListIsThePersonsFinalMarks()
    {
        MarkedShot[] detected =
        [
            new(1, new PointD(100, 100), ShotProvenance.Automatic, Bull: 1, MeasuredDiameterInches: 0.25),
            new(2, new PointD(200, 200), ShotProvenance.Automatic, Bull: 2, MeasuredDiameterInches: 0.26),
            new(3, new PointD(300, 300), ShotProvenance.Automatic, Bull: 3),
            new(4, new PointD(400, 400), ShotProvenance.Automatic, Bull: 4),
        ];
        MarkedShot[] final =
        [
            detected[0],
            detected[1] with { Image = new PointD(203.5, 198.25), Provenance = ShotProvenance.Corrected },
            detected[2] with { Bull = 5, Exclusion = ExclusionReason.CalledFlyer },
            new(5, new PointD(500, 500), ShotProvenance.Manual, Bull: 5),
        ];
        var terms = ReceiverTerms.Current;
        var package = TargetPackages.Build([1, 2, 3], "target.png", detected, final, [], [], [], "log", ConsentLevel.Testing, terms);
        var json = JsonNode.Parse(package.Json)!.AsObject();
        var back = TargetPackages.Final(json["corrected"]!.AsObject());
        Assert.Equal(final.Select(s => (s.Id, s.Image, s.Bull, s.Exclusion?.ToString(), s.NotAShot)), back);

        var changes = json["corrected"]!["marks"]!.AsArray().ToDictionary(m => (int)m!["id"]!, m => (string)m!["change"]!);
        Assert.Equal("kept", changes[1]);
        Assert.Equal("moved", changes[2]);
        Assert.Equal("reassigned, excluded", changes[3]);
        Assert.Equal("added", changes[5]);
        Assert.Equal(4, (int)json["corrected"]!["removed"]![0]!["id"]!);
        Assert.Equal(4, json["detected"]!["marks"]!.AsArray().Count);

        // The manifest names the image by its size and hash, and the consent is limits.json's own words for the level.
        Assert.Equal(3, (int)json["manifest"]!["image"]!["bytes"]!);
        Assert.Equal("testing", (string)json["consent"]!["level"]!);
        Assert.Equal(terms.TestingText, (string)json["consent"]!["text"]!);
        Assert.Equal(TargetPackages.Parts, TargetPackages.Parts.Where(p => json.ContainsKey(p)));
    }

    /// <summary>Section 2 and entry 159's one source rule: the application's consent texts are the ones limits.json holds for the page and both receivers.</summary>
    [Fact]
    public void TheConsentIsTheOneTheReceiversAndThePageUse()
    {
        using var limits = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Repo.PathTo("website", "api", "limits.json")));
        var root = limits.RootElement;
        var terms = ReceiverTerms.Current;
        Assert.Equal(root.GetProperty("consentVersion").GetString(), terms.ConsentVersion);
        Assert.Equal(root.GetProperty("consentTexts").GetProperty("testing").GetString(), terms.TestingText);
        Assert.Equal(root.GetProperty("consentTexts").GetProperty("publishable").GetString(), terms.PublishableText);
        Assert.Equal(root.GetProperty("appOpen").GetBoolean(), terms.AppOpen);
        foreach (string receiver in new[] { "upload.php", "app-submission.php" })
        {
            string php = File.ReadAllText(Repo.PathTo("website", "api", receiver));
            Assert.Contains(terms.TestingText, php, StringComparison.Ordinal);
            Assert.Contains(terms.PublishableText, php, StringComparison.Ordinal);
        }
    }
}
