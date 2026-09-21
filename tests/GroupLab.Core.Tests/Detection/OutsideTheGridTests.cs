using GroupLab.Core.Detection;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.4: scan 5 had two holes off the bull grid, above bull 2 near the top edge and left of bull 12,
/// and neither was detected.
/// <para>
/// They were not refused for size or shape. They were refused for position, by a rule that threw away any mark centred outside every bull's
/// cell. <b>That rule is not a mistake</b>, which is what makes this delicate: the detector records it as "the position prior that would have
/// removed every false positive the survey's baselines made". Dropping it would trade two missed holes for an unknown number of invented
/// ones, and an invented hole is the worse of the two, because a shooter can see a shot that is missing and cannot see one that was never
/// fired.
/// </para>
/// <para>
/// So it is narrowed, not dropped, and these hold both halves: near the grid is kept, far out is still refused.
/// </para>
/// </summary>
public class OutsideTheGridTests
{
    /// <summary>A row of three bull cells, 600 dmm apart, each 240 dmm across.</summary>
    private static List<(double X, double Y, double HalfWidth, double HalfHeight)> Cells() =>
    [
        (300, 300, 120, 120),
        (900, 300, 120, 120),
        (1500, 300, 120, 120),
    ];

    [Fact]
    public void AMarkInsideACellIsObviouslyNearEnough()
    {
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(300, 300, Cells()));
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(380, 250, Cells()));
    }

    /// <summary>
    /// Scan 5's case: just off a bull, still plainly a shot at it. Before this, these were dropped with no candidate and nothing said.
    /// </summary>
    [Fact]
    public void AShotThatMissedItsBullIsKept()
    {
        // Above the first bull, past the top of its cell but within a bull's width of it.
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(300, 120, Cells()));

        // To the left of the first bull, likewise.
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(120, 300, Cells()));

        // And diagonally off a corner of the cell.
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(140, 140, Cells()));
    }

    /// <summary>
    /// The half that protects the measurement: out in the margins is still refused, because that is where the survey's false positives were
    /// and no shot aimed at the grid lands there.
    /// </summary>
    [Fact]
    public void AMarkOutInTheMarginsIsStillRefused()
    {
        // Far above the row, at the top edge of a sheet.
        Assert.False(OutsideTheGrid.NearEnoughToBeAMissedShot(300, -400, Cells()));

        // Far to the left, in the margin.
        Assert.False(OutsideTheGrid.NearEnoughToBeAMissedShot(-500, 300, Cells()));

        // And far below every cell.
        Assert.False(OutsideTheGrid.NearEnoughToBeAMissedShot(900, 1400, Cells()));
    }

    /// <summary>The boundary is a bull's width past the cell, stated rather than implied, so a change to it is a decision.</summary>
    [Fact]
    public void TheBoundaryIsABullsWidthPastTheCell()
    {
        var cells = Cells();
        double edge = 300 - 120 - (120 * OutsideTheGrid.NearTheGrid);

        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(edge + 10, 300, cells));
        Assert.False(OutsideTheGrid.NearEnoughToBeAMissedShot(edge - 10, 300, cells));
    }

    /// <summary>
    /// A mark between two bulls is near both, which is the ordinary case on a grid and must not be refused by an off-by-one in the
    /// arithmetic.
    /// </summary>
    [Fact]
    public void AMarkBetweenTwoBullsIsNearBoth()
    {
        Assert.True(OutsideTheGrid.NearEnoughToBeAMissedShot(600, 300, Cells()));
    }

    /// <summary>What a kept one says, because the point is that the shooter sees it rather than it being counted quietly.</summary>
    [Fact]
    public void AKeptOneIsPutInFrontOfTheShooter()
    {
        Assert.Contains("landed off the bulls", OutsideTheGrid.Says, StringComparison.Ordinal);
        Assert.Contains("no bull of its own until you give it one", OutsideTheGrid.Says, StringComparison.Ordinal);

        // And there is a way to say it is not a shot, so a smudge that gets through costs one click rather than a wrong figure.
        Assert.Contains("not a shot", OutsideTheGrid.Says, StringComparison.Ordinal);
    }

    /// <summary>A sheet with no cells at all refuses everything rather than accepting everything.</summary>
    [Fact]
    public void NoCellsMeansNothingIsNearTheGrid()
    {
        Assert.False(OutsideTheGrid.NearEnoughToBeAMissedShot(300, 300, []));
    }
}
