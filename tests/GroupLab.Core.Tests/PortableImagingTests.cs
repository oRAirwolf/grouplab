using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 101: the warp, the sub-pixel and contour corner refinements and the homography's final fit, moved from native code to
/// managed code so every platform gives the same answer. These check that each still does its job; the gate record workflow checks that
/// each gives the same answer everywhere.
/// </summary>
public class PortableImagingTests
{
    [Fact]
    public void TheSeriesExponentialAgreesWithTheLibraryOneWhereTheRefinementUsesIt()
    {
        for (double x = -1; x <= 0; x += 0.01)
        {
            // Within two units in the last place of a double near 1, which is 4.4e-16.
            Assert.Equal(Math.Exp(x), PortableImaging.Exp(x), 1e-15);
        }
    }

    [Fact]
    public void TheIdentityWarpReturnsTheImageAndATranslationMovesIt()
    {
        var random = new Random(101);
        var pixels = new byte[40 * 30];
        random.NextBytes(pixels);
        var image = new GrayImage(40, 30, pixels);

        Assert.Equal(pixels, PortableImaging.WarpPerspective(image, Homography.Identity, 40, 30).Pixels);

        var moved = PortableImaging.WarpPerspective(image, new Homography([1, 0, 3, 0, 1, 2, 0, 0, 1]), 40, 30);
        Assert.Equal(image[10, 10], moved[13, 12]);
        Assert.Equal(255, moved[0, 0]);
    }

    /// <summary>A dark quadrant meeting at a corner placed off the pixel grid, smoothed across one pixel: the refinement finds it from two pixels away.</summary>
    [Fact]
    public void TheRefinementFindsACornerBetweenPixels()
    {
        const double cx = 20.3, cy = 17.6;
        var pixels = new byte[40 * 40];
        for (int y = 0; y < 40; y++)
        {
            for (int x = 0; x < 40; x++)
            {
                double sx = Math.Clamp(x - cx + 0.5, 0, 1), sy = Math.Clamp(y - cy + 0.5, 0, 1);
                double dark = ((1 - sx) * (1 - sy)) + (sx * sy);
                pixels[(y * 40) + x] = (byte)Math.Round(40 + (180 * (1 - dark)));
            }
        }

        var refined = PortableImaging.CornerSubPix(new GrayImage(40, 40, pixels), [new PointD(22, 16)], 5, 100, 0.001);

        Assert.InRange(refined[0].X, cx - 0.1, cx + 0.1);
        Assert.InRange(refined[0].Y, cy - 0.1, cy + 0.1);
    }

    /// <summary>A contour starting part way along a side, so the points before the first corner join the last side, as OpenCV joins them.</summary>
    [Fact]
    public void TheContourRefinementCrossesTheFittedSides()
    {
        var contour = new List<(int X, int Y)>();
        for (int x = 30; x < 50; x++)
        {
            contour.Add((x, 10));
        }

        for (int y = 10; y < 60; y++)
        {
            contour.Add((50, y));
        }

        for (int x = 50; x > 10; x--)
        {
            contour.Add((x, 60));
        }

        for (int y = 60; y > 10; y--)
        {
            contour.Add((10, y));
        }

        for (int x = 10; x < 30; x++)
        {
            contour.Add((x, 10));
        }

        PointD[] corners = [new(10, 10), new(50, 10), new(50, 60), new(10, 60)];

        var refined = PortableImaging.RefineOnContour(contour, corners);

        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(corners[i].X, refined[i].X, 9);
            Assert.Equal(corners[i].Y, refined[i].Y, 9);
        }

        Assert.Throws<ArgumentException>(() => PortableImaging.RefineOnContour(contour, [new(10, 10), new(50, 10), new(50, 60), new(11, 59)]));
    }

    [Fact]
    public void TheFinalFitRecoversAKnownHomographyAndIgnoresWhatItIsToldTo()
    {
        var truth = new Homography([1.02, 0.03, 12, -0.02, 0.98, -7, 1e-4, -2e-4, 1]);
        var source = new List<PointD>();
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                source.Add(new PointD(40 * i, 50 * j));
            }
        }

        var destination = source.Select(truth.Apply).ToList();
        destination[3] = new PointD(destination[3].X + 30, destination[3].Y);
        var use = source.Select((_, i) => i != 3).ToList();

        var fit = PortableImaging.RefineHomography(source, destination, use)!;

        foreach (var p in source)
        {
            var a = fit.Apply(p);
            var b = truth.Apply(p);
            Assert.True(Math.Abs(a.X - b.X) < 1e-8 && Math.Abs(a.Y - b.Y) < 1e-8, $"{p}: {a} against {b}");
        }
    }
}
