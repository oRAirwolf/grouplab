using GroupLab.Core.Imaging;

namespace GroupLab.Core.StoreTargets;

/// <summary>
/// A picture in Lab colour, OpenCV's 8-bit encoding, three bytes a pixel row by row: what a <see cref="ColourLayout"/> is read against.
/// </summary>
public sealed record LabImage(int Width, int Height, byte[] Pixels);

/// <summary>
/// A picture made ready for recognition: its local features found at <see cref="RecognitionSide"/> pixels on its long side, their
/// descriptors, the picture in Lab at that size, and how that size relates to the picture's own pixels.
/// </summary>
/// <param name="Keys">Each feature's position in the reduced picture's pixels.</param>
/// <param name="Descriptors">The features' descriptors, <paramref name="DescriptorBytes"/> each, in the order of <paramref name="Keys"/>.</param>
/// <param name="Lab">The reduced picture in Lab, the same size as the one the features were found on.</param>
/// <param name="Reduction">Reduced pixels per pixel of the picture as it is stored: below one for a picture larger than the working size.</param>
public sealed record PictureFeatures(IReadOnlyList<PointD> Keys, byte[] Descriptors, int DescriptorBytes, LabImage Lab, double Reduction)
{
    /// <summary>The trial's choice: features found at 3000 pixels on the picture's long side, after the phone's 8 megapixel working copy.</summary>
    public const int RecognitionSide = 3000;

    /// <summary>The trial's working copy, <c>WorkingSize.PhoneMegapixels</c>.</summary>
    public const double WorkingMegapixels = 8;

    /// <summary>At most this many of the strongest features of a picture are kept.</summary>
    public const int MostFeatures = 6000;
}

/// <summary>One fingerprint feature near a picture feature: its index in the fingerprint and how far its descriptor is, in bits.</summary>
public readonly record struct DescriptorMatch(int Picture, int Fingerprint, int Distance);

/// <summary>
/// What recognition needs from an imaging library, DESIGN.md section 7: Core holds the method, the decisions and the thresholds; the
/// library finds features, matches descriptors, fits homographies and smooths colour. OpenCV through OpenCvSharp answers it on the desktop
/// and on the phone (src/GroupLab.Cli/Imaging/OpenCvFingerprintBackend.cs).
/// </summary>
public interface IFingerprintBackend
{
    /// <summary>
    /// The picture in the file at <paramref name="path"/>, decoded without its orientation tag or any other metadata, made ready for
    /// recognition; null where it is not an image.
    /// </summary>
    PictureFeatures? Describe(string path);

    /// <summary>For each picture feature, its <paramref name="k"/> nearest fingerprint features by Hamming distance, nearest first.</summary>
    IReadOnlyList<DescriptorMatch[]> Nearest(PictureFeatures picture, TargetFingerprint fingerprint, int k);

    /// <summary>
    /// A homography from <paramref name="source"/> to <paramref name="destination"/>, robust to outliers further than
    /// <paramref name="threshold"/> (RANSAC, or MAGSAC where <paramref name="magsac"/>), then fitted again by least squares to the inliers
    /// alone; with which correspondences were inliers. Null where no homography could be fitted.
    /// </summary>
    (double[] H, bool[] Inliers)? FitHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double threshold, bool magsac);

    /// <summary>The Lab picture reduced by <paramref name="factor"/> (area averaging) and smoothed with a Gaussian of <paramref name="sigma"/> pixels.</summary>
    LabImage Smooth(LabImage lab, double factor, double sigma);
}
