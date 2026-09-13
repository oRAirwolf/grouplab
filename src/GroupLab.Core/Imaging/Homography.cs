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

    /// <summary>The transform that applies <paramref name="first"/> and then <paramref name="second"/>.</summary>
    public static Homography Compose(Homography first, Homography second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var result = new double[9];
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                for (int k = 0; k < 3; k++)
                {
                    result[(row * 3) + column] += second[row, k] * first[k, column];
                }
            }
        }

        return new Homography(result);
    }

    public Homography Inverse()
    {
        double a = _m[0], b = _m[1], c = _m[2], d = _m[3], e = _m[4], f = _m[5], g = _m[6], h = _m[7], i = _m[8];
        double c11 = (e * i) - (f * h), c12 = (f * g) - (d * i), c13 = (d * h) - (e * g);
        double determinant = (a * c11) + (b * c12) + (c * c13);
        if (Math.Abs(determinant) < 1e-300)
        {
            throw new InvalidOperationException("The homography is singular.");
        }

        return new Homography([
            c11 / determinant, ((c * h) - (b * i)) / determinant, ((b * f) - (c * e)) / determinant,
            c12 / determinant, ((a * i) - (c * g)) / determinant, ((c * d) - (a * f)) / determinant,
            c13 / determinant, ((b * g) - (a * h)) / determinant, ((a * e) - (b * d)) / determinant]);
    }

    /// <summary>The derivative of <see cref="Apply"/> at <paramref name="p"/>: how far the output moves per unit of input on each axis.</summary>
    public (double XX, double XY, double YX, double YY) Jacobian(PointD p)
    {
        double w = (_m[6] * p.X) + (_m[7] * p.Y) + _m[8];
        var q = Apply(p);
        return ((_m[0] - (_m[6] * q.X)) / w, (_m[1] - (_m[7] * q.X)) / w, (_m[3] - (_m[6] * q.Y)) / w, (_m[4] - (_m[7] * q.Y)) / w);
    }
}
