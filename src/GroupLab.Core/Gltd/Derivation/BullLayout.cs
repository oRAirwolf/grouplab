using GroupLab.Core.Gltd.Model;

namespace GroupLab.Core.Gltd.Derivation;

/// <summary>A complete grid of scoring bulls. <see cref="OriginX"/> and <see cref="OriginY"/> are the first bull's centre.</summary>
public sealed record GridLayout(int Cols, int Rows, int OriginX, int OriginY, int PitchX, int PitchY, string RingSet, int Order);

public sealed record SighterRowLayout(int Count, int OriginX, int OriginY, int PitchX, string RingSet);

public sealed record ParametricLayout(GridLayout Grid, IReadOnlyList<SighterRowLayout> Sighters);

/// <summary>
/// Recognises the parametric layout of TARGET-SCHEMA.md section 5.2 in a bull list, per
/// docs/SPEC-ERRATA.md C9. The encoder uses it to choose parametric mode and the fiducial derivations
/// use it to find the lattice, so both read the same grid.
/// </summary>
public static class BullLayout
{
    public const int MaxSighterRows = 4;

    public static ParametricLayout? Recognise(TargetDefinition d)
    {
        ArgumentNullException.ThrowIfNull(d);
        var bulls = d.Bulls;
        int n = 0;
        while (n < bulls.Count && bulls[n].Scoring)
        {
            n++;
        }

        if (n == 0 || bulls.Skip(n).Any(b => b.Scoring) || bulls.Take(n).Any(b => b.RingSet != bulls[0].RingSet))
        {
            return null;
        }

        var xs = bulls.Take(n).Select(b => b.X).Distinct().Order().ToList();
        var ys = bulls.Take(n).Select(b => b.Y).Distinct().Order().ToList();
        int cols = xs.Count, rows = ys.Count;
        if (cols * rows != n || cols > 255 || rows > 255 || Step(xs) is not { } pitchX || Step(ys) is not { } pitchY)
        {
            return null;
        }

        var declared = d.Cells?.Grid;
        if (cols == 1)
        {
            pitchX = declared?.PitchX ?? (rows > 1 ? pitchY : 0);
        }

        if (rows == 1)
        {
            pitchY = declared?.PitchY ?? (cols > 1 ? pitchX : 0);
        }

        if (pitchX > ushort.MaxValue || pitchY > ushort.MaxValue)
        {
            return null;
        }

        int order = -1;
        for (int o = 0; o <= 2 && order < 0; o++)
        {
            bool matches = true;
            for (int k = 0; k < n && matches; k++)
            {
                var (row, col) = GridCell(k, o, cols, rows);
                matches = bulls[k].X == xs[0] + (col * pitchX) && bulls[k].Y == ys[0] + (row * pitchY);
            }

            if (matches)
            {
                order = o;
            }
        }

        if (order < 0)
        {
            return null;
        }

        var sighters = new List<SighterRowLayout>();
        for (int i = n; i < bulls.Count;)
        {
            int j = i;
            while (j < bulls.Count && bulls[j].Y == bulls[i].Y && bulls[j].RingSet == bulls[i].RingSet)
            {
                j++;
            }

            int count = j - i;
            int pitch = count == 1 ? pitchX : bulls[i + 1].X - bulls[i].X;
            if (count > 255 || pitch < 0 || pitch > ushort.MaxValue)
            {
                return null;
            }

            for (int k = 0; k < count; k++)
            {
                if (bulls[i + k].X != bulls[i].X + (k * pitch))
                {
                    return null;
                }
            }

            sighters.Add(new SighterRowLayout(count, bulls[i].X, bulls[i].Y, pitch, bulls[i].RingSet));
            i = j;
        }

        if (sighters.Count > MaxSighterRows)
        {
            return null;
        }

        return new ParametricLayout(new GridLayout(cols, rows, xs[0], ys[0], pitchX, pitchY, bulls[0].RingSet, order), sighters);
    }

    /// <summary>Row and column of the k-th grid bull under the grid order byte of section 5.2.</summary>
    public static (int Row, int Col) GridCell(int k, int order, int cols, int rows) => order switch
    {
        1 => (k % rows, k / rows),
        2 => (k / cols, (k / cols) % 2 == 0 ? k % cols : cols - 1 - (k % cols)),
        _ => (k / cols, k % cols),
    };

    /// <summary>The common step of sorted distinct values: 0 for one value, null when uneven.</summary>
    private static int? Step(List<int> values)
    {
        if (values.Count == 1)
        {
            return 0;
        }

        int step = values[1] - values[0];
        for (int i = 2; i < values.Count; i++)
        {
            if (values[i] - values[i - 1] != step)
            {
                return null;
            }
        }

        return step;
    }
}
