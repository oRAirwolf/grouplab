using GroupLab.Cli.Spike;
using GroupLab.Core.Rendering;
using GroupLab.Core.Tests.Support;

namespace GroupLab.Core.Tests.Rendering;

/// <summary>
/// NOTES-FROM-PLANNING.md entry 77 section 3 item 2: a change to printed artwork cannot land until the corpus has been re-run. The corpus
/// detection counts are recorded against the fingerprint of every definition's printed PDF, and this test fails as soon as the renderer
/// prints something different from what the counts were measured against.
/// </summary>
public class ArtworkFingerprintTests
{
    [Fact]
    public void ThePrintedArtworkIsWhatTheCorpusCountsWereMeasuredAgainst()
    {
        var recorded = CorpusCounts.RecordedArtwork(Repo.PathTo(CorpusCounts.CommittedRecord));
        var current = ArtworkFingerprint.All(Repo.PathTo("targets"));
        var moved = current.Keys.Union(recorded.Keys).Where(k => !recorded.TryGetValue(k, out string? was) || !current.TryGetValue(k, out string? now) || was != now)
            .Order(StringComparer.Ordinal).ToList();
        Assert.True(moved.Count == 0,
            $"The printed artwork of {string.Join(", ", moved)} differs from what the corpus detection counts were measured against. " +
            "Printed artwork decides what render-and-difference reads on every sheet already printed. Run `grouplab corpus counts`, and with " +
            "`--local <manifest>` over any local shot sheets, compare the counts before and after, and record them with `--write` once the " +
            "change is understood (docs/NOTES-FROM-PLANNING.md entry 77 section 3).");
    }

    [Fact]
    public void TheFingerprintIsTheSameForTheSameDefinitionAndDiffersWhenTheArtworkDoes()
    {
        string file = Repo.PathTo("targets", "GL-CF25-LTR.gltd.json");
        string? first = ArtworkFingerprint.Of(file, out _);
        Assert.NotNull(first);
        Assert.Equal(first, ArtworkFingerprint.Of(file, out _));

        string other = Repo.PathTo("targets", "GL-CF25-A4.gltd.json");
        Assert.NotEqual(first, ArtworkFingerprint.Of(other, out _));
    }
}
