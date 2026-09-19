using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 26 and entry 24 sections 4 and 7: the view turns in quarter turns and never moves a mark, undo covers
/// the turn, the EXIF tag sets where the view starts, and the marking file records the stored pixel frame, the rotation and the
/// scale's values, refuses a frame it does not know, and migrates version 1.
/// </summary>
public class ViewRotationTests
{
    private const double Width = 800, Height = 600;

    [Fact]
    public void TheTurnedFrameRoundTripsAndPutsTheImageCornersWhereAQuarterTurnClockwiseDoes()
    {
        PointD[] points = [new(0, 0), new(12.5, 480.25), new(799, 1), new(400, 300)];
        for (int turns = -5; turns <= 5; turns++)
        {
            var (a, b, c, d, e, f) = ViewRotation.Affine(turns, Width, Height);
            foreach (var p in points)
            {
                var display = ViewRotation.ToDisplay(p, turns, Width, Height);
                var back = ViewRotation.ToImage(display, turns, Width, Height);
                Assert.Equal(p.X, back.X, 12);
                Assert.Equal(p.Y, back.Y, 12);
                Assert.Equal((a * p.X) + (b * p.Y) + c, display.X, 12);
                Assert.Equal((d * p.X) + (e * p.Y) + f, display.Y, 12);
            }
        }

        // A quarter turn clockwise: the image's top left goes to the top right, its bottom left to the top left.
        Assert.Equal((Height, Width), ViewRotation.DisplaySize(1, Width, Height));
        Assert.Equal(new PointD(Height, 0), ViewRotation.ToDisplay(new PointD(0, 0), 1, Width, Height));
        Assert.Equal(new PointD(0, 0), ViewRotation.ToDisplay(new PointD(0, Height), 1, Width, Height));
        Assert.Equal(new PointD(Width, Height), ViewRotation.ToDisplay(new PointD(0, 0), 2, Width, Height));
        Assert.Equal(new PointD(0, Width), ViewRotation.ToDisplay(new PointD(0, 0), 3, Width, Height));
        Assert.Equal(new PointD(0, 1), ViewRotation.VectorToDisplay(new PointD(1, 0), 1));
    }

    [Fact]
    public void TheOrientationTagSetsWhereTheViewStartsAndAMirrorIsNotApplied()
    {
        Assert.Equal(0, ViewRotation.FromExifOrientation(null));
        Assert.Equal(0, ViewRotation.FromExifOrientation(1));
        Assert.Equal(2, ViewRotation.FromExifOrientation(3));
        Assert.Equal(1, ViewRotation.FromExifOrientation(6));
        Assert.Equal(3, ViewRotation.FromExifOrientation(8));
        Assert.All(new int?[] { 2, 4, 5, 7 }, tag => Assert.True(ViewRotation.ExifMirrors(tag) && ViewRotation.FromExifOrientation(tag) == 0));

        var session = new MarkingSession();
        session.Open("phone.jpg", 6);
        Assert.Equal(1, session.State.ViewQuarterTurns);
        Assert.Equal(6, session.State.ExifOrientation);
        Assert.False(session.CanUndo);
    }

    /// <summary>Entry 26's trap: twelve marks, then a turn, and every stored and target position is exactly what it was.</summary>
    [Fact]
    public void RotatingIsAnUndoableViewChangeThatMovesNoMark()
    {
        var session = new MarkingSession();
        session.Open("scan.png");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(400, 300));
        var random = new Random(26);
        for (int i = 0; i < 12; i++)
        {
            session.AddShot(new PointD(350 + (100 * random.NextDouble()), 250 + (100 * random.NextDouble())));
        }

        var before = session.State;
        var report = GroupAnalysis.Analyse(before);
        session.Rotate(1);
        session.Rotate(1);
        session.Rotate(-3);
        var after = session.State;
        Assert.Equal(0, ViewRotation.Normalise(after.ViewQuarterTurns - 3));
        Assert.Same(before.Shots, after.Shots);
        Assert.Equal(before.PointOfAim, after.PointOfAim);
        Assert.Same(before.Scale, after.Scale);
        Assert.Equal(report.AllShots!.MeanRadius!.Value, GroupAnalysis.Analyse(after).AllShots!.MeanRadius!.Value);
        Assert.Equal(report.AllShots.CentreFromAim, GroupAnalysis.Analyse(after).AllShots!.CentreFromAim);

