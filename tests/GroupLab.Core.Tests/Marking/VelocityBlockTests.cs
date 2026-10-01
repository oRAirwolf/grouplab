using GroupLab.Core.Ballistics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 323: "Velocity and the vertical", the block the desktop and the phone both draw. Every state in the words
/// Alan approved, the figures the same as <c>grouplab velocity</c> prints, and the confidence the one the result states.
/// </summary>
public sealed class VelocityBlockTests
{
    private const double Yards = 600;

    private static readonly Load G7Load = new("Test load", null) { BallisticCoefficient = 0.243, DragModel = DragModel.G7, BulletWeightGrains = 175 };

    private static string Size(double inches) => inches.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " in";

    /// <summary>A group of shots at the given heights, up positive, in inches at the target, 100 pixels to the inch, at 600 yd.</summary>
    private static MarkingState Group(IReadOnlyList<double> up, double? distanceInches = Yards * 36)
    {
        var session = new MarkingSession(MarkingState.Empty with
        {
            Scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
            PointOfAim = new PointD(1000, 1000),
            ShotDistanceInches = distanceInches,
        });
        var random = new Random(3);
        foreach (double height in up)
        {
            session.AddShot(new PointD(1000 + (random.NextDouble() * 60) - 30, 1000 - (height * 100)));
        }

        return session.State;
    }

    private static ChronographString Readings(params double[] fps) => new(7, 1, "Chronograph", null, fps);

    /// <summary>Pairs each shot, in the order added, with the reading of the same position.</summary>
    private static List<ShotVelocity> InOrder(MarkingState state) =>
        [.. state.Shots.Select((s, i) => new ShotVelocity(1, s.Id, 7, i + 1))];

    private static readonly double[] Velocities = [2690, 2712, 2701, 2685, 2718, 2696, 2707, 2693, 2710, 2688];

    private static double K => Projection.DropPerFps(new BallisticInput(0.243, DragModel.G7, Velocities.Average(), 175), Yards);

    private static VelocityBlock Build(MarkingState state, ChronographString? readings, IReadOnlyList<ShotVelocity>? pairs = null, Load? load = null, double confidence = 0.90) =>
        VelocityBlocks.Build(state, load ?? G7Load, readings, pairs ?? [], UnitSettings.Imperial, Size, confidence)!;

    [Fact]
    public void WithoutReadingsTheBlockAsksForThemInTheApprovedWords()
    {
        var block = Build(Group([0.5, -0.3, 1.1, -0.9, 0.2]), null);
        Assert.Equal(VelocityBlockState.NoReadings, block.State);
        Assert.Equal("Add this group's chronograph readings to see how much of its vertical comes from velocity.", block.Sentence);
        Assert.Equal("Add readings", block.Action);
        Assert.Empty(block.Bars);
        Assert.Empty(block.Dots);
        Assert.Null(block.Band);
    }

    [Fact]
    public void WithoutTheDistanceOrTheBcTheBlockSaysTheSolverNeedsThem()
    {
        var noDistance = Build(Group([0.5, -0.3, 1.1, -0.9, 0.2], distanceInches: null), Readings(Velocities));
        Assert.Equal(VelocityBlockState.NoDistance, noDistance.State);
        Assert.Equal("The solver needs the distance shot and the bullet's BC to work this out.", noDistance.Sentence);
        Assert.Equal("Set the distance", noDistance.Action);
        Assert.Null(noDistance.Band);

        var noBc = Build(Group([0.5, -0.3, 1.1, -0.9, 0.2]), Readings(Velocities), load: new Load("Plain", null));
        Assert.Equal(VelocityBlockState.NoBc, noBc.State);
        Assert.Equal(noDistance.Sentence, noBc.Sentence);
        Assert.Equal("Set the load's BC", noBc.Action);
        Assert.Empty(noBc.Bars);

        var noModel = Build(Group([0.5, -0.3, 1.1, -0.9, 0.2]), Readings(Velocities), load: new Load("No model", null) { BallisticCoefficient = 0.243 });
        Assert.Equal(VelocityBlockState.NoBc, noModel.State);
    }

    [Fact]
    public void FewerThanTwoShotsHaveNoVerticalAndNoBlock() =>
        Assert.Null(VelocityBlocks.Build(Group([0.5]), G7Load, Readings(Velocities), [], UnitSettings.Imperial, Size));

    /// <summary>State 1: a wide vertical against a tight string, so velocity is a small share the data can bound away from all of it.</summary>
    [Fact]
    public void WhenTheDataCanTellTheShareIsTheHeadlineAndTheFiguresAreTheCommandsOwn()
    {
        double[] up = [3.0, -3.8, 1.0, 5.2, -2.0, -5.0, 4.0, -0.5, 2.2, -2.8];
        var state = Group(up);
        var block = Build(state, Readings(Velocities));
        Assert.Equal(VelocityBlockState.Result, block.State);

        var (expected, _) = VelocityVertical.Analyze(new BallisticInput(0.243, DragModel.G7, 1, 175), Yards, Velocities, up, null, 0.90);
        Assert.NotNull(expected);
        Assert.False(expected.CannotTell);
        Assert.Equal("about " + VelocityVertical.Percent(expected.Share.Value), block.Headline);
        Assert.EndsWith("% at 90%", block.Interval, StringComparison.Ordinal);
        Assert.Equal("of this group's vertical comes from velocity alone; the rest is the rifle, the shooter and the wind.", block.Sentence);
        Assert.Equal(["Measured vertical SD", "From velocity alone"], block.Bars.Select(b => b.Label));
        Assert.Equal(expected.MeasuredVerticalSdInches.Value, block.Bars[0].Value, 9);
        Assert.Equal(expected.PredictedVerticalSdInches.Value, block.Bars[1].Value, 9);
        Assert.Equal(Size(expected.PredictedVerticalSdInches.Value), block.Bars[1].Text);
        Assert.True(block.Bars[1].Amber);
        Assert.False(block.Bars[0].Amber);
        Assert.Equal(expected.PredictedVerticalSdInches.Value, block.Band!.Value.PredictedSdInches, 9);
        Assert.Equal(expected.MeasuredVerticalSdInches.Value, block.Band!.Value.MeasuredSdInches, 9);

        // No reading is paired with a shot, so there is no chart and no sentence about one.
        Assert.Empty(block.Dots);
        Assert.Null(block.SlopeSentence);
        Assert.Equal(VelocityBlock.WhyClosing, block.Why[^1]);
        Assert.Contains(block.Why, w => w.StartsWith("Velocity: 10 readings", StringComparison.Ordinal));
        Assert.Contains(block.Why, w => w.Contains("Test load", StringComparison.Ordinal));
    }

