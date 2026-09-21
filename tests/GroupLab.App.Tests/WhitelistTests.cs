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

    /// <summary>
    /// NOTES-FROM-PLANNING.md entry 129 section 3.5.2a: the quarantine worker rebuilds an uploaded photograph from its pixels and writes the
    /// camera facts back in freshly. It needs the same whitelist, and the entry is explicit that there must not be a third copy of that list
    /// deciding anything.
    /// <para>
    /// The worker is python on a server, so it cannot share the constant; what it can do is be held to it. If the two ever drift, the
    /// failure would be silent and one-way: a field dropped here is a measurement GroupLab can no longer make from a donated photograph, and
    /// a field added there is something kept from a stranger's file that nobody decided to keep.
    /// </para>
    /// </summary>
    [Fact]
    public void TheQuarantineWorkerKeepsExactlyWhatTheScrubberKeeps()
    {
        string worker = File.ReadAllText(Path.Combine(Repository(), "website", "server", "grouplab-intake-worker.py"));

        int from = worker.IndexOf("KEPT = [", StringComparison.Ordinal);
        Assert.True(from >= 0, "the worker no longer has a KEPT list, so this cannot hold it to anything");
        int to = worker.IndexOf(']', from);

        var kept = worker[from..to]
            .Split('"')
            .Where((_, i) => i % 2 == 1)
            .ToList();

        Assert.Equal(ImageScrubber.KeptFieldNames.Order(StringComparer.Ordinal), kept.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// And the things that must never survive the rebuild. Named here rather than implied, because the consent text promises that GPS is
    /// removed, and after entry 129 that promise is kept on the server rather than on Alan's machine.
    /// </summary>
    [Fact]
    public void TheQuarantineWorkerKeepsNothingAboutWhereOrWhenOrWho()
    {
        string worker = File.ReadAllText(Path.Combine(Repository(), "website", "server", "grouplab-intake-worker.py"));

        int from = worker.IndexOf("KEPT = [", StringComparison.Ordinal);
        var kept = worker[from..worker.IndexOf(']', from)].Split('"').Where((_, i) => i % 2 == 1).ToList();

        foreach (string never in new[] { "GPS", "DateTime", "Artist", "Copyright", "Owner", "Serial", "MakerNote", "XMP", "UserComment", "Software" })
        {
            Assert.DoesNotContain(kept, k => k.Contains(never, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static string Repository([System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));
}
