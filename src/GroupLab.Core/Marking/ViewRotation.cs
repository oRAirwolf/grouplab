using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// How the marking screen turns the image for display, in quarter turns clockwise: NOTES-FROM-PLANNING.md entry 26 and entry 24
/// section 4. Rotation is a property of the view and never of the image. Every mark lives in the stored pixel frame, x right and y
/// down from the top left of the file's pixel grid as decoded with EXIF orientation not applied, and this maps between that frame
/// and the turned frame the screen draws, so turning the view changes where a mark is drawn and never what it is.
/// </summary>
public static class ViewRotation
{
    /// <summary>The canonical frame's name, recorded in the export (entry 26 point 3).</summary>
    public const string StoredPixelFrame = "stored-pixels";

    public static int Normalise(int quarterTurns) => ((quarterTurns % 4) + 4) % 4;

    /// <summary>
    /// The quarter turns that display an image as its EXIF Orientation tag asks: 3 is a half turn, 6 a quarter turn clockwise and 8 a
    /// quarter turn anticlockwise. The mirrored values, 2, 4, 5 and 7, are not applied (entry 26, "Not asking for"), and nor is a
    /// missing tag, which is what a flatbed scan carries.
    /// </summary>
    public static int FromExifOrientation(int? tag) => tag switch
    {
        3 => 2,
        6 => 1,
        8 => 3,
        _ => 0,
    };

    /// <summary>True for the EXIF Orientation values that include a mirror, which the screen does not apply.</summary>
    public static bool ExifMirrors(int? tag) => tag is 2 or 4 or 5 or 7;

    /// <summary>The turned image's size, for an image <paramref name="width"/> by <paramref name="height"/> in its stored pixels.</summary>
    public static (double Width, double Height) DisplaySize(int quarterTurns, double width, double height) =>
        Normalise(quarterTurns) % 2 == 0 ? (width, height) : (height, width);

    /// <summary>A stored-pixel point in the turned frame.</summary>
    public static PointD ToDisplay(PointD image, int quarterTurns, double width, double height)
    {
        var (a, b, c, d, e, f) = Affine(quarterTurns, width, height);
        return new PointD((a * image.X) + (b * image.Y) + c, (d * image.X) + (e * image.Y) + f);
    }

    /// <summary>A direction in stored-pixel axes as the turned view shows it: the same map without its translation.</summary>
    public static PointD VectorToDisplay(PointD vector, int quarterTurns)
    {
        var (a, b, _, d, e, _) = Affine(quarterTurns, 0, 0);
        return new PointD((a * vector.X) + (b * vector.Y), (d * vector.X) + (e * vector.Y));
    }

    /// <summary>A point of the turned frame in stored pixels.</summary>
    public static PointD ToImage(PointD display, int quarterTurns, double width, double height) => Normalise(quarterTurns) switch
    {
        1 => new PointD(display.Y, height - display.X),
        2 => new PointD(width - display.X, height - display.Y),
        3 => new PointD(width - display.Y, display.X),
        _ => display,
    };

    /// <summary>
    /// The map from stored pixels to the turned frame as affine coefficients, display x = A x + B y + C and display y = D x + E y + F,
    /// so the screen can draw the image and its marks through one transform.
    /// </summary>
    public static (double A, double B, double C, double D, double E, double F) Affine(int quarterTurns, double width, double height) => Normalise(quarterTurns) switch
    {
        1 => (0, -1, height, 1, 0, 0),
        2 => (-1, 0, width, 0, -1, height),
        3 => (0, 1, 0, -1, 0, width),
        _ => (1, 0, 0, 0, 1, 0),
    };
}
