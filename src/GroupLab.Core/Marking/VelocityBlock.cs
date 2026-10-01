using System.Globalization;
using GroupLab.Core.Ballistics;
using GroupLab.Core.Records;

namespace GroupLab.Core.Marking;

/// <summary>Which of the five approved states the block is in, NOTES-FROM-PLANNING.md entry 323 section 4.</summary>
public enum VelocityBlockState
{
    /// <summary>State 1: the data can tell, and the share is the headline.</summary>
    Result,

    /// <summary>State 2: the share's interval reaches all of the vertical, so the data cannot tell.</summary>
    CannotTell,

    /// <summary>State 3: the session has no chronograph readings.</summary>
    NoReadings,

    /// <summary>State 4: the distance shot is missing.</summary>
    NoDistance,

    /// <summary>State 4: the load's BC, or its drag model, is missing.</summary>
    NoBc,

    /// <summary>The solver gave no answer for the readings and shots, with the reason as the sentence.</summary>
    Refused,
}

/// <summary>
/// One of the two bars on a shared scale: its name, the value with its interval in inches, the value as the Group block writes it, and
/// beneath it the size on the paper where the value is an angle, as the Group block's figures carry it.
/// </summary>
public sealed record VelocityBar(string Label, double Value, double Lower, double Upper, string Text, bool Amber, string? Beneath = null);

/// <summary>One shot on the chart: the velocity read for it, and its height at the target in inches, up positive.</summary>
public sealed record VelocityDot(double Fps, double UpInches);

/// <summary>
/// "Velocity and the vertical", NOTES-FROM-PLANNING.md entry 323: what the desktop's block and the phone's card both draw, built once from
/// <see cref="VelocityVertical"/> so the two can never say different things, and every figure is the one <c>grouplab velocity</c> prints.
/// </summary>
/// <param name="State">Which approved state this is.</param>
/// <param name="Headline">"about 42%", or "the data cannot tell"; empty where there is no result.</param>
/// <param name="Interval">"18 to 87% at 90%", beside the headline, at the confidence the result states; null without a result.</param>
/// <param name="Sentence">The sentence under the headline, or the state's own sentence where there is no result.</param>
/// <param name="Share">The share and its interval, for the meter; null without a result.</param>
/// <param name="Bars">Measured vertical SD and the vertical SD from velocity alone, on one scale; empty without a result.</param>
/// <param name="Dots">Each shot matched to a reading by an accepted reconciliation; empty where none were, and then there is no chart.</param>
/// <param name="MeasuredSlope">The regression's slope, inches per ft/s, where there is a chart.</param>
/// <param name="SolverSlope">The solver's slope, inches per ft/s, where there is a chart.</param>
/// <param name="SlopeSentence">The sentence under the chart.</param>
/// <param name="SlopeDisagrees">True for state 5: the solver's slope lies outside the measured slope's interval.</param>
/// <param name="Why">The lines behind "why", ending with <see cref="WhyClosing"/>.</param>
/// <param name="Action">The button's words in states 3 and 4: "Add readings", "Set the distance", or "Set the load's BC".</param>
/// <param name="PredictedSdInches">The vertical SD from velocity alone, for the band on the plot.</param>
/// <param name="MeasuredSdInches">The measured vertical SD, for the band's dotted lines.</param>
public sealed record VelocityBlock(
    VelocityBlockState State,
    string Headline,
    string? Interval,
    string Sentence,
    IntervalFigure? Share,
    IReadOnlyList<VelocityBar> Bars,
    IReadOnlyList<VelocityDot> Dots,
    double? MeasuredSlope,
    double? SolverSlope,
    string? SlopeSentence,
    bool SlopeDisagrees,
    IReadOnlyList<string> Why,
    string? Action,
    double? PredictedSdInches,
    double? MeasuredSdInches)
{
    /// <summary>The block's heading on the desktop and the card's on the phone.</summary>
    public const string Heading = "Velocity and the vertical";

    /// <summary>The sentence under the headline in state 1, entry 323 section 1.3.</summary>
    public const string ShareSentence = "of this group's vertical comes from velocity alone; the rest is the rifle, the shooter and the wind.";

    /// <summary>The last line behind "why", entry 323 section 1.7.</summary>
    public const string WhyClosing = "The predicted figures come from the solver and the readings; only the measured vertical comes from the shots. The range errs wide, never narrow.";

    /// <summary>State 3's sentence, approved word for word.</summary>
    public const string NoReadingsSentence = "Add this group's chronograph readings to see how much of its vertical comes from velocity.";

    /// <summary>State 4's sentence, approved word for word.</summary>
    public const string NeedsSentence = "The solver needs the distance shot and the bullet's BC to work this out.";

    /// <summary>The chart's title, entry 323 section 1.6.</summary>
    public const string ChartTitle = "Height against velocity, shot by shot";

    /// <summary>The band's two labels on the plot, entry 323 section 2.</summary>
    public const string BandLabel = "velocity alone, 1 SD each way", MeasuredLabel = "measured, 1 SD each way";

    /// <summary>Whether there is a result to draw: the meter, the bars, and the band on the plot.</summary>
    public bool HasResult => State is VelocityBlockState.Result or VelocityBlockState.CannotTell;

    /// <summary>The band for the plot, where there is a result: one predicted and one measured vertical SD each way of the group center.</summary>
    public (double PredictedSdInches, double MeasuredSdInches)? Band =>
        HasResult && PredictedSdInches is { } p && MeasuredSdInches is { } m ? (p, m) : null;

    /// <summary>Everything the block says in words, in order, for a screen reader and the tests.</summary>
    public string Words => string.Join(" ", new[] { Headline, Interval, Sentence, string.Join(" ", Bars.Select(b => b.Label + " " + b.Text)), SlopeSentence, Action }
        .Where(s => !string.IsNullOrEmpty(s)));
}

