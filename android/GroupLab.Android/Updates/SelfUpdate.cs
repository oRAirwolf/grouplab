using System.Globalization;
using System.Security.Cryptography;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using AndroidX.Work;
using GroupLab.App.Diagnostics;
using GroupLab.Core.Updates;

// Entry 288: only a build with the updater asks for this, so the copy for Google Play never carries it (UpdaterFlavorTests).
[assembly: UsesPermission(global::Android.Manifest.Permission.RequestInstallPackages)]

namespace GroupLab.Android.Updates;

/// <summary>
/// The updater in the sideloaded builds, NOTES-FROM-PLANNING.md entry 288. It checks on launch and about every six hours with one GET of the
/// signed manifest, downloads on Wi-Fi in the background and carries a broken download on, checks the SHA-256 and the signing certificate,
/// and installs through a PackageInstaller session: silently from Android 12 once GroupLab is its own installer of record, and never in the
/// middle of work. The rules are <see cref="AndroidUpdates"/>'s, in Core, where they are tested; this is only what Android needs doing.
/// <para>
/// The whole folder is compiled only where <c>GroupLabUpdater</c> is true, which the project allows only for a sideloaded APK. A copy whose
/// installer of record is Google Play turns it off as well, in case one ever reaches Play by another road.
/// </para>
/// </summary>
internal static class SelfUpdate
{
    internal const string WorkName = "grouplab-self-update";

#if GROUPLAB_DEV
    internal const string Kind = AndroidUpdates.DevKind;
    private const bool AutomaticByDefault = true;
#else
    internal const string Kind = AndroidUpdates.SideloadKind;
    private const bool AutomaticByDefault = false;
#endif

    private static readonly SemaphoreSlim One = new(1, 1);

    private static Context Context => global::Android.App.Application.Context;

    private static ISharedPreferences Prefs => Context.GetSharedPreferences("grouplab-updates", FileCreationMode.Private)!;

    /// <summary>Where a download goes: GroupLab's own files, not the cache Android may clear half way through.</summary>
    private static string Folder => Path.Combine(Context.NoBackupFilesDir!.AbsolutePath, "updates");

    /// <summary>This build, with GroupLab Dev's <c>-dev</c> mark taken off its place in the order.</summary>
    internal static BuildIdentity Build => AndroidUpdates.Identity(AppInfo.Version, AppInfo.Train);

    /// <summary>What the updater last found or did, for the settings page.</summary>
    internal static string Status { get; private set; } = "";

    internal static event Action? Changed;

    internal static bool Automatic
    {
        get => Prefs.GetBoolean("automatic", AutomaticByDefault);
        set
        {
            Prefs.Edit()!.PutBoolean("automatic", value)!.Apply();
            Log("update.automatic", ("on", value));
        }
    }

    /// <summary>The newest version on this build's train, as the last check found it, or null before any check has.</summary>
    internal static string? Newest => Prefs.GetString("newest", null);

    /// <summary>When the last check that reached the manifest ran.</summary>
    internal static DateTimeOffset? CheckedUtc =>
        Prefs.GetLong("checkedUtc", 0) is > 0 and var ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : null;

    /// <summary>The version downloaded, checked and waiting to install, or null.</summary>
    internal static string? Ready => ReadyAsset() is { } ready && File.Exists(ready.Path) ? ready.Version : null;

