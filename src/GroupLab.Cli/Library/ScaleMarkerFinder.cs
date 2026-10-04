using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;
using OpenCvSharp;

namespace GroupLab.Cli.Library;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 365 section 0: the one marker finder, on the computer and the phone. It reads every tag36h11 code in the photo
/// at four expected sizes (a bracket's code beside a 12 inch target and a bar's beside a poster differ ten times), keeps those in the range
/// kept for markers, and looks for a bank card; <see cref="ScaleMarkerReading"/> turns what it found into a scale.
/// </summary>
public static class ScaleMarkerFinder
{
    /// <summary>What was found: the markers' codes, and a card's corners and the outline blanked from the photo.</summary>
    public sealed record Sighting(IReadOnlyList<DetectedMarker> Markers, CardSighting? Card, IReadOnlyList<PointD>? Blanked);

    /// <summary>
    /// The codes in <paramref name="grey"/>, and a card in <paramref name="colour"/> where one is given. A card found is blanked at once in
    /// every copy given (entry 365 section D: never kept, shown in a saved picture, logged or sent), before anything else sees the photo.
    /// </summary>
    public static Sighting Find(GrayImage grey, Mat? colour = null, bool lookForCard = true, params GrayImage[] alsoBlank)
    {
        ArgumentNullException.ThrowIfNull(grey);
        var markers = Codes(grey);
        if (!lookForCard)
        {
            return new Sighting(markers, null, null);
        }

        // Markers' own codes are not a card; a card is looked for only away from them.
        var card = Card(grey, markers);
        if (card is null)
        {
            return new Sighting(markers, null, null);
        }

        var outline = Grown(card.Corners, 0.08);
        Blank(grey, outline);
        foreach (var other in alsoBlank)
        {
            Blank(other, outline);
        }

        if (colour is not null)
        {
            Cv2.FillPoly(colour, [outline.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray()], new Scalar(128, 128, 128));
        }

        return new Sighting(markers, card, outline);
    }

    /// <summary>The finding for a photo: <see cref="Find"/>, then <see cref="ScaleMarkerReading.Read"/>.</summary>
    public static (MarkerFinding? Finding, string? Said, Sighting Sighting) Read(GrayImage grey, Mat? colour, PrinterProfile? printer, IReadOnlyList<ScaleBoard> boards, params GrayImage[] alsoBlank)
    {
        var sighting = Find(grey, colour, true, alsoBlank);
        var (finding, said) = ScaleMarkerReading.Read(sighting.Markers, sighting.Card, printer, boards);
        return (finding, said, sighting);
    }

