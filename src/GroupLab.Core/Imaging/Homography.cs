namespace GroupLab.Core.Imaging;

/// <summary>A 3 by 3 projective transform, stored row-major.</summary>
public sealed class Homography
{
    private readonly double[] _m;

    public Homography(IReadOnlyList<double> rowMajor)
    {
        ArgumentNullException.ThrowIfNull(rowMajor);
        if (rowMajor.Count != 9)
        {
            throw new ArgumentException($"A homography has 9 entries, got {rowMajor.Count}.", nameof(rowMajor));
        }

        _m = [.. rowMajor];
    }

    public static Homography Identity { get; } = new([1, 0, 0, 0, 1, 0, 0, 0, 1]);

    public double this[int row, int column] => _m[(row * 3) + column];

    public PointD Apply(PointD p)
    {
        double w = (_m[6] * p.X) + (_m[7] * p.Y) + _m[8];
        return new PointD(
            ((_m[0] * p.X) + (_m[1] * p.Y) + _m[2]) / w,
            ((_m[3] * p.X) + (_m[4] * p.Y) + _m[5]) / w);
    }
}
