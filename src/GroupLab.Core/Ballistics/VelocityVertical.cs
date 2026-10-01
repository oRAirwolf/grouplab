using System.Globalization;
using GroupLab.Core.Records;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Ballistics;

/// <summary>A figure with the interval around it, at the confidence the result states.</summary>
public sealed record IntervalFigure(double Value, double Lower, double Upper);

/// <summary>
/// The regression of vertical impact on velocity over shots whose readings were matched to them: the slope measured, in inches per ft/s, with
/// its interval, beside the slope the solver predicts at the distance shot.
/// </summary>
public sealed record VelocitySlope(int Pairs, IntervalFigure SlopeInchesPerFps, double PredictedInchesPerFps, bool PredictedInside, bool IncludesNone, string Sentence);

/// <summary>
/// What velocity alone would add to a group's vertical at the distance shot, set against the vertical measured: the readings' SD and the
/// vertical SD it predicts, the group's own vertical SD, the share of the vertical variance velocity accounts for, each with its interval,
/// and the regression on matched shots where there is one.
/// </summary>
public sealed record VelocityVerticalResult(
    double DistanceYards,
    double Confidence,
    int Readings,
    double MeanFps,
    IntervalFigure VelocitySdFps,
    double DropPerFpsInches,
    IntervalFigure PredictedVerticalSdInches,
    int Shots,
    IntervalFigure MeasuredVerticalSdInches,
    IntervalFigure Share,
    bool CannotTell,
    string Sentence,
    VelocitySlope? Slope);

/// <summary>
/// Velocity regression, and predicted against measured vertical, NOTES-FROM-PLANNING.md entry 322 section 3 (Phase 5).
/// <para>
/// <b>Predicted.</b> The solver gives the change of the bullet's height at the distance per ft/s of muzzle velocity, with the sight left where
/// the load's mean velocity zeroes it (<see cref="Projection.DropPerFps"/>, a central difference). Velocity alone then adds a vertical SD of
/// |d drop / d V| times the velocity SD; that is linear in the SD, so the SD's chi-squared interval maps straight across.
/// </para>
/// <para>
/// <b>Measured.</b> The group's vertical SD on n minus 1, with its own chi-squared interval. The share is velocity's variance over the
/// measured variance. Its interval treats the two SDs as independent samples, so the ratio of each to its truth is F on (readings - 1,
/// shots - 1) degrees of freedom. When the readings come from the same shots the two are positively correlated, which makes the true
/// interval narrower than this one: the interval errs wide, never narrow. Where it reaches 100% the sentence says the data cannot tell.
/// </para>
/// <para>
/// <b>Regression.</b> Only where readings were matched to shots by the reconciliation a person accepted (DESIGN.md section 15): least squares
/// of vertical on velocity, the slope's interval from Student's t on n minus 2, against the solver's slope.
/// </para>
/// </summary>
public static class VelocityVertical
{
    /// <summary>
    /// The comparison, or the reason there is none. <paramref name="verticalInches"/> is each shot's height at the target, up positive, in
    /// inches at the distance shot; <paramref name="matched"/> the accepted pairs of a reading and its shot's height, or null.
    /// </summary>
    public static (VelocityVerticalResult? Result, string? Refusal) Analyze(
        BallisticInput input,
        double distanceYards,
        IReadOnlyList<double> readings,
        IReadOnlyList<double> verticalInches,
        IReadOnlyList<(double Fps, double VerticalInches)>? matched = null,
        double confidence = 0.90)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(verticalInches);
        if (!(distanceYards > 0))
        {
            return (null, "The distance shot must be more than zero.");
        }

        if (!(confidence is > 0 and < 1))
        {
            return (null, "The confidence must lie between 0 and 1.");
        }

        if (Chronograph.Spread(readings) is not { } velocity)
        {
            return (null, "There must be at least two chronograph readings to have a velocity SD.");
        }

        if (verticalInches.Count < 2)
        {
            return (null, "There must be at least two shots to have a vertical SD.");
        }

        double mean = verticalInches.Average();
        double sdY = Math.Sqrt(verticalInches.Sum(y => (y - mean) * (y - mean)) / (verticalInches.Count - 1));
        if (!(sdY > 0))
        {
            return (null, "The shots have no vertical spread at all, so there is nothing to compare velocity with.");
        }

        var load = input with { MuzzleVelocityFps = velocity.MeanFps };
        double k = Projection.DropPerFps(load, distanceYards);
        double gain = Math.Abs(k);
        var (vLow, vHigh) = Chronograph.SdInterval(velocity.Readings, velocity.SdFps, confidence)!.Value;
        var (yLow, yHigh) = Chronograph.SdInterval(verticalInches.Count, sdY, confidence)!.Value;
        var predicted = new IntervalFigure(gain * velocity.SdFps, gain * vLow, gain * vHigh);

