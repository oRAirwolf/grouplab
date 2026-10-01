using GroupLab.Core.Records;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 342, worker B item 2, on the phone: a string imported from a file proposes its own marks when the list is
/// read, and says why; a typed list pairs in order as before.
/// </summary>
public class Entry342Tests
{
    [Fact]
    public void AnImportedStringProposesItsOwnMarks()
    {
        // Five shots on paper; the chronograph timed them, then two more twenty minutes later at the next target.
        var start = new TimeSpan(14, 0, 0);
        var timed = Enumerable.Range(0, 7).Select(i => new ChronographShot(i + 1, 2800 + i, Time: start + TimeSpan.FromSeconds(40 * i) + (i >= 5 ? TimeSpan.FromMinutes(20) : TimeSpan.Zero))).ToList();
        var readings = timed.Select(t => t.Fps).ToList();
        var import = new ChronographImport(ChronographFormat.GarminXero, readings, "", false, [], 1) { Shots = timed };
        int[] shots = [11, 12, 13, 14, 15];

        var (pairs, reasons) = VelocityPages.Proposed(shots, readings, import);
        Assert.Equal([0, 1, 2, 3, 4], pairs.Where(p => p.ShotId is not null).Select(p => p.Reading!.Value));
        Assert.Equal([5, 6], pairs.Where(p => p.ShotId is null).Select(p => p.Reading!.Value));
        Assert.Contains("only shots 1 to 5", reasons, StringComparison.Ordinal);

        var (typed, none) = VelocityPages.Proposed(shots, readings, null);
        Assert.Equal("", none);
        Assert.Equal([0, 1, 2, 3, 4], typed.Where(p => p.ShotId is not null).Select(p => p.Reading!.Value));
    }
}
