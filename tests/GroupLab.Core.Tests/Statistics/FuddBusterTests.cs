using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>NOTES-FROM-PLANNING.md entry 279 section 3: "Fudd buster mode", the lessons of small samples from a shooter's own shots.</summary>
public class FuddBusterTests
{
    // Thirty shots from one round normal pattern, sigma 0.5 in, drawn with a fixed seed.
    private static List<PointD> Group(int n = 30, ulong seed = 7)
    {
        var random = new StatisticsRandom(seed);
        return [.. Enumerable.Range(0, n).Select(_ => new PointD(0.5 * random.NextNormal(), 0.5 * random.NextNormal()))];
    }

    [Fact]
    public void BelowTwentyShotsThereIsNoLesson() => Assert.Null(FuddBuster.Of(Group(19)));

    [Fact]
    public void TheSameShotsAlwaysShowTheSameExamples()
    {
        var a = FuddBuster.Of(Group())!;
        var b = FuddBuster.Of(Group())!;
        Assert.Equal(a.Seed, b.Seed);
        Assert.Equal(a.Tightest.Shots, b.Tightest.Shots);
        Assert.Equal(a.Threes.MeanInches, b.Threes.MeanInches);
        Assert.NotEqual(a.Seed, FuddBuster.Of(Group(seed: 8))!.Seed);
    }

    [Fact]
    public void ThreeShotGroupsFromOneRifleRangeWidely()
    {
        var lesson = FuddBuster.Of(Group())!;
        Assert.Equal(3, lesson.Tightest.Shots.Distinct().Count());
        Assert.True(lesson.Widest.ExtremeSpreadInches > 3 * lesson.Tightest.ExtremeSpreadInches);
        Assert.True(lesson.Widest.ExtremeSpreadInches <= lesson.AllShotsExtremeSpreadInches);
    }

    [Fact]
    public void AveragingSmallGroupsIsBiasedLowAndStillMoves()
    {
        var lesson = FuddBuster.Of(Group())!;
        // A small group's extreme spread is smaller than the whole group's, so their average is too, and fives sit above threes.
        Assert.True(lesson.Threes.MeanInches < lesson.Fives.MeanInches);
        Assert.True(lesson.Fives.MeanInches < lesson.AllShotsExtremeSpreadInches);
        Assert.True(lesson.Threes.LowestInches < lesson.Threes.MeanInches && lesson.Threes.MeanInches < lesson.Threes.HighestInches);
        Assert.Equal(FuddBuster.SplitCount, lesson.Threes.Splits);
    }

    [Fact]
    public void TheZeroChaseTakesFiveAtATimeAndSaysHowFarEachWandered()
    {
        // Thirty shots: every group of five centered 0.4 in right of the whole, then the next 0.4 in left, around a true center at the aim.
        var shots = new List<PointD>();
        for (int g = 0; g < 6; g++)
        {
            double x = g % 2 == 0 ? 0.4 : -0.4;
            shots.AddRange([new(x + 0.1, 0.1), new(x - 0.1, 0.1), new(x, 0), new(x + 0.1, -0.1), new(x - 0.1, -0.1)]);
        }

        var lesson = FuddBuster.Of(shots, new Rifle("Test rifle", 0.25, AngularUnit.Moa), 3600)!;
        Assert.Equal(6, lesson.Chase.Count);
        Assert.Equal((1, 5), (lesson.Chase[0].First, lesson.Chase[0].Last));
        Assert.All(lesson.Chase, step => Assert.Equal(0.4, step.FromTrueCentreInches, 9));
        // 0.4 in at 100 yd is 0.382 MOA, 1.53 clicks: 2 clicks, against the direction the five sat.
        Assert.Equal("2 clicks left", lesson.Chase[0].Across!.Describe());
        Assert.Equal("2 clicks right", lesson.Chase[1].Across!.Describe());
    }

    [Fact]
    public void TheWordsSayTheNumbersAndNeverCallAveragingInvalid()
    {
        var lesson = FuddBuster.Of(Group())!;
        var units = UnitSettings.Imperial;
        var all = FuddBusterWords.Threes(lesson, units).Concat(FuddBusterWords.Averages(lesson, units)).Concat(FuddBusterWords.Chase(lesson, units)).ToList();
        Assert.Contains(FuddBusterWords.SameRifle, all);
        Assert.Contains(all, l => l.Contains(units.Length(lesson.Tightest.ExtremeSpreadInches), StringComparison.Ordinal));
        Assert.Contains(all, l => l.Contains(units.Length(lesson.Threes.MeanInches), StringComparison.Ordinal));
        Assert.DoesNotContain(all, l => l.Contains("invalid", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(lesson.Chase.Count + 2, FuddBusterWords.Chase(lesson, units).Count);
    }
}
