using System.Globalization;
using System.Text.Json.Nodes;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab st4</c>, NOTES-FROM-PLANNING.md entries 158 and 172: the 2026-09-20 National Target Company ST-4 sheet measured against Alan's
/// ground truth. For each frame, the sheet's orange 1 in grid registered as dense ground truth, with the residual of a plain homography
/// and of one with the lens's radial terms; the holes found by the neutral darkness detector, placed in grid inches and counted against each
/// group's known shots; and each group's center, so the frames that show the same group can be compared. Nothing is written but the table.
/// </summary>
public static class St4Verb
{
    public const string Usage = "grouplab st4 <st4-2026-09-20.json> [--frames <folder>]";

    private sealed record Group(int Id, double X, double Y, int Shots, IReadOnlyList<string> Frames);

    public static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        if (args.Count < 1 || !File.Exists(args[0]))
        {
            error.WriteLine(Usage);
            return 2;
        }

        var truth = JsonNode.Parse(File.ReadAllText(args[0]))!;
        string folder = args.Count >= 3 && args[1] == "--frames" ? args[2] : (string)truth["folder"]!;
        var groups = truth["groups"]!.AsArray().Select(g => new Group((int)g!["group"]!, (double)g["xInches"]!, (double)g["yInches"]!, (int)g["shots"]!,
            [(string)g["closeUp"]!, .. g["alsoIn"]!.AsArray().Select(f => (string)f!)])).ToList();
        double calibre = (double)truth["bulletInches"]!;
        var inv = CultureInfo.InvariantCulture;
        var backend = new OpenCvSharpBackend();
        var centres = new Dictionary<int, List<(string Frame, PointD Centre, int Blobs)>>();
        int allTruth = 0, allFound = 0, allMissed = 0, allFalse = 0;

        foreach (var frame in truth["frames"]!.AsArray())
        {
            string file = (string)frame!["file"]!;
            string path = Path.Combine(folder, file);
            if (!File.Exists(path))
            {
                output.WriteLine($"{file}: not on this machine");
                continue;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var metadata = ImageMetadataReader.Read(bytes);
            using var colour = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation);
            int w = colour.Width, h = colour.Height;
            var channels = Cv2.Split(colour);
            var blue = OpenCvSharpBackend.Copy(channels[0]).Pixels;
            var red = OpenCvSharpBackend.Copy(channels[2]).Pixels;
            foreach (var channel in channels)
            {
                channel.Dispose();
            }

            // The grid is orange: red well above blue, where paper and holes have the two nearly equal.
            var lines = new bool[w * h];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = red[i] - blue[i] > 25;
            }

            var thin = GridRegistration.Thin(lines, w, h, 12);
            var crossings = GridRegistration.Crossings(thin, w, h, 40);
            var (lattice, spacing) = GridRegistration.Index(crossings, w, h);
            var fit = GridRegistration.Fit(lattice, spacing, w, h);
            if (fit is null || lattice.Count < 20)
            {
                output.WriteLine(string.Create(inv, $"{file}: the grid was not registered: {crossings.Count} crossings, {lattice.Count} in the lattice, step {spacing:0} px; crossings {string.Join(" ", crossings.Take(60).Select(c => $"{c.X:0},{c.Y:0}"))}"));
                continue;
            }

            // Stored pixels to the upright view the photograph was taken in, then lattice units to grid inches, y up.
            Func<PointD, PointD> toLattice = fit.Lens is { } lens ? lens.ToPage : fit.ImageToLattice.Apply;
            int orientation = metadata.Orientation ?? 1;
            PointD Upright(PointD l) => orientation switch
            {
                6 => new PointD(-l.Y, l.X),
                8 => new PointD(l.Y, -l.X),
                3 => new PointD(-l.X, -l.Y),
                _ => l,
            };

            var (max, _) = ImageLoader.LoadMaxChannel(path);
            var holes = NeutralDarknessHoleDetector.Detect(max, spacing, backend, new HoleDetectionOptions(CalibreInches: calibre)).Holes;
            var upright = holes.Select(hole => (Hole: hole, At: Upright(toLattice(new PointD(hole.X, hole.Y))))).ToList();

