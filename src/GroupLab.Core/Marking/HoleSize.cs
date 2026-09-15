using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// A marked shot whose dark region looks too large for the group's calibre, with what was measured, and whether that region is mostly
/// the sheet's printed artwork, known only on a registered GroupLab sheet (NOTES-FROM-PLANNING.md entry 40 section 1).
/// </summary>
public sealed record HoleSizeFlag(int ShotId, double ApparentInches, double LargestExpectedInches, string Problem, bool OnArtwork = false);

/// <summary>
/// What the calibre does for marking, NOTES-FROM-PLANNING.md entry 24 section 5 points 2 and 3: a snap radius sized to the hole, and
/// a flag on a hole that reads too large for the calibre, which is how two overlapping holes marked as one look, the failure
/// docs/PHASE1-RESULTS.md M2.2 found reported silently. Neither gates anything.
/// </summary>
public static class HoleSize
{
    /// <summary>
    /// How far above the nominal bullet diameter a single hole's extent can read before it is flagged: docs/SCAN-MEASUREMENTS.md
    /// section 3.5 measured 260 holes of known calibre at a mean of 0.0202 in below nominal with a standard deviation of 0.0509 in,
    /// roughly constant in absolute terms, so three standard deviations above that mean is nominal plus 0.132 in. A pair of touching
    /// holes reads about two diameters.
    /// </summary>
    public const double AllowanceInches = -0.0202 + (3 * 0.0509);

    /// <summary>The snap search radius as a multiple of the bullet diameter: a tap anywhere on the hole still finds all of it.</summary>
    public const double SnapRadiusInDiameters = 1.0;

    /// <summary>The size-check search radius as a multiple of the bullet diameter, wide enough to hold a merged pair marked at its middle.</summary>
    public const double CheckRadiusInDiameters = 2.0;

    /// <summary>The scale's pixels per inch at an image point, averaged over the two axes, so a perspective scale is read locally.</summary>
    public static double PixelsPerInch(ScaleReference scale, PointD at)
    {
        ArgumentNullException.ThrowIfNull(scale);
        var o = scale.ToTarget(at);
        var x = scale.ToTarget(new PointD(at.X + 1, at.Y));
        var y = scale.ToTarget(new PointD(at.X, at.Y + 1));
        double inchesPerPixel = (Math.Sqrt(((x.X - o.X) * (x.X - o.X)) + ((x.Y - o.Y) * (x.Y - o.Y))) + Math.Sqrt(((y.X - o.X) * (y.X - o.X)) + ((y.Y - o.Y) * (y.Y - o.Y)))) / 2;
        return 1 / inchesPerPixel;
    }

