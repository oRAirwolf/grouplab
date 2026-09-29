using GroupLab.Core.Reporting;
using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// The picture on the one-page report, NOTES-FROM-PLANNING.md entry 280 section 2 (board Report): the stored pixels turned as the marking's
/// view turns them, no longer than <c>longest</c> pixels on a side, and encoded again as a JPEG, so nothing the original file carried, its
/// location above all, goes onto the page. The desktop and the phone both make it here.
/// </summary>
public static class ReportPicture
{
    /// <summary>The picture as a JPEG for the page, or null where it cannot be read.</summary>
    public static DocumentImage? From(string path, int quarterTurns, int longest = 1600)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var image = Cv2.ImRead(path, ImreadModes.Color | ImreadModes.IgnoreOrientation);
        if (image.Empty())
        {
            return null;
        }

        double k = Math.Min(1, (double)longest / Math.Max(image.Width, image.Height));
        using var scaled = k < 1 ? image.Resize(new Size(Math.Max(1, (int)Math.Round(image.Width * k)), Math.Max(1, (int)Math.Round(image.Height * k))), 0, 0, InterpolationFlags.Area) : image.Clone();
        using var turned = new Mat();
        switch (((quarterTurns % 4) + 4) % 4)
        {
            case 1:
                Cv2.Rotate(scaled, turned, RotateFlags.Rotate90Clockwise);
                break;
            case 2:
                Cv2.Rotate(scaled, turned, RotateFlags.Rotate180);
                break;
            case 3:
                Cv2.Rotate(scaled, turned, RotateFlags.Rotate90Counterclockwise);
                break;
            default:
                scaled.CopyTo(turned);
                break;
        }

        Cv2.ImEncode(".jpg", turned, out byte[] jpeg, new ImageEncodingParam(ImwriteFlags.JpegQuality, 85));
        return new DocumentImage(jpeg, turned.Width, turned.Height);
    }
}
