using GroupLab.Core.Ballistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 156 section 5: the simulation held to the one case with an exact answer, repeatable from its seed, never
/// helped by an extra error source, and different for an error drawn per string than for one of the same size drawn per shot, which is what
/// stops section 2's distinction being quietly undone. Section 8's definition of rifle precision is held here too.
/// </summary>
public class HitProbabilityTests
{
    private static readonly BallisticInput Rifle = new(0.3, DragModel.G7, 2800, 140, PressureInHg: 29.92);

    private static readonly Dictionary<HitSource, HitUncertainty> NoErrors = [];

    private static HitSetup Setup(double sigmaMrad, double diameterInches, IReadOnlyDictionary<HitSource, HitUncertainty>? errors = null, double? df = null, int shots = 1, ulong seed = HitProbability.DefaultSeed) =>
        new(Rifle, 1000, HitTarget.Circle(diameterInches), new HitPrecision(sigmaMrad, df, null), errors ?? NoErrors, shots, HitProbability.DefaultTrials, seed);

    /// <summary>Section 5 item 1: dispersion alone on a centered circle is 1 - exp(-R^2 / 2 sigma^2), to within the simulation's own error.</summary>
    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    public void DispersionAloneIsTheRayleighClosedForm(double radiusOverSigma)
    {
        const double sigmaMrad = 0.3;
        double sigmaInches = sigmaMrad * 36, radius = radiusOverSigma * sigmaInches;
        var answer = HitProbability.Work(Setup(sigmaMrad, 2 * radius));
        double exact = 1 - Math.Exp(-radius * radius / (2 * sigmaInches * sigmaInches));
        double se = Math.Sqrt(exact * (1 - exact) / HitProbability.DefaultTrials);
        Assert.InRange(answer.FirstRound.Value, exact - (4 * se) - 1e-9, exact + (4 * se) + 1e-9);
        Assert.Null(answer.Refusal);
    }

    /// <summary>
    /// Entry 156 section 7: the second round is dialed off the first shot's whole miss, so it carries its own dispersion and the first's, and
    /// its closed form is the Rayleigh one at sigma times the square root of two.
    /// </summary>
    [Fact]
    public void TheSecondRoundCarriesTheFirstShotsDispersion()
    {
        const double sigmaMrad = 0.3;
        double sigmaInches = sigmaMrad * 36, radius = 1.2 * sigmaInches;
        var answer = HitProbability.Work(Setup(sigmaMrad, 2 * radius));
        double exact = 1 - Math.Exp(-radius * radius / (4 * sigmaInches * sigmaInches));
        double se = Math.Sqrt(exact * (1 - exact) / HitProbability.DefaultTrials);
        Assert.InRange(answer.SecondRound.Value, exact - (4 * se), exact + (4 * se));
        Assert.True(answer.SecondRound.Value < answer.FirstRound.Value, "with no string error to take out, correcting from the first shot only adds its dispersion");

        // With a wind call error the second round is the better one, because the correction takes the string's error out.
        var windy = HitProbability.Work(Setup(sigmaMrad, 2 * radius, new Dictionary<HitSource, HitUncertainty> { [HitSource.Wind] = new(3) }));
        Assert.True(windy.SecondRound.Value > windy.FirstRound.Value);
        Assert.InRange(windy.SecondRound.Value, exact - (4 * se), exact + (4 * se));
    }

    /// <summary>Section 5 item 2: the seed makes a run repeatable, and another seed is another run.</summary>
    [Fact]
    public void TheSeedMakesARunRepeatable()
    {
        var errors = new Dictionary<HitSource, HitUncertainty> { [HitSource.Wind] = new(2), [HitSource.Range] = new(5), [HitSource.Velocity] = new(10) };
        var a = HitProbability.Work(Setup(0.25, 12, errors, df: 18, shots: 3));
        var b = HitProbability.Work(Setup(0.25, 12, errors, df: 18, shots: 3));
        Assert.Equal(a.FirstRound, b.FirstRound);
        Assert.Equal(a.SecondRound, b.SecondRound);
        Assert.Equal(a.AtLeastOne, b.AtLeastOne);
        Assert.Equal(a.FirstScatter, b.FirstScatter);
        Assert.Equal(a.Costs, b.Costs);
        var c = HitProbability.Work(Setup(0.25, 12, errors, df: 18, shots: 3, seed: 7));
        Assert.NotEqual(a.FirstScatter, c.FirstScatter);
    }

