using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Cli.Spike;

/// <summary>
/// docs/PHASE1-BRIEF.md sections 4.3 and 4.4: render-and-difference and the ported baseline against truth on synthetic
/// GroupLab sheets. The sheet is the live <c>GL-CF25-LTR</c>, the one a user prints. Holes come from
/// <see cref="SyntheticSheet"/>, drawn from the survey's measured distributions, around every bull.
/// <list type="bullet">
/// <item><b>Main:</b> 600 and 300 DPI, one and two holes per bull, exact registration.</item>
/// <item><b>Registration:</b> 600 DPI, one hole per bull, render-and-difference given a registration wrong by a known
/// offset, from 0.005 to 0.08 in, which is what couples M2 to M1.</item>
/// <item><b>Hard cases</b> the survey documented, one set each: overlapping pairs, X marks over holes, captions and
/// arrowheads in hand ink, holes on the edge of a bull's outer ring, and a dark backing as a photograph has.</item>
/// </list>
/// A detection matches a truth hole within 0.15 in, pairs taken nearest first; the gate's centre tolerance is 0.01 in, and a
/// stray is a detection with no truth hole within 0.15 in. The baseline sees the same images and ignores registration.
/// </summary>
public static class HolesSynthetic
{
    private const double DmmPerInch = 254, Loose = 0.15, Tight = 0.01;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly object ProgressLock = new();

    private sealed record Case(string Group, string Name, double Dpi, int PerBull, double ErrorInches, string Hard, int Seeds, double RotationDegrees = 0);

    private sealed record Trial(string Group, string Name, double Dpi, int PerBull, double ErrorInches, string Method, int Seed, int Holes, int FoundTight, int FoundLoose, int Strays,
        IReadOnlyList<double> CentreErrorsInches, IReadOnlyList<object> Missed, IReadOnlyList<object> StrayDetections, IReadOnlyList<double> MatchedClosures, IReadOnlyList<double> StrayClosures,
        int AssignedRight = 0, int NearestRight = 0, int AssignmentDiffers = 0, int AssignmentFlagged = 0, string AssignmentMethod = "",
        IReadOnlyList<double>? SingleElongations = null, IReadOnlyList<double>? PairElongations = null, int SplitHalves = 0, int SplitHalvesMatched = 0, int Oversized = 0, int OversizedOverTwo = 0, int MergedUnflagged = 0);

    private sealed record Realism(double DiameterInches, double CoreV, double AnnulusMinimumV, double RaggednessInches);

    /// <param name="realismOnly">Run only the main 600 DPI set and report the realism table, to calibrate the synthesis without the sweep.</param>
    /// <param name="heldOut">
    /// Draw every sheet from seeds offset by 1000, which nothing was developed against. The development seeds are the
    /// default; a change to the detector is made on them and the gate is read once on these (docs/PHASE1-BRIEF.md section 7).
    /// </param>
    public static int Run(string targets, string phase1Scans, TextWriter output, bool realismOnly = false, bool heldOut = false)
    {
        ArgumentNullException.ThrowIfNull(output);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        void Progress(string line)
        {
            lock (ProgressLock)
            {
                output.WriteLine(string.Create(Inv, $"[{clock.Elapsed.TotalSeconds,6:0}s] {line}"));
                output.Flush();
            }
        }

        var definition = GltdJsonReader.Read(File.ReadAllBytes(Path.Combine(targets, "GL-CF25-LTR.gltd.json"))).Definition!;
        var cases = new List<Case>();
        foreach (double dpi in (double[])[600, 300])
        {
            foreach (int perBull in (int[])[1, 2])
            {
                cases.Add(new Case("main", "exact registration", dpi, perBull, 0, "", 3));
            }
        }

        foreach (double error in (double[])[0.005, 0.01, 0.02, 0.04, 0.08])
        {
            cases.Add(new Case("registration", string.Create(Inv, $"registration off by {error:0.000} in"), 600, 1, error, "", 2));
        }

        // A rotation about the page centre, which no single shift absorbs: 0.25 and 0.5 degrees move the page corners 0.030 and 0.061 in.
        foreach (double degrees in (double[])[0.25, 0.5])
        {
            cases.Add(new Case("registration", string.Create(Inv, $"registration rotated {degrees:0.00} deg"), 600, 1, 0, "", 2, degrees));
        }

        foreach (string hard in (string[])["overlapping pairs", "X marks over holes", "captions and arrowheads", "holes on the outer ring edge", "dark backing", "cross-cell shots"])
        {
            cases.Add(new Case("hard", hard, 600, hard == "overlapping pairs" ? 2 : 1, 0, hard, 2));
        }

        // Past the local alignment's 0.1 in reach, to find where it breaks (docs/PHASE1-BRIEF.md section 4.4, gate 2). Appended
        // last, because a case's random stream follows its index, so every earlier case draws the sheets it drew before.
        foreach (double error in (double[])[0.12, 0.16, 0.24])
        {
            cases.Add(new Case("registration", string.Create(Inv, $"registration off by {error:0.000} in"), 600, 1, error, "", 2));
        }

        if (realismOnly)
        {
            cases = [];
        }

        var renders = new Dictionary<double, GrayImage>();
        foreach (double dpi in cases.Select(c => c.Dpi).Distinct())
        {
            renders[dpi] = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);
        }

