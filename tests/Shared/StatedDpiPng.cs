namespace GroupLab.Tests.Support;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 481 part E: a plain sheet of paper as a flatbed scan saves it, a PNG that states its own resolution in its
/// pHYs chunk, for the tests of the scan's own resolution offered as the scale. No codes and no markers, so nothing else sets a scale.
/// </summary>
internal static class StatedDpiPng
{
    /// <summary>Writes a light grey <paramref name="width"/> by <paramref name="height"/> PNG stating <paramref name="dpi"/> both ways.</summary>
    public static string Write(string path, int width, int height, double dpi)
    {
        byte[] png;
        using (var mat = new OpenCvSharp.Mat(height, width, OpenCvSharp.MatType.CV_8UC1, OpenCvSharp.Scalar.All(235)))
        {
            png = mat.ImEncode(".png");
        }

        // The pHYs chunk goes straight after IHDR: the 8 byte signature, then IHDR's 4 byte length, 4 byte type, 13 bytes and 4 byte CRC.
        const int AfterHeader = 8 + 4 + 4 + 13 + 4;
        uint perMetre = (uint)Math.Round(dpi / 0.0254);
        var data = new byte[9];
        Big(data, 0, perMetre);
        Big(data, 4, perMetre);
        data[8] = 1;
        var chunk = new byte[4 + 4 + data.Length + 4];
        Big(chunk, 0, (uint)data.Length);
        "pHYs"u8.CopyTo(chunk.AsSpan(4));
        data.CopyTo(chunk, 8);
        Big(chunk, 8 + data.Length, Crc(chunk.AsSpan(4, 4 + data.Length)));
        File.WriteAllBytes(path, [.. png.AsSpan(0, AfterHeader), .. chunk, .. png.AsSpan(AfterHeader)]);
        return path;
    }

    private static void Big(byte[] into, int at, uint value)
    {
        into[at] = (byte)(value >> 24);
        into[at + 1] = (byte)(value >> 16);
        into[at + 2] = (byte)(value >> 8);
        into[at + 3] = (byte)value;
    }

    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }
}
