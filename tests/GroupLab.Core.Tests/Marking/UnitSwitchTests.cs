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
        Assert.Equal(["mil", "MOA", "SMOA"], angles.Select(c => c.Symbol));
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

    /// <summary>Entry 280 section 1: a tap switches the one number tapped, from what it shows, a range as a whole.</summary>
    [Fact]
    public void OneNumberIsConvertedFromWhatItShows()
    {
        Assert.Equal("1.12 cm", UnitSwitch.Convert("0.442 in", UnitKind.Length, "cm"));
        Assert.Equal("0.442 in", UnitSwitch.Convert("1.123 cm", UnitKind.Length, "in"));
        Assert.Equal("0.29 mil", UnitSwitch.Convert("1.00 MOA", UnitKind.Angle, "mil"));
        Assert.Equal("3.44 MOA", UnitSwitch.Convert("1.00 mil", UnitKind.Angle, "MOA"));
        Assert.Equal("91 m", UnitSwitch.Convert("100 yd", UnitKind.Distance, "m"));
        Assert.Equal("95% interval 0.28 to 0.42 cm", UnitSwitch.Convert("95% interval 0.110 to 0.166 in", UnitKind.Length, "cm"));
        // Only numbers of the kind tapped change: the angle stays, the size switches.
        Assert.Equal("0.42 MOA  ·  1.12 cm", UnitSwitch.Convert("0.42 MOA  ·  0.442 in", UnitKind.Length, "cm"));
        Assert.Equal("cm", UnitSwitch.Next(UnitKind.Length, "in"));
        Assert.Equal("MOA", UnitSwitch.Next(UnitKind.Angle, "SMOA"));
        Assert.Equal("in", UnitSwitch.SymbolIn("0.42 MOA  ·  0.442 in", UnitKind.Length));
    }
}
