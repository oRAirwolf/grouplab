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

    /// <summary>
    /// The most pixels GroupLab will decode from one file, NOTES-FROM-PLANNING.md entry 143, question 43.
    /// <para>
    /// <b>It is a limit against a hostile or broken file, not a judgement about scanning.</b> A few bytes of header can claim an image of
    /// any size, and decoding it allocates the whole thing before anything else can object. 400 megapixels is twelve times Alan's largest
    /// real file, a 600 dpi letter scan at about 32 megapixels, so nothing anybody scans comes near it.
    /// </para>
    /// </summary>
    public const long MostPixels = 400L * 1000 * 1000;

    /// <summary>
    /// Refuses a decoded image larger than <see cref="MostPixels"/>, saying the measured size and the limit.
    /// <para>
    /// The check is after the decode because that is where the size is known: OpenCV reads the header and allocates in one call, and there
    /// is no way through it to ask first. So this does not prevent the allocation; it stops everything downstream from working on a file
    /// nothing here should be working on, and says why in a sentence a person can act on.
    /// </para>
    /// </summary>
    private static void NotTooLarge(Mat mat, string path)
    {
        long pixels = (long)mat.Width * mat.Height;
        if (pixels > MostPixels)
        {
            throw new InvalidDataException(FormattableString.Invariant(
                $"{path} is {mat.Width} by {mat.Height}, which is {pixels / 1_000_000} megapixels. GroupLab reads up to {MostPixels / 1_000_000} megapixels, which is far larger than any scan or photograph of a target. A file this big is either broken or built to exhaust memory."));
        }
    }

    public static (GrayImage Image, ImageMetadata Metadata) Load(string path) => Load(path, null);

    /// <summary>
    /// The image at no more than <paramref name="mostMegapixels"/> (entry 219 item A1, <see cref="WorkingSize"/>), decoded at a reduced size
    /// where the format allows it, so the full image is never held; null works at full size.
    /// </summary>
    public static (GrayImage Image, ImageMetadata Metadata) Load(string path, double? mostMegapixels)
    {
        byte[] bytes = Bytes(path);
        var metadata = ImageMetadataReader.Read(bytes);
        using var mat = Decode(bytes, path, colour: false, metadata, mostMegapixels);
        return (OpenCvSharpBackend.Copy(mat), Working(metadata, mat, bytes, mostMegapixels));
    }

    /// <summary>
    /// The decode at working size: a JPEG reduced by a power of two while it is decoded, where that alone does not undershoot, then an area
    /// resample to exactly the working size. Too large is judged on the image's full size, as ever.
    /// </summary>
    private static Mat Decode(byte[] bytes, string path, bool colour, ImageMetadata metadata, double? mostMegapixels)
    {
        int width = metadata.Width ?? 0, height = metadata.Height ?? 0;
        double scale = width > 0 && height > 0 ? WorkingSize.Scale(width, height, mostMegapixels) : 1;
        int reduce = scale <= 0.125 ? 8 : scale <= 0.25 ? 4 : scale <= 0.5 ? 2 : 1;
        var mode = (colour, reduce) switch
        {
            (false, 8) => ImreadModes.ReducedGrayscale8,
            (false, 4) => ImreadModes.ReducedGrayscale4,
            (false, 2) => ImreadModes.ReducedGrayscale2,
            (false, _) => ImreadModes.Grayscale,
            (true, 8) => ImreadModes.ReducedColor8,
            (true, 4) => ImreadModes.ReducedColor4,
            (true, 2) => ImreadModes.ReducedColor2,
            _ => ImreadModes.Color,
        };
        var mat = Cv2.ImDecode(bytes, mode | ImreadModes.IgnoreOrientation);
        if (mat.Empty())
        {
            mat.Dispose();
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        if (reduce == 1)
        {
            NotTooLarge(mat, path);
        }

        // Where the file did not say its size, the decode does; the working size is then reached by the resample alone.
        double rest = WorkingSize.Scale(mat.Width, mat.Height, mostMegapixels);
        if (rest < 1)
        {
            var smaller = new Mat();
            Cv2.Resize(mat, smaller, new Size(0, 0), rest, rest, InterpolationFlags.Area);
            mat.Dispose();
            return smaller;
        }

        return mat;
    }

    /// <summary>The metadata for the image as decoded: its size, and its resolution scaled with it where it was brought down.</summary>
    private static ImageMetadata Working(ImageMetadata metadata, Mat mat, byte[] bytes, double? mostMegapixels)
    {
        if (mostMegapixels is null || metadata.Width is not { } width || width <= 0)
        {
            return metadata;
        }

        double scale = (double)mat.Width / width;
        return WorkingSize.Scaled(metadata, mat.Width, mat.Height, scale);
    }

    /// <summary>
    /// The image decoded in colour and reduced to max(R, G, B) per pixel, the channel both neutral darkness and HSV Value are
    /// built on (docs/SCAN-MEASUREMENTS.md section 3.1), with what the file says about itself.
    /// </summary>
    public static (GrayImage MaxChannel, ImageMetadata Metadata) LoadMaxChannel(string path) => LoadMaxChannel(path, null);

    /// <summary>The brightest channel at no more than <paramref name="mostMegapixels"/>, the same size <see cref="Load(string, double?)"/> gives.</summary>
    public static (GrayImage MaxChannel, ImageMetadata Metadata) LoadMaxChannel(string path, double? mostMegapixels)
    {
        byte[] bytes = Bytes(path);
        var read = ImageMetadataReader.Read(bytes);
        using var mat = Decode(bytes, path, colour: true, read, mostMegapixels);
        var metadata = Working(read, mat, bytes, mostMegapixels);

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
    /// Everything the editor needs from one file, from one read and one decode, NOTES-FROM-PLANNING.md entry 130 section 6 item 1.
    /// <para>
    /// <b>Why the application was slower than the command line on the same scan.</b> Opening an image read the file three times and decoded
    /// it three times: once grey through <see cref="Load(string)"/>, once in colour through <see cref="LoadMaxChannel(string)"/>, and once more in colour to
    /// make the picture on the screen. On a 600 dpi letter scan that is three decodes of a 34 megapixel image where the command line does
    /// one, and all of it on the thread that draws.
    /// </para>
    /// <para>
    /// <b>The grey is still decoded as grey, and that is a finding rather than a choice.</b> The obvious further saving is to convert the
    /// colour image to grey instead of decoding a second time, since OpenCV's grayscale decode and its BGR to grey conversion are built on
    /// the same coefficients. They do not agree. Measured over the screen renders, the two differ at about eight percent of pixels by one
    /// level, every image, which is `ImageLoaderSameResultTests` doing its job. Entry 130 section 6 item 2 is explicit that an optimisation
    /// changes no result, and every threshold in the detector is a comparison against a grey level, so one level is enough to move a hole.
    /// </para>
    /// <para>
    /// So this reads once and decodes twice, where opening an image used to read three times and decode three times.
    /// </para>
    /// </summary>
    /// <returns>The grey image, the max(R, G, B) image, the colour image for the screen, and what the file says about itself.</returns>
    public static (GrayImage Grey, GrayImage MaxChannel, Mat Colour, ImageMetadata Metadata) LoadForEditor(string path)
    {
        byte[] bytes = Bytes(path);
        var metadata = ImageMetadataReader.Read(bytes);
        var colour = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            colour.Dispose();
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        try
        {
            NotTooLarge(colour, path);

            using var grey = Cv2.ImDecode(bytes, ImreadModes.Grayscale | ImreadModes.IgnoreOrientation);
            if (grey.Empty())
            {
                throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
            }

            var channels = Cv2.Split(colour);
            try
            {
                using var max = new Mat();
                Cv2.Max(channels[0], channels[1], max);
                Cv2.Max(max, channels[2], max);
                return (OpenCvSharpBackend.Copy(grey), OpenCvSharpBackend.Copy(max), colour, metadata);
            }
            finally
            {
                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
            }
        }
        catch
        {
            colour.Dispose();
            throw;
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

        NotTooLarge(mat, path);

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
