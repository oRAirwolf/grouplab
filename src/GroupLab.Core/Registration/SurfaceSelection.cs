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
/// </summary>
public static class SurfaceSelection
{
    public static SurfaceChoice Choose(SurfaceFrameResult fit, IReadOnlyList<PointD> image, IReadOnlyList<PointD> page, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(fit);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(page);
        var keptImage = image.Where((_, i) => fit.Kept[i]).ToList();
        var keptPage = page.Where((_, i) => fit.Kept[i]).ToList();
        bool perspective = fit.Model.Projection == SurfaceProjection.Perspective;
        int surfaceParameters = perspective ? 14 : 10, extra = perspective ? 4 : 2;
        double critical = perspective ? 18.467 / 4 : 13.816 / 2;
        if (keptImage.Count < 8 || HomographyEstimate.Fit(keptImage, keptPage) is not { } homography)
        {
            return new SurfaceChoice(null, double.NaN, double.NaN, double.NaN, critical, true);
        }

        IPageMapping planar = perspective ? LensFit.Fit(keptImage, keptPage, homography, width, height) : new HomographyMapping(homography);
        double planarSum = keptImage.Select((p, i) => Squared(planar.ToPage(p), keptPage[i])).Sum();
        double surfaceSum = fit.PageErrors.Where((_, i) => fit.Kept[i]).Sum(e => e * e);
        int residuals = 2 * keptImage.Count;
        double f = (planarSum - surfaceSum) / extra / (surfaceSum / Math.Max(1, residuals - surfaceParameters));
        return new SurfaceChoice(planar, planarSum, surfaceSum, f, critical, f > critical);
    }

    private static double Squared(PointD a, PointD b) => Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2);
}
