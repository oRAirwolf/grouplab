namespace GroupLab.Core.Imaging;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 362 section 1: a photograph turned the way its EXIF Orientation tag says it reads, all eight values, and the
/// quarter turns of section 5's Rotate button. The analysis keeps its marks in stored pixels and turns only the view
/// (<see cref="Marking.ViewRotation"/>); the store-bought target's screens work on the turned picture itself, since what they keep is the
/// target upright, not the photograph.
/// </summary>
public static class Upright
{
    /// <summary>The size of the picture once turned, for one <paramref name="width"/> by <paramref name="height"/> as stored.</summary>
    public static (int Width, int Height) Size(int? orientation, int width, int height) =>
        orientation is 5 or 6 or 7 or 8 ? (height, width) : (width, height);

    /// <summary>Where the turned picture's pixel (<paramref name="x"/>, <paramref name="y"/>) comes from in the stored one.</summary>
    public static (int X, int Y) Source(int? orientation, int x, int y, int width, int height) => orientation switch
    {
        2 => (width - 1 - x, y),
        3 => (width - 1 - x, height - 1 - y),
        4 => (x, height - 1 - y),
        5 => (y, x),
        6 => (y, height - 1 - x),
        7 => (width - 1 - y, height - 1 - x),
        8 => (width - 1 - y, x),
        _ => (x, y),
    };

    /// <summary>The picture as its orientation tag says it reads; the same picture where the tag is 1, missing or not a valid value.</summary>
    public static GrayImage Apply(GrayImage image, int? orientation)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (orientation is not (>= 2 and <= 8))
        {
            return image;
        }

        var (w, h) = Size(orientation, image.Width, image.Height);
        var pixels = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var (sx, sy) = Source(orientation, x, y, image.Width, image.Height);
                pixels[(y * w) + x] = image[sx, sy];
            }
        }

        return new GrayImage(w, h, pixels);
    }

    /// <summary>A quarter turn clockwise (or anticlockwise), as section 5's Rotate button turns the picture.</summary>
    public static GrayImage Turn(GrayImage image, bool clockwise) => Apply(image, clockwise ? 6 : 8);

    /// <summary>
    /// A point in a picture <paramref name="width"/> by <paramref name="height"/> where it lands after a quarter turn, in pixel coordinates
    /// whose centres are whole numbers, so the point stays on the same pixel.
    /// </summary>
    public static PointD Turn(PointD p, bool clockwise, double width, double height) =>
        clockwise ? new PointD(height - 1 - p.Y, p.X) : new PointD(p.Y, width - 1 - p.X);

    /// <summary>A point on a target <paramref name="width"/> by <paramref name="height"/> inches, measured from its top left corner, after a quarter turn.</summary>
    public static PointD TurnInches(PointD p, bool clockwise, double width, double height) =>
        clockwise ? new PointD(height - p.Y, p.X) : new PointD(p.Y, width - p.X);
}
