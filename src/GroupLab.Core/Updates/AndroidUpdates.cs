using System.Globalization;
using System.Security.Cryptography;

namespace GroupLab.Core.Updates;

/// <summary>Why the Android updater did not offer, download or install something, NOTES-FROM-PLANNING.md entry 288.</summary>
public enum ApkRefusal
{
    None,

    /// <summary>Nothing answered. Entry 288: offline is silent, a line in the log and nothing on screen.</summary>
    Offline,

    /// <summary>A build made on somebody's own machine, which never updates itself.</summary>
    Development,

    /// <summary>The copy came from Google Play, which updates it; the updater turns itself off.</summary>
    StoreCopy,

    BadSignature,
    UnknownManifest,
    WrongTrain,
    NotNewer,

    /// <summary>The newest build on the train carries no Android file of this kind.</summary>
    NoApk,

    /// <summary>The download is not the size, or not the SHA-256, the signed manifest gives.</summary>
    WrongHash,

    /// <summary>The download is signed by a certificate other than the one the installed copy was signed with.</summary>
    DifferentCertificate,

    /// <summary>The download could not be completed; the part stays, and the next try carries on from it.</summary>
    NotDownloaded,
}

/// <summary>What a check found: an update to download, or why not.</summary>
/// <param name="PublishedUtc">When the offered build was published, as its manifest says, for the time from publishing to installing.</param>
public sealed record ApkOffer(ApkRefusal Refusal, string Says, SemanticVersion? Version = null, UpdateAsset? Asset = null, string? Notes = null, string? PublishedUtc = null)
{
    public bool Offered => Refusal == ApkRefusal.None && Asset is not null;
}

/// <summary>When an update that is downloaded and checked may be installed.</summary>
public enum InstallWhen
{
    /// <summary>Install it now.</summary>
    Now,

    /// <summary>Not while the camera is open, an analysis is running or a change is unsaved.</summary>
    NotDuringWork,

    /// <summary>Automatic, but GroupLab is on screen: it installs once the person has left it.</summary>
    WhenLeft,

    /// <summary>Automatic installs are off: it waits for "Update now".</summary>
    OnRequest,
}

/// <summary>
/// The updater in the sideloaded Android builds, NOTES-FROM-PLANNING.md entry 288. It uses the desktop's trains, ordering and signed
/// manifest (docs/UPDATES.md) and adds only what installing an APK needs: the file's own signing certificate, the installer of record, and
/// when it is safe to replace the application under somebody's hands. Nothing here touches Android; the application hands it what Android
/// said, so every rule is tested as a rule.
/// </summary>
public static class AndroidUpdates
{
    /// <summary>The platform an Android file is listed under in the manifest.</summary>
    public const string Platform = "android";

    /// <summary>The kind of the GroupLab Dev APK, the development build installed beside the copy from Google Play.</summary>
    public const string DevKind = "apk-dev";

    /// <summary>The kind of the plain APK of GroupLab itself from the download page, published in the manifest since entry 386.</summary>
    public const string SideloadKind = "apk";

    /// <summary>Google Play's package, as Android names the installer of record of a copy that came from it.</summary>
    public const string PlayInstaller = "com.android.vending";

    /// <summary>Android 12, from which an application may update itself without a tap once it is its own installer of record.</summary>
    public const int SilentFromSdk = 31;

    /// <summary>How often the background check runs, entry 288: "about every six hours".</summary>
    public static readonly TimeSpan Every = TimeSpan.FromHours(6);

    /// <summary>
    /// The build's identity for updating. GroupLab Dev's version ends in <c>-dev</c> (entry 234), which marks its reports and is not part of
    /// its place in the order: 0.2.0-nightly.124-dev is nightly 124, and read as it stands it would sort after nightly 125.
    /// </summary>
    public static BuildIdentity Identity(string? informationalVersion, string? train)
    {
        string? version = informationalVersion;
        if (version is not null)
        {
            string[] parts = version.Split('+', 2);
            string number = parts[0].EndsWith("-dev", StringComparison.OrdinalIgnoreCase) ? parts[0][..^4] : parts[0];
            version = parts.Length > 1 ? number + "+" + parts[1] : number;
        }

        return BuildIdentity.Read(version, train);
    }

    /// <summary>Whether this copy came from Google Play, in which case the updater does nothing at all.</summary>
    public static bool IsStoreCopy(string? installerOfRecord) =>
        string.Equals(installerOfRecord, PlayInstaller, StringComparison.Ordinal);

