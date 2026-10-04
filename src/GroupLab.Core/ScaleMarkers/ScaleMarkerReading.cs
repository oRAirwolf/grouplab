using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>
/// Entry 365 section C: a target board with four code stickers on it, where each sticker is measured once from a photo with a GroupLab sheet on
/// the board, saved under a name. Each sticker's four corners are kept in the board's millimetres.
/// </summary>
public sealed record ScaleBoard(string Name, char Set, DateOnly MeasuredOn, double Uncertainty, IReadOnlyDictionary<int, PointD[]> Stickers)
{
    /// <summary>How far, millimetres root mean square, the stickers may move from where they were measured before the board is measured again.</summary>
    public const double Agreement = 0.3;

    public JsonObject ToJson() => new()
    {
        ["name"] = Name,
        ["set"] = Set.ToString(),
        ["measuredOn"] = MeasuredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["uncertainty"] = Math.Round(Uncertainty, 5),
        ["stickers"] = new JsonObject(Stickers.OrderBy(s => s.Key).Select(s => new KeyValuePair<string, JsonNode?>(
            s.Key.ToString(CultureInfo.InvariantCulture), new JsonArray([.. s.Value.SelectMany(p => new[] { Math.Round(p.X, 3), Math.Round(p.Y, 3) }).Select(v => (JsonNode)JsonValue.Create(v))])))),
    };

    public static ScaleBoard? FromJson(JsonNode? node)
    {
        try
        {
            if (node is not JsonObject o || (string?)o["name"] is not { Length: > 0 } name || (string?)o["set"] is not { Length: 1 } set
                || o["stickers"] is not JsonObject stickers)
            {
                return null;
            }

            var tags = new Dictionary<int, PointD[]>();
            foreach (var (key, value) in stickers)
            {
                var v = value!.AsArray().Select(n => (double)n!).ToArray();
                if (v.Length != 8 || !int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || !ScaleMarkerLayout.IsMarker(id))
                {
                    return null;
                }

                tags[id] = [new(v[0], v[1]), new(v[2], v[3]), new(v[4], v[5]), new(v[6], v[7])];
            }

            return new ScaleBoard(name, set[0], DateOnly.ParseExact((string)o["measuredOn"]!, "yyyy-MM-dd", CultureInfo.InvariantCulture), (double?)o["uncertainty"] ?? 0.002, tags);
        }
        catch (Exception e) when (e is FormatException or InvalidOperationException or NullReferenceException)
        {
            return null;
        }
    }

    /// <summary>
    /// How well a board is measured from a GroupLab sheet on it, at two standard deviations: the sheet's registration residual carried out to
    /// the stickers with the square of their reach beyond it, as <see cref="StoreTargets.TargetStraightening.FromGroupLabSheet"/> does for a
    /// target, and the sheet's print scale, 0.1 percent with a printer check and 1.5 percent without.
    /// </summary>
    public static double MeasuredDoubt(double residualInches, double sheetInches, double boardInches, bool printerChecked)
    {
        double reach = Math.Max(1, boardInches / sheetInches);
        double registration = Math.Sqrt(2) * reach * reach * Math.Max(residualInches, 0.01) / sheetInches, print = printerChecked ? 0.001 : 0.015;
        return 2 * Math.Sqrt((registration * registration) + Math.Pow(print / 2, 2));
    }

    /// <summary>
    /// The board measured from one photo: the four stickers of one set, each corner taken to inches by the GroupLab sheet's plane
    /// (<paramref name="sheet"/>), which is good to <paramref name="sheetUncertainty"/>. Null, with the reason, where the photo does not show
    /// all four of one set.
    /// </summary>
    public static (ScaleBoard? Board, string? Why) Measure(string name, IReadOnlyList<DetectedMarker> found, ITargetPlane sheet, double sheetUncertainty, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(sheet);
        var stickers = found.Where(m => ScaleMarkerLayout.Tag(m.Id) is { Kind: MarkerKind.Sticker }).GroupBy(m => ScaleMarkerLayout.SetOf(m.Id)).ToList();
        if (stickers.FirstOrDefault(g => g.Select(m => m.Id).Distinct().Count() == 4) is not { } set)
        {
            return (null, stickers.Count == 0
                ? ScaleMarkerWords.NoStickers
                : string.Create(CultureInfo.InvariantCulture, $"Only {stickers.Max(g => g.Select(m => m.Id).Distinct().Count())} of the four stickers of one set were found. All four of one set need to show, with the GroupLab sheet, in the photo."));
        }

        var tags = set.GroupBy(m => m.Id).ToDictionary(g => g.Key, g => g.First().Corners.Select(c => sheet.ToInches(c)).Select(p => new PointD(p.X * 25.4, p.Y * 25.4)).ToArray());
        return (new ScaleBoard(string.IsNullOrWhiteSpace(name) ? "Board 1" : name.Trim(), set.Key, today, sheetUncertainty, tags), null);
    }
}

