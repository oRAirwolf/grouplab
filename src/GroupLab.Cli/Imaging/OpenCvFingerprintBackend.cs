using System.Runtime.InteropServices;
using GroupLab.Core.Imaging;
using GroupLab.Core.StoreTargets;
using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// Store-bought target recognition's imaging, NOTES-FROM-PLANNING.md entry 340, through OpenCV as the trial of entry 332 measured it
/// (docs/notes/fingerprint-trial.md): ORB features, brute force Hamming matching, RANSAC and MAGSAC homographies, and Lab smoothing. The
/// method and its decisions are Core's, <see cref="StoreTargetRecognizer"/>; the phone compiles this same file.
/// </summary>
public sealed class OpenCvFingerprintBackend : IFingerprintBackend
{
    /// <summary>The trial's ORB: Harris scores, eight levels a factor of 1.2 apart, a 31 pixel patch and a FAST threshold of 12.</summary>
    internal static ORB Orb(int features) => ORB.Create(features, 1.2f, 8, 31, 0, 2, ORBScoreType.Harris, 31, 12);

    public PictureFeatures? Describe(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!File.Exists(path))
        {
            return null;
        }

        // Pixels only, decoded without the picture's orientation tag or any other metadata, as every reading of a picture is.
        using var full = Cv2.ImDecode(File.ReadAllBytes(path), ImreadModes.Color | ImreadModes.IgnoreOrientation);
        return full.Empty() ? null : Describe(full);
    }

    /// <summary>A decoded colour picture made ready: the 8 megapixel working copy, then 3000 pixels on the long side, as the trial did.</summary>
    public static PictureFeatures Describe(Mat full)
    {
        ArgumentNullException.ThrowIfNull(full);
        double toWorking = Math.Min(1, Math.Sqrt(PictureFeatures.WorkingMegapixels * 1e6 / ((double)full.Width * full.Height)));
        using var working = new Mat();
        Cv2.Resize(full, working, new Size(0, 0), toWorking, toWorking, InterpolationFlags.Area);
        double toSide = Math.Min(1, PictureFeatures.RecognitionSide / (double)Math.Max(working.Width, working.Height));
        using var small = new Mat();
        Cv2.Resize(working, small, new Size(0, 0), toSide, toSide, InterpolationFlags.Area);
        using var gray = new Mat();
        Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);
        using var detector = Orb(PictureFeatures.MostFeatures);
        var keys = detector.Detect(gray);
        if (keys.Length > PictureFeatures.MostFeatures)
        {
            keys = [.. keys.OrderByDescending(k => k.Response).Take(PictureFeatures.MostFeatures)];
        }

        using var descriptors = new Mat();
        detector.Compute(gray, ref keys, descriptors);
        using var lab = new Mat();
        Cv2.CvtColor(small, lab, ColorConversionCodes.BGR2Lab);
        int n = Math.Min(keys.Length, descriptors.Rows);
        byte[] bytes = descriptors.Empty() ? [] : Bytes(descriptors);
        return new PictureFeatures([.. keys.Take(n).Select(k => new PointD(k.Pt.X, k.Pt.Y))], bytes.Length >= n * 32 ? bytes[..(n * 32)] : bytes, 32,
            new LabImage(lab.Width, lab.Height, Bytes(lab)), toWorking * toSide);
    }

    public IReadOnlyList<DescriptorMatch[]> Nearest(PictureFeatures picture, TargetFingerprint fingerprint, int k)
    {
        ArgumentNullException.ThrowIfNull(picture);
        ArgumentNullException.ThrowIfNull(fingerprint);
        int n = picture.Keys.Count;
        if (n == 0 || fingerprint.Points.Count == 0)
        {
            return [];
        }

        using var query = Matrix(picture.Descriptors, n, picture.DescriptorBytes);
        using var train = Matrix(fingerprint.Descriptors, fingerprint.Points.Count, fingerprint.DescriptorBytes);
        using var matcher = new BFMatcher(NormTypes.Hamming);
        return [.. matcher.KnnMatch(query, train, k).Select(m => m.Select(d => new DescriptorMatch(d.QueryIdx, d.TrainIdx, (int)Math.Round(d.Distance))).ToArray())];
    }

    public (double[] H, bool[] Inliers)? FitHomography(IReadOnlyList<PointD> source, IReadOnlyList<PointD> destination, double threshold, bool magsac)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var src = source.Select(p => new Point2d(p.X, p.Y)).ToArray();
        var dst = destination.Select(p => new Point2d(p.X, p.Y)).ToArray();
        using var mask = new Mat();
        using var robust = Cv2.FindHomography(src, dst, magsac ? HomographyMethods.USAC_MAGSAC : HomographyMethods.Ransac, threshold, mask, 10000, 0.999);
        if (robust.Empty())
        {
            return null;
        }

        byte[] m = Bytes(mask);
        var inliers = m.Select(v => v != 0).ToArray();
        var s = src.Where((_, i) => inliers[i]).ToArray();
        var d = dst.Where((_, i) => inliers[i]).ToArray();
        if (s.Length < 8)
        {
            return (Values(robust), inliers);
        }

        // A least squares fit to the inliers alone.
        using var leastSquares = Cv2.FindHomography(s, d, HomographyMethods.None, 0, null);
        return (Values(leastSquares.Empty() ? robust : leastSquares), inliers);
    }

    public LabImage Smooth(LabImage lab, double factor, double sigma)
    {
        ArgumentNullException.ThrowIfNull(lab);
        using var mat = new Mat(lab.Height, lab.Width, MatType.CV_8UC3);
        Marshal.Copy(lab.Pixels, 0, mat.Data, lab.Pixels.Length);
        using var small = new Mat();
        Cv2.Resize(mat, small, new Size(0, 0), factor, factor, InterpolationFlags.Area);
        Cv2.GaussianBlur(small, small, new Size(0, 0), sigma);
        return new LabImage(small.Width, small.Height, Bytes(small));
    }

    private static double[] Values(Mat h)
    {
        var r = new double[9];
        for (int i = 0; i < 9; i++)
        {
            r[i] = h.At<double>(i / 3, i % 3);
        }

        return r;
    }

    private static Mat Matrix(byte[] bytes, int rows, int cols)
    {
        var mat = new Mat(rows, cols, MatType.CV_8UC1);
        Marshal.Copy(bytes, 0, mat.Data, rows * cols);
        return mat;
    }

    internal static byte[] Bytes(Mat mat)
    {
        using var copy = mat.IsContinuous() ? null : mat.Clone();
        var m = copy ?? mat;
        var bytes = new byte[m.Total() * m.ElemSize()];
        Marshal.Copy(m.Data, bytes, 0, bytes.Length);
        return bytes;
    }
}
