using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the iPhone and iPad camera's choices that do not need a camera, the mode the analysis
/// and the picture come from, the scale the guidance judges resolution at, and the level read from iOS's gravity with the screen turned.
/// </summary>
public class PhoneCameraTests
{
    [Fact]
    public void OnlyFourByThreeCounts()
    {
        Assert.True(PhoneCamera.IsFourByThree(1920, 1440));
        Assert.True(PhoneCamera.IsFourByThree(3024, 4032));
        Assert.False(PhoneCamera.IsFourByThree(1920, 1080));
        Assert.False(PhoneCamera.IsFourByThree(0, 0));
        Assert.Equal(-1, PhoneCamera.ChooseMode([new CameraMode(1920, 1080, 1920, 1080, true), new CameraMode(3840, 2160, 3840, 2160, true)]));
    }

    [Fact]
    public void TheAnalysisSizeIsTakenWhereItIsOffered()
    {
        CameraMode[] modes =
        [
            new(640, 480, 640, 480, true),
            new(1920, 1080, 4032, 3024, true),
            new(4032, 3024, 4032, 3024, true),
            new(1920, 1440, 4032, 3024, false),
            new(1920, 1440, 4032, 3024, true),
            new(1920, 1440, 1920, 1440, true),
        ];
        Assert.Equal(4, PhoneCamera.ChooseMode(modes));
    }

    [Fact]
    public void TheNearestLargerComesBeforeTheNearestSmaller()
    {
        CameraMode[] modes =
        [
            new(1440, 1080, 4032, 3024, true),
            new(4032, 3024, 4032, 3024, true),
            new(2592, 1944, 2592, 1944, true),
        ];
        Assert.Equal(2, PhoneCamera.ChooseMode(modes));

        // Where every mode is smaller, the largest of them.
        Assert.Equal(1, PhoneCamera.ChooseMode([new CameraMode(640, 480, 640, 480, true), new CameraMode(1440, 1080, 3264, 2448, true)]));
    }

    [Fact]
    public void AmongOneSizeTheLargerPictureWins()
    {
        CameraMode[] modes =
        [
            new(1920, 1440, 3264, 2448, true),
            new(1920, 1440, 8064, 6048, false),
            new(1920, 1440, 4032, 3024, true),
        ];
        Assert.Equal(1, PhoneCamera.ChooseMode(modes));
    }

    [Fact]
    public void ResolutionIsJudgedAtTheWorkingCopysSize()
    {
        // A 12 megapixel picture is measured at 8 megapixels: its long side 4032 becomes about 3292, over the frame's 1920.
        double scale = PhoneCamera.MeasuredScale(4032, 3024, 1920, 1440);
        Assert.Equal(4032 * Math.Sqrt(8e6 / (4032.0 * 3024)) / 1920, scale, 9);
        Assert.InRange(scale, 1.70, 1.72);

        // A picture no larger than the working copy is measured as it is; an unknown size changes nothing.
        Assert.Equal(2, PhoneCamera.MeasuredScale(2000, 1500, 1000, 750), 9);
        Assert.Equal(1, PhoneCamera.MeasuredScale(0, 0, 1920, 1440));
    }

    [Fact]
    public void FlatFaceUpIsLevelWhicheverWayTheScreenIsTurned()
    {
        foreach (var turn in Enum.GetValues<ScreenTurn>())
        {
            var (x, y, z) = PhoneCamera.LevelFromGravity(0, 0, -1, turn);
            Assert.True(BubbleLevel.Ready(x, y, z), turn.ToString());
            Assert.Equal((0.0, 0.0), BubbleLevel.Dot(x, y, z));
        }
    }

    /// <summary>
    /// Core Motion's gravity is the pull toward the ground in the device's axes. Raising the device's right edge by a few degrees tips the
    /// pull toward its left, a negative x; the bubble goes to the raised side as the screen shows it.
    /// </summary>
    [Fact]
    public void TheBubbleGoesToTheRaisedSideOfTheScreen()
    {
        double s = Math.Sin(6 * Math.PI / 180), c = -Math.Cos(6 * Math.PI / 180);

        // The device's right edge raised.
        var upright = PhoneCamera.LevelFromGravity(-s, 0, c, ScreenTurn.Upright);
        var (right, down) = BubbleLevel.Dot(upright.X, upright.Y, upright.Z);
        Assert.True(right > 0.3 && Math.Abs(down) < 1e-9, "upright: the device's right is the screen's right");

        // Turned clockwise, landscape left: the device's right edge is the bottom of the screen, so the bubble goes down.
        var left = PhoneCamera.LevelFromGravity(-s, 0, c, ScreenTurn.LandscapeLeft);
        (right, down) = BubbleLevel.Dot(left.X, left.Y, left.Z);
        Assert.True(down > 0.3 && Math.Abs(right) < 1e-9, "landscape left: the device's right is the screen's bottom");

        // Turned counterclockwise, landscape right: the device's right edge is the top of the screen.
        var rightTurn = PhoneCamera.LevelFromGravity(-s, 0, c, ScreenTurn.LandscapeRight);
        (right, down) = BubbleLevel.Dot(rightTurn.X, rightTurn.Y, rightTurn.Z);
        Assert.True(down < -0.3 && Math.Abs(right) < 1e-9, "landscape right: the device's right is the screen's top");

        // Upside down: the device's right edge is the screen's left.
        var over = PhoneCamera.LevelFromGravity(-s, 0, c, ScreenTurn.UpsideDown);
        (right, down) = BubbleLevel.Dot(over.X, over.Y, over.Z);
        Assert.True(right < -0.3 && Math.Abs(down) < 1e-9, "upside down: the device's right is the screen's left");

        // The device's top edge raised, upright: the bubble goes up the screen.
        var top = PhoneCamera.LevelFromGravity(0, -s, c, ScreenTurn.Upright);
        (right, down) = BubbleLevel.Dot(top.X, top.Y, top.Z);
        Assert.True(down < -0.3 && Math.Abs(right) < 1e-9, "upright: the device's top is the screen's top");
    }

    [Fact]
    public void ThePictureStandsTheWayTheScreenDoes()
    {
        Assert.Equal(90, PhoneCamera.RotationDegrees(ScreenTurn.Upright));
        Assert.Equal(0, PhoneCamera.RotationDegrees(ScreenTurn.LandscapeRight));
        Assert.Equal(180, PhoneCamera.RotationDegrees(ScreenTurn.LandscapeLeft));
        Assert.Equal(270, PhoneCamera.RotationDegrees(ScreenTurn.UpsideDown));
    }
}
