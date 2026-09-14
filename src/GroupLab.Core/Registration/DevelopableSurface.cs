using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>How a <see cref="SurfaceModel"/> is seen: through a pinhole camera, or orthographically as a flatbed scans.</summary>
public enum SurfaceProjection
{
    Perspective,
    Orthographic,
}

/// <summary>
/// A sheet bent as a generalised cylinder and seen by a camera, PHASE1-BRIEF.md section 3.1. Paper bends without
/// stretching (DESIGN.md section 6), so the sheet bends about a family of parallel straight rulings, at
/// <see cref="RulingAngle"/> in page coordinates, and its cross-section is a curve parameterised by arc length.
/// <see cref="Bend"/> holds that curve's tangent angle as a polynomial in arc length: coefficient k multiplies
/// <c>(t / BendLength)^k</c>, in radians, and the fit keeps coefficient 0 at zero because a constant angle is a rigid
/// rotation the camera already carries. Integrating the tangent angle along arc length is what makes the map from page to
/// sheet an isometry by construction; all zeros is a flat sheet.
/// <para>
/// A perspective model has a rotation vector and translation in dmm, a focal length in normalised units (pixels divided
/// by <see cref="Scale"/>), and the Phase 0 lens: image coordinates normalised about <see cref="CentreX"/>,
/// <see cref="CentreY"/> by <see cref="Scale"/> are undistorted as <c>u' = u (1 + k1 r^2 + k2 r^4)</c>, the model of
/// <see cref="RadialHomographyMapping"/>. An orthographic model, for a scan, has no depth or lens: <see cref="Focal"/> is
/// normalised units per dmm and the translation is in normalised units.
/// </para>
/// <para>
/// <see cref="Family"/> is the cylinder unless it says <see cref="SurfaceFamily.General"/>, whose rulings turn across the
/// page by <see cref="Turn"/>, coefficient k multiplying <c>(t / BendLength)^(k+1)</c> in radians (<see cref="FoldedSheet"/>).
/// </para>
/// </summary>
public sealed record SurfaceModel(
    SurfaceProjection Projection,
    double RulingAngle,
    IReadOnlyList<double> Bend,
    double RotationX,
    double RotationY,
    double RotationZ,
    double TranslationX,
    double TranslationY,
    double TranslationZ,
    double Focal,
    double K1,
    double K2,
    double CentreX,
    double CentreY,
    double Scale,
    double PageCentreX,
    double PageCentreY,
    SurfaceFamily Family = SurfaceFamily.Cylinder,
    IReadOnlyList<double>? Turn = null)
{
    /// <summary>The arc length, in dmm, that the bend polynomial's variable is divided by, so its coefficients are of order one.</summary>
    public const double BendLength = 1000;

    public double FocalPixels => Projection == SurfaceProjection.Perspective ? Focal * Scale : double.NaN;
}

/// <summary>The geometry of <see cref="SurfaceModel"/>: page to sheet, sheet to image, and the lens.</summary>
public static class DevelopableSurface
{
    // Eight-point Gauss-Legendre on [-1, 1], applied on two panels: exact for the flat sheet and far below a micron for
    // any bend a sheet of paper can take.
    private static readonly double[] Nodes = [-0.9602898564975363, -0.7966664774136267, -0.5255324099163290, -0.1834346424956498, 0.1834346424956498, 0.5255324099163290, 0.7966664774136267, 0.9602898564975363];

    private static readonly double[] Weights = [0.1012285362903763, 0.2223810344533745, 0.3137066458778873, 0.3626837833783620, 0.3626837833783620, 0.3137066458778873, 0.2223810344533745, 0.1012285362903763];

    /// <summary>The page point's coordinates along the rulings and across them, in dmm from the page centre.</summary>
    public static (double Along, double Across) RulingCoordinates(SurfaceModel model, PointD page)
    {
        ArgumentNullException.ThrowIfNull(model);
        double dx = page.X - model.PageCentreX, dy = page.Y - model.PageCentreY;
        double c = Math.Cos(model.RulingAngle), s = Math.Sin(model.RulingAngle);
        return ((dx * c) + (dy * s), (-dx * s) + (dy * c));
    }

    public static double TangentAngle(IReadOnlyList<double> bend, double across)
    {
        ArgumentNullException.ThrowIfNull(bend);
        double x = across / SurfaceModel.BendLength, power = 1, angle = 0;
        for (int k = 0; k < bend.Count; k++)
        {
            angle += bend[k] * power;
            power *= x;
        }

        return angle;
    }

