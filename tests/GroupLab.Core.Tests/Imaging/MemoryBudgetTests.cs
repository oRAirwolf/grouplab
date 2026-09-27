using GroupLab.Core.Imaging;

namespace GroupLab.Core.Tests.Imaging;

/// <summary>NOTES-FROM-PLANNING.md entry 240: a budget from what the device says it has, which keeps every phone at 8 megapixels.</summary>
public class MemoryBudgetTests
{
    [Theory]
    // total, available, threshold, low, the budget docs/ANDROID.md works out
    [InlineData(4096, 1800, 200, false, 400)]
    [InlineData(8192, 3600, 300, false, 819.2)]
    [InlineData(12288, 5400, 400, false, 1228.8)]
    [InlineData(16384, 7200, 500, false, 1638.4)]
    [InlineData(4096, 900, 200, false, 400)]
    [InlineData(16384, 7200, 500, true, 400)]
    public void TheBudgetIsAShareOfWhatIsAvailableBetweenTheFloorAndTheCeiling(double total, double available, double threshold, bool low, double expected)
    {
        Assert.Equal(expected, MemoryBudget.Phone(total, available, threshold, low), 1);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(1638.4)]
    public void EveryPhoneWorksAtEightMegapixels(double budget) => Assert.Equal(WorkingSize.PhoneMegapixels, MemoryBudget.PhoneWorkingMegapixels(budget));

    [Theory]
    [InlineData(65536, 400)]
    [InlineData(16384, 400)]
    [InlineData(8192, 269.2)]
    [InlineData(2048, 60)]
    public void ADesktopReadsWholeWhatHalfItsMemoryHolds(double total, double expected)
    {
        Assert.Equal(expected, MemoryBudget.DesktopMostMegapixels(total, 400), 1);
    }
}
