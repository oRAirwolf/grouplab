using System.Text;

namespace GroupLab.Core.Updates;

/// <summary>
/// What to show somebody who skipped some builds, NOTES-FROM-PLANNING.md entry 138 section 5.
/// <para>
/// <b>Everywhere else, a version's notes are only its own changes.</b> The release page shows one build a block, because somebody reading it
/// is asking what a particular build did. The update bar is the one place that has to combine them, because somebody on nightly 31 being
/// offered nightly 40 has not seen 32 to 39 either, and all of it is new to them.
/// </para>
/// <para>
/// It is built from the per-version notes the manifest carries, never by re-reading history: the machine being updated has no history to
/// read, and the notes were already written once when each build was published.
/// </para>
/// </summary>
public static class SkippedVersions
{
    /// <summary>
    /// The versions a person on <paramref name="installed"/> has not seen, up to and including <paramref name="offered"/>, newest first.
    /// <para>
    /// The comparison is <see cref="UpdateOrder"/>, the same ordering the updater uses to decide what is newer, so the bar and the offer can
    /// never disagree about which builds are between them. A version that does not parse is left out rather than guessed at.
    /// </para>
    /// </summary>
    public static IReadOnlyList<VersionNotes> Between(IReadOnlyList<VersionNotes>? versions, string? installed, string? offered)
    {
        if (versions is null || versions.Count == 0)
        {
            return [];
        }

        var was = SemanticVersion.Parse(installed);
        var next = SemanticVersion.Parse(offered);

        return
        [
            .. versions
                .Select(v => (Notes: v, Parsed: SemanticVersion.Parse(v.Version)))
                .Where(v => v.Parsed is not null)
                .Where(v => next is null || UpdateOrder.Compare(v.Parsed, next) <= 0)
                .Where(v => was is null || UpdateOrder.Compare(was, v.Parsed) < 0)
                .OrderByDescending(v => v.Parsed, Comparer<SemanticVersion?>.Create(UpdateOrder.Compare))
                .Select(v => v.Notes),
        ];
    }

    /// <summary>
    /// Those versions as one piece of text for the bar: each version named, its own notes under it, newest first.
    /// <para>
    /// Where only one version is new, the heading is left off: somebody updating one build at a time does not need to be told which build
    /// they are reading about, and the bar already says it.
    /// </para>
    /// </summary>
    public static string Combined(IReadOnlyList<VersionNotes>? versions, string? installed, string? offered, string? fallback = null)
    {
        var between = Between(versions, installed, offered);
        if (between.Count == 0)
        {
            return fallback?.Trim() ?? "";
        }

        if (between.Count == 1)
        {
            return between[0].Notes.Trim();
        }

        var text = new StringBuilder();
        foreach (var version in between)
        {
            if (text.Length > 0)
            {
                text.Append('\n').Append('\n');
            }

            text.Append("GroupLab ").Append(version.Version).Append('\n').Append('\n').Append(version.Notes.Trim());
        }

        return text.ToString();
    }

    /// <summary>
    /// How the bar introduces a run of skipped builds, or null where only the offered one is new. It counts builds rather than listing them,
    /// because the list is underneath it.
    /// </summary>
    public static string? Says(IReadOnlyList<VersionNotes>? versions, string? installed, string? offered)
    {
        int count = Between(versions, installed, offered).Count;
        return count > 1
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{count} builds are new to you, newest first.")
            : null;
    }
}
