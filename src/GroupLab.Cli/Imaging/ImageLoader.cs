using System.Runtime.InteropServices;
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
    /// Refuses an image larger than <see cref="MostPixels"/>, saying the measured size and the limit.
    /// <para>
    /// Question 43 (entry 388 section 3, Alan approves): the size is judged from the file's own header before anything is decoded, so a few
    /// bytes claiming an enormous image are refused before OpenCV allocates it. A file whose header could not be read is judged again after
    /// the decode, from the decoded size, which is where the check used to be.
    /// </para>
    /// </summary>
    private static void NotTooLarge(long width, long height, string path)
    {
        long pixels = width * height;
        if (pixels <= 0)
        {
            return;
        }

        // Entry 240 section 1.5: below the fixed cap, what this machine's memory holds, so a computer short of memory says so rather than
        // running out part way through. On anything with 12 GB or more it is the cap.
        long memory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        double most = memory > 0 ? GroupLab.Core.Imaging.MemoryBudget.DesktopMostMegapixels(memory / (1024.0 * 1024), MostPixels / 1e6) : MostPixels / 1e6;
        if (pixels > most * 1e6 && most * 1e6 < MostPixels)
        {
            throw TooLarge(FormattableString.Invariant(
                $"{path} is {width} by {height}, which is {pixels / 1_000_000} megapixels. This computer's memory holds images up to about {most:0} megapixels for GroupLab to read whole; a scan at a lower resolution will read."));
        }

        if (pixels > MostPixels)
        {
            throw TooLarge(FormattableString.Invariant(
                $"{path} is {width} by {height}, which is {pixels / 1_000_000} megapixels. GroupLab reads up to {MostPixels / 1_000_000} megapixels, which is far larger than any scan or photograph of a target. A file this big is either broken or built to exhaust memory."));
        }
    }

    private const string TooLargeKey = "GroupLab.TooLarge";

    private static InvalidDataException TooLarge(string message)
    {
        var refused = new InvalidDataException(message);
        refused.Data[TooLargeKey] = true;
        return refused;
    }

    /// <summary>Whether an exception is a refusal for size, whose message names the size measured and the limit, for the person to read.</summary>
    public static bool IsTooLarge(Exception e) => e is InvalidDataException && e.Data.Contains(TooLargeKey);

    private static void NotTooLarge(Mat mat, string path) => NotTooLarge(mat.Width, mat.Height, path);

    /// <summary>The header's size judged before the decode, where the header gave one.</summary>
    private static void NotTooLarge(ImageMetadata metadata, string path) => NotTooLarge(metadata.Width ?? 0, metadata.Height ?? 0, path);

    /// <summary>
    /// The file's bytes, refused before any decode where its header claims more than GroupLab reads (question 43), for the places that
    /// decode a person's file themselves rather than through <see cref="Load(string)"/>: the store-bought target's photo and scale markers.
    /// </summary>
    public static byte[] Checked(string path)
    {
        byte[] bytes = Bytes(path);
        NotTooLarge(ImageMetadataReader.Read(bytes), path);
        return bytes;
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
        NotTooLarge(metadata, path);
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
        return (MaxOf(mat), Working(read, mat, bytes, mostMegapixels));
    }

    /// <summary>
    /// The grey image and the brightest channel together, the same images <see cref="Load(string, double?)"/> and
    /// <see cref="LoadMaxChannel(string, double?)"/> give, from one read of the file and, for a PNG kept at full size, one decode (entry
    /// 414 section 1.1).
    /// </summary>
    public static (GrayImage Grey, GrayImage MaxChannel, ImageMetadata Metadata) LoadGreyAndMax(string path, double? mostMegapixels = null)
    {
        byte[] bytes = Bytes(path);
        var read = ImageMetadataReader.Read(bytes);
        bool fullSize = read.Width is { } w && read.Height is { } h && w > 0 && h > 0 && WorkingSize.Scale(w, h, mostMegapixels) >= 1;
        if (fullSize && OneDecodeServes(bytes))
        {
            using var colour = Decode(bytes, path, colour: true, read, mostMegapixels);
            return (GreyLikeLibpng(colour), MaxOf(colour), Working(read, colour, bytes, mostMegapixels));
        }

        using var greyMat = Decode(bytes, path, colour: false, read, mostMegapixels);
        using var colourMat = Decode(bytes, path, colour: true, read, mostMegapixels);
        return (OpenCvSharpBackend.Copy(greyMat), MaxOf(colourMat), Working(read, greyMat, bytes, mostMegapixels));
    }

    /// <summary>max(R, G, B) at each pixel of a colour image.</summary>
    private static GrayImage MaxOf(Mat colour)
    {
        var channels = Cv2.Split(colour);
        try
        {
            using var max = new Mat();
            Cv2.Max(channels[0], channels[1], max);
            Cv2.Max(max, channels[2], max);
            return OpenCvSharpBackend.Copy(max);
        }
        finally
        {
            foreach (var channel in channels)
            {
                channel.Dispose();
            }
        }
    }

    /// <summary>The grey image of a file whose colour decode is <paramref name="colour"/>: made from it where that is identical, else decoded.</summary>
    private static GrayImage GreyOf(byte[] bytes, Mat colour, string path)
    {
        if (OneDecodeServes(bytes))
        {
            return GreyLikeLibpng(colour);
        }

        using var grey = Cv2.ImDecode(bytes, ImreadModes.Grayscale | ImreadModes.IgnoreOrientation);
        if (grey.Empty())
        {
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        return OpenCvSharpBackend.Copy(grey);
    }

    /// <summary>
    /// Whether a grey decode of <paramref name="bytes"/> is <see cref="GreyLikeLibpng"/> of its colour decode: an 8 bit PNG with no
    /// gAMA, sRGB, iCCP or cHRM chunk before its pixels. Any of those can turn on libpng's gamma correction of the grey, which is measured
    /// to move it (a gAMA of 0.45455 moved 103,817 of 921,600 pixels), so a file carrying one is decoded twice as before.
    /// </summary>
    public static bool OneDecodeServes(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (bytes.Length < 33 || !bytes[..8].SequenceEqual(signature) || bytes[24] != 8)
        {
            return false;
        }

        for (long at = 8; at + 8 <= bytes.Length;)
        {
            long length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes[(int)at..]);
            string type = System.Text.Encoding.ASCII.GetString(bytes.Slice((int)at + 4, 4));
            if (type is "IDAT" or "IEND")
            {
                return true;
            }

            if (type is "gAMA" or "sRGB" or "iCCP" or "cHRM")
            {
                return false;
            }

            at += length + 12;
        }

        return false;
    }

    /// <summary>
    /// libpng's conversion of an 8 bit colour pixel to grey with no gamma, as OpenCV asks for it (red 0.299 and green 0.587, which libpng
    /// keeps as 9797 and 19234 parts of 32768, and blue the 3737 left), truncated as libpng truncates, and a pixel whose three channels are
    /// equal left as it is. Identical to OpenCV's grey decode at every pixel of the 600 dpi sample, two of Alan's 600 dpi scans, a camera
    /// PNG and the screen renders, alpha and palette PNGs included (entry 414 section 1.1, held by ImageLoaderSameResultTests).
    /// </summary>
    public static GrayImage GreyLikeLibpng(Mat colour)
    {
        int width = colour.Width, height = colour.Height;
        var pixels = new byte[(long)width * height];
        using var contiguous = colour.IsContinuous() ? null : colour.Clone();
        IntPtr start = (contiguous ?? colour).Data;
        Parallel.For(0, height, () => new byte[width * 3], (y, _, row) =>
        {
            Marshal.Copy(IntPtr.Add(start, y * width * 3), row, 0, row.Length);
            long to = (long)y * width;
            for (int x = 0, i = 0; x < width; x++, i += 3)
            {
                int b = row[i], g = row[i + 1], r = row[i + 2];
                pixels[to + x] = r == g && r == b ? (byte)r : (byte)(((9797 * r) + (19234 * g) + (3737 * b)) >> 15);
            }

            return row;
        }, _ => { });
        return new GrayImage(width, height, pixels);
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
    /// <para>
    /// <b>Entry 414 section 1.1: a PNG decodes once.</b> OpenCV's grey decode of a PNG is libpng's own conversion, and without a gamma in the
    /// file that is a fixed sum truncated, which <see cref="GreyLikeLibpng"/> makes from the colour decode identical to the bit. A JPEG,
    /// a 16 bit PNG and a PNG that names its gamma or colour space are still decoded twice.
    /// </para>
    /// </summary>
    /// <returns>The grey image, the max(R, G, B) image, the colour image for the screen, and what the file says about itself.</returns>
    public static (GrayImage Grey, GrayImage MaxChannel, Mat Colour, ImageMetadata Metadata) LoadForEditor(string path)
    {
        byte[] bytes = Bytes(path);
        var metadata = ImageMetadataReader.Read(bytes);
        NotTooLarge(metadata, path);
        var colour = Cv2.ImDecode(bytes, ImreadModes.Color | ImreadModes.IgnoreOrientation);
        if (colour.Empty())
        {
            colour.Dispose();
            throw new InvalidDataException($"{path} is not an image OpenCV can decode.");
        }

        try
        {
            NotTooLarge(colour, path);

            return (GreyOf(bytes, colour, path), MaxOf(colour), colour, metadata);
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
        NotTooLarge(metadata, path);
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

