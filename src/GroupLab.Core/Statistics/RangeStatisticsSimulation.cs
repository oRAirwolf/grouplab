using System.Globalization;

namespace GroupLab.Core.Statistics;

/// <summary>One cell of the range-statistic table: the mean, standard deviation, and 2.5 and 97.5 percent quantiles of a statistic.</summary>
public readonly record struct RangeMoments(double Mean, double Sd, double Q025, double Q975);

/// <summary>
/// One row of GroupLab's range-statistic table, docs/STATISTICS.md sections 5 and 15.3: for groups of <see cref="N"/> shots
/// from a circular bivariate normal with sigma 1, averaged over <see cref="Groups"/> groups, the distribution of extreme
/// spread, figure of merit, bounding-box diagonal and Rayleigh sigma.
/// </summary>
public sealed record RangeTableRow(int N, int Groups, long Replications, RangeMoments ExtremeSpread, RangeMoments FigureOfMerit, RangeMoments Diagonal, RangeMoments RayleighSigma)
{
    public const string Header = "n,nGroups,replications,ES_M,ES_SD,ES_Q025,ES_Q975,FoM_M,FoM_SD,FoM_Q025,FoM_Q975,D_M,D_SD,D_Q025,D_Q975,RS_M,RS_SD,RS_Q025,RS_Q975";

    public string ToCsv()
    {
        static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        static string M(RangeMoments m) => $"{F(m.Mean)},{F(m.Sd)},{F(m.Q025)},{F(m.Q975)}";
        return string.Create(CultureInfo.InvariantCulture, $"{N},{Groups},{Replications},{M(ExtremeSpread)},{M(FigureOfMerit)},{M(Diagonal)},{M(RayleighSigma)}");
    }

    public static RangeTableRow Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        var p = line.Split(',');
        double D(int i) => double.Parse(p[i], CultureInfo.InvariantCulture);
        RangeMoments M(int i) => new(D(i), D(i + 1), D(i + 2), D(i + 3));
        return new RangeTableRow(int.Parse(p[0], CultureInfo.InvariantCulture), int.Parse(p[1], CultureInfo.InvariantCulture), long.Parse(p[2], CultureInfo.InvariantCulture), M(3), M(7), M(11), M(15));
    }
}

/// <summary>
/// GroupLab's own Monte Carlo simulation of range statistics, docs/STATISTICS.md section 15.3: shotGroups' <c>DFdistr</c> is
/// GPL data and GroupLab must generate its own table and be measured against it, not ship it.
/// <para>
/// <b>What one replication is.</b> Ten independent groups of n shots are drawn from a circular bivariate normal with sigma 1.
/// Each group gives its extreme spread, figure of merit (the mean of the bounding box's width and height), bounding-box
/// diagonal, and Rayleigh sigma with the c4 correction of section 3.2. The running means over the first k groups are the
/// replication's values for k = 1 to 10 groups, so every cell has its full count of independent replications at the cost of ten
/// groups each, not fifty-five.
/// </para>
/// <para>
/// <b>Determinism.</b> Replications are split into a fixed number of chunks, each drawing from its own xoshiro256** stream seeded
/// by SplitMix64 from the seed, n and the chunk index, and chunks are merged in index order, so the table is identical whatever
/// the number of threads. Means and standard deviations are exact sums. Quantiles come from a histogram of 8,192 bins over six
/// times a pilot's mean, interpolated within the bin: one bin is 7.3e-4 of the mean, against section 15.3's 5e-3 tolerance, and small
/// enough that the forty histograms a replication writes stay in cache.
/// </para>
/// </summary>
public static class RangeStatisticsSimulation
{
    public const int MaximumGroups = 10;
    private const int Statistics = 4, Bins = 8192, Chunks = 256;

    public static IReadOnlyList<RangeTableRow> Simulate(int n, long replications, ulong seed)
    {
        if (n < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(n), n, "a range statistic needs two shots.");
        }

        // A pilot sets the histogram's range: six times the pilot mean holds far more than 97.5 percent of every statistic.
        var pilot = Chunk(n, 20000, seed ^ 0x9E3779B97F4A7C15UL, long.MaxValue, null);
        double[] upper = [.. Enumerable.Range(0, Statistics).Select(s => 6 * pilot.Sum[s, 0] / 20000)];

