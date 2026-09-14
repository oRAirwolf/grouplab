using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// The generalised cylinder of PHASE1-BRIEF.md section 3.1 against known truth, built before any real frame is touched,
/// per section 3.3. Truth is the main camera of the Phase 0 photographs: 3000 by 4000, a focal length near the EXIF
/// estimate but not at it, and the lens the flat frames fitted.
/// </summary>
public class DevelopableSurfaceTests(ITestOutputHelper output)
{
    private const int Width = 3000, Height = 4000;
    private const double PageWidth = 2159, PageHeight = 2794;

    /// <summary>PHASE1-BRIEF.md section 3.2's starting estimate: 23 mm equivalent over 36 mm, times the long side.</summary>
    private const double ExifFocalPixels = 4000 * 23 / 36.0;

    [Fact]
    public void AStraightPageLineKeepsItsLengthOnTheBentSheet()
    {
        var model = Truth(0.4, [0.3, 0.25, -0.2, 0.15]);
        PointD a = new(150, 200), b = new(2000, 2600);
        const int samples = 4000;
        double length = 0;
        var previous = DevelopableSurface.Sheet(model, a);
        for (int i = 1; i <= samples; i++)
        {
            var next = DevelopableSurface.Sheet(model, new PointD(a.X + ((b.X - a.X) * i / samples), a.Y + ((b.Y - a.Y) * i / samples)));
            length += Math.Sqrt(Math.Pow(next.X - previous.X, 2) + Math.Pow(next.Y - previous.Y, 2) + Math.Pow(next.Z - previous.Z, 2));
            previous = next;
        }

        double page = Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
        Assert.True(Math.Abs(length - page) < 1e-4, $"{length} dmm on the sheet against {page} dmm on the page");
        Assert.True(DevelopableSurface.Deflection(model, PageWidth, PageHeight) > 50, "the test sheet is meant to be bent");
    }

    [Fact]
    public void PageToImageAndBackReturnsThePagePoint()
    {
        var mapping = new SurfaceMapping(Truth(1.2, [0, 0.4, -0.1, 0.05]), 0, 0, PageWidth, PageHeight);
        foreach (var p in (PointD[])[new(0, 0), new(1080, 1397), new(2159, 2794), new(300, 2500), new(1900, 150)])
        {
            var back = mapping.ToPage(mapping.ToImage(p));
            Assert.True(Distance(back, p) < 1e-6, $"({p.X}, {p.Y}) came back as ({back.X}, {back.Y})");
        }
    }

    /// <summary>
    /// At the true focal length the starting pose is the lens fit's plane exactly. At the EXIF estimate it cannot be: a tilted
    /// plane's homography carries the focal length, and the wrong one leaves the pose's two in-plane axes not quite
    /// perpendicular, which is why the fit refines focal length rather than taking the estimate.
    /// </summary>
    [Fact]
    public void TheStartingPoseReproducesThePhase0LensFit()
    {
        var truth = Truth(0, [0, 0, 0, 0]);
        var (image, page) = Corners(truth, 0, 0, 1);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);

        var exact = SurfaceFit.StartFromLens(lens, truth.FocalPixels, PageWidth / 2, PageHeight / 2);
        var estimated = SurfaceFit.StartFromLens(lens, ExifFocalPixels, PageWidth / 2, PageHeight / 2);

        Assert.All(page, p => Assert.True(Distance(DevelopableSurface.ToImage(exact, p), lens.ToImage(p)) < 0.01));
        Assert.All(page, p => Assert.True(Distance(DevelopableSurface.ToImage(estimated, p), lens.ToImage(p)) < 2));
    }

    [Theory]
    [InlineData(0.0, 0.00025)]
    [InlineData(SyntheticSurface.MeasuredCornerNoisePixels, SyntheticScanCheck.Gate / 254 * 5)]
    public void ABentSheetsBullsAreRecoveredFromItsCorners(double noise, double gateInches)
    {
        // A bow of about a quarter inch across the page, between two vertical fixings: rulings along the page's y axis,
        // constant curvature.
        double sag = 0.25 * 254, curvature = 8 * sag / (PageWidth * PageWidth);
        var truth = Truth(Math.PI / 2, [0, curvature * SurfaceModel.BendLength, 0, 0]);
        var (image, page) = Corners(truth, 0, noise, 20260914);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("synthetic", image, page, [.. image.Select(_ => true)],
            SurfaceFit.StartFromLens(lens, ExifFocalPixels, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);

        var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];

        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var errors = definition.Bulls.Select(b =>
        {
            var at = SyntheticSurface.Image(truth, 0, PageWidth, PageHeight, new PointD(b.X, b.Y));
            return (Surface: Distance(fit.Mapping.ToPage(at), new PointD(b.X, b.Y)), Planar: Distance(lens.ToPage(at), new PointD(b.X, b.Y)));
        }).ToList();
        output.WriteLine($"noise {noise} px: true deflection {DevelopableSurface.Deflection(truth, PageWidth, PageHeight) / 254:0.0000} in, fitted {fit.DeflectionDmm / 254:0.0000} in; " +
            $"focal {fit.Model.FocalPixels:0} px against truth {truth.FocalPixels:0}; worst bull surface {errors.Max(e => e.Surface) / 254:0.00000} in, " +
            $"planar {errors.Max(e => e.Planar) / 254:0.00000} in; corners kept {fit.Kept.Count(k => k)} of {fit.Kept.Count}, rms {fit.RmsKept / 254:0.00000} in");

        Assert.True(errors.Max(e => e.Surface) / 254 < gateInches);
        Assert.True(errors.Max(e => e.Planar) > 3 * errors.Max(e => e.Surface), "a bow this size should defeat the planar model");
    }

    [Fact]
    public void AFlatSheetIsFittedFlat()
    {
        var truth = Truth(0, [0, 0, 0, 0]);
        var (image, page) = Corners(truth, 0, 0, 7);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("flat", image, page, [.. image.Select(_ => true)],
            SurfaceFit.StartFromLens(lens, ExifFocalPixels, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);

        var fit = SurfaceFit.Fit([frame], shareCamera: false)[0];

        output.WriteLine($"deflection {fit.DeflectionDmm / 254:0.000000} in, rms {fit.RmsKept / 254:0.0000000} in");
        Assert.True(fit.DeflectionDmm < 0.254, $"a flat sheet was fitted {fit.DeflectionDmm / 254:0.00000} in bent");
    }

    /// <summary>The main camera, 2600 px focal length against the EXIF estimate of 2556, looking at the page centre from 260 mm, tilted 8 degrees.</summary>
    private static SurfaceModel Truth(double rulingAngle, double[] bend) =>
        SyntheticSurface.Camera(Width, Height, 2600, -0.05, 0.065, 2600, 8 * Math.PI / 180, PageWidth, PageHeight, rulingAngle, bend);

    private static (List<PointD> Image, List<PointD> Page) Corners(SurfaceModel truth, double twist, double noise, int seed)
    {
        var matches = SyntheticSurface.Corners(BuiltIns.Load("GL-CF25-LTR.gltd.json"), truth, twist, noise, new Random(seed), Width, Height);
        return ([.. matches.SelectMany(m => m.ImageCorners)], [.. matches.SelectMany(m => m.PageCorners)]);
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
}
