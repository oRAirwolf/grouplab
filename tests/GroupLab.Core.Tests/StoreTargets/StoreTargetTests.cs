using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using GroupLab.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Core.Tests.StoreTargets;

/// <summary>
/// NOTES-FROM-PLANNING.md entries 340 and 341: store-bought target recognition ships for the five fingerprinted Birchwood Casey products,
/// the Shoot-N-C bullseye at 6 and 8 inches is a family whose sizes the person is asked about when the picture cannot tell them apart, and
/// the printed size is used as the scale with a warning that is never shown for GroupLab's own sheets.
/// </summary>
[Collection("StoreTargetLibrary")]
public class StoreTargetTests
{
    private const string SixInch = "bc-34550-shoot-n-c-6in-bull";
    private const string EightInch = "bc-34805-shoot-n-c-8in-bull";

    private static StoreTarget T(string id) => StoreTargetLibrary.Find(id)!;

    /// <summary>A product's candidate whose fit puts its inches at <paramref name="ppi"/> picture pixels an inch.</summary>
    private static StoreTargetCandidate C(string id, int features, double layout, double ppi) =>
        new(T(id), features, new Homography([ppi, 0, 40, 0, ppi, 30, 0, 0, 1]), layout);

    /// <summary>Inches on the target between two picture points 100 pixels apart, as an answer's scale reads them.</summary>
    private static double InchesPer100Pixels(StoreTargetMatch match)
    {
        var scale = match.Scale();
        var a = scale.ToTarget(new PointD(200, 200));
        var b = scale.ToTarget(new PointD(300, 200));
        return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    }

    [Fact]
    public void EveryProductShipsAFingerprintAndNothingElse()
    {
        Assert.Equal(5, StoreTargetLibrary.All.Count);
        foreach (var target in StoreTargetLibrary.All)
        {
            var fp = target.Fingerprint;
            Assert.Equal(target.Id, fp.Product);
            Assert.True(fp.Points.Count > 500, $"{target.Id}: {fp.Points.Count} features");
            Assert.NotEmpty(fp.Bulls);
            Assert.True(fp.Layout.Cols >= 12 && fp.Layout.Rows >= 12, target.Id);
            using var stream = typeof(StoreTargetLibrary).Assembly.GetManifestResourceStream(StoreTargetLibrary.ResourceName(target.Id))!;
            Assert.True(stream.Length < 100 * 1024, $"{target.Id}: {stream.Length} bytes");
        }

        // Entry 340 section 1: fingerprints only. No picture of another maker's target is a resource of the application.
        Assert.DoesNotContain(typeof(StoreTargetLibrary).Assembly.GetManifestResourceNames(),
            n => n.Contains("StoreTargets", StringComparison.Ordinal) && !n.EndsWith(".glfp", StringComparison.Ordinal) && n != StoreTargetLibrary.ListResource);
    }

    [Fact]
    public void AFingerprintReadsBackAsItWasWritten()
    {
        var fp = T(EightInch).Fingerprint;
        using var stream = new MemoryStream(fp.ToBytes());
        var read = TargetFingerprint.Read(stream);
        Assert.Equal(fp.Points, read.Points);
        Assert.Equal(fp.Descriptors, read.Descriptors);
        Assert.Equal(fp.Bulls, read.Bulls);
        Assert.Equal(fp.Layout.Lab, read.Layout.Lab);
    }

    [Fact]
    public void TheShootNCBullseyeAt6And8InchesIsTheOnlyFamily()
    {
        Assert.Equal([SixInch, EightInch], StoreTargetLibrary.Members(StoreTargetLibrary.ShootNCBullseye).Select(t => t.Id));
        Assert.Single(StoreTargetLibrary.All.Select(t => t.Family).OfType<string>().Distinct());
        Assert.Equal((6.0, 6.0), T(SixInch).PrintedInches);
        Assert.Equal((8.0, 8.0), T(EightInch).PrintedInches);
    }

