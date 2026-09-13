namespace GroupLab.Core.Imaging;

/// <summary>An 8-bit single-channel raster, row-major, 0 black and 255 white.</summary>
public sealed class GrayImage
{
    public GrayImage(int width, int height, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(pixels);
        if (pixels.LongLength != (long)width * height)
        {
            throw new ArgumentException(
                $"Expected {(long)width * height} pixels for {width} x {height}, got {pixels.LongLength}.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    public byte this[int x, int y] => Pixels[(y * Width) + x];
}
