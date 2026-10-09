using GroupLab.Cli;
using GroupLab.Core.Analysis;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143 and question 44: the bent-sheet model threw on one photograph, and entry 392 section 3 fixed it.
/// <para>
/// <c>20260920_153336.jpg</c> under <c>compare-photos --model surface</c> threw <c>System.IndexOutOfRangeException</c> from
/// <c>ExpectedImage.Render</c>'s <c>mapping.ToPage</c>. <b>The cause, found with a build that inlines nothing:</b> the photograph holds more
/// than one sheet, so some of the pixels <c>ExpectedImage.Render</c> asks about are far from this sheet, and <see cref="SurfaceMapping.ToPage"/>'s
/// Newton steps for them run far off the page. <see cref="SurfaceMapping.ToImage"/> then looked the cross-section up in its table with
/// <c>(int)Math.Floor(u)</c>; past <c>int.MaxValue</c> the conversion saturates, <c>i + 1</c> wrapped negative, and the bounds test let it
/// through. The fold arrays question 44 suspected were never involved: this photograph's model is the generalized cylinder.
/// </para>
/// <para>
/// <b>The fix is the smaller honest one of the two the entry named</b>: the lookup is clamped, its range tested before the conversion, so a
/// point beyond the table is computed exactly instead, as it always was when the conversion did not wrap. The Newton iteration is left as
/// it is: where it lands for a pixel off the sheet does not matter, because the sheet's mask removes those pixels before anything is read.
/// </para>
/// </summary>
public class SurfaceCrashTests
{
    private const string Folder = @"C:\Dev\grouplab-range-2026-09-20\photos";

    /// <summary>The one photograph of the fifteen that threw. Named here so the next person starts from the file rather than the trace.</summary>
    private const string TheOne = "20260920_153336.jpg";

    private const double PageWidth = 2159, PageHeight = 2794;

    /// <summary>
    /// Runs everywhere: a page point so far off the page that its table index passes <c>int.MaxValue</c>, which threw before entry 392, and
    /// an image point far outside the frame, whose Newton steps go there. Each gives a number or NaN, and the far page point gives what the
    /// model computed directly gives.
    /// </summary>
    [Fact]
    public void APointFarOffThePageIsComputedRatherThanIndexed()
    {
        var model = SyntheticSurface.Camera(3000, 4000, 2600, -0.05, 0.065, 2600, 8 * Math.PI / 180, PageWidth, PageHeight, 1.2, [0, 0.4, -0.1, 0.05]);
        var mapping = new SurfaceMapping(model, 0, 0, PageWidth, PageHeight);

        foreach (var far in (PointD[])[new(1e13, 1e13), new(-1e13, 4e12), new(3e12, -1e13)])
        {
            var image = mapping.ToImage(far);
            var direct = DevelopableSurface.ToImage(model, far);
            Assert.Equal(double.IsNaN(direct.X), double.IsNaN(image.X));
        }

        foreach (var outside in (PointD[])[new(1e7, 1e7), new(-1e6, 2e6), new(5e8, -5e8)])
        {
            var page = mapping.ToPage(outside);
            Assert.False(double.IsInfinity(page.X) || double.IsInfinity(page.Y), $"({outside.X}, {outside.Y}) went to an infinite page point");
        }
    }

    /// <summary>
    /// The photograph itself, where it is on this machine: the surface model fits it, maps its markers back, and the whole reading, the call
    /// that threw included, now completes and finds the sheet's holes. Its answer is bounded by the plain model's: on 2026-10-09 the surface
    /// model put the bull centres at a median of 0.0050 in from the scan's against the homography's 0.0110, and matched 18 of the scan's 19 holes.
    /// </summary>
    [Fact]
    public void TheSurfaceModelReadsTheCrashingPhotographToTheEnd()
    {
        string path = Path.Combine(Folder, TheOne);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not on this machine, so the crashing photograph was not re-measured");
            return;
        }

        string readable = GroupLab.Tests.Support.Temp.Readable(path);
        var (image, metadata) = GroupLab.Cli.Imaging.ImageLoader.Load(readable);
        var (value, _) = GroupLab.Cli.Imaging.ImageLoader.LoadMaxChannel(readable);
        var backend = new GroupLab.Cli.Imaging.OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(image, SheetIdentification.Candidates([Repo.PathTo("targets")]), backend, new Core.Trace.TraceRecorder());
        Assert.NotNull(identity.Definition);

        var measured = SheetMeasurer.Measure(image, metadata, identity.Definition!, new MeasureOptions(Model: RegistrationModel.Surface), backend);
        Assert.Null(measured.Failure);
        Assert.NotNull(measured.Registration);
        Assert.IsType<SurfaceMapping>(measured.Registration!.Mapping);
        foreach (var corner in measured.Registration.Corners)
        {
            var back = measured.Registration.Mapping.ToPage(corner.Image);
            Assert.False(double.IsNaN(back.X), $"{TheOne}: the inverse gave nothing back at marker {corner.MarkerId} corner {corner.Corner}");
        }

        var result = AutomaticMarking.Run(image, value, metadata, identity.Definition!, backend, options: new MeasureOptions(Model: RegistrationModel.Surface));
        Assert.Null(result.Failure);
        Assert.InRange(result.Detections.Count, 15, 30);
    }
}
