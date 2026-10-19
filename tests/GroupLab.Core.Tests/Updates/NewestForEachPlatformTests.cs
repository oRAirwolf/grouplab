using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 454: a phone follows the newest nightly that has an Android file, even when a newer nightly has none yet.
/// Alan's tablet stopped at nightly 202 because 203 was published with no Android download and the phone read only the newest build. The
/// cases the entry names are each here: a nightly with no Android file, one whose file arrives late, a phone on the build before, a phone
/// already on the newest Android build, an older build reading the new manifest, and the desktop doing the same for its own platform.
/// </summary>
public class NewestForEachPlatformTests : IDisposable
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private static readonly byte[] Apk203 = Encoding.UTF8.GetBytes("nightly 203's GroupLab Dev APK");

    private const string Numbered203 = "https://example.invalid/v0.2.0-nightly.203/grouplab-0.2.0-nightly.203-android-dev-d1c07ea7.apk";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "grouplab-newest-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_folder))
        {
            GroupLab.Tests.Support.Temp.Delete(_folder);
        }
    }

    private static UpdateAsset Desktop(string version) =>
        new("windows", "installer", "grouplab-setup-win-x64.exe", 10, new string('1', 64), "https://example.invalid/nightly/grouplab-setup-win-x64.exe#" + version);

    private static UpdateAsset Android203 => new(AndroidUpdates.Platform, AndroidUpdates.DevKind, "grouplab-0.2.0-nightly.203-android-dev-d1c07ea7.apk",
        Apk203.LongLength, UpdateSignature.Sha256(Apk203), Numbered203);

    /// <summary>
    /// Nightly 204 as its publish job signs it: the desktop files at the top, no Android file there, and Android's newest build carried as
    /// 203 on its own numbered release.
    /// </summary>
    private static UpdateManifest Nightly204WithoutAndroid() =>
        new(UpdateManifest.Current, "0.2.0-nightly.204", "nightly", "e5e5e5e5", "2026-10-20T06:00:00Z", "what changed in 204",
            [Desktop("204")],
            [new VersionNotes("0.2.0-nightly.204", "what changed in 204"), new VersionNotes("0.2.0-nightly.203", "what changed in 203")],
            [
                new PlatformBuild("windows", "0.2.0-nightly.204", "e5e5e5e5", "2026-10-20T06:00:00Z", [Desktop("204")]),
                new PlatformBuild(AndroidUpdates.Platform, "0.2.0-nightly.203", "d1c07ea7", "2026-10-19T06:00:00Z", [Android203]),
            ]);

    private static RecordedOutsideWorld Publishing(UpdateManifest manifest)
    {
        var outside = new RecordedOutsideWorld();
        outside.Text[UpdateTrain.Nightly.PublishedAddress()!] = UpdateSignature.Publish(manifest, Convert.FromBase64String(Key.PrivateKeyBase64)).ToJson();
        outside.Download[Numbered203] = Apk203;
        return outside;
    }

    private static BuildIdentity Phone(int nightly) => AndroidUpdates.Identity($"0.2.0-nightly.{nightly}-dev+abc1234", "nightly");

    private static Task<ApkOffer> Check(UpdateManifest manifest, int nightly) =>
        AndroidUpdates.CheckAsync(Publishing(manifest), Phone(nightly), AndroidUpdates.DevKind, Key.PublicKeyBase64, CancellationToken.None);

    [Fact]
    public async Task APhoneOnTheBuildBeforeIsOfferedTheNewestBuildThatHasAnAndroidFile()
    {
        var offer = await Check(Nightly204WithoutAndroid(), 202);
        Assert.True(offer.Offered, offer.Says);
        Assert.Equal("0.2.0-nightly.203", offer.Version!.Number);
        Assert.Equal(Numbered203, offer.Asset!.Url);
        // Its own notes, not 204's, which it does not install.
        Assert.Contains("what changed in 203", offer.Notes, StringComparison.Ordinal);
        Assert.DoesNotContain("what changed in 204", offer.Notes, StringComparison.Ordinal);

        var (path, refusal) = await AndroidUpdates.DownloadAsync(Publishing(Nightly204WithoutAndroid()), offer.Asset, _folder, null, CancellationToken.None);
        Assert.Equal(ApkRefusal.None, refusal);
        Assert.True(AndroidUpdates.Matches(offer.Asset, path!));
    }

    [Fact]
    public async Task APhoneAlreadyOnTheNewestAndroidBuildIsToldSoInItsOwnWords()
    {
        foreach (int nightly in new[] { 203, 204 })
        {
            var offer = await Check(Nightly204WithoutAndroid(), nightly);
            Assert.Equal(ApkRefusal.NotNewer, offer.Refusal);
            Assert.False(offer.Offered);
            Assert.Equal($"You have the newest GroupLab Dev for Android, 0.2.0-nightly.{nightly}.", offer.Says);
        }
    }

    [Fact]
    public async Task ANightlyWhoseAndroidFileArrivesLateIsOfferedOnceItIsThere()
    {
        // The Android job signs 204 again with its files: at the top, and as Android's newest build.
        var apk204 = new UpdateAsset(AndroidUpdates.Platform, AndroidUpdates.DevKind, "grouplab-0.2.0-nightly.204-android-dev-e5e5e5e5.apk", Apk203.LongLength,
            UpdateSignature.Sha256(Apk203), "https://example.invalid/v0.2.0-nightly.204/grouplab-0.2.0-nightly.204-android-dev-e5e5e5e5.apk");
        var before = Nightly204WithoutAndroid();
        var after = before with
        {
            Assets = [.. before.Assets, apk204 with { Url = "https://example.invalid/nightly/grouplab-android-dev.apk" }],
            Platforms = [before.Platforms![0], new PlatformBuild(AndroidUpdates.Platform, "0.2.0-nightly.204", "e5e5e5e5", "2026-10-20T06:20:00Z", [apk204])],
        };

        Assert.Equal("0.2.0-nightly.203", (await Check(before, 202)).Version!.Number);
        var offer = await Check(after, 203);
        Assert.True(offer.Offered, offer.Says);
        Assert.Equal("0.2.0-nightly.204", offer.Version!.Number);
        Assert.Equal(apk204.Url, offer.Asset!.Url);
    }

    [Fact]
    public async Task AManifestThatNamesNoAndroidBuildAtAllSaysThePhoneIsOnTheNewest()
    {
        var none = Nightly204WithoutAndroid() with { Platforms = null };
        var offer = await Check(none, 202);
        Assert.Equal(ApkRefusal.NoApk, offer.Refusal);
        Assert.Equal("You have the newest GroupLab Dev for Android, 0.2.0-nightly.202.", offer.Says);
        Assert.DoesNotContain("204", offer.Says, StringComparison.Ordinal);
    }

    /// <summary>
    /// What a phone built before entry 454 reads: the record without the new field. It still verifies (the second format is checked over the
    /// bytes as written), it ignores the field, and the newest build has no Android file at the top, so it offers nothing rather than an
    /// older APK under a newer number. An older phone handed 203's APK as "204" would reinstall 203 for ever.
    /// </summary>
    [Fact]
    public void AnOlderBuildReadsTheNewManifestSafely()
    {
        var published = UpdateSignature.Publish(Nightly204WithoutAndroid(), Convert.FromBase64String(Key.PrivateKeyBase64));
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(PublishedManifest.Read(published.ToJson()), Key.PublicKeyBase64));

        string body = Encoding.UTF8.GetString(published.Bytes!);
        var old = JsonSerializer.Deserialize<BeforeEntry454>(body, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })!;
        Assert.Equal("0.2.0-nightly.204", old.Version);
        Assert.DoesNotContain(old.Assets, a => a.Platform == AndroidUpdates.Platform);

        // And the first format, which every desktop build before the second reads, carries nothing new at all.
        var signed = UpdateSignature.Sign(Nightly204WithoutAndroid(), Convert.FromBase64String(Key.PrivateKeyBase64));
        Assert.Null(signed.Payload.Platforms);
        Assert.DoesNotContain("platforms", signed.ToJson(), StringComparison.Ordinal);
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(SignedManifest.Read(signed.ToJson()), Key.PublicKeyBase64));
    }

    /// <summary>The manifest record as it was before entry 454, which is what every phone and desktop already installed deserialises into.</summary>
    private sealed record BeforeEntry454(
        [property: JsonPropertyName("manifest")] int Manifest,
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("train")] string Train,
        [property: JsonPropertyName("commit")] string Commit,
        [property: JsonPropertyName("publishedUtc")] string PublishedUtc,
        [property: JsonPropertyName("notes")] string Notes,
        [property: JsonPropertyName("assets")] IReadOnlyList<UpdateAsset> Assets,
        [property: JsonPropertyName("versions")] IReadOnlyList<VersionNotes>? Versions = null);

    [Fact]
    public void TheDesktopComparesItselfWithItsOwnPlatformsBuild()
    {
        // A Linux entry left on 203 while Windows is on 204: a Linux build on 203 is offered nothing, a Windows build on 203 is offered 204.
        var manifest = Nightly204WithoutAndroid() with
        {
            Platforms = [.. Nightly204WithoutAndroid().Platforms!, new PlatformBuild("linux", "0.2.0-nightly.203", "d1c07ea7", "2026-10-19T06:00:00Z", [])],
        };
        var published = UpdateSignature.Publish(manifest, Convert.FromBase64String(Key.PrivateKeyBase64));
        var on203 = BuildIdentity.Read("0.2.0-nightly.203+d1c07ea", "nightly");
        var prefs = UpdatePreferences.Default(UpdateTrain.Nightly);

        var linux = UpdatePolicy.Decide(on203, prefs, published, Key.PublicKeyBase64, "linux");
        Assert.False(linux.Offer);
        Assert.Equal(UpdateSignature.Refusal.NotNewer, linux.Refusal);

        var windows = UpdatePolicy.Decide(on203, prefs, published, Key.PublicKeyBase64, "windows");
        Assert.True(windows.Offer, windows.Reason);
        Assert.Equal("0.2.0-nightly.204", windows.Version!.Number);

        // A platform the manifest names no build for reads the manifest as it is, as macOS does.
        Assert.Equal("0.2.0-nightly.204", UpdatePolicy.Decide(on203, prefs, published, Key.PublicKeyBase64, "macos").Version!.Number);
    }

    [Fact]
    public async Task TheDesktopUpdateRunDownloadsItsOwnPlatformsBuild()
    {
        var outside = Publishing(Nightly204WithoutAndroid());
        var run = new UpdateRun(outside, BuildIdentity.Read("0.2.0-nightly.202+ff9f61c", "nightly"), _folder);
        var (state, decision) = await run.CheckAsync(UpdatePreferences.Default(UpdateTrain.Nightly), Key.PublicKeyBase64, CancellationToken.None, "windows");
        Assert.True(decision!.Offer, state.Says);
        Assert.Equal("0.2.0-nightly.204", run.Manifest!.Version);
        Assert.Null(run.Manifest.Platforms);
    }

    /// <summary>
    /// The command line that signs the nightly's manifest: this build's own files are each platform's newest; a platform this build has no
    /// file for keeps the entry the manifest before named, when that is an older build; and a manifest before that does not verify carries
    /// nothing at all.
    /// </summary>
    [Fact]
    public void TheNightlyCarriesAPlatformsBuildOnlyWhereItHasNoFileOfItsOwn()
    {
        Directory.CreateDirectory(_folder);
        string before = Path.Combine(_folder, "previous-manifest-2.json");
        var nightly203 = new UpdateManifest(UpdateManifest.Current, "0.2.0-nightly.203", "nightly", "d1c07ea7", "2026-10-19T06:00:00Z", "", [Desktop("203"), Android203],
            null, [new PlatformBuild("windows", "0.2.0-nightly.203", "d1c07ea7", "2026-10-19T06:00:00Z", [Desktop("203")]),
                   new PlatformBuild(AndroidUpdates.Platform, "0.2.0-nightly.203", "d1c07ea7", "2026-10-19T06:00:00Z", [Android203])]);
        File.WriteAllText(before, UpdateSignature.Publish(nightly203, Convert.FromBase64String(Key.PrivateKeyBase64)).ToJson());
        var error = new StringWriter();

        var builds = GroupLab.Cli.UpdateVerbs.Platforms("0.2.0-nightly.204", "e5e5e5e5", "2026-10-20T06:00:00Z", [Desktop("204")], before, Key.PrivateKeyBase64, error);
        Assert.Equal("0.2.0-nightly.204", Assert.Single(builds, b => b.Platform == "windows").Version);
        Assert.Equal(Numbered203, Assert.Single(Assert.Single(builds, b => b.Platform == AndroidUpdates.Platform).Assets).Url);

        // With its own Android file, 204's is Android's newest and nothing is carried.
        var own = GroupLab.Cli.UpdateVerbs.Platforms("0.2.0-nightly.204", "e5e5e5e5", "2026-10-20T06:00:00Z",
            [Desktop("204"), Android203 with { Name = "204.apk" }], before, Key.PrivateKeyBase64, error);
        Assert.Equal("0.2.0-nightly.204", Assert.Single(own, b => b.Platform == AndroidUpdates.Platform).Version);

        // A manifest before that is not older is never carried: the Android job signing 203 again must not name 203 as carried from itself.
        var same = GroupLab.Cli.UpdateVerbs.Platforms("0.2.0-nightly.203", "d1c07ea7", "2026-10-19T06:00:00Z", [Desktop("203")], before, Key.PrivateKeyBase64, error);
        Assert.DoesNotContain(same, b => b.Platform == AndroidUpdates.Platform);

        // Signed with another key: nothing carried, and said.
        var other = UpdateSignature.NewKeyPair();
        File.WriteAllText(before, UpdateSignature.Publish(nightly203, Convert.FromBase64String(other.PrivateKeyBase64)).ToJson());
        var forged = GroupLab.Cli.UpdateVerbs.Platforms("0.2.0-nightly.204", "e5e5e5e5", "2026-10-20T06:00:00Z", [Desktop("204")], before, Key.PrivateKeyBase64, error);
        Assert.DoesNotContain(forged, b => b.Platform == AndroidUpdates.Platform);
        Assert.Contains("does not verify", error.ToString(), StringComparison.Ordinal);
    }
}
