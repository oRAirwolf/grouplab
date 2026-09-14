using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab stats coverage</c>: the bootstrap row of docs/STATISTICS.md section 15.3, "not compared numerically; compare coverage
/// over 1000 simulated datasets". Bitwise agreement between two bootstraps is meaningless, so what is measured is whether GroupLab's
/// BCa interval covers a known truth as often as it says.
/// <para>
/// The statistic is the elliptical CEP at 50 percent by the Grubbs-Patnaik approximation of section 4, a plug-in of the sample
/// covariance, whose population value is the same approximation applied to the true covariance: so the truth is known exactly, the
/// statistic is skewed and biased at small n as section 6 says bootstrapped statistics are, and each evaluation is cheap enough for
/// 1000 datasets of 9999 resamples. The truth is an elliptical normal with standard deviations 1 and 2 and correlation 0.3.
/// </para>
/// </summary>
public static class StatsCoverage
{
    public static int Run(TextWriter output, int datasets = 1000, int resamples = Bootstrap.DefaultResamples)
    {
        ArgumentNullException.ThrowIfNull(output);
        const double sx = 1, sy = 2, rho = 0.3;
        double cxx = sx * sx, cxy = rho * sx * sy, cyy = sy * sy;
        double truth = GroupStatistics.CepGrubbsPatnaik(cxx, cxy, cyy, 0.5);
        static double Cep(IReadOnlyList<PointD> shots)
        {
            var (xx, xy, yy) = GroupStatistics.Covariance(shots);
            return GroupStatistics.CepGrubbsPatnaik(xx, xy, yy, 0.5);
        }

        var clock = System.Diagnostics.Stopwatch.StartNew();
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"BCa coverage of the Grubbs-Patnaik CEP(0.5), truth {truth:0.00000}, {datasets} datasets of {resamples} resamples each"));
        output.WriteLine();
        output.WriteLine("| Shots | Datasets | Covered | Coverage | Binomial 95% band | Below truth / above | Percentile fallbacks | Flagged unreliable |");
        output.WriteLine("|---|---|---|---|---|---|---|---|");
        foreach (int n in (int[])[10, 25, 50])
        {
            int covered = 0, missedLow = 0, missedHigh = 0, fallbacks = 0, unreliable = 0;
            object gate = new();
            Parallel.For(0, datasets, d =>
            {
                ulong seed = (ulong)((n * 1_000_003) + d);
                var random = new Random((int)(seed & 0x7FFFFFFF));
                var shots = new PointD[n];
                for (int i = 0; i < n; i++)
                {
                    double z1 = Gaussian(random), z2 = Gaussian(random);
                    shots[i] = new PointD(sx * z1, sy * ((rho * z1) + (Math.Sqrt(1 - (rho * rho)) * z2)));
                }

                var interval = Bootstrap.Interval(shots, Cep, resamples: resamples, seed: seed);
                lock (gate)
                {
                    covered += interval.Lower <= truth && truth <= interval.Upper ? 1 : 0;
                    missedLow += interval.Upper < truth ? 1 : 0;
                    missedHigh += interval.Lower > truth ? 1 : 0;
                    fallbacks += interval.Method == "BCa" ? 0 : 1;
                    unreliable += interval.Reliable ? 0 : 1;
                }
            });

            double p = (double)covered / datasets, band = 1.96 * Math.Sqrt(0.95 * 0.05 / datasets);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"| {n} | {datasets} | {covered} | {100 * p:0.0}% | {100 * (0.95 - band):0.0} to {100 * (0.95 + band):0.0}% | {missedLow} / {missedHigh} | {fallbacks} | {unreliable} |"));
            output.Flush();
        }

        output.WriteLine();
        output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"done in {clock.Elapsed.TotalMinutes:0.0} minutes"));
        return 0;
    }

    /// <summary>
    /// The datasets are drawn with the runtime's generator seeded per dataset, deliberately a different generator from the engine's
    /// resampling one, so the truth's draws and the bootstrap's draws cannot share a stream.
    /// </summary>
    private static double Gaussian(Random random)
    {
        double u = 1 - random.NextDouble(), v = random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }
}
