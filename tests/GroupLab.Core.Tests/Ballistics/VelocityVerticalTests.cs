using GroupLab.Core.Ballistics;

namespace GroupLab.Core.Tests.Ballistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 322 section 3: velocity's predicted share of the vertical against the vertical measured, and the regression of
/// vertical on velocity, recovered from synthetic groups made with a known velocity SD, a known other vertical noise and a known slope.
/// </summary>
public sealed class VelocityVerticalTests
{
    private static readonly BallisticInput Load = new(0.243, DragModel.G7, 2700, 175);

    private const double Yards = 600;

    /// <summary>A group shot with velocity SD <paramref name="sdV"/> and other vertical noise <paramref name="sdOther"/>, each shot moved by <paramref name="slope"/> per ft/s.</summary>
    private static (double[] Velocities, double[] Vertical) Group(int seed, int n, double sdV, double sdOther, double slope)
    {
        var random = new Random(seed);
        double Normal() => Math.Sqrt(-2 * Math.Log(1 - random.NextDouble())) * Math.Cos(2 * Math.PI * random.NextDouble());
        var velocities = new double[n];
        var vertical = new double[n];
        for (int i = 0; i < n; i++)
        {
            velocities[i] = 2700 + sdV * Normal();
            vertical[i] = slope * (velocities[i] - 2700) + sdOther * Normal();
        }

        return (velocities, vertical);
    }

