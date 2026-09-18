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
/// <para>
/// NOTES-FROM-PLANNING.md entry 103: CEP at 50, 90 and 95 percent, the group's width and height in target coordinates with the per-axis
/// standard deviations, and the two shape tests of docs/STATISTICS.md section 7, circularity and vertical stringing, which the analysis
/// state's judgement cards read, and the worst shot calibrated against circular groups measured the same way (entry 104 section 2, where
/// <see cref="ExpectedWorstInMeanRadii"/> is section 10's population figure and <see cref="WorstShot"/> is the one to judge a group's own
/// worst shot by). Below the dispersion minimum they are null, and <see cref="DispersionWithheld"/> says why; where the
/// shots lie on a line the tests are null and <see cref="ShapeTestsUnavailable"/> says so.
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
    double? ExtremeSpreadEdgeToEdge,
    string? ExtremeSpreadEdgeToEdgeUnavailable,
    TrueSizeRange? TrueSizeRange,
    string? TrueSizeRangeUnavailable,
    double? AspectRatio,
    string? AspectRatioUnavailable,
    double? AngleDegrees,
    string? AngleDegreesUnavailable,
    double? CircularMedianAspect,
    string? CircularMedianAspectUnavailable,
    double? CircularAspectExceedance,
    string? CircularAspectExceedanceUnavailable,
    double? WorstShotInMeanRadii,
    string? WorstShotInMeanRadiiUnavailable,
    double? ExpectedWorstInMeanRadii,
    string? ExpectedWorstInMeanRadiiUnavailable,
    string? DispersionWithheld,
    ReportedEstimate? Cep50 = null,
    ReportedEstimate? Cep90 = null,
    ReportedEstimate? Cep95 = null,
    double? Width = null,
    double? Height = null,
    double? SdX = null,
    double? SdY = null,
    CircularityTest? Circularity = null,
    StringingTest? Stringing = null,
    string? ShapeTestsUnavailable = null,
    WorstShotCalibration? WorstShot = null);

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
    string? Problem,
    int SighterShots = 0,
    string? Detection = null);

/// <summary>
/// One subgroup of a sheet, NOTES-FROM-PLANNING.md entry 94 section 2: its name, the bulls that hold it, and its own figures, computed
/// exactly as a whole sheet's are. A sheet of thirty bulls carrying six charge weights is six of these.
/// </summary>
public sealed record Subgroup(string Name, IReadOnlyList<int> Bulls, int Shots, GroupFigures? Figures);

