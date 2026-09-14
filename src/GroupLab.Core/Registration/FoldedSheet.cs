using System.Runtime.CompilerServices;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>Which developable surface a <see cref="SurfaceModel"/> bends as.</summary>
public enum SurfaceFamily
{
    /// <summary>Parallel rulings at <see cref="SurfaceModel.RulingAngle"/>, the generalised cylinder of PHASE1-BRIEF.md section 3.1.</summary>
    Cylinder,

    /// <summary>Rulings whose direction turns across the page by <see cref="SurfaceModel.Turn"/>, folded as <see cref="FoldedSheet"/> describes.</summary>
    General,
}

/// <summary>
/// The general developable surface of NOTES-FROM-PLANNING.md entry 16 section 5: a ruling direction that varies across the
/// page instead of a single angle, so that it takes a cone, or any sheet whose rulings are not parallel, as well as a
/// cylinder. PHASE1-RESULTS.md M1.7 found curvature along the fitted rulings on six of seven mounted frames, which is what a
/// cylinder leaves on such a sheet.
/// <para>
/// The spine is the line through the page centre across the mean rulings, at <see cref="SurfaceModel.RulingAngle"/>. The
/// ruling at arc length t along it runs straight across the flat page at angle
/// <c>RulingAngle + sum over k of Turn[k] (t / L)^(k+1)</c>, with L the <see cref="SurfaceModel.BendLength"/>. The sheet is
/// folded along rulings <see cref="FoldSpacing"/> apart, each by the change in the tangent angle of
/// <see cref="SurfaceModel.Bend"/> between the strips either side of it, and the central strip is turned by the tangent
/// angle at the spine. Every strip is a rigid piece of the page, so the map from page to sheet is an isometry by
/// construction, as the cylinder's is. With no turn the rulings are parallel and the surface is the generalised cylinder,
/// discretised; <c>GeneralDevelopableTests</c> holds the two together.
/// </para>
/// <para>
/// Rulings that cross inside the page would fold the sheet through itself, so such a model is invalid and every point maps
/// to NaN, which a fit reads as a residual it cannot reach. A cone whose apex sits at or beyond the page edge, as a sheet
/// hanging from a pin makes, is valid. Folds are built out to 1.2 times the page's half-diagonal along the spine; a point
/// beyond the last fold is carried by it.
/// </para>
/// </summary>
public sealed class FoldedSheet
{
    /// <summary>Arc length between folds along the spine, dmm.</summary>
    public const double FoldSpacing = 10;

    private static readonly ConditionalWeakTable<SurfaceModel, FoldedSheet> Cache = new();

    private readonly double _centreX, _centreY;
    private readonly double[] _central;
    private readonly Side _positive, _negative;

    public FoldedSheet(SurfaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _centreX = model.PageCentreX;
        _centreY = model.PageCentreY;
        double halfWidth = Math.Abs(model.PageCentreX), halfHeight = Math.Abs(model.PageCentreY);
        int count = (int)Math.Ceiling(1.2 * Math.Sqrt((halfWidth * halfWidth) + (halfHeight * halfHeight)) / FoldSpacing) + 1;
        double nx = -Math.Sin(model.RulingAngle), ny = Math.Cos(model.RulingAngle);
        double theta0 = Theta(model, 0);
        _central = Fold([1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0], 0, 0, Math.Cos(theta0), Math.Sin(theta0), DevelopableSurface.TangentAngle(model.Bend, 0));
        _positive = Build(model, count, 1, nx, ny, _central);
        _negative = Build(model, count, -1, nx, ny, _central);

        bool valid = !Crosses(0, 0, Math.Cos(theta0), Math.Sin(theta0), _positive, 0, halfWidth, halfHeight)
            && !Crosses(0, 0, Math.Cos(theta0), Math.Sin(theta0), _negative, 0, halfWidth, halfHeight);
        foreach (var side in (Side[])[_positive, _negative])
        {
            for (int k = 0; valid && k + 1 < count; k++)
            {
                valid = !Crosses(side.Sx[k], side.Sy[k], side.Dx[k], side.Dy[k], side, k + 1, halfWidth, halfHeight);
            }
        }

        Valid = valid;
    }

    /// <summary>False when two rulings cross inside the page.</summary>
    public bool Valid { get; }

    /// <summary>The folded sheet of <paramref name="model"/>, built once per model instance.</summary>
    public static FoldedSheet For(SurfaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Cache.GetValue(model, m => new FoldedSheet(m));
    }

    /// <summary>The ruling direction at arc length <paramref name="along"/> on the spine, radians in page coordinates.</summary>
    public static double Theta(SurfaceModel model, double along)
    {
        ArgumentNullException.ThrowIfNull(model);
        double angle = model.RulingAngle, x = along / SurfaceModel.BendLength, power = x;
        foreach (double turn in model.Turn ?? [])
        {
            angle += turn * power;
            power *= x;
        }

        return angle;
    }

