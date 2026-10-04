using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 3.1: the aiming marks on a store-bought target that <see cref="StoreFingerprintBuilder.Bulls"/>
/// does not see. That finder looks for a red centre ringed in black, the Shoot-N-C's; on Alan's photographs it found none on the Allen EZ
/// Aim sight-in grid (orange diamonds), the Eze-Scorer sight-in grid (red dots in green squares) or the Allen splash bull (a black disc).
/// <para>
/// <b>How.</b> What a person aims at is drawn as shapes round one point: rings, nested diamonds, a square round a dot. So the printing, any
/// ink dark or coloured, is outlined at 100 dpi, every closed outline that is compact (not a line of a grid, not a long word) is reduced to
/// its centre and size, and a centre that three or more outlines of clearly different sizes share, the largest at least a quarter of an
/// inch across and twice the smallest, is an aiming mark. A grid's cells each stand alone; a digit and its hole are only two.
/// </para>
/// </summary>
public static class AimMarks
{
    private const double Dpi = 100;


    /// <summary>The aiming marks' centres on a straightened target, inches from its top left, in reading order.</summary>
    public static PointD[] Find(Mat straight, double sourceDpi)
    {
        ArgumentNullException.ThrowIfNull(straight);
        using var small = new Mat();
        Cv2.Resize(straight, small, new Size(0, 0), Dpi / sourceDpi, Dpi / sourceDpi, InterpolationFlags.Area);
        using var grey = new Mat();
        Cv2.CvtColor(small, grey, ColorConversionCodes.BGR2GRAY);
        using var hsv = new Mat();
        Cv2.CvtColor(small, hsv, ColorConversionCodes.BGR2HSV);

        // Dark ink is anything darker than the paper round it; the paper's own brightness is taken locally (the lightest nearby,
        // smoothed), so a shadow or uneven light is not ink.
        using var local = new Mat();
        using (var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(31, 31)))
        {
            Cv2.Dilate(grey, local, kernel);
        }

        Cv2.GaussianBlur(local, local, new Size(0, 0), 15);
        using var ratio = new Mat();
        Cv2.Divide(grey, local, ratio, 255.0);
        var masks = new List<(Mat Mask, bool Colour)>();
        var dark = new Mat();
        // Dark: clearly darker than the paper round it. Orange and red count here too, which joins a dark diamond to the orange disc it sits
        // in (the Rigid crosshair's), and is what finds a black bull under glare (the splash bull's); each colour is also outlined alone below.
        Cv2.Threshold(ratio, dark, 190, 255, ThresholdTypes.BinaryInv);
        masks.Add((dark, false));

        // Each ink colour on its own, so a red dot in a green square is two shapes and not one blob: red, orange, yellow, green, blue.
        foreach (var (low, high) in new[] { (0, 9), (170, 180), (9, 22), (22, 35), (35, 85), (85, 130) })
        {
            var mask = new Mat();
            Cv2.InRange(hsv, new Scalar(low, 70, 40), new Scalar(high, 255, 255), mask);
            masks.Add((mask, true));
        }

        var shapes = new List<(PointD Centre, double Size, bool SolidColour, double Round)>();
        using (var opening = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
        {
            foreach (var (mask, colour) in masks)
            {
                // A grid's thin lines, a dot or two wide at 100 dpi, would join the shapes they cross; an opening takes them away.
                Cv2.MorphologyEx(mask, mask, MorphTypes.Open, opening);
                Cv2.FindContours(mask, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.CComp, ContourApproximationModes.ApproxSimple);
                for (int i = 0; i < contours.Length; i++)
                {
                    var contour = contours[i];
                    double area = Cv2.ContourArea(contour);
                    if (area < 0.04 * 0.04 * Dpi * Dpi)
                    {
                        continue;
                    }

                    var box = Cv2.BoundingRect(contour);
                    double longer = Math.Max(box.Width, box.Height), shorter = Math.Min(box.Width, box.Height);
                    // Compact: about as wide as tall, and filling a good part of its box (a circle 0.79, a diamond 0.5, a square 1).
                    if (longer > 1.3 * shorter || area < 0.42 * box.Width * box.Height || longer > 0.9 * Math.Min(small.Width, small.Height))
                    {
                        continue;
                    }

                    var m = Cv2.Moments(contour);
                    if (m.M00 <= 0)
                    {
                        continue;
                    }

                    // An outer outline of a coloured region with no hole in it is a solid coloured dot or square.
                    bool solid = colour && hierarchy[i].Parent < 0 && hierarchy[i].Child < 0;
                    // How much of the circle round it the shape fills: a disc 1, a diamond or square about 0.64, a letter far less.
                    Cv2.MinEnclosingCircle(contour, out _, out float radius);
                    shapes.Add((new PointD(m.M10 / m.M00, m.M01 / m.M00), Math.Sqrt(area), solid, area / (Math.PI * radius * radius)));
                }

                mask.Dispose();
            }
        }

        // Centres that several shapes share: each shape's neighbours within a twentieth of an inch, or a twentieth of its own size.
        var marks = new List<PointD>();
        foreach (var (centre, size, _, _) in shapes.OrderByDescending(s => s.Size))
        {
            double near = Math.Max(0.05 * Dpi, 0.04 * size);
            var together = shapes.Where(s => Apart(s.Centre, centre) <= near).OrderBy(s => s.Size).ToList();
            var levels = new List<double>();
            foreach (var s in together)
            {
                if (levels.Count == 0 || s.Size > 1.12 * levels[^1])
                {
                    levels.Add(s.Size);
                }
            }


            // Three outlines or more of clearly different sizes, the largest a quarter inch or more and twice the smallest; or a solid coloured
            // dot a tenth of an inch or more with an outline round it two and a half times its size, as a red dot in a square.
            bool nested = levels.Count >= 3 && levels[^1] >= 0.25 * Dpi && levels[^1] >= 2 * levels[0];
            bool dot = together.FirstOrDefault() is { SolidColour: true } inner && inner.Size >= 0.1 * Dpi && levels.Count >= 2 && levels[^1] >= 2.5 * inner.Size;

            // A solid coloured dot or diamond on its own, a sixth of an inch to an inch across: what a Shoot-N-C or an Eze-Scorer grid prints
            // to aim at.
            bool alone = size == together[0].Size && together[0].SolidColour && together[0].Round >= 0.55 && size >= 0.15 * Dpi && size <= 1.0 * Dpi;
            // Repair pasters, ringed and dotted like a bull, sit in the corners and along the edges; an aim point is an inch or more inside.
            bool inside = centre.X >= Dpi && centre.Y >= Dpi && centre.X <= small.Width - Dpi && centre.Y <= small.Height - Dpi;
            if ((!nested && !dot && !alone) || !inside)
            {
                continue;
            }

            var at = new PointD(together.Average(s => s.Centre.X) / Dpi, together.Average(s => s.Centre.Y) / Dpi);
            if (marks.All(p => Apart(p, at) > 0.3))
            {
                marks.Add(at);
            }
        }

        return [.. marks.OrderBy(p => Math.Round(p.Y * 2)).ThenBy(p => p.X)];
    }

    /// <summary>The red finder's marks with these added where they are not already within 0.3 in of one.</summary>
    public static PointD[] Merge(IReadOnlyList<PointD> red, IReadOnlyList<PointD> shapes)
    {
        ArgumentNullException.ThrowIfNull(red);
        ArgumentNullException.ThrowIfNull(shapes);
        var all = red.ToList();
        all.AddRange(shapes.Where(s => all.All(p => Apart(p, s) > 0.3)));
        return [.. all];
    }

    private static double Apart(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
