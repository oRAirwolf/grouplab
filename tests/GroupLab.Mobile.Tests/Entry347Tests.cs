using GroupLab.Core.Updates;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 347 on the phones: the store-bought fingerprint library is looked for only on an unmetered connection, with
/// the battery and the storage not low, and at most every six hours, entry 343's rules for GroupLab Dev's update check, on every copy.
/// </summary>
public class Entry347Tests
{
    [Fact]
    public async Task TheLibraryIsLookedForOnlyWhenEntry343sRulesAllow()
    {
        if (Phone.Platform is null)
        {
            Phone.Start(new TestPhone(), Avalonia.Application.Current ?? new Avalonia.Application(), () => "US", null);
        }

        var phone = (TestPhone)Phone.Platform!;
        var outside = new RecordedOutsideWorld();
        var now = new DateTimeOffset(2026, 10, 2, 6, 0, 0, TimeSpan.Zero);
        string stamp = Path.Combine(Phone.LibraryFolder, "checked");
        Directory.CreateDirectory(Phone.LibraryFolder);
        File.Delete(stamp);
        try
        {
            (phone.Unmetered, phone.BatteryLow, phone.StorageLow) = (null, null, null);
            Assert.Equal("not on an unmetered connection", await Phone.RefreshLibraryAsync(now, outside));
            phone.Unmetered = false;
            Assert.Equal("not on an unmetered connection", await Phone.RefreshLibraryAsync(now, outside));

            phone.Unmetered = true;
            Assert.Equal("the battery or the storage is low, or the phone cannot say", await Phone.RefreshLibraryAsync(now, outside));
            phone.BatteryLow = false;
            phone.StorageLow = true;
            Assert.Equal("the battery or the storage is low, or the phone cannot say", await Phone.RefreshLibraryAsync(now, outside));
            Assert.Empty(outside.Asked);

            // Allowed: it looks, and finds nothing on this recorder; then not again for six hours.
            phone.StorageLow = false;
            Assert.Equal("no library could be reached", await Phone.RefreshLibraryAsync(now, outside));
            Assert.Equal("looked within six hours", await Phone.RefreshLibraryAsync(now.AddHours(5), outside));
            Assert.Equal("no library could be reached", await Phone.RefreshLibraryAsync(now.AddHours(7), outside));
        }
        finally
        {
            (phone.Unmetered, phone.BatteryLow, phone.StorageLow) = (null, null, null);
            File.Delete(stamp);
        }
    }
}