    /// <summary>The page point on the folded sheet, dmm, in the frame <see cref="DevelopableSurface.Sheet"/> uses.</summary>
    public (double X, double Y, double Z) Sheet(PointD page)
    {
        if (!Valid)
        {
            return (double.NaN, double.NaN, double.NaN);
        }

        double qx = page.X - _centreX, qy = page.Y - _centreY;
        double[] t = _central;
        if (Beyond(_positive, 0, qx, qy))
        {
            t = _positive.T[Last(_positive, qx, qy)];
        }
        else if (Beyond(_negative, 0, qx, qy))
        {
            t = _negative.T[Last(_negative, qx, qy)];
        }

        return ((t[0] * qx) + (t[1] * qy) + t[9], (t[3] * qx) + (t[4] * qy) + t[10], (t[6] * qx) + (t[7] * qy) + t[11]);
    }

    private sealed class Side(int count, int sign)
    {
        public int Sign { get; } = sign;

        public double[] Sx { get; } = new double[count];

        public double[] Sy { get; } = new double[count];

        public double[] Dx { get; } = new double[count];

        public double[] Dy { get; } = new double[count];

        public double[][] T { get; } = new double[count][];
    }

    private static Side Build(SurfaceModel model, int count, int sign, double nx, double ny, double[] central)
    {
        var side = new Side(count, sign);
        var t = central;
        for (int m = 1; m <= count; m++)
        {
            double at = sign * (m - 0.5) * FoldSpacing;
            double theta = Theta(model, at);
            double angle = DevelopableSurface.TangentAngle(model.Bend, sign * m * FoldSpacing) - DevelopableSurface.TangentAngle(model.Bend, sign * (m - 1) * FoldSpacing);
            int k = m - 1;
            side.Sx[k] = at * nx;
            side.Sy[k] = at * ny;
            side.Dx[k] = Math.Cos(theta);
            side.Dy[k] = Math.Sin(theta);
            t = Fold(t, side.Sx[k], side.Sy[k], side.Dx[k], side.Dy[k], angle);
            side.T[k] = t;
        }

        return side;
    }

    /// <summary>
    /// <paramref name="t"/> composed with a rotation by <paramref name="angle"/> about the flat page line through
    /// (<paramref name="sx"/>, <paramref name="sy"/>) along (<paramref name="dx"/>, <paramref name="dy"/>): the fold is applied in
    /// flat coordinates first and carried by the folds nearer the spine. A positive angle turns the far side towards +z.
    /// </summary>
    private static double[] Fold(double[] t, double sx, double sy, double dx, double dy, double angle)
    {
        double c = Math.Cos(angle), s = Math.Sin(angle), v = 1 - c;
        double f00 = c + (dx * dx * v), f01 = dx * dy * v, f02 = dy * s;
        double f10 = dx * dy * v, f11 = c + (dy * dy * v), f12 = -dx * s;
        double f20 = -dy * s, f21 = dx * s, f22 = c;
        double ux = sx - ((f00 * sx) + (f01 * sy)), uy = sy - ((f10 * sx) + (f11 * sy)), uz = -((f20 * sx) + (f21 * sy));
        return
        [
            (t[0] * f00) + (t[1] * f10) + (t[2] * f20), (t[0] * f01) + (t[1] * f11) + (t[2] * f21), (t[0] * f02) + (t[1] * f12) + (t[2] * f22),
            (t[3] * f00) + (t[4] * f10) + (t[5] * f20), (t[3] * f01) + (t[4] * f11) + (t[5] * f21), (t[3] * f02) + (t[4] * f12) + (t[5] * f22),
            (t[6] * f00) + (t[7] * f10) + (t[8] * f20), (t[6] * f01) + (t[7] * f11) + (t[8] * f21), (t[6] * f02) + (t[7] * f12) + (t[8] * f22),
            (t[0] * ux) + (t[1] * uy) + (t[2] * uz) + t[9], (t[3] * ux) + (t[4] * uy) + (t[5] * uz) + t[10], (t[6] * ux) + (t[7] * uy) + (t[8] * uz) + t[11],
        ];
    }

    /// <summary>Whether the page point lies beyond fold <paramref name="k"/> of <paramref name="side"/>, away from the spine.</summary>
    private static bool Beyond(Side side, int k, double qx, double qy) =>
        side.Sign * ((side.Dx[k] * (qy - side.Sy[k])) - (side.Dy[k] * (qx - side.Sx[k]))) > 0;

    /// <summary>The farthest fold the point lies beyond, by bisection: rulings that do not cross inside the page are ordered.</summary>
    private static int Last(Side side, double qx, double qy)
    {
        int lo = 0, hi = side.T.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (Beyond(side, mid, qx, qy))
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    /// <summary>Whether the line through (<paramref name="sx"/>, <paramref name="sy"/>) along (<paramref name="dx"/>, <paramref name="dy"/>) crosses fold <paramref name="k"/> inside the page.</summary>
    private static bool Crosses(double sx, double sy, double dx, double dy, Side side, int k, double halfWidth, double halfHeight)
    {
        double den = (dx * side.Dy[k]) - (dy * side.Dx[k]);
        if (Math.Abs(den) < 1e-12)
        {
            return false;
        }

        double u = (((side.Sx[k] - sx) * side.Dy[k]) - ((side.Sy[k] - sy) * side.Dx[k])) / den;
        double px = sx + (u * dx), py = sy + (u * dy);
        return Math.Abs(px) < halfWidth && Math.Abs(py) < halfHeight;
    }
}