/// <summary>Entry 365 section D: a bank card's four corners in the photo, where its straight sides would meet, top left first, clockwise.</summary>
public sealed record CardSighting(IReadOnlyList<PointD> Corners);

/// <summary>What the markers in one photo gave: the plane, how far it can be trusted, and what was found and used.</summary>
public sealed class MarkerFinding
{
    internal MarkerFinding(MarkerFit fit, double print, double extra, IReadOnlyList<(MarkerKind Kind, int Piece)> used, bool scaleOnly,
        IReadOnlyList<(PointD A, PointD B, double Inches)> lengths, IReadOnlyList<PointD>? corners, string? board, string? boardSaid, bool printerChecked)
    {
        Fit = fit;
        Print = print;
        Extra = extra;
        Used = used;
        ScaleOnly = scaleOnly;
        Lengths = lengths;
        TargetCorners = corners;
        Board = board;
        BoardSaid = boardSaid;
        PrinterChecked = printerChecked;
        Plane = new HomographyPlane(Homography.Compose(fit.ImageToSurface, new Homography([1 / 25.4, 0, 0, 0, 1 / 25.4, 0, 0, 0, 1])));
    }

    public MarkerFit Fit { get; }

    /// <summary>The photo's pixels to inches on the surface the markers lie on.</summary>
    public HomographyPlane Plane { get; }

    /// <summary>The print scale's doubt and any other that does not shrink with the fit (a card's thickness), each at two standard deviations.</summary>
    public double Print { get; }

    public double Extra { get; }

    /// <summary>Every piece that counted, the first fixing where the surface starts.</summary>
    public IReadOnlyList<(MarkerKind Kind, int Piece)> Used { get; }

    /// <summary>
    /// True where only a bar, two bars in one line, or a card alone were found: the scale along them is good and the camera's angle is not, so
    /// it is taken from the target's corners where there are corners.
    /// </summary>
    public bool ScaleOnly { get; }

    /// <summary>The known lengths in the photo, for a target whose corners give the angle: a bar's two code centres, a card's sides.</summary>
    public IReadOnlyList<(PointD A, PointD B, double Inches)> Lengths { get; }

    /// <summary>The target's four corners in the photo when all four brackets were found (top left, top right, bottom right, bottom left).</summary>
    public IReadOnlyList<PointD>? TargetCorners { get; }

    /// <summary>The saved board that was found, and what was said about a board whose stickers no longer agree.</summary>
    public string? Board { get; }

    public string? BoardSaid { get; }

    public bool PrinterChecked { get; }

    /// <summary>The scale's doubt over the target at two standard deviations, its corners given in the photo, or the markers' own reach where not.</summary>
    public double UncertaintyAt(IReadOnlyList<PointD>? target)
    {
        var points = target is { Count: > 0 } ? target : Fit.Bodies.SelectMany(b => b.Points).Select(p => p.Image).ToList();
        var at = points.Append(new PointD(points.Average(p => p.X), points.Average(p => p.Y)));
        double fit = 2 * at.Max(Fit.ScaleDoubtAt);
        return Math.Sqrt((fit * fit) + (Print * Print) + (Extra * Extra));
    }

    /// <summary>The sentence for the screen: what was found and used, and how good it is.</summary>
    public string Says(double uncertainty) => string.Create(CultureInfo.InvariantCulture,
        $"Scale from {ScaleMarkerWords.What(Used, Board)} in the photo: good to about {100 * uncertainty:0.##} percent.")
        + (PrinterChecked || Used.All(u => u.Kind is MarkerKind.Card or MarkerKind.Sticker) ? "" : " " + ScaleMarkerWords.NoPrinterCheck)
        + (Used.Any(u => u.Kind == MarkerKind.Card) ? " " + ScaleMarkerWords.CardLeast : "")
        + (BoardSaid is { } said ? " " + said : "");
}

/// <summary>
/// Entry 365 section 0: from the codes and the card found in one photo to a scale, using the best of what is there and combining what agrees.
/// A GroupLab sheet's own codes come first on a GroupLab sheet; this is for everything else.
/// </summary>
public static class ScaleMarkerReading
{
    /// <summary>How far astray a code's corner is found, pixels, one standard deviation; and a card's corner, from its straight sides.</summary>
    public const double TagPixels = 0.3, CardPixels = 1.0;

    /// <summary>The print scale's doubt without a printer check: printers at actual size run up to a percent or so small, as for the sheets.</summary>
    public const double UncheckedPrint = 0.015;

    /// <summary>A card's thickness lifts its face toward the camera: at the 15 to 35 in a phone is held at, 0.08 to 0.2 percent too large.</summary>
    public const double CardThickness = 0.002;