        var jobs = cases.SelectMany((c, ci) => Enumerable.Range(1, c.Seeds).Select(seed => (Case: c, Index: ci, Seed: seed))).ToList();
        Progress($"{jobs.Count} synthetic sheets of GL-CF25-LTR in {cases.Count} cases");
        var trials = new List<Trial>[jobs.Count];
        var realism = new List<Realism>[jobs.Count];
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = 2 }, j =>
        {
            var (c, index, seed) = jobs[j];
            int drawn = seed + (heldOut ? 1000 : 0);
            (trials[j], realism[j]) = RunTrial(definition, renders[c.Dpi], c, drawn, (drawn * 7919) + (index * 104729));
            var rd = trials[j].Single(t => t.Method == "render-and-difference");
            var nd = trials[j].Single(t => t.Method == "baseline");
            Progress(string.Create(Inv, $"{c.Group}, {c.Name}, {c.Dpi:0} DPI, {c.PerBull} per bull, seed {seed}: render-and-difference {rd.FoundTight}/{rd.FoundLoose} of {rd.Holes} within 0.01/0.15 in, {rd.Strays} strays; baseline {nd.FoundTight}/{nd.FoundLoose}, {nd.Strays} strays"));
        });

        var all = trials.SelectMany(t => t).ToList();
        output.WriteLine();
        output.WriteLine("Render-and-difference and the baseline against truth, summed over seeds. Found: matched to a truth hole within 0.01 in, and within 0.15 in. Strays: detections with no truth hole within 0.15 in.");
        output.WriteLine();
        output.WriteLine("| Group | Case | DPI | Holes per bull | Method | Holes | Found within 0.01 in | Found within 0.15 in | Strays | Centre error median / 95th pct (in) |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var group in all.GroupBy(t => (t.Group, t.Name, t.Dpi, t.PerBull, t.Method)))
        {
            var g = group.ToList();
            int holes = g.Sum(t => t.Holes), tight = g.Sum(t => t.FoundTight), loose = g.Sum(t => t.FoundLoose), strays = g.Sum(t => t.Strays);
            var errors = g.SelectMany(t => t.CentreErrorsInches).Order().ToList();
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Group} | {group.Key.Name} | {group.Key.Dpi:0} | {group.Key.PerBull} | {group.Key.Method} | {holes} | {tight} ({100.0 * tight / holes:0.0}%) | {loose} ({100.0 * loose / holes:0.0}%) | {strays} | {Quantile(errors, 0.5):0.0000} / {Quantile(errors, 0.95):0.0000} |"));
        }

        output.WriteLine();
        output.WriteLine("Render-and-difference rim closure: the fraction of 360 rays from a detection's centre meeting residual within one hull diameter, for detections matched to a truth hole and for strays.");
        output.WriteLine();
        output.WriteLine("| Group | Case | DPI | Holes per bull | Matched n | Matched min / 1st / 5th pct | Strays n | Strays median / max |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (var group in all.Where(t => t.Method == "render-and-difference").GroupBy(t => (t.Group, t.Name, t.Dpi, t.PerBull)))
        {
            var matched = group.SelectMany(t => t.MatchedClosures).Order().ToList();
            var strays = group.SelectMany(t => t.StrayClosures).Order().ToList();
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Group} | {group.Key.Name} | {group.Key.Dpi:0} | {group.Key.PerBull} | {matched.Count} | {matched.DefaultIfEmpty(double.NaN).First():0.000} / {Quantile(matched, 0.01):0.000} / {Quantile(matched, 0.05):0.000} | {strays.Count} | {Quantile(strays, 0.5):0.000} / {strays.DefaultIfEmpty(double.NaN).Last():0.000} |"));
        }

        output.WriteLine();
        output.WriteLine("Assignment of render-and-difference's detections, stage S9: one-to-one where detections are no more than bulls, otherwise nearest-bull with every shot flagged; and nearest-bull alone. Right: a detection matched to a truth hole and given the bull that hole was drawn for. Differs and flagged count every detection, strays included.");
        output.WriteLine();
        output.WriteLine("| Group | Case | DPI | Holes per bull | S9 method | Matched | S9 right | Nearest-bull right | S9 differs from nearest | Flagged |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var group in all.Where(t => t.Method == "render-and-difference").GroupBy(t => (t.Group, t.Name, t.Dpi, t.PerBull)))
        {
            var g = group.ToList();
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Group} | {group.Key.Name} | {group.Key.Dpi:0} | {group.Key.PerBull} | {string.Join(", ", g.Select(t => t.AssignmentMethod).Distinct())} | {g.Sum(t => t.FoundLoose)} | {g.Sum(t => t.AssignedRight)} | {g.Sum(t => t.NearestRight)} | {g.Sum(t => t.AssignmentDiffers)} | {g.Sum(t => t.AssignmentFlagged)} |"));
        }

        output.WriteLine();
        output.WriteLine("Render-and-difference's flags. Split: holes split from one blob as a possible merge, and how many were matched to a truth hole. Oversized: whole marks flagged as covering 1.35 single holes or more, a single hole being the sheet's quarter-point round mark (entries 81 and 82), and how many have two or more truth holes within half their hull diameter. The synthetic holes mix the survey's calibres, so this counts more flags than a one-calibre sheet would. Unflagged merges: unsplit, unflagged detections with two or more truth holes within half their hull diameter, reported silently as one.");
        output.WriteLine();
        output.WriteLine("| Group | Case | DPI | Holes per bull | Split halves | Split halves matched | Oversized | Oversized over two or more holes | Unflagged merges |");
        output.WriteLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var group in all.Where(t => t.Method == "render-and-difference").GroupBy(t => (t.Group, t.Name, t.Dpi, t.PerBull)))
        {
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Group} | {group.Key.Name} | {group.Key.Dpi:0} | {group.Key.PerBull} | {group.Sum(t => t.SplitHalves)} | {group.Sum(t => t.SplitHalvesMatched)} | {group.Sum(t => t.Oversized)} | {group.Sum(t => t.OversizedOverTwo)} | {group.Sum(t => t.MergedUnflagged)} |"));
        }

        output.WriteLine();
        output.WriteLine("Render-and-difference's elongation, the ratio of the principal standard deviations of the residual in a detection's hull, unsplit detections only: those with one truth hole within half their hull diameter, and those with two or more, merged neighbours.");
        output.WriteLine();
        output.WriteLine("| Group | Case | DPI | Holes per bull | One hole n | One hole 95th / 99th pct / max | Two or more n | Two or more min / 5th pct / median |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (var group in all.Where(t => t.Method == "render-and-difference").GroupBy(t => (t.Group, t.Name, t.Dpi, t.PerBull)))
        {
            var one = group.SelectMany(t => t.SingleElongations ?? []).Order().ToList();
            var two = group.SelectMany(t => t.PairElongations ?? []).Order().ToList();
            output.WriteLine(string.Create(Inv,
                $"| {group.Key.Group} | {group.Key.Name} | {group.Key.Dpi:0} | {group.Key.PerBull} | {one.Count} | {Quantile(one, 0.95):0.00} / {Quantile(one, 0.99):0.00} / {one.DefaultIfEmpty(double.NaN).Last():0.00} | {two.Count} | {two.DefaultIfEmpty(double.NaN).First():0.00} / {Quantile(two, 0.05):0.00} / {Quantile(two, 0.5):0.00} |"));
        }

        var measured = Enumerable.Range(1, 3).AsParallel().WithDegreeOfParallelism(2).SelectMany(seed => Calibration(definition, 600, seed)).ToList();
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"Realism: the baseline's own measures of {measured.Count} synthetic holes on a blank page at 600 DPI, three seeds, against docs/SCAN-MEASUREMENTS.md section 3.6's 229 real holes on bare paper, mean ± sd. The comparison is on a blank page because every hole placed near a GL-CF25-LTR bull lies within reach of its solid black discs, which the survey's targets do not have and which merge with a hole in the baseline's hull."));
        output.WriteLine();
        output.WriteLine("| Quantity | Real (survey) | Synthetic |");
        output.WriteLine("|---|---|---|");
        output.WriteLine(string.Create(Inv, $"| hull diameter (in) | 0.2717 ± 0.0537 | {Mean(measured.Select(m => m.DiameterInches)):0.0000} ± {Sd(measured.Select(m => m.DiameterInches)):0.0000} |"));
        output.WriteLine(string.Create(Inv, $"| core mean V | 195.6 ± 24.5 | {Mean(measured.Select(m => m.CoreV)):0.00} ± {Sd(measured.Select(m => m.CoreV)):0.00} |"));
        output.WriteLine(string.Create(Inv, $"| annulus minimum V | 33.8 ± 30.0 | {Mean(measured.Select(m => m.AnnulusMinimumV)):0.00} ± {Sd(measured.Select(m => m.AnnulusMinimumV)):0.00} |"));
        output.WriteLine(string.Create(Inv, $"| raggedness, sd of rim radius (in) | 0.0456 | {Mean(measured.Select(m => m.RaggednessInches)):0.0000} ± {Sd(measured.Select(m => m.RaggednessInches)):0.0000} |"));

        if (!realismOnly)
        {
            RawMeasurements.Write(phase1Scans, heldOut ? "holes-synthetic-held-out" : "holes-synthetic", all);
        }

        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    private static (List<Trial> Trials, List<Realism> Realism) RunTrial(TargetDefinition definition, GrayImage render, Case c, int seed, int stream)
    {
        var random = new Random(stream);
        double dpi = c.Dpi, s = DmmPerInch / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        double offset = c.ErrorInches * DmmPerInch / Math.Sqrt(2);
        double theta = c.RotationDegrees * Math.PI / 180, cos = Math.Cos(theta), sin = Math.Sin(theta);
        double centreX = definition.Page.Width / 2.0, centreY = definition.Page.Height / 2.0;
        var registration = new HomographyMapping(new Homography([
            s * cos, -s * sin, (cos * ((0.5 * s) - centreX)) - (sin * ((0.5 * s) - centreY)) + centreX + offset,
            s * sin, s * cos, (sin * ((0.5 * s) - centreX)) + (cos * ((0.5 * s) - centreY)) + centreY + offset,
            0, 0, 1]));
        var backing = c.Hard == "dark backing" ? HoleBacking.Dark : HoleBacking.ScannerLid;

        bool OnInk(double x, double y)
        {
            int u = Math.Clamp((int)(x / s), 0, render.Width - 1), v = Math.Clamp((int)(y / s), 0, render.Height - 1);
            return render.Pixels[(v * render.Width) + u] < 128;
        }

        var holes = new List<SyntheticHole>();
        var strokes = new List<InkStroke>();
        var bullOf = new List<int>();
        int bullIndex = -1;
        foreach (var bull in definition.Bulls)
        {
            bullIndex++;
            double outerRadius = definition.RingSets.First(r => r.Key == bull.RingSet).Discs.Max(d => d.Diameter) / 2.0;
            var here = new List<SyntheticHole>();
            for (int k = 0; k < c.PerBull; k++)
            {
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    double x, y;
                    if (c.Hard == "holes on the outer ring edge")
                    {
                        double angle = random.NextDouble() * 2 * Math.PI;
                        (x, y) = (bull.X + (outerRadius * Math.Cos(angle)), bull.Y + (outerRadius * Math.Sin(angle)));
                    }
                    else if (c.Hard == "cross-cell shots" && bullIndex % 7 == 3)
                    {
                        // Past the midpoint towards the nearest other bull, so nearest-bull gives it to the neighbour (docs/SCAN-MEASUREMENTS.md section 7.2).
                        var neighbour = definition.Bulls.Where(o => o.X != bull.X || o.Y != bull.Y).MinBy(o => Math.Pow(o.X - bull.X, 2) + Math.Pow(o.Y - bull.Y, 2))!;
                        double along = 0.52 + (0.08 * random.NextDouble());
                        (x, y) = (bull.X + (along * (neighbour.X - bull.X)), bull.Y + (along * (neighbour.Y - bull.Y)));
                    }
                    else if (c.Hard == "overlapping pairs" && k == 1)
                    {
                        var first = here[0];
                        double angle = random.NextDouble() * 2 * Math.PI, apart = (0.5 + (0.4 * random.NextDouble())) * 2 * first.RimRadius;
                        (x, y) = (first.X + (apart * Math.Cos(angle)), first.Y + (apart * Math.Sin(angle)));
                    }
                    else
                    {
                        double dx = 0.3 * DmmPerInch * SyntheticSurface.Gaussian(random), dy = 0.3 * DmmPerInch * SyntheticSurface.Gaussian(random);
                        double r = Math.Sqrt((dx * dx) + (dy * dy)), limit = 0.55 * DmmPerInch;
                        (x, y) = r > limit ? (bull.X + (dx * limit / r), bull.Y + (dy * limit / r)) : (bull.X + dx, bull.Y + dy);
                    }

                    var hole = SyntheticSheet.SampleHole(random, x, y, OnInk(x, y), backing);
                    bool clear = c.Hard == "overlapping pairs" || here.All(o => Math.Sqrt(Math.Pow(o.X - x, 2) + Math.Pow(o.Y - y, 2)) >= 1.1 * (o.RimRadius + hole.RimRadius + o.RimWidth + hole.RimWidth));
                    if (clear)
                    {
                        here.Add(hole);
                        break;
                    }
                }
            }

            holes.AddRange(here);
            bullOf.AddRange(Enumerable.Repeat(bullIndex, here.Count));
            if (c.Hard == "X marks over holes")
            {
                foreach (var h in here)
                {
                    strokes.AddRange(SyntheticSheet.Cross(h.X, h.Y, 0.29 * DmmPerInch, 0.0625 * DmmPerInch, 98, random.NextDouble() * Math.PI));
                }
            }

            if (c.Hard == "captions and arrowheads")
            {
                // An arrowhead in the cell, well clear of the hole.
                var h = here[0];
                double angle = Math.Atan2(bull.Y - h.Y, bull.X - h.X), away = 0.45 * DmmPerInch;
                double ax = bull.X + (away * Math.Cos(angle)), ay = bull.Y + (away * Math.Sin(angle));
                if (Math.Sqrt(Math.Pow(ax - h.X, 2) + Math.Pow(ay - h.Y, 2)) > 0.4 * DmmPerInch)
                {
                    strokes.AddRange(SyntheticSheet.Arrowhead(ax, ay, 0.15 * DmmPerInch, 0.05 * DmmPerInch, 105, random.NextDouble() * 2 * Math.PI));
                }
            }
        }

        if (c.Hard == "captions and arrowheads")
        {
            double bottom = definition.Bulls.Max(b => b.Y) + (0.9 * DmmPerInch), left = definition.Bulls.Min(b => b.X);
            for (int k = 0; k < 8; k++)
            {
                strokes.AddRange(SyntheticSheet.Bowl(left + (k * 0.2 * DmmPerInch), bottom, 0.14 * DmmPerInch, 0.04 * DmmPerInch, 54));
            }

            if (definition.DataBlock is { } data)
            {
                for (int k = 0; k < 8; k++)
                {
                    strokes.AddRange(SyntheticSheet.Bowl(data.X + (0.3 * DmmPerInch) + (k * 0.2 * DmmPerInch), data.Y + (data.Height / 2.0), 0.14 * DmmPerInch, 0.04 * DmmPerInch, 54));
                }
            }
        }

        var observed = SyntheticSheet.Compose(render, dpi, truth, render.Width, render.Height, holes, strokes, random);
        var backend = new OpenCvSharpBackend();
        var rd = RenderDifferenceHoleDetector.Detect(observed, definition, 0, registration, dpi, backend, pageRender: render);
        var nd = NeutralDarknessHoleDetector.Detect(observed, dpi, backend);
        var truthImage = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();

        Trial Score(string method, IReadOnlyList<PointD> detections, IReadOnlyList<double>? closures = null, List<(int T, int D)>? matches = null)
        {
            var pairs = new List<(int T, int D, double Distance)>();
            for (int t = 0; t < truthImage.Count; t++)
            {
                for (int d = 0; d < detections.Count; d++)
                {
                    double distance = Math.Sqrt(Math.Pow(detections[d].X - truthImage[t].X, 2) + Math.Pow(detections[d].Y - truthImage[t].Y, 2)) / dpi;
                    if (distance <= Loose)
                    {
                        pairs.Add((t, d, distance));
                    }
                }
            }

            var usedT = new bool[truthImage.Count];
            var usedD = new bool[detections.Count];
            var errors = new List<double>();
            var matchedClosures = new List<double>();
            foreach (var (t, d, distance) in pairs.OrderBy(p => p.Distance))
            {
                if (!usedT[t] && !usedD[d])
                {
                    usedT[t] = usedD[d] = true;
                    errors.Add(distance);
                    matches?.Add((t, d));
                    if (closures is not null)
                    {
                        matchedClosures.Add(closures[d]);
                    }
                }
            }

            var missed = holes.Where((_, t) => !usedT[t]).Select(h => (object)new { xIn = RawMeasurements.R(h.X / DmmPerInch), yIn = RawMeasurements.R(h.Y / DmmPerInch), rimRadiusIn = RawMeasurements.R(h.RimRadius / DmmPerInch), rimWidthIn = RawMeasurements.R(h.RimWidth / DmmPerInch), rimV = RawMeasurements.R(h.RimV), coreV = RawMeasurements.R(h.CoreV) }).ToList();
            var strayIndices = Enumerable.Range(0, detections.Count).Where(d => !usedD[d]).ToList();
            var stray = strayIndices.Select(d => (object)new { xIn = RawMeasurements.R((detections[d].X + 0.5) / dpi), yIn = RawMeasurements.R((detections[d].Y + 0.5) / dpi), closure = closures is null ? (double?)null : RawMeasurements.R(closures[d]) }).ToList();
            var strayClosures = closures is null ? [] : strayIndices.Select(d => closures[d]).ToList();
            return new Trial(c.Group, c.Name, dpi, c.PerBull, c.ErrorInches, method, seed, holes.Count, errors.Count(e => e <= Tight), errors.Count, stray.Count, errors, missed, stray, matchedClosures, strayClosures);
        }

        var ndHoles = nd.Holes.Select(h => new PointD(h.X, h.Y)).ToList();

        var realism = new List<Realism>();
        var rdMatches = new List<(int T, int D)>();
        var rdTrial = Score("render-and-difference", [.. rd.Holes.Select(h => new PointD(h.X, h.Y))], [.. rd.Holes.Select(h => h.Closure)], rdMatches);

        // Stage S9 on the detections, strays included as a pipeline would see them: its rule and nearest-bull, each against the bull the matched hole was drawn for.
        var assignment = ShotAssignment.Assign([.. rd.Holes.Select(h => registration.ToPage(new PointD(h.X, h.Y)))], [.. definition.Bulls.Select(b => new PointD(b.X, b.Y))]);
        var covering = rd.Holes.Select(h => truthImage.Count(t => Math.Sqrt(Math.Pow(h.X - t.X, 2) + Math.Pow(h.Y - t.Y, 2)) / dpi <= h.DiameterInches / 2)).ToList();
        rdTrial = rdTrial with
        {
            AssignedRight = rdMatches.Count(m => assignment.Shots[m.D].Bull == bullOf[m.T]),
            NearestRight = rdMatches.Count(m => assignment.Shots[m.D].NearestBull == bullOf[m.T]),
            AssignmentDiffers = assignment.Shots.Count(a => a.Bull != a.NearestBull),
            AssignmentFlagged = assignment.Shots.Count(a => a.Ambiguous),
            AssignmentMethod = assignment.Method.ToString(),
            SingleElongations = [.. rd.Holes.Where((h, d) => covering[d] == 1 && !h.PossibleMerge).Select(h => h.Elongation)],
            PairElongations = [.. rd.Holes.Where((h, d) => covering[d] >= 2 && !h.PossibleMerge).Select(h => h.Elongation)],
            SplitHalves = rd.Holes.Count(h => h.PossibleMerge),
            SplitHalvesMatched = rdMatches.Count(m => rd.Holes[m.D].PossibleMerge),
            Oversized = rd.Holes.Count(h => h.Oversized),
            OversizedOverTwo = rd.Holes.Where((h, d) => h.Oversized && covering[d] >= 2).Count(),
            MergedUnflagged = rd.Holes.Where((h, d) => covering[d] >= 2 && !h.PossibleMerge && !h.Oversized).Count(),
        };

        return ([rdTrial, Score("baseline", ndHoles)], realism);
    }

    /// <summary>
    /// The calibration page: 63 synthetic holes on bare paper, a one-inch grid across a blank Letter page, each drawn by
    /// <see cref="SyntheticSheet.SampleHole"/> exactly as on a target, measured by the baseline and matched to truth within 0.15 in.
    /// </summary>
    private static List<Realism> Calibration(TargetDefinition definition, double dpi, int seed)
    {
        var random = new Random(seed * 31337);
        double s = DmmPerInch / dpi;
        var truth = new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1]));
        int width = (int)Math.Round(definition.Page.Width / s), height = (int)Math.Round(definition.Page.Height / s);
        var pixels = new byte[width * height];
        Array.Fill(pixels, (byte)255);
        var blank = new GrayImage(width, height, pixels);
        var holes = new List<SyntheticHole>();
        for (int gy = 1; gy <= 9; gy++)
        {
            for (int gx = 1; gx <= 7; gx++)
            {
                holes.Add(SyntheticSheet.SampleHole(random, (gx + 0.25) * DmmPerInch, (gy + 0.5) * DmmPerInch, onInk: false, HoleBacking.ScannerLid));
            }
        }

        var observed = SyntheticSheet.Compose(blank, dpi, truth, width, height, holes, [], random);
        var detection = NeutralDarknessHoleDetector.Detect(observed, dpi, new OpenCvSharpBackend());
        var centres = holes.Select(h => truth.ToImage(new PointD(h.X, h.Y))).ToList();
        return [.. detection.Holes.Where(h => centres.Any(t => Math.Sqrt(Math.Pow(h.X - t.X, 2) + Math.Pow(h.Y - t.Y, 2)) / dpi <= Loose))
            .Select(h => new Realism(h.DiameterInches, h.CoreMeanV, h.AnnulusMinimumV, h.RaggednessInches))];
    }

    private static double Quantile(IReadOnlyList<double> sorted, double q) => sorted.Count == 0 ? double.NaN : sorted[Math.Min(sorted.Count - 1, (int)Math.Floor(q * (sorted.Count - 1) + 0.5))];

    private static double Mean(IEnumerable<double> values) => values.DefaultIfEmpty(double.NaN).Average();

    private static double Sd(IEnumerable<double> values)
    {
        var v = values.ToList();
        if (v.Count < 2)
        {
            return double.NaN;
        }

        double mean = v.Average();
        return Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / (v.Count - 1));
    }
}
