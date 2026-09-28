using GroupLab.App;
using GroupLab.Core.Marking;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 232: the Play build on a US Fold 7 asked for the distance in meters, because the phone runs with invariant
/// globalization and .NET could not see its region. The phone now says its own region, and a store with no units chosen follows it.
/// </summary>
[Collection("settings region")]
public class Entry232Tests
{
    [Theory]
    [InlineData("US", LinearUnit.Inch)]
    [InlineData("DE", LinearUnit.Centimetre)]
    public void AStoreWithNoUnitsFollowsTheRegionThePhoneGives(string region, LinearUnit expected)
    {
        string root = Path.Combine(Path.GetTempPath(), $"grouplab-entry232-{Guid.NewGuid():N}");
        AppSettingsStore.RegionSource = () => region;
        try
        {
            var store = new AppSettingsStore(Path.Combine(root, "settings.json"));
            Assert.Equal(expected, store.LoadUnits().Linear);
        }
        finally
        {
            AppSettingsStore.RegionSource = null;
            GroupLab.Tests.Support.Temp.Delete(root);
        }
    }

    /// <summary>Entry 246: the phone's Targets screen offers the paper of the phone's region, Letter in the United States and A4 in Britain.</summary>
    [Theory]
    [InlineData("US", true)]
    [InlineData("ca", true)]
    [InlineData("GB", false)]
    [InlineData("DE", false)]
    [InlineData(null, false)]
    public void ThePaperFollowsTheRegionThePhoneGives(string? region, bool letter)
    {
        AppSettingsStore.RegionSource = () => region;
        try
        {
            Assert.Equal(letter, AppSettingsStore.LetterRegion(region));
            if (region is not null)
            {
                Assert.Equal(region, AppSettingsStore.Region());
            }
        }
        finally
        {
            AppSettingsStore.RegionSource = null;
        }
    }
}
