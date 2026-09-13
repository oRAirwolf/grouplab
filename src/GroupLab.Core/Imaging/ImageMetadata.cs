using System.Buffers.Binary;
using System.Text;

namespace GroupLab.Core.Imaging;

/// <summary>
/// What an image file says about itself, read for stages S0 and S1 of DETECTION-PIPELINE.md. <see cref="DpiX"/> and
/// <see cref="DpiY"/> are the resolution a scan claims; a camera's resolution tags describe nothing physical, so they are
/// not reported as one. DESIGN.md section 11: no camera metadata is required, and none is used to measure.
/// </summary>
public sealed record ImageMetadata(
    string Format,
    int? Width,
    int? Height,
    double? DpiX,
    double? DpiY,
    string? CameraMake,
    string? CameraModel,
    int? Orientation,
    double? FocalLengthMm,
    int? FocalLength35mm,
    double? FNumber = null)
{
    /// <summary>
    /// A photograph rather than a scan. A focal length is the one tag a scanner never writes. A lens is identified by
    /// <see cref="FocalLengthMm"/> and <see cref="FNumber"/>, not by <see cref="FocalLength35mm"/>, which a camera app computes and
    /// the Phase 0 phone writes inconsistently (NOTES-FROM-PLANNING.md entry 6).
    /// </summary>
    public bool IsCamera => FocalLengthMm is not null;

    public static ImageMetadata ForScan(int width, int height, double dpi) =>
        new("raster", width, height, dpi, dpi, null, null, null, null, null);
}

