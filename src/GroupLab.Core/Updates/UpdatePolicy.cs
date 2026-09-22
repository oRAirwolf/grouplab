using System.Globalization;

namespace GroupLab.Core.Updates;

/// <summary>How often GroupLab looks, NOTES-FROM-PLANNING.md entry 119 section 4.2. On every launch is the default.</summary>
public enum UpdateCheckInterval
{
    EveryLaunch,
    Daily,
    Weekly,
    Never,
}

/// <summary>What the settings page calls each choice, and how long it means.</summary>
public static class UpdateCheckIntervals
{
    public static IReadOnlyList<UpdateCheckInterval> All { get; } =
        [UpdateCheckInterval.EveryLaunch, UpdateCheckInterval.Daily, UpdateCheckInterval.Weekly, UpdateCheckInterval.Never];

    public static string Words(this UpdateCheckInterval interval) => interval switch
    {
        UpdateCheckInterval.EveryLaunch => "On every launch",
        UpdateCheckInterval.Daily => "Once a day",
        UpdateCheckInterval.Weekly => "Once a week",
        _ => "Never, only when I ask",
    };

    public static TimeSpan? Gap(this UpdateCheckInterval interval) => interval switch
    {
        UpdateCheckInterval.EveryLaunch => TimeSpan.Zero,
        UpdateCheckInterval.Daily => TimeSpan.FromDays(1),
        UpdateCheckInterval.Weekly => TimeSpan.FromDays(7),
        _ => null,
    };
}

/// <summary>What a person has decided about updating: the train, how often, and the one version they said to skip.</summary>
public sealed record UpdatePreferences(UpdateTrain Train, UpdateCheckInterval Interval, string? SkippedVersion, DateTimeOffset? LastCheckUtc)
{
    /// <summary>What a fresh installation does: follow the train it was built on, and look on every launch.</summary>
    public static UpdatePreferences Default(UpdateTrain train) => new(train, UpdateCheckInterval.EveryLaunch, null, null);
}

/// <summary>What the application should do about an update, and why, in words a person can read.</summary>
public sealed record UpdateDecision(bool Offer, SemanticVersion? Version, string Reason, UpdateSignature.Refusal Refusal = UpdateSignature.Refusal.None);

/// <summary>
/// The rules the updater turns on, NOTES-FROM-PLANNING.md entry 119 sections 4.1 to 4.5, kept away from the network and away from the window
/// so they can be tested as rules. Nothing here downloads anything or looks at a clock it was not given.
/// </summary>
public static class UpdatePolicy
{
    /// <summary>Whether it is time to look, given when it last looked.</summary>
    public static bool ShouldCheck(UpdatePreferences preferences, DateTimeOffset now, bool launching)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var gap = preferences.Interval.Gap();
        if (gap is null)
        {
            return false;
        }

        if (preferences.Interval == UpdateCheckInterval.EveryLaunch)
        {
            return launching;
        }

        return preferences.LastCheckUtc is not { } last || now - last >= gap;
    }

    /// <summary>
    /// What to do with a manifest that has arrived: refuse it, ignore it, or offer it. The build's own identity decides as much as the
    /// manifest does, because a development build never offers anything and a build with no key can check nothing.
    /// </summary>
    public static UpdateDecision Decide(BuildIdentity build, UpdatePreferences preferences, SignedManifest? signed, string? publicKey)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(preferences);

        // Entry 119 section 1.2: a build nobody published never offers to update itself.
        if (build.IsDevelopment)
        {
            return new UpdateDecision(false, null, "This is a development build, so it does not update itself.");
        }

        return Decided(build, preferences, UpdateSignature.Verify(signed, publicKey), signed?.Payload);
    }

    /// <summary>
    /// The same decision about a manifest in the second format, entry 139 section 3. The rules are the same rules; what differs is that the
    /// signature was checked over the bytes as they arrived, so the manifest may carry fields this build has never heard of.
    /// </summary>
    public static UpdateDecision Decide(BuildIdentity build, UpdatePreferences preferences, PublishedManifest? published, string? publicKey)
    {
        ArgumentNullException.ThrowIfNull(build);
        ArgumentNullException.ThrowIfNull(preferences);
        if (build.IsDevelopment)
        {
            return new UpdateDecision(false, null, "This is a development build, so it does not update itself.");
        }

        return Decided(build, preferences, UpdateSignature.Verify(published, publicKey), published?.Body);
    }

    private static UpdateDecision Decided(BuildIdentity build, UpdatePreferences preferences, UpdateSignature.Refusal refusal, UpdateManifest? manifest)
    {
        if (refusal != UpdateSignature.Refusal.None)
        {
            return new UpdateDecision(false, null, refusal.Words(), refusal);
        }

        if (manifest?.Offered is not { } offered)
        {
            return new UpdateDecision(false, null, UpdateSignature.Refusal.UnknownManifest.Words(), UpdateSignature.Refusal.UnknownManifest);
        }

        // Entry 119 section 4.5: a train takes from itself and from steadier ones, never the other way.
        if (!preferences.Train.Accepts().Contains(manifest.OnTrain))
        {
            return new UpdateDecision(false, offered, UpdateSignature.Refusal.WrongTrain.Words(), UpdateSignature.Refusal.WrongTrain);
        }

        if (!UpdateOrder.IsNewer(build.Version, offered))
        {
            return new UpdateDecision(false, offered, UpdateSignature.Refusal.NotNewer.Words(), UpdateSignature.Refusal.NotNewer);
        }

        // Skip means silence until something newer than the skipped build appears; it is not "never ask again".
        if (SemanticVersion.Parse(preferences.SkippedVersion) is { } skipped && !UpdateOrder.IsNewer(skipped, offered))
        {
            return new UpdateDecision(false, offered, string.Create(CultureInfo.InvariantCulture, $"You asked GroupLab to skip {skipped.Number}, so it will say nothing until there is something newer."));
        }

        return new UpdateDecision(true, offered, string.Create(CultureInfo.InvariantCulture, $"GroupLab {offered.Number} is ready to install."));
    }

    /// <summary>
    /// Whether a downloaded file is the one the manifest describes. A download that does not match is refused and deleted rather than run:
    /// a truncated or swapped installer is exactly what this check is for.
    /// </summary>
    public static bool Matches(UpdateAsset asset, ReadOnlySpan<byte> downloaded)
    {
        ArgumentNullException.ThrowIfNull(asset);
        return downloaded.Length == asset.Bytes && string.Equals(UpdateSignature.Sha256(downloaded), asset.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>What to say when a download does not match what was promised.</summary>
    public const string DownloadDoesNotMatch =
        "The update GroupLab downloaded is not the file its signed information describes, so it was thrown away and nothing was installed. "
        + "Try again, and if it happens twice, download the newest build by hand.";
}
