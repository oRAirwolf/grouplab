namespace GroupLab.Core.Updates;

/// <summary>
/// Where the release notes for a version live on the site, NOTES-FROM-PLANNING.md entry 136 sections 1.2 and 1.4.
/// <para>
/// The anchor has to be worked out the same way in two places that cannot see each other: <c>website/build.py</c> makes the page's
/// <c>id</c> from the version's heading, and the update bar has to be able to link straight at it. Both do the same plain thing, which is
/// why it is written down here rather than left as a format string at the call site: anything that is not a letter or a digit becomes a
/// hyphen, so <c>0.2.0-nightly.31</c> becomes <c>0-2-0-nightly-31</c>.
/// </para>
/// </summary>
public static class ReleaseNotesPage
{
    /// <summary>The page itself, where somebody wants the whole history rather than one build.</summary>
    public const string Address = "https://grouplab.org/releases/";

    /// <summary>The anchor for one version, without its leading hash.</summary>
    public static string Anchor(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "";
        }

        // The same rule the site builder's slug() uses: lower case, every run of anything else becomes one hyphen, and no hyphen is left at
        // either end. A run rather than a character, or the two would disagree the first time a version held two punctuation marks together.
        var text = new System.Text.StringBuilder(version.Length + 1);
        foreach (char c in version.Trim())
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                text.Append(char.ToLowerInvariant(c));
            }
            else if (text.Length > 0 && text[^1] != '-')
            {
                text.Append('-');
            }
        }

        return text.ToString().TrimEnd('-');
    }

    /// <summary>
    /// The address that opens the page at one version's block, or the page itself where there is no version to open. A version nobody has
    /// written notes for yet lands on the page rather than on nothing, because an anchor that matches no block is simply ignored by the
    /// browser, and the page above it is still the thing the person asked for.
    /// </summary>
    public static string For(string? version) =>
        Anchor(version) is { Length: > 0 } anchor ? Address + "#" + anchor : Address;
}