            // The lattice is counted from wherever the walk began, so its offset to the sheet's own grid is the whole-inch shift that puts the
            // most holes beside the groups Alan placed, which are good to about half an inch. Only the groups this frame shows, as the planning
            // session matched them, are used: the sheet's four quadrants look alike, and holes alone could not tell one from another.
            var shown = groups.Where(g => g.Frames.Contains(file)).ToList();
            var best = (X: 0, Y: 0, Count: -1, Sum: double.MaxValue);
            for (int x0 = -14; x0 <= 14; x0++)
            {
                for (int y0 = -14; y0 <= 14; y0++)
                {
                    int count = 0;
                    double sum = 0;
                    foreach (var (_, at) in upright)
                    {
                        double d = shown.Min(g => Math.Sqrt(Math.Pow(at.X + x0 - g.X, 2) + Math.Pow(-at.Y + y0 - g.Y, 2)));
                        if (d <= 1.0)
                        {
                            count++;
                            sum += d;
                        }
                    }

                    if (count > best.Count || (count == best.Count && sum < best.Sum))
                    {
                        best = (x0, y0, count, sum);
                    }
                }
            }

            PointD Grid(PointD at) => new(at.X + best.X, -at.Y + best.Y);

            // A group is in the frame where its place maps inside the image with a margin of an inch's worth of pixels.
            var fromGrid = fit.ImageToLattice.Inverse();
            bool Inside(Group g)
            {
                var l = new PointD(g.X - best.X, -(g.Y - best.Y));
                var stored = orientation switch
                {
                    6 => new PointD(l.Y, -l.X),
                    8 => new PointD(-l.Y, l.X),
                    3 => new PointD(-l.X, -l.Y),
                    _ => l,
                };
                var image = fromGrid.Apply(stored);
                return image.X > spacing && image.Y > spacing && image.X < w - spacing && image.Y < h - spacing;
            }

            output.WriteLine(string.Create(inv,
                $"{file}: {lattice.Count} crossings, {spacing:0} px an inch, fit rms {fit.HomographyRms:0.0000} in by homography, {fit.LensRms:0.0000} in with the lens; {holes.Count} marks found"));
            int frameFalse = 0;
            var byGroup = groups.ToDictionary(g => g.Id, _ => new List<(DetectedHole Hole, PointD At)>());
            foreach (var (hole, at) in upright)
            {
                var g = Grid(at);
                var nearest = groups.MinBy(q => Math.Sqrt(Math.Pow(g.X - q.X, 2) + Math.Pow(g.Y - q.Y, 2)))!;
                if (Math.Sqrt(Math.Pow(g.X - nearest.X, 2) + Math.Pow(g.Y - nearest.Y, 2)) <= 1.0)
                {
                    byGroup[nearest.Id].Add((hole, g));
                }
                else if (Math.Abs(g.X) <= 6.5 && Math.Abs(g.Y) <= 7.5)
                {
                    // Only a mark on the printed grid counts against the detector: the board around the sheet is full of old holes.
                    frameFalse++;
                }
            }

            foreach (var g in groups.Where(Inside))
            {
                var found = byGroup[g.Id];
                int blobs = found.Count;
                int missed = Math.Max(0, g.Shots - blobs), extra = Math.Max(0, blobs - g.Shots);
                allTruth += g.Shots;
                allFound += Math.Min(blobs, g.Shots);
                allMissed += missed;
                allFalse += extra;
                var centre = blobs == 0 ? new PointD(double.NaN, double.NaN) : new PointD(found.Average(f => f.At.X), found.Average(f => f.At.Y));
                if (blobs > 0)
                {
                    (centres.TryGetValue(g.Id, out var list) ? list : centres[g.Id] = []).Add((file, centre, blobs));
                }

                output.WriteLine(string.Create(inv,
                    $"  group {g.Id,2}: {g.Shots,2} shots, {blobs,2} marks, {missed} missed, {extra} over; marks {string.Join(" ", found.Select(f => $"{f.Hole.DiameterInches:0.00}"))} in across; center ({centre.X:0.00}, {centre.Y:0.00})"));
            }

            allFalse += frameFalse;
            output.WriteLine($"  {frameFalse} marks on the grid near no group");
        }

        output.WriteLine(string.Create(inv, $"over every frame: {allTruth} shots in view, {allFound} found as a mark of their own, {allMissed} missed, {allFalse} marks over or near no group"));
        output.WriteLine("the same group's center in each frame that shows it, grid inches:");
        foreach (var (id, list) in centres.OrderBy(c => c.Key))
        {
            if (list.Count < 2)
            {
                continue;
            }

            double mx = list.Average(c => c.Centre.X), my = list.Average(c => c.Centre.Y);
            double spread = list.Max(c => Math.Sqrt(Math.Pow(c.Centre.X - mx, 2) + Math.Pow(c.Centre.Y - my, 2)));
            output.WriteLine(string.Create(inv,
                $"  group {id,2}: {string.Join("; ", list.Select(c => $"{c.Frame[9..15]} ({c.Centre.X:0.00}, {c.Centre.Y:0.00}) from {c.Blobs}"))}; furthest from their mean {spread:0.000} in"));
        }

        return 0;
    }
}
