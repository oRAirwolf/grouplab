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

/// <summary>
/// What one frame showed, and the one instruction that follows from it; with, for the capture screen's panel (entry 260, Capture B), the
/// frame's quality where the sheet was registered, the markers and square codes read against those the sheet has, and how even the light
/// on the paper is, the dimmest bull's paper against the brightest.
/// </summary>
public sealed record FrameVerdict(Instruction Say, string Words, bool SheetInFrame, bool Detected, bool AngleWithin, bool InFocus, bool ExposureWithin, bool MarkingsRead,
    CaptureQuality? Quality = null, int? MarkersRead = null, int? MarkersExpected = null, int? CodesRead = null, int? CodesExpected = null, double? Evenness = null);

/// <summary>
/// docs/MOBILE-CAPTURE.md items C1 to C3, entry 219 item A2: the capture screen's conditions, all judged from one frame, and **one**
/// instruction at a time, the first failing in C3's order: move back (the sheet runs out of the frame), move closer (the least
/// resolution is below the resolution part's useless level), less angle (beyond the limit), hold steadier (focus), more or less light
/// (exposure). The shutter fires only when every condition holds.
/// <para>
/// A condition holds where its part of the quality score (section 5) is at least <see cref="Holds"/>, halfway from worthless to
/// perfect, and the angle is within <see cref="OffAxisLimit.Degrees"/>. The parts and their levels are <see cref="CaptureQualities"/>'s;
/// nothing here sets a threshold of its own but that half.
/// </para>
/// <para>
/// Entry 260 changed two things after the Fold 7's camera test. Whether the sheet is in the frame is judged from its own markers once it is
/// registered, by where its four corners fall, and not from the paper's outline, which a white sheet on an off-white counter does not
/// have: the outline said "out of the frame" of a sheet lying in plain view, and the screen said "Move back" for five minutes. And paper that
/// is not four straight sides no longer holds the shutter: curl and waves are followed by the registration and reported afterwards
/// (Alan: the application "needs to work for people under less than ideal conditions").
/// </para>
/// </summary>
public static class CaptureGuidance
{
    /// <summary>The part of the score, from 0 to 1, at which a condition counts as holding.</summary>
    public const double Holds = 0.5;

    /// <summary>
    /// The verdict on one camera frame of a GroupLab sheet, judged the way the desktop judges a photograph: the paper's outline
    /// (<see cref="SheetOutline.Find"/>), then the sheet's own markers (<see cref="SheetMeasurer.Measure"/>), and from them the angle,
    /// quality (<see cref="CaptureRecord.Of"/>), where the sheet's corners fall, and how even the light is. The phone runs this on its
    /// analysis stream, several times a second.
    /// </summary>
    public static FrameVerdict JudgeFrame(GrayImage frame, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend, int? codesRead = null)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(definition);
        var outline = SheetOutline.Find(frame, out string? reason);
        var measurement = SheetMeasurer.Measure(frame, metadata, definition, new MeasureOptions(), backend, new TraceRecorder());
        CaptureQuality? quality = null;
        bool? cornersIn = null;
        double? evenness = null;
        int? read = null, expected = null;
        if (measurement.Registration is { } registration && measurement.Fiducials is { } markers)
        {
            (read, expected) = (markers.Matches.Count, markers.Expected);
            quality = CaptureRecord.Of(frame, metadata, registration.Mapping, definition.Page.Width, definition.Page.Height, markers.Matches.Count,
                markers.Expected, measurement.Lens?.K1, measurement.Lens?.K2).Quality;
            var pageToImage = CaptureRecord.PageToImage(registration.Mapping, definition.Page.Width, definition.Page.Height);
            double w = definition.Page.Width / 254.0, h = definition.Page.Height / 254.0;
            cornersIn = new PointD[] { new(0, 0), new(w, 0), new(w, h), new(0, h) }.Select(pageToImage.Apply)
                .All(c => c.X >= 0 && c.Y >= 0 && c.X <= frame.Width - 1 && c.Y <= frame.Height - 1);
            evenness = PictureCheck.Evenness(PictureCheck.BullPaper(frame, pageToImage, definition));
        }

