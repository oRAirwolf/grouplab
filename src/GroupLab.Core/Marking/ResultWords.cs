using System.Globalization;
using GroupLab.Core.Statistics;

namespace GroupLab.Core.Marking;

/// <summary>What Zero from this group says, in order: each axis, the scope, how well the centre is known, and the verdict; or why there is none.</summary>
public sealed record ZeroFromWords(IReadOnlyList<string> Axes, string Scope, string HowWell, string Verdict, string? Refusal);

/// <summary>
/// A group's offset from its aim point as the zero offset Ballistics carries at every range, NOTES-FROM-PLANNING.md entry 280 section 2 (board
/// ZeroFrom): the angle to dial up (negative down) and left (negative right), in MOA, and the sentence that says it was carried in.
/// </summary>
public sealed record ZeroOffset(double UpMoa, double LeftMoa, int Shots, bool Worth, string Words)
{
    /// <summary>The offset's elevation at <paramref name="rangeInches"/>, in inches at that range, up positive.</summary>
    public double UpInchesAt(double rangeInches) => Math.Tan(UpMoa * 2 / Angular.Constant(AngularUnit.Moa)) * rangeInches;
}

/// <summary>
/// The words of the row 10 screens of the phone parity canvas, NOTES-FROM-PLANNING.md entry 280 section 2, shared by the phone's pages and the
/// desktop's windows so the two cannot say different things: Shots A's summary and rows, Zero from this group, an aim point's own figures,
/// and the zero offset handed to Ballistics. The numbers are <see cref="GroupAnalysis"/>'s, <see cref="ShotOffsets"/>' and
/// <see cref="Zeroing"/>'s; nothing here computes a figure of its own.
/// </summary>
public static class ResultWords
{
    /// <summary>Shots A's summary: the counted shots, their mean radius, and that the shooter left some out; null where nothing is counted.</summary>
    public static string? ShotsSummary(GroupReport report, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(units);
        if (report.Counted is not { } counted)
        {
            return null;
        }

        string left = report.Excluded == 0 ? "" : string.Create(CultureInfo.InvariantCulture, $"; {report.Excluded} left out by the shooter, still on the record");
        string radius = counted.MeanRadius is { } mr ? ", mean radius " + units.Length(mr.Value) : "";
        return string.Create(CultureInfo.InvariantCulture, $"{counted.Shots} shots counted{radius}{left}.");
    }

    /// <summary>The line above Shots A's table: what the columns are, and the click value where there is one.</summary>
    public static string ShotsIntro(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Rifle is null || state.ShotDistanceInches is null
            ? "Across and up from the aim point. With a rifle's click value and the distance, each shot also shows its clicks."
            : $"Across and up from the aim point, and the clicks that would bring each shot onto it, at {state.Rifle.DescribeClick()}.";
    }

    /// <summary>What Shots A says where there is no aim point to measure from.</summary>
    public const string NoOffsets = "These shots have no aim point to measure from, so there are no offsets to show.";

    /// <summary>A row's offset in words: "0.12 in right, 0.30 in down".</summary>
    public static string Offset(ShotOffsetRow row, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(units);
        return units.Length(Math.Abs(row.AcrossInches)) + " " + (row.AcrossInches >= 0 ? "right" : "left") + ", "
            + units.Length(Math.Abs(row.UpInches)) + " " + (row.UpInches >= 0 ? "up" : "down");
    }

    /// <summary>A row's clicks, "3 clicks left, 5 clicks down", or empty where there is no click value.</summary>
    public static string Clicks(ShotOffsetRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return string.Join(", ", new[] { row.AcrossClicks?.Describe(), row.UpClicks?.Describe() }.Where(c => c is not null));
    }

