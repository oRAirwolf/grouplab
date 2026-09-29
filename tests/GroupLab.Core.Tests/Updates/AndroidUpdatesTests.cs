using System.Text;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 288: the Android updater's rules, driven with no network and no phone. The six the entry names are all here:
/// a bad signature, a wrong hash, a different certificate, an older version, another train, and offline. The rest hold what the entry adds
/// around them: when it may install, when it may do so without a tap, the copy from Google Play, and a download that carries on.
/// </summary>
public class AndroidUpdatesTests : IDisposable
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private const string Address = "https://example.invalid/grouplab-android-dev.apk";

    private static readonly byte[] TheApk = Encoding.UTF8.GetBytes("this stands in for GroupLab Dev's APK");

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "grouplab-android-updates-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            if (Directory.Exists(_folder))
            {
                GroupLab.Tests.Support.Temp.Delete(_folder);
            }
        }
        catch (IOException)
        {
        }
    }

    private static UpdateManifest Manifest(string version = "0.2.0-nightly.125", string train = "nightly", byte[]? apk = null, string? saySha = null, string kind = AndroidUpdates.DevKind)
    {
        byte[] bytes = apk ?? TheApk;
        return new UpdateManifest(UpdateManifest.Current, version, train, "abc1234", "2026-09-29T00:00:00Z", "what changed",
        [
            new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", 10, new string('0', 64), "https://example.invalid/setup.exe"),
            new UpdateAsset(AndroidUpdates.Platform, kind, "grouplab-android-dev.apk", bytes.LongLength, saySha ?? UpdateSignature.Sha256(bytes), Address),
        ]);
    }

    private static RecordedOutsideWorld Publishing(UpdateManifest manifest, string? privateKey = null)
    {
        var outside = new RecordedOutsideWorld();
        outside.Text[UpdateTrain.Nightly.PublishedAddress()!] =
            UpdateSignature.Publish(manifest, Convert.FromBase64String(privateKey ?? Key.PrivateKeyBase64)).ToJson();
        outside.Download[Address] = TheApk;
        return outside;
    }

    /// <summary>GroupLab Dev nightly 124, as the nightly stamps it.</summary>
    private static BuildIdentity Installed(string version = "0.2.0-nightly.124-dev+abc1234") => AndroidUpdates.Identity(version, "nightly");

    [Fact]
    public void TheDevMarkIsNotPartOfTheOrder()
    {
        var build = Installed();
        Assert.Equal(UpdateTrain.Nightly, build.Train);
        Assert.Equal("0.2.0-nightly.124", build.Version.Number);
        Assert.Equal("abc1234", build.Commit);
        Assert.True(UpdateOrder.IsNewer(build.Version, SemanticVersion.Parse("0.2.0-nightly.125")!));

        // A copy built on somebody's own machine names no train, and never updates itself.
        Assert.True(AndroidUpdates.Identity("0.2.0-dev", null).IsDevelopment);
    }

    [Fact]
    public async Task ANewerBuildOnTheTrainIsOfferedWithItsApk()
    {
        var offer = await AndroidUpdates.CheckAsync(Publishing(Manifest()), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.True(offer.Offered, offer.Says);
        Assert.Equal("0.2.0-nightly.125", offer.Version!.Number);
        Assert.Equal(Address, offer.Asset!.Url);
    }

    [Fact]
    public async Task ABadSignatureIsRefusedAndNothingIsDownloaded()
    {
        var other = UpdateSignature.NewKeyPair();
        var outside = Publishing(Manifest(), other.PrivateKeyBase64);
        var offer = await AndroidUpdates.CheckAsync(outside, Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.Equal(ApkRefusal.BadSignature, offer.Refusal);
        Assert.Null(offer.Asset);
        Assert.DoesNotContain(outside.Asked, a => a.What == "download");
    }

    [Fact]
    public async Task AWrongHashIsDeletedAndNeverOffered()
    {
        var manifest = Manifest(saySha: new string('a', 64));
        var offer = await AndroidUpdates.CheckAsync(Publishing(manifest), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.True(offer.Offered);

        var (path, refusal) = await AndroidUpdates.DownloadAsync(Publishing(manifest), offer.Asset!, _folder, null, CancellationToken.None);
        Assert.Null(path);
        Assert.Equal(ApkRefusal.WrongHash, refusal);
        Assert.Empty(Directory.EnumerateFiles(_folder));
    }

    [Fact]
    public async Task ADifferentCertificateIsDeletedBeforeInstalling()
    {
        var manifest = Manifest();
        var asset = manifest.For(AndroidUpdates.Platform, AndroidUpdates.DevKind)!;
        var (path, refusal) = await AndroidUpdates.DownloadAsync(Publishing(manifest), asset, _folder, null, CancellationToken.None);
        Assert.Equal(ApkRefusal.None, refusal);
        Assert.True(File.Exists(path));

        Assert.Equal(ApkRefusal.DifferentCertificate, AndroidUpdates.CheckBeforeInstall(asset, path!, ["AB12"], ["cd34"]));
        Assert.False(File.Exists(path));

        // The same certificate, however Android happened to write its case, is the same certificate.
        (path, _) = await AndroidUpdates.DownloadAsync(Publishing(manifest), asset, _folder, null, CancellationToken.None);
        Assert.Equal(ApkRefusal.None, AndroidUpdates.CheckBeforeInstall(asset, path!, ["AB12"], ["ab12"]));
        Assert.True(File.Exists(path));

        // No certificate at all is never a match.
        Assert.False(AndroidUpdates.SameCertificates([], []));
    }

    [Fact]
    public async Task AnOlderOrTheSameVersionIsNotOffered()
    {
        foreach (string version in new[] { "0.2.0-nightly.123", "0.2.0-nightly.124" })
        {
            var offer = await AndroidUpdates.CheckAsync(Publishing(Manifest(version)), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
            Assert.Equal(ApkRefusal.NotNewer, offer.Refusal);
            Assert.False(offer.Offered);
        }
    }

    [Fact]
    public async Task AnotherTrainIsLeftAlone()
    {
        // A nightly build takes from steadier trains, never the reverse; a manifest naming a train this build does not know is another train.
        var offer = await AndroidUpdates.CheckAsync(Publishing(Manifest("0.2.0-nightly.125", "canary")), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.False(offer.Offered);
        Assert.NotEqual(ApkRefusal.None, offer.Refusal);

        var beta = AndroidUpdates.Identity("0.2.0-beta.3-dev", "beta");
        var fromNightly = await AndroidUpdates.CheckAsync(Publishing(Manifest()), beta, AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.False(fromNightly.Offered);
    }

    [Fact]
    public async Task OfflineIsSilent()
    {
        var offer = await AndroidUpdates.CheckAsync(new RecordedOutsideWorld(), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.Equal(ApkRefusal.Offline, offer.Refusal);
        Assert.False(offer.Offered);
    }

    [Fact]
    public async Task ABuildWithNoApkOfItsKindOffersNothing()
    {
        var offer = await AndroidUpdates.CheckAsync(Publishing(Manifest(kind: "apk-other")), Installed(), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.Equal(ApkRefusal.NoApk, offer.Refusal);
    }

    [Fact]
    public async Task ADevelopmentBuildNeverLooks()
    {
        var outside = Publishing(Manifest());
        var offer = await AndroidUpdates.CheckAsync(outside, AndroidUpdates.Identity("0.2.0", null), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);
        Assert.Equal(ApkRefusal.Development, offer.Refusal);
        Assert.Empty(outside.Asked);
    }

    [Fact]
    public async Task AFinishedDownloadIsKeptAndAnOlderOneIsCleared()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "grouplab-android-dev-older.apk"), "an older update");
        var asset = Manifest().For(AndroidUpdates.Platform, AndroidUpdates.DevKind)!;
        var (path, _) = await AndroidUpdates.DownloadAsync(Publishing(Manifest()), asset, _folder, null, CancellationToken.None);
        Assert.Equal([path!], Directory.EnumerateFiles(_folder));

        // The second time, the checked file already here is used, and nothing is downloaded again.
        var again = Publishing(Manifest());
        var (second, refusal) = await AndroidUpdates.DownloadAsync(again, asset, _folder, null, CancellationToken.None);
        Assert.Equal(path, second);
        Assert.Equal(ApkRefusal.None, refusal);
        Assert.DoesNotContain(again.Asked, a => a.What == "download");
    }

    [Fact]
    public void NeverInTheMiddleOfWork()
    {
        Assert.Equal(InstallWhen.NotDuringWork, AndroidUpdates.When(cameraOpen: true, analysing: false, unsaved: false, onScreen: false, automatic: true, askedNow: true));
        Assert.Equal(InstallWhen.NotDuringWork, AndroidUpdates.When(false, analysing: true, false, false, true, false));
        Assert.Equal(InstallWhen.NotDuringWork, AndroidUpdates.When(false, false, unsaved: true, false, true, true));
        Assert.Equal(InstallWhen.Now, AndroidUpdates.When(false, false, false, onScreen: true, automatic: false, askedNow: true));
        Assert.Equal(InstallWhen.OnRequest, AndroidUpdates.When(false, false, false, onScreen: false, automatic: false, askedNow: false));
        Assert.Equal(InstallWhen.WhenLeft, AndroidUpdates.When(false, false, false, onScreen: true, automatic: true, askedNow: false));
        Assert.Equal(InstallWhen.Now, AndroidUpdates.When(false, false, false, onScreen: false, automatic: true, askedNow: false));
    }

    [Fact]
    public void SilentOnlyFromAndroid12AndOnlyOnceItIsItsOwnInstaller()
    {
        const string own = "org.grouplab.app.dev";
        Assert.False(AndroidUpdates.MayInstallSilently(34, null, own));
        Assert.False(AndroidUpdates.MayInstallSilently(34, "com.android.shell", own));
        Assert.False(AndroidUpdates.MayInstallSilently(30, own, own));
        Assert.True(AndroidUpdates.MayInstallSilently(31, own, own));
        Assert.True(AndroidUpdates.MayInstallSilently(36, own, own));
    }

    [Fact]
    public void ACopyFromGooglePlayIsLeftToGooglePlay()
    {
        Assert.True(AndroidUpdates.IsStoreCopy("com.android.vending"));
        Assert.False(AndroidUpdates.IsStoreCopy("org.grouplab.app.dev"));
        Assert.False(AndroidUpdates.IsStoreCopy(null));
    }

    [Fact]
    public void TheNoticeNamesTheNightly()
    {
        Assert.Equal("Updated to nightly 125", AndroidUpdates.UpdatedTo(SemanticVersion.Parse("0.2.0-nightly.125")!));
        Assert.Equal("Updated to GroupLab 0.3.0", AndroidUpdates.UpdatedTo(SemanticVersion.Parse("0.3.0")!));
    }
}
