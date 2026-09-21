using System.Globalization;

namespace GroupLab.Core.Marking;

/// <summary>One mark on the scale: where it sits, and whose rule of thumb it is.</summary>
/// <param name="InchesPer100">Mean radius per 100 yards, in inches.</param>
/// <param name="Says">What that source called it.</param>
public sealed record ScaleMark(double InchesPer100, string Says);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 131 section 6.1: the group's mean radius per 100 yards on a scale, with reference marks.
/// <para>
/// <b>The marks are somebody else's rules of thumb and are labelled as such.</b> Alan's reference is what a Hornady podcast has said about
/// mean radius per 100 yards. GroupLab does not endorse the numbers, has not measured them, and must not present them as its own verdict:
/// this project's whole argument is that a handful of shots does not support a verdict, and it would be absurd to then hand one down using
/// numbers off a podcast.
/// </para>
/// <para>
/// So the scale shows where the group sits, attributes the marks, shows the shot count beside them, and says plainly that with few shots the
/// interval is wide and the comparison weak. A person can then use the scale for what it is good for, which is a sense of scale.
/// </para>
/// </summary>
public static class MeanRadiusScale
{
    /// <summary>Who the marks come from, said wherever they are shown.</summary>
    public const string Attribution = "Rules of thumb quoted on a Hornady podcast, not GroupLab's own measurements.";

    /// <summary>
    /// The marks, in inches of mean radius per 100 yards, as that source gave them.
    /// </summary>
    public static IReadOnlyList<ScaleMark> Marks { get; } =
    [
        new(0.300, "pretty good"),
        new(0.200, "solid"),
        new(0.175, "really, really good"),
    ];

    /// <summary>The widest the scale runs, so a poor group still has somewhere to sit rather than falling off the end.</summary>
    public const double WidestInchesPer100 = 0.60;

    /// <summary>
    /// A mean radius measured at one distance, expressed per 100 yards so groups shot at different distances can be put on one scale.
    /// </summary>
    public static double PerHundredYards(double meanRadiusInches, double distanceYards) =>
        distanceYards <= 0 ? double.NaN : meanRadiusInches * 100.0 / distanceYards;

    /// <summary>
    /// Where a value sits along the scale, from 0 at the best end to 1 at the widest. Used to draw the bar, never to judge anything.
    /// </summary>
    public static double Along(double inchesPer100) =>
        double.IsNaN(inchesPer100) ? double.NaN : Math.Clamp(inchesPer100 / WidestInchesPer100, 0, 1);

    /// <summary>
    /// What the scale says beside the bar, including the shot count and what it does to the comparison.
    /// <para>
    /// The caveat is not optional and its wording is deliberate. Twenty to thirty shots is what that same source attached to its best
    /// figure, so a five shot group sitting on that mark has not met the condition the number came with, and saying so is the difference
    /// between a scale and a scoreboard.
    /// </para>
    /// </summary>
    public static string Caveat(int shots)
    {
        var inv = CultureInfo.InvariantCulture;
        if (shots >= 20)
        {
            return string.Create(inv, $"From {shots} shots, which is enough for this comparison to mean much. {Attribution}");
        }

        if (shots >= 10)
        {
            return string.Create(inv,
                $"From {shots} shots. The interval is still wide, so where this sits on the scale could move with another group. {Attribution}");
        }

        return string.Create(inv,
            $"From {shots} shots. That is too few for this comparison to carry weight: the interval is wide, and the same rifle could land "
            + $"anywhere across several of these marks. The marks were quoted for groups of twenty to thirty. {Attribution}");
    }

    /// <summary>
    /// Whether the interval is wide enough that the group overlaps more than one mark, which is the plainest way of saying a comparison is
    /// not settled: if the group could honestly be called two different things, it is not either of them yet.
    /// </summary>
    public static bool StraddlesAMark(double lowerPer100, double upperPer100)
    {
        if (double.IsNaN(lowerPer100) || double.IsNaN(upperPer100))
        {
            return false;
        }

        return Marks.Any(m => m.InchesPer100 > Math.Min(lowerPer100, upperPer100) && m.InchesPer100 < Math.Max(lowerPer100, upperPer100));
    }
}
