using System.Globalization;
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
    /// The refinement window defaults to 5 px capped at one module rather than OpenCV's 0.3. On a clean 300 DPI render of
    /// GL-CF25-LTR, where a module is 5.9 px, 0.3 of a module left corners 0.24 px RMS from truth, one module 0.16 px, and
    /// two modules 2.0 px, once the window reached the next module's edges. Contour and AprilTag refinement measured 0.7
    /// to 0.8 px. These figures, and the 0.10 px inward bias left at one module, are measurement 3 of FIDUCIAL-DECISION.md
    /// section 10. <see cref="MarkerDetectionOptions.RefinementWindowModules"/> overrides the window for that measurement.
    /// Bits are read from a canonical image with about one pixel per module pixel, clamped to 4 to 16, rather than
    /// OpenCV's fixed 4 per cell. On the Phase 0 inkjet scans at 600 DPI a 94 px marker resampled to 32 px aliases the
    /// printed black's texture into wrong bits: 4 per cell read 6 and 5 of 9 markers on two tiles and 31 of 34 on a
    /// reference sheet, every loss a candidate quad found at the right place but unread, while 8 and 12 per cell read 9, 9
    /// and 34. Error-correction rate, border-bit tolerance, Otsu floor and threshold window changed nothing.
    /// </summary>
    public MarkerDetection DetectMarkers(GrayImage image, MarkerDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Family != MarkerFamily.AprilTag36h11)
        {
            throw new NotSupportedException($"Marker family {options.Family} is not supported.");
        }

        using var full = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var reduced = new Mat();
        var input = full;
        double scaleX = 1, scaleY = 1;
        if (options.DownsampleFactor > 1)
        {
            Cv2.Resize(full, reduced, new Size(image.Width / options.DownsampleFactor, image.Height / options.DownsampleFactor), 0, 0, InterpolationFlags.Area);
            input = reduced;
            scaleX = (double)image.Width / reduced.Width;
            scaleY = (double)image.Height / reduced.Height;
        }

        double side = options.ExpectedMarkerSidePixels / scaleX;
        double module = side / 8;
        int windowMax = options.ThresholdWindowMaxPixels is { } requested ? Odd(Math.Max(3, requested)) : Odd(Math.Max(23, (int)Math.Ceiling(side / 2)));
        (int refineWindow, float refineRelative) = options.RefinementWindowModules is { } modules
            ? (Math.Max(1, (int)Math.Round(module * modules)), (float)modules)
            : (5, 1f);
        var parameters = new DetectorParameters
        {
            AdaptiveThreshWinSizeMin = 3,
            AdaptiveThreshWinSizeMax = windowMax,
            AdaptiveThreshWinSizeStep = Math.Max(10, (windowMax - 3) / 4),
            AdaptiveThreshConstant = 7,
            MinMarkerPerimeterRate = 4 * side * Math.Sqrt(MinimumAreaFraction) / Math.Max(input.Width, input.Height),
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
            CornerRefinementWinSize = refineWindow,
            RelativeCornerRefinmentWinSize = refineRelative,
            CornerRefinementMaxIterations = 100,
            CornerRefinementMinAccuracy = 0.001,
            MarkerBorderBits = 1,
            PerspectiveRemovePixelPerCell = Math.Clamp((int)Math.Round(module), 4, 16),
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

        using var dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.DictAprilTag_36h11);
        using var detector = new ArucoDetector(dictionary, parameters, new RefineParameters());
        detector.DetectMarkers(input, out Point2f[][] corners, out int[] ids, out Point2f[][] notDecoded);

        // FIDUCIAL-DECISION.md section 11: OpenCV's DICT_APRILTAG_36h11 holds each code turned 180 degrees from the
        // official orientation GroupLab prints, so its first corner is the printed bottom-right. The corners are turned
        // back here, and OpenCvMarkerTableTests checks the correction against all 587 codes.
        var markers = new List<DetectedMarker>(ids.Length);
        var rejected = new List<MarkerRejection>();
        for (int i = 0; i < ids.Length; i++)
        {
            var c = corners[i];
            PointD[] printed = [Full(c[2]), Full(c[3]), Full(c[0]), Full(c[1])];
            if (ShapeGateFailure(printed, options.ExpectedMarkerSidePixels) is { } reason)
            {
                rejected.Add(new MarkerRejection(ids[i], printed, reason));
            }
            else
            {
                markers.Add(new DetectedMarker(ids[i], printed));
            }
        }

        return new MarkerDetection(markers, rejected, [.. notDecoded.Select(q => (IReadOnlyList<PointD>)[.. q.Select(Full)])]);

        // A pixel centre at u in the reduced image covers full-resolution pixels u * s to (u + 1) * s - 1.
        PointD Full(Point2f p) => new(((p.X + 0.5) * scaleX) - 0.5, ((p.Y + 0.5) * scaleY) - 0.5);
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
        return Copy(result);
    }

    /// <summary>Copies a single-channel 8-bit OpenCV image into a <see cref="GrayImage"/>.</summary>
    public static GrayImage Copy(Mat mat)
    {
        ArgumentNullException.ThrowIfNull(mat);
        if (mat.Type() != MatType.CV_8UC1)
        {
            throw new InvalidOperationException($"Expected an 8-bit single-channel image, got {mat.Type()}.");
        }

        using var contiguous = mat.IsContinuous() ? null : mat.Clone();
        var data = contiguous ?? mat;
        var pixels = new byte[(long)data.Width * data.Height];
        Marshal.Copy(data.Data, pixels, 0, pixels.Length);
        return new GrayImage(data.Width, data.Height, pixels);
    }

    private static string? ShapeGateFailure(PointD[] c, double expectedSide)
    {
        double area = 0, shortest = double.MaxValue, longest = 0;
        for (int k = 0; k < 4; k++)
        {
            var p = c[k];
            var q = c[(k + 1) % 4];
            area += (p.X * q.Y) - (q.X * p.Y);
            double length = Math.Sqrt(Math.Pow(q.X - p.X, 2) + Math.Pow(q.Y - p.Y, 2));
            shortest = Math.Min(shortest, length);
            longest = Math.Max(longest, length);
        }

        double fraction = Math.Abs(area) / 2 / (expectedSide * expectedSide);
        if (fraction < MinimumAreaFraction)
        {
            return string.Create(CultureInfo.InvariantCulture, $"area {fraction:0.00} of the expected marker, below {MinimumAreaFraction:0.0}");
        }

        return longest > MaximumSideRatio * shortest
            ? string.Create(CultureInfo.InvariantCulture, $"side ratio {longest / shortest:0.00}, above {MaximumSideRatio:0.0}")
            : null;
    }

    private static int Odd(int n) => n % 2 == 0 ? n + 1 : n;
}
