using System.Runtime.InteropServices;
using GroupLab.Core.Imaging;
using OpenCvSharp;
using OpenCvSharp.Aruco;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// The desktop imaging backend of DESIGN.md section 7: OpenCV through OpenCvSharp (Apache-2.0). It needs the full
/// OpenCvSharp4.runtime.win package; the slim runtime compiles out every ArUco export and fails only at run time
/// (PHASE0-BRIEF.md section 6).
/// </summary>
public sealed class OpenCvSharpBackend : IImagingBackend
{
    /// <summary>DETECTION-PIPELINE.md stage S2: reject a candidate below half the expected marker area.</summary>
    public const double MinimumAreaFraction = 0.5;

    /// <summary>DETECTION-PIPELINE.md stage S2: reject a candidate whose longest side exceeds twice its shortest.</summary>
    public const double MaximumSideRatio = 2.0;

    /// <summary>
    /// Stage S2 settings, set in full rather than trusting a struct's defaults. Two differ from OpenCV's defaults, as S2
    /// requires: corner refinement is on, and the adaptive threshold window grows with the expected marker. The area gate
    /// runs inside OpenCV before decoding, as a minimum perimeter (a square of half the area has 1/sqrt(2) the
    /// perimeter). OpenCV has no side-ratio setting, so that gate runs on the decoded corners instead, as stage S2 records.
    /// The refinement window is capped at one module rather than OpenCV's 0.3. On a clean 300 DPI render of GL-CF25-LTR,
    /// where a module is 5.9 px, 0.3 of a module left corners 0.24 px RMS from truth, one module 0.16 px, and two modules
    /// 2.0 px, once the window reached the next module's edges. Contour and AprilTag refinement measured 0.7 to 0.8 px.
    /// These figures, and the 0.10 px inward bias left at one module, are measurement 3 of FIDUCIAL-DECISION.md section 10.
    /// </summary>
    public IReadOnlyList<DetectedMarker> DetectMarkers(GrayImage image, MarkerDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Family != MarkerFamily.AprilTag36h11)
        {
            throw new NotSupportedException($"Marker family {options.Family} is not supported.");
        }

