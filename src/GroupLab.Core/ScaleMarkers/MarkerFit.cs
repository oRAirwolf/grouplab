using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.ScaleMarkers;

/// <summary>A point of a marker: where it is on its piece, millimetres, where it was found in the photo, and how far astray it may be, pixels.</summary>
public sealed record MarkerPoint(PointD Model, PointD Image, double Sigma);

/// <summary>
/// One rigid piece found in the photo: a bracket's two codes, a bar's two, a board's stickers as measured, or a card's four corners. Its
/// points keep their shape exactly; where it lies on the surface is fitted, except for the first, which fixes where the surface's millimetres
/// start.
/// </summary>
public sealed record MarkerBody(MarkerKind Kind, int Piece, IReadOnlyList<MarkerPoint> Points);

/// <summary>
/// Entry 365 section 0: one fit of the flat surface the target lies on to every marker found on it. The surface's millimetres are taken to
/// the photo by a homography; every piece but the first may lie anywhere on it, turned any way, and keeps its own shape, so two bars laid in
/// an L, four brackets at the corners and a card beside them all count together, each weighted by how closely its points are found.
/// <para>
/// The answer carries its own doubt: the fit's covariance, its scatter taken as at least what each point was said to be good to, carried to
/// the scale at any point of the photo. A single bar or a card alone fixes the scale along it well and the camera's angle badly, and the
/// covariance says so.
/// </para>
/// </summary>
public sealed class MarkerFit
{
    private readonly double[,] covariance;

    private MarkerFit(Homography surfaceToImage, double rmsPixels, double[,] covariance, IReadOnlyList<(double Angle, PointD Shift)> poses, IReadOnlyList<MarkerBody> bodies)
    {
        SurfaceToImage = surfaceToImage;
        ImageToSurface = surfaceToImage.Inverse();
        RmsPixels = rmsPixels;
        this.covariance = covariance;
        Poses = poses;
        Bodies = bodies;
    }

    /// <summary>The surface's millimetres to the photo's pixels.</summary>
    public Homography SurfaceToImage { get; }

    public Homography ImageToSurface { get; }

    /// <summary>How far the found points lie from where the fit puts them, pixels, root mean square.</summary>
    public double RmsPixels { get; }

    /// <summary>Each body's place on the surface, in the order given: the first is where the surface's millimetres start.</summary>
    public IReadOnlyList<(double Angle, PointD Shift)> Poses { get; }

    public IReadOnlyList<MarkerBody> Bodies { get; }

    /// <summary>A point of body <paramref name="index"/>, in its own millimetres, as it lies on the surface.</summary>
    public PointD OnSurface(int index, PointD model)
    {
        var (angle, shift) = Poses[index];
        double c = Math.Cos(angle), s = Math.Sin(angle);
        return new PointD((c * model.X) - (s * model.Y) + shift.X, (s * model.X) + (c * model.Y) + shift.Y);
    }

    /// <summary>A point of body <paramref name="index"/>, in its own millimetres, where it is in the photo.</summary>
    public PointD InImage(int index, PointD model) => SurfaceToImage.Apply(OnSurface(index, model));

