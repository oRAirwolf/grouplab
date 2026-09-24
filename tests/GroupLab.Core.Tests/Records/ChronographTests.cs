using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 115 section 3 and DESIGN.md section 15: the shots and the chronograph's readings are two ordered lists that
/// get reconciled, never assumed to align. The entry's four cases: equal counts, one reading missing, one extra reading, and no string at all.
/// </summary>
public class ChronographTests
{
    private static readonly int[] Shots = [1, 2, 3, 4, 5];

    [Fact]
    public void EqualCountsPairInOrderAsAProposal()
    {
        double[] readings = [2705, 2711, 2698, 2716, 2702];
        var pairs = Chronograph.Pair(Shots, readings);
        Assert.Equal(Shots.Select(s => (int?)s), pairs.Select(p => p.ShotId));
        Assert.Equal(new int?[] { 0, 1, 2, 3, 4 }, pairs.Select(p => p.Reading));
        Assert.Equal(
            "5 of 5 readings sit beside a shot. The counts agree; check the order before accepting it, because a chronograph can miss a shot and record a neighbor's.",
            Chronograph.Describe(pairs, readings.Length));
    }

    [Fact]
    public void AShotWithNoReadingLeavesTheRestInOrder()
    {
        // The chronograph missed the third shot: four readings for five shots, and marking shot 3 puts the rest back in step.
        double[] readings = [2705, 2711, 2716, 2702];
        var naive = Chronograph.Pair(Shots, readings);
        Assert.Equal(new int?[] { 0, 1, 2, 3, null }, naive.Select(p => p.Reading));
        Assert.Contains("1 shot has no reading", Chronograph.Describe(naive, readings.Length), StringComparison.Ordinal);

        var marked = Chronograph.Pair(Shots, readings, shotsWithNoReading: new HashSet<int> { 3 });
        Assert.Equal(new int?[] { 0, 1, null, 2, 3 }, marked.Select(p => p.Reading));
        Assert.Equal(4, marked.Count(p => p is { ShotId: not null, Reading: not null }));
    }

    [Fact]
    public void AReadingOfNoShotIsMarkedAndTheRestPairInOrder()
    {
        // The first reading is the fouling round fired into the berm: six readings for five shots.
        double[] readings = [2650, 2705, 2711, 2698, 2716, 2702];
        var naive = Chronograph.Pair(Shots, readings);
        Assert.Contains("1 reading belongs to no shot", Chronograph.Describe(naive, readings.Length), StringComparison.Ordinal);
        Assert.Single(naive, p => p is { ShotId: null, Reading: 5 });

        var marked = Chronograph.Pair(Shots, readings, readingsOfNoShot: new HashSet<int> { 0 });
        Assert.Equal(new int?[] { 1, 2, 3, 4, 5, 0 }, marked.Select(p => p.Reading));
        Assert.Equal(5, marked.Count(p => p is { ShotId: not null, Reading: not null }));
        Assert.Single(marked, p => p is { ShotId: null, Reading: 0 });
    }

    [Fact]
    public void WithNoStringThereIsNothingToPairAndNoSpread()
    {
        var pairs = Chronograph.Pair(Shots, []);
        Assert.All(pairs, p => Assert.Null(p.Reading));
        Assert.Contains("5 shots have no reading", Chronograph.Describe(pairs, 0), StringComparison.Ordinal);
        Assert.Null(Chronograph.Spread([]));
        Assert.Null(Chronograph.Spread([2705]));

        var spread = Chronograph.Spread([2705, 2711, 2698, 2716, 2702])!;
        Assert.Equal(5, spread.Readings);
        Assert.Equal(2706.4, spread.MeanFps, 6);
        Assert.Equal(Math.Sqrt(205.2 / 4), spread.SdFps, 9);
    }

    [Fact]
    public void TheListIsReadAsPastedAndARefusalNamesWhatIsWrong()
    {
        Assert.Equal([2705, 2711.5, 2698], Chronograph.Read("2705, 2711.5\n2698").Velocities);
        Assert.Equal([2705, 2711], Chronograph.Read(" 2705 ; 2711 ").Velocities);
        Assert.Contains("is not a velocity", Chronograph.Read("2705 fps").Refusal, StringComparison.Ordinal);
        Assert.Contains("is not a muzzle velocity in ft/s", Chronograph.Read("2705, 825000").Refusal, StringComparison.Ordinal);
        Assert.Equal("There are no readings in that.", Chronograph.Read("   ").Refusal);
    }
}