    /// <summary>Which app Android says installed this copy: adb and a file manager give another, the updater gives GroupLab itself.</summary>
    internal static string? InstallerOfRecord
    {
        get
        {
            try
            {
                return OperatingSystem.IsAndroidVersionAtLeast(30)
                    ? Context.PackageManager!.GetInstallSourceInfo(Context.PackageName!).InstallingPackageName
                    : LegacyInstaller();
            }
            catch (PackageManager.NameNotFoundException)
            {
                return null;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "Android 10 has only this form; Android 11 and later take the branch above.")]
    private static string? LegacyInstaller()
    {
#pragma warning disable CA1422
        return Context.PackageManager!.GetInstallerPackageName(Context.PackageName!);
#pragma warning restore CA1422
    }

    /// <summary>Why the updater does nothing on this copy, or null where it runs.</summary>
    internal static ApkRefusal? Off =>
        AndroidUpdates.IsStoreCopy(InstallerOfRecord) ? ApkRefusal.StoreCopy
        : Build.IsDevelopment ? ApkRefusal.Development
        : null;

    /// <summary>Whether Android will install the next update with no tap.</summary>
    internal static bool Silent => AndroidUpdates.MayInstallSilently((int)global::Android.OS.Build.VERSION.SdkInt, InstallerOfRecord, Context.PackageName!);

    /// <summary>Whether "Install unknown apps" is allowed for GroupLab, without which Android installs nothing it hands over.</summary>
    internal static bool MayInstall => Context.PackageManager!.CanRequestPackageInstalls();

    /// <summary>At launch: say what was just installed, schedule the six-hourly check, and look now.</summary>
    internal static void Launched()
    {
        if (Off is { } off)
        {
            Status = off.Words();
            Log("update.off", ("why", off.ToString()), ("installer", InstallerOfRecord ?? "none"));
            return;
        }

        Announce();
        Schedule();
        _ = RunAsync(asked: false);
    }

    /// <summary>When GroupLab leaves the screen: the moment an automatic update may install without closing anything under the person.</summary>
    internal static void Left()
    {
        if (Off is null && Ready is not null && Automatic)
        {
            _ = Task.Run(() => TryInstall(asked: false));
        }
    }

    /// <summary>The periodic check, on an unmetered network, which WorkManager guarantees before it runs the worker.</summary>
    internal static void Schedule()
    {
        var constraints = new Constraints.Builder().SetRequiredNetworkType(NetworkType.Unmetered!).Build();
        var request = new PeriodicWorkRequest.Builder(Java.Lang.Class.FromType(typeof(UpdateWorker)), (long)AndroidUpdates.Every.TotalHours, Java.Util.Concurrent.TimeUnit.Hours!)
            .SetConstraints(constraints)
            .Build();
        WorkManager.GetInstance(Context).EnqueueUniquePeriodicWork(WorkName, ExistingPeriodicWorkPolicy.Keep!, (PeriodicWorkRequest)request);
    }

    /// <summary>
    /// Checks, downloads and checks the file, then installs where the rules allow. <paramref name="asked"/> is "Update now": it downloads
    /// on any network, since the person chose to, and installs straight away unless something is open.
    /// </summary>
    internal static async Task RunAsync(bool asked, bool fromWorker = false)
    {
        if (Off is not null || !await One.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            Say("Looking for a newer build…");
            var offer = await AndroidUpdates.CheckAsync(TheOutsideWorld.Current, Build, Kind, UpdateKeys.PublicKey, CancellationToken.None).ConfigureAwait(false);
            Log("update.check", ("result", offer.Refusal.ToString()), ("offered", offer.Version?.Number), ("worker", fromWorker));
            if (offer.Refusal != ApkRefusal.Offline)
            {
                var edit = Prefs.Edit()!.PutLong("checkedUtc", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                edit!.PutString("newest", offer.Version?.Number ?? Build.Version.Number)!.Apply();
            }

            if (!offer.Offered)
            {
                // Entry 288: offline is silent; the settings page says so if somebody looks, and nothing else does.
                Say(offer.Refusal == ApkRefusal.NotNewer ? "GroupLab is up to date." : offer.Refusal.Words());
                return;
            }

            if (!asked && !Unmetered())
            {
                Say($"GroupLab {offer.Version!.Number} is out. It downloads on Wi-Fi, or tap Update now.");
                return;
            }

            var started = DateTimeOffset.UtcNow;
            var progress = new Progress<double>(share => Say(string.Create(CultureInfo.CurrentCulture, $"Downloading {offer.Version!.Number}: {share:P0}")));
            var (path, refusal) = await AndroidUpdates.DownloadAsync(TheOutsideWorld.Current, offer.Asset!, Folder, progress, CancellationToken.None).ConfigureAwait(false);
            if (path is null)
            {
                Warn("update.download", ("result", refusal.ToString()));
                Say(refusal.Words());
                return;
            }

            var certificateCheck = AndroidUpdates.CheckBeforeInstall(offer.Asset!, path, Certificates(null), Certificates(path));
            if (certificateCheck != ApkRefusal.None)
            {
                Warn("update.refused", ("result", certificateCheck.ToString()), ("version", offer.Version!.Number));
                Say(certificateCheck.Words());
                return;
            }

            Log("update.downloaded", ("version", offer.Version!.Number), ("bytes", offer.Asset!.Bytes), ("seconds", (int)(DateTimeOffset.UtcNow - started).TotalSeconds));
            Prefs.Edit()!
                .PutString("readyPath", path)!
                .PutString("readyVersion", offer.Version.Number)!
                .PutString("readySha", offer.Asset.Sha256)!
                .PutLong("readyBytes", offer.Asset.Bytes)!
                .PutString("readyUrl", offer.Asset.Url)!
                .PutString("readyPublished", offer.PublishedUtc)!
                .Apply();
            Say($"GroupLab {offer.Version.Number} is downloaded and checked.");
            TryInstall(asked, fromWorker);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Java.Lang.Exception)
        {
            Warn("update.failed", ("type", e.GetType().Name));
            Say("The update could not be finished. GroupLab is unchanged and tries again later.");
        }
        finally
        {
            One.Release();
        }
    }

    /// <summary>Installs the checked download where <see cref="AndroidUpdates.When"/> allows it, and says what it is waiting for where not.</summary>
    internal static void TryInstall(bool asked, bool fromWorker = false)
    {
        if (ReadyAsset() is not { } ready || !File.Exists(ready.Path))
        {
            return;
        }

        var when = AndroidUpdates.When(WorkInProgress.CameraOpen, WorkInProgress.Analysing, WorkInProgress.Unsaved, WorkInProgress.OnScreen, Automatic, asked);
        Log("update.install.when", ("when", when.ToString()), ("silent", Silent), ("mayInstall", MayInstall), ("worker", fromWorker));
        switch (when)
        {
            case InstallWhen.NotDuringWork:
                Say($"GroupLab {ready.Version} is ready. It installs once nothing is open: no camera, no reading, nothing unsaved.");
                return;
            case InstallWhen.OnRequest:
                Say($"GroupLab {ready.Version} is ready. Tap Update now to install it.");
                Notify($"GroupLab {ready.Version} is downloaded. Install it now?", "Update now", () => TryInstall(asked: true));
                return;
            case InstallWhen.WhenLeft:
                // A copy that still needs a tap (installed by adb, or before Android 12) is asked now, while the person is here to tap;
                // one that installs silently waits until they leave, so nothing closes under them.
                if (!Silent && WorkInProgress.OnScreen)
                {
                    Install(ready);
                    return;
                }

                Say($"GroupLab {ready.Version} is ready. It installs when you leave GroupLab.");
                Notify($"GroupLab {ready.Version} is downloaded. It installs when you leave GroupLab.", "Install now", () => TryInstall(asked: true));
                return;
            default:
                // Off screen, a tap cannot be answered: the copy that needs one waits for the next launch.
                if (!Silent && !WorkInProgress.OnScreen)
                {
                    Say($"GroupLab {ready.Version} is ready. Android asks once before GroupLab can update itself; it asks next time you open it.");
                    return;
                }

                Install(ready);
                return;
        }
    }

    /// <summary>
    /// The PackageInstaller session. It asks for no tap from Android 12 on; Android still shows its own question where it requires one, the
    /// first time for a copy installed by adb, and the result comes back to <see cref="InstallResultReceiver"/>.
    /// </summary>
    private static void Install((string Path, string Version, UpdateAsset Asset, string? Published) ready)
    {
        if (!MayInstall)
        {
            Say("Android needs you to allow GroupLab to install apps before it can update itself.");
            if (WorkInProgress.OnScreen)
            {
                Notify("To update itself, GroupLab needs Android's \"Install unknown apps\" permission; it is only ever used for GroupLab's own signed updates.",
                    "Open the setting", OpenUnknownAppsSetting);
            }

            Log("update.install.permission", ("granted", false));
            return;
        }

        // The file is checked once more immediately before it is handed over, in case anything touched it since it was downloaded.
        var check = AndroidUpdates.CheckBeforeInstall(ready.Asset, ready.Path, Certificates(null), Certificates(ready.Path));
        if (check != ApkRefusal.None)
        {
            Warn("update.refused", ("result", check.ToString()), ("version", ready.Version), ("stage", "install"));
            Forget();
            Say(check.Words());
            return;
        }

        var installer = Context.PackageManager!.PackageInstaller;
        var parameters = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
        parameters.SetAppPackageName(Context.PackageName);
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            parameters.SetRequireUserAction((int)PackageInstallUserAction.NotRequired);
        }

        int id = installer.CreateSession(parameters);
        using (var session = installer.OpenSession(id))
        {
            using (var source = File.OpenRead(ready.Path))
            using (var into = session.OpenWrite("grouplab.apk", 0, source.Length))
            {
                source.CopyTo(into);
                session.Fsync(into);
            }

            Prefs.Edit()!
                .PutString("installing", ready.Version)!
                .PutString("installingFrom", Build.Version.Number)!
                .PutString("installingPublished", ready.Published)!
                .PutBoolean("installingSilent", Silent)!
                .Apply();
            Log("update.install.start", ("version", ready.Version), ("silent", Silent), ("sdk", (int)global::Android.OS.Build.VERSION.SdkInt));
            var flags = PendingIntentFlags.UpdateCurrent | (OperatingSystem.IsAndroidVersionAtLeast(31) ? PendingIntentFlags.Mutable : 0);
            var pending = PendingIntent.GetBroadcast(Context, id, new Intent(Context, typeof(InstallResultReceiver)), flags)!;
            session.Commit(pending.IntentSender);
        }

        Say($"Installing GroupLab {ready.Version}…");
    }

    /// <summary>Opens Android's "Install unknown apps" page for GroupLab.</summary>
    internal static void OpenUnknownAppsSetting()
    {
        var intent = new Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources, global::Android.Net.Uri.Parse("package:" + Context.PackageName));
        intent.AddFlags(ActivityFlags.NewTask);
        Context.StartActivity(intent);
    }