    /// <summary>
    /// Zero from this group, board ZeroFrom: the group's centre from the aim point, the clicks with the scope named, how well the centre is
    /// known at this many shots (entry 53 section 3's rule: dial an axis only where its interval excludes zero), and the verdict.
    /// </summary>
    public static ZeroFromWords ZeroFrom(MarkingState state, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        if (Zeroing.For(state) is not { } zero)
        {
            return new ZeroFromWords([], "", "", "", "This group has no aim point to measure a zero from, or too few shots for one.");
        }

        string Axis(ZeroAxis axis, string name)
        {
            string amount = units.Length(Math.Abs(axis.OffsetInches)) + " " + axis.Sits;
            string dial = axis.Clicks is { } c ? c.Describe() : "dial " + axis.Dial;
            return axis.Distinguishable
                ? $"{name}: the group sits {amount}; {dial}."
                : $"{name}: the group sits {amount}, too little to dial at {zero.Shots} shots"
                  + (axis.ShotsToSettle is { } n ? string.Create(CultureInfo.InvariantCulture, $"; about {n} shots would settle it.") : ".");
        }

        return new ZeroFromWords(
            [Axis(zero.Elevation, "Up and down"), Axis(zero.Windage, "Across")],
            state.Rifle is { } rifle ? $"Clicks at {rifle.DescribeClick()}, the click value of {rifle.Name}." : "Choose a rifle with its click value to see clicks.",
            string.Create(CultureInfo.InvariantCulture,
                $"How well the center is known: the smallest offset these {zero.Shots} shots can call is {units.Length(zero.DetectableInches)}; an axis is worth dialing only where the group sits further off than that."),
            zero.Worth ? "Worth dialing." : "Neither axis is far enough off to be worth dialing at this many shots.",
            null);
    }

    /// <summary>
    /// The group's offset as the zero offset Ballistics carries, "Use as the zero offset in Ballistics": the angle the centre sits from the
    /// aim at the distance shot, to dial the other way at every range; null without a distance or a zero.
    /// </summary>
    public static ZeroOffset? ZeroOffsetFor(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.ShotDistanceInches is not { } shotAt || shotAt <= 0 || Zeroing.For(state) is not { } zero)
        {
            return null;
        }

        static double Moa(double inches, double at) => Angular.Constant(AngularUnit.Moa) / 2 * Math.Atan(Math.Abs(inches) / at);
        double up = zero.Elevation.Dial == "up" ? Moa(zero.Elevation.OffsetInches, shotAt) : -Moa(zero.Elevation.OffsetInches, shotAt);
        double left = zero.Windage.Dial == "left" ? Moa(zero.Windage.OffsetInches, shotAt) : -Moa(zero.Windage.OffsetInches, shotAt);
        string words = string.Create(CultureInfo.InvariantCulture,
            $"With the zero offset of the group you carried in, {Math.Abs(up):0.00} MOA {(up >= 0 ? "up" : "down")} and {Math.Abs(left):0.00} MOA {(left >= 0 ? "left" : "right")} at every range, from {zero.Shots} shots{(zero.Worth ? "" : "; at this many shots it is not yet worth dialing")}.");
        return new ZeroOffset(up, left, zero.Shots, zero.Worth, words);
    }

    /// <summary>One aim point's own figures, board MultiAim: "Aim B: 5 shots, mean radius 0.41 in, extreme spread 1.20 in, center 0.30 in from its aim."</summary>
    public static string AimPoint(BullAim aim, GroupFigures? own, UnitSettings units)
    {
        ArgumentNullException.ThrowIfNull(aim);
        ArgumentNullException.ThrowIfNull(units);
        return own is null
            ? $"Aim {aim.Label}: no shots yet."
            : string.Create(CultureInfo.InvariantCulture, $"Aim {aim.Label}: {own.Shots} shots")
              + (own.MeanRadius is { } mr ? ", mean radius " + units.Length(mr.Value) : "")
              + (own.ExtremeSpread is { } es ? ", extreme spread " + units.Length(es.Value) : "")
              + (own.CentreFromAim is { } c ? ", center " + units.Length(Math.Sqrt((c.X * c.X) + (c.Y * c.Y))) + " from its aim" : "") + ".";
    }

    /// <summary>The note under the aim points: the figures above are pooled.</summary>
    public const string AimPointsPooled = "The figures above pool every aim point's shots, each measured from its own aim point.";

    /// <summary>Whether the aim points were placed by hand on a target GroupLab did not print, where each has a color and its own figures.</summary>
    public static bool AimedByHand(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Bulls.Count > 0 && state.Scale is not SheetReference && state.Bulls.All(b => b.Declared is null);
    }
}
