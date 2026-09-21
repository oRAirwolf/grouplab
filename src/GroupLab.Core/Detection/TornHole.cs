namespace GroupLab.Core.Detection;

/// <summary>
/// A hole the paper tore rather than punched, NOTES-FROM-PLANNING.md entry 130 section 2b.1.
/// <para>
/// <b>This is the defect that started the whole "never silent" rule.</b> Alan fired fifteen at scan 1 and GroupLab found fourteen, with an
/// empty review queue and no prompt of any kind. Reading the detector's own rejections at that bull finally showed why: the hole was seen,
/// measured at 0.236 in across, and refused for <b>hull solidity 0.54 against a floor of 0.55</b>. It missed by one hundredth.
/// </para>
/// <para>
/// <b>Lowering the floor is not the fix.</b> On that same sheet, marks at solidity 0.53, 0.50, 0.48, 0.47 and 0.41 are handwriting and paper
/// damage up in the header, and a floor low enough to admit the hole admits several of them. Solidity alone cannot tell a torn hole from a
/// pen stroke, because both are ragged.
/// </para>
/// <para>
/// <b>What tells them apart is everything else about the mark.</b> The hole at bull 2 is the size a .224 bullet makes in paper and it sits
/// 0.101 in from the middle of a bull. The ragged marks are the wrong size, or they are half an inch to two inches away and outside any
/// bull's cell. So a mark is rescued from the compactness gate only when it is the right size for the calibre the shooter named, it is
/// inside a bull's cell, and it is within <see cref="WithinCalibres"/> bullet widths of that cell's middle, which is where a shot aimed at
/// that bull actually lands.
/// </para>
/// <para>
/// <b>It needs a stated calibre, and that is deliberate.</b> Without one there is no size to judge against, and the rule would come down to
/// "ragged things near bulls are holes", which is the kind of position prior that invents shots. This is also why entry 131 section 6.3 asks
/// for the calibre before Accept: the answer is worth real holes.
/// </para>
/// </summary>
public static class TornHole
{
    /// <summary>
    /// How ragged a mark may be and still be rescued. Below this, the marks on the one sheet where this was measured are handwriting, so
    /// nothing is rescued there however well placed it is.
    /// </summary>
    public const double LeastSolidity = 0.45;

    /// <summary>
    /// How far from the middle of a bull a torn hole may sit, in hole widths. A shot aimed at a bull lands within about this much of it,
    /// and every ragged mark on scan 1 that is not a hole is further out than this or outside a cell altogether.
    /// </summary>
    public const double WithinCalibres = 1.5;

    /// <summary>The widest a rescued mark may be, in hole widths. A torn hole is a little larger than a clean one, not twice the size.</summary>
    public const double WidestInCalibres = 1.6;

    /// <summary>
    /// Whether a mark the compactness gate is about to refuse should be kept as a torn hole.
    /// </summary>
    /// <param name="solidity">The mark's hull solidity, the ratio of its area to its convex hull's.</param>
    /// <param name="minimumSolidity">The compactness floor it failed.</param>
    /// <param name="diameterInches">The mark's diameter.</param>
    /// <param name="holeInches">What a hole from the named calibre is taken to measure, which is what this stage holds rather than the bullet's own diameter, or null where no calibre was named.</param>
    /// <param name="insideACell">Whether the mark is inside a bull's cell.</param>
    /// <param name="fromCellCentreInches">How far the mark is from the middle of the nearest bull's cell.</param>
    public static bool Rescues(double solidity, double minimumSolidity, double diameterInches, double? holeInches, bool insideACell, double fromCellCentreInches)
    {
        if (solidity >= minimumSolidity)
        {
            // It is not being refused for compactness, so there is nothing to rescue it from.
            return false;
        }

        if (holeInches is not { } hole || hole <= 0 || !insideACell)
        {
            return false;
        }

        return solidity >= LeastSolidity
            && diameterInches >= HoleSizeGate.MinimumDiameter(hole, HoleSizeGate.WithoutACalibreInches)
            && diameterInches <= hole * WidestInCalibres
            && fromCellCentreInches <= hole * WithinCalibres;
    }

    /// <summary>How a rescued mark reads in the record, so a torn hole is never counted silently.</summary>
    public static string Describe(double solidity) =>
        System.FormattableString.Invariant($"torn, hull solidity {solidity:0.00}, kept because it is the calibre's size and on a bull");
}
