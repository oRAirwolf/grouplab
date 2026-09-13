namespace GroupLab.Core.Gltd.Derivation;

/// <summary>Minor line positions of a measurement grid, TARGET-SCHEMA.md section 3.13.</summary>
public static class MeasurementGridLines
{
    /// <summary>
    /// Offsets from the centre of minor lines 0 to <paramref name="divisions"/>: <c>round(half * i / divisions)</c>
    /// from the stored half, ties toward zero (section 2, conformance test 37a).
    /// </summary>
    public static IReadOnlyList<int> Offsets(int half, int divisions)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(half);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(divisions);
        var offsets = new int[divisions + 1];
        for (int i = 0; i <= divisions; i++)
        {
            offsets[i] = DerivedRounding.Boundary(half, i, divisions);
        }

        return offsets;
    }

    /// <summary>Absolute positions of every minor line, from <c>-divisions</c> to <c>+divisions</c>, ascending.</summary>
    public static IReadOnlyList<int> Positions(int centre, int half, int divisions)
    {
        var offsets = Offsets(half, divisions);
        var positions = new int[(2 * divisions) + 1];
        for (int i = -divisions; i <= divisions; i++)
        {
            positions[i + divisions] = centre + (Math.Sign(i) * offsets[Math.Abs(i)]);
        }

        return positions;
    }
}
