using System.Globalization;

namespace GroupLab.Core.Capture;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 226 section 5.2: whether a sheet can be photographed whole, worked out rather than hoped. A phone photograph
/// with the sheet filling nine tenths of the frame gives so many pixels an inch; the quality score reads 120 as good and 90 as usable
/// (<see cref="CaptureQualities"/>), a marker's 0.5 mm module wants about 3 pixels, 150 an inch, and a code's 0.4 mm module about 5, 300 an
/// inch, below which the sheet is chosen by hand rather than read from its code.
/// </summary>
public static class PhotographLimit
{
    /// <summary>A 12 MP and a 50 MP phone camera, 4:3, full resolution: a 50 MP phone saves 12 MP unless its full resolution mode is on.</summary>
    public static readonly (string Name, int Long, int Short)[] Cameras = [("12 MP", 4000, 3000), ("50 MP", 8160, 6120)];

    /// <summary>How much of the frame a sheet fills in a real photograph, with a margin round it to be sure it is all in.</summary>
    public const double Fill = 0.9;

    public const double Good = 120, Markers = 150, Codes = 300;

    /// <summary>The largest sheet a flatbed takes whole, in inches: A4's 8.27 by 11.69 and Letter's 8.5 by 11 both fit it.</summary>
    public const double FlatbedShort = 8.5, FlatbedLong = 11.7;

    /// <summary>Pixels an inch across the sheet for each camera.</summary>
    public static IReadOnlyList<(string Camera, double PixelsPerInch)> PixelsPerInch(double widthInches, double heightInches)
    {
        double longSide = Math.Max(widthInches, heightInches), shortSide = Math.Min(widthInches, heightInches);
        return [.. Cameras.Select(c => (c.Name, Fill * Math.Min(c.Long / longSide, c.Short / shortSide)))];
    }

    /// <summary>Whether a flatbed takes the sheet whole.</summary>
    public static bool FitsAFlatbed(double widthInches, double heightInches) =>
        Math.Min(widthInches, heightInches) <= FlatbedShort + 0.01 && Math.Max(widthInches, heightInches) <= FlatbedLong + 0.01;

    /// <summary>What to tell a person about a sheet too large for a flatbed, or null for one a flatbed takes.</summary>
    public static string? Advice(double widthInches, double heightInches)
    {
        if (FitsAFlatbed(widthInches, heightInches))
        {
            return null;
        }

        var ppi = PixelsPerInch(widthInches, heightInches);
        // Entry 328 section 2: rounded down, so 149.6 is never "150" beside "below the 150".
        string Of(int i) => string.Create(CultureInfo.InvariantCulture, $"{ppi[i].Camera} {Math.Floor(ppi[i].PixelsPerInch):0}");
        string size = string.Create(CultureInfo.InvariantCulture, $"This sheet is {widthInches:0.#} by {heightInches:0.#} in, too large for a flatbed scanner.");
        string numbers = $"Photographed whole, a phone gives about {Of(0)} and {Of(1)} pixels an inch.";
        return ppi[0].PixelsPerInch >= Markers
            ? $"{size} {numbers} A 12 MP photograph of the whole sheet is enough; its codes may not read at that size, and then you choose the sheet from the list."
            : ppi[1].PixelsPerInch >= Markers
                ? $"{size} {numbers} Photograph it whole only at a 50 MP phone's full resolution; a 12 MP photograph is below the {(ppi[0].PixelsPerInch < Good ? $"{Good:0} GroupLab needs for a good reading" : $"{Markers:0} GroupLab needs to read its markers")}. Tiled Letter or A4 pages are the better choice for a large target."
                : $"{size} {numbers} That is too few to read its markers from one photograph: print tiled Letter or A4 pages instead, or photograph it in pieces.";
    }

    /// <summary>
    /// Entry 226 section 5, shared with the phone by entry 258: what a sheet too large for a flatbed means for scanning or photographing it,
    /// and that tiled Letter or A4 pages are the better choice for a large target; null for a sheet a flatbed takes.
    /// </summary>
    public static string? ForSheet(GroupLab.Core.Gltd.Model.TargetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var t = definition.Tiling;
        if (t is not null)
        {
            return "Each sheet scans on a flatbed by itself, and the sheets can be printed on one large page with cut lines for a plotter.";
        }

        return Advice(definition.Page.Width / 254.0, definition.Page.Height / 254.0);
    }
}
