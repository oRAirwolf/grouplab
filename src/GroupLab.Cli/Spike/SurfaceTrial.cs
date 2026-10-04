using System.Globalization;
using System.Text.RegularExpressions;
using GroupLab.Cli.Imaging;
using GroupLab.Cli.Library;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.ScaleMarkers;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab surface-trial [--scenes N] [--seed S] [--save folder] [--markers]</c>, NOTES-FROM-PLANNING.md entry 371: the corner finder and
/// the scale markers on many surfaces, not only the thirty photos Alan has taken. Real blank targets (his 600 dpi scans of the Birchwood
/// Casey sheets, and his phone photos flattened by their checked corners) are laid on procedural surfaces (no texture photograph is used, so
/// no licence is needed): a black and a dark grey table, dark and light wood with grain and seams, white and cream counters, a cardboard
/// backer, OSB, foam board, a backer full of holes, staples and tape, a target stapled over older shot targets, grass, gravel and carpet.
/// Each is photographed by the poster trial's camera, tilted up to 30 degrees and turned, with sometimes a shadow across an edge, glare on
/// the glossy Shoot-N-C, warm or cool light, tape over a corner or a corner out of the frame, and always blur, noise and JPEG. The truth is
/// the projection's own corners. Nothing it draws is committed. Alan's real photos are the held-out check, run by the tests.
/// </summary>
public static partial class SurfaceTrial
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const int W = PosterTrial.Width, H = PosterTrial.Height;

    internal static readonly string[] Surfaces =
        ["black table", "dark grey table", "dark wood", "light wood", "white counter", "cream counter", "cardboard", "OSB", "foam board",
         "backer with holes", "over old targets", "grass", "gravel", "carpet"];

    private sealed record Flat(string Name, Mat Picture, double Dpi, double Width, double Height, bool Glossy);

    private sealed record Outcome(string Surface, bool Found, bool Right, double Weakest, bool OutOfFrame, int Agree, double RunOn);

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        int scenes = Option(args, "--scenes", 6), seed = Option(args, "--seed", 371);
        string? save = args.SkipWhile(a => a != "--save").Skip(1).FirstOrDefault();
        var rng = new Random(seed);
        var flats = Flats();
        if (flats.Count == 0)
        {
            error.WriteLine("No blank targets: this needs Alan's scans and photos under C:\\Dev\\grouplab-local\\commercial-targets.");
            return 1;
        }

        output.WriteLine(string.Create(Inv, $"{flats.Count} targets, {Surfaces.Length} surfaces, {scenes} scenes each, seed {seed}"));
        if (args.Contains("--labels"))
        {
            return Labels(flats.Where(f => f.Name.StartsWith("scan", StringComparison.Ordinal)).ToList(), scenes, rng, output);
        }

        if (args.Contains("--markers"))
        {
            return Markers(flats.Where(f => f.Name.StartsWith("scan", StringComparison.Ordinal)).ToList(), scenes, rng, output);
        }

        var outcomes = new List<Outcome>();
        foreach (string surface in Surfaces)
        {
            for (int n = 0; n < scenes; n++)
            {
                var flat = flats[rng.Next(flats.Count)];
                bool outOfFrame = rng.NextDouble() < 0.08;
                var (photo, truth) = Scene(flat, surface, rng, outOfFrame, out _);
                using (photo)
                {
                    var found = StoreTargetOutline.Find(photo);
                    // A scan's corners are exact; a phone photo's were checked to about one percent, so its truth is that loose.
                    double tolerance = Math.Max(12, (flat.Name.StartsWith("scan", StringComparison.Ordinal) ? 0.012 : 0.03) * Distance(truth[0], truth[1]));
                    bool right = !outOfFrame && Enumerable.Range(0, 4).All(i => Distance(found.Corners[i], truth[i]) <= tolerance);
                    outcomes.Add(new Outcome(surface, found.Found, right, Weakest(found.Tried), outOfFrame, Field(found.Tried, AgreeLine()), Field(found.Tried, RunOnLine())));
                    if (args.Contains("--verbose"))
                    {
                        output.WriteLine(string.Create(Inv, $"  {surface} {n} {flat.Name}: {(found.Found ? "found" : "unsure")}, worst corner {Enumerable.Range(0, 4).Max(i => Distance(found.Corners[i], truth[i])):0} px of {tolerance:0}, weakest {Weakest(found.Tried):0.00}, {found.Method}"));
                    }
                    if (save is not null)
                    {
                        Directory.CreateDirectory(save);
                        Cv2.Polylines(photo, [found.Corners.Select(p => new Point(p.X, p.Y)).ToArray()], true, found.Found ? new Scalar(0, 255, 0) : new Scalar(0, 165, 255), 8);
                        Cv2.Polylines(photo, [truth.Select(p => new Point(p.X, p.Y)).ToArray()], true, new Scalar(255, 0, 255), 3);
                        using var small = new Mat();
                        Cv2.Resize(photo, small, new Size(1000, 750));
                        Cv2.ImWrite(Path.Combine(save, string.Create(Inv, $"{surface.Replace(' ', '-')}-{n:00}-{(right ? "right" : "wrong")}{(found.Found ? "-found" : "")}.jpg")), small, [(int)ImwriteFlags.JpegQuality, 80]);
                    }
                }
            }
        }

        // Entry 371 section 1: how often "found" is wrong, as the code calls it now and at other thresholds on the weakest side.
        output.WriteLine("surface             scenes  right  found  found and wrong  not sure but right");
        foreach (var g in outcomes.GroupBy(o => o.Surface))
        {
            output.WriteLine(string.Create(Inv, $"{g.Key,-19} {g.Count(),6} {g.Count(o => o.Right),6} {g.Count(o => o.Found),6} {g.Count(o => o.Found && !o.Right),16} {g.Count(o => !o.Found && o.Right),19}"));
        }

        output.WriteLine(string.Create(Inv, $"all                 {outcomes.Count,6} {outcomes.Count(o => o.Right),6} {outcomes.Count(o => o.Found),6} {outcomes.Count(o => o.Found && !o.Right),16} {outcomes.Count(o => !o.Found && o.Right),19}"));
        output.WriteLine("threshold on the weakest side: called found, of them wrong, right ones not called found");
        foreach (double t in new[] { 0.7, 0.8, 0.85, 0.9, 0.92, 0.95, 0.98 })
        {
            var called = outcomes.Where(o => o.Weakest >= t && !double.IsNaN(o.Weakest)).ToList();
            output.WriteLine(string.Create(Inv, $"  {t:0.00}: {called.Count} found, {called.Count(o => !o.Right)} wrong ({(called.Count == 0 ? 0 : 100.0 * called.Count(o => !o.Right) / called.Count):0.0} percent), {outcomes.Count(o => o.Right && !(o.Weakest >= t))} right left unsure"));
        }

        output.WriteLine("with agreement: called found when the weakest side reaches t and at least a other ways agree");
        foreach (double t in new[] { 0.7, 0.8, 0.9 })
        {
            foreach (int a in new[] { 1, 2 })
            {
                var called = outcomes.Where(o => o.Weakest >= t && o.Agree >= a).ToList();
                output.WriteLine(string.Create(Inv, $"  {t:0.00}, {a}: {called.Count} found, {called.Count(o => !o.Right)} wrong ({(called.Count == 0 ? 0 : 100.0 * called.Count(o => !o.Right) / called.Count):0.0} percent), {outcomes.Count(o => o.Right && !(o.Weakest >= t && o.Agree >= a))} right left unsure"));
            }
        }

        return 0;
    }

    private static int Field(IReadOnlyList<string> tried, Regex pattern) =>
        tried.Select(t => pattern.Match(t)).FirstOrDefault(m => m.Success) is { } m ? (int)Math.Round(double.Parse(m.Groups[1].Value, Inv)) : -1;

    [GeneratedRegex(@"^chose .*, (\d+) other ways agree")]
    private static partial Regex AgreeLine();

    [GeneratedRegex(@"^chose .*, run on ([0-9.]+)")]
    private static partial Regex RunOnLine();

    /// <summary>
    /// Entry 371 section 4: brackets and bars touching the target and set off by 2 to 20 mm, on a dark and a light surface: where the brackets
    /// put the target's corners, whether the corners are right with the bars painted out, and the scale's error, against the truth.
    /// </summary>
    private static int Markers(List<Flat> flats, int scenes, Random rng, TextWriter output)
    {
        var printer = new PrinterProfile("trial", 1, 1, PrinterMethod.Scan, new DateOnly(2026, 10, 4), 0.001);
        output.WriteLine("surface          gap mm  brackets: corner off mm, size error percent | bars: corners right, scale error percent");
        foreach (string surface in new[] { "dark wood", "white counter", "cardboard", "black table" })
        {
            foreach (double gap in new[] { 0.0, 2, 5, 10, 20 })
            {
                var cornerOff = new List<double>();
                var sizeErr = new List<double>();
                var barErr = new List<double>();
                int barRight = 0, barScenes = 0;
                for (int n = 0; n < scenes; n++)
                {
                    var flat = flats[rng.Next(flats.Count)];
                    var (photo, truth) = Scene(flat, surface, rng, false, out var wallToImage, gap);
                    using (photo)
                    {
                        using var greyMat = new Mat();
                        Cv2.CvtColor(photo, greyMat, ColorConversionCodes.BGR2GRAY);
                        var grey = OpenCvSharpBackend.Copy(greyMat);
                        var codes = ScaleMarkerFinder.Codes(grey);
                        var toWall = new Homography(wallToImage).Inverse();

                        var (brackets, _) = ScaleMarkerReading.Read([.. codes.Where(c => c.Id < ScaleMarkerLayout.InchBarFirst)], null, printer, []);
                        if (brackets?.TargetCorners is { } bc)
                        {
                            cornerOff.Add(bc.Select((c, i) => Distance(toWall.Apply(c), toWall.Apply(truth[i])) * 25.4).Max());
                            var plane = TargetStraightening.FromMarkers(brackets, bc, W, H, null);
                            sizeErr.Add(100 * Math.Max(Math.Abs((plane.WidthInches / flat.Width) - 1), Math.Abs((plane.HeightInches / flat.Height) - 1)));
                        }

                        var (bars, _) = ScaleMarkerReading.Read([.. codes.Where(c => c.Id is >= ScaleMarkerLayout.InchBarFirst and < ScaleMarkerLayout.MetricBarFirst)], null, printer, []);
                        if (bars is not null)
                        {
                            barScenes++;
                            using var painted = photo.Clone();
                            ScaleMarkerFinder.PaintOverBars(painted, bars);
                            // The brackets are in the photo too, at the corners; only the bars' effect on the outline is measured here, so they
                            // are judged where the brackets are not used.
                            var found = StoreTargetOutline.Find(painted);
                            double tolerance = Math.Max(12, 0.012 * Distance(truth[0], truth[1]));
                            if (found.Found && Enumerable.Range(0, 4).All(i => Distance(found.Corners[i], truth[i]) <= tolerance))
                            {
                                barRight++;
                            }

                            var plane = TargetStraightening.FromMarkers(bars, truth, W, H, null);
                            barErr.Add(100 * Math.Max(Math.Abs((plane.WidthInches / flat.Width) - 1), Math.Abs((plane.HeightInches / flat.Height) - 1)));
                        }
                    }
                }

                output.WriteLine(string.Create(Inv, $"{surface,-16} {gap,6:0}  {Median(cornerOff),6:0.00} mm, {Median(sizeErr),5:0.00} percent ({cornerOff.Count}/{scenes}) | {barRight}/{barScenes} right, {Median(barErr):0.000} percent"));
            }
        }

        return 0;
    }

    /// <summary>
    /// Entry 372 section 3: scale labels stuck on the target, one in its top left or two at opposite corners, 70 by 80 and 50 by 30 mm, on
    /// four surfaces: how often they are read and the scale's error, the target's corners taking out the angle as in the app.
    /// </summary>
    private static int Labels(List<Flat> flats, int scenes, Random rng, TextWriter output)
    {
        var printer = new PrinterProfile("trial", 1, 1, PrinterMethod.Scan, new DateOnly(2026, 10, 4), 0.001);
        output.WriteLine("surface          label    count  read   median error  worst error  claimed (median), percent");
        int serial = 1;
        foreach (string surface in new[] { "dark wood", "white counter", "cardboard", "black table" })
        {
            foreach (var (lw, lh) in new[] { (70, 80), (50, 30) })
            {
                foreach (int count in new[] { 1, 2 })
                {
                    var errors = new List<double>();
                    var claimed = new List<double>();
                    for (int n = 0; n < scenes; n++)
                    {
                        var flat = flats[rng.Next(flats.Count)];
                        var (photo, truth) = Scene(flat, surface, rng, false, out var wallToImage, null, (lw, lh, count, serial));
                        serial += count;
                        using (photo)
                        {
                            using var greyMat = new Mat();
                            Cv2.CvtColor(photo, greyMat, ColorConversionCodes.BGR2GRAY);
                            var (finding, _) = ScaleMarkerReading.Read(ScaleMarkerFinder.Codes(OpenCvSharpBackend.Copy(greyMat)), null, printer, []);
                            if (finding is null)
                            {
                                continue;
                            }

                            var plane = TargetStraightening.FromMarkers(finding, truth, W, H, null);
                            errors.Add(100 * Math.Max(Math.Abs((plane.WidthInches / flat.Width) - 1), Math.Abs((plane.HeightInches / flat.Height) - 1)));
                            claimed.Add(100 * plane.Uncertainty);
                        }
                    }

                    output.WriteLine(string.Create(Inv, $"{surface,-16} {lw}x{lh}  {count,5}  {errors.Count,2}/{scenes}  {Median(errors),12:0.000}  {(errors.Count > 0 ? errors.Max() : double.NaN),11:0.000}  {Median(claimed),8:0.000}"));
                }
            }
        }

        return 0;
    }

    private static double Median(List<double> v) => v.Count == 0 ? double.NaN : v.Order().ElementAt(v.Count / 2);

    /// <summary>The weakest side of the outline the finder chose, from what it said it tried; NaN where it chose none.</summary>
    private static double Weakest(IReadOnlyList<string> tried) =>
        tried.Select(t => ChoseLine().Match(t)).FirstOrDefault(m => m.Success) is { } m ? double.Parse(m.Groups[1].Value, Inv) : double.NaN;

    [GeneratedRegex(@"^chose .*: weakest side ([0-9.]+) on an edge")]
    private static partial Regex ChoseLine();

    /// <summary>
    /// A scene: the target on the surface, photographed. With <paramref name="gap"/> the four brackets lie that many millimetres off the
    /// target's corners and two bars that far off its bottom and left edges (null: no markers).
    /// </summary>
    private static (Mat Photo, PointD[] Truth) Scene(Flat flat, string surface, Random rng, bool outOfFrame, out double[] wallToImage, double? gap = null,
        (int Width, int Height, int Count, int Serial)? labels = null)
    {
        double w = flat.Width, h = flat.Height, margin = gap is null ? 1.5 : 5;
        double left = -margin, top = -margin, right = w + margin, bottom = h + margin;
        double cx = (left + right) / 2, cy = (top + bottom) / 2, sceneW = right - left, sceneH = bottom - top;
        if (outOfFrame)
        {
            cx += 0.45 * w;
        }

        double tilt = rng.NextDouble() * 30 * Math.PI / 180, axis = rng.NextDouble() * 2 * Math.PI, roll = PosterTrial.Gauss(rng, 8 * Math.PI / 180);
        double fill = 0.7 + (rng.NextDouble() * 0.22);
        double distance = PosterTrial.Focal * Math.Max(sceneW / (W * fill), sceneH / (H * fill)) * (1 + (0.4 * Math.Sin(tilt)));
        double[] rot = PosterTrial.Multiply(PosterTrial.Rodrigues(Math.Cos(axis) * tilt, Math.Sin(axis) * tilt), PosterTrial.RotZ(roll));
        double[] t = [-(rot[0] * cx) - (rot[1] * cy), -(rot[3] * cx) - (rot[4] * cy), distance - (rot[6] * cx) - (rot[7] * cy)];
        double[] k = [PosterTrial.Focal, 0, W / 2.0, 0, PosterTrial.Focal, H / 2.0, 0, 0, 1];
        wallToImage = PosterTrial.Multiply(k, [rot[0], rot[1], t[0], rot[3], rot[4], t[1], rot[6], rot[7], t[2]]);

        var photo = Surface(surface, rng, wallToImage);
        PosterTrial.Place(photo, flat.Picture, flat.Dpi, wallToImage);
        if (gap is { } g)
        {
            double off = g / 25.4;
            PointD[] at = [new(-off, -off), new(w + off, -off), new(w + off, h + off), new(-off, h + off)];
            for (int piece = 1; piece <= 4; piece++)
            {
                var (picture, mask, origin) = MarkerTrial.Bracket(piece);
                MarkerTrial.Put(photo, picture, mask, origin, wallToImage, at[piece - 1], PosterTrial.Gauss(rng, 0.5 * Math.PI / 180));
                picture.Dispose();
                mask.Dispose();
            }

            // Bars along the bottom and up the left, their near edge the gap away from the target's edge (past the brackets' arms).
            double half = ScaleMarkerLayout.BarWidth / 2 / 25.4, length = ScaleMarkerLayout.InchBarLength / 25.4;
            using (var bar = MarkerTrial.Bar(1))
            {
                MarkerTrial.Put(photo, bar.Picture, bar.Mask, bar.Origin, wallToImage, new PointD((w / 2) - (length / 2), h + off + half + 1.0), PosterTrial.Gauss(rng, 0.5 * Math.PI / 180));
            }

            using (var bar = MarkerTrial.Bar(2))
            {
                MarkerTrial.Put(photo, bar.Picture, bar.Mask, bar.Origin, wallToImage, new PointD(-off - half - 1.0, (h / 2) - (length / 2)), (Math.PI / 2) + PosterTrial.Gauss(rng, 0.5 * Math.PI / 180));
            }
        }

        if (labels is { } l)
        {
            // Stuck an inch in from the top left corner, and the second an inch in from the bottom right, square to the target.
            var pages = GroupLab.Core.ScaleMarkers.ScaleLabels.Pages(l.Width, l.Height, l.Serial, l.Count, "M220");
            PointD[] at = [new(1, 1), new(w - 1 - (l.Width / 25.4), h - 1 - (l.Height / 25.4))];
            for (int i = 0; i < l.Count; i++)
            {
                var (pw, ph, bgr) = GroupLab.Core.Rendering.SceneRasterizer.RasterizeBgr(pages[i], 25.4 * 16, words: true);
                using var picture = Mat.FromPixelData(ph, pw, MatType.CV_8UC3, bgr);
                using var mask = new Mat(picture.Size(), MatType.CV_8UC1, Scalar.White);
                MarkerTrial.Put(photo, picture, mask, new PointD(0, 0), wallToImage, at[i], PosterTrial.Gauss(rng, 1 * Math.PI / 180));
            }
        }

        var toImage = new Homography(wallToImage);
        PointD[] truth = [toImage.Apply(new(0, 0)), toImage.Apply(new(w, 0)), toImage.Apply(new(w, h)), toImage.Apply(new(0, h))];
        Conditions(photo, flat, truth, rng);
        Cv2.ImEncode(".jpg", photo, out byte[] jpeg, [(int)ImwriteFlags.JpegQuality, 85]);
        photo.Dispose();
        return (Cv2.ImDecode(jpeg, ImreadModes.Color), truth);
    }

    /// <summary>A shadow across one edge, glare on a glossy target, tape over a corner, a colour cast, blur and noise.</summary>
    private static void Conditions(Mat photo, Flat flat, PointD[] truth, Random rng)
    {
        if (rng.NextDouble() < 0.3)
        {
            // A hand or phone's shadow: a soft dark band across one side.
            int side = rng.Next(4);
            var a = truth[side];
            var b = truth[(side + 1) % 4];
            var mid = new PointD((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            using var shade = new Mat(photo.Size(), MatType.CV_8UC1, Scalar.Black);
            Cv2.Ellipse(shade, new RotatedRect(new Point2f((float)mid.X, (float)mid.Y), new Size2f(700 + rng.Next(800), 300 + rng.Next(300)), (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI)), Scalar.White, -1);
            Cv2.GaussianBlur(shade, shade, new Size(0, 0), 60);
            using var shaded = new Mat();
            photo.ConvertTo(shaded, MatType.CV_32FC3);
            using var factor = new Mat();
            shade.ConvertTo(factor, MatType.CV_32FC1, -0.45 / 255, 1);
            using var factor3 = new Mat();
            Cv2.Merge([factor, factor, factor], factor3);
            Cv2.Multiply(shaded, factor3, shaded);
            shaded.ConvertTo(photo, MatType.CV_8UC3);
        }

        if (flat.Glossy && rng.NextDouble() < 0.6)
        {
            var c = new PointD(truth.Average(p => p.X) + PosterTrial.Gauss(rng, 300), truth.Average(p => p.Y) + PosterTrial.Gauss(rng, 300));
            using var glare = new Mat(photo.Size(), MatType.CV_8UC3, Scalar.Black);
            Cv2.Ellipse(glare, new RotatedRect(new Point2f((float)c.X, (float)c.Y), new Size2f(500 + rng.Next(700), 250 + rng.Next(300)), rng.Next(180)), new Scalar(150, 150, 150), -1);
            Cv2.GaussianBlur(glare, glare, new Size(0, 0), 70);
            Cv2.Add(photo, glare, photo);
        }

        if (rng.NextDouble() < 0.15)
        {
            // Tape over a corner: a translucent beige strip across it.
            var corner = truth[rng.Next(4)];
            using var tape = photo.Clone();
            var box = new RotatedRect(new Point2f((float)corner.X, (float)corner.Y), new Size2f(320, 110), rng.Next(180));
            Cv2.Ellipse(tape, box, new Scalar(170, 205, 220), -1);
            Cv2.AddWeighted(photo, 0.45, tape, 0.55, 0, photo);
        }

        // Warm or cool light.
        double warm = PosterTrial.Gauss(rng, 0.08);
        var channels = photo.Split();
        channels[0].ConvertTo(channels[0], -1, 1 - warm);
        channels[2].ConvertTo(channels[2], -1, 1 + warm);
        Cv2.Merge(channels, photo);
        foreach (var ch in channels)
        {
            ch.Dispose();
        }

        Cv2.GaussianBlur(photo, photo, new Size(0, 0), 0.6 + (rng.NextDouble() * 0.9));
        using var noise = new Mat(photo.Size(), MatType.CV_16SC3);
        Cv2.Randn(noise, Scalar.All(0), Scalar.All(4));
        using var wide = new Mat();
        photo.ConvertTo(wide, MatType.CV_16SC3);
        Cv2.Add(wide, noise, wide);
        wide.ConvertTo(photo, MatType.CV_8UC3);
    }

    /// <summary>
    /// A surface, drawn in its own inches and laid under the camera like the target, so its grain and seams keep their perspective. 1 px a
    /// 40th of an inch over 60 by 60 in.
    /// </summary>
    private static Mat Surface(string surface, Random rng, double[] wallToImage)
    {
        const double dpi = 30, extent = 90;
        int size = (int)(extent * dpi);
        var s = new Mat(size, size, MatType.CV_8UC3);
        Scalar Jitter(Scalar c, int by) => new(c.Val0 + rng.Next(-by, by + 1), c.Val1 + rng.Next(-by, by + 1), c.Val2 + rng.Next(-by, by + 1));
        void Noise(double sigma)
        {
            using var n = new Mat(s.Size(), MatType.CV_16SC3);
            Cv2.Randn(n, Scalar.All(0), Scalar.All(sigma));
            using var wide = new Mat();
            s.ConvertTo(wide, MatType.CV_16SC3);
            Cv2.Add(wide, n, wide);
            wide.ConvertTo(s, MatType.CV_8UC3);
        }

        void Grain(Scalar dark, Scalar light, double plank)
        {
            var ring = new double[size];
            double phase = rng.NextDouble() * 10;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double v = 0.5 + (0.5 * Math.Sin((y * 0.09) + (3 * Math.Sin((x * 0.004) + phase + (y * 0.002))) + (0.6 * Math.Sin(x * 0.03))));
                    s.Set(y, x, new Vec3b((byte)(dark.Val0 + ((light.Val0 - dark.Val0) * v)), (byte)(dark.Val1 + ((light.Val1 - dark.Val1) * v)), (byte)(dark.Val2 + ((light.Val2 - dark.Val2) * v))));
                }
            }

            // Plank seams: straight dark lines, the kind that ran beside the paper's edges on Alan's floor.
            for (double y = rng.NextDouble() * plank * dpi; y < size; y += plank * dpi)
            {
                Cv2.Line(s, new Point(0, (int)y), new Point(size, (int)y), new Scalar(dark.Val0 * 0.5, dark.Val1 * 0.5, dark.Val2 * 0.5), 2);
            }

            Noise(6);
        }

        switch (surface)
        {
            case "black table":
                s.SetTo(Jitter(new Scalar(25, 25, 28), 6));
                Noise(4);
                break;
            case "dark grey table":
                s.SetTo(Jitter(new Scalar(70, 72, 75), 8));
                Noise(5);
                break;
            case "dark wood":
                Grain(new Scalar(25, 45, 70), new Scalar(50, 80, 115), 5);
                break;
            case "light wood":
                Grain(new Scalar(80, 140, 190), new Scalar(120, 180, 225), 7);
                break;
            case "white counter":
                s.SetTo(Jitter(new Scalar(232, 234, 235), 4));
                Noise(5);
                break;
            case "cream counter":
                s.SetTo(Jitter(new Scalar(195, 220, 232), 6));
                Noise(6);
                break;
            case "cardboard":
            case "backer with holes":
            case "over old targets":
                s.SetTo(Jitter(new Scalar(105, 145, 185), 10));
                Noise(10);
                Cv2.GaussianBlur(s, s, new Size(9, 1), 0);
                if (surface == "backer with holes")
                {
                    for (int i = 0; i < 400; i++)
                    {
                        Cv2.Circle(s, new Point(rng.Next(size), rng.Next(size)), 2 + rng.Next(5), new Scalar(25, 30, 35), -1);
                    }

                    for (int i = 0; i < 40; i++)
                    {
                        var p = new Point(rng.Next(size), rng.Next(size));
                        Cv2.Line(s, p, new Point(p.X + 18, p.Y + rng.Next(-3, 4)), new Scalar(150, 150, 150), 2);
                    }

                    for (int i = 0; i < 12; i++)
                    {
                        Cv2.Rectangle(s, new Rect(rng.Next(size), rng.Next(size), 40 + rng.Next(80), 25), new Scalar(150, 195, 215), -1);
                    }
                }

                if (surface == "over old targets")
                {
                    // Older white targets with black rings, shot, under the new one.
                    for (int i = 0; i < 4; i++)
                    {
                        var c = new Point(size / 2 + rng.Next(-500, 500), size / 2 + rng.Next(-500, 500));
                        Cv2.Rectangle(s, new Rect(c.X - 300, c.Y - 300, 600, 600), new Scalar(230, 232, 234), -1);
                        for (int r = 280; r > 20; r -= 50)
                        {
                            Cv2.Circle(s, c, r, Scalar.Black, 4);
                        }

                        for (int j = 0; j < 30; j++)
                        {
                            Cv2.Circle(s, new Point(c.X + rng.Next(-250, 250), c.Y + rng.Next(-250, 250)), 3, new Scalar(20, 20, 20), -1);
                        }
                    }
                }

                break;
            case "OSB":
                s.SetTo(new Scalar(110, 160, 200));
                for (int i = 0; i < 6000; i++)
                {
                    var c = new Point(rng.Next(size), rng.Next(size));
                    Cv2.Ellipse(s, new RotatedRect(new Point2f(c.X, c.Y), new Size2f(30 + rng.Next(90), 8 + rng.Next(20)), rng.Next(180)), Jitter(new Scalar(90, 140, 185), 30), -1);
                }

                Noise(6);
                break;
            case "foam board":
                s.SetTo(Jitter(new Scalar(240, 242, 242), 3));
                Noise(3);
                break;
            case "grass":
                s.SetTo(new Scalar(40, 110, 60));
                for (int i = 0; i < 30000; i++)
                {
                    var p = new Point(rng.Next(size), rng.Next(size));
                    Cv2.Line(s, p, new Point(p.X + rng.Next(-4, 5), p.Y - 6 - rng.Next(10)), Jitter(new Scalar(45, 125, 70), 35), 1);
                }

                break;
            case "gravel":
                s.SetTo(new Scalar(120, 125, 130));
                for (int i = 0; i < 20000; i++)
                {
                    Cv2.Circle(s, new Point(rng.Next(size), rng.Next(size)), 2 + rng.Next(5), Jitter(new Scalar(125, 128, 132), 50), -1);
                }

                break;
            default:
                s.SetTo(Jitter(new Scalar(80, 70, 95), 10));
                Noise(14);
                break;
        }

        var photo = new Mat(H, W, MatType.CV_8UC3, Scalar.Black);
        // The surface's own inches start 30 in up and left of the target's origin.
        PosterTrial.Place(photo, s, dpi, PosterTrial.Multiply(wallToImage, [1, 0, -extent / 2, 0, 1, -extent / 2, 0, 0, 1]));
        s.Dispose();
        return photo;
    }

    /// <summary>The blank targets, flat: the scans at their own 600 dpi, and the phone photos flattened by their checked corners, 12 in wide.</summary>
    private static List<Flat> Flats()
    {
        string root = @"C:\Dev\grouplab-local\commercial-targets";
        var flats = new List<Flat>();
        foreach (var (id, corners) in new (string, double[])[]
        {
            ("bc-34105-shoot-n-c-sight-in", [3, 3, 4956, 3, 4956, 4875, 3, 4890]), ("bc-34550-shoot-n-c-6in-bull", [3, 3, 3721, 3, 3727, 3708, 3, 3719]),
            ("bc-34805-shoot-n-c-8in-bull", [3, 3, 4956, 3, 4956, 4947, 3, 4975]), ("bc-34806-shoot-n-c-8in-crosshair", [3, 3, 4956, 3, 4956, 4887, 3, 4912]),
        })
        {
            string path = Path.Combine(root, id, "blank.png");
            if (File.Exists(path))
            {
                flats.Add(Flatten("scan " + id, path, corners, 600, glossy: true, scan: true));
            }
        }

        string photos = Path.Combine(root, "corner-photos-2026-10-03");
        foreach (var (file, corners) in new (string, double[])[]
        {
            ("allen-ezaim-sight-in-55134A.jpg", [668, 121, 3503, 152, 3528, 2802, 668, 2847]), ("allen-splash-bull-55124A.jpg", [847, 152, 3506, 185, 3524, 2845, 831, 2874]),
            ("eze-scorer-bull.jpg", [273, 577, 2899, 572, 2910, 3189, 280, 3212]), ("eze-scorer-sight-in-grid.jpg", [208, 516, 2844, 459, 2845, 3137, 204, 3137]),
            ("ntc-st4-100yd-precision-rifle.jpg", [170, 221, 2831, 213, 2864, 3246, 154, 3260]), ("birchwood-shoot-n-c-12in-sight-in.jpg", [849, 178, 3530, 159, 3522, 2815, 876, 2844]),
        })
        {
            string path = Path.Combine(photos, file);
            if (File.Exists(path))
            {
                flats.Add(Flatten("photo " + file, path, corners, 0, glossy: file.Contains("shoot-n-c", StringComparison.Ordinal) || file.Contains("splash", StringComparison.Ordinal), scan: false));
            }
        }

        return flats;
    }

    private static Flat Flatten(string name, string path, double[] c, double dpi, bool glossy, bool scan)
    {
        using var raw = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
        var (_, meta) = ImageLoader.Load(path, 1);
        using var up = scan ? raw.Clone() : UprightMat.Apply(raw, meta.Orientation);
        PointD[] q = [new(c[0], c[1]), new(c[2], c[3]), new(c[4], c[5]), new(c[6], c[7])];
        double wPx = (Distance(q[0], q[1]) + Distance(q[3], q[2])) / 2, hPx = (Distance(q[0], q[3]) + Distance(q[1], q[2])) / 2;
        double inches = scan ? wPx / dpi : 12;
        double outDpi = 100;
        int ow = (int)Math.Round(inches * outDpi), oh = (int)Math.Round(inches * hPx / wPx * outDpi);
        var src = q.Select(p => new Point2f((float)p.X, (float)p.Y)).ToArray();
        var dst = new[] { new Point2f(0, 0), new Point2f(ow, 0), new Point2f(ow, oh), new Point2f(0, oh) };
        using var m = Cv2.GetPerspectiveTransform(src, dst);
        var flat = new Mat();
        Cv2.WarpPerspective(up, flat, m, new Size(ow, oh), InterpolationFlags.Area);
        return new Flat(name, flat, outDpi, ow / outDpi, oh / outDpi, glossy);
    }

    private static int Option(IReadOnlyList<string> args, string name, int fallback)
    {
        int i = args.ToList().IndexOf(name);
        return i >= 0 && i + 1 < args.Count && int.TryParse(args[i + 1], NumberStyles.Integer, Inv, out int v) ? v : fallback;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
}
