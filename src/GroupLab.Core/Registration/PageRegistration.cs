using GroupLab.Core.Gltd.Derivation;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// A registration: the transform from image pixels to page dmm, its residual over the inlier marker corners in dmm, and
/// the print scale against the nominal resolution, on each axis and by area (DETECTION-PIPELINE.md stage S4).
/// </summary>
public sealed record RegistrationResult(
    Homography? ImageToPage,
    int MarkersExpected,
    int MarkersFound,
    int Inliers,
    double RmsResidual,
    double MaxResidual,
    double ScaleX,
    double ScaleY,
    double Scale,
    string? Failure);

/// <summary>
/// Stages S2 and S3 of DETECTION-PIPELINE.md and nothing more, as PHASE0-BRIEF.md section 3 limits Phase 0a: find the
/// definition's markers, then fit a homography from image pixels to page dmm by RANSAC over their corners.
/// </summary>
public static class PageRegistration
{
    /// <summary>RANSAC inlier distance in page dmm, ten times the residual gate: a true corner is never rejected, a misplaced marker always is.</summary>
    public const double RansacThreshold = 2.54;

    /// <summary>The markers printed on one tile, with the identifiers TARGET-SCHEMA.md section 3.7 assigns over the assembly.</summary>
    public static IReadOnlyList<Marker> ExpectedMarkers(TargetDefinition definition, int tileIndex)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Fiducials is not { } f)
        {
            return [];
        }

        if (f.Scheme == "explicit")
        {
            return f.Markers ?? [];
        }

        return FiducialDerivation.Derive(definition).Markers is { } derived
            ? MarkerIds.Assign(derived.Positions, definition.Tiling, tileIndex, MarkerIds.DictionarySize(f.Family)).Markers
            : [];
    }

    public static RegistrationResult Register(GrayImage image, TargetDefinition definition, int tileIndex, double nominalDpi, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(backend);
        var expected = ExpectedMarkers(definition, tileIndex);
        if (expected.Count == 0 || definition.Fiducials is not { } f)
        {
            return Failed(0, 0, "the definition has no markers");
        }

        double pixelsPerDmm = nominalDpi / 254.0;
        var detected = backend.DetectMarkers(image, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, f.MarkerSize * pixelsPerDmm)).Markers;

        // A marker's square is markerSize across (section 3.7), and its corners come back top-left first, clockwise.
        var unmatched = expected.GroupBy(m => m.Id).Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => g.First());
        var source = new List<PointD>();
        var destination = new List<PointD>();
        foreach (var marker in detected)
        {
            if (!unmatched.Remove(marker.Id, out var m))
            {
                continue;
            }

            double h = f.MarkerSize / 2.0;
            source.AddRange(marker.Corners);
            destination.AddRange([new(m.X - h, m.Y - h), new(m.X + h, m.Y - h), new(m.X + h, m.Y + h), new(m.X - h, m.Y + h)]);
        }

        int found = source.Count / 4;
        if (found < 4)
        {
            return Failed(expected.Count, found, $"{found} of {expected.Count} markers found; a homography needs 4");
        }

        HomographyFit fit;
        try
        {
            fit = backend.FindHomography(source, destination, RansacThreshold);
        }
        catch (InvalidOperationException ex)
        {
            return Failed(expected.Count, found, ex.Message);
        }

        double sum = 0, max = 0;
        int inliers = 0;
        for (int i = 0; i < source.Count; i++)
        {
            if (!fit.Inliers[i])
            {
                continue;
            }

            var p = fit.Transform.Apply(source[i]);
            double error = Math.Sqrt(Math.Pow(p.X - destination[i].X, 2) + Math.Pow(p.Y - destination[i].Y, 2));
            sum += error * error;
            max = Math.Max(max, error);
            inliers++;
        }

        // Image pixels per page dmm at the page centre, against the nominal resolution.
        var j = fit.Transform.Inverse().Jacobian(new PointD(definition.Page.Width / 2.0, definition.Page.Height / 2.0));
        return new RegistrationResult(
            fit.Transform,
            expected.Count,
            found,
            inliers,
            inliers == 0 ? double.NaN : Math.Sqrt(sum / inliers),
            max,
            Math.Sqrt((j.XX * j.XX) + (j.YX * j.YX)) / pixelsPerDmm,
            Math.Sqrt((j.XY * j.XY) + (j.YY * j.YY)) / pixelsPerDmm,
            Math.Sqrt(Math.Abs((j.XX * j.YY) - (j.XY * j.YX))) / pixelsPerDmm,
            null);
    }

    private static RegistrationResult Failed(int expected, int found, string reason) =>
        new(null, expected, found, 0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, reason);
}