        double side = options.ExpectedMarkerSidePixels;
        int windowMax = Odd(Math.Max(23, (int)Math.Ceiling(side / 2)));
        var parameters = new DetectorParameters
        {
            AdaptiveThreshWinSizeMin = 3,
            AdaptiveThreshWinSizeMax = windowMax,
            AdaptiveThreshWinSizeStep = Math.Max(10, (windowMax - 3) / 4),
            AdaptiveThreshConstant = 7,
            MinMarkerPerimeterRate = 4 * side * Math.Sqrt(MinimumAreaFraction) / Math.Max(image.Width, image.Height),
            MaxMarkerPerimeterRate = 4,
            PolygonalApproxAccuracyRate = 0.03,
            MinCornerDistanceRate = 0.05,
            MinDistanceToBorder = 3,
            MinMarkerDistanceRate = 0.125,
            MinGroupDistance = 0.21f,
            CornerRefinementMethod = options.Refinement switch
            {
                CornerRefinement.None => CornerRefineMethod.None,
                CornerRefinement.Contour => CornerRefineMethod.Contour,
                _ => CornerRefineMethod.Subpix,
            },
            CornerRefinementWinSize = 5,
            RelativeCornerRefinmentWinSize = 1f,
            CornerRefinementMaxIterations = 100,
            CornerRefinementMinAccuracy = 0.001,
            MarkerBorderBits = 1,
            PerspectiveRemovePixelPerCell = 4,
            PerspectiveRemoveIgnoredMarginPerCell = 0.13,
            MaxErroneousBitsInBorderRate = 0.35,
            MinOtsuStdDev = 5,
            ErrorCorrectionRate = 0.6,
            AprilTagQuadDecimate = 0,
            AprilTagQuadSigma = 0,
            AprilTagMinClusterPixels = 5,
            AprilTagMaxNmaxima = 10,
            AprilTagCriticalRad = (float)(10 * Math.PI / 180),
            AprilTagMaxLineFitMse = 10,
            AprilTagMinWhiteBlackDiff = 5,
            AprilTagDeglitch = 0,
            DetectInvertedMarker = false,
            UseAruco3Detection = false,
            MinSideLengthCanonicalImg = 32,
            MinMarkerLengthRatioOriginalImg = 0,
        };

        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.DictAprilTag_36h11);
        using var detector = new ArucoDetector(dictionary, parameters, new RefineParameters());
        detector.DetectMarkers(mat, out Point2f[][] corners, out int[] ids, out _);

        // FIDUCIAL-DECISION.md section 11: OpenCV's DICT_APRILTAG_36h11 holds each code turned 180 degrees from the
        // official orientation GroupLab prints, so its first corner is the printed bottom-right. The corners are turned
        // back here, and OpenCvMarkerTableTests checks the correction against all 587 codes.
        var markers = new List<DetectedMarker>(ids.Length);
        for (int i = 0; i < ids.Length; i++)
        {
            if (PassesShapeGates(corners[i], side * side))
            {
                var c = corners[i];
                markers.Add(new DetectedMarker(ids[i], [new(c[2].X, c[2].Y), new(c[3].X, c[3].Y), new(c[0].X, c[0].Y), new(c[1].X, c[1].Y)]));
            }
        }

        return markers;
    }

    public HomographyFit FindHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double ransacThreshold)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (source.Count != destination.Count || source.Count < 4)
        {
            throw new InvalidOperationException($"A homography needs at least 4 matched points, got {source.Count} and {destination.Count}.");
        }

        using var mask = new Mat();
        using var h = Cv2.FindHomography(
            source.Select(p => new Point2d(p.X, p.Y)), destination.Select(p => new Point2d(p.X, p.Y)),
            HomographyMethods.Ransac, ransacThreshold, mask, 10000, 0.9999);
        if (h.Empty())
        {
            throw new InvalidOperationException("OpenCV found no homography.");
        }

        h.GetArray(out double[] values);
        mask.GetArray(out byte[] inliers);
        return new HomographyFit(new Homography(values), [.. inliers.Select(b => b != 0)]);
    }

    /// <summary>Resamples bilinearly, with <paramref name="transform"/> taking source pixels to destination pixels, and paper beyond the source.</summary>
    public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(transform);
        double[] values = [.. Enumerable.Range(0, 9).Select(i => transform[i / 3, i % 3])];
        using var source = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var matrix = Mat.FromPixelData(3, 3, MatType.CV_64FC1, values);
        using var result = new Mat();
        Cv2.WarpPerspective(source, result, matrix, new Size(width, height), InterpolationFlags.Linear, BorderTypes.Constant, new Scalar(255));
        if (!result.IsContinuous())
        {
            throw new InvalidOperationException("OpenCV returned a non-contiguous image.");
        }

        var pixels = new byte[(long)width * height];
        Marshal.Copy(result.Data, pixels, 0, pixels.Length);
        return new GrayImage(width, height, pixels);
    }

    private static bool PassesShapeGates(Point2f[] c, double expectedArea)
    {
        double area = 0, shortest = double.MaxValue, longest = 0;
        for (int k = 0; k < 4; k++)
        {
            var p = c[k];
            var q = c[(k + 1) % 4];
            area += ((double)p.X * q.Y) - ((double)q.X * p.Y);
            double length = Math.Sqrt(Math.Pow((double)q.X - p.X, 2) + Math.Pow((double)q.Y - p.Y, 2));
            shortest = Math.Min(shortest, length);
            longest = Math.Max(longest, length);
        }

        return Math.Abs(area) / 2 >= MinimumAreaFraction * expectedArea && longest <= MaximumSideRatio * shortest;
    }

    private static int Odd(int n) => n % 2 == 0 ? n + 1 : n;
}
