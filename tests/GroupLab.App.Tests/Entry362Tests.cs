using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Tests.Support;
using OpenCvSharp;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 362: Add a store-bought target never found the corners of Alan's five photographs, showed them sideways or
/// upside down, and could not be zoomed on the phone. A photograph is now read the way its orientation tag says it reads, for each of the
/// eight values; Rotate turns it a quarter turn with everything placed on it; a corner let go lands on the clear corner of the picture near
/// it, and Undo puts it back; and on the photographs and scans on Alan's computer the corners are where they were checked by eye to be.
/// </summary>
[Collection("StoreTargetLibrary")]
public class Entry362Tests
{
    private static double Apart(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    /// <summary>
    /// A picture 60 by 40 as it reads: red in its top left, green in its top right, blue in its bottom left, grey elsewhere, written as the
    /// camera would store it for orientation <paramref name="orientation"/>, with that tag in its EXIF.
    /// </summary>
    private static string Tagged(int orientation, string folder)
    {
        using var upright = new Mat(40, 60, MatType.CV_8UC3, new Scalar(128, 128, 128));
        upright[new Rect(0, 0, 20, 14)].SetTo(new Scalar(0, 0, 255));
        upright[new Rect(40, 0, 20, 14)].SetTo(new Scalar(0, 255, 0));
        upright[new Rect(0, 26, 20, 14)].SetTo(new Scalar(255, 0, 0));
        // What the camera stores is the picture turned back: the tag's turn undone, which is the same turn for every value but 6 and 8.
        using var stored = UprightMat.Apply(upright, orientation switch { 6 => 8, 8 => 6, _ => orientation });
        Cv2.ImEncode(".jpg", stored, out byte[] jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, 95));
        var tiff = new List<byte>();
        tiff.AddRange("II"u8.ToArray());
        tiff.AddRange(BitConverter.GetBytes((ushort)42));
        tiff.AddRange(BitConverter.GetBytes(8u));
        tiff.AddRange(BitConverter.GetBytes((ushort)1));
        tiff.AddRange(BitConverter.GetBytes((ushort)0x0112));
        tiff.AddRange(BitConverter.GetBytes((ushort)3));
        tiff.AddRange(BitConverter.GetBytes(1u));
        tiff.AddRange(BitConverter.GetBytes((uint)orientation));
        tiff.AddRange(BitConverter.GetBytes(0u));
        byte[] body = [.. "Exif\0\0"u8.ToArray(), .. tiff];
        int length = body.Length + 2;
        byte[] file = [0xFF, 0xD8, 0xFF, 0xE1, (byte)(length >> 8), (byte)length, .. body, .. jpeg[2..]];
        string path = Path.Combine(folder, $"tagged-{orientation}.jpg");
        File.WriteAllBytes(path, file);
        return path;
    }

    /// <summary>The colour the screen's copy shows at a point, as blue, green and red.</summary>
    private static Vec3b ShownAt(FingerprintSession session, double fx, double fy)
    {
        using var shown = Cv2.ImDecode(session.Shown!, ImreadModes.Color);
        return shown.At<Vec3b>((int)(fy * shown.Height), (int)(fx * shown.Width));
    }

    private static void IsRedTopLeft(FingerprintSession session, string because)
    {
        Assert.True((session.Width, session.Height) == (60, 40), $"{because}: {session.Width} by {session.Height}");
        var red = ShownAt(session, 0.15, 0.15);
        var green = ShownAt(session, 0.85, 0.15);
        var blue = ShownAt(session, 0.15, 0.85);
        Assert.True(red.Item2 > 180 && red.Item0 < 80, $"{because}: top left is not red");
        Assert.True(green.Item1 > 180 && green.Item2 < 80, $"{because}: top right is not green");
        Assert.True(blue.Item0 > 180 && blue.Item2 < 80, $"{because}: bottom left is not blue");
    }