    /// <summary>
    /// Section 5 item 3: adding an error source never increases the hit probability. Each run shares its random numbers with the one before,
    /// so the only tolerance is the simulation's own error, two standard errors.
    /// </summary>
    [Fact]
    public void AddingAnErrorSourceNeverHelps()
    {
        var steps = new (HitSource Source, HitUncertainty Error)[]
        {
            (HitSource.Velocity, new(12)),
            (HitSource.Wind, new(2)),
            (HitSource.Range, new(4)),
            (HitSource.Zero, new(0.05)),
            (HitSource.Drag, new(2)),
            (HitSource.Temperature, new(8)),
            (HitSource.Pressure, new(0.2)),
            (HitSource.Humidity, new(20)),
            (HitSource.Inclination, new(2)),
        };
        var errors = new Dictionary<HitSource, HitUncertainty>();
        double before = HitProbability.Work(Setup(0.2, 10, errors)).FirstRound.Value;
        foreach (var (source, error) in steps)
        {
            double alone = HitProbability.Work(Setup(0.2, 10, new Dictionary<HitSource, HitUncertainty> { [source] = error })).FirstRound.Value;
            double baseline = HitProbability.Work(Setup(0.2, 10)).FirstRound.Value;
            Assert.True(alone <= baseline + Tolerance(baseline), $"{source} alone raised the chance from {baseline:0.0000} to {alone:0.0000}");

            errors[source] = error;
            double after = HitProbability.Work(Setup(0.2, 10, errors)).FirstRound.Value;
            Assert.True(after <= before + Tolerance(before), $"adding {source} raised the chance from {before:0.0000} to {after:0.0000}");
            before = after;
        }

        Assert.True(before < 0.9 * HitProbability.Work(Setup(0.2, 10)).FirstRound.Value, "the errors together cost something");
        static double Tolerance(double p) => 2 * Math.Sqrt(p * (1 - p) / HitProbability.DefaultTrials);
    }

    /// <summary>
    /// Section 5 item 4: a per-string error and a per-shot error of the same size give the same first shot and different strings. The zero's
    /// error is drawn per string; the same size added to the dispersion in quadrature is drawn per shot. A string shares its zero error, so it
    /// misses together, and the chance of at least one hit in five is lower.
    /// </summary>
    [Fact]
    public void AnErrorDrawnPerStringIsNotOneDrawnPerShot()
    {
        const double sigma = 0.1, zero = 0.4;
        var perString = HitProbability.Work(Setup(sigma, 12, new Dictionary<HitSource, HitUncertainty> { [HitSource.Zero] = new(zero) }, shots: 5));
        var perShot = HitProbability.Work(Setup(Math.Sqrt((sigma * sigma) + (zero * zero)), 12, shots: 5));
        double se = 1.96 * Math.Sqrt(2 * 0.25 / HitProbability.DefaultTrials);
        Assert.InRange(perString.FirstRound.Value - perShot.FirstRound.Value, -2 * se, 2 * se);
        Assert.True(perShot.AtLeastOne.Value - perString.AtLeastOne.Value > 5 * se,
            $"at least one in five: {perString.AtLeastOne.Value:0.000} drawn per string, {perShot.AtLeastOne.Value:0.000} per shot");
        Assert.Equal(perString.HitShare.Value, perShot.HitShare.Value, 1);
    }

