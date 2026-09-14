using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;

namespace GroupLab.Core.Detection;

/// <summary>
/// The settings of <see cref="RenderDifferenceHoleDetector"/>. Every length is in inches and every threshold a fraction of
/// the local paper-to-ink range, never a grey level, because docs/DETECTION-PIPELINE.md stage S7 measured one absolute
/// threshold needing 28, 18 and 12 at 600, 93 and 300 DPI on the same targets.
/// <para>
/// The opening is not the survey's. Its 0.032 in radius exists to erase printed artwork, which in a difference has already
/// cancelled, and on synthetic sheets it erased holes whose rim is thinner than the 0.064 in disk around a core near paper
/// level, the misses of both detectors (docs/PHASE1-RESULTS.md M2.2). What is left to remove after differencing is the thin
/// sliver a registration error leaves along every printed edge, so the opening here is sized for that.
/// </para>
/// <para>
/// The split threshold of 1.45 lies between the elongation of single synthetic holes, 99th percentile 1.26 to 1.37 by case,
/// and that of merged neighbours, 5th percentile 1.51 and 1.58, on the development seeds (docs/PHASE1-RESULTS.md M2.2). A
/// lobed single hole can pass it, as far as 1.61 there, and becomes two holes, both flagged.
/// </para>
/// </summary>
public sealed record RenderDifferenceOptions(
    double OpenRadiusInches = 0.012,
    double CloseRadiusInches = 0.055,
    double ResidualFraction = 0.15,
    double MinimumDiameterInches = 0.15,
    double MaximumDiameterInches = 0.60,
    double MinimumSolidity = 0.55,
    double MaximumAspect = 2.2,
    double PaperBlockInches = 0.25,
    double MaximumShiftInches = 0.1,
    double MinimumClosure = 0,
    double SplitElongation = 1.45);

/// <summary>
/// A hole found by render-and-difference, image pixels: the intensity-weighted centroid of its residual, its hull diameter,
/// whether it sits on printed ink, its rim closure, the fraction of 360 rays from the centre that meet residual, the ratio of
/// the principal standard deviations of that residual, and whether it is one of two holes split from a single blob.
/// </summary>
public sealed record RenderDifferenceHole(double X, double Y, double HullX, double HullY, double DiameterInches, double Solidity, bool OnInk, double Closure,
    double Elongation = double.NaN, bool PossibleMerge = false, bool Oversized = false);

/// <summary>
/// One render-and-difference pass: the resolution, the measured ink level as a fraction of paper, the resolved residual
/// threshold in grey levels, what was kept and what was refused, and the local shift, pixels, each bull's cell was aligned by.
/// </summary>
public sealed record RenderDifferenceResult(double Dpi, double InkFraction, double Threshold, IReadOnlyList<RenderDifferenceHole> Holes, IReadOnlyList<RejectedBlob> Rejected, IReadOnlyList<PointD> CellShifts);