    /// <summary>The fit, or null where the points cannot fix a surface: fewer than four, or all on one line.</summary>
    public static MarkerFit? Fit(IReadOnlyList<MarkerBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        var usable = bodies.Where(b => b.Points.Count > 0).ToList();
        if (usable.Sum(b => b.Points.Count) < 4 || usable.Count == 0)
        {
            return null;
        }

        // The first body is the one that fixes the most on its own: the most points over the widest reach in the photo.
        usable = [.. usable.OrderByDescending(b => b.Points.Count >= 4 ? b.Points.Count * Reach(b.Points.Select(p => p.Image)) : 0)];
        var first = usable[0];
        if (first.Points.Count < 4 || HomographyEstimate.Fit([.. first.Points.Select(p => p.Model)], [.. first.Points.Select(p => p.Image)]) is not { } start)
        {
            return null;
        }

        var back = start.Inverse();
        double scale = start[2, 2];
        var x = new List<double> { start[0, 0] / scale, start[0, 1] / scale, start[0, 2] / scale, start[1, 0] / scale, start[1, 1] / scale, start[1, 2] / scale, start[2, 0] / scale, start[2, 1] / scale };
        foreach (var body in usable.Skip(1))
        {
            var (angle, shift) = Rigid([.. body.Points.Select(p => p.Model)], [.. body.Points.Select(p => back.Apply(p.Image))]);
            x.AddRange([angle, shift.X, shift.Y]);
        }

        int n = 2 * usable.Sum(b => b.Points.Count);
        IPoses model = new FreePoses();
        var (parameters, cost, steps) = Solve(usable, model, [.. x], n);

        // Brackets laid near a target's corners lie on a rectangle, as nearly as a hand lays them: three or more are held to one, each
        // inside corner within about 3 mm and each piece square to it within about 3 degrees, as extra observations the photo can outweigh.
        // A hard rectangle took a third of a millimetre astray into a percent of scale; none left the camera's angle across a poster loose
        // (docs/PHASE1-RESULTS.md, entry 365). Entry 375 loosened it from half a millimetre and half a degree, since the brackets need not be
        // cut or laid neatly: the brackets alone measured the same, 0.12 percent median and 0.75 worst on the posters.
        var brackets = Enumerable.Range(0, usable.Count).Where(i => usable[i].Kind == MarkerKind.Bracket).ToList();
        SoftRectangle? rectangle = null;
        if (brackets.Count >= 3 && brackets.Select(i => usable[i].Piece).Distinct().Count() == brackets.Count)
        {
            rectangle = new SoftRectangle([.. brackets], [.. brackets.Select(i => usable[i].Piece)], parameters.Length);
            var start2 = parameters.Concat(rectangle.Start(parameters, model)).ToArray();
            var (p2, c2, s2) = Solve(usable, model, start2, n + rectangle.Count, rectangle);
            (parameters, cost, steps) = (p2, c2, s2);
            n += rectangle.Count;
        }

        double Residuals(double[] p, double[] r) => Cost(usable, model, p, r, rectangle);
        var result = new LevenbergMarquardt.Result(parameters, cost, 0);

        // The covariance: the Jacobian at the answer, and the scatter taken as at least what the points were said to be good to.
        int k = parameters.Length;
        var r0 = new double[n];
        var r1 = new double[n];
        Residuals(parameters, r0);
        var jtj = new double[k, k];
        var columns = new double[k][];
        for (int j = 0; j < k; j++)
        {
            var moved = (double[])parameters.Clone();
            moved[j] += steps[j];
            Residuals(moved, r1);
            columns[j] = new double[n];
            for (int i = 0; i < n; i++)
            {
                // Each parameter in units of its own step, which keeps a homography's entries, 1e-5 to 1e3 apart, from swamping the inverse.
                columns[j][i] = r1[i] - r0[i];
            }
        }

        for (int a = 0; a < k; a++)
        {
            for (int b = a; b < k; b++)
            {
                double sum = 0;
                for (int i = 0; i < n; i++)
                {
                    sum += columns[a][i] * columns[b][i];
                }

                jtj[a, b] = jtj[b, a] = sum;
            }
        }

        if (Invert(jtj) is not { } inverse)
        {
            return null;
        }

        double spread = n > k ? Math.Max(1, result.Cost / (n - k)) : 1;
        for (int a = 0; a < k; a++)
        {
            for (int b = 0; b < k; b++)
            {
                inverse[a, b] *= spread * steps[a] * steps[b];
            }
        }

        double squares = 0;
        int index = 0;
        foreach (var point in usable.SelectMany(b => b.Points))
        {
            squares += (Math.Pow(r0[index] * point.Sigma, 2) + Math.Pow(r0[index + 1] * point.Sigma, 2)) / 2;
            index += 2;
        }

        var poses = Enumerable.Range(0, usable.Count).Select(b => model.Pose(parameters, b)).ToList();

        return new MarkerFit(Surface(parameters), Math.Sqrt(squares / (n / 2)), inverse, poses, usable);
    }

    private static (double[] Parameters, double Cost, double[] Steps) Solve(IReadOnlyList<MarkerBody> bodies, IPoses model, double[] start, int n, SoftRectangle? rectangle = null)
    {
        var steps = start.Select((v, i) => i < 8 ? Math.Max(Math.Abs(v) * 1e-6, i is 6 or 7 ? 1e-12 : 1e-9)
            : rectangle is not null && i >= rectangle.At ? rectangle.Step(i - rectangle.At) : model.Step(i - 8)).ToArray();
        var result = LevenbergMarquardt.Minimise((p, r) => Cost(bodies, model, p, r, rectangle), start, n, steps);
        return (result.Parameters, result.Cost, steps);
    }

