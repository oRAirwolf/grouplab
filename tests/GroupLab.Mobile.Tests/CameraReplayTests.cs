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
/// <para>
/// Entry 321 added the range: a sheet left on its backer and photographed upright, in sun with the shooter's shadow, moving a little in the
/// wind, curled and taped on cardboard. Guided takes each under the same rules as a sheet flat on a table, the level follows the sheet, and
/// "Hold steadier" is said only of a frame that is shaken.
/// </para>
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

    /// <summary>Gravity's reading for a phone flat over a table, very nearly level.</summary>
    private static readonly (double X, double Y, double Z) Flat = (0.1, -0.2, 9.8);

    /// <summary>Gravity's reading for a phone upright in portrait, its camera 6 degrees below the horizon, square to a backer leaning back that much.</summary>
    private static readonly (double X, double Y, double Z) Upright = (0.05, 9.81 * Math.Cos(6 * Math.PI / 180), 9.81 * Math.Sin(6 * Math.PI / 180));

    /// <summary>
    /// A clip recorded frame by frame as a phone's camera records it, <paramref name="everyMs"/> apart, with the level's reading
    /// <paramref name="gravity"/>, flat over a table where not given, and read back from its folder.
    /// </summary>
    private static CameraClip Record(string folder, IReadOnlyList<GrayImage> frames, long everyMs, (double X, double Y, double Z)? gravity = null)
    {
        var recorder = new ClipRecorder(folder, "test", seconds: 30);
        for (int i = 0; i < frames.Count; i++)
        {
            recorder.Offer(frames[i], 5000 + (i * everyMs), gravity ?? Flat, 1, 5, Still);
        }

        string written = recorder.Write(new DateTime(2026, 9, 30, 10, 15, 0, DateTimeKind.Local))!;
        var clip = CameraClip.Open(written, out string? why);
        Assert.True(clip is not null, why);
        return clip;
    }

    /// <summary>The clip played through the cameras' own code, frame by frame, at its recorded times, the level fed each frame's gravity as a head feeds it.</summary>
    private static List<CameraStep> Replay(CameraClip clip)
    {
        var judge = new CameraJudge(() => Library);
        judge.ResetTorch(5);
        var steps = new List<CameraStep>();
        new ClipPlayer(clip, (_, grey, frame, ms) =>
        {
            if (frame.Gravity is var (x, y, z))
            {
                judge.Level.Felt(x, y, z, ms);
            }

            steps.Add(judge.Next(grey, () => ms, clip.MeasuredScale(grey), torchOnAuto: true, manual: false));
        }).PlayNow();
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

            // Entry 321: looking down, and level by the sheet's own angle, which the frames read square on.
            Assert.All(steps, s => Assert.Equal((LevelMode.LookingDown, LevelSource.Sheet, true), (s.Level!.Mode!.Value, s.Level.Source, s.Level.Ready)));
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

            // Entry 321: the same hand upright at a backer is not taken either.
            clip = Record(Path.Combine(folder, "upright"), [sharp, shaken, sharp, shaken, sharp, shaken, sharp], 300, Upright);
            steps = Replay(clip);
            Assert.True(steps.Exists(s => s.Decided == Instruction.HoldSteadier), Said(steps));
            Assert.False(steps.Exists(s => s.Fire), Said(steps));
            Assert.All(steps, s => Assert.Equal(LevelMode.Upright, s.Level!.Mode));
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

    /// <summary>What the range does to a frame (entry 321 section 4).</summary>
    private sealed record Range(double Height = 1300, double Turn = 0, double Right = 0, double Down = 0, double Sun = 0.85, bool Shadow = false,
        int Smear = 0, double Curl = 0, bool Tape = false);

    /// <summary>
    /// The sample on a cardboard backer as the analysis stream sees it: <see cref="Range.Height"/> pixels tall in the 1920 by 1440 frame,
    /// turned <see cref="Range.Turn"/> degrees about its upright axis with its right side farther, moved by <see cref="Range.Right"/> and
    /// <see cref="Range.Down"/>, its paper at <see cref="Range.Sun"/> of the sample's and clipped as bright sun clips it, a head's shadow
    /// across its lower right, bowed by <see cref="Range.Curl"/> pixels across its width, taped at the middle of its top and bottom edges,
    /// and smeared sideways by <see cref="Range.Smear"/> pixels.
    /// </summary>
    private static GrayImage Scene(GrayImage sample, Range range)
    {
        const int W = 1920, H = 1440;
        double h = range.Height, w = h * sample.Width / sample.Height, t = range.Turn * Math.PI / 180;
        // The far side smaller by the turn, as a camera about 1.5 ft away sees a sheet 8.5 in across.
        double far = Math.Cos(t) / (1 + (Math.Sin(t) * 0.25)), near = Math.Cos(t) / (1 - (Math.Sin(t) * 0.25));
        double cx = (W / 2.0) + range.Right, cy = (H / 2.0) + range.Down, half = w / 2;
        var corners = new List<PointD>
        {
            new(cx - (half * near), cy - (h / 2 * near)), new(cx + (half * far), cy - (h / 2 * far)),
            new(cx + (half * far), cy + (h / 2 * far)), new(cx - (half * near), cy + (h / 2 * near)),
        };
        var toSample = HomographyEstimate.Fit(corners, [new(0, 0), new(sample.Width, 0), new(sample.Width, sample.Height), new(0, sample.Height)])!;
        var pixels = new byte[W * H];
        var noise = new Random(321);
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                // The bow moves each column of the sheet down by the curl at its place across.
                double across = Math.Clamp((x - corners[0].X) / (corners[1].X - corners[0].X), 0, 1);
                var p = toSample.Apply(new PointD(x, y - (range.Curl * Math.Sin(Math.PI * across))));
                bool onSheet = p.X >= 0 && p.Y >= 0 && p.X < sample.Width - 1 && p.Y < sample.Height - 1;
                double v = onSheet ? sample.Pixels[((int)p.Y * sample.Width) + (int)p.X] * range.Sun : 110 + (noise.NextDouble() * 12) - 6;
                if (range.Tape && Math.Abs(x - cx) < 45 && (Math.Abs(y - (cy - (h / 2))) < 22 || Math.Abs(y - (cy + (h / 2))) < 22))
                {
                    v = (v * 0.4) + 140;
                }

                if (range.Shadow && Math.Sqrt(((x - (cx + (0.2 * w))) * (x - (cx + (0.2 * w)))) + ((y - (cy + (0.28 * h))) * (y - (cy + (0.28 * h))))) < 0.18 * h)
                {
                    v *= 0.35;
                }

                pixels[(y * W) + x] = (byte)Math.Clamp(Math.Round(v), 0, 255);
            }
        }

        var frame = new GrayImage(W, H, pixels);
        if (range.Smear < 2)
        {
            return frame;
        }

        using var mat = Mat.FromPixelData(H, W, MatType.CV_8UC1, frame.Pixels);
        using var smeared = new Mat();
        Cv2.Blur(mat, smeared, new Size(range.Smear, 1));
        return OpenCvSharpBackend.Copy(smeared);
    }

    /// <summary>
    /// Sun so strong that the paper clips to white is not a shaken picture: the camera says less light, never hold steadier, and waits, since
    /// a hole in paper that has gone white cannot be told from it.
    /// </summary>
    [Fact]
    public void PaperTheSunHasTurnedWhiteIsLessLightNotHoldSteadier()
    {
        string folder = Temp.Folder("camera-clip");
        try
        {
            var glare = Scene(Sample(), new Range(Height: 1200, Sun: 1.15, Shadow: true));
            var steps = Replay(Record(folder, [glare, glare, glare, glare, glare], 300, Upright));
            Assert.Contains(steps, s => s.Decided == Instruction.LessLight);
            Assert.DoesNotContain(steps, s => s.Decided == Instruction.HoldSteadier);
            Assert.DoesNotContain(steps, s => s.Fire);
        }
        finally
        {
            Temp.Delete(folder);
        }
    }

    public static TheoryData<string> RangeClips => ["sun and shadow", "wind", "curled and taped"];

    /// <summary>
    /// Entry 321 section 4, at the range and upright: an arm's length photograph in bright sun with the shooter's shadow on the sheet; a sheet
    /// moving a few pixels between frames in the wind, one frame a little smeared; a sheet bowed on cardboard with tape across its edges. Each
    /// is taken by Guided under the same rules as a sheet on a table, the level follows the sheet, and none is told to hold steadier.
    /// </summary>
    [Theory]
    [MemberData(nameof(RangeClips))]
    public void AtTheRangeUprightGuidedTakesItAndNeverSaysHoldSteadier(string what)
    {
        string folder = Temp.Folder("camera-clip");
        try
        {
            var sample = Sample();
            GrayImage[] frames = what switch
            {
                "sun and shadow" => [.. Enumerable.Repeat(Scene(sample, new Range(Height: 1200, Turn: 8, Sun: 0.92, Shadow: true)), 5)],
                "wind" => [.. new[] { (0, 0, 0), (5, -3, 0), (-4, 2, 2), (3, 4, 0), (-2, -5, 0), (4, 1, 0) }
                    .Select(m => Scene(sample, new Range(Height: 1100, Right: m.Item1, Down: m.Item2, Smear: m.Item3)))],
                _ => [.. Enumerable.Repeat(Scene(sample, new Range(Height: 1150, Sun: 0.9, Curl: 14, Tape: true)), 5)],
            };
            var steps = Replay(Record(folder, frames, 300, Upright));
            Assert.True(steps.Exists(s => s.Fire), Said(steps));
            Assert.DoesNotContain(steps, s => s.Decided == Instruction.HoldSteadier);
            var taken = steps.First(s => s.Fire).Level!;
            Assert.Equal((LevelMode.Upright, LevelSource.Sheet), (taken.Mode!.Value, taken.Source));

            // The backer leans back 6 degrees, so gravity alone would not be level; square on to the sheet, the sheet's angle is.
            if (what != "sun and shadow")
            {
                Assert.True(taken.Ready, $"{what}: tilt {taken.Tilt:0.0}");
            }
            else
            {
                // Turned 8 degrees, the level says so, to the right, where the far side is, and the picture is still taken.
                Assert.InRange(taken.Tilt, 5, 11);
                Assert.True(taken.Right > 0, $"right {taken.Right:0.0}");
            }
        }
        finally
        {
            Temp.Delete(folder);
        }
    }
}
