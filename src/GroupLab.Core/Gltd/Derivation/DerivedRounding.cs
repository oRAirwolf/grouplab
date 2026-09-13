namespace GroupLab.Core.Gltd.Derivation;

/// <summary>
/// The rounding rule of TARGET-SCHEMA.md section 2 for every derived boundary: nearest, ties toward zero,
/// in integer arithmetic so that no floating-point value touches a stored coordinate.
/// </summary>
public static class DerivedRounding
{
    /// <summary><c>round(numerator / denominator)</c>, ties toward zero: <c>(2 * numerator + denominator - 1) / (2 * denominator)</c>.</summary>
    public static long Divide(long numerator, long denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(denominator);
        return numerator >= 0
            ? ((2 * numerator) + denominator - 1) / (2 * denominator)
            : -(((-2 * numerator) + denominator - 1) / (2 * denominator));
    }

    /// <summary><c>round(extent * i / n)</c>, the form section 2 writes.</summary>
    public static int Boundary(int extent, int i, int n) => checked((int)Divide((long)extent * i, n));

    /// <summary>Halves a doubled coordinate under the same rule.</summary>
    public static int Halve(long doubled) => checked((int)Divide(doubled, 2));
}
