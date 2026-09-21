using GroupLab.Core.Detection;

namespace GroupLab.Core.Tests.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.3, from what entry 120 found on scan 4 of the second range day.
/// <para>
/// <b>The harm.</b> Five real .22 LR holes, measuring 0.11 to 0.14 in, were refused as "too small". Alan had shot the sheet and could see
/// them. Naming the calibre did not help, because the gate was a fixed 0.15 in and nothing read the calibre.
/// </para>
/// <para>
/// The mistake was assuming a hole is about as wide as the bullet. Paper stretches ahead of a bullet and closes behind it, so the hole is
/// reliably narrower than the bullet, and the smallest of those five was 0.49 of its calibre. These hold that a named calibre opens the gate
/// far enough for that, and no further.
/// </para>
/// </summary>
public class HoleSizeGateTests
{
    /// <summary>A .224 in bullet, which is what .22 LR and .223 both measure.</summary>
    private const double TwentyTwo = 0.224;

    /// <summary>The five holes scan 4 refused, in inches, as entry 120 reported them.</summary>
    private static readonly double[] TheRefusedFive = [0.11, 0.12, 0.13, 0.135, 0.14];

    [Fact]
    public void TheHolesScanFourRefusedAreAdmittedWhenTheCalibreIsNamed()
    {
        double floor = HoleSizeGate.MinimumDiameter(TwentyTwo, HoleSizeGate.WithoutACalibreInches);

        foreach (double hole in TheRefusedFive)
        {
            Assert.True(hole >= floor, $"a {hole:0.000} in hole from a {TwentyTwo:0.000} in bullet is still refused by a {floor:0.000} in floor");
        }
    }

    [Fact]
    public void WithoutACalibreTheOldFloorStands()
    {
        // Nothing better can be said with no calibre, so nothing different is said.
        Assert.Equal(HoleSizeGate.WithoutACalibreInches, HoleSizeGate.MinimumDiameter(null, HoleSizeGate.WithoutACalibreInches));
        Assert.Equal(HoleSizeGate.WithoutACalibreInches, HoleSizeGate.MinimumDiameter(0, HoleSizeGate.WithoutACalibreInches));
    }

    /// <summary>
    /// The gate still has to be a gate. A speck of dust, a paper fibre and a scanner artefact are all far smaller than any hole, and the
    /// point of opening the floor for .22 was not to open it for those.
    /// </summary>
    [Fact]
    public void SpecksAndFibresAreStillRefused()
    {
        double floor = HoleSizeGate.MinimumDiameter(TwentyTwo, HoleSizeGate.WithoutACalibreInches);

        foreach (double speck in new[] { 0.004, 0.01, 0.02, 0.04 })
        {
            Assert.True(speck < floor, $"a {speck:0.000} in speck would be taken for a hole");
        }

        // At 600 dpi the absolute floor is this many pixels across, which no fibre reaches.
        Assert.True(HoleSizeGate.AbsoluteFloorInches * 600 >= 30);
    }

    /// <summary>A nonsense calibre must not open the gate to everything, which is what the absolute floor is for.</summary>
    [Fact]
    public void AnAbsurdlySmallCalibreCannotOpenTheGate()
    {
        Assert.Equal(HoleSizeGate.AbsoluteFloorInches, HoleSizeGate.MinimumDiameter(0.001, HoleSizeGate.WithoutACalibreInches));
        Assert.Equal(HoleSizeGate.AbsoluteFloorInches, HoleSizeGate.MinimumDiameter(0.05, HoleSizeGate.WithoutACalibreInches));
    }

    /// <summary>A larger bullet raises the floor with it, so naming a calibre is never a way to let more through than it should.</summary>
    [Fact]
    public void ALargerBulletRaisesTheFloor()
    {
        double small = HoleSizeGate.MinimumDiameter(TwentyTwo, HoleSizeGate.WithoutACalibreInches);
        double medium = HoleSizeGate.MinimumDiameter(0.308, HoleSizeGate.WithoutACalibreInches);
        double large = HoleSizeGate.MinimumDiameter(0.458, HoleSizeGate.WithoutACalibreInches);

        Assert.True(small < medium);
        Assert.True(medium < large);

        // And a hole that has closed as far as scan 4's would still be admitted at those calibres.
        Assert.True(0.308 * 0.49 >= medium);
        Assert.True(0.458 * 0.49 >= large);
    }

    /// <summary>What a person is told is the number that was applied, so a refusal can be argued with.</summary>
    [Fact]
    public void TheRefusalSaysWhichFloorWasUsedAndWhy()
    {
        string named = HoleSizeGate.Describe(TwentyTwo, HoleSizeGate.WithoutACalibreInches);
        Assert.Contains("0.101 in", named, StringComparison.Ordinal);
        Assert.Contains("0.224 in bullet named", named, StringComparison.Ordinal);

        string unnamed = HoleSizeGate.Describe(null, HoleSizeGate.WithoutACalibreInches);
        Assert.Contains("0.150 in", unnamed, StringComparison.Ordinal);
        Assert.Contains("no calibre named", unnamed, StringComparison.Ordinal);
    }

    /// <summary>
    /// The options carry the calibre now, so a detector run with one gates differently from the same detector run without. Entry 80 section
    /// 5 already says two markings are only comparable when their detection records agree, and this is why.
    /// </summary>
    [Fact]
    public void TheDetectorOptionsCarryTheCalibre()
    {
        var without = new HoleDetectionOptions();
        var with = new HoleDetectionOptions(CalibreInches: TwentyTwo);

        Assert.Null(without.CalibreInches);
        Assert.Equal(TwentyTwo, with.CalibreInches);
        Assert.True(
            HoleSizeGate.MinimumDiameter(with.CalibreInches, with.MinimumDiameterInches)
            < HoleSizeGate.MinimumDiameter(without.CalibreInches, without.MinimumDiameterInches));
    }
}
