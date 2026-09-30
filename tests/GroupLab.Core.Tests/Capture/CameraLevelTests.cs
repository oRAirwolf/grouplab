using GroupLab.Core.Capture;
using GroupLab.Core.Evaluation;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 321: the level works flat over a table and upright at a backer, chooses between them by itself without
/// flickering, and follows the sheet's own angle once its markers are read.
/// </summary>
public class CameraLevelTests
{
    private const double G = 9.81;

    private static double Rad(double degrees) => degrees * Math.PI / 180;

    /// <summary>Gravity's reading, away from the ground in the screen's axes, for a phone upright in portrait, pitched and rolled.</summary>
    private static (double X, double Y, double Z) Portrait(double cameraDownDegrees, double rightRaisedDegrees = 0) =>
        (G * Math.Cos(Rad(cameraDownDegrees)) * Math.Sin(Rad(rightRaisedDegrees)), G * Math.Cos(Rad(cameraDownDegrees)) * Math.Cos(Rad(rightRaisedDegrees)),
            G * Math.Sin(Rad(cameraDownDegrees)));

    /// <summary>A phone looking down at <paramref name="steepDegrees"/> below the horizon, pitched about its left-right axis.</summary>
    private static (double X, double Y, double Z) Steep(double steepDegrees) => (0, G * Math.Cos(Rad(steepDegrees)), G * Math.Sin(Rad(steepDegrees)));

    [Fact]
    public void TheModeIsChosenByHowSteeplyTheCameraLooksDown()
    {
        Assert.Equal(LevelMode.LookingDown, BubbleLevel.ModeOf(0, 0, G, null));
        Assert.Equal(LevelMode.Upright, BubbleLevel.ModeOf(0, G, 0, null));
        Assert.Equal(LevelMode.Upright, BubbleLevel.ModeOf(G, 0, 0, null));
        Assert.Equal(LevelMode.Upright, BubbleLevel.ModeOf(-G, 0, 0, null));
        Assert.Equal(LevelMode.Upright, BubbleLevel.ModeOf(0, -G, 0, null));
        var (x, y, z) = Steep(46);
        Assert.Equal(LevelMode.LookingDown, BubbleLevel.ModeOf(x, y, z, null));
        (x, y, z) = Steep(44);
        Assert.Equal(LevelMode.Upright, BubbleLevel.ModeOf(x, y, z, null));
    }

    [Fact]
    public void AroundTheThresholdTheModeNeverFlickers()
    {
        // A hand wavering 8 degrees either side of the middle keeps whichever mode it came from; only going past the margin changes it.
        var random = new Random(321);
        foreach (var start in new[] { LevelMode.LookingDown, LevelMode.Upright })
        {
            LevelMode mode = start;
            for (int i = 0; i < 500; i++)
            {
                var (x, y, z) = Steep(BubbleLevel.ModeDegrees + ((random.NextDouble() * 16) - 8));
                mode = BubbleLevel.ModeOf(x, y, z, mode);
                Assert.Equal(start, mode);
            }
        }

        // Swept down from upright and back up, it changes once each way, past the margin.
        var changes = new List<(double Steep, LevelMode Mode)>();
        LevelMode? now = null;
        foreach (double steep in Enumerable.Range(0, 91).Select(d => (double)d).Concat(Enumerable.Range(0, 91).Select(d => 90.0 - d)))
        {
            var (x, y, z) = Steep(steep);
            var next = BubbleLevel.ModeOf(x, y, z, now);
            if (now is not null && next != now)
            {
                changes.Add((steep, next));
            }

            now = next;
        }

        Assert.Equal([(56.0, LevelMode.LookingDown), (34.0, LevelMode.Upright)], changes);
    }

    [Fact]
    public void UprightIsLevelInPortraitAndLandscapeAndOnEitherEnd()
    {
        foreach (var (x, y, z) in new[] { (0.0, G, 0.0), (0.0, -G, 0.0), (G, 0.0, 0.0), (-G, 0.0, 0.0) })
        {
            var level = new CameraLevel().Felt(x, y, z, 0);
            Assert.Equal(LevelMode.Upright, level.Mode);
            Assert.True(level.Ready, $"{x},{y},{z}");
            Assert.Equal((0.0, 0.0), level.Dot);
            Assert.Equal("Upright", level.Word);
        }

        var flat = new CameraLevel().Felt(0, 0, G, 0);
        Assert.Equal((LevelMode.LookingDown, "Looking down", true), (flat.Mode!.Value, flat.Word, flat.Ready));
    }