    [Fact]
    public void TheShareOfASyntheticGroupIsRecoveredWithinItsIntervalAtAboutItsCoverage()
    {
        double k = Projection.DropPerFps(Load, Yards);
        Assert.InRange(k, 0.01, 0.2);
        const double sdV = 15, sdOther = 1.0;
        double truth = k * k * sdV * sdV / (k * k * sdV * sdV + sdOther * sdOther);

        int covered = 0;
        const int runs = 200;
        for (int seed = 0; seed < runs; seed++)
        {
            var (velocities, vertical) = Group(seed, 20, sdV, sdOther, k);
            var (result, refusal) = VelocityVertical.Analyze(Load, Yards, velocities, vertical);
            Assert.Null(refusal);
            Assert.NotNull(result);
            if (truth >= result.Share.Lower && truth <= result.Share.Upper)
            {
                covered++;
            }
        }

        // Nominal 90%; the readings come from the same shots, which makes the interval err wide, so coverage at or above nominal is expected.
        Assert.InRange(covered / (double)runs, 0.85, 1.0);

        var (one, _) = VelocityVertical.Analyze(Load, Yards, Group(1, 30, sdV, sdOther, k).Velocities, Group(1, 30, sdV, sdOther, k).Vertical);
        Assert.NotNull(one);
        Assert.InRange(one.Share.Value, one.Share.Lower, one.Share.Upper);
        Assert.Equal(Math.Abs(one.DropPerFpsInches) * one.VelocitySdFps.Value, one.PredictedVerticalSdInches.Value, 9);
        Assert.Equal(Math.Abs(one.DropPerFpsInches) * one.VelocitySdFps.Lower, one.PredictedVerticalSdInches.Lower, 9);
        Assert.InRange(one.MeasuredVerticalSdInches.Value, one.MeasuredVerticalSdInches.Lower, one.MeasuredVerticalSdInches.Upper);
        Assert.StartsWith("Velocity alone accounts for about ", one.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void TheShareSentenceSaysTheDataCannotTellWhereTheIntervalReachesAllOfTheVertical()
    {
        Assert.Equal(
            "Velocity alone accounts for about 40% of the vertical, 15% to 80% at 90%.",
            VelocityVertical.ShareSentence(new IntervalFigure(0.40, 0.15, 0.80), 0.90));
        Assert.Contains("the data cannot tell whether anything besides velocity", VelocityVertical.ShareSentence(new IntervalFigure(0.6, 0.2, 1.7), 0.90), StringComparison.Ordinal);
        Assert.Contains("more vertical than the group shows", VelocityVertical.ShareSentence(new IntervalFigure(1.3, 0.5, 3.0), 0.90), StringComparison.Ordinal);

        // Five readings and five shots cannot pin a share down: with half the vertical from velocity, the interval reaches 100%.
        double k = Projection.DropPerFps(Load, Yards);
        var (velocities, vertical) = Group(7, 5, 15, k * 15, k);
        var (result, _) = VelocityVertical.Analyze(Load, Yards, velocities, vertical);
        Assert.NotNull(result);
        Assert.True(result.CannotTell);
        Assert.Contains("cannot tell", result.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRegressionRecoversAKnownSlopeAndSaysWhetherTheSolverAgrees()
    {
        double k = Projection.DropPerFps(Load, Yards);
        var (velocities, vertical) = Group(11, 40, 15, 0.5, k);
        var (result, _) = VelocityVertical.Analyze(Load, Yards, velocities, vertical, [.. velocities.Zip(vertical)]);
        Assert.NotNull(result?.Slope);
        Assert.Equal(40, result.Slope.Pairs);
        Assert.InRange(k, result.Slope.SlopeInchesPerFps.Lower, result.Slope.SlopeInchesPerFps.Upper);
        Assert.True(result.Slope.PredictedInside);
        Assert.False(result.Slope.IncludesNone);
        Assert.Contains("as the solver says it should", result.Slope.Sentence, StringComparison.Ordinal);

        // A slope three times the solver's, as a wrong distance or BC would give, is recovered and the solver's figure falls outside it.
        var (v3, y3) = Group(12, 40, 15, 0.5, 3 * k);
        var slope = VelocityVertical.Regress([.. v3.Zip(y3)], k);
        Assert.NotNull(slope);
        Assert.InRange(3 * k, slope.SlopeInchesPerFps.Lower, slope.SlopeInchesPerFps.Upper);
        Assert.False(slope.PredictedInside);

        Assert.Null(VelocityVertical.Regress([(2700, 0.1), (2700, 0.2), (2700, 0.3)], k));
        Assert.Null(VelocityVertical.Regress([(2700, 0.1), (2710, 0.2)], k));
    }

    [Fact]
    public void ItRefusesWhatItCannotCompare()
    {
        Assert.NotNull(VelocityVertical.Analyze(Load, Yards, [2700], [0.1, 0.2]).Refusal);
        Assert.NotNull(VelocityVertical.Analyze(Load, Yards, [2700, 2710], [0.1]).Refusal);
        Assert.NotNull(VelocityVertical.Analyze(Load, Yards, [2700, 2710], [0.1, 0.1]).Refusal);
        Assert.NotNull(VelocityVertical.Analyze(Load, 0, [2700, 2710], [0.1, 0.2]).Refusal);
    }

    [Fact]
    public void TheVelocityCommandPrintsTheFiguresForASampleSession()
    {
        const string session = """
            {
              "distanceYards": 300, "ballisticCoefficient": 0.243, "dragModel": "G7", "bulletWeightGrains": 175,
              "shots": [
                { "id": 1, "x": 0.1, "y": 0.42 }, { "id": 2, "x": -0.3, "y": -0.35 }, { "id": 3, "x": 0.2, "y": 0.05 },
                { "id": 4, "x": 0.0, "y": -0.61 }, { "id": 5, "x": 0.4, "y": 0.30 }, { "id": 6, "x": -0.1, "y": 0.12 },
                { "id": 7, "x": 0.2, "y": -0.20 }, { "id": 8, "x": -0.4, "y": 0.27 }, { "id": 9, "x": 0.1, "y": -0.08 },
                { "id": 10, "x": 0.3, "y": 0.10 }
              ],
              "chronograph": "2712, 2688, 2701, 2679, 2709, 2703, 2690, 2711, 2696, 2702",
              "shotVelocities": [
                { "shotId": 1, "ordinal": 1 }, { "shotId": 2, "ordinal": 2 }, { "shotId": 3, "ordinal": 3 }, { "shotId": 4, "ordinal": 4 },
                { "shotId": 5, "ordinal": 5 }, { "shotId": 6, "ordinal": 6 }, { "shotId": 7, "ordinal": 7 }, { "shotId": 8, "ordinal": 8 },
                { "shotId": 9, "ordinal": 9 }, { "shotId": 10, "ordinal": 10 }
              ]
            }
            """;
        string file = Path.Combine(Path.GetTempPath(), "velocity-session.json");
        File.WriteAllText(file, session);
        try
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(0, GroupLab.Cli.VelocityVerb.Run([file, "--confidence", "0.9"], output, error));
            string text = output.ToString();
            Assert.Contains("G7 BC 0.243, sight 1.50 in, zeroed at 100 yd, 59 F at 0 ft, shot at 300 yd", text, StringComparison.Ordinal);
            Assert.Contains("Velocity            10 readings, mean 2699 fps", text, StringComparison.Ordinal);
            Assert.Contains("Predicted vertical  SD ", text, StringComparison.Ordinal);
            Assert.Contains("Measured vertical   SD ", text, StringComparison.Ordinal);
            Assert.Contains("of the vertical", text, StringComparison.Ordinal);
            Assert.Contains("Shot by shot over 10 matched shots", text, StringComparison.Ordinal);
            Assert.Contains("Aerodynamic jump is not modeled.", text, StringComparison.Ordinal);
            Assert.Equal("", error.ToString());

            Assert.Equal(2, GroupLab.Cli.VelocityVerb.Print("""{ "shots": [] }""", [], new StringWriter(), error));
            Assert.Contains("the distance, the BC and the drag model are required", error.ToString(), StringComparison.Ordinal);
            Assert.Equal(2, GroupLab.Cli.VelocityVerb.Run([], new StringWriter(), new StringWriter()));
        }
        finally
        {
            File.Delete(file);
        }
    }
}
