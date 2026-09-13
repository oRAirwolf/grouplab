using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>The <c>corners-1</c> code placement rule, TARGET-SCHEMA.md section 3.8, in full.</summary>
public static class Corners1
{
    public const int SafeMargin = 120;
    public const int Clearance = 30;

    /// <summary>57 modules of a version 10 symbol plus 4 quiet modules each side, whatever the version.</summary>
    public const int FootprintModules = 65;

    public static bool Supports(int count) => count is 0 or 2 or 4;

    /// <summary>
    /// Code centres: top pair for a count of 2, both pairs for 4. A half-dmm centre, which only an odd module
    /// size produces, rounds under the tie rule of section 2.
    /// </summary>
    public static IReadOnlyList<PointDmm> Positions(int pageWidth, int pageHeight, int dataBlockHeight, int count, int moduleSize)
    {
        if (!Supports(count))
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "corners-1 places 0, 2 or 4 codes.");
        }

        if (count == 0)
        {
            return [];
        }

        long footprint = FootprintModules * (long)moduleSize;
        int near = DerivedRounding.Halve((2L * SafeMargin) + footprint);
        int right = DerivedRounding.Halve((2L * (pageWidth - SafeMargin)) - footprint);
        int bottom = DerivedRounding.Halve(
            (2L * (pageHeight - SafeMargin - dataBlockHeight - (dataBlockHeight > 0 ? Clearance : 0))) - footprint);

        var positions = new List<PointDmm>(count) { new(near, near), new(right, near) };
        if (count == 4)
        {
            positions.Add(new PointDmm(near, bottom));
            positions.Add(new PointDmm(right, bottom));
        }

        return positions;
    }
}
