namespace GroupLab.Core.Detection;

/// <summary>
/// How small a mark may be and still be a bullet hole, NOTES-FROM-PLANNING.md entry 130 section 2b.3.
/// <para>
/// <b>Why this is not a constant.</b> The gate was a fixed 0.15 in, and on scan 4 of the second range day it refused five real .22 LR holes
/// measuring 0.11 to 0.14 in. Naming the calibre did not help, because nothing read the calibre here. Alan had shot the sheet, the holes were
/// plainly there, and GroupLab said they were too small to be holes.
/// </para>
/// <para>
/// The mistake was assuming a hole is about as wide as the bullet. It is not. Paper is elastic: it stretches ahead of the bullet and closes
/// behind it, so the hole left in a sheet is reliably narrower than the bullet that made it, and the lighter and faster the bullet the more
/// so. A .224 in bullet leaving a 0.11 in hole is not a marginal case; it is normal, and the smallest of those five measured 0.49 of its
/// calibre.
/// </para>
/// <para>
/// So a stated calibre sets the floor, at a fraction that admits a hole that has closed as far as those did, with a little room below. Where
/// no calibre is stated the old fixed floor stands, because without one there is nothing better to say.
/// </para>
/// </summary>
public static class HoleSizeGate
{
    /// <summary>
    /// The smallest hole a bullet of a given calibre is allowed to leave, as a fraction of that calibre.
    /// <para>
    /// 0.45 rather than 0.49, which is the smallest real hole measured, because a gate set exactly at the worst case seen so far refuses the
    /// next one that is slightly worse. It is not lower than that because the point of the gate is to keep out specks and paper fibres, and
    /// every tenth below this is another kind of speck let in.
    /// </para>
    /// </summary>
    public const double SmallestShareOfCalibre = 0.45;

    /// <summary>
    /// An absolute floor, whatever the calibre. At 600 dpi this is 36 pixels across, which no fibre, speck or scanner artefact reaches, and
    /// it stops a nonsense calibre from opening the gate to everything.
    /// </summary>
    public const double AbsoluteFloorInches = 0.06;

    /// <summary>The floor used when nobody has named a calibre, which is where this project started.</summary>
    public const double WithoutACalibreInches = 0.15;

    /// <summary>
    /// The smallest diameter to accept, in inches.
    /// </summary>
    /// <param name="calibreInches">The bullet's diameter, where the shooter has named one.</param>
    /// <param name="fallbackInches">What to use when they have not, normally <see cref="WithoutACalibreInches"/>.</param>
    public static double MinimumDiameter(double? calibreInches, double fallbackInches)
    {
        if (calibreInches is not { } calibre || calibre <= 0)
        {
            return fallbackInches;
        }

        return Math.Max(calibre * SmallestShareOfCalibre, AbsoluteFloorInches);
    }

    /// <summary>What the log and a refusal say, so the number a person is shown is the number that was applied.</summary>
    public static string Describe(double? calibreInches, double fallbackInches)
    {
        double floor = MinimumDiameter(calibreInches, fallbackInches);
        return calibreInches is { } calibre && calibre > 0
            ? FormattableString.Invariant($"{floor:0.000} in, from the {calibre:0.000} in bullet named")
            : FormattableString.Invariant($"{floor:0.000} in, with no calibre named");
    }
}