    /// <summary>The cross-section at arc length <paramref name="across"/>: position in the flat direction and out of the page, dmm.</summary>
    public static (double X, double Z) Profile(IReadOnlyList<double> bend, double across)
    {
        double x = 0, z = 0, half = across / 4;
        for (int panel = 0; panel < 2; panel++)
        {
            double mid = half * ((2 * panel) + 1);
            for (int i = 0; i < Nodes.Length; i++)
            {
                double angle = TangentAngle(bend, mid + (half * Nodes[i]));
                x += Weights[i] * half * Math.Cos(angle);
                z += Weights[i] * half * Math.Sin(angle);
            }
        }

        return (x, z);
    }

    /// <summary>The page point on the bent sheet, dmm, in a frame whose x and y are the flat page's about its centre.</summary>
    public static (double X, double Y, double Z) Sheet(SurfaceModel model, PointD page)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Family == SurfaceFamily.General)
        {
            return FoldedSheet.For(model).Sheet(page);
        }

        var (along, across) = RulingCoordinates(model, page);
        var (x, z) = Profile(model.Bend, across);
        double c = Math.Cos(model.RulingAngle), s = Math.Sin(model.RulingAngle);
        return ((along * c) - (x * s), (along * s) + (x * c), z);
    }

    /// <summary>Undistorted normalised image coordinates of a sheet point, or NaN when it is behind a perspective camera.</summary>
    public static PointD Project(SurfaceModel model, (double X, double Y, double Z) sheet)
    {
        ArgumentNullException.ThrowIfNull(model);
        return Project(model, Rotation(model.RotationX, model.RotationY, model.RotationZ), sheet);
    }

    /// <summary>As the overload without a matrix, with the model's rotation matrix already computed.</summary>
    public static PointD Project(SurfaceModel model, double[,] r, (double X, double Y, double Z) sheet)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(r);
        double qx = (r[0, 0] * sheet.X) + (r[0, 1] * sheet.Y) + (r[0, 2] * sheet.Z);
        double qy = (r[1, 0] * sheet.X) + (r[1, 1] * sheet.Y) + (r[1, 2] * sheet.Z);
        if (model.Projection == SurfaceProjection.Orthographic)
        {
            return new PointD((model.Focal * qx) + model.TranslationX, (model.Focal * qy) + model.TranslationY);
        }

        double qz = (r[2, 0] * sheet.X) + (r[2, 1] * sheet.Y) + (r[2, 2] * sheet.Z) + model.TranslationZ;
        if (qz <= 0)
        {
            return new PointD(double.NaN, double.NaN);
        }

        return new PointD(model.Focal * (qx + model.TranslationX) / qz, model.Focal * (qy + model.TranslationY) / qz);
    }

    /// <summary>An image pixel, normalised and undistorted by the Phase 0 lens model.</summary>
    public static PointD Undistort(SurfaceModel model, PointD image)
    {
        ArgumentNullException.ThrowIfNull(model);
        double x = (image.X - model.CentreX) / model.Scale, y = (image.Y - model.CentreY) / model.Scale;
        double r2 = (x * x) + (y * y);
        double g = 1 + (model.K1 * r2) + (model.K2 * r2 * r2);
        return new PointD(x * g, y * g);
    }

    /// <summary>The pixel whose undistorted normalised coordinates are <paramref name="normalised"/>, by Newton on the radius.</summary>
    public static PointD Distort(SurfaceModel model, PointD normalised)
    {
        ArgumentNullException.ThrowIfNull(model);
        double rho = Math.Sqrt((normalised.X * normalised.X) + (normalised.Y * normalised.Y));
        if (double.IsNaN(rho))
        {
            return normalised;
        }

        if (rho == 0 || (model.K1 == 0 && model.K2 == 0))
        {
            return new PointD(model.CentreX + (model.Scale * normalised.X), model.CentreY + (model.Scale * normalised.Y));
        }

        double r = rho;
        for (int i = 0; i < 30; i++)
        {
            double r2 = r * r;
            double slope = 1 + (3 * model.K1 * r2) + (5 * model.K2 * r2 * r2);
            if (slope == 0)
            {
                break;
            }

            double next = r - (((r * (1 + (model.K1 * r2) + (model.K2 * r2 * r2))) - rho) / slope);
            bool converged = Math.Abs(next - r) < 1e-14;
            r = next;
            if (converged)
            {
                break;
            }
        }

        double k = r / rho;
        return new PointD(model.CentreX + (model.Scale * normalised.X * k), model.CentreY + (model.Scale * normalised.Y * k));
    }

    public static PointD ToImage(SurfaceModel model, PointD page) => Distort(model, Project(model, Sheet(model, page)));

    /// <summary>
    /// The largest distance, in dmm, of the bent cross-section from the straight line joining its two ends across the
    /// page: the deflection a ruler laid across the sheet perpendicular to the rulings would show.
    /// </summary>
    public static double Deflection(SurfaceModel model, double pageWidth, double pageHeight)
    {
        ArgumentNullException.ThrowIfNull(model);
        var across = new[] { new PointD(0, 0), new PointD(pageWidth, 0), new PointD(pageWidth, pageHeight), new PointD(0, pageHeight) }
            .Select(p => RulingCoordinates(model, p).Across).ToList();
        double t0 = across.Min(), t1 = across.Max();
        if (model.Family == SurfaceFamily.General)
        {
            // The same ruler, laid along the spine: the largest distance of the sheet under it from the chord between its ends.
            double nx = -Math.Sin(model.RulingAngle), ny = Math.Cos(model.RulingAngle);
            (double X, double Y, double Z) At(double t) => Sheet(model, new PointD(model.PageCentreX + (t * nx), model.PageCentreY + (t * ny)));
            var start = At(t0);
            var end = At(t1);
            double cx = end.X - start.X, cy = end.Y - start.Y, cz = end.Z - start.Z, chord = Math.Sqrt((cx * cx) + (cy * cy) + (cz * cz));
            double far = 0;
            for (int i = 1; i < 200; i++)
            {
                var q = At(t0 + ((t1 - t0) * i / 200));
                double px = q.X - start.X, py = q.Y - start.Y, pz = q.Z - start.Z;
                double ox = (py * cz) - (pz * cy), oy = (pz * cx) - (px * cz), oz = (px * cy) - (py * cx);
                far = Math.Max(far, Math.Sqrt((ox * ox) + (oy * oy) + (oz * oz)) / chord);
            }

            return far;
        }

        var a = Profile(model.Bend, t0);
        var b = Profile(model.Bend, t1);
        double lx = b.X - a.X, lz = b.Z - a.Z, length = Math.Sqrt((lx * lx) + (lz * lz));
        double worst = 0;
        for (int i = 1; i < 200; i++)
        {
            var p = Profile(model.Bend, t0 + ((t1 - t0) * i / 200));
            worst = Math.Max(worst, Math.Abs(((p.X - a.X) * lz) - ((p.Z - a.Z) * lx)) / length);
        }

        return worst;
    }

    /// <summary>The rotation matrix of a rotation vector, by Rodrigues' formula.</summary>
    public static double[,] Rotation(double wx, double wy, double wz)
    {
        double angle = Math.Sqrt((wx * wx) + (wy * wy) + (wz * wz));
        if (angle < 1e-12)
        {
            return new double[,] { { 1, -wz, wy }, { wz, 1, -wx }, { -wy, wx, 1 } };
        }

        double x = wx / angle, y = wy / angle, z = wz / angle, c = Math.Cos(angle), s = Math.Sin(angle), v = 1 - c;
        return new double[,]
        {
            { c + (x * x * v), (x * y * v) - (z * s), (x * z * v) + (y * s) },
            { (y * x * v) + (z * s), c + (y * y * v), (y * z * v) - (x * s) },
            { (z * x * v) - (y * s), (z * y * v) + (x * s), c + (z * z * v) },
        };
    }

    /// <summary>The rotation vector of a rotation matrix, robust at half a turn, where an upside-down photograph puts it.</summary>
    public static (double X, double Y, double Z) RotationVector(double[,] r)
    {
        ArgumentNullException.ThrowIfNull(r);
        double cosine = Math.Clamp((r[0, 0] + r[1, 1] + r[2, 2] - 1) / 2, -1, 1);
        double angle = Math.Acos(cosine);
        double sx = r[2, 1] - r[1, 2], sy = r[0, 2] - r[2, 0], sz = r[1, 0] - r[0, 1];
        if (angle < 1e-9)
        {
            return (sx / 2, sy / 2, sz / 2);
        }

        if (Math.PI - angle > 1e-6)
        {
            double f = angle / (2 * Math.Sin(angle));
            return (f * sx, f * sy, f * sz);
        }

        double xx = Math.Sqrt(Math.Max(0, (r[0, 0] + 1) / 2)), yy = Math.Sqrt(Math.Max(0, (r[1, 1] + 1) / 2)), zz = Math.Sqrt(Math.Max(0, (r[2, 2] + 1) / 2));
        if (xx >= yy && xx >= zz)
        {
            yy = Math.CopySign(yy, r[0, 1]);
            zz = Math.CopySign(zz, r[0, 2]);
        }
        else if (yy >= zz)
        {
            xx = Math.CopySign(xx, r[0, 1]);
            zz = Math.CopySign(zz, r[1, 2]);
        }
        else
        {
            xx = Math.CopySign(xx, r[0, 2]);
            yy = Math.CopySign(yy, r[1, 2]);
        }

        return (angle * xx, angle * yy, angle * zz);
    }
}