/// <summary>
/// Render-and-difference, stages S5 to S8 of docs/DETECTION-PIPELINE.md and docs/PHASE1-BRIEF.md section 4.2. A GroupLab
/// sheet is registered and its definition says exactly what was printed, so the expected image is rendered through the
/// registration and whatever the observed image has that the expected one does not is what a shot did.
/// <list type="number">
/// <item><b>S5, alignment.</b> A registration is never exact, and every printed edge it misses by leaves a sliver in the
/// difference that a small opening cannot remove: on synthetic sheets a 0.02 in error cost 40 percent of holes and 0.04 in
/// all of them (docs/PHASE1-RESULTS.md M2.2). The printed rings are fiducials in their own right, so each bull's cell of the
/// expected image is aligned to the observed one by phase correlation, up to <see cref="RenderDifferenceOptions.MaximumShiftInches"/>,
/// and the rest of the page by the median of those shifts.</item>
/// <item><b>S5.</b> The definition is rasterised and resampled into image space (<see cref="ExpectedImage"/>). Paper is
/// estimated locally, as the 95th percentile of the observed pixels the render calls paper in blocks of a quarter inch,
/// smoothed and interpolated, never globally. The ink level is measured as the median ratio of observed to local paper
/// where the render calls solid ink.</item>
/// <item><b>S6.</b> Observed and expected are both normalised by local paper, and the residual is the absolute difference
/// as a fraction of the paper-to-ink range: darker than expected where a rim darkens paper, and lighter than expected where
/// a perforation removes printed ink, which is the only signal a hole on a black disc gives.</item>
/// <item><b>S7.</b> The residual is opened with a disk wider than any printed stroke, which also erases the thin slivers a
/// small registration error leaves along every printed edge, thresholded at a fraction of the range, closed, filled, and
/// each blob measured by its convex hull.</item>
/// <item><b>S8.</b> The survey's size, compactness and elongation filters; a candidate centred inside a zone the definition
/// declares printed matter, a marker footprint, a code, the data block or the identifier, is refused with the zone named;
/// and one centred outside every bull's cell is refused, which is the position prior that would have removed every false
/// positive the survey's baselines made. An accepted hole's centre is the residual-weighted centroid over its hull, not the
/// hull's centroid, because a lobed rim puts a hull centroid wherever the lobes fall. Rim closure, S8's added signature, is
/// measured and recorded but not gated: the fraction of 360 rays from that centre meeting thresholded residual within one
/// hull diameter read 0.65 to 0.81 on synthetic arrowheads against holes as low as 0.73, and a real rim is often a C, which
/// the synthesis does not draw (docs/PHASE1-RESULTS.md M2.2).</item>
/// <item><b>S8, merged neighbours.</b> Two holes within a closing radius of each other become one blob, as at bull 20 of
/// <c>300_nm_hand_load</c>, which docs/DETECTION-PIPELINE.md's corpus table requires reported as two shots or flagged, never
/// silently as one. A blob whose residual's principal standard deviations differ by a ratio of at least
/// <see cref="RenderDifferenceOptions.SplitElongation"/>, up to twice the largest single hole's area, is split by weighted
/// two-means into two holes, each flagged as a possible merge. A hole wider than the sheet's median by two robust standard
/// deviations is flagged as oversized, S8's response to ink over a hole, because an overlapping pair's residual can be as
/// round as a single hole's and no split finds it.</item>
/// </list>
/// </summary>
public static class RenderDifferenceHoleDetector
{
    private sealed record Zone(string Name, double Left, double Top, double Right, double Bottom);

