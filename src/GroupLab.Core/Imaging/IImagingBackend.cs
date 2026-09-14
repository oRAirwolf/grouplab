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
    /// bottom-right, bottom-left as printed. Candidates refused by the shape gates of
    /// DETECTION-PIPELINE.md stage S2 are returned as rejections, a separate reason from the code check.
    /// </summary>
    MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options);

    /// <summary>
    /// Fits a homography mapping <paramref name="source"/> onto <paramref name="destination"/>
    /// with RANSAC, rejecting correspondences further than the threshold, in destination units.
    /// </summary>
    HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination,
        double ransacThreshold);

    /// <summary>Resamples <paramref name="image"/> through <paramref name="transform"/>.</summary>
    GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height);

    /// <summary>
    /// A morphological opening or closing with an elliptical structuring element of radius <paramref name="radius"/>, a
    /// square of side 2 r + 1 as OpenCV builds it: the operations of the hole-detection primitive of
    /// docs/SCAN-MEASUREMENTS.md section 3.1.
    /// </summary>
    GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius);

    /// <summary>
    /// The blobs of a binary image, non-zero foreground: each external outline filled, then the 8-connected components of
    /// the filled image, each with its pixel count, bounding box, and the convex hull of its outline in image pixels.
    /// </summary>
    IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary);
}

/// <summary>An image or page coordinate. Measurement happens in floating point; only the definition is integer.</summary>
public readonly record struct PointD(double X, double Y);

/// <summary>The two operations of <see cref="IImagingBackend.Morphology"/>.</summary>
public enum MorphologyOperation
{
    Open,
    Close,
}

/// <summary>A connected blob: its pixel count, bounding box, and the convex hull of its outline, image pixels.</summary>
public sealed record ImageBlob(int Area, int Left, int Top, int Width, int Height, IReadOnlyList<PointD> Hull);

/// <summary>Values match the GLTD-B family byte of TARGET-SCHEMA.md section 5.5.</summary>
public enum MarkerFamily
{
    AprilTag36h11 = 7,
}

/// <summary>
/// Corner refinement methods. DETECTION-PIPELINE.md stage S2 requires refinement to be on, because OpenCV's
/// default leaves corners unrefined against a 0.6 pixel registration gate; FIDUCIAL-DECISION.md section 10 compares them.
/// </summary>
public enum CornerRefinement
{
    None,
    Subpixel,
    Contour,
}

/// <summary>
/// Detector settings. <see cref="ExpectedMarkerSidePixels"/> sizes the adaptive threshold window and the shape gates of
/// DETECTION-PIPELINE.md stage S2. The remaining settings exist for FIDUCIAL-DECISION.md section 10's measurements 3 and 4
/// and default to what the backend ships: <see cref="RefinementWindowModules"/> the subpixel window in marker modules,
/// <see cref="ThresholdWindowMaxPixels"/> the largest adaptive threshold window in working pixels, and
/// <see cref="DownsampleFactor"/> an area reduction applied before detection, with corners returned at full resolution.
/// </summary>
public sealed record MarkerDetectionOptions(
    MarkerFamily Family,
    double ExpectedMarkerSidePixels,
    CornerRefinement Refinement = CornerRefinement.Subpixel,
    double? RefinementWindowModules = null,
    int? ThresholdWindowMaxPixels = null,
    int DownsampleFactor = 1);

/// <summary>A decoded marker with its four corners in image pixels.</summary>
public sealed record DetectedMarker(int Id, IReadOnlyList<PointD> Corners);

/// <summary>A decoded candidate refused by a stage S2 shape gate, with the reason.</summary>
public sealed record MarkerRejection(int Id, IReadOnlyList<PointD> Corners, string Reason);

/// <summary>
/// What detection kept, what the shape gates refused, and the candidate quads that never decoded, in the detector's own
/// corner order. The last tell a missing marker's two failure modes apart: found as a quad but unreadable, or never found.
/// </summary>
public sealed record MarkerDetection(IReadOnlyList<DetectedMarker> Markers, IReadOnlyList<MarkerRejection> Rejected, IReadOnlyList<IReadOnlyList<PointD>> Undecoded)
{
    public int CandidatesNotDecoded => Undecoded.Count;
}

/// <summary>A fitted transform and which correspondences RANSAC kept (DETECTION-PIPELINE.md stage S3).</summary>
public sealed record HomographyFit(Homography Transform, IReadOnlyList<bool> Inliers);