/// <summary>
/// The registration of a bent sheet, <see cref="SurfaceModel"/>, as the page mapping every measurement already uses. The
/// model runs from page to image, the direction a camera does, so <see cref="ToPage"/> is the iterative one: Newton from a
/// homography fitted to the model over the page.
/// <para>
/// A bull locator calls the mapping for every pixel of a bull's box, millions of times on a 600 DPI scan, so the mapping
/// computes the rotation matrix once and tabulates the cross-section at 1 dmm, with its exact slope, the cosine and sine of
/// the tangent angle, at every node, read back by cubic Hermite interpolation. The fit itself integrates directly.
/// <c>DevelopableSurfaceTests</c> holds the two within a millionth of a pixel on a strongly bent sheet.
/// </para>
/// </summary>
public sealed class SurfaceMapping : IPageMapping
{
    private const double TableStep = 1;

    private readonly Homography _approximateToPage;
    private readonly double[,] _rotation;
    private readonly double _rulingCos, _rulingSin, _tableStart;
    private readonly double[] _x, _z, _slopeX, _slopeZ;
    private readonly FoldedSheet? _folded;

    public SurfaceMapping(SurfaceModel parameters, double pageLeft, double pageTop, double pageRight, double pageBottom)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
        _rotation = DevelopableSurface.Rotation(parameters.RotationX, parameters.RotationY, parameters.RotationZ);
        _folded = parameters.Family == SurfaceFamily.General ? new FoldedSheet(parameters) : null;
        _rulingCos = Math.Cos(parameters.RulingAngle);
        _rulingSin = Math.Sin(parameters.RulingAngle);
        double pad = 0.5 * Math.Max(pageRight - pageLeft, pageBottom - pageTop);
        var across = new[] { new PointD(pageLeft - pad, pageTop - pad), new PointD(pageRight + pad, pageTop - pad), new PointD(pageRight + pad, pageBottom + pad), new PointD(pageLeft - pad, pageBottom + pad) }
            .Select(q => DevelopableSurface.RulingCoordinates(parameters, q).Across).ToList();
        _tableStart = Math.Floor(across.Min());
        int nodes = (int)Math.Ceiling((across.Max() - _tableStart) / TableStep) + 2;
        _x = new double[nodes];
        _z = new double[nodes];
        _slopeX = new double[nodes];
        _slopeZ = new double[nodes];
        for (int i = 0; i < nodes; i++)
        {
            double t = _tableStart + (i * TableStep);
            (_x[i], _z[i]) = DevelopableSurface.Profile(parameters.Bend, t);
            double angle = DevelopableSurface.TangentAngle(parameters.Bend, t);
            _slopeX[i] = Math.Cos(angle);
            _slopeZ[i] = Math.Sin(angle);
        }

