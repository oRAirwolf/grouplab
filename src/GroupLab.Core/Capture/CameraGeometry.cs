using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Capture;

/// <summary>Where the focal length an angle was worked out with came from.</summary>
public enum FocalSource
{
    /// <summary>The camera's own 35 mm equivalent focal length, from the file.</summary>
    Camera,

    /// <summary>Solved from the sheet's own outline, which a tilted rectangle makes possible.</summary>
    Solved,

    /// <summary>Neither said, so <see cref="CameraGeometry.AssumedEquivalentMm"/> was taken.</summary>
    Assumed,
}

/// <summary>
/// How far off square to the sheet a photograph was taken: the angle between the camera's axis and the sheet's normal, in degrees, the focal
/// length in pixels it was worked out with and where that came from, and, where the page was a unit square, the paper's width over its
/// height as the photograph shows it.
/// </summary>
public sealed record OffAxis(double Degrees, double FocalPixels, FocalSource Focal, double Aspect);

/// <summary>
/// The camera geometry a photograph of a flat sheet holds, NOTES-FROM-PLANNING.md entry 157 sections 3 and 4.
/// <para>
/// <b>The angle.</b> A flat sheet's image is a homography H from the page to the image, and H = K [r1 r2 t] up to scale, where K holds the
/// focal length f and the principal point, taken at the image's center with square pixels. So K^-1 H's first two columns are the page's two
/// axes as the camera sees them, r1 and r2, each up to its own scale. Their cross product is the sheet's normal, and the angle between that
/// normal and the camera's axis is how far off square the photograph is. Normalizing r1 and r2 separately makes the angle independent of the
/// units and the aspect of the page coordinates, so four paper corners mapped to a unit square give the same angle as the sheet's markers.
/// </para>
/// <para>
/// <b>The focal length.</b> From the file's 35 mm equivalent where it has one, the equivalent being defined by the 43.27 mm diagonal of a
/// 36 by 24 mm frame. Otherwise from the sheet itself: r1 and r2 are orthogonal and of equal length, which gives two equations in 1/f^2 once
/// the principal point is moved to the origin, solved together by least squares. They are the whiteboard method of Zhang and He. When the
/// sheet is too nearly square on for them to say anything, the angle hardly depends on f, and <see cref="AssumedEquivalentMm"/> is taken.
/// </para>
/// </summary>
public static class CameraGeometry
{
    /// <summary>The diagonal of a 36 by 24 mm frame, which a 35 mm equivalent focal length is defined against.</summary>
    public const double FullFrameDiagonalMm = 43.2666;

    /// <summary>The 35 mm equivalent focal length taken when neither the file nor the sheet gives one.</summary>
    public const double AssumedEquivalentMm = 26;

    /// <summary>
    /// The angle, worked out with the assumed focal length, below which the focal length is not solved from the sheet. On the 2026-09-20
    /// range photographs the solve came within 2 degrees of the camera's own answer above 20 degrees, and gave from 1.0 to 4.3 times the
    /// camera's focal length below it, one photograph at 10 degrees reading 38.
    /// </summary>
    public const double SolveFromDegrees = 20;

    /// <summary>The focal length in pixels from the file's 35 mm equivalent, or null without one.</summary>
    public static double? FocalFromMetadata(ImageMetadata? metadata, int width, int height) =>
        metadata?.FocalLength35mm is int equivalent && equivalent > 0
            ? equivalent * Math.Sqrt(((double)width * width) + ((double)height * height)) / FullFrameDiagonalMm
            : null;

    /// <summary>The focal length in pixels an assumed 35 mm equivalent gives this image.</summary>
    public static double AssumedFocal(int width, int height) =>
        AssumedEquivalentMm * Math.Sqrt(((double)width * width) + ((double)height * height)) / FullFrameDiagonalMm;

    /// <summary>
    /// The focal length in pixels solved from a page-to-image homography, with the principal point at (<paramref name="cx"/>,
    /// <paramref name="cy"/>), or null where the sheet does not say, or the answer is not a focal length a camera has.
    /// <para>
    /// The page's two axes are at right angles whatever its units, so that equation always holds. Their lengths are equal only when the page
    /// coordinates are true lengths, as a sheet's markers give, and not when four paper corners are mapped to a unit square, whose paper may be
    /// any shape: <paramref name="trueLengths"/> says which. With only the right angle, a sheet tilted about an axis parallel to one of its sides
    /// gives nothing, because one of its vanishing points is at infinity; a camera turned at all about the other axis breaks the tie.
    /// </para>
    /// </summary>
    public static double? SolveFocal(Homography pageToImage, double cx, double cy, double diagonal, bool trueLengths = false)
    {
        ArgumentNullException.ThrowIfNull(pageToImage);
        var (h1, h2) = Columns(pageToImage, cx, cy);
        double scale = Math.Max(Norm(h1.X, h1.Y), Norm(h2.X, h2.Y));
        h1 = (h1.X / scale, h1.Y / scale, h1.Z / scale * diagonal);
        h2 = (h2.X / scale, h2.Y / scale, h2.Z / scale * diagonal);

        // In units of the diagonal: u = (diagonal / f)^2, a u + b = 0 and c u + d = 0.
        double a = (h1.X * h2.X) + (h1.Y * h2.Y), b = h1.Z * h2.Z;
        double c = (h1.X * h1.X) + (h1.Y * h1.Y) - (h2.X * h2.X) - (h2.Y * h2.Y), d = (h1.Z * h1.Z) - (h2.Z * h2.Z);
        // The coefficients of u vanish as the sheet comes square on, when f no longer changes what the camera sees: below about 8 degrees of
        // tilt the solve is noise, and the angle it would feed hardly depends on f anyway.
        if (!trueLengths)
        {
            c = 0;
            d = 0;
        }

        double evidence = Math.Sqrt((a * a) + (c * c));
        if (evidence < 0.02)
        {
            return null;
        }

        double u = -((a * b) + (c * d)) / ((a * a) + (c * c));
        if (u <= 0)
        {
            return null;
        }

        double f = diagonal / Math.Sqrt(u);
        return f > 0.3 * diagonal && f < 5 * diagonal ? f : null;
    }

