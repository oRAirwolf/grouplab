using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Imaging;
using GroupLab.Core.Measurement;
using GroupLab.Core.Registration;
using GroupLab.Core.Trace;

namespace GroupLab.Cli.Spike;

/// <summary>
/// <c>grouplab surface held-out</c>: NOTES-FROM-PLANNING.md entry 143, question 44, the measurement that decides whether the thin-plate
/// spline of entry 130 section 6b item 2 is worth writing.
/// <para>
/// <b>The question.</b> A bent-sheet model is fitted to the printed markers and then used to say where a bullet hole is. A hole is not a
/// marker: it sits between them, in the middle of the sheet, where nothing was measured. So the model's residual at the markers it was fitted
/// to says almost nothing about how well it places a hole. What does say something is holding a marker out of the fit and asking the model to
/// predict it. A model that cannot predict a marker it did not see will not predict a hole, and no amount of extra flexibility will help.
/// </para>
/// <para>
/// <b>What it does.</b> For each photograph, it registers the sheet as the application does, with the surface model. Then, for each marker the
/// fit kept, it refits with that marker's four corners excluded and measures how far the refitted model puts those corners from where the
/// sheet says they are. The held-out error is reported beside the fit's own residual, in inches, so the two can be read together.
/// </para>
/// <para>
/// <b>One optimism, stated.</b> The refit starts from the same place the real fit starts from, a lens fit over the kept corners, so nothing
/// from the held-out marker reaches the starting point. The held-out marker's corners are still in the image, so the detector saw them; only
/// the fit is denied them. That is the question being asked, and not a claim about a detector that misses a marker.
/// </para>
/// <para>
/// It reads only the files it is given and writes nothing.
/// </para>
/// </summary>
public static class SurfaceHeldOut
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public const string Usage = "grouplab surface held-out <photograph>... [--library <directory>]...";

    public static int Run(IReadOnlyList<string> images, IReadOnlyList<string> libraries, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (images.Count == 0)
        {
            error.WriteLine("surface held-out: give at least one photograph of a GroupLab sheet");
            error.WriteLine(Usage);
            return 2;
        }

        output.WriteLine("Leave one marker out of the bent-sheet fit, then ask the fit where that marker is.");
        output.WriteLine("Entry 143, question 44. A model that cannot predict a marker it did not see will not predict a hole.");
        output.WriteLine();
        output.WriteLine("photograph                          markers  fit rms  held-out median  held-out worst   ratio");

        var everyHeldOut = new List<double>();
        var everyWorst = new List<double>();
        int read = 0, unreadable = 0;

        foreach (string path in images)
        {
            string name = Path.GetFileName(path);
            var row = OnePhotograph(path, libraries, error);
            if (row is null)
            {
                unreadable++;
                output.WriteLine($"{Short(name),-34}  {"-",7}  {"-",7}  {"-",15}  {"-",14}   {"could not be fitted",-8}");
                continue;
            }

            read++;
            var (markers, fitRms, median, worst) = row.Value;
            everyHeldOut.Add(median);
            everyWorst.Add(worst);
            output.WriteLine(string.Create(Inv,
                $"{Short(name),-34}  {markers,7}  {fitRms,7:0.0000}  {median,15:0.0000}  {worst,14:0.0000}   {(fitRms > 0 ? median / fitRms : double.NaN),5:0.0}"));
        }

        output.WriteLine();
        output.WriteLine(string.Create(Inv, $"{read} photograph{(read == 1 ? "" : "s")} fitted, {unreadable} could not be."));

        if (everyHeldOut.Count > 0)
        {
            var medians = everyHeldOut.Order().ToList();
            var worsts = everyWorst.Order().ToList();
            output.WriteLine(string.Create(Inv, $"Held-out error, median over photographs: {medians[medians.Count / 2]:0.0000} in."));
            output.WriteLine(string.Create(Inv, $"Held-out error, worst marker, median over photographs: {worsts[worsts.Count / 2]:0.0000} in."));
            output.WriteLine(string.Create(Inv, $"Held-out error, worst marker on any photograph: {worsts[^1]:0.0000} in."));
            output.WriteLine();
            output.WriteLine("The photograph gate is 0.005 in on the worst bull. A held-out marker error far above that is the model failing");
            output.WriteLine("to predict a point it did not see, and a more flexible surface fitted to the same markers would do no better.");
        }

        return 0;
    }

    private static string Short(string name) => name.Length <= 34 ? name : name[..31] + "...";

    /// <summary>The fit's own residual and the held-out errors for one photograph, in inches, or null where it cannot be fitted.</summary>
    private static (int Markers, double FitRms, double Median, double Worst)? OnePhotograph(string path, IReadOnlyList<string> libraries, TextWriter error)
    {
        GrayImage image;
        ImageMetadata metadata;
        try
        {
            (image, metadata) = ImageLoader.Load(path);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or OpenCvSharp.OpenCVException)
        {
            error.WriteLine($"surface held-out: {Path.GetFileName(path)}: {ex.Message}");
            return null;
        }

        var backend = new OpenCvSharpBackend();
        var trace = new TraceRecorder();
        var identity = SheetIdentification.Identify(image, SheetIdentification.Candidates(libraries.Count > 0 ? libraries : [AnalyzeVerb.DefaultLibrary]), backend, trace);
        if (identity.Definition is not { } definition)
        {
            return null;
        }

        var options = new MeasureOptions(Model: RegistrationModel.Surface);
        var measured = SheetMeasurer.Measure(image, metadata, definition, options, backend);
        if (measured.Registration is not { } fit || measured.Failure is not null)
        {
            return null;
        }

        // The corners as the fit saw them, in its own order: four per marker.
        var corners = fit.Corners;
        var imagePoints = corners.Select(c => c.Image).ToList();
        var pagePoints = corners.Select(c => c.Page).ToList();
        var inliers = corners.Select(c => c.Inlier).ToList();

        if (SurfaceFit.FocalPixelsFromExif(metadata, image.Width, image.Height) is not { } focal)
        {
            return null;
        }

        double left = 0, top = 0, right = definition.Page.Width, bottom = definition.Page.Height;
        var held = new List<double>();

        foreach (int marker in corners.Where(c => c.Inlier).Select(c => c.MarkerId).Distinct())
        {
            var usable = new List<bool>(inliers);
            var indices = new List<int>();
            for (int i = 0; i < corners.Count; i++)
            {
                if (corners[i].MarkerId == marker)
                {
                    usable[i] = false;
                    indices.Add(i);
                }
            }

            // Two markers left is not a fit of anything, so a sheet that thin is not measured rather than measured badly.
            if (usable.Count(u => u) < 12)
            {
                return null;
            }

            SurfaceModel start;
            try
            {
                var keptImage = Where(imagePoints, usable);
                var keptPage = Where(pagePoints, usable);
                var homography = HomographyEstimate.Fit(keptImage, keptPage);
                if (homography is null)
                {
                    continue;
                }

                var lens = LensFit.Fit(keptImage, keptPage, homography, image.Width, image.Height);
                start = SurfaceFit.StartFromLens(lens, focal, (left + right) / 2, (top + bottom) / 2);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            SurfaceFrameResult refitted;
            try
            {
                refitted = SurfaceFit.Fit([new SurfaceFrame("held-out", imagePoints, pagePoints, usable, start, left, top, right, bottom)], shareCamera: false)[0];
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (int i in indices)
            {
                var predicted = refitted.Mapping.ToPage(imagePoints[i]);
                double dx = predicted.X - pagePoints[i].X, dy = predicted.Y - pagePoints[i].Y;
                held.Add(Math.Sqrt((dx * dx) + (dy * dy)) / 254);
            }
        }

        if (held.Count == 0)
        {
            return null;
        }

        held.Sort();
        return (corners.Where(c => c.Inlier).Select(c => c.MarkerId).Distinct().Count(), fit.RmsResidual / 254, held[held.Count / 2], held[^1]);
    }

    private static List<PointD> Where(IReadOnlyList<PointD> points, IReadOnlyList<bool> usable) =>
        [.. points.Where((_, i) => usable[i])];
}