    /// <summary>Entry 362 section 1: each of the eight orientation values, read through the screen's own session, reads upright.</summary>
    [Fact]
    public void APhotographReadsUprightForEveryOrientationValue()
    {
        string folder = Temp.Folder("entry362-tagged");
        Directory.CreateDirectory(folder);
        try
        {
            for (int orientation = 1; orientation <= 8; orientation++)
            {
                using var session = new FingerprintSession(() => []);
                Assert.Null(session.Load(Tagged(orientation, folder)));
                IsRedTopLeft(session, $"orientation {orientation}");
            }
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    /// <summary>The grey and colour turns are the same turn, for every value, and a point turns with its pixel.</summary>
    [Fact]
    public void TheGreyAndColourTurnsAgreeAndPointsTurnWithTheirPixels()
    {
        var random = new Random(362);
        var pixels = new byte[7 * 5];
        random.NextBytes(pixels);
        var grey = new GrayImage(7, 5, pixels);
        using var mat = new Mat(5, 7, MatType.CV_8UC1);
        mat.SetArray(pixels);
        for (int orientation = 1; orientation <= 8; orientation++)
        {
            var turned = Upright.Apply(grey, orientation);
            using var turnedMat = UprightMat.Apply(mat, orientation);
            turnedMat.GetArray(out byte[] fromMat);
            Assert.Equal((turnedMat.Width, turnedMat.Height), (turned.Width, turned.Height));
            Assert.Equal(fromMat, turned.Pixels);
        }

        foreach (bool clockwise in new[] { true, false })
        {
            var turned = Upright.Turn(grey, clockwise);
            var p = new PointD(2, 1);
            var q = Upright.Turn(p, clockwise, grey.Width, grey.Height);
            Assert.Equal(grey[2, 1], turned[(int)q.X, (int)q.Y]);
        }
    }

    /// <summary>Entry 362 section 5: Rotate turns the picture a quarter turn, and back.</summary>
    [Fact]
    public void RotateTurnsThePictureAQuarterTurnAndBack()
    {
        string folder = Temp.Folder("entry362-rotate");
        Directory.CreateDirectory(folder);
        try
        {
            using var session = new FingerprintSession(() => []);
            Assert.Null(session.Load(Tagged(1, folder)));
            session.Rotate(clockwise: true);
            Assert.Equal((40, 60), (session.Width, session.Height));
            // Turned clockwise, what was the bottom left (blue) is now the top left.
            var topLeft = ShownAt(session, 0.15, 0.1);
            Assert.True(topLeft.Item0 > 180 && topLeft.Item2 < 80, "after a clockwise turn the top left is not blue");
            session.Rotate(clockwise: false);
            IsRedTopLeft(session, "turned back");
            Assert.Equal(4, session.Corners.Length);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    /// <summary>Entry 362 section 3: a corner let go near a sharp corner lands on it, Undo puts it back, and on a plain picture it stays.</summary>
    [Fact]
    public void ACornerLetGoSnapsToTheClearCornerNearItAndUndoPutsItBack()
    {
        string folder = Temp.Folder("entry362-snap");
        Directory.CreateDirectory(folder);
        try
        {
            using (var picture = new Mat(600, 800, MatType.CV_8UC3, new Scalar(90, 90, 90)))
            {
                picture[new Rect(200, 150, 400, 300)].SetTo(new Scalar(235, 235, 235));
                Cv2.ImWrite(Path.Combine(folder, "sheet.png"), picture);
            }

            using var session = new FingerprintSession(() => []);
            Assert.Null(session.Load(Path.Combine(folder, "sheet.png")));
            var placed = new PointD(205, 156);
            session.MoveCorner(0, placed);
            Assert.True(session.SnapCorner(0), "the corner did not snap");
            Assert.True(Apart(session.Corners[0], new PointD(199.5, 149.5)) < 1.5, $"snapped to {session.Corners[0]}");
            session.UndoSnap();
            Assert.Equal(placed, session.Corners[0]);

            session.MoveCorner(0, new PointD(400, 300));
            Assert.False(session.SnapCorner(0), "a corner in the middle of plain paper snapped");
            Assert.Equal(new PointD(400, 300), session.Corners[0]);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // Alan's photographs of 2026-10-03 and the 600 dpi scans, where they are on this computer. Never on CI: they are not committed.

    private const string Photos = @"C:\Dev\grouplab-local\commercial-targets\corner-photos-2026-10-03";

    private const string Blanks = @"C:\Dev\grouplab-local\commercial-targets";

    /// <summary>
    /// The corners found on each, upright, in full size pixels, as checked by eye on 2026-10-04 against crops of each corner: within about
    /// ten pixels on the sight-in, splash bull, Eze-Scorer bull, NTC ST-4 and Shoot-N-C 12 in sight-in, about twenty five on one corner of
    /// the sight-in grid. The rigid crosshair's paper edge barely shows on the counter, so it is not called found; the guess its handles start
    /// on is within about twenty pixels. The Shoot-N-C lies beside the counter's own edge, which once made a larger outline (Alan's second
    /// set of photographs, 2026-10-04).
    /// </summary>
    public static TheoryData<string, bool, double[]> Corners => new()
    {
        { Path.Combine(Photos, "allen-ezaim-sight-in-55134A.jpg"), true, [668, 121, 3503, 152, 3528, 2802, 668, 2847] },
        { Path.Combine(Photos, "allen-splash-bull-55124A.jpg"), true, [847, 152, 3506, 185, 3524, 2845, 831, 2874] },
        { Path.Combine(Photos, "birchwood-rigid-crosshair.jpg"), false, [1119, 239, 3628, 250, 3617, 2763, 1108, 2752] },
        { Path.Combine(Photos, "eze-scorer-bull.jpg"), true, [273, 577, 2899, 572, 2910, 3189, 280, 3212] },
        { Path.Combine(Photos, "eze-scorer-sight-in-grid.jpg"), true, [208, 516, 2844, 459, 2845, 3137, 204, 3137] },
        { Path.Combine(Photos, "ntc-st4-100yd-precision-rifle.jpg"), true, [170, 221, 2831, 213, 2864, 3246, 154, 3260] },
        { Path.Combine(Photos, "birchwood-shoot-n-c-12in-sight-in.jpg"), true, [849, 178, 3530, 159, 3522, 2815, 876, 2844] },
        { Path.Combine(Blanks, "bc-34105-shoot-n-c-sight-in", "blank.png"), true, [3, 3, 4956, 3, 4956, 4875, 3, 4890] },
        { Path.Combine(Blanks, "bc-34550-shoot-n-c-6in-bull", "blank.png"), true, [3, 3, 3721, 3, 3727, 3708, 3, 3719] },
        { Path.Combine(Blanks, "bc-34805-shoot-n-c-8in-bull", "blank.png"), true, [3, 3, 4956, 3, 4956, 4947, 3, 4975] },
        { Path.Combine(Blanks, "bc-34806-shoot-n-c-8in-crosshair", "blank.png"), true, [3, 3, 4956, 3, 4956, 4887, 3, 4912] },
    };

    /// <summary>Each photograph's size as it reads upright: all seven carry orientation 3 or 6, and the screen once showed them unturned.</summary>
    [Theory]
    [InlineData("allen-ezaim-sight-in-55134A.jpg", 4000, 3000)]
    [InlineData("allen-splash-bull-55124A.jpg", 4000, 3000)]
    [InlineData("birchwood-rigid-crosshair.jpg", 4000, 3000)]
    [InlineData("eze-scorer-bull.jpg", 3000, 4000)]
    [InlineData("eze-scorer-sight-in-grid.jpg", 3000, 4000)]
    [InlineData("ntc-st4-100yd-precision-rifle.jpg", 3000, 4000)]
    [InlineData("birchwood-shoot-n-c-12in-sight-in.jpg", 4000, 3000)]
    public void AlansPhotographsReadUpright(string file, int width, int height)
    {
        string path = Path.Combine(Photos, file);
        if (!File.Exists(path))
        {
            return;
        }

        var metadata = GroupLab.Core.Imaging.ImageMetadataReader.Read(File.ReadAllBytes(path));
        Assert.True(metadata.Orientation is 3 or 6, $"{file} carries orientation {metadata.Orientation}");
        using var session = new FingerprintSession(() => []);
        Assert.Null(session.Load(path));
        Assert.Equal((width, height), (session.Width, session.Height));
    }

    /// <summary>
    /// The whole five steps on each photograph whose package Alan photographed, with the package's name and printed size
    /// (packaging/ beside the photographs, 2026-10-04): every one is 12 by 12 in, both green Eze-Scorer targets come in one package, and the
    /// file written names the target and its size. Where the bull finder finds none, the bull is added by hand, as on the screen.
    /// </summary>
    [Theory]
    [InlineData("allen-ezaim-sight-in-55134A.jpg", "Allen EZ Aim Sight-In Grid Target 55134A")]
    [InlineData("allen-splash-bull-55124A.jpg", "Allen GWG Dual Splash Adhesive Bullseye Target 55124A")]
    [InlineData("eze-scorer-bull.jpg", "Birchwood Casey Eze-Scorer 12 in bullseye")]
    [InlineData("eze-scorer-sight-in-grid.jpg", "Birchwood Casey Eze-Scorer 12 in sight-in grid")]
    [InlineData("birchwood-shoot-n-c-12in-sight-in.jpg", "Birchwood Casey Shoot-N-C 12 in sight-in")]
    public void EachPackagedTargetGoesThroughTheFiveStepsAtItsPrintedSize(string file, string name)
    {
        string path = Path.Combine(Photos, file);
        if (!File.Exists(path))
        {
            return;
        }

        string folder = Temp.Folder("entry362-package");
        Directory.CreateDirectory(folder);
        try
        {
            using var session = new FingerprintSession(() => []);
            Assert.Null(session.Load(path));
            Assert.True(session.CornersFound, $"{file}: corners not found");
            Assert.Null(session.Next());
            session.Source = GroupLab.Core.StoreTargets.ScaleSource.PrintedSize;
            (session.WidthInches, session.HeightInches) = (12, 12);
            Assert.Null(session.Next());
            Assert.Null(session.Next());
            Assert.Equal(GroupLab.Core.StoreTargets.FingerprintStep.Bulls, session.Step);
            if (session.Bulls.Count == 0)
            {
                // GroupLab's bull finder finds none on the sight-in grids and the splash bull's diamonds and rings (entry 362, 2026-10-04): the
                // person adds them on The bulls, as here, the middle one.
                session.AddBull(new PointD(6, 6));
            }

            Assert.NotEmpty(session.Bulls);
            Assert.Null(session.Next());
            session.Name = name;
            var reference = session.Write(Path.Combine(folder, session.FileName));
            Assert.Equal(name, reference.Target.Name);
            Assert.Equal("12 by 12 in", session.Size);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    [Theory]
    [MemberData(nameof(Corners))]
    public void OnAlansPhotographsAndScansTheCornersAreWhereTheyWereChecked(string path, bool found, double[] corners)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var session = new FingerprintSession(() => []);
        Assert.Null(session.Load(path));
        Assert.Equal(found, session.CornersFound);
        Assert.Equal(found, session.CornersSaid is null);
        for (int i = 0; i < 4; i++)
        {
            var expected = new PointD(corners[2 * i], corners[(2 * i) + 1]);
            Assert.True(Apart(session.Corners[i], expected) < 15, $"{Path.GetFileName(path)} corner {i} is at {session.Corners[i]}, {Apart(session.Corners[i], expected):0} pixels from {expected}");
        }
    }
}
