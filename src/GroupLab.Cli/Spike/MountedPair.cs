using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab mounted pair</c>: docs/NOTES-FROM-PLANNING.md entry 20 section 5, the first ground truth a photograph has had. The
/// <c>N568 GM210M</c> sheet exists as a 600 DPI flatbed scan, <c>scans/n568-gm210m.jpg</c>, and as a phone photograph,
/// <c>scans/mounted/20260329_183028.jpg</c>, so the scan, flat by construction, is the reference the photograph is measured against.
/// <list type="number">
/// <item><b>Bulls.</b> The OnTarget #18 sheet prints thirty coloured rings, five columns by six rows with the sighter row lower,
/// each with a small centre dot. Printed ink has chroma and a hole has none. The rings themselves do not survive as clean shapes:
/// holes break them and the scan's rings carry light stripes. The dots do, so each compact coloured blob of dot size is a candidate,
/// confirmed as a bull by 72 rays from it meeting a ring at a consistent radius, which a digit or a grid rule does not give. The bull's
/// centre is then a circle fitted to the midpoints of the rays' ring crossings, trimmed at 2.5 robust standard deviations over three
/// passes as docs/SCAN-MEASUREMENTS.md section 2 trimmed its ring fits, and the dot's centroid is kept beside it as an independent
/// check.</item>
/// <item><b>Models.</b> Scan to photograph by the Phase 0 photograph model, homography then homography with the two-term radial lens,
/// and then M1.10's generalised cylinder and general developable surface, the frame alone from its EXIF focal length. The residual
/// is each bull's fitted page point less its scan position, in scan inches, and its spatial correlation is M1.11's statistic.</item>
/// <item><b>Holes.</b> Entry 19 measurement B asks how the neutral-darkness detector does on photographed holes, and this is the one
/// frame where that has an answer: the detector's holes on the scan, where the survey verified it, are the truth, mapped through the
/// fitted lens model, and the detector is run on the photograph untuned at the resolution that model gives.</item>
/// </list>
/// The sheet is lying on a mat, not mounted, so this is the flat-control case of entry 20 section 5, and it is labelled that way.
/// </summary>
public static class MountedPair
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const string ScanFile = "n568-gm210m.jpg", PhotoFile = "20260329_183028.jpg";
    private const double ScanDpi = 600, DmmPerInch = 254, ChromaThreshold = 50;
    private const int Columns = 5, Rows = 6, Rays = 72;

    /// <summary>The printed ring's centreline radius over its centre dot's radius, from docs/SCAN-MEASUREMENTS.md section 2.3's #18 style: 0.474 in over 0.027 in.</summary>
    private const double RingOverDot = 17.7;

    private sealed record Ring(int Row, int Column, PointD Centre, PointD Dot, double Radius, double RmsPixels, int Rays);

    private sealed record Component(int Area, int Left, int Top, int Right, int Bottom, double X, double Y);

    /// <summary>Where the ring finder lists its candidates when asked.</summary>
    public static string? DebugDirectory { get; set; }

    public static int Run(string scans, string phase1Scans, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var backend = new OpenCvSharpBackend();
        var (scanMax, scanChroma, _) = ImageLoader.LoadMaxAndChroma(Path.Combine(scans, ScanFile));
        var (photoMax, photoChroma, photoMetadata) = ImageLoader.LoadMaxAndChroma(Path.Combine(scans, "mounted", PhotoFile));

        var scanRings = FindRings(scanChroma, output, "scan");
        var photoRings = FindRings(photoChroma, output, "photograph");
        if (scanRings is null || photoRings is null)
        {
            return 1;
        }

        var pairs = scanRings.Join(photoRings, s => (s.Row, s.Column), p => (p.Row, p.Column), (s, p) => (Scan: s, Photo: p)).OrderBy(x => x.Scan.Row).ThenBy(x => x.Scan.Column).ToList();
        var image = pairs.Select(x => x.Photo.Centre).ToList();
        PointD ScanToDmm(PointD p) => new(p.X / ScanDpi * DmmPerInch, p.Y / ScanDpi * DmmPerInch);
        var page = pairs.Select(x => ScanToDmm(x.Scan.Centre)).ToList();

        double DotRingScanInches(Ring r) => double.IsNaN(r.Dot.X) ? double.NaN : Math.Sqrt(Math.Pow(r.Dot.X - r.Centre.X, 2) + Math.Pow(r.Dot.Y - r.Centre.Y, 2)) / ScanDpi;
        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"{pairs.Count} bulls matched by grid position. Ring circle fits: scan RMS {scanRings.Average(r => r.RmsPixels):0.00} px over {scanRings.Average(r => r.Rays):0} of {Rays} rays, photograph {photoRings.Average(r => r.RmsPixels):0.00} px over {photoRings.Average(r => r.Rays):0}. Centre dot against ring centre on the scan: median {Median(scanRings.Select(DotRingScanInches)):0.0000} in, worst {scanRings.Select(DotRingScanInches).Where(double.IsFinite).Max():0.0000} in."));

        var homography = HomographyEstimate.Fit(image, page) ?? throw new InvalidOperationException("no homography");
        var planar = new HomographyMapping(homography);
        var lens = LensFit.Fit(image, page, homography, photoMax.Width, photoMax.Height);
        double? exif = SurfaceFit.FocalPixelsFromExif(photoMetadata, photoMax.Width, photoMax.Height);
        double pageWidth = scanMax.Width / ScanDpi * DmmPerInch, pageHeight = scanMax.Height / ScanDpi * DmmPerInch;
        var models = new List<(string Name, IPageMapping Mapping, string Detail)>
        {
            ("homography", planar, "8 parameters"),
            ("homography and radial lens", lens, string.Create(Inv, $"k1 {lens.K1:+0.0000;-0.0000}, k2 {lens.K2:+0.0000;-0.0000}")),
        };
        var raw = new Dictionary<string, object?>
        {
            ["scan"] = ScanFile,
            ["photograph"] = PhotoFile,
            ["camera"] = new { photoMetadata.CameraModel, photoMetadata.FocalLengthMm, photoMetadata.FNumber, photoMetadata.FocalLength35mm, width = photoMax.Width, height = photoMax.Height },
            ["bulls"] = pairs.Select(x => new
            {
                row = x.Scan.Row,
                column = x.Scan.Column,
                scanX = RawMeasurements.R(x.Scan.Centre.X),
                scanY = RawMeasurements.R(x.Scan.Centre.Y),
                scanDotX = double.IsNaN(x.Scan.Dot.X) ? (double?)null : RawMeasurements.R(x.Scan.Dot.X),
                scanDotY = double.IsNaN(x.Scan.Dot.Y) ? (double?)null : RawMeasurements.R(x.Scan.Dot.Y),
                photoX = RawMeasurements.R(x.Photo.Centre.X),
                photoY = RawMeasurements.R(x.Photo.Centre.Y),
                photoDotX = double.IsNaN(x.Photo.Dot.X) ? (double?)null : RawMeasurements.R(x.Photo.Dot.X),
                photoDotY = double.IsNaN(x.Photo.Dot.Y) ? (double?)null : RawMeasurements.R(x.Photo.Dot.Y),
                scanRmsPx = RawMeasurements.R(x.Scan.RmsPixels),
                photoRmsPx = RawMeasurements.R(x.Photo.RmsPixels),
            }).ToArray(),
        };

        if (exif is { } focal)
        {
            var frame = new SurfaceFrame("pair", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, focal, pageWidth / 2, pageHeight / 2), 0, 0, pageWidth, pageHeight);
            var cylinder = SurfaceFit.Fit([frame], shareCamera: false)[0];
            var general = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.None, SurfaceFamily.General)[0];
            models.Add(("generalised cylinder", cylinder.Mapping, string.Create(Inv, $"deflection {cylinder.DeflectionDmm / DmmPerInch:0.000} in, focal {cylinder.Model.FocalPixels:0} px from EXIF {focal:0}, kept {cylinder.Kept.Count(k => k)} of {cylinder.Kept.Count}")));
            models.Add(("general developable surface", general.Mapping, string.Create(Inv, $"deflection {general.DeflectionDmm / DmmPerInch:0.000} in, focal {general.Model.FocalPixels:0} px, kept {general.Kept.Count(k => k)} of {general.Kept.Count}")));
        }
        else
        {
            output.WriteLine("The photograph carries no 35 mm equivalent focal length, so the surface models cannot start.");
        }

        output.WriteLine();
        output.WriteLine("Scan to photograph: each bull's photograph centre mapped to the scan through the model, less its scan centre, in scan inches; residual RMS and worst, and M1.11's neighbour correlation of the per-bull residual with its permutation p over 2000 shuffles. The sheet is lying on a mat: the flat control, not the mounted case.");
        output.WriteLine();
        output.WriteLine("| Model | Bulls | RMS residual (in) | Worst (in) | Neighbour correlation | p | Far correlation | Detail |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        var modelRows = new List<object>();
        foreach (var (name, mapping, detail) in models)
        {
            var residuals = image.Select((q, i) =>
            {
                var mapped = mapping.ToPage(q);
                return new PointD(mapped.X - page[i].X, mapped.Y - page[i].Y);
            }).ToList();
            double rms = Math.Sqrt(residuals.Average(r => (r.X * r.X) + (r.Y * r.Y))) / DmmPerInch;
            double worst = residuals.Max(r => Math.Sqrt((r.X * r.X) + (r.Y * r.Y))) / DmmPerInch;
            double mx = residuals.Average(r => r.X), my = residuals.Average(r => r.Y);
            var field = SurfaceCorrelation.Correlate(page, [.. residuals.Select(r => new PointD(r.X - mx, r.Y - my))], 20);
            output.WriteLine(string.Create(Inv, $"| {name} | {residuals.Count} | {rms:0.00000} | {worst:0.00000} | {field.Neighbour:+0.00;-0.00} | {(field.NeighbourP < 0.001 ? "< 0.001" : field.NeighbourP.ToString("0.000", Inv))} | {field.Far:+0.00;-0.00} | {detail} |"));
            modelRows.Add(new { model = name, detail, rmsInches = RawMeasurements.R(rms), worstInches = RawMeasurements.R(worst), field, residualsInches = residuals.Select(r => new { dx = RawMeasurements.R(r.X / DmmPerInch), dy = RawMeasurements.R(r.Y / DmmPerInch) }).ToArray() });
        }

        raw["models"] = modelRows;
        raw["holes"] = Holes(scanMax, photoMax, lens, pageWidth, pageHeight, backend, output);
        RawMeasurements.Write(phase1Scans, "mounted-pair", new[] { raw });
        return 0;
    }

    /// <summary>The scan's detections as truth in the photograph, and the detector's recall and strays there.</summary>
    private static object Holes(GrayImage scanMax, GrayImage photoMax, RadialHomographyMapping lens, double pageWidth, double pageHeight, OpenCvSharpBackend backend, TextWriter output)
    {
        var scanHoles = NeutralDarknessHoleDetector.Detect(scanMax, ScanDpi, backend).Holes;
        var centreImage = lens.ToImage(new PointD(pageWidth / 2, pageHeight / 2));
        var inchAway = lens.ToImage(new PointD((pageWidth / 2) + DmmPerInch, pageHeight / 2));
        double photoDpi = Math.Sqrt(Math.Pow(inchAway.X - centreImage.X, 2) + Math.Pow(inchAway.Y - centreImage.Y, 2));
        var truth = scanHoles.Select(h => new PointD(h.X / ScanDpi * DmmPerInch, h.Y / ScanDpi * DmmPerInch)).ToList();

        // The primitive assumes paper fills the frame, which a scan guarantees and a photograph does not. It is run as it is on the
        // whole photograph, and then, still untuned, on the sheet alone: the rectangle the fitted lens model maps the scan's page
        // into, which is what registration would hand any real pipeline.
        var corners = new[] { new PointD(0, 0), new PointD(pageWidth, 0), new PointD(pageWidth, pageHeight), new PointD(0, pageHeight) }.Select(lens.ToImage).ToList();
        int cropLeft = Math.Max(0, (int)corners.Min(c => c.X)), cropTop = Math.Max(0, (int)corners.Min(c => c.Y));
        int cropRight = Math.Min(photoMax.Width, (int)Math.Ceiling(corners.Max(c => c.X))), cropBottom = Math.Min(photoMax.Height, (int)Math.Ceiling(corners.Max(c => c.Y)));
        var sheetPixels = new byte[(cropRight - cropLeft) * (cropBottom - cropTop)];
        for (int y = cropTop; y < cropBottom; y++)
        {
            Array.Copy(photoMax.Pixels, (y * photoMax.Width) + cropLeft, sheetPixels, (y - cropTop) * (cropRight - cropLeft), cropRight - cropLeft);
        }

        var sheet = new GrayImage(cropRight - cropLeft, cropBottom - cropTop, sheetPixels);
        var runs = new List<object>();
        foreach (var (name, pixels, offsetX, offsetY) in new[] { ("whole photograph", photoMax, 0, 0), ("sheet only", sheet, cropLeft, cropTop) })
        {
            var photoDetection = NeutralDarknessHoleDetector.Detect(pixels, photoDpi, backend);
            var found = photoDetection.Holes.Select(h => lens.ToPage(new PointD(h.X + offsetX, h.Y + offsetY))).ToList();
            var candidates = new List<(int T, int D, double Inches)>();
            for (int t = 0; t < truth.Count; t++)
            {
                for (int d = 0; d < found.Count; d++)
                {
                    double inches = Math.Sqrt(Math.Pow(truth[t].X - found[d].X, 2) + Math.Pow(truth[t].Y - found[d].Y, 2)) / DmmPerInch;
                    if (inches <= 0.15)
                    {
                        candidates.Add((t, d, inches));
                    }
                }
            }

            var usedT = new bool[truth.Count];
            var usedD = new bool[found.Count];
            var errors = new List<double>();
            foreach (var (t, d, inches) in candidates.OrderBy(c => c.Inches))
            {
                if (!usedT[t] && !usedD[d])
                {
                    usedT[t] = usedD[d] = true;
                    errors.Add(inches);
                }
            }

            errors.Sort();
            output.WriteLine();
            output.WriteLine(string.Create(Inv, $"Holes, {name}: {truth.Count} holes on the scan are the truth. The neutral-darkness detector, untuned at {photoDpi:0} px per inch from the lens model: {found.Count} detections, {errors.Count} matched within 0.15 in ({100.0 * errors.Count / Math.Max(1, truth.Count):0}% recall), {found.Count - errors.Count} strays, centre difference median {(errors.Count == 0 ? double.NaN : errors[errors.Count / 2]):0.0000} in and worst {(errors.Count == 0 ? double.NaN : errors[^1]):0.0000} in."));
            foreach (var reason in photoDetection.Rejected.GroupBy(r => new string(r.Reason.TakeWhile(ch => ch != ',').ToArray())).OrderByDescending(g => g.Count()))
            {
                output.WriteLine(string.Create(Inv, $"  refused, {reason.Key}: {reason.Count()} (hull diameters {string.Join(", ", reason.Select(r => r.DiameterInches).Order().Take(12).Select(d => d.ToString("0.000", Inv)))} in)"));
            }

            foreach (var i in Enumerable.Range(0, found.Count).Where(i => !usedD[i]))
            {
                var h = photoDetection.Holes[i];
                output.WriteLine(string.Create(Inv, $"  stray at scan ({found[i].X / DmmPerInch:0.00}, {found[i].Y / DmmPerInch:0.00}) in, hull {h.DiameterInches:0.000} in, core V {h.CoreMeanV:0}"));
            }

            foreach (var i in Enumerable.Range(0, truth.Count).Where(i => !usedT[i]))
            {
                output.WriteLine(string.Create(Inv, $"  missed: the scan hole at ({truth[i].X / DmmPerInch:0.00}, {truth[i].Y / DmmPerInch:0.00}) in, scan hull {scanHoles[i].DiameterInches:0.000} in"));
            }

            runs.Add(new
            {
                run = name,
                detections = found.Count,
                matched = errors.Count,
                strays = found.Count - errors.Count,
                centreDifferencesInches = errors.Select(RawMeasurements.R).ToArray(),
                photoDetections = photoDetection.Holes.Select((h, i) => new { scanXIn = RawMeasurements.R(found[i].X / DmmPerInch), scanYIn = RawMeasurements.R(found[i].Y / DmmPerInch), hullIn = RawMeasurements.R(h.DiameterInches), coreV = RawMeasurements.R(h.CoreMeanV), matched = usedD[i] }).ToArray(),
                missedScanHoles = Enumerable.Range(0, truth.Count).Where(i => !usedT[i]).Select(i => new { scanXIn = RawMeasurements.R(truth[i].X / DmmPerInch), scanYIn = RawMeasurements.R(truth[i].Y / DmmPerInch) }).ToArray(),
                refused = photoDetection.Rejected.Select(r => new { hullIn = RawMeasurements.R(r.DiameterInches), r.Reason }).ToArray(),
            });
        }

        return new { truthHoles = truth.Count, photoPixelsPerInch = RawMeasurements.R(photoDpi), sheetCrop = new { cropLeft, cropTop, cropRight, cropBottom }, runs };
    }

    /// <summary>The sheet layout, inches: a 1.5 in pitch, and the sighter row 1.799 in below the last scoring row (docs/SCAN-MEASUREMENTS.md section 2.2).</summary>
    private static PointD Layout(int r, int c) => new(c * 1.5, r < Rows - 1 ? r * 1.5 : ((Rows - 2) * 1.5) + 1.799);

    /// <summary>The thirty bulls by grid position, or null with the reason printed.</summary>
    private static List<Ring>? FindRings(GrayImage chroma, TextWriter output, string label)
    {
        int width = chroma.Width;
        var components = Components(chroma);

        // Dot-sized, compact, round: a disc fills pi / 4 of its box.
        var dots = components.Where(c =>
        {
            int w = c.Right - c.Left + 1, h = c.Bottom - c.Top + 1;
            return w >= 0.003 * width && w <= 0.014 * width && h >= 0.003 * width && h <= 0.014 * width
                && Math.Max(w, h) / (double)Math.Min(w, h) <= 1.35 && c.Area / (double)(w * h) >= 0.6;
        }).ToList();

        var confirmed = new List<(Component Dot, PointD Centre, double Radius, double Rms, int Hits)>();
        foreach (var dot in dots)
        {
            double dotRadius = Math.Sqrt(dot.Area / Math.PI);
            if (RingAround(chroma, new PointD(dot.X, dot.Y), 0.6 * RingOverDot * dotRadius, 1.5 * RingOverDot * dotRadius) is { } ring)
            {
                confirmed.Add((dot, ring.Centre, ring.Radius, ring.Rms, ring.Hits));
            }
        }

        if (DebugDirectory is not null)
        {
            output.WriteLine(string.Create(Inv, $"  {label}: {components.Count} components, {dots.Count} dot candidates, {confirmed.Count} with a ring"));
        }

        if (confirmed.Count < 10)
        {
            output.WriteLine(string.Create(Inv, $"{label}: only {confirmed.Count} dots with a ring about them, of {dots.Count} dot-sized blobs"));
            return null;
        }

        // A ring at the consistent radius: each within 15 percent of the median.
        double medianRadius = Median(confirmed.Select(c => c.Radius));
        confirmed = [.. confirmed.Where(c => Math.Abs(c.Radius - medianRadius) <= 0.15 * medianRadius)];

        // The same ring confirmed from two nearby blobs keeps the better fit.
        var bulls = new List<(Component Dot, PointD Centre, double Radius, double Rms, int Hits)>();
        foreach (var c in confirmed.OrderBy(c => c.Rms))
        {
            if (bulls.All(b => Math.Sqrt(Math.Pow(b.Centre.X - c.Centre.X, 2) + Math.Pow(b.Centre.Y - c.Centre.Y, 2)) > medianRadius))
            {
                bulls.Add(c);
            }
        }

        // The grid, whatever way the pixels are stored: a camera may keep the sensor landscape and record the turn in EXIF only.
        // Each bull gets lattice indices on both image axes from the axis phase (the circular mean of its position modulo the
        // pitch, which the 25 scoring bulls dominate), and the sighter line shows as a sixth line 0.2 pitch off the lattice. When
        // no sighter dot was found, the sighter line is looked for on each of the four sides and taken where its rings are.
        double pitch = 1.5 / 0.474 * medianRadius;
        static double Origin(IEnumerable<double> values, double step)
        {
            double sin = 0, cos = 0;
            foreach (double v in values)
            {
                sin += Math.Sin(2 * Math.PI * v / step);
                cos += Math.Cos(2 * Math.PI * v / step);
            }

            return Math.Atan2(sin, cos) / (2 * Math.PI) * step;
        }

        double ox = Origin(bulls.Select(b => b.Centre.X), pitch), oy = Origin(bulls.Select(b => b.Centre.Y), pitch);
        var cells = bulls.Select(b =>
        {
            double vx = (b.Centre.X - ox) / pitch, vy = (b.Centre.Y - oy) / pitch;
            return (Bull: b, I: (int)Math.Round(vx), J: (int)Math.Round(vy), Fx: vx - Math.Round(vx), Fy: vy - Math.Round(vy));
        }).ToList();
        int minI = cells.Min(c => c.I), minJ = cells.Min(c => c.J);
        cells = [.. cells.Select(c => (c.Bull, c.I - minI, c.J - minJ, c.Fx, c.Fy))];
        int spanI = cells.Max(c => c.I), spanJ = cells.Max(c => c.J);

        bool rowsAlongJ, sighterHigh;
        if (spanI == Rows - 1 || spanJ == Rows - 1)
        {
            rowsAlongJ = spanJ == Rows - 1;
            double MeanFraction(int line) => cells.Where(c => (rowsAlongJ ? c.J : c.I) == line).Select(c => rowsAlongJ ? c.Fy : c.Fx).DefaultIfEmpty(0).Average();
            sighterHigh = Math.Abs(MeanFraction(Rows - 1)) >= Math.Abs(MeanFraction(0));
        }
        else if (spanI == Columns - 1 && spanJ == Columns - 1)
        {
            var lattice = HomographyEstimate.Fit([.. cells.Select(c => new PointD(c.I, c.J))], [.. cells.Select(c => c.Bull.Centre)]);
            if (lattice is null)
            {
                output.WriteLine(string.Create(Inv, $"{label}: no lattice homography"));
                return null;
            }

            var toImage = new HomographyMapping(lattice);
            const double beyond = 1.799 / 1.5;
            int Rings(bool alongJ, bool high) => Enumerable.Range(0, Columns).Count(k =>
            {
                var at = alongJ ? new PointD(k, high ? Columns - 1 + beyond : -beyond) : new PointD(high ? Columns - 1 + beyond : -beyond, k);
                var guess = toImage.ToPage(at);
                var wide = RingAround(chroma, guess, 0.4 * medianRadius, 1.6 * medianRadius);
                return wide is not null && Math.Abs(wide.Value.Radius - medianRadius) <= 0.15 * medianRadius;
            });
            var sides = new[] { (J: true, High: true), (J: true, High: false), (J: false, High: true), (J: false, High: false) }.Select(side => (side, Found: Rings(side.J, side.High))).OrderByDescending(x => x.Found).ToList();
            output.WriteLine(string.Create(Inv, $"{label}: no sighter dots found; sighter rings on the four sides: {string.Join(", ", sides.Select(x => $"{(x.side.J ? "y" : "x")} {(x.side.High ? "high" : "low")} {x.Found}"))}"));
            if (sides[0].Found < 3)
            {
                return null;
            }

            (rowsAlongJ, sighterHigh) = sides[0].side;
        }
        else
        {
            output.WriteLine(string.Create(Inv, $"{label}: lattice spans {spanI + 1} by {spanJ + 1} lines, not a {Columns} by {Rows} grid"));
            return null;
        }

        // Rows count away from the sighter line, which is the last row. Columns run whichever way keeps the layout unmirrored: the
        // image of the sheet is never its reflection, so the layout-to-image homography keeps a positive determinant.
        int scoringOffset = spanI == Rows - 1 || spanJ == Rows - 1 ? (sighterHigh ? 0 : 1) : 0;
        int RowOf((Component Dot, PointD Centre, double Radius, double Rms, int Hits) b, int line)
        {
            int scoringLine = line - scoringOffset;
            bool isSighter = (spanI == Rows - 1 || spanJ == Rows - 1) && line == (sighterHigh ? Rows - 1 : 0);
            return isSighter ? Rows - 1 : sighterHigh ? scoringLine : Columns - 1 - scoringLine;
        }

        Dictionary<(int Row, int Column), (Component Dot, PointD Centre, double Radius, double Rms, int Hits)> Assign(bool reversed)
        {
            var result = new Dictionary<(int Row, int Column), (Component Dot, PointD Centre, double Radius, double Rms, int Hits)>();
            foreach (var cell in cells)
            {
                int line = rowsAlongJ ? cell.J : cell.I, across = rowsAlongJ ? cell.I : cell.J;
                int r = RowOf(cell.Bull, line), c = reversed ? Columns - 1 - across : across;
                if (r >= 0 && r < Rows && c >= 0 && c < Columns && (!result.TryGetValue((r, c), out var existing) || cell.Bull.Rms < existing.Rms))
                {
                    result[(r, c)] = cell.Bull;
                }
            }

            return result;
        }

        double Determinant(Dictionary<(int Row, int Column), (Component Dot, PointD Centre, double Radius, double Rms, int Hits)> map)
        {
            var h = HomographyEstimate.Fit([.. map.Keys.Select(k => Layout(k.Row, k.Column))], [.. map.Keys.Select(k => map[k].Centre)]);
            return h is null ? double.NaN : ((h[0, 0] * h[1, 1]) - (h[0, 1] * h[1, 0])) / (h[2, 2] * h[2, 2]);
        }

        var forward = Assign(reversed: false);
        var assigned = Determinant(forward) > 0 ? forward : Assign(reversed: true);

        var keys = assigned.Keys.ToList();
        var layoutToImage = HomographyEstimate.Fit([.. keys.Select(k => Layout(k.Row, k.Column))], [.. keys.Select(k => assigned[k].Centre)]);
        if (layoutToImage is null)
        {
            output.WriteLine(string.Create(Inv, $"{label}: no layout homography from {assigned.Count} bulls"));
            return null;
        }

        var predictor = new HomographyMapping(layoutToImage);
        var rings = new List<Ring>();
        var predicted = new List<string>();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                if (assigned.TryGetValue((r, c), out var b))
                {
                    rings.Add(new Ring(r, c, b.Centre, new PointD(b.Dot.X, b.Dot.Y), b.Radius, b.Rms, b.Hits));
                    continue;
                }

                var guess = predictor.ToPage(Layout(r, c));
                if (DebugDirectory is not null)
                {
                    output.WriteLine(string.Create(Inv, $"  {label}: predicting row {r + 1} column {c + 1} at ({guess.X:0}, {guess.Y:0}), ring radius {medianRadius:0.0} px; assigned: {string.Join("; ", assigned.OrderBy(k => k.Key).Select(k => $"r{k.Key.Row + 1}c{k.Key.Column + 1} ({k.Value.Centre.X:0}, {k.Value.Centre.Y:0})"))}"));
                    int half = (int)(2 * medianRadius), x0 = Math.Max(0, (int)guess.X - half), y0 = Math.Max(0, (int)guess.Y - half);
                    int x1 = Math.Min(chroma.Width, (int)guess.X + half), y1 = Math.Min(chroma.Height, (int)guess.Y + half);
                    if (x1 > x0 && y1 > y0)
                    {
                        byte[] crop = new byte[(x1 - x0) * (y1 - y0)];
                        for (int yy = y0; yy < y1; yy++)
                        {
                            for (int xx = x0; xx < x1; xx++)
                            {
                                crop[((yy - y0) * (x1 - x0)) + xx - x0] = chroma.Pixels[(yy * chroma.Width) + xx];
                            }
                        }

                        Directory.CreateDirectory(DebugDirectory);
                        using var mat = OpenCvSharp.Mat.FromPixelData(y1 - y0, x1 - x0, OpenCvSharp.MatType.CV_8UC1, crop);
                        OpenCvSharp.Cv2.ImWrite(Path.Combine(DebugDirectory, $"{label}-predicted-r{r + 1}c{c + 1}.png"), mat);
                    }
                }

                // An extrapolated guess can be a third of a radius out, so a wide band finds the ring and a tight band about that
                // first fit refines it.
                var wide = RingAround(chroma, guess, 0.4 * medianRadius, 1.6 * medianRadius);
                if (wide is null || RingAround(chroma, wide.Value.Centre, 0.7 * medianRadius, 1.3 * medianRadius) is not { } ring)
                {
                    output.WriteLine(string.Create(Inv, $"{label}: no ring found at the predicted position of row {r + 1}, column {c + 1}"));
                    return null;
                }

                rings.Add(new Ring(r, c, ring.Centre, new PointD(double.NaN, double.NaN), ring.Radius, ring.Rms, ring.Hits));
                predicted.Add(string.Create(Inv, $"row {r + 1} column {c + 1}"));
            }
        }

        output.WriteLine(string.Create(Inv, $"{label}: {assigned.Count} bulls from their dots, {predicted.Count} from the layout ({string.Join(", ", predicted)})"));
        return rings;
    }

    /// <summary>
    /// A ring about a point: on each of 72 rays the first run of chroma between the two radii, its gaps of up to four pixels bridged,
    /// gives the ring's crossing at the run's midpoint; with at least half the rays crossing, a circle is fitted through the
    /// crossings by Kasa's algebraic fit and refitted three times without crossings beyond 2.5 robust standard deviations.
    /// </summary>
    private static (PointD Centre, double Radius, double Rms, int Hits)? RingAround(GrayImage chroma, PointD start, double inner, double outer)
    {
        var crossings = new List<PointD>();
        for (int k = 0; k < Rays; k++)
        {
            double angle = 2 * Math.PI * k / Rays, dx = Math.Cos(angle), dy = Math.Sin(angle);
            double? first = null, last = null;
            for (double r = inner; r <= outer; r += 0.5)
            {
                int x = (int)Math.Round(start.X + (r * dx)), y = (int)Math.Round(start.Y + (r * dy));
                if (x < 0 || y < 0 || x >= chroma.Width || y >= chroma.Height)
                {
                    break;
                }

                bool ink = chroma.Pixels[(y * chroma.Width) + x] >= ChromaThreshold;
                if (ink)
                {
                    first ??= r;
                    last = r;
                }
                else if (first is not null && r - last!.Value > 4)
                {
                    break;
                }
            }

            if (first is { } a && last is { } b && b - a >= 2)
            {
                double mid = (a + b) / 2;
                crossings.Add(new PointD(start.X + (mid * dx), start.Y + (mid * dy)));
            }
        }

        if (crossings.Count < Rays / 2)
        {
            return null;
        }

        var used = crossings;
        PointD centre = start;
        double radius = 0, rms = double.NaN;
        for (int pass = 0; pass < 4 && used.Count >= 8; pass++)
        {
            (centre, radius) = Kasa(used, start);
            var c = centre;
            double rr = radius;
            var residual = used.Select(p => Math.Sqrt(Math.Pow(p.X - c.X, 2) + Math.Pow(p.Y - c.Y, 2)) - rr).ToList();
            rms = Math.Sqrt(residual.Average(e => e * e));
            double median = Median(residual);
            double mad = 1.4826 * Median(residual.Select(e => Math.Abs(e - median)));
            used = [.. crossings.Where(p => Math.Abs(Math.Sqrt(Math.Pow(p.X - c.X, 2) + Math.Pow(p.Y - c.Y, 2)) - rr - median) <= 2.5 * Math.Max(mad, 0.25))];
        }

        return used.Count >= Rays / 2 ? (centre, radius, rms, used.Count) : null;
    }

    private static (PointD Centre, double Radius) Kasa(List<PointD> points, PointD origin)
    {
        double sxx = 0, sxy = 0, sx = 0, syy = 0, sy = 0, sz = 0, szx = 0, szy = 0;
        int n = points.Count;
        foreach (var p in points)
        {
            double x = p.X - origin.X, y = p.Y - origin.Y, z = (x * x) + (y * y);
            sxx += x * x;
            sxy += x * y;
            sx += x;
            syy += y * y;
            sy += y;
            sz += z;
            szx += z * x;
            szy += z * y;
        }

        var s = Solve3(new[,] { { sxx, sxy, sx }, { sxy, syy, sy }, { sx, sy, n } }, [-szx, -szy, -sz]);
        double cx = -s[0] / 2, cy = -s[1] / 2;
        return (new PointD(origin.X + cx, origin.Y + cy), Math.Sqrt((cx * cx) + (cy * cy) - s[2]));
    }

    /// <summary>Eight-connected components of the chroma mask, with each one's chroma-weighted centroid.</summary>
    private static List<Component> Components(GrayImage chroma)
    {
        int width = chroma.Width, height = chroma.Height;
        var seen = new bool[width * height];
        var result = new List<Component>();
        var stack = new Stack<int>();
        for (int start = 0; start < seen.Length; start++)
        {
            if (seen[start] || chroma.Pixels[start] < ChromaThreshold)
            {
                continue;
            }

            seen[start] = true;
            stack.Push(start);
            int area = 0, left = int.MaxValue, top = int.MaxValue, right = -1, bottom = -1;
            double sw = 0, sx = 0, sy = 0;
            while (stack.Count > 0)
            {
                int i = stack.Pop(), x = i % width, y = i / width;
                double w = chroma.Pixels[i];
                area++;
                sw += w;
                sx += w * x;
                sy += w * y;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
                top = Math.Min(top, y);
                bottom = Math.Max(bottom, y);
                for (int ny = Math.Max(0, y - 1); ny <= Math.Min(height - 1, y + 1); ny++)
                {
                    for (int nx = Math.Max(0, x - 1); nx <= Math.Min(width - 1, x + 1); nx++)
                    {
                        int j = (ny * width) + nx;
                        if (!seen[j] && chroma.Pixels[j] >= ChromaThreshold)
                        {
                            seen[j] = true;
                            stack.Push(j);
                        }
                    }
                }
            }

            result.Add(new Component(area, left, top, right, bottom, sx / sw, sy / sw));
        }

        return result;
    }

    private static double[] Solve3(double[,] a, double[] b)
    {
        static double Det(double[,] m) => (m[0, 0] * ((m[1, 1] * m[2, 2]) - (m[1, 2] * m[2, 1]))) - (m[0, 1] * ((m[1, 0] * m[2, 2]) - (m[1, 2] * m[2, 0]))) + (m[0, 2] * ((m[1, 0] * m[2, 1]) - (m[1, 1] * m[2, 0])));
        double d = Det(a);
        var result = new double[3];
        for (int k = 0; k < 3; k++)
        {
            var m = (double[,])a.Clone();
            for (int i = 0; i < 3; i++)
            {
                m[i, k] = b[i];
            }

            result[k] = Det(m) / d;
        }

        return result;
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Where(v => !double.IsNaN(v)).Order().ToList();
        return sorted.Count == 0 ? double.NaN : sorted[sorted.Count / 2];
    }
}
