using System.Text.Json;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Tests.Marking;

/// <summary>
/// The marking screen's model, NOTES-FROM-PLANNING.md entry 21 section 3 and DESIGN.md section 13, without a screen: the two
/// manual scales, undo and redo, provenance, exclusion reported both ways, the composite group, and the export.
/// </summary>
public class MarkingSessionTests
{
    [Fact]
    public void AReferenceLengthIsAUniformScale()
    {
        var scale = new LengthReference(new PointD(100, 100), new PointD(100, 350), 1);
        var p = scale.ToTarget(new PointD(500, 250));
        Assert.Equal(2.0, p.X, 12);
        Assert.Equal(1.0, p.Y, 12);
        Assert.True(scale.AssumesSquareOn);
    }

    /// <summary>Entry 21 section 4: a rectangle photographed off axis maps back to the target without the perspective.</summary>
    [Fact]
    public void AReferenceRectangleRemovesPerspectiveExactly()
    {
        // A 4 by 3 in rectangle seen through a perspective that shrinks its far edge.
        var camera = new Homography([180, 30, 400, 10, 150, 300, 0.02, 0.05, 1]);
        PointD[] corners = [camera.Apply(new PointD(0, 0)), camera.Apply(new PointD(4, 0)), camera.Apply(new PointD(4, 3)), camera.Apply(new PointD(0, 3))];
        var scale = new RectangleReference(corners, 4, 3);
        var inside = scale.ToTarget(camera.Apply(new PointD(1.25, 2.5)));
        Assert.Equal(1.25, inside.X, 9);
        Assert.Equal(2.5, inside.Y, 9);
        Assert.False(scale.AssumesSquareOn);
    }

    [Fact]
    public void EveryChangeCanBeUndoneAndRedone()
    {
        var session = new MarkingSession();
        session.Open("target.jpg");
        int id = session.AddShot(new PointD(10, 10));
        session.MoveShot(id, new PointD(20, 20));
        Assert.Equal(new PointD(20, 20), session.State.Find(id)!.Image);
        session.Undo();
        Assert.Equal(new PointD(10, 10), session.State.Find(id)!.Image);
        session.Undo();
        Assert.Empty(session.State.Shots);
        Assert.False(session.CanUndo);
        session.Redo();
        session.Redo();
        Assert.Equal(new PointD(20, 20), session.State.Find(id)!.Image);
        Assert.False(session.CanRedo);
    }

    /// <summary>DESIGN.md section 13: provenance per shot, automatic, automatic then corrected, or manual.</summary>
    [Fact]
    public void ADetectedShotTheUserTouchesBecomesCorrectedAndHandPlacedShotsSurviveANewDetection()
    {
        var session = new MarkingSession();
        int manual = session.AddShot(new PointD(1, 1));
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);
        session.LoadDetections(scale, [new BullAim(0, "1", new PointD(50, 50))], [(new PointD(40, 40), 0), (new PointD(60, 60), 0)], "test");
        var detected = session.State.Shots.Where(s => s.Provenance == ShotProvenance.Automatic).ToList();
        Assert.Equal(2, detected.Count);
        Assert.NotNull(session.State.Find(manual));

