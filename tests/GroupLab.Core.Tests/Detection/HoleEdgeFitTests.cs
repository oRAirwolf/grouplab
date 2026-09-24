using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 170 section 4: a hole's centre from its edge, not from its dark area. A scanned hole shows the lid through
/// it, a light grey, with the shadow of the torn edge as a dark crescent on the side away from the lamp. The residual the detector weights
/// its centroid by is far larger in the crescent than in the lid, so the centroid sat toward the shadow on every scan measured.
/// </summary>
public class HoleEdgeFitTests
{
    /// <summary>
    /// A residual image as the detector makes it: zero on paper, 70 over the lid seen through the hole, 200 in a crescent of shadow along its
    /// lower left edge. The true centre is known to the pixel.
    /// </summary>
    private static (GrayImage Residual, PointD Truth, double Radius) Drawn()
    {
        const int size = 300;
        var truth = new PointD(151.3, 148.7);
        const double radius = 60;
        var pixels = new byte[size * size];
        var shadow = new PointD(-Math.Sqrt(0.5), Math.Sqrt(0.5));
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - truth.X, dy = y - truth.Y, d = Math.Sqrt((dx * dx) + (dy * dy));
                if (d > radius)
                {
                    continue;
                }

                // The crescent: within a sixth of the radius of the edge, on the side the shadow falls.
                bool towardShadow = d > 0 && ((dx * shadow.X) + (dy * shadow.Y)) / d > 0.3;
                pixels[(y * size) + x] = (byte)(towardShadow && d > radius * 5 / 6 ? 200 : 70);
            }
        }

        return (new GrayImage(size, size, pixels), truth, radius);
    }

    [Fact]
    public void TheEdgeFindsTheCentreAndTheWeightedCentroidIsPulledTowardTheShadow()
    {
        var (residual, truth, radius) = Drawn();

        double sw = 0, sx = 0, sy = 0;
        for (int y = 0; y < residual.Height; y++)
        {
            for (int x = 0; x < residual.Width; x++)
            {
                double w = residual[x, y];
                sw += w;
                sx += w * x;
                sy += w * y;
            }
        }

        var centroid = new PointD(sx / sw, sy / sw);
        double centroidError = Math.Sqrt(Math.Pow(centroid.X - truth.X, 2) + Math.Pow(centroid.Y - truth.Y, 2));
        Assert.True(centroidError > 3, $"the weighted centroid is {centroidError:0.00} px off, where the shadow should pull it several");
        Assert.True(centroid.X < truth.X && centroid.Y > truth.Y, "and toward the shadow, down and to the left");

        // Started from the biased centroid, as the detector starts it.
        var edge = HoleEdgeFit.Fit(residual, centroid, radius, residual: true)!;
        Assert.NotNull(edge);
        double edgeError = Math.Sqrt(Math.Pow(edge.Centre.X - truth.X, 2) + Math.Pow(edge.Centre.Y - truth.Y, 2));
        Assert.True(edgeError < 0.75, $"the edge fit is {edgeError:0.00} px from the true centre");
        Assert.InRange(edge.RadiusPixels, radius - 1.5, radius + 1.5);
        Assert.True(edge.Rays >= HoleEdgeFit.RayCount * 2 / 3);
    }

    /// <summary>Nothing to fit, no paper around it or no contrast, gives no fit rather than a guess.</summary>
    [Fact]
    public void WithoutAnEdgeThereIsNoFit()
    {
        var blank = new GrayImage(200, 200, new byte[200 * 200]);
        Assert.Null(HoleEdgeFit.Fit(blank, new PointD(100, 100), 40, residual: true));
        Assert.Null(HoleEdgeFit.Fit(blank, new PointD(100, 100), 1, residual: true));
    }
}
