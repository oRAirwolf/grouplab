using System.Text;
using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 123 section 2.7, found by the real update test: <b>every published manifest was refused as BadSignature on
/// Windows.</b>
/// <para>
/// The signed bytes were indented JSON, and from .NET 9 the writer's newline defaults to <see cref="Environment.NewLine"/>. So the same
/// manifest serialised with a carriage return on Windows and without one on Linux. The nightly is signed on a Linux runner and verified on a
/// Windows machine, so the bytes never matched and the updater could not install anything at all.
/// </para>
/// <para>
/// <b>No existing test could have caught it</b>, because a test signs and verifies in one process on one machine, where the two agree with
/// each other however wrong they both are. These do catch it: they hold the actual bytes, not the round trip.
/// </para>
/// </summary>
public class SignableBytesTests
{
    private static UpdateManifest Manifest(string notes = "**Changed**\n\n- one thing\n- another\n") =>
        new(UpdateManifest.Current, "0.2.0-nightly.16", "nightly", "b39b7afd24f631b345b7b71c3196a47af291f490", "2026-09-21T09:32:18Z", notes,
            [new UpdateAsset("windows", "installer", "grouplab-setup-win-x64.exe", 102053144, new string('a', 64), "https://example.invalid/x")]);

    /// <summary>
    /// The bytes carry no line break of any kind. This is the whole fault in one assertion: a line break is the only thing in JSON whose
    /// spelling depends on the machine, so bytes without one cannot differ between a Linux runner and a Windows desktop.
    /// </summary>
    [Fact]
    public void TheSignedBytesCarryNoLineBreakSoTheyCannotDifferBetweenMachines()
    {
        byte[] signable = Manifest().Signable();

        Assert.DoesNotContain((byte)'\r', signable);

        // The notes hold newlines of their own, and those are escaped inside the JSON string as \n rather than written as bytes. So a raw
        // line feed byte would have to come from the formatting, which is what this is about.
        Assert.DoesNotContain((byte)'\n', signable);
        Assert.Contains(@"\n", Encoding.UTF8.GetString(signable), StringComparison.Ordinal);
    }

    /// <summary>
    /// The same manifest gives the same bytes whatever the machine's newline is. Nothing in the test can change
    /// <see cref="Environment.NewLine"/>, so this holds the thing that would have changed with it: the exact bytes, recorded here.
    /// </summary>
    [Fact]
    public void TheSignedBytesAreExactlyThese()
    {
        string actual = Encoding.UTF8.GetString(Manifest("hello").Signable());

        const string Expected =
            """{"manifest":1,"version":"0.2.0-nightly.16","train":"nightly","commit":"b39b7afd24f631b345b7b71c3196a47af291f490","publishedUtc":"2026-09-21T09:32:18Z","notes":"hello","assets":[{"Platform":"windows","Kind":"installer","Name":"grouplab-setup-win-x64.exe","Bytes":102053144,"Sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","Url":"https://example.invalid/x"}],"Offered":{"Major":0,"Minor":2,"Patch":0,"PreRelease":"nightly.16","Number":"0.2.0-nightly.16","IsPreRelease":true},"OnTrain":1}""";

        // If this fails, the bytes that get signed have changed. That is not a formatting detail: every manifest signed before the change is
        // refused by every build after it, and the other way round. It is a decision, so it has to be made here on purpose.
        Assert.Equal(Expected, actual);
    }

    /// <summary>A signature made from these bytes verifies, which is the part that already worked and must keep working.</summary>
    [Fact]
    public void AManifestStillVerifiesAgainstTheKeyThatSignedIt()
    {
        var key = UpdateSignature.NewKeyPair();
        var signed = UpdateSignature.Sign(Manifest(), Convert.FromBase64String(key.PrivateKeyBase64));

        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(signed, key.PublicKeyBase64));

        // And through the file, which is what actually travels: the file is still indented for a person to read, and that no longer matters.
        var read = SignedManifest.Read(signed.ToJson());
        Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(read, key.PublicKeyBase64));
    }

    /// <summary>
    /// The file a person downloads may be written with whatever line endings, and the signature still verifies. This is the case that was
    /// broken: the manifest arrived from a Linux runner and was read on Windows.
    /// </summary>
    [Fact]
    public void AManifestVerifiesHoweverTheFileItArrivedInWasWritten()
    {
        var key = UpdateSignature.NewKeyPair();
        var signed = UpdateSignature.Sign(Manifest(), Convert.FromBase64String(key.PrivateKeyBase64));
        string json = signed.ToJson();

        foreach (string rewritten in new[] { json.ReplaceLineEndings("\n"), json.ReplaceLineEndings("\r\n"), json.ReplaceLineEndings("\r") })
        {
            var read = SignedManifest.Read(rewritten);
            Assert.NotNull(read);
            Assert.Equal(UpdateSignature.Refusal.None, UpdateSignature.Verify(read, key.PublicKeyBase64));
        }
    }
}
