using GroupLab.Core.Updates;

namespace GroupLab.Core.Tests.Updates;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 138 section 5 and its test in section 6: the update bar is the one place that combines versions.
/// <para>
/// <b>Everywhere else a version's notes are only its own.</b> The release page shows one build a block, because a person reading it asks
/// what that build did. The bar is different: somebody on nightly 31 being offered nightly 40 has not seen 32 to 39 either, and all of it
/// is new to them. Showing them only the newest build's notes would hide eight builds' worth of work.
/// </para>
/// </summary>
public class SkippedVersionsTests
{
    private static readonly IReadOnlyList<VersionNotes> History =
    [
        new("0.2.0-nightly.40", "the fortieth build's own change"),
        new("0.2.0-nightly.38", "the thirty eighth build's own change"),
        new("0.2.0-nightly.35", "the thirty fifth build's own change"),
        new("0.2.0-nightly.31", "the thirty first build's own change"),
        new("0.2.0-nightly.28", "the twenty eighth build's own change"),
    ];

    /// <summary>Entry 138 section 5's own example: on 31, offered 40, every version between them and nothing older.</summary>
    [Fact]
    public void EveryVersionBetweenTheInstalledAndTheOfferedOne()
    {
        var between = SkippedVersions.Between(History, "0.2.0-nightly.31", "0.2.0-nightly.40");

        Assert.Equal(["0.2.0-nightly.40", "0.2.0-nightly.38", "0.2.0-nightly.35"], between.Select(v => v.Version));
    }

    /// <summary>The installed build is not included: the person has already seen it.</summary>
    [Fact]
    public void TheInstalledBuildIsNotIncluded()
    {
        var between = SkippedVersions.Between(History, "0.2.0-nightly.38", "0.2.0-nightly.40");

        Assert.Equal(["0.2.0-nightly.40"], between.Select(v => v.Version));
    }

    /// <summary>Nothing newer than the offered build, even where the manifest happens to know about one.</summary>
    [Fact]
    public void NothingNewerThanWhatIsBeingOffered()
    {
        var between = SkippedVersions.Between(History, "0.2.0-nightly.31", "0.2.0-nightly.35");

        Assert.Equal(["0.2.0-nightly.35"], between.Select(v => v.Version));
    }

    /// <summary>One new build reads as itself, with no version heading: the bar already says which build it is offering.</summary>
    [Fact]
    public void OneNewBuildIsJustItsNotes()
    {
        string text = SkippedVersions.Combined(History, "0.2.0-nightly.38", "0.2.0-nightly.40");

        Assert.Equal("the fortieth build's own change", text);
        Assert.Null(SkippedVersions.Says(History, "0.2.0-nightly.38", "0.2.0-nightly.40"));
    }

    /// <summary>Several are grouped by version, newest first, each under its own name.</summary>
    [Fact]
    public void SeveralAreGroupedByVersionNewestFirst()
    {
        string text = SkippedVersions.Combined(History, "0.2.0-nightly.31", "0.2.0-nightly.40");

        Assert.StartsWith("GroupLab 0.2.0-nightly.40", text, StringComparison.Ordinal);
        Assert.Contains("GroupLab 0.2.0-nightly.38", text, StringComparison.Ordinal);
        Assert.Contains("GroupLab 0.2.0-nightly.35", text, StringComparison.Ordinal);
        Assert.DoesNotContain("nightly.31", text, StringComparison.Ordinal);
        Assert.DoesNotContain("nightly.28", text, StringComparison.Ordinal);

        Assert.Equal("3 builds are new to you, newest first.", SkippedVersions.Says(History, "0.2.0-nightly.31", "0.2.0-nightly.40"));
    }

    /// <summary>
    /// A manifest written before this existed carries no per-version notes, and an older build's bar must still say something. It falls back
    /// to the one set of notes the manifest has always had.
    /// </summary>
    [Fact]
    public void WithNoPerVersionNotesItFallsBackToTheOnesTheManifestAlwaysHad()
    {
        Assert.Empty(SkippedVersions.Between(null, "0.2.0-nightly.31", "0.2.0-nightly.40"));
        Assert.Equal("what the manifest said", SkippedVersions.Combined(null, "0.2.0-nightly.31", "0.2.0-nightly.40", "what the manifest said"));
        Assert.Equal("", SkippedVersions.Combined(null, "0.2.0-nightly.31", "0.2.0-nightly.40"));
    }

    /// <summary>A version that does not parse is left out rather than guessed at, and does not take the rest with it.</summary>
    [Fact]
    public void AVersionThatDoesNotParseIsLeftOut()
    {
        IReadOnlyList<VersionNotes> odd =
        [
            new("not a version", "something"),
            new("0.2.0-nightly.40", "the fortieth build's own change"),
        ];

        Assert.Equal(["0.2.0-nightly.40"], SkippedVersions.Between(odd, "0.2.0-nightly.31", "0.2.0-nightly.40").Select(v => v.Version));
    }

    /// <summary>With no installed version known, everything up to the offered build is new.</summary>
    [Fact]
    public void WithNoInstalledVersionEverythingUpToTheOfferedOneIsNew()
    {
        var between = SkippedVersions.Between(History, null, "0.2.0-nightly.35");

        Assert.Equal(["0.2.0-nightly.35", "0.2.0-nightly.31", "0.2.0-nightly.28"], between.Select(v => v.Version));
    }
    /// <summary>
    /// <b>The rule this feature ran into, held so nobody meets it the hard way again.</b>
    /// <para>
    /// A manifest is verified by serialising the record it was read into and checking those bytes against the signature. A build that does
    /// not know a field drops it on the way back out, so the bytes it checks are not the bytes that were signed. Adding one field for entry
    /// 138 section 5 published nightly 42 with it, and nightly 37 answered <c>update.check result=Refused refusal=BadSignature</c>: every
    /// build already installed was unable to update itself at all, which is worse than the fault that change was travelling with.
    /// </para>
    /// <para>
    /// So the signed payload's shape is fixed until every build in the field understands a new one. This test names the fields, so adding
    /// one fails here rather than in somebody's copy of GroupLab.
    /// </para>
    /// </summary>
    [Fact]
    public void TheSignedManifestHasExactlyTheseFieldsAndAddingOneBreaksEveryInstalledBuild()
    {
        var written = System.Text.Json.Nodes.JsonNode.Parse(
            System.Text.Encoding.UTF8.GetString(
                new UpdateManifest(UpdateManifest.Current, "0.2.0-nightly.1", "nightly", "abc1234", "2026-01-01T00:00:00Z", "notes",
                    [new UpdateAsset("windows", "installer", "a.exe", 1, "hash", "https://example.invalid/a.exe")]).Signable()))!.AsObject();

        // Offered and OnTrain are computed from the others and still land in the signed bytes, which is its own trap: changing what either
        // one returns changes the signature of every manifest as surely as adding a field would.
        Assert.Equal(
            ["manifest", "version", "train", "commit", "publishedUtc", "notes", "assets", "Offered", "OnTrain"],
            written.Select(p => p.Key));
    }
}