    [Fact]
    public void UprightTheBubbleFollowsThePitchAndTheRoll()
    {
        // The camera pointing below the horizon moves the bubble down the screen, a raised right edge moves it right, by the tilt.
        var (x, y, z) = Portrait(cameraDownDegrees: 5);
        var (right, down) = BubbleLevel.Lean(x, y, z, LevelMode.Upright);
        Assert.Equal(0, right, 9);
        Assert.Equal(5, down, 6);
        (x, y, z) = Portrait(cameraDownDegrees: 0, rightRaisedDegrees: 4);
        (right, down) = BubbleLevel.Lean(x, y, z, LevelMode.Upright);
        Assert.Equal(4, right, 6);
        Assert.Equal(0, down, 9);
        (x, y, z) = Portrait(cameraDownDegrees: 2, rightRaisedDegrees: 2);
        Assert.True(new CameraLevel().Felt(x, y, z, 0).Ready, "within 3 degrees: 2.8");
        (x, y, z) = Portrait(cameraDownDegrees: -4);
        Assert.False(new CameraLevel().Felt(x, y, z, 0).Ready, "pointing up 4 degrees");

        // In landscape with the right edge up, the camera pointing down moves the bubble toward the screen's left, which is down as the
        // phone stands; with the left edge up, toward the screen's right.
        double c = G * Math.Cos(Rad(5)), s = G * Math.Sin(Rad(5));
        (right, down) = BubbleLevel.Lean(c, 0, s, LevelMode.Upright);
        Assert.Equal((-5.0, 0.0), (Math.Round(right, 6), Math.Round(down, 6)));
        (right, down) = BubbleLevel.Lean(-c, 0, s, LevelMode.Upright);
        Assert.Equal((5.0, 0.0), (Math.Round(right, 6), Math.Round(down, 6)));
    }

    [Fact]
    public void TheSheetsOwnAngleDecidesOnceItIsRead()
    {
        // A backer leaning back 10 degrees: gravity says the upright phone square to it points 10 degrees down, and is not level.
        var level = new CameraLevel();
        var (x, y, z) = Portrait(cameraDownDegrees: 10);
        var felt = level.Felt(x, y, z, 1000);
        Assert.Equal((LevelSource.Gravity, false), (felt.Source, felt.Ready));

        // The frame read the sheet square on: the sheet decides, and the level is green.
        var seen = level.Seen((0.4, -0.3), (x, y, z), 1200)!;
        Assert.Equal((LevelSource.Sheet, LevelMode.Upright, true), (seen.Source, seen.Mode!.Value, seen.Ready));
        Assert.Equal(0.5, seen.Tilt, 6);

        // Between frames the hand's movement is added: two degrees more down, and the bubble follows at once.
        (x, y, z) = Portrait(cameraDownDegrees: 12);
        var moved = level.Felt(x, y, z, 1500);
        Assert.Equal(LevelSource.Sheet, moved.Source);
        Assert.Equal(1.7, moved.Down, 6);

        // A frame that did not read the sheet leaves it deciding until it is stale; then gravity decides again.
        Assert.Equal(LevelSource.Sheet, level.Seen(null, (x, y, z), 2000)!.Source);
        var stale = level.Now(1200 + CameraLevel.SheetFreshMs + 1)!;
        Assert.Equal((LevelSource.Gravity, false), (stale.Source, stale.Ready));
        Assert.Equal(12, stale.Tilt, 6);
    }

    [Fact]
    public void ChangingModeForgetsWhatTheSheetSaid()
    {
        var level = new CameraLevel();
        var (x, y, z) = Portrait(cameraDownDegrees: 10);
        level.Felt(x, y, z, 0);
        level.Seen((0, 0), (x, y, z), 100);
        var flat = level.Felt(0.5, 0, G, 200);
        Assert.Equal((LevelMode.LookingDown, LevelSource.Gravity), (flat.Mode!.Value, flat.Source));
    }

    [Fact]
    public void WithNoGravityTheSheetIsShownAsItIs()
    {
        var level = new CameraLevel();
        Assert.Null(level.Now(0));
        var seen = level.Seen((6, 8), null, 0)!;
        Assert.Equal((null, LevelSource.Sheet, 10.0, ""), (seen.Mode, seen.Source, seen.Tilt, seen.Word));
        Assert.Null(level.Now(CameraLevel.SheetFreshMs + 1));
    }

