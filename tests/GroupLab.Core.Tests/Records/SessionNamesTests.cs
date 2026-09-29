using GroupLab.Core.Records;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 295 section 3: on the phone, Compare named two loads "GroupLab 5x5 Load Development with Load Block, Letter",
/// the sheet both were shot on, so they could not be told apart. A session is named by its load, then its date, then its time, as far as it
/// takes to tell it from the others, with the sheet's name beneath.
/// </summary>
public class SessionNamesTests
{
    private const string Sheet = "GroupLab 5x5 Load Development with Load Block, Letter";

    private static IReadOnlyList<SessionName> Named(params (long Id, string? Load, string? ShotDate, string CreatedUtc)[] sessions) =>
        SessionNames.For([.. sessions.Select(s => (s.Id, s.Load, s.ShotDate, s.CreatedUtc, Sheet))], TimeZoneInfo.Utc);

    [Fact]
    public void TwoSessionsOnOneSheetOnOneDayAreToldApartByTheirTimes()
    {
        var names = Named((1, null, "2026-09-29", "2026-09-29T04:40:00Z"), (2, null, "2026-09-29", "2026-09-29T05:01:00Z"));
        Assert.Equal(["2026-09-29, 04:40", "2026-09-29, 05:01"], names.Select(n => n.Name));
        Assert.All(names, n => Assert.Equal(Sheet, n.Sheet));
        Assert.DoesNotContain(names, n => n.Name.Contains(Sheet, StringComparison.Ordinal));
    }

    [Fact]
    public void ALoadNamesItAndTheDateAndTimeComeOnlyWhereTheyAreNeeded()
    {
        var names = Named(
            (1, "41.5 gr H4350", "2026-09-28", "2026-09-28T10:00:00Z"),
            (2, "42.1 gr H4350", "2026-09-29", "2026-09-29T10:00:00Z"),
            (3, "41.5 gr H4350", "2026-09-29", "2026-09-29T11:00:00Z"),
            (4, "42.1 gr H4350", "2026-09-29", "2026-09-29T12:30:00Z"),
            (5, "Factory 150", "2026-09-29", "2026-09-29T13:00:00Z"));
        Assert.Equal(
            ["41.5 gr H4350, 2026-09-28", "42.1 gr H4350, 2026-09-29, 10:00", "41.5 gr H4350, 2026-09-29", "42.1 gr H4350, 2026-09-29, 12:30", "Factory 150"],
            names.Select(n => n.Name));
        Assert.Equal(names.Count, names.Select(n => n.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("2026-09-29, 12:30", names[3].When);
        Assert.Equal("2026-09-29", names[4].When);
    }

    [Fact]
    public void TwoThatStillReadTheSameCarryTheirNumbers()
    {
        var names = Named((7, "A", "2026-09-29", "2026-09-29T05:01:10Z"), (8, "A", "2026-09-29", "2026-09-29T05:01:40Z"));
        Assert.Equal(["A, 2026-09-29, 05:01, session 7", "A, 2026-09-29, 05:01, session 8"], names.Select(n => n.Name));
    }

    [Fact]
    public void TheTimeIsTheLocalTime()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("five hours behind", TimeSpan.FromHours(-5), "five hours behind", "five hours behind");
        var names = SessionNames.For([(1L, (string?)null, (string?)null, "2026-09-29T04:40:00Z", Sheet), (2L, null, null, "2026-09-29T04:50:00Z", Sheet)], zone);
        Assert.Equal(["2026-09-28, 23:40", "2026-09-28, 23:50"], names.Select(n => n.When));
    }
}
