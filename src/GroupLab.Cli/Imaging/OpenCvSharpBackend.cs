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
    /// Bits are read from a canonical image of at least 8 pixels per cell, one per module pixel above that up to 16, with
    /// 0.3 of each cell ignored at its edge, rather than OpenCV's fixed 4 and 0.13. On the Phase 0 inkjet scans at 600 DPI
    /// a 94 px marker resampled to 32 px aliases the printed black's texture into wrong bits: 4 per cell read 6 and 5 of 9
    /// markers on two tiles and 31 of 34 on a reference sheet, every loss a candidate quad found at the right place but
    /// unread, while 8 and 12 per cell read 9, 9 and 34. A floor of 8 rather than one per module pixel matters for smaller
    /// markers: on the photographs, whose markers are 36 to 46 px, one per module pixel read 21 to 33 of 34 and 8 per cell
    /// with a 0.3 margin read 26 to 34, and over all 21 scans the floor took detection from 512 to 514 of 514. Error-
    /// correction rate, border-bit tolerance, Otsu floor and threshold window changed nothing.
    /// The largest adaptive threshold window is the marker's side. Over the 600 DPI scans of the ten Phase 0 sheets, 240
    /// markers, a 7 px window found none, OpenCV's 23 px default 238, half the side 239, and the side 240, in the same
    /// detection time. With the canonical cell above, downsampling by 2 or 3 before detection found 240 and 239 at the
    /// same worst bull, and is not used (PHASE0-RESULTS.md, measurement 3 and section 7).
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
        int windowMax = options.ThresholdWindowMaxPixels is { } requested ? Odd(Math.Max(3, requested)) : Odd(Math.Max(23, (int)Math.Ceiling(side)));
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
            // Entry 101: both refinements are done in managed code below, on the unrefined corners, because the native build refines
            // differently on each platform.
            CornerRefinementMethod = CornerRefineMethod.None,
            CornerRefinementWinSize = refineWindow,
            RelativeCornerRefinmentWinSize = refineRelative,
            CornerRefinementMaxIterations = 100,
            CornerRefinementMinAccuracy = 0.001,
            MarkerBorderBits = 1,
            PerspectiveRemovePixelPerCell = Math.Clamp((int)Math.Round(module), 8, 16),
            PerspectiveRemoveIgnoredMarginPerCell = 0.3,
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
        if (options.Refinement == CornerRefinement.Subpixel)
        {
            corners = RefineSubPixel(input, corners, refineWindow, refineRelative);
        }
        else if (options.Refinement == CornerRefinement.Contour)
        {
            corners = RefineOnContours(input, corners, parameters);
        }

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

    /// <summary>
    /// OpenCV ArUco's sub-pixel step, in managed code (entry 101): each marker's window is its module size times the relative window, at
    /// least 2 and at most the configured window, as <c>ArucoDetector</c> sizes it, then <see cref="PortableImaging.CornerSubPix"/> with
    /// OpenCV's 100 iterations and 0.001 px.
    /// </summary>
    private static Point2f[][] RefineSubPixel(Mat input, Point2f[][] corners, int window, float relative)
    {
        input.GetArray(out byte[] pixels);
        var image = new GrayImage(input.Width, input.Height, pixels);
        var refined = new Point2f[corners.Length][];
        for (int m = 0; m < corners.Length; m++)
        {
            var c = corners[m];
            double perimeter = 0;
            for (int i = 0; i < 4; i++)
            {
                double dx = c[(i + 1) % 4].X - c[i].X, dy = c[(i + 1) % 4].Y - c[i].Y;
                perimeter += Math.Sqrt((dx * dx) + (dy * dy));
            }

            // getAverageModuleSize: the mean side over the marker's 6 bits plus a border bit each side.
            float cell = (float)(perimeter / 4 / 8);
            int size = relative > 0 ? Math.Min(Math.Max((int)Math.Round(relative * cell, MidpointRounding.ToEven), 2), window) : window;
            var moved = PortableImaging.CornerSubPix(image, [.. c.Select(p => new PointD(p.X, p.Y))], size, 100, 0.001);
            refined[m] = [.. moved.Select(p => new Point2f((float)p.X, (float)p.Y))];
        }

        return refined;
    }

    /// <summary>
    /// OpenCV ArUco's contour refinement, with the line fitting in managed code (entry 101). OpenCV does not return the contour each marker
    /// was found on, so it is found again the way <c>ArucoDetector</c> finds it: the same adaptive thresholds, contours and polygon test
    /// over the same windows, which are integer work and identical on every platform; the candidates in the same order, largest perimeter
    /// first and otherwise in the order found; and the first whose four corners are the marker's unrefined corners, which is the one a group
    /// of near-identical candidates keeps. <see cref="PortableImaging.RefineOnContour"/> then fits the sides.
    /// </summary>
    private static Point2f[][] RefineOnContours(Mat input, Point2f[][] corners, DetectorParameters p)
    {
        int longest = Math.Max(input.Width, input.Height);
        uint maxPerimeter = (uint)(p.MaxMarkerPerimeterRate * longest);

        // _findMarkerContours takes minSideLengthCanonicalImg as its minimum side whenever it is set, as it is here.
        uint minPerimeter = p.MinSideLengthCanonicalImg != 0 ? (uint)(4 * p.MinSideLengthCanonicalImg) : (uint)(p.MinMarkerPerimeterRate * longest);
        var candidates = new List<(Point[] Contour, Point2f[] Corners, float Perimeter)>();
        int scales = ((p.AdaptiveThreshWinSizeMax - p.AdaptiveThreshWinSizeMin) / p.AdaptiveThreshWinSizeStep) + 1;
        using var thresholded = new Mat();
        for (int s = 0; s < scales; s++)
        {
            int window = p.AdaptiveThreshWinSizeMin + (s * p.AdaptiveThreshWinSizeStep);
            Cv2.AdaptiveThreshold(input, thresholded, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.BinaryInv, window | 1, p.AdaptiveThreshConstant);
            Cv2.FindContours(thresholded, out Point[][] contours, out _, RetrievalModes.List, ContourApproximationModes.ApproxNone);
            foreach (var contour in contours)
            {
                if (contour.Length < minPerimeter || contour.Length > maxPerimeter)
                {
                    continue;
                }

                var polygon = Cv2.ApproxPolyDP(contour, contour.Length * p.PolygonalApproxAccuracyRate, true);
                if (polygon.Length != 4 || !Cv2.IsContourConvex(polygon))
                {
                    continue;
                }

                double minSideSquared = (double)longest * longest;
                for (int j = 0; j < 4; j++)
                {
                    double dx = polygon[j].X - polygon[(j + 1) % 4].X, dy = polygon[j].Y - polygon[(j + 1) % 4].Y;
                    minSideSquared = Math.Min(minSideSquared, (dx * dx) + (dy * dy));
                }

                double minCorner = contour.Length * p.MinCornerDistanceRate;
                if (minSideSquared < minCorner * minCorner)
                {
                    continue;
                }

                Point2f[] c = [.. polygon.Select(q => new Point2f(q.X, q.Y))];
                double cross = ((c[1].X - c[0].X) * (c[2].Y - c[0].Y)) - ((c[1].Y - c[0].Y) * (c[2].X - c[0].X));
                if (cross < 0)
                {
                    (c[1], c[3]) = (c[3], c[1]);
                }

                float perimeter = 0;
                for (int j = 0; j < 4; j++)
                {
                    float dx = c[j].X - c[(j + 1) % 4].X, dy = c[j].Y - c[(j + 1) % 4].Y;
                    perimeter += MathF.Sqrt((dx * dx) + (dy * dy));
                }

                candidates.Add((contour, c, perimeter));
            }
        }

        // std::stable_sort, largest perimeter first.
        var ordered = candidates.Select((c, i) => (c, i)).OrderByDescending(x => x.c.Perimeter).ThenBy(x => x.i).Select(x => x.c).ToList();
        var refined = new Point2f[corners.Length][];
        for (int m = 0; m < corners.Length; m++)
        {
            var marker = corners[m];
            var match = ordered.FirstOrDefault(c => marker.All(q => c.Corners.Contains(q)));
            if (match.Contour is null)
            {
                throw new InvalidOperationException("A detected marker's contour was not found again.");
            }

            var moved = PortableImaging.RefineOnContour([.. match.Contour.Select(q => (q.X, q.Y))], [.. marker.Select(q => new PointD(q.X, q.Y))]);
            refined[m] = [.. moved.Select(q => new Point2f((float)q.X, (float)q.Y))];
        }

        return refined;
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
        bool[] use = [.. inliers.Select(b => b != 0)];

        // Entry 101: RANSAC chooses the inliers, which the record shows is the same on every platform; the final fit over them is managed,
        // because the native refinement's arithmetic differs by platform in the last printed digit.
        return new HomographyFit(PortableImaging.RefineHomography(source, destination, use) ?? new Homography(values), use);
    }

    /// <summary>Resamples bilinearly, with <paramref name="transform"/> taking source pixels to destination pixels, and paper beyond the source.</summary>
    public GrayImage WarpPerspective(GrayImage image, Homography transform, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(transform);
        // Entry 101: managed, because the native warp made a different synthetic image on each platform before anything was detected.
        return PortableImaging.WarpPerspective(image, transform, width, height);
    }

    /// <summary>Copies a single-channel 8-bit OpenCV image into a <see cref="GrayImage"/>.</summary>
    public GrayImage Morphology(GrayImage image, MorphologyOperation operation, int radius)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var source = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size((2 * radius) + 1, (2 * radius) + 1));
        using var result = new Mat();
        Cv2.MorphologyEx(source, result, operation == MorphologyOperation.Open ? MorphTypes.Open : MorphTypes.Close, kernel);
        return Copy(result);
    }

    public IReadOnlyList<ImageBlob> FilledBlobs(GrayImage binary)
    {
        ArgumentNullException.ThrowIfNull(binary);
        using var wrapped = Mat.FromPixelData(binary.Height, binary.Width, MatType.CV_8UC1, binary.Pixels);
        using var source = wrapped.Clone();
        Cv2.FindContours(source, out Point[][] outlines, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
        using var filled = new Mat(binary.Height, binary.Width, MatType.CV_8UC1, Scalar.All(0));
        Cv2.DrawContours(filled, outlines, -1, Scalar.All(1), -1);
        using var labels = new Mat();
        using var stats = new Mat();
        using var centroids = new Mat();
        int count = Cv2.ConnectedComponentsWithStats(filled, labels, stats, centroids, PixelConnectivity.Connectivity8);
        var blobs = new List<ImageBlob>(Math.Max(0, count - 1));
        for (int i = 1; i < count; i++)
        {
            int left = stats.At<int>(i, (int)ConnectedComponentsTypes.Left), top = stats.At<int>(i, (int)ConnectedComponentsTypes.Top);
            int width = stats.At<int>(i, (int)ConnectedComponentsTypes.Width), height = stats.At<int>(i, (int)ConnectedComponentsTypes.Height);
            int area = stats.At<int>(i, (int)ConnectedComponentsTypes.Area);
            using var region = new Mat(labels, new Rect(left, top, width, height));
            using var mask = new Mat();
            Cv2.InRange(region, new Scalar(i), new Scalar(i), mask);
            Cv2.FindContours(mask, out Point[][] parts, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            var hull = Cv2.ConvexHull(parts.SelectMany(p => p));
            blobs.Add(new ImageBlob(area, left, top, width, height, [.. hull.Select(p => new PointD(p.X + left, p.Y + top))]));
        }

        return blobs;
    }

    public (PointD Shift, double Response) PhaseCorrelate(GrayImage reference, GrayImage moved)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(moved);
        using var a8 = Mat.FromPixelData(reference.Height, reference.Width, MatType.CV_8UC1, reference.Pixels);
        using var b8 = Mat.FromPixelData(moved.Height, moved.Width, MatType.CV_8UC1, moved.Pixels);
        using var a = new Mat();
        using var b = new Mat();
        a8.ConvertTo(a, MatType.CV_32FC1);
        b8.ConvertTo(b, MatType.CV_32FC1);
        using var window = new Mat();
        Cv2.CreateHanningWindow(window, new Size(reference.Width, reference.Height), MatType.CV_32FC1);
        var shift = Cv2.PhaseCorrelate(a, b, window, out double response);
        return (new PointD(shift.X, shift.Y), response);
    }

    /// <summary>
    /// QR codes located by OpenCV's WeChat detector, without its neural network models, and each decoded by the plain
    /// <see cref="QRCodeDetector"/> at the corners it found, together with whatever the plain detector finds by itself (NOTES-FROM-PLANNING.md
    /// entry 35 section 6 item 3). Neither is enough alone. On the Phase 0 scans the plain detector missed the codes on the 600 DPI scan, a
    /// tile and a photograph it was tried on. The WeChat detector found them on all three, but it hands each payload back through a UTF-8
    /// string, so every byte of a GLTD-B frame above 0x7F came back as U+FFFD and no frame passed its CRC. The plain decoder's string holds
    /// one byte per character, which Latin-1 turns back into the encoded bytes exactly: 29 of the 37 Phase 0 images gave a frame that decoded
    /// to the definition they were printed from, at full, half or quarter resolution.
    /// </summary>
    public IReadOnlyList<byte[]> ReadCodes(GrayImage image, double scale)
    {
        ArgumentNullException.ThrowIfNull(image);
        using var full = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var resized = new Mat();
        var input = full;
        if (scale is not 1.0)
        {
            Cv2.Resize(full, resized, new Size(0, 0), scale, scale, scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
            input = resized;
        }

        var texts = Read(input);

        // NOTES-FROM-PLANNING.md entry 195 section 4, question 56: on Unholy's 600 dpi scan of GL-ZERO-MIL-100Y neither detector found a
        // single code in the whole 5100 by 7013 image, at full or half resolution, and in each corner third of it the WeChat detector found
        // the code and the plain decoder read all 85 bytes, at full, half and a third of the resolution alike. Every GroupLab sheet keeps its
        // codes near the corners of the page, so where the whole image gives nothing each corner third is searched on its own.
        //
        // On a roll sheet a corner third is still a sheet of Letter or larger, with the code a small part of it, and on Linux and macOS no
        // code was found in any of them (CI on 760083c, GL-LR300-R24, R36 and R42). So where the thirds give nothing too, square corners
        // of a quarter and then an eighth of the shorter side are searched: every sheet's codes sit within about 250 dmm of their corners.
        //
        // Those three are also the only sheets longer than SheetIdentification.MaximumWorkingSide at 300 dpi, so they are only ever asked
        // for at half resolution, where a code's module is about 2.4 pixels: Windows' decoder reads that and the Linux and macOS builds do
        // not (CI on 84a256a). A corner is small whatever the image, so where the whole was shrunk the corners are cut from the full image.
        var corners = scale < 1 ? full : input;
        foreach (Func<int, int, (int W, int H)> size in new Func<int, int, (int W, int H)>[]
        {
            (w, h) => (w / 3, h / 3),
            (w, h) => (Math.Min(w, h) / 4, Math.Min(w, h) / 4),
            (w, h) => (Math.Min(w, h) / 8, Math.Min(w, h) / 8),
        })
        {
            if (texts.Count > 0)
            {
                break;
            }

            var (cw, ch) = size(corners.Width, corners.Height);
            if (cw < 64 || ch < 64)
            {
                continue;
            }

            foreach (var corner in new[] { new Rect(0, 0, cw, ch), new Rect(corners.Width - cw, 0, cw, ch), new Rect(0, corners.Height - ch, cw, ch), new Rect(corners.Width - cw, corners.Height - ch, cw, ch) })
            {
                using var part = new Mat(corners, corner);
                texts.AddRange(Read(part));
            }
        }

        return [.. texts.Select(t => System.Text.Encoding.Latin1.GetBytes(t))];
    }

    /// <summary>Every code the two detectors find in an image, each decoded by the plain decoder at the corners found; the empty ones left out.</summary>
    private static List<string> Read(Mat input)
    {
        using var locator = new WeChatQRCode("", "", "", "");
        using var decoder = new QRCodeDetector();
        var texts = new List<string>();
        locator.DetectAndDecode(input, out Point2f[][] boxes);
        foreach (var box in boxes)
        {
            texts.Add(decoder.Decode(input, box) ?? "");
        }

        if (decoder.DetectMulti(input, out Point2f[] corners) && decoder.DecodeMulti(input, corners, out string?[] decoded))
        {
            texts.AddRange(decoded.Select(t => t ?? ""));
        }

        return [.. texts.Where(t => t.Length > 0)];
    }

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
