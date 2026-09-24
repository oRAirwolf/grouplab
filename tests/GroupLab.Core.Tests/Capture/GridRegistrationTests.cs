using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 158 and 172 section 3 item 4: a printed grid found, counted and fitted, on a grid drawn through a known
/// lens with a thick printed bar across it, as the ST-4's diamonds and bars cross its grid.
/// </summary>
public class GridRegistrationTests
{
    private static bool[] Grid(int w, int h, double pitch, double k1, double degrees)
    {
        var mask = new bool[w * h];
        double cx = (w - 1) / 2.0, cy = (h - 1) / 2.0, s = Math.Max(w, h) / 2.0, a = degrees * Math.PI / 180;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // The pixel undistorted, then into the grid's own units.
                double dx = (x - cx) / s, dy = (y - cy) / s, ux = dx, uy = dy;
                for (int i = 0; i < 8; i++)
                {
                    double f = 1 + (k1 * ((ux * ux) + (uy * uy)));
                    ux = dx / f;
                    uy = dy / f;
                }

                double px = ux * s, py = uy * s;
                double lx = (((px * Math.Cos(a)) + (py * Math.Sin(a))) / pitch) + 0.3, ly = (((-px * Math.Sin(a)) + (py * Math.Cos(a))) / pitch) + 0.2;
                mask[(y * w) + x] = Math.Abs(lx - Math.Round(lx)) * pitch < 1.5 || Math.Abs(ly - Math.Round(ly)) * pitch < 1.5;
            }
        }

        // A thick printed bar, which must give no crossings of its own.
        for (int y = 100; y < 400; y++)
        {
            for (int x = 100; x < 160; x++)
            {
                mask[(y * w) + x] = true;
            }
        }

        return mask;
    }

    [Fact]
    public void AGridIsFoundCountedAndFittedThroughItsLens()
    {
        const int w = 800, h = 600;
        var thin = GridRegistration.Thin(Grid(w, h, 50, -0.02, 3), w, h, 6);
        var crossings = GridRegistration.Crossings(thin, w, h, 12);
        var (lattice, spacing) = GridRegistration.Index(crossings, w, h);
        Assert.InRange(spacing, 45, 55);
        Assert.True(lattice.Count > 120, $"{lattice.Count} crossings counted of {crossings.Count} found");

        // Neighbours in the lattice are a spacing apart in the image: nothing was counted twice or skipped.
        var at = lattice.ToDictionary(c => (c.Column, c.Row), c => c.Image);
        foreach (var c in lattice)
        {
            if (at.TryGetValue((c.Column + 1, c.Row), out var right))
            {
                Assert.InRange(Math.Sqrt(Math.Pow(right.X - c.Image.X, 2) + Math.Pow(right.Y - c.Image.Y, 2)), 0.8 * spacing, 1.2 * spacing);
            }
        }

        var fit = GridRegistration.Fit(lattice, spacing, w, h)!;
        Assert.True(fit.LensRms < fit.HomographyRms / 2, $"lens {fit.LensRms:0.0000} against homography {fit.HomographyRms:0.0000}");
        Assert.True(fit.LensRms < 0.02, $"lens residual {fit.LensRms:0.0000} of a grid square");
    }

    [Fact]
    public void TooFewCrossingsAreNoGrid()
    {
        Assert.Empty(GridRegistration.Index([new(10, 10), new(60, 10), new(10, 60)], 100, 100).Crossings);
        Assert.Null(GridRegistration.Fit([], 50, 100, 100));
    }
}
