using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// The non-developable shape a surface fit left behind, as offsets along the fitted sheet's normal:
/// <see cref="AlongDmm"/> of <c>(along / L)^2</c>, curvature along the rulings, and <see cref="SaddleDmm"/> of
/// <c>(along / L)(across / L)</c>, a twist of the rulings, with L the <see cref="SurfaceModel.BendLength"/>. Each has its t
/// statistic; <see cref="F"/> tests the two together against no leftover shape; <see cref="CornerOffsetDmm"/> is their sum
/// at the page corner where it is largest, and <see cref="VarianceExplained"/> the fraction of the corners' page residual
/// they account for.
/// </summary>
public sealed record TwistEstimate(double AlongDmm, double SaddleDmm, double AlongT, double SaddleT, double F, double CornerOffsetDmm, double VarianceExplained, int Corners);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 15 section 4: a generalised cylinder fitted to a twisted sheet leaves a residual that turns
/// systematically along the rulings. A developable sheet has no curvature along its rulings and no twist of them, so the
/// two second-order shapes it cannot take, in the fitted ruling coordinates, are <c>along^2</c> and <c>along x across</c>;
/// <c>across^2</c> is bend the fit already has. Both are needed. A synthetic twist is a saddle in the truth's rulings, and a
/// fit that turns its rulings 45 degrees absorbs half of it as bend and leaves the other half as curvature along its own
/// rulings, which a saddle term alone does not see. Each corner's page residual under the fitted mapping is regressed on
/// the page displacement each shape would produce there: the fitted sheet point moved one dmm along its normal, imaged,
/// and mapped back to the page.
/// <para>
/// It measures only what the fit left, and a free lens leaves little. On synthetic truth, a quarter inch of twist over a
/// quarter inch bow at 0.52 px of corner noise gives F 0.9 with the lens free, because k1, the focal length, the pose and
/// the ruling angle absorb it, and F 23.5 with the lens held at truth (<c>SurfaceLensTests</c>). Read it with the lens held.
/// <c>grouplab surface lens-sweep</c> reports the series.
/// </para>
/// </summary>
public static class SurfaceTwist
{
    /// <summary>The F statistic's critical value at p = 0.001 for two parameters against hundreds of residuals: the chi-square quantile over two.</summary>
    public const double CriticalF = 13.816 / 2;

    public static TwistEstimate Estimate(SurfaceFrame frame, SurfaceMapping mapping, IReadOnlyList<bool> use)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(use);
        var model = mapping.Parameters;
        double l2 = SurfaceModel.BendLength * SurfaceModel.BendLength;
        double s11 = 0, s12 = 0, s22 = 0, r1 = 0, r2 = 0, srr = 0;
        int n = 0;
        for (int i = 0; i < frame.Image.Count; i++)
        {
            if (!use[i])
            {
                continue;
            }

            var page = frame.Page[i];
            var mapped = mapping.ToPage(frame.Image[i]);
            double rx = mapped.X - page.X, ry = mapped.Y - page.Y;
            var (along, across) = DevelopableSurface.RulingCoordinates(model, page);
            var sheet = DevelopableSurface.Sheet(model, page);
            var normal = Normal(model, page);
            var at = mapping.ToPage(DevelopableSurface.Distort(model, DevelopableSurface.Project(model, sheet)));
            var off = mapping.ToPage(DevelopableSurface.Distort(model, DevelopableSurface.Project(model, (sheet.X + normal.X, sheet.Y + normal.Y, sheet.Z + normal.Z))));
            double gx = off.X - at.X, gy = off.Y - at.Y, b1 = along * along / l2, b2 = along * across / l2;
            if (!double.IsFinite(rx) || !double.IsFinite(ry) || !double.IsFinite(gx) || !double.IsFinite(gy))
            {
                continue;
            }

            double gg = (gx * gx) + (gy * gy), gr = (gx * rx) + (gy * ry);
            s11 += b1 * b1 * gg;
            s12 += b1 * b2 * gg;
            s22 += b2 * b2 * gg;
            r1 += b1 * gr;
            r2 += b2 * gr;
            srr += (rx * rx) + (ry * ry);
            n++;
        }

        double det = (s11 * s22) - (s12 * s12);
        if (n < 4 || !(det > 0))
        {
            return new TwistEstimate(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, n);
        }

        double along2 = ((s22 * r1) - (s12 * r2)) / det, saddle = ((s11 * r2) - (s12 * r1)) / det;
        double sse = Math.Max(0, srr - (along2 * r1) - (saddle * r2));
        double sigma2 = sse / Math.Max(1, (2 * n) - 2);
        double f = (srr - sse) / 2 / sigma2;
        var corners = new[] { new PointD(frame.PageLeft, frame.PageTop), new PointD(frame.PageRight, frame.PageTop), new PointD(frame.PageRight, frame.PageBottom), new PointD(frame.PageLeft, frame.PageBottom) }
            .Select(p => DevelopableSurface.RulingCoordinates(model, p))
            .Select(c => ((along2 * c.Along * c.Along) + (saddle * c.Along * c.Across)) / l2)
            .MaxBy(Math.Abs);
        return new TwistEstimate(along2, saddle, along2 / Math.Sqrt(sigma2 * s22 / det), saddle / Math.Sqrt(sigma2 * s11 / det), f, corners, srr > 0 ? 1 - (sse / srr) : double.NaN, n);
    }

    /// <summary>The unit normal of the fitted sheet at a page point, pointing out of the page as +z does on a flat sheet.</summary>
    private static (double X, double Y, double Z) Normal(SurfaceModel model, PointD page)
    {
        var ax = DevelopableSurface.Sheet(model, new PointD(page.X + 1, page.Y));
        var bx = DevelopableSurface.Sheet(model, new PointD(page.X - 1, page.Y));
        var ay = DevelopableSurface.Sheet(model, new PointD(page.X, page.Y + 1));
        var by = DevelopableSurface.Sheet(model, new PointD(page.X, page.Y - 1));
        double ux = ax.X - bx.X, uy = ax.Y - bx.Y, uz = ax.Z - bx.Z, vx = ay.X - by.X, vy = ay.Y - by.Y, vz = ay.Z - by.Z;
        double nx = (uy * vz) - (uz * vy), ny = (uz * vx) - (ux * vz), nz = (ux * vy) - (uy * vx);
        double length = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        return (nx / length, ny / length, nz / length);
    }
}
