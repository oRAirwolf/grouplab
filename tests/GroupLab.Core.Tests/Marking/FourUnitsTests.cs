using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 3.1: the zero correction in MOA, mil, inches and centimetres at once, with the scope's own
/// clicks where the rifle records them.
/// <para>
/// <b>Showing all four is the honest answer rather than indecision.</b> A scope adjusts in one of two angular units and a person measures in
/// one of two linear ones, and which pair somebody thinks in depends on the scope they own and where they grew up. Making them convert in
/// their head, at the range, from a number they are about to dial into a turret, is where mistakes come from.
/// </para>
/// </summary>
public class FourUnitsTests
{
    /// <summary>
    /// The conversions themselves, against the values a person can check: one MOA is 1.047 in at 100 yards and one mil is 3.6 in, so ten
    /// inches at 100 yards is about 9.5 MOA and about 2.8 mil.
    /// </summary>
    [Fact]
    public void TenInchesAtAHundredYardsIsTheFamiliarNumbers()
    {
        var four = FourUnits.Of(10, 100);

        Assert.Equal(9.55, four.Moa, 2);
        Assert.Equal(2.78, four.Mil, 2);
        Assert.Equal(10, four.Inches, 6);
        Assert.Equal(25.4, four.Centimetres, 6);
    }

    /// <summary>
    /// An angle is the same whatever the distance, which is the whole reason a scope is marked in one. The same 1 MOA correction is 1.05 in
    /// at 100 yards and 2.09 in at 200.
    /// </summary>
    [Fact]
    public void TheAngleIsTheSameAtEveryDistance()
    {
        var near = FourUnits.Of(FourUnits.MoaInchesPerHundredYards, 100);
        var far = FourUnits.Of(FourUnits.MoaInchesPerHundredYards * 2, 200);

        Assert.Equal(1.0, near.Moa, 6);
        Assert.Equal(1.0, far.Moa, 6);
        Assert.NotEqual(near.Inches, far.Inches, 6);
    }

    /// <summary>
    /// With no distance there is no angle, and a wrong angle is worse than none, because it is the one a person would dial. The linear
    /// figures still stand, since they do not depend on the distance being known.
    /// </summary>
    [Fact]
    public void WithoutADistanceTheAnglesAreNotInvented()
    {
        var four = FourUnits.Of(3, 0);

        Assert.True(double.IsNaN(four.Moa));
        Assert.True(double.IsNaN(four.Mil));
        Assert.Equal(3, four.Inches, 6);
        Assert.Equal(7.62, four.Centimetres, 2);
    }

    [Fact]
    public void TheScopesOwnUnitLeadsAndTheRestFollow()
    {
        Assert.Equal("mil", FourUnits.Headline("mil"));
        Assert.Equal("mil", FourUnits.Headline("MRAD"));
        Assert.Equal("moa", FourUnits.Headline("MOA"));

        // No record means MOA, the commoner marking on the scopes this project has seen, rather than nothing at all.
        Assert.Equal("moa", FourUnits.Headline(null));
        Assert.Equal("moa", FourUnits.Headline("  "));

        Assert.Equal(["moa", "in", "cm"], FourUnits.Beneath("mil"));
        Assert.Equal(["mil", "in", "cm"], FourUnits.Beneath("moa"));
    }

    /// <summary>"Up 8 clicks at 0.1 mil" is what a person actually does, so it is spelled out rather than left as an angle to divide.</summary>
    [Fact]
    public void TheClicksAreSpelledOut()
    {
        // 2.78 mil at 0.1 mil a click is 28 clicks.
        Assert.Equal("28 clicks at 0.1 mil", FourUnits.Clicks(10, 100, "mil", 0.1));

        // 9.55 MOA at a quarter minute a click is 38.
        Assert.Equal("38 clicks at 0.25 MOA", FourUnits.Clicks(10, 100, "moa", 0.25));

        // And one click reads as one, not "1 clicks".
        Assert.Equal("1 click at 1 MOA", FourUnits.Clicks(FourUnits.MoaInchesPerHundredYards, 100, "moa", 1));
    }

    /// <summary>
    /// A click value is never guessed. A scope adjusting in quarter minutes and one adjusting in tenth mils are both common, and assuming
    /// either would send somebody a long way in the wrong direction.
    /// </summary>
    [Fact]
    public void ClicksAreNeverGuessed()
    {
        Assert.Null(FourUnits.Clicks(10, 100, "mil", null));
        Assert.Null(FourUnits.Clicks(10, 100, "mil", 0));
        Assert.Null(FourUnits.Clicks(10, 0, "mil", 0.1));
    }

    /// <summary>A correction too small to dial says so, rather than rounding to zero clicks and looking like nothing to do.</summary>
    [Fact]
    public void ACorrectionTooSmallToDialSaysSo()
    {
        Assert.Equal("less than one click", FourUnits.Clicks(0.01, 100, "mil", 0.1));
    }

    /// <summary>The wording the table uses, so its four rows cannot disagree with each other.</summary>
    [Fact]
    public void EachRowIsWrittenTheSameWay()
    {
        var four = FourUnits.Of(10, 100);

        Assert.Equal("9.5 MOA", four.Say("moa"));
        Assert.Equal("2.78 mil", four.Say("mil"));
        Assert.Equal("10.00 in", four.Say("in"));
        Assert.Equal("25.4 cm", four.Say("cm"));
    }
}
