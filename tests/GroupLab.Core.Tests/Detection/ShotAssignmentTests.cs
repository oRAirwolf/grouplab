using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// docs/PHASE1-BRIEF.md section 4.5 and docs/DETECTION-PIPELINE.md stage S9: one-to-one matching where the counts allow it,
/// nearest-bull kept for comparison, and no matching forced when there are more shots than bulls.
/// </summary>
public class ShotAssignmentTests
{
    private const double Inch = 254;

    /// <summary>
    /// The survey's case on <c>338lmao.jpg</c> (docs/SCAN-MEASUREMENTS.md section 7.2): a shot 0.627 in from bull 4 and
    /// 1.043 in from bull 9, where bull 4 already has a shot at 0.289 in and bull 9 has none. Nearest-bull gives both shots
    /// to bull 4; one-to-one gives the far shot to bull 9, which is right.
    /// </summary>
    [Fact]
    public void OneToOneGetsTheSurveysCrossCellShotRightWhereNearestBullDoesNot()
    {
        PointD[] bulls = [new(0, 0), new(1.5 * Inch, 0)];
        double x = (2.25 - ((1.043 * 1.043) - (0.627 * 0.627))) / 3, y = Math.Sqrt((0.627 * 0.627) - (x * x));
        PointD[] shots = [new(0.289 * Inch, 0), new(x * Inch, y * Inch)];

        var result = ShotAssignment.Assign(shots, bulls);

        Assert.Equal(AssignmentMethod.OneToOne, result.Method);
        Assert.Equal(0, result.Shots[0].Bull);
        Assert.Equal(1, result.Shots[1].Bull);
        Assert.Equal(0, result.Shots[1].NearestBull);
        Assert.True(Math.Abs((result.Shots[1].NearestDistance / Inch) - 0.627) < 0.001);
        Assert.True(result.Shots[1].Ambiguous, "a shot given a bull other than its nearest is flagged");
        Assert.False(result.Shots[0].Ambiguous);
    }

    [Fact]
    public void FewerShotsThanBullsAreMatchedAgainstASubset()
    {
        PointD[] bulls = [new(0, 0), new(1.5 * Inch, 0), new(3 * Inch, 0)];
        PointD[] shots = [new(0.1 * Inch, 0), new(2.9 * Inch, 0.2 * Inch)];

        var result = ShotAssignment.Assign(shots, bulls);

        Assert.Equal(AssignmentMethod.OneToOne, result.Method);
        Assert.Equal([0, 2], result.Shots.Select(s => s.Bull!.Value).ToArray());
    }

    /// <summary>More shots than bulls: forcing a matching manufactured a false cross-cell result on <c>n568-gm210m.jpg</c>, so nearest-bull is used and every shot flagged.</summary>
    [Fact]
    public void MoreShotsThanBullsFallBackToNearestBullAndAreFlagged()
    {
        PointD[] bulls = [new(0, 0), new(1.5 * Inch, 0)];
        PointD[] shots = [new(0.1 * Inch, 0), new(-0.1 * Inch, 0), new(0.05 * Inch, 0.1 * Inch)];

        var result = ShotAssignment.Assign(shots, bulls);

        Assert.Equal(AssignmentMethod.NearestBull, result.Method);
        Assert.All(result.Shots, s => Assert.Equal(0, s.Bull));
        Assert.All(result.Shots, s => Assert.True(s.Ambiguous));
    }

    [Fact]
    public void AShotNearlyEquidistantBetweenTwoBullsIsFlagged()
    {
        PointD[] bulls = [new(0, 0), new(1.5 * Inch, 0)];
        PointD[] shots = [new(0.70 * Inch, 0)];

        var result = ShotAssignment.Assign(shots, bulls);

        Assert.True(result.Shots[0].Margin / Inch < ShotAssignment.AmbiguousMarginInches);
        Assert.True(result.Shots[0].Ambiguous);
    }
}
