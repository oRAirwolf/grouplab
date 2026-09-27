namespace GroupLab.Core.Trace;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 243 section 3.2: what an analysis is doing, in the plain words a progress line shows, one step at a time:
/// loading, reading the codes, finding the sheet, finding the bulls, finding the holes, measuring. The pipeline's stage names are for the
/// log and the timeline; these are for a person waiting.
/// </summary>
public static class StageWords
{
    /// <summary>What comes next once <paramref name="stage"/> has finished, or null where a stage says nothing new.</summary>
    public static string? After(string stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return stage switch
        {
            "S0.decode" => "Reading the sheet's codes…",
            "S0.identify" => "Finding the sheet from its markers…",
            "S3.register" => "Finding the bulls…",
            "P0.bulls" => "Finding the holes…",
            "S5-S8.holes" => "Measuring the group…",
            _ => null,
        };
    }

    /// <summary>The first line, before any stage has finished.</summary>
    public const string Starting = "Loading the picture…";
}
