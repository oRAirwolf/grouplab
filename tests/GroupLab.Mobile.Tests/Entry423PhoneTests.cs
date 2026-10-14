using GroupLab.Core.Gltd.Json;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 423 section 5 on the phone: pictures chosen or shared together that are all sheets of one set open as the
/// set's checklist with the pooled group showing; anything else is shown as before, the last one's result.
/// </summary>
public class Entry423PhoneTests
{
    private static TargetDefinition Load(string file) => GltdJsonReader.ReadFile(Repo.PathTo("targets", file)).Definition!;

    private static PhoneResult Read(TargetDefinition? definition, int? sheet) =>
        new(MarkingState.Empty with { SetSheet = sheet }, definition, null, null);

    [Fact]
    public void SheetsOfOneSetAreOneSetAndAnythingElseIsNot()
    {
        var set = Load("GL-CF9-T.gltd.json");
        var page = Load("GL-CF9-LTR.gltd.json");

        Assert.Same(set, SetPage.OneSet([Read(set, 0), Read(set, 2), Read(set, 1)]));
        Assert.Null(SetPage.OneSet([Read(set, 0)]));
        Assert.Null(SetPage.OneSet([Read(set, 0), Read(page, null)]));
        Assert.Null(SetPage.OneSet([Read(set, 0), Read(null, null)]));
        Assert.Null(SetPage.OneSet([Read(page, null), Read(page, null)]));
        Assert.Null(SetPage.OneSet([Read(set, 0), Read(set, null)]));
    }
}
