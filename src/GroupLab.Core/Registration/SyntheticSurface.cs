using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;

namespace GroupLab.Core.Registration;

/// <summary>
/// Known truth for PHASE1-BRIEF.md section 3.3: a camera, a bent sheet, and optionally a twist the generalised cylinder
/// cannot represent, with marker corners projected through them and perturbed by the corner noise Phase 0 measured.
/// </summary>
public static class SyntheticSurface
{
    /// <summary>
    /// Corner noise per image axis, pixels. <c>main_flat1</c>, the flat frame with every marker decoded, fitted 0.00270 in
    /// RMS over its 136 corners under the Phase 0 lens model (PHASE0-RESULTS.md section 3b), at 1.07 image pixels per page
    /// dmm, its 42.9 px markers over 40 dmm: 0.735 px of two-dimensional RMS, or 0.52 px per axis. It includes whatever
    /// that frame's flat model left, so it is a ceiling on detector noise rather than a floor.
    /// </summary>
    public const double MeasuredCornerNoisePixels = 0.52;

    /// <summary>A perspective truth model: a camera looking at the page centre from <paramref name="distance"/> dmm, tilted about the page's x axis.</summary>
    public static SurfaceModel Camera(int width, int height, double focalPixels, double k1, double k2, double distance, double tilt,
        double pageWidth, double pageHeight, double rulingAngle, IReadOnlyList<double> bend)
    {
        double scale = Math.Max(width, height) / 2.0;
        return new SurfaceModel(SurfaceProjection.Perspective, rulingAngle, bend, tilt, 0, 0, 0, 0, distance, focalPixels / scale, k1, k2,
            (width - 1) / 2.0, (height - 1) / 2.0, scale, pageWidth / 2, pageHeight / 2);
    }

    /// <summary>
    /// Where the page point lands, pixels, with an optional twist: <paramref name="twist"/> dmm out of the page at the page
    /// corners, as a saddle in the along-ruling and across-ruling coordinates. A saddle has non-zero Gaussian curvature, so
    /// no developable surface, and no generalised cylinder, can take it.
    /// </summary>
    public static PointD Image(SurfaceModel truth, double twist, double pageWidth, double pageHeight, PointD page)
    {
        ArgumentNullException.ThrowIfNull(truth);
        var sheet = DevelopableSurface.Sheet(truth, page);
        if (twist != 0)
        {
            var (along, across) = DevelopableSurface.RulingCoordinates(truth, page);
            var extent = new[] { new PointD(0, 0), new PointD(pageWidth, 0), new PointD(pageWidth, pageHeight), new PointD(0, pageHeight) }
                .Select(p => DevelopableSurface.RulingCoordinates(truth, p)).ToList();
            double al = extent.Max(e => Math.Abs(e.Along)), ac = extent.Max(e => Math.Abs(e.Across));
            sheet = (sheet.X, sheet.Y, sheet.Z + (twist * (along / al) * (across / ac)));
        }

        return DevelopableSurface.Distort(truth, DevelopableSurface.Project(truth, sheet));
    }

    /// <summary>The top edge's image length over the bottom edge's: the keystone figure of NOTES-FROM-PLANNING.md entry 10.</summary>
    public static double Keystone(SurfaceModel truth, double pageWidth, double pageHeight)
    {
        var tl = DevelopableSurface.ToImage(truth, new PointD(0, 0));
        var tr = DevelopableSurface.ToImage(truth, new PointD(pageWidth, 0));
        var bl = DevelopableSurface.ToImage(truth, new PointD(0, pageHeight));
        var br = DevelopableSurface.ToImage(truth, new PointD(pageWidth, pageHeight));
        return Distance(tl, tr) / Distance(bl, br);
    }

    /// <summary>
    /// Every expected marker of tile 0 whose four corners land inside the image, or those in <paramref name="subset"/>,
    /// with Gaussian noise of <paramref name="noisePixels"/> per axis on each corner.
    /// </summary>
    public static List<MarkerMatch> Corners(TargetDefinition definition, SurfaceModel truth, double twist, double noisePixels, Random random,
        int width, int height, IReadOnlyCollection<int>? subset = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(random);
        double half = definition.Fiducials!.MarkerSize / 2.0;
        var matches = new List<MarkerMatch>();
        foreach (var marker in PageRegistration.ExpectedMarkers(definition, 0))
        {
            if (subset is not null && !subset.Contains(marker.Id))
            {
                continue;
            }

            PointD[] page = [new(marker.X - half, marker.Y - half), new(marker.X + half, marker.Y - half), new(marker.X + half, marker.Y + half), new(marker.X - half, marker.Y + half)];
            var image = page.Select(p => Image(truth, twist, definition.Page.Width, definition.Page.Height, p)).ToList();
            if (image.Any(q => double.IsNaN(q.X) || q.X < 0 || q.Y < 0 || q.X > width - 1 || q.Y > height - 1))
            {
                continue;
            }

            matches.Add(new MarkerMatch(marker.Id, [.. image.Select(q => new PointD(q.X + (noisePixels * Gaussian(random)), q.Y + (noisePixels * Gaussian(random))))], page));
        }

        return matches;
    }

    public static double Gaussian(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        double u = 1 - random.NextDouble(), v = random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
