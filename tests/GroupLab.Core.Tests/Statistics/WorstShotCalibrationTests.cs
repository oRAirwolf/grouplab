using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Statistics;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 104 section 2: the flyer card judged a worst shot measured against the group's own mean radius about its own
/// centre by a closed form that assumes the true ones. These hold the simulated calibration that replaced it to the statistic the screen
/// computes, and to an independent simulation of it.
/// </summary>
public class WorstShotCalibrationTests
{
    /// <summary>The calibration's statistic is the one the report quotes, so the card compares like with like.</summary>
    [Fact]
    public void TheStatisticIsTheOneTheReportQuotes()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(0, 0));
        PointD[] inches = [new(0.10, 0.02), new(-0.05, 0.11), new(0.03, -0.12), new(-0.09, -0.04), new(0.32, 0.08), new(-0.02, 0.05)];
        foreach (var p in inches)
        {
            session.AddShot(new PointD(100 * p.X, 100 * p.Y));
        }

        var figures = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.Equal(figures.WorstShotInMeanRadii!.Value, Flyers.WorstInSampleMeanRadii(inches), 9);
        Assert.Equal(figures.WorstShotInMeanRadii!.Value, figures.WorstShot!.Observed, 12);
        Assert.Equal(inches.Length, figures.WorstShot.Shots);
    }

    /// <summary>
    /// The five percent points from an independent simulation of 200,000 groups a count (Python, seed 7, the same statistic): 1.733 at five
    /// shots and 2.623 at twenty-five. The calibration puts about one group in twenty past each.
    /// </summary>
    [Theory]
    [InlineData(5, 1.733)]
    [InlineData(25, 2.623)]
    public void TheCalibrationPutsOneGroupInTwentyPastTheIndependentFivePercentPoint(int shots, double cut)
    {
        var calibrated = Flyers.CalibrateWorst(shots, cut, resamples: 100_000, seed: 104);
        Assert.InRange(calibrated.PValue, 0.045, 0.055);
    }

    /// <summary>
    /// Why the closed form could not stand at small counts: about its own centre, the worst of five shots is at most sqrt(4 / 5) of the root sum
    /// of squared radii, about 1.96 of the group's own mean radii, and the closed form's five percent line is at 2.42, which no group reaches.
    /// </summary>
    [Fact]
    public void AtFiveShotsTheClosedFormsLineIsBeyondAnyGroup()
    {
        const int n = 5;
        double closedCut = 2.4;
        Assert.True(Flyers.ProbabilityWorstBeyond(n, closedCut) > 0.05);

        // Four shots at the centre and one far out is as extreme as five shots can be.
        PointD[] extreme = [new(0, 0), new(0, 0), new(0, 0), new(0, 0), new(1, 0)];
        double ceiling = Flyers.WorstInSampleMeanRadii(extreme);
        Assert.InRange(ceiling, 1.9, 2.0);
        Assert.Equal(1.0 / 10_000, Flyers.CalibrateWorst(n, closedCut).PValue, 12);
    }

    [Fact]
    public void TheSameGroupAlwaysReadsTheSame() =>
        Assert.Equal(Flyers.CalibrateWorst(12, 2.1).PValue, Flyers.CalibrateWorst(12, 2.1).PValue);
}
