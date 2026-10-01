using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker B item 2: the pairing a file's own evidence proposes. The string is Alan's own Garmin Xero export
/// of 2026-04-25 (39 shots over the afternoon, four groups), its shot rows only, as the reader receives them; the shots on paper are
/// synthetic, numbered 1 upwards in firing order.
/// </summary>
public class ChronographProposalTests
{
    private static readonly (double Fps, string Time)[] April25 =
    [
        (2841.0, "14:20:04"), (2846.2, "14:20:55"), (2836.3, "14:21:23"), (2832.9, "14:21:48"), (2835.1, "14:22:37"), (2845.2, "14:23:22"),
        (2844.0, "14:24:00"), (2834.3, "14:25:09"), (2837.7, "14:56:24"), (2833.9, "14:57:05"), (2837.4, "14:59:26"), (2833.4, "14:59:59"),
        (2849.1, "15:00:38"), (2853.9, "15:01:17"), (2831.7, "15:01:42"), (2832.9, "15:02:21"), (2836.5, "15:02:59"), (2833.2, "15:03:30"),
        (2837.2, "15:05:12"), (2832.7, "15:12:45"), (2838.2, "15:14:25"), (2838.6, "15:15:01"), (2823.2, "15:15:55"), (2837.8, "15:16:24"),
        (2825.6, "15:17:12"), (2842.4, "15:17:48"), (2835.3, "15:18:19"), (2835.7, "15:18:43"), (2835.3, "15:19:08"), (2836.5, "15:19:45"),
        (2833.7, "17:44:03"), (2822.0, "17:45:30"), (2834.3, "17:46:27"), (2834.0, "17:49:45"), (2827.1, "17:50:27"), (2821.4, "17:51:20"),
        (2831.9, "17:53:30"), (2830.9, "17:54:03"), (2837.2, "17:54:41"),
    ];

    /// <summary>The string as the Xero writes it, through the reader, so the time column is read the way an import reads it.</summary>
    private static IReadOnlyList<ChronographShot> Read(Func<int, string>? delta = null, Func<int, bool>? keep = null)
    {
        var lines = new List<string> { "300 Norma Mag", "#,Speed (FPS),Δ Avg (FPS),KE (FT-LBS),Power Factor (kgr⋅ft/s),Time,Clean Bore,Cold Bore,Shot Notes" };
        for (int i = 0; i < April25.Length; i++)
        {
            if (keep?.Invoke(i + 1) ?? true)
            {
                lines.Add(FormattableString.Invariant($"{i + 1},{April25[i].Fps:0.0},{delta?.Invoke(i + 1) ?? "0.0"},,,{April25[i].Time},,,"));
            }
        }

        var read = ChronographFiles.Read(string.Join("\n", lines) + "\n");
        Assert.Equal(ChronographFormat.GarminXero, read.Format);
        return read.Shots;
    }

    private static int[] Shots(int count) => [.. Enumerable.Range(1, count)];

    [Fact]
    public void TheXeroReaderKeepsEachShotsTime()
    {
        var shots = Read();
        Assert.Equal(39, shots.Count);
        Assert.Equal(new TimeSpan(14, 20, 4), shots[0].Time);
        Assert.Equal(new TimeSpan(17, 54, 41), shots[^1].Time);
        Assert.Equal(new TimeSpan(14, 20, 4), ChronographFiles.Clock("12/31/1899 14:20:04"));
        Assert.Equal(TimeSpan.FromHours(12), ChronographFiles.Clock("0.5"));
        Assert.Null(ChronographFiles.Clock("soon"));
    }

    [Fact]
    public void TheAfternoonSplitsIntoItsFourGroups()
    {
        // 3 min 18 s between shots 33 and 34 stays inside the last group; 7.5 minutes and more separate the groups.
        Assert.Equal([(0, 7), (8, 18), (19, 29), (30, 38)], ChronographReconciliation.Runs(Read()));
    }

