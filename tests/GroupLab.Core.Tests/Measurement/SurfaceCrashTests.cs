using GroupLab.Cli;
using GroupLab.Core.Analysis;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Measurement;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 143, question 44: the bent-sheet model throws on one photograph.
/// <para>
/// <c>20260920_153336.jpg</c> under <c>compare-photos --model surface</c> throws
/// <c>System.IndexOutOfRangeException</c> at <c>ExpectedImage.Render</c>'s <c>mapping.ToPage</c> call. Entry 143 allows an hour on it and
/// then says to leave it with a test recording the photograph and a note, rather than a speculative fix. This is that test.
/// </para>
/// <para>
/// <b>What the hour bought, which is more than the stack trace said.</b> The leave-one-marker-out measurement of question 44 fitted this
/// photograph cleanly: 19 markers, a fit residual of 0.0047 in, and a held-out median of 0.0040 in, with the surface refitted nineteen times
/// over with a different marker held out each time. So the fit works here, and so does <see cref="SurfaceMapping.ToPage"/> at every marker
/// corner, nineteen times each.
/// </para>
/// <para>
/// <b>What that leaves.</b> The measurement never calls <c>ToPage</c> for every pixel of a bull's box, and <c>ExpectedImage.Render</c> does.
/// <c>ToPage</c> is a Newton iteration from a homography's guess, and nothing bounds where it may step on the way; a page point far outside
/// the sheet reaches <c>FoldedSheet.Sheet</c>, whose fold arrays are built to span the page and no further. So the fault is in the inverse at
/// a point outside the page, not in the fit.
/// </para>
/// <para>
/// <b>Why it is not fixed here.</b> The model is not reachable from the application: <c>Auto</c> never selects it and the window passes no
/// options, so it is a defect in a candidate rather than a live fault. And question 44's measurement says the candidate should not be
/// extended anyway, because the model already predicts a marker it has never seen as well as one it was fitted to.
/// </para>
/// </summary>
public class SurfaceCrashTests
{
    private const string Folder = @"C:\Dev\grouplab-range-2026-09-20\photos";

    /// <summary>The one photograph of the fifteen that throws. Named here so the next person starts from the file rather than the trace.</summary>
    private const string TheOne = "20260920_153336.jpg";

    /// <summary>
    /// The half that is proved, and the half the narrowing rests on: the surface model fits this photograph and maps its markers back to the
    /// page without throwing. If this ever starts throwing, the narrowing above is wrong and the fault is in the fit after all.
    /// </summary>
    [Fact]
    public void TheSurfaceModelFitsTheCrashingPhotographWithoutThrowing()
    {
        string path = Path.Combine(Folder, TheOne);
        if (!File.Exists(path))
        {
            Assert.True(true, $"skipped: {path} is not on this machine, so the crashing photograph was not re-measured");
            return;
        }

        var (image, metadata) = GroupLab.Cli.Imaging.ImageLoader.Load(GroupLab.Tests.Support.Temp.Readable(path));
        var backend = new GroupLab.Cli.Imaging.OpenCvSharpBackend();
        var identity = SheetIdentification.Identify(image, SheetIdentification.Candidates([Repo.PathTo("targets")]), backend, new Core.Trace.TraceRecorder());
        Assert.NotNull(identity.Definition);

        var measured = SheetMeasurer.Measure(image, metadata, identity.Definition!, new MeasureOptions(Model: RegistrationModel.Surface), backend);

        Assert.Null(measured.Failure);
        Assert.NotNull(measured.Registration);

        // Every marker corner maps back through the inverse. This is the call that throws inside ExpectedImage.Render, and here it does not,
        // because here it is only ever asked about points on the sheet.
        foreach (var corner in measured.Registration!.Corners)
        {
            var back = measured.Registration.Mapping.ToPage(corner.Image);
            Assert.False(double.IsNaN(back.X), $"{TheOne}: the inverse gave nothing back at marker {corner.MarkerId} corner {corner.Corner}");
        }
    }
}
