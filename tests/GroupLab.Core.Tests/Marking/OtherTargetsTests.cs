using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 228, suggested by Unholy (also TNA): several bulls placed by hand on a target GroupLab did not print, and a
/// scale that is right everywhere on a photograph. A 3 by 3 target on Letter, bulls 2.5 in apart, with shots at known offsets, is seen
/// through known perspective transforms. Four known corners recover every offset to a few thousandths of an inch; a scale at each bull
/// does better than one scale for the sheet; the angle is said out loud; and the bulls, their scales and a template survive the file.
/// </summary>
public class OtherTargetsTests
{
    private static readonly PointD[] BullsInches = [.. from r in new[] { 3.0, 5.5, 8.0 } from c in new[] { 1.75, 4.25, 6.75 } select new PointD(c, r)];

    private static readonly PointD[] ShotOffsets = [new(0.30, -0.20), new(-0.25, 0.35), new(0.10, 0.40)];

    /// <summary>A camera looking at the sheet: target inches to image pixels, about 300 pixels an inch, with a tilt set by <paramref name="tilt"/>.</summary>
    private static Func<PointD, PointD> Camera(double tilt, double turn = 0.1)
    {
        double c = Math.Cos(turn), s = Math.Sin(turn);
        return p =>
        {
            double x = (c * p.X) - (s * p.Y), y = (s * p.X) + (c * p.Y);
            double w = 1 + (tilt * y);
            return new PointD((300 * x / w) + 200, (300 * y / w) + 150);
        };
    }

    private static (MarkingSession Session, List<PointD> Truth) Marked(Func<PointD, PointD> camera)
    {
        var session = new MarkingSession();
        foreach (var bull in BullsInches)
        {
            session.AddBull(camera(bull));
        }

        var truth = new List<PointD>();
        foreach (var (bull, i) in BullsInches.Select((b, i) => (b, i)))
        {
            var offset = ShotOffsets[i % ShotOffsets.Length];
            session.AddShot(camera(new PointD(bull.X + offset.X, bull.Y + offset.Y)));
            truth.Add(offset);
        }

        return (session, truth);
    }

