namespace GroupLab.Core.Detection;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 2b.4: a hole that landed off the bull grid is still a hole.
/// <para>
/// Scan 5 had two of them, above bull 2 near the top edge and left of bull 12, and neither was detected. They were not refused for being the
/// wrong size or shape: they were refused for being in the wrong place, by a rule that throws away any mark centred outside every bull's
/// cell.
/// </para>
/// <para>
/// <b>That rule is not a mistake, which is what makes this delicate.</b> It is written down in the detector as "the position prior that would
/// have removed every false positive the survey's baselines made". Removing it outright would trade two missed holes for an unknown number of
/// invented ones, and an invented hole is worse: a missing shot is visible to the shooter who fired it, and a false one is not.
/// </para>
/// <para>
/// So the prior is narrowed rather than dropped. A mark that is near the grid, within a bull's width of it, is a shot that missed its bull,
/// and it is kept and offered for assignment. A mark far out in the margins is still refused, because that is where the survey's false
/// positives were and no shot aimed at the grid lands there. What is kept is marked as outside the grid, left unassigned, and put in front of
/// the shooter rather than quietly counted.
/// </para>
/// </summary>
public static class OutsideTheGrid
{
    /// <summary>
    /// How far beyond a bull's cell a mark may be and still be treated as a shot that missed, as a multiple of the cell's own half width.
    /// One cell's width: a shot that missed its bull by more than a whole bull is a shot at something else.
    /// </summary>
    public const double NearTheGrid = 2.0;

    /// <summary>What a mark outside every cell is: near enough to be a missed shot, or out in the margins.</summary>
    /// <param name="pageX">The mark's centre on the page, dmm.</param>
    /// <param name="pageY">The mark's centre on the page, dmm.</param>
    /// <param name="cells">Every bull's cell, as centre and half extents in page dmm.</param>
    public static bool NearEnoughToBeAMissedShot(
        double pageX, double pageY, IReadOnlyList<(double X, double Y, double HalfWidth, double HalfHeight)> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        foreach (var cell in cells)
        {
            double acrossOver = Math.Abs(pageX - cell.X) - cell.HalfWidth;
            double downOver = Math.Abs(pageY - cell.Y) - cell.HalfHeight;

            // Inside the cell entirely: not this function's business, and the caller will have taken it already.
            if (acrossOver <= 0 && downOver <= 0)
            {
                return true;
            }

            if (acrossOver <= cell.HalfWidth * NearTheGrid && downOver <= cell.HalfHeight * NearTheGrid)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>What the review queue says about one, which is the point: it is shown rather than counted quietly.</summary>
    public const string Says =
        "This shot landed off the bulls. It is counted and kept, with no bull of its own until you give it one. If it is not a shot, mark it "
        + "as not a shot and the figures will leave it out.";

    /// <summary>What a mark too far out is refused with, naming the rule rather than the symptom.</summary>
    public const string TooFarOut = "off the sheet's bull grid by more than a bull's width";
}
