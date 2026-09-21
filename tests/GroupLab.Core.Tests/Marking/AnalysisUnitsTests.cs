using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 3.2: a metric and imperial toggle on the analysis page, defaulting from the Settings units,
/// remembering the choice, and never changing stored data.
/// <para>
/// <b>The last clause is the one worth testing.</b> A marking holds inches because that is what it was measured in. Somebody switching the
/// page to centimetres is asking to read it differently, not to convert it, and a toggle that rewrote the marking would quietly make two
/// people's copies of one session disagree about what was measured.
/// </para>
/// </summary>
public class AnalysisUnitsTests
{
    [Fact]
    public void WithNoChoiceItFollowsTheSettings()
    {
        Assert.Equal(UnitSettings.Imperial, UnitSettings.ForAnalysis(null, UnitSettings.Imperial));
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForAnalysis(null, UnitSettings.Metric));
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForAnalysis("   ", UnitSettings.Metric));
    }

    [Fact]
    public void AChoiceOnThePageWinsOverTheSettings()
    {
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForAnalysis("metric", UnitSettings.Imperial));
        Assert.Equal(UnitSettings.Imperial, UnitSettings.ForAnalysis("imperial", UnitSettings.Metric));
    }

    /// <summary>Switching twice comes back to where it started, which is what a toggle has to do to be a toggle.</summary>
    [Fact]
    public void PressingItTwiceReturnsToWhereItStarted()
    {
        var settings = UnitSettings.Imperial;

        string first = UnitSettings.Other(null, settings);
        Assert.Equal("metric", first);

        string second = UnitSettings.Other(first, settings);
        Assert.Equal("imperial", second);

        Assert.Equal(UnitSettings.ForAnalysis(null, settings), UnitSettings.ForAnalysis(second, settings));
    }

    /// <summary>And from metric settings it starts the other way round, because the default is the person's own choice.</summary>
    [Fact]
    public void FromMetricSettingsTheToggleStartsTheOtherWay()
    {
        Assert.Equal("imperial", UnitSettings.Other(null, UnitSettings.Metric));
    }

    /// <summary>
    /// Imperial means inches with MOA and metric means centimetres with mil, which is section 3.2's wording rather than a free mix of the
    /// four: a person thinking in centimetres is not usually thinking in minutes of angle.
    /// </summary>
    [Fact]
    public void EachPairIsTheOneAPersonActuallyUses()
    {
        var imperial = UnitSettings.ForAnalysis("imperial", UnitSettings.Metric);
        Assert.Equal(LinearUnit.Inch, imperial.Linear);
        Assert.Equal(AngularUnit.Moa, imperial.Angular);
        Assert.Equal(DistanceUnit.Yard, imperial.Distance);

        var metric = UnitSettings.ForAnalysis("metric", UnitSettings.Imperial);
        Assert.Equal(LinearUnit.Centimetre, metric.Linear);
        Assert.Equal(AngularUnit.Mrad, metric.Angular);
        Assert.Equal(DistanceUnit.Metre, metric.Distance);
    }

    /// <summary>
    /// Something unrecognised falls back to the settings rather than to a guess. A settings file edited by hand, or written by a newer
    /// build, must not leave somebody reading their groups in a unit nobody chose.
    /// </summary>
    [Fact]
    public void SomethingUnrecognisedFallsBackToTheSettings()
    {
        Assert.Equal(UnitSettings.Imperial, UnitSettings.ForAnalysis("furlongs", UnitSettings.Imperial));
        Assert.Equal(UnitSettings.Metric, UnitSettings.ForAnalysis("IMPERIALISH", UnitSettings.Metric));
    }
}