    /// <param name="observed">The image, grey, as V or luminance.</param>
    /// <param name="registration">Image pixels to page dmm.</param>
    /// <param name="dpi">The image's resolution at the page, to convert inches to pixels and to rasterise the definition at.</param>
    /// <param name="pageRender">The tile's render at <paramref name="dpi"/>, when the caller already has one.</param>
    public static RenderDifferenceResult Detect(GrayImage observed, TargetDefinition definition, int tile, IPageMapping registration, double dpi, IImagingBackend backend,
        RenderDifferenceOptions? options = null, GrayImage? pageRender = null)
    {
        ArgumentNullException.ThrowIfNull(observed);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(backend);
        options ??= new RenderDifferenceOptions();
        var scene = SceneBuilder.Build(definition).Pages[tile];
        pageRender ??= SceneRasterizer.Rasterize(scene, dpi);
        int width = observed.Width, height = observed.Height;
        var expected = ExpectedImage.Render(pageRender, dpi, registration, width, height);

        // Detection runs inside the registered sheet, NOTES-FROM-PLANNING.md entry 23 section 4 and docs/DETECTION-PIPELINE.md before S5.
        // Beyond the page's edge the observed image is replaced by the sheet's own paper level before any stage reads it, so a dark mat
        // around a photographed sheet can neither lower the local paper estimate nor become a residual, whatever image the caller passed.
        var inside = SheetMask(definition.Page.Width, definition.Page.Height, registration, width, height);
        observed = WithinSheet(observed, expected, inside);

        // S5: local paper, from pixels the render calls paper, and the ink level against it.
        int block = Math.Max(4, (int)Math.Round(options.PaperBlockInches * dpi));
        var paper = PaperField(observed, expected, block);
        double inkFraction = InkFraction(observed, expected, paper, block);

        // S5, alignment: each bull's cell by phase correlation, the rest by the median shift.
        var cells = Cells(definition);
        var (aligned, shifts) = Align(observed, expected, paper, block, inkFraction, cells, registration, options.MaximumShiftInches * dpi, backend);
        expected = aligned;
        inkFraction = InkFraction(observed, expected, paper, block);

        // S6: the residual as a fraction of the paper-to-ink range, both signs.
        double range = Math.Max(0.05, 1 - inkFraction);
        var residual = new byte[width * height];
        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width) + x;
                double p = Math.Max(1, PaperAt(paper, block, width, height, x, y));
                double e = 1 - ((1 - (expected.Pixels[i] / 255.0)) * (1 - inkFraction));
                double n = observed.Pixels[i] / p;
                residual[i] = (byte)Math.Clamp(Math.Round(255 * Math.Abs(e - n) / range), 0, 255);
            }
        });

        // And nothing beyond the page's edge can become a candidate: on the N568 photograph the mat merged into one blob that swallowed every hole.
        for (int i = 0; i < residual.Length; i++)
        {
            if (!inside[i])
            {
                residual[i] = 0;
            }
        }

        // S7: open, threshold, close, fill, hull.
        int open = Math.Max(2, (int)Math.Round(options.OpenRadiusInches * dpi, MidpointRounding.ToEven));
        int close = Math.Max(3, (int)Math.Round(options.CloseRadiusInches * dpi, MidpointRounding.ToEven));
        var opened = backend.Morphology(new GrayImage(width, height, residual), MorphologyOperation.Open, open);
        double threshold = options.ResidualFraction * 255;
        var binary = new byte[width * height];
        for (int i = 0; i < binary.Length; i++)
        {
            binary[i] = opened.Pixels[i] >= threshold ? (byte)1 : (byte)0;
        }

        var closed = backend.Morphology(new GrayImage(width, height, binary), MorphologyOperation.Close, close);

        // S8: filters, declared zones, the position prior, and the weighted centre.
        var zones = Zones(definition, scene, tile);
        double smallest = Math.PI * Math.Pow(options.MinimumDiameterInches * dpi / 2, 2), largest = Math.PI * Math.Pow(options.MaximumDiameterInches * dpi / 2, 2);
        var holes = new List<RenderDifferenceHole>();
        var rejected = new List<RejectedBlob>();
        foreach (var blob in backend.FilledBlobs(closed))
        {
            var (area, hx, hy) = Polygon(blob.Hull);
            if (area <= 0)
            {
                continue;
            }

            double diameter = 2 * Math.Sqrt(area / Math.PI), solidity = blob.Area / area;
            double aspect = Math.Max(blob.Width, blob.Height) / Math.Max(1.0, Math.Min(blob.Width, blob.Height));
            var page = registration.ToPage(new PointD(hx, hy));
            var zone = zones.FirstOrDefault(z => page.X >= z.Left && page.X <= z.Right && page.Y >= z.Top && page.Y <= z.Bottom);
            var moments = area < smallest ? default : Moments(residual, expected, width, blob);
            bool merge = moments.Elongation >= options.SplitElongation && area <= 2 * largest;
            string? why = area < smallest ? FormattableString.Invariant($"too small, {diameter / dpi:0.000} in")
                : area > largest && !merge ? FormattableString.Invariant($"too large, {diameter / dpi:0.000} in")
                : solidity < options.MinimumSolidity ? FormattableString.Invariant($"not compact, hull solidity {solidity:0.00}")
                : aspect > options.MaximumAspect && !merge ? FormattableString.Invariant($"elongated, aspect {aspect:0.00}")
                : zone is not null ? $"inside {zone.Name}"
                : !cells.Any(c => Math.Abs(page.X - c.X) <= c.HalfWidth && Math.Abs(page.Y - c.Y) <= c.HalfHeight) ? "outside every bull's cell"
                : null;
            if (why is not null)
            {
                rejected.Add(new RejectedBlob(hx, hy, diameter / dpi, why));
                continue;
            }

            double cx = moments.X, cy = moments.Y;
            double closure = Closure(binary, width, height, cx, cy, diameter);
            if (closure < options.MinimumClosure)
            {
                rejected.Add(new RejectedBlob(hx, hy, diameter / dpi, FormattableString.Invariant($"open, rim closure {closure:0.00}")));
                continue;
            }

            if (merge)
            {
                foreach (var (mx, my) in Split(residual, width, blob, moments))
                {
                    holes.Add(new RenderDifferenceHole(mx, my, hx, hy, diameter / dpi, solidity, moments.Ink > 0.3, closure, moments.Elongation, PossibleMerge: true));
                }

                continue;
            }

            holes.Add(new RenderDifferenceHole(cx, cy, hx, hy, diameter / dpi, solidity, moments.Ink > 0.3, closure, moments.Elongation));
        }

        // S8: wider than the sheet's median by two robust standard deviations, flagged rather than refused.
        var single = holes.Where(h => !h.PossibleMerge).Select(h => h.DiameterInches).Order().ToList();
        if (single.Count >= 5)
        {
            double median = single[single.Count / 2];
            double spread = 1.4826 * single.Select(d => Math.Abs(d - median)).Order().ElementAt(single.Count / 2);
            for (int k = 0; k < holes.Count; k++)
            {
                if (!holes[k].PossibleMerge && holes[k].DiameterInches > median + (2 * spread))
                {
                    holes[k] = holes[k] with { Oversized = true };
                }
            }
        }

        return new RenderDifferenceResult(dpi, inkFraction, threshold, holes, rejected, shifts);
    }

    /// <summary>
    /// The expected image aligned to the observed one: each bull's cell shifted by the phase correlation of its expected and
    /// observed patches, both normalised by local paper, where the peak is clear and the shift within reach; everything else by
    /// the median of those shifts. The shifts are held on a grid of 16 pixels.
    /// </summary>
    private static (GrayImage Aligned, IReadOnlyList<PointD> Shifts) Align(GrayImage observed, GrayImage expected, double[] paper, int block, double inkFraction,
        IReadOnlyList<(double X, double Y, double HalfWidth, double HalfHeight)> cells, IPageMapping registration, double maximumShift, IImagingBackend backend)
    {
        int width = observed.Width, height = observed.Height;
        var found = new List<(int Left, int Top, int Right, int Bottom, PointD Shift)>();
        var shifts = new List<PointD>();
        foreach (var cell in cells)
        {
            var corners = new[] { new PointD(cell.X - cell.HalfWidth, cell.Y - cell.HalfHeight), new PointD(cell.X + cell.HalfWidth, cell.Y - cell.HalfHeight), new PointD(cell.X + cell.HalfWidth, cell.Y + cell.HalfHeight), new PointD(cell.X - cell.HalfWidth, cell.Y + cell.HalfHeight) }
                .Select(registration.ToImage).ToList();
            int left = Math.Max(0, (int)Math.Floor(corners.Min(c => c.X))), top = Math.Max(0, (int)Math.Floor(corners.Min(c => c.Y)));
            int right = Math.Min(width - 1, (int)Math.Ceiling(corners.Max(c => c.X))), bottom = Math.Min(height - 1, (int)Math.Ceiling(corners.Max(c => c.Y)));
            int w = right - left + 1, h = bottom - top + 1;
            if (w < 32 || h < 32)
            {
                shifts.Add(new PointD(double.NaN, double.NaN));
                continue;
            }

            var e = new byte[w * h];
            var n = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = ((top + y) * width) + left + x;
                    double p = Math.Max(1, PaperAt(paper, block, width, height, left + x, top + y));
                    e[(y * w) + x] = (byte)Math.Clamp(Math.Round(255 * (1 - ((1 - (expected.Pixels[i] / 255.0)) * (1 - inkFraction)))), 0, 255);
                    n[(y * w) + x] = (byte)Math.Clamp(Math.Round(255 * observed.Pixels[i] / p), 0, 255);
                }
            }

            var (shift, response) = backend.PhaseCorrelate(new GrayImage(w, h, e), new GrayImage(w, h, n));
            bool usable = response >= 0.05 && Math.Sqrt((shift.X * shift.X) + (shift.Y * shift.Y)) <= maximumShift;
            shifts.Add(usable ? shift : new PointD(double.NaN, double.NaN));
            if (usable)
            {
                found.Add((left, top, right, bottom, shift));
            }
        }

        var median = found.Count == 0 ? new PointD(0, 0) : new PointD(Median(found.Select(f => f.Shift.X)), Median(found.Select(f => f.Shift.Y)));
        const int grid = 16;
        int gw = (width + grid - 1) / grid, gh = (height + grid - 1) / grid;
        var sx = new double[gw * gh];
        var sy = new double[gw * gh];
        for (int j = 0; j < gh; j++)
        {
            for (int i = 0; i < gw; i++)
            {
                int cx = (i * grid) + (grid / 2), cy = (j * grid) + (grid / 2);
                var covering = found.FirstOrDefault(f => cx >= f.Left && cx <= f.Right && cy >= f.Top && cy <= f.Bottom);
                var s = covering.Right > 0 ? covering.Shift : median;
                sx[(j * gw) + i] = s.X;
                sy[(j * gw) + i] = s.Y;
            }
        }

        var pixels = new byte[width * height];
        Parallel.For(0, height, y =>
        {
            int j = y / grid;
            for (int x = 0; x < width; x++)
            {
                int g = (j * gw) + (x / grid);
                pixels[(y * width) + x] = Sample(expected, x - sx[g], y - sy[g]);
            }
        });

        return (new GrayImage(width, height, pixels), shifts);
    }

    private static byte Sample(GrayImage image, double x, double y)
    {
        if (!(x >= 0 && y >= 0 && x <= image.Width - 1 && y <= image.Height - 1))
        {
            return 255;
        }

        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, image.Width - 1), y1 = Math.Min(y0 + 1, image.Height - 1);
        double fx = x - x0, fy = y - y0;
        var p = image.Pixels;
        int w = image.Width;
        return (byte)Math.Clamp(Math.Round(((1 - fy) * (((1 - fx) * p[(y0 * w) + x0]) + (fx * p[(y0 * w) + x1]))) + (fy * (((1 - fx) * p[(y1 * w) + x0]) + (fx * p[(y1 * w) + x1])))), 0, 255);
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        int n = sorted.Length;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2;
    }

    /// <summary>
    /// The image pixels inside the page, NOTES-FROM-PLANNING.md entry 23 section 4: the page's edge in dmm, 64 points a side, mapped into
    /// the image through the registration, and filled row by row by the even-odd rule at pixel centres. The points along each side
    /// follow a photograph's curved mapping as well as a scan's straight one.
    /// </summary>
    internal static bool[] SheetMask(int pageWidth, int pageHeight, IPageMapping registration, int width, int height)
    {
        const int PerSide = 64;
        var edge = new List<PointD>(4 * PerSide);
        for (int side = 0; side < 4; side++)
        {
            for (int k = 0; k < PerSide; k++)
            {
                double t = (double)k / PerSide;
                var page = side switch
                {
                    0 => new PointD(t * pageWidth, 0),
                    1 => new PointD(pageWidth, t * pageHeight),
                    2 => new PointD((1 - t) * pageWidth, pageHeight),
                    _ => new PointD(0, (1 - t) * pageHeight),
                };
                edge.Add(registration.ToImage(page));
            }
        }

        var inside = new bool[width * height];
        Parallel.For(0, height, y =>
        {
            var crossings = new List<double>();
            for (int k = 0; k < edge.Count; k++)
            {
                PointD a = edge[k], b = edge[(k + 1) % edge.Count];
                if ((a.Y <= y) != (b.Y <= y))
                {
                    crossings.Add(a.X + ((y - a.Y) * (b.X - a.X) / (b.Y - a.Y)));
                }
            }

            crossings.Sort();
            for (int c = 0; c + 1 < crossings.Count; c += 2)
            {
                int x0 = Math.Max(0, (int)Math.Ceiling(crossings[c])), x1 = Math.Min(width - 1, (int)Math.Floor(crossings[c + 1]));
                for (int x = x0; x <= x1; x++)
                {
                    inside[(y * width) + x] = true;
                }
            }
        });

        return inside;
    }

    /// <summary>
    /// The observed image with every pixel beyond the sheet set to the sheet's paper level: the 90th percentile of the pixels inside the
    /// sheet that the render calls paper. The caller's image is not changed.
    /// </summary>
    private static GrayImage WithinSheet(GrayImage observed, GrayImage expected, bool[] inside)
    {
        if (Array.TrueForAll(inside, i => i))
        {
            return observed;
        }

        var histogram = new long[256];
        long count = 0;
        for (int i = 0; i < inside.Length; i++)
        {
            if (inside[i] && expected.Pixels[i] >= 250)
            {
                histogram[observed.Pixels[i]]++;
                count++;
            }
        }

        byte paper = 255;
        for (int level = 0, seen = 0; level < 256 && count > 0; level++)
        {
            seen += (int)histogram[level];
            if (seen >= 0.9 * count)
            {
                paper = (byte)level;
                break;
            }
        }

        var pixels = (byte[])observed.Pixels.Clone();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (!inside[i])
            {
                pixels[i] = paper;
            }
        }

        return new GrayImage(observed.Width, observed.Height, pixels);
    }

    /// <summary>The 95th percentile of the observed pixels the render calls paper, per block, with empty blocks filled from their neighbours and every block averaged with its eight.</summary>
    private static double[] PaperField(GrayImage observed, GrayImage expected, int block)
    {
        int bw = (observed.Width + block - 1) / block, bh = (observed.Height + block - 1) / block;
        var level = new double[bw * bh];
        Parallel.For(0, bh, j =>
        {
            var histogram = new int[256];
            for (int i = 0; i < bw; i++)
            {
                Array.Clear(histogram);
                int count = 0;
                for (int y = j * block; y < Math.Min(observed.Height, (j + 1) * block); y++)
                {
                    for (int x = i * block; x < Math.Min(observed.Width, (i + 1) * block); x++)
                    {
                        int k = (y * observed.Width) + x;
                        if (expected.Pixels[k] >= 230)
                        {
                            histogram[observed.Pixels[k]]++;
                            count++;
                        }
                    }
                }

                level[(j * bw) + i] = count < 16 ? double.NaN : Quantile(histogram, count, 0.95);
            }
        });

        for (int pass = 0; pass < Math.Max(bw, bh) && level.Any(double.IsNaN); pass++)
        {
            var next = (double[])level.Clone();
            for (int j = 0; j < bh; j++)
            {
                for (int i = 0; i < bw; i++)
                {
                    if (!double.IsNaN(level[(j * bw) + i]))
                    {
                        continue;
                    }

                    var around = Neighbours(level, bw, bh, i, j).Where(v => !double.IsNaN(v)).ToList();
                    next[(j * bw) + i] = around.Count == 0 ? double.NaN : around.Average();
                }
            }

            level = next;
        }

        var smoothed = new double[level.Length];
        for (int j = 0; j < bh; j++)
        {
            for (int i = 0; i < bw; i++)
            {
                smoothed[(j * bw) + i] = Neighbours(level, bw, bh, i, j).Append(level[(j * bw) + i]).Where(v => !double.IsNaN(v)).DefaultIfEmpty(245).Average();
            }
        }

        return smoothed;
    }

    private static IEnumerable<double> Neighbours(double[] level, int bw, int bh, int i, int j)
    {
        for (int dj = -1; dj <= 1; dj++)
        {
            for (int di = -1; di <= 1; di++)
            {
                if ((di != 0 || dj != 0) && i + di >= 0 && i + di < bw && j + dj >= 0 && j + dj < bh)
                {
                    yield return level[((j + dj) * bw) + i + di];
                }
            }
        }
    }

    private static double PaperAt(double[] level, int block, int width, int height, int x, int y)
    {
        int bw = (width + block - 1) / block, bh = (height + block - 1) / block;
        double fx = ((x + 0.5) / block) - 0.5, fy = ((y + 0.5) / block) - 0.5;
        int i0 = Math.Clamp((int)Math.Floor(fx), 0, bw - 1), j0 = Math.Clamp((int)Math.Floor(fy), 0, bh - 1);
        int i1 = Math.Min(i0 + 1, bw - 1), j1 = Math.Min(j0 + 1, bh - 1);
        double tx = Math.Clamp(fx - i0, 0, 1), ty = Math.Clamp(fy - j0, 0, 1);
        return ((1 - ty) * (((1 - tx) * level[(j0 * bw) + i0]) + (tx * level[(j0 * bw) + i1]))) + (ty * (((1 - tx) * level[(j1 * bw) + i0]) + (tx * level[(j1 * bw) + i1])));
    }

    /// <summary>The median ratio of observed to local paper where the render calls solid ink.</summary>
    private static double InkFraction(GrayImage observed, GrayImage expected, double[] paper, int block)
    {
        var histogram = new int[1001];
        int count = 0;
        for (int y = 0; y < observed.Height; y += 2)
        {
            for (int x = 0; x < observed.Width; x += 2)
            {
                int k = (y * observed.Width) + x;
                if (expected.Pixels[k] <= 20)
                {
                    double ratio = observed.Pixels[k] / Math.Max(1, PaperAt(paper, block, observed.Width, observed.Height, x, y));
                    histogram[Math.Clamp((int)Math.Round(ratio * 1000), 0, 1000)]++;
                    count++;
                }
            }
        }

        if (count == 0)
        {
            return 0.25;
        }

        int seen = 0;
        for (int b = 0; b <= 1000; b++)
        {
            seen += histogram[b];
            if (seen * 2 >= count)
            {
                return b / 1000.0;
            }
        }

        return 0.25;
    }

    private static double Quantile(int[] histogram, int count, double q)
    {
        long target = Math.Max(1, (long)Math.Ceiling(q * count)), seen = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            seen += histogram[i];
            if (seen >= target)
            {
                return i;
            }
        }

        return histogram.Length - 1;
    }

    private readonly record struct BlobMoments(double X, double Y, double Ink, double Elongation, double AxisX, double AxisY, double Major);

    /// <summary>
    /// The residual-weighted centroid inside the hull, the mean expected ink coverage there, and the weighted second moments:
    /// the ratio of the principal standard deviations, the major axis, and the standard deviation along it, pixels.
    /// </summary>
    private static BlobMoments Moments(byte[] residual, GrayImage expected, int width, ImageBlob blob)
    {
        double sw = 0, sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, ink = 0;
        int n = 0;
        for (int y = blob.Top; y < blob.Top + blob.Height; y++)
        {
            for (int x = blob.Left; x < blob.Left + blob.Width; x++)
            {
                if (!Inside(blob.Hull, x, y))
                {
                    continue;
                }

                int i = (y * width) + x;
                double weight = residual[i];
                sw += weight;
                sx += weight * x;
                sy += weight * y;
                sxx += weight * x * x;
                syy += weight * y * y;
                sxy += weight * x * y;
                ink += 1 - (expected.Pixels[i] / 255.0);
                n++;
            }
        }

        if (sw == 0)
        {
            return new BlobMoments(blob.Left + (blob.Width / 2.0), blob.Top + (blob.Height / 2.0), 0, 1, 1, 0, 0);
        }

        double mx = sx / sw, my = sy / sw;
        double a = (sxx / sw) - (mx * mx), c = (syy / sw) - (my * my), b = (sxy / sw) - (mx * my);
        double mean = (a + c) / 2, root = Math.Sqrt((((a - c) / 2) * ((a - c) / 2)) + (b * b));
        double major = Math.Max(0, mean + root), minor = Math.Max(1e-9, mean - root), angle = 0.5 * Math.Atan2(2 * b, a - c);
        return new BlobMoments(mx, my, ink / Math.Max(1, n), Math.Sqrt(major / minor), Math.Cos(angle), Math.Sin(angle), Math.Sqrt(major));
    }

    /// <summary>Two centres by residual-weighted two-means over the hull, started one standard deviation either side of the centroid along the major axis.</summary>
    private static (double X, double Y)[] Split(byte[] residual, int width, ImageBlob blob, BlobMoments m)
    {
        var centres = new (double X, double Y)[] { (m.X - (m.Major * m.AxisX), m.Y - (m.Major * m.AxisY)), (m.X + (m.Major * m.AxisX), m.Y + (m.Major * m.AxisY)) };
        for (int iteration = 0; iteration < 20; iteration++)
        {
            var sw = new double[2];
            var sx = new double[2];
            var sy = new double[2];
            for (int y = blob.Top; y < blob.Top + blob.Height; y++)
            {
                for (int x = blob.Left; x < blob.Left + blob.Width; x++)
                {
                    double weight = residual[(y * width) + x];
                    if (weight == 0 || !Inside(blob.Hull, x, y))
                    {
                        continue;
                    }

                    int k = Math.Pow(x - centres[0].X, 2) + Math.Pow(y - centres[0].Y, 2) <= Math.Pow(x - centres[1].X, 2) + Math.Pow(y - centres[1].Y, 2) ? 0 : 1;
                    sw[k] += weight;
                    sx[k] += weight * x;
                    sy[k] += weight * y;
                }
            }

            for (int k = 0; k < 2; k++)
            {
                if (sw[k] > 0)
                {
                    centres[k] = (sx[k] / sw[k], sy[k] / sw[k]);
                }
            }
        }

        return centres;
    }

    /// <summary>The fraction of 360 rays from the centre, one pixel steps, that meet a foreground pixel of <paramref name="binary"/> within <paramref name="reach"/> pixels.</summary>
    private static double Closure(byte[] binary, int width, int height, double cx, double cy, double reach)
    {
        int closed = 0;
        for (int k = 0; k < 360; k++)
        {
            double dx = Math.Cos(k * Math.PI / 180), dy = Math.Sin(k * Math.PI / 180);
            for (double r = 0; r <= reach; r++)
            {
                int x = (int)Math.Round(cx + (r * dx)), y = (int)Math.Round(cy + (r * dy));
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    break;
                }

                if (binary[(y * width) + x] != 0)
                {
                    closed++;
                    break;
                }
            }
        }

        return closed / 360.0;
    }

    private static bool Inside(IReadOnlyList<PointD> hull, double x, double y)
    {
        int sign = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            var a = hull[i];
            var b = hull[(i + 1) % hull.Count];
            double cross = ((b.X - a.X) * (y - a.Y)) - ((b.Y - a.Y) * (x - a.X));
            if (cross == 0)
            {
                continue;
            }

            int s = Math.Sign(cross);
            if (sign == 0)
            {
                sign = s;
            }
            else if (s != sign)
            {
                return false;
            }
        }

        return true;
    }

    private static (double Area, double X, double Y) Polygon(IReadOnlyList<PointD> hull)
    {
        double a = 0, sx = 0, sy = 0;
        for (int i = 0; i < hull.Count; i++)
        {
            var p = hull[i];
            var q = hull[(i + 1) % hull.Count];
            double cross = (p.X * q.Y) - (q.X * p.Y);
            a += cross;
            sx += (p.X + q.X) * cross;
            sy += (p.Y + q.Y) * cross;
        }

        return a == 0 ? (0, double.NaN, double.NaN) : (Math.Abs(a) / 2, sx / (3 * a), sy / (3 * a));
    }

    /// <summary>
    /// The declared exclusion zones of stage S5, page dmm: every marker's footprint with its quiet zone, every code's modules
    /// with its quiet zone, the data block, and the identifier text.
    /// </summary>
    private static List<Zone> Zones(TargetDefinition definition, Scene scene, int tile)
    {
        var zones = new List<Zone>();
        if (definition.Fiducials is { } fiducials)
        {
            double half = (fiducials.MarkerSize / 2.0) + fiducials.QuietZone;
            zones.AddRange(PageRegistration.ExpectedMarkers(definition, tile).Select(m => new Zone($"marker {m.Id}", m.X - half, m.Y - half, m.X + half, m.Y + half)));
        }

        var codeItems = scene.Items.OfType<RectFill>().Where(r => r.Layer == SceneLayer.Codes).ToList();
        if (definition.Codes is { } codes && codeItems.Count > 0)
        {
            double quiet = (codes.QuietZone ?? 4) * codes.ModuleSize;
            foreach (var group in codeItems.GroupBy(r => codes.Positions.Select((p, k) => (k, Distance: Math.Pow((r.X / 2.0) - p.X, 2) + Math.Pow((r.Y / 2.0) - p.Y, 2))).MinBy(t => t.Distance).k))
            {
                zones.Add(new Zone($"code {group.Key}", group.Min(r => r.X / 2.0) - quiet, group.Min(r => r.Y / 2.0) - quiet, group.Max(r => (r.X + r.Width) / 2.0) + quiet, group.Max(r => (r.Y + r.Height) / 2.0) + quiet));
            }
        }

        if (definition.DataBlock is { } data)
        {
            zones.Add(new Zone("the data block", data.X, data.Y, data.X + data.Width, data.Y + data.Height));
        }

        foreach (var text in scene.Items.OfType<TextRun>().Where(t => t.Layer == SceneLayer.Identifier))
        {
            double size = text.FontSize / 2.0, length = 0.6 * size * text.Text.Length, x = text.X / 2.0, baseline = text.Baseline / 2.0;
            double left = text.Anchor switch { TextAnchor.Centre => x - (length / 2), TextAnchor.Right => x - length, _ => x };
            zones.Add(new Zone("the identifier", left - 10, baseline - size - 10, left + length + 10, baseline + (0.3 * size) + 10));
        }

        return zones;
    }

    /// <summary>Each bull's cell, page dmm: centred on the bull, the grid pitch across and down, from the spacing of the bulls.</summary>
    private static List<(double X, double Y, double HalfWidth, double HalfHeight)> Cells(TargetDefinition definition)
    {
        static double Pitch(IEnumerable<int> values)
        {
            var distinct = values.Distinct().Order().ToList();
            var gaps = distinct.Zip(distinct.Skip(1), (a, b) => b - a).Where(g => g > 0).ToList();
            return gaps.Count == 0 ? 0 : gaps.Min();
        }

        var scoring = definition.Bulls.Where(b => b.Scoring).ToList();
        double pitchX = Pitch(scoring.Select(b => b.X)), pitchY = Pitch(scoring.Select(b => b.Y));
        double pitch = Math.Max(pitchX, pitchY);
        pitchX = pitchX > 0 ? pitchX : pitch;
        pitchY = pitchY > 0 ? pitchY : pitch;
        return [.. definition.Bulls.Select(b => ((double)b.X, (double)b.Y, pitchX / 2, pitchY / 2))];
    }
}
