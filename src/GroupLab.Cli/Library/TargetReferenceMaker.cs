using System.Globalization;
using GroupLab.Cli.Imaging;
using GroupLab.Core.Capture;
using GroupLab.Core.Gltd.Model;
using GroupLab.Core.Imaging;
using GroupLab.Core.Marking;
using GroupLab.Core.Registration;
using GroupLab.Core.StoreTargets;
using GroupLab.Core.Trace;
using OpenCvSharp;

namespace GroupLab.Cli.Library;

/// <summary>What a new reference has to do with the library already shipped: nothing, the same product again, or the same artwork at another size.</summary>
public sealed record FamilyFinding(StoreTarget? Existing, double SizeRatio, bool Duplicate)
{
    /// <summary>The family the new target joins: the existing product's, or a new one named after it where it had none.</summary>
    public string? Family => Existing is null || Duplicate ? null : Existing.Family ?? FamilyName(Existing);

    /// <summary>A family's name from a product's: its name with the size taken out.</summary>
    public static string FamilyName(StoreTarget target) =>
        string.Join(' ', target.Name.Replace("{size}", "", StringComparison.Ordinal).Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

/// <summary>
/// NOTES-FROM-PLANNING.md entry 344 sections 1 and 2, the imaging half: a photograph of a blank store-bought target straightened to the
/// target by one of the three scale sources of <see cref="TargetStraightening"/>, its fingerprint made from the straightened picture as
/// entry 332 made them from scans, and the new target checked against the whole library for a family. The photograph is read and let go;
/// only the fingerprint is kept.
/// </summary>
public static class TargetReferenceMaker
{
    /// <summary>The straightened picture is made at the photograph's own detail, between these: the fingerprint is made at 100 and 50.</summary>
    public const double LeastDpi = 60, MostDpi = 150;

    /// <summary>
    /// (b) A GroupLab sheet in the photograph, named by its codes and registered from its markers as any capture is: its plane, the
    /// registration's residual in inches and the sheet's size. Null where no sheet was read.
    /// </summary>
    public static (ITargetPlane Plane, double ResidualInches, double SheetInches)? ReadGroupLabSheet(GrayImage grey, GrayImage value, ImageMetadata metadata,
        IReadOnlyList<TargetDefinition> library, double printScale = 1, TargetDefinition? known = null)
    {
        var backend = new OpenCvSharpBackend();
        if ((known ?? SheetIdentification.Identify(grey, library, backend, new TraceRecorder()).Definition) is not { } definition)
        {
            return null;
        }

        var result = AutomaticMarking.Run(grey, value, metadata, definition, backend);
        if (result.Scale is not { } sheet || result.Measurement.Registration is not { } fit)
        {
            return null;
        }

        double across = Math.Sqrt(Math.Pow(definition.Page.Width, 2) + Math.Pow(definition.Page.Height, 2)) / 254 * printScale;
        return (new SheetPlane(sheet.Mapping, printScale), fit.RmsResidual / 254 * printScale, across);
    }

    /// <summary>The photograph's pixels an inch on the target, at its middle.</summary>
    public static double NativeDpi(StraightenedTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        var a = target.Plane.ToImage(new PointD(target.WidthInches / 2, target.HeightInches / 2));
        var b = target.Plane.ToImage(new PointD((target.WidthInches / 2) + 1, target.HeightInches / 2));
        return Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    }

    /// <summary>
    /// The photograph straightened to the target, at its own detail: every pixel of the result looked up through the plane, on a coarse grid
    /// filled in between, so a lens-corrected plane costs no more than a flat one.
    /// </summary>
    public static (Mat Picture, double Dpi) Straighten(Mat colour, StraightenedTarget target)
    {
        ArgumentNullException.ThrowIfNull(colour);
        ArgumentNullException.ThrowIfNull(target);
        double dpi = Math.Round(Math.Clamp(NativeDpi(target), LeastDpi, MostDpi));
        int w = (int)Math.Round(target.WidthInches * dpi), h = (int)Math.Round(target.HeightInches * dpi);
        const int step = 8;
        int gw = (w / step) + 2, gh = (h / step) + 2;
        using var coarseX = new Mat(gh, gw, MatType.CV_32FC1);
        using var coarseY = new Mat(gh, gw, MatType.CV_32FC1);
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                var p = target.Plane.ToImage(new PointD((gx * step + 0.5) / dpi, (gy * step + 0.5) / dpi));
                coarseX.Set(gy, gx, (float)p.X);
                coarseY.Set(gy, gx, (float)p.Y);
            }
        }

        using var mapX = new Mat();
        using var mapY = new Mat();
        Cv2.Resize(coarseX, mapX, new Size(gw * step, gh * step), 0, 0, InterpolationFlags.Linear);
        Cv2.Resize(coarseY, mapY, new Size(gw * step, gh * step), 0, 0, InterpolationFlags.Linear);
        using var cropX = new Mat(mapX, new Rect(0, 0, w, h));
        using var cropY = new Mat(mapY, new Rect(0, 0, w, h));
        var picture = new Mat();
        Cv2.Remap(colour, picture, cropX, cropY, InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(255));
        return (picture, dpi);
    }

    /// <summary>
    /// Entry 344 section 2: the new target against every product already in the library, read on its own straightened picture. One that fits
    /// with the trial's thresholds at the same size is a duplicate; at another size it is the same artwork, and the new one joins its family.
    /// </summary>
    public static FamilyFinding CheckFamily(Mat straightened, double dpi, IReadOnlyList<StoreTarget> library)
    {
        ArgumentNullException.ThrowIfNull(straightened);
        ArgumentNullException.ThrowIfNull(library);
        var backend = new OpenCvFingerprintBackend();
        var picture = OpenCvFingerprintBackend.Describe(straightened);
        var best = library.Select(t => StoreTargetRecognizer.Try(t, picture, backend))
            .Where(c => c.Inliers >= StoreTargetRecognizer.LeastInliers && c.ToImage is not null && c.Layout >= StoreTargetRecognizer.LeastLayout)
            .OrderByDescending(c => c.Layout).FirstOrDefault();
        if (best is null)
        {
            return new FamilyFinding(null, 1, false);
        }

        // The existing product's inches in the new picture's pixels, against the new picture's own: their ratio is the size of one to the other.
        var centre = new PointD(best.Target.PrintedInches.Width / 2, best.Target.PrintedInches.Height / 2);
        var (xx, xy, yx, yy) = best.ToImage!.Jacobian(centre);
        double ratio = Math.Sqrt(Math.Abs((xx * yy) - (xy * yx))) / dpi;
        return new FamilyFinding(best.Target, ratio, Math.Abs(ratio - 1) < 0.03);
    }

    /// <summary>The words for a family finding.</summary>
    public static string Describe(FamilyFinding finding) => finding switch
    {
        { Existing: null } => "Not like anything in the library: a new product with no family.",
        { Duplicate: true } => $"The same as {finding.Existing.Title} already in the library, at the same size: a duplicate.",
        _ => string.Create(CultureInfo.InvariantCulture,
            $"The same artwork as {finding.Existing.Title} at {finding.SizeRatio:0.00} times its size: it joins the family \"{finding.Family}\"."),
    };
}