        session.AssignBull(detected[0].Id, null);
        session.MoveShot(detected[1].Id, new PointD(61, 61));
        session.MoveShot(manual, new PointD(2, 2));
        Assert.All(detected, d => Assert.Equal(ShotProvenance.Corrected, session.State.Find(d.Id)!.Provenance));
        Assert.Equal(ShotProvenance.Manual, session.State.Find(manual)!.Provenance);
    }

    /// <summary>
    /// docs/STATISTICS.md section 10: an excluded shot needs a reason and every report prints the full and reduced figures side by
    /// side; a detection marked as not a shot is in neither. The headline is mean radius from the engine of M3.
    /// </summary>
    [Fact]
    public void ExclusionsAreReportedBothWaysAndNotAShotIsInNeither()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        PointD[] image = [new(100, 100), new(130, 110), new(95, 140), new(120, 125), new(110, 90), new(300, 300)];
        var ids = image.Select(p => session.AddShot(p)).ToList();
        int handwriting = session.AddShot(new PointD(900, 900));
        session.SetNotAShot(handwriting, true);
        session.SetExclusion(ids[^1], ExclusionReason.CalledFlyer);

        var report = GroupAnalysis.Analyse(session.State);
        Assert.Equal(1, report.Excluded);
        Assert.Equal(1, report.NotShots);
        Assert.Equal(6, report.AllShots!.Shots);
        Assert.Equal(5, report.WithoutExclusions!.Shots);

        var inches = image.Select(p => new PointD(p.X / 100, p.Y / 100)).ToList();
        var expected = GroupStatistics.Rayleigh(inches);
        Assert.Equal(expected.MeanRadius.Value, report.AllShots.MeanRadius!.Value, 12);
        Assert.Equal(expected.MeanRadius.Lower, report.AllShots.MeanRadius.Lower!.Value, 12);
        Assert.True(report.WithoutExclusions.MeanRadius!.Value < report.AllShots.MeanRadius.Value);
        Assert.Null(report.AllShots.CentreFromAim);
    }

    /// <summary>docs/STATISTICS.md section 2: the composite group is each shot's offset from its own bull.</summary>
    [Fact]
    public void ShotsAssignedToBullsFormTheCompositeGroup()
    {
        var session = new MarkingSession();
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);
        PointD[] offsets = [new(5, 0), new(-3, 4), new(0, -6), new(2, 2)];
        var bulls = new[] { new BullAim(0, "1", new PointD(200, 200)), new BullAim(1, "2", new PointD(500, 200)) };
        var detections = bulls.SelectMany(b => offsets.Take(2).Select(o => (new PointD(b.Image.X + o.X, b.Image.Y + o.Y), (int?)b.Index)))
            .Concat(offsets.Skip(2).Select(o => (new PointD(bulls[0].Image.X + o.X, bulls[0].Image.Y + o.Y), (int?)0)))
            .ToList();
        session.LoadDetections(scale, bulls, detections, "test");

        var report = GroupAnalysis.Analyse(session.State);
        var composite = detections.Select(d => new PointD((d.Item1.X - bulls[d.Item2!.Value].Image.X) / 100, (d.Item1.Y - bulls[d.Item2.Value].Image.Y) / 100)).ToList();
        Assert.Equal(GroupGeometry.MaximumPairDistance(composite).Distance, report.AllShots!.ExtremeSpread!.Value, 12);
        Assert.NotNull(report.AllShots.CentreFromAim);
        Assert.Equal(GroupStatistics.Centre(composite).X, report.AllShots.CentreFromAim!.Value.X, 12);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 24 section 1: two shots print no dispersion figure, only what is missing, while the count and the
    /// centre from the aim, which are exact at any count, are still reported. At the minimum the figures appear, each interval
    /// labelled with its actual coverage rather than a bare 95 percent.
    /// </summary>
    [Fact]
    public void BelowTheMinimumTheReportWithholdsDispersionButKeepsTheCentre()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(100, 100));
        session.AddShot(new PointD(150, 120));
        session.AddShot(new PointD(190, 60));

        var two = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.Equal(2, two.Shots);
        Assert.Null(two.MeanRadius);
        Assert.Null(two.Sigma);
        Assert.Null(two.ExtremeSpread);
        Assert.NotNull(two.MeanRadiusUnavailable);
        Assert.Contains("At least 5", two.DispersionWithheld, StringComparison.Ordinal);
        Assert.Equal(0.7, two.CentreFromAim!.Value.X, 12);
        Assert.Equal(-0.1, two.CentreFromAim.Value.Y, 12);

        session.AddShot(new PointD(160, 90));
        session.AddShot(new PointD(170, 110));
        Assert.Null(GroupAnalysis.Analyse(session.State).AllShots!.MeanRadius);
        session.AddShot(new PointD(140, 100));
        var five = GroupAnalysis.Analyse(session.State).AllShots!;
        Assert.Null(five.DispersionWithheld);
        Assert.NotNull(five.MeanRadius);
        Assert.Equal(IntervalCoverage.RayleighSigma(5), five.MeanRadius.Coverage!.Value, 15);
        Assert.True(five.MeanRadius.Coverage < 0.95);
        Assert.Equal(0.95, five.ExtremeSpread!.Coverage);
        Assert.Equal(SampleSize.SigmaIntervalMultiples(5).Upper, five.TrueSizeRange!.Upper, 15);

        // Entry 76 section 1: the aspect is printed beside what five circular shots give, and how often they give more.
        Assert.Equal(CircularAspect.Median(5), five.CircularMedianAspect!.Value, 15);
        Assert.Equal(CircularAspect.ProbabilityAbove(5, five.AspectRatio!.Value), five.CircularAspectExceedance!.Value, 15);
    }

    /// <summary>
    /// Entry 24 section 3: an undefined figure exports as null with a sibling saying why, never as the string "NaN", at two shots and
    /// on a five-shot group in a straight line, whose error ellipse has no shape.
    /// </summary>
    [Fact]
    public void TheExportCarriesNullWithAReasonAndNeverNaN()
    {
        var session = new MarkingSession();
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.AddShot(new PointD(100, 100));
        session.AddShot(new PointD(130, 100));
        string two = GroupAnalysis.Export(session.State);
        Assert.DoesNotContain("NaN", two, StringComparison.Ordinal);
        using (var json = JsonDocument.Parse(two))
        {
            var figures = json.RootElement.GetProperty("report").GetProperty("allShots");
            Assert.Equal(JsonValueKind.Null, figures.GetProperty("aspectRatio").ValueKind);
            Assert.Equal("needs at least 3 shots", figures.GetProperty("aspectRatioUnavailable").GetString());
            Assert.Equal(JsonValueKind.Null, figures.GetProperty("meanRadius").ValueKind);
            Assert.Equal(JsonValueKind.Null, figures.GetProperty("centreFromAim").ValueKind);
            Assert.False(string.IsNullOrEmpty(figures.GetProperty("centreFromAimUnavailable").GetString()));
        }

        session.AddShot(new PointD(160, 100));
        session.AddShot(new PointD(190, 100));
        session.AddShot(new PointD(220, 100));
        string line = GroupAnalysis.Export(session.State);
        Assert.DoesNotContain("NaN", line, StringComparison.Ordinal);
        using var lineJson = JsonDocument.Parse(line);
        var lineFigures = lineJson.RootElement.GetProperty("report").GetProperty("allShots");
        Assert.Equal(JsonValueKind.Number, lineFigures.GetProperty("meanRadius").GetProperty("value").ValueKind);
        Assert.Equal(JsonValueKind.Null, lineFigures.GetProperty("aspectRatio").ValueKind);
        Assert.Contains("line", lineFigures.GetProperty("aspectRatioUnavailable").GetString(), StringComparison.Ordinal);
        Assert.Equal(JsonValueKind.Null, lineFigures.GetProperty("circularAspectExceedance").ValueKind);
        Assert.Contains("line", lineFigures.GetProperty("circularAspectExceedanceUnavailable").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutAScaleTheReportSaysWhatIsMissing()
    {
        var session = new MarkingSession();
        session.AddShot(new PointD(1, 1));
        session.AddShot(new PointD(2, 2));
        var report = GroupAnalysis.Analyse(session.State);
        Assert.Null(report.AllShots);
        Assert.Contains("scale", report.Problem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheExportRecordsEveryShotWithItsProvenanceAndTheScaleAssumption()
    {
        var session = new MarkingSession();
        session.Open("group.jpg");
        session.SetScale(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1));
        session.SetPointOfAim(new PointD(100, 100));
        int a = session.AddShot(new PointD(110, 105));
        session.AddShot(new PointD(90, 95));
        session.AddShot(new PointD(104, 88));
        session.SetExclusion(a, ExclusionReason.PulledShot);

        using var json = JsonDocument.Parse(GroupAnalysis.Export(session.State));
        var root = json.RootElement;
        Assert.Equal(MarkingFile.Format, root.GetProperty("format").GetString());
        Assert.Equal("length", root.GetProperty("scale").GetProperty("kind").GetString());
        Assert.Equal(1, root.GetProperty("scale").GetProperty("inches").GetDouble());
        Assert.True(root.GetProperty("scaleAssumesSquareOn").GetBoolean());
        Assert.Equal(3, root.GetProperty("shots").GetArrayLength());
        Assert.Equal("PulledShot", root.GetProperty("shots")[0].GetProperty("exclusion").GetString());
        Assert.Equal("Manual", root.GetProperty("shots")[1].GetProperty("provenance").GetString());
        Assert.Equal(3, root.GetProperty("report").GetProperty("allShots").GetProperty("shots").GetInt32());
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 80 section 5: a marking records what its detection ran with, a calibre or its absence, apart from the
    /// calibre named now, and the file and the report both carry it. A marking nothing detected records no detection at all.
    /// </summary>
    [Fact]
    public void AMarkingRecordsTheCalibreItsDetectionRanWithIncludingNone()
    {
        var bulls = new[] { new BullAim(0, "1", new PointD(100, 100)) };
        var assigned = new AssignedShot(0, 0, 0, 0, 0, double.PositiveInfinity, false);
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);

        var none = new MarkingSession();
        none.Open("sheet.png");
        none.LoadDetections(scale, bulls, [new DetectedShot(new PointD(110, 100), assigned)], null, [], "test", new DetectionRecord(null, null));
        using (var json = JsonDocument.Parse(MarkingFile.Write(none.State)))
        {
            var detection = json.RootElement.GetProperty("detection");
            Assert.Equal(JsonValueKind.Null, detection.GetProperty("calibre").ValueKind);
            Assert.Contains("without a caliber", detection.GetProperty("description").GetString(), StringComparison.Ordinal);
            Assert.Contains("without a caliber", json.RootElement.GetProperty("report").GetProperty("detection").GetString(), StringComparison.Ordinal);
        }

        var named = new MarkingSession();
        named.Open("sheet.png");
        var calibre = Calibre.Of(0.308);
        named.LoadDetections(scale, bulls, [new DetectedShot(new PointD(110, 100), assigned, 0.5, new DetectedOversize(1.87, true))], null, [], "test", new DetectionRecord(calibre, 0.2908));
        named.SetCalibre(Calibre.Of(0.224));
        var (read, _) = MarkingFile.Read(MarkingFile.Write(named.State));
        Assert.Equal(new DetectionRecord(calibre, 0.2908), read.Detection);
        Assert.Equal(new DetectedOversize(1.87, true), read.Shots.Single().Oversize);
        Assert.Equal(0.224, read.Calibre!.DiameterInches, 9);
        Assert.Contains("with the caliber .308", GroupAnalysis.Analyse(read).Detection, StringComparison.Ordinal);

        var manual = new MarkingSession();
        manual.Open("group.jpg");
        manual.SetScale(scale);
        manual.AddShot(new PointD(10, 10));
        using var plain = JsonDocument.Parse(MarkingFile.Write(manual.State));
        Assert.Equal(JsonValueKind.Null, plain.RootElement.GetProperty("detection").ValueKind);
        Assert.Null(GroupAnalysis.Analyse(manual.State).Detection);
    }

    [Fact]
    public void ARoughClickSnapsToTheHoleItIsNear()
    {
        const int size = 100;
        var pixels = Enumerable.Repeat((byte)230, size * size).ToArray();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (((x - 60.0) * (x - 60.0)) + ((y - 45.0) * (y - 45.0)) <= 36)
                {
                    pixels[(y * size) + x] = 40;
                }
            }
        }

        var snapped = Snapping.ToDarkCentroid(new GrayImage(size, size, pixels), new PointD(55, 50), 15);
        Assert.Equal(60, snapped.X, 1);
        Assert.Equal(45, snapped.Y, 1);
    }

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 70 section 4: what the matching decided reaches the marking. Each detected shot keeps its figures in inches
    /// under the id it is given, a hand-placed shot kept from before has none, the refused candidates come through, and undo takes it all back.
    /// </summary>
    [Fact]
    public void TheMatchingsFiguresAndTheRefusedCandidatesReachTheMarkingUnderTheShotIds()
    {
        var scale = new LengthReference(new PointD(0, 0), new PointD(100, 0), 1);
        var bulls = new[] { new BullAim(0, "1", new PointD(200, 200), Declared: new PointD(254, 254)), new BullAim(1, "2", new PointD(500, 200), Declared: new PointD(635, 254)) };
        var session = new MarkingSession();
        int hand = session.AddShot(new PointD(190, 190));
        var assignment = new ShotAssignmentResult(AssignmentMethod.OneToOne, "as many shots as bulls",
        [
            new AssignedShot(0, 1, 104.14, 0, 159.258, 105.918, true),
            new AssignedShot(1, 0, 25.4, 0, 25.4, 381, false),
        ]);
        DetectedShot[] detections = [new(new PointD(330, 200), assignment.Shots[0]), new(new PointD(210, 210), assignment.Shots[1])];
        RejectedCandidate[] rejected = [new(new PointD(700, 700), 0.14, "below the size gate")];

        session.LoadDetections(scale, bulls, detections, assignment, rejected, "test");

        var review = session.State.Assignment!;
        Assert.Equal(AssignmentMethod.OneToOne, review.Method);
        Assert.Null(review.For(hand));
        var ids = session.State.Shots.Where(s => s.Provenance == ShotProvenance.Automatic).Select(s => s.Id).ToList();
        var contested = review.For(ids[0])!;
        Assert.Equal((1, 0), (contested.Bull!.Value, contested.NearestBull));
        Assert.Equal(0.41, contested.DistanceInches, 6);
        Assert.Equal(0.627, contested.NearestInches, 6);
        Assert.Equal(0.417, contested.MarginInches, 6);
        Assert.Equal(1, review.NeedingReview);
        Assert.Equal("below the size gate", Assert.Single(review.Rejected).Reason);

        session.Undo();
        Assert.Null(session.State.Assignment);
    }
}
