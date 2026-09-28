using GroupLab.Core.Marking;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>NOTES-FROM-PLANNING.md entries 272 and 273: tapping a number switches its kind's unit, and press and hold lists them all.</summary>
public class UnitSwitchTests
{
    [Fact]
    public void ATapSwitchesOnlyItsOwnKind()
    {
        var units = UnitSettings.Imperial;
        var angle = UnitSwitch.Switch(units, UnitKind.Angle);
        Assert.Equal(units with { Angular = AngularUnit.Mrad }, angle);
        Assert.Equal(units, UnitSwitch.Switch(angle, UnitKind.Angle));
        Assert.Equal(units with { Linear = LinearUnit.Centimetre }, UnitSwitch.Switch(units, UnitKind.Length));
        Assert.Equal(units with { Distance = DistanceUnit.Metre }, UnitSwitch.Switch(units, UnitKind.Distance));

        // A unit only press and hold reaches goes back to the pair's first on a tap.
        Assert.Equal(AngularUnit.Mrad, UnitSwitch.Switch(units with { Angular = AngularUnit.Smoa }, UnitKind.Angle).Angular);
        Assert.Equal(LinearUnit.Inch, UnitSwitch.Switch(units with { Linear = LinearUnit.Millimetre }, UnitKind.Length).Linear);
    }

    [Fact]
    public void PressAndHoldListsEveryUnitAndMarksTheCurrentOne()
    {
        var angles = UnitSwitch.Choices(UnitSettings.Imperial, UnitKind.Angle);
        Assert.Equal(["MOA", "mil", "SMOA"], angles.Select(c => c.Symbol));
        Assert.Single(angles, c => c.Current);
        Assert.Equal(["in", "cm", "mm"], UnitSwitch.Choices(UnitSettings.Imperial, UnitKind.Length).Select(c => c.Symbol));
        Assert.Equal(["yd", "m"], UnitSwitch.Choices(UnitSettings.Metric, UnitKind.Distance).Select(c => c.Symbol));
    }

    [Fact]
    public void TheNoteSaysWhatChanged()
    {
        Assert.Equal("Angles now in mil everywhere", UnitSwitch.Said(UnitSettings.Metric, UnitKind.Angle));
        Assert.Equal("Sizes now in centimeters everywhere", UnitSwitch.Said(UnitSettings.Metric, UnitKind.Length));
        Assert.Equal("Distances now in yards everywhere", UnitSwitch.Said(UnitSettings.Imperial, UnitKind.Distance));
        Assert.Equal("Tap a number to switch units", UnitSwitch.Hint);
    }
}
