using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace GroupLab.Core.Tests.Registration;

/// <summary>
/// The general developable surface of NOTES-FROM-PLANNING.md entry 16 section 5 against known truth, before any real frame,
/// as the cylinder was (<see cref="DevelopableSurfaceTests"/>): it is the cylinder when its rulings do not turn, it keeps
/// lengths when they do, its mapping inverts, it refuses rulings that cross on the page, and it recovers a cone.
/// </summary>
public class GeneralDevelopableTests(ITestOutputHelper output)
{
    private const int Width = 3000, Height = 4000;
    private const double PageWidth = 2159, PageHeight = 2794;

    /// <summary>The photograph gate, 0.005 in, in dmm.</summary>
    private const double PhotographGate = SyntheticScanCheck.Gate * 5;

    [Fact]
    public void WithNoTurnTheFoldedSheetIsTheCylinder()
    {
        var cylinder = Truth(0.4, [0.3, 0.25, -0.2, 0.15]);
        var general = cylinder with { Family = SurfaceFamily.General, Turn = [0.0, 0.0] };
        double worst = 0;
        for (double x = 0; x <= PageWidth; x += 53.7)
        {
            for (double y = 0; y <= PageHeight; y += 61.3)
            {
                var a = DevelopableSurface.Sheet(cylinder, new PointD(x, y));
                var b = DevelopableSurface.Sheet(general, new PointD(x, y));
                worst = Math.Max(worst, Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2)));
            }
        }

        output.WriteLine($"largest gap between the folded sheet and the cylinder: {worst:0.000000} dmm");
        Assert.True(worst < 0.05, $"{worst} dmm");
    }

    [Fact]
    public void AStraightPageLineKeepsItsLengthOnATurnedSheet()
    {
        var model = Truth(0.4, [0.1, 0.4, -0.2, 0.1]) with { Family = SurfaceFamily.General, Turn = [0.2, 0.05] };
        Assert.True(FoldedSheet.For(model).Valid);
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
        double deflection = DevelopableSurface.Deflection(model, PageWidth, PageHeight);
        output.WriteLine($"{length:0.0000} dmm on the sheet against {page:0.0000} on the page, deflection {deflection / 254:0.000} in");
        Assert.True(Math.Abs(length - page) < 0.01, $"{length} dmm on the sheet against {page} dmm on the page");
        Assert.True(deflection > 50, "the test sheet is meant to be bent");
    }

    [Fact]
    public void PageToImageAndBackReturnsThePagePointOnATurnedSheet()
    {
        var model = Truth(1.2, [0, 0.4, -0.1, 0.05]) with { Family = SurfaceFamily.General, Turn = [0.15, -0.05] };
        var mapping = new SurfaceMapping(model, 0, 0, PageWidth, PageHeight);
        foreach (var p in (PointD[])[new(0, 0), new(1080, 1397), new(2159, 2794), new(300, 2500), new(1900, 150)])
        {
            var image = mapping.ToImage(p);
            var back = mapping.ToPage(image);
            Assert.True(Math.Sqrt(Math.Pow(back.X - p.X, 2) + Math.Pow(back.Y - p.Y, 2)) < 1e-6, $"({p.X}, {p.Y}) came back as ({back.X}, {back.Y})");
        }
    }

    [Fact]
    public void RulingsThatCrossInsideThePageMakeTheModelInvalid()
    {
        var model = Truth(0.4, [0, 0.3, 0, 0]) with { Family = SurfaceFamily.General, Turn = [3.0, 0] };
        Assert.False(FoldedSheet.For(model).Valid);
        Assert.True(double.IsNaN(DevelopableSurface.Sheet(model, new PointD(1080, 1397)).X));
    }

    [Fact]
    public void AConesBullsAreRecoveredByTheGeneralSurface()
    {
        double sag = 0.25 * 254, curvature = 8 * sag / (PageWidth * PageWidth);
        var truth = Truth(Math.PI / 2, [0, curvature * SurfaceModel.BendLength, 0, 0]) with { Family = SurfaceFamily.General, Turn = [0.3, 0] };
        var definition = BuiltIns.Load("GL-CF25-LTR.gltd.json");
        var matches = SyntheticSurface.Corners(definition, truth, 0, SyntheticSurface.MeasuredCornerNoisePixels, new Random(20260914), Width, Height);
        var image = matches.SelectMany(m => m.ImageCorners).ToList();
        var page = matches.SelectMany(m => m.PageCorners).ToList();
        var lens = LensFit.Fit(image, page, HomographyEstimate.Fit(image, page)!, Width, Height);
        var frame = new SurfaceFrame("cone", image, page, [.. image.Select(_ => true)], SurfaceFit.StartFromLens(lens, 2556, PageWidth / 2, PageHeight / 2), 0, 0, PageWidth, PageHeight);

        var cylinder = SurfaceFit.Fit([frame], shareCamera: false)[0];
        var general = SurfaceFit.Fit([frame], shareCamera: false, SurfaceHold.None, SurfaceFamily.General)[0];

        double Worst(IPageMapping mapping) => definition.Bulls.Max(b =>
        {
            var declared = new PointD(b.X, b.Y);
            var back = mapping.ToPage(SyntheticSurface.Image(truth, 0, PageWidth, PageHeight, declared));
            return Math.Sqrt(Math.Pow(back.X - declared.X, 2) + Math.Pow(back.Y - declared.Y, 2));
        });
        double cylinderWorst = Worst(cylinder.Mapping), generalWorst = Worst(general.Mapping);
        output.WriteLine($"cone: cylinder worst bull {cylinderWorst / 254:0.00000} in, {cylinder.Kept.Count(k => k)} corners kept; general {generalWorst / 254:0.00000} in, {general.Kept.Count(k => k)} kept, turn {general.Model.Turn![0]:0.000} / {general.Model.Turn[1]:0.000} against 0.3 / 0");
        Assert.True(generalWorst < PhotographGate, $"{generalWorst / 254} in");
        Assert.True(generalWorst < cylinderWorst);
    }

    /// <summary>The main camera of <see cref="DevelopableSurfaceTests"/>.</summary>
    private static SurfaceModel Truth(double rulingAngle, double[] bend) =>
        SyntheticSurface.Camera(Width, Height, 2600, -0.05, 0.065, 2600, 8 * Math.PI / 180, PageWidth, PageHeight, rulingAngle, bend);
}