    /// <summary>
    /// Entry 340 section 4, the crops that fooled the trial, at the numbers recognition measured on them: each picture is of one member, the
    /// other member fits it almost as well, and the question is asked; each answer gives that member's own scale.
    /// </summary>
    [Theory]
    [InlineData(EightInch, 200, 0.988, 60.0, 72, 0.928, 80.0)] // an 8 in bullseye cut off at two sides, 60 pixels an inch
    [InlineData(SixInch, 145, 0.997, 45.0, 46, 0.988, 33.8)] // a 6 in bullseye in the corner of the frame, 45 pixels an inch
    public void ACropBothSizesFitAsksWhichAndEachAnswerGivesItsOwnScale(string truth, int truthFeatures, double truthLayout, double truthPpi, int otherFeatures, double otherLayout, double otherPpi)
    {
        string other = truth == SixInch ? EightInch : SixInch;
        var seen = StoreTargetRecognizer.Decide([C(truth, truthFeatures, truthLayout, truthPpi), C(other, otherFeatures, otherLayout, otherPpi), C("bc-34806-shoot-n-c-8in-crosshair", 0, 0, 1)]);
        Assert.True(seen.AsksWhichSize);
        Assert.Null(seen.Named);
        var answers = FamilyQuestion.Answers(seen, null);
        Assert.Equal([SixInch, EightInch], answers.Select(a => a.Match.Target.Id));
        foreach (var answer in answers)
        {
            double ppi = answer.Match.Target.Id == truth ? truthPpi : otherPpi;
            Assert.Equal(answer.Match.Target.Id, answer.Match.Scale().PrintedTarget);
            Assert.Equal(100 / ppi, InchesPer100Pixels(answer.Match), 6);
            Assert.Equal(answer.Match.Target.ShortName, answer.Name);
            Assert.Contains($"{answer.Match.Target.PrintedInches.Width:0.#} by", answer.Printed, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The crops the trial's rule alone claimed as the wrong size, with no other member close: the wrong size fitted at 0.87 to 0.90 with
    /// 45 to 104 features. They are asked about too, and the member that did not fit is offered where the other's fit puts it, its own
    /// printed size making it 8/6 the size.
    /// </summary>
    [Theory]
    [InlineData(EightInch, SixInch, 104, 0.900, 88.7)]
    [InlineData(EightInch, SixInch, 54, 0.886, 62.6)]
    [InlineData(SixInch, EightInch, 51, 0.866, 64.6)]
    [InlineData(SixInch, EightInch, 45, 0.868, 47.9)]
    public void ACropOneSizeClaimedAsTheOtherIsAskedAbout(string truth, string claimed, int features, double layout, double ppi)
    {
        var seen = StoreTargetRecognizer.Decide([C(claimed, features, layout, ppi), C(truth, 30, 0.6, ppi * 0.7)]);
        Assert.True(seen.AsksWhichSize);
        var answers = FamilyQuestion.Answers(seen, null);
        var six = answers.Single(a => a.Match.Target.Id == SixInch).Match;
        var eight = answers.Single(a => a.Match.Target.Id == EightInch).Match;
        Assert.Equal(8.0 / 6.0, InchesPer100Pixels(eight) / InchesPer100Pixels(six), 6);
        Assert.Equal(100 / ppi, InchesPer100Pixels(answers.Single(a => a.Match.Target.Id == claimed).Match), 6);
    }

    /// <summary>Entry 340 section 2: when the picture does tell the sizes apart, no question is asked.</summary>
    [Theory]
    [InlineData(SixInch, 146, 0.993, EightInch, 145, 0.597)] // the whole 6 in sheet
    [InlineData(EightInch, 319, 0.996, SixInch, 62, 0.668)] // the whole 8 in sheet
    [InlineData(EightInch, 457, 0.91, SixInch, 0, 0.0)] // a shot 8 in sheet, its layout pulled down by holes and pasters, carried by many features
    public void APictureThatIsClearlyOneSizeAsksNothing(string truth, int features, double layout, string other, int otherFeatures, double otherLayout)
    {
        var seen = StoreTargetRecognizer.Decide([C(truth, features, layout, 100), C(other, otherFeatures, otherLayout, 70)]);
        Assert.False(seen.AsksWhichSize);
        Assert.Equal(truth, seen.Named?.Target.Id);
        Assert.Equal(1.0, InchesPer100Pixels(seen.Named!), 6);
    }

    [Fact]
    public void OutsideAFamilyTheTrialsRuleStands()
    {
        // Below the floor, or two products of no family within the margin: nothing is claimed and nothing asked.
        Assert.False(StoreTargetRecognizer.Decide([C("bc-34806-shoot-n-c-8in-crosshair", 300, 0.84, 50)]).Found);
        Assert.False(StoreTargetRecognizer.Decide([C("bc-34806-shoot-n-c-8in-crosshair", 300, 0.95, 50), C("bc-37826-eze-scorer-bull", 300, 0.9, 50)]).Found);
        Assert.False(StoreTargetRecognizer.Decide([C("bc-34806-shoot-n-c-8in-crosshair", 20, 0.99, 50)]).Found);

        // A product of no family is named by the trial's rule, at its floor.
        Assert.Equal("bc-34105-shoot-n-c-sight-in", StoreTargetRecognizer.Decide([C("bc-34105-shoot-n-c-sight-in", 25, 0.86, 50)]).Named?.Target.Id);
    }

    [Fact]
    public void TheLastAnswerForTheFamilyIsOfferedFirst()
    {
        var seen = StoreTargetRecognizer.Decide([C(EightInch, 200, 0.988, 60), C(SixInch, 72, 0.928, 80)]);
        Assert.Equal([SixInch, EightInch], FamilyQuestion.Answers(seen, null).Select(a => a.Match.Target.Id));
        Assert.Equal([EightInch, SixInch], FamilyQuestion.Answers(seen, EightInch).Select(a => a.Match.Target.Id));
        Assert.Equal(StoreTargetLibrary.ShootNCBullseye, FamilyQuestion.Family(seen));
        Assert.Equal("Which target is this?", FamilyQuestion.Title);
        Assert.Equal("Not sure", FamilyQuestion.NotSure);
    }

    /// <summary>Entry 341 section 2: the warning is in play for a store-bought target's scale, never for a GroupLab sheet's or one drawn by hand.</summary>
    [Fact]
    public void TheWarningFollowsAStoreBoughtScaleAndNeverAGroupLabSheet()
    {
        var session = new MarkingSession();
        session.Open("target.jpg");
        var match = StoreTargetRecognizer.Decide([C(EightInch, 300, 0.99, 50)]).Named!;
        session.PlaceStoreTarget(match, 2000, 2000);
        Assert.True(StoreTargetMatch.InPlay(session.State));
        var bull = Assert.Single(session.State.Bulls);
        Assert.Equal(match.ToImage.Apply(T(EightInch).Fingerprint.Bulls[0]), bull.Image);
        Assert.Contains("printed targets vary a little from sheet to sheet", session.State.Scale!.Description, StringComparison.Ordinal);
        Assert.Contains("Scale from this target's printed size.", StoreTargetMatch.Warning, StringComparison.Ordinal);
        Assert.Contains("check it against a ruler or a GroupLab sheet", StoreTargetMatch.Warning, StringComparison.Ordinal);
        session.Undo();
        Assert.Null(session.State.Scale);
        Assert.Empty(session.State.Bulls);

        var sheet = MarkingState.Empty with { Scale = new SheetReference(new HomographyMapping(Homography.Identity), "test") };
        Assert.False(StoreTargetMatch.InPlay(sheet));
        var drawn = MarkingState.Empty with { Scale = new RectangleReference([new(0, 0), new(100, 0), new(100, 100), new(0, 100)], 1, 1) };
        Assert.False(StoreTargetMatch.InPlay(drawn));
    }

    [Fact]
    public void AMarkingFileKeepsWhereTheScaleCameFrom()
    {
        var match = StoreTargetRecognizer.Decide([C(SixInch, 300, 0.99, 50)]).Named!;
        var state = MarkingState.Empty with { ImagePath = "target.jpg", Scale = match.Scale() };
        string json = MarkingFile.Write(state);
        Assert.Contains("\"printedTarget\": \"" + SixInch + "\"", json, StringComparison.Ordinal);
        var (read, _) = MarkingFile.Read(json);
        Assert.Equal(SixInch, Assert.IsType<RectangleReference>(read.Scale).PrintedTarget);

        // A rectangle drawn by hand is written exactly as before.
        var drawn = state with { Scale = new RectangleReference([new(0, 0), new(100, 0), new(100, 100), new(0, 100)], 1, 1) };
        Assert.DoesNotContain("printedTarget", MarkingFile.Write(drawn), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------
    // The blanks themselves, where they are on this computer. Never on CI: they are another maker's printing and nothing of them is
    // committed but the fingerprints (samples/PROVENANCE.md).

    private const string Blanks = @"C:\Dev\grouplab-local\commercial-targets";

    /// <summary>
    /// A square of the blank as a camera framing it would see it: centred <paramref name="offset"/> of the sheet from the bull, half
    /// <paramref name="half"/> of the sheet across, at <paramref name="ppi"/> pixels an inch, what lies past the scan black.
    /// </summary>
    private static string Crop(string id, double ox, double oy, double half, double ppi, string folder)
    {
        using var blank = Cv2.ImRead(Path.Combine(Blanks, id, "blank.png"), ImreadModes.Color);
        var target = T(id);
        var bull = target.Fingerprint.Bulls[0];
        double side = target.PrintedInches.Width, h = half * side;
        double cx = bull.X + (ox * side), cy = bull.Y + (oy * side);
        var box = new Rect((int)((cx - h) * 600), (int)((cy - h) * 600), (int)(2 * h * 600), (int)(2 * h * 600));
        using var square = new Mat(box.Height, box.Width, MatType.CV_8UC3, Scalar.All(0));
        var inside = box.Intersect(new Rect(0, 0, blank.Width, blank.Height));
        using (var from = new Mat(blank, inside))
        using (var to = new Mat(square, new Rect(inside.X - box.X, inside.Y - box.Y, inside.Width, inside.Height)))
        {
            from.CopyTo(to);
        }

        int px = (int)(2 * h * ppi);
        using var small = new Mat();
        Cv2.Resize(square, small, new Size(px, px), 0, 0, InterpolationFlags.Area);
        string path = Path.Combine(folder, $"{id}-{ox}-{oy}-{half}-{ppi}.jpg");
        Cv2.ImWrite(path, small, new ImageEncodingParam(ImwriteFlags.JpegQuality, 90));
        return Temp.Readable(path);
    }

    /// <summary>
    /// Entry 340 section 4 on the real printing: each family member at crops that fooled the trial's rule asks the question, every answer
    /// sets that member's printed size, and the whole sheet of each, clearly one size, asks nothing and is scaled about its bull to within half a
    /// percent.
    /// </summary>
    [Fact]
    public void OnTheBlanksTheCropsThatFooledTheTrialAskAndAWholeSheetDoesNot()
    {
        if (!Directory.Exists(Blanks))
        {
            return;
        }

        string folder = Temp.Folder("store-targets");
        Directory.CreateDirectory(folder);
        try
        {
            var backend = new OpenCvFingerprintBackend();
            foreach (var (id, ox, oy, half, ppi) in new[]
            {
                // Claimed as the 6 in by the trial's rule alone (0.86, no 8 in fit), and both sizes within the margin (0.985 and 0.998).
                (EightInch, -0.3, -0.1, 0.46, 45.0), (EightInch, 0.3, 0.3, 0.46, 45.0),

                // Claimed as the 8 in by the trial's rule alone, 0.91 against 0.79 and 0.95 against 0.78.
                (SixInch, -0.1, -0.3, 0.46, 75.0), (SixInch, -0.1, -0.1, 0.55, 45.0),
            })
            {
                var seen = StoreTargetRecognizer.Recognize(Crop(id, ox, oy, half, ppi, folder), backend)!;
                Assert.True(seen.AsksWhichSize, $"{id} at ({ox}, {oy}), {half}, {ppi} ppi: {seen.Describe()}");
                Assert.Equal([SixInch, EightInch], FamilyQuestion.Answers(seen, null).Select(a => a.Match.Target.Id));
                Assert.All(seen.Family, m => Assert.Equal(m.Target.Id, m.Scale().PrintedTarget));
            }

            foreach (string id in new[] { SixInch, EightInch })
            {
                var seen = StoreTargetRecognizer.Recognize(Crop(id, 0, 0, 0.5, 100, folder), backend)!;
                Assert.False(seen.AsksWhichSize, seen.Describe());
                Assert.Equal(id, seen.Named?.Target.Id);

                // At 100 pixels an inch, two points an inch either side of the bull are two inches apart on the target.
                var scale = seen.Named!.Scale();
                var bull = seen.Named.ToImage.Apply(T(id).Fingerprint.Bulls[0]);
                var left = scale.ToTarget(new PointD(bull.X - 100, bull.Y));
                var right = scale.ToTarget(new PointD(bull.X + 100, bull.Y));
                double inches = Math.Sqrt(Math.Pow(right.X - left.X, 2) + Math.Pow(right.Y - left.Y, 2));
                Assert.True(Math.Abs(inches - 2) < 0.01, $"{id}: {inches:0.0000} in where 2 were");
            }
        }
        finally
        {
            Temp.Delete(folder);
        }
    }
}