        var page = new List<PointD>();
        var image = new List<PointD>();
        for (int i = 0; i <= 4; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                var p = new PointD(pageLeft + ((pageRight - pageLeft) * i / 4), pageTop + ((pageBottom - pageTop) * j / 4));
                var q = ToImage(p);
                if (!double.IsNaN(q.X))
                {
                    page.Add(p);
                    image.Add(q);
                }
            }
        }

        _approximateToPage = HomographyEstimate.Fit(image, page) ?? Homography.Identity;
    }

    public SurfaceModel Parameters { get; }

    public string Model => (Parameters.Family == SurfaceFamily.General ? "general developable surface" : "generalised cylinder")
        + (Parameters.Projection == SurfaceProjection.Perspective ? " through a camera with radial distortion" : ", orthographic");

    public PointD ToImage(PointD page)
    {
        if (_folded is not null)
        {
            return DevelopableSurface.Distort(Parameters, DevelopableSurface.Project(Parameters, _rotation, _folded.Sheet(page)));
        }

        double dx = page.X - Parameters.PageCentreX, dy = page.Y - Parameters.PageCentreY;
        double along = (dx * _rulingCos) + (dy * _rulingSin), across = (-dx * _rulingSin) + (dy * _rulingCos);
        double x, z;
        double u = (across - _tableStart) / TableStep;
        int i = (int)Math.Floor(u);
        if (i >= 0 && i + 1 < _x.Length)
        {
            double s = u - i, s2 = s * s, s3 = s2 * s;
            double h00 = (2 * s3) - (3 * s2) + 1, h10 = s3 - (2 * s2) + s, h01 = (-2 * s3) + (3 * s2), h11 = s3 - s2;
            x = (h00 * _x[i]) + (h10 * TableStep * _slopeX[i]) + (h01 * _x[i + 1]) + (h11 * TableStep * _slopeX[i + 1]);
            z = (h00 * _z[i]) + (h10 * TableStep * _slopeZ[i]) + (h01 * _z[i + 1]) + (h11 * TableStep * _slopeZ[i + 1]);
        }
        else
        {
            (x, z) = DevelopableSurface.Profile(Parameters.Bend, across);
        }

        var sheet = ((along * _rulingCos) - (x * _rulingSin), (along * _rulingSin) + (x * _rulingCos), z);
        return DevelopableSurface.Distort(Parameters, DevelopableSurface.Project(Parameters, _rotation, sheet));
    }

    public PointD ToPage(PointD image)
    {
        var p = _approximateToPage.Apply(image);
        const double h = 0.5;
        for (int i = 0; i < 40; i++)
        {
            var q = ToImage(p);
            double ex = image.X - q.X, ey = image.Y - q.Y;
            if (double.IsNaN(ex) || ((ex * ex) + (ey * ey)) < 1e-20)
            {
                break;
            }

            var qx = ToImage(new PointD(p.X + h, p.Y));
            var qy = ToImage(new PointD(p.X, p.Y + h));
            double a = (qx.X - q.X) / h, b = (qy.X - q.X) / h, c = (qx.Y - q.Y) / h, d = (qy.Y - q.Y) / h;
            double det = (a * d) - (b * c);
            if (det == 0 || double.IsNaN(det))
            {
                break;
            }

            double dx = ((d * ex) - (b * ey)) / det, dy = ((a * ey) - (c * ex)) / det;
            p = new PointD(p.X + dx, p.Y + dy);
            if (Math.Abs(dx) + Math.Abs(dy) < 1e-10)
            {
                break;
            }
        }

        return p;
    }

    public (double XX, double XY, double YX, double YY) Jacobian(PointD image)
    {
        const double h = 0.5;
        var px = ToPage(new PointD(image.X + h, image.Y));
        var mx = ToPage(new PointD(image.X - h, image.Y));
        var py = ToPage(new PointD(image.X, image.Y + h));
        var my = ToPage(new PointD(image.X, image.Y - h));
        return ((px.X - mx.X) / (2 * h), (py.X - my.X) / (2 * h), (px.Y - mx.Y) / (2 * h), (py.Y - my.Y) / (2 * h));
    }
}

