using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>The planar fit a surface fit was compared with, both residuals, the F statistic and the choice.</summary>
public sealed record SurfaceChoice(IPageMapping? Planar, double PlanarSumSquares, double SurfaceSumSquares, double F, double CriticalF, bool PreferSurface);

/// <summary>
/// Whether a bend is supported by the corners, decided before any real frame was fitted (PHASE1-BRIEF.md section 3.3,
/// PHASE1-RESULTS.md M1). The synthetic sweep showed the generalised cylinder fitting bend to corner noise on a flat sheet,
/// doubling the worst bull against the Phase 0 model, 0.00217 in median against 0.00108, with the extrapolated sighters
/// taking most of it; section 3.4 of the brief requires a flat sheet not to get worse. So the bend is kept only when it
/// earns its extra parameters: an F test of the page-space residual sum of squares over the corners the surface fit kept,
/// against the planar model refitted to those same corners, at p = 0.001. A photograph's planar model is the Phase 0
/// homography with radial distortion, ten parameters against the surface's fourteen; a scan's is a homography, eight
/// against the orthographic surface's ten. The critical value is the chi-square quantile over its degrees of freedom,
/// which is the F quantile for the hundreds of residuals a sheet provides.
/// <para>
/// With the lens held (<see cref="SurfaceHold.Distortion"/>, NOTES-FROM-PLANNING.md entry 15 section 4) the planar model
/// holds the same lens: a homography from the surface's undistorted normalised coordinates to the page, eight parameters,
/// refined on the page residual as the Phase 0 lens fit is; and the surface loses its two lens parameters, and its focal
/// length when that is held too. The bend is still four extra parameters.
/// </para>
/// <para>
/// A general developable surface (<see cref="SurfaceFamily.General"/>) adds its turn coefficients to the bend's extra
/// parameters, six for a photograph, and the critical value follows the degrees of freedom.
/// </para>
/// </summary>
public static class SurfaceSelection
{
    public static SurfaceChoice Choose(SurfaceFrameResult fit, IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, int width, int height, SurfaceHold hold = SurfaceHold.None)
    {
        ArgumentNullException.ThrowIfNull(fit);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        var keptImage = image.Where((_, i) => fit.Kept[i]).ToList();
        var keptPage = page.Where((_, i) => fit.Kept[i]).ToList();
        bool perspective = fit.Model.Projection == SurfaceProjection.Perspective;
        bool heldLens = perspective && hold.HasFlag(SurfaceHold.Distortion);
        int turn = fit.Model.Family == SurfaceFamily.General ? fit.Model.Turn?.Count ?? 0 : 0;
        int surfaceParameters = (perspective ? 14 : 10) + turn - (heldLens ? 2 : 0) - (perspective && hold.HasFlag(SurfaceHold.Focal) ? 1 : 0), extra = (perspective ? 4 : 2) + turn;

        // The chi-square quantile at p = 0.001 over the degrees of freedom the bend adds.
        double critical = extra switch { 2 => 13.816 / 2, 4 => 18.467 / 4, 6 => 22.458 / 6, _ => 26.124 / 8 };
        if (keptImage.Count < 8 || HomographyEstimate.Fit(keptImage, keptPage) is not { } homography)
        {
            return new SurfaceChoice(null, double.NaN, double.NaN, double.NaN, critical, true);
        }

        IPageMapping? planar = !perspective ? new HomographyMapping(homography)
            : heldLens ? HeldLensPlane(fit.Model, keptImage, keptPage)
            : LensFit.Fit(keptImage, keptPage, homography, width, height);
        if (planar is null)
        {
            return new SurfaceChoice(null, double.NaN, double.NaN, double.NaN, critical, true);
        }
        double planarSum = keptImage.Select((p, i) => Squared(planar.ToPage(p), keptPage[i])).Sum();
        double surfaceSum = fit.PageErrors.Where((_, i) => fit.Kept[i]).Sum(e => e * e);
        int residuals = 2 * keptImage.Count;
        double f = (planarSum - surfaceSum) / extra / (surfaceSum / Math.Max(1, residuals - surfaceParameters));
        return new SurfaceChoice(planar, planarSum, surfaceSum, f, critical, f > critical);
    }

    /// <summary>The Phase 0 photograph model with the surface's lens held: a homography from undistorted normalised coordinates to the page.</summary>
    private static RadialHomographyMapping? HeldLensPlane(SurfaceModel model, List<PointD> image, List<PointD> page)
    {
        var u = image.Select(q => DevelopableSurface.Undistort(model, q)).ToList();
        if (HomographyEstimate.Fit(u, page) is not { } start)
        {
            return null;
        }

        static Homography H(double[] x) => new([x[0], x[1], x[2], x[3], x[4], x[5], x[6], x[7], 1]);
        double[] x0 = [.. Enumerable.Range(0, 8).Select(k => start[k / 3, k % 3] / start[2, 2])];
        double Residuals(double[] x, double[] r)
        {
            var h = H(x);
            double cost = 0;
            for (int i = 0; i < u.Count; i++)
            {
                var p = h.Apply(u[i]);
                r[2 * i] = p.X - page[i].X;
                r[(2 * i) + 1] = p.Y - page[i].Y;
                cost += (r[2 * i] * r[2 * i]) + (r[(2 * i) + 1] * r[(2 * i) + 1]);
            }

            return cost;
        }

        var result = LevenbergMarquardt.Minimise(Residuals, x0, 2 * u.Count, [.. x0.Select(v => Math.Max(1e-9, Math.Abs(v) * 1e-7))]);
        return new RadialHomographyMapping(model.CentreX, model.CentreY, model.Scale, model.K1, model.K2, H(result.Parameters));
    }

    private static double Squared(PointD a, PointD b) => Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2);
}
