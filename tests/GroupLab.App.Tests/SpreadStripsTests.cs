using GroupLab.App;
using GroupLab.Core.Imaging;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 141 section 5.2.2: is my group wider than it is tall?
/// <para>
/// <b>The caption is the test, not the picture.</b> Every group is lopsided one way or the other; none is ever exactly square. So a picture
/// of two spreads, on its own, invites a person to read wind or a bipod into the ordinary lopsidedness of a handful of shots. What this
/// holds is that the words never say a group is taller than it is wide without saying whether the shots can tell.
/// </para>
/// </summary>
public class SpreadStripsTests
{
    private static SpreadStrips Strips(double acrossSd, double upDownSd, double? p, int shots = 12)
    {
        var offsets = Enumerable.Range(0, shots)
            .Select(k => new PointD(acrossSd * Math.Cos(2 * Math.PI * k / shots), upDownSd * Math.Sin(2 * Math.PI * k / shots)))
            .ToList();
        return new SpreadStrips { Offsets = offsets, AcrossSd = acrossSd, UpDownSd = upDownSd, RoundPValue = p };
    }

    [Fact]
    public void ALopsidedGroupTheShotsCannotSeparateSaysSo()
    {
        var strips = Strips(0.16, 0.20, p: 0.78);

        Assert.Contains("taller than it is wide", strips.Description, StringComparison.Ordinal);
        Assert.Contains("cannot tell that from an ordinary round group", strips.Description, StringComparison.Ordinal);
        Assert.Contains("12 shots", strips.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void ALopsidedGroupTheShotsCanSeparateSaysThatInstead()
    {
        var strips = Strips(0.40, 0.12, p: 0.004);

        Assert.Contains("wider than it is tall", strips.Description, StringComparison.Ordinal);
        Assert.Contains("are enough to say so", strips.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("cannot tell", strips.Description, StringComparison.Ordinal);
    }

    /// <summary>With no circularity test there is no claim to make, and it says that rather than falling back to the picture.</summary>
    [Fact]
    public void WithNoTestItClaimsNothing()
    {
        var strips = Strips(0.30, 0.10, p: null, shots: 3);

        Assert.Contains("Too few shots to say whether that difference is real", strips.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("enough to say so", strips.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void WithNothingToCompareItSaysSo()
    {
        Assert.Equal("Not enough shots to compare the two spreads.", new SpreadStrips().Description);
        Assert.Equal("Not enough shots to compare the two spreads.", new SpreadStrips { Offsets = [new PointD(0, 0)] }.Description);
    }

    /// <summary>Both figures are in the person's own units, which is what the analysis passes in.</summary>
    [Fact]
    public void BothSpreadsAreShownInTheUnitsInForce()
    {
        var strips = Strips(0.16, 0.20, p: 0.78);
        strips.Length = inches => $"{inches * 25.4:0.0} mm";

        Assert.Contains("Across 4.1 mm, up and down 5.1 mm.", strips.Description, StringComparison.Ordinal);
    }
}
