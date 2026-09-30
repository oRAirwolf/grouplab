namespace GroupLab.Core.Capture;

/// <summary>
/// When Guided mode takes the picture by itself, the same on Android and iOS, NOTES-FROM-PLANNING.md entry 311 section 1. Alan, on the iPad
/// and on Android: "the threshold for taking a photo automatically seems too high and it takes a long time". It fired after the words had
/// said "Hold it there" for three analysis frames in a row, and the words change only once a new instruction has held for
/// <see cref="GuidanceSteadier.HoldMs"/>, so a frame judged ready waited for the hold and then three frames more. The Fold 7's analysis
/// frames took 892 ms at the median of 79 logged (618 at the lower quarter, 1115 at the upper), so a sheet framed well was taken about 2.7
/// seconds after the first frame that judged it ready, and never sooner than about 1.9.
/// <para>
/// Now the picture is taken once every frame for at least <see cref="SteadyMs"/>, and at least <see cref="LeastFrames"/> of them, has been
/// judged ready on its own (<see cref="GuidanceSteadier.Decided"/>): in frame, close enough, square, not shaken and well lit. Those are the
/// same checks at the same thresholds, so what is taken is judged exactly as before; only the waiting after the checks all pass is shorter,
/// one frame at the Fold 7's pace, about 0.9 seconds. A frame judged anything else starts the wait again. A frame that could not be judged
/// at all, a stumble just after a good one (<see cref="GuidanceSteadier.StumbleMs"/>), neither counts nor starts it again, as the words
/// already treat it.
/// </para>
/// </summary>
public sealed class AutoShutter
{
    /// <summary>How long every frame must have been judged ready before the picture is taken, in milliseconds.</summary>
    public const long SteadyMs = 600;

    /// <summary>The fewest frames judged ready, the first included, before the picture is taken: two, so a steady hand is seen twice.</summary>
    public const int LeastFrames = 2;

    private long? since;
    private int frames;

    /// <summary>How long the frames have been ready, in milliseconds, at the last frame; 0 where they are not.</summary>
    public long ReadyMs { get; private set; }

    /// <summary>Frames judged ready in a row, at the last frame.</summary>
    public int Frames => frames;

    /// <summary>Forgets the frames seen, as when the mode changes or the camera starts again.</summary>
    public void Reset()
    {
        since = null;
        frames = 0;
        ReadyMs = 0;
    }

    /// <summary>
    /// One frame: <paramref name="decided"/> is its own instruction, null where it could not be judged; <paramref name="allowed"/> false
    /// where something else holds the picture back, such as the card not yet laid on the printer check page. True where the picture is to
    /// be taken now, after which the frames are forgotten.
    /// </summary>
    public bool Next(Instruction? decided, bool allowed, long nowMs)
    {
        if (decided is null)
        {
            ReadyMs = since is { } started ? nowMs - started : 0;
            return false;
        }

        if (decided != Instruction.Ready || !allowed)
        {
            Reset();
            return false;
        }

        since ??= nowMs;
        frames++;
        ReadyMs = nowMs - since.Value;
        if (frames >= LeastFrames && ReadyMs >= SteadyMs)
        {
            since = null;
            frames = 0;
            return true;
        }

        return false;
    }

    /// <summary>How far the wait has got, 0 to 1, for the ring round the shutter.</summary>
    public double Progress => frames == 0 ? 0 : Math.Clamp(Math.Max((double)ReadyMs / SteadyMs, (double)frames / (LeastFrames + 1)), 0, 1);
}
