using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Records;
using Microsoft.Data.Sqlite;

namespace GroupLab.Core.Tests.Records;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 511 section 1 (suggestion 56): Session records filled with rows all named after the same target. A session's
/// own name, the label the analysis screen keeps, now comes first wherever a session is named, with the target's name after it, and a session
/// can be renamed from its record, which writes the same label into its marking so the name reads the same everywhere and synchronizes.
/// </summary>
public sealed class SessionRenameTests : IDisposable
{
    private const string Target = "GroupLab 3x3 2 MOA, C Bull, Set of 3 Letter Sheets";

    private readonly string root = Path.Combine(Path.GetTempPath(), $"grouplab-rename-{Guid.NewGuid():N}");

    public SessionRenameTests() => Directory.CreateDirectory(root);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GroupLab.Tests.Support.Temp.Delete(root);
    }

    private SessionStore Store() => SessionStore.Open(Path.Combine(root, "grouplab.db"));

    /// <summary>A picture with a scale and three shots, as Accept and analyze would save it, with <paramref name="label"/> as its name.</summary>
    private static SessionRecord Record(string? label, string created = "2026-11-22T10:00:00Z")
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(600, 0), 1));
        session.AddShot(new PointD(100, 100));
        session.AddShot(new PointD(130, 110));
        session.AddShot(new PointD(105, 140));
        session.SetSheetLabel(label);
        var state = MarkingFile.Read(MarkingFile.Write(session.State)).State;
        var when = DateTime.Parse(created, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        return SessionRecords.Build(state, null, UnitSettings.Imperial, false, null, null, null, null, when, when) with { SheetName = Target + (label is null ? "" : ", " + label) };
    }

    [Fact]
    public void ASessionWithANameIsCalledByItFirstAndTheTargetComesBeneathAlone()
    {
        var names = SessionNames.For(
            [(1L, "Rifle A", "41.5 gr H4350", "2026-11-22", "2026-11-22T10:00:00Z", Target + ", Rifle A"),
             (2L, (string?)null, "41.5 gr H4350", "2026-11-22", "2026-11-22T11:00:00Z", Target)], TimeZoneInfo.Utc);
        Assert.Equal("Rifle A", names[0].Name);
        Assert.Equal(Target, names[0].Sheet);
        Assert.Equal("41.5 gr H4350", names[1].Name);
        Assert.Equal(Target, names[1].Sheet);
    }

    [Fact]
    public void TwoSessionsGivenTheSameNameAreStillToldApart()
    {
        var names = SessionNames.For(
            [(1L, "Rifle A", (string?)null, "2026-11-21", "2026-11-21T10:00:00Z", Target + ", Rifle A"),
             (2L, "Rifle A", (string?)null, "2026-11-22", "2026-11-22T10:00:00Z", Target + ", Rifle A")], TimeZoneInfo.Utc);
        Assert.Equal(["Rifle A, 2026-11-21", "Rifle A, 2026-11-22"], names.Select(n => n.Name));
    }

    [Fact]
    public void TheStoredSheetNameKeepsItsOldShapeAndAListShowsTheNameFirst()
    {
        Assert.Equal(Target, SessionNames.TargetOf(Target + ", Rifle A", "Rifle A"));
        Assert.Equal("Rifle A, " + Target, SessionNames.Titled(Target + ", Rifle A", "Rifle A"));
        Assert.Equal(Target, SessionNames.Titled(Target, null));
        // A name that is not at the end of the stored sheet name leaves the sheet name as it is.
        Assert.Equal("Rifle A, Other", SessionNames.Titled("Other", "Rifle A"));
        Assert.Equal(Target + ", Rifle B", SessionNames.Relabelled(Target + ", Rifle A", "Rifle A", "Rifle B"));
        Assert.Equal(Target, SessionNames.Relabelled(Target + ", Rifle A", "Rifle A", null));
    }

    [Fact]
    public void RenamingASessionWritesTheSameLabelTheAnalysisScreenKeeps()
    {
        var store = Store();
        long id = store.Save(Record(null));
        Assert.Null(Assert.Single(store.List()).Label);

        var renamed = store.Rename(id, "  Rifle A  ");
        Assert.NotNull(renamed);
        var got = store.Get(id)!;
        Assert.Equal(Target + ", Rifle A", got.SheetName);
        Assert.Equal("Rifle A", SessionNames.LabelOf(got.MarkingJson));
        // The marking still reads, with the name where the analysis screen reads it.
        Assert.Equal("Rifle A", MarkingFile.Read(got.MarkingJson).State.SheetLabel);
        Assert.Equal(3, MarkingFile.Read(got.MarkingJson).State.Shots.Count);
        var listed = Assert.Single(store.List());
        Assert.Equal("Rifle A", listed.Label);
        Assert.Equal("Rifle A", SessionNames.For(store.List())[0].Name);

        store.Rename(id, "Rifle B");
        Assert.Equal(Target + ", Rifle B", store.Get(id)!.SheetName);

        store.Rename(id, " ");
        got = store.Get(id)!;
        Assert.Equal(Target, got.SheetName);
        Assert.Null(MarkingFile.Read(got.MarkingJson).State.SheetLabel);
        Assert.Null(Assert.Single(store.List()).Label);
    }

    [Fact]
    public void RenamingKeepsEverythingElseAndANameAlreadyGivenChangesNothing()
    {
        var store = Store();
        var record = Record("Rifle A");
        long id = store.Save(record);
        var before = store.Get(id)!;
        Assert.Null(store.Rename(id + 100, "Nobody"));
        Assert.Equal(before.MarkingJson, store.Rename(id, "Rifle A")!.MarkingJson);

        var after = store.Rename(id, "Rifle C")!;
        Assert.Equal(before with { SheetName = after.SheetName, MarkingJson = after.MarkingJson, ProofImage = after.ProofImage }, after);
        Assert.Equal(before.CreatedUtc, store.Get(id)!.CreatedUtc);
    }

    [Fact]
    public void AMarkingThatIsNotJsonHasNoNameAndStopsNoList()
    {
        var store = Store();
        store.Save(Record(null) with { MarkingJson = "not a marking" });
        store.Save(Record("Rifle A", "2026-11-22T12:00:00Z"));
        var list = store.List();
        Assert.Equal(2, list.Count);
        Assert.Equal("Rifle A", list[0].Label);
        Assert.Null(list[1].Label);
        Assert.Null(SessionNames.LabelOf("not a marking"));
    }
}
