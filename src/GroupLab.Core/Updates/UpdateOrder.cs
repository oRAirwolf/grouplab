namespace GroupLab.Core.Updates;

/// <summary>
/// Which of two builds is newer, for the updater, NOTES-FROM-PLANNING.md entry 119 section 1.3.
/// <para>
/// <b>This is not quite SemVer, and the difference is deliberate.</b> The entry gives the ordering it wants as
/// <c>0.1.0-nightly.12 &lt; 0.1.0-beta.1 &lt; 0.1.0 &lt; 0.2.0-nightly.1</c>, and calls it SemVer precedence. Strict SemVer compares
/// pre-release identifiers as text, and "beta" sorts before "nightly" alphabetically, so strict SemVer puts
/// <c>0.1.0-beta.1</c> <b>below</b> <c>0.1.0-nightly.12</c>, which is the opposite of what the entry says and the opposite of what section
/// 4.5 needs: a beta is steadier than a nightly and must read as the newer of the two.
/// </para>
/// <para>
/// So the trains are ranked by how steady they are, nightly below beta below release, and only versions whose pre-release parts are not
/// train names fall back to SemVer's own text ordering. <see cref="SemanticVersion"/> itself stays strictly SemVer, because it is also what
/// reads a tag; the deviation lives here, where the updater uses it. Question 30 asks the planning session to confirm it.
/// </para>
/// </summary>
public static class UpdateOrder
{
    /// <summary>How steady a train is: a higher number is steadier, and steadier is newer at the same version.</summary>
    private static int Rank(UpdateTrain train) => train switch
    {
        UpdateTrain.Nightly => 1,
        UpdateTrain.Beta => 2,
        UpdateTrain.Release => 3,
        _ => 0,
    };

    /// <summary>Negative where the first is older, positive where it is newer, zero where neither is.</summary>
    public static int Compare(SemanticVersion? left, SemanticVersion? right)
    {
        if (left is null)
        {
            return right is null ? 0 : -1;
        }

        if (right is null)
        {
            return 1;
        }

        // The version core decides first, exactly as SemVer says.
        int core = (left.Major, left.Minor, left.Patch).CompareTo((right.Major, right.Minor, right.Patch));
        if (core != 0)
        {
            return core;
        }

        UpdateTrain a = UpdateTrains.Of(left), b = UpdateTrains.Of(right);
        if (Rank(a) == 0 || Rank(b) == 0)
        {
            // One of them is not on a train GroupLab publishes, so there is nothing better than SemVer's own rule.
            return left.CompareTo(right);
        }

        if (Rank(a) != Rank(b))
        {
            return Rank(a).CompareTo(Rank(b));
        }

        // Same train: the number after its name, compared as SemVer compares identifiers.
        return left.CompareTo(right);
    }

    /// <summary>Whether the second build is one this build should offer, on the train the person follows.</summary>
    public static bool IsNewer(SemanticVersion? installed, SemanticVersion? offered) => Compare(installed, offered) < 0;
}
