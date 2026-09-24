using GroupLab.Core.Detection;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 162 section 3.1: where a calibre is still used without the sheet's own marks, it holds across the whole
/// range a hole has measured over its bullet, 0.76 to 1.14, and says so.
/// <para>
/// The calibre does two things now and both are held here. It sets the smallest mark accepted as a hole, which must sit below the smallest
/// hole ever measured, or a real hole on thin paper is thrown away. And it keeps a single hole from being split, which must hold for the
/// largest hole ever measured, or a real hole on card stock becomes two shots nobody fired.
/// </para>
/// </summary>
public class CalibreRangeTests
{
    [Fact]
    public void TheSmallestHoleAcceptedIsBelowTheSmallestHoleMeasured()
    {
        const double bullet = 0.264;
        double floor = HoleSizeGate.MinimumDiameter(bullet * AutomaticMarking.HoleToCalibre, HoleSizeGate.WithoutACalibreInches);

        Assert.True(floor < bullet * AutomaticMarking.HoleToCalibreLow,
            $"a {bullet} in bullet's smallest accepted hole is {floor:0.000} in, and holes have measured as small as "
            + $"{bullet * AutomaticMarking.HoleToCalibreLow:0.000} in: a real hole on thin paper would be thrown away.");
    }

    [Fact]
    public void ASingleHoleAtTheLargestRatioMeasuredIsNotSplit()
    {
        // The veto keeps a mark whole while its area is under SplitMinimumHoles holes of the calibre's reference size.
        double reference = AutomaticMarking.HoleToCalibre;
        double largest = AutomaticMarking.HoleToCalibreHigh;
        double holes = (largest / reference) * (largest / reference);

        Assert.True(holes < new RenderDifferenceOptions().SplitMinimumHoles,
            $"a single hole at {largest} of the bullet is {holes:0.00} of the calibre's holes by area, and a mark is split from "
            + $"{new RenderDifferenceOptions().SplitMinimumHoles:0.0}: one real hole on card stock would become two shots.");
    }

    [Fact]
    public void TheRangeIncludesBothEndsThatWereMeasured()
    {
        Assert.True(AutomaticMarking.HoleToCalibreLow <= 0.765 && AutomaticMarking.HoleToCalibreHigh >= 1.14,
            "the range must include the .22 LR scan at 0.765 and the friend's card stock at 1.14.");
        Assert.InRange(AutomaticMarking.HoleToCalibre, AutomaticMarking.HoleToCalibreLow, AutomaticMarking.HoleToCalibreHigh);
    }
}
