using System.Collections.Immutable;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;
using GroupLab.Cli.Imaging;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 3.1, the part that was built and never connected: holes are assigned in the shifted frame, so a
/// group that landed a whole bull away from where it was aimed is measured against the bulls it was aimed at.
/// <para>
/// <b>This is the worst defect this project has found, and these tests are the shape of the fix.</b> On scan 5 of the second range day every
/// shot was measured against a bull it was not aimed at, and nothing looked wrong: the group came out tight, centred, and the zero
/// correction said there was nothing to dial. A shooter would have believed it.
/// </para>
/// <para>
/// <b>The restraint matters as much as the correction.</b> The offset is applied only where the shooter has said which bulls they aimed at,
/// because a sheet of twenty five bulls with ten shot has a translation that explains the holes for almost any reading. Solving over every
/// bull would have the software choosing between readings on a margin it cannot justify, on exactly the sheets where being wrong is
/// quietest. <see cref="AnOrdinarySheetIsAssignedExactlyAsBefore"/> holds the other side of that: a sheet shot at its own bulls must be
/// untouched by any of this.
/// </para>
/// </summary>
public class SheetOffsetAssignmentTests
{
    /// <summary>
    /// A synthetic GL-CF25-LTR shot at the second to fifth bull of every row, twenty shots, with the whole group landing one bull to the
    /// left of where it was aimed. That is scan 5's case: every hole sits nearer a bull it was not fired at, and the bull it is nearest to
    /// is one nobody shot at. It is shifted along one axis only so that every hole still lands on the grid rather than off the top of it,
    /// which would test the detector's edge handling instead of the assignment.
    /// </summary>
    private static (MarkingSession Session, ImmutableList<int> Aimed) ShiftedSheet()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 300);
        var random = new Random(5150);

        var scoring = definition.Bulls.Select((b, i) => (Bull: b, Index: i)).Where(b => b.Bull.Scoring).ToList();
        Assert.Equal(25, scoring.Count);

        // The grid's pitch, from the first two bulls of a row and the first bull of the next.
        double pitchX = scoring[1].Bull.X - scoring[0].Bull.X;
        Assert.True(pitchX > 0);

        // Columns 2 to 5 of each row, which is where the shooter aimed.
        var aimed = scoring.Where((_, k) => k % 5 != 0).ToList();
        Assert.Equal(20, aimed.Count);

        // And the whole group landed one bull to the left of that, with a little scatter of its own.
        var holes = aimed
            .Select(b => SyntheticSheet.SampleHole(
                random,
                b.Bull.X - pitchX + random.Next(-25, 26),
                b.Bull.Y + random.Next(-25, 26),
                onInk: false,
                HoleBacking.ScannerLid,
                0.871))
            .ToList();

        double s = 254 / 300.0;
        var image = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(image, image, new ImageMetadata("PNG", image.Width, image.Height, 300, 300, null, null, null, null, null), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);

        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        Assert.Equal(20, session.State.Shots.Count(sh => sh.IsShot));
        return (session, [.. aimed.Select(b => b.Index)]);
    }

    /// <summary>The rule that says which bulls were aimed at, one shot each.</summary>
    private static AssignmentRule AimedAt(IEnumerable<int> bulls) => new(false, bulls.ToImmutableDictionary(b => b, _ => 1));

    /// <summary>
    /// Told nothing, some shots land on bulls nobody aimed at. This is the baseline the fix is measured against rather than asserted over.
    /// <para>
    /// <b>It is worth recording how mild the baseline is, because it explains why the defect is so quiet.</b> The one-to-one matching is
    /// global, not nearest-bull: it minimises the total distance over the whole sheet, and on this fixture that alone puts fifteen of the
    /// twenty shots on the bull they were actually fired at. So the sheet does not come out obviously scrambled. It comes out mostly right,
    /// with a handful of shots measured from the wrong centres, which is exactly the kind of wrong a person cannot see.
    /// </para>
    /// </summary>
    [Fact]
    public void ToldNothingSomeShotsLandOnBullsNobodyAimedAt()
    {
        var (session, aimed) = ShiftedSheet();
        var wanted = aimed.ToHashSet();

        int wrong = session.State.Shots.Count(sh => sh.IsShot && !(sh.Bull is { } b && wanted.Contains(b)));

        Assert.True(wrong > 0, "the fixture is meant to be a sheet the matching cannot read without being told which bulls were aimed at");
    }

    /// <summary>
    /// Told which bulls were aimed at, every shot is assigned to the bull it was fired at. The holes have not moved: the matching ran in the
    /// frame the group actually landed in.
    /// </summary>
    [Fact]
    public void ToldWhichBullsWereAimedAtEveryShotFindsItsOwn()
    {
        var (session, aimed) = ShiftedSheet();
        var before = session.State.Shots.Where(sh => sh.IsShot).ToDictionary(sh => sh.Id, sh => sh.Image);

        session.SetAssignmentRule(AimedAt(aimed));

        var wanted = aimed.ToHashSet();
        var shots = session.State.Shots.Where(sh => sh.IsShot).ToList();
        Assert.All(shots, sh => Assert.True(sh.Bull is { } b && wanted.Contains(b), $"shot {sh.Id} was given bull {sh.Bull}, which nobody aimed at"));

        // One shot a bull, and every aimed bull holding exactly one.
        Assert.Equal(20, shots.Select(sh => sh.Bull).Distinct().Count());

        // Nothing stored moved. The shift is a frame the matching runs in, not an edit to the marking.
        Assert.All(shots, sh => Assert.Equal(before[sh.Id], sh.Image));
    }

    /// <summary>
    /// An ordinary sheet, one shot into every bull, is assigned exactly as it was before any of this existed. The offset on such a sheet is
    /// the group's own small wandering, well under the tenth of an inch that makes it worth applying, so nothing is shifted.
    /// </summary>
    [Fact]
    public void AnOrdinarySheetIsAssignedExactlyAsBefore()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], 300);
        var random = new Random(4242);
        var scoring = definition.Bulls.Select((b, i) => (Bull: b, Index: i)).Where(b => b.Bull.Scoring).ToList();
        var holes = scoring
            .Select(b => SyntheticSheet.SampleHole(random, b.Bull.X + random.Next(-40, 41), b.Bull.Y + random.Next(-40, 41), onInk: false, HoleBacking.ScannerLid, 0.871))
            .ToList();

        double s = 254 / 300.0;
        var image = SyntheticSheet.Compose(render, 300, new HomographyMapping(new Homography([s, 0, 0.5 * s, 0, s, 0.5 * s, 0, 0, 1])), render.Width, render.Height, holes, [], random);
        var result = AutomaticMarking.Run(image, image, new ImageMetadata("PNG", image.Width, image.Height, 300, 300, null, null, null, null, null), definition, new OpenCvSharpBackend());
        Assert.Null(result.Failure);

        var session = new MarkingSession();
        session.LoadDetections(result.Scale!, result.Bulls, result.Detections, result.Assignment, result.Rejected ?? [], result.Summary, result.Detection);
        var before = session.State.Shots.Where(sh => sh.IsShot).ToDictionary(sh => sh.Id, sh => sh.Bull);

        session.SetAssignmentRule(AimedAt(scoring.Select(b => b.Index)));

        Assert.All(session.State.Shots.Where(sh => sh.IsShot), sh => Assert.Equal(before[sh.Id], sh.Bull));
    }

    /// <summary>
    /// Reading the sheet by nearest bull is a person saying "do not match these", so no offset is solved and nothing is shifted. A rule that
    /// overrode that would be the software ignoring an instruction it was given.
    /// </summary>
    [Fact]
    public void NearestBullIsLeftAlone()
    {
        var (session, _) = ShiftedSheet();
        var before = session.State.Shots.Where(sh => sh.IsShot).ToDictionary(sh => sh.Id, sh => sh.Bull);

        session.SetAssignmentRule(AssignmentRule.Nearest);

        Assert.All(session.State.Shots.Where(sh => sh.IsShot), sh => Assert.Equal(before[sh.Id], sh.Bull));
    }

    /// <summary>Setting the rule and undoing it puts the assignment back, because it is one undoable step like every other.</summary>
    [Fact]
    public void TheShiftIsOneUndoStep()
    {
        var (session, aimed) = ShiftedSheet();
        var before = session.State.Shots.Where(sh => sh.IsShot).ToDictionary(sh => sh.Id, sh => sh.Bull);

        session.SetAssignmentRule(AimedAt(aimed));
        session.Undo();

        Assert.All(session.State.Shots.Where(sh => sh.IsShot), sh => Assert.Equal(before[sh.Id], sh.Bull));
    }
}
