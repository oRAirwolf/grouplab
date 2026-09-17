using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 76 section 1: the error ellipse's aspect ratio from a circular group, the reference the report prints beside
/// the measured aspect.
/// </summary>
public class CircularAspectTests
{
    /// <summary>Entry 76 section 1's simulated quantiles, reproduced within the rounding of the printed digits, the 99th percentile within its simulation noise.</summary>
    [Theory]
    [InlineData(10, 1.53, 1.83, 2.21, 2.51, 3.25)]
    [InlineData(12, 1.46, 1.71, 2.02, 2.25, 2.80)]
    public void Entry76QuantilesAreReproduced(int n, double median, double p75, double p90, double p95, double p99)
    {
        Assert.Equal(median, CircularAspect.Median(n), 0.006);
        Assert.Equal(p75, CircularAspect.Quantile(n, 0.25), 0.006);
        Assert.Equal(p90, CircularAspect.Quantile(n, 0.10), 0.006);
        Assert.Equal(p95, CircularAspect.Quantile(n, 0.05), 0.006);
        Assert.Equal(p99, CircularAspect.Quantile(n, 0.01), 0.02);
    }

    /// <summary>Entry 76's worked case: Alan's scan, ten shots at aspect 2.818, which circular groups exceed about one time in forty.</summary>
    [Fact]
    public void TheScansAspectIsExceededOneTimeInForty()
    {
        Assert.Equal(0.025, CircularAspect.ProbabilityAbove(10, 2.818), 3);
        Assert.Equal(1, CircularAspect.ProbabilityAbove(10, 1));
        Assert.Equal(0.5, CircularAspect.ProbabilityAbove(10, CircularAspect.Median(10)), 9);
    }

    /// <summary>A seeded simulation agrees with the closed form, including at three shots where the density's shape changes.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(25)]
    public void ASeededSimulationAgrees(int n)
    {
        var random = new StatisticsRandom(76);
        const int replications = 40000;
        double median = CircularAspect.Median(n), tail = CircularAspect.Quantile(n, 0.05);
        int aboveMedian = 0, aboveTail = 0;
        var shots = new PointD[n];
        for (int r = 0; r < replications; r++)
        {
            for (int i = 0; i < n; i++)
            {
                shots[i] = new PointD(random.NextNormal(), random.NextNormal());
            }

            var (xx, xy, yy) = GroupStatistics.Covariance(shots);
            double aspect = GroupStatistics.Shape(xx, xy, yy).AspectRatio;
            aboveMedian += aspect > median ? 1 : 0;
            aboveTail += aspect > tail ? 1 : 0;
        }

        Assert.Equal(0.5, (double)aboveMedian / replications, 1.5e-2);
        Assert.Equal(0.05, (double)aboveTail / replications, 6e-3);
    }
}
