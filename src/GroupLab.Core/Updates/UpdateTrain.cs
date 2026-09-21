using System.Globalization;

namespace GroupLab.Core.Updates;

/// <summary>
/// A stream of builds a person can follow, NOTES-FROM-PLANNING.md entry 119. Only <see cref="Nightly"/> exists today; the other two are
/// built into the application and shown as not available yet, so that turning one on later is a change of one line rather than a screen.
/// </summary>
public enum UpdateTrain
{
    /// <summary>A build made on somebody's own machine. It never offers to update itself.</summary>
    Development,

    /// <summary>Every commit that passes the tests. Broken builds reach this train by design.</summary>
    Nightly,

    /// <summary>Not available yet.</summary>
    Beta,

    /// <summary>Not available yet.</summary>
    Release,
}

/// <summary>What a train is called, and what it may take.</summary>
public static class UpdateTrains
{
    /// <summary>The trains a person may choose between, in the order the settings page shows them: steadiest first.</summary>
    public static IReadOnlyList<UpdateTrain> Choosable { get; } = [UpdateTrain.Release, UpdateTrain.Beta, UpdateTrain.Nightly];

    /// <summary>The trains that have builds on them today. Entry 119: the others are shown greyed out until Alan asks for the first one.</summary>
    public static IReadOnlyList<UpdateTrain> Available { get; } = [UpdateTrain.Nightly];

    public static bool IsAvailable(this UpdateTrain train) => Available.Contains(train);

    public static string Words(this UpdateTrain train) => train switch
    {
        UpdateTrain.Release => "Release",
        UpdateTrain.Beta => "Beta",
        UpdateTrain.Nightly => "Nightly",
        _ => "Development",
    };

    /// <summary>The name used in a version's pre-release part and in a manifest, which is the same word in lower case.</summary>
    public static string Tag(this UpdateTrain train) => train.Words().ToLowerInvariant();

    /// <summary>What the settings page says beside a train nobody can choose yet.</summary>
    public const string NotAvailableYet = "Not available yet";

    /// <summary>
    /// Which trains a person on this one may receive a build from, entry 119 section 4.5: "a user on a less stable train also receives newer
    /// builds from a more stable one (nightly users get a beta or release if it is newer), never the reverse".
    /// </summary>
    public static IReadOnlyList<UpdateTrain> Accepts(this UpdateTrain train) => train switch
    {
        UpdateTrain.Nightly => [UpdateTrain.Nightly, UpdateTrain.Beta, UpdateTrain.Release],
        UpdateTrain.Beta => [UpdateTrain.Beta, UpdateTrain.Release],
        UpdateTrain.Release => [UpdateTrain.Release],
        _ => [],
    };

    /// <summary>Reads the train a version belongs to from its pre-release part: 0.2.0-nightly.14 is nightly, 0.2.0 is a release.</summary>
    public static UpdateTrain Of(SemanticVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        string? pre = version.PreRelease?.Split('.')[0].ToLowerInvariant();
        return pre switch
        {
            "nightly" => UpdateTrain.Nightly,
            "beta" => UpdateTrain.Beta,
            null => UpdateTrain.Release,
            _ => UpdateTrain.Development,
        };
    }

    /// <summary>The address of a train's newest manifest: one fixed URL GitHub serves without its API (entry 119 section 3.3).</summary>
    public static string? ManifestAddress(this UpdateTrain train) => train switch
    {
        UpdateTrain.Nightly => "https://github.com/oRAirwolf/grouplab/releases/download/nightly/update-manifest.json",
        UpdateTrain.Beta => null,
        UpdateTrain.Release => null,
        _ => null,
    };
}

/// <summary>
/// What this build is: its version, the train it came from, and the commit it was made at, all stamped in at build time (entry 119
/// section 1.2). A build made on somebody's own machine says so and never offers to update itself.
/// </summary>
public sealed record BuildIdentity(SemanticVersion Version, UpdateTrain Train, string? Commit)
{
    /// <summary>A build nobody published. It has no train, and the updater leaves it alone.</summary>
    public bool IsDevelopment => Train == UpdateTrain.Development;

    /// <summary>One line a person can read and paste into a report.</summary>
    public string Line => string.Create(CultureInfo.InvariantCulture,
        $"GroupLab {Version.Number}, {(IsDevelopment ? "development build" : Train.Words().ToLowerInvariant() + " build")}, {(Commit is null ? "no commit recorded" : "commit " + Commit)}");

    /// <summary>
    /// Reads the identity from what the build stamped in: the informational version, which carries the commit after a plus sign, and the
    /// train, which a published build sets and a local build does not.
    /// </summary>
    public static BuildIdentity Read(string? informationalVersion, string? train)
    {
        var version = SemanticVersion.Parse(informationalVersion?.Split('+')[0]) ?? new SemanticVersion(0, 0, 0, null, null);
        string? commit = informationalVersion?.Split('+') is [_, var sha, ..] && sha.Length > 0 ? (sha.Length > 7 ? sha[..7] : sha) : null;
        var named = train?.Trim().ToLowerInvariant() switch
        {
            "nightly" => UpdateTrain.Nightly,
            "beta" => UpdateTrain.Beta,
            "release" => UpdateTrain.Release,
            _ => UpdateTrain.Development,
        };

        // A build that names no train, or names one its version disagrees with, is a development build: only the workflow stamps a train.
        return new BuildIdentity(version, named == UpdateTrain.Development || UpdateTrains.Of(version) != named ? UpdateTrain.Development : named, commit);
    }
}
