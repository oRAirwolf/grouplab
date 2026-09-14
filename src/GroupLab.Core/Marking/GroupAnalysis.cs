using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>
/// One set of group figures, in inches at the target: the count, mean radius with its interval as the headline, sigma with its
/// interval beneath it, extreme spread with its interval and visibly subordinate (docs/PHASE1-BRIEF.md section 5), the group centre
/// relative to the aim where there is one, the error ellipse's aspect and angle, and what section 10 expects of the worst shot.
/// </summary>
public sealed record GroupFigures(
    int Shots,
    Estimate MeanRadius,
    Estimate Sigma,
    Estimate ExtremeSpread,
    PointD? CentreFromAim,
    double AspectRatio,
    double AngleDegrees,
    double WorstShotInMeanRadii,
    double ExpectedWorstInMeanRadii);

/// <summary>
/// A marking's report: every figure computed with and without the excluded shots, side by side, so an exclusion can never be
/// hidden (docs/STATISTICS.md section 10); how the shots were placed; and how the scale was set, with its assumption.
/// </summary>
public sealed record GroupReport(
    GroupFigures? AllShots,
    GroupFigures? WithoutExclusions,
    int Excluded,
    int NotShots,
    int Automatic,
    int Corrected,
    int Manual,
    string Scale,
    bool ScaleAssumesSquareOn,
    string? Problem);

/// <summary>
/// The statistics the marking screen shows, from the engine of M3 and nothing it does not specify. Each shot's offset is taken
/// from its bull's centre when it is assigned to one, which is the composite group of docs/STATISTICS.md section 2, and otherwise
/// from the point of aim when one is marked, and otherwise from the image origin, in which case there is no centre offset to report.
/// </summary>
public static class GroupAnalysis
{
    /// <summary>The fewest shots the figures are computed for: a group of one has no dispersion.</summary>
    public const int MinimumShots = 2;

    public static GroupReport Analyse(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var shots = state.Shots.Where(s => s.IsShot).ToList();
        int excluded = shots.Count(s => s.Exclusion is not null);
        int notShots = state.Shots.Count(s => !s.IsShot);
        int automatic = shots.Count(s => s.Provenance == ShotProvenance.Automatic), corrected = shots.Count(s => s.Provenance == ShotProvenance.Corrected), manual = shots.Count(s => s.Provenance == ShotProvenance.Manual);
        if (state.Scale is null)
        {
            return new GroupReport(null, null, excluded, notShots, automatic, corrected, manual, "no scale set", false, "Set a scale before the group can be measured: a reference length, a reference rectangle, or a GroupLab sheet's markers.");
        }

        var all = Figures(state, shots);
        var reduced = excluded == 0 ? all : Figures(state, [.. shots.Where(s => s.Exclusion is null)]);
        string? problem = all is null ? $"Mark at least {MinimumShots} shots." : null;
        return new GroupReport(all, reduced, excluded, notShots, automatic, corrected, manual, state.Scale.Description, state.Scale.AssumesSquareOn, problem);
    }

    private static GroupFigures? Figures(MarkingState state, IReadOnlyList<MarkedShot> shots)
    {
        if (shots.Count < MinimumShots || state.Scale is not { } scale)
        {
            return null;
        }

        var bulls = state.Bulls.ToDictionary(b => b.Index);
        PointD? aim = state.PointOfAim is { } poa ? scale.ToTarget(poa) : null;
        bool anyAim = aim is not null || shots.Any(s => s.Bull is { } b && bulls.ContainsKey(b));
        var offsets = shots.Select(s =>
        {
            var at = scale.ToTarget(s.Image);
            PointD? origin = s.Bull is { } b && bulls.TryGetValue(b, out var bull) ? scale.ToTarget(bull.Image) : aim;
            return origin is { } o ? new PointD(at.X - o.X, at.Y - o.Y) : at;
        }).ToList();

        var rayleigh = GroupStatistics.Rayleigh(offsets);
        double spread = GroupGeometry.MaximumPairDistance(offsets).Distance;
        var spreadInterval = RangeStatistics.Interval(RangeStatistic.ExtremeSpread, spread, offsets.Count);
        var centre = GroupStatistics.Centre(offsets);
        var (xx, xy, yy) = GroupStatistics.Covariance(offsets);
        var shape = GroupStatistics.Shape(xx, xy, yy);
        double worst = GroupStatistics.Radii(offsets, centre).Max();
        return new GroupFigures(
            offsets.Count,
            rayleigh.MeanRadius,
            rayleigh.Sigma,
            spreadInterval,
            anyAim ? centre : null,
            offsets.Count >= 3 ? shape.AspectRatio : double.NaN,
            offsets.Count >= 3 ? shape.AngleDegrees : double.NaN,
            worst / rayleigh.MeanRadius.Value,
            Flyers.ExpectedWorstInMeanRadii(offsets.Count));
    }

    /// <summary>
    /// The export a user saves, docs/PHASE1-BRIEF.md section 6 item 6: the image, how it was scaled, every shot with its image and
    /// target position, provenance, exclusion and assignment, and the report. JSON, because it is GroupLab's own record and not any
    /// other application's format.
    /// </summary>
    public static string Export(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var report = Analyse(state);
        var document = new
        {
            format = "grouplab-marking-1",
            image = state.ImagePath,
            scale = report.Scale,
            scaleAssumesSquareOn = report.ScaleAssumesSquareOn,
            registration = state.RegistrationSummary,
            pointOfAim = state.PointOfAim,
            bulls = state.Bulls.Select(b => new { b.Index, b.Label, image = b.Image }),
            shots = state.Shots.Select(s => new
            {
                s.Id,
                image = s.Image,
                targetInches = state.Scale?.ToTarget(s.Image),
                provenance = s.Provenance.ToString(),
                exclusion = s.Exclusion?.ToString(),
                s.NotAShot,
                s.Bull,
            }),
            report,
        };
        return JsonSerializer.Serialize(document, Options);
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };
}