    /// <summary>The worst error, in inches, of the offsets GroupLab measures against the true ones, after turning both to a common frame.</summary>
    private static double Worst(MarkingSession session, List<PointD> truth, double turn)
    {
        var measured = GroupAnalysis.CompositeOffsets(session.State, session.State.Shots);
        double c = Math.Cos(turn), s = Math.Sin(turn);
        return measured.Zip(truth, (m, t) =>
        {
            // The rectangle's frame is the paper's own; the per-bull frames are the drawn lengths', which follow the paper too.
            return Math.Sqrt(Math.Pow(m.X - t.X, 2) + Math.Pow(m.Y - t.Y, 2));
        }).Max();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.02)]
    [InlineData(0.05)]
    public void FourKnownCornersRecoverEveryOffset(double tilt)
    {
        var camera = Camera(tilt);
        var (session, truth) = Marked(camera);
        session.SetScale(new RectangleReference([camera(new(0, 0)), camera(new(8.5, 0)), camera(new(8.5, 11)), camera(new(0, 11))], 8.5, 11));

        Assert.All(session.State.Shots, s => Assert.NotNull(s.Bull));
        Assert.True(Worst(session, truth, 0.1) < 0.002, $"worst {Worst(session, truth, 0.1):0.0000} in at tilt {tilt}");
    }

    private static void ScaleEveryBull(MarkingSession session, Func<PointD, PointD> camera, bool both)
    {
        foreach (var (bull, i) in BullsInches.Select((b, i) => (b, i)))
        {
            int index = session.State.Bulls[i].Index;
            var across = new DrawnLength(camera(new(bull.X - 0.5, bull.Y)), camera(new(bull.X + 0.5, bull.Y)), 1.0);
            var upDown = both ? new DrawnLength(camera(new(bull.X, bull.Y - 0.5)), camera(new(bull.X, bull.Y + 0.5)), 1.0) : null;
            session.SetBullScale(index, across, upDown);
        }
    }

    [Fact]
    public void AScaleAtEachBullBeatsOneScaleForTheSheetOnAnAngledPhoto()
    {
        var camera = Camera(0.05, turn: 0);
        var (global, truth) = Marked(camera);
        var middle = BullsInches[4];
        global.SetScale(new LengthReference(camera(new(middle.X - 0.5, middle.Y)), camera(new(middle.X + 0.5, middle.Y)), 1.0));
        double one = Worst(global, truth, 0);

        var (perBull, _) = Marked(camera);
        ScaleEveryBull(perBull, camera, both: true);
        double each = Worst(perBull, truth, 0);

        Assert.True(each < one / 3, $"a scale at each bull {each:0.0000} in, one for the sheet {one:0.0000} in");
        Assert.True(each < 0.01, $"a scale at each bull {each:0.0000} in");
    }

    /// <summary>
    /// A photograph turned a quarter, as two of Alan's 2026-09-26 photographs were: the sheet's up and down runs nearly level in the image,
    /// and a length's direction must not flip from bull to bull on a pixel's tilt. On those photographs the flip put shots 0.75 in wrong.
    /// </summary>
    [Theory]
    [InlineData(1.5708)]
    [InlineData(1.5608)]
    [InlineData(1.5808)]
    [InlineData(3.1416)]
    public void ATurnedPhotoKeepsOneFrameAtEveryBull(double turn)
    {
        var camera = Camera(0.02, turn);
        var (session, truth) = Marked(camera);
        ScaleEveryBull(session, camera, both: true);
        var measured = GroupAnalysis.CompositeOffsets(session.State, session.State.Shots);

        // The same rigid turn for every shot, whatever it is: the sizes and the shape do not depend on the frame.
        double th = Math.Atan2(measured[0].Y, measured[0].X) - Math.Atan2(truth[0].Y, truth[0].X);
        double c = Math.Cos(th), s = Math.Sin(th);
        var worst = measured.Zip(truth, (m, t) => Math.Sqrt(Math.Pow(m.X - ((c * t.X) - (s * t.Y)), 2) + Math.Pow(m.Y - ((s * t.X) + (c * t.Y)), 2))).Max();
        Assert.True(worst < 0.01, $"worst {worst:0.0000} in at a turn of {turn}");
    }

    [Fact]
    public void AnAngledPhotoIsSaidOutLoudAndASquareOnOneIsNot()
    {
        var angled = Marked(Camera(0.05, turn: 0)).Session;
        ScaleEveryBull(angled, Camera(0.05, turn: 0), both: true);
        var pb = (PerBullReference)angled.State.Scale!;
        var said = pb.Checks(b => (b + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(said, s => s.Contains("taken at an angle", StringComparison.Ordinal));
        Assert.True(pb.RelativeUncertainty > 0.01);
        Assert.False(pb.AssumesSquareOn);

        var square = Marked(Camera(0.0, turn: 0)).Session;
        ScaleEveryBull(square, Camera(0.0, turn: 0), both: true);
        var flat = (PerBullReference)square.State.Scale!;
        Assert.Empty(flat.Checks(b => b.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        Assert.True(flat.RelativeUncertainty < 0.001);

        var single = Marked(Camera(0.0, turn: 0)).Session;
        ScaleEveryBull(single, Camera(0.0, turn: 0), both: false);
        Assert.Contains(((PerBullReference)single.State.Scale!).Checks(b => b.ToString(System.Globalization.CultureInfo.InvariantCulture)), s => s.Contains("one length only", StringComparison.Ordinal));
    }

    [Fact]
    public void OneScaleOnAPhotographIsWarnedAbout()
    {
        var length = new LengthReference(new(0, 0), new(300, 0), 1);
        Assert.NotNull(PerBullReference.SingleScaleOnAPhoto(length, new ImageMetadata("JPEG", 4000, 3000, null, null, "samsung", "SM-F966U1", null, null, null)));
        Assert.Null(PerBullReference.SingleScaleOnAPhoto(length, new ImageMetadata("PNG", 5100, 6600, 600, 600, null, null, null, null, null)));
    }

    [Fact]
    public void ShotsGoToTheirNearestBullAndFollowWhenABullGoes()
    {
        var camera = Camera(0.0, turn: 0);
        var (session, _) = Marked(camera);
        Assert.Equal(Enumerable.Range(0, 9), session.State.Shots.Select(s => s.Bull!.Value));

        session.DeleteBull(session.State.Bulls[4].Index);
        Assert.Equal(8, session.State.Bulls.Count);
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8"], session.State.Bulls.Select(b => b.Label));
        Assert.All(session.State.Shots, s => Assert.Contains(session.State.Bulls, b => b.Index == s.Bull));

        // A shot put on a bull by hand stays there when the bulls move.
        int shot = session.State.Shots[0].Id;
        session.AssignBulls([shot], session.State.Bulls[7].Index);
        session.MoveBull(session.State.Bulls[0].Index, camera(new(1.8, 3.0)));
        Assert.Equal(session.State.Bulls[7].Index, session.State.Find(shot)!.Bull);
    }

    [Fact]
    public void ATemplatePlacesTheRestFromTheFirstTwo()
    {
        var first = Camera(0.0, turn: 0);
        var (made, _) = Marked(first);
        ScaleEveryBull(made, first, both: true);
        var template = BullTemplate.From("Commercial 3x3", made.State)!;

        var next = Camera(0.0, turn: 0.2);
        var session = new MarkingSession();
        session.AddBull(next(BullsInches[0]));
        session.AddBull(next(BullsInches[1]));
        Assert.Equal(7, session.ApplyTemplate(template));
        foreach (var (bull, i) in BullsInches.Select((b, i) => (b, i)))
        {
            var want = next(bull);
            var got = session.State.Bulls[i].Image;
            Assert.True(Math.Abs(want.X - got.X) < 0.5 && Math.Abs(want.Y - got.Y) < 0.5, $"bull {i + 1}: {got} against {want}");
        }
    }

    [Fact]
    public void ThePlacedBullsAndTheirScalesSurviveTheFile()
    {
        var camera = Camera(0.05, turn: 0);
        var (session, _) = Marked(camera);
        ScaleEveryBull(session, camera, both: true);
        var (read, _) = MarkingFile.Read(MarkingFile.Write(session.State));
        Assert.IsType<PerBullReference>(read.Scale);
        Assert.Equal(9, read.Bulls.Count);
        var before = GroupAnalysis.CompositeOffsets(session.State, session.State.Shots);
        var after = GroupAnalysis.CompositeOffsets(read, read.Shots);
        Assert.All(before.Zip(after), p => Assert.True(Math.Abs(p.First.X - p.Second.X) < 1e-9 && Math.Abs(p.First.Y - p.Second.Y) < 1e-9));
    }
}