    /// <summary>
    /// Whether Android will install without a tap: Android 12 or later, and GroupLab already its own installer of record. A copy installed by
    /// adb or by hand has another installer, so its first self-update needs one tap, and every one after it does not.
    /// </summary>
    public static bool MayInstallSilently(int sdk, string? installerOfRecord, string ownPackage) =>
        sdk >= SilentFromSdk && string.Equals(installerOfRecord, ownPackage, StringComparison.Ordinal);

    /// <summary>
    /// When a checked download may be installed. Never in the middle of work, even when asked: installing closes GroupLab, and a picture
    /// being taken, a sheet being read or a mark not yet saved would go with it.
    /// </summary>
    public static InstallWhen When(bool cameraOpen, bool analysing, bool unsaved, bool onScreen, bool automatic, bool askedNow)
    {
        if (cameraOpen || analysing || unsaved)
        {
            return InstallWhen.NotDuringWork;
        }

        if (askedNow)
        {
            return InstallWhen.Now;
        }

        if (!automatic)
        {
            return InstallWhen.OnRequest;
        }

        return onScreen ? InstallWhen.WhenLeft : InstallWhen.Now;
    }

    /// <summary>
    /// Looks for a newer build on this build's train with one plain HTTPS GET of the signed manifest, in the second format only: every Android
    /// build that has an updater was published after that format existed.
    /// </summary>
    public static async Task<ApkOffer> CheckAsync(IOutsideWorld outside, BuildIdentity build, string kind, string? publicKey, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(outside);
        ArgumentNullException.ThrowIfNull(build);
        if (build.IsDevelopment)
        {
            return new ApkOffer(ApkRefusal.Development, "This is a development build, so it does not update itself.");
        }

        if (build.Train.PublishedAddress() is not { } address)
        {
            return new ApkOffer(ApkRefusal.WrongTrain, build.Train.Words() + ": " + UpdateTrains.NotAvailableYet + ".");
        }

        if (await outside.GetTextAsync(address, token).ConfigureAwait(false) is not { } json)
        {
            return new ApkOffer(ApkRefusal.Offline, "GroupLab could not reach the update page. Nothing has changed.");
        }

        var published = PublishedManifest.Read(json);
        var decision = UpdatePolicy.Decide(build, UpdatePreferences.Default(build.Train), published, publicKey);
        if (!decision.Offer)
        {
            var refusal = decision.Refusal switch
            {
                UpdateSignature.Refusal.WrongTrain => ApkRefusal.WrongTrain,
                UpdateSignature.Refusal.NotNewer => ApkRefusal.NotNewer,
                UpdateSignature.Refusal.UnknownManifest => ApkRefusal.UnknownManifest,
                _ => ApkRefusal.BadSignature,
            };
            return new ApkOffer(refusal, decision.Reason, decision.Version);
        }

        var manifest = published!.Body!;
        if (manifest.For(Platform, kind) is not { } asset)
        {
            return new ApkOffer(ApkRefusal.NoApk, string.Create(CultureInfo.InvariantCulture,
                $"GroupLab {manifest.Version} is out, and has no Android file of this kind yet. Nothing was downloaded."), decision.Version);
        }

        return new ApkOffer(ApkRefusal.None, decision.Reason, decision.Version, asset,
            SkippedVersions.Combined(manifest.Versions, build.Version.Number, manifest.Version, manifest.Notes), manifest.PublishedUtc);
    }

    /// <summary>
    /// Downloads the offered APK into <paramref name="folder"/>, carrying on from a part a dropped connection left, and checks its size and
    /// SHA-256 against the signed manifest. A file that does not match is deleted, never kept and never installed.
    /// </summary>
    public static async Task<(string? Path, ApkRefusal Refusal)> DownloadAsync(IOutsideWorld outside, UpdateAsset asset, string folder, IProgress<double>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(outside);
        ArgumentNullException.ThrowIfNull(asset);
        Directory.CreateDirectory(folder);
        string into = Path.Combine(folder, Path.GetFileName(asset.Name));

        // A finished file from an earlier try is used as it is when it is still the one promised, and thrown away when it is not.
        if (File.Exists(into))
        {
            if (Matches(asset, into))
            {
                return (into, ApkRefusal.None);
            }

            File.Delete(into);
        }

        // Anything else in the folder is an older update, or a part of one, and has no further use.
        foreach (string old in Directory.EnumerateFiles(folder))
        {
            if (!string.Equals(Path.GetFileName(old), Path.GetFileName(into) + ".part", StringComparison.Ordinal))
            {
                File.Delete(old);
            }
        }

        long? written = await outside.ResumeDownloadAsync(asset.Url, into, progress, token).ConfigureAwait(false);
        if (written is null || !File.Exists(into))
        {
            return (null, ApkRefusal.NotDownloaded);
        }

        if (!Matches(asset, into))
        {
            File.Delete(into);
            return (null, ApkRefusal.WrongHash);
        }

        return (into, ApkRefusal.None);
    }

