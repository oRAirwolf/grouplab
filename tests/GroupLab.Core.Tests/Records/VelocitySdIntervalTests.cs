using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.5: an SD reported without the range that many shots pins it to.
/// <para>
/// <b>The test that matters is <see cref="TheIntervalCoversTheTrueSdAboutAsOftenAsItClaims"/>.</b> An interval is only worth printing if it
/// covers the truth as often as it says it does, and that is measurable: draw strings from a rifle whose SD is known, and count.
/// </para>
/// </summary>
public class VelocitySdIntervalTests
{
    /// <summary>Ten shots pin an SD down very loosely, and the numbers say how loosely.</summary>
    [Fact]
    public void TenShotsPinTheSdDownOnlyToAboutTwoThirdsToTwice()
    {
        var (lower, upper) = Chronograph.SdInterval(10, 10.0)!.Value;

        Assert.Equal(6.88, lower, 2);
        Assert.Equal(18.26, upper, 2);
    }

    /// <summary>And thirty shots, which almost nobody fires for this, are still not tight.</summary>
    [Fact]
    public void ThirtyShotsAreStillNotTight()
    {
        var (lower, upper) = Chronograph.SdInterval(30, 10.0)!.Value;

        Assert.True(lower is > 7.9 and < 8.1, $"lower was {lower}");
        Assert.True(upper is > 13.3 and < 13.5, $"upper was {upper}");
    }

    /// <summary>The interval always contains the measurement it was built from, at every size.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(100)]
    public void TheMeasuredSdIsAlwaysInsideItsOwnInterval(int readings)
    {
        var (lower, upper) = Chronograph.SdInterval(readings, 12.5)!.Value;

        Assert.True(lower < 12.5 && 12.5 < upper, $"{readings} readings gave {lower} to {upper}");
    }

    /// <summary>More shots never widen it, which is the whole reason to fire more.</summary>
    [Fact]
    public void MoreShotsNarrowIt()
    {
        double previous = double.MaxValue;
        foreach (int readings in new[] { 3, 5, 10, 20, 50 })
        {
            var (lower, upper) = Chronograph.SdInterval(readings, 10.0)!.Value;
            double width = upper - lower;
            Assert.True(width < previous, $"{readings} readings were no narrower than the count before it");
            previous = width;
        }
    }

    /// <summary>One reading has no SD, so it has no interval either, and that is said with a null rather than a wide range.</summary>
    [Fact]
    public void FewerThanTwoReadingsGiveNothing()
    {
        Assert.Null(Chronograph.SdInterval(1, 10.0));
        Assert.Null(Chronograph.SdInterval(0, 10.0));
    }

    /// <summary>
    /// The measurement that decides whether the interval is honest. Two thousand strings of ten shots from a rifle whose velocity SD is
    /// truly 12 ft/s, and the share of intervals covering 12 should be about 95 percent. Much less and the interval is a decoration.
    /// </summary>
    [Fact]
    public void TheIntervalCoversTheTrueSdAboutAsOftenAsItClaims()
    {
        const double trueSd = 12.0;
        const int strings = 2000, shots = 10;
        var random = new Random(20260922);
        int covered = 0;
        for (int s = 0; s < strings; s++)
        {
            var readings = new List<double>(shots);
            for (int i = 0; i < shots; i++)
            {
                // Box-Muller, so the velocities are normal, which is what the chi-squared interval assumes.
                double u1 = 1 - random.NextDouble(), u2 = random.NextDouble();
                readings.Add(2700 + (trueSd * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2)));
            }

            var spread = Chronograph.Spread(readings)!;
            var (lower, upper) = Chronograph.SdInterval(spread.Readings, spread.SdFps)!.Value;
            if (lower <= trueSd && trueSd <= upper)
            {
                covered++;
            }
        }

        double share = (double)covered / strings;
        Assert.True(share is > 0.93 and < 0.97, $"it covered the true SD on {share:P1} of {strings} strings, where 95 percent is claimed");
    }

    /// <summary>The extreme spread is the two ends and nothing else, and one reading has none.</summary>
    [Fact]
    public void TheExtremeSpreadIsTheTwoEnds()
    {
        Assert.Equal(34, Chronograph.ExtremeSpreadFps([2705, 2711, 2698, 2732, 2700])!.Value, 6);
        Assert.Null(Chronograph.ExtremeSpreadFps([2705]));
    }
}
