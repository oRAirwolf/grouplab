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
}
