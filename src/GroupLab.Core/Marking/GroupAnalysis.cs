using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>
/// A figure with its interval. The interval carries the coverage it actually has, never a bare nominal level
/// (NOTES-FROM-PLANNING.md entry 23 section 2 and entry 24 section 1), and what that coverage rests on. Where there is no interval,
/// its bounds and coverage are null and <see cref="IntervalUnavailable"/> says why.
/// </summary>
public sealed record ReportedEstimate(double Value, double? Lower, double? Upper, double? Coverage, string? CoverageBasis, string? IntervalUnavailable);

/// <summary>
/// How far the true dispersion can lie from the measured one at this many shots, as multiples of the measurement: docs/STATISTICS.md
/// section 9.1's 95 percent interval for sigma.
/// </summary>
public sealed record TrueSizeRange(double Lower, double Upper);

/// <summary>
/// One set of group figures, in inches at the target. What needs no sample size is there at any count: the count and the centre
/// relative to the aim. The dispersion figures are there only from <see cref="GroupAnalysis.MinimumShotsForDispersion"/> shots, and
/// otherwise null with <see cref="DispersionWithheld"/> saying so in the shooter's terms (NOTES-FROM-PLANNING.md entry 24 section 1).
/// The headline order is mean radius, then sigma, then extreme spread, subordinate (docs/PHASE1-BRIEF.md section 5).
/// <para>
/// Nothing is ever NaN. Every figure that can be undefined is null beside a sibling "Unavailable" field saying why, entry 24
/// section 3, so a consumer of the export gets a number or a null and never a string where it expects a number.
/// </para>
/// </summary>
public sealed record GroupFigures(
    int Shots,
    PointD? CentreFromAim,
    string? CentreFromAimUnavailable,
    ReportedEstimate? MeanRadius,
    string? MeanRadiusUnavailable,
    ReportedEstimate? Sigma,
    string? SigmaUnavailable,
    ReportedEstimate? ExtremeSpread,
    string? ExtremeSpreadUnavailable,
    TrueSizeRange? TrueSizeRange,
    string? TrueSizeRangeUnavailable,
    double? AspectRatio,
    string? AspectRatioUnavailable,
    double? AngleDegrees,
    string? AngleDegreesUnavailable,
    double? WorstShotInMeanRadii,
    string? WorstShotInMeanRadiiUnavailable,
    double? ExpectedWorstInMeanRadii,
    string? ExpectedWorstInMeanRadiiUnavailable,
    string? DispersionWithheld);

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
    /// <summary>
    /// The fewest shots a dispersion figure is quoted for. Below it the screen says what is missing instead of printing a number:
    /// two shots gave a mean radius of 0.914 in whose interval spanned a factor of twelve (NOTES-FROM-PLANNING.md entry 24 section 1).
    /// Five is docs/STATISTICS.md section 9.1's "the five-shot row is the one to put in front of a user", where the true dispersion
    /// lies within a factor of 2.84 of the measured; section 9 gives no sharper threshold, so it is interim and question 12 in
    /// docs/QUESTIONS-FOR-PLANNING.md.
    /// </summary>
    public const int MinimumShotsForDispersion = 5;

    /// <summary>
    /// Below this many shots the screen also states how far the true group size can lie from the measured one, section 9.1's range:
    /// entry 24 section 1's "between that minimum and about twenty". At twenty that range is 0.817 to 1.289.
    /// </summary>
    public const int SmallGroupShots = 20;

    /// <summary>The fewest shots the error ellipse's shape is defined for.</summary>
    public const int MinimumShotsForShape = 3;

    private const string RayleighBasis = "exact under the circular normal model: docs/STATISTICS.md section 3.3's chi-square interval with the c4 correction on both endpoints";
    private const string RangeBasis = "the simulated range-statistic table under the circular normal model, docs/STATISTICS.md section 5";

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
        string? problem = all is null ? "Mark the shots." : null;
        return new GroupReport(all, reduced, excluded, notShots, automatic, corrected, manual, state.Scale.Description, state.Scale.AssumesSquareOn, problem);
    }

    /// <summary>The plain sentence the screen shows for a group too small to quote, with section 9.1's range at that count where there is one.</summary>
    public static string WithheldSentence(int shots)
    {
        string range = "";
        if (shots >= 2)
        {
            var (lower, upper) = SampleSize.SigmaIntervalMultiples(shots);
            range = string.Create(CultureInfo.InvariantCulture, $" From {shots} shots the true group size could be anywhere from {lower:0.00} to {upper:0.00} times what they measure.");
        }

        return string.Create(CultureInfo.InvariantCulture,
            $"{shots} shot{(shots == 1 ? "" : "s")}. At least {MinimumShotsForDispersion} are needed before a group size is worth quoting, so none is shown.{range}");
    }

    private static GroupFigures? Figures(MarkingState state, IReadOnlyList<MarkedShot> shots)
    {
        if (shots.Count == 0 || state.Scale is not { } scale)
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

        int n = offsets.Count;
        var centre = GroupStatistics.Centre(offsets);
        PointD? centreFromAim = anyAim ? centre : null;
        string? centreUnavailable = anyAim ? null : "needs a point of aim, or shots assigned to bulls";

        if (n < MinimumShotsForDispersion)
        {
            string withheld = string.Create(CultureInfo.InvariantCulture, $"not quoted below {MinimumShotsForDispersion} shots");
            string shape = n < MinimumShotsForShape ? string.Create(CultureInfo.InvariantCulture, $"needs at least {MinimumShotsForShape} shots") : withheld;
            return new GroupFigures(
                Shots: n,
                CentreFromAim: centreFromAim,
                CentreFromAimUnavailable: centreUnavailable,
                MeanRadius: null,
                MeanRadiusUnavailable: withheld,
                Sigma: null,
                SigmaUnavailable: withheld,
                ExtremeSpread: null,
                ExtremeSpreadUnavailable: withheld,
                TrueSizeRange: null,
                TrueSizeRangeUnavailable: withheld,
                AspectRatio: null,
                AspectRatioUnavailable: shape,
                AngleDegrees: null,
                AngleDegreesUnavailable: shape,
                WorstShotInMeanRadii: null,
                WorstShotInMeanRadiiUnavailable: withheld,
                ExpectedWorstInMeanRadii: null,
                ExpectedWorstInMeanRadiiUnavailable: withheld,
                DispersionWithheld: WithheldSentence(n));
        }

        var rayleigh = GroupStatistics.Rayleigh(offsets);
        double rayleighCoverage = IntervalCoverage.RayleighSigma(n);
        double spread = GroupGeometry.MaximumPairDistance(offsets).Distance;
        var spreadInterval = RangeStatistics.MeanInterval(RangeStatistic.ExtremeSpread, spread, n);
        var (xx, xy, yy) = GroupStatistics.Covariance(offsets);
        var ellipse = GroupStatistics.Shape(xx, xy, yy);
        bool shapeDefined = double.IsFinite(ellipse.AspectRatio) && double.IsFinite(ellipse.AngleDegrees);
        const string collinear = "undefined: the shots lie on a line";
        double worst = GroupStatistics.Radii(offsets, centre).Max() / rayleigh.MeanRadius.Value;
        var (lower, upper) = SampleSize.SigmaIntervalMultiples(n);
        return new GroupFigures(
            Shots: n,
            CentreFromAim: centreFromAim,
            CentreFromAimUnavailable: centreUnavailable,
            MeanRadius: Reported(rayleigh.MeanRadius, rayleighCoverage, RayleighBasis, null),
            MeanRadiusUnavailable: null,
            Sigma: Reported(rayleigh.Sigma, rayleighCoverage, RayleighBasis, null),
            SigmaUnavailable: null,
            ExtremeSpread: Reported(spreadInterval, 0.95, RangeBasis, "beyond the range-statistic table's 100 shots"),
            ExtremeSpreadUnavailable: null,
            TrueSizeRange: new TrueSizeRange(lower, upper),
            TrueSizeRangeUnavailable: null,
            AspectRatio: shapeDefined ? ellipse.AspectRatio : null,
            AspectRatioUnavailable: shapeDefined ? null : collinear,
            AngleDegrees: shapeDefined ? ellipse.AngleDegrees : null,
            AngleDegreesUnavailable: shapeDefined ? null : collinear,
            WorstShotInMeanRadii: double.IsFinite(worst) ? worst : null,
            WorstShotInMeanRadiiUnavailable: double.IsFinite(worst) ? null : "undefined: every shot is in the same place",
            ExpectedWorstInMeanRadii: Flyers.ExpectedWorstInMeanRadii(n),
            ExpectedWorstInMeanRadiiUnavailable: null,
            DispersionWithheld: null);
    }

    private static ReportedEstimate Reported(Estimate e, double coverage, string basis, string? whyNoInterval) =>
        double.IsFinite(e.Lower) && double.IsFinite(e.Upper)
            ? new ReportedEstimate(e.Value, e.Lower, e.Upper, coverage, basis, null)
            : new ReportedEstimate(e.Value, null, null, null, null, whyNoInterval ?? "not available");

    /// <summary>
    /// The export a user saves, docs/PHASE1-BRIEF.md section 6 item 6: the image, how it was scaled, every shot with its image and
    /// target position, provenance, exclusion and assignment, and the report. JSON, because it is GroupLab's own record and not any
    /// other application's format. The serializer refuses NaN rather than quoting it, so an undefined figure that escaped
    /// <see cref="GroupFigures"/>'s nulls fails here instead of reaching a file.
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
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
