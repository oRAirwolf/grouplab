using System.Buffers.Binary;
using System.Text;

namespace GroupLab.Core.Publication;

/// <summary>
/// What must never be published in an image, NOTES-FROM-PLANNING.md entry 22 section 3: a location. The check reads the bytes rather
/// than trusting how a file was made, and reports every place a location can hide in the formats the project takes: an EXIF GPS block,
/// GPS fields in XMP or in PNG text, and data after the image's end marker, where phones append video and trailers nothing here parses.
/// </summary>
public static class PublicationCheck
{
    /// <summary>The provenance record every published submission directory carries.</summary>
    public const string ProvenanceFile = "provenance.json";

    public static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    /// <summary>Every reason this image must not be published as it stands; empty when it may be.</summary>
    public static IReadOnlyList<string> LocationProblems(byte[] file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var problems = new List<string>();
        try
        {
            if (file.Length > 3 && file[0] == 0xFF && file[1] == 0xD8)
            {
                Jpeg(file, problems);
            }
            else if (file.Length > 8 && file[0] == 0x89 && file[1] == 0x50)
            {
                Png(file, problems);
            }
            else
            {
                problems.Add("not a JPEG or PNG, so it cannot be checked");
            }
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            problems.Add("its metadata could not be read to the end, so it cannot be shown to be clean");
        }

        return problems;
    }

    private static void Jpeg(byte[] f, List<string> problems)
    {
        int pos = 2;
        while (pos + 4 <= f.Length && f[pos] == 0xFF)
        {
            byte marker = f[pos + 1];
            if (marker == 0xDA)
            {
                int i = pos + 2 + BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(pos + 2));
                while (i + 1 < f.Length && !(f[i] == 0xFF && f[i + 1] == 0xD9))
                {
                    i++;
                }

                if (i + 2 < f.Length)
                {
                    problems.Add($"{f.Length - i - 2} bytes after the image's end marker");
                }

                return;
            }

            int length = BinaryPrimitives.ReadUInt16BigEndian(f.AsSpan(pos + 2));
            var body = f.AsSpan(pos + 4, length - 2);
            if (marker == 0xE1 && body.StartsWith("Exif\0\0"u8) && HasGpsIfd(body[6..]))
            {
                problems.Add("an EXIF GPS block");
            }
            else if (marker == 0xE1 && body.IndexOf("ns.adobe.com/xap"u8) >= 0 && body.IndexOf("GPS"u8) >= 0)
            {
                problems.Add("GPS fields in XMP");
            }

            pos += 2 + length;
        }
    }

    private static bool HasGpsIfd(ReadOnlySpan<byte> t)
    {
        bool little = t[0] == (byte)'I';
        uint ifd0 = little ? BinaryPrimitives.ReadUInt32LittleEndian(t[4..]) : BinaryPrimitives.ReadUInt32BigEndian(t[4..]);
        int count = little ? BinaryPrimitives.ReadUInt16LittleEndian(t[(int)ifd0..]) : BinaryPrimitives.ReadUInt16BigEndian(t[(int)ifd0..]);
        for (int k = 0; k < count; k++)
        {
            var entry = t[((int)ifd0 + 2 + (12 * k))..];
            int tag = little ? BinaryPrimitives.ReadUInt16LittleEndian(entry) : BinaryPrimitives.ReadUInt16BigEndian(entry);
            if (tag == 0x8825)
            {
                return true;
            }
        }

        return false;
    }

    private static void Png(byte[] f, List<string> problems)
    {
        int pos = 8;
        while (pos + 12 <= f.Length)
        {
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(f.AsSpan(pos));
            string type = Encoding.ASCII.GetString(f, pos + 4, 4);
            var body = f.AsSpan(pos + 8, length);
            if (type == "eXIf" && HasGpsIfd(body))
            {
                problems.Add("an EXIF GPS block in eXIf");
            }
            else if (type is "tEXt" or "iTXt" or "zTXt" && body.IndexOf("GPS"u8) >= 0)
            {
                problems.Add($"GPS in a {type} chunk");
            }

            pos += 12 + length;
            if (type == "IEND")
            {
                if (pos < f.Length)
                {
                    problems.Add($"{f.Length - pos} bytes after the image's end");
                }

                return;
            }
        }
    }
}