    /// <summary>Whether a file on disk is the size and SHA-256 the manifest gives, read as a stream: an APK is tens of megabytes.</summary>
    public static bool Matches(UpdateAsset asset, string file)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var info = new FileInfo(file);
        if (!info.Exists || info.Length != asset.Bytes)
        {
            return false;
        }

        using var stream = info.OpenRead();
        string sha = Convert.ToHexStringLower(SHA256.HashData(stream));
        return string.Equals(sha, asset.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The last check before installing, entry 288: the file is still the one the manifest promised, and it is signed by exactly the
    /// certificates the installed copy was signed with (SHA-256 of each, as Android gives them). Android would refuse a different signer
    /// itself; checking first means the person is never asked to install something that then fails, and the file is deleted here.
    /// </summary>
    public static ApkRefusal CheckBeforeInstall(UpdateAsset asset, string file, IReadOnlyCollection<string> installedCertificates, IReadOnlyCollection<string> apkCertificates)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(installedCertificates);
        ArgumentNullException.ThrowIfNull(apkCertificates);
        var refusal = !Matches(asset, file) ? ApkRefusal.WrongHash
            : !SameCertificates(installedCertificates, apkCertificates) ? ApkRefusal.DifferentCertificate
            : ApkRefusal.None;
        if (refusal != ApkRefusal.None && File.Exists(file))
        {
            File.Delete(file);
        }

        return refusal;
    }

    /// <summary>The same non-empty set of certificate digests, ignoring case and order.</summary>
    public static bool SameCertificates(IReadOnlyCollection<string> installed, IReadOnlyCollection<string> apk)
    {
        ArgumentNullException.ThrowIfNull(installed);
        ArgumentNullException.ThrowIfNull(apk);
        var a = installed.Select(s => s.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var b = apk.Select(s => s.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        return a.Count > 0 && a.SetEquals(b);
    }

    /// <summary>What to say about a refusal, in the words the settings page shows and the log carries.</summary>
    public static string Words(this ApkRefusal refusal) => refusal switch
    {
        ApkRefusal.Offline => "GroupLab could not reach the update page. Nothing has changed.",
        ApkRefusal.Development => "This is a development build, so it does not update itself.",
        ApkRefusal.StoreCopy => "This copy came from Google Play, which keeps it up to date.",
        ApkRefusal.BadSignature => UpdateSignature.Refusal.BadSignature.Words(),
        ApkRefusal.UnknownManifest => UpdateSignature.Refusal.UnknownManifest.Words(),
        ApkRefusal.WrongTrain => UpdateSignature.Refusal.WrongTrain.Words(),
        ApkRefusal.NotNewer => UpdateSignature.Refusal.NotNewer.Words(),
        ApkRefusal.NoApk => "The newest build has no Android file of this kind yet. Nothing was downloaded.",
        ApkRefusal.WrongHash => "The update GroupLab downloaded is not the file its signed information describes, so it was deleted and nothing was installed.",
        ApkRefusal.DifferentCertificate => "The update GroupLab downloaded is not signed by the same key as the copy you have, so it was deleted and nothing was installed.",
        ApkRefusal.NotDownloaded => "The update has not finished downloading. It carries on from where it stopped next time GroupLab is on Wi-Fi.",
        _ => "",
    };

    /// <summary>"Updated to nightly 125", or the version as it is written where it is not a nightly.</summary>
    public static string UpdatedTo(SemanticVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        string[] pre = version.PreRelease?.Split('.') ?? [];
        return pre is ["nightly", var n] ? "Updated to nightly " + n : "Updated to GroupLab " + version.Number;
    }
}
