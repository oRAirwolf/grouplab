using System.Globalization;
using GroupLab.Core.Marking;

namespace GroupLab.Core.Ballistics;

/// <summary>A group's precision for the hit chance: the precision, the zero's own error, the shots, and the sigma's range, in mrad.</summary>
public sealed record GroupPrecision(HitPrecision Precision, double ZeroMrad, int Shots, double DistanceInches, double? LowerMrad, double? UpperMrad);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 258: the hit chance's precision from a marked group, moved out of the desktop's Hit probability view so the
/// phone's Hit chance uses the same numbers. The group without its excluded shots where it has some; sigma per axis as an angle at the shot
/// distance, with 2 (n - 1) degrees of freedom; and the zero's error, sigma over the root of the shots.
/// </summary>
public static class HitFromGroup
{
    /// <summary>The group's precision, or null and why not.</summary>
    public static (GroupPrecision? Group, string? Refusal) Of(MarkingState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var figures = GroupAnalysis.Analyse(state);
        var group = figures.Excluded > 0 ? figures.WithoutExclusions : figures.AllShots;
        if (state.ShotDistanceInches is not { } distance)
        {
            return (null, "The group open in the analysis has no shot distance; set it in the marking.");
        }

        if (group?.Sigma is not { } sigma)
        {
            return (null, "The analysis has no group with a sigma; mark at least " + GroupAnalysis.MinimumShotsForDispersion.ToString(CultureInfo.InvariantCulture) + " shots and accept them.");
        }

        double yards = distance / 36, mrad = HitPrecision.MradFromInches(sigma.Value, yards);
        return (new GroupPrecision(new HitPrecision(mrad, 2.0 * (group.Shots - 1), yards), mrad / Math.Sqrt(group.Shots), group.Shots, distance,
            sigma.Lower is { } lo ? HitPrecision.MradFromInches(lo, yards) : null, sigma.Upper is { } hi ? HitPrecision.MradFromInches(hi, yards) : null), null);
    }
}
