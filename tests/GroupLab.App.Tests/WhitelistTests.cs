using GroupLab.App.Diagnostics;
using GroupLab.Core.Imaging;
using GroupLab.Core.Publication;

namespace GroupLab.App.Tests;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 48 section 3: the publication scrubber and the diagnostic log judge the same data by the same rule, keep what
/// describes the camera and the exposure and drop where, when, who and anything typed, so their whitelists must name the same fields.
/// </summary>
public class WhitelistTests
{
    /// <summary>Each camera or exposure fact the log records, by the EXIF field the scrubber keeps for it.</summary>
    private static readonly Dictionary<string, string> LoggedAs = new()
    {
        ["make"] = "Make",
        ["model"] = "Model",
        ["orientation"] = "Orientation",
        ["exposure"] = "ExposureTime",
        ["fnumber"] = "FNumber",
        ["iso"] = "ISOSpeedRatings",
        ["focal"] = "FocalLength",
        ["w"] = "PixelXDimension",
        ["h"] = "PixelYDimension",
        ["zoom"] = "DigitalZoomRatio",
        ["focal35"] = "FocalLengthIn35mmFilm",
        ["lens"] = "LensModel",
    };

    /// <summary>Facts about the file rather than its metadata block: its format, and the resolution a scan states, which the scrubber keeps in the JFIF header or pHYs chunk.</summary>
    private static readonly string[] AboutTheFile = ["format", "dpi"];

    [Fact]
    public void TheLogRecordsExactlyTheCameraFactsThePublicationScrubberKeeps()
    {
        var logged = ImageFacts.Of(ImageMetadata.ForScan(10, 10, 300)).Select(f => f.Key).ToList();
        var unexplained = logged.Where(k => !LoggedAs.ContainsKey(k) && !AboutTheFile.Contains(k)).ToList();
        Assert.True(unexplained.Count == 0, "logged, and neither a kept field nor a fact about the file: " + string.Join(", ", unexplained));
        Assert.Equal(ImageScrubber.KeptFieldNames.Order(StringComparer.Ordinal), logged.Where(LoggedAs.ContainsKey).Select(k => LoggedAs[k]).Order(StringComparer.Ordinal));
    }
}
