using System.Buffers.Binary;
using System.Text;

namespace GroupLab.Core.Publication;

/// <summary>What scrubbing did to one file.</summary>
public sealed record ScrubResult(byte[] Bytes, IReadOnlyList<string> Removed, IReadOnlyList<string> Kept);

/// <summary>
/// Strips location and identifying metadata from a JPEG or PNG before it is published, NOTES-FROM-PLANNING.md entry 22 section 2
/// point 2, without touching a pixel: the compressed image data is copied byte for byte and only the metadata around it is rebuilt.
/// It follows <c>tools/scan_analysis/scrub_exif.py</c>'s policy, which nothing here can run because its library is not installed.
/// <list type="bullet">
/// <item><b>Kept, in a fresh EXIF block:</b> Make, Model and Orientation; exposure time, f-number, ISO, focal length, the 35 mm
/// equivalent and the pixel dimensions; and the digital zoom ratio, which entry 27 section 2 makes part of the lens grouping key.</item>
/// <item><b>Removed:</b> every other EXIF field, which is every GPS field, maker note, serial number, date and thumbnail; XMP, which
/// can carry GPS and dates of its own; IPTC and every other application segment but the JFIF header and an ICC profile; comments;
/// and anything after the image's end marker, where phones append motion-photo video and trailers.</item>
/// </list>
/// A file whose metadata cannot be parsed loses all of it: better no metadata than GPS.
/// </summary>
public static class ImageScrubber
{
    private static readonly ushort[] KeptPrimary = [0x010F, 0x0110, 0x0112];
    private static readonly ushort[] KeptExif = [0x829A, 0x829D, 0x8827, 0x920A, 0xA002, 0xA003, 0xA404, 0xA405];

    private static readonly Dictionary<ushort, string> Names = new()
    {
        [0x010F] = "Make",
        [0x0110] = "Model",
        [0x0112] = "Orientation",
        [0x829A] = "ExposureTime",
        [0x829D] = "FNumber",
        [0x8827] = "ISOSpeedRatings",
        [0x920A] = "FocalLength",
        [0xA002] = "PixelXDimension",
        [0xA003] = "PixelYDimension",
        [0xA404] = "DigitalZoomRatio",
        [0xA405] = "FocalLengthIn35mmFilm",
    };

    private static readonly string[] KeptPngChunks = ["IHDR", "PLTE", "IDAT", "IEND", "tRNS", "cHRM", "gAMA", "iCCP", "sBIT", "sRGB", "pHYs", "bKGD"];

