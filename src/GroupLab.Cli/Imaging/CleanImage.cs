using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// An image's pixels encoded again with nothing else, NOTES-FROM-PLANNING.md entry 219 item A5 and docs/ANDROID.md section 8: before a
/// session file leaves the device, the picture in it is re-encoded from its stored pixels, so no EXIF, GPS, XMP, comment or trailer the
/// original carried goes with it. The pixels are kept as stored, without applying the orientation tag, which is what every mark's
/// coordinates are measured in.
/// </summary>
public static class CleanImage
{
    /// <summary>The image as a JPEG at quality 92, or a PNG where the original was one; null where it cannot be read.</summary>
    public static (byte[] Bytes, string Extension)? From(string path)
    {
        using var image = Cv2.ImRead(path, ImreadModes.Unchanged | ImreadModes.IgnoreOrientation);
        if (image.Empty())
        {
            return null;
        }

        bool png = string.Equals(Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase);
        return png
            ? (image.ImEncode(".png"), ".png")
            : (image.ImEncode(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 92)), ".jpg");
    }
}
