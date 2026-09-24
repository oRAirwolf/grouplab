using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 162 section 3.2: paper and backing are recorded fields, because what a hole measures depends on them and
/// every measurement of that ratio so far is confounded by their not being recorded.
/// </summary>
public class TargetMaterialTests
{
    [Fact]
    public void TheChoicesAreTheEntrysOwn()
    {
        Assert.Equal(["copy paper", "card stock", "other"], TargetMaterial.Papers);
        Assert.Equal(["cardboard", "foam board", "none", "other"], TargetMaterial.Backings);
    }

    [Fact]
    public void TheyAreSavedWithTheMarkingAndReadBack()
    {
        var session = new MarkingSession();
        session.SetMaterial("card stock", "cardboard");
        Assert.Equal(("card stock", "cardboard"), (session.State.Paper, session.State.Backing));

        var back = MarkingFile.Read(MarkingFile.Write(session.State)).State;
        Assert.Equal(("card stock", "cardboard"), (back.Paper, back.Backing));
    }

    /// <summary>A value that is not one of the choices is recorded as not said, so a hand-edited file cannot invent a sixth word.</summary>
    [Fact]
    public void AnythingElseIsNotSaid()
    {
        var session = new MarkingSession();
        session.SetMaterial("newsprint", "a hay bale");
        Assert.Equal(((string?)null, (string?)null), (session.State.Paper, session.State.Backing));
    }
}
