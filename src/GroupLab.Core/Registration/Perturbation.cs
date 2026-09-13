using GroupLab.Core.Imaging;

namespace GroupLab.Core.Registration;

/// <summary>
/// The known distortion conformance test 43 applies to a clean render before analysing it as a scan: a rotation, a
/// shear, unequal axis scales, an offset and a crop, about the image centre. The shear is not zero because
/// DETECTION-PIPELINE.md stage S3 measured 0.12 to 0.17 degrees of systematic shear on real scanners, and the axis scales
/// differ because stage S4 has the axes measured separately.
/// </summary>
public sealed record Perturbation(
    double RotationDegrees,
    double ShearDegrees,
    double ScaleX,
    double ScaleY,
    double OffsetXInches,
    double OffsetYInches,
    double CropInches)
{
    /// <summary>The Phase 0a gate's distortion.</summary>
    public static Perturbation Phase0 { get; } = new(0.7, 0.15, 0.999, 1.001, 0.023, -0.017, 0.12);

    /// <summary>
    /// The source-to-destination pixel transform for a <paramref name="width"/> by <paramref name="height"/> image at
    /// <paramref name="dpi"/>, and the size of the cropped result.
    /// </summary>
    public (Homography Transform, int Width, int Height) For(int width, int height, double dpi)
    {
        double crop = CropInches * dpi;
        double cx = (width - 1) / 2.0, cy = (height - 1) / 2.0;
        double angle = RotationDegrees * Math.PI / 180, shear = Math.Tan(ShearDegrees * Math.PI / 180);
        double cos = Math.Cos(angle), sin = Math.Sin(angle);

        // Rotation of (shear of (scale)): [cos -sin; sin cos] [1 shear; 0 1] [sx 0; 0 sy].
        double m00 = cos * ScaleX, m01 = ((cos * shear) - sin) * ScaleY;
        double m10 = sin * ScaleX, m11 = ((sin * shear) + cos) * ScaleY;
        double tx = cx - ((m00 * cx) + (m01 * cy)) + (OffsetXInches * dpi) - crop;
        double ty = cy - ((m10 * cx) + (m11 * cy)) + (OffsetYInches * dpi) - crop;
        return (new Homography([m00, m01, tx, m10, m11, ty, 0, 0, 1]), (int)Math.Floor(width - (2 * crop)), (int)Math.Floor(height - (2 * crop)));
    }
}
