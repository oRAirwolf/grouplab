using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 281 section 1.4: in the Fold 7's camera test "Move closer" and "Move back" took turns more than twenty times.
/// The words now hold a band with two edges and change only after a new instruction has held for half a second.
/// </summary>
public class GuidanceSteadierTests
{
    // A registered frame that is good but for what each test changes: square, sharp, well lit, every marker read.
    private static FrameVerdict Frame(double ppi, double edgeRoom, double degrees = 5)
    {
        var quality = new CaptureQuality(90, "good", 0.001, 1, 0, 200, 1, degrees, 1, ppi, CaptureQualities.ResolutionPart(ppi), 34, 34, 1);
        return new FrameVerdict(Instruction.Ready, "Hold it there.", true, true, true, true, true, true, quality, EdgeRoom: edgeRoom);
    }

    private static List<Instruction> Run(GuidanceSteadier steadier, IEnumerable<FrameVerdict> frames, long stepMs = 100, double scale = 1)
    {
        long now = 0;
        return [.. frames.Select(f => steadier.Next(f, now += stepMs, scale).Say)];
    }

    [Fact]
    public void ASheetAtTheFramesEdgeDoesNotFlipBetweenCloserAndBack()
    {
        // Once in, the corner wandering between 1.5 percent clear and half a percent past the edge, and the resolution between 125 and 160.
        var steadier = new GuidanceSteadier();
        var settle = Enumerable.Repeat(Frame(160, 0.03), 8);
        var shaking = Enumerable.Range(0, 40).Select(i => Frame(i % 2 == 0 ? 125 : 160, i % 3 == 0 ? -0.005 : 0.015));
        var said = Run(steadier, settle.Concat(shaking));
        Assert.Equal(Instruction.Ready, said[^1]);
        Assert.DoesNotContain(Instruction.MoveBack, said.Skip(8));
        Assert.DoesNotContain(Instruction.MoveCloser, said.Skip(8));
    }

    [Fact]
    public void ANewInstructionShowsOnlyAfterHalfASecond()
    {
        var steadier = new GuidanceSteadier();
        var said = Run(steadier, Enumerable.Repeat(Frame(160, 0.05), 8).Concat(Enumerable.Repeat(Frame(60, 0.05), 8)));
        // 100 ms a frame: the too-far frames start at the ninth, and the words follow five frames later, 500 ms on.
        Assert.Equal(Instruction.Ready, said[11]);
        Assert.Equal(Instruction.MoveCloser, said[13]);
    }

    [Fact]
    public void ClearlyOutsideTheBandStillSaysSo()
    {
        var steadier = new GuidanceSteadier();
        var said = Run(steadier, Enumerable.Repeat(Frame(160, 0.05), 8).Concat(Enumerable.Repeat(Frame(160, -0.05), 8)));
        Assert.Equal(Instruction.MoveBack, said[^1]);
    }

    [Fact]
    public void ResolutionIsJudgedAtTheSizeThePictureIsMeasuredAt()
    {
        // 80 pixels an inch in the stream is 136 in a picture 1.7 times as large: not yet close enough. 90 is 153: close enough.
        Assert.Equal(Instruction.MoveCloser, Run(new GuidanceSteadier(), Enumerable.Repeat(Frame(80, 0.05), 10), scale: 1.7)[^1]);
        Assert.Equal(Instruction.Ready, Run(new GuidanceSteadier(), Enumerable.Repeat(Frame(90, 0.05), 10), scale: 1.7)[^1]);
    }

    [Fact]
    public void AFrameThatFailsToRegisterAmongGoodOnesChangesNothing()
    {
        // The camera test's "Move back" frames: nothing registered, and the outline search saying the sheet ran out of the frame.
        var lost = new FrameVerdict(Instruction.MoveBack, "Move back.", false, false, false, false, false, false);
        var steadier = new GuidanceSteadier();
        var frames = Enumerable.Repeat(Frame(160, 0.05), 8).Concat(Enumerable.Range(0, 30).Select(i => i % 3 == 0 ? lost : Frame(160, 0.05)));
        var said = Run(steadier, frames);
        Assert.DoesNotContain(Instruction.MoveBack, said.Skip(8));
    }
}