    private static double Cost(IReadOnlyList<MarkerBody> bodies, IPoses model, double[] p, double[] r, SoftRectangle? rectangle = null)
    {
        var h = Surface(p);
        int i = 0;
        double cost = 0;
        for (int b = 0; b < bodies.Count; b++)
        {
            var (angle, shift) = model.Pose(p, b);
            double c = Math.Cos(angle), s = Math.Sin(angle);
            foreach (var point in bodies[b].Points)
            {
                var q = h.Apply(new PointD((c * point.Model.X) - (s * point.Model.Y) + shift.X, (s * point.Model.X) + (c * point.Model.Y) + shift.Y));
                r[i] = (q.X - point.Image.X) / point.Sigma;
                r[i + 1] = (q.Y - point.Image.Y) / point.Sigma;
                cost += (r[i] * r[i]) + (r[i + 1] * r[i + 1]);
                i += 2;
            }
        }

        if (rectangle is not null)
        {
            cost += rectangle.Residuals(p, model, r, i);
        }

        return double.IsFinite(cost) ? cost : 1e30;
    }

    /// <summary>Where each body lies on the surface, from the parameters after the homography's eight.</summary>
    private interface IPoses
    {
        (double Angle, PointD Shift) Pose(double[] p, int body);

        double Step(int index);
    }

    /// <summary>Every body free but the first: a turn and a shift each.</summary>
    private sealed class FreePoses : IPoses
    {
        public (double Angle, PointD Shift) Pose(double[] p, int body) => body == 0
            ? (0, new PointD(0, 0))
            : (p[8 + (3 * (body - 1))], new PointD(p[9 + (3 * (body - 1))], p[10 + (3 * (body - 1))]));

        public double Step(int index) => index % 3 == 0 ? 1e-7 : 1e-5;
    }

    /// <summary>
    /// The brackets held to a rectangle W by H, turned by A and moved by (X, Y) on the surface: five parameters after every other, and for each
    /// bracket three observations, its inside corner's two coordinates and its turn, each against how closely a hand lays a piece.
    /// </summary>
    private sealed class SoftRectangle(int[] brackets, int[] pieces, int at)
    {
        /// <summary>How closely a piece is laid against the corner, millimetres, and square to the target's edges, radians.</summary>
        public const double Laid = 3, Square = 3 * Math.PI / 180;

        public int At { get; } = at;

        public int Count => 3 * brackets.Length;

        public double Step(int index) => index == 0 ? 1e-7 : 1e-5;

        private static PointD Corner(int piece, double w, double h) => new(piece is 2 or 3 ? w : 0, piece >= 3 ? h : 0);

        public double Residuals(double[] p, IPoses model, double[] r, int i)
        {
            double angle = p[At], w = p[At + 3], h = p[At + 4], c = Math.Cos(angle), s = Math.Sin(angle), cost = 0;
            for (int k = 0; k < brackets.Length; k++)
            {
                var (turn, shift) = model.Pose(p, brackets[k]);
                var corner = Corner(pieces[k], w, h);
                r[i] = (shift.X - (p[At + 1] + (c * corner.X) - (s * corner.Y))) / Laid;
                r[i + 1] = (shift.Y - (p[At + 2] + (s * corner.X) + (c * corner.Y))) / Laid;
                r[i + 2] = Math.IEEERemainder(turn - angle, 2 * Math.PI) / Square;
                cost += (r[i] * r[i]) + (r[i + 1] * r[i + 1]) + (r[i + 2] * r[i + 2]);
                i += 3;
            }

            return cost;
        }

        /// <summary>The rectangle that the free fit's brackets come closest to.</summary>
        public double[] Start(double[] p, IPoses model)
        {
            var poses = brackets.Select(b => model.Pose(p, b)).ToArray();
            double angle = Math.Atan2(poses.Average(q => Math.Sin(q.Angle)), poses.Average(q => Math.Cos(q.Angle)));
            double c = Math.Cos(-angle), s = Math.Sin(-angle);
            var d = poses.Select(q => new PointD((c * q.Shift.X) - (s * q.Shift.Y), (s * q.Shift.X) + (c * q.Shift.Y))).ToArray();
            double Mean(Func<int, bool> which, Func<PointD, double> axis) => Enumerable.Range(0, pieces.Length).Where(i => which(pieces[i])).Select(i => axis(d[i])).DefaultIfEmpty(0).Average();
            double x0 = Mean(n => n is 1 or 4, q => q.X), y0 = Mean(n => n <= 2, q => q.Y);
            double w = Mean(n => n is 2 or 3, q => q.X) - x0, h = Mean(n => n >= 3, q => q.Y) - y0;
            double cc = Math.Cos(angle), ss = Math.Sin(angle);
            return [angle, (cc * x0) - (ss * y0), (ss * x0) + (cc * y0), w, h];
        }
    }

