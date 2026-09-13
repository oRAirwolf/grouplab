using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>A bull centre as declared and as recovered from an image, in page dmm.</summary>
public sealed record BullRecovery(int Index, string? Label, double DeclaredX, double DeclaredY, double RecoveredX, double RecoveredY)
{
    public double Error => Math.Sqrt(Math.Pow(RecoveredX - DeclaredX, 2) + Math.Pow(RecoveredY - DeclaredY, 2));
}

/// <summary>
/// Recovers bull centres for conformance test 43 of TARGET-SCHEMA.md section 10. The centre is an ink-weighted centroid
/// over a window on the page just larger than the bull's outermost disc, predicted into the image through the
/// registration and mapped back, iterated until it stops moving. A disc stack is symmetric about its centre (section
/// 3.4), so the centroid of a clean render is the centre, and what the test then measures is whether the renderer, the
/// rasteriser and the registration agree about where that is.
/// </summary>
public static class BullLocator
{
    /// <summary>How far the window reaches past the outermost disc, in dmm. The nearest other artwork, a label, is 15 dmm out.</summary>
    public const double WindowMargin = 5;

    private const int MaximumIterations = 10;

    public static IReadOnlyList<BullRecovery> Locate(GrayImage image, TargetDefinition definition, Homography imageToPage)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(imageToPage);
        var pageToImage = imageToPage.Inverse();
        var radii = definition.RingSets.GroupBy(s => s.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Discs.Max(disc => disc.Diameter) / 2.0, StringComparer.Ordinal);

        var recovered = new List<BullRecovery>();
        for (int i = 0; i < definition.Bulls.Count; i++)
        {
            var bull = definition.Bulls[i];
            if (!radii.TryGetValue(bull.RingSet, out double radius))
            {
                continue;
            }

            var centre = new PointD(bull.X, bull.Y);
            for (int iteration = 0; iteration < MaximumIterations; iteration++)
            {
                if (Centroid(image, imageToPage, pageToImage, centre, radius + WindowMargin) is not { } next)
                {
                    break;
                }

                double shift = Math.Sqrt(Math.Pow(next.X - centre.X, 2) + Math.Pow(next.Y - centre.Y, 2));
                centre = next;
                if (shift < 0.001)
                {
                    break;
                }
            }

            recovered.Add(new BullRecovery(i, bull.Label, bull.X, bull.Y, centre.X, centre.Y));
        }

        return recovered;
    }

    /// <summary>
    /// The ink-weighted centroid, in page dmm, of the pixels whose centres fall within <paramref name="window"/> dmm of
    /// <paramref name="centre"/>. Pixel (u, v) is taken at (u, v), the convention the marker corners were measured in.
    /// </summary>
    private static PointD? Centroid(GrayImage image, Homography imageToPage, Homography pageToImage, PointD centre, double window)
    {
        PointD[] box =
        [
            pageToImage.Apply(new PointD(centre.X - window, centre.Y - window)),
            pageToImage.Apply(new PointD(centre.X + window, centre.Y - window)),
            pageToImage.Apply(new PointD(centre.X + window, centre.Y + window)),
            pageToImage.Apply(new PointD(centre.X - window, centre.Y + window)),
        ];
        int u0 = Math.Max(0, (int)Math.Floor(box.Min(p => p.X))), u1 = Math.Min(image.Width - 1, (int)Math.Ceiling(box.Max(p => p.X)));
        int v0 = Math.Max(0, (int)Math.Floor(box.Min(p => p.Y))), v1 = Math.Min(image.Height - 1, (int)Math.Ceiling(box.Max(p => p.Y)));

        double sumX = 0, sumY = 0, sum = 0, limit = window * window;
        var pixels = image.Pixels;
        for (int v = v0; v <= v1; v++)
        {
            int row = v * image.Width;
            for (int u = u0; u <= u1; u++)
            {
                byte value = pixels[row + u];
                if (value == 255)
                {
                    continue;
                }

                var p = imageToPage.Apply(new PointD(u, v));
                if (Math.Pow(p.X - centre.X, 2) + Math.Pow(p.Y - centre.Y, 2) > limit)
                {
                    continue;
                }

                double ink = (255 - value) / 255.0;
                sumX += ink * u;
                sumY += ink * v;
                sum += ink;
            }
        }

        return sum == 0 ? null : imageToPage.Apply(new PointD(sumX / sum, sumY / sum));
    }
}
