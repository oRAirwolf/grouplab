using System.Globalization;

namespace GroupLab.Core.Updates;

/// <summary>What an update is doing, for the bar in the window.</summary>
public enum UpdateStage
{
    Idle,
    Checking,
    Offered,
    Downloading,
    ReadyToInstall,
    Installing,
    Refused,
}

/// <summary>Where an update has got to, in one value the window can draw without knowing how any of it works.</summary>
/// <param name="Skipped">
/// How the bar introduces a run of builds a person has not seen, entry 138 section 5, or null where only the offered build is new to them.
/// </param>
public sealed record UpdateState(UpdateStage Stage, string Says, SemanticVersion? Version = null, double Share = 0, string? Notes = null, string? Skipped = null);

/// <summary>
/// One update, from looking to a ready installer, NOTES-FROM-PLANNING.md entry 123 section 2. Everything it does outside the process goes
/// through <see cref="IOutsideWorld"/>, so a test drives a whole update with no network and no installer.
/// <para>
/// <b>It never installs anything by itself.</b> It gets as far as a verified file and the switches to run it with, and hands both back; the
/// window saves the person's work, says what is about to happen, and only then starts it. Nothing here can surprise somebody.
/// </para>
/// </summary>
public sealed class UpdateRun(IOutsideWorld outside, BuildIdentity build, string folder)
{
    private readonly IOutsideWorld _outside = outside;
    private readonly BuildIdentity _build = build;
    private readonly string _folder = folder;

    /// <summary>The switches that make the Inno Setup installer silent, entry 119 section 4.6.</summary>
    public const string SilentSwitches = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS";

    /// <summary>Where the download goes: a folder GroupLab owns, beside its own data, never a temporary directory somebody else sweeps.</summary>
    public string Folder => _folder;

    /// <summary>The manifest this run verified, or null before one has been.</summary>
    public UpdateManifest? Manifest { get; private set; }

    /// <summary>
    /// Which format the manifest that was accepted came in, for the log and for the tests: the second where it was there, and the first where
    /// the build being updated from was published before the second existed (entry 139 section 3).
    /// </summary>
    public int ManifestFormat { get; private set; }

    /// <summary>The file that was downloaded and checked, or null.</summary>
    public string? Downloaded { get; private set; }

    /// <summary>
    /// Looks for a newer build on the train the person follows and says what it found. It reads one small public file and nothing else.
    /// </summary>
    public async Task<(UpdateState State, UpdateDecision? Decision)> CheckAsync(UpdatePreferences preferences, string? publicKey, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (_build.IsDevelopment)
        {
            return (new UpdateState(UpdateStage.Idle, "This is a development build, so it does not update itself."), null);
        }

        if (preferences.Train.ManifestAddress() is not { } address)
        {
            return (new UpdateState(UpdateStage.Idle, preferences.Train.Words() + ": " + UpdateTrains.NotAvailableYet + "."), null);
        }

        // Entry 139 section 3: the second format first, the first format only where the second is not there. A build published before the
        // second format existed still updates itself, and a build published after it never has to re-serialise anything to check a signature.
        UpdateDecision? decision = null;
        UpdateManifest? manifest = null;
        int format = 0;
        bool reached = false;

        if (preferences.Train.PublishedAddress() is { } second
            && await _outside.GetTextAsync(second, token).ConfigureAwait(false) is { } sealedJson)
        {
            reached = true;
            if (PublishedManifest.Read(sealedJson) is { } published)
            {
                decision = UpdatePolicy.Decide(_build, preferences, published, publicKey);
                manifest = published.Body;
                format = 2;
            }
        }

        if (decision is null)
        {
            string? json = await _outside.GetTextAsync(address, token).ConfigureAwait(false);
            if (json is null)
            {
                return reached
                    // The second address answered and the first did not, which is a release missing a file rather than a machine offline.
                    ? (new UpdateState(UpdateStage.Refused, UpdateSignature.Refusal.UnknownManifest.Words(), null), new UpdateDecision(false, null, UpdateSignature.Refusal.UnknownManifest.Words(), UpdateSignature.Refusal.UnknownManifest))
                    // Entry 119 section 4.1: offline means no message at all, only a line in the log. The window decides that; this says what it is.
                    : (new UpdateState(UpdateStage.Idle, "GroupLab could not reach the update page. Nothing has changed."), null);
            }

            var signed = SignedManifest.Read(json);
            decision = UpdatePolicy.Decide(_build, preferences, signed, publicKey);
            manifest = signed?.Payload;
            format = 1;
        }

        Manifest = decision.Offer ? manifest : null;
        ManifestFormat = decision.Offer ? format : 0;
        return (decision.Offer
            // Entry 138 section 5: everything between the installed build and the offered one, where the manifest carries it. The second
            // format is what made that safe to publish; a first-format manifest has only the one set of notes and falls back to them.
            ? new UpdateState(UpdateStage.Offered, decision.Reason, decision.Version, 0,
                SkippedVersions.Combined(manifest!.Versions, _build.Version.Number, manifest.Version, manifest.Notes),
                SkippedVersions.Says(manifest.Versions, _build.Version.Number, manifest.Version))
            : new UpdateState(decision.Refusal == UpdateSignature.Refusal.NotNewer ? UpdateStage.Idle : UpdateStage.Refused, decision.Reason, decision.Version), decision);
    }

