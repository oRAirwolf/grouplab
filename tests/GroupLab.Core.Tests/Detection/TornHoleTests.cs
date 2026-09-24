using GroupLab.Core.Detection;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.1: the clear hole on scan 1 bull 2 that raised no candidate and said nothing.
/// <para>
/// <b>The numbers in these tests are that sheet's own.</b> The hole measured 0.236 in across at hull solidity 0.54, against a compactness
/// floor of 0.55, and sat 0.101 in from the middle of its bull. The ragged marks on the same sheet that are not holes measured 0.102, 0.124,
/// 0.231, 0.245, 0.274 and 0.285 in at solidity 0.41 to 0.53, and every one of them is half an inch to nearly two inches from any bull, up
/// in the header where the writing is. So the tests are not a restatement of the rule: they are the evidence it was drawn from.
/// </para>
/// </summary>
public class TornHoleTests
{
    /// <summary>What a .224 bullet's hole is taken to measure on paper, which is what the detector carries rather than the bullet's width.</summary>
    private const double Hole = 0.21168;

    private const double Floor = 0.55;

    /// <summary>The hole itself: ragged, the right size, and on a bull.</summary>
    [Fact]
    public void TheHoleOnScan1Bull2IsKept()
    {
        Assert.True(TornHole.Rescues(0.54, Floor, 0.236, Hole, insideACell: true, fromCellCentreInches: 0.101));
    }

    /// <summary>
    /// And nothing else ragged on that sheet is. These are the six marks the detector refused for compactness, at their measured sizes and
    /// distances: handwriting and paper damage in the header, and one mark out by the sighters.
    /// </summary>
    [Theory]
    [InlineData(0.41, 0.231, 0.505)]
    [InlineData(0.47, 0.285, 0.841)]
    [InlineData(0.50, 0.274, 0.989)]
    [InlineData(0.53, 0.102, 1.319)]
    [InlineData(0.48, 0.245, 1.389)]
    [InlineData(0.53, 0.124, 1.414)]
    public void NothingElseRaggedOnThatSheetIsKept(double solidity, double diameter, double away)
    {
        Assert.False(TornHole.Rescues(solidity, Floor, diameter, Hole, insideACell: true, fromCellCentreInches: away));
    }

    /// <summary>
    /// Without a stated calibre nothing is rescued at all. There is no size to judge against, and the rule would come down to "ragged things
    /// near bulls are holes", which is the kind of position prior that invents shots.
    /// </summary>
    [Fact]
    public void WithNoCalibreNothingIsRescued()
    {
        Assert.False(TornHole.Rescues(0.54, Floor, 0.236, null, insideACell: true, fromCellCentreInches: 0.101));
    }

    /// <summary>A mark off the bulls is never rescued, however well sized: the cell is half the evidence.</summary>
    [Fact]
    public void AMarkOffTheBullsIsNotRescued()
    {
        Assert.False(TornHole.Rescues(0.54, Floor, 0.236, Hole, insideACell: false, fromCellCentreInches: 0.101));
    }

    /// <summary>Below the floor the rule keeps out, nothing is rescued however well placed.</summary>
    [Fact]
    public void TooRaggedIsStillTooRagged()
    {
        Assert.False(TornHole.Rescues(TornHole.LeastSolidity - 0.01, Floor, 0.236, Hole, insideACell: true, fromCellCentreInches: 0.05));
    }

    /// <summary>A mark the compactness gate was never going to refuse is not this rule's business.</summary>
    [Fact]
    public void ACompactMarkIsNotRescuedBecauseItNeedsNoRescue()
    {
        Assert.False(TornHole.Rescues(0.95, Floor, 0.236, Hole, insideACell: true, fromCellCentreInches: 0.05));
    }

    /// <summary>Something twice a hole's width is not one torn hole, whatever its shape.</summary>
    [Fact]
    public void SomethingFarTooLargeIsNotATornHole()
    {
        Assert.False(TornHole.Rescues(0.54, Floor, Hole * 2.5, Hole, insideACell: true, fromCellCentreInches: 0.05));
    }

    /// <summary>The refusal a rescued mark carries says what it is and why it was kept, so nothing is counted silently.</summary>
    [Fact]
    public void ARescuedMarkSaysWhyItWasKept()
    {
        string said = TornHole.Describe(0.54);

        Assert.Contains("torn", said, StringComparison.Ordinal);
        Assert.Contains("0.54", said, StringComparison.Ordinal);
        Assert.Contains("caliber's size", said, StringComparison.Ordinal);
    }
}
