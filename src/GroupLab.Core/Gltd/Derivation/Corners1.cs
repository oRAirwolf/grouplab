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
    /// Code centres: top pair for a count of 2, both pairs for 4. A half-dmm centre, which only an odd
    /// module size produces, rounds half to even exactly as <c>tools/gltd/check.py</c> does.
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

        double half = FootprintModules * moduleSize / 2.0;
        double top = SafeMargin + half;
        double bottom = pageHeight - SafeMargin - dataBlockHeight - (dataBlockHeight > 0 ? Clearance : 0) - half;
        double left = SafeMargin + half;
        double right = pageWidth - SafeMargin - half;

        var positions = new List<PointDmm>(count) { new(Round(left), Round(top)), new(Round(right), Round(top)) };
        if (count == 4)
        {
            positions.Add(new PointDmm(Round(left), Round(bottom)));
            positions.Add(new PointDmm(Round(right), Round(bottom)));
        }

        return positions;
    }

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.ToEven);
}
