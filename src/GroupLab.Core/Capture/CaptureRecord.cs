using System.Globalization;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;

namespace GroupLab.Core.Capture;

/// <summary>
/// What is kept with a photograph, NOTES-FROM-PLANNING.md entry 157 section 3 item 5: the lens and its focal lengths as the file states them,
/// the off-axis angle measured from the sheet and the focal length it was measured with, the correction applied, with the lens's radial
/// terms where it was fitted, and the quality. Never a location or a time: nothing here is read from anywhere but the lens tags and the
/// pixels.
/// </summary>
public sealed record CaptureRecord(
    string? Lens,
    double? FocalLengthMm,
    int? FocalLength35mm,
    double OffAxisDegrees,
    string FocalSource,
    string Correction,
    double? K1,
    double? K2,
    CaptureQuality Quality)
{
    /// <summary>The clause the registration summary carries, so a person sees the photograph's quality where they see the markers found.</summary>
    public string Clause => string.Create(CultureInfo.InvariantCulture, $"photographed {OffAxisDegrees:0} degrees off square, {Quality.Words}");

    /// <summary>
    /// A photograph's record from its registration: the angle from the markers, whose page lengths are true, with the file's focal length
    /// where it has one, and the quality over the whole sheet.
    /// </summary>
    public static CaptureRecord Of(GrayImage image, ImageMetadata metadata, IPageMapping mapping, double pageWidthDmm, double pageHeightDmm, int markersRead, int markersExpected, double? k1, double? k2)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(mapping);
        var pageToImage = PageToImage(mapping, pageWidthDmm, pageHeightDmm);
        var angle = CameraGeometry.Measure(pageToImage, image.Width, image.Height, metadata, trueLengths: true);
        var quality = CaptureQualities.Measure(image, pageToImage, pageWidthDmm / 254, pageHeightDmm / 254, angle.Degrees, markersRead, markersExpected);
        return new CaptureRecord(metadata.LensModel, metadata.FocalLengthMm, metadata.FocalLength35mm, angle.Degrees, CameraGeometry.Describe(angle.Focal), mapping.Model, k1, k2, quality);
    }

    /// <summary>A page-to-image homography in page inches, (0, 0) at the top left, fitted to a registration's own mapping across the page.</summary>
    public static Homography PageToImage(IPageMapping mapping, double widthDmm, double heightDmm)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        var page = new List<PointD>();
        var image = new List<PointD>();
        for (int i = 0; i <= 10; i++)
        {
            for (int j = 0; j <= 10; j++)
            {
                var p = new PointD(widthDmm * i / 10, heightDmm * j / 10);
                page.Add(new PointD(p.X / 254, p.Y / 254));
                image.Add(mapping.ToImage(p));
            }
        }

        return HomographyEstimate.Fit(page, image) ?? throw new InvalidOperationException("the registration does not map the page to the image");
    }
}
