using System.Globalization;
using System.Text.RegularExpressions;

namespace GroupLab.Core.Updates;

/// <summary>
/// A version, ordered as SemVer 2.0.0 orders them, NOTES-FROM-PLANNING.md entry 119 section 1. The ordering is the whole point: an updater
/// that gets it wrong offers a person older code, which is what entry 121 caught before it could happen.
/// <para>
/// The rule that matters here: a pre-release sorts <b>below</b> the release it is named for, so <c>0.1.0-nightly.12</c> is older than
/// <c>0.1.0</c>, and pre-release identifiers are compared one by one, numbers numerically and anything else as text.
/// </para>
/// </summary>
public sealed partial record SemanticVersion(int Major, int Minor, int Patch, string? PreRelease, string? Build) : IComparable<SemanticVersion>
{
    /// <summary>Reads a version, or null where the text is not one.</summary>
    public static SemanticVersion? Parse(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var m = Shape().Match(text.Trim());
        if (!m.Success)
        {
            return null;
        }

        return new SemanticVersion(
            int.Parse(m.Groups["major"].Value, CultureInfo.InvariantCulture),
            int.Parse(m.Groups["minor"].Value, CultureInfo.InvariantCulture),
            int.Parse(m.Groups["patch"].Value, CultureInfo.InvariantCulture),
            m.Groups["pre"].Success ? m.Groups["pre"].Value : null,
            m.Groups["build"].Success ? m.Groups["build"].Value : null);
    }

    /// <summary>The version as it is written, without the build metadata, which SemVer says carries no ordering.</summary>
    public string Number => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}") + (PreRelease is null ? "" : "-" + PreRelease);

    public override string ToString() => Number + (Build is null ? "" : "+" + Build);

    /// <summary>Whether this version is a pre-release: a nightly or a beta, rather than a release.</summary>
    public bool IsPreRelease => PreRelease is not null;

    /// <summary>
    /// SemVer precedence. Build metadata is ignored, as the specification requires: two builds of the same version differ only in where they
    /// came from, and neither is newer.
    /// </summary>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        int number = Major.CompareTo(other.Major);
        if (number != 0)
        {
            return number;
        }

        number = Minor.CompareTo(other.Minor);
        if (number != 0)
        {
            return number;
        }

        number = Patch.CompareTo(other.Patch);
        if (number != 0)
        {
            return number;
        }

        // A version with a pre-release is older than the same version without one.
        if (PreRelease is null && other.PreRelease is null)
        {
            return 0;
        }

        if (PreRelease is null)
        {
            return 1;
        }

        if (other.PreRelease is null)
        {
            return -1;
        }

        return ComparePreRelease(PreRelease, other.PreRelease);
    }

    /// <summary>Identifier by identifier: numbers numerically, anything else as text, and a shorter run of identifiers is the older.</summary>
    private static int ComparePreRelease(string mine, string theirs)
    {
        string[] a = mine.Split('.'), b = theirs.Split('.');
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            if (i >= a.Length)
            {
                return -1;
            }

            if (i >= b.Length)
            {
                return 1;
            }

            bool an = int.TryParse(a[i], NumberStyles.None, CultureInfo.InvariantCulture, out int ai);
            bool bn = int.TryParse(b[i], NumberStyles.None, CultureInfo.InvariantCulture, out int bi);
            int order = (an, bn) switch
            {
                (true, true) => ai.CompareTo(bi),
                (true, false) => -1,
                (false, true) => 1,
                _ => string.CompareOrdinal(a[i], b[i]),
            };
            if (order != 0)
            {
                return order;
            }
        }

        return 0;
    }

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) < 0;

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) > 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) <= 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) >= 0;

    private static int Compare(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    [GeneratedRegex(@"^v?(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<pre>[0-9A-Za-z.-]+))?(?:\+(?<build>[0-9A-Za-z.-]+))?$")]
    private static partial Regex Shape();
}
