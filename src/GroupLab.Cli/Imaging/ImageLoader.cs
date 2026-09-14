using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// Stage S0 of DETECTION-PIPELINE.md for the command line: decode a PNG or JPEG to grey through OpenCV, and read what the
/// file says about itself. EXIF orientation is not applied. Registration against the markers is indifferent to how the
/// image is turned, and leaving the camera's pixels where the camera put them keeps the lens centre where the lens was.
/// </summary>
public static class ImageLoader
{
    public static (GrayImage Image, ImageMetadata Metadata) Load(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var metadata = ImageMetadataReader.Read(bytes);
        using var mat = Cv2.ImDecode(bytes, ImreadModes.Grayscale | ImreadModes.IgnoreOrientation);
        if (mat.Empty())
        {
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        return (OpenCvSharpBackend.Copy(mat), metadata);
    }

    /// <summary>
    /// The image decoded in colour and reduced to max(R, G, B) per pixel, the channel both neutral darkness and HSV Value are
    /// built on (docs/SCAN-MEASUREMENTS.md section 3.1), with what the file says about itself.
    /// </summary>
    public static (GrayImage MaxChannel, ImageMetadata Metadata) LoadMaxChannel(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var metadata = ImageMetadataReader.Read(bytes);
        using var mat = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation);
        if (mat.Empty())
        {
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        var channels = Cv2.Split(mat);
        try
        {
            using var max = new Mat();
            Cv2.Max(channels[0], channels[1], max);
            Cv2.Max(max, channels[2], max);
            return (OpenCvSharpBackend.Copy(max), metadata);
        }
        finally
        {
            foreach (var channel in channels)
            {
                channel.Dispose();
            }
        }
    }
}
