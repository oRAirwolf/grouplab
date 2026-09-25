using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Imaging;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// docs/MOBILE-CAPTURE.md items C1 and C3, the two tests it names as "to write", on the capture screen's judgement of a frame
/// (<see cref="CaptureGuidance"/>, entry 219 item A2): one instruction at a time in C3's order, and the shutter only when every condition
/// holds.
/// </summary>
public class CaptureScreenTests
{
    private static readonly SheetQuad Square = new(new PointD(100, 100), new PointD(900, 100), new PointD(900, 1200), new PointD(100, 1200), 0.4, 0.6);

    /// <summary>A frame's quality with every part perfect unless it is named.</summary>
    private static CaptureQuality Quality(double resolution = 1, double degrees = 5, double focus = 1, double exposure = 1, double clipped = 0, double? markings = 1) =>
        new(80, "good", 0.003, focus, clipped, 180, exposure, degrees, 1, 200, resolution, 30, 33, markings);

    [Fact]
    public void TheShutterFiresOnlyWhenEveryConditionHolds()
    {
        Assert.Equal(Instruction.Ready, CaptureGuidance.Judge(Square, null, Quality()).Say);
        foreach (var (what, verdict) in new (string, FrameVerdict)[]
        {
            ("out of the frame", CaptureGuidance.Judge(null, SheetOutline.OutOfFrame, null)),
            ("no sheet", CaptureGuidance.Judge(null, SheetOutline.NoSheet, null)),
            ("low resolution", CaptureGuidance.Judge(Square, null, Quality(resolution: 0.2))),
            ("too much angle", CaptureGuidance.Judge(Square, null, Quality(degrees: OffAxisLimit.Degrees + 1))),
            ("blurred", CaptureGuidance.Judge(Square, null, Quality(focus: 0.3))),
            ("dark", CaptureGuidance.Judge(Square, null, Quality(exposure: 0.2))),
            ("curled", CaptureGuidance.Judge(null, SheetOutline.NotFourSides, Quality())),
            ("few markings read", CaptureGuidance.Judge(Square, null, Quality(markings: 0.1))),
        })
        {
            Assert.True(verdict.Say != Instruction.Ready, $"{what}: the shutter would have fired");
        }
    }

    /// <summary>With several conditions failing at once, the first in C3's order is the only thing said, and fixing it moves to the next.</summary>
    [Fact]
    public void OnlyOneInstructionIsShownAtATime()
    {
        Assert.Equal(Instruction.MoveBack, CaptureGuidance.Judge(null, SheetOutline.OutOfFrame, null).Say);
        Assert.Equal(Instruction.FindTheSheet, CaptureGuidance.Judge(null, SheetOutline.NoSheet, null).Say);

        var steps = new (CaptureQuality Quality, string? Reason, Instruction Expected)[]
        {
            (Quality(resolution: 0.2, degrees: 50, focus: 0.1, exposure: 0.1), SheetOutline.NotFourSides, Instruction.MoveCloser),
            (Quality(degrees: 50, focus: 0.1, exposure: 0.1), SheetOutline.NotFourSides, Instruction.LessAngle),
            (Quality(focus: 0.1, exposure: 0.1), SheetOutline.NotFourSides, Instruction.HoldSteadier),
            (Quality(exposure: 0.1, clipped: 0.3), SheetOutline.NotFourSides, Instruction.LessLight),
            (Quality(exposure: 0.1), SheetOutline.NotFourSides, Instruction.MoreLight),
            (Quality(), SheetOutline.NotFourSides, Instruction.FlattenThePaper),
            (Quality(), null, Instruction.Ready),
        };
        foreach (var (quality, reason, expected) in steps)
        {
            var verdict = CaptureGuidance.Judge(reason is null ? Square : null, reason, quality);
            Assert.Equal(expected, verdict.Say);
            Assert.False(string.IsNullOrWhiteSpace(verdict.Words));
            Assert.DoesNotContain("\n", verdict.Words, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A whole frame judged as the phone will judge it: a Letter sheet rendered at 150 dpi on a dark board is ready, and the same sheet
    /// filling the frame edge to edge is told to move back. Its paper is the grey a photograph's is, not a render's pure white.
    /// </summary>
    [Fact]
    public void ARealFrameIsJudgedFromItsOwnMarkers()
    {
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        const double dpi = 150;
        var render = SceneRasterizer.Rasterize(SceneBuilder.Build(definition).Pages[0], dpi);

        // Paper in a photograph is not pure white; a render's is, and would read as blown out. So the paper is brought to 225.
        for (int i = 0; i < render.Pixels.Length; i++)
        {
            render.Pixels[i] = (byte)(render.Pixels[i] * 225 / 255);
        }
        const int border = 120;
        int w = render.Width + (2 * border), h = render.Height + (2 * border);
        var board = new byte[w * h];
        Array.Fill(board, (byte)40);
        for (int y = 0; y < render.Height; y++)
        {
            Array.Copy(render.Pixels, y * render.Width, board, ((y + border) * w) + border, render.Width);
        }

        var camera = new ImageMetadata("JPEG", w, h, null, null, "TestMake", "TestPhone", 1, 6.25, 24);
        var onBoard = CaptureGuidance.JudgeFrame(new GrayImage(w, h, board), camera, definition, new OpenCvSharpBackend());
        Assert.True(onBoard.SheetInFrame && onBoard.Detected, onBoard.Words);
        Assert.Equal(Instruction.Ready, onBoard.Say);

        var cropped = new byte[(render.Width - 200) * (render.Height - 200)];
        for (int y = 0; y < render.Height - 200; y++)
        {
            Array.Copy(render.Pixels, ((y + 100) * render.Width) + 100, cropped, y * (render.Width - 200), render.Width - 200);
        }

        var tight = CaptureGuidance.JudgeFrame(new GrayImage(render.Width - 200, render.Height - 200, cropped), camera, definition, new OpenCvSharpBackend());
        Assert.Equal(Instruction.MoveBack, tight.Say);
    }
}
