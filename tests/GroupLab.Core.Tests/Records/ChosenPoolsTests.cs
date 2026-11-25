using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// Report 57's suggestion, Alan's yes to the general case (2026-11-24): the planner the computer's and the phone's Pool the chosen share.
/// Sheets of one set read as the set, a loose sheet with the set's bulls stands in, anything else pools as separate sheets in the order
/// ticked, and only one loose sheet, different distances or different calibers are refused.
/// </summary>
public class ChosenPoolsTests
{
    private static SessionRecord Sheet(string file, string id, int? setSheet, double distance = 3600, double calibre = 0.308)
    {
        var state = new MarkingState(null, null, null, [], [], 1, Calibre: Calibre.Of(calibre), ShotDistanceInches: distance, SetSheet: setSheet);
        return new SessionRecord(0, "2026-11-24T00:00:00Z", "2026-11-24", file, id, File.ReadAllText(Repo.PathTo("targets", file)), distance, null, null, null,
            calibre, MarkingFile.Write(state), 0, null, null, null, null, null, null, null);
    }

    [Fact]
    public void SheetsOfOneSetReadAsTheSet()
    {
        var plan = ChosenPools.Plan([Sheet("GL-CF9-T-C.gltd.json", "SET", 0), Sheet("GL-CF9-T-C.gltd.json", "SET", 2)]);
        Assert.Equal(ChosenPoolKind.Set, plan.Kind);
        Assert.NotNull(plan.Set);
    }

    [Fact]
    public void ALooseSheetWithTheSetsBullsStandsInAndOneTooManyPoolsAsSeparateSheets()
    {
        var standIn = ChosenPools.Plan([Sheet("GL-CF9-T-C.gltd.json", "SET", 0), Sheet("GL-CF9-LTR-C.gltd.json", "LOOSE", null)]);
        Assert.Equal(ChosenPoolKind.StandIn, standIn.Kind);
        Assert.Equal([1, 2], standIn.StandIn!.Places);

        var full = ChosenPools.Plan([Sheet("GL-CF9-T-C.gltd.json", "SET", 0), Sheet("GL-CF9-T-C.gltd.json", "SET", 1), Sheet("GL-CF9-T-C.gltd.json", "SET", 2),
            Sheet("GL-CF9-LTR-C.gltd.json", "LOOSE", null)]);
        Assert.Equal(ChosenPoolKind.Mixed, full.Kind);
        Assert.StartsWith("Every sheet of ", full.Words, StringComparison.Ordinal);
        Assert.Contains("The 4 sheets are pooled as separate sheets", full.Words, StringComparison.Ordinal);
    }

    [Fact]
    public void DifferentDesignsAndLooseSheetsOfOneDesignPoolAsSeparateSheets()
    {
        var mixed = ChosenPools.Plan([Sheet("GL-CF9-T-C.gltd.json", "SET", 0), Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null)]);
        Assert.Equal(ChosenPoolKind.Mixed, mixed.Kind);
        Assert.Equal(PoolRefusal.Mixed(2), mixed.Words);

        var loose = ChosenPools.Plan([Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null), Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null, distance: 3600.2)]);
        Assert.Equal(ChosenPoolKind.Mixed, loose.Kind);
        Assert.Equal("2 pictures pooled", ChosenPools.MixedComposite(loose).SheetLabel);
    }

    [Fact]
    public void OneLooseSheetDifferentDistancesAndDifferentCalibresAreRefused()
    {
        Assert.Equal(PoolRefusal.NothingTicked, ChosenPools.Plan([]).Words);
        Assert.Equal(PoolRefusal.OneSheet, ChosenPools.Plan([Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null)]).Words);
        var far = ChosenPools.Plan([Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null), Sheet("GL-CF9-LTR-C.gltd.json", "LOOSE", null, distance: 3937)]);
        Assert.Equal(ChosenPoolKind.Refused, far.Kind);
        Assert.Equal(PoolRefusal.DifferentDistances, far.Words);
        var small = ChosenPools.Plan([Sheet("GL-CF25-LTR-D-C.gltd.json", "OTHER", null), Sheet("GL-CF9-LTR-C.gltd.json", "LOOSE", null, calibre: 0.224)]);
        Assert.Equal(PoolRefusal.DifferentCalibres, small.Words);
    }
}
