using GroupLab.Cli;
using GroupLab.Core.Analysis;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 42: "Test on scan 5 and scan 6."
/// <para>
/// <see cref="CorrectionsSurviveDetectionTests"/> holds the rule on a sheet built for the purpose, where every distance is a round number
/// and the scale is a hundred pixels to the inch. This runs the same rule on two real scans, at their real resolution, with the real
/// detector and the sheet's own size reference, because a rule that works on a constructed case and not on a photograph of paper is a rule
/// that does not work.
/// </para>
/// <para>
/// <b>The scans are not committed and never will be</b>, so this skips where the folder is absent, saying so rather than passing silently.
/// </para>
/// </summary>
public class CorrectionsOnRealScansTests
{
    private const string Folder = @"C:\Dev\grouplab-range-2026-09-20\scans";

    public static TheoryData<string, string> Scans() => new()
    {
        { "5-600-dpi09202026.png", "0.224" },
        { "6-600-dpi09202026.png", "0.243" },
    };

    /// <summary>The session as detection leaves it, or null where this machine has no such scan.</summary>
    private static (MarkingSession Session, SheetAnalysisResult Result)? Detected(string file, string calibre)
    {
        string path = Path.Combine(Folder, file);
        if (!File.Exists(path))
        {
            return null;
        }

        var result = AnalyzeVerb.Analyze(GroupLab.Tests.Support.Temp.Readable(path), null, out string? failure,
            [Repo.PathTo("targets")], Calibre.Parse(calibre, out _));
        Assert.Null(failure);
        Assert.NotNull(result);

        var session = new MarkingSession();
        Load(session, result!);
        return (session, result!);
    }

    private static void Load(MarkingSession session, SheetAnalysisResult result) =>
        session.LoadDetections(result.Automatic.Scale!, result.Automatic.Bulls, result.Automatic.Detections,
            result.Automatic.Assignment, result.Automatic.Rejected ?? [], result.Automatic.Summary, result.Automatic.Detection);

    /// <summary>
    /// A nudge a person would really make, a fifth of a hole's width, and then the detector run again. The mark stays where they put it and
    /// the sheet does not gain a shot.
    /// </summary>
    [Theory]
    [MemberData(nameof(Scans))]
    public void ANudgeSurvivesDetectingAgain(string file, string calibre)
    {
        if (Detected(file, calibre) is not var (session, result))
        {
            Assert.True(true, $"skipped: {Path.Combine(Folder, file)} is not on this machine");
            return;
        }

        int before = session.State.Shots.Count;
        Assert.True(before > 0, $"{file}: nothing was detected, so there is no correction to make");

        // A fifth of a hole across, in image pixels. The scale maps pixels to inches, so this is the same nudge whatever the resolution.
        var scale = session.State.Scale!;
        double hole = session.State.Detection?.HoleSizeInches ?? 0.243;
        var origin = scale.ToTarget(new PointD(0, 0));
        var across = scale.ToTarget(new PointD(1000, 0));
        double inchesPerPixel = Math.Abs(across.X - origin.X) / 1000;
        double nudge = hole / 5 / inchesPerPixel;

        var shot = session.State.Shots.OrderBy(s => s.Id).First();
        var moved = new PointD(shot.Image.X + nudge, shot.Image.Y);
        session.MoveShot(shot.Id, moved);
        Assert.Equal(ShotProvenance.Corrected, session.State.Find(shot.Id)!.Provenance);

        Load(session, result);

        Assert.Equal(before, session.State.Shots.Count);

        var kept = session.State.Find(shot.Id);
        Assert.NotNull(kept);
        Assert.Equal(ShotProvenance.Corrected, kept!.Provenance);
        Assert.Equal(moved.X, kept.Image.X, 6);
        Assert.Equal(moved.Y, kept.Image.Y, 6);
    }

    /// <summary>
    /// The other direction, on the same real sheet: a mark dragged right across the sheet is a different mark, so it survives on its own and
    /// the hole it came from is found again. The sheet ends with one more shot than it started with, which is the honest answer.
    /// </summary>
    [Theory]
    [MemberData(nameof(Scans))]
    public void AMarkDraggedAcrossTheSheetSurvivesOnItsOwn(string file, string calibre)
    {
        if (Detected(file, calibre) is not var (session, result))
        {
            Assert.True(true, $"skipped: {Path.Combine(Folder, file)} is not on this machine");
            return;
        }

        int before = session.State.Shots.Count;
        var shot = session.State.Shots.OrderBy(s => s.Id).First();

        // Clear of every hole on the sheet rather than a fixed distance away. A first attempt moved it 1500 pixels down and across, which on
        // a 600 dpi scan is two and a half inches, and on a sheet whose bulls sit an inch and a half apart that landed on another hole: the
        // rule matched it, correctly, and the test read the right answer as a failure.
        double edgeX = session.State.Shots.Max(s => s.Image.X) + 400;
        double edgeY = session.State.Shots.Max(s => s.Image.Y) + 400;
        var far = new PointD(edgeX, edgeY);
        session.MoveShot(shot.Id, far);

        Load(session, result);

        Assert.Equal(before + 1, session.State.Shots.Count);
        Assert.Equal(far.X, session.State.Find(shot.Id)!.Image.X, 6);
    }

    /// <summary>The count the button shows is the number of marks a person has placed or moved, on a real sheet as on a made-up one.</summary>
    [Fact]
    public void TheButtonsCountIsNoughtUntilSomebodyTouchesTheSheet()
    {
        if (Detected("5-600-dpi09202026.png", "0.224") is not var (session, _))
        {
            Assert.True(true, $"skipped: {Folder} is not on this machine");
            return;
        }

        Assert.Equal(0, session.CorrectionsThatWouldBeKept());

        session.MoveShot(session.State.Shots[0].Id, new PointD(session.State.Shots[0].Image.X + 3, session.State.Shots[0].Image.Y));
        Assert.Equal(1, session.CorrectionsThatWouldBeKept());
    }
}