/// <summary>
/// The conditions the solver flies the load in, NOTES-FROM-PLANNING.md entry 329: the air and the shooting angle as the person entered them,
/// and where they were entered, said under "why". Null where nothing was entered, and then the solver takes the standard day and says so.
/// </summary>
public sealed record VelocityConditions(AirInput Air, double AngleDegrees, string From)
{
    /// <summary>Whether the air is the standard day's own: 59 F, no pressure stated, sea level.</summary>
    public bool StandardAir => Air.TemperatureF == 59 && Air.PressureInHg is null && Air.AltitudeFt == 0;
}

/// <summary>Builds <see cref="VelocityBlock"/> from a marking, its records and the session's chronograph string.</summary>
public static class VelocityBlocks
{
    /// <summary>The confidence the block asks for; what it shows is always the confidence the result states.</summary>
    public const double Confidence = 0.90;

    /// <summary>
    /// The block for a marking, or null where the group has fewer than two counted shots and so no vertical to compare with anything.
    /// <paramref name="readings"/> is the session's newest chronograph string, never several pooled, since two strings are two measurements;
    /// <paramref name="pairs"/> the accepted pairing of that string's readings with shots, ordinals counting readings from 1 as the store does.
    /// <paramref name="conditions"/> is the air and angle the person entered, the session's own first and Ballistics' for that rifle and load
    /// next (entry 329); the rifle gives the sight height, zero, and twist, and the load the bullet's diameter and length.
    /// <paramref name="size"/> writes a length at the target by the Group block's unit rule, and <paramref name="beneath"/> the size on the paper
    /// beneath it where that rule writes an angle. The sentences give heights on the paper, as the entry's own sample sentence does: an angle
    /// to two places reads 0.00 for the few hundredths of an inch velocity moves a hole at 100 yd.
    /// </summary>
    public static VelocityBlock? Build(MarkingState state, Load? load, ChronographString? readings, IReadOnlyList<ShotVelocity> pairs, UnitSettings units, Func<double, string> size,
        double confidence = Confidence, Func<double, string?>? beneath = null, VelocityConditions? conditions = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(size);
        var counted = state.Shots.Where(s => s.IsShot && s.Exclusion is null && !GroupAnalysis.OnSighter(state, s)).ToList();
        var offsets = GroupAnalysis.CompositeOffsets(state, counted);
        if (offsets.Count < 2)
        {
            return null;
        }

        // The image's y runs down the sheet; the solver's height runs up.
        var up = counted.Select((s, i) => (s.Id, Up: -offsets[i].Y)).ToList();
        if (readings is null || readings.VelocitiesFps.Count < 2)
        {
            return Empty(VelocityBlockState.NoReadings, VelocityBlock.NoReadingsSentence, "Add readings");
        }

        if (state.ShotDistanceInches is not { } distanceInches || distanceInches <= 0)
        {
            return Empty(VelocityBlockState.NoDistance, VelocityBlock.NeedsSentence, "Set the distance");
        }

        if (load is not { BallisticCoefficient: > 0, DragModel: { } model })
        {
            return Empty(VelocityBlockState.NoBc, VelocityBlock.NeedsSentence, "Set the load's BC");
        }

        // The point-mass drop does not depend on the bullet's weight, which the BC already carries; the solver asks for one only for energy.
        var rifle = state.Rifle;
        var air = conditions?.Air ?? new AirInput();
        var input = new BallisticInput(load.BallisticCoefficient.Value, model, 1, load.BulletWeightGrains ?? 150, rifle?.SightHeightInches ?? 1.5,
            rifle?.ZeroDistanceYards ?? 100, air.TemperatureF, air.PressureInHg, air.AltitudeFt, air.HumidityPct, 0, conditions?.AngleDegrees ?? 0,
            load.BcReference ?? ReferenceAtmosphere.Icao, rifle?.TwistInches, rifle?.TwistDirection ?? 1, load.BulletDiameterInches, load.BulletLengthInches);
        double yards = distanceInches / 36;
        var byId = up.ToDictionary(p => p.Id, p => p.Up);
        var matched = pairs.Where(p => p.StringId == readings.Id && byId.ContainsKey(p.ShotId) && p.Ordinal >= 1 && p.Ordinal <= readings.VelocitiesFps.Count)
            .Select(p => (Fps: readings.VelocitiesFps[p.Ordinal - 1], VerticalInches: byId[p.ShotId])).ToList();
        VelocityVerticalResult? result;
        string? refusal;
        try
        {
            (result, refusal) = VelocityVertical.Analyze(input, yards, readings.VelocitiesFps, [.. up.Select(p => p.Up)], matched.Count > 0 ? matched : null, confidence);
        }
        catch (ArgumentException e)
        {
            (result, refusal) = (null, e.Message);
        }

        if (result is null)
        {
            return Empty(VelocityBlockState.Refused, refusal ?? "The solver gave no answer for these readings and shots.", null);
        }

        var inv = CultureInfo.InvariantCulture;
        string level = string.Create(inv, $"{result.Confidence * 100:0.#}%");
        var share = result.Share;
        string headline, interval, sentence;
        if (result.CannotTell)
        {
            headline = "the data cannot tell";
            interval = string.Create(inv, $"{VelocityVertical.Percent(share.Lower)} to all of it at {level}");
            sentence = string.Create(inv, $"Anywhere from {VelocityVertical.Percent(share.Lower)} to all of it, at {level}, from {result.Readings} readings and {result.Shots} shots. More readings and shots narrow it.");
        }
        else
        {
            headline = "about " + VelocityVertical.Percent(share.Value);
            interval = string.Create(inv, $"{Whole(share.Lower)} to {VelocityVertical.Percent(share.Upper)} at {level}");
            sentence = VelocityBlock.ShareSentence;
        }

        var bars = new List<VelocityBar>
        {
            new("Measured vertical SD", result.MeasuredVerticalSdInches.Value, result.MeasuredVerticalSdInches.Lower, result.MeasuredVerticalSdInches.Upper, size(result.MeasuredVerticalSdInches.Value), Amber: false, beneath?.Invoke(result.MeasuredVerticalSdInches.Value)),
            new("From velocity alone", result.PredictedVerticalSdInches.Value, result.PredictedVerticalSdInches.Lower, result.PredictedVerticalSdInches.Upper, size(result.PredictedVerticalSdInches.Value), Amber: true, beneath?.Invoke(result.PredictedVerticalSdInches.Value)),
        };

        // Metric readers think in steps of 3 m/s, near enough the 10 ft/s an imperial reader thinks in.
        bool metric = units.Distance == DistanceUnit.Metre;
        double step = metric ? 3 / 0.3048 : 10;
        string stepWords = metric ? "3 m/s" : "10 fps";
        string Paper(double inches) => units.Length(inches);
        string? slopeSentence = null;
        IReadOnlyList<VelocityDot> dots = [];
        if (result.Slope is { } slope)
        {
            dots = [.. matched.Select(m => new VelocityDot(m.Fps, m.VerticalInches))];
            double b = slope.SlopeInchesPerFps.Value, sign = b < 0 ? -1 : 1;
            double low = Math.Min(slope.SlopeInchesPerFps.Lower * sign, slope.SlopeInchesPerFps.Upper * sign) * step;
            double high = Math.Max(slope.SlopeInchesPerFps.Lower * sign, slope.SlopeInchesPerFps.Upper * sign) * step;
            double solver = slope.PredictedInchesPerFps * step;
            string solverWords = solver * sign >= 0 ? Paper(Math.Abs(solver)) : (solver >= 0 ? "a rise of " : "a fall of ") + Paper(Math.Abs(solver));
            slopeSentence = $"Shot by shot, each {stepWords} {(sign > 0 ? "raised" : "lowered")} the hole {Paper(Math.Abs(b) * step)} ({Signed(low, Paper)} to {Signed(high, Paper)}). "
                + (slope.PredictedInside
                    ? $"The solver says {solverWords}, inside that range."
                    : $"The solver says {solverWords}, outside that range: check the distance, the BC, or the order the readings were matched in.");
        }

        var why = new List<string>
        {
            $"Velocity: {result.Readings} readings, mean {units.Speed(result.MeanFps)}, SD {units.SpeedDifference(result.VelocitySdFps.Value)} ({units.SpeedDifference(result.VelocitySdFps.Lower)} to {units.SpeedDifference(result.VelocitySdFps.Upper)}).",
            $"The solver: {Paper(Math.Abs(result.DropPerFpsInches) * step)} of height per {stepWords} at {units.DistanceText(distanceInches)}, from the load {load.Name}, {model} BC {load.BallisticCoefficient.Value.ToString("0.000", inv)}.",
            $"Vertical SD from velocity alone: {Paper(result.PredictedVerticalSdInches.Value)} ({Paper(result.PredictedVerticalSdInches.Lower)} to {Paper(result.PredictedVerticalSdInches.Upper)}).",
            $"Measured vertical SD over the shots: {Paper(result.MeasuredVerticalSdInches.Value)} over {result.Shots} shots ({Paper(result.MeasuredVerticalSdInches.Lower)} to {Paper(result.MeasuredVerticalSdInches.Upper)}).",
            $"Every range is at {level}.",
        };
        why.Add(ConditionsLine(conditions));
        if (rifle?.SightHeightInches is not > 0 || rifle?.ZeroDistanceYards is not > 0)
        {
            string missing = (rifle?.SightHeightInches is not > 0, rifle?.ZeroDistanceYards is not > 0) switch
            {
                (true, true) => "Sight height 1.5 in and zero 100 yd assumed: the rifle has neither recorded.",
                (true, false) => "Sight height 1.5 in assumed: the rifle has none recorded.",
                _ => "Zero 100 yd assumed: the rifle has no zero distance recorded.",
            };
            why.Add(missing);
        }

        why.Add(VelocityBlock.WhyClosing);
        return new VelocityBlock(result.CannotTell ? VelocityBlockState.CannotTell : VelocityBlockState.Result, headline, interval, sentence, share, bars, dots,
            result.Slope?.SlopeInchesPerFps.Value, result.Slope?.PredictedInchesPerFps, slopeSentence, result.Slope is { PredictedInside: false }, why, null,
            result.PredictedVerticalSdInches.Value, result.MeasuredVerticalSdInches.Value);
    }

