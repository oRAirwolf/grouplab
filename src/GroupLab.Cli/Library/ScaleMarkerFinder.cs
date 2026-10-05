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

    /// <summary>
    /// The scale bars found, painted over with the surface beside them, on a copy for the corner finder: a bar laid along the target's edge,
    /// touching it, was taken for part of the target and made it an inch taller (Alan's photos on wood, 2026-10-04). The colour is the surface
    /// a little beyond the bar on the side away from the middle of the photo, where the target is.
    /// </summary>
    public static void PaintOverBars(Mat colour, MarkerFinding finding)
    {
        ArgumentNullException.ThrowIfNull(colour);
        ArgumentNullException.ThrowIfNull(finding);
        var middle = new PointD(colour.Width / 2.0, colour.Height / 2.0);
        for (int i = 0; i < finding.Fit.Bodies.Count; i++)
        {
            var body = finding.Fit.Bodies[i];
            if (body.Kind is not (MarkerKind.InchBar or MarkerKind.MetricBar))
            {
                continue;
            }

            // The strip from the outer edge of one code to the outer edge of the other and 8 mm on, whichever code was read first.
            double x0 = body.Points.Min(p => p.Model.X) - 8, x1 = body.Points.Max(p => p.Model.X) + 8;
            PointD[] strip = [new(x0, -14), new(x1, -14), new(x1, 14), new(x0, 14)];
            var outline = strip.Select(p => finding.Fit.InImage(i, p)).Select(p => new Point((int)Math.Round(p.X), (int)Math.Round(p.Y))).ToArray();
            var sides = new[] { 24.0, -24.0 }.Select(y => finding.Fit.InImage(i, new PointD((x0 + x1) / 2, y))).ToArray();
            var away = sides.OrderByDescending(p => Math.Pow(p.X - middle.X, 2) + Math.Pow(p.Y - middle.Y, 2)).First();
            int ax = Math.Clamp((int)away.X - 8, 0, colour.Width - 17), ay = Math.Clamp((int)away.Y - 8, 0, colour.Height - 17);
            using var patch = new Mat(colour, new Rect(ax, ay, 16, 16));
            Cv2.FillPoly(colour, [outline], Cv2.Mean(patch));
        }
    }

    /// <summary>
    /// Entry 375: the target's own corners where brackets lie near them, from the target's edges and never from a bracket's cut edge. Alan:
    /// "it is extremely difficult to cut the corner markers perfectly square". The brackets' printed codes give the plane in millimetres; each
    /// side's paper edge is looked for across a strip 16 mm either side of the line between two brackets' inside corners, along the middle
    /// of the side where no arm lies, and the corners are where those four edges meet. They are called found only where the four make a
    /// rectangle in the codes' plane, square to within a degree and its opposite sides equal to within 1.5 mm: the codes and the paper are
    /// the two ways that agree. Elsewhere the corners start at the brackets' inside corners, not found, for the person to drag.
    /// </summary>
    public static (PointD[] Corners, bool Found) CornersNearBrackets(Mat colour, MarkerFinding finding)
    {
        ArgumentNullException.ThrowIfNull(colour);
        ArgumentNullException.ThrowIfNull(finding);
        var near = finding.NearCorners ?? throw new ArgumentException("no brackets at the corners", nameof(finding));
        using var painted = colour.Clone();
        PaintOverBars(painted, finding);
        using var grey = new Mat();
        if (painted.Channels() == 1)
        {
            painted.CopyTo(grey);
        }
        else
        {
            Cv2.CvtColor(painted, grey, ColorConversionCodes.BGR2GRAY);
        }

        var plane = finding.Plane;
        var mm = near.Select(p => plane.ToInches(p)).Select(q => new PointD(q.X * 25.4, q.Y * 25.4)).ToArray();
        var middle = new PointD(mm.Average(p => p.X), mm.Average(p => p.Y));
        var sides = new (PointD P, PointD D)[4];
        var steps = new (int Sign, double Size, bool Bare)[4];
        for (int k = 0; k < 4; k++)
        {
            if (Edge(grey, plane, mm[k], mm[(k + 1) % 4], middle) is not { } side)
            {
                return ([.. near], false);
            }

            sides[k] = (side.P, side.D);
            steps[k] = (side.Sign, side.Size, side.Bare);
        }

        var corners = new PointD[4];
        for (int k = 0; k < 4; k++)
        {
            // Corner k is where the side ending at it meets the side starting from it.
            if (Meet(sides[(k + 3) % 4], sides[k]) is not { } c)
            {
                return ([.. near], false);
            }

            corners[k] = c;
        }

        double Length(int k) => Math.Sqrt(Math.Pow(corners[(k + 1) % 4].X - corners[k].X, 2) + Math.Pow(corners[(k + 1) % 4].Y - corners[k].Y, 2));
        bool square = Enumerable.Range(0, 4).All(k => Math.Abs((sides[(k + 3) % 4].D.X * sides[k].D.X) + (sides[(k + 3) % 4].D.Y * sides[k].D.Y)) < Math.Sin(Math.PI / 180));
        bool equal = Math.Abs(Length(0) - Length(2)) < 1.5 && Math.Abs(Length(1) - Length(3)) < 1.5;
        bool close = Enumerable.Range(0, 4).All(k => Math.Sqrt(Math.Pow(corners[k].X - mm[k].X, 2) + Math.Pow(corners[k].Y - mm[k].Y, 2)) < 25);
        // One paper on one surface steps the same way, by much the same, all round, and darker going out: on a white counter the paper's edge
        // hardly shows, and a printed frame 10 mm inside it, its white margin beside the white counter, was taken for it in the trial. A target
        // inked to its edge on a light surface is therefore never sure, and starts at the brackets.
        bool alike = steps.All(x => x.Sign < 0) && steps.Max(x => x.Size) < 1.5 * steps.Min(x => x.Size) && steps.All(x => x.Bare);
        if (!(square && equal && close && alike))
        {
            return ([.. near], false);
        }

        return ([.. corners.Select(c => plane.ToImage(new PointD(c.X / 25.4, c.Y / 25.4)))], true);
    }

    /// <summary>
    /// The paper's edge along one side, in the codes' millimetres: a strip from <paramref name="a"/> to <paramref name="b"/>, 16 mm either side
    /// of that line and 8 mm clear of each bracket's arm, straightened at 0.1 mm across and 0.5 mm along; in every 2 mm of its length the
    /// strongest steps across it; and the line most of those steps agree on, the outermost where a printed border inside the paper agrees as
    /// well; with the step's sign going outward, its median size, and whether the surface lies just outside it. Null where under 20 mm of side is clear of the arms or no line holds three fifths of the steps.
    /// </summary>
    private static (PointD P, PointD D, int Sign, double Size, bool Bare)? Edge(Mat grey, GroupLab.Core.StoreTargets.HomographyPlane plane, PointD a, PointD b, PointD middle)
    {
        const double Reach = 16, Across = 0.1, Along = 0.5, Group = 2;
        double length = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        var u = new PointD((b.X - a.X) / length, (b.Y - a.Y) / length);
        var n = new PointD(u.Y, -u.X);
        if ((((a.X + b.X) / 2) - middle.X) * n.X + ((((a.Y + b.Y) / 2) - middle.Y) * n.Y) < 0)
        {
            n = new PointD(-n.X, -n.Y);
        }

        double t0 = ScaleMarkerLayout.ArmLength + 8, t1 = length - ScaleMarkerLayout.ArmLength - 8;
        if (t1 - t0 < 20)
        {
            return null;
        }

        int cols = (int)((t1 - t0) / Along) + 1, rows = (int)(2 * Reach / Across) + 1;
        using var mapX = new Mat(rows, cols, MatType.CV_32FC1);
        using var mapY = new Mat(rows, cols, MatType.CV_32FC1);
        for (int c = 0; c < cols; c++)
        {
            double t = t0 + (c * Along);
            for (int r = 0; r < rows; r++)
            {
                double s = -Reach + (r * Across);
                var p = plane.ToImage(new PointD((a.X + (t * u.X) + (s * n.X)) / 25.4, (a.Y + (t * u.Y) + (s * n.Y)) / 25.4));
                mapX.Set(r, c, (float)p.X);
                mapY.Set(r, c, (float)p.Y);
            }
        }

        using var strip = new Mat();
        Cv2.Remap(grey, strip, mapX, mapY, InterpolationFlags.Linear, BorderTypes.Replicate);
        using var smooth = new Mat();
        strip.ConvertTo(smooth, MatType.CV_32FC1);
        Cv2.GaussianBlur(smooth, smooth, new Size(5, 7), 0);

        // Steps across the strip, each 2 mm of its length averaged, with their sign.
        int per = (int)(Group / Along), groups = cols / per;
        var profiles = new double[groups][];
        double most = 0;
        for (int g = 0; g < groups; g++)
        {
            var profile = new double[rows];
            for (int r = 1; r < rows - 1; r++)
            {
                double sum = 0;
                for (int c = g * per; c < (g + 1) * per; c++)
                {
                    sum += smooth.At<float>(r + 1, c) - smooth.At<float>(r - 1, c);
                }

                profile[r] = sum / per;
                most = Math.Max(most, Math.Abs(profile[r]));
            }

            profiles[g] = profile;
        }

        if (most < 2)
        {
            return null;
        }

        // Up to three steps a group, each refined to a tenth of a row.
        var steps = new List<(int G, double T, double S, int Sign, double Size)>();
        for (int g = 0; g < groups; g++)
        {
            var p = profiles[g];
            var peaks = new List<(double S, int Sign, double Size)>();
            for (int r = 2; r < rows - 2; r++)
            {
                double v = Math.Abs(p[r]), l = Math.Abs(p[r - 1]), h = Math.Abs(p[r + 1]);
                if (v < 0.25 * most || v < l || v < h)
                {
                    continue;
                }

                double bend = l - (2 * v) + h;
                double offset = bend < 0 ? Math.Clamp(0.5 * (l - h) / bend, -0.5, 0.5) : 0;
                peaks.Add((-Reach + ((r + offset) * Across), Math.Sign(p[r]), v));
            }

            double t = t0 + (((g * per) + ((per - 1) / 2.0)) * Along);
            steps.AddRange(peaks.OrderByDescending(q => q.Size).Take(3).Select(q => (g, t, q.S, q.Sign, q.Size)));
        }

        // Lines through two steps of one sign within three degrees of the side; a line holds the groups with a step of its sign within 0.3 mm.
        var lines = new List<(int Sign, List<(double T, double S, double Size)> Held)>();
        var tried = new HashSet<(int, int, int)>();
        for (int i = 0; i < steps.Count; i++)
        {
            for (int j = i + 1; j < steps.Count; j++)
            {
                var (p, q) = (steps[i], steps[j]);
                if (p.Sign != q.Sign || Math.Abs(q.T - p.T) < 10)
                {
                    continue;
                }

                double slope = (q.S - p.S) / (q.T - p.T), at = p.S - (slope * p.T);
                if (Math.Abs(slope) > 0.05 || !tried.Add(((int)Math.Round(at * 3), (int)Math.Round(slope * 2000), p.Sign)))
                {
                    continue;
                }

                var held = steps.Where(x => x.Sign == p.Sign && Math.Abs(x.S - (at + (slope * x.T))) < 0.3).GroupBy(x => x.G)
                    .Select(x => x.OrderBy(y => Math.Abs(y.S - (at + (slope * y.T)))).First()).Select(x => (x.T, x.S, x.Size)).ToList();
                if (held.Count >= 0.6 * groups)
                {
                    lines.Add((p.Sign, held));
                }
            }
        }

        if (lines.Count == 0)
        {
            return null;
        }

        // Each refitted by least squares on its own steps; the paper's edge is the outermost of those nearly as well held as the best.
        int best = lines.Max(l => l.Held.Count);
        var edge = lines.Where(l => l.Held.Count >= 0.8 * best).Select(line =>
        {
            var l = line.Held;
            double mt = l.Average(x => x.T), ms = l.Average(x => x.S);
            double stt = l.Sum(x => (x.T - mt) * (x.T - mt)), sts = l.Sum(x => (x.T - mt) * (x.S - ms));
            double slope = stt > 0 ? sts / stt : 0;
            return (A: ms - (slope * mt), B: slope, Mid: ms + (slope * (((t0 + t1) / 2) - mt)), line.Sign, Size: l.Select(x => x.Size).Order().ElementAt(l.Count / 2));
        }).OrderByDescending(l => l.Mid).First();

        // Just outside the paper's edge is the surface itself: the band 0.8 to 3 mm outside the line must look like the strip's outer 4 mm, which
        // lies beyond any gap the trial tried. Outside a printed frame is the paper's white margin, which seldom matches the surface so closely.
        double outside = 0, beyond = 0;
        int Row(double at) => Math.Clamp((int)Math.Round((at + Reach) / Across), 0, rows - 1);
        for (int c = 0; c < cols; c++)
        {
            double line = edge.A + (edge.B * (t0 + (c * Along)));
            for (int r = Row(line + 0.8); r <= Row(line + 3); r++)
            {
                outside += smooth.At<float>(r, c) / (Row(line + 3) - Row(line + 0.8) + 1);
            }

            for (int r = Row(Reach - 4); r < rows; r++)
            {
                beyond += smooth.At<float>(r, c) / (rows - Row(Reach - 4));
            }
        }

        bool bare = edge.A + (edge.B * t1) < Reach - 7 && edge.A + (edge.B * t0) < Reach - 7 && Math.Abs(outside - beyond) / cols < Math.Max(4, 0.1 * edge.Size * 7);

        // Back to the plane: the line's point at t0, and its direction.
        double s0 = edge.A + (edge.B * t0), norm = Math.Sqrt(1 + (edge.B * edge.B));
        return (new PointD(a.X + (t0 * u.X) + (s0 * n.X), a.Y + (t0 * u.Y) + (s0 * n.Y)), new PointD((u.X + (edge.B * n.X)) / norm, (u.Y + (edge.B * n.Y)) / norm), edge.Sign, edge.Size, bare);
    }

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
    public static CardSighting? Card(GrayImage grey, IReadOnlyList<DetectedMarker>? markers = null, Action<string>? trace = null) =>
        CardAt(grey, markers, trace, 1600, 30, 90) ?? ByStripe(grey, markers, trace);

    /// <summary>
    /// A light card lying on white paper (Alan's two photos on the kitchen table, 2026-10-04): its own edges are faint and the target's grid
    /// lines run into them, so its outline never closes. Its magnetic stripe does not fade: a dark band from one short edge of the card to the
    /// other. So the stripe gives the short edges and the card's direction; the long edges are the strongest steps parallel to it, one within
    /// a few millimetres of the stripe, the other a card's height across; the corners are refined at full size as for any card, and the
    /// result must still run 1.38 to 1.84 to 1.
    /// </summary>
    private static CardSighting? ByStripe(GrayImage grey, IReadOnlyList<DetectedMarker>? markers, Action<string>? trace)
    {
        using var full = Mat.FromPixelData(grey.Height, grey.Width, MatType.CV_8UC1, grey.Pixels);
        double f = Math.Min(1, 1600.0 / Math.Max(grey.Width, grey.Height));
        using var small = new Mat();
        Cv2.Resize(full, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        var codes = (markers ?? []).Select(m => m.Corners.Select(c => new PointD(c.X * f, c.Y * f)).ToArray()).ToList();

        // The stripe is near black; a dark print round it (the Eze-Scorer's dark green) joins it at a lighter threshold, so darker ones follow.
        var all = new List<Point[]>();
        foreach (int level in new[] { 70, 45, 30 })
        {
            using var dark = new Mat();
            Cv2.Threshold(small, dark, level, 255, ThresholdTypes.BinaryInv);
            Cv2.FindContours(dark, out Point[][] found, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            all.AddRange(found);
        }

        foreach (var contour in all.OrderByDescending(c => Cv2.ContourArea(c)))
        {
            var box = Cv2.MinAreaRect(contour);
            double longer = Math.Max(box.Size.Width, box.Size.Height), shorter = Math.Min(box.Size.Width, box.Size.Height);
            if (shorter < 4 || longer / shorter is < 5 or > 12 || Cv2.ContourArea(contour) < 0.85 * longer * shorter || longer < 0.05 * Math.Max(small.Width, small.Height))
            {
                if (longer > 0.05 * Math.Max(small.Width, small.Height))
                {
                    trace?.Invoke($"dark {longer:0}x{shorter:0} fill {Cv2.ContourArea(contour) / Math.Max(1, longer * shorter):0.00}");
                }

                continue;
            }

            // The stripe's axis, its two ends, and the normal across it.
            double angle = (box.Size.Width >= box.Size.Height ? box.Angle : box.Angle + 90) * Math.PI / 180;
            var u = new PointD(Math.Cos(angle), Math.Sin(angle));
            var n = new PointD(-u.Y, u.X);
            var centre = new PointD(box.Center.X, box.Center.Y);
            if (codes.Any(code => code.Any(p => Math.Abs(((p.X - centre.X) * u.X) + ((p.Y - centre.Y) * u.Y)) < longer && Math.Abs(((p.X - centre.X) * n.X) + ((p.Y - centre.Y) * n.Y)) < longer)))
            {
                continue;
            }

            double mm = longer / ScaleMarkerLayout.CardWidth, height = ScaleMarkerLayout.CardHeight * mm;

            // The step across lines parallel to the stripe at offset t (pixels along n from the stripe's middle), summed along its length.
            double Step(double t)
            {
                double sum = 0;
                for (int i = 1; i < 20; i++)
                {
                    double a = (i / 20.0) - 0.5;
                    var p = new PointD(centre.X + (u.X * a * longer * 0.9) + (n.X * t), centre.Y + (u.Y * a * longer * 0.9) + (n.Y * t));
                    sum += Math.Abs(SampleMat(small, p.X + n.X, p.Y + n.Y) - SampleMat(small, p.X - n.X, p.Y - n.Y));
                }

                return sum / 19;
            }

            // The near long edge within 1 to 9 mm beyond the stripe's side, on whichever side it is clearer; the far one exactly a card's height
            // across, since the stripe's length is the card's width and the far edge, out on the target's print, is the one a grid line can
            // take (Alan's Rigid crosshair: searched for, it made the card 1.54 to 1 and the target 1.7 percent small).
            double half = shorter / 2;
            (double At, double Strength) Strongest(double sign, double from, double to)
            {
                (double, double) best = (from, -1);
                for (double t = from; t <= to; t += 0.5)
                {
                    double s = Step(sign * t);
                    if (s > best.Item2)
                    {
                        best = (t, s);
                    }
                }

                return best;
            }

            var chosen = new[] { 1.0, -1.0 }.Select(sign =>
            {
                var near = Strongest(sign, half + (1 * mm), half + (9 * mm));
                return (NearT: sign * near.At, FarT: sign * (near.At - height), Score: near.Strength);
            }).MaxBy(c => c.Score);

            PointD At(double along, double across) => new((centre.X + (u.X * along) + (n.X * across)) / f, (centre.Y + (u.Y * along) + (n.Y * across)) / f);
            PointD[] rough = [At(-longer / 2, chosen.NearT), At(longer / 2, chosen.NearT), At(longer / 2, chosen.FarT), At(-longer / 2, chosen.FarT)];
            // Its own shape by construction, so no ratio test: the stripe must lie on something card-sized and light, which its near edge's
            // step says.
            trace?.Invoke($"stripe {longer:0}x{shorter:0}, near edge step {chosen.Score:0}");
            if (chosen.Score >= 8)
            {
                return new CardSighting(Ordered(rough)) { FromStripe = true };
            }
        }

        return null;
    }

    private static double SampleMat(Mat m, double x, double y)
    {
        int xi = Math.Clamp((int)Math.Round(x), 0, m.Width - 1), yi = Math.Clamp((int)Math.Round(y), 0, m.Height - 1);
        return m.At<byte>(yi, xi);
    }

    private static CardSighting? CardAt(GrayImage grey, IReadOnlyList<DetectedMarker>? markers, Action<string>? trace, double most, int low, int high)
    {
        ArgumentNullException.ThrowIfNull(grey);
        using var full = Mat.FromPixelData(grey.Height, grey.Width, MatType.CV_8UC1, grey.Pixels);
        double f = Math.Min(1, most / Math.Max(grey.Width, grey.Height));
        using var small = new Mat();
        Cv2.Resize(full, small, new Size(0, 0), f, f, InterpolationFlags.Area);
        Cv2.GaussianBlur(small, small, new Size(5, 5), 0);
        using var edges = new Mat();
        Cv2.Canny(small, edges, low, high);
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
