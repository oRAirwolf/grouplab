using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Capture;

/// <summary>A crossing of a printed grid in a photograph, in image pixels, with its place in the lattice, columns and rows from the seed.</summary>
public sealed record GridCrossing(PointD Image, int Column, int Row);

/// <summary>
/// A printed grid registered: its crossings, the spacing between them in pixels, and two mappings from image pixels to lattice units, a plain
/// homography and one with the lens's radial distortion, each with the root mean square distance of the crossings from where it puts them.
/// </summary>
public sealed record GridFit(IReadOnlyList<GridCrossing> Crossings, double SpacingPixels, Homography ImageToLattice, double HomographyRms, RadialHomographyMapping? Lens, double? LensRms);

/// <summary>
/// Registration from a sheet's own printed grid, NOTES-FROM-PLANNING.md entry 172 section 3 item 4 and entry 158: a target GroupLab did not
/// print, whose grid of lines at a known pitch is dense ground truth for perspective and lens distortion, because every crossing's true
/// position is known once it is counted.
/// <para>
/// <b>How.</b> The caller gives the lines as a mask: on the National Target Company ST-4 they are orange, and red less blue picks them out.
/// Thick printing, bars and bands, is taken out by an opening wider than a line. A crossing is where a long run of line pixels passes through
/// a point both along the rows and down the columns. The lattice is walked from the crossing nearest the image's center: each neighbour is
/// looked for where the neighbours already found say it should be, so a lens that bows the lines is followed rather than fought. Crossings
/// the walk never reaches, such as those of printing inside a diamond, are left out.
/// </para>
/// </summary>
public static class GridRegistration
{
    /// <summary>
    /// The thin lines of a mask: <paramref name="lines"/> less anything <paramref name="thickRadius"/> or more across, which an opening keeps.
    /// </summary>
    public static bool[] Thin(bool[] lines, int width, int height, int thickRadius)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var thick = Dilate(Erode(lines, width, height, thickRadius), width, height, thickRadius + 4);
        var thin = new bool[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            thin[i] = lines[i] && !thick[i];
        }

