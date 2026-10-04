using System.Globalization;
using GroupLab.Cli;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Updates;
using GroupLab.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 344, the engine: a photograph of a store-bought target straightened by each of three scale sources with an
/// honest uncertainty, made into a small reference file that never holds the photograph, checked and added to the library, and the library
/// as a signed file the application can fetch, read only when its signature checks.
/// </summary>
[Collection("StoreTargetLibrary")]
public class TargetReferenceTests
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>A camera 50 in from a wall, turned <paramref name="degrees"/> about its vertical axis: wall inches to a 4000 by 3000 picture.</summary>
    private static Homography Camera(double degrees, double cx = 9, double cy = 6)
    {
        double a = degrees * Math.PI / 180, f = 2900, d = 50;
        double[] r = [Math.Cos(a), 0, Math.Sin(a), 0, 1, 0, -Math.Sin(a), 0, Math.Cos(a)];
        double[] t = [-(r[0] * cx) - (r[1] * cy), -(r[3] * cx) - (r[4] * cy), d - (r[6] * cx) - (r[7] * cy)];
        double[] k = [f, 0, 2000, 0, f, 1500, 0, 0, 1];
        double[] m = [r[0], r[1], t[0], r[3], r[4], t[1], r[6], r[7], t[2]];
        var h = new double[9];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                h[(i * 3) + j] = (k[i * 3] * m[j]) + (k[(i * 3) + 1] * m[3 + j]) + (k[(i * 3) + 2] * m[6 + j]);
            }
        }

        return new Homography(h);
    }

    private static PointD[] Corners(Homography toImage, double w, double h) => [toImage.Apply(new(0, 0)), toImage.Apply(new(w, 0)), toImage.Apply(new(w, h)), toImage.Apply(new(0, h))];

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    [Fact]
    public void ThePrintedSizeStraightensTheTargetAndSaysHowGood()
    {
        var camera = Camera(25);
        var target = TargetStraightening.FromPrintedSize(Corners(camera, 18, 12), 18, 12, 1);
        Assert.Equal(ScaleSource.PrintedSize, target.Source);
        var p = target.Plane.ToInches(camera.Apply(new(13.5, 7.25)));
        Assert.Equal(13.5, p.X, 6);
        Assert.Equal(7.25, p.Y, 6);
        Assert.InRange(target.Uncertainty, 0.001, 0.02);
        Assert.StartsWith("Scale from the printed size typed, 18 by 12 in", target.Says, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoTappedPointsSetTheSizeAndTheCornersTakeOutTheAngle()
    {
        var camera = Camera(20);
        var a = camera.Apply(new(1, 1));
        var b = camera.Apply(new(17, 11));
        double apart = Math.Sqrt((16 * 16) + (10 * 10));
        var target = TargetStraightening.FromTwoPoints(Corners(camera, 18, 12), a, b, apart, 4000, 3000, null);
        Assert.Equal(ScaleSource.TwoPoints, target.Source);

        // No focal length in the file, so it is assumed; what that leaves is inside what the source claims.
        double error = Math.Max(Math.Abs((target.WidthInches / 18) - 1), Math.Abs((target.HeightInches / 12) - 1));
        Assert.True(error <= target.Uncertainty, $"error {error:P2} against a claimed {target.Uncertainty:P2}");
        Assert.Contains("two points", target.Says, StringComparison.Ordinal);
    }

    [Fact]
    public void AGroupLabSheetOnTheSameWallScalesTheTargetAndSaysWhatAnUncheckedPrinterLeaves()
    {
        var camera = Camera(15);

        // The sheet's registration: picture pixels to page decimillimetres on the same wall.
        var toPage = Homography.Compose(camera.Inverse(), new Homography([254, 0, 0, 0, 254, 0, 0, 0, 1]));
        var sheet = new SheetPlane(new HomographyMapping(toPage));
        var unchecked_ = TargetStraightening.FromGroupLabSheet(sheet, Corners(camera, 18, 12), 0.002, 13.9, printerMeasured: false);
        Assert.Equal(18, unchecked_.WidthInches, 6);
        Assert.Equal(12, unchecked_.HeightInches, 6);
        Assert.Contains(TargetStraightening.UnmeasuredPrinter, unchecked_.Says, StringComparison.Ordinal);
        Assert.True(unchecked_.Uncertainty >= 0.03);

        var checkedPrinter = TargetStraightening.FromGroupLabSheet(sheet, Corners(camera, 18, 12), 0.002, 13.9, printerMeasured: true);
        Assert.DoesNotContain(TargetStraightening.UnmeasuredPrinter, checkedPrinter.Says, StringComparison.Ordinal);
        Assert.True(checkedPrinter.Uncertainty < unchecked_.Uncertainty);
    }

    [Fact]
    public void AReferenceFileHoldsTheFingerprintAndWordsAndReadsBack()
    {
        var target = new StoreTarget("test-poster", "Maker", "Poster {size}", "12 x 18 in", "123", null);
        var fp = StoreTargetLibrary.Find("bc-34805-shoot-n-c-8in-bull")!.Fingerprint;
        var sameProduct = new TargetFingerprint("test-poster", fp.Points, fp.Descriptors, fp.DescriptorBytes, fp.Bulls, fp.Layout);
        var reference = new TargetReference(target, sameProduct, ScaleSource.PrintedSize, 0.006, "Scale from the printed size typed.");
        string json = reference.Write();
        Assert.True(json.Length < 100_000, $"{json.Length} characters");
        var read = TargetReference.Read(json);
        Assert.Equal(target, read.Target);
        Assert.Equal(ScaleSource.PrintedSize, read.Source);
        Assert.Equal(0.006, read.Uncertainty, 6);
        Assert.Equal(sameProduct.Points, read.Fingerprint.Points);

        // A fingerprint of another product inside is refused.
        var wrong = reference with { Fingerprint = fp };
        Assert.Throws<InvalidDataException>(() => TargetReference.Read(wrong.Write()));
        Assert.Throws<InvalidDataException>(() => TargetReference.Read("{\"format\": \"something else\"}"));
    }

    [Fact]
    public void TheLibraryFileIsReadOnlyWhenItsSignatureChecks()
    {
        var (privateKey, publicKey) = UpdateSignature.NewKeyPair();
        var references = StoreTargetLibrary.Shipped.Select(t => new TargetReference(t, t.Fingerprint, ScaleSource.Scan, 0, "")).ToList();
        byte[] payload = StoreLibraryFile.Payload(7, references);
        string signed = StoreLibraryFile.Sign(payload, Convert.FromBase64String(privateKey));

        var (refusal, contents) = StoreLibraryFile.Read(signed, publicKey);
        Assert.Equal(StoreLibraryFile.Refusal.None, refusal);
        Assert.Equal(7, contents!.Version);
        Assert.Equal(StoreTargetLibrary.Shipped.Select(t => t.Id), contents.Targets.Select(t => t.Target.Id));

        // Another key, a changed payload, no key at all, or nothing signed: refused, and nothing read.
        var (_, otherKey) = UpdateSignature.NewKeyPair();
        Assert.Equal(StoreLibraryFile.Refusal.BadSignature, StoreLibraryFile.Read(signed, otherKey).Refusal);
        string tampered = signed.Replace(Convert.ToBase64String(payload), Convert.ToBase64String(StoreLibraryFile.Payload(8, references)), StringComparison.Ordinal);
        Assert.Equal(StoreLibraryFile.Refusal.BadSignature, StoreLibraryFile.Read(tampered, publicKey).Refusal);
        Assert.Equal(StoreLibraryFile.Refusal.NoKey, StoreLibraryFile.Read(signed, "").Refusal);
        Assert.Equal(StoreLibraryFile.Refusal.NotSigned, StoreLibraryFile.Read("{}", publicKey).Refusal);
        Assert.Null(StoreLibraryFile.Read(tampered, publicKey).Contents);

        // Signed, but not a library this build knows.
        string strange = StoreLibraryFile.Sign("{\"format\": \"grouplab-target-library-9\"}"u8.ToArray(), Convert.FromBase64String(privateKey));
        Assert.Equal(StoreLibraryFile.Refusal.UnknownFormat, StoreLibraryFile.Read(strange, publicKey).Refusal);

        // By default it is checked against GroupLab's update key, which this test's key is not.
        Assert.Equal(StoreLibraryFile.Refusal.BadSignature, StoreLibraryFile.Read(signed).Refusal);
    }

    [Fact]
    public void AFetchedLibraryAddsToTheBuiltInOneUntilTakenAway()
    {
        Assert.Equal(9, StoreTargetLibrary.Shipped.Count); // entry 364 added four of Alan's to the five
        var fp = StoreTargetLibrary.Find("bc-34805-shoot-n-c-8in-bull")!.Fingerprint;
        var added = new TargetReference(new StoreTarget("fetched-poster", "Maker", "Poster {size}", "12 x 18 in", "", null),
            new TargetFingerprint("fetched-poster", fp.Points, fp.Descriptors, fp.DescriptorBytes, fp.Bulls, fp.Layout), ScaleSource.PrintedSize, 0.006, "");
        try
        {
            StoreTargetLibrary.Install([added]);
            Assert.Equal(10, StoreTargetLibrary.All.Count);
            Assert.Same(added.Fingerprint, StoreTargetLibrary.Find("fetched-poster")!.Fingerprint);
        }
        finally
        {
            StoreTargetLibrary.Uninstall();
        }

        Assert.Equal(9, StoreTargetLibrary.All.Count);
        Assert.Null(StoreTargetLibrary.Find("fetched-poster"));
    }

    /// <summary>
    /// Steps 1 to 3 from a photograph file, as Alan would run them: a poster GroupLab draws, photographed at an angle on a wall, made into a
    /// reference by its printed size, no family found, and then checked: it recognizes its own photograph and nothing else claims it.
    /// </summary>
    [Fact]
    public void TheCommandMakesAReferenceFromAPhotographAndChecksIt()
    {
        string folder = Temp.Folder("target-reference");
        Directory.CreateDirectory(folder);
        try
        {
            const double dpi = 100, w = 12, h = 18;
            using var poster = new Mat((int)(h * dpi), (int)(w * dpi), MatType.CV_8UC3, new Scalar(225, 235, 240));
            var rng = new Random(344);
            for (int i = 0; i < 60; i++)
            {
                var c = new Point(rng.Next(100, 1100), rng.Next(100, 1700));
                Cv2.Circle(poster, c, rng.Next(20, 120), new Scalar(rng.Next(0, 200), rng.Next(0, 200), rng.Next(0, 200)), rng.Next(3, 15), LineTypes.AntiAlias);
                Cv2.PutText(poster, "RANGE " + i.ToString(Inv), new Point(rng.Next(50, 900), rng.Next(80, 1750)), HersheyFonts.HersheyDuplex, 1.2, Scalar.Black, 2, LineTypes.AntiAlias);
            }

            var camera = Camera(18, w / 2, h / 2);
            double[] toPicture = [camera[0, 0] / dpi, camera[0, 1] / dpi, camera[0, 2], camera[1, 0] / dpi, camera[1, 1] / dpi, camera[1, 2], camera[2, 0] / dpi, camera[2, 1] / dpi, camera[2, 2]];
            using var photo = new Mat(3000, 4000, MatType.CV_8UC3, new Scalar(60, 70, 80));
            using (var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, toPicture))
            {
                Cv2.WarpPerspective(poster, photo, hm, photo.Size(), InterpolationFlags.Linear, BorderTypes.Transparent);
            }

            string file = Path.Combine(folder, "poster.jpg");
            Cv2.ImWrite(file, photo, new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
            string corners = string.Join(",", Corners(camera, w, h).Select(p => string.Create(Inv, $"{p.X:0.0},{p.Y:0.0}")));
            string reference = Path.Combine(folder, "poster.glref");
            var said = new StringWriter();
            int made = TargetReferenceVerb.Run(["make", Temp.Readable(file), "--id", "test-poster", "--maker", "GroupLab", "--name", "Test poster {size}", "--size", "12 x 18 in",
                "--printed", "12x18", "--corners", corners, "--bulls", "6,9", "--out", reference], said, said);
            Assert.True(made == 0, said.ToString());
            Assert.Contains("Not like anything in the library", said.ToString(), StringComparison.Ordinal);
            Assert.True(new FileInfo(reference).Length < 200 * 1024);

            var written = TargetReference.Read(File.ReadAllText(reference));
            Assert.Equal(ScaleSource.PrintedSize, written.Source);
            Assert.Equal([new PointD(6, 9)], written.Fingerprint.Bulls.Select(b => new PointD(Math.Round(b.X, 3), Math.Round(b.Y, 3))));
            Assert.Equal(12, written.Fingerprint.Layout.Width, 0);

            var checkedSaid = new StringWriter();
            Assert.True(TargetReferenceVerb.Run(["check", reference, file], checkedSaid, checkedSaid) == 0, checkedSaid.ToString());
            Assert.Contains("recognized", checkedSaid.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }
}