    /// <summary>A pinhole's view of a flat page turned about its upright axis by <paramref name="aboutUpright"/> and its across axis by <paramref name="aboutAcross"/>.</summary>
    private static Homography Pose(double aboutUpright, double aboutAcross, double focal, int width, int height)
    {
        double ca = Math.Cos(Rad(aboutUpright)), sa = Math.Sin(Rad(aboutUpright)), cb = Math.Cos(Rad(aboutAcross)), sb = Math.Sin(Rad(aboutAcross));
        // R = Ry(a) Rx(b): the page's right axis and its down axis as the camera sees them.
        (double X, double Y, double Z) r1 = (ca, 0, sa);
        (double X, double Y, double Z) r2 = (sa * sb, cb, -ca * sb);
        double d = 30, cx = (width - 1) / 2.0, cy = (height - 1) / 2.0;
        return new Homography([
            (focal * r1.X) + (cx * r1.Z), (focal * r2.X) + (cx * r2.Z), cx * d,
            (focal * r1.Y) + (cy * r1.Z), (focal * r2.Y) + (cy * r2.Z), cy * d,
            r1.Z, r2.Z, d]);
    }

    [Fact]
    public void TheSheetLeansTowardItsFarSide()
    {
        const int W = 1920, H = 1440;
        double f = CameraGeometry.AssumedFocal(W, H);
        var right = CameraGeometry.Measure(Pose(12, 0, f, W, H), W, H, f, FocalSource.Camera);
        Assert.Equal((12.0, 12.0, 0.0), (Math.Round(right.Degrees, 6), Math.Round(right.Right, 6), Math.Round(right.Down, 6)));
        var down = CameraGeometry.Measure(Pose(0, -8, f, W, H), W, H, f, FocalSource.Camera);
        Assert.Equal(8, down.Down, 6);
        Assert.Equal(0, down.Right, 6);

        // The scoreboard's angled sheet, its right-hand column far, leans right by its angle, measured as the pipeline measures it.
        var view = Scoreboard.Angle(20).View!(2550, 3300);
        var seen = CameraGeometry.Measure(view.Transform, view.Width, view.Height, CameraGeometry.AssumedFocal(2550, 3300), FocalSource.Assumed);
        Assert.InRange(seen.Right, 19.95, 20.05);
        Assert.InRange(seen.Down, -0.05, 0.05);
    }

    [Fact]
    public void AFramesLeanIsTurnedIntoTheScreensAxes()
    {
        Assert.Equal((0.0, 1.0), BubbleLevel.Turned((1, 0), 90));
        Assert.Equal((-1.0, 0.0), BubbleLevel.Turned((0, 1), 90));
        Assert.Equal((-1.0, -2.0), BubbleLevel.Turned((1, 2), 180));
        Assert.Equal((2.0, -1.0), BubbleLevel.Turned((1, 2), 270));
        Assert.Equal((1.0, 2.0), BubbleLevel.Turned((1, 2), 360));
    }

    [Fact]
    public void AndroidsSensorIsTurnedAsIosIsForTheSamePose()
    {
        // The phone turned counterclockwise into landscape: Android's display is at 90 degrees, iOS's screen is landscape right, and Core
        // Motion reads toward the ground where Android reads away from it.
        var (ax, ay, az) = (3.0, -2.0, 8.0);
        Assert.Equal(PhoneCamera.LevelFromGravity(-ax, -ay, -az, ScreenTurn.LandscapeRight), PhoneCamera.LevelFromAndroid(ax, ay, az, 90));
        Assert.Equal(PhoneCamera.LevelFromGravity(-ax, -ay, -az, ScreenTurn.LandscapeLeft), PhoneCamera.LevelFromAndroid(ax, ay, az, 270));
        Assert.Equal(PhoneCamera.LevelFromGravity(-ax, -ay, -az, ScreenTurn.UpsideDown), PhoneCamera.LevelFromAndroid(ax, ay, az, 180));
        Assert.Equal(PhoneCamera.LevelFromGravity(-ax, -ay, -az, ScreenTurn.Upright), PhoneCamera.LevelFromAndroid(ax, ay, az, 0));
    }
}
