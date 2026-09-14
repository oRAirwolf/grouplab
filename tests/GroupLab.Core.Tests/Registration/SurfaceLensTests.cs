using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 15 on synthetic truth: section 3 step 5's regression test for one starting focal length per
/// joint fit, and section 4's held lens and twist diagnostic, before either is pointed at a real frame. Truth is the
/// main camera of <see cref="DevelopableSurfaceTests"/>.
/// </summary>
public class SurfaceLensTests(ITestOutputHelper output)
{
    private const int Width = 3000, Height = 4000;
    private const double PageWidth = 2159, PageHeight = 2794, TruthFocal = 2600, K1 = -0.05, K2 = 0.065;

    /// <summary>The photograph gate, 0.005 in, in dmm: five times conformance test 43's, as <see cref="DevelopableSurfaceTests"/> uses.</summary>
    private const double PhotographGate = SyntheticScanCheck.Gate * 5;

    [Fact]
    public void FramesOfOneLensWithDisagreeingFocalTagsStartFromOneFocalLengthAndFitTogether()
    {
        // Two frames of one physical lens, 6.25 mm f/1.7, one tagged with the main camera's 23 mm equivalent and one with
        // the ultrawide's 13 mm: the Phase 0 table frames' disagreement (PHASE1-RESULTS.md M1.5).
        (string Name, SurfaceModel Truth, int Tag, int Seed)[] cases =
        [
            ("tagged 23 mm", Camera(2600, 8, Math.PI / 2, Bow(0.25)), 23, 11),
            ("tagged 13 mm", Camera(2800, -6, 80 * Math.PI / 180, Bow(0.15)), 13, 12),
        ];
        var frames = cases.Select(c =>
        {
            var (image, page) = Corners(c.Truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, c.Seed);
            var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
            var metadata = new ImageMetadata("synthetic", Width, Height, null, null, "synthetic", "camera", 1, 6.25, c.Tag, 1.7);
            Func<double, SurfaceFrame> at = f => new SurfaceFrame(c.Name, image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, f, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);
            return (c.Name, c.Truth, Metadata: metadata, At: at);
        }).ToList();

        var seed = SurfaceFit.SeedFocal([.. frames.Select(f => (f.Name, f.Metadata, Width, Height, f.At))])!;
        var fits = SurfaceFit.Fit([.. frames.Select(f => f.At(seed.FocalPixels))], shareCamera: true);

        Assert.Equal(2, seed.Candidates.Count);
        var disagreeing = frames.Where(f => Math.Round(SurfaceFit.FocalPixelsFromExif(f.Metadata, Width, Height)!.Value, 1) != seed.FocalPixels).Select(f => f.Name).ToList();
        var warned = Assert.Single(seed.Warnings);
        Assert.StartsWith(Assert.Single(disagreeing) + ":", warned, StringComparison.Ordinal);
        Assert.Equal(fits[0].Model.Focal, fits[1].Model.Focal);
        output.WriteLine($"start {seed.FocalPixels:0} px, shared focal {fits[0].Model.FocalPixels:0} px against truth {TruthFocal}");
        Assert.True(Math.Abs(fits[0].Model.FocalPixels - TruthFocal) / TruthFocal < 0.03);
        for (int f = 0; f < frames.Count; f++)
        {
            double worst = WorstBull(frames[f].Truth, 0, fits[f].Mapping);
            output.WriteLine($"{frames[f].Name}: worst bull {worst / 254:0.00000} in, deflection {fits[f].DeflectionDmm / 254:0.000} in");
            Assert.True(worst < PhotographGate, $"{frames[f].Name}: {worst / 254:0.00000} in");
        }
    }

    [Fact]
    public void AHeldLensStaysAtItsStartAndTheBendIsStillRecovered()
    {
        var truth = Camera(2600, 8, Math.PI / 2, Bow(0.25));
        var (image, page) = Corners(truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, 20260914);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var start = SurfaceFit.StartFromLens(lens, 2556, PageWidth / 2, PageHeight / 2) with { K1 = K1, K2 = K2 };
        var frame = new SurfaceFrame("held", image, page, [.. image.Select(_ => true)], start, 0, 0, PageWidth, PageHeight);

        var fit = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.Distortion)[0];
        var choice = SurfaceSelection.Choose(fit, image, page, Width, Height, SurfaceHold.Distortion);

