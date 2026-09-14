using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 17 section 4: with too few corners to test the bend, <see cref="SurfaceSelection"/> prefers
/// the model with fewer parameters, the plane, and never the bend by default.
/// </summary>
public class SurfaceSelectionTests
{
    private const int Width = 3000, Height = 4000;
    private const double PageWidth = 2159, PageHeight = 2794;

    [Theory]
    [InlineData(7, true)]
    [InlineData(5, true)]
    [InlineData(3, false)]
    public void FewerThanEightKeptCornersSelectThePlane(int kept, bool planeFitted)
    {
        double curvature = 8 * 0.25 * 254 / (PageWidth * PageWidth);
        var truth = SyntheticSurface.Camera(Width, Height, 2600, -0.05, 0.065, 2600, 8 * Math.PI / 180, PageWidth, PageHeight, Math.PI / 2, [0, curvature * SurfaceModel.BendLength, 0, 0]);
        var matches = SyntheticSurface.Corners(BuiltIns.Load("GL-CF25-LTR.gltd.json"), truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, new Random(17), Width, Height);
        var image = matches.SelectMany(m => m.ImageCorners).ToList();
        var page = matches.SelectMany(m => m.PageCorners).ToList();
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("few", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, 2556, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);
        var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];

        // Keep corners spread over the sheet: every seventeenth.
        var few = fit with { Kept = [.. image.Select((_, i) => i % 17 == 0 && i / 17 < kept)] };
        var choice = SurfaceSelection.Choose(few, image, page, Width, Height);

        Assert.Equal(kept, few.Kept.Count(k => k));
        Assert.False(choice.PreferSurface);
        Assert.Equal(planeFitted, choice.Planar is not null);
    }
}