    /// <summary>
    /// The finding, or null where nothing usable was found, and a sentence where something was found that could not be used (a board whose
    /// stickers have moved). <paramref name="found"/> is every code the detector read (the sheets' are ignored);
    /// <paramref name="printer"/> the chosen printer's check, which scales what was printed; <paramref name="boards"/> the boards saved.
    /// </summary>
    public static (MarkerFinding? Finding, string? Said) Read(IReadOnlyList<DetectedMarker> found, CardSighting? card, PrinterProfile? printer, IReadOnlyList<ScaleBoard> boards)
    {
        ArgumentNullException.ThrowIfNull(found);
        ArgumentNullException.ThrowIfNull(boards);
        double across = printer?.Across ?? 1, down = printer?.Down ?? 1;
        var bodies = new List<MarkerBody>();
        var markers = found.Where(m => ScaleMarkerLayout.IsMarker(m.Id) && m.Corners.Count == 4).GroupBy(m => m.Id).Select(g => g.First()).ToList();

        // Printed pieces: each bracket and each bar is one body, its points scaled as the printer prints. A bracket's page is upright, so
        // across and down are the page's; a bar runs along the long side of a sideways page, which a printer feeds as its length.
        foreach (var piece in markers.Select(m => ScaleMarkerLayout.Tag(m.Id)!).Where(t => t.Kind != MarkerKind.Sticker).GroupBy(t => (t.Kind, t.Piece)))
        {
            var points = new List<MarkerPoint>();
            foreach (var tag in piece)
            {
                var image = markers.First(m => m.Id == tag.Id).Corners;
                PointD Printed(PointD p) => tag.Kind == MarkerKind.Bracket ? new PointD(p.X * across, p.Y * down) : new PointD(p.X * down, p.Y * across);
                points.AddRange(Code(image, [.. tag.Corners.Select(Printed)]));
            }

            bodies.Add(new MarkerBody(piece.Key.Kind, piece.Key.Piece, points));
        }

        // A saved board, where its stickers still agree with where they were measured.
        string? board = null, boardSaid = null;
        double boardDoubt = 0;
        var stickers = markers.Where(m => ScaleMarkerLayout.Tag(m.Id) is { Kind: MarkerKind.Sticker }).ToList();
        if (stickers.Count > 0)
        {
            var candidates = boards.Select(b => (Board: b, Points: stickers.Where(s => b.Stickers.ContainsKey(s.Id))
                    .SelectMany(s => Code(s.Corners, b.Stickers[s.Id])).ToList()))
                .Where(c => c.Points.Count >= 8).ToList();
            var fits = candidates.Select(c => (c.Board, c.Points, Rms: Disagreement(c.Points))).OrderBy(c => c.Rms).ToList();
            if (fits.FirstOrDefault() is { Board: not null } best)
            {
                if (best.Rms <= ScaleBoard.Agreement)
                {
                    bodies.Add(new MarkerBody(MarkerKind.Sticker, 0, best.Points));
                    board = best.Board.Name;
                    boardDoubt = best.Board.Uncertainty;
                }
                else
                {
                    boardSaid = ScaleMarkerWords.BoardMoved(best.Board.Name, best.Rms);
                }
            }
            else if (boards.Count == 0)
            {
                boardSaid = ScaleMarkerWords.BoardNotMeasured;
            }
        }

        if (card is { Corners.Count: 4 } c4)
        {
            // The longer sides are the card's 85.60 mm.
            var k = c4.Corners;
            bool wide = Distance(k[0], k[1]) + Distance(k[2], k[3]) >= Distance(k[1], k[2]) + Distance(k[3], k[0]);
            double w = wide ? ScaleMarkerLayout.CardWidth : ScaleMarkerLayout.CardHeight, h = wide ? ScaleMarkerLayout.CardHeight : ScaleMarkerLayout.CardWidth;
            PointD[] model = [new(0, 0), new(w, 0), new(w, h), new(0, h)];
            bodies.Add(new MarkerBody(MarkerKind.Card, 1, [.. model.Select((m, i) => new MarkerPoint(m, k[i], CardPixels))]));
        }

        // A lone code is too small to count: a body needs two codes, or a board two stickers, or a card.
        bodies = [.. bodies.Where(b => b.Points.Count >= 8 || b.Kind == MarkerKind.Card)];
        if (bodies.Count == 0 || MarkerFit.Fit(bodies) is not { } fit)
        {
            return (null, boardSaid);
        }

        var used = fit.Bodies.Select(b => (b.Kind, b.Piece)).ToList();
        bool printed = used.Any(u => u.Kind is MarkerKind.Bracket or MarkerKind.InchBar or MarkerKind.MetricBar);
        double print = printed ? printer is { } p ? p.Uncertainty : UncheckedPrint : 0;
        double extra = Math.Sqrt(Math.Pow(used.Any(u => u.Kind == MarkerKind.Card) ? CardThickness : 0, 2) + (boardDoubt * boardDoubt));

        // Only bars all in one line, or a card alone, give the scale along them and not the angle.
        var bars = fit.Bodies.Where(b => b.Kind is MarkerKind.InchBar or MarkerKind.MetricBar).ToList();
        bool scaleOnly = fit.Bodies.All(b => b.Kind is MarkerKind.InchBar or MarkerKind.MetricBar or MarkerKind.Card)
            && (fit.Bodies.Count == 1 || (bars.Count == fit.Bodies.Count && bars.Count > 1 && Parallel(fit, bars)));

        var lengths = new List<(PointD, PointD, double)>();
        for (int i = 0; i < fit.Bodies.Count; i++)
        {
            var body = fit.Bodies[i];
            if (body.Kind is MarkerKind.InchBar or MarkerKind.MetricBar)
            {
                lengths.Add((Diagonals([.. body.Points.Take(4).Select(p => p.Image)]), Diagonals([.. body.Points.Skip(4).Take(4).Select(p => p.Image)]),
                    Distance(body.Points[0].Model, body.Points[4].Model) / 25.4));
            }
            else if (body.Kind == MarkerKind.Card)
            {
                for (int j = 0; j < 4; j++)
                {
                    lengths.Add((body.Points[j].Image, body.Points[(j + 1) % 4].Image, Distance(body.Points[j].Model, body.Points[(j + 1) % 4].Model) / 25.4));
                }
            }
        }

        // All four brackets: their inside corners are the target's.
        IReadOnlyList<PointD>? corners = null;
        var brackets = Enumerable.Range(1, 4).Select(n => fit.Bodies.ToList().FindIndex(b => b.Kind == MarkerKind.Bracket && b.Piece == n)).ToArray();
        if (brackets.All(i => i >= 0))
        {
            corners = [.. brackets.Select(i => fit.InImage(i, new PointD(0, 0)))];
        }

        return (new MarkerFinding(fit, print, extra, used, scaleOnly, lengths, corners, board, boardSaid, printer is not null), boardSaid);
    }

