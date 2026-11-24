using System.Buffers.Binary;
using System.IO.Compression;
using GroupLab.Core.Printing.Thermal;

namespace GroupLab.Core.Reporting;

/// <summary>
/// A picture in color as a PNG, eight bits a channel, with its resolution written in (the pHYs chunk), so the exported comparison of
/// suggestion 61 is one file that any phone, computer or chat window shows. Written by GroupLab itself, as the thermal page is, so the phone
/// and the computer make the same bytes.
/// </summary>
public static class ColourPng
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The PNG of a picture given as three channels interleaved blue, green, red, as <see cref="Rendering.SceneRasterizer.RasterizeBgr"/> draws it.</summary>
    public static byte[] Write(int width, int height, byte[] bgr, double dotsPerInch)
    {
        ArgumentNullException.ThrowIfNull(bgr);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dotsPerInch);
        if (bgr.Length != width * height * 3)
        {
            throw new ArgumentException("three bytes are needed for each pixel", nameof(bgr));
        }

        using var output = new MemoryStream();
        output.Write(Signature);
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
        header[8] = 8;
        header[9] = 2;
        ThermalPng.Chunk(output, "IHDR", header);

        var physical = new byte[9];
        uint perMetre = (uint)Math.Round(dotsPerInch / 0.0254);
        BinaryPrimitives.WriteUInt32BigEndian(physical, perMetre);
        BinaryPrimitives.WriteUInt32BigEndian(physical.AsSpan(4), perMetre);
        physical[8] = 1;
        ThermalPng.Chunk(output, "pHYs", physical);

        // Each row: filter type 0, then red, green, blue for each pixel.
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
        {
            var row = new byte[(width * 3) + 1];
            for (int y = 0; y < height; y++)
            {
                row[0] = 0;
                for (int x = 0, i = y * width * 3; x < width; x++, i += 3)
                {
                    row[1 + (3 * x)] = bgr[i + 2];
                    row[2 + (3 * x)] = bgr[i + 1];
                    row[3 + (3 * x)] = bgr[i];
                }

                z.Write(row);
            }
        }

        ThermalPng.Chunk(output, "IDAT", packed.ToArray());
        ThermalPng.Chunk(output, "IEND", []);
        return output.ToArray();
    }
}
