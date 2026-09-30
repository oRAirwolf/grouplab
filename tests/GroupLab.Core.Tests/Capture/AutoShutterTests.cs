using GroupLab.Core.Capture;

namespace GroupLab.Core.Tests.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 311 section 1: Guided takes the picture once every frame for <see cref="AutoShutter.SteadyMs"/>, at least
/// <see cref="AutoShutter.LeastFrames"/> of them, has been judged ready, the same on Android and iOS. It was three frames of words saying
/// "Hold it there" after the words' own hold, about 2.7 seconds at the Fold 7's median analysis frame.
/// </summary>
public class AutoShutterTests
{
    /// <summary>When the picture is taken, in milliseconds after the first frame judged ready, at one frame every <paramref name="frameMs"/>.</summary>
    private static long FiresAfter(long frameMs)
    {
        var auto = new AutoShutter();
        for (long t = 0; t < 10_000; t += frameMs)
        {
            if (auto.Next(Instruction.Ready, true, t))
            {
                return t;
            }
        }

        return -1;
    }

    [Theory]
    [InlineData(892, 892)] // the Fold 7's median analysis frame: the second frame judged ready
    [InlineData(618, 618)] // its lower quarter
    [InlineData(1115, 1115)] // its upper quarter
    [InlineData(100, 600)] // a quick stream: the first frame 0.6 seconds on
    public void AWellFramedSheetIsTakenAboutASecondAfterItIsFirstReady(long frameMs, long expected) =>
        Assert.Equal(expected, FiresAfter(frameMs));

    [Fact]
    public void AFrameJudgedAnythingElseStartsTheWaitAgain()
    {
        var auto = new AutoShutter();
        Assert.False(auto.Next(Instruction.Ready, true, 0));
        Assert.False(auto.Next(Instruction.HoldSteadier, true, 400));
        Assert.False(auto.Next(Instruction.Ready, true, 800));
        Assert.False(auto.Next(Instruction.Ready, true, 1300));
        Assert.True(auto.Next(Instruction.Ready, true, 1400));
    }

    [Fact]
    public void AFrameThatCouldNotBeJudgedNeitherCountsNorStartsAgain()
    {
        var auto = new AutoShutter();
        Assert.False(auto.Next(Instruction.Ready, true, 0));
        Assert.False(auto.Next(null, true, 700));
        Assert.True(auto.Next(Instruction.Ready, true, 900));
    }

    [Fact]
    public void OneFrameAloneIsNeverEnoughHoweverLongItTook()
    {
        var auto = new AutoShutter();
        Assert.False(auto.Next(Instruction.Ready, true, 0));
        Assert.False(auto.Next(null, true, 5000));
        Assert.True(auto.Next(Instruction.Ready, true, 5100));
    }

    [Fact]
    public void TheCardNotYetLaidHoldsItBack()
    {
        var auto = new AutoShutter();
        Assert.False(auto.Next(Instruction.Ready, false, 0));
        Assert.False(auto.Next(Instruction.Ready, false, 900));
        Assert.False(auto.Next(Instruction.Ready, true, 1800));
        Assert.True(auto.Next(Instruction.Ready, true, 2700));
    }

    [Fact]
    public void TheRingFillsAndEmptiesAfterThePicture()
    {
        var auto = new AutoShutter();
        Assert.Equal(0, auto.Progress);
        auto.Next(Instruction.Ready, true, 0);
        Assert.InRange(auto.Progress, 0.3, 0.4);
        auto.Next(Instruction.Ready, true, 300);
        Assert.InRange(auto.Progress, 0.6, 0.7);
        Assert.True(auto.Next(Instruction.Ready, true, 600));
        Assert.Equal(0, auto.Progress);
    }

    [Fact]
    public void TheSteadierSaysWhatEachFrameDecidedAndNothingForAStumble()
    {
        var steadier = new GuidanceSteadier();
        Assert.Null(steadier.Decided);
        var lost = new FrameVerdict(Instruction.FindTheSheet, "Point the camera at the sheet.", false, false, false, false, false, false);
        steadier.Next(lost, 0);
        Assert.Equal(Instruction.FindTheSheet, steadier.Decided);
        steadier.Reset();
        Assert.Null(steadier.Decided);
    }
}
