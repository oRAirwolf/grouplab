using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 424, Alan's choice: what the zero correction says when one axis is inside the scatter of the group's centre.
/// </summary>
public enum InsideScatter
{
    /// <summary>The default: the axis is named with "leave it", and why.</summary>
    LeaveIt,

    /// <summary>Both axes are rounded to whole clicks, the uncertain one marked as within the scatter.</summary>
    ClicksAnyway,
}

/// <summary>The zero correction in words, the same on the computer and the phone: the line to act on, each axis's own words, and the why.</summary>
/// <param name="Verdict">The line under the zero correction: what to dial, and what to leave and why.</param>
/// <param name="Dial">Whether anything is to be dialled.</param>
/// <param name="Axes">Each axis's own line, elevation then windage, for a screen that shows them one under the other.</param>
/// <param name="Why">The reasons, in order: why an axis was left out or given anyway, how well the centre is known, rounding, the method.</param>
/// <param name="Head">The verdict's first sentence: what to dial, or that nothing is; question 91's report joins its distance and rounding to it.</param>
/// <param name="Rest">The rest of the verdict: what to leave and why, or which click is within the scatter; empty where there is none.</param>
public sealed record ZeroSaid(string Verdict, bool Dial, IReadOnlyList<string> Axes, IReadOnlyList<string> Why, string Head, string Rest);

/// <summary>
/// Entry 424, Alan on nightly 192 with a pooled set of 20 shots: the correction said "Dial 1 click left, at 0.1 mil a click." and nothing at
/// all about 0.08 mil low, which he read as a rounding mistake. It was entry 53 section 3's rule, an axis inside what the shots can tell apart
/// left undialled, applied without a word. This says it, and with <see cref="InsideScatter.ClicksAnyway"/> gives the click anyway, marked.
/// </summary>
public static class ZeroWords
{
    public const string SettingLabel = "When an axis is inside the scatter";

    public const string LeaveItChoice = "Say leave it, and why";

    public const string AnywayChoice = "Give the clicks anyway";

    public const string SettingSays = "The zero correction leaves an axis alone when the group's center is not far enough off on it to tell from the scatter at this many shots. Give the clicks anyway rounds both axes to whole clicks and marks the uncertain one.";

    /// <summary>Where Shots Needed to Zero is pointed to from the line.</summary>
    public const string MoreShots = "Shots Needed to Zero works out how many shots would settle it.";

    private static string Name(ZeroCorrection zero, ZeroAxis axis) => ReferenceEquals(axis, zero.Elevation) ? "Elevation" : "Windage";

    /// <summary>The direction a click goes as a person says it: "up", "left".</summary>
    private static string Word(ZeroAxis axis) => axis.Dial;

