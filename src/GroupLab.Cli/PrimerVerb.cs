using GroupLab.Core.Statistics;

namespace GroupLab.Cli;

/// <summary>
/// <c>grouplab primer [&lt;docs directory&gt;]</c>, NOTES-FROM-PLANNING.md entry 438 section 2.5: writes <see cref="PrimerNumbers"/> to
/// <c>docs/figures/primer/primer-data.json</c>, where the site reads the statistics primer's figures; <c>PrimerTests</c> holds the
/// committed file to it.
/// </summary>
public static class PrimerVerb
{
    public const string Usage = "grouplab primer [<docs directory>]";

    public static int Run(string docs, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);
        string folder = Path.Combine(docs, "figures", "primer");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "primer-data.json");
        File.WriteAllText(path, PrimerNumbers.Json());
        output.WriteLine($"Wrote {path}.");
        return 0;
    }
}
