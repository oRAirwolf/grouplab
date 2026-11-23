using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 512 section 3, report 57's suggestion answered yes to the narrow case: a loose sheet whose bulls are a set's
/// stands in for a sheet of the set missing from those chosen, so a spoiled sheet can be replaced. Any other mix of designs is still refused.
/// </summary>
public class Entry512StandInTests
{
    private static readonly TargetDefinition Set = BuiltIns.Load("GL-CF9-T-C.gltd.json");

    private static (string?, TargetDefinition?, MarkingState) SetSheet(int k) => ("set", Set, MarkingState.Empty with { SetSheet = k });

    private static (string?, TargetDefinition?, MarkingState) Loose(string file = "GL-CF9-LTR-C.gltd.json") => (file, BuiltIns.Load(file), MarkingState.Empty);

    [Fact]
    public void OnlyALooseSheetWithTheSetsPageAndBullsStandsIn()
    {
        Assert.True(SetPool.StandsIn(Set, BuiltIns.Load("GL-CF9-LTR-C.gltd.json")));
        Assert.False(SetPool.StandsIn(Set, BuiltIns.Load("GL-CF9-LTR-E.gltd.json")));
        Assert.False(SetPool.StandsIn(Set, BuiltIns.Load("GL-CF9-A4-C.gltd.json")));
        Assert.False(SetPool.StandsIn(Set, BuiltIns.Load("GL-CF25-LTR.gltd.json")));
        Assert.False(SetPool.StandsIn(Set, Set));
        Assert.False(SetPool.StandsIn(BuiltIns.Load("GL-CF9-LTR-C.gltd.json"), BuiltIns.Load("GL-CF9-LTR-C.gltd.json")));
    }

    [Fact]
    public void ALooseSheetMayTakeOnlyTheSetsSheetsMissingFromThoseChosen()
    {
        var one = SetPool.FindStandIn([SetSheet(0), SetSheet(2), Loose()]);
        Assert.NotNull(one);
        Assert.Same(Set, one.Set);
        Assert.Equal([2], one.Loose);
        Assert.Equal([1], one.Places);

        var two = SetPool.FindStandIn([Loose(), SetSheet(1)]);
        Assert.Equal([0], two!.Loose);
        Assert.Equal([0, 2], two.Places);

        Assert.Empty(SetPool.FindStandIn([SetSheet(0), SetSheet(1), SetSheet(2), Loose()])!.Places);
    }

    [Fact]
    public void AnyOtherMixIsNotAStandIn()
    {
        Assert.Null(SetPool.FindStandIn([SetSheet(0), Loose("GL-CF9-LTR-E.gltd.json")]));
        Assert.Null(SetPool.FindStandIn([SetSheet(0), SetSheet(1)]));
        Assert.Null(SetPool.FindStandIn([("set", Set, MarkingState.Empty), Loose()]));
        Assert.Null(SetPool.FindStandIn([SetSheet(0), ("other set", BuiltIns.Load("GL-CF9-T-E.gltd.json"), MarkingState.Empty with { SetSheet = 1 }), Loose()]));
        Assert.Null(SetPool.FindStandIn([SetSheet(0), ("GL-CF9-LTR-C.gltd.json", BuiltIns.Load("GL-CF9-LTR-C.gltd.json"), MarkingState.Empty with { SetSheet = 1 })]));
        Assert.Null(SetPool.FindStandIn([SetSheet(0), ("unread", null, MarkingState.Empty)]));
    }
}