    public static ScrubResult Scrub(byte[] file)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length > 3 && file[0] == 0xFF && file[1] == 0xD8)
        {
            return Jpeg(file);
        }

        if (file.Length > 8 && file.AsSpan(0, 8).SequenceEqual((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
        {
            return Png(file);
        }

        throw new NotSupportedException("Only JPEG and PNG files can be scrubbed.");
    }

    private static ScrubResult Jpeg(byte[] f)
    {
        var output = new List<byte>(f.Length) { 0xFF, 0xD8 };
        var removed = new List<string>();
        var kept = new List<string>();
        int pos = 2;
        while (pos + 4 <= f.Length)
        {
            if (f[pos] != 0xFF)
            {
                throw new InvalidDataException("The JPEG's segments are malformed.");
            }

            byte marker = f[pos + 1];
            if (marker == 0xFF)
            {
                pos++;
                continue;
            }

            if (marker == 0xDA)
            {
                int end = EndOfImage(f, pos);
                output.AddRange(f.AsSpan(pos, end - pos));
                if (end < f.Length)
                {
                    removed.Add($"{f.Length - end} bytes after the image's end marker");
                }

                return new ScrubResult([.. output], removed, kept);
            }

            int length = BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(pos + 2));
            if (length < 2 || pos + 2 + length > f.Length)
            {
                throw new InvalidDataException("A JPEG segment runs past the end of the file.");
            }

            var body = f.AsSpan(pos + 4, length - 2);
            bool keep;
            switch (marker)
            {
                case 0xE0:
                    keep = body.StartsWith("JFIF\0"u8);
                    if (!keep)
                    {
                        removed.Add("APP0 extension");
                    }

                    break;
                case 0xE1 when body.StartsWith("Exif\0\0"u8):
                    keep = false;
                    var exif = RebuildExif(body[6..].ToArray(), removed, kept);
                    if (exif is not null)
                    {
                        output.AddRange((ReadOnlySpan<byte>)[0xFF, 0xE1]);
                        int size = exif.Length + 6 + 2;
                        output.Add((byte)(size >> 8));
                        output.Add((byte)size);
                        output.AddRange("Exif\0\0"u8.ToArray());
                        output.AddRange(exif);
                    }

                    break;
                case 0xE1:
                    keep = false;
                    removed.Add(body.IndexOf("ns.adobe.com/xap"u8) >= 0 ? "XMP" : "APP1 segment");
                    break;
                case 0xE2:
                    keep = body.StartsWith("ICC_PROFILE\0"u8);
                    if (!keep)
                    {
                        removed.Add(body.StartsWith("MPF\0"u8) ? "multi-picture index" : "APP2 segment");
                    }

                    break;
                case >= 0xE3 and <= 0xEF:
                    keep = false;
                    removed.Add(marker == 0xED ? "IPTC" : $"APP{marker - 0xE0} segment");
                    break;
                case 0xFE:
                    keep = false;
                    removed.Add("comment");
                    break;
                default:
                    keep = true;
                    break;
            }

            if (keep)
            {
                output.AddRange(f.AsSpan(pos, 2 + length));
            }

            pos += 2 + length;
        }

        throw new InvalidDataException("The JPEG has no image data.");
    }

    /// <summary>The offset just past the primary image's end marker: the first FF D9 in the entropy-coded data, where FF is otherwise always followed by 00 or a restart marker.</summary>
    private static int EndOfImage(byte[] f, int scan)
    {
        int i = scan + 2 + BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(scan + 2));
        while (i + 1 < f.Length)
        {
            if (f[i] == 0xFF && f[i + 1] == 0xD9)
            {
                return i + 2;
            }

            i++;
        }

        return f.Length;
    }

    /// <summary>A little-endian TIFF block holding only the kept tags, or null when nothing is kept or the source cannot be parsed.</summary>
    private static byte[]? RebuildExif(byte[] tiff, List<string> removed, List<string> kept)
    {
        try
        {
            bool little = tiff[0] == (byte)'I';
            var primary = ReadIfd(tiff, U32(tiff, 4, little), little);
            var exif = primary.TryGetValue(0x8769, out var pointer) ? ReadIfd(tiff, U32(pointer.Value, 0, little), little) : [];
            if (primary.ContainsKey(0x8825))
            {
                removed.Add("GPS");
            }

            int dropped = primary.Keys.Count(t => t is not 0x8769 and not 0x8825 && !KeptPrimary.Contains(t)) + exif.Keys.Count(t => !KeptExif.Contains(t));
            if (dropped > 0)
            {
                removed.Add($"{dropped} other EXIF fields");
            }

            if (U32(tiff, 4, little) is var ifd0 && ifd0 + 2 <= tiff.Length && NextIfd(tiff, ifd0, little) != 0)
            {
                removed.Add("thumbnail");
            }

            var outPrimary = primary.Where(e => KeptPrimary.Contains(e.Key)).Select(e => Convert(e.Key, e.Value, little)).ToList();
            var outExif = exif.Where(e => KeptExif.Contains(e.Key)).Select(e => Convert(e.Key, e.Value, little)).ToList();
            kept.AddRange(outPrimary.Concat(outExif).Select(e => Names[e.Tag]));
            if (outPrimary.Count == 0 && outExif.Count == 0)
            {
                return null;
            }

            if (outExif.Count > 0)
            {
                outPrimary.Add((0x8769, 4, 1, new byte[4]));
            }

            return WriteTiff(outPrimary, outExif);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException or InvalidDataException)
        {
            removed.Add("all EXIF, which could not be parsed");
            return null;
        }
    }

    private static uint NextIfd(byte[] t, uint offset, bool little) => U32(t, (int)offset + 2 + (12 * U16(t, (int)offset, little)), little);

    /// <summary>One IFD's entries by tag: type, count and the value's bytes, in the source's byte order.</summary>
    private static Dictionary<ushort, (ushort Type, uint Count, byte[] Value)> ReadIfd(byte[] t, uint offset, bool little)
    {
        var entries = new Dictionary<ushort, (ushort, uint, byte[])>();
        int count = U16(t, (int)offset, little);
        for (int k = 0; k < count; k++)
        {
            int e = (int)offset + 2 + (12 * k);
            ushort tag = U16(t, e, little), type = U16(t, e + 2, little);
            uint n = U32(t, e + 4, little);
            int size = type switch
            {
                1 or 2 or 6 or 7 => 1,
                3 or 8 => 2,
                4 or 9 or 11 => 4,
                5 or 10 or 12 => 8,
                _ => throw new InvalidDataException("unknown TIFF type"),
            };
            long bytes = size * (long)n;
            if (bytes > 1 << 20)
            {
                throw new InvalidDataException("an implausible TIFF count");
            }

            int at = bytes <= 4 ? e + 8 : (int)U32(t, e + 8, little);
            entries[tag] = (type, n, t.AsSpan(at, (int)bytes).ToArray());
        }

        return entries;
    }

    /// <summary>An entry's value in little-endian order, with ISO reduced to its first value.</summary>
    private static (ushort Tag, ushort Type, uint Count, byte[] Value) Convert(ushort tag, (ushort Type, uint Count, byte[] Value) entry, bool little)
    {
        var (type, count, value) = entry;
        if (tag == 0x8827 && count > 1)
        {
            count = 1;
            value = value[..2];
        }

        if (!little)
        {
            value = (byte[])value.Clone();
            int size = type switch { 3 or 8 => 2, 4 or 9 => 4, 5 or 10 => 4, _ => 1 };
            for (int i = 0; size > 1 && i + size <= value.Length; i += size)
            {
                Array.Reverse(value, i, size);
            }
        }

        return (tag, type, count, value);
    }

    private static byte[] WriteTiff(List<(ushort Tag, ushort Type, uint Count, byte[] Value)> primary, List<(ushort Tag, ushort Type, uint Count, byte[] Value)> exif)
    {
        primary.Sort((a, b) => a.Tag.CompareTo(b.Tag));
        exif.Sort((a, b) => a.Tag.CompareTo(b.Tag));
        uint primaryAt = 8;
        uint primaryEnd = primaryAt + 2 + (12 * (uint)primary.Count) + 4;
        uint primaryData = (uint)primary.Where(e => e.Value.Length > 4).Sum(e => (e.Value.Length + 1) & ~1);
        uint exifAt = primaryEnd + primaryData;
        var output = new List<byte>();
        void U16Out(int v) => output.AddRange(BitConverter.GetBytes((ushort)v));
        void U32Out(uint v) => output.AddRange(BitConverter.GetBytes(v));
        output.AddRange("II"u8.ToArray());
        U16Out(42);
        U32Out(primaryAt);

        void Ifd(List<(ushort Tag, ushort Type, uint Count, byte[] Value)> entries, uint start)
        {
            uint data = start + 2 + (12 * (uint)entries.Count) + 4;
            var overflow = new List<byte>();
            U16Out(entries.Count);
            foreach (var (tag, type, count, value) in entries)
            {
                U16Out(tag);
                U16Out(type);
                U32Out(count);
                if (tag == 0x8769)
                {
                    U32Out(exifAt);
                }
                else if (value.Length <= 4)
                {
                    output.AddRange(value);
                    output.AddRange(new byte[4 - value.Length]);
                }
                else
                {
                    U32Out(data + (uint)overflow.Count);
                    overflow.AddRange(value);
                    if (value.Length % 2 == 1)
                    {
                        overflow.Add(0);
                    }
                }
            }

            U32Out(0);
            output.AddRange(overflow);
        }

        Ifd(primary, primaryAt);
        if (exif.Count > 0)
        {
            Ifd(exif, exifAt);
        }

        return [.. output];
    }

    private static ScrubResult Png(byte[] f)
    {
        var output = new List<byte>(f.Length);
        output.AddRange(f.AsSpan(0, 8));
        var removed = new List<string>();
        int pos = 8;
        while (pos + 12 <= f.Length)
        {
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(pos));
            if (length < 0 || pos + 12 + length > f.Length)
            {
                throw new InvalidDataException("A PNG chunk runs past the end of the file.");
            }

            string type = Encoding.ASCII.GetString(f, pos + 4, 4);
            bool critical = char.IsUpper(type[0]);
            if (critical || KeptPngChunks.Contains(type))
            {
                output.AddRange(f.AsSpan(pos, 12 + length));
            }
            else
            {
                removed.Add(type + " chunk");
            }

            pos += 12 + length;
            if (type == "IEND")
            {
                if (pos < f.Length)
                {
                    removed.Add($"{f.Length - pos} bytes after the image's end");
                }

                break;
            }
        }

        return new ScrubResult([.. output], removed, []);
    }

    private static ushort U16(byte[] t, int at, bool little) =>
        little ? BinaryPrimitives.ReadUInt16LittleEndian(t.AsSpan(at)) : BinaryPrimitives.ReadUInt16BigEndian(t.AsSpan(at));

    private static uint U32(byte[] t, int at, bool little) =>
        little ? BinaryPrimitives.ReadUInt32LittleEndian(t.AsSpan(at)) : BinaryPrimitives.ReadUInt32BigEndian(t.AsSpan(at));
}