    /// <summary>The block's newest chronograph string of a session, and the pairs of it a person accepted.</summary>
    public static (ChronographString? Readings, IReadOnlyList<ShotVelocity> Pairs) Readings(SessionStore store, long session)
    {
        ArgumentNullException.ThrowIfNull(store);
        var newest = store.ChronographStrings(session).LastOrDefault();
        return (newest, newest is null ? [] : [.. store.ShotVelocities(session).Where(v => v.StringId == newest.Id)]);
    }

    /// <summary>
    /// The line under "why" saying which conditions the solver used and where they came from, or that it fell back to the standard day,
    /// entry 329 section 2.
    /// </summary>
    public static string ConditionsLine(VelocityConditions? conditions)
    {
        var inv = CultureInfo.InvariantCulture;
        if (conditions is null || conditions.StandardAir)
        {
            string angle = conditions is { AngleDegrees: not 0 } a ? string.Create(inv, $" Shot at {a.AngleDegrees:0.#} degrees, from {a.From}.") : "";
            return "Standard day assumed: no temperature or altitude entered." + angle;
        }

        var air = conditions.Air;
        string where = air.PressureInHg is { } p ? string.Create(inv, $"{p:0.00} inHg") : string.Create(inv, $"{air.AltitudeFt:0} ft of altitude");
        string angleWords = conditions.AngleDegrees == 0 ? "level" : string.Create(inv, $"at {conditions.AngleDegrees:0.#} degrees");
        return string.Create(inv, $"Conditions: {air.TemperatureF:0.#} F, {where}, {air.HumidityPct:0} percent humidity, shot {angleWords}, from {conditions.From}.");
    }

    private static VelocityBlock Empty(VelocityBlockState state, string sentence, string? action) =>
        new(state, "", null, sentence, null, [], [], null, null, null, false, [], action, null, null);

    /// <summary>A share's lower end, without its percent sign, as "18 to 87%" writes it.</summary>
    private static string Whole(double share) => VelocityVertical.Percent(share).TrimEnd('%');

    private static string Signed(double inches, Func<double, string> size) => inches < 0 ? "-" + size(-inches) : size(inches);
}
