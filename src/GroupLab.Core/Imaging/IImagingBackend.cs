namespace GroupLab.Core.Imaging;

/// <summary>
/// The operations Core needs from an imaging library, per DESIGN.md section 7.
/// Desktop and mobile bind different libraries behind this interface, so Core never
/// references one directly.
/// </summary>
public interface IImagingBackend
{
    /// <summary>
    /// Detects fiducial markers. Corners are in image pixels, ordered top-left, top-right,
    /// bottom-right, bottom-left as printed.
    /// </summary>
    IReadOnlyList<DetectedMarker> DetectMarkers(GrayImage image, MarkerDetectionOptions options);

    /// <summary>
    /// Fits a homography mapping <paramref name="source"/> onto <paramref name="destination"/>
    /// with RANSAC, rejecting correspondences further than the threshold, in destination units.
    /// </summary>
    HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination,
        double ransacThreshold);

    /// <summary>Resamples <paramref name="image"/> through <paramref name="transform"/>.</summary>
    GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height);
}

public readonly record struct PointD(double X, double Y);

/// <summary>Values match the GLTD-B family byte of TARGET-SCHEMA.md section 5.5.</summary>
public enum MarkerFamily
{
    AprilTag36h11 = 7,
}

public enum CornerRefinement
{
    None,
    Subpixel,
    Contour,
}

public sealed record MarkerDetectionOptions(
    MarkerFamily Family,
    double ExpectedMarkerSidePixels,
    CornerRefinement Refinement = CornerRefinement.Subpixel);

public sealed record DetectedMarker(int Id, IReadOnlyList<PointD> Corners);

public sealed record HomographyFit(Homography Transform, IReadOnlyList<bool> Inliers);