    /// <summary>The snap radius in pixels for a tap at a point, from the calibre and the scale, or null when either is missing.</summary>
    public static double? SnapRadiusPixels(MarkingState state, PointD at)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Calibre is { } calibre && state.Scale is { } scale ? SnapRadiusInDiameters * calibre.DiameterInches * PixelsPerInch(scale, at) : null;
    }

    /// <summary>
    /// The largest extent, in pixels, of the dark region under a point: the pixels clearly darker than the window's paper level, as the
    /// snap reads them, connected to the dark pixel nearest the point. Null when there is no dark region, or when it reaches the edge of
    /// the search circle, which is a mark on ink or on a dark backer rather than on a hole in paper, where no size can be read.
    /// </summary>
    public static double? ApparentExtentPixels(GrayImage value, PointD at, double radiusPixels) => Measure(value, at, radiusPixels, null)?.Extent;

    /// <summary>The apparent extent, and the fraction of the region's pixels the expected artwork calls printed, zero without artwork.</summary>
    private static (double Extent, double ArtworkFraction)? Measure(GrayImage value, PointD at, double radiusPixels, GrayImage? artwork)
    {
        ArgumentNullException.ThrowIfNull(value);
        int x0 = Math.Max(0, (int)Math.Floor(at.X - radiusPixels)), x1 = Math.Min(value.Width - 1, (int)Math.Ceiling(at.X + radiusPixels));
        int y0 = Math.Max(0, (int)Math.Floor(at.Y - radiusPixels)), y1 = Math.Min(value.Height - 1, (int)Math.Ceiling(at.Y + radiusPixels));
        if (x1 - x0 < 2 || y1 - y0 < 2)
        {
            return null;
        }

        int w = x1 - x0 + 1, h = y1 - y0 + 1;
        var histogram = new int[256];
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                histogram[value.Pixels[(y * value.Width) + x]]++;
            }
        }

        int paper = 255;
        for (int level = 0, seen = 0; level < 256; level++)
        {
            seen += histogram[level];
            if (seen >= 0.9 * w * h)
            {
                paper = level;
                break;
            }
        }

        bool Inside(int x, int y) => ((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y)) <= radiusPixels * radiusPixels;
        bool Dark(int x, int y) => Inside(x, y) && paper - value.Pixels[(y * value.Width) + x] - 40 > 0;

        int seedX = -1, seedY = -1;
        double nearest = double.MaxValue;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                double d = ((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y));
                if (d < nearest && Dark(x, y))
                {
                    nearest = d;
                    seedX = x;
                    seedY = y;
                }
            }
        }

        if (seedX < 0)
        {
            return null;
        }

        var visited = new bool[w * h];
        var stack = new Stack<(int X, int Y)>();
        var region = new List<(int X, int Y)>();
        stack.Push((seedX, seedY));
        visited[((seedY - y0) * w) + seedX - x0] = true;
        double edge = (radiusPixels - 1.5) * (radiusPixels - 1.5);
        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (((x - at.X) * (x - at.X)) + ((y - at.Y) * (y - at.Y)) > edge)
            {
                return null;
            }

            region.Add((x, y));
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx >= x0 && nx <= x1 && ny >= y0 && ny <= y1 && !visited[((ny - y0) * w) + nx - x0] && Dark(nx, ny))
                {
                    visited[((ny - y0) * w) + nx - x0] = true;
                    stack.Push((nx, ny));
                }
            }
        }

        // The largest projection over sixteen directions, one pixel added for the pixels' own width.
        double largest = 0;
        for (int k = 0; k < 16; k++)
        {
            double angle = k * Math.PI / 16, c = Math.Cos(angle), s = Math.Sin(angle);
            double min = double.MaxValue, max = double.MinValue;
            foreach (var (x, y) in region)
            {
                double p = (x * c) + (y * s);
                min = Math.Min(min, p);
                max = Math.Max(max, p);
            }

            largest = Math.Max(largest, max - min + 1);
        }

        bool useArtwork = artwork is not null && artwork.Width == value.Width && artwork.Height == value.Height;
        double printed = useArtwork ? region.Count(p => artwork!.Pixels[(p.Y * value.Width) + p.X] < Snapping.ArtworkPaper) / (double)region.Count : 0;
        return (largest, printed);
    }

    /// <summary>
    /// The sentence for a flag, in whatever units <paramref name="length"/> writes, NOTES-FROM-PLANNING.md entry 40 section 1. An oversized
    /// dark region is two holes marked as one or a mark on the printed target, and both are named. Where the artwork is known and the
    /// region is mostly printed, only the second is, because it is then known.
    /// </summary>
    public static string Describe(HoleSizeFlag flag, double calibreInches, Func<double, string> length)
    {
        ArgumentNullException.ThrowIfNull(flag);
        ArgumentNullException.ThrowIfNull(length);
        string size = $"reads {length(flag.ApparentInches)} across, larger than one {length(calibreInches)} bullet hole ({length(flag.LargestExpectedInches)})";
        return flag.OnArtwork
            ? size + ", and it sits on the printed target: this is printed ink under the mark rather than a hole."
            : size + ". Two holes marked as one, or a tap that snapped to the printed target rather than a hole.";
    }

    /// <summary>
    /// Every counted shot whose hole reads larger than a single hole of the group's calibre can, once a calibre and a scale are set. With
    /// the sheet's expected artwork in image pixels, each flag says whether the region is mostly printed (entry 40 section 1).
    /// </summary>
    public static IReadOnlyList<HoleSizeFlag> Check(MarkingState state, GrayImage value, GrayImage? artwork = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Calibre is not { } calibre || state.Scale is not { } scale)
        {
            return [];
        }

        double largestExpected = calibre.DiameterInches + AllowanceInches;
        var flags = new List<HoleSizeFlag>();
        foreach (var shot in state.Shots.Where(s => s.IsShot))
        {
            double pixelsPerInch = PixelsPerInch(scale, shot.Image);
            if (Measure(value, shot.Image, CheckRadiusInDiameters * calibre.DiameterInches * pixelsPerInch, artwork) is { } measured && measured.Extent / pixelsPerInch > largestExpected)
            {
                var flag = new HoleSizeFlag(shot.Id, measured.Extent / pixelsPerInch, largestExpected, "", measured.ArtworkFraction >= 0.5);
                flags.Add(flag with { Problem = Describe(flag, calibre.DiameterInches, inches => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{inches:0.000} in")) });
            }
        }

        return flags;
    }
}