        return thin;
    }

    /// <summary>
    /// Where lines cross: at each point, the share of line pixels in a run of 2 <paramref name="halfLength"/> + 1 along the row and down the
    /// column, the lesser of the two, as local maxima above <paramref name="least"/>, no two closer than a run.
    /// </summary>
    public static IReadOnlyList<PointD> Crossings(bool[] thin, int width, int height, int halfLength, double least = 0.35)
    {
        ArgumentNullException.ThrowIfNull(thin);
        var along = new float[thin.Length];
        var down = new float[thin.Length];
        int run = (2 * halfLength) + 1;
        for (int y = 0; y < height; y++)
        {
            int sum = 0;
            for (int x = -halfLength; x < width + halfLength; x++)
            {
                if (x + halfLength < width && thin[(y * width) + x + halfLength])
                {
                    sum++;
                }

                if (x - halfLength - 1 >= 0 && thin[(y * width) + x - halfLength - 1])
                {
                    sum--;
                }

                if (x >= 0 && x < width)
                {
                    along[(y * width) + x] = (float)sum / run;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            int sum = 0;
            for (int y = -halfLength; y < height + halfLength; y++)
            {
                if (y + halfLength < height && thin[((y + halfLength) * width) + x])
                {
                    sum++;
                }

                if (y - halfLength - 1 >= 0 && thin[((y - halfLength - 1) * width) + x])
                {
                    sum--;
                }

                if (y >= 0 && y < height)
                {
                    down[(y * width) + x] = (float)sum / run;
                }
            }
        }

        var candidates = new List<(int X, int Y, float Score)>();
        for (int i = 0; i < thin.Length; i++)
        {
            float score = Math.Min(along[i], down[i]);
            if (score > least)
            {
                candidates.Add((i % width, i / width, score));
            }
        }

        // Strongest first, and nothing kept within a run of a stronger one; then each to the centroid of the candidates that fell to it.
        var kept = new List<(double X, double Y, double W, float Score)>();
        foreach (var c in candidates.OrderByDescending(c => c.Score))
        {
            int near = kept.FindIndex(k => Math.Abs(k.X / k.W - c.X) <= halfLength && Math.Abs(k.Y / k.W - c.Y) <= halfLength);
            if (near < 0)
            {
                kept.Add((c.X * c.Score, c.Y * c.Score, c.Score, c.Score));
            }
            else if (Math.Abs(kept[near].X / kept[near].W - c.X) <= 3 && Math.Abs(kept[near].Y / kept[near].W - c.Y) <= 3)
            {
                var k = kept[near];
                kept[near] = (k.X + (c.X * c.Score), k.Y + (c.Y * c.Score), k.W + c.Score, k.Score);
            }
        }

        return [.. kept.Select(k => new PointD(k.X / k.W, k.Y / k.W))];
    }

    /// <summary>
    /// The crossings counted into a lattice, from the one nearest the image's center, or an empty list where the crossings do not make a grid.
    /// </summary>
    public static (IReadOnlyList<GridCrossing> Crossings, double Spacing) Index(IReadOnlyList<PointD> crossings, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(crossings);
        if (crossings.Count < 9)
        {
            return ([], 0);
        }

        // The grid's step, by a vote: every pair of crossings between 40 and 1200 pixels apart votes for the vector between them, folded into one
        // half plane, and the most voted vector that no shorter well voted vector divides is the step. Spurious crossings, printed numerals and
        // the corners of a diamond, vote for nothing in particular, while the lattice's own step is voted for by nearly every crossing.
        var votes = new Dictionary<(int X, int Y), HashSet<int>>();
        const int bin = 12;
        for (int a = 0; a < crossings.Count; a++)
        {
            for (int b = 0; b < crossings.Count; b++)
            {
                double dx = crossings[b].X - crossings[a].X, dy = crossings[b].Y - crossings[a].Y, length = Math.Sqrt((dx * dx) + (dy * dy));
                if (length < 40 || length > 1200 || dx < 0 || (dx == 0 && dy < 0))
                {
                    continue;
                }

                var key = ((int)Math.Round(dx / bin), (int)Math.Round(dy / bin));
                if (!votes.TryGetValue(key, out var joined))
                {
                    votes[key] = joined = [];
                }

                joined.Add(a);
                joined.Add(b);
            }
        }

        if (votes.Count == 0)
        {
            return ([], 0);
        }

        // A step is as good as the number of different crossings it joins: the lattice's joins nearly all of them, a numeral's only its own.
        int most = votes.Values.Max(j => j.Count);
        var strong = votes.Where(kv => kv.Value.Count >= 0.8 * most).Select(kv => new PointD(kv.Key.X * bin, kv.Key.Y * bin)).OrderBy(s => (s.X * s.X) + (s.Y * s.Y)).ToList();
        var u = strong[0];
        double spacing = Math.Sqrt((u.X * u.X) + (u.Y * u.Y));
        if (Math.Abs(u.Y) > Math.Abs(u.X))
        {
            u = new PointD(u.Y, -u.X);
        }

        if (u.X < 0)
        {
            u = new PointD(-u.X, -u.Y);
        }

        var v = new PointD(-u.Y, u.X);

        // A seed inside printing starts a walk that goes nowhere, and the middle of a photograph is often printing, so every crossing is tried
        // as the seed and the walk that reaches the most crossings is kept; a tie goes to the seed nearest the middle.
        var centre = new PointD(width / 2.0, height / 2.0);
        Dictionary<(int Column, int Row), PointD> best = [];
        foreach (var start in crossings.OrderBy(p => Distance(p, centre)))
        {
            var walked = Walk(start);
            if (walked.Count > best.Count)
            {
                best = walked;
            }
        }

        return ([.. best.Select(kv => new GridCrossing(kv.Value, kv.Key.Column, kv.Key.Row))], spacing);

        Dictionary<(int Column, int Row), PointD> Walk(PointD seed)
        {
            var at = new Dictionary<(int Column, int Row), PointD> { [(0, 0)] = seed };
            var used = new HashSet<PointD> { seed };
            var queue = new Queue<(int Column, int Row)>();
            queue.Enqueue((0, 0));
            while (queue.Count > 0)
            {
                var (c, r) = queue.Dequeue();
                var here = at[(c, r)];
                foreach (var (dc, dr) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var key = (c + dc, r + dr);
                    if (at.ContainsKey(key))
                    {
                        continue;
                    }

                    // Where the neighbour should be: from the step this crossing's own found neighbours make, else the global step.
                    var step = LocalStep(at, (c, r), dc, dr) ?? new PointD((u.X * dc) + (v.X * dr), (u.Y * dc) + (v.Y * dr));
                    var expected = new PointD(here.X + step.X, here.Y + step.Y);
                    var found = crossings.Where(q => !used.Contains(q)).Select(q => (PointD?)q).MinBy(q => Distance(q!.Value, expected));
                    if (found is { } f && Distance(f, expected) < 0.2 * spacing)
                    {
                        at[key] = f;
                        used.Add(f);
                        queue.Enqueue(key);
                    }
                }
            }

            return at;
        }
    }

    /// <summary>The step to the neighbour in direction (<paramref name="dc"/>, <paramref name="dr"/>) that the crossings already found beside it make.</summary>
    private static PointD? LocalStep(Dictionary<(int Column, int Row), PointD> at, (int Column, int Row) from, int dc, int dr)
    {
        var here = at[from];
        if (at.TryGetValue((from.Column - dc, from.Row - dr), out var behind))
        {
            return new PointD(here.X - behind.X, here.Y - behind.Y);
        }

        foreach (var side in new[] { (dr, dc), (-dr, -dc) })
        {
            if (at.TryGetValue((from.Column + side.Item1, from.Row + side.Item2), out var beside) && at.TryGetValue((from.Column + side.Item1 + dc, from.Row + side.Item2 + dr), out var diagonal))
            {
                return new PointD(diagonal.X - beside.X, diagonal.Y - beside.Y);
            }
        }

        return null;
    }

    /// <summary>
    /// The lattice fitted: a homography from image pixels to lattice units, and with at least a dozen crossings the lens's radial terms as
    /// well, each with its root mean square residual in lattice units.
    /// </summary>
    public static GridFit? Fit(IReadOnlyList<GridCrossing> crossings, double spacing, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(crossings);
        if (crossings.Count < 6)
        {
            return null;
        }

        var image = crossings.Select(c => c.Image).ToList();
        var lattice = crossings.Select(c => new PointD(c.Column, c.Row)).ToList();
        if (HomographyEstimate.Fit(image, lattice) is not { } homography)
        {
            return null;
        }

        double Rms(Func<PointD, PointD> map) => Math.Sqrt(image.Zip(lattice).Average(p => Math.Pow(Distance(map(p.First), p.Second), 2)));
        RadialHomographyMapping? lens = null;
        double? lensRms = null;
        if (crossings.Count >= 12)
        {
            lens = LensFit.Fit(image, lattice, homography, width, height);
            lensRms = Rms(lens.ToPage);
        }

        return new GridFit(crossings, spacing, homography, Rms(homography.Apply), lens, lensRms);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToList();
        return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
    }

    private static bool[] Dilate(bool[] mask, int w, int h, int r) => Sweep(mask, w, h, r, true);

    private static bool[] Erode(bool[] mask, int w, int h, int r) => Sweep(mask, w, h, r, false);

    private static bool[] Sweep(bool[] mask, int w, int h, int r, bool any)
    {
        var rows = new bool[mask.Length];
        for (int y = 0; y < h; y++)
        {
            int count = 0;
            for (int x = -r; x < w + r; x++)
            {
                if (x + r < w && x + r >= 0 && mask[(y * w) + x + r] == any)
                {
                    count++;
                }

                if (x - r - 1 >= 0 && x - r - 1 < w && mask[(y * w) + x - r - 1] == any)
                {
                    count--;
                }

                if (x >= 0 && x < w)
                {
                    rows[(y * w) + x] = any ? count > 0 : count == 0;
                }
            }
        }

        var result = new bool[mask.Length];
        for (int x = 0; x < w; x++)
        {
            int count = 0;
            for (int y = -r; y < h + r; y++)
            {
                if (y + r < h && y + r >= 0 && rows[((y + r) * w) + x] == any)
                {
                    count++;
                }

                if (y - r - 1 >= 0 && y - r - 1 < h && rows[((y - r - 1) * w) + x] == any)
                {
                    count--;
                }

                if (y >= 0 && y < h)
                {
                    result[(y * w) + x] = any ? count > 0 : count == 0;
                }
            }
        }

        return result;
    }
}
