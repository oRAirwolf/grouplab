using System.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 291, the second camera sitting on the Fold 7: what a test can hold of sections 2.1, 3.1, 3.2 and 3.5. The
/// real pictures that set the numbers stay on the computer that measured them; these hold the rules to rendered and synthetic frames.
/// </summary>
public class SecondSittingTests
{
    private static readonly IReadOnlyList<GroupLab.Core.Gltd.Model.TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    /// <summary>The committed 600 dpi sample, photographed at an angle: its far edge a third narrower, at about the Fold 7's working size.</summary>
    private static GrayImage OffAxis()
    {
        var (sample, _) = ImageLoader.Load(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"));
        double w = sample.Width, h = sample.Height;
        // The sheet's top edge drawn a third narrower than its bottom, the whole about 2400 pixels tall: a picture taken from below, at an angle.
        var source = new List<PointD> { new(0, 0), new(w, 0), new(w, h), new(0, h) };
        var target = new List<PointD> { new(700, 300), new(1900, 300), new(2200, 2300), new(400, 2300) };
        var transform = HomographyEstimate.Fit(source, target)!;
        var warped = new OpenCvSharpBackend().WarpPerspective(sample, transform, 2600, 2600);
        return warped;
    }

    [Fact]
    public void AnOffAxisPictureIsNamedFromItsCodesTurnedSquareOnWithoutTheSlowSearch()
    {
        var trace = new TraceRecorder();
        var clock = Stopwatch.StartNew();
        var identity = SheetIdentification.Identify(OffAxis(), Library, new OpenCvSharpBackend(), trace);
        Assert.True(identity.Failure is null, identity.Failure);
        var stage = Assert.Single(trace.Records, r => r.Stage == "S0.identify");
        Assert.Contains("square on", stage.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain(stage.Details, d => d.Contains("times full resolution", StringComparison.Ordinal));
        Assert.Equal(2, identity.CodesRead);
        Assert.True(clock.ElapsedMilliseconds < 15_000, $"{clock.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void EachCodeViewIsCenteredOnItsCodeAndViewsOfOneCodeCountOnce()
    {
        var (sample, _) = ImageLoader.Load(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"));
        var views = LiveSheet.CodeViews(sample, Library, new OpenCvSharpBackend());
        var backend = new OpenCvSharpBackend();
        var read = views.Where(v => backend.ReadCodes(v.Image, 1.0).Count > 0).ToList();
        var distinct = new List<CodeView>();
        foreach (var view in read.Where(v => !distinct.Exists(d => d.SameCodeAs(v))))
        {
            distinct.Add(view);
        }

        Assert.Equal(2, distinct.Count);
    }

    [Fact]
    public void TheWaitingLineNamesTheStepAsItStarts()
    {
        var trace = new TraceRecorder();
        var begun = new List<string>();
        trace.Begun += begun.Add;
        using (trace.Begin("S0.identify"))
        {
            Assert.Equal(["S0.identify"], begun);
            Assert.Empty(trace.Records);
        }

        foreach (string stage in new[] { "S0.identify", "S3.register", "P0.bulls", "S5-S8.holes", "S10.group" })
        {
            Assert.NotNull(StageWords.During(stage));
        }

        Assert.Equal("Reading the sheet's codes…", StageWords.During("S0.identify"));
    }

    [Fact]
    public void ASidewaysShakeIsSeenInTheWorseDirection()
    {
        // Black squares on white, sharp; then smeared 6 pixels sideways, as a hand's shake does.
        var pixels = new byte[400 * 400];
        Array.Fill(pixels, (byte)230);
        for (int by = 20; by < 380; by += 40)
        {
            for (int bx = 20; bx < 380; bx += 40)
            {
                for (int y = by; y < by + 20; y++)
                {
                    for (int x = bx; x < bx + 20; x++)
                    {
                        pixels[(y * 400) + x] = 20;
                    }
                }
            }
        }

        var sharp = new GrayImage(400, 400, pixels);
        var smeared = new byte[pixels.Length];
        for (int y = 0; y < 400; y++)
        {
            for (int x = 0; x < 400; x++)
            {
                int sum = 0;
                for (int d = -3; d < 3; d++)
                {
                    sum += pixels[(y * 400) + Math.Clamp(x + d, 0, 399)];
                }

                smeared[(y * 400) + x] = (byte)(sum / 6);
            }
        }

        var shaken = new GrayImage(400, 400, smeared);
        Assert.True(CaptureQualities.DirectionalBlur(sharp) < 0.5, $"{CaptureQualities.DirectionalBlur(sharp)}");
        Assert.True(CaptureQualities.DirectionalBlur(shaken) > 1.2, $"{CaptureQualities.DirectionalBlur(shaken)}");
        Assert.True(CaptureQualities.DirectionalBlur(shaken) > 2 * CaptureQualities.Blur(shaken), "the steepest edges alone barely see a shake");
    }

    /// <summary>A registered frame: square, well lit, 25 pixels an inch across the stream's least corner unless changed.</summary>
    private static FrameVerdict Frame(double printedRoom, double modulePixels, int markersRead = 34, double blurInches = 0.002, double edgeRoom = -0.02)
    {
        var quality = new CaptureQuality(20, "poor", 0.01, 0.4, 0, 200, 1, 5, 1, 100, 0.5, markersRead, 34, 0, blurInches);
        return new FrameVerdict(Instruction.HoldSteadier, "Hold steadier.", true, true, true, false, true, markersRead >= 31, quality, markersRead, 34, 0, 2, EdgeRoom: edgeRoom,
            PrintedRoom: printedRoom, ModulePixels: modulePixels, MarkerPixels: modulePixels * 10, MarkersInFrame: 34);
    }

    private static Instruction Settled(FrameVerdict frame, double scale = 2.27)
    {
        var steadier = new GuidanceSteadier();
        long now = 0;
        Instruction said = Instruction.FindTheSheet;
        for (int i = 0; i < 12; i++)
        {
            said = steadier.Next(frame, now += 100, scale).Say;
        }

        return said;
    }

    [Fact]
    public void MoveBackOnlyWhenThePrintingRunsOutOfTheFrameNotThePaper()
    {
        // The paper's corner 2 percent past the edge, the printing 4 percent inside: in the frame.
        Assert.Equal(Instruction.Ready, Settled(Frame(0.04, 1.3)));
        Assert.Equal(Instruction.MoveBack, Settled(Frame(-0.01, 1.3)));
    }

    [Fact]
    public void FewMarkersReadInTheStreamIsNotHoldSteadier()
    {
        // The second sitting's "Hold steadier" frames read 7 to 13 of 34 markers; the pictures taken after them read all 34.
        Assert.Equal(Instruction.Ready, Settled(Frame(0.05, 1.3, markersRead: 9)));
    }

    [Fact]
    public void CloserIsSaidFromTheModulePixelsThePictureWillGet()
    {
        // 1.0 stream pixel a module is 2.27 in the picture, under the 2.4 that enters the band; 1.1 is 2.5.
        Assert.Equal(Instruction.MoveCloser, Settled(Frame(0.05, 1.0)));
        Assert.Equal(Instruction.Ready, Settled(Frame(0.05, 1.1)));
    }

    [Fact]
    public void HoldSteadierOnlyForAShakeThePictureCannotMeasureThrough()
    {
        // At 100 pixels an inch, 0.003 in of blur is 0.3 stream pixel, all of it the stream's own: steady.
        Assert.Equal(Instruction.Ready, Settled(Frame(0.05, 1.3, blurInches: 0.003)));
        // 0.012 in is 1.2 stream pixels, 2.6 in the picture, past the 0.6 from which a frame is shaky.
        Assert.Equal(Instruction.HoldSteadier, Settled(Frame(0.05, 1.3, blurInches: 0.012)));

        // A frame too smeared to fit the markers to is told so, and never "Move back" from the paper's outline alone.
        var smeared = new FrameVerdict(Instruction.MoveBack, "Move back.", false, true, false, false, false, false, FrameBlurPixels: 1.5);
        Assert.Equal(Instruction.HoldSteadier, Settled(smeared));
        Assert.Equal(Instruction.FindTheSheet, Settled(smeared with { FrameBlurPixels = 0.3 }));
    }

    [Fact]
    public void TheLiveFrameForetellsThePicturesMarkersFromTheirSize()
    {
        var frame = Frame(0.05, 1.0, markersRead: 9);
        // Markers 10 stream pixels across, 22.7 in the picture: every marker in the frame will be read.
        Assert.Equal(34, frame.MarkersPredicted(2.27));
        // 1.2 times: 12 pixels, under the 14 a marker needs; only those the stream read are foretold.
        Assert.Equal(9, frame.MarkersPredicted(1.2));
    }

    [Fact]
    public void ARegisteredSheetIsShownUprightWhateverThePictureTags()
    {
        // A portrait page drawn on its side: page x runs down the picture and page y runs right to left.
        double w = 3000, h = 2000;
        var pageToImage = new Homography([0, -1, 2500, 1, 0, 200, 0, 0, 1]);
        var sheet = new SheetReference(new HomographyMapping(pageToImage.Inverse()), "test");
        int turns = ViewRotation.Upright(sheet, w, h, fallback: 0);
        var top = ViewRotation.ToDisplay(pageToImage.Apply(new PointD(1000, 100)), turns, w, h);
        var bottom = ViewRotation.ToDisplay(pageToImage.Apply(new PointD(1000, 2000)), turns, w, h);
        Assert.True(bottom.Y > top.Y && Math.Abs(bottom.X - top.X) < 1, $"{turns}: {top} {bottom}");
        Assert.Equal(3, ViewRotation.Upright(new LengthReference(new PointD(0, 0), new PointD(100, 0), 1), w, h, fallback: 3));
    }
}
