using System.Diagnostics;
using GroupLab.App.Diagnostics;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;

namespace GroupLab.Mobile;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 315 section 3: what the capture screen makes of each analysis frame, once, for Android, iOS and GroupLab
/// Dev's replay camera. A head's camera hands it a frame's luminance; it looks for the sheet (<see cref="LiveSheet"/>), judges the frame
/// (<see cref="CaptureGuidance"/>), decides the torch on Auto (<see cref="TorchGovernor"/>), holds the words steady
/// (<see cref="GuidanceSteadier"/>) and says when Guided mode takes the picture (<see cref="AutoShutter"/>). Until entry 315 each head
/// carried its own copy of these steps, line for line; one copy is what lets a clip recorded on either phone test both, since a replay runs
/// exactly what the live camera runs and only the frames, their times and the level's readings come from somewhere else.
/// </summary>
/// <param name="library">The sheets whose codes are matched; the phone's own built-in ones where not given.</param>
internal sealed class CameraJudge(Func<IReadOnlyList<TargetDefinition>>? library = null)
{
    private readonly Func<IReadOnlyList<TargetDefinition>> library = library ?? PhoneAnalysis.Library;
    private readonly GuidanceSteadier steadier = new();
    private readonly AutoShutter auto = new();
    private volatile TorchGovernor torchAuto = new(0);
    private int torchMax;
    private TargetDefinition? definition;
    private bool definitionFromCodes;
    private long frames;
    private int? codesRead;
    private Instruction? lastSay;
    private long readySince;

    /// <summary>The sheet the frames have named, null until one has.</summary>
    public TargetDefinition? Definition => definition;

    /// <summary>Everything forgotten but the sheet, as when the camera starts again.</summary>
    public void Reset()
    {
        steadier.Reset();
        auto.Reset();
    }

    /// <summary>The frames judged ready forgotten, as when the mode changes.</summary>
    public void ResetShutter() => auto.Reset();

    /// <summary>The torch on Auto started again from off, with <paramref name="levels"/> levels: 1 for on and off only, 0 for no torch.</summary>
    public void ResetTorch(int levels)
    {
        torchMax = levels;
        torchAuto = new TorchGovernor(levels);
    }

