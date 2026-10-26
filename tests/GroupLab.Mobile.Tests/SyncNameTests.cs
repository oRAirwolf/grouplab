namespace GroupLab.Mobile.Tests;

/// <summary>Entry 474: a phone is first named in the sync folder by its maker and model, not by the diagnostics line with the version in it.</summary>
public class SyncNameTests
{
    [Theory]
    [InlineData("GroupLab 0.2.0-nightly.221+70a3b0d8 on samsung SM-F966U", "samsung SM-F966U")]
    [InlineData("GroupLab 0.2.0+8fee20cf on Google sdk_gphone64_x86_64", "Google sdk_gphone64_x86_64")]
    [InlineData("GroupLab 0.2.0 on iPhone 16", "iPhone 16")]
    [InlineData("A phone with no version", "A phone with no version")]
    [InlineData("GroupLab 0.2.0 on ", "GroupLab 0.2.0 on ")]
    [InlineData("", "This phone")]
    [InlineData(null, "This phone")]
    public void APhoneIsNamedByItsMakerAndModel(string? words, string name) => Assert.Equal(name, PhoneSync.DefaultName(words));
}
