using GroupLab.Core.Imaging;
using OpenCvSharp;

namespace GroupLab.Cli.Imaging;

/// <summary>
/// <see cref="Upright"/> for a colour picture: NOTES-FROM-PLANNING.md entry 362 sections 1 and 5. The same eight orientation values, done
/// with OpenCV's flips, transposes and quarter turns; a test holds the two to the same result.
/// </summary>
public static class UprightMat
{
    /// <summary>A new picture as the orientation tag says it reads; a copy where the tag is 1, missing or not a valid value.</summary>
    public static Mat Apply(Mat image, int? orientation)
    {
        ArgumentNullException.ThrowIfNull(image);
        var result = new Mat();
        switch (orientation)
        {
            case 2:
                Cv2.Flip(image, result, FlipMode.Y);
                break;
            case 3:
                Cv2.Rotate(image, result, RotateFlags.Rotate180);
                break;
            case 4:
                Cv2.Flip(image, result, FlipMode.X);
                break;
            case 5:
                Cv2.Transpose(image, result);
                break;
            case 6:
                Cv2.Rotate(image, result, RotateFlags.Rotate90Clockwise);
                break;
            case 7:
                using (var transposed = new Mat())
                {
                    Cv2.Transpose(image, transposed);
                    Cv2.Flip(transposed, result, FlipMode.XY);
                }

                break;
            case 8:
                Cv2.Rotate(image, result, RotateFlags.Rotate90Counterclockwise);
                break;
            default:
                image.CopyTo(result);
                break;
        }

        return result;
    }

    /// <summary>A quarter turn clockwise (or anticlockwise), as the Rotate button turns the picture.</summary>
    public static Mat Turn(Mat image, bool clockwise) => Apply(image, clockwise ? 6 : 8);
}
