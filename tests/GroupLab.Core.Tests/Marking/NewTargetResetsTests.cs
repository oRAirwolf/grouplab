using System.Reflection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 140: a new image is a new target, and nothing from the last one is carried over silently.
/// <para>
/// <b>What Alan saw.</b> He opened a fifteen shot sheet straight after a twenty five shot one. GroupLab told him "You fired 25 and 15 are
/// marked", flagged every single one of the fifteen holes as "possibly two holes" at 2.0 to 2.36 holes' area, and raised sixteen review
/// items on a sheet with nothing wrong with it. The holes read as doubles because they were being measured against the previous sheet's
/// smaller bullet. A review queue that cries wolf teaches people to ignore it, which costs far more than the convenience was worth.
/// </para>
/// <para>
/// <b>The test that matters is <see cref="EveryFieldThatDescribesOneSheetIsEmptyOnANewOne"/>.</b> Entry 140 section 1.1 asks for the fields
/// to be found by reading the state rather than from memory, so it walks the record's own properties. A field added later is caught here
/// rather than by somebody opening their second target of the day.
/// </para>
/// </summary>
public class NewTargetResetsTests
{
    /// <summary>
    /// The fields that are not one sheet's own, with the reason each is allowed to survive opening another image. A name here is a decision;
    /// a name missing from here and left set after opening is a fault, and the test says so.
    /// </summary>
    private static readonly Dictionary<string, string> NotOneSheets = new(StringComparer.Ordinal)
    {
        ["ImagePath"] = "It is the new image's own path, which is the whole point of opening one.",
        ["ViewQuarterTurns"] = "How the new image is turned for display, taken from its own orientation tag.",
        ["ExifOrientation"] = "The new image's own tag, as read from the new image.",
        ["NextId"] = "A counter, not a fact about the sheet. It starts at one on an empty marking.",
    };

    /// <summary>A marking with every per-sheet field set, as a sheet looks after a person has worked on it.</summary>
    private static MarkingState Worked() => new(
        @"C:\first.png",
        new LengthReference(new PointD(0, 0), new PointD(100, 0), 1),
        new PointD(10, 10),
        [new BullAim(1, "1", new PointD(100, 100))],
        [new MarkedShot(1, new PointD(101, 101), ShotProvenance.Automatic, Bull: 1) { Note = "called low", Flyer = true }],
        2,
        RegistrationSummary: "34 of 34 markers",
        Calibre: Calibre.Of(0.224),
        ShotDistanceInches: 3600,
        Detection: new DetectionRecord(Calibre.Of(0.224), 0.212),
        Dismissed: ["something"],
        Subgroups: new SubgroupMap(System.Collections.Immutable.ImmutableDictionary<int, string>.Empty.Add(1, "41.2 gr")),
        ExpectedShots: 25,
        Rifle: new Rifle("Tikka", 0.25, GroupLab.Core.Statistics.AngularUnit.Moa),
        Barrel: "Bartlein",
        Load: "41.2 gr",
        Rule: new AssignmentRule(false, System.Collections.Immutable.ImmutableDictionary<int, int>.Empty.Add(1, 1)));

    [Fact]
    public void EveryFieldThatDescribesOneSheetIsEmptyOnANewOne()
    {
        var session = new MarkingSession(Worked());
        session.Open(@"C:\second.png", exifOrientation: 1);
        var now = session.State;
        var fresh = MarkingState.Empty;

        var wrong = new List<string>();
        foreach (var property in typeof(MarkingState).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (NotOneSheets.ContainsKey(property.Name))
            {
                continue;
            }

            object? after = property.GetValue(now);
            object? empty = property.GetValue(fresh);

            // A collection is compared by whether it holds anything, since two empty lists are not the same object.
            bool same = (after, empty) switch
            {
                (System.Collections.IEnumerable a, System.Collections.IEnumerable b) => !a.GetEnumerator().MoveNext() && !b.GetEnumerator().MoveNext(),
                _ => Equals(after, empty),
            };

            if (!same)
            {
                wrong.Add($"{property.Name} is still {after}");
            }
        }

        Assert.True(wrong.Count == 0,
            "opening a new image left one sheet's facts on the next: " + string.Join("; ", wrong)
            + ". Reset it in MarkingSession.Open, or name it in NotOneSheets with the reason it belongs to the session rather than the sheet.");
    }

    /// <summary>
    /// The calibre is the one that caused the harm, so it is worth its own test rather than only being covered by the sweep above. It used
    /// to be carried over deliberately.
    /// </summary>
    [Fact]
    public void TheCalibreDoesNotFollowTheLastSheet()
    {
        var session = new MarkingSession(Worked());

        session.Open(@"C:\second.png");

        Assert.Null(session.State.Calibre);
        Assert.Null(session.State.Detection);
    }

    /// <summary>And neither does the count that made GroupLab say "you fired 25" about a sheet of fifteen.</summary>
    [Fact]
    public void TheRoundsFiredDoNotFollowTheLastSheet()
    {
        var session = new MarkingSession(Worked());

        session.Open(@"C:\second.png");

        Assert.Null(session.State.ExpectedShots);
        Assert.Null(session.State.Rule);
    }

    /// <summary>Opening a new image is not undoable back into the last sheet: the history goes with it.</summary>
    [Fact]
    public void TheUndoHistoryDoesNotSurviveANewImage()
    {
        var session = new MarkingSession(Worked());
        session.SetExpectedShots(10);
        Assert.True(session.CanUndo);

        session.Open(@"C:\second.png");

        Assert.False(session.CanUndo);
        Assert.False(session.CanRedo);
    }
}