/// <summary>A least-squares homography from point pairs, with Hartley normalisation, for starting values rather than for measurement.</summary>
public static class HomographyEstimate
{
    public static Homography? Fit(IReadOnlyList<PointD> source, IReadOnlyList<PointD> target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        if (source.Count != target.Count || source.Count < 4)
        {
            return null;
        }

        var (ts, s) = Normalise(source);
        var (tt, t) = Normalise(target);
        var normal = new double[8, 8];
        var rhs = new double[8];
        for (int i = 0; i < s.Length; i++)
        {
            double x = s[i].X, y = s[i].Y, u = t[i].X, v = t[i].Y;
            double[][] rows = [[x, y, 1, 0, 0, 0, -u * x, -u * y], [0, 0, 0, x, y, 1, -v * x, -v * y]];
            double[] values = [u, v];
            for (int k = 0; k < 2; k++)
            {
                for (int a = 0; a < 8; a++)
                {
                    rhs[a] += rows[k][a] * values[k];
                    for (int b = 0; b < 8; b++)
                    {
                        normal[a, b] += rows[k][a] * rows[k][b];
                    }
                }
            }
        }

        if (LinearSolve.Solve(normal, rhs) is not { } h)
        {
            return null;
        }

        var hn = new Homography([h[0], h[1], h[2], h[3], h[4], h[5], h[6], h[7], 1]);
        return Homography.Compose(Homography.Compose(ts, hn), tt.Inverse());
    }

    private static (Homography Transform, PointD[] Points) Normalise(IReadOnlyList<PointD> points)
    {
        double mx = points.Average(p => p.X), my = points.Average(p => p.Y);
        double mean = points.Average(p => Math.Sqrt(Math.Pow(p.X - mx, 2) + Math.Pow(p.Y - my, 2)));
        double k = mean > 0 ? Math.Sqrt(2) / mean : 1;
        var transform = new Homography([k, 0, -k * mx, 0, k, -k * my, 0, 0, 1]);
        return (transform, [.. points.Select(transform.Apply)]);
    }
}