    /// <summary>Section 3 item 1 and section 2: the interval holds the sigma's own uncertainty, which from nine shots dominates the simulation's.</summary>
    [Fact]
    public void TheIntervalHoldsTheUncertaintyInSigma()
    {
        var nine = HitProbability.Work(Setup(0.2, 16, df: 16));
        var ninety = HitProbability.Work(Setup(0.2, 16, df: 178));
        var typed = HitProbability.Work(Setup(0.2, 16));
        Assert.True(nine.FirstRound.SigmaDominates);
        Assert.True(nine.FirstRound.Upper - nine.FirstRound.Lower > 2 * (ninety.FirstRound.Upper - ninety.FirstRound.Lower));
        Assert.Equal(0, typed.FirstRound.SigmaHalfWidth);
        Assert.False(typed.FirstRound.SigmaDominates);
        Assert.Contains(typed.NotIncluded, s => s.Contains("typed", StringComparison.Ordinal));
        Assert.InRange(nine.FirstRound.Value, nine.FirstRound.Lower, nine.FirstRound.Upper);
    }

    /// <summary>Section 4 item 4: a group too small to say anything is refused, with the shots it would take.</summary>
    [Fact]
    public void AGroupTooSmallToSayAnythingIsRefused()
    {
        // Five shots: sigma's interval runs from about 0.68 to 1.9 times the estimate, which on a target near half moves the answer across most of the scale.
        var five = HitProbability.Work(Setup(0.25, 21, df: 8));
        Assert.NotNull(five.Refusal);
        Assert.StartsWith("The group behind this precision is too small to say", five.Refusal, StringComparison.Ordinal);
        Assert.True(five.ShotsNeeded is > 5 and < HitProbability.MostShotsNamed, $"shots needed {five.ShotsNeeded}");
        Assert.Contains($"About {five.ShotsNeeded} shots in one group", five.Refusal, StringComparison.Ordinal);
        var enough = HitProbability.Work(Setup(0.25, 21, df: 2.0 * five.ShotsNeeded!.Value));
        Assert.Null(enough.Refusal);
    }

    /// <summary>Section 3 item 5: the sensitivity list names what costs the most, and a large wind call error costs more than a small range error.</summary>
    [Fact]
    public void TheCostsAreRankedByWhatTheyTakeAway()
    {
        var answer = HitProbability.Work(Setup(0.15, 12, new Dictionary<HitSource, HitUncertainty> { [HitSource.Wind] = new(3), [HitSource.Range] = new(1), [HitSource.Velocity] = new(8) }));
        Assert.Equal(HitSource.Wind, answer.Costs[0].Source);
        Assert.False(answer.Costs[0].PerShot);
        Assert.Contains(answer.Costs, c => c.Source == HitSource.Velocity && c.PerShot);
        Assert.Equal(answer.Costs.OrderByDescending(c => c.Cost).Select(c => c.Source), answer.Costs.Select(c => c.Source));
        var wind = answer.Costs[0];
        Assert.True(wind.AcrossInches > 5 * wind.UpDownInches, "a crosswind's error is across");
        Assert.Contains(answer.NotIncluded, s => s.Contains("latitude", StringComparison.Ordinal));
    }

    /// <summary>Entry 156 section 7 item 1: a bias moves the group off the aim, which a standard deviation of zero cannot.</summary>
    [Fact]
    public void ABiasIsNotAStandardDeviation()
    {
        var fast = HitProbability.Work(Setup(0.1, 8, new Dictionary<HitSource, HitUncertainty> { [HitSource.Velocity] = new(0, -40) }));
        var exact = HitProbability.Work(Setup(0.1, 8));
        Assert.True(fast.FirstRound.Value < exact.FirstRound.Value - 0.2);
        Assert.True(fast.FirstScatter.Average(p => p.Y) < -4, "a chronograph reading fast puts the shots low");
    }

