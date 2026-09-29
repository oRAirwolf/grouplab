using Foundation;
using ImageIO;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 290 section 2 item 5: the camera's picture written as a JPEG. The camera is asked for a JPEG, and gives
/// one; where it gives its own HEIC instead, the picture is turned into a JPEG here, through ImageIO, with its orientation and the rest of
/// its description carried over, because OpenCV on iOS reads no HEIC and the picture is read by OpenCV.
/// </summary>
internal static class StillFile
{
    /// <summary>The quality a converted picture is written at, as Android's camera writes its own.</summary>
    internal const float Quality = 0.95f;

    /// <summary>Whether the bytes are a JPEG already: every JPEG starts FF D8 FF.</summary>
    internal static bool IsJpeg(ReadOnlySpan<byte> start) => start.Length >= 3 && start[0] == 0xFF && start[1] == 0xD8 && start[2] == 0xFF;

    /// <summary>Writes the picture to <paramref name="path"/> as a JPEG, as it is where it is one and converted where not; false where it could not.</summary>
    internal static bool WriteJpeg(NSData data, string path)
    {
        byte[] bytes = data.ToArray();
        if (IsJpeg(bytes))
        {
            File.WriteAllBytes(path, bytes);
            return true;
        }

        using var source = CGImageSource.FromData(data);
        if (source is null || source.ImageCount < 1)
        {
            return false;
        }

        File.Delete(path);
        using var url = NSUrl.FromFilename(path);
        using var destination = CGImageDestination.Create(url, "public.jpeg", 1);
        if (destination is null)
        {
            return false;
        }

        destination.AddImage(source, 0, new CGImageDestinationOptions { LossyCompressionQuality = Quality });
        return destination.Close() && File.Exists(path);
    }
}