    /// <summary>
    /// The words for a correction. <paramref name="units"/> are the page's units; the angle is given in the rifle's scope unit where the
    /// marking names one, which is the unit its click is in.
    /// </summary>
    public static ZeroSaid For(ZeroCorrection zero, MarkingState state, UnitSettings units, InsideScatter rule = InsideScatter.LeaveIt)
    {
        ArgumentNullException.ThrowIfNull(zero);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(units);
        var aim = units.Aiming(state.Rifle);
        double? distance = state.ShotDistanceInches;
        string Angle(double inches) => aim.AngleText(Math.Abs(inches), distance) ?? units.Length(Math.Abs(inches));
        string Both(double inches) => aim.AngleText(Math.Abs(inches), distance) is { } angle
            ? $"{units.Length(Math.Abs(inches))} ({angle})"
            : units.Length(Math.Abs(inches));
        // An axis that shows as nothing at all is on the aim, which is said so rather than as "0.00 mil low".
        bool OnAim(ZeroAxis axis) => Angle(axis.OffsetInches).TrimStart('0', '.').StartsWith(' ');
        string Within(ZeroAxis axis) => OnAim(axis)
            ? string.Create(CultureInfo.InvariantCulture, $"it is on the aim, as near as these {zero.Shots} shots can tell (about {Angle(axis.HalfWidthInches)} either way)")
            : string.Create(CultureInfo.InvariantCulture, $"{Angle(axis.OffsetInches)} {axis.Sits} is within what these {zero.Shots} shots can tell apart (about {Angle(axis.HalfWidthInches)} either way)");

        var axes = new[] { zero.Windage, zero.Elevation };
        var rifle = state.Rifle;
        bool clicksKnown = axes.All(a => a.Rounded is not null) && rifle is not null;
        var why = new List<string>();
        var lines = new List<string>();
        string verdict;
        string rest = "";
        bool dial;

        if (rule == InsideScatter.ClicksAnyway && clicksKnown)
        {
            // Entry 424 section 2: both axes rounded to whole clicks; one that rounds to none says "no click", either way.
            var turned = axes.Where(a => a.Rounded!.Count > 0).ToList();
            var none = axes.Where(a => a.Rounded!.Count == 0).ToList();
            var unsure = turned.Where(a => !a.Distinguishable).ToList();
            dial = turned.Count > 0;
            verdict = turned.Count > 0
                ? $"Dial {string.Join(" and ", turned.Select(a => a.Rounded!.Describe()))}, at {rifle!.DescribeClick()}."
                : $"No click either way, at {rifle!.DescribeClick()}.";
            if (turned.Count > 0 && none.Count > 0)
            {
                rest += " " + string.Join(" ", none.Select(a => $"{Name(zero, a)}: no click."));
            }

            if (unsure.Count > 0)
            {
                rest += unsure.Count == 2
                    ? " Both clicks are within the scatter; more shots would tell whether they are needed."
                    : $" The {Word(unsure[0])} click is within the scatter; more shots would tell whether it is needed.";
                why.AddRange(unsure.Select(a => $"{Name(zero, a)} was given its click anyway, as Settings asks, though {Within(a)}."));
            }

            foreach (var axis in new[] { zero.Elevation, zero.Windage })
            {
                lines.Add(axis.Rounded!.Count == 0 ? "no click" : axis.Rounded.Describe() + (axis.Distinguishable ? "" : ", within the scatter"));
            }
        }
        else
        {
            var dialled = axes.Where(a => a.Distinguishable).ToList();
            var left = axes.Where(a => !a.Distinguishable).ToList();
            dial = dialled.Count > 0;
            if (dialled.Count > 0)
            {
                verdict = clicksKnown
                    ? $"Dial {string.Join(" and ", dialled.Select(a => a.Rounded!.Describe()))}, at {rifle!.DescribeClick()}."
                    : $"Dial {string.Join(" and ", dialled.Select(a => a.Dial))} by the figures above.";
            }
            else
            {
                int? settle = zero.Windage.ShotsToSettle is { } w && zero.Elevation.ShotsToSettle is { } e ? Math.Min(w, e) : zero.Windage.ShotsToSettle ?? zero.Elevation.ShotsToSettle;
                verdict = string.Create(CultureInfo.InvariantCulture, $"Not distinguishable from zero at {zero.Shots} shots.")
                    + (settle is { } more ? string.Create(CultureInfo.InvariantCulture, $" About {more} shots would settle it.") : " Nothing this rifle can shoot would settle an offset this small.");
            }

            // Entry 424 section 1: an axis left alone is named, with why, rather than dropped without a word.
            foreach (var axis in left)
            {
                rest += $" {Name(zero, axis)}: leave it. {Upper(Within(axis))}.";
                why.Add($"{Name(zero, axis)} got no click: {Within(axis)}, so a click there could as easily be chasing the scatter as the zero.");
            }

            if (dialled.Count > 0 && left.Any(a => a.ShotsToSettle is not null))
            {
                rest += " " + MoreShots;
            }

            foreach (var axis in new[] { zero.Elevation, zero.Windage })
            {
                lines.Add(axis.Distinguishable
                    ? axis.Rounded is { } c && rifle is not null ? axis.Dial + ", " + c.Describe() : axis.Dial
                    : "leave it: " + Within(axis));
            }
        }

        why.Add($"The group's center is known to within {Both(zero.Windage.HalfWidthInches)} across and {Both(zero.Elevation.HalfWidthInches)} up and down, at {100 * Zeroing.Level:0} percent.");
        if (clicksKnown)
        {
            foreach (var axis in axes.Where(a => a.Rounded!.Count > 0 && (a.Distinguishable || rule == InsideScatter.ClicksAnyway)))
            {
                why.Add(string.Create(CultureInfo.InvariantCulture,
                    $"Rounding {axis.Dial} to whole clicks leaves {Math.Abs(axis.Rounded!.ResidualAngle):0.00} {UnitSettings.Symbol(axis.Rounded.Unit)}."));
            }
        }

        why.Add(Method(zero));
        return new ZeroSaid(verdict + rest, dial, lines, why, verdict, rest.TrimStart());
    }

    /// <summary>How the give or take was worked out, in a shooter's words.</summary>
    public static string Method(ZeroCorrection zero)
    {
        ArgumentNullException.ThrowIfNull(zero);
        if (zero.Circular)
        {
            return string.Create(CultureInfo.InvariantCulture, $"Worked out from both axes together from {zero.Shots} shots, because the group is about as wide as it is tall.");
        }

        string shape = zero.Elevation.HalfWidthInches >= zero.Windage.HalfWidthInches ? "taller than it is wide" : "wider than it is tall";
        return string.Create(CultureInfo.InvariantCulture, $"Worked out on each axis separately from {zero.Shots} shots, because the group is {shape}.");
    }

    private static string Upper(string words) => words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
}