    /// <summary>State 2: a wide string against a tight vertical, so the interval reaches all of it.</summary>
    [Fact]
    public void WhenTheIntervalReachesAllOfItTheBlockSaysTheDataCannotTell()
    {
        double[] wide = [2650, 2760, 2700, 2620, 2780, 2680];
        var block = Build(Group([0.05, -0.04, 0.02, -0.06, 0.03]), Readings(wide));
        Assert.Equal(VelocityBlockState.CannotTell, block.State);
        Assert.Equal("the data cannot tell", block.Headline);
        Assert.StartsWith("Anywhere from ", block.Sentence, StringComparison.Ordinal);
        Assert.EndsWith("to all of it, at 90%, from 6 readings and 5 shots. More readings and shots narrow it.", block.Sentence, StringComparison.Ordinal);
        Assert.True(block.Share!.Upper >= 1);
        Assert.NotNull(block.Band);
    }

    /// <summary>The confidence shown is the one the result states, never a fixed 90%.</summary>
    [Fact]
    public void TheConfidenceShownIsTheResults()
    {
        var block = Build(Group([3.0, -3.8, 1.0, 5.2, -2.0, -5.0, 4.0, -0.5, 2.2, -2.8]), Readings(Velocities), confidence: 0.80);
        Assert.EndsWith("at 80%", block.Interval, StringComparison.Ordinal);
        Assert.Contains("Every range is at 80%.", block.Why);
    }

    /// <summary>Section 1.6: matched shots that follow the solver's slope get the chart and the agreeing sentence; faster shots sit higher.</summary>
    [Fact]
    public void ShotsMatchedByAnAcceptedPairingGetTheChartAndTheSlopeSentence()
    {
        double mean = Velocities.Average();
        double[] noise = [0.01, -0.02, 0.015, -0.01, 0.0, 0.02, -0.015, 0.01, -0.005, 0.005];
        double[] up = [.. Velocities.Select((v, i) => (K * (v - mean)) + noise[i])];
        var state = Group(up);
        var block = Build(state, Readings(Velocities), InOrder(state));
        Assert.Equal(Velocities.Length, block.Dots.Count);
        Assert.NotNull(block.MeasuredSlope);
        Assert.Equal(K, block.SolverSlope!.Value, 9);
        Assert.False(block.SlopeDisagrees);
        Assert.StartsWith("Shot by shot, each 10 fps raised the hole ", block.SlopeSentence, StringComparison.Ordinal);
        Assert.EndsWith(", inside that range.", block.SlopeSentence, StringComparison.Ordinal);
        Assert.Contains("The solver says " + UnitSettings.Imperial.Length(K * 10) + ",", block.SlopeSentence, StringComparison.Ordinal);
    }

    /// <summary>State 5: the shots move three times as far as the solver says, and the sentence names what to check.</summary>
    [Fact]
    public void WhenShotByShotDisagreesWithTheSolverTheSentenceSaysWhatToCheck()
    {
        double mean = Velocities.Average();
        double[] noise = [0.01, -0.02, 0.015, -0.01, 0.0, 0.02, -0.015, 0.01, -0.005, 0.005];
        double[] up = [.. Velocities.Select((v, i) => (3 * K * (v - mean)) + noise[i])];
        var state = Group(up);
        var block = Build(state, Readings(Velocities), InOrder(state));
        Assert.True(block.SlopeDisagrees);
        Assert.EndsWith(", outside that range: check the distance, the BC, or the order the readings were matched in.", block.SlopeSentence, StringComparison.Ordinal);
    }

    /// <summary>A pairing from another string, or naming a shot left out, is not this string's, and is not drawn.</summary>
    [Fact]
    public void OnlyTheNewestStringsPairsOfCountedShotsAreDrawn()
    {
        double mean = Velocities.Average();
        double[] up = [.. Velocities.Select((v, i) => (K * (v - mean)) + (0.01 * (i % 3)))];
        var state = Group(up);
        var otherString = state.Shots.Select((s, i) => new ShotVelocity(1, s.Id, 99, i + 1)).ToList();
        Assert.Empty(Build(state, Readings(Velocities), otherString).Dots);

        var session = new MarkingSession(state);
        session.SetExclusion(state.Shots[0].Id, ExclusionReason.CalledFlyer);
        var block = Build(session.State, Readings(Velocities), InOrder(state));
        Assert.Equal(Velocities.Length - 1, block.Dots.Count);
    }
}