    /// <summary>
    /// The scale's relative doubt at a point of the photo, one standard deviation: how much the surface's size at that pixel moves as the fit
    /// may, through its covariance.
    /// </summary>
    public double ScaleDoubtAt(PointD image)
    {
        var p = Parameters(SurfaceToImage);
        double baseline = LogScale(p, image);
        var gradient = new double[8];
        for (int j = 0; j < 8; j++)
        {
            double step = Math.Max(Math.Abs(p[j]) * 1e-6, j is 6 or 7 ? 1e-12 : 1e-9);
            var moved = (double[])p.Clone();
            moved[j] += step;
            gradient[j] = (LogScale(moved, image) - baseline) / step;
        }

        double variance = 0;
        for (int a = 0; a < 8; a++)
        {
            for (int b = 0; b < 8; b++)
            {
                variance += gradient[a] * covariance[a, b] * gradient[b];
            }
        }

        return Math.Sqrt(Math.Max(0, variance));
    }

    /// <summary>The surface's millimetres a pixel at <paramref name="image"/>, the square root of the area a pixel covers.</summary>
    public double MillimetresPerPixel(PointD image)
    {
        var (xx, xy, yx, yy) = ImageToSurface.Jacobian(image);
        return Math.Sqrt(Math.Abs((xx * yy) - (xy * yx)));
    }

    private static double LogScale(double[] p, PointD image)
    {
        var (xx, xy, yx, yy) = Surface(p).Inverse().Jacobian(image);
        return 0.5 * Math.Log(Math.Abs((xx * yy) - (xy * yx)));
    }

    private static Homography Surface(double[] p) => new([p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], 1]);

    private static double[] Parameters(Homography h) => [h[0, 0] / h[2, 2], h[0, 1] / h[2, 2], h[0, 2] / h[2, 2], h[1, 0] / h[2, 2], h[1, 1] / h[2, 2], h[1, 2] / h[2, 2], h[2, 0] / h[2, 2], h[2, 1] / h[2, 2]];

    /// <summary>The turn and shift that best lay <paramref name="model"/> onto <paramref name="target"/>, no change of size.</summary>
    internal static (double Angle, PointD Shift) Rigid(IReadOnlyList<PointD> model, IReadOnlyList<PointD> target)
    {
        double mx = model.Average(p => p.X), my = model.Average(p => p.Y), tx = target.Average(p => p.X), ty = target.Average(p => p.Y);
        double sin = 0, cos = 0;
        for (int i = 0; i < model.Count; i++)
        {
            double ax = model[i].X - mx, ay = model[i].Y - my, bx = target[i].X - tx, by = target[i].Y - ty;
            sin += (ax * by) - (ay * bx);
            cos += (ax * bx) + (ay * by);
        }

        double angle = Math.Atan2(sin, cos), c = Math.Cos(angle), s = Math.Sin(angle);
        return (angle, new PointD(tx - ((c * mx) - (s * my)), ty - ((s * mx) + (c * my))));
    }

    private static double Reach(IEnumerable<PointD> points)
    {
        var list = points.ToList();
        return Math.Max(1, Math.Sqrt(Math.Pow(list.Max(p => p.X) - list.Min(p => p.X), 2) + Math.Pow(list.Max(p => p.Y) - list.Min(p => p.Y), 2)));
    }

    /// <summary>Gauss-Jordan with partial pivoting, or null for a singular matrix.</summary>
    internal static double[,]? Invert(double[,] m)
    {
        int k = m.GetLength(0);
        var a = (double[,])m.Clone();
        var inv = new double[k, k];
        for (int i = 0; i < k; i++)
        {
            inv[i, i] = 1;
        }

        double largest = 0;
        for (int i = 0; i < k; i++)
        {
            largest = Math.Max(largest, Math.Abs(a[i, i]));
        }

        for (int col = 0; col < k; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < k; row++)
            {
                if (Math.Abs(a[row, col]) > Math.Abs(a[pivot, col]))
                {
                    pivot = row;
                }
            }

            if (Math.Abs(a[pivot, col]) <= largest * 1e-14)
            {
                return null;
            }

            if (pivot != col)
            {
                for (int j = 0; j < k; j++)
                {
                    (a[col, j], a[pivot, j]) = (a[pivot, j], a[col, j]);
                    (inv[col, j], inv[pivot, j]) = (inv[pivot, j], inv[col, j]);
                }
            }

            double d = a[col, col];
            for (int j = 0; j < k; j++)
            {
                a[col, j] /= d;
                inv[col, j] /= d;
            }

            for (int row = 0; row < k; row++)
            {
                if (row == col || a[row, col] == 0)
                {
                    continue;
                }

                double f = a[row, col];
                for (int j = 0; j < k; j++)
                {
                    a[row, j] -= f * a[col, j];
                    inv[row, j] -= f * inv[col, j];
                }
            }
        }

        return inv;
    }
}
