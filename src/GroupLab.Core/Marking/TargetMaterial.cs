namespace GroupLab.Core.Marking;

/// <summary>
/// What a target was shot on, NOTES-FROM-PLANNING.md entry 162 section 3.2: the paper and what was behind it.
/// <para>
/// <b>Why these are fields at all.</b> A hole measures anywhere from 0.76 to 1.14 of the bullet that made it on the scans measured so far,
/// and the friend's sheet that measured 1.14 was card stock over cardboard where the others were not. Paper and backing changed together
/// with everything else between those sheets, so no measurement yet can say which of them did it. Without them recorded, every future
/// measurement of that ratio is confounded the same way, and entry 158's controlled test needs exactly these two for every sheet.
/// </para>
/// <para>
/// Plain choices rather than free text, so two sheets on the same paper say so in the same words. Both are optional: a blank is the
/// honest answer from somebody who does not know, and it is never guessed.
/// </para>
/// </summary>
public static class TargetMaterial
{
    /// <summary>The paper a sheet was printed on.</summary>
    public static IReadOnlyList<string> Papers { get; } = ["copy paper", "card stock", "other"];

    /// <summary>What was behind the sheet when it was shot.</summary>
    public static IReadOnlyList<string> Backings { get; } = ["cardboard", "foam board", "none", "other"];

    /// <summary>The value as recorded: one of the choices, or null for anything else, so a file edited by hand cannot invent a sixth.</summary>
    public static string? Paper(string? value) => value is not null && Papers.Contains(value) ? value : null;

    /// <inheritdoc cref="Paper(string?)"/>
    public static string? Backing(string? value) => value is not null && Backings.Contains(value) ? value : null;
}
