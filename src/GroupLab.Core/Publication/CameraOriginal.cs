using System.Text.RegularExpressions;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Publication;

/// <summary>
/// Whether a file is a camera original, NOTES-FROM-PLANNING.md entry 35 section 1. Public test data exists to give the lens and surface
/// work frames whose camera geometry is known. A screenshot, or a copy that went through a messaging app, has lost its lens information
/// and had its pixels recompressed, so it cannot contribute whoever took the photograph, and it is held by default. A file name that a
/// screenshot tool or messaging app writes is the cheap signal; a missing camera make is the stronger one. Either is enough.
/// </summary>
public static partial class CameraOriginal
{
    /// <summary>Why the file is not a camera original, or null when it may be one. Every name the file has gone by is checked.</summary>
    public static string? Problem(byte[] file, params string?[] names)
    {
        ArgumentNullException.ThrowIfNull(file);
        foreach (string? name in names)
        {
            if (name is not null && CopyName().Match(name) is { Success: true } match)
            {
                string source = match.Groups["screenshot"].Success ? "a screenshot tool" : match.Groups["signal"].Success ? "the Signal messenger" : match.Groups["whatsapp"].Success ? "WhatsApp" : "Facebook";
                return $"not a camera original: {name} is a name {source} writes, so the file is a copy without its lens information and with recompressed pixels";
            }
        }

        return ImageMetadataReader.Read(file).CameraMake is { Length: > 0 }
            ? null
            : "not a camera original: it has no camera make, so nothing records the camera or lens that took it";
    }

    [GeneratedRegex(@"(?:^|[_\-\s])(?:(?<screenshot>screen[ _-]?shot)|(?<signal>signal-)|(?<whatsapp>IMG-\d{8}-WA\d+)|(?<facebook>FB_IMG_))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CopyName();
}
