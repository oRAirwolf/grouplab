using GroupLab.Core.Publication;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab scrub &lt;input&gt; &lt;output&gt;</c>: one image through <see cref="ImageScrubber"/>, the authoritative scrubber
/// (NOTES-FROM-PLANNING.md entry 29 section 2), which is what the history rewrite of entry 29 uses. It refuses to write a result that
/// <see cref="PublicationCheck"/> still finds a location in, and prints what it removed and kept.
/// </summary>
internal static class ScrubVerb
{
    public static int Run(string input, string output, TextWriter console)
    {
        ScrubResult result;
        try
        {
            result = ImageScrubber.Scrub(File.ReadAllBytes(input));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
        {
            console.WriteLine($"scrub: {input}: {ex.Message}");
            return 1;
        }

        if (PublicationCheck.LocationProblems(result.Bytes) is { Count: > 0 } problems)
        {
            console.WriteLine($"scrub: {input} still carries {string.Join(", ", problems)}; nothing written");
            return 1;
        }

        File.WriteAllBytes(output, result.Bytes);
        console.WriteLine($"{input}: removed {(result.Removed.Count == 0 ? "nothing" : string.Join(", ", result.Removed))}; kept {(result.Kept.Count == 0 ? "no EXIF" : string.Join(", ", result.Kept))}");
        return 0;
    }
}