    /// <summary>
    /// A code as the fit sees it: its four corners, each to <see cref="TagPixels"/> on a code 60 pixels or more across and proportionally
    /// worse on a smaller one, whose blurred edges place its corners less surely.
    /// </summary>
    private static IEnumerable<MarkerPoint> Code(IReadOnlyList<PointD> image, IReadOnlyList<PointD> model)
    {
        double side = Enumerable.Range(0, 4).Average(i => Distance(image[i], image[(i + 1) % 4]));
        double sigma = TagPixels * Math.Max(1, 60 / Math.Max(side, 1));
        return Enumerable.Range(0, 4).Select(i => new MarkerPoint(model[i], image[i], sigma));
    }

    /// <summary>How far a board's stickers lie from where they were measured, millimetres root mean square, after the best homography.</summary>
    private static double Disagreement(IReadOnlyList<MarkerPoint> points)
    {
        if (HomographyEstimate.Fit([.. points.Select(p => p.Image)], [.. points.Select(p => p.Model)]) is not { } h)
        {
            return double.PositiveInfinity;
        }

        return Math.Sqrt(points.Average(p => Math.Pow(Distance(h.Apply(p.Image), p.Model), 2)));
    }

    private static bool Parallel(MarkerFit fit, IReadOnlyList<MarkerBody> bars)
    {
        var directions = bars.Select(b => fit.Bodies.ToList().IndexOf(b)).Select(i => fit.Poses[i].Angle).ToList();
        return directions.All(a => Math.Abs(Math.Sin(a - directions[0])) < Math.Sin(20 * Math.PI / 180));
    }

    /// <summary>Where a square's diagonals cross, which perspective keeps, not its corners' average.</summary>
    internal static PointD Diagonals(IReadOnlyList<PointD> c)
    {
        double d = ((c[0].X - c[2].X) * (c[1].Y - c[3].Y)) - ((c[0].Y - c[2].Y) * (c[1].X - c[3].X));
        if (Math.Abs(d) < 1e-9)
        {
            return new PointD(c.Average(p => p.X), c.Average(p => p.Y));
        }

        double a = (c[0].X * c[2].Y) - (c[0].Y * c[2].X), b = (c[1].X * c[3].Y) - (c[1].Y * c[3].X);
        return new PointD(((a * (c[1].X - c[3].X)) - ((c[0].X - c[2].X) * b)) / d, ((a * (c[1].Y - c[3].Y)) - ((c[0].Y - c[2].Y) * b)) / d);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
