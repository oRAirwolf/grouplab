using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Mobile.Dev;
using GroupLab.Tests.Support;
using OpenCvSharp;

namespace GroupLab.Mobile.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 3: GroupLab Dev's replay camera. A clip is recorded as the phone records one, read back, and
/// played through the code both phones' cameras run (<see cref="CameraJudge"/>): Guided mode takes the picture once the frames have been
/// judged ready for <see cref="AutoShutter.SteadyMs"/>, and never while the hand shakes. The frames are made here from the committed 600
/// dpi sample, framed as the stream would see it, and written only in this test's own temporary folder.
/// </summary>
public class CameraReplayTests
{
    private static readonly IReadOnlyList<TargetDefinition> Library = SheetIdentification.Candidates([Repo.PathTo("targets")]);

    /// <summary>The Fold 7's still, whose working copy the guidance judges resolution at (entry 281 section 1.4).</summary>
    private static readonly (int Width, int Height) Still = (4080, 3060);

    /// <summary>
    /// The sample framed as the analysis stream sees it: 1920 by 1440, the sheet upright in the middle with room all round, its paper a
    /// little under white as a lit sheet is; smeared sideways by <paramref name="shake"/> pixels, as a hand's shake does, where asked.
    /// </summary>
    private static GrayImage Frame(GrayImage sample, int shake = 0)
    {
        double w = sample.Width, h = sample.Height;
        var source = new List<PointD> { new(0, 0), new(w, 0), new(w, h), new(0, h) };
        var target = new List<PointD> { new(458, 70), new(1462, 70), new(1462, 1370), new(458, 1370) };
        var framed = new OpenCvSharpBackend().WarpPerspective(sample, HomographyEstimate.Fit(source, target)!, 1920, 1440);
        byte[] pixels = framed.Pixels.Select(p => (byte)(p * 0.85)).ToArray();
        if (shake > 1)
        {
            using var mat = Mat.FromPixelData(1440, 1920, MatType.CV_8UC1, pixels);
            using var smeared = new Mat();
            Cv2.Blur(mat, smeared, new Size(shake, 1));
            return OpenCvSharpBackend.Copy(smeared);
        }

        return new GrayImage(1920, 1440, pixels);
    }

    private static GrayImage Sample() => ImageLoader.Load(Repo.PathTo("samples", "gl-cf25-ltr-d-25-shots-600-dpi.png"), 2.0).Image;

    /// <summary>A clip recorded frame by frame as a phone's camera records it, <paramref name="everyMs"/> apart, and read back from its folder.</summary>
    private static CameraClip Record(string folder, IReadOnlyList<GrayImage> frames, long everyMs)
    {
        var recorder = new ClipRecorder(folder, "test", seconds: 30);
        for (int i = 0; i < frames.Count; i++)
        {
            recorder.Offer(frames[i], 5000 + (i * everyMs), (0.1, -0.2, 9.8), 1, 5, Still);
        }

        string written = recorder.Write(new DateTime(2026, 9, 30, 10, 15, 0, DateTimeKind.Local))!;
        var clip = CameraClip.Open(written, out string? why);
        Assert.True(clip is not null, why);
        return clip;
    }

    /// <summary>The clip played through the cameras' own code, frame by frame, at its recorded times.</summary>
    private static List<CameraStep> Replay(CameraClip clip)
    {
        var judge = new CameraJudge(() => Library);
        judge.ResetTorch(5);
        var steps = new List<CameraStep>();
        new ClipPlayer(clip, (_, grey, _, ms) => steps.Add(judge.Next(grey, () => ms, clip.MeasuredScale(grey), torchOnAuto: true, manual: false))).PlayNow();
        return steps;
    }

    private static string Said(List<CameraStep> steps) => string.Join(", ", steps.Select(s => $"{s.NowMs}:{s.Decided}/{s.Verdict.Say}{(s.Fire ? " fires" : "")} m{s.Verdict.MarkersRead} c{s.Verdict.CodesRead} b{s.Verdict.FrameBlurPixels:0.00}"));