/// <summary>Reads <see cref="ImageMetadata"/> from PNG and JPEG bytes. Malformed or truncated input yields what was read before the fault.</summary>
public static class ImageMetadataReader
{
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageMetadata Read(ReadOnlySpan<byte> file)
    {
        var m = new Fields();
        try
        {
            if (file.StartsWith(PngSignature))
            {
                Png(file, m);
            }
            else if (file.Length > 3 && file[0] == 0xFF && file[1] == 0xD8)
            {
                Jpeg(file, m);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (IndexOutOfRangeException)
        {
        }

        bool camera = m.Focal is not null;
        double? dpiX = camera ? null : m.PngDpiX ?? m.JfifX ?? ExifDpi(m.ExifX, m.ResolutionUnit);
        double? dpiY = camera ? null : m.PngDpiY ?? m.JfifY ?? ExifDpi(m.ExifY, m.ResolutionUnit);
        return new ImageMetadata(m.Format, m.Width, m.Height, dpiX, dpiY, m.Make, m.Model, m.Orientation, m.Focal, m.Focal35, m.FNumber);
    }

    private static double? ExifDpi(double? value, int? unit) => value is { } v && v > 0
        ? unit switch { 3 => v * 2.54, 2 or null => v, _ => null }
        : null;

    private static void Png(ReadOnlySpan<byte> f, Fields m)
    {
        m.Format = "PNG";
        int pos = 8;
        while (pos + 12 <= f.Length)
        {
            uint length = BinaryPrimitives.ReadUInt32BigEndian(f[pos..]);
            if (length > (uint)(f.Length - pos - 12))
            {
                break;
            }

            var type = f.Slice(pos + 4, 4);
            var body = f.Slice(pos + 8, (int)length);
            if (type.SequenceEqual("IHDR"u8) && length >= 8)
            {
                m.Width = (int)BinaryPrimitives.ReadUInt32BigEndian(body);
                m.Height = (int)BinaryPrimitives.ReadUInt32BigEndian(body[4..]);
            }
            else if (type.SequenceEqual("pHYs"u8) && length >= 9 && body[8] == 1)
            {
                m.PngDpiX = BinaryPrimitives.ReadUInt32BigEndian(body) * 0.0254;
                m.PngDpiY = BinaryPrimitives.ReadUInt32BigEndian(body[4..]) * 0.0254;
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                break;
            }

            pos += 12 + (int)length;
        }
    }

    private static void Jpeg(ReadOnlySpan<byte> f, Fields m)
    {
        m.Format = "JPEG";
        int pos = 2;
        while (pos + 4 <= f.Length)
        {
            if (f[pos] != 0xFF)
            {
                pos++;
                continue;
            }

            byte marker = f[pos + 1];
            if (marker == 0xFF)
            {
                pos++;
                continue;
            }

            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7))
            {
                pos += 2;
                continue;
            }

            if (marker is 0xD9 or 0xDA)
            {
                break;
            }

            int length = BinaryPrimitives.ReadUInt16BigEndian(f[(pos + 2)..]);
            if (length < 2 || pos + 2 + length > f.Length)
            {
                break;
            }

            var segment = f.Slice(pos + 4, length - 2);
            if (marker == 0xE0 && segment.Length >= 12 && segment[..5].SequenceEqual("JFIF\0"u8))
            {
                double perInch = segment[7] switch { 1 => 1, 2 => 2.54, _ => 0 };
                if (perInch > 0)
                {
                    m.JfifX = BinaryPrimitives.ReadUInt16BigEndian(segment[8..]) * perInch;
                    m.JfifY = BinaryPrimitives.ReadUInt16BigEndian(segment[10..]) * perInch;
                }
            }
            else if (marker == 0xE1 && segment.Length > 14 && segment[..6].SequenceEqual("Exif\0\0"u8))
            {
                var tiff = segment[6..];
                bool little = tiff[0] == (byte)'I';
                Ifd(tiff, U32(tiff, 4, little), little, m, 0);
            }
            else if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC && segment.Length >= 5)
            {
                m.Height = BinaryPrimitives.ReadUInt16BigEndian(segment[1..]);
                m.Width = BinaryPrimitives.ReadUInt16BigEndian(segment[3..]);
            }

            pos += 2 + length;
        }
    }

    private static void Ifd(ReadOnlySpan<byte> t, uint offset, bool little, Fields m, int depth)
    {
        if (depth > 1 || offset > (uint)(t.Length - 2))
        {
            return;
        }

        int o = (int)offset;
        int count = U16(t, o, little);
        for (int k = 0; k < count; k++)
        {
            int e = o + 2 + (12 * k);
            if (e + 12 > t.Length)
            {
                return;
            }

            int tag = U16(t, e, little), type = U16(t, e + 2, little);
            uint n = U32(t, e + 4, little);
            switch (tag)
            {
                case 0x010F:
                    m.Make = Ascii(t, e, n, little);
                    break;
                case 0x0110:
                    m.Model = Ascii(t, e, n, little);
                    break;
                case 0x0112:
                    m.Orientation = U16(t, e + 8, little);
                    break;
                case 0x011A:
                    m.ExifX = Rational(t, e, little);
                    break;
                case 0x011B:
                    m.ExifY = Rational(t, e, little);
                    break;
                case 0x0128:
                    m.ResolutionUnit = U16(t, e + 8, little);
                    break;
                case 0x8769:
                    Ifd(t, U32(t, e + 8, little), little, m, depth + 1);
                    break;
                case 0x829D:
                    m.FNumber = Rational(t, e, little);
                    break;
                case 0x920A:
                    m.Focal = Rational(t, e, little);
                    break;
                case 0xA405:
                    m.Focal35 = type == 4 ? (int)U32(t, e + 8, little) : U16(t, e + 8, little);
                    break;
            }
        }
    }

    private static string? Ascii(ReadOnlySpan<byte> t, int entry, uint count, bool little)
    {
        if (count == 0 || count > 256)
        {
            return null;
        }

        int start = count <= 4 ? entry + 8 : (int)U32(t, entry + 8, little);
        if (start < 0 || start + (int)count > t.Length)
        {
            return null;
        }

        return Encoding.ASCII.GetString(t.Slice(start, (int)count)).TrimEnd('\0', ' ');
    }

    private static double? Rational(ReadOnlySpan<byte> t, int entry, bool little)
    {
        uint offset = U32(t, entry + 8, little);
        if (offset > (uint)(t.Length - 8))
        {
            return null;
        }

        uint numerator = U32(t, (int)offset, little), denominator = U32(t, (int)offset + 4, little);
        return denominator == 0 ? null : (double)numerator / denominator;
    }

    private static int U16(ReadOnlySpan<byte> t, int at, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(t[at..]) : BinaryPrimitives.ReadUInt16BigEndian(t[at..]);

    private static uint U32(ReadOnlySpan<byte> t, int at, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(t[at..]) : BinaryPrimitives.ReadUInt32BigEndian(t[at..]);

    private sealed class Fields
    {
        public string Format { get; set; } = "unknown";

        public int? Width { get; set; }

        public int? Height { get; set; }

        public double? PngDpiX { get; set; }

        public double? PngDpiY { get; set; }

        public double? JfifX { get; set; }

        public double? JfifY { get; set; }

        public double? ExifX { get; set; }

        public double? ExifY { get; set; }

        public int? ResolutionUnit { get; set; }

        public string? Make { get; set; }

        public string? Model { get; set; }

        public int? Orientation { get; set; }

        public double? Focal { get; set; }

        public int? Focal35 { get; set; }

        public double? FNumber { get; set; }
    }
}
