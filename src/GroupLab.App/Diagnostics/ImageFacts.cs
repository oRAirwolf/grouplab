using GroupLab.Core.Imaging;

namespace GroupLab.App.Diagnostics;

/// <summary>
/// The only facts about an image the log may record, NOTES-FROM-PLANNING.md entry 41 section 2: a whitelist in one place, for the reason
/// <c>scrub_exif.py</c> uses one. A log must never contain a GPS coordinate or the contents of a photograph's metadata block, so nothing
/// here reads the file: it takes the <see cref="ImageMetadata"/> GroupLab already measures with, which holds no location, no timestamp,
/// no maker note and no free text field, and picks from it by name.
/// <para>
/// Entry 41 permits more than GroupLab reads today (bit depth, channels, lens model, ISO, exposure and the count of EXIF tags). Those are
/// left out rather than read afresh, because reading more of a photograph's metadata for the sake of a log is the step the rule exists to
/// stop.
/// </para>
/// </summary>
public static class ImageFacts
{
    public static IReadOnlyList<(string Key, object? Value)> Of(ImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return
        [
            ("format", metadata.Format),
            ("w", metadata.Width),
            ("h", metadata.Height),
            ("dpi", metadata.DpiX),
            ("orientation", metadata.Orientation),
            ("make", metadata.CameraMake),
            ("model", metadata.CameraModel),
            ("focal", metadata.FocalLengthMm),
            ("focal35", metadata.FocalLength35mm),
            ("fnumber", metadata.FNumber),
            ("zoom", metadata.DigitalZoomRatio),
        ];
    }
}
