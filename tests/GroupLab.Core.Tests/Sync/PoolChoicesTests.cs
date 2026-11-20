using GroupLab.Core.Records;
using GroupLab.Core.Sync;

namespace GroupLab.Core.Tests.Sync;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 504 section 1, concept A: the choice between two versions of a pooled set, shared by the computer and the
/// phone. The three tags, the count of differences (only the amber items), and a sheet not here yet never counting by itself.
/// </summary>
public sealed class PoolChoicesTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private static SessionRecord Sheet(long id, string date) =>
        new(id, date + "T15:00:00Z", date, "Sheet", null, null, null, "6.5 Creedmoor", null, "41.5 gr", null, "{}", 5, null, null, null, null, null, null, null);

    private static PoolRecord Pool(IReadOnlyList<string> sheets, bool pictures = false) =>
        new(1, "k", "2026-11-19T14:05:00Z", sheets, SessionPools.Choices(pictures));

    [Fact]
    public void SheetsInBothAreSameAndNothingDiffers()
    {
        var a = Sheet(1, "2026-11-19");
        var b = Sheet(2, "2026-11-19");
        var choice = PoolChoices.Build(Pool(["a", "b"]), [a, b], Pool(["a", "b"]), [a, b], "Desk", "Pixel", "2026-11-19T16:00:00Z", phone: false, Utc);
        Assert.Equal(0, choice.Differences);
        Assert.All(choice.Here.Sheets.Concat(choice.There.Sheets), s => Assert.Equal(PoolSheetTag.Same, s.Tag));
        Assert.False(choice.Here.PooledAsDiffers);
        Assert.Equal("This computer (Desk)", choice.Here.Device);
        Assert.Equal("made 19 November, 14:05", choice.Here.When);
        Assert.Equal("arrived 19 November, 16:00", choice.There.When);
        Assert.Equal("same", PoolChoices.Words(PoolSheetTag.Same));
    }

    [Fact]
    public void ASheetInOneVersionOnlyIsAmberAndCounted()
    {
        var a = Sheet(1, "2026-11-19");
        var b = Sheet(2, "2026-11-19");
        var c = Sheet(3, "2026-11-19");
        var choice = PoolChoices.Build(Pool(["a", "b", "c"]), [a, b, c], Pool(["a", "b"], pictures: true), [a, b], "Desk", "Pixel", "2026-11-19T16:00:00Z", phone: false, Utc);
        Assert.Equal(PoolSheetTag.OnlyInThisOne, choice.Here.Sheets[2].Tag);
        Assert.Equal("only in this one", PoolChoices.Words(choice.Here.Sheets[2].Tag));
        Assert.All(choice.There.Sheets, s => Assert.Equal(PoolSheetTag.Same, s.Tag));
        Assert.True(choice.Here.PooledAsDiffers && choice.There.PooledAsDiffers);
        Assert.Equal(PoolChoices.AsSet, choice.Here.PooledAs);
        Assert.Equal(PoolChoices.AsPictures, choice.There.PooledAs);
        Assert.Equal(2, choice.Differences);
        Assert.Equal("6.5 Creedmoor, 41.5 gr, 19 November. 2 differences: how the sheets were pooled, and sheet 3. Both versions are kept until you choose.", choice.Line);
    }

    [Fact]
    public void ASheetNotHereYetIsNeverCountedOrAmber()
    {
        var a = Sheet(1, "2026-11-19");
        var b = Sheet(2, "2026-11-19");
        // The other version names a third sheet that has not reached this device: it is tagged not here yet, and nothing differs.
        var choice = PoolChoices.Build(Pool(["a", "b"]), [a, b], Pool(["a", "b", "z"]), [a, b, null], "Pixel", "Desk", "2026-11-19T16:00:00Z", phone: true, Utc);
        Assert.Equal(PoolSheetTag.NotHereYet, choice.There.Sheets[2].Tag);
        Assert.Equal("not here yet", PoolChoices.Words(choice.There.Sheets[2].Tag));
        Assert.Equal("Sheet 3", choice.There.Sheets[2].ShortName);
        Assert.Equal(0, choice.Differences);
        Assert.DoesNotContain(choice.Here.Sheets.Concat(choice.There.Sheets), s => s.Tag == PoolSheetTag.OnlyInThisOne);
        Assert.Equal("This phone (Pixel)", choice.Here.Device);
        Assert.Equal("Sheet 1, 19 Nov", choice.Here.Sheets[0].ShortName);
    }

    [Fact]
    public void ThePhoneLineCountsSheetsAndHowPooled()
    {
        var a = Sheet(1, "2026-11-19");
        var b = Sheet(2, "2026-11-19");
        var c = Sheet(3, "2026-11-19");
        var d = Sheet(4, "2026-11-19");
        var choice = PoolChoices.Build(Pool(["a", "b", "c", "d"]), [a, b, c, d], Pool(["a", "b"], pictures: true), [a, b], "Pixel", "Desk", "2026-11-19T16:00:00Z", phone: true, Utc);
        Assert.Equal(3, choice.Differences);
        Assert.Equal("6.5 Creedmoor, 41.5 gr, 19 Nov. 3 differences: how pooled, sheets 3 and 4.", choice.PhoneLine);
        Assert.Equal("Keep this phone's", PoolChoices.KeepThisDevice(phone: true));
        Assert.Equal("Keep Desk's", PoolChoices.KeepOther("Desk", phone: true));
        Assert.Equal("Keep the Desk's", PoolChoices.KeepOther("Desk", phone: false));
    }

    [Fact]
    public void OneSheetNamedTwiceIsSameOnlyAsOftenAsTheOtherNamesIt()
    {
        var a = Sheet(1, "2026-11-19");
        var a2 = Sheet(2, "2026-11-19");
        var choice = PoolChoices.Build(Pool(["a", "a"]), [a, a2], Pool(["a"]), [a], "Desk", "Pixel", "2026-11-19T16:00:00Z", phone: false, Utc);
        Assert.Equal([PoolSheetTag.Same, PoolSheetTag.OnlyInThisOne], choice.Here.Sheets.Select(s => s.Tag));
        Assert.Equal(1, choice.Differences);
    }
}