    /// <summary>
    /// Downloads the file for this platform and checks it against the manifest before anything can run it. A file that does not match is
    /// deleted rather than kept, because a half-right installer is worse than none.
    /// </summary>
    public async Task<UpdateState> DownloadAsync(string platform, string kind, IProgress<double>? progress, CancellationToken token)
    {
        if (Manifest?.For(platform, kind) is not { } asset)
        {
            return new UpdateState(UpdateStage.Refused, "This build has nothing to install for " + platform + ".");
        }

        Directory.CreateDirectory(_folder);
        string into = Path.Combine(_folder, asset.Name);

        // A part left by a dropped connection is never resumed into: it is started again, because a half file with the right name would
        // pass a hash check only by accident and fail it every other time.
        foreach (string stale in new[] { into, into + ".part" })
        {
            if (File.Exists(stale))
            {
                File.Delete(stale);
            }
        }

        long? written;
        try
        {
            written = await _outside.DownloadAsync(asset.Url, into, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Clean(into);
            return new UpdateState(UpdateStage.Idle, "The download was stopped. Nothing was installed and nothing was kept.");
        }

        if (written is null)
        {
            Clean(into);
            return new UpdateState(UpdateStage.Refused, "The update could not be downloaded. GroupLab is unchanged; try again later.");
        }

        if (!File.Exists(into))
        {
            return new UpdateState(UpdateStage.Refused, "The update could not be downloaded. GroupLab is unchanged; try again later.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(into, token).ConfigureAwait(false);
        if (!UpdatePolicy.Matches(asset, bytes))
        {
            Clean(into);
            return new UpdateState(UpdateStage.Refused, UpdatePolicy.DownloadDoesNotMatch);
        }

        Downloaded = into;
        return new UpdateState(UpdateStage.ReadyToInstall,
            string.Create(CultureInfo.InvariantCulture, $"GroupLab {Manifest.Version} is downloaded and checked."),
            Manifest.Offered, 1, Manifest.Notes);
    }

    /// <summary>
    /// The switches the installer is started with: silent, showing no window, asking for no restart, and telling it to bring GroupLab back
    /// up afterwards. Entry 123 section 2.3. What version this was, and what screen the person was on, are written to the settings file
    /// instead of passed here, so there is one place the new version reads them from.
    /// </summary>
    public string InstallerArguments() => SilentSwitches + " /relaunch=yes";

    /// <summary>Whether there is a checked file ready to run.</summary>
    public bool Ready => Downloaded is not null && File.Exists(Downloaded);

    private static void Clean(string path)
    {
        foreach (string file in new[] { path, path + ".part" })
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
