using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 139: the signature covers the bytes as published, so the manifest can gain a field without stranding anybody.
/// <para>
/// <b>The fault this format exists because of.</b> The first format signs a record and verifies by serialising the record it was read into. A
/// build that does not know a field drops it on the way back out, the bytes it checks are not the bytes that were signed, and the update is
/// refused as <c>BadSignature</c>. Entry 138 section 5 added one field, nightly 42 published with it, and nightly 37 answered
/// <c>refusal=BadSignature</c>: every installed build unable to update itself at all, which is worse than anything that change carried.
/// </para>
/// <para>
/// <b>The two halves of the fix are both held here.</b> The second format never re-serialises anything, so an unknown field is ignored; and
/// the first format can never gain a field, which <see cref="TheFirstFormatCannotGainAFieldHoweverItIsAskedTo"/> and
/// <see cref="AnOlderBuildsUpdaterStillAcceptsAManifestPublishedToday"/> hold from both ends.
/// </para>
/// </summary>
public class PublishedManifestTests
{
    private static readonly (string PrivateKeyBase64, string PublicKeyBase64) Key = UpdateSignature.NewKeyPair();

    private static UpdateManifest Built(IReadOnlyList<VersionNotes>? versions = null) => new(
        UpdateManifest.Current, "0.2.0-nightly.44", "nightly", "abc1234", "2026-09-22T00:00:00Z", "what changed",
        [new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", 1234, "abcd", "https://example.invalid/a.exe")],
        versions);

    private static byte[] Private() => Convert.FromBase64String(Key.PrivateKeyBase64);

    /// <summary>Entry 139 section 1: the round trip, with the notes of the builds somebody skipped riding along.</summary>
    [Fact]
    public void WhatIsPublishedVerifiesAndReadsBackAsItself()
    {
        var published = UpdateSignature.Publish(Built([new VersionNotes("0.2.0-nightly.44", "the newest build's own change")]), Private());

        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(published, Key.PublicKeyBase64));
        var body = published.Body!;
        Assert.Equal("0.2.0-nightly.44", body.Version);
        Assert.Equal("grouplab-setup-win-x64.exe", body.For("windows", "installer")!.Name);
        Assert.Equal(["0.2.0-nightly.44"], body.Versions!.Select(v => v.Version));

        // And it survives being written to a file and read back, which is the only way it ever travels.
        var again = PublishedManifest.Read(published.ToJson());
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(again, Key.PublicKeyBase64));
    }

    /// <summary>
    /// Entry 139 section 4, the one that matters: a manifest carrying fields this build has never heard of verifies, and parses, and the
    /// unknown fields are simply not there afterwards. Under the first format this is the exact shape of a bricked updater.
    /// </summary>
    [Fact]
    public void AManifestWithFieldsThisBuildDoesNotKnowVerifiesAndParses()
    {
        // Written by hand, as a publisher two years from now would write it: the fields this build knows, and four it does not.
        string json = """
            {"manifest":1,"version":"0.2.0-nightly.44","train":"nightly","commit":"abc1234","publishedUtc":"2026-09-22T00:00:00Z",
             "notes":"what changed","assets":[{"Platform":"windows","Kind":"installer","Name":"a.exe","Bytes":1,"Sha256":"ab","Url":"https://example.invalid/a.exe"}],
             "minimumWindows":"10.0.19041","signedBy":"a key this build has never seen","rollout":{"share":0.25,"after":"2026-09-23"},"deprecates":["0.1.0"]}
            """;
        var published = UpdateSignature.Publish(Encoding.UTF8.GetBytes(json), Private());

        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(published, Key.PublicKeyBase64));
        var body = published.Body!;
        Assert.Equal("0.2.0-nightly.44", body.Version);
        Assert.Equal("a.exe", body.For("windows", "installer")!.Name);

        // The same payload under the first format is refused, which is the fault this whole entry came from.
        var asTheFirstFormatWouldHaveIt = new SignedManifest(UpdateSignature.Algorithm, published.Signature, body);
        Assert.Equal(UpdateSignature.Refusal.BadSignature, UpdateSignature.Verify(asTheFirstFormatWouldHaveIt, Key.PublicKeyBase64));
    }

    /// <summary>Entry 139 section 4: a changed byte anywhere in the payload is refused, wherever in it the change is.</summary>
    [Fact]
    public void AChangedByteAnywhereInThePayloadIsRefused()
    {
        var published = UpdateSignature.Publish(Built(), Private());
        byte[] payload = published.Bytes!;

        foreach (int at in new[] { 0, 1, payload.Length / 3, payload.Length / 2, payload.Length - 2, payload.Length - 1 })
        {
            byte[] changed = [.. payload];
            changed[at] ^= 0x20;
            var tampered = published with { Payload = Convert.ToBase64String(changed) };
            Assert.True(
                UpdateSignature.Verify(tampered, Key.PublicKeyBase64) is UpdateSignature.Refusal.BadSignature or UpdateSignature.Refusal.UnknownManifest,
                $"a byte changed at {at} of {payload.Length} was not refused");
        }

        // And a signature from another key over the right bytes is refused too.
        var other = UpdateSignature.NewKeyPair();
        var elsewhere = UpdateSignature.Publish(Built(), Convert.FromBase64String(other.PrivateKeyBase64));
        Assert.Equal(UpdateSignature.Refusal.BadSignature, UpdateSignature.Verify(elsewhere, Key.PublicKeyBase64));
    }

    /// <summary>
    /// Entry 139 section 4: the first format's signed bytes, held to the string as they are. A change to this string is a change every
    /// installed build would refuse, so it must be looked at and not slipped past.
    /// </summary>
    [Fact]
    public void TheFirstFormatsSignedBytesAreHeldToARecordedString()
    {
        const string Recorded =
            """{"manifest":1,"version":"0.2.0-nightly.44","train":"nightly","commit":"abc1234","publishedUtc":"2026-09-22T00:00:00Z","notes":"what changed","assets":[{"Platform":"windows","Kind":"installer","Name":"grouplab-setup-win-x64.exe","Bytes":1234,"Sha256":"abcd","Url":"https://example.invalid/a.exe"}],"Offered":{"Major":0,"Minor":2,"Patch":0,"PreRelease":"nightly.44","Number":"0.2.0-nightly.44","IsPreRelease":true},"OnTrain":1}""";

        Assert.Equal(Recorded, Encoding.UTF8.GetString(Built().Signable()));
        Assert.Equal(Recorded, Encoding.UTF8.GetString(Built([new VersionNotes("0.2.0-nightly.44", "something")]).Legacy().Signable()));
    }

    /// <summary>
    /// Entry 139 section 2: the first format cannot gain a field however it is asked to. Signing is the one door into it, and it takes the
    /// legacy shape rather than what it was handed.
    /// </summary>
    [Fact]
    public void TheFirstFormatCannotGainAFieldHoweverItIsAskedTo()
    {
        var withVersions = Built([new VersionNotes("0.2.0-nightly.44", "the newest build's own change")]);
        var signed = UpdateSignature.Sign(withVersions, Private());

        Assert.Null(signed.Payload.Versions);
        Assert.DoesNotContain("versions", signed.ToJson(), StringComparison.Ordinal);
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(signed, Key.PublicKeyBase64));

        // The second format, from the same manifest, keeps them.
        Assert.NotNull(UpdateSignature.Publish(withVersions, Private()).Body!.Versions);
    }

    /// <summary>
    /// Entry 139 section 4's last test: nightly 37's updater, pinned as it was, against a manifest generated by today's code. If this ever
    /// fails, the build published that day stops being able to update itself, and the only way out is a download by hand.
    /// </summary>
    [Fact]
    public void AnOlderBuildsUpdaterStillAcceptsAManifestPublishedToday()
    {
        string published = UpdateSignature.Sign(Built([new VersionNotes("0.2.0-nightly.44", "notes it will never see")]), Private()).ToJson();

        Assert.True(Nightly37.Verifies(published, Key.PublicKeyBase64), "nightly 37 would refuse the manifest published today");
    }

    /// <summary>
    /// The verification exactly as nightly 37 does it, pinned here and never changed to match whatever the code does now. It is deliberately
    /// its own copy of the record, with only the fields that build knew, because the whole fault was a build meeting a field it did not know.
    /// </summary>
    private static class Nightly37
    {
        private sealed record Asset(string Platform, string Kind, string Name, long Bytes, string Sha256, string Url);

        private sealed record Manifest(
            [property: JsonPropertyName("manifest")] int ManifestVersion,
            [property: JsonPropertyName("version")] string Version,
            [property: JsonPropertyName("train")] string Train,
            [property: JsonPropertyName("commit")] string Commit,
            [property: JsonPropertyName("publishedUtc")] string PublishedUtc,
            [property: JsonPropertyName("notes")] string Notes,
            [property: JsonPropertyName("assets")] IReadOnlyList<Asset> Assets)
        {
            public SemanticVersion? Offered => SemanticVersion.Parse(Version);

            public UpdateTrain OnTrain => Train?.Trim().ToLowerInvariant() switch
            {
                "nightly" => UpdateTrain.Nightly,
                "beta" => UpdateTrain.Beta,
                "release" => UpdateTrain.Release,
                _ => UpdateTrain.Development,
            };
        }

        private sealed record Signed(
            [property: JsonPropertyName("algorithm")] string Algorithm,
            [property: JsonPropertyName("signature")] string Signature,
            [property: JsonPropertyName("manifest")] Manifest Payload);

        private static readonly JsonSerializerOptions Reading = new() { WriteIndented = true };

        private static readonly JsonSerializerOptions Signing = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static bool Verifies(string json, string publicKeyBase64)
        {
            var signed = JsonSerializer.Deserialize<Signed>(json, Reading);
            if (signed is null || !string.Equals(signed.Algorithm, "ecdsa-p256-sha256", StringComparison.Ordinal))
            {
                return false;
            }

            // The fault itself: the bytes checked are a fresh serialisation of what was read, not what arrived.
            byte[] signable = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(signed.Payload, Signing));
            using var key = System.Security.Cryptography.ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);
            return key.VerifyData(signable, Convert.FromBase64String(signed.Signature), System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence);
        }
    }
}