    [Fact]
    public void ASteadyClipIsTakenOnceItsFramesHaveBeenReadyLongEnough()
    {
        string folder = Temp.Folder("camera-clip");
        try
        {
            var sharp = Frame(Sample());
            var clip = Record(folder, [sharp, sharp, sharp, sharp, sharp], 300);

            // The clip as written: times from its first frame, the level and the torch as they were, and the frames made smaller.
            Assert.Equal(5, clip.Frames.Count);
            Assert.Equal([0L, 300, 600, 900, 1200], clip.Frames.Select(f => f.Ms));
            Assert.Equal((0.1, -0.2, 9.8), clip.Frames[0].Gravity);
            Assert.Equal((1, 5), (clip.Frames[0].Torch, clip.Frames[0].TorchOf));
            Assert.Equal(Still, clip.Picture);
            Assert.Equal((1920, 1440), clip.Stream);
            Assert.Equal(ClipRecorder.DefaultWidth, clip.Frame(0).Width);

            // Ready from the first frame; taken at the third, the first with two frames judged ready over at least 600 ms.
            var steps = Replay(clip);
            Assert.True(steps.All(s => s.Decided == Instruction.Ready), Said(steps));
            Assert.Equal(2, steps.FindIndex(s => s.Fire));
            Assert.True(steps[2].SteadyMs >= AutoShutter.SteadyMs, Said(steps));
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    [Fact]
    public void AnUnsteadyClipIsNeverTaken()
    {
        string folder = Temp.Folder("camera-clip");
        try
        {
            var sample = Sample();
            var sharp = Frame(sample);
            // Smeared 3 pixels: still registered, and judged shaken. A frame smeared so far that it no longer registers is a stumble, which
            // neither counts nor starts the wait again (entry 311 section 1), so it would not stop the shutter.
            var shaken = Frame(sample, shake: 3);
            var clip = Record(folder, [sharp, shaken, sharp, shaken, sharp, shaken, sharp], 300);
            var steps = Replay(clip);
            Assert.True(steps.Exists(s => s.Decided == Instruction.HoldSteadier), Said(steps));
            Assert.False(steps.Exists(s => s.Fire), Said(steps));
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    [Fact]
    public void APictureIsAClipAndABadClipSaysWhy()
    {
        string folder = Temp.Folder("camera-clip");
        try
        {
            Directory.CreateDirectory(folder);
            string picture = Path.Combine(folder, "sheet.png");
            Cv2.ImWrite(picture, Mat.FromPixelData(1440, 1920, MatType.CV_8UC1, Frame(Sample()).Pixels));
            var clip = CameraClip.Open(picture, out _);
            Assert.NotNull(clip);
            Assert.Equal(CameraClip.PictureFrames, clip.Frames.Count);
            Assert.Equal(CameraClip.PictureEveryMs, clip.Frames[1].Ms);
            Assert.True(BubbleLevel.Ready(0, 0, 9.81) && clip.Frames.All(f => f.Gravity == (0.0, 0.0, 9.81)));
            Assert.Equal((1920, 1440), clip.Picture);

            // Not a clip, a later version, and a frame outside the clip's own folder.
            string bad = Directory.CreateDirectory(Path.Combine(folder, "bad")).FullName;
            File.WriteAllText(Path.Combine(bad, CameraClip.FileName), "{\"format\":\"something else\"}");
            Assert.Null(CameraClip.Open(bad, out string? why));
            Assert.Equal("it is not a camera clip", why);
            File.WriteAllText(Path.Combine(bad, CameraClip.FileName), "{\"format\":\"grouplab-camera-clip\",\"version\":99,\"frames\":[]}");
            Assert.Null(CameraClip.Open(bad, out why));
            Assert.Contains("later version", why, StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(bad, CameraClip.FileName), "{\"format\":\"grouplab-camera-clip\",\"version\":1,\"frames\":[{\"file\":\"../sheet.png\",\"ms\":0}]}");
            Assert.Null(CameraClip.Open(bad, out why));
            Assert.Equal("the clip has no frames", why);

            // The replay and record steps are the bridge's too.
            Assert.Contains("replay", Bridge.Steps);
            Assert.Contains("record", Bridge.Steps);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }
}
