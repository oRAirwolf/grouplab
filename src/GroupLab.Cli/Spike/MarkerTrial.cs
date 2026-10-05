using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab marker-trial [--photos N] [--seed S] [--save folder]</c>, NOTES-FROM-PLANNING.md entry 365, "Accuracy, measured, not guessed":
/// what each scale marker achieves, the way entry 344's poster trial measured the scale sources. A 12 by 12 in and a 23 by 35 in target
/// (drawn by <see cref="PosterTrial"/>) are laid on a board with every kind of marker round them: the four corner brackets, a scale bar
/// below and another up the left side, four board stickers whose positions were measured to 0.1 mm, and a bank card to the right. Each is
/// photographed by the poster trial's camera, tilted up to 30 degrees, with blur and noise. Each kind is then read on its own, and all
/// together, and the target's size it gives is compared with the truth beside the doubt it claimed. The markers print at exactly their
/// size, as with a printer check. Nothing it writes is committed.
/// </summary>
public static class MarkerTrial
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const double PieceDpi = 25.4 * 16;

    private static readonly string[] Kinds = ["brackets", "one bar", "two bars", "board", "card", "all"];

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        int photos = Option(args, "--photos", 12), seed = Option(args, "--seed", 365);
        string? save = args.SkipWhile(a => a != "--save").Skip(1).FirstOrDefault();
        var rng = new Random(seed);
        var results = Kinds.ToDictionary(k => k, _ => new List<(double Size, double Error, double Claimed, double Tilt)>());
        var missed = Kinds.ToDictionary(k => k, _ => 0);
        var corners = new List<double>();
        var printer = new PrinterProfile("trial", 1, 1, PrinterMethod.Scan, new DateOnly(2026, 10, 4), 0.001);
        foreach (var (w, h) in new[] { (12.0, 12.0), (23.0, 35.0) })
        {
            for (int n = 0; n < photos; n++)
            {
                using var target = PosterTrial.Draw(w, h, rng);
                var (photo, toImage, tilt, board, cardTruth) = Photograph(target, w, h, rng);
                using (photo)
                {
                    if (save is not null)
                    {
                        Directory.CreateDirectory(save);
                        Cv2.ImWrite(Path.Combine(save, string.Create(Inv, $"{w:0}x{h:0}-{n:00}.jpg")), photo, [(int)ImwriteFlags.JpegQuality, 90]);
                    }

                    using var greyMat = new Mat();
                    Cv2.CvtColor(photo, greyMat, ColorConversionCodes.BGR2GRAY);
                    var grey = OpenCvSharpBackend.Copy(greyMat);
                    var sighting = ScaleMarkerFinder.Find(grey, null, lookForCard: true);
                    if (save is not null && sighting.Card is { } seenCard)
                    {
                        using var marked = photo.Clone();
                        Cv2.Polylines(marked, [seenCard.Corners.Select(p => new Point((int)p.X, (int)p.Y)).ToArray()], true, new Scalar(0, 0, 255), 3);
                        Cv2.ImWrite(Path.Combine(save, string.Create(Inv, $"{w:0}x{h:0}-{n:00}-card.jpg")), marked, [(int)ImwriteFlags.JpegQuality, 85]);
                    }
                    if (args.Contains("--verbose") && sighting.Card is { } c)
                    {
                        output.WriteLine(string.Create(Inv, $"  card corners astray, px: {string.Join(", ", c.Corners.Select(p => cardTruth.Min(t => Distance(p, t))).Select(d => d.ToString("0.0", Inv)))}; card {Distance(cardTruth[0], cardTruth[1]):0} px wide"));
                    }

                    PointD[] truth = [toImage.Apply(new(0, 0)), toImage.Apply(new(w, 0)), toImage.Apply(new(w, h)), toImage.Apply(new(0, h))];
                    foreach (string kind in Kinds)
                    {
                        var codes = sighting.Markers.Where(m => kind switch
                        {
                            "brackets" => m.Id < ScaleMarkerLayout.InchBarFirst,
                            "one bar" => m.Id is ScaleMarkerLayout.InchBarFirst or ScaleMarkerLayout.InchBarFirst + 1,
                            "two bars" => m.Id is >= ScaleMarkerLayout.InchBarFirst and < ScaleMarkerLayout.MetricBarFirst,
                            "board" => m.Id >= ScaleMarkerLayout.StickerFirst,
                            "card" => false,
                            _ => true,
                        }).ToList();
                        var card = kind is "card" or "all" ? sighting.Card : null;
                        var (finding, _) = ScaleMarkerReading.Read(codes, card, printer, [board]);
                        if (finding is null)
                        {
                            missed[kind]++;
                            continue;
                        }

                        var straight = TargetStraightening.FromMarkers(finding, truth, PosterTrial.Width, PosterTrial.Height, null);
                        double err = Math.Max(Math.Abs((straight.WidthInches / w) - 1), Math.Abs((straight.HeightInches / h) - 1));
                        results[kind].Add((w, err, straight.Uncertainty, tilt));
                        if (args.Contains("--verbose"))
                        {
                            output.WriteLine(string.Create(Inv, $"  {w:0}x{h:0} #{n} {kind}: {100 * err:0.000} percent, claimed {100 * straight.Uncertainty:0.000}, {finding.Fit.Bodies.Count} bodies, {finding.Fit.RmsPixels:0.00} px, tilt {tilt:0}"));
                        }
                        if (kind == "brackets" && finding.NearCorners is { } found)
                        {
                            var plane = new HomographyPlane(toImage.Inverse());
                            corners.Add(found.Select((c, i) => Distance(plane.ToInches(c), plane.ToInches(truth[i])) * 25.4).Max());
                        }
                    }
                }
            }
        }

        output.WriteLine(string.Create(Inv, $"{photos} photos of each target, seed {seed}; error = the larger of width and height against the truth, percent."));
        output.WriteLine("kind       target   found   median   95th    worst   claimed (median)   within claim");
        foreach (string kind in Kinds)
        {
            foreach (double size in new[] { 12.0, 23.0 })
            {
                var r = results[kind].Where(x => x.Size == size).ToList();
                if (r.Count == 0)
                {
                    output.WriteLine(string.Create(Inv, $"{kind,-10} {(size == 12 ? "12x12" : "23x35"),-8} 0/{photos}"));
                    continue;
                }

                var e = r.Select(x => 100 * x.Error).Order().ToList();
                var c = r.Select(x => 100 * x.Claimed).Order().ToList();
                output.WriteLine(string.Create(Inv, $"{kind,-10} {(size == 12 ? "12x12" : "23x35"),-8} {r.Count,2}/{photos}   {Pct(e, 50),6:0.000}  {Pct(e, 95),6:0.000}  {e[^1],6:0.000}   {Pct(c, 50),8:0.000}           {r.Count(x => x.Error <= x.Claimed)}/{r.Count}"));
            }
        }

        if (corners.Count > 0)
        {
            var sorted = corners.Order().ToList();
            output.WriteLine(string.Create(Inv, $"bracket corners against the target's: median {Pct(sorted, 50):0.00} mm, worst {sorted[^1]:0.00} mm"));
        }

        foreach (string kind in Kinds)
        {
            var all = results[kind].Select(x => 100 * x.Error).Order().ToList();
            output.WriteLine(string.Create(Inv, $"overall {kind}: {all.Count}/{2 * photos}, median {(all.Count > 0 ? Pct(all, 50) : double.NaN):0.00}, worst {(all.Count > 0 ? all[^1] : double.NaN):0.00}"));
        }

        return 0;
    }

    /// <summary>
    /// The board in wall inches: the target at the origin, the brackets at its corners, a bar 1.5 in below it and one 1.5 in left of it, the
    /// four stickers 3 in out from its corners and the card 2.5 in right of its middle, turned at random.
    /// </summary>
    private static (Mat Photo, Homography ToImage, double Tilt, ScaleBoard Board, PointD[] Card) Photograph(Mat target, double w, double h, Random rng)
    {
        double left = -4, top = -4, right = w + 5, bottom = h + 4;
        double cx = (left + right) / 2, cy = (top + bottom) / 2, sceneW = right - left, sceneH = bottom - top;
        double tilt = rng.NextDouble() * 30 * Math.PI / 180, axis = rng.NextDouble() * 2 * Math.PI, roll = (rng.NextDouble() - 0.5) * 6 * Math.PI / 180;
        double fill = 0.72 + (rng.NextDouble() * 0.12);
        double distance = PosterTrial.Focal * Math.Max(sceneW / (PosterTrial.Width * fill), sceneH / (PosterTrial.Height * fill)) * (1 + (0.4 * Math.Sin(tilt)));
        double[] rot = PosterTrial.Multiply(PosterTrial.Rodrigues(Math.Cos(axis) * tilt, Math.Sin(axis) * tilt), PosterTrial.RotZ(roll));
        double[] t = [-(rot[0] * cx) - (rot[1] * cy), -(rot[3] * cx) - (rot[4] * cy), distance - (rot[6] * cx) - (rot[7] * cy)];
        double[] k = [PosterTrial.Focal, 0, PosterTrial.Width / 2.0, 0, PosterTrial.Focal, PosterTrial.Height / 2.0, 0, 0, 1];
        double[] wallToImage = PosterTrial.Multiply(k, [rot[0], rot[1], t[0], rot[3], rot[4], t[1], rot[6], rot[7], t[2]]);

        var photo = new Mat(PosterTrial.Height, PosterTrial.Width, MatType.CV_8UC3, new Scalar(90 + rng.Next(50), 110 + rng.Next(40), 130 + rng.Next(40)));
        PosterTrial.Place(photo, target, PosterTrial.PosterDpi, wallToImage);

        // The brackets, each a third of a millimetre and half a degree from where it was meant to be.
        PointD[] targetCorners = [new(0, 0), new(w, 0), new(w, h), new(0, h)];
        for (int piece = 1; piece <= 4; piece++)
        {
            var at = targetCorners[piece - 1];
            var (picture, mask, origin) = Bracket(piece);
            Put(photo, picture, mask, origin, wallToImage, new PointD(at.X + (PosterTrial.Gauss(rng, 0.3) / 25.4), at.Y + (PosterTrial.Gauss(rng, 0.3) / 25.4)), PosterTrial.Gauss(rng, 0.5 * Math.PI / 180));
            picture.Dispose();
            mask.Dispose();
        }

        // Bar 1 below, bar 2 up the left side.
        double half = ScaleMarkerLayout.InchBarLength / 25.4 / 2;
        using (var bar = Bar(1))
        {
            Put(photo, bar.Picture, bar.Mask, bar.Origin, wallToImage, new PointD((w / 2) - half, h + 1.5), PosterTrial.Gauss(rng, 1 * Math.PI / 180));
        }

        using (var bar = Bar(2))
        {
            Put(photo, bar.Picture, bar.Mask, bar.Origin, wallToImage, new PointD(-1.5, (h / 2) - half), (Math.PI / 2) + PosterTrial.Gauss(rng, 1 * Math.PI / 180));
        }

        // The stickers, and the board as measured: their true corners, each 0.1 mm astray.
        var layout = new Dictionary<int, PointD[]>();
        PointD[] stickerAt = [new(-2.8, -2.8), new(w + 2.8, -2.8), new(w + 2.8, h + 2.8), new(-2.8, h + 2.8)];
        for (int i = 0; i < 4; i++)
        {
            var tag = ScaleMarkerLayout.Sticker('A', i + 1);
            double angle = PosterTrial.Gauss(rng, 10 * Math.PI / 180);
            using var sticker = Sticker(tag.Id);
            Put(photo, sticker.Picture, sticker.Mask, sticker.Origin, wallToImage, stickerAt[i], angle);
            double c = Math.Cos(angle), s = Math.Sin(angle);
            layout[tag.Id] = [.. tag.Corners.Select(p => new PointD((stickerAt[i].X * 25.4) + (c * p.X) - (s * p.Y) + PosterTrial.Gauss(rng, 0.1), (stickerAt[i].Y * 25.4) + (s * p.X) + (c * p.Y) + PosterTrial.Gauss(rng, 0.1)))];
        }

        PointD[] cardCorners;
        using (var card = Card(rng))
        {
            var cardAt = new PointD(w + 0.9, (h / 2) - 1);
            double cardAngle = PosterTrial.Gauss(rng, 25 * Math.PI / 180), cc = Math.Cos(cardAngle), cs = Math.Sin(cardAngle);
            Put(photo, card.Picture, card.Mask, card.Origin, wallToImage, cardAt, cardAngle);
            var toImage = new Homography(wallToImage);
            cardCorners = [.. new PointD[] { new(0, 0), new(ScaleMarkerLayout.CardWidth, 0), new(ScaleMarkerLayout.CardWidth, ScaleMarkerLayout.CardHeight), new(0, ScaleMarkerLayout.CardHeight) }
                .Select(m => toImage.Apply(new PointD(cardAt.X + (((cc * m.X) - (cs * m.Y)) / 25.4), cardAt.Y + (((cs * m.X) + (cc * m.Y)) / 25.4))))];
        }

        Cv2.GaussianBlur(photo, photo, new Size(0, 0), 0.6 + (rng.NextDouble() * 0.6));
        using (var noise = new Mat(photo.Size(), MatType.CV_16SC3))
        {
            Cv2.Randn(noise, Scalar.All(0), Scalar.All(3));
            using var wide = new Mat();
            photo.ConvertTo(wide, MatType.CV_16SC3);
            Cv2.Add(wide, noise, wide);
            wide.ConvertTo(photo, MatType.CV_8UC3);
        }

        Cv2.ImEncode(".jpg", photo, out byte[] jpeg, [(int)ImwriteFlags.JpegQuality, 92]);
        var decoded = Cv2.ImDecode(jpeg, ImreadModes.Color);
        photo.Dispose();
        return (decoded, new Homography(wallToImage), tilt * 180 / Math.PI, new ScaleBoard("Trial board", 'A', new DateOnly(2026, 10, 4), 0.001, layout), cardCorners);
    }

    internal sealed record Piece(Mat Picture, Mat Mask, PointD Origin) : IDisposable
    {
        public void Dispose()
        {
            Picture.Dispose();
            Mask.Dispose();
        }
    }

    /// <summary>A piece drawn in its own millimetres at <see cref="PieceDpi"/>, laid on the wall at <paramref name="at"/> inches, turned by <paramref name="angle"/>.</summary>
    internal static void Put(Mat photo, Mat picture, Mat mask, PointD origin, double[] wallToImage, PointD at, double angle)
    {
        double c = Math.Cos(angle), s = Math.Sin(angle), px = 25.4 / PieceDpi;
        double[] pieceToWall = [c / 25.4, -s / 25.4, at.X, s / 25.4, c / 25.4, at.Y, 0, 0, 1];
        double[] pictureToPiece = [px, 0, origin.X, 0, px, origin.Y, 0, 0, 1];
        double[] h = PosterTrial.Multiply(wallToImage, PosterTrial.Multiply(pieceToWall, pictureToPiece));
        using var hm = Mat.FromPixelData(3, 3, MatType.CV_64FC1, h);
        using var warped = new Mat();
        using var warpedMask = new Mat();
        Cv2.WarpPerspective(picture, warped, hm, photo.Size(), InterpolationFlags.Area);
        Cv2.WarpPerspective(mask, warpedMask, hm, photo.Size(), InterpolationFlags.Linear);
        warped.CopyTo(photo, warpedMask);
    }

    private static int Px(double mm) => (int)Math.Round(mm * PieceDpi / 25.4);

    internal static (Mat, Mat, PointD) Bracket(int piece)
    {
        var outline = ScaleMarkerLayout.BracketOutline(piece);
        double x0 = outline.Min(p => p.X), y0 = outline.Min(p => p.Y);
        int size = Px(outline.Max(p => p.X) - x0) + 1;
        var picture = new Mat(size, size, MatType.CV_8UC3, Scalar.White);
        var mask = new Mat(size, size, MatType.CV_8UC1, Scalar.Black);
        Cv2.FillPoly(mask, [outline.Select(p => new Point(Px(p.X - x0), Px(p.Y - y0))).ToArray()], Scalar.White);
        foreach (var tag in ScaleMarkerLayout.Bracket(piece))
        {
            Code(picture, tag, x0, y0);
        }

        return (picture, mask, new PointD(x0, y0));
    }

    internal static Piece Bar(int number)
    {
        var tags = ScaleMarkerLayout.Bar(false, number);
        // White beyond each code as far as the page's edge, 6.7 mm on Letter.
        double x0 = -(tags[0].Side / 2) - 6.7, y0 = -ScaleMarkerLayout.BarWidth / 2, length = tags[1].Centre.X - (2 * x0);
        var picture = new Mat(Px(ScaleMarkerLayout.BarWidth), Px(length), MatType.CV_8UC3, Scalar.White);
        var mask = new Mat(picture.Size(), MatType.CV_8UC1, Scalar.White);
        for (double x = 0; x <= tags[1].Centre.X; x += 25.4 / 4)
        {
            Cv2.Line(picture, new Point(Px(x - x0), 0), new Point(Px(x - x0), Px(3)), Scalar.Black, 2);
        }

        foreach (var tag in tags)
        {
            Code(picture, tag, x0, y0);
        }

        return new Piece(picture, mask, new PointD(x0, y0));
    }

    private static Piece Sticker(int id)
    {
        var tag = ScaleMarkerLayout.Tag(id)!;
        var picture = new Mat(Px(44), Px(44), MatType.CV_8UC3, Scalar.White);
        var mask = new Mat(picture.Size(), MatType.CV_8UC1, Scalar.White);
        Code(picture, tag, -22, -22);
        return new Piece(picture, mask, new PointD(-22, -22));
    }

    /// <summary>A card, light or dark at random, with a dark stripe and some marks: its sharp corners are its own millimetres' 0 to 85.6 and 0 to 53.98.</summary>
    internal static Piece Card(Random rng)
    {
        int w = Px(ScaleMarkerLayout.CardWidth), h = Px(ScaleMarkerLayout.CardHeight), r = Px(ScaleMarkerLayout.CardRadius);
        bool light = rng.Next(2) == 0;
        var picture = new Mat(h, w, MatType.CV_8UC3, light ? new Scalar(225, 228, 230) : new Scalar(110, 60, 30));
        var mask = new Mat(h, w, MatType.CV_8UC1, Scalar.Black);
        Cv2.Rectangle(mask, new Rect(r, 0, w - (2 * r), h), Scalar.White, -1);
        Cv2.Rectangle(mask, new Rect(0, r, w, h - (2 * r)), Scalar.White, -1);
        foreach (var (x, y) in new[] { (r, r), (w - r - 1, r), (w - r - 1, h - r - 1), (r, h - r - 1) })
        {
            Cv2.Circle(mask, new Point(x, y), r, Scalar.White, -1);
        }

        Cv2.Rectangle(picture, new Rect(0, Px(5), w, Px(12)), new Scalar(30, 30, 30), -1);
        Cv2.Rectangle(picture, new Rect(Px(8), Px(24), Px(50), Px(8)), light ? new Scalar(250, 250, 250) : new Scalar(160, 120, 90), -1);
        return new Piece(picture, mask, new PointD(0, 0));
    }

    private static void Code(Mat picture, MarkerTag tag, double x0, double y0)
    {
        var inked = ScaleMarkerLayout.Inked(tag.Id);
        double module = tag.Side / ScaleMarkerLayout.Modules, left = tag.Centre.X - (tag.Side / 2) - x0, top = tag.Centre.Y - (tag.Side / 2) - y0;
        for (int row = 0; row < ScaleMarkerLayout.Modules; row++)
        {
            for (int col = 0; col < ScaleMarkerLayout.Modules; col++)
            {
                if (inked[row, col])
                {
                    Cv2.Rectangle(picture, new Point(Px(left + (col * module)), Px(top + (row * module))),
                        new Point(Px(left + ((col + 1) * module)) - 1, Px(top + ((row + 1) * module)) - 1), Scalar.Black, -1);
                }
            }
        }
    }

    private static int Option(IReadOnlyList<string> args, string name, int fallback)
    {
        int i = args.ToList().IndexOf(name);
        return i >= 0 && i + 1 < args.Count && int.TryParse(args[i + 1], NumberStyles.Integer, Inv, out int v) ? v : fallback;
    }

    private static double Pct(List<double> sorted, double p) => sorted[Math.Clamp((int)Math.Round(p / 100 * (sorted.Count - 1)), 0, sorted.Count - 1)];

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