        session.Undo();
        Assert.Equal(2, session.State.ViewQuarterTurns);
        session.Undo();
        session.Undo();
        Assert.Equal(0, session.State.ViewQuarterTurns);
        Assert.Same(before.Shots, session.State.Shots);
    }

    [Fact]
    public void AMarkingFileReopensWithItsMarksItsScaleValuesAndItsRotation()
    {
        var session = new MarkingSession();
        session.Open("C:/targets/group.jpg", 8);
        session.SetScale(new LengthReference(new PointD(10, 20), new PointD(160, 20), 1.5));
        session.SetPointOfAim(new PointD(300, 310));
        int a = session.AddShot(new PointD(310.25, 305.5));
        int b = session.AddShot(new PointD(290, 330), 0);
        session.AddShot(new PointD(305, 280));
        session.SetExclusion(a, ExclusionReason.CalledFlyer);
        session.SetNotAShot(b, true);
        session.Rotate(1);

        var (state, notes) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.Empty(notes);
        Assert.Equal("C:/targets/group.jpg", state.ImagePath);
        Assert.Equal(8, state.ExifOrientation);
        Assert.Equal(0, state.ViewQuarterTurns);
        Assert.Equal(session.State.Shots, state.Shots);
        Assert.Equal(session.State.NextId, state.NextId);
        Assert.Equal(session.State.PointOfAim, state.PointOfAim);
        var length = Assert.IsType<LengthReference>(state.Scale);
        Assert.Equal(new LengthReference(new PointD(10, 20), new PointD(160, 20), 1.5), length);

        PointD[] corners = [new(100, 100), new(520, 110), new(510, 700), new(95, 690)];
        session.SetScale(new RectangleReference(corners, 6, 9));
        var rectangle = Assert.IsType<RectangleReference>(MarkingFile.Read(MarkingFile.Write(session.State)).State.Scale);
        Assert.Equal(corners, rectangle.Corners);
        Assert.Equal(session.State.Scale!.ToTarget(new PointD(300, 400)), rectangle.ToTarget(new PointD(300, 400)));

        session.SetScale(new SheetReference(new HomographyMapping(new Homography([1, 0, 0, 0, 1, 0, 0, 0, 1])), "36 of 38 markers"));
        // NOTES-FROM-PLANNING.md entry 112 section 1: the sheet's registration is kept, so a session reopens and reads with no image.
        var sheet = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.Equal("36 of 38 markers", Assert.IsType<SheetReference>(sheet.State.Scale).Summary);
        Assert.Empty(sheet.Notes);
    }

    [Fact]
    public void AVersionOneFileIsMigratedAndAnUnknownConventionIsRefused()
    {
        const string version1 = """
            {
              "format": "grouplab-marking-1",
              "image": "scans/mounted/20260329_183028.jpg",
              "scale": "a single 1.5 in reference length, which assumes the photograph is square on and the sheet flat",
              "pointOfAim": { "x": 2000, "y": 1500 },
              "bulls": [],
              "shots": [
                { "id": 1, "image": { "x": 2100.5, "y": 1480 }, "provenance": "Manual", "exclusion": null, "notAShot": false, "bull": null },
                { "id": 3, "image": { "x": 1950, "y": 1600 }, "provenance": "Manual", "exclusion": "PulledShot", "notAShot": false, "bull": null }
              ],
              "report": { "allShots": { "aspectRatio": "NaN" } }
            }
            """;
        var (state, notes) = MarkingFile.Read(version1);
        Assert.Equal(2, state.Shots.Count);
        Assert.Equal(new PointD(2100.5, 1480), state.Shots[0].Image);
        Assert.Equal(ExclusionReason.PulledShot, state.Shots[1].Exclusion);
        Assert.Equal(4, state.NextId);
        Assert.Null(state.Scale);
        Assert.Equal(0, state.ViewQuarterTurns);
        Assert.Contains(notes, n => n.Contains("set the scale again", StringComparison.Ordinal));

        Assert.Throws<MarkingFileException>(() => MarkingFile.Read("""{ "format": "grouplab-marking-9" }"""));
        Assert.Throws<MarkingFileException>(() => MarkingFile.Read("""{ "format": "grouplab-marking-2", "imageFrame": { "name": "displayed" } }"""));
        Assert.Throws<MarkingFileException>(() => MarkingFile.Read("""{ "format": "grouplab-marking-2", "imageFrame": { "name": "stored-pixels" }, "displayRotationDegrees": 45 }"""));
        Assert.Throws<MarkingFileException>(() => MarkingFile.Read("""{ "format": "ballistic" """));
        Assert.Throws<MarkingFileException>(() => MarkingFile.Read("""{ "shots": [] }"""));
    }
}
