using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>The imaging types Core owns independently of any backend (DESIGN.md section 7).</summary>
public class HomographyTests
{
    [Fact]
    public void IdentityLeavesPointsUnchanged()
    {
        var p = Homography.Identity.Apply(new PointD(12.5, -3.25));

        Assert.Equal(12.5, p.X);
        Assert.Equal(-3.25, p.Y);
    }

    [Fact]
    public void ProjectiveTermDividesThrough()
    {
        var h = new Homography([2, 0, 1, 0, 2, 0, 0, 0, 2]);

        var p = h.Apply(new PointD(3, 4));

        Assert.Equal(3.5, p.X);
        Assert.Equal(4, p.Y);
    }

    [Fact]
    public void GrayImageRejectsMismatchedBuffer()
    {
        Assert.Throws<ArgumentException>(() => new GrayImage(4, 4, new byte[15]));
    }
}
