using Foundation;
using ImageIO;

namespace GroupLab.iOS;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 292 section 2.1 and 2.4: a picture from Photos, Files or another app made readable by OpenCV. OpenCV on iOS
/// reads JPEG and PNG only, so anything else, an iPhone's own HEIC above all, is written as a JPEG through ImageIO at the camera's quality
/// (<see cref="StillFile"/>), at its full size, with its orientation and the rest of its description carried over. The location is not
/// carried: the new picture is written without it, and nothing here reads it.
/// </summary>
internal static class PhotoFiles
{
    /// <summary>Whether the bytes are a PNG: every PNG starts 89 50 4E 47.</summary>
    internal static bool IsPng(ReadOnlySpan<byte> start) => start.Length >= 4 && start[0] == 0x89 && start[1] == 0x50 && start[2] == 0x4E && start[3] == 0x47;

    /// <summary>The ending a picture will have once readable: a PNG stays one, everything else becomes a JPEG.</summary>
    internal static string Ending(string? extensionOrType)
    {
        string said = (extensionOrType ?? "").ToLowerInvariant();
        return said.EndsWith("png", StringComparison.Ordinal) ? ".png" : ".jpg";
    }

    /// <summary>
    /// The picture at <paramref name="path"/> as a JPEG or PNG: the same file where it already is one, else a JPEG written beside it and the
    /// original deleted. Where ImageIO cannot read it either, the file is left as it is and the decode says it is not a picture.
    /// </summary>
    internal static string Readable(string path)
    {
        var start = new byte[4];
        int got;
        using (var file = File.OpenRead(path))
        {
            got = file.ReadAtLeast(start, start.Length, throwOnEndOfStream: false);
        }

        if (StillFile.IsJpeg(start.AsSpan(0, got)) || IsPng(start.AsSpan(0, got)))
        {
            return path;
        }

        using var url = NSUrl.FromFilename(path);
        using var source = CGImageSource.FromUrl(url);
        if (source is null || source.ImageCount < 1)
        {
            return path;
        }

        string jpeg = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "-jpeg.jpg");
        File.Delete(jpeg);
        using var to = NSUrl.FromFilename(jpeg);
        using var destination = CGImageDestination.Create(to, "public.jpeg", 1);
        if (destination is null)
        {
            return path;
        }

        // The quality as the camera's, and the location set to nothing, which ImageIO takes as leaving it out of the copy.
        using var options = new NSMutableDictionary
        {
            [new NSString("kCGImageDestinationLossyCompressionQuality")] = NSNumber.FromFloat(StillFile.Quality),
            [new NSString("{GPS}")] = NSNull.Null,
        };
        destination.AddImage(source, 0, options);
        if (!destination.Close() || !File.Exists(jpeg))
        {
            return path;
        }

        File.Delete(path);
        GroupLab.App.Diagnostics.DiagnosticLog.Info("ios.photo.converted", ("to", "jpeg"));
        return jpeg;
    }

    /// <summary>A stream of a picture made readable, which deletes the file when it is closed: nothing handed over is kept.</summary>
    internal static Stream OpenOnce(string path) =>
        new FileStream(Readable(path), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.DeleteOnClose);
}