    /// <summary>Section 2's velocity share: a velocity SD larger than the group measured allows is refused, as the projection refuses it.</summary>
    [Fact]
    public void AVelocitySpreadLargerThanTheGroupIsRefused()
    {
        var setup = new HitSetup(Rifle, 1000, HitTarget.Circle(12), new HitPrecision(0.05, 18, 600), new Dictionary<HitSource, HitUncertainty> { [HitSource.Velocity] = new(60) });
        var answer = HitProbability.Work(setup);
        Assert.Contains("too large for this group", answer.Refusal, StringComparison.Ordinal);
    }

    /// <summary>
    /// Section 8: rifle precision is the per-axis standard deviation as an angle, which for circular dispersion is the Rayleigh sigma, and the
    /// conversions are written down here. One inch at 100 yd is 1 / 3600 of a radian; one MOA is 0.290888 mrad.
    /// </summary>
    [Fact]
    public void RiflePrecisionIsThePerAxisSigmaInMrad()
    {
        Assert.Equal(1000.0 / 3600, HitPrecision.MradFromInches(1, 100), 12);
        Assert.Equal(0.290888, HitPrecision.MradFromMoa(1), 12);
        Assert.Equal(HitPrecision.MradFromInches(0.5, 100), HitPrecision.MradFromInches(5, 1000), 12);
    }

    /// <summary>Section 4 item 3: no more than two significant figures, and never a digit the trial count does not support.</summary>
    [Theory]
    [InlineData(0.7342, 0.009, "73")]
    [InlineData(0.07342, 0.005, "7.3")]
    [InlineData(0.07342, 0.02, "7")]
    [InlineData(0.004231, 0.0013, "0.4")]
    [InlineData(0.9981, 0.001, "more than 99")]
    [InlineData(0.0, 0.0001, "under 0.01")]
    public void AProbabilityIsNeverPrintedFinerThanItIsKnown(double p, double half, string expected) =>
        Assert.Equal(expected, HitProbability.Percent(p, half));

    /// <summary>An interval's ends are at the value's decimals and rounded outward, so it never reads narrower than it is.</summary>
    [Fact]
    public void AnIntervalIsRoundedOutward()
    {
        Assert.Equal(("more than 99", "99", "100"), HitProbability.Percents(new HitChance(0.998, 0.9975, 1.0, 0, 0.001)));
        Assert.Equal(("63", "62", "65"), HitProbability.Percents(new HitChance(0.63, 0.621, 0.641, 0.001, 0.009)));
        Assert.Equal(("7.3", "6.9", "7.8"), HitProbability.Percents(new HitChance(0.0734, 0.0699, 0.0771, 0.001, 0.005)));
    }

    /// <summary>The IPSC outline: the body with its corners cut, the head above it, and nothing beside the head.</summary>
    [Fact]
    public void TheIpscOutlineIsABodyAndAHead()
    {
        var ipsc = new HitTarget(HitShape.Ipsc, 18, 30);
        Assert.True(ipsc.Contains(0, 0));
        Assert.True(ipsc.Contains(8.9, 0));
        Assert.False(ipsc.Contains(8.9, 11.9), "the body's corner is cut");
        Assert.True(ipsc.Contains(0, 14));
        Assert.False(ipsc.Contains(6, 14), "nothing beside the head");
        Assert.False(ipsc.Contains(0, 18.1));
        Assert.False(ipsc.Contains(0, -12.1));
    }

    /// <summary>Section 3 item 4: the curve falls with distance and its band holds it.</summary>
    [Fact]
    public void TheCurveFallsWithDistance()
    {
        var curve = HitProbability.Curve(Setup(0.2, 10, new Dictionary<HitSource, HitUncertainty> { [HitSource.Wind] = new(2) }, df: 18), [300, 600, 900, 1200]);
        Assert.Equal(4, curve.Count);
        Assert.True(curve.Zip(curve.Skip(1)).All(p => p.First.Value > p.Second.Value));
        Assert.All(curve, p => Assert.InRange(p.Value, p.Lower, p.Upper));
    }
}
