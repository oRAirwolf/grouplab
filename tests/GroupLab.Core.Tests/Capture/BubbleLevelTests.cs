using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>NOTES-FROM-PLANNING.md entry 281 section 1.1: the dot moves like a bubble, toward the high side, and sits at the center when flat.</summary>
public class BubbleLevelTests
{
    private const double G = 9.81;

    private static (double X, double Y, double Z) Tilted(double rightRaisedDegrees, double topRaisedDegrees)
    {
        double a = rightRaisedDegrees * Math.PI / 180, b = topRaisedDegrees * Math.PI / 180;
        return (G * Math.Sin(a), G * Math.Sin(b), G * Math.Cos(a) * Math.Cos(b));
    }

    [Fact]
    public void FlatIsTheCenterAndReady()
    {
        Assert.Equal((0.0, 0.0), BubbleLevel.Dot(0, 0, G));
        Assert.True(BubbleLevel.Ready(0, 0, G));
    }

    [Fact]
    public void TheDotDriftsTowardTheHighSide()
    {
        var (x, y, z) = Tilted(5, 0);
        var (right, down) = BubbleLevel.Dot(x, y, z);
        Assert.True(right > 0.3 && Math.Abs(down) < 1e-9, "right edge raised: the dot goes right");
        (x, y, z) = Tilted(0, 5);
        (right, down) = BubbleLevel.Dot(x, y, z);
        Assert.True(down < -0.3 && Math.Abs(right) < 1e-9, "top edge raised: the dot goes up the screen");
        Assert.False(BubbleLevel.Ready(x, y, z));
    }

    [Fact]
    public void ItReachesTheArmsEndAndStaysThere()
    {
        var (x, y, z) = Tilted(40, 0);
        Assert.Equal(1, BubbleLevel.Dot(x, y, z).Right, 9);
        Assert.Equal(40, BubbleLevel.Tilt(x, y, z), 6);
    }
}
