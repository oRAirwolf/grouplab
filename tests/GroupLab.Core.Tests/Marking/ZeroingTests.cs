using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// The zero correction, NOTES-FROM-PLANNING.md entries 91 and 92 against entry 53 section 3: the offset, its uncertainty, and a refusal with
/// a shot count where the offset is smaller than the shots can resolve.
/// </summary>
public class ZeroingTests
{
    /// <summary>Entry 53 section 3's table, which was computed there and is reproduced here from the formula rather than copied as numbers.</summary>
    [Theory]
    [InlineData(3, 1.603)]
    [InlineData(5, 1.031)]
    [InlineData(10, 0.664)]
    [InlineData(20, 0.453)]
    [InlineData(25, 0.402)]
    [InlineData(50, 0.281)]
    public void TheDetectableOffsetMatchesTheTableItWasSpecifiedFrom(int shots, double expected) =>
        Assert.Equal(expected, Zeroing.DetectableMultiple(shots, circular: true), 3);

    /// <summary>
    /// Entry 91 section 2's own example: ten shots, sigma about 0.27 in, an offset well inside what ten shots can resolve. The answer is a
    /// refusal and a number of shots, never a bare correction, because dialling it would be correcting for sampling noise.
    /// </summary>
    [Fact]
    public void AnOffsetSmallerThanTheShotsCanResolveIsRefusedWithTheShotsThatWouldSettleIt()
    {
        var session = Group(offsetInches: 0.10, sigmaInches: 0.27, shots: 10);

        var zero = Zeroing.For(session.State)!;

        Assert.Equal(10, zero.Shots);
        Assert.False(zero.Worth);
        Assert.False(zero.Windage.Distinguishable);
        Assert.True(zero.DetectableInches > 0.10, $"the smallest offset ten shots can call is {zero.DetectableInches:0.000} in");
        Assert.NotNull(zero.Windage.ShotsToSettle);
        Assert.True(zero.Windage.ShotsToSettle > 10, "settling it needs more shots than were fired");
    }

    /// <summary>An offset several times the sampling error is a correction, and the turret goes the other way from where the group sits.</summary>
    [Fact]
    public void AnOffsetLargerThanTheSamplingErrorIsACorrectionInTheOppositeDirection()
    {
        var session = Group(offsetInches: 1.2, sigmaInches: 0.27, shots: 10);

        var zero = Zeroing.For(session.State)!;

        Assert.True(zero.Worth);
        Assert.True(zero.Windage.Distinguishable);
        Assert.Equal("right", zero.Windage.Sits);
        Assert.Equal("left", zero.Windage.Dial);
        Assert.Null(zero.Windage.ShotsToSettle);
        Assert.Equal(18, zero.DegreesOfFreedom);
    }

    /// <summary>Below the dispersion floor there is no sigma, so there is nothing to tell an offset from noise with and no correction is offered.</summary>
    [Fact]
    public void TooFewShotsGiveNoCorrectionAtAll()
    {
        Assert.Null(Zeroing.For(Group(offsetInches: 1.0, sigmaInches: 0.27, shots: 3).State));
        Assert.Null(Zeroing.For(new MarkingSession().State));
    }

    /// <summary>
    /// A marking of <paramref name="shots"/> shots evenly around a circle of the radius that gives the sigma asked for, centred
    /// <paramref name="offsetInches"/> right of the point of aim. It is deterministic, isotropic, and its centre is exactly the offset, so
    /// the test reads the rule rather than a draw from a random number generator.
    /// </summary>
    private static MarkingSession Group(double offsetInches, double sigmaInches, int shots)
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        double radius = sigmaInches * Math.Sqrt(2);
        for (int k = 0; k < shots; k++)
        {
            double angle = 2 * Math.PI * k / shots;
            session.AddShot(new PointD(100 * (offsetInches + (radius * Math.Cos(angle))), 100 * radius * Math.Sin(angle)));
        }

        return session;
    }
}
