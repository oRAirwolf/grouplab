using System.Globalization;
using System.Text;
using GroupLab.Core.Imaging;
using GroupLab.Core.Registration;
using OpenCvSharp;

namespace GroupLab.Core.Tests.Support;

/// <summary>
/// What each of OpenCV's QR detectors found in an image at each resolution <see cref="SheetIdentification"/> tries, for a failure message.
/// The detectors come from a different native build on each platform, and a test that fails only on a CI runner has to say which step came
/// back empty: the WeChat detector's boxes, the plain decoder at those boxes, or the plain detector alone.
/// </summary>
internal static class CodeDiagnostics
{
    public static string Describe(GrayImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var text = new StringBuilder();
        using var full = Mat.FromPixelData(image.Height, image.Width, MatType.CV_8UC1, image.Pixels);
        using var wechat = new WeChatQRCode("", "", "", "");
        using var plain = new QRCodeDetector();
        foreach (double scale in SheetIdentification.Scales)
        {
            using var input = new Mat();
            Cv2.Resize(full, input, new Size(0, 0), scale, scale, scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
            string[] texts = wechat.DetectAndDecode(input, out Point2f[][] boxes);
            var atBoxes = boxes.Select(b => (plain.Decode(input, b) ?? "").Length).ToList();
            bool found = plain.DetectMulti(input, out Point2f[] corners);
            string?[] decoded = [];
            if (found)
            {
                plain.DecodeMulti(input, corners, out decoded);
            }

            text.Append(CultureInfo.InvariantCulture, $"\n  at {scale} ({input.Width}x{input.Height}): WeChat {boxes.Length} boxes, text lengths [{string.Join(",", texts.Select(t => t.Length))}]; plain decoder at those boxes [{string.Join(",", atBoxes)}]; plain detector alone found {found}, {corners.Length / 4} codes, decoded lengths [{string.Join(",", decoded.Select(d => d?.Length ?? 0))}]");
        }

        return text.ToString();
    }
}