        double tail = (1 - confidence) / 2;
        double share = predicted.Value * predicted.Value / (sdY * sdY);
        double df1 = velocity.Readings - 1, df2 = verticalInches.Count - 1;
        var shareFigure = new IntervalFigure(share, share / Distributions.FQuantile(1 - tail, df1, df2), share / Distributions.FQuantile(tail, df1, df2));
        bool cannotTell = shareFigure.Upper >= 1;

        var slope = matched is { Count: >= 3 } ? Regress(matched, k, confidence) : null;
        var result = new VelocityVerticalResult(
            distanceYards, confidence, velocity.Readings, velocity.MeanFps, new IntervalFigure(velocity.SdFps, vLow, vHigh), k, predicted,
            verticalInches.Count, new IntervalFigure(sdY, yLow, yHigh), shareFigure, cannotTell, ShareSentence(shareFigure, confidence), slope);
        return (result, null);
    }

    /// <summary>The share in one plain sentence: about how much, the interval, and where the interval reaches all of it, that the data cannot tell.</summary>
    public static string ShareSentence(IntervalFigure share, double confidence)
    {
        ArgumentNullException.ThrowIfNull(share);
        var inv = CultureInfo.InvariantCulture;
        string level = string.Create(inv, $"{confidence * 100:0.#}%");
        string range = string.Create(inv, $"{Percent(share.Lower)} to {Percent(share.Upper)} at {level}");
        if (share.Value >= 1)
        {
            return $"The velocity spread measured would by itself make more vertical than the group shows (about {Percent(share.Value)}, {range}), "
                + "so the data cannot tell how much of the vertical is velocity; more readings or more shots would.";
        }

        return share.Upper >= 1
            ? $"Velocity alone accounts for about {Percent(share.Value)} of the vertical, {range}; that reaches 100%, so the data cannot tell whether anything besides velocity moves these shots up and down."
            : $"Velocity alone accounts for about {Percent(share.Value)} of the vertical, {range}.";
    }

    /// <summary>
    /// Least squares of vertical on velocity, with the slope's interval from Student's t on n minus 2 degrees of freedom, or null where the
    /// velocities do not vary.
    /// </summary>
    public static VelocitySlope? Regress(IReadOnlyList<(double Fps, double VerticalInches)> pairs, double predictedInchesPerFps, double confidence = 0.90)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        int n = pairs.Count;
        if (n < 3)
        {
            return null;
        }

        double mx = pairs.Average(p => p.Fps), my = pairs.Average(p => p.VerticalInches);
        double sxx = pairs.Sum(p => (p.Fps - mx) * (p.Fps - mx));
        if (!(sxx > 0))
        {
            return null;
        }

        double sxy = pairs.Sum(p => (p.Fps - mx) * (p.VerticalInches - my));
        double b = sxy / sxx;
        double sse = pairs.Sum(p => Math.Pow(p.VerticalInches - my - b * (p.Fps - mx), 2));
        double se = Math.Sqrt(sse / (n - 2) / sxx);
        double t = Distributions.StudentTQuantile(1 - (1 - confidence) / 2, n - 2);
        var slope = new IntervalFigure(b, b - t * se, b + t * se);
        bool inside = predictedInchesPerFps >= slope.Lower && predictedInchesPerFps <= slope.Upper;
        bool none = slope.Lower <= 0 && slope.Upper >= 0;
        var inv = CultureInfo.InvariantCulture;
        string measured = string.Create(inv, $"Shot by shot over {n} matched shots, 10 ft/s faster moved the impact {b * 10:+0.000;-0.000} in ({slope.Lower * 10:+0.000;-0.000} to {slope.Upper * 10:+0.000;-0.000} at {confidence * 100:0.#}%), against the solver's {predictedInchesPerFps * 10:+0.000;-0.000} in");
        string verdict = (inside, none) switch
        {
            (true, true) => "; the solver's figure lies inside the interval, but so does no effect at all, so these shots cannot tell the two apart.",
            (true, false) => "; the solver's figure lies inside the interval, so the vertical follows velocity as the solver says it should.",
            (false, true) => "; the solver's figure lies outside the interval and no effect lies inside it, so something besides velocity drives this vertical, or the readings are paired with the wrong shots.",
            (false, false) => "; the solver's figure lies outside the interval, so the vertical follows velocity by a different amount than the solver predicts. Check the pairing, the distance and the BC.",
        };
        return new VelocitySlope(n, slope, predictedInchesPerFps, inside, none, measured + verdict);
    }

    /// <summary>A share as a whole percent, or "under 1%" for a share above nothing that rounds to none.</summary>
    public static string Percent(double share) =>
        share is > 0 and < 0.01 ? "under 1%" : string.Create(CultureInfo.InvariantCulture, $"{share * 100:0}%");
}
