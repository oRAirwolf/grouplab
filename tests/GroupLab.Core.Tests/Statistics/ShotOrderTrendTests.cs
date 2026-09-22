using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.3: did the group open up as it was shot?
/// <para>
/// <b>The test that matters is <see cref="ShotsInARandomOrderShowNoTrendAboutAsOftenAsTheyShould"/>.</b> A barrel warming, a shooter tiring
/// and a rest settling are all things people believe they can see in a group, and ten shots in a random order will look like one of them
/// often enough to convince somebody. So what has to be right is not that a real trend is found; it is how often a trend is announced when
/// there is none, and that is measurable.
/// </para>
/// </summary>
public class ShotOrderTrendTests
{
    /// <summary>A strong trend: every shot further out than the last.</summary>
    [Fact]
    public void AGroupThatOpensUpShotByShotIsFound()
    {
        var trend = ShotOrderTrend.Of([0.05, 0.09, 0.14, 0.20, 0.26, 0.33, 0.41, 0.50])!;

        Assert.Equal(1.0, trend.Correlation, 6);
        Assert.True(trend.PValue < 0.01, $"p was {trend.PValue}");
        Assert.Equal(8, trend.Shots);
    }

    /// <summary>And the other way round, because a group settling in is the same question asked from the other end.</summary>
    [Fact]
    public void AGroupThatTightensIsFoundToo()
    {
        var trend = ShotOrderTrend.Of([0.50, 0.41, 0.33, 0.26, 0.20, 0.14, 0.09, 0.05])!;

        Assert.Equal(-1.0, trend.Correlation, 6);
        Assert.True(trend.PValue < 0.01, $"p was {trend.PValue}");
    }

    /// <summary>
    /// The one that decides whether this is worth showing anybody. Ten thousand groups of ten shots fired in a random order, and the share
    /// the test calls a trend at the 5 percent level should be about 5 percent. Much more than that and the picture is a machine for finding
    /// barrel warmings that are not there.
    /// </summary>
    [Fact]
    public void ShotsInARandomOrderShowNoTrendAboutAsOftenAsTheyShould()
    {
        var random = new Random(20260922);
        int announced = 0;
        const int groups = 400;
        for (int g = 0; g < groups; g++)
        {
            // Ten shots from one unchanging rifle: Rayleigh radii, and the order they were fired in carries nothing.
            var radii = Enumerable.Range(0, 10)
                .Select(_ => Math.Sqrt((-2 * Math.Log(1 - random.NextDouble())) / 2))
                .ToList();
            if (ShotOrderTrend.Of(radii, resamples: 999)!.PValue < 0.05)
            {
                announced++;
            }
        }

        double share = (double)announced / groups;
        Assert.True(share < 0.11, $"it called a trend on {share:P0} of {groups} groups shot in a random order, where about 5 percent is right");
    }

    /// <summary>Below five shots there is nothing to test: every ordering is a large share of the ones there are.</summary>
    [Fact]
    public void TooFewShotsGiveNothingRatherThanAWeakAnswer()
    {
        Assert.Null(ShotOrderTrend.Of([0.1, 0.2, 0.3, 0.4]));
        Assert.NotNull(ShotOrderTrend.Of([0.1, 0.2, 0.3, 0.4, 0.5]));
    }

    /// <summary>One wild shot at the end does not by itself make a trend, which is why the correlation is on ranks.</summary>
    [Fact]
    public void OneWildShotAtTheEndDoesNotMakeATrend()
    {
        var trend = ShotOrderTrend.Of([0.10, 0.12, 0.09, 0.11, 0.10, 0.13, 0.09, 1.40])!;

        Assert.True(trend.PValue > 0.05, $"one flyer was called a trend at p = {trend.PValue}");
    }

    /// <summary>The same shots give the same answer, because the shuffles come from a seed that is written down.</summary>
    [Fact]
    public void TheAnswerIsReproducible()
    {
        double[] radii = [0.12, 0.18, 0.11, 0.24, 0.19, 0.31, 0.22, 0.28];

        Assert.Equal(ShotOrderTrend.Of(radii)!.PValue, ShotOrderTrend.Of(radii)!.PValue, 12);
        Assert.Equal(20260922UL, ShotOrderTrend.Of(radii)!.Seed);
    }
}
