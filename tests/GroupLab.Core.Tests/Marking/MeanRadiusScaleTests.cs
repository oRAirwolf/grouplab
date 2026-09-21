using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 6.1: the group's mean radius per 100 yards on a scale with reference marks.
/// <para>
/// <b>The marks are somebody else's rules of thumb, and the tests hold that they are labelled as such.</b> This project's whole argument is
/// that a handful of shots does not support a verdict. Handing one down anyway, using numbers quoted on a podcast, would be the clearest
/// possible contradiction of everything else it says, so the attribution and the caveat are held by tests rather than left to a comment.
/// </para>
/// </summary>
public class MeanRadiusScaleTests
{
    /// <summary>An angle is an angle: half an inch at 200 yards is the same as a quarter at 100.</summary>
    [Fact]
    public void AGroupIsPutOnTheScaleByItsAngle()
    {
        Assert.Equal(0.25, MeanRadiusScale.PerHundredYards(0.25, 100), 6);
        Assert.Equal(0.25, MeanRadiusScale.PerHundredYards(0.50, 200), 6);
        Assert.Equal(0.25, MeanRadiusScale.PerHundredYards(1.00, 400), 6);
    }

    [Fact]
    public void WithoutADistanceThereIsNoPlaceOnTheScale()
    {
        Assert.True(double.IsNaN(MeanRadiusScale.PerHundredYards(0.25, 0)));
        Assert.True(double.IsNaN(MeanRadiusScale.Along(double.NaN)));
    }

    /// <summary>A poor group still sits somewhere rather than falling off the end of the bar.</summary>
    [Fact]
    public void EveryGroupSitsSomewhereOnTheBar()
    {
        Assert.Equal(0, MeanRadiusScale.Along(0), 6);
        Assert.Equal(1, MeanRadiusScale.Along(MeanRadiusScale.WidestInchesPer100), 6);
        Assert.Equal(1, MeanRadiusScale.Along(5), 6);
        Assert.InRange(MeanRadiusScale.Along(0.3), 0.4, 0.6);
    }

    /// <summary>The marks are the ones that source gave, at the values it gave them.</summary>
    [Fact]
    public void TheMarksAreTheOnesQuoted()
    {
        Assert.Equal([0.300, 0.200, 0.175], MeanRadiusScale.Marks.Select(m => m.InchesPer100));
        Assert.Contains(MeanRadiusScale.Marks, m => m.Says == "pretty good");
        Assert.Contains(MeanRadiusScale.Marks, m => m.Says == "really, really good");
    }

    /// <summary>
    /// They are attributed wherever they appear. A mark on a GroupLab screen reads as GroupLab's opinion unless it says otherwise, and this
    /// is not GroupLab's opinion.
    /// </summary>
    [Fact]
    public void TheMarksAreAlwaysAttributed()
    {
        Assert.Contains("Hornady podcast", MeanRadiusScale.Attribution, StringComparison.Ordinal);
        Assert.Contains("not GroupLab's own measurements", MeanRadiusScale.Attribution, StringComparison.Ordinal);

        foreach (int shots in new[] { 3, 5, 10, 20, 40 })
        {
            Assert.Contains(MeanRadiusScale.Attribution, MeanRadiusScale.Caveat(shots), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A five shot group is told plainly that it cannot carry this comparison. The marks were quoted for twenty to thirty shots, so a small
    /// group sitting on one has not met the condition the number came with.
    /// </summary>
    [Fact]
    public void AFewShotsAreToldTheComparisonIsWeak()
    {
        string few = MeanRadiusScale.Caveat(5);

        Assert.Contains("too few", few, StringComparison.Ordinal);
        Assert.Contains("anywhere across several of these marks", few, StringComparison.Ordinal);
        Assert.Contains("twenty to thirty", few, StringComparison.Ordinal);
    }

    [Fact]
    public void ManyShotsAreToldTheComparisonMeansSomething()
    {
        string many = MeanRadiusScale.Caveat(25);

        Assert.Contains("25 shots", many, StringComparison.Ordinal);
        Assert.Contains("enough for this comparison to mean much", many, StringComparison.Ordinal);
    }

    /// <summary>
    /// The plainest statement of an unsettled comparison: if the interval covers a mark, the group could honestly be called two different
    /// things, so it is not either of them yet.
    /// </summary>
    [Fact]
    public void AnIntervalCoveringAMarkIsSaidToStraddleIt()
    {
        // An interval from 0.15 to 0.25 covers both the 0.175 and the 0.200 marks.
        Assert.True(MeanRadiusScale.StraddlesAMark(0.15, 0.25));

        // One from 0.21 to 0.24 sits between marks and settles nothing either way.
        Assert.False(MeanRadiusScale.StraddlesAMark(0.21, 0.24));

        Assert.False(MeanRadiusScale.StraddlesAMark(double.NaN, 0.25));
    }
}
