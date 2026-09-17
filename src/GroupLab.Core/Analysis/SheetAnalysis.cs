using System.Globalization;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Trace;

namespace GroupLab.Core.Analysis;

/// <summary>
/// One recovered shot: where it is on the image and on the page, the bull it was assigned to, its offset from that bull, and whether that
/// bull is a sighter, whose shots are reported but not pooled into the group.
/// </summary>
public sealed record AnalysedShot(int Id, PointD Image, PointD PageInches, int? Bull, string? BullLabel, PointD? OffsetInches, bool Sighter = false);

/// <summary>A whole analysis: the automatic path's result, every recovered shot, the pooled group's report, the marking it forms, and the trace.</summary>
public sealed record SheetAnalysisResult(
    AutomaticResult Automatic,
    IReadOnlyList<AnalysedShot> Shots,
    GroupReport? Report,
    MarkingState? Marking,
    IReadOnlyList<StageRecord> Trace,
    string? Failure);

/// <summary>
/// The primary path as one thing, NOTES-FROM-PLANNING.md entry 33 section 1: a photograph or scan of a GroupLab sheet in, a group out.
/// Registration from the printed markers (S1 to S4), render-and-difference inside the registered sheet (S5 to S8), assignment of each
/// hole to its bull (S9), and the composite group of every shot's offset from its own bull with the statistics of M3 (S10, docs/STATISTICS.md
/// section 2). It composes the same code the marking screen uses, <see cref="AutomaticMarking"/> and <see cref="GroupAnalysis"/>, so the
/// command line and the screen cannot disagree, and every stage files a <see cref="StageRecord"/> (DESIGN.md section 19 [r3]).
/// </summary>
public static class SheetAnalysis
{
    public static SheetAnalysisResult Run(string imagePath, GrayImage grey, GrayImage value, ImageMetadata metadata, TargetDefinition definition, IImagingBackend backend, TraceRecorder trace, Calibre? calibre = null)
    {
        ArgumentNullException.ThrowIfNull(trace);
        var automatic = AutomaticMarking.Run(grey, value, metadata, definition, backend, trace, calibre: calibre);
        if (automatic.Failure is not null || automatic.Scale is not { } scale)
        {
            return new SheetAnalysisResult(automatic, [], null, null, trace.Records, automatic.Failure ?? "registration failed");
        }

        using var stage = trace.Begin("S10.group");
        var session = new MarkingSession();
        session.Open(imagePath, metadata.Orientation);
        session.LoadDetections(scale, automatic.Bulls, automatic.Detections, automatic.Assignment, automatic.Rejected ?? [], automatic.Summary, automatic.Detection);
        if (calibre is not null)
        {
            session.SetCalibre(calibre);
        }

        var state = session.State;
        var bulls = state.Bulls.ToDictionary(b => b.Index);
        var shots = state.Shots.Select(s =>
        {
            var page = scale.ToTarget(s.Image);
            PointD? offset = s.Bull is { } b && bulls.TryGetValue(b, out var bull) && scale.ToTarget(bull.Image) is var centre
                ? new PointD(page.X - centre.X, page.Y - centre.Y)
                : null;
            var aim = s.Bull is { } i && bulls.TryGetValue(i, out var found) ? found : null;
            return new AnalysedShot(s.Id, s.Image, page, s.Bull, aim?.Label, offset, aim is { Scoring: false });
        }).ToList();
        var report = GroupAnalysis.Analyse(state);
        int unassigned = shots.Count(s => s.Bull is null);
        stage.Metric("shots in the group", report.AllShots?.Shots ?? 0, "count");
        if (report.AllShots?.MeanRadius is { } meanRadius)
        {
            stage.Metric("mean radius", meanRadius.Value, "in");
        }

        stage.Done(report.AllShots is null ? StageStatus.Degraded : StageStatus.Ok, report.AllShots is { } all
            ? string.Create(CultureInfo.InvariantCulture, $"{all.Shots} shots pooled about their own bulls{(report.SighterShots > 0 ? $", {report.SighterShots} sighter shots left out" : "")}{(unassigned > 0 ? $", {unassigned} without a bull" : "")}{(all.MeanRadius is { } mr ? $", mean radius {mr.Value:0.000} in" : $": {all.DispersionWithheld}")}")
            : "no shots to pool");
        return new SheetAnalysisResult(automatic, shots, report, state, trace.Records, null);
    }
}
