using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace GroupLab.Core.Printing.Thermal;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 363 section 2a: a thermal page as a black and white PNG, one pixel to a printer dot, with its resolution
/// written in (the pHYs chunk, pixels a metre), so a printer's own app that honours it prints the page at its true size. One bit a pixel,
/// grey palette, 0 black and 1 white, so it stays small and nothing can come out grey.
/// </summary>
public static class ThermalPng
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Write(DotImage image, double dotsPerInch)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dotsPerInch);
        using var output = new MemoryStream();
        output.Write(Signature);

        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)image.Width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)image.Height);
        header[8] = 1;
        header[9] = 0;
        Chunk(output, "IHDR", header);

        var physical = new byte[9];
        uint perMetre = (uint)Math.Round(dotsPerInch / 0.0254);
        BinaryPrimitives.WriteUInt32BigEndian(physical, perMetre);
        BinaryPrimitives.WriteUInt32BigEndian(physical.AsSpan(4), perMetre);
        physical[8] = 1;
        Chunk(output, "pHYs", physical);

        // Each row: filter type 0, then the row with black as 0, as PNG's greyscale takes it; the bits past the width are white.
        byte[] rows = image.Inverted();
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            for (int y = 0; y < image.Height; y++)
            {
                z.WriteByte(0);
                z.Write(rows, y * image.RowBytes, image.RowBytes);
            }
        }

        Chunk(output, "IDAT", packed.ToArray());
        Chunk(output, "IEND", []);
        return output.ToArray();
    }

    /// <summary>The resolution a PNG says it has, dots an inch across, or null where it says none.</summary>
    public static double? Dpi(byte[] png)
    {
        ArgumentNullException.ThrowIfNull(png);
        for (int at = 8; at + 12 <= png.Length;)
        {
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at));
            string type = Encoding.ASCII.GetString(png, at + 4, 4);
            if (type == "pHYs" && length == 9 && png[at + 16] == 1)
            {
                return BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8)) * 0.0254;
            }

            at += 12 + length;
        }

        return null;
    }

    private static void Chunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        output.Write(length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc([.. typeBytes, .. data]));
        output.Write(crc);
    }

    private static readonly uint[] Table = Enumerable.Range(0, 256).Select(n =>
    {
        uint c = (uint)n;
        for (int k = 0; k < 8; k++)
        {
            c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        }

        return c;
    }).ToArray();

    private static uint Crc(byte[] bytes)
    {
        uint c = 0xFFFFFFFFu;
        foreach (byte b in bytes)
        {
            c = Table[(c ^ b) & 0xFF] ^ (c >> 8);
        }

        return c ^ 0xFFFFFFFFu;
    }
}
