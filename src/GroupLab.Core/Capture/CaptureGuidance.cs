using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Capture;

/// <summary>The one thing the capture screen says, docs/MOBILE-CAPTURE.md item C3, or that it is ready.</summary>
public enum Instruction
{
    /// <summary>Every condition holds; the shutter fires (item C1).</summary>
    Ready,

    /// <summary>No sheet stands out from what is behind it.</summary>
    FindTheSheet,

    MoveBack,
    MoveCloser,
    LessAngle,
    HoldSteadier,
    MoreLight,
    LessLight,
    FlattenThePaper,
}

/// <summary>What one frame showed, and the one instruction that follows from it.</summary>
public sealed record FrameVerdict(Instruction Say, string Words, bool SheetInFrame, bool Detected, bool AngleWithin, bool InFocus, bool ExposureWithin, bool MarkingsRead);

/// <summary>
/// docs/MOBILE-CAPTURE.md items C1 to C3, entry 219 item A2: the capture screen's conditions, all judged from one frame, and **one**
/// instruction at a time, the first failing in C3's order: move back (the sheet runs out of the frame), move closer (the least
/// resolution is below the resolution part's useless level), less angle (beyond the limit), hold steadier (focus), more or less light
/// (exposure), flatten the paper (the outline is not four straight sides). The shutter fires only when every condition holds.
/// <para>
/// A condition holds where its part of the quality score (section 5) is at least <see cref="Holds"/>, halfway from worthless to
/// perfect, and the angle is within <see cref="OffAxisLimit.Degrees"/>. The parts and their levels are <see cref="CaptureQualities"/>'s;
/// nothing here sets a threshold of its own but that half.
/// </para>
/// </summary>
public static class CaptureGuidance
{
    /// <summary>The part of the score, from 0 to 1, at which a condition counts as holding.</summary>
    public const double Holds = 0.5;

    /// <summary>
    /// The verdict on one camera frame of a GroupLab sheet, judged the way the desktop judges a photograph: the paper's outline
    /// (<see cref="SheetOutline.Find"/>), then the sheet's own markers (<see cref="SheetMeasurer.Measure"/>), and from them the angle and
    /// quality (<see cref="CaptureRecord.Of"/>). The phone runs this on its analysis stream, several times a second.
    /// </summary>
    public static FrameVerdict JudgeFrame(GrayImage frame, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var outline = SheetOutline.Find(frame, out string? reason);
        var measurement = SheetMeasurer.Measure(frame, metadata, definition, new MeasureOptions(), backend, new TraceRecorder());
        CaptureQuality? quality = null;
        if (measurement.Registration is { } registration && measurement.Fiducials is { } markers)
        {
            quality = CaptureRecord.Of(frame, metadata, registration.Mapping, definition.Page.Width, definition.Page.Height, markers.Matches.Count,
                markers.Expected, measurement.Lens?.K1, measurement.Lens?.K2).Quality;
        }

        return Judge(outline, reason, quality);
    }

    /// <summary>
    /// The verdict on one frame, from the outline search (<see cref="SheetOutline.Find"/>'s quad and reason) and, where the sheet was
    /// registered, the frame's quality and its angle. With no quality, nothing past the outline can be judged, and it says so.
    /// </summary>
    public static FrameVerdict Judge(SheetQuad? outline, string? outlineReason, CaptureQuality? quality)
    {
        bool inFrame = outline is not null || outlineReason is not (SheetOutline.OutOfFrame or SheetOutline.NoSheet);
        bool detected = outline is not null || quality is not null;
        bool angle = quality is { } q1 && q1.OffAxisDegrees <= OffAxisLimit.Degrees;
        bool resolution = quality is { } q2 && q2.ResolutionPart >= Holds;
        bool focus = quality is { FocusPart: { } f } && f >= Holds;
        bool light = quality is { ExposurePart: { } e } && e >= Holds;
        bool markings = quality is { MarkingsPart: null } || quality is { MarkingsPart: { } m } && m >= Holds;
        bool flat = outlineReason != SheetOutline.NotFourSides;

        (Instruction Say, string Words) next =
            outlineReason == SheetOutline.NoSheet && quality is null ? (Instruction.FindTheSheet, "Point the camera at the sheet.")
            : !inFrame ? (Instruction.MoveBack, "Move back.")
            : quality is null ? (Instruction.FindTheSheet, "Hold still while GroupLab finds the sheet.")
            : !resolution ? (Instruction.MoveCloser, "Move closer.")
            : !angle ? (Instruction.LessAngle, "Less angle: hold the phone square to the sheet.")
            : !focus ? (Instruction.HoldSteadier, "Hold steadier.")
            : !light ? (quality.ClippedShare is { } clipped && clipped > CaptureQualities.FineClipped ? (Instruction.LessLight, "Less light: the paper is too bright.") : (Instruction.MoreLight, "More light."))
            : !flat ? (Instruction.FlattenThePaper, "Flatten the paper.")
            : !markings ? (Instruction.HoldSteadier, "Hold steadier.")
            : (Instruction.Ready, "Hold it there.");
        return new FrameVerdict(next.Say, next.Words, inFrame, detected, angle, focus, light, markings);
    }
}
