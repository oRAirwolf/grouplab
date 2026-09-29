namespace GroupLab.Core.Capture;

/// <summary>
/// The capture screen's words held steady, NOTES-FROM-PLANNING.md entry 281 section 1.4. In the Fold 7's camera test of 2026-09-29 "Move
/// closer" and "Move back" took turns more than twenty times, some within a fifth of a second: each frame was judged on its own, with one
/// value for each condition, and the resolution was judged at the analysis stream's size, less than half the picture's, so at the distance
/// where the whole sheet fits with room the stream said "closer" while the frame's edge said "back". This keeps three things:
/// <list type="bullet">
/// <item>Resolution is judged at the size the picture is measured at, the stream's pixels an inch times <c>measuredScale</c>.</item>
/// <item>Each distance and angle condition is a band with two edges: it becomes good at one value and stops being good only past another,
/// so a hand's tremor at the edge changes nothing.</item>
/// <item>The words change only when a new instruction has held for <see cref="HoldMs"/>, and "Hold it there" the same.</item>
/// </list>
/// </summary>
public sealed class GuidanceSteadier
{
    /// <summary>How long a new instruction must hold before the words change, in milliseconds.</summary>
    public const long HoldMs = 500;

    /// <summary>
    /// The resolution band, pixels an inch at the size the picture is measured at, at the sheet's worst corner: close enough from
    /// <see cref="CaptureQualities.FinePixelsPerInch"/>, 150, and still close enough down to 120. On the phone's 3266 by 2449 working copy a
    /// Letter sheet reaches 150 pixels an inch when its 11 inches take half the picture's long side.
    /// </summary>
    public const double EnterPixelsPerInch = CaptureQualities.FinePixelsPerInch;

    public const double LeavePixelsPerInch = 120;

    /// <summary>
    /// The frame band, the room left beyond the sheet's nearest corner as a share of the frame's longer side: in the frame once 2 percent
    /// is clear all round, and out only once a corner is 1 percent past the edge.
    /// </summary>
    public const double EnterEdgeRoom = 0.02;

    public const double LeaveEdgeRoom = -0.01;

    /// <summary>The angle band: square enough within <see cref="OffAxisLimit.Degrees"/>, and not too angled until this much past it.</summary>
    public const double LeaveExtraDegrees = 4;

    /// <summary>
    /// How long a frame that fails to register, soon after one that did, is taken for a stumble rather than news. In the camera test the
    /// "Move back" frames were those: no markers matched, and the paper's outline, which a sheet on an off-white counter does not show, said
    /// the sheet ran out of the frame.
    /// </summary>
    public const long StumbleMs = 1500;

    private bool inFrame;
    private bool closeEnough;
    private bool square;
    private Instruction shown = Instruction.FindTheSheet;
    private string shownWords = "Point the camera at the sheet.";
    private Instruction? pending;
    private long pendingSince;
    private long? lastRegistered;

    /// <summary>Forgets everything, as when the camera starts again.</summary>
    public void Reset()
    {
        (inFrame, closeEnough, square, pending, lastRegistered) = (false, false, false, null, null);
        (shown, shownWords) = (Instruction.FindTheSheet, "Point the camera at the sheet.");
    }

    /// <summary>The instruction to show after this frame, at <paramref name="nowMs"/> on any steady clock.</summary>
    /// <param name="measuredScale">The picture's pixels, as it is measured, per pixel of the analysis stream.</param>
    public FrameVerdict Next(FrameVerdict raw, long nowMs, double measuredScale = 1)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Quality is null && lastRegistered is { } seen && nowMs - seen < StumbleMs)
        {
            return raw with { Say = shown, Words = shownWords };
        }

        if (raw.Quality is not null)
        {
            lastRegistered = nowMs;
        }

        var (say, words) = Decide(raw, measuredScale);
        if (say == shown)
        {
            pending = null;
            shownWords = words;
        }
        else if (pending != say)
        {
            (pending, pendingSince) = (say, nowMs);
        }
        else if (nowMs - pendingSince >= HoldMs)
        {
            (shown, shownWords, pending) = (say, words, null);
        }

        return raw with { Say = shown, Words = shownWords };
    }

    private (Instruction Say, string Words) Decide(FrameVerdict raw, double measuredScale)
    {
        if (raw.Quality is not { } quality)
        {
            // Nothing registered: no band to hold, and the next registered frame starts every band afresh.
            (inFrame, closeEnough, square) = (false, false, false);
            return (raw.Say, raw.Words);
        }

        double ppi = quality.LeastPixelsPerInch * measuredScale;
        closeEnough = ppi >= (closeEnough ? LeavePixelsPerInch : EnterPixelsPerInch);
        inFrame = raw.EdgeRoom is { } room ? room >= (inFrame ? LeaveEdgeRoom : EnterEdgeRoom) : raw.SheetInFrame;
        square = quality.OffAxisDegrees <= OffAxisLimit.Degrees + (square ? LeaveExtraDegrees : 0);
        return !inFrame ? (Instruction.MoveBack, "Move back.")
            : !closeEnough ? (Instruction.MoveCloser, "Move closer.")
            : !square ? (Instruction.LessAngle, "Less angle: hold the phone square to the sheet.")
            : !raw.InFocus ? (Instruction.HoldSteadier, "Hold steadier.")
            : !raw.ExposureWithin ? (quality.ClippedShare is { } clipped && clipped > CaptureQualities.FineClipped ? (Instruction.LessLight, "Less light: the paper is too bright.") : (Instruction.MoreLight, "More light."))
            : !raw.MarkingsRead ? (Instruction.HoldSteadier, "Hold steadier.")
            : (Instruction.Ready, "Hold it there.");
    }
}
