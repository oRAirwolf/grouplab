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
    /// <summary>How many times a locked file is tried, and how long between. Under a second in total, and almost always one attempt.</summary>
    private const int Tries = 5;

    private const int WaitMilliseconds = 120;

    /// <summary>
    /// The file's bytes, waiting briefly where another process has it open.
    /// <para>
    /// <b>This is not defensive coding; it is a fault that happened.</b> On 2026-09-22 CI went red on <c>windows-latest</c> because the
    /// benchmark wrote <c>bench-25-shots.png</c> and could not read it back: "the process cannot access the file because it is being used
    /// by another process". On a clean hosted runner, so it is not one machine's virus scanner. The same thing happens to a person who
    /// opens a scan the moment their scanner finished writing it, and to anyone whose files are in a synchronised folder.
    /// </para>
    /// <para>
    /// A moment's wait is the right answer to a moment's lock. A file still held after that is a real refusal and is reported as one.
    /// </para>
    /// </summary>
    private static byte[] Bytes(string path)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (IOException) when (attempt < Tries)
            {
                Thread.Sleep(WaitMilliseconds);
            }
        }
    }

    public static (GrayImage Image, ImageMetadata Metadata) Load(string path)
    {
        byte[] bytes = Bytes(path);
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
        byte[] bytes = Bytes(path);
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

    /// <summary>
    /// The image in colour reduced two ways, max(R, G, B) and the chroma max(R, G, B) - min(R, G, B), with its metadata. Chroma
    /// separates coloured printed ink from a hole, which is neutral whatever its darkness (docs/DETECTION-PIPELINE.md stage S6).
    /// </summary>
    public static (GrayImage MaxChannel, GrayImage Chroma, ImageMetadata Metadata) LoadMaxAndChroma(string path)
    {
        byte[] bytes = Bytes(path);
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
            using var min = new Mat();
            using var chroma = new Mat();
            Cv2.Max(channels[0], channels[1], max);
            Cv2.Max(max, channels[2], max);
            Cv2.Min(channels[0], channels[1], min);
            Cv2.Min(min, channels[2], min);
            Cv2.Subtract(max, min, chroma);

            // Only cool ink: where blue is the smallest channel the colour is a brown, orange or yellow, such as a mat or a board.
            using var warm = new Mat();
            Cv2.Compare(channels[0], min, warm, CmpTypes.EQ);
            chroma.SetTo(new Scalar(0), warm);
            return (OpenCvSharpBackend.Copy(max), OpenCvSharpBackend.Copy(chroma), metadata);
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