    /// <summary>
    /// Entry 365 section C, "Measure a board": a photo of the board with any GroupLab sheet on it and the four stickers of one set round it.
    /// The sheet's plane, corrected by the printer check where there is one, takes each sticker's corners to inches; the board is good to what
    /// <see cref="ScaleBoard.MeasuredDoubt"/> says. The board, or null, and the sentence for the screen either way.
    /// </summary>
    public static (ScaleBoard? Board, string Said) MeasureBoard(string path, IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> sheets, PrinterProfile? printer,
        string name, DateOnly today, double? mostMegapixels = null)
    {
        GrayImage grey, value;
        ImageMetadata metadata;
        try
        {
            var (g, m) = ImageLoader.Load(path, mostMegapixels);
            var (v, _) = ImageLoader.LoadMaxChannel(path, mostMegapixels);
            (grey, value, metadata) = (Upright.Apply(g, m.Orientation), Upright.Apply(v, m.Orientation), m);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException or UnauthorizedAccessException)
        {
            return (null, "That file is not a picture GroupLab can read.");
        }

        double printScale = printer is { } p ? (p.Across + p.Down) / 2 : 1;
        if (TargetReferenceMaker.ReadGroupLabSheet(grey, value, metadata, sheets, printScale) is not { } sheet)
        {
            return (null, ScaleMarkerWords.NoBoardSheet);
        }

        var (board, why) = ScaleBoard.Measure(name, Codes(grey), sheet.Plane, 0, today);
        if (board is null)
        {
            return (null, why ?? ScaleMarkerWords.NoStickers);
        }

        var centres = board.Stickers.Values.Select(c => new PointD(c.Average(q => q.X), c.Average(q => q.Y))).ToList();
        double across = centres.SelectMany(a => centres.Select(b => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)))).Max() / 25.4;
        board = board with { Uncertainty = ScaleBoard.MeasuredDoubt(sheet.ResidualInches, sheet.SheetInches, across, printer is not null) };
        return (board, ScaleMarkerWords.BoardSaved(board) + (printer is null ? " " + ScaleMarkerWords.BoardNoPrinterCheck : ""));
    }

    /// <summary>
    /// Entry 365: a target marked by hand on the phone, before its page opens. Markers in the photo give the marking its scale; a card is
    /// blanked into a copy in <paramref name="blankFolder"/>, and the marking is made on the copy, so the card is never kept, shown or sent.
    /// The marking to start from (null where nothing was found), what to say, and the picture to mark.
    /// </summary>
    public static (MarkingState? State, string? Said, string Path) ForMarking(string path, int? orientation, PrinterProfile? printer, IReadOnlyList<ScaleBoard> boards, string blankFolder)
    {
        GrayImage grey, value;
        try
        {
            (grey, _) = ImageLoader.Load(path);
            (value, _) = ImageLoader.LoadMaxChannel(path);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException or UnauthorizedAccessException)
        {
            return (null, null, path);
        }

        var (finding, said, sighting) = Read(grey, null, printer, boards, value);
        string use = path;
        if (sighting.Blanked is { } outline)
        {
            try
            {
                Directory.CreateDirectory(blankFolder);
                use = Path.Combine(blankFolder, Path.GetFileNameWithoutExtension(path) + "-card-blanked.png");
                File.WriteAllBytes(use, BlankedFile(path, [.. outline.Select(p => new PointD(p.X / grey.Width, p.Y / grey.Height))], 1));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A card that could not be blanked is not used, and nothing is kept of it.
                return (null, said, path);
            }
        }

        if (finding is null)
        {
            return (null, said, use);
        }

        double u = finding.UncertaintyAt(null);
        var scale = MarkerReference.Upright(finding.Plane.ImageToInches, new PointD(grey.Width / 2.0, grey.Height / 2.0), finding.Says(u));
        var state = MarkingState.Empty with { ImagePath = use, ExifOrientation = orientation, ViewQuarterTurns = ViewRotation.FromExifOrientation(orientation), Scale = scale };
        return (state, finding.Says(u) + (sighting.Blanked is null ? "" : " " + ScaleMarkerWords.CardBlanked), use);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> CardChecked = new(StringComparer.Ordinal);

    /// <summary>
    /// Entry 365 section D, without exception: a picture is never sent with a bank card in it, whatever was or was not done with it on the
    /// screen. Checked once a picture, at a reduced size; a picture that cannot be read is taken to have none (it cannot be sent either).
    /// </summary>
    public static bool ContainsCard(string path) => CardChecked.GetOrAdd(path, p =>
    {
        try
        {
            var (g, _) = ImageLoader.Load(p, 4);
            return Card(g) is not null;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or OpenCVException or UnauthorizedAccessException)
        {
            return false;
        }
    });

    /// <summary>Every marker code in the photo, each once, read at the finest size that found it.</summary>
    public static IReadOnlyList<DetectedMarker> Codes(GrayImage grey)
    {
        ArgumentNullException.ThrowIfNull(grey);
        var backend = new OpenCvSharpBackend();
        var found = new Dictionary<int, DetectedMarker>();
        int longest = Math.Max(grey.Width, grey.Height);
        foreach (var (side, down) in new[] { (14.0, 1), (30.0, 1), (70.0, 1), (200.0, longest > 3000 ? 2 : 1) })
        {
            var detection = backend.DetectMarkers(grey, new MarkerDetectionOptions(MarkerFamily.AprilTag36h11, side, DownsampleFactor: down));
            foreach (var m in detection.Markers.Where(m => ScaleMarkerLayout.IsMarker(m.Id)))
            {
                found.TryAdd(m.Id, m);
            }
        }

        return [.. found.Values.OrderBy(m => m.Id)];
    }

    /// <summary>
    /// A bank card's four corners, where its straight sides meet, or null. A card is a quadrilateral whose sides run about 1.59 to 1, its opposite sides within 15 percent of each other and its outline filling it, a
    /// fifth of a percent to eight percent of the photo, nearly filled by its outline, and with rounded corners: the outline stands off each
    /// sharp corner by about 1.5 percent of the card's width, where a printed box's meets it. That last is what tells a card from a label.
    /// </summary>
    public static CardSighting? Card(GrayImage grey, IReadOnlyList<DetectedMarker>? markers = null, Action<string>? trace = null)
    {
        ArgumentNullException.ThrowIfNull(grey);
        using var full = Mat.FromPixelData(grey.Height, grey.Width, MatType.CV_8UC1, grey.Pixels);
        double f = Math.Min(1, 1600.0 / Math.Max(grey.Width, grey.Height));
        using var small = new Mat();
        Cv2.Resize(full, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        Cv2.GaussianBlur(small, small, new Size(5, 5), 0);
        using var edges = new Mat();
        Cv2.Canny(small, edges, 30, 90);
        using (var k = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
        {
            Cv2.Dilate(edges, edges, k);
        }

        Cv2.FindContours(edges, out Point[][] contours, out _, RetrievalModes.List, ContourApproximationModes.ApproxNone);
        double area = small.Width * small.Height;
        var codes = (markers ?? []).Select(m => m.Corners.Select(c => new PointD(c.X * f, c.Y * f)).ToArray()).ToList();
        (PointD[] Corners, double Score)? best = null;
        foreach (var contour in contours)
        {
            var hull = Cv2.ConvexHull(contour);
            double hullArea = Cv2.ContourArea(hull);
            if (hullArea < 0.001 * area || hullArea > 0.08 * area)
            {
                continue;
            }

            var quad = Quadrilateral(hull, trace);
            if (quad is null)
            {
                trace?.Invoke($"no four corners, hull {hullArea / area:0.0000}");
                continue;
            }

            double quadArea = Math.Abs(Cv2.ContourArea(quad.Select(p => new Point2f((float)p.X, (float)p.Y)).ToArray()));
            if (hullArea < 0.95 * quadArea)
            {
                trace?.Invoke($"fill {hullArea / quadArea:0.000}");
                continue;
            }

            // A card's outline fills its four sides all but its rounded corners, and its opposite sides are nearly equal even tilted: a
            // silhouette's head and shoulders, a trapezoid with a rounded top, fail one or the other.
            double filled = Math.Abs(Cv2.ContourArea(contour)) / quadArea;
            double across = Math.Min(Side(quad, 0), Side(quad, 2)) / Math.Max(Side(quad, 0), Side(quad, 2)), down = Math.Min(Side(quad, 1), Side(quad, 3)) / Math.Max(Side(quad, 1), Side(quad, 3));
            if (filled < 0.93 || Math.Min(across, down) < 0.85)
            {
                trace?.Invoke($"filled {filled:0.000}, sides {across:0.00} {down:0.00}");
                continue;
            }

            double a = Side(quad, 0) + Side(quad, 2), b = Side(quad, 1) + Side(quad, 3);
            double ratio = Math.Max(a, b) / Math.Min(a, b);
            if (ratio is < 1.35 or > 1.85)
            {
                trace?.Invoke($"ratio {ratio:0.00}");
                continue;
            }

            // Rounded corners: the hull stands off each sharp corner by about r (sqrt 2 - 1), r being 3.7 percent of the card's width.
            double width = Math.Max(a, b) / 2;
            int rounded = quad.Count(c => hull.Min(h => Math.Sqrt(Math.Pow(h.X - c.X, 2) + Math.Pow(h.Y - c.Y, 2))) / width is > 0.005 and < 0.035);
            if (rounded < 3)
            {
                trace?.Invoke($"rounded {rounded}: " + string.Join(", ", quad.Select(c => (hull.Min(h => Math.Sqrt(Math.Pow(h.X - c.X, 2) + Math.Pow(h.Y - c.Y, 2))) / width).ToString("0.0000"))));
                continue;
            }

            // A bracket's L reduces to four corners too; nothing with a marker's code on it or just beside it is a card.
            var near = Grown(quad, 0.15);
            if (codes.Any(code => code.Any(p => Inside(near, p))))
            {
                continue;
            }

            double score = -Math.Abs(ratio - (ScaleMarkerLayout.CardWidth / ScaleMarkerLayout.CardHeight)) + (hullArea / area);
            if (best is null || score > best.Value.Score)
            {
                best = (quad, score);
            }
        }

        if (best is not { } found)
        {
            return null;
        }

        var rough = Ordered(found.Corners).Select(p => new PointD(p.X / f, p.Y / f)).ToArray();
        var corners = Refined(grey, rough) ?? rough;

        // Its sides again, found to a tenth of a pixel: a card tilted up to 30 degrees runs 1.38 to 1.84 to 1. A printed box on a target at
        // 1.34 passed on the reduced picture's corners (the NTC ST-4's).
        double longer = Side(corners, 0) + Side(corners, 2), shorter = Side(corners, 1) + Side(corners, 3);
        double sides = Math.Max(longer, shorter) / Math.Min(longer, shorter);
        if (sides is < 1.38 or > 1.84)
        {
            trace?.Invoke($"refined ratio {sides:0.00}");
            return null;
        }

        return new CardSighting(corners);
    }

    /// <summary>
    /// The card's sides found again at full size: along the middle of each side, the strongest brightness step across it to a tenth of a pixel,
    /// a straight line through those, and the corners where the lines meet. The edges found on the reduced picture were thickened to be
    /// closed, and lay a pixel or two outside the card, which is two percent on a small card.
    /// </summary>
    private static PointD[]? Refined(GrayImage grey, PointD[] corners)
    {
        var lines = new (PointD P, PointD D)[4];
        for (int i = 0; i < 4; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % 4];
            double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
            double ux = (b.X - a.X) / length, uy = (b.Y - a.Y) / length, nx = -uy, ny = ux;
            double reach = Math.Max(10, 0.06 * length);
            var edge = new List<Point2f>();
            for (int k = 0; k <= 40; k++)
            {
                double t = 0.15 + (0.7 * k / 40);
                double px = a.X + (ux * length * t), py = a.Y + (uy * length * t);
                double bestStep = 0, bestAt = double.NaN;
                double previous = Sample(grey, px - (nx * (reach + 0.5)), py - (ny * (reach + 0.5)));
                var steps = new List<double>();
                for (double o = -reach; o <= reach; o += 0.5)
                {
                    double here = Sample(grey, px + (nx * o), py + (ny * o));
                    steps.Add(Math.Abs(here - previous));
                    previous = here;
                }

                for (int j = 1; j < steps.Count - 1; j++)
                {
                    if (steps[j] > bestStep && steps[j] >= steps[j - 1] && steps[j] >= steps[j + 1])
                    {
                        double denominator = steps[j - 1] - (2 * steps[j]) + steps[j + 1];
                        double offset = denominator == 0 ? 0 : 0.5 * (steps[j - 1] - steps[j + 1]) / denominator;
                        bestStep = steps[j];
                        bestAt = -reach + ((j + offset - 0.5) * 0.5);
                    }
                }

                if (bestStep > 8 && !double.IsNaN(bestAt))
                {
                    edge.Add(new Point2f((float)(px + (nx * bestAt)), (float)(py + (ny * bestAt))));
                }
            }

            if (edge.Count < 12)
            {
                return null;
            }

            var line = Cv2.FitLine(edge.ToArray(), DistanceTypes.Huber, 0, 0.01, 0.01);
            lines[i] = (new PointD(line.X1, line.Y1), new PointD(line.Vx, line.Vy));
        }

        var refined = new PointD[4];
        for (int i = 0; i < 4; i++)
        {
            if (Meet(lines[(i + 3) % 4], lines[i]) is not { } c || Math.Sqrt(Math.Pow(c.X - corners[i].X, 2) + Math.Pow(c.Y - corners[i].Y, 2)) > 16)
            {
                return null;
            }

            refined[i] = c;
        }

        return refined;
    }

    private static double Sample(GrayImage g, double x, double y)
    {
        int x0 = Math.Clamp((int)Math.Floor(x), 0, g.Width - 2), y0 = Math.Clamp((int)Math.Floor(y), 0, g.Height - 2);
        double fx = Math.Clamp(x - x0, 0, 1), fy = Math.Clamp(y - y0, 0, 1);
        return ((1 - fy) * (((1 - fx) * g[x0, y0]) + (fx * g[x0 + 1, y0]))) + (fy * (((1 - fx) * g[x0, y0 + 1]) + (fx * g[x0 + 1, y0 + 1])));
    }

    /// <summary>Fills the outline in a grey copy with mid grey.</summary>
    public static void Blank(GrayImage image, IReadOnlyList<PointD> outline)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(outline);
        using var mat = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        Cv2.FillPoly(mat, [outline.Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray()], new Scalar(128));
    }

    /// <summary>A photo's file with the card's outline blanked, as a lossless PNG, for the copy GroupLab keeps, shows and sends instead.</summary>
    public static byte[] BlankedFile(string path, IReadOnlyList<PointD> outline, int orientation)
    {
        using var raw = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
        using var upright = UprightMat.Apply(raw, orientation);
        Cv2.FillPoly(upright, [outline.Select(p => new Point((int)Math.Round(p.X * upright.Width), (int)Math.Round(p.Y * upright.Height))).ToArray()], new Scalar(128, 128, 128));
        Cv2.ImEncode(".png", upright, out byte[] png);
        return png;
    }

    /// <summary>The outline grown about its centre, so the rounded corners and the card's edge go too.</summary>
    public static PointD[] Grown(IReadOnlyList<PointD> corners, double by)
    {
        ArgumentNullException.ThrowIfNull(corners);
        double cx = corners.Average(p => p.X), cy = corners.Average(p => p.Y);
        return [.. corners.Select(p => new PointD(cx + ((p.X - cx) * (1 + by)), cy + ((p.Y - cy) * (1 + by))))];
    }

    /// <summary>The hull reduced to four corners where its four longest straight runs meet, or null where it has no four.</summary>
    private static PointD[]? Quadrilateral(Point[] hull, Action<string>? trace = null)
    {
        double perimeter = Cv2.ArcLength(hull, true);
        for (double e = 0.01; e <= 0.06; e += 0.005)
        {
            var approx = Cv2.ApproxPolyDP(hull, e * perimeter, true);
            trace?.Invoke($"e {e:0.000}: {approx.Length}");
            if (approx.Length == 4 && Cv2.IsContourConvex(approx))
            {
                // Each side refitted to the hull's points along its middle, away from the rounded corners: first every point within 4 percent
                // of the side's length of the chord, because the reduced corners sit on the arcs and the chord runs a few pixels off the edge,
                // then again with only those within 2 pixels of that first line.
                var lines = new (PointD P, PointD D)[4];
                var dense = Dense(hull);
                for (int i = 0; i < 4; i++)
                {
                    var a = approx[i];
                    var b = approx[(i + 1) % 4];
                    double band = Math.Max(2, 0.04 * Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2)));
                    var along = dense.Where(p => Near(p, new PointD(a.X, a.Y), new PointD(b.X - a.X, b.Y - a.Y), band, chord: true)).ToArray();
                    if (along.Length < 4)
                    {
                        trace?.Invoke($"side {i}: {along.Length} points of {dense.Count}");
                        return null;
                    }

                    var first = Fit(along);
                    var close = along.Where(p => Near(p, first.P, first.D, 2, chord: false)).ToArray();
                    lines[i] = close.Length >= 4 ? Fit(close) : first;
                }

                var corners = new PointD[4];
                for (int i = 0; i < 4; i++)
                {
                    if (Meet(lines[(i + 3) % 4], lines[i]) is not { } c)
                    {
                        trace?.Invoke("parallel");
                        return null;
                    }

                    corners[i] = c;
                }

                return corners;
            }
        }

        return null;
    }

    /// <summary>The hull's edges walked a pixel at a time, so a side with only its two ends in the hull still has points along it.</summary>
    private static List<PointD> Dense(Point[] hull)
    {
        var points = new List<PointD>();
        for (int i = 0; i < hull.Length; i++)
        {
            var a = hull[i];
            var b = hull[(i + 1) % hull.Length];
            int n = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2))));
            for (int s = 0; s < n; s++)
            {
                points.Add(new PointD(a.X + ((b.X - a.X) * s / (double)n), a.Y + ((b.Y - a.Y) * s / (double)n)));
            }
        }

        return points;
    }

    /// <summary>
    /// A point within <paramref name="band"/> pixels of the line through <paramref name="at"/> along <paramref name="along"/>; for a chord,
    /// only on its middle 70 percent, away from the corners.
    /// </summary>
    private static bool Near(PointD p, PointD at, PointD along, double band, bool chord)
    {
        double l2 = (along.X * along.X) + (along.Y * along.Y);
        double t = (((p.X - at.X) * along.X) + ((p.Y - at.Y) * along.Y)) / l2;
        double d = Math.Abs(((p.X - at.X) * along.Y) - ((p.Y - at.Y) * along.X)) / Math.Sqrt(l2);
        return (!chord || t is > 0.15 and < 0.85) && d <= band;
    }

    private static (PointD P, PointD D) Fit(PointD[] points)
    {
        var line = Cv2.FitLine(points.Select(p => new Point2f((float)p.X, (float)p.Y)).ToArray(), DistanceTypes.Huber, 0, 0.01, 0.01);
        return (new PointD(line.X1, line.Y1), new PointD(line.Vx, line.Vy));
    }

    private static PointD? Meet((PointD P, PointD D) a, (PointD P, PointD D) b)
    {
        double cross = (a.D.X * b.D.Y) - (a.D.Y * b.D.X);
        if (Math.Abs(cross) < 1e-6)
        {
            return null;
        }

        double t = (((b.P.X - a.P.X) * b.D.Y) - ((b.P.Y - a.P.Y) * b.D.X)) / cross;
        return new PointD(a.P.X + (t * a.D.X), a.P.Y + (t * a.D.Y));
    }

    /// <summary>Top left first, clockwise, as the photo shows it.</summary>
    private static PointD[] Ordered(PointD[] c)
    {
        double cx = c.Average(p => p.X), cy = c.Average(p => p.Y);
        var round = c.OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx)).ToArray();
        int first = Array.IndexOf(round, round.OrderBy(p => p.X + p.Y).First());
        return [.. Enumerable.Range(0, 4).Select(i => round[(first + i) % 4])];
    }

    private static double Side(PointD[] q, int i) => Math.Sqrt(Math.Pow(q[(i + 1) % 4].X - q[i].X, 2) + Math.Pow(q[(i + 1) % 4].Y - q[i].Y, 2));

    private static bool Inside(IReadOnlyList<PointD> polygon, PointD p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) && p.X < ((polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y)) + polygon[i].X)
            {
                inside = !inside;
            }
        }

        return inside;
    }
}
