using System.Globalization;
using GroupLab.Core.Imaging;

namespace GroupLab.Core.Marking;

/// <summary>
/// A scale the image file itself states, offered rather than applied, NOTES-FROM-PLANNING.md entry 130 section 4.1 from entry 120.
/// </summary>
/// <param name="DotsPerInch">What the file says its resolution is.</param>
/// <param name="Says">The offer, in words, naming the number so the person can judge it.</param>
/// <param name="Reference">The scale it would set, if the person accepts it.</param>
public sealed record StatedScale(double DotsPerInch, string Says, LengthReference Reference);

/// <summary>
/// NOTES-FROM-PLANNING.md entry 130 section 4.1: on a blank sheet, where there are no printed markers to measure from, a scan usually says
/// its own resolution, and that is a scale.
/// <para>
/// <b>It is offered and never applied by itself.</b> A scale decides what every figure means: get it wrong and a group of one inch reads as
/// two, and nothing on the screen looks unusual. A scanner that states 600 dpi is almost always right, but "almost always" is not a basis
/// for silently deciding what a person's measurements mean, and the cost of being wrong is carried entirely by them.
/// </para>
/// <para>
/// So the number is shown, the person accepts or refuses it, and refusing leaves them exactly where they were: measuring a known distance by
/// hand, which is what they would have done anyway.
/// </para>
/// </summary>
public static class StatedResolutionScale
{
    /// <summary>
    /// Below this, a stated resolution is more likely to be a default nobody set than a measurement. 72 and 96 are what an image gets when
    /// the thing that wrote it had nothing to say.
    /// </summary>
    public const double LowestBelievable = 100;

    /// <summary>Above this it is not a scan of a sheet of paper, whatever it says.</summary>
    public const double HighestBelievable = 4800;

    /// <summary>
    /// The offer, or null where the file says nothing worth offering: a photograph, no resolution at all, or a number that is a default
    /// rather than a measurement.
    /// </summary>
    /// <param name="metadata">What the file says about itself.</param>
    /// <param name="width">The image's width in pixels, used to set the reference across a sensible part of it.</param>
    public static StatedScale? Offer(ImageMetadata metadata, int width)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        // A photograph's stated resolution describes the file, not the paper: it says nothing about how far the camera was from the sheet.
        if (metadata.IsCamera)
        {
            return null;
        }

        if (metadata.DpiX is not { } dpi || dpi < LowestBelievable || dpi > HighestBelievable)
        {
            return null;
        }

        // One inch across, at the resolution stated, placed where a person can see both ends of it.
        double y = Math.Max(1, width / 20.0);
        var reference = new LengthReference(new PointD(y, y), new PointD(y + dpi, y), 1);

        string says = string.Create(CultureInfo.InvariantCulture,
            $"{dpi:0} dpi, stated by the file. Use it as the scale, or measure a known distance yourself instead.");

        return new StatedScale(dpi, says, reference);
    }

    /// <summary>
    /// Whether the two axes disagree enough to be worth saying so. A scan whose horizontal and vertical resolutions differ has been
    /// stretched, and one number cannot describe it.
    /// </summary>
    public static string? Stretched(ImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (metadata.DpiX is not { } x || metadata.DpiY is not { } y || x <= 0 || y <= 0)
        {
            return null;
        }

        double difference = Math.Abs(x - y) / Math.Max(x, y);
        return difference < 0.01
            ? null
            : string.Create(CultureInfo.InvariantCulture, $"The file says {x:0} dpi across and {y:0} dpi down, so it has been stretched one way. One scale cannot describe it: measure a known distance in each direction, or scan it again.");
    }
}
