using System.Collections.Immutable;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>A shot as the automatic path found it: its position in image pixels and how stage S9 assigned it (NOTES-FROM-PLANNING.md entry 70 section 4).</summary>
public sealed record DetectedShot(PointD Image, AssignedShot Assignment);

/// <summary>
/// A candidate the hole detector refused: where it was in image pixels, how large it read, and why. The concept's review queue shows two
/// kinds, a candidate below the size gate and a marker caption rejected on stroke width, so they reach the marking rather than only the
/// trace (NOTES-FROM-PLANNING.md entry 70 section 4).
/// </summary>
public sealed record RejectedCandidate(PointD Image, double DiameterInches, string Reason);

/// <summary>
/// One shot's assignment as a person reviews it, in inches: the bull it holds and its distance there, its nearest bull and that distance,
/// the margin between its nearest and second-nearest bulls, and whether it wants a look, because the margin is small or the matching gave
/// it a bull other than its nearest. Keyed by the marking's shot id, not by a position in a list.
/// </summary>
public sealed record ShotAssignmentDetail(int ShotId, int? Bull, double DistanceInches, int NearestBull, double NearestInches, double MarginInches, bool Ambiguous);

/// <summary>
/// What the matching decided, carried into the marking so the editor shows it rather than rebuilding it from the trace (NOTES-FROM-PLANNING.md
/// entry 70 section 4): the method and its one-line reason, each detected shot's figures, and the candidates detection refused.
/// <para>
/// <see cref="Reason"/> is a note about the method, such as "as many shots as bulls". The contested card's sentence is not stored: it is
/// composed where it is shown, from one shot's figures and the other shots' bulls.
/// </para>
/// </summary>
public sealed record AssignmentReview(AssignmentMethod Method, string Reason, ImmutableList<ShotAssignmentDetail> Shots, ImmutableList<RejectedCandidate> Rejected)
{
    /// <summary>The figures for one shot, or null for a shot the matching did not place, such as one placed by hand.</summary>
    public ShotAssignmentDetail? For(int shotId) => Shots.FirstOrDefault(s => s.ShotId == shotId);

    /// <summary>How many shots want a person's look: the concept's "2 of 26 need review".</summary>
    public int NeedingReview => Shots.Count(s => s.Ambiguous);

    /// <summary>A matched shot's figures, from the page dmm <see cref="ShotAssignment"/> works in to inches.</summary>
    internal static ShotAssignmentDetail Detail(int shotId, AssignedShot shot) =>
        new(shotId, shot.Bull, shot.Distance / DmmPerInch, shot.NearestBull, shot.NearestDistance / DmmPerInch, shot.Margin / DmmPerInch, shot.Ambiguous);

    private const double DmmPerInch = 254;
}
