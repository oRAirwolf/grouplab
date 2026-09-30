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
/// <para>
/// Entry 291 sections 3.2 and 3.5, after the second sitting: "Move back" came whenever the paper's white margin touched the frame's edge,
/// though every marker and code was in view, and "Hold steadier" whenever the stream read fewer markers than the picture would, which at
/// less than half the picture's pixels is most of the time, or whenever a far sheet's resolution made its edges soft in inches. Now the
/// frame is judged by what the picture will read: "Move back" only when the printing runs out of the frame, "Move closer" from the pixels a
/// code's module will get in the picture, and "Hold steadier" only when a shake has smeared the markers' edges more than the picture can
/// measure through. The numbers and the pictures that set them are in docs/MOBILE-CAPTURE.md section 8.
/// </para>
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

    /// <summary>
    /// Entry 291 section 3.2: the frame band on the printing, the markers' corners and the codes, not the paper: out of the frame only once
    /// some of it is past the edge, and in again once all of it is half a percent of the frame inside.
    /// </summary>
    public const double EnterPrintedRoom = 0.005;

    public const double LeavePrintedRoom = 0;

    /// <summary>
    /// Entry 291 section 3.2: the distance band, in pixels of the picture a code's module (0.4 mm) gets at the sheet's worst corner. The
    /// Fold 7's pictures of the second sitting, made smaller step by step, read every marker, both codes square on in about a second, and
    /// every hole down to 1.6 in one and 2.1 in another; from 1.9 down in some, 1.3 in others, the codes were no longer read square on, the
    /// sheet took 20 to 40 seconds to name or was not named at all, and marks that are not holes appeared. So close enough from 2.4, above
    /// every failure, and still close enough down to 2.1.
    /// </summary>
    public const double EnterModulePixels = 2.4;

    public const double LeaveModulePixels = 2.1;

    /// <summary>
    /// Entry 291 section 3.5: the shake band, in pixels of the picture, the frame's blur in the worse direction less the stream's own
    /// softness (<see cref="StreamSoftnessPixels"/>). The second sitting's pictures, blurred step by step and then made as small as the
    /// stream, were read whole wherever the frame measured 0.24 to 0.34 pixels (a Gaussian of 1 pixel in the picture, a shake of 4), 0.36
    /// in the picture by this measure; from 0.46 (a Gaussian of 1.5), 0.79 here, two of three were no longer named. Shaky from 0.6,
    /// steady again under 0.45.
    /// </summary>
    public const double ShakyPixels = 0.6;

    public const double SteadyPixels = 0.45;

    /// <summary>
    /// The blur a still frame of the stream shows by itself, in its own pixels: its sensor, its lens and its scaling, not a hand. The
    /// second sitting's pictures made as small as the stream measure 0.0 to 0.16; the stream itself was not kept, so this allows it 0.3
    /// of its own until the next sitting's kept frames measure it.
    /// </summary>
    public const double StreamSoftnessPixels = 0.3;

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
    private bool shaky;
    private Instruction shown = Instruction.FindTheSheet;
    private string shownWords = "Point the camera at the sheet.";
    private Instruction? pending;
    private long pendingSince;
    private long? lastRegistered;

    /// <summary>
    /// The last frame's own instruction, before the words were held: what <see cref="AutoShutter"/> reads (entry 311 section 1). Null where
    /// the frame could not be judged, a stumble just after a registered frame, for which the words are held as they were.
    /// </summary>
    public Instruction? Decided { get; private set; }

    /// <summary>Forgets everything, as when the camera starts again.</summary>
    public void Reset()
    {
        Decided = null;
        (inFrame, closeEnough, square, shaky, pending, lastRegistered) = (false, false, false, false, null, null);
        (shown, shownWords) = (Instruction.FindTheSheet, "Point the camera at the sheet.");
    }

    /// <summary>The instruction to show after this frame, at <paramref name="nowMs"/> on any steady clock.</summary>
    /// <param name="measuredScale">The picture's pixels, as it is measured, per pixel of the analysis stream.</param>
    public FrameVerdict Next(FrameVerdict raw, long nowMs, double measuredScale = 1)
    {
        ArgumentNullException.ThrowIfNull(raw);
        if (raw.Quality is null && lastRegistered is { } seen && nowMs - seen < StumbleMs)
        {
            Decided = null;
            return raw with { Say = shown, Words = shownWords };
        }

        if (raw.Quality is not null)
        {
            lastRegistered = nowMs;
        }

        var (say, words) = Decide(raw, measuredScale);
        Decided = say;
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

    /// <summary>
    /// The shake a frame shows, in pixels of the picture: the markers' edges' blur in the worse direction, at the frame's least resolution,
    /// less the stream's own softness, times the picture's pixels to the frame's. Null where the blur could not be measured.
    /// </summary>
    public static double? ShakePixels(CaptureQuality quality, double measuredScale)
    {
        ArgumentNullException.ThrowIfNull(quality);
        if (quality.WorstBlurInches is not { } worst)
        {
            return null;
        }

        return Shake(worst * Math.Min(quality.LeastPixelsPerInch, 200), measuredScale);
    }

    /// <summary>A blur of <paramref name="framePixels"/> in the stream, as a shake in the picture's pixels: the stream's own softness taken out.</summary>
    public static double Shake(double framePixels, double measuredScale) =>
        Math.Sqrt(Math.Max(0, (framePixels * framePixels) - (StreamSoftnessPixels * StreamSoftnessPixels))) * measuredScale;

    /// <summary>The instruction for this frame alone, with no hold and every band entered afresh: what the tuning table reports.</summary>
    public FrameVerdict Instant(FrameVerdict raw, double measuredScale = 1)
    {
        ArgumentNullException.ThrowIfNull(raw);
        var (say, words) = Decide(raw, measuredScale);
        return raw with { Say = say, Words = words };
    }

    private (Instruction Say, string Words) Decide(FrameVerdict raw, double measuredScale)
    {
        if (raw.Quality is not { } quality)
        {
            // Nothing registered: no band to hold, and the next registered frame starts every band afresh. Entry 291: a smeared frame is
            // told to hold steadier, and "Move back" from the paper's outline alone, with no printing fitted, is not said: in the second
            // sitting it was said to sheets whose printing was in view but too small or too smeared to fit, until nothing could be read.
            (inFrame, closeEnough, square, shaky) = (false, false, false, false);
            if (raw.FrameBlurPixels is { } smear && Shake(smear, measuredScale) > ShakyPixels)
            {
                shaky = true;
                return (Instruction.HoldSteadier, "Hold steadier.");
            }

            // Markers read, but too few to fit the sheet by and small: the sheet is too far for the stream to read.
            if (raw.MarkersRead is > 0 && raw.MarkerPixels is { } side && side < LiveSheet.ReadableSidePixels)
            {
                return (Instruction.MoveCloser, "Move closer.");
            }

            return raw.Say == Instruction.MoveBack && raw.MarkersRead is null or > 0
                ? (Instruction.FindTheSheet, "Hold still while GroupLab finds the sheet.")
                : (raw.Say, raw.Words);
        }

        closeEnough = raw.ModulePixels is { } module
            ? module * measuredScale >= (closeEnough ? LeaveModulePixels : EnterModulePixels)
            : quality.LeastPixelsPerInch * measuredScale >= (closeEnough ? LeavePixelsPerInch : EnterPixelsPerInch);
        inFrame = raw.PrintedRoom is { } printed ? printed >= (inFrame ? LeavePrintedRoom : EnterPrintedRoom)
            : raw.EdgeRoom is { } room ? room >= (inFrame ? LeaveEdgeRoom : EnterEdgeRoom) : raw.SheetInFrame;
        square = quality.OffAxisDegrees <= OffAxisLimit.Degrees + (square ? LeaveExtraDegrees : 0);
        double? shake = raw.FrameBlurPixels is { } frameBlur ? Shake(frameBlur, measuredScale) : ShakePixels(quality, measuredScale);
        shaky = shake is { } s ? s > (shaky ? SteadyPixels : ShakyPixels) : !raw.InFocus;
        return !inFrame ? (Instruction.MoveBack, "Move back.")
            : !closeEnough ? (Instruction.MoveCloser, "Move closer.")
            : !square ? (Instruction.LessAngle, "Less angle: hold the phone square to the sheet.")
            : shaky ? (Instruction.HoldSteadier, "Hold steadier.")
            : !raw.ExposureWithin ? (quality.ClippedShare is { } clipped && clipped > CaptureQualities.FineClipped ? (Instruction.LessLight, "Less light: the paper is too bright.") : (Instruction.MoreLight, "More light."))
            : (Instruction.Ready, "Hold it there.");
    }
}