    /// <summary>Opens the notes of a version on grouplab.org.</summary>
    internal static void OpenNotes(string version)
    {
        var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(ReleaseNotesPage.For(version)));
        intent.AddFlags(ActivityFlags.NewTask);
        Context.StartActivity(intent);
    }

    /// <summary>What the installer said, from <see cref="InstallResultReceiver"/>. A success ends this process, so it is logged by the next launch.</summary>
    internal static void Installed(int status, string? message)
    {
        Warn("update.install.result", ("status", status), ("message", message));
        if (status != (int)PackageInstallStatus.Success)
        {
            Prefs.Edit()!.Remove("installing")!.Apply();
            Say(status == (int)PackageInstallStatus.FailureAborted
                ? "The update was not installed. It stays downloaded; tap Update now when you want it."
                : "Android did not install the update. GroupLab is unchanged; it tries again later.");
        }
    }

    /// <summary>After an update: "Updated to nightly N", its notes a tap away, and the time from publishing to installing in the log.</summary>
    private static void Announce()
    {
        string? installing = Prefs.GetString("installing", null);
        if (installing is null || installing != Build.Version.Number)
        {
            return;
        }

        string? published = Prefs.GetString("installingPublished", null);
        double? minutes = DateTimeOffset.TryParse(published, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at)
            ? Math.Round((DateTimeOffset.UtcNow - at).TotalMinutes) : null;
        Log("update.installed", ("version", installing), ("from", Prefs.GetString("installingFrom", null)), ("silent", Prefs.GetBoolean("installingSilent", false)),
            ("minutesFromPublish", minutes), ("installer", InstallerOfRecord ?? "none"));
        Forget();
        Prefs.Edit()!.Remove("installing")!.Remove("installingFrom")!.Remove("installingPublished")!.Remove("installingSilent")!.Apply();
        string words = AndroidUpdates.UpdatedTo(Build.Version);
        Say(words + ".");
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Shell.Current?.Notice(words + ".", "What changed", () => OpenNotes(installing)));
    }

    /// <summary>Deletes the download and what was remembered about it.</summary>
    private static void Forget()
    {
        if (Prefs.GetString("readyPath", null) is { } path && File.Exists(path))
        {
            File.Delete(path);
        }

        Prefs.Edit()!.Remove("readyPath")!.Remove("readyVersion")!.Remove("readySha")!.Remove("readyBytes")!.Remove("readyUrl")!.Remove("readyPublished")!.Apply();
    }

    private static (string Path, string Version, UpdateAsset Asset, string? Published)? ReadyAsset()
    {
        var prefs = Prefs;
        if (prefs.GetString("readyPath", null) is not { } path || prefs.GetString("readyVersion", null) is not { } version
            || prefs.GetString("readySha", null) is not { } sha || prefs.GetString("readyUrl", null) is not { } url)
        {
            return null;
        }

        // The installed build already is this version, or newer: the download has no further use.
        if (SemanticVersion.Parse(version) is not { } offered || !UpdateOrder.IsNewer(Build.Version, offered))
        {
            Forget();
            return null;
        }

        var asset = new UpdateAsset(AndroidUpdates.Platform, Kind, Path.GetFileName(path), prefs.GetLong("readyBytes", 0), sha, url);
        return (path, version, asset, prefs.GetString("readyPublished", null));
    }

    /// <summary>
    /// The SHA-256 of each certificate an APK is signed with, or of the installed copy's where <paramref name="apk"/> is null, as Android
    /// reads them: the signers of the APK's contents, or where there is one signer, its certificate history, newest first.
    /// </summary>
    private static List<string> Certificates(string? apk)
    {
        var pm = Context.PackageManager!;
        PackageInfo? info = apk is null
            ? (OperatingSystem.IsAndroidVersionAtLeast(33)
                ? pm.GetPackageInfo(Context.PackageName!, PackageManager.PackageInfoFlags.Of((long)PackageInfoFlags.SigningCertificates))
                : LegacyInfo(pm, null))
            : (OperatingSystem.IsAndroidVersionAtLeast(33)
                ? pm.GetPackageArchiveInfo(apk, PackageManager.PackageInfoFlags.Of((long)PackageInfoFlags.SigningCertificates))
                : LegacyInfo(pm, apk));
        var signing = info?.SigningInfo;
        var signatures = signing is null ? null : signing.HasMultipleSigners ? signing.GetApkContentsSigners() : signing.GetSigningCertificateHistory();
        return signatures?.Select(s => Convert.ToHexStringLower(SHA256.HashData(s.ToByteArray()!))).ToList() ?? [];
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1422", Justification = "Android 10 to 12 have only these forms; Android 13 and later take the typed ones.")]
    private static PackageInfo? LegacyInfo(PackageManager pm, string? apk)
    {
#pragma warning disable CA1422
        return apk is null
            ? pm.GetPackageInfo(Context.PackageName!, PackageInfoFlags.SigningCertificates)
            : pm.GetPackageArchiveInfo(apk, PackageInfoFlags.SigningCertificates);
#pragma warning restore CA1422
    }

    /// <summary>Whether the network in use is not metered, which is what Wi-Fi only means to Android and to WorkManager.</summary>
    private static bool Unmetered()
    {
        var connectivity = (ConnectivityManager?)Context.GetSystemService(Context.ConnectivityService);
        return connectivity?.GetNetworkCapabilities(connectivity.ActiveNetwork) is { } capabilities && capabilities.HasCapability(NetCapability.NotMetered);
    }

    private static void Say(string words)
    {
        Status = words;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => Changed?.Invoke());
    }

    private static void Notify(string words, string action, Action acted)
    {
        if (WorkInProgress.OnScreen)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Shell.Current?.Notice(words, action, acted));
        }
    }

    // The worker can run in a process where GroupLab's window never started, and so its log never opened: those lines go to logcat alone.
    private static void Log(string name, params (string Key, object? Value)[] fields)
    {
        if (DiagnosticLog.Current.IsEnabled)
        {
            DiagnosticLog.Info(name, fields);
        }
        else
        {
            global::Android.Util.Log.Info(App.LogTag, name + " " + string.Join(' ', fields.Select(f => f.Key + "=" + f.Value)));
        }
    }

    private static void Warn(string name, params (string Key, object? Value)[] fields)
    {
        if (DiagnosticLog.Current.IsEnabled)
        {
            DiagnosticLog.Warn(name, fields);
        }
        else
        {
            global::Android.Util.Log.Warn(App.LogTag, name + " " + string.Join(' ', fields.Select(f => f.Key + "=" + f.Value)));
        }
    }
}
