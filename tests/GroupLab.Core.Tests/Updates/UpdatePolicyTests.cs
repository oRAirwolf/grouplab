using System.Text;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 119 section 9: the updater's logic is tested without touching GitHub. Everything here is a manifest this
/// test made and signed with a key this test made, so a run needs no network and no secret, and a signature that should fail can be made to
/// fail on purpose.
/// </summary>
public class UpdatePolicyTests
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private static SignedManifest Manifest(string version, string train = "nightly", string? signWith = null, string notes = "hello")
    {
        var manifest = new UpdateManifest(UpdateManifest.Current, version, train, "abc1234", "2026-09-21T00:00:00Z", notes,
            [new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", 6, UpdateSignature.Sha256("abcdef"u8), "https://example.invalid/x")]);
        return UpdateSignature.Sign(manifest, Convert.FromBase64String(signWith ?? Key.PrivateKeyBase64));
    }

    private static BuildIdentity Installed(string version = "0.2.0-nightly.5", string train = "nightly") =>
        BuildIdentity.Read(version + "+abc1234def", train);

    [Fact]
    public void ANewerBuildOnTheSameTrainIsOffered()
    {
        var decision = UpdatePolicy.Decide(Installed(), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest("0.2.0-nightly.6"), Key.PublicKeyBase64);
        Assert.True(decision.Offer);
        Assert.Equal("0.2.0-nightly.6", decision.Version!.Number);
        Assert.Contains("ready to install", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSameBuildOrAnOlderOneIsNotOffered()
    {
        foreach (string version in new[] { "0.2.0-nightly.5", "0.2.0-nightly.4" })
        {
            var decision = UpdatePolicy.Decide(Installed(), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest(version), Key.PublicKeyBase64);
            Assert.False(decision.Offer);
            Assert.Equal(UpdateSignature.Refusal.NotNewer, decision.Refusal);
        }
    }

    [Fact]
    public void ASignatureFromAnotherKeyIsRefusedAndSaysSo()
    {
        var stranger = UpdateSignature.NewKeyPair();
        var decision = UpdatePolicy.Decide(Installed(), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest("0.2.0-nightly.6", signWith: stranger.PrivateKeyBase64), Key.PublicKeyBase64);
        Assert.False(decision.Offer);
        Assert.Equal(UpdateSignature.Refusal.BadSignature, decision.Refusal);
        Assert.Contains("does not match", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AManifestChangedAfterSigningIsRefused()
    {
        var signed = Manifest("0.2.0-nightly.6");
        var tampered = signed with { Payload = signed.Payload with { Version = "9.9.9" } };
        Assert.Equal(UpdateSignature.Refusal.BadSignature, UpdateSignature.Verify(tampered, Key.PublicKeyBase64));

        // The notes are signed too, so nobody can put words in GroupLab's mouth without breaking the signature.
        var reworded = signed with { Payload = signed.Payload with { Notes = "download this instead" } };
        Assert.Equal(UpdateSignature.Refusal.BadSignature, UpdateSignature.Verify(reworded, Key.PublicKeyBase64));
    }

    [Fact]
    public void ABuildWithNoKeyInstallsNothing()
    {
        var decision = UpdatePolicy.Decide(Installed(), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest("0.2.0-nightly.6"), publicKey: "");
        Assert.False(decision.Offer);
        Assert.Equal(UpdateSignature.Refusal.NoKey, decision.Refusal);
        Assert.Contains("forged", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ADevelopmentBuildNeverOffersToUpdateItself()
    {
        var decision = UpdatePolicy.Decide(BuildIdentity.Read("0.2.0+abc", null), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest("9.9.9"), Key.PublicKeyBase64);
        Assert.False(decision.Offer);
        Assert.Contains("development build", decision.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AManifestFromTheWrongTrainIsLeftAlone()
    {
        // Someone on release is never offered a nightly.
        var onRelease = UpdatePolicy.Decide(Installed("0.2.0", "release"), UpdatePreferences.Default(UpdateTrain.Release), Manifest("0.3.0-nightly.1"), Key.PublicKeyBase64);
        Assert.False(onRelease.Offer);
        Assert.Equal(UpdateSignature.Refusal.WrongTrain, onRelease.Refusal);

        // Someone on nightly is offered a newer release, which is the way round entry 119 section 4.5 allows.
        var onNightly = UpdatePolicy.Decide(Installed(), UpdatePreferences.Default(UpdateTrain.Nightly), Manifest("0.3.0", "release"), Key.PublicKeyBase64);
        Assert.True(onNightly.Offer);
    }

    [Fact]
    public void SkipIsSilentUntilSomethingNewerThanTheSkippedBuild()
    {
        var preferences = UpdatePreferences.Default(UpdateTrain.Nightly) with { SkippedVersion = "0.2.0-nightly.6" };
        Assert.False(UpdatePolicy.Decide(Installed(), preferences, Manifest("0.2.0-nightly.6"), Key.PublicKeyBase64).Offer);
        Assert.True(UpdatePolicy.Decide(Installed(), preferences, Manifest("0.2.0-nightly.7"), Key.PublicKeyBase64).Offer);
    }

    [Fact]
    public void HowOftenItLooksIsWhatWasChosen()
    {
        var now = DateTimeOffset.Parse("2026-09-21T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        var never = UpdatePreferences.Default(UpdateTrain.Nightly) with { Interval = UpdateCheckInterval.Never };
        Assert.False(UpdatePolicy.ShouldCheck(never, now, launching: true));

        var launch = UpdatePreferences.Default(UpdateTrain.Nightly);
        Assert.True(UpdatePolicy.ShouldCheck(launch, now, launching: true));
        Assert.False(UpdatePolicy.ShouldCheck(launch, now, launching: false));

        var daily = launch with { Interval = UpdateCheckInterval.Daily, LastCheckUtc = now.AddHours(-23) };
        Assert.False(UpdatePolicy.ShouldCheck(daily, now, launching: true));
        Assert.True(UpdatePolicy.ShouldCheck(daily with { LastCheckUtc = now.AddHours(-25) }, now, launching: true));

        var weekly = launch with { Interval = UpdateCheckInterval.Weekly, LastCheckUtc = now.AddDays(-6) };
        Assert.False(UpdatePolicy.ShouldCheck(weekly, now, launching: true));
        Assert.True(UpdatePolicy.ShouldCheck(weekly with { LastCheckUtc = null }, now, launching: true));
    }

    [Fact]
    public void ADownloadThatIsNotWhatWasPromisedIsRefused()
    {
        var asset = new UpdateAsset("windows", "installer", "x.exe", 6, UpdateSignature.Sha256("abcdef"u8), "https://example.invalid/x");
        Assert.True(UpdatePolicy.Matches(asset, "abcdef"u8));

        // A different file of the same length, and a truncated one: both refused.
        Assert.False(UpdatePolicy.Matches(asset, "abcdeg"u8));
        Assert.False(UpdatePolicy.Matches(asset, "abc"u8));
        Assert.Contains("thrown away", UpdatePolicy.DownloadDoesNotMatch, StringComparison.Ordinal);
    }

    [Fact]
    public void AManifestFromANewerFormatIsRefusedByName()
    {
        var signed = Manifest("0.2.0-nightly.6");
        var future = signed with { Payload = signed.Payload with { Manifest = UpdateManifest.Current + 1 } };
        Assert.Equal(UpdateSignature.Refusal.UnknownManifest, UpdateSignature.Verify(future, Key.PublicKeyBase64));
        Assert.Null(UpdateManifest.Read(future.Payload.ToJson()));

        // And what is not a manifest at all is not mistaken for one.
        Assert.Null(SignedManifest.Read("{}"));
        Assert.Null(SignedManifest.Read("not json"));
        Assert.Null(UpdateManifest.Read(null));
    }

    [Fact]
    public void AManifestSurvivesBeingWrittenAndReadBack()
    {
        var signed = Manifest("0.2.0-nightly.6");
        var again = SignedManifest.Read(signed.ToJson());
        Assert.NotNull(again);
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(again, Key.PublicKeyBase64));
        Assert.Equal("0.2.0-nightly.6", again!.Payload.Offered!.Number);
        Assert.Equal(UpdateTrain.Nightly, again.Payload.OnTrain);
        Assert.Equal("grouplab-setup-win-x64.exe", again.Payload.For("windows", "installer")!.Name);
        Assert.Null(again.Payload.For("macos", "installer"));
        Assert.Equal(UpdateSignature.Algorithm, again.Algorithm);

        // The bytes that are signed do not depend on how the JSON was laid out when it arrived.
        Assert.Equal(Encoding.UTF8.GetString(signed.Payload.Signable()), Encoding.UTF8.GetString(again.Payload.Signable()));
    }
}