        return Judge(outline, reason, quality, cornersIn) with
        {
            MarkersRead = read,
            MarkersExpected = expected,
            CodesRead = codesRead,
            CodesExpected = definition.Codes?.Positions.Count,
            Evenness = evenness,
        };
    }

    /// <summary>
    /// The verdict on a frame whose sheet is not yet known (<see cref="LiveSheet.Find"/> named none): markers seen but too small to read
    /// the codes from mean move closer; markers seen and large enough mean hold still while they are read; with none at all, the outline
    /// decides.
    /// </summary>
    public static FrameVerdict Search(LiveSearch search, SheetQuad? outline, string? outlineReason)
    {
        ArgumentNullException.ThrowIfNull(search);
        bool markers = search.MarkersFound >= LiveSheet.LeastMarkers;
        (Instruction Say, string Words) next =
            markers && search.MedianSidePixels < LiveSheet.ReadableSidePixels ? (Instruction.MoveCloser, "Move closer, so GroupLab can read the sheet's codes.")
            : markers ? (Instruction.FindTheSheet, "Hold still while GroupLab reads the sheet.")
            : outlineReason == SheetOutline.OutOfFrame ? (Instruction.MoveBack, "Fit the whole sheet in view, with a little space around it.")
            : outline is not null ? (Instruction.MoveCloser, "Move closer to the sheet.")
            : (Instruction.FindTheSheet, "Point the camera at the sheet.");
        return new FrameVerdict(next.Say, next.Words, outline is not null || markers, markers, false, false, false, false,
            MarkersRead: search.MarkersFound, CodesRead: search.CodesRead);
    }

    /// <summary>
    /// The verdict on one frame, from the outline search (<see cref="SheetOutline.Find"/>'s quad and reason) and, where the sheet was
    /// registered, the frame's quality, its angle, and whether its four corners fall inside the frame. With no quality, nothing past the
    /// outline can be judged, and it says so.
    /// </summary>
    public static FrameVerdict Judge(SheetQuad? outline, string? outlineReason, CaptureQuality? quality, bool? cornersInFrame = null)
    {
        bool inFrame = quality is not null && cornersInFrame is { } known ? known
            : outline is not null || outlineReason is not (SheetOutline.OutOfFrame or SheetOutline.NoSheet);
        bool detected = outline is not null || quality is not null;
        bool angle = quality is { } q1 && q1.OffAxisDegrees <= OffAxisLimit.Degrees;
        bool resolution = quality is { } q2 && q2.ResolutionPart >= Holds;
        bool focus = quality is { FocusPart: { } f } && f >= Holds;
        bool light = quality is { ExposurePart: { } e } && e >= Holds;
        bool markings = quality is { MarkingsPart: null } || quality is { MarkingsPart: { } m } && m >= Holds;

        (Instruction Say, string Words) next =
            outlineReason == SheetOutline.NoSheet && quality is null ? (Instruction.FindTheSheet, "Point the camera at the sheet.")
            : !inFrame ? (Instruction.MoveBack, "Move back.")
            : quality is null ? (Instruction.FindTheSheet, "Hold still while GroupLab finds the sheet.")
            : !resolution ? (Instruction.MoveCloser, "Move closer.")
            : !angle ? (Instruction.LessAngle, "Less angle: hold the phone square to the sheet.")
            : !focus ? (Instruction.HoldSteadier, "Hold steadier.")
            : !light ? (quality.ClippedShare is { } clipped && clipped > CaptureQualities.FineClipped ? (Instruction.LessLight, "Less light: the paper is too bright.") : (Instruction.MoreLight, "More light."))
            : !markings ? (Instruction.HoldSteadier, "Hold steadier.")
            : (Instruction.Ready, "Hold it there.");
        return new FrameVerdict(next.Say, next.Words, inFrame, detected, angle, focus, light, markings, quality);
    }
}