        double worst = WorstBull(truth, 0, fit.Mapping);
        output.WriteLine($"worst bull {worst / 254:0.00000} in, deflection {fit.DeflectionDmm / 254:0.000} in, focal {fit.Model.FocalPixels:0} px, F {choice.F:0.0}");
        Assert.Equal(K1, fit.Model.K1);
        Assert.Equal(K2, fit.Model.K2);
        Assert.True(worst < PhotographGate);
        Assert.True(choice.PreferSurface);
        var planar = Assert.IsType<RadialHomographyMapping>(choice.Planar);
        Assert.Equal(K1, planar.K1);
    }

    [Fact]
    public void AFlatHoldFitsNoBend()
    {
        var truth = Camera(2600, 8, 0, [0, 0, 0, 0]);
        var (image, page) = Corners(truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, 5);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("flat", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, 2556, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);

        var fit = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.Flat)[0];

        output.WriteLine($"k1 {fit.Model.K1:0.0000} k2 {fit.Model.K2:0.0000} focal {fit.Model.FocalPixels:0} px");
        Assert.Equal(0, fit.Model.RulingAngle);
        Assert.All(fit.Model.Bend, b => Assert.Equal(0, b));
        Assert.Equal(0, fit.DeflectionDmm);
    }

    /// <summary>
    /// With the lens held, a quarter inch of twist over a quarter inch bow is found and noise alone is not. With the lens
    /// free the same twist is not found: measured before this test was written, k1 moves from -0.050 to -0.057, the rulings
    /// turn to 72 degrees, corner RMS rises only from 0.00287 to 0.00346 in, and F is 0.9. The free lens absorbs the twist,
    /// and holding it makes the worst bull 0.01219 in against 0.00517, so the diagnostic is read with the lens held.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    public void TheTwistDiagnosticFindsATwistWhenTheLensIsHeld(double twistInches)
    {
        var truth = Camera(2600, 8, Math.PI / 2, Bow(0.25));
        var (image, page) = Corners(truth, twistInches * 254, SyntheticSurface.MeasuredCornerNoisePixels, 3);
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var start = SurfaceFit.StartFromLens(lens, 2556, PageWidth / 2, PageHeight / 2) with { K1 = K1, K2 = K2 };
        var frame = new SurfaceFrame("twist", image, page, [.. image.Select(_ => true)], start, 0, 0, PageWidth, PageHeight);
        var fit = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.Distortion)[0];

        var twist = SurfaceTwist.Estimate(frame, fit.Mapping, [.. fit.PageErrors.Select(e => e <= SurfaceFit.MisreadThreshold)]);

        output.WriteLine($"twist {twistInches} in: fitted rulings at {fit.Model.RulingAngle * 180 / Math.PI:0.0} degrees, leftover at the corner {twist.CornerOffsetDmm / 254:+0.0000;-0.0000} in, t along {twist.AlongT:0.0}, t saddle {twist.SaddleT:0.0}, F {twist.F:0.0}, explains {twist.VarianceExplained:0.00}, {twist.Corners} corners");
        if (twistInches == 0)
        {
            Assert.True(twist.F < SurfaceTwist.CriticalF, $"F {twist.F}");
        }
        else
        {
            Assert.True(twist.F > 2 * SurfaceTwist.CriticalF, $"F {twist.F}");
        }
    }

    private static double[] Bow(double inches)
    {
        double sag = inches * 254, curvature = 8 * sag / (PageWidth * PageWidth);
        return [0, curvature * SurfaceModel.BendLength, 0, 0];
    }

    private static SurfaceModel Camera(double distance, double tiltDegrees, double rulingAngle, double[] bend) =>
        SyntheticSurface.Camera(Width, Height, TruthFocal, K1, K2, distance, tiltDegrees * Math.PI / 180, PageWidth, PageHeight, rulingAngle, bend);

    private static (List<PointD> Image, List<PointD> Page) Corners(SurfaceModel truth, double twist, double noise, int seed)
    {
        var matches = SyntheticSurface.Corners(BuiltIns.Load("GL-CF25-LTR.gltd.json"), truth, twist, noise, new Random(seed), Width, Height);
        return ([.. matches.SelectMany(m => m.ImageCorners)], [.. matches.SelectMany(m => m.PageCorners)]);
    }

    private static double WorstBull(SurfaceModel truth, double twist, IPageMapping mapping) =>
        BuiltIns.Load("GL-CF25-LTR.gltd.json").Bulls.Max(b =>
        {
            var declared = new PointD(b.X, b.Y);
            var back = mapping.ToPage(SyntheticSurface.Image(truth, twist, PageWidth, PageHeight, declared));
            return Math.Sqrt(Math.Pow(back.X - declared.X, 2) + Math.Pow(back.Y - declared.Y, 2));
        });
}