    /// <summary>
    /// One frame. <paramref name="clock"/> is read once the frame has been judged, as the live camera always read its steady clock, and a
    /// replay answers it with the time the frame was recorded at, so the waits are the recording's whatever this machine's pace.
    /// </summary>
    /// <param name="grey">The frame's luminance.</param>
    /// <param name="clock">Milliseconds on any steady clock.</param>
    /// <param name="measuredScale">The picture's pixels, as it is measured, per pixel of this frame (<see cref="PhoneCamera.MeasuredScale"/>).</param>
    /// <param name="torchOnAuto">Whether the torch is on Auto, so its governor decides.</param>
    /// <param name="manual">Whether the screen is in Manual, where the shutter never fires by itself.</param>
    public CameraStep Next(GrayImage grey, Func<long> clock, double measuredScale, bool torchOnAuto, bool manual)
    {
        ArgumentNullException.ThrowIfNull(grey);
        ArgumentNullException.ThrowIfNull(clock);
        var frameClock = Stopwatch.StartNew();
        var metadata = new ImageMetadata("YUV", grey.Width, grey.Height, null, null, "camera", "analysis", 1, null, null);
        var backend = new OpenCvSharpBackend();
        frames++;
        FrameVerdict verdict;
        if (definition is null || (!definitionFromCodes && frames % 5 == 0))
        {
            var search = LiveSheet.Find(grey, library(), backend);
            codesRead = search.CodesRead;
            if (search.Definition is not null && (definition is null || search.FromCodes))
            {
                (definition, definitionFromCodes) = (search.Definition, search.FromCodes);
                DiagnosticLog.Info("camera.sheet", ("from", search.FromCodes ? "codes" : "markers"), ("markers", search.MarkersFound));
            }

            verdict = definition is null
                ? CaptureGuidance.Search(search, SheetOutline.Find(grey, out string? reason), reason)
                : CaptureGuidance.JudgeFrame(grey, metadata, definition, backend, codesRead);
        }
        else
        {
            if (frames % 3 == 0)
            {
                codesRead = backend.ReadCodes(grey, 1.0).Count;
            }

            verdict = CaptureGuidance.JudgeFrame(grey, metadata, definition, backend, codesRead);
        }

        long now = clock();

        // Entry 302, torch on Auto: it starts at the lowest level, steps up only while the paper is dim, and steps down or goes off on glare, a
        // hotspot or paper already bright, with a settling time between changes so it never flickers.
        TorchChange? torch = null;
        if (torchOnAuto && torchAuto.Next(verdict.Quality, verdict.Evenness, now) is { } change)
        {
            torch = change;
            DiagnosticLog.Info("camera.torch", [("auto", change.Level == 0 ? "off" : "on"), ("level", change.Level), ("of", torchMax), ("reason", change.Reason),
                ("paper", verdict.Quality?.PaperLevel), ("clipped", verdict.Quality?.ClippedShare), ("evenness", verdict.Evenness), .. DeviceHealth.Fields()]);
        }

        // Entry 281 section 1.4: the words held steady, with resolution judged at the size the picture is measured at.
        verdict = steadier.Next(verdict, now, measuredScale);
        if (verdict.Say != lastSay)
        {
            lastSay = verdict.Say;
            readySince = now;
            DiagnosticLog.Info("camera.say", [("say", verdict.Say.ToString()), ("ms", now), ("frameMs", frameClock.ElapsedMilliseconds),
                ("markers", verdict.MarkersRead), ("codes", verdict.CodesRead), ("score", verdict.Quality?.Score), ("mode", manual ? "manual" : "guided"),
                ("failing", DiagnosticsOverlay.Failing(verdict)), .. DeviceHealth.Fields()]);
        }

        // Entry 273: on the printer check page the card is looked for too, and the shutter waits for it.
        bool? card = null;
        if (PrinterCheck.IsCheckPage(definition))
        {
            card = verdict.Mapping is { } mapping && verdict.PixelsPerMm is { } perMm && CardCheck.Measure(grey, mapping, definition!, perMm, 0) is not null;
            if (verdict.Say == Instruction.Ready)
            {
                verdict = verdict with { Words = card == true ? "Card found. Hold still." : "Lay the card inside the outline, flat." };
            }
        }

        // Entry 311 section 1: taken once every frame for AutoShutter.SteadyMs has been judged ready on its own.
        var decided = steadier.Decided;
        bool fire = auto.Next(decided, card != false, now);
        int? forecast = verdict.Quality is { } quality ? PictureCheck.Forecast(quality) : null;
        double progress = manual ? 0 : fire ? 1 : auto.Progress;
        return new CameraStep(verdict, decided, card, forecast, torch, !manual && fire, progress, now, frameClock.ElapsedMilliseconds, now - readySince, auto.ReadyMs);
    }
}

/// <summary>What one frame came to, for the head to show and act on.</summary>
/// <param name="Verdict">The frame's verdict with the words held steady: what the screen shows.</param>
/// <param name="Decided">The frame's own instruction before the words were held, null where it could not be judged.</param>
/// <param name="Card">On the printer check page, whether the card was found; null elsewhere.</param>
/// <param name="Forecast">What the picture is forecast to score, where the frame registered.</param>
/// <param name="Torch">The torch on Auto's change, null for none.</param>
/// <param name="Fire">Guided mode takes the picture now.</param>
/// <param name="Progress">The ring round the shutter, 0 to 1.</param>
/// <param name="NowMs">The time the frame was judged at.</param>
/// <param name="FrameMs">How long judging it took.</param>
/// <param name="SaidForMs">How long the words shown have been shown.</param>
/// <param name="SteadyMs">How long the frames have been judged ready.</param>
internal sealed record CameraStep(FrameVerdict Verdict, Instruction? Decided, bool? Card, int? Forecast, TorchChange? Torch, bool Fire, double Progress,
    long NowMs, long FrameMs, long SaidForMs, long SteadyMs);
