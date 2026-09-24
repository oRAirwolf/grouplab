using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Capture;

/// <summary>
/// A photograph's quality, NOTES-FROM-PLANNING.md entry 157 section 5: one number from 0 to 100 kept with the image, the words a person is
/// shown, and every part it was made from, so that a support conversation can tell a poor photograph from a poor detection. A part that
/// could not be measured is null and plays no part in the score.
/// </summary>
public sealed record CaptureQuality(
    int Score,
    string Words,
    double? BlurInches,
    double? FocusPart,
    double? ClippedShare,
    double? PaperLevel,
    double? ExposurePart,
    double OffAxisDegrees,
    double AnglePart,
    double LeastPixelsPerInch,
    double ResolutionPart,
    int? MarkingsRead,
    int? MarkingsExpected,
    double? MarkingsPart)
{
    /// <summary>The part that set the score, in words, for a person who wants to know why.</summary>
    public string Weakest => new (string Name, double? Part)[]
    {
        ("focus", FocusPart),
        ("exposure", ExposurePart),
        ("angle", AnglePart),
        ("resolution", ResolutionPart),
        ("markings read", MarkingsPart),
    }.Where(p => p.Part is not null).MinBy(p => p.Part)!.Name;

    /// <summary>One line for a record or a report.</summary>
    public string Describe() => string.Create(CultureInfo.InvariantCulture,
        $"{Words}, {Score} of 100, set by its {Weakest}: {(BlurInches is { } b ? $"blur {b:0.0000} in, " : "")}{(ClippedShare is { } c ? $"{100 * c:0.#} percent of the paper blown out at level {PaperLevel:0}, " : "")}{OffAxisDegrees:0.#} degrees off square, {LeastPixelsPerInch:0} pixels an inch at the least{(MarkingsRead is { } r ? $", {r} of {MarkingsExpected} markings read" : "")}.");
}

/// <summary>
/// The quality score, so that it can be recomputed by hand from docs/MOBILE-CAPTURE.md section 5, where every figure below is given.
/// <para>
/// Each part is between 0 and 1, linear between the level at which it is perfect and the level at which it is worthless, and the score is
/// 100 times the least of them, rounded: a photograph is as good as its weakest part, and one blurred photograph with perfect light is still
/// blurred. 70 and above reads good, 40 and above usable, below that poor.
/// </para>
/// </summary>
public static class CaptureQualities
{
    public const double SharpBlurInches = 0.004;
    public const double UselessBlurInches = 0.015;
    public const double FineClipped = 0.02;
    public const double UselessClipped = 0.2;
    public const double FinePaperLevel = 140;
    public const double UselessPaperLevel = 70;
    public const double FineDegrees = 10;
    public const double FinePixelsPerInch = 150;
    public const double UselessPixelsPerInch = 50;
    public const double FineMarkings = 0.9;
    public const double UselessMarkings = 0.5;
    public const int Good = 70;
    public const int Usable = 40;

    /// <summary>The resolution the sheet is rectified to for measuring focus and exposure, at most, in pixels an inch.</summary>
    private const double RectifiedCap = 200;

    /// <summary>
    /// Measures a photograph of a sheet. <paramref name="pageToImage"/> takes page inches, (0, 0) at the sheet's top left, to image pixels, and
    /// the sheet is <paramref name="widthInches"/> by <paramref name="heightInches"/>. Markings are the markers or codes read against those the
    /// sheet has, null for a sheet with none.
    /// </summary>
    public static CaptureQuality Measure(GrayImage image, Homography pageToImage, double widthInches, double heightInches, double offAxisDegrees, int? markingsRead = null, int? markingsExpected = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(pageToImage);
        double least = LeastPixelsPerInch(pageToImage, widthInches, heightInches);
        double ppi = Math.Clamp(least, 20, RectifiedCap);

        // The sheet rectified at no more than its own least resolution, a margin of 3 percent left off every edge.
        double inset = 0.03 * Math.Min(widthInches, heightInches);
        int w = (int)((widthInches - (2 * inset)) * ppi), h = (int)((heightInches - (2 * inset)) * ppi);
        double? blur = null, clipped = null, level = null;
        if (w >= 40 && h >= 40)
        {
            var toRectified = Homography.Compose(pageToImage.Inverse(), new Homography([ppi, 0, -inset * ppi, 0, ppi, -inset * ppi, 0, 0, 1]));
            var sheet = PortableImaging.WarpPerspective(image, toRectified, w, h);
            blur = Blur(sheet) is var sigma && !double.IsNaN(sigma) ? sigma / ppi : null;
            (clipped, level) = Exposure(sheet);
        }

        static double Part(double value, double fine, double useless) =>
            fine > useless ? Math.Clamp((value - useless) / (fine - useless), 0, 1) : Math.Clamp((useless - value) / (useless - fine), 0, 1);

        double? focus = blur is { } b ? Part(b, SharpBlurInches, UselessBlurInches) : null;
        double? exposure = clipped is { } c && level is { } l ? Math.Min(Part(c, FineClipped, UselessClipped), Part(l, FinePaperLevel, UselessPaperLevel)) : null;
        double angle = Part(offAxisDegrees, FineDegrees, OffAxisLimit.Degrees);
        double resolution = Part(least, FinePixelsPerInch, UselessPixelsPerInch);
        double? markings = markingsRead is { } r && markingsExpected is { } e && e > 0 ? Part((double)r / e, FineMarkings, UselessMarkings) : null;
        double weakest = new[] { focus, exposure, angle, resolution, markings }.Where(p => p is not null).Min()!.Value;
        int score = (int)Math.Round(100 * weakest, MidpointRounding.AwayFromZero);
        return new CaptureQuality(score, Words(score), blur, focus, clipped, level, exposure, offAxisDegrees, angle, least, resolution, markingsRead, markingsExpected, markings);
    }

    /// <summary>A score as a person is shown it: good, usable or poor, never the number.</summary>
    public static string Words(int score) => score >= Good ? "good" : score >= Usable ? "usable" : "poor";

    /// <summary>
    /// The fewest image pixels an inch of the sheet gets anywhere on it: the smaller singular value of the homography's Jacobian, at the four
    /// corners and the center, since a perspective's resolution is least at a corner.
    /// </summary>
    public static double LeastPixelsPerInch(Homography pageToImage, double widthInches, double heightInches)
    {
        ArgumentNullException.ThrowIfNull(pageToImage);
        double least = double.MaxValue;
        foreach (var p in new PointD[] { new(0, 0), new(widthInches, 0), new(widthInches, heightInches), new(0, heightInches), new(widthInches / 2, heightInches / 2) })
        {
            var (xx, xy, yx, yy) = pageToImage.Jacobian(p);
            double a = (xx * xx) + (yx * yx), b = (xx * xy) + (yx * yy), c = (xy * xy) + (yy * yy);
            double smaller = Math.Sqrt(Math.Max(0, ((a + c) / 2) - Math.Sqrt((((a - c) / 2) * ((a - c) / 2)) + (b * b))));
            least = Math.Min(least, smaller);
        }

        return least;
    }

    /// <summary>
    /// The blur of a sharp edge, in pixels, as the standard deviation of the Gaussian that would spread a step that far. At an edge of
    /// contrast A blurred by sigma the steepest slope is A / (sigma sqrt(2 pi)), so sigma is read from each of the strongest edges' slope
    /// against the contrast around it, and the median of those is the image's blur. A perfectly sharp step reads 2 / sqrt(2 pi), about 0.8
    /// pixel, because a central difference spreads it over two, so that floor is taken out in quadrature: a sharp photograph reads near
    /// nothing at any resolution, and the figure is the blur the camera added.
    /// </summary>
    public static double Blur(GrayImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        int w = image.Width, h = image.Height;
        var slopes = new List<(double Slope, int X, int Y)>();
        for (int y = 5; y < h - 5; y++)
        {
            for (int x = 5; x < w - 5; x++)
            {
                double gx = (image[x + 1, y] - image[x - 1, y]) / 2.0, gy = (image[x, y + 1] - image[x, y - 1]) / 2.0;
                double g = Math.Sqrt((gx * gx) + (gy * gy));
                if (g >= 12)
                {
                    slopes.Add((g, x, y));
                }
            }
        }

        if (slopes.Count < 50)
        {
            return double.NaN;
        }

        var strongest = slopes.OrderByDescending(s => s.Slope).Take(Math.Max(50, slopes.Count / 50)).ToList();
        var sigmas = new List<double>();
        foreach (var (slope, x, y) in strongest)
        {
            int low = 255, high = 0;
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int v = image[x + dx, y + dy];
                    low = Math.Min(low, v);
                    high = Math.Max(high, v);
                }
            }

            sigmas.Add((high - low) / (slope * Math.Sqrt(2 * Math.PI)));
        }

        sigmas.Sort();
        double median = sigmas[sigmas.Count / 2], floor = 2 / Math.Sqrt(2 * Math.PI);
        return Math.Sqrt(Math.Max(0, (median * median) - (floor * floor)));
    }

    /// <summary>The share of the paper's pixels at 250 or above, and the paper's median level: the paper being the light side of Otsu's threshold.</summary>
    public static (double Clipped, double Level) Exposure(GrayImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        byte threshold = SheetOutline.Otsu(image.Pixels);
        var paper = image.Pixels.Where(p => p > threshold).ToArray();
        if (paper.Length == 0)
        {
            return (0, 0);
        }

        Array.Sort(paper);
        return ((double)paper.Count(p => p >= 250) / paper.Length, paper[paper.Length / 2]);
    }
}

/// <summary>
/// How far off square a photograph may be before GroupLab refuses it, NOTES-FROM-PLANNING.md entry 157 section 3 item 4, set from what was
/// measured: docs/MOBILE-CAPTURE.md section 4 gives it. Every one of the 2026-09-20 range photographs registered up to 35 degrees, the
/// steepest, and the twelve measured against their scans up to 32 degrees kept the same hole error as the squarest. A rendered sheet
/// photographed through a known camera kept every bull within 0.001 in to 60 degrees and lost its markers past 65. The limit sits above
/// everything the real photographs showed working and well inside where the ideal case breaks; question 54 asks planning to read it.
/// </summary>
public static class OffAxisLimit
{
    /// <summary>The limit in degrees.</summary>
    public const double Degrees = 40;

    /// <summary>The refusal, naming the angle and the limit, or null inside it.</summary>
    public static string? Refusal(double degrees) => degrees <= Degrees ? null : string.Create(CultureInfo.InvariantCulture,
        $"This photograph was taken {degrees:0} degrees off square to the sheet, and GroupLab corrects up to {Degrees:0} degrees. Hold the camera more squarely over the sheet and take it again.");
}