    /// <summary>
    /// How far off square the photograph is, from the page-to-image homography, with the focal length from the file, else solved from the
    /// sheet, else assumed. <paramref name="trueLengths"/> as for <see cref="SolveFocal"/>.
    /// </summary>
    public static OffAxis Measure(Homography pageToImage, int width, int height, ImageMetadata? metadata = null, bool trueLengths = false)
    {
        ArgumentNullException.ThrowIfNull(pageToImage);
        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0, diagonal = Math.Sqrt(((double)width * width) + ((double)height * height));
        if (FocalFromMetadata(metadata, width, height) is { } camera)
        {
            return Measure(pageToImage, width, height, camera, FocalSource.Camera);
        }

        var assumed = Measure(pageToImage, width, height, AssumedFocal(width, height), FocalSource.Assumed);
        return assumed.Degrees >= SolveFromDegrees && SolveFocal(pageToImage, cx, cy, diagonal, trueLengths) is { } solved
            ? Measure(pageToImage, width, height, solved, FocalSource.Solved)
            : assumed;
    }

    /// <summary>The angle with a stated focal length in pixels.</summary>
    public static OffAxis Measure(Homography pageToImage, int width, int height, double focalPixels, FocalSource source)
    {
        ArgumentNullException.ThrowIfNull(pageToImage);
        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0;
        var (h1, h2) = Columns(pageToImage, cx, cy);
        var r1 = (X: h1.X / focalPixels, Y: h1.Y / focalPixels, Z: h1.Z);
        var r2 = (X: h2.X / focalPixels, Y: h2.Y / focalPixels, Z: h2.Z);
        double n1 = Norm(r1.X, r1.Y, r1.Z), n2 = Norm(r2.X, r2.Y, r2.Z);
        double nx = ((r1.Y * r2.Z) - (r1.Z * r2.Y)) / (n1 * n2), ny = ((r1.Z * r2.X) - (r1.X * r2.Z)) / (n1 * n2), nz = ((r1.X * r2.Y) - (r1.Y * r2.X)) / (n1 * n2);
        double degrees = Math.Acos(Math.Clamp(Math.Abs(nz) / Norm(nx, ny, nz), 0, 1)) * 180 / Math.PI;
        return new OffAxis(degrees, focalPixels, source, n1 / n2);
    }

    /// <summary>The homography's first two columns with the principal point moved to the origin.</summary>
    private static ((double X, double Y, double Z) H1, (double X, double Y, double Z) H2) Columns(Homography h, double cx, double cy) =>
        ((h[0, 0] - (cx * h[2, 0]), h[1, 0] - (cy * h[2, 0]), h[2, 0]), (h[0, 1] - (cx * h[2, 1]), h[1, 1] - (cy * h[2, 1]), h[2, 1]));

    private static double Norm(double x, double y, double z = 0) => Math.Sqrt((x * x) + (y * y) + (z * z));

    /// <summary>The standard paper sizes an outline's shape is compared with, width over height in portrait.</summary>
    public static IReadOnlyList<(string Name, double WidthInches, double HeightInches)> PaperSizes { get; } =
    [
        ("Letter", 8.5, 11),
        ("Legal", 8.5, 14),
        ("Tabloid", 11, 17),
        ("A4", 8.27, 11.69),
        ("A3", 11.69, 16.54),
        ("12 by 18 in", 12, 18),
    ];

    /// <summary>
    /// The standard paper size nearest a measured shape, and how far off it is, the shape being the longer side over the shorter. A size is
    /// only named when it is within 3 percent, because Letter and A4 are 9 percent apart and a guess between them would be wrong often.
    /// </summary>
    public static (string Name, double WidthInches, double HeightInches)? NearestPaper(double aspect)
    {
        double shape = aspect >= 1 ? aspect : 1 / aspect;
        var best = PaperSizes.MinBy(p => Math.Abs((p.HeightInches / p.WidthInches) - shape));
        return Math.Abs((best.HeightInches / best.WidthInches / shape) - 1) <= 0.03 ? best : null;
    }

    /// <summary>A focal source in words, for a record or a sentence.</summary>
    public static string Describe(FocalSource source) => source switch
    {
        FocalSource.Camera => "the camera's own focal length",
        FocalSource.Solved => "a focal length solved from the sheet",
        _ => string.Create(CultureInfo.InvariantCulture, $"an assumed {AssumedEquivalentMm:0} mm equivalent focal length"),
    };
}