        long perChunk = replications / Chunks;
        var results = new Accumulator[Chunks];
        Parallel.For(0, Chunks, c => results[c] = Chunk(n, c == Chunks - 1 ? replications - (perChunk * (Chunks - 1)) : perChunk, Stream(seed, n, c), 0, upper));

        var total = new Accumulator(upper);
        foreach (var r in results)
        {
            total.Add(r);
        }

        var rows = new List<RangeTableRow>();
        for (int k = 0; k < MaximumGroups; k++)
        {
            RangeMoments Moments(int s)
            {
                double mean = total.Sum[s, k] / replications;
                double sd = Math.Sqrt(Math.Max(0, (total.SumSquares[s, k] - (replications * mean * mean)) / (replications - 1)));
                return new RangeMoments(mean, sd, total.Quantile(s, k, 0.025, replications), total.Quantile(s, k, 0.975, replications));
            }

            rows.Add(new RangeTableRow(n, k + 1, replications, Moments(0), Moments(1), Moments(2), Moments(3)));
        }

        return rows;
    }

    private static ulong Stream(ulong seed, int n, int chunk)
    {
        ulong state = seed ^ ((ulong)n << 32) ^ (ulong)chunk;
        return SplitMix(ref state);
    }

    private static ulong SplitMix(ref ulong state)
    {
        ulong z = state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Sums, sums of squares and histograms per statistic and group count.</summary>
    private sealed class Accumulator(double[]? upper)
    {
        public double[,] Sum { get; } = new double[Statistics, MaximumGroups];

        public double[,] SumSquares { get; } = new double[Statistics, MaximumGroups];

        public long[]? Histogram { get; } = upper is null ? null : new long[Statistics * MaximumGroups * (Bins + 1)];

        public double[]? Upper { get; } = upper;

        public void Record(int s, int k, double value)
        {
            Sum[s, k] += value;
            SumSquares[s, k] += value * value;
            if (Histogram is not null)
            {
                int bin = (int)(value / Upper![s] * Bins);
                Histogram[(((s * MaximumGroups) + k) * (Bins + 1)) + Math.Clamp(bin, 0, Bins)]++;
            }
        }

        public void Add(Accumulator other)
        {
            for (int s = 0; s < Statistics; s++)
            {
                for (int k = 0; k < MaximumGroups; k++)
                {
                    Sum[s, k] += other.Sum[s, k];
                    SumSquares[s, k] += other.SumSquares[s, k];
                }
            }

            for (int i = 0; i < Histogram!.Length; i++)
            {
                Histogram[i] += other.Histogram![i];
            }
        }

        /// <summary>The quantile at R's type 7 position (N - 1) p, located in the histogram and interpolated uniformly within its bin.</summary>
        public double Quantile(int s, int k, double p, long count)
        {
            double target = (count - 1) * p;
            long seen = 0;
            int offset = ((s * MaximumGroups) + k) * (Bins + 1);
            double width = Upper![s] / Bins;
            for (int b = 0; b <= Bins; b++)
            {
                long here = Histogram![offset + b];
                if (seen + here > target)
                {
                    return b == Bins ? double.PositiveInfinity : width * (b + ((target - seen + 0.5) / here));
                }

                seen += here;
            }

            return double.NaN;
        }
    }

    private static Accumulator Chunk(int n, long replications, ulong streamSeed, long unused, double[]? upper)
    {
        _ = unused;
        var rng = new Xoshiro(streamSeed);
        var accumulator = new Accumulator(upper);
        var x = new double[n];
        var y = new double[n];
        var hx = new double[n + 1];
        var hy = new double[n + 1];
        double correction = 1 / SpecialFunctions.C4((2.0 * n) - 1), df = 2.0 * (n - 1);
        Span<double> running = stackalloc double[Statistics];
        for (long r = 0; r < replications; r++)
        {
            running.Clear();
            for (int g = 0; g < MaximumGroups; g++)
            {
                double minX = double.PositiveInfinity, maxX = double.NegativeInfinity, minY = double.PositiveInfinity, maxY = double.NegativeInfinity, sx = 0, sy = 0, sxx = 0;
                for (int i = 0; i < n; i++)
                {
                    rng.NormalPair(out double a, out double b);
                    x[i] = a;
                    y[i] = b;
                    minX = Math.Min(minX, a);
                    maxX = Math.Max(maxX, a);
                    minY = Math.Min(minY, b);
                    maxY = Math.Max(maxY, b);
                    sx += a;
                    sy += b;
                    sxx += (a * a) + (b * b);
                }

                double w = maxX - minX, h = maxY - minY;
                double rss = sxx - (((sx * sx) + (sy * sy)) / n);
                running[0] += ExtremeSpread(x, y, n, hx, hy);
                running[1] += (w + h) / 2;
                running[2] += Math.Sqrt((w * w) + (h * h));
                running[3] += correction * Math.Sqrt(Math.Max(0, rss) / df);
                for (int s = 0; s < Statistics; s++)
                {
                    accumulator.Record(s, g, running[s] / (g + 1));
                }
            }
        }

        return accumulator;
    }

    /// <summary>
    /// The largest distance between two of the n points: every pair for small n, and above 24 shots every pair of the convex
    /// hull's vertices, since the farthest pair of a set is always a pair of its hull's vertices. The arrays are reordered.
    /// </summary>
    private static double ExtremeSpread(double[] x, double[] y, int n, double[] hx, double[] hy)
    {
        if (n <= 24)
        {
            double best = 0;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double dx = x[i] - x[j], dy = y[i] - y[j], d = (dx * dx) + (dy * dy);
                    best = d > best ? d : best;
                }
            }

            return Math.Sqrt(best);
        }

        Array.Sort(x, y, 0, n);
        int m = 0;
        for (int i = 0; i < n; i++)
        {
            while (m >= 2 && (((hx[m - 1] - hx[m - 2]) * (y[i] - hy[m - 2])) - ((hy[m - 1] - hy[m - 2]) * (x[i] - hx[m - 2]))) <= 0)
            {
                m--;
            }

            hx[m] = x[i];
            hy[m] = y[i];
            m++;
        }

        int lower = m + 1;
        for (int i = n - 2; i >= 0; i--)
        {
            while (m >= lower && (((hx[m - 1] - hx[m - 2]) * (y[i] - hy[m - 2])) - ((hy[m - 1] - hy[m - 2]) * (x[i] - hx[m - 2]))) <= 0)
            {
                m--;
            }

            hx[m] = x[i];
            hy[m] = y[i];
            m++;
        }

        m--;
        double farthest = 0;
        for (int i = 0; i < m; i++)
        {
            for (int j = i + 1; j < m; j++)
            {
                double dx = hx[i] - hx[j], dy = hy[i] - hy[j], d = (dx * dx) + (dy * dy);
                farthest = d > farthest ? d : farthest;
            }
        }

        return Math.Sqrt(farthest);
    }

    /// <summary>xoshiro256** with normal deviates in pairs by Marsaglia's polar method.</summary>
    private struct Xoshiro
    {
        private ulong s0, s1, s2, s3;

        public Xoshiro(ulong seed)
        {
            ulong state = seed;
            s0 = SplitMix(ref state);
            s1 = SplitMix(ref state);
            s2 = SplitMix(ref state);
            s3 = SplitMix(ref state);
        }

        private ulong Next()
        {
            ulong result = ulong.RotateLeft(s1 * 5, 7) * 9, t = s1 << 17;
            s2 ^= s0;
            s3 ^= s1;
            s1 ^= s2;
            s0 ^= s3;
            s2 ^= t;
            s3 = ulong.RotateLeft(s3, 45);
            return result;
        }

        public void NormalPair(out double a, out double b)
        {
            while (true)
            {
                double u = ((Next() >> 11) * (1.0 / (1UL << 53)) * 2) - 1, v = ((Next() >> 11) * (1.0 / (1UL << 53)) * 2) - 1, s = (u * u) + (v * v);
                if (s > 0 && s < 1)
                {
                    double f = Math.Sqrt(-2 * Math.Log(s) / s);
                    a = u * f;
                    b = v * f;
                    return;
                }
            }
        }
    }
}
