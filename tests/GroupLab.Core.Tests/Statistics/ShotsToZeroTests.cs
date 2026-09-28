using System.Diagnostics;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 252 sections 3 and 4, "Shots Needed to Zero", suggested by Jylee: the closed form against a numerical
/// integration of the same procedure and against the procedure simulated shot by shot, the planning session's own figures, and the shape
/// of the answer: more shots never lower the chance, a wider rifle needs more, and an uncertain sigma needs more than a known one.
/// </summary>
public class ShotsToZeroTests
{
    /// <summary>One axis, by the midpoint rule over where the true zero lies within its click: the procedure's own integral, done slowly.</summary>
    private static double Integrated(double sigma, int shots, double h)
    {
        const int Steps = 20_000;
        double t = sigma / Math.Sqrt(shots), sum = 0;
        for (int i = 0; i < Steps; i++)
        {
            double u = -0.5 + ((i + 0.5) / Steps);
            sum += Distributions.NormalCdf((h - u) / t) - Distributions.NormalCdf((-h - u) / t);
        }

        return sum / Steps;
    }

    [Theory]
    [InlineData(0.3, 1)]
    [InlineData(1.0, 5)]
    [InlineData(1.0, 40)]
    [InlineData(2.5, 10)]
    [InlineData(4.0, 200)]
    public void OneAxisIsItsIntegral(double sigma, int shots)
    {
        Assert.Equal(Integrated(sigma, shots, 0.5), ShotsToZero.Axis(sigma, shots, 0.5), 1e-6);
        Assert.Equal(Integrated(sigma, shots, 1.5), ShotsToZero.Axis(sigma, shots, 1.5), 1e-6);
    }

    /// <summary>The procedure itself, shot by shot, both axes: zero anywhere in a click, fire, centre, round, and see where it landed.</summary>
    [Fact]
    public void TheProcedureSimulatedShotByShotAgrees()
    {
        var random = new Random(252);
        const double Sigma = 1.0;
        const int Shots = 10, Trials = 200_000;
        int closest = 0, within = 0;
        for (int k = 0; k < Trials; k++)
        {
            bool both = true, bothWithin = true;
            for (int axis = 0; axis < 2; axis++)
            {
                double zero = random.NextDouble() - 0.5, centre = 0;
                for (int s = 0; s < Shots; s++)
                {
                    centre += zero + (Sigma * Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble()));
                }

                int dialled = (int)Math.Round(centre / Shots, MidpointRounding.AwayFromZero);
                both &= dialled == 0;
                bothWithin &= Math.Abs(dialled) <= 1;
            }

            closest += both ? 1 : 0;
            within += bothWithin ? 1 : 0;
        }

        Assert.Equal(Math.Pow(ShotsToZero.Axis(Sigma, Shots, 0.5), 2), closest / (double)Trials, 0.005);
        Assert.Equal(Math.Pow(ShotsToZero.Axis(Sigma, Shots, 1.5), 2), within / (double)Trials, 0.005);
    }

    /// <summary>The planning session's check of the same procedure, 200,000 trials, sigma of 1 click an axis.</summary>
    [Theory]
    [InlineData(5, 0.42)]
    [InlineData(10, 0.56)]
    [InlineData(20, 0.68)]
    [InlineData(40, 0.76)]
    public void ItAgreesWithThePlanningSessionsCheck(int shots, double closest)
    {
        Assert.Equal(closest, Math.Pow(ShotsToZero.Axis(1.0, shots, 0.5), 2), 0.01);
        Assert.True(Math.Pow(ShotsToZero.Axis(1.0, 5, 1.5), 2) >= 0.99);
    }

    [Fact]
    public void MoreShotsNeverLowerTheChance()
    {
        foreach (bool exact in new[] { true, false })
        {
            var answer = ShotsToZero.Work(1.2, 9, exact, seed: 41);
            for (int i = 1; i < answer.Curve.Count; i++)
            {
                Assert.True(answer.Curve[i].ClosestClick >= answer.Curve[i - 1].ClosestClick - 1e-12);
                Assert.True(answer.Curve[i].WithinOneClick >= answer.Curve[i - 1].WithinOneClick - 1e-12);
            }
        }
    }

    [Fact]
    public void AWiderRifleNeedsMoreShots()
    {
        var narrow = ShotsToZero.Work(0.5, null, true, 41);
        var wide = ShotsToZero.Work(1.5, null, true, 41);
        Assert.True(wide.WithinOneClick.Ninety > narrow.WithinOneClick.Ninety);
        Assert.True((wide.ClosestClick.Ninety ?? int.MaxValue) > (narrow.ClosestClick.Ninety ?? int.MaxValue) || narrow.ClosestClick.Ninety is null);
    }

    /// <summary>A sigma from few shots may be larger than it looks, so it asks for more shots than the same sigma known exactly.</summary>
    [Fact]
    public void AnUncertainSigmaNeedsMoreThanAKnownOne()
    {
        var known = ShotsToZero.Work(0.8, 8, exact: true, seed: 41);
        var uncertain = ShotsToZero.Work(0.8, 8, exact: false, seed: 41);
        Assert.True(uncertain.WithinOneClick.NinetyNine >= known.WithinOneClick.NinetyNine);
        Assert.True(uncertain.WithinOneClick.NinetyNine > known.WithinOneClick.NinetyNine || uncertain.WithinOneClick.NinetyFive > known.WithinOneClick.NinetyFive);
        Assert.Equal(0, known.Trials);
        Assert.Equal(ShotsToZero.DefaultTrials, uncertain.Trials);
    }

    [Fact]
    public void TheSameSeedGivesTheSameAnswer()
    {
        var a = ShotsToZero.Work(1.0, 18, false, 7);
        var b = ShotsToZero.Work(1.0, 18, false, 7);
        Assert.Equal(a.ClosestClick, b.ClosestClick);
        Assert.Equal(a.WithinOneClick, b.WithinOneClick);
        Assert.Equal(a.Curve, b.Curve);
    }

    /// <summary>The closest click converges slowly: at 1 click of sigma the 99 percent needs more than a thousand shots.</summary>
    [Fact]
    public void AGroupNearTheBoundaryTakesVeryManyShots()
    {
        var answer = ShotsToZero.Work(1.0, null, true, 41);
        Assert.Null(answer.ClosestClick.NinetyNine);
        Assert.NotNull(answer.WithinOneClick.NinetyNine);
    }

    /// <summary>
    /// Section 4's budget, held: the numbers appear within half a second on the desktop. A CI runner is slower than Alan's machine, so the
    /// test allows four times that; the measured times are in docs/PHASE1-RESULTS.md.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(100)]
    public void ItIsWorkedOutWithinTheBudget(int groupShots)
    {
        ShotsToZero.Work(1.0, 2 * groupShots - 2, false, 1);
        var clock = Stopwatch.StartNew();
        foreach (double sigma in new[] { 0.3, 1.0, 3.0 })
        {
            ShotsToZero.Work(sigma, (2 * groupShots) - 2, false, 41);
        }

        if (Environment.GetEnvironmentVariable("GROUPLAB_SHOTS_TIMES") is { Length: > 0 } times)
        {
            File.AppendAllText(times, $"{groupShots} shots: {clock.Elapsed.TotalMilliseconds / 3:0} ms a calculation{Environment.NewLine}");
        }

        Assert.True(clock.Elapsed.TotalSeconds / 3 < 2.0, $"{clock.Elapsed.TotalMilliseconds / 3:0} ms a calculation for a {groupShots} shot group");
    }
}