/// <summary>
/// The sheet's subgroups and what comparing them says, entry 94 section 2 and entry 89 section 3. Both tests are the non-parametric ones
/// docs/STATISTICS.md section 8.3 names, because a subgroup on one sheet is small and a five-shot load is the usual case:
/// <see cref="DispersionPValue"/> is Fligner-Killeen on each shot's distance from its own subgroup's centre, and
/// <see cref="CentrePValue"/> is the one-way MANOVA of section 8.2 on the shot offsets, which for two subgroups is Hotelling's test.
/// <para>
/// No verdict is drawn here. A p-value is reported beside the subgroups and the reader decides, because "this load is better" is a claim
/// about the next group rather than about this one, and entry 91 makes the same point about a zero correction.
/// </para>
/// </summary>
public sealed record SubgroupReport(IReadOnlyList<Subgroup> Subgroups, double? DispersionPValue, double? CentrePValue, string? ComparisonUnavailable);

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

        // A shot assigned to a sighter bull is a sighting shot, not part of the group: the first end-to-end run (NOTES-FROM-PLANNING.md
        // entry 33 section 1) pooled a GL-CF25-LTR sheet's three sighters into its 25-shot group. They are counted, and left out.
        var sighterBulls = state.Bulls.Where(b => !b.Scoring).Select(b => b.Index).ToHashSet();
        var candidates = state.Shots.Where(s => s.IsShot).ToList();
        int sighterShots = candidates.Count(s => s.Bull is { } b && sighterBulls.Contains(b));
        var shots = candidates.Where(s => !(s.Bull is { } b && sighterBulls.Contains(b))).ToList();
        int excluded = shots.Count(s => s.Exclusion is not null);
        int notShots = state.Shots.Count(s => !s.IsShot);
        int automatic = shots.Count(s => s.Provenance == ShotProvenance.Automatic), corrected = shots.Count(s => s.Provenance == ShotProvenance.Corrected), manual = shots.Count(s => s.Provenance == ShotProvenance.Manual);
        if (state.Scale is null)
        {
            return new GroupReport(null, null, excluded, notShots, automatic, corrected, manual, "no scale set", false, "Set a scale before the group can be measured: a reference length, a reference rectangle, or a GroupLab sheet's markers.", sighterShots, state.Detection?.Describe());
        }

        // Entry 39 section 1: on a sheet of several bulls, a shot with no bull is measured from the single point of aim, so twelve shots near
        // twelve bulls measured the distance between bulls, to three decimals with an interval. The figures are withheld with what is missing.
        if (state.Bulls.Count(b => b.Scoring) > 1 && shots.Count(s => s.Bull is null) is > 0 and var unassigned)
        {
            string sentence = UnassignedSentence(shots.Count, unassigned);
            var withheld = Withheld(shots.Count, null, "needs every shot assigned to a bull", "needs every shot assigned to a bull", "needs every shot assigned to a bull", sentence);
            var withheldReduced = excluded == 0 ? withheld : withheld with { Shots = shots.Count(s => s.Exclusion is null) };
            return new GroupReport(withheld, withheldReduced, excluded, notShots, automatic, corrected, manual, state.Scale.Description, state.Scale.AssumesSquareOn, null, sighterShots, state.Detection?.Describe());
        }

        var all = Figures(state, shots);
        var reduced = excluded == 0 ? all : Figures(state, [.. shots.Where(s => s.Exclusion is null)]);
        string? problem = all is null ? "Mark the shots." : null;
        return new GroupReport(all, reduced, excluded, notShots, automatic, corrected, manual, state.Scale.Description, state.Scale.AssumesSquareOn, problem, sighterShots, state.Detection?.Describe());
    }

    /// <summary>
    /// The sheet's subgroups, NOTES-FROM-PLANNING.md entry 94 section 2, or null where no bull has been put in one. Shots on bulls with no
    /// subgroup are left out of every subgroup and stay in the whole-sheet figures, which are unchanged by any of this: a subgroup is a way
    /// of reading one sheet, not a different sheet.
    /// </summary>
    public static SubgroupReport? Subgroups(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Subgroups is not { } map || map.ByBull.IsEmpty || state.Scale is not { } scale)
        {
            return null;
        }

        var bulls = state.Bulls.ToDictionary(b => b.Index);
        var scoring = state.Bulls.Where(b => b.Scoring).Select(b => b.Index).ToHashSet();
        var shots = state.Shots
            .Where(s => s.IsShot && s.Exclusion is null && s.Bull is { } b && scoring.Contains(b) && map.For(b) is not null)
            .ToList();

        var subgroups = map.Names
            .Select(name =>
            {
                var theirs = shots.Where(s => map.For(s.Bull!.Value) == name).ToList();
                var theirBulls = map.ByBull.Where(p => p.Value == name).Select(p => p.Key).Order().ToList();
                return new Subgroup(name, theirBulls, theirs.Count, Figures(state, theirs));
            })
            .ToList();

        // The comparison needs two subgroups that each have shots, and the offsets are composite: each shot from its own bull's centre.
        var usable = subgroups.Where(g => g.Shots > 0).ToList();
        if (usable.Count < 2)
        {
            return new SubgroupReport(subgroups, null, null, "needs shots in at least two subgroups");
        }

        var offsets = new List<PointD>();
        var labels = new List<int>();
        for (int k = 0; k < usable.Count; k++)
        {
            foreach (var shot in shots.Where(s => map.For(s.Bull!.Value) == usable[k].Name))
            {
                var at = scale.ToTarget(shot.Image);
                var origin = scale.ToTarget(bulls[shot.Bull!.Value].Image);
                offsets.Add(new PointD(at.X - origin.X, at.Y - origin.Y));
                labels.Add(k);
            }
        }

        // Fligner-Killeen reads one number per shot, so it gets each shot's distance from its own subgroup's centre.
        var centres = Enumerable.Range(0, usable.Count)
            .ToDictionary(k => k, k => GroupStatistics.Centre([.. Enumerable.Range(0, offsets.Count).Where(i => labels[i] == k).Select(i => offsets[i])]));
        var radii = Enumerable.Range(0, offsets.Count)
            .Select(i => Math.Sqrt(Math.Pow(offsets[i].X - centres[labels[i]].X, 2) + Math.Pow(offsets[i].Y - centres[labels[i]].Y, 2)))
            .ToList();

        double? dispersion = null, centre = null;
        string? unavailable = null;
        if (offsets.Count <= usable.Count + 1)
        {
            unavailable = "needs more shots than subgroups before either test has anything to say";
        }
        else
        {
            dispersion = GroupComparison.FlignerKilleen(radii, labels).PValue;
            centre = GroupComparison.ManovaGroups(offsets, labels).PValue;
        }

        return new SubgroupReport(subgroups, dispersion, centre, unavailable);
    }

    /// <summary>
    /// Each shot's offset from what it is measured against, in inches at the target: its own bull's centre where it has one, which is the
    /// composite group of docs/STATISTICS.md section 2, and otherwise the point of aim. It is one definition, read by the figures and by the
    /// zero correction (NOTES-FROM-PLANNING.md entry 91), so the two can never disagree about where the centre is.
    /// </summary>
    public static IReadOnlyList<PointD> CompositeOffsets(MarkingState state, IReadOnlyList<MarkedShot> shots)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shots);
        if (state.Scale is not { } scale)
        {
            return [];
        }

        var bulls = state.Bulls.ToDictionary(b => b.Index);
        PointD? aim = state.PointOfAim is { } poa ? scale.ToTarget(poa) : null;
        return [.. shots.Select(s =>
        {
            var at = scale.ToTarget(s.Image);
            PointD? origin = s.Bull is { } b && bulls.TryGetValue(b, out var bull) ? scale.ToTarget(bull.Image) : aim;
            return origin is { } o ? new PointD(at.X - o.X, at.Y - o.Y) : at;
        })];
    }

    /// <summary>Whether there is anything to measure an offset from: a point of aim, or shots on bulls.</summary>
    public static bool HasOrigin(MarkingState state, IReadOnlyList<MarkedShot> shots)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shots);
        var bulls = state.Bulls.Select(b => b.Index).ToHashSet();
        return state.PointOfAim is not null || shots.Any(s => s.Bull is { } b && bulls.Contains(b));
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

    /// <summary>
    /// The sentence shown where the figures would be when shots on a sheet of several bulls are not all assigned to one (entry 39
    /// section 1): what the numbers would have measured, and what to do.
    /// </summary>
    public static string UnassignedSentence(int shots, int unassigned) => unassigned == shots
        ? string.Create(CultureInfo.InvariantCulture,
            $"{shots} shot{(shots == 1 ? "" : "s")}, none assigned to a bull, so figures from them would measure the spread of your marks across the sheet rather than the group. Assign them to bulls, or mark them on one bull.")
        : string.Create(CultureInfo.InvariantCulture,
            $"{unassigned} of {shots} shots {(unassigned == 1 ? "is" : "are")} not assigned to a bull, so {(unassigned == 1 ? "it would be" : "they would be")} measured from the point of aim and the rest from their bulls, and no figure means anything. Assign every shot to its bull.");

    /// <summary>
    /// Figures that cannot be quoted, with the sentence the screen shows in their place: every dispersion figure null beside its reason.
    /// The rule this implements, from entry 24 section 1 and generalised by entry 39 section 1: whenever the figures cannot measure what
    /// a reader takes them to measure, the report says what is missing instead of printing a number.
    /// </summary>
    private static GroupFigures Withheld(int n, PointD? centreFromAim, string? centreUnavailable, string reason, string shapeReason, string sentence) => new(
        Shots: n,
        CentreFromAim: centreFromAim,
        CentreFromAimUnavailable: centreUnavailable,
        MeanRadius: null,
        MeanRadiusUnavailable: reason,
        Sigma: null,
        SigmaUnavailable: reason,
        ExtremeSpread: null,
        ExtremeSpreadUnavailable: reason,
        ExtremeSpreadEdgeToEdge: null,
        ExtremeSpreadEdgeToEdgeUnavailable: reason,
        TrueSizeRange: null,
        TrueSizeRangeUnavailable: reason,
        AspectRatio: null,
        AspectRatioUnavailable: shapeReason,
        AngleDegrees: null,
        AngleDegreesUnavailable: shapeReason,
        CircularMedianAspect: null,
        CircularMedianAspectUnavailable: shapeReason,
        CircularAspectExceedance: null,
        CircularAspectExceedanceUnavailable: shapeReason,
        WorstShotInMeanRadii: null,
        WorstShotInMeanRadiiUnavailable: reason,
        ExpectedWorstInMeanRadii: null,
        ExpectedWorstInMeanRadiiUnavailable: reason,
        DispersionWithheld: sentence);

    private static GroupFigures? Figures(MarkingState state, IReadOnlyList<MarkedShot> shots)
    {
        if (shots.Count == 0 || state.Scale is not { } scale)
        {
            return null;
        }

        bool anyAim = HasOrigin(state, shots);
        var offsets = CompositeOffsets(state, shots);

        int n = offsets.Count;
        var centre = GroupStatistics.Centre(offsets);
        PointD? centreFromAim = anyAim ? centre : null;
        string? centreUnavailable = anyAim ? null : "needs a point of aim, or shots assigned to bulls";

        if (n < MinimumShotsForDispersion)
        {
            string withheld = string.Create(CultureInfo.InvariantCulture, $"not quoted below {MinimumShotsForDispersion} shots");
            string shape = n < MinimumShotsForShape ? string.Create(CultureInfo.InvariantCulture, $"needs at least {MinimumShotsForShape} shots") : withheld;
            return Withheld(n, centreFromAim, centreUnavailable, withheld, shape, WithheldSentence(n));
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
        double width = offsets.Max(o => o.X) - offsets.Min(o => o.X), height = offsets.Max(o => o.Y) - offsets.Min(o => o.Y);
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
            ExtremeSpreadEdgeToEdge: state.Calibre is { } calibre ? spread + calibre.DiameterInches : null,
            ExtremeSpreadEdgeToEdgeUnavailable: state.Calibre is null ? "needs the group's calibre" : null,
            TrueSizeRange: new TrueSizeRange(lower, upper),
            TrueSizeRangeUnavailable: null,
            AspectRatio: shapeDefined ? ellipse.AspectRatio : null,
            AspectRatioUnavailable: shapeDefined ? null : collinear,
            AngleDegrees: shapeDefined ? ellipse.AngleDegrees : null,
            AngleDegreesUnavailable: shapeDefined ? null : collinear,
            CircularMedianAspect: CircularAspect.Median(n),
            CircularMedianAspectUnavailable: null,
            CircularAspectExceedance: shapeDefined ? CircularAspect.ProbabilityAbove(n, ellipse.AspectRatio) : null,
            CircularAspectExceedanceUnavailable: shapeDefined ? null : collinear,
            WorstShotInMeanRadii: double.IsFinite(worst) ? worst : null,
            WorstShotInMeanRadiiUnavailable: double.IsFinite(worst) ? null : "undefined: every shot is in the same place",
            ExpectedWorstInMeanRadii: Flyers.ExpectedWorstInMeanRadii(n),
            ExpectedWorstInMeanRadiiUnavailable: null,
            DispersionWithheld: null,
            Cep50: Reported(rayleigh.Cep(0.5), rayleighCoverage, RayleighBasis, null),
            Cep90: Reported(rayleigh.Cep(0.9), rayleighCoverage, RayleighBasis, null),
            Cep95: Reported(rayleigh.Cep(0.95), rayleighCoverage, RayleighBasis, null),
            Width: width,
            Height: height,
            SdX: Math.Sqrt(xx),
            SdY: Math.Sqrt(yy),
            Circularity: shapeDefined ? ShapeTests.Circularity(offsets) : null,
            Stringing: shapeDefined ? ShapeTests.VerticalStringing(offsets) : null,
            ShapeTestsUnavailable: shapeDefined ? null : collinear,
            WorstShot: double.IsFinite(worst) ? Flyers.CalibrateWorst(n, worst) : null);
    }

    private static ReportedEstimate Reported(Estimate e, double coverage, string basis, string? whyNoInterval) =>
        double.IsFinite(e.Lower) && double.IsFinite(e.Upper)
            ? new ReportedEstimate(e.Value, e.Lower, e.Upper, coverage, basis, null)
            : new ReportedEstimate(e.Value, null, null, null, null, whyNoInterval ?? "not available");

    /// <summary>
    /// The export a user saves, <see cref="MarkingFile.Write"/>. Its serializer refuses NaN rather than quoting it, so an undefined
    /// figure that escaped <see cref="GroupFigures"/>'s nulls fails here instead of reaching a file.
    /// </summary>
    public static string Export(MarkingState state) => MarkingFile.Write(state);
}