    [Theory]
    [InlineData(8, 0, 7)]
    [InlineData(9, 30, 38)]
    [InlineData(19, 0, 18)]
    [InlineData(22, 8, 29)]
    public void AGroupMatchingOneStretchKeepsOnlyThatStretch(int count, int first, int last)
    {
        var proposal = ChronographReconciliation.Propose(Shots(count), Read());
        Assert.Equal(Enumerable.Range(0, 39).Where(i => i < first || i > last), proposal.ReadingsOfNoShot.Order());
        Assert.Empty(proposal.ShotsWithNoReading);
        Assert.Contains("only shots", Assert.Single(proposal.Reasons), StringComparison.Ordinal);

        var readings = April25.Select(s => s.Fps).ToList();
        var pairs = proposal.Pairs(Shots(count), readings);
        Assert.Equal(Enumerable.Range(first, count), pairs.Where(p => p.ShotId is not null).Select(p => p.Reading!.Value));
    }

    [Fact]
    public void TwoStretchesOfTheSameSizeProposeNothing()
    {
        // The second and third groups are both 11 shots: which one was this sheet is the person's to say.
        var proposal = ChronographReconciliation.Propose(Shots(11), Read());
        Assert.Empty(proposal.ReadingsOfNoShot);
        Assert.Contains("2 stretches", Assert.Single(proposal.Reasons), StringComparison.Ordinal);
    }

    [Fact]
    public void TheChronographsOwnLeftOutShotIsProposedFirstThenACleanBoreShot()
    {
        // One group of ten with a "--" shot and nothing else to go on: no times, so no runs.
        var readings = Enumerable.Range(1, 11).Select(n => new ChronographShot(n, 2830 + n, CleanBore: n == 1, LeftOutByChronograph: n == 6)).ToList();
        var one = ChronographReconciliation.Propose(Shots(10), readings);
        Assert.Equal([5], one.ReadingsOfNoShot.Order());
        Assert.Contains("left out of the chronograph's own figures", Assert.Single(one.Reasons), StringComparison.Ordinal);

        var two = ChronographReconciliation.Propose(Shots(9), readings);
        Assert.Equal([0, 5], two.ReadingsOfNoShot.Order());
        Assert.Contains(two.Reasons, r => r.Contains("clean bore", StringComparison.Ordinal));

        var three = ChronographReconciliation.Propose(Shots(8), readings);
        Assert.Equal([0, 5], three.ReadingsOfNoShot.Order());
        Assert.Contains(three.Reasons, r => r.Contains("1 more reading than shots and nothing in the file says which", StringComparison.Ordinal));
    }

    [Fact]
    public void ADeletedShotLeavesTheShotFiredInItsPlaceWithoutAReading()
    {
        // The first group with its fourth shot deleted on the chronograph, against the eight shots on the paper.
        var readings = Read(keep: n => n <= 8 && n != 4);
        var proposal = ChronographReconciliation.Propose(Shots(8), readings);
        Assert.Equal([4], proposal.ShotsWithNoReading.Order());
        Assert.Contains("skips shot 4", Assert.Single(proposal.Reasons), StringComparison.Ordinal);
        var pairs = proposal.Pairs(Shots(8), [.. readings.Select(r => r.Fps)]);
        Assert.Equal([0, 1, 2, null, 3, 4, 5, 6], pairs.Where(p => p.ShotId is not null).Select(p => p.Reading));
    }

    [Fact]
    public void AMissedShotThatLeavesNoGapIsNotGuessed()
    {
        var readings = Read(keep: n => n <= 7);
        var proposal = ChronographReconciliation.Propose(Shots(8), readings);
        Assert.Empty(proposal.ShotsWithNoReading);
        Assert.Contains("nothing in the file says which it missed", Assert.Single(proposal.Reasons), StringComparison.Ordinal);
    }

    [Fact]
    public void CountsThatAgreeProposeNothing()
    {
        var proposal = ChronographReconciliation.Propose(Shots(39), Read());
        Assert.Empty(proposal.ReadingsOfNoShot);
        Assert.Empty(proposal.ShotsWithNoReading);
        Assert.Empty(proposal.Reasons);
    }
}
