using System.Collections.Immutable;
using GroupLab.Core.Detection;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// A shot as the automatic path found it: its position in image pixels, how stage S9 assigned it (NOTES-FROM-PLANNING.md entry 70 section
/// 4), and the diameter the detector measured, which the canvas draws (entry 76 section 4).
/// </summary>
public sealed record DetectedShot(PointD Image, AssignedShot Assignment, double? DiameterInches = null, DetectedOversize? Oversize = null);

/// <summary>
/// A detection flagged as oversized, NOTES-FROM-PLANNING.md entry 82 section 6: about how many single holes' area it holds, and whether the
/// size it was judged against came from too few marks to trust. It reaches the marking so the person sees it, not only the analysis.
/// </summary>
public sealed record DetectedOversize(double Holes, bool Tentative)
{
    /// <summary>The sentence for a shot, in plain words and without naming one cause.</summary>
    public string Describe(string shot) => Tentative
        ? string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"Shot {shot} may be two holes: it covers about {Holes:0.0} holes' area, judged from too few marks to be sure. Name the calibre to check it.")
        : string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"Shot {shot} covers about {Holes:0.0} holes' area: two shots through one hole, or a hole joined to ink, would each read this way. Look at it, and add the second shot if there is one.");
}

/// <summary>
/// A candidate the hole detector refused: where it was in image pixels, how large it read, and why. The concept's review queue shows two
/// kinds, a candidate below the size gate and a marker caption rejected on stroke width, so they reach the marking rather than only the
/// trace (NOTES-FROM-PLANNING.md entry 70 section 4).
/// </summary>
public sealed record RejectedCandidate(PointD Image, double DiameterInches, string Reason);

/// <summary>
/// One shot's assignment as a person reviews it, in inches: the bull it holds and its distance there, its nearest bull and that distance,
/// the margin between its nearest and second-nearest bulls, and whether it wants a look, because the margin is small or the matching gave
/// it a bull other than its nearest. Keyed by the marking's shot id, not by a position in a list. <see cref="DetectedBull"/> is the bull
/// detection first gave it, so a shot the matching has since moved can be shown as moved for as long as it stays moved (entry 70 section 3).
/// </summary>
public sealed record ShotAssignmentDetail(int ShotId, int? Bull, double DistanceInches, int NearestBull, double NearestInches, double MarginInches, bool Ambiguous, int? DetectedBull);

/// <summary>
/// What the matching decided, carried into the marking so the editor shows it rather than rebuilding it from the trace (NOTES-FROM-PLANNING.md
/// entry 70 section 4): the method and its one-line reason, each detected shot's figures, and the candidates detection refused.
/// <para>
/// <see cref="Reason"/> is a note about the method, such as "as many shots as bulls". The contested card's sentence is not stored: it is
/// composed where it is shown, from one shot's figures and the other shots' bulls.
/// </para>
/// </summary>
public sealed record AssignmentReview(AssignmentMethod Method, string Reason, ImmutableList<ShotAssignmentDetail> Shots, ImmutableList<RejectedCandidate> Rejected, AssignmentMethod DetectedMethod)
{
    /// <summary>
    /// The shots the matching has moved off the bull they first had, the one detection gave them or, for a shot placed by hand, the one it
    /// was placed with, because an edit elsewhere took that bull or freed another (entry 70 section 3 item 3). Resolving one contested case
    /// can cause a second, and the second must never be invisible.
    /// </summary>
    public IEnumerable<ShotAssignmentDetail> Moved => Shots.Where(s => s.Bull != s.DetectedBull);

    /// <summary>
    /// Whether the method is no longer the one detection used: an edit took the unplaced shots above the bulls left to them, so matching
    /// stopped being forced, or back below. A method that changes without saying so is a confident wrong answer (entry 70 section 3 item 4).
    /// </summary>
    public bool MethodChanged => Method != DetectedMethod;

    /// <summary>The figures for one shot, or null for a shot the matching did not place, such as one whose bull a person chose.</summary>
    public ShotAssignmentDetail? For(int shotId) => Shots.FirstOrDefault(s => s.ShotId == shotId);

    /// <summary>How many shots want a person's look: the concept's "2 of 26 need review".</summary>
    public int NeedingReview => Shots.Count(s => s.Ambiguous);

    /// <summary>A matched shot's figures, from the page dmm <see cref="ShotAssignment"/> works in to inches.</summary>
    internal static ShotAssignmentDetail Detail(int shotId, AssignedShot shot, int? detectedBull) =>
        new(shotId, shot.Bull, shot.Distance / DmmPerInch, shot.NearestBull, shot.NearestDistance / DmmPerInch, shot.Margin / DmmPerInch, shot.Ambiguous, detectedBull);

    private const double DmmPerInch = 254;
}
